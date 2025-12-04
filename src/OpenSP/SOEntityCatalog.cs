// Copyright (c) 1994, 1995, 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Factory class for creating OASIS catalog managers
public abstract class SOCatalogManager : ExtendEntityManager.CatalogManager
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

    // Abstract methods - implemented by SOCatalogManagerImpl
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
        CatalogParser parser = new CatalogParser(catalogCharset_ ?? docCharset);

        // Load system catalogs that must exist (specified via -c option)
        for (nuint i = 0; i < nSystemCatalogsMustExist_; i++)
        {
            parser.parseCatalog(systemCatalogs_[i], true, sysidCharset_ ?? docCharset,
                               catalogCharset_ ?? docCharset, null, catalog, mgr);
        }

        // Load document catalog if configured (looks for "catalog" file in document directory)
        if (useDocCatalog_)
            addCatalogsForDocument(parser, systemId, catalog, docCharset, mgr);

        // Load remaining system catalogs (from SGML_CATALOG_FILES env) that may not exist
        for (nuint i = nSystemCatalogsMustExist_; i < systemCatalogs_.size(); i++)
        {
            parser.parseCatalog(systemCatalogs_[i], false, sysidCharset_ ?? docCharset,
                               catalogCharset_ ?? docCharset, null, catalog, mgr);
        }

        catalog.endCatalog();
        return new ConstPtr<EntityCatalog>(catalog);
    }

    // void addCatalogsForDocument(CatalogParser &parser, StringC &sysid, SOEntityCatalog *,
    //                              const CharsetInfo &charset, Messenger &mgr) const;
    private void addCatalogsForDocument(CatalogParser parser, StringC sysid,
                                        SOEntityCatalog catalog, CharsetInfo charset,
                                        Messenger mgr)
    {
        Ptr<ExtendEntityManager> em = catalog.entityManager();
        if (em.isNull())
            return;

        ParsedSystemId v = new ParsedSystemId();
        if (!em.pointer()!.parseSystemId(sysid, charset, false, null, mgr, v))
            return;

        // Handle catalog map entries (for <CATALOG> type system IDs)
        if (v.maps.size() > 0)
        {
            if (v.maps[0].type == ParsedSystemId.Map.Type.catalogDocument)
            {
                // Remove the catalog map entry
                Vector<ParsedSystemId.Map> newMaps = new Vector<ParsedSystemId.Map>();
                for (nuint i = 1; i < v.maps.size(); i++)
                    newMaps.push_back(v.maps[i]);
                v.maps = newMaps;

                StringC tem = new StringC();
                v.unparse(charset, false, tem);
                parser.parseCatalog(tem, true, charset, catalogCharset_ ?? charset,
                                   null, catalog, mgr);
                if (!catalog.document(charset, mgr, sysid))
                {
                    mgr.message(CatalogMessages.noDocumentEntry, new StringMessageArg(tem));
                    sysid.resize(0);
                }
            }
            return;
        }

        // Look for "catalog" file in the same directory as each storage object
        Vector<StringC> catalogs = new Vector<StringC>();
        for (nuint i = 0; i < v.size(); i++)
        {
            StorageObjectSpec spec = v[i];
            if (spec.storageManager != null && spec.storageManager.inheritable())
            {
                // Create catalog system ID
                ParsedSystemId catalogId = new ParsedSystemId();
                catalogId.resize(1);
                StorageObjectSpec catSpec = catalogId[0];
                catSpec.storageManager = spec.storageManager;
                catSpec.codingSystemType = spec.codingSystemType;
                catSpec.codingSystemName = spec.codingSystemName;

                // Set specId to "catalog" using the storage manager's charset
                CharsetInfo? idCharset = spec.storageManager.idCharset();
                if (idCharset != null)
                    catSpec.specId = idCharset.execToDesc("catalog");
                else
                {
                    // Fallback: create "catalog" directly
                    catSpec.specId = new StringC();
                    catSpec.specId.append("catalog");
                }

                // Resolve "catalog" relative to the document's specId
                spec.storageManager.resolveRelative(spec.specId, catSpec.specId, false);

                catSpec.baseId = new StringC(spec.baseId);
                catSpec.records = spec.records;
                catalogId[0] = catSpec;

                // Unparse to get the system ID string
                StringC tem = new StringC();
                catalogId.unparse(charset, false, tem);

                // Check if we already have this catalog
                Boolean duplicate = false;
                for (nuint j = 0; j < catalogs.size(); j++)
                {
                    if (tem == catalogs[j])
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate && tem.size() > 0)
                    catalogs.push_back(tem);
            }
        }

        // Parse the catalog files (mustExist=false, so missing catalogs are ignored)
        for (nuint i = 0; i < catalogs.size(); i++)
        {
            parser.parseCatalog(catalogs[i], false, charset,
                               catalogCharset_ ?? charset, InputSourceOrigin.make(), catalog, mgr);
        }
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

        // Fallback: if entity has a system ID, expand it directly
        StringC? sysId = entity.systemIdPointer();
        if (sysId != null && sysId.size() > 0 && !em_.isNull())
        {
            Boolean isNdata = entity.dataType() == EntityDecl.DataType.ndata;
            return em_.pointer()!.expandSystemId(sysId, entity.defLocation(), isNdata,
                                                 charset, null, mgr, result);
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

        // Use the base location if baseNumber > 0, otherwise use loc
        Location baseLoc = (baseNumber > 0 && baseNumber <= base_.size())
                          ? base_[baseNumber - 1]
                          : loc;

        return em_.pointer()!.expandSystemId(str, baseLoc, isNdata, charset,
                                             lookupPublicId, mgr, result);
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

// Full catalog file parser - ported from OpenSP SOEntityCatalog.cxx
internal class CatalogParser : IMessenger
{
    // Parameter types returned by parseParam
    public enum Param
    {
        eofParam,
        literalParam,
        nameParam,
        percentParam
    }

    // Character categories
    private const int catData = 0;
    private const int catEof = 1;
    private const int catNul = 2;
    private const int catLit = 3;
    private const int catLita = 4;
    private const int catMinus = 5;
    private const int catS = 6;
    private const int catMin = 7;  // minimum data characters

    private const uint minimumLiteral = 0x01;

    // Character category table
    private int[] categoryTable_ = new int[65536];

    // Keywords
    private StringC entityKey_;
    private StringC publicKey_;
    private StringC systemKey_;
    private StringC doctypeKey_;
    private StringC linktypeKey_;
    private StringC notationKey_;
    private StringC overrideKey_;
    private StringC sgmlDeclKey_;
    private StringC documentKey_;
    private StringC catalogKey_;
    private StringC yesKey_;
    private StringC noKey_;
    private StringC baseKey_;
    private StringC delegateKey_;
    private StringC dtddeclKey_;
    private StringC sgmlKey_;

    // Substitution table for case-insensitive comparison
    private SubstTable substTable_ = new SubstTable();

    // Special characters
    private Char minus_;
    private Char tab_;
    private Char re_;
    private Char rs_;
    private Char space_;

    // Parser state
    private InputSource? in_;
    private SOEntityCatalog? catalog_;
    private Messenger? mgr_;
    private Boolean override_;
    private StringC param_ = new StringC();
    private Location paramLoc_ = new Location();
    private CharsetInfo catalogCharset_;

    public CatalogParser(CharsetInfo charset)
    {
        catalogCharset_ = charset;

        // Initialize category table - default is data
        for (int i = 0; i < categoryTable_.Length; i++)
            categoryTable_[i] = catData;

        // Initialize keywords using ASCII strings (catalog files are ASCII)
        entityKey_ = makeKeyword("ENTITY");
        publicKey_ = makeKeyword("PUBLIC");
        systemKey_ = makeKeyword("SYSTEM");
        doctypeKey_ = makeKeyword("DOCTYPE");
        linktypeKey_ = makeKeyword("LINKTYPE");
        notationKey_ = makeKeyword("NOTATION");
        overrideKey_ = makeKeyword("OVERRIDE");
        sgmlDeclKey_ = makeKeyword("SGMLDECL");
        documentKey_ = makeKeyword("DOCUMENT");
        catalogKey_ = makeKeyword("CATALOG");
        yesKey_ = makeKeyword("YES");
        noKey_ = makeKeyword("NO");
        baseKey_ = makeKeyword("BASE");
        delegateKey_ = makeKeyword("DELEGATE");
        dtddeclKey_ = makeKeyword("DTDDECL");
        sgmlKey_ = makeKeyword("SGML");

        // Set up character categories using direct ASCII values
        // (catalog files are ASCII regardless of document charset)
        string lcletters = "abcdefghijklmnopqrstuvwxyz";
        string ucletters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string minChars = "0123456789-.'()+,/:=?";
        byte[] wwwMinChars = { 33, 35, 36, 37, 42, 59, 64, 95 };  // ! # $ % * ; @ _
        string sChars = " \n\r\t";

        // NUL character
        categoryTable_[0] = catNul;

        // Letters (also set up substitution for case-insensitive matching)
        for (int i = 0; i < lcletters.Length; i++)
        {
            Char lc = (Char)lcletters[i];
            Char uc = (Char)ucletters[i];
            substTable_.addSubst(lc, uc);
            categoryTable_[lc] = catMin;
            categoryTable_[uc] = catMin;
        }

        // Whitespace
        for (int i = 0; i < sChars.Length; i++)
        {
            Char c = (Char)sChars[i];
            categoryTable_[c] = catS;
        }

        // Minimum data characters
        for (int i = 0; i < minChars.Length; i++)
        {
            Char c = (Char)minChars[i];
            categoryTable_[c] = catMin;
        }

        // WWW minimum data characters
        for (int i = 0; i < wwwMinChars.Length; i++)
        {
            Char c = (Char)wwwMinChars[i];
            categoryTable_[c] = catMin;
        }

        // Quote characters
        categoryTable_['\''] = catLita;
        categoryTable_['"'] = catLit;

        // Minus
        minus_ = '-';
        categoryTable_[minus_] = catMinus;

        // Special chars
        tab_ = '\t';
        re_ = '\r';
        rs_ = '\n';
        space_ = ' ';
    }

    // Helper to create StringC from ASCII string
    private static StringC makeKeyword(string s)
    {
        StringC result = new StringC();
        foreach (char c in s)
            result.operatorPlusAssign((Char)c);
        return result;
    }

    private int getCategory(Xchar c)
    {
        if (c < 0) return catEof;
        if (c < categoryTable_.Length) return categoryTable_[c];
        return catData;
    }

    public void parseCatalog(StringC sysid, Boolean mustExist, CharsetInfo sysidCharset,
                             CharsetInfo catalogCharset, InputSourceOrigin? origin,
                             SOEntityCatalog catalog, Messenger mgr)
    {
        Ptr<ExtendEntityManager> em = catalog.entityManager();
        if (em.isNull())
            return;

        uint flags = mustExist ? 0u : (uint)ExtendEntityManager.mayNotExist;
        in_ = em.pointer()!.open(sysid, sysidCharset, origin, flags, mgr);
        if (in_ == null)
            return;

        catalog_ = catalog;
        mgr_ = mgr;
        override_ = false;

        Boolean recovering = false;
        Vector<StringC> subSysids = new Vector<StringC>();
        Vector<Location> subSysidLocs = new Vector<Location>();

        for (; ; )
        {
            Param parm = parseParam(0);
            if (parm == Param.nameParam)
            {
                upcase(param_);
                Boolean wasRecovering = recovering;
                recovering = false;

                // Use direct ASCII comparison for keywords
                if (matchesKeyword(param_, "PUBLIC"))
                    parsePublic();
                else if (matchesKeyword(param_, "SYSTEM"))
                    parseSystem();
                else if (matchesKeyword(param_, "ENTITY"))
                    parseNameMap(EntityDecl.DeclType.generalEntity);
                else if (matchesKeyword(param_, "DOCTYPE"))
                    parseNameMap(EntityDecl.DeclType.doctype);
                else if (matchesKeyword(param_, "LINKTYPE"))
                    parseNameMap(EntityDecl.DeclType.linktype);
                else if (matchesKeyword(param_, "NOTATION"))
                    parseNameMap(EntityDecl.DeclType.notation);
                else if (matchesKeyword(param_, "SGML"))
                    parseNameMap(EntityDecl.DeclType.sgml);
                else if (matchesKeyword(param_, "SGMLDECL"))
                {
                    if (parseArg())
                        catalog_.setSgmlDecl(param_, paramLoc_);
                }
                else if (matchesKeyword(param_, "DOCUMENT"))
                {
                    if (parseArg())
                        catalog_.setDocument(param_, paramLoc_);
                }
                else if (matchesKeyword(param_, "OVERRIDE"))
                    parseOverride();
                else if (matchesKeyword(param_, "CATALOG"))
                {
                    if (parseArg())
                    {
                        if (inLoop(paramLoc_))
                            break;
                        subSysids.push_back(new StringC(param_));
                        subSysidLocs.push_back(new Location(paramLoc_));
                    }
                }
                else if (matchesKeyword(param_, "BASE"))
                {
                    if (parseArg())
                    {
                        StringC tem = new StringC();
                        if (em.pointer()!.expandSystemId(param_, paramLoc_, false, catalogCharset, null, mgr, tem))
                        {
                            InputSource? baseIn = em.pointer()!.open(tem, catalogCharset,
                                InputSourceOrigin.make(paramLoc_), 0, mgr);
                            if (baseIn != null)
                            {
                                Xchar ch = baseIn.get(mgr);
                                if (ch != InputSource.eE || !baseIn.accessError())
                                    catalog.setBase(baseIn.currentLocation());
                            }
                        }
                    }
                }
                else if (matchesKeyword(param_, "DELEGATE"))
                    parseDelegate();
                else if (matchesKeyword(param_, "DTDDECL"))
                    parseDtddecl();
                else
                {
                    if (!wasRecovering && parseParam(0) == Param.eofParam)
                        break;
                    recovering = true;
                }
            }
            else if (parm == Param.eofParam)
                break;
            else if (!recovering)
            {
                recovering = true;
                message(CatalogMessages.nameExpected);
            }
        }

        catalog.endCatalog();

        // Process sub-catalogs
        for (nuint i = 0; i < subSysids.size(); i++)
        {
            StringC tem = new StringC();
            if (em.pointer()!.expandSystemId(subSysids[i], subSysidLocs[i], false, catalogCharset, null, mgr, tem))
            {
                parseCatalog(tem, true, catalogCharset, catalogCharset,
                             InputSourceOrigin.make(subSysidLocs[i]), catalog, mgr);
            }
        }
    }

    private Boolean inLoop(Location loc)
    {
        InputSourceOrigin? origin = paramLoc_.origin().pointer()?.asInputSourceOrigin();
        if (origin == null)
            return false;
        ExternalInfo? info = origin.externalInfo();
        if (info == null)
            return false;
        StorageObjectLocation soLoc = new StorageObjectLocation();
        if (!ExtendEntityManager.externalize(info, origin.startOffset(paramLoc_.index()), soLoc))
            return false;

        // Check for loop by comparing with parent locations
        for (; ; )
        {
            Location parent = origin.parent()!;
            if (parent.origin().isNull())
                break;
            origin = parent.origin().pointer()?.asInputSourceOrigin();
            if (origin == null)
                break;
            ExternalInfo? info1 = origin.externalInfo();
            if (info1 != null)
            {
                StorageObjectLocation soLoc1 = new StorageObjectLocation();
                if (ExtendEntityManager.externalize(info1, origin.startOffset(parent.index()), soLoc1))
                {
                    if (soLoc.storageObjectSpec?.storageManager == soLoc1.storageObjectSpec?.storageManager
                        && soLoc.actualStorageId.Equals(soLoc1.actualStorageId))
                    {
                        setNextLocation(loc.origin().pointer()?.parent() ?? new Location());
                        message(CatalogMessages.inLoop);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void parseOverride()
    {
        if (parseParam(0) != Param.nameParam)
        {
            message(CatalogMessages.overrideYesOrNo);
            return;
        }
        upcase(param_);
        if (matchesKeyword(param_, "YES"))
            override_ = true;
        else if (matchesKeyword(param_, "NO"))
            override_ = false;
        else
            message(CatalogMessages.overrideYesOrNo);
    }

    private void parsePublic()
    {
        if (parseParam(minimumLiteral) != Param.literalParam)
        {
            message(CatalogMessages.literalExpected);
            return;
        }
        StringC publicId = new StringC(param_);
        if (!parseArg())
            return;
        catalog_!.addPublicId(publicId, param_, paramLoc_, override_);
    }

    private void parseDelegate()
    {
        if (parseParam(minimumLiteral) != Param.literalParam)
        {
            message(CatalogMessages.literalExpected);
            return;
        }
        StringC publicId = new StringC(param_);
        if (!parseArg())
            return;
        catalog_!.addDelegate(publicId, param_, paramLoc_, override_);
    }

    private void parseDtddecl()
    {
        if (parseParam(minimumLiteral) != Param.literalParam)
        {
            message(CatalogMessages.literalExpected);
            return;
        }
        StringC publicId = new StringC(param_);
        if (!parseArg())
            return;
        catalog_!.addDtdDecl(publicId, param_, paramLoc_, override_);
    }

    private void parseSystem()
    {
        if (!parseArg())
            return;
        StringC systemId = new StringC(param_);
        Param parm = parseParam(0);
        if (parm == Param.nameParam)
            message(CatalogMessages.systemShouldQuote);
        else if (parm != Param.literalParam && parm != Param.nameParam)
        {
            message(CatalogMessages.literalExpected);
            return;
        }
        catalog_!.addSystemId(systemId, param_, paramLoc_);
    }

    private void parseNameMap(EntityDecl.DeclType declType)
    {
        if (!parseArg())
            return;
        StringC name = new StringC(param_);
        if (!parseArg())
            return;
        catalog_!.addName(name, declType, param_, paramLoc_, override_);
    }

    private Boolean parseArg()
    {
        Param parm = parseParam(0);
        if (parm != Param.nameParam && parm != Param.literalParam)
        {
            message(CatalogMessages.nameOrLiteralExpected);
            return false;
        }
        return true;
    }

    private Param parseParam(uint flags)
    {
        for (; ; )
        {
            Xchar c = get();
            int cat = getCategory(c);
            switch (cat)
            {
                case catEof:
                    return Param.eofParam;
                case catLit:
                case catLita:
                    parseLiteral((Char)c, flags);
                    return Param.literalParam;
                case catS:
                    break;
                case catNul:
                    message(CatalogMessages.nulChar);
                    break;
                case catMinus:
                    c = get();
                    if (c == minus_)
                    {
                        skipComment();
                        break;
                    }
                    unget();
                    goto default;
                default:
                    parseName();
                    return Param.nameParam;
            }
        }
    }

    private void skipComment()
    {
        for (; ; )
        {
            Xchar c = get();
            if (c == minus_)
            {
                c = get();
                if (c == minus_)
                    break;
            }
            if (c == InputSource.eE)
            {
                message(CatalogMessages.eofInComment);
                break;
            }
        }
    }

    private void parseLiteral(Char delim, uint flags)
    {
        paramLoc_ = in_!.currentLocation();
        int skipping = 1; // yesBegin = 1
        param_.resize(0);

        for (; ; )
        {
            Xchar c = get();
            if (c == InputSource.eE)
            {
                message(CatalogMessages.eofInLiteral);
                break;
            }
            if ((Char)c == delim)
                break;

            if ((flags & minimumLiteral) != 0)
            {
                if (!isMinimumData(c))
                    message(CatalogMessages.minimumData);
                if (c == rs_)
                {
                    // skip RS
                }
                else if (c == space_ || c == re_)
                {
                    if (skipping == 0)  // no
                    {
                        param_.operatorPlusAssign(space_);
                        skipping = 2;  // yesMiddle
                    }
                }
                else
                {
                    skipping = 0;  // no
                    param_.operatorPlusAssign((Char)c);
                }
            }
            else
                param_.operatorPlusAssign((Char)c);
        }

        // Remove trailing space if skipping in middle
        if (skipping == 2 && param_.size() > 0)  // yesMiddle
            param_.resize(param_.size() - 1);
    }

    private void parseName()
    {
        paramLoc_ = in_!.currentLocation();
        nuint length;

        for (length = 1; ; length++)
        {
            Xchar c = in_!.tokenChar(mgr_!);
            int cat = getCategory(c);
            if (cat == catEof || cat == catS)
                break;
            if (cat == catNul)
                message(CatalogMessages.nulChar);
        }

        in_!.endToken(length);

        Char[]? start = in_!.currentTokenStart();
        nuint startIdx = in_!.currentTokenStartIndex();
        nuint tokenLength = in_!.currentTokenLength();

        param_.resize(0);
        if (start != null)
        {
            for (nuint i = 0; i < tokenLength; i++)
                param_.operatorPlusAssign(start[startIdx + i]);
        }
    }

    private Boolean isMinimumData(Xchar c)
    {
        if (c < 0) return false;
        int cat = getCategory(c);
        // catMinus is also minimum data (- character)
        return cat == catMin || cat == catS || cat == catMinus;
    }

    private void upcase(StringC str)
    {
        substTable_.subst(str);
    }

    // Helper to compare StringC with ASCII string directly
    private Boolean matchesKeyword(StringC str, string keyword)
    {
        if (str.size() != (nuint)keyword.Length)
            return false;
        for (int i = 0; i < keyword.Length; i++)
        {
            if (str[(nuint)i] != (Char)keyword[i])
                return false;
        }
        return true;
    }

    private Xchar get()
    {
        return in_!.get(mgr_!);
    }

    private void unget()
    {
        in_!.ungetToken();
    }

    private void message(MessageType0 type)
    {
        Message msg = new Message(0);
        initMessage(msg);
        msg.type = type;
        mgr_!.dispatchMessage(msg);
    }

    // IMessenger implementation
    public void dispatchMessage(Message msg)
    {
        mgr_!.dispatchMessage(msg);
    }

    public void initMessage(Message msg)
    {
        msg.loc = new Location(in_!.currentLocation());
    }

    public void setNextLocation(Location loc)
    {
        // Not used in catalog parser
    }
}
