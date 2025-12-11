// Copyright (c) 1994, 1995, 1996 James Clark
// See the file COPYING for copying permission.

using System.Runtime.InteropServices;

namespace OpenSP;

// StorageObjectSpec holds the specification for reading a storage object
public class StorageObjectSpec
{
    public StorageManager? storageManager;
    public string? codingSystemName;
    public InputCodingSystem? codingSystem;
    public StringC specId = new StringC();   // specified id
    public StringC baseId = new StringC();   // id that specified id is relative to

    public enum Records
    {
        find,
        cr,
        lf,
        crlf,
        asis
    }

    public Records records;
    public PackedBoolean notrack;
    public PackedBoolean zapEof;   // zap a final Ctrl-Z
    public PackedBoolean search;

    // Coding system types
    public const int encoding = 0;
    public const int bctf = 1;
    public const int special = 2;

    public sbyte codingSystemType;

    // StorageObjectSpec();
    public StorageObjectSpec()
    {
        storageManager = null;
        codingSystem = null;
        codingSystemName = null;
        notrack = false;
        records = Records.find;
        zapEof = true;
        search = true;
        codingSystemType = (sbyte)encoding;
    }

    // ~StorageObjectSpec();
    // C# GC handles cleanup

    // StorageObjectSpec(const StorageObjectSpec&);
    public StorageObjectSpec(StorageObjectSpec x)
    {
        codingSystemName = x.codingSystemName;
        codingSystem = x.codingSystem;
        specId = new StringC(x.specId);
        baseId = new StringC(x.baseId);
        records = x.records;
        notrack = x.notrack;
        zapEof = x.zapEof;
        search = x.search;
        codingSystemType = x.codingSystemType;
        storageManager = x.storageManager;
    }

    // StorageObjectSpec& operator=(const StorageObjectSpec&);
    public void operatorAssign(StorageObjectSpec x)
    {
        if (this != x)
        {
            codingSystemName = x.codingSystemName;
            codingSystem = x.codingSystem;
            specId = new StringC(x.specId);
            baseId = new StringC(x.baseId);
            records = x.records;
            notrack = x.notrack;
            zapEof = x.zapEof;
            search = x.search;
            codingSystemType = x.codingSystemType;
            storageManager = x.storageManager;
        }
    }
}

// ParsedSystemId is a vector of StorageObjectSpecs with catalog maps
public class ParsedSystemId : Vector<StorageObjectSpec>
{
    public class Map
    {
        public enum Type
        {
            catalogDocument,
            catalogPublic
        }

        public Type type;
        public StringC publicId = new StringC();

        // Map();
        public Map()
        {
            type = Type.catalogDocument;
        }

        // Map(const Map&);
        public Map(Map x)
        {
            type = x.type;
            publicId = new StringC(x.publicId);
        }

        // ~Map();
        // C# GC handles cleanup

        // Map& operator=(const Map&);
        public void operatorAssign(Map x)
        {
            if (this != x)
            {
                type = x.type;
                publicId = new StringC(x.publicId);
            }
        }
    }

    public Vector<Map> maps = new Vector<Map>();

    // ParsedSystemId();
    public ParsedSystemId()
    {
    }

    // void unparse(const CharsetInfo &resultCharset, Boolean isNdata, StringC &result) const;
    public void unparse(CharsetInfo resultCharset, Boolean isNdata, StringC result)
    {
        nuint len = size();
        result.resize(0);

        // Output catalog maps
        for (nuint i = 0; i < maps.size(); i++)
        {
            if (maps[i].type == Map.Type.catalogDocument)
                result.operatorPlusAssign(resultCharset.execToDesc("<CATALOG>"));
            else if (maps[i].type == Map.Type.catalogPublic)
            {
                result.operatorPlusAssign(resultCharset.execToDesc("<CATALOG PUBLIC=\""));
                result.operatorPlusAssign(maps[i].publicId);
                result.operatorPlusAssign(resultCharset.execToDesc("\">"));
            }
        }

        // Output storage object specs
        for (nuint i = 0; i < len; i++)
        {
            StorageObjectSpec sos = this[i];
            result.operatorPlusAssign(resultCharset.execToDesc((sbyte)'<'));
            if (sos.storageManager != null)
                result.operatorPlusAssign(resultCharset.execToDesc(sos.storageManager.type()));
            if (sos.notrack)
                result.operatorPlusAssign(resultCharset.execToDesc(" NOTRACK"));
            if (!sos.search)
                result.operatorPlusAssign(resultCharset.execToDesc(" NOSEARCH"));
            if (sos.storageManager != null && !sos.storageManager.requiresCr()
                && sos.records != (isNdata ? StorageObjectSpec.Records.asis : StorageObjectSpec.Records.find))
            {
                result.operatorPlusAssign(resultCharset.execToDesc((sbyte)' '));
                result.operatorPlusAssign(resultCharset.execToDesc(recordsName(sos.records)));
            }
            if (sos.codingSystemName != null && sos.codingSystemType != StorageObjectSpec.special)
            {
                if (!sos.zapEof)
                    result.operatorPlusAssign(resultCharset.execToDesc(" NOZAPEOF"));
                result.operatorPlusAssign(resultCharset.execToDesc(
                    sos.codingSystemType == StorageObjectSpec.bctf ? " BCTF=" : " ENCODING="));
                result.operatorPlusAssign(resultCharset.execToDesc(sos.codingSystemName));
            }
            if (sos.baseId.size() != 0)
            {
                result.operatorPlusAssign(resultCharset.execToDesc(" SOIBASE='"));
                unparseSoi(sos.baseId, sos.storageManager?.idCharset(), resultCharset, result);
                result.operatorPlusAssign(resultCharset.execToDesc((sbyte)'\''));
            }
            result.operatorPlusAssign(resultCharset.execToDesc((sbyte)'>'));
            unparseSoi(sos.specId, sos.storageManager?.idCharset(), resultCharset, result);
        }
    }

    private static string recordsName(StorageObjectSpec.Records records)
    {
        switch (records)
        {
            case StorageObjectSpec.Records.find: return "FIND";
            case StorageObjectSpec.Records.asis: return "ASIS";
            case StorageObjectSpec.Records.cr: return "CR";
            case StorageObjectSpec.Records.lf: return "LF";
            case StorageObjectSpec.Records.crlf: return "CRLF";
            default: return "";
        }
    }

