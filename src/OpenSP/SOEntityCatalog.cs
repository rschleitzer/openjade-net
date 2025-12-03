// Copyright (c) 1994, 1995, 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Factory class for creating OASIS catalog managers
public class SOCatalogManager : ExtendEntityManager.CatalogManager
{
    // static ExtendEntityManager::CatalogManager *make(...);
    public static ExtendEntityManager.CatalogManager make(Vector<StringC> sysids,
                                                           nuint nSysidsMustExist,
                                                           CharsetInfo? sysidCharset,
                                                           CharsetInfo? catalogCharset,
                                                           Boolean useDocCatalog)
    {
        return new SOCatalogManagerImpl(sysids, nSysidsMustExist, sysidCharset, catalogCharset, useDocCatalog);
    }

    public override ConstPtr<EntityCatalog> makeCatalog(StringC systemId,
                                                         CharsetInfo docCharset,
                                                         ExtendEntityManager? em,
                                                         Messenger mgr)
    {
        throw new NotImplementedException();
    }

    public override Boolean mapCatalog(ParsedSystemId systemId,
                                       ExtendEntityManager? em,
                                       Messenger mgr)
    {
        throw new NotImplementedException();
    }
}

// Implementation of catalog manager
internal class SOCatalogManagerImpl : SOCatalogManager
{
    private nuint nSystemCatalogsMustExist_;
    private Vector<StringC> systemCatalogs_ = new Vector<StringC>();
    private CharsetInfo? sysidCharset_;
    private CharsetInfo? catalogCharset_;
    private Boolean useDocCatalog_;

    public SOCatalogManagerImpl(Vector<StringC> sysids,
                                nuint nSysidsMustExist,
                                CharsetInfo? sysidCharset,
                                CharsetInfo? catalogCharset,
                                Boolean useDocCatalog)
    {
        nSystemCatalogsMustExist_ = nSysidsMustExist;
        sysidCharset_ = sysidCharset;
        catalogCharset_ = catalogCharset;
        useDocCatalog_ = useDocCatalog;

        for (nuint i = 0; i < sysids.size(); i++)
        {
            systemCatalogs_.push_back(new StringC(sysids[i]));
        }
    }

    public override ConstPtr<EntityCatalog> makeCatalog(StringC systemId,
                                                         CharsetInfo docCharset,
                                                         ExtendEntityManager? em,
                                                         Messenger mgr)
    {
        if (em == null)
            return new ConstPtr<EntityCatalog>();

        SOEntityCatalog catalog = new SOEntityCatalog(new Ptr<ExtendEntityManager>(em));

        // Load system catalogs
        for (nuint i = 0; i < systemCatalogs_.size(); i++)
        {
            Boolean mustExist = (i < nSystemCatalogsMustExist_);
            CatalogParser parser = new CatalogParser(catalogCharset_ ?? docCharset);
            parser.parseCatalog(systemCatalogs_[i], mustExist, sysidCharset_ ?? docCharset,
                               catalogCharset_ ?? docCharset, null, catalog, mgr);
        }

        // Load document catalog if configured
        if (useDocCatalog_ && systemId.size() > 0)
        {
            CatalogParser parser = new CatalogParser(catalogCharset_ ?? docCharset);
            parser.parseCatalog(systemId, false, sysidCharset_ ?? docCharset,
                               catalogCharset_ ?? docCharset, null, catalog, mgr);
        }

        catalog.endCatalog();
        return new ConstPtr<EntityCatalog>(catalog);
    }

    public override Boolean mapCatalog(ParsedSystemId systemId,
                                       ExtendEntityManager? em,
                                       Messenger mgr)
    {
        // Process catalog map entries
        for (nuint i = 0; i < systemId.maps.size(); i++)
        {
            ParsedSystemId.Map map = systemId.maps[i];
            if (map.type == ParsedSystemId.Map.Type.catalogDocument)
            {
                // Document catalogs are handled by makeCatalog
            }
            else if (map.type == ParsedSystemId.Map.Type.catalogPublic)
            {
                // Public catalog mapping - look up the public ID
            }
        }
        return true;
    }
}

// Catalog entry for storing public/system ID mappings
public class CatalogEntry
{
    public StringC systemId = new StringC();
    public Location loc = new Location();
    public nuint baseNumber;

    public CatalogEntry()
    {
        baseNumber = 0;
    }

