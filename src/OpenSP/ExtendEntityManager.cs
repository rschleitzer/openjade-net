// Copyright (c) 1994, 1995, 1996 James Clark
// See the file COPYING for copying permission.

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
            if (!idCharset.descToUniv(soi[i], out univ)
                || univ >= 127
                || univ < 32
                || univ == 36        // $
                || univ == 96        // `
                || univ == 92        // backslash
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
        if (catalogManager_ == null || !catalogManager_.mapCatalog(parsedSysid, this, mgr))
            return null;
        // Create ExternalInputSource with parsedSysid
        // This is a simplified version - full implementation would create ExternalInputSource
        return null; // TODO: implement ExternalInputSource
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
        StorageObjectLocation? defSoLocP = null;
        // TODO: Get default location from defLoc

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
        // Simplified FSI parsing - just create a single storage object spec
        parsedSysid.resize(parsedSysid.size() + 1);
        StorageObjectSpec sos = parsedSysid.back();
        sos.specId.assign(str.data()!, str.size());
        sos.storageManager = guessStorageType(str, docCharset);
        if (sos.storageManager == null)
        {
            if (defLoc != null && defLoc.storageObjectSpec?.storageManager?.inheritable() == true)
                sos.storageManager = defLoc.storageObjectSpec.storageManager;
            else
                sos.storageManager = defaultStorageManager_;
        }
        setDefaults(sos, isNdataFlag, defLoc);
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