    private static void unparseSoi(StringC soi, CharsetInfo? idCharset, CharsetInfo resultCharset, StringC result)
    {
        if (idCharset == null)
        {
            for (nuint i = 0; i < soi.size(); i++)
            {
                string buf = string.Format("&#{0};", (ulong)soi[i]);
                result.operatorPlusAssign(resultCharset.execToDesc(buf));
            }
            return;
        }
        for (nuint i = 0; i < soi.size(); i++)
        {
            UnivChar univ;
            WideChar to;
            ISet<WideChar> toSet = new ISet<WideChar>();
            // On Windows, don't escape backslashes since they are path separators
            bool escapeBackslash = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (!idCharset.descToUniv(soi[i], out univ)
                || univ >= 127
                || univ < 32
                || univ == 36        // $
                || univ == 96        // `
                || (univ == 92 && escapeBackslash)  // backslash (not on Windows)
                || univ == 94        // ^
                || resultCharset.univToDesc(univ, out to, toSet) != 1)
            {
                string buf = string.Format("^{0};", (ulong)soi[i]);
                result.operatorPlusAssign(resultCharset.execToDesc(buf));
            }
            else
            {
                switch (univ)
                {
                    case 34:    // double quote
                    case 35:    // #
                    case 39:    // apostrophe
                    case 60:    // <
                        {
                            string buf = string.Format("&#{0};", (ulong)to);
                            result.operatorPlusAssign(resultCharset.execToDesc(buf));
                        }
                        break;
                    default:
                        result.operatorPlusAssign((Char)to);
                        break;
                }
            }
        }
    }
}

// StorageObjectLocation holds location information for error reporting
public class StorageObjectLocation
{
    public StorageObjectSpec? storageObjectSpec;
    public StringC actualStorageId = new StringC();
    public ulong lineNumber;
    public ulong columnNumber;
    public ulong byteIndex;
    public ulong storageObjectOffset;

    public StorageObjectLocation()
    {
        storageObjectSpec = null;
        lineNumber = 0;
        columnNumber = 0;
        byteIndex = 0;
        storageObjectOffset = 0;
    }
}

// Abstract base class for extended entity management
public abstract class ExtendEntityManager : EntityManager
{
    // Additional flags for open
    public const int mayNotExist = 0x40;  // 0100 octal
    public const int isNdata = 0x80;      // 0200 octal

    // virtual ~ExtendEntityManager();
    // C# GC handles cleanup

    // CatalogManager abstract class
    public abstract class CatalogManager
    {
        // virtual ~CatalogManager();
        // C# GC handles cleanup

        // virtual ConstPtr<EntityCatalog> makeCatalog(...) const = 0;
        public abstract ConstPtr<EntityCatalog> makeCatalog(StringC systemId,
                                                             CharsetInfo docCharset,
                                                             ExtendEntityManager? em,
                                                             Messenger mgr);

        // virtual Boolean mapCatalog(...) const = 0;
        public abstract Boolean mapCatalog(ParsedSystemId systemId,
                                           ExtendEntityManager? em,
                                           Messenger mgr);
    }

    // virtual void registerStorageManager(StorageManager *) = 0;
    public abstract void registerStorageManager(StorageManager sm);

    // virtual void setCatalogManager(CatalogManager *) = 0;
    public abstract void setCatalogManager(CatalogManager cm);

    // virtual Boolean expandSystemId(...) = 0;
    public abstract Boolean expandSystemId(StringC str,
                                           Location defLoc,
                                           Boolean isNdataFlag,
                                           CharsetInfo charset,
                                           StringC? mapCatalogPublic,
                                           Messenger mgr,
                                           StringC result);

    // virtual Boolean mergeSystemIds(...) const = 0;
    public abstract Boolean mergeSystemIds(Vector<StringC> sysids,
                                           Boolean mapCatalogDocument,
                                           CharsetInfo charset,
                                           Messenger mgr,
                                           StringC result);

    // virtual Boolean parseSystemId(...) const = 0;
    public abstract Boolean parseSystemId(StringC str,
                                          CharsetInfo docCharset,
                                          Boolean isNdataFlag,
                                          StorageObjectLocation? defLoc,
                                          Messenger mgr,
                                          ParsedSystemId parsedSysid);

    // static const ParsedSystemId *externalInfoParsedSystemId(const ExternalInfo *);
    public static ParsedSystemId? externalInfoParsedSystemId(ExternalInfo? info)
    {
        if (info == null)
            return null;
        ExternalInfoImpl? p = info as ExternalInfoImpl;
        if (p == null)
            return null;
        return p.parsedSystemId();
    }

    // static Boolean externalize(const ExternalInfo *, Offset, StorageObjectLocation &);
    public static Boolean externalize(ExternalInfo? info, Offset off, StorageObjectLocation loc)
    {
        if (info == null)
            return false;
        ExternalInfoImpl? p = info as ExternalInfoImpl;
        if (p == null)
            return false;
        return p.convertOffset(off, loc);
    }

    // static Boolean defLocation(const Location &, StorageObjectLocation &);
    // Converts a Location to a StorageObjectLocation by walking up the origin chain
    protected static Boolean defLocation(Location defLocation, StorageObjectLocation soLoc)
    {
        Offset off = 0;
        ExternalInfo? info = null;
        Origin? origin = defLocation.origin().pointer();
        Index index = defLocation.index();

        for (;;)
        {
            if (origin == null)
                return false;

            InputSourceOrigin? inputSourceOrigin = origin.asInputSourceOrigin();
            if (inputSourceOrigin != null)
            {
                off = inputSourceOrigin.startOffset(index);
                info = inputSourceOrigin.externalInfo();
                if (info != null)
                    break;
                Origin? newOrigin;
                Index newIndex;
                if (!inputSourceOrigin.defLocation(off, out newOrigin, out newIndex))
                    return false;
                origin = newOrigin;
                index = newIndex;
            }
            else
            {
                Location parentLoc = origin.parent();
                origin = parentLoc.origin().pointer();
                index = parentLoc.index();
            }
        }

        return externalize(info, off, soLoc);
    }