    public CatalogEntry(CatalogEntry x)
    {
        systemId = new StringC(x.systemId);
        loc = new Location(x.loc);
        baseNumber = x.baseNumber;
    }
}

// OASIS catalog implementation
public class SOEntityCatalog : EntityCatalog
{
    // Internal table for public/entity lookups
    private class Table
    {
        private Dictionary<StringC, CatalogEntry> overrideEntries_ = new Dictionary<StringC, CatalogEntry>();
        private Dictionary<StringC, CatalogEntry> normalEntries_ = new Dictionary<StringC, CatalogEntry>();

        public Table()
        {
        }

        public CatalogEntry? lookup(StringC key, Boolean overrideOnly)
        {
            if (!overrideOnly && normalEntries_.TryGetValue(key, out CatalogEntry? normalEntry))
                return normalEntry;
            if (overrideEntries_.TryGetValue(key, out CatalogEntry? overrideEntry))
                return overrideEntry;
            return null;
        }

        public CatalogEntry? lookup(StringC key, SubstTable substTable, Boolean overrideOnly)
        {
            // Create substituted key
            StringC substKey = new StringC();
            for (nuint i = 0; i < key.size(); i++)
                substKey.operatorPlusAssign(substTable[key[i]]);
            return lookup(substKey, overrideOnly);
        }

        public void insert(StringC key, CatalogEntry entry, Boolean @override)
        {
            if (@override)
            {
                if (!overrideEntries_.ContainsKey(key))
                    overrideEntries_[key] = entry;
            }
            else
            {
                if (!normalEntries_.ContainsKey(key))
                    normalEntries_[key] = entry;
            }
        }

        public nuint count()
        {
            return (nuint)(overrideEntries_.Count + normalEntries_.Count);
        }
    }

    private Table publicIds_ = new Table();
    private Table delegates_ = new Table();
    private Dictionary<StringC, CatalogEntry> dtdDecls_ = new Dictionary<StringC, CatalogEntry>();
    private Dictionary<StringC, CatalogEntry> systemIds_ = new Dictionary<StringC, CatalogEntry>();
    private Table[] names_ = new Table[5];
    private nuint catalogNumber_;
    private Boolean haveSgmlDecl_;
    private StringC sgmlDecl_ = new StringC();
    private Location sgmlDeclLoc_ = new Location();
    private nuint sgmlDeclBaseNumber_;
    private StringC document_ = new StringC();
    private Boolean haveDocument_;
    private Location documentLoc_ = new Location();
    private nuint documentBaseNumber_;
    private Boolean haveCurrentBase_;
    private Vector<Location> base_ = new Vector<Location>();
    private Ptr<ExtendEntityManager> em_ = new Ptr<ExtendEntityManager>();

    public SOEntityCatalog(Ptr<ExtendEntityManager> em)
    {
        em_ = em;
        catalogNumber_ = 0;
        haveSgmlDecl_ = false;
        haveDocument_ = false;
        haveCurrentBase_ = false;
        sgmlDeclBaseNumber_ = 0;
        documentBaseNumber_ = 0;

        for (int i = 0; i < 5; i++)
            names_[i] = new Table();
    }

    // Boolean document(const CharsetInfo &, Messenger &, StringC &) const;
    public Boolean document(CharsetInfo charset, Messenger mgr, StringC result)
    {
        if (!haveDocument_)
            return false;

        return expandCatalogSystemId(document_, documentLoc_, documentBaseNumber_,
                                     false, charset, null, mgr, result);
    }

    // Boolean sgmlDecl(const CharsetInfo &, Messenger &, const StringC &, StringC &) const;
    public override Boolean sgmlDecl(CharsetInfo charset, Messenger mgr, StringC sysid, StringC result)
    {
        if (!haveSgmlDecl_)
            return false;

        return expandCatalogSystemId(sgmlDecl_, sgmlDeclLoc_, sgmlDeclBaseNumber_,
                                     false, charset, null, mgr, result);
    }