    // static ExtendEntityManager *make(...);
    public static ExtendEntityManager make(StorageManager sm,
                                           InputCodingSystem cs,
                                           ConstPtr<InputCodingSystemKit> csKit,
                                           Boolean internalCharsetIsDocCharsetFlag)
    {
        return new EntityManagerImpl(sm, cs, csKit, internalCharsetIsDocCharsetFlag);
    }
}

// Implementation of ExternalInfo for tracking external entity positions
internal class ExternalInfoImpl : ExternalInfo
{
    private ParsedSystemId parsedSysid_ = new ParsedSystemId();
    private Vector<StorageObjectPosition> position_ = new Vector<StorageObjectPosition>();
    private nuint currentIndex_;
    private OffsetOrderedList rsList_ = new OffsetOrderedList();
    private PackedBoolean notrack_;
    private object mutex_ = new object();

    // ExternalInfoImpl(ParsedSystemId &parsedSysid);
    public ExternalInfoImpl(ParsedSystemId parsedSysid)
    {
        currentIndex_ = 0;
        position_.resize(parsedSysid.size());
        for (nuint i = 0; i < position_.size(); i++)
            position_[i] = new StorageObjectPosition();
        parsedSysid.swap(parsedSysid_);
        if (parsedSysid_.size() > 0)
            notrack_ = parsedSysid_[0].notrack;
    }

    // void setId(size_t i, StringC &id);
    public void setId(nuint i, StringC id)
    {
        lock (mutex_)
        {
            id.swap(position_[i].id);
        }
    }

    // void getId(size_t i, StringC &id) const;
    public void getId(nuint i, StringC id)
    {
        lock (mutex_)
        {
            id.assign(position_[i].id.data()!, position_[i].id.size());
        }
    }

    // void setDecoder(size_t i, Decoder *decoder);
    public void setDecoder(nuint i, Decoder? decoder)
    {
        lock (mutex_)
        {
            position_[i].decoder = new Owner<Decoder>(decoder);
        }
    }

    // void noteInsertedRSs();
    public void noteInsertedRSs()
    {
        position_[currentIndex_].insertedRSs = true;
    }

    // void noteRS(Offset offset);
    public void noteRS(Offset offset)
    {
        if (!notrack_)
            rsList_.append(offset);
        Offset prevEndOffset = (currentIndex_ == 0) ? 0 : position_[(nuint)(currentIndex_ - 1)].endOffset;
        if (offset == prevEndOffset)
            position_[currentIndex_].startsWithRS = true;
    }

    // void noteStorageObjectEnd(Offset offset);
    public void noteStorageObjectEnd(Offset offset)
    {
        lock (mutex_)
        {
            if (currentIndex_ < position_.size() - 1)
            {
                position_[currentIndex_++].endOffset = offset;
                position_[currentIndex_].line1RS = rsList_.size();
                notrack_ = parsedSysid_[currentIndex_].notrack;
            }
        }
    }

    // Boolean convertOffset(Offset off, StorageObjectLocation &ret) const;
    public Boolean convertOffset(Offset off, StorageObjectLocation ret)
    {
        lock (mutex_)
        {
            if (off == Offset.MaxValue || position_.size() == 0)
                return false;

            // Find the storage object containing this offset
            int i;
            for (i = 0; i < (int)position_.size() && off >= position_[(nuint)i].endOffset; i++)
                ;
            // Clamp i to valid range before accessing position_
            if (i >= (int)position_.size())
                i = (int)position_.size() - 1;
            if (i < 0)
                return false;
            // Back up to find a valid id
            for (; i >= 0 && position_[(nuint)i].id.size() == 0; i--)
                if (i == 0)
                    return false;

            if (i < 0)
                return false;

            ret.storageObjectSpec = parsedSysid_[(nuint)i];
            ret.actualStorageId.assign(position_[(nuint)i].id.data()!, position_[(nuint)i].id.size());
            Offset startOffset = (i == 0) ? 0 : position_[(nuint)(i - 1)].endOffset;
            ret.storageObjectOffset = off - startOffset;
            ret.byteIndex = ret.storageObjectOffset;

            if (parsedSysid_[(nuint)i].notrack
                || parsedSysid_[(nuint)i].records == StorageObjectSpec.Records.asis)
            {
                ret.lineNumber = ulong.MaxValue;
                if (parsedSysid_[(nuint)i].records != StorageObjectSpec.Records.asis)
                {
                    if (position_[(nuint)i].insertedRSs)
                        ret.byteIndex = ulong.MaxValue;
                    else if (ret.byteIndex > 0 && position_[(nuint)i].startsWithRS)
                        ret.byteIndex--;
                }
                ret.columnNumber = ulong.MaxValue;
                return true;
            }
            else
            {
                nuint line1RS = position_[(nuint)i].line1RS;
                nuint j;
                Offset colStart;
                if (rsList_.findPreceding(off, out j, out colStart))
                {
                    if (position_[(nuint)i].insertedRSs)
                        ret.byteIndex -= j + 1 - line1RS;
                    else if (ret.byteIndex > 0 && position_[(nuint)i].startsWithRS)
                        ret.byteIndex--;
                    j++;
                    colStart++;
                }
                else
                {
                    j = 0;
                    colStart = 0;
                }
                ret.lineNumber = j - line1RS + 1 - (position_[(nuint)i].startsWithRS ? 1UL : 0UL);
                if (colStart < startOffset)
                    colStart = startOffset;
                ret.columnNumber = 1 + off - colStart;
            }

            if (position_[(nuint)i].decoder.pointer() == null
                || !position_[(nuint)i].decoder.pointer()!.convertOffset(ref ret.byteIndex))
                ret.byteIndex = ulong.MaxValue;

            return true;
        }
    }

    // const StorageObjectSpec &spec(size_t i) const;
    public StorageObjectSpec spec(nuint i)
    {
        return parsedSysid_[i];
    }

    // size_t nSpecs() const;
    public nuint nSpecs()
    {
        return parsedSysid_.size();
    }

    // const ParsedSystemId &parsedSystemId() const;
    public ParsedSystemId parsedSystemId()
    {
        return parsedSysid_;
    }
}

// EntityManagerImpl - concrete implementation of ExtendEntityManager
internal class EntityManagerImpl : ExtendEntityManager
{
    private Vector<StorageManager> storageManagers_ = new Vector<StorageManager>();
    private StorageManager defaultStorageManager_;
    private InputCodingSystem defaultCodingSystem_;
    private ExtendEntityManager.CatalogManager? catalogManager_;
    private Boolean internalCharsetIsDocCharset_;
    private ConstPtr<InputCodingSystemKit> codingSystemKit_;

    // EntityManagerImpl(...);
    public EntityManagerImpl(StorageManager defaultStorageManager,
                             InputCodingSystem defaultCodingSystem,
                             ConstPtr<InputCodingSystemKit> codingSystemKit,
                             Boolean internalCharsetIsDocCharsetFlag)
    {
        defaultStorageManager_ = defaultStorageManager;
        defaultCodingSystem_ = defaultCodingSystem;
        codingSystemKit_ = codingSystemKit;
        internalCharsetIsDocCharset_ = internalCharsetIsDocCharsetFlag;
    }

    // Boolean internalCharsetIsDocCharset() const;
    public override Boolean internalCharsetIsDocCharset()
    {
        return internalCharsetIsDocCharset_;
    }

    // const CharsetInfo &charset() const;
    public override CharsetInfo charset()
    {
        return codingSystemKit_.pointer()!.systemCharset();
    }

    // InputSource *open(...);
    public override InputSource? open(StringC sysid,
                                       CharsetInfo docCharset,
                                       InputSourceOrigin? origin,
                                       uint flags,
                                       Messenger mgr)
    {
        ParsedSystemId parsedSysid = new ParsedSystemId();
        if (!parseSystemId(sysid, docCharset, (flags & (uint)isNdata) != 0, null, mgr, parsedSysid))
            return null;
        if (catalogManager_ != null && !catalogManager_.mapCatalog(parsedSysid, this, mgr))
            return null;

        // Create ExternalInputSource with parsedSysid
        Char replacementChar = 0xFFFD; // Unicode replacement character
        return new ExternalInputSource(parsedSysid, charset(), docCharset,
                                       internalCharsetIsDocCharset_, replacementChar,
                                       origin, flags);
    }

    // ConstPtr<EntityCatalog> makeCatalog(...);
    public override ConstPtr<EntityCatalog> makeCatalog(StringC systemId,
                                                         CharsetInfo charset,
                                                         Messenger mgr)
    {
        if (catalogManager_ != null)
            return catalogManager_.makeCatalog(systemId, charset, this, mgr);
        return new ConstPtr<EntityCatalog>();
    }

    // void registerStorageManager(StorageManager *);
    public override void registerStorageManager(StorageManager sm)
    {
        storageManagers_.push_back(sm);
    }

    // void setCatalogManager(CatalogManager *);
    public override void setCatalogManager(ExtendEntityManager.CatalogManager cm)
    {
        catalogManager_ = cm;
    }

    // Boolean expandSystemId(...);
    public override Boolean expandSystemId(StringC str,
                                            Location defLoc,
                                            Boolean isNdataFlag,
                                            CharsetInfo charsetInfo,
                                            StringC? mapCatalogPublic,
                                            Messenger mgr,
                                            StringC result)
    {
        ParsedSystemId parsedSysid = new ParsedSystemId();
        StorageObjectLocation defSoLoc = new StorageObjectLocation();
        StorageObjectLocation? defSoLocP = null;

        // Get default location from defLoc if possible
        if (defLocation(defLoc, defSoLoc))
            defSoLocP = defSoLoc;

        if (!parseSystemId(str, charsetInfo, isNdataFlag, defSoLocP, mgr, parsedSysid))
            return false;

        if (mapCatalogPublic != null)
        {
            ParsedSystemId.Map map = new ParsedSystemId.Map();
            map.type = ParsedSystemId.Map.Type.catalogPublic;
            map.publicId.assign(mapCatalogPublic.data()!, mapCatalogPublic.size());
            parsedSysid.maps.resize(parsedSysid.maps.size() + 1);
            for (nuint i = parsedSysid.maps.size() - 1; i > 0; i--)
                parsedSysid.maps[i] = parsedSysid.maps[(nuint)(i - 1)];
            parsedSysid.maps[0] = map;
        }

        parsedSysid.unparse(internalCharset(charsetInfo), isNdataFlag, result);
        return true;
    }

    // Boolean mergeSystemIds(...) const;
    public override Boolean mergeSystemIds(Vector<StringC> sysids,
                                            Boolean mapCatalogDocument,
                                            CharsetInfo charsetInfo,
                                            Messenger mgr,
                                            StringC result)
    {
        ParsedSystemId parsedSysid = new ParsedSystemId();
        if (mapCatalogDocument)
        {
            parsedSysid.maps.resize(parsedSysid.maps.size() + 1);
            parsedSysid.maps.back().type = ParsedSystemId.Map.Type.catalogDocument;
        }
        for (nuint i = 0; i < sysids.size(); i++)
        {
            if (!parseSystemId(sysids[i], charsetInfo, false, null, mgr, parsedSysid))
                return false;
        }
        parsedSysid.unparse(internalCharset(charsetInfo), false, result);
        return true;
    }