    // Boolean lookup(const EntityDecl &, const Syntax &, const CharsetInfo &, Messenger &, StringC &) const;
    public override Boolean lookup(EntityDecl entity, EntityCatalog.Syntax syntax, CharsetInfo charset,
                                   Messenger mgr, StringC result)
    {
        // Look up by public ID first
        StringC? publicId = entity.publicIdPointer();
        if (publicId != null && publicId.size() > 0)
        {
            Boolean haveSystemId = entity.systemIdPointer() != null;
            Boolean delegated;
            CatalogEntry? entry = findBestPublicEntry(publicId, haveSystemId, charset, out delegated);
            if (entry != null)
            {
                Boolean isNdata = entity.dataType() == EntityDecl.DataType.ndata;
                return expandCatalogSystemId(entry.systemId, entry.loc, entry.baseNumber,
                                             isNdata, charset, publicId, mgr, result);
            }
        }

        // Try entity name
        int declType = (int)entity.declType();
        if (declType >= 0 && declType < 5)
        {
            Boolean haveSystemId = entity.systemIdPointer() != null;
            CatalogEntry? nameEntry = names_[declType].lookup(entity.name(),
                                                              syntax.upperSubstTable(),
                                                              haveSystemId);
            if (nameEntry != null)
            {
                Boolean isNdata = entity.dataType() == EntityDecl.DataType.ndata;
                return expandCatalogSystemId(nameEntry.systemId, nameEntry.loc, nameEntry.baseNumber,
                                             isNdata, charset, null, mgr, result);
            }
        }

        return false;
    }

    // Boolean lookupPublic(const StringC &, const CharsetInfo &, Messenger &, StringC &) const;
    public override Boolean lookupPublic(StringC publicId, CharsetInfo charset,
                                         Messenger mgr, StringC result)
    {
        Boolean delegated;
        CatalogEntry? entry = findBestPublicEntry(publicId, false, charset, out delegated);
        if (entry != null)
        {
            return expandCatalogSystemId(entry.systemId, entry.loc, entry.baseNumber,
                                         false, charset, publicId, mgr, result);
        }
        return false;
    }

    // Boolean lookupChar(const StringC &, const CharsetInfo &, Messenger &, UnivChar &) const;
    public override Boolean lookupChar(StringC name, CharsetInfo charset,
                                       Messenger mgr, out UnivChar c)
    {
        c = 0;
        // Character entity lookup not implemented
        return false;
    }

    // void addPublicId(StringC &publicId, StringC &systemId, const Location &, Boolean override);
    public void addPublicId(StringC publicId, StringC systemId, Location loc, Boolean @override)
    {
        CatalogEntry entry = new CatalogEntry();
        entry.systemId.assign(systemId.data()!, systemId.size());
        entry.loc = new Location(loc);
        entry.baseNumber = haveCurrentBase_ ? base_.size() - 1 : 0;
        publicIds_.insert(normalizePublicId(publicId), entry, @override);
    }

    // void addDtdDecl(StringC &publicId, StringC &systemId, const Location &, Boolean override);
    public void addDtdDecl(StringC publicId, StringC systemId, Location loc, Boolean @override)
    {
        StringC key = normalizePublicId(publicId);
        if (!dtdDecls_.ContainsKey(key) || @override)
        {
            CatalogEntry entry = new CatalogEntry();
            entry.systemId.assign(systemId.data()!, systemId.size());
            entry.loc = new Location(loc);
            entry.baseNumber = haveCurrentBase_ ? base_.size() - 1 : 0;
            dtdDecls_[key] = entry;
        }
    }

    // void addDelegate(StringC &prefix, StringC &systemId, const Location &, Boolean override);
    public void addDelegate(StringC prefix, StringC systemId, Location loc, Boolean @override)
    {
        CatalogEntry entry = new CatalogEntry();
        entry.systemId.assign(systemId.data()!, systemId.size());
        entry.loc = new Location(loc);
        entry.baseNumber = haveCurrentBase_ ? base_.size() - 1 : 0;
        delegates_.insert(normalizePublicId(prefix), entry, @override);
    }

    // void addSystemId(StringC &systemId, StringC &replSystemId, const Location &);
    public void addSystemId(StringC systemId, StringC replSystemId, Location loc)
    {
        if (!systemIds_.ContainsKey(systemId))
        {
            CatalogEntry entry = new CatalogEntry();
            entry.systemId.assign(replSystemId.data()!, replSystemId.size());
            entry.loc = new Location(loc);
            entry.baseNumber = haveCurrentBase_ ? base_.size() - 1 : 0;
            systemIds_[systemId] = entry;
        }
    }