    // Boolean parseSystemId(...) const;
    public override Boolean parseSystemId(StringC str,
                                          CharsetInfo docCharset,
                                          Boolean isNdataFlag,
                                          StorageObjectLocation? defLoc,
                                          Messenger mgr,
                                          ParsedSystemId parsedSysid)
    {
        // Parse FSI (Formal System Identifier) format: <StorageType>specId
        // Also handles <CATALOG> and <CATALOG PUBLIC="..."> directives
        nuint pos = 0;

        while (pos < str.size())
        {
            // Skip whitespace
            while (pos < str.size() && (str[pos] == ' ' || str[pos] == '\t'))
                pos++;

            if (pos >= str.size())
                break;

            // Check if this is an FSI starting with '<'
            if (str[pos] == '<')
            {
                pos++; // skip '<'

                // Extract storage type name
                nuint typeStart = pos;
                while (pos < str.size() && str[pos] != '>' && str[pos] != ' ')
                    pos++;

                // Check if this is a CATALOG directive
                StringC typeName = new StringC();
                for (nuint i = typeStart; i < pos; i++)
                    typeName.operatorPlusAssign(str[i]);

                if (typeName.ToString() == "CATALOG")
                {
                    // Handle CATALOG directive - this is a map, not a storage spec
                    ParsedSystemId.Map map = new ParsedSystemId.Map();

                    // Check for PUBLIC attribute
                    while (pos < str.size() && str[pos] == ' ')
                        pos++;

                    if (pos < str.size() && str[pos] != '>')
                    {
                        // Look for PUBLIC="..."
                        nuint attrStart = pos;
                        while (pos < str.size() && str[pos] != '=' && str[pos] != '>')
                            pos++;

                        StringC attrName = new StringC();
                        for (nuint i = attrStart; i < pos; i++)
                            attrName.operatorPlusAssign(str[i]);

                        if (attrName.ToString() == "PUBLIC" && pos < str.size() && str[pos] == '=')
                        {
                            pos++; // skip '='
                            if (pos < str.size() && str[pos] == '"')
                            {
                                pos++; // skip opening quote
                                nuint publicIdStart = pos;
                                while (pos < str.size() && str[pos] != '"')
                                    pos++;
                                // Extract public ID
                                for (nuint i = publicIdStart; i < pos; i++)
                                    map.publicId.operatorPlusAssign(str[i]);
                                if (pos < str.size())
                                    pos++; // skip closing quote
                                map.type = ParsedSystemId.Map.Type.catalogPublic;
                            }
                        }
                    }
                    else
                    {
                        // No PUBLIC attribute - catalogDocument type
                        map.type = ParsedSystemId.Map.Type.catalogDocument;
                    }

                    // Skip to closing '>'
                    while (pos < str.size() && str[pos] != '>')
                        pos++;
                    if (pos < str.size())
                        pos++; // skip '>'

                    parsedSysid.maps.push_back(map);
                    continue; // Continue parsing - don't create a storage spec for CATALOG
                }

                // Regular storage type - create storage object spec
                parsedSysid.resize(parsedSysid.size() + 1);
                StorageObjectSpec sos = parsedSysid.back();

                sos.storageManager = lookupStorageType(typeName, docCharset);

                // Skip any attributes until '>'
                while (pos < str.size() && str[pos] != '>')
                    pos++;

                if (pos < str.size())
                    pos++; // skip '>'

                // Extract spec ID (everything after '>' until end or next '<')
                nuint specStart = pos;
                while (pos < str.size() && str[pos] != '<')
                    pos++;

                // Assign spec ID
                if (pos > specStart)
                {
                    Char[] specData = new Char[pos - specStart];
                    for (nuint i = 0; i < pos - specStart; i++)
                        specData[i] = str[specStart + i];
                    sos.specId.assign(specData, pos - specStart);
                }

                if (sos.storageManager == null)
                {
                    if (defLoc != null && defLoc.storageObjectSpec?.storageManager?.inheritable() == true)
                        sos.storageManager = defLoc.storageObjectSpec.storageManager;
                    else
                        sos.storageManager = defaultStorageManager_;
                }
                setDefaults(sos, isNdataFlag, defLoc);
            }
            else
            {
                // No FSI prefix - treat entire string as spec ID
                parsedSysid.resize(parsedSysid.size() + 1);
                StorageObjectSpec sos = parsedSysid.back();
                sos.specId.assign(str.data(), str.size());
                sos.storageManager = guessStorageType(str, docCharset);
                pos = str.size(); // consume everything

                if (sos.storageManager == null)
                {
                    if (defLoc != null && defLoc.storageObjectSpec?.storageManager?.inheritable() == true)
                        sos.storageManager = defLoc.storageObjectSpec.storageManager;
                    else
                        sos.storageManager = defaultStorageManager_;
                }
                setDefaults(sos, isNdataFlag, defLoc);
            }
        }

        return true;
    }

    private void setDefaults(StorageObjectSpec sos, Boolean isNdataFlag, StorageObjectLocation? defLoc)
    {
        if (sos.storageManager!.requiresCr())
            sos.records = StorageObjectSpec.Records.cr;
        else if (isNdataFlag)
            sos.records = StorageObjectSpec.Records.asis;
        if (isNdataFlag)
            sos.zapEof = false;

        // Set base ID from default location if available and storage managers match
        if (defLoc != null && defLoc.storageObjectSpec != null &&
            defLoc.storageObjectSpec.storageManager == sos.storageManager)
        {
            // Use the actualStorageId if available (the resolved path where file was found)
            // Otherwise construct from specId + baseId
            if (defLoc.actualStorageId.size() > 0)
            {
                sos.baseId = new StringC(defLoc.actualStorageId);
            }
            else
            {
                sos.baseId = new StringC(defLoc.storageObjectSpec.specId);
                sos.storageManager.resolveRelative(defLoc.storageObjectSpec.baseId,
                                                   sos.baseId, false);
            }
        }

        // Resolve the specId relative to baseId
        if (sos.baseId.size() > 0)
        {
            if (sos.storageManager.resolveRelative(sos.baseId, sos.specId, sos.search))
                sos.baseId.resize(0);
        }

        sos.codingSystem = sos.storageManager.requiredCodingSystem();
        if (sos.codingSystem != null)
        {
            sos.zapEof = false;
            sos.codingSystemType = (sbyte)StorageObjectSpec.special;
        }
        else
        {
            sos.codingSystem = defaultCodingSystem_;
            sos.codingSystemType = (sbyte)(internalCharsetIsDocCharset_
                                           ? StorageObjectSpec.bctf
                                           : StorageObjectSpec.encoding);
            if (isNdataFlag && codingSystemKit_.pointer() != null)
            {
                sos.codingSystem = codingSystemKit_.pointer()!.identityInputCodingSystem();
                sos.codingSystemType = (sbyte)StorageObjectSpec.special;
            }
        }
    }

    // StorageManager *guessStorageType(...) const;
    private StorageManager? guessStorageType(StringC type, CharsetInfo internalCharset)
    {
        for (nuint i = 0; i < storageManagers_.size(); i++)
        {
            if (storageManagers_[i].guessIsId(type, internalCharset))
                return storageManagers_[i];
        }
        if (defaultStorageManager_.guessIsId(type, internalCharset))
            return defaultStorageManager_;
        return null;
    }

    // StorageManager *lookupStorageType(...) const;
    public StorageManager? lookupStorageType(StringC type, CharsetInfo internalCharset)
    {
        if (type.size() == 0)
            return null;
        if (matchKey(type, defaultStorageManager_.type(), internalCharset))
            return defaultStorageManager_;
        for (nuint i = 0; i < storageManagers_.size(); i++)
        {
            if (matchKey(type, storageManagers_[i].type(), internalCharset))
                return storageManagers_[i];
        }
        return null;
    }

    // StorageManager *lookupStorageType(const char *) const;
    public StorageManager? lookupStorageType(string type)
    {
        if (type == defaultStorageManager_.type())
            return defaultStorageManager_;
        for (nuint i = 0; i < storageManagers_.size(); i++)
        {
            if (type == storageManagers_[i].type())
                return storageManagers_[i];
        }
        return null;
    }

    private static Boolean matchKey(StringC type, string s, CharsetInfo internalCharset)
    {
        if (s.Length != (int)type.size())
            return false;
        for (nuint i = 0; i < type.size(); i++)
        {
            char upper = char.ToUpper(s[(int)i]);
            char lower = char.ToLower(s[(int)i]);
            if (internalCharset.execToDesc((sbyte)upper) != type[i]
                && internalCharset.execToDesc((sbyte)lower) != type[i])
                return false;
        }
        return true;
    }

    private CharsetInfo internalCharset(CharsetInfo docCharset)
    {
        if (internalCharsetIsDocCharset_)
            return docCharset;
        else
            return charset();
    }
}

// ExternalInputSource - reads from external storage objects with character decoding
internal class ExternalInputSource : InputSource
{
    private const Char RS = 10;  // Record start (LF)
    private const Char RE = 13;  // Record end (CR) - for output, input CR/LF -> RE
    private const byte EOFCHAR = 0x1A;  // Ctrl-Z

    private enum RecordType
    {
        unknown,
        crUnknown,
        crlf,
        lf,
        cr,
        asis
    }

    private ExternalInfoImpl info_;
    private Char[] buf_;
    private nuint bufLim_;           // End of decoded data in buffer
    private Offset bufLimOffset_;
    private nuint bufSize_;
    private nuint readSize_;
    private Vector<Owner<StorageObject>> sov_ = new Vector<Owner<StorageObject>>();
    private StorageObject? so_;
    private nuint soIndex_;
    private Boolean insertRS_;
    private Decoder? decoder_;
    private Boolean mayRewind_;
    private Boolean mayNotExist_;
    private RecordType recordType_;
    private Boolean zapEof_;
    private byte[] leftOver_ = Array.Empty<byte>();  // Unconsumed bytes from decoder
    private nuint nLeftOver_ = 0;                     // Number of leftover bytes
    private Boolean needMoreData_ = false;            // Signal to continue fill loop
    private Boolean internalCharsetIsDocCharset_;
    private Char replacementChar_;

    public ExternalInputSource(ParsedSystemId parsedSysid,
                               CharsetInfo systemCharset,
                               CharsetInfo docCharset,
                               Boolean internalCharsetIsDocCharset,
                               Char replacementChar,
                               InputSourceOrigin? origin,
                               uint flags)
        : base(origin ?? InputSourceOrigin.make(), null, 0, null, 0)
    {
        mayRewind_ = (flags & (uint)EntityManager.mayRewind) != 0;
        mayNotExist_ = (flags & (uint)ExtendEntityManager.mayNotExist) != 0;
        internalCharsetIsDocCharset_ = internalCharsetIsDocCharset;
        replacementChar_ = replacementChar;

        sov_.resize(parsedSysid.size());
        for (nuint i = 0; i < sov_.size(); i++)
            sov_[i] = new Owner<StorageObject>();

        bufSize_ = 8192;
        buf_ = new Char[bufSize_];
        init();
        info_ = new ExternalInfoImpl(parsedSysid);
        // Always set external info on the origin (use base class's origin)
        inputSourceOrigin()?.setExternalInfo(info_);
    }

    private void init()
    {
        so_ = null;
        bufLim_ = 0;
        bufLimOffset_ = 0;
        insertRS_ = true;
        soIndex_ = 0;
        recordType_ = RecordType.unknown;
    }

    public override void pushCharRef(Char ch, NamedCharRef charRef)
    {
        noteCharRef(nextIndex(), charRef);
    }

    public override Boolean rewind(Messenger mgr)
    {
        reset(null, 0, null, 0);
        for (nuint i = 0; i < soIndex_; i++)
        {
            if (sov_[i].pointer() != null && !sov_[i].pointer()!.rewind(mgr))
                return false;
        }
        init();
        return true;
    }

    public override void willNotRewind()
    {
        for (nuint i = 0; i < sov_.size(); i++)
        {
            if (sov_[i].pointer() != null)
                sov_[i].pointer()!.willNotRewind();
        }
        mayRewind_ = false;
    }