    // void addName(StringC &name, DeclType, StringC &systemId, const Location &, Boolean override);
    public void addName(StringC name, EntityDecl.DeclType declType, StringC systemId,
                        Location loc, Boolean @override)
    {
        int idx = (int)declType;
        if (idx >= 0 && idx < 5)
        {
            CatalogEntry entry = new CatalogEntry();
            entry.systemId.assign(systemId.data()!, systemId.size());
            entry.loc = new Location(loc);
            entry.baseNumber = haveCurrentBase_ ? base_.size() - 1 : 0;
            names_[idx].insert(name, entry, @override);
        }
    }

    // void setSgmlDecl(StringC &str, const Location &loc);
    public void setSgmlDecl(StringC str, Location loc)
    {
        if (!haveSgmlDecl_)
        {
            haveSgmlDecl_ = true;
            sgmlDecl_.assign(str.data()!, str.size());
            sgmlDeclLoc_ = new Location(loc);
            sgmlDeclBaseNumber_ = haveCurrentBase_ ? base_.size() - 1 : 0;
        }
    }

    // void setDocument(StringC &str, const Location &loc);
    public void setDocument(StringC str, Location loc)
    {
        if (!haveDocument_)
        {
            haveDocument_ = true;
            document_.assign(str.data()!, str.size());
            documentLoc_ = new Location(loc);
            documentBaseNumber_ = haveCurrentBase_ ? base_.size() - 1 : 0;
        }
    }

    // void setBase(const Location &loc);
    public void setBase(Location loc)
    {
        haveCurrentBase_ = true;
        base_.push_back(new Location(loc));
    }

    // void endCatalog();
    public void endCatalog()
    {
        catalogNumber_++;
        haveCurrentBase_ = false;
    }

    // const Ptr<ExtendEntityManager> &entityManager();
    public Ptr<ExtendEntityManager> entityManager()
    {
        return em_;
    }

    private Boolean expandCatalogSystemId(StringC str, Location loc, nuint baseNumber,
                                          Boolean isNdata, CharsetInfo charset,
                                          StringC? lookupPublicId, Messenger mgr, StringC result)
    {
        if (em_.isNull())
            return false;

        // Simply copy the system ID for now
        // Full implementation would resolve relative paths based on base
        result.assign(str.data()!, str.size());
        return true;
    }

    private CatalogEntry? findBestPublicEntry(StringC publicId, Boolean overrideOnly,
                                              CharsetInfo charset, out Boolean delegated)
    {
        delegated = false;
        StringC normalizedId = normalizePublicId(publicId);

        // Look for direct match
        CatalogEntry? entry = publicIds_.lookup(normalizedId, overrideOnly);
        if (entry != null)
            return entry;

        // Look for delegate match (longest prefix wins)
        // Simplified - just check for exact prefix match
        CatalogEntry? delegateEntry = delegates_.lookup(normalizedId, overrideOnly);
        if (delegateEntry != null)
        {
            delegated = true;
            return delegateEntry;
        }

        return null;
    }

    private static StringC normalizePublicId(StringC publicId)
    {
        // Normalize public ID: collapse whitespace, uppercase
        StringC result = new StringC();
        Boolean lastWasSpace = true;

        for (nuint i = 0; i < publicId.size(); i++)
        {
            Char c = publicId[i];
            Boolean isSpace = (c == ' ' || c == '\t' || c == '\r' || c == '\n');

            if (isSpace)
            {
                if (!lastWasSpace)
                {
                    result.operatorPlusAssign((Char)' ');
                    lastWasSpace = true;
                }
            }
            else
            {
                // Uppercase for case-insensitive matching
                if (c >= 'a' && c <= 'z')
                    c = (Char)(c - 32);
                result.operatorPlusAssign(c);
                lastWasSpace = false;
            }
        }

        // Remove trailing space
        if (result.size() > 0 && result[result.size() - 1] == ' ')
            result.resize(result.size() - 1);

        return result;
    }
}

// Simplified catalog file parser
internal class CatalogParser
{
    private CharsetInfo charset_;

    public CatalogParser(CharsetInfo charset)
    {
        charset_ = charset;
    }

    public void parseCatalog(StringC sysid, Boolean mustExist, CharsetInfo sysidCharset,
                             CharsetInfo catalogCharset, InputSourceOrigin? origin,
                             SOEntityCatalog catalog, Messenger mgr)
    {
        // Simplified catalog parsing
        // Full implementation would read and parse the catalog file
        // For now, just set the base location if we have an origin
        if (origin != null)
        {
            catalog.setBase(origin.parent()!);
        }
    }
}