    protected override Xchar fill(Messenger mgr)
    {
        // Fill buffer while end() >= bufLim_ (no data available between end and bufLim)
        while (endIdx() >= bufLim_)
        {
            // Open next storage object if needed
            while (so_ == null)
            {
                if (soIndex_ >= sov_.size())
                    return eE;

                if (soIndex_ > 0)
                    info_.noteStorageObjectEnd((Offset)(bufLimOffset_ - (bufLim_ - endIdx())));

                StorageObjectSpec spec = info_.spec(soIndex_);

                if (sov_[soIndex_].pointer() == null)
                {
                    StringC id = new StringC();
                    StorageObject? storageObj;

                    if (mayNotExist_)
                    {
                        NullMessenger nullMgr = new NullMessenger();
                        storageObj = spec.storageManager?.makeStorageObject(
                            spec.specId, spec.baseId, spec.search, mayRewind_, nullMgr, id);
                    }
                    else
                    {
                        storageObj = spec.storageManager?.makeStorageObject(
                            spec.specId, spec.baseId, spec.search, mayRewind_, mgr, id);
                    }

                    sov_[soIndex_] = new Owner<StorageObject>(storageObj);
                    info_.setId(soIndex_, id);
                }

                so_ = sov_[soIndex_].pointer();

                if (so_ != null)
                {
                    decoder_ = spec.codingSystem?.makeDecoder();
                    info_.setDecoder(soIndex_, decoder_);
                    zapEof_ = spec.zapEof;

                    switch (spec.records)
                    {
                        case StorageObjectSpec.Records.asis:
                            recordType_ = RecordType.asis;
                            insertRS_ = false;
                            break;
                        case StorageObjectSpec.Records.cr:
                            recordType_ = RecordType.cr;
                            break;
                        case StorageObjectSpec.Records.lf:
                            recordType_ = RecordType.lf;
                            break;
                        case StorageObjectSpec.Records.crlf:
                            recordType_ = RecordType.crlf;
                            break;
                        case StorageObjectSpec.Records.find:
                        default:
                            recordType_ = RecordType.unknown;
                            break;
                    }

                    soIndex_++;
                    readSize_ = so_.getBlockSize();
                    break;
                }
                else
                {
                    setAccessError();
                }

                soIndex_++;
            }

            // Move start to end, discarding old data
            nuint keepSize = endIdx() - startIdx();
            if (keepSize > 0)
            {
                for (nuint i = 0; i < keepSize; i++)
                    buf_[i] = buf_[startIdx() + i];
            }
            // After compaction, data is at [0, keepSize), so we need to reset endIndex to keepSize
            moveStart(buf_, 0);
            advanceEnd(keepSize);  // FIX: Set endIndex to keepSize after compaction
            bufLim_ = keepSize;     // FIX: Set bufLim_ to keepSize, not old endIdx()

            // Ensure buffer is large enough
            nuint neededSize = bufLim_ + readSize_ + 64;
            if (bufSize_ < neededSize)
            {
                Char[] newBuf = new Char[neededSize];
                Array.Copy(buf_, newBuf, (int)bufLim_);
                changeBuffer(newBuf, buf_);
                buf_ = newBuf;
                bufSize_ = neededSize;
            }

            // Read raw bytes
            byte[] rawBuf = new byte[readSize_];
            nuint nread;
            if (!so_!.read(rawBuf, readSize_, mgr, out nread) || nread == 0)
            {
                so_ = null;
                continue;
            }

            // Handle Ctrl-Z at end
            if (zapEof_ && nread > 0 && rawBuf[(int)nread - 1] == EOFCHAR)
                nread--;

            if (nread == 0)
            {
                so_ = null;
                continue;
            }

            // Decode bytes to characters at bufLim_ position
            nuint nChars;
            if (decoder_ != null)
            {
                // Combine leftover bytes with newly read bytes
                nuint totalBytes = nLeftOver_ + nread;
                byte[] combinedBuf;
                if (nLeftOver_ > 0)
                {
                    combinedBuf = new byte[totalBytes];
                    Array.Copy(leftOver_, 0, combinedBuf, 0, (int)nLeftOver_);
                    Array.Copy(rawBuf, 0, combinedBuf, (int)nLeftOver_, (int)nread);
                }
                else
                {
                    combinedBuf = rawBuf;
                }

                Char[] decodeBuf = new Char[totalBytes];
                nuint inputUsed;
                nChars = decoder_.decode(decodeBuf, combinedBuf, totalBytes, out inputUsed);
                for (nuint i = 0; i < nChars; i++)
                    buf_[bufLim_ + i] = decodeBuf[i];

                // Save any unconsumed bytes as leftovers for next read
                nLeftOver_ = totalBytes - inputUsed;
                if (nLeftOver_ > 0)
                {
                    leftOver_ = new byte[nLeftOver_];
                    Array.Copy(combinedBuf, (int)inputUsed, leftOver_, 0, (int)nLeftOver_);
                }
            }
            else
            {
                // No decoder - treat bytes as characters directly
                for (nuint i = 0; i < nread; i++)
                    buf_[bufLim_ + i] = (Char)rawBuf[i];
                nChars = nread;
            }

            if (nChars > 0)
            {
                // Insert RS at beginning if needed
                if (insertRS_)
                {
                    info_.noteRS(bufLimOffset_);
                    // Shift data right by 1 to make room for RS
                    for (nuint i = nChars; i > 0; i--)
                        buf_[bufLim_ + i] = buf_[bufLim_ + i - 1];
                    buf_[bufLim_] = RS;
                    // DEBUG: Console.Error.WriteLine fill: INSERTING RS at bufLim_={bufLim_}, next chars: '{new string(buf_.Skip((int)bufLim_+1).Take(10).Select(c => c < 32 ? '.' : (char)c).ToArray())}'");
                    nChars++;
                    insertRS_ = false;
                    bufLimOffset_++;
                    // Advance end past the RS so processRecords doesn't find it as an LF
                    advanceEnd(bufLim_ + 1);
                }
                bufLim_ += nChars;
                bufLimOffset_ += (Offset)nChars - 1;  // -1 because RS was already counted
                break;
            }
        }

        // Insert RS at current position if needed (BEFORE processRecords)
        // This handles the case where insertRS_ was set in a previous fill() call
        // and there's still data in the buffer (we didn't need to read new data)
        if (insertRS_ && endIdx() < bufLim_)
        {
            // Ensure buffer has room for one more character
            if (bufLim_ >= bufSize_)
            {
                nuint neededSize = bufLim_ + 64;
                Char[] newBuf = new Char[neededSize];
                Array.Copy(buf_, newBuf, (int)bufLim_);
                changeBuffer(newBuf, buf_);
                buf_ = newBuf;
                bufSize_ = neededSize;
            }
            info_.noteRS((Offset)(bufLimOffset_ - (bufLim_ - endIdx())));
            // Shift data from endIdx to bufLim by 1 to make room for RS
            for (nuint i = bufLim_; i > endIdx(); i--)
                buf_[(int)i] = buf_[(int)(i - 1)];
            buf_[(int)endIdx()] = RS;
            // DEBUG: Console.Error.WriteLine fill AFTER LOOP: INSERTING RS at endIdx()={endIdx()}, bufLim_={bufLim_}, next chars: '{new string(buf_.Skip((int)endIdx()+1).Take(10).Select(c => c < 32 ? '.' : (char)c).ToArray())}'");
            bufLim_++;
            bufLimOffset_++;
            advanceEnd(endIdx() + 1);
            insertRS_ = false;
        }

        // Process records - convert LF/CR to RE and set up next RS
        for (;;)
        {
            needMoreData_ = false;
            processRecords();

            // If processRecords signaled we need more data, continue reading
            if (needMoreData_)
            {
                needMoreData_ = false;
                // We need to read more data - the main while loop should have already done this
                // But if endIdx() == bufLim_ after processRecords, we need to read more
                if (endIdx() >= bufLim_)
                {
                    // Go back to the main while loop to read more data
                    return fill(mgr);  // Recursive call like original C++
                }
                // Otherwise continue processing records
                continue;
            }
            break;
        }

        // Return next character if available
        if (curIdx() < endIdx())
            return (Xchar)nextChar();

        return eE;
    }

    private void processRecords()
    {
        // Process from end() to bufLim_, converting line endings and advancing end()
        switch (recordType_)
        {
            case RecordType.unknown:
                {
                    // Scan for first CR or LF to determine record type
                    nuint? found = findNextCrOrLf(endIdx(), bufLim_);
                    if (found.HasValue)
                    {
                        nuint e = found.Value;
                        if (buf_[(int)e] == '\n')
                        {
                            recordType_ = RecordType.lf;
                            info_.noteInsertedRSs();
                            buf_[(int)e] = RE;  // Convert LF to RE
                            advanceEnd(e + 1);
                            insertRS_ = true;
                        }
                        else  // CR
                        {
                            if (e + 1 < bufLim_)
                            {
                                if (buf_[(int)(e + 1)] == '\n')
                                {
                                    // CRLF
                                    recordType_ = RecordType.crlf;
                                    buf_[(int)e] = RE;  // Convert CR to RE
                                    // Remove the LF by shifting data
                                    for (nuint i = e + 1; i < bufLim_ - 1; i++)
                                        buf_[(int)i] = buf_[(int)(i + 1)];
                                    bufLim_--;
                                    advanceEnd(e + 1);
                                    insertRS_ = true;
                                }
                                else
                                {
                                    // CR only
                                    recordType_ = RecordType.cr;
                                    info_.noteInsertedRSs();
                                    buf_[(int)e] = RE;
                                    advanceEnd(e + 1);
                                    insertRS_ = true;
                                }
                            }
                            else
                            {
                                // CR at end of buffer - don't know yet
                                recordType_ = RecordType.crUnknown;
                                advanceEnd(e + 1);  // Include the CR, convert it later if needed
                            }
                        }
                    }
                    else
                    {
                        // No line ending found, process all available data
                        advanceEnd(bufLim_);
                    }
                }
                break;

            case RecordType.crUnknown:
                // We had a CR at end of previous buffer
                // Check if this buffer starts with LF
                if (endIdx() < bufLim_)
                {
                    if (buf_[(int)endIdx()] == '\n')
                    {
                        recordType_ = RecordType.crlf;
                        // The CR from previous buffer becomes RE, skip this LF
                        advanceEnd(endIdx() + 1);
                    }
                    else
                    {
                        recordType_ = RecordType.cr;
                        info_.noteInsertedRSs();
                    }
                    // Now continue with the appropriate record type
                    processRecords();
                }
                break;

            case RecordType.lf:
                {
                    nuint? found = findNextLf(endIdx(), bufLim_);
                    if (found.HasValue)
                    {
                        buf_[(int)found.Value] = RE;
                        advanceEnd(found.Value + 1);
                        insertRS_ = true;
                        // DEBUG: Console.Error.WriteLine processRecords: found LF at {found.Value}, set insertRS_=true, endIdx now={endIdx()}, bufLim_={bufLim_}");
                    }
                    else
                    {
                        advanceEnd(bufLim_);
                    }
                }
                break;

            case RecordType.cr:
                {
                    nuint? found = findNextCr(endIdx(), bufLim_);
                    if (found.HasValue)
                    {
                        buf_[(int)found.Value] = RE;
                        advanceEnd(found.Value + 1);
                        insertRS_ = true;
                    }
                    else
                    {
                        advanceEnd(bufLim_);
                    }
                }
                break;

            case RecordType.crlf:
                {
                    // Process CRLF sequences - look for LF to remove
                    nuint e = endIdx();
                    for (;;)
                    {
                        nuint? found = findNextLf(e, bufLim_);
                        if (!found.HasValue)
                        {
                            advanceEnd(bufLim_);
                            break;
                        }
                        nuint lfPos = found.Value;
                        // Need to delete final RS if not followed by anything
                        if (lfPos + 1 == bufLim_)
                        {
                            bufLim_--;
                            bufLimOffset_--;
                            advanceEnd(lfPos);
                            insertRS_ = true;
                            if (curIdx() == endIdx())
                                needMoreData_ = true;  // Signal that we need more data
                            break;
                        }
                        noteRSAt(lfPos);
                        e = lfPos + 1;
                    }
                }
                break;

            case RecordType.asis:
                advanceEnd(bufLim_);
                break;
        }
    }

    private nuint? findNextCr(nuint start, nuint end)
    {
        for (nuint i = start; i < end; i++)
            if (buf_[(int)i] == '\r')
                return i;
        return null;
    }

    private nuint? findNextLf(nuint start, nuint end)
    {
        for (nuint i = start; i < end; i++)
            if (buf_[(int)i] == '\n')
                return i;
        return null;
    }

    private nuint? findNextCrOrLf(nuint start, nuint end)
    {
        for (nuint i = start; i < end; i++)
            if (buf_[(int)i] == '\r' || buf_[(int)i] == '\n')
                return i;
        return null;
    }

    // void noteRSAt(const Char *p);
    private void noteRSAt(nuint idx)
    {
        info_.noteRS((Offset)(bufLimOffset_ - (bufLim_ - idx)));
    }
}
