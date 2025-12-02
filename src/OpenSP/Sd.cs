// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Information about the SGML declaration that is not syntax specific.
public class Sd : Resource
{
    // These must be in the same order as in the SGML declaration.
    public enum BooleanFeature
    {
        fDATATAG,
        fOMITTAG,
        fRANK,
        fSTARTTAGEMPTY,
        fSTARTTAGUNCLOSED,
        fENDTAGEMPTY,
        fENDTAGUNCLOSED,
        fATTRIBDEFAULT,
        fATTRIBOMITNAME,
        fATTRIBVALUE,
        fEMPTYNRM,
        fIMPLYDEFATTLIST,
        fIMPLYDEFDOCTYPE,
        fIMPLYDEFENTITY,
        fIMPLYDEFNOTATION,
        fIMPLICIT,
        fFORMAL,
        fURN,
        fKEEPRSRE
    }

    public const int nBooleanFeature = (int)BooleanFeature.fKEEPRSRE + 1;
    public const int fSHORTTAG_FIRST = (int)BooleanFeature.fSTARTTAGEMPTY;
    public const int fSHORTTAG_LAST = (int)BooleanFeature.fATTRIBVALUE;

    // These must be in the same order as in the SGML declaration.
    public enum NumberFeature
    {
        fSIMPLE,
        fEXPLICIT,
        fCONCUR,
        fSUBDOC
    }

    public const int nNumberFeature = (int)NumberFeature.fSUBDOC + 1;

    public enum NetEnable
    {
        netEnableNo,
        netEnableImmednet,
        netEnableAll
    }

    public enum EntityRef
    {
        entityRefAny,
        entityRefInternal,
        entityRefNone
    }

    public enum ImplydefElement
    {
        implydefElementNo,
        implydefElementYes,
        implydefElementAnyother
    }

    // These are names used in the SGML declaration.
    public enum ReservedName
    {
        rALL,
        rANY,
        rANYOTHER,
        rAPPINFO,
        rATTLIST,
        rATTRIB,
        rBASESET,
        rCAPACITY,
        rCHARSET,
        rCONCUR,
        rCONTROLS,
        rDATATAG,
        rDEFAULT,
        rDELIM,
        rDESCSET,
        rDOCTYPE,
        rDOCUMENT,
        rELEMENT,
        rEMPTY,
        rEMPTYNRM,
        rENDTAG,
        rENTITIES,
        rENTITY,
        rEXPLICIT,
        rFEATURES,
        rFORMAL,
        rFUNCHAR,
        rFUNCTION,
        rGENERAL,
        rIMMEDNET,
        rIMPLICIT,
        rIMPLYDEF,
        rINSTANCE,
        rINTEGRAL,
        rINTERNAL,
        rKEEPRSRE,
        rLCNMCHAR,
        rLCNMSTRT,
        rLINK,
        rMINIMIZE,
        rMSICHAR,
        rMSOCHAR,
        rMSSCHAR,
        rNAMECASE,
        rNAMECHAR,
        rNAMES,
        rNAMESTRT,
        rNAMING,
        rNETENABL,
        rNO,
        rNOASSERT,
        rNONE,
        rNOTATION,
        rOMITNAME,
        rOMITTAG,
        rOTHER,
        rPUBLIC,
        rQUANTITY,
        rRANK,
        rRE,
        rREF,
        rRS,
        rSCOPE,
        rSEEALSO,
        rSEPCHAR,
        rSGML,
        rSGMLREF,
        rSHORTREF,
        rSHORTTAG,
        rSHUNCHAR,
        rSIMPLE,
        rSPACE,
        rSTARTTAG,
        rSUBDOC,
        rSWITCHES,
        rSYNTAX,
        rSYSTEM,
        rTYPE,
        rUCNMCHAR,
        rUCNMSTRT,
        rUNCLOSED,
        rUNUSED,
        rURN,
        rVALIDITY,
        rVALUE,
        rYES
    }

    public enum Capacity
    {
        TOTALCAP,
        ENTCAP,
        ENTCHCAP,
        ELEMCAP,
        GRPCAP,
        EXGRPCAP,
        EXNMCAP,
        ATTCAP,
        ATTCHCAP,
        AVGRPCAP,
        NOTCAP,
        NOTCHCAP,
        IDCAP,
        IDREFCAP,
        MAPCAP,
        LKSETCAP,
        LKNMCAP
    }

    public const int nCapacity = (int)Capacity.LKNMCAP + 1;

    // Private fields
    private PackedBoolean[] booleanFeature_ = new PackedBoolean[nBooleanFeature];
    private Number[] numberFeature_ = new Number[nNumberFeature];
    private Number[] capacity_ = new Number[nCapacity];
    private PackedBoolean internalCharsetIsDocCharset_;
    // if null, use docCharset_
    private CharsetInfo? internalCharsetPtr_;
    private CharsetInfo docCharset_ = new CharsetInfo();
    private CharsetDecl docCharsetDecl_ = new CharsetDecl();
    private Boolean scopeInstance_;
    private Boolean www_;
    private NetEnable netEnable_;
    private EntityRef entityRef_;
    private ImplydefElement implydefElement_;
    private Boolean typeValid_;
    private Boolean integrallyStored_;
    private HashTable<int> namedCharTable_ = new HashTable<int>();
    private Ptr<EntityManager> entityManager_ = new Ptr<EntityManager>();

    // Static string arrays
    private static readonly string[] reservedName_ = {
        "ALL",
        "ANY",
        "ANYOTHER",
        "APPINFO",
        "ATTLIST",
        "ATTRIB",
        "BASESET",
        "CAPACITY",
        "CHARSET",
        "CONCUR",
        "CONTROLS",
        "DATATAG",
        "DEFAULT",
        "DELIM",
        "DESCSET",
        "DOCTYPE",
        "DOCUMENT",
        "ELEMENT",
        "EMPTY",
        "EMPTYNRM",
        "ENDTAG",
        "ENTITIES",
        "ENTITY",
        "EXPLICIT",
        "FEATURES",
        "FORMAL",
        "FUNCHAR",
        "FUNCTION",
        "GENERAL",
        "IMMEDNET",
        "IMPLICIT",
        "IMPLYDEF",
        "INSTANCE",
        "INTEGRAL",
        "INTERNAL",
        "KEEPRSRE",
        "LCNMCHAR",
        "LCNMSTRT",
        "LINK",
        "MINIMIZE",
        "MSICHAR",
        "MSOCHAR",
        "MSSCHAR",
        "NAMECASE",
        "NAMECHAR",
        "NAMES",
        "NAMESTRT",
        "NAMING",
        "NETENABL",
        "NO",
        "NOASSERT",
        "NONE",
        "NOTATION",
        "OMITNAME",
        "OMITTAG",
        "OTHER",
        "PUBLIC",
        "QUANTITY",
        "RANK",
        "RE",
        "REF",
        "RS",
        "SCOPE",
        "SEEALSO",
        "SEPCHAR",
        "SGML",
        "SGMLREF",
        "SHORTREF",
        "SHORTTAG",
        "SHUNCHAR",
        "SIMPLE",
        "SPACE",
        "STARTTAG",
        "SUBDOC",
        "SWITCHES",
        "SYNTAX",
        "SYSTEM",
        "TYPE",
        "UCNMCHAR",
        "UCNMSTRT",
        "UNCLOSED",
        "UNUSED",
        "URN",
        "VALIDITY",
        "VALUE",
        "YES"
    };

    private static readonly string[] capacityName_ = {
        "TOTALCAP",
        "ENTCAP",
        "ENTCHCAP",
        "ELEMCAP",
        "GRPCAP",
        "EXGRPCAP",
        "EXNMCAP",
        "ATTCAP",
        "ATTCHCAP",
        "AVGRPCAP",
        "NOTCAP",
        "NOTCHCAP",
        "IDCAP",
        "IDREFCAP",
        "MAPCAP",
        "LKSETCAP",
        "LKNMCAP"
    };

    private static readonly string[] quantityName_ = {
        "ATTCNT",
        "ATTSPLEN",
        "BSEQLEN",
        "DTAGLEN",
        "DTEMPLEN",
        "ENTLVL",
        "GRPCNT",
        "GRPGTCNT",
        "GRPLVL",
        "LITLEN",
        "NAMELEN",
        "NORMSEP",
        "PILEN",
        "TAGLEN",
        "TAGLVL"
    };

    private static readonly string[] generalDelimiterName_ = {
        "AND",
        "COM",
        "CRO",
        "DSC",
        "DSO",
        "DTGC",
        "DTGO",
        "ERO",
        "ETAGO",
        "GRPC",
        "GRPO",
        "HCRO",
        "LIT",
        "LITA",
        "MDC",
        "MDO",
        "MINUS",
        "MSC",
        "NET",
        "NESTC",
        "OPT",
        "OR",
        "PERO",
        "PIC",
        "PIO",
        "PLUS",
        "REFC",
        "REP",
        "RNI",
        "SEQ",
        "STAGO",
        "TAGC",
        "VI"
    };

    // Sd(const Ptr<EntityManager> &);
    public Sd(Ptr<EntityManager> entityManager)
    {
        entityManager_ = entityManager;
        EntityManager? em = entityManager.pointer();
        if (em != null)
        {
            internalCharsetIsDocCharset_ = em.internalCharsetIsDocCharset();
            docCharset_ = em.charset();
        }
        else
        {
            internalCharsetIsDocCharset_ = true;
        }
        scopeInstance_ = false;
        www_ = false;
        netEnable_ = NetEnable.netEnableNo;
        entityRef_ = EntityRef.entityRefAny;
        typeValid_ = true;
        integrallyStored_ = false;
        implydefElement_ = ImplydefElement.implydefElementNo;

        for (int i = 0; i < nBooleanFeature; i++)
            booleanFeature_[i] = false;
        for (int i = 0; i < nNumberFeature; i++)
            numberFeature_[i] = 0;
        for (int i = 0; i < nCapacity; i++)
            capacity_[i] = 35000;

        if (internalCharsetIsDocCharset_)
            internalCharsetPtr_ = null;
        else if (em != null)
            internalCharsetPtr_ = em.charset();
    }

    // Default constructor for convenience
    public Sd() : this(new Ptr<EntityManager>())
    {
    }

    // ~Sd();
    // C# GC handles cleanup

    // void setDocCharsetDesc(const UnivCharsetDesc &);
    public void setDocCharsetDesc(UnivCharsetDesc desc)
    {
        docCharset_.set(desc);
    }

    // Boolean matchesReservedName(const StringC &, ReservedName) const;
    public Boolean matchesReservedName(StringC name, ReservedName rn)
    {
        return execToInternal(reservedName_[(int)rn]).operatorEqual(name);
    }

    // int digitWeight(Char) const;
    public int digitWeight(Char c)
    {
        return internalCharset().digitWeight(c);
    }

    // int hexDigitWeight(Char) const;
    public int hexDigitWeight(Char c)
    {
        return internalCharset().hexDigitWeight(c);
    }

    // Boolean link() const;
    public Boolean link()
    {
        return numberFeature_[(int)NumberFeature.fSIMPLE] != 0
               || booleanFeature_[(int)BooleanFeature.fIMPLICIT]
               || numberFeature_[(int)NumberFeature.fEXPLICIT] != 0;
    }

    // Number simpleLink() const;
    public Number simpleLink()
    {
        return numberFeature_[(int)NumberFeature.fSIMPLE];
    }

    // Boolean implicitLink() const;
    public Boolean implicitLink()
    {
        return booleanFeature_[(int)BooleanFeature.fIMPLICIT];
    }

    // Number explicitLink() const;
    public Number explicitLink()
    {
        return numberFeature_[(int)NumberFeature.fEXPLICIT];
    }

    // Boolean startTagEmpty() const;
    public Boolean startTagEmpty()
    {
        return booleanFeature_[(int)BooleanFeature.fSTARTTAGEMPTY];
    }

    // Boolean startTagUnclosed() const;
    public Boolean startTagUnclosed()
    {
        return booleanFeature_[(int)BooleanFeature.fSTARTTAGUNCLOSED];
    }

    // NetEnable startTagNetEnable() const;
    public NetEnable startTagNetEnable()
    {
        return netEnable_;
    }

    // void setStartTagNetEnable(NetEnable);
    public void setStartTagNetEnable(NetEnable e)
    {
        netEnable_ = e;
    }

    // Boolean endTagEmpty() const;
    public Boolean endTagEmpty()
    {
        return booleanFeature_[(int)BooleanFeature.fENDTAGEMPTY];
    }

    // Boolean endTagUnclosed() const;
    public Boolean endTagUnclosed()
    {
        return booleanFeature_[(int)BooleanFeature.fENDTAGUNCLOSED];
    }

    // Boolean attributeDefault() const;
    public Boolean attributeDefault()
    {
        return booleanFeature_[(int)BooleanFeature.fATTRIBDEFAULT];
    }

    // Boolean attributeValueNotLiteral() const;
    public Boolean attributeValueNotLiteral()
    {
        return booleanFeature_[(int)BooleanFeature.fATTRIBVALUE];
    }

    // Boolean attributeOmitName() const;
    public Boolean attributeOmitName()
    {
        return booleanFeature_[(int)BooleanFeature.fATTRIBOMITNAME];
    }

    // Boolean emptyElementNormal() const;
    public Boolean emptyElementNormal()
    {
        return booleanFeature_[(int)BooleanFeature.fEMPTYNRM];
    }

    // Boolean implydefAttlist() const;
    public Boolean implydefAttlist()
    {
        return booleanFeature_[(int)BooleanFeature.fIMPLYDEFATTLIST];
    }

    // Boolean implydefDoctype() const;
    public Boolean implydefDoctype()
    {
        return booleanFeature_[(int)BooleanFeature.fIMPLYDEFDOCTYPE];
    }

    // ImplydefElement implydefElement() const;
    public ImplydefElement implydefElement()
    {
        return implydefElement_;
    }

    // void setImplydefElement(ImplydefElement);
    public void setImplydefElement(ImplydefElement i)
    {
        implydefElement_ = i;
    }

    // Boolean implydefEntity() const;
    public Boolean implydefEntity()
    {
        return booleanFeature_[(int)BooleanFeature.fIMPLYDEFENTITY];
    }

    // Boolean implydefNotation() const;
    public Boolean implydefNotation()
    {
        return booleanFeature_[(int)BooleanFeature.fIMPLYDEFNOTATION];
    }

    // Number concur() const;
    public Number concur()
    {
        return numberFeature_[(int)NumberFeature.fCONCUR];
    }

    // Boolean omittag() const;
    public Boolean omittag()
    {
        return booleanFeature_[(int)BooleanFeature.fOMITTAG];
    }

    // Boolean rank() const;
    public Boolean rank()
    {
        return booleanFeature_[(int)BooleanFeature.fRANK];
    }

    // Boolean datatag() const;
    public Boolean datatag()
    {
        return booleanFeature_[(int)BooleanFeature.fDATATAG];
    }

    // Boolean formal() const;
    public Boolean formal()
    {
        return booleanFeature_[(int)BooleanFeature.fFORMAL];
    }

    // Boolean urn() const;
    public Boolean urn()
    {
        return booleanFeature_[(int)BooleanFeature.fURN];
    }

    // Boolean keeprsre() const;
    public Boolean keeprsre()
    {
        return booleanFeature_[(int)BooleanFeature.fKEEPRSRE];
    }

    // Number subdoc() const;
    public Number subdoc()
    {
        return numberFeature_[(int)NumberFeature.fSUBDOC];
    }

    // StringC reservedName(int) const;
    public StringC reservedName(int i)
    {
        return execToInternal(reservedName_[i]);
    }

    // Boolean lookupQuantityName(const StringC &, Syntax::Quantity &) const;
    public Boolean lookupQuantityName(StringC name, out Syntax.Quantity quantity)
    {
        for (int i = 0; i < quantityName_.Length; i++)
        {
            if (execToInternal(quantityName_[i]).operatorEqual(name))
            {
                quantity = (Syntax.Quantity)i;
                return true;
            }
        }
        quantity = 0;
        return false;
    }

    // Boolean lookupGeneralDelimiterName(const StringC &, Syntax::DelimGeneral &) const;
    public Boolean lookupGeneralDelimiterName(StringC name, out Syntax.DelimGeneral delimGeneral)
    {
        for (int i = 0; i < generalDelimiterName_.Length; i++)
        {
            if (execToInternal(generalDelimiterName_[i]).operatorEqual(name))
            {
                delimGeneral = (Syntax.DelimGeneral)i;
                return true;
            }
        }
        delimGeneral = 0;
        return false;
    }

    // Boolean lookupCapacityName(const StringC &, Sd::Capacity &) const;
    public Boolean lookupCapacityName(StringC name, out Capacity capacity)
    {
        for (int i = 0; i < capacityName_.Length; i++)
        {
            if (execToInternal(capacityName_[i]).operatorEqual(name))
            {
                capacity = (Capacity)i;
                return true;
            }
        }
        capacity = 0;
        return false;
    }

    // StringC quantityName(Syntax::Quantity) const;
    public StringC quantityName(Syntax.Quantity q)
    {
        return execToInternal(quantityName_[(int)q]);
    }

    // Boolean internalCharsetIsDocCharset() const;
    public Boolean internalCharsetIsDocCharset()
    {
        return internalCharsetIsDocCharset_;
    }

    // const CharsetInfo &internalCharset() const;
    public CharsetInfo internalCharset()
    {
        return internalCharsetPtr_ ?? docCharset_;
    }

    // const CharsetInfo &docCharset() const;
    public CharsetInfo docCharset()
    {
        return docCharset_;
    }

    // Char execToInternal(char) const;
    public Char execToInternal(sbyte c)
    {
        return internalCharset().execToDesc(c);
    }

    // StringC execToInternal(const char *) const;
    public StringC execToInternal(string s)
    {
        return internalCharset().execToDesc(s);
    }

    // Number capacity(int) const;
    public Number capacity(int i)
    {
        return capacity_[i];
    }

    // void setCapacity(int, Number);
    public void setCapacity(int i, Number n)
    {
        capacity_[i] = n;
    }

    // StringC capacityName(int) const;
    public StringC capacityName(int i)
    {
        return execToInternal(capacityName_[i]);
    }

    // Boolean scopeInstance() const;
    public Boolean scopeInstance()
    {
        return scopeInstance_;
    }

    // void setScopeInstance();
    public void setScopeInstance()
    {
        scopeInstance_ = true;
    }

    // void setDocCharsetDecl(CharsetDecl &);
    public void setDocCharsetDecl(CharsetDecl decl)
    {
        decl.swap(docCharsetDecl_);
    }

    // const CharsetDecl &docCharsetDecl() const;
    public CharsetDecl docCharsetDecl()
    {
        return docCharsetDecl_;
    }

    // void setBooleanFeature(BooleanFeature, Boolean);
    public void setBooleanFeature(BooleanFeature i, Boolean b)
    {
        booleanFeature_[(int)i] = b;
    }

    // void setShorttag(Boolean);
    public void setShorttag(Boolean b)
    {
        for (int i = fSHORTTAG_FIRST; i <= fSHORTTAG_LAST; i++)
            booleanFeature_[i] = b;
        netEnable_ = NetEnable.netEnableAll;
    }

    // void setNumberFeature(NumberFeature, Number);
    public void setNumberFeature(NumberFeature i, Number n)
    {
        numberFeature_[(int)i] = n;
    }

    // StringC generalDelimiterName(Syntax::DelimGeneral) const;
    public StringC generalDelimiterName(Syntax.DelimGeneral d)
    {
        return execToInternal(generalDelimiterName_[(int)d]);
    }

    // UnivChar nameToUniv(const StringC &);
    public UnivChar nameToUniv(StringC name)
    {
        int? p = namedCharTable_.lookup(name);
        int n;
        if (p.HasValue)
        {
            n = p.Value;
        }
        else
        {
            n = (int)namedCharTable_.count();
            namedCharTable_.insert(name, n);
        }
        return (UnivChar)(n + 0x60000000);  // 10646 private use group
    }

    // Boolean www() const;
    public Boolean www()
    {
        return www_;
    }

    // void setWww(Boolean);
    public void setWww(Boolean b)
    {
        www_ = b;
    }

    // EntityRef entityRef() const;
    public EntityRef entityRef()
    {
        return entityRef_;
    }

    // void setEntityRef(EntityRef);
    public void setEntityRef(EntityRef r)
    {
        entityRef_ = r;
    }

    // Boolean typeValid() const;
    public Boolean typeValid()
    {
        return typeValid_;
    }

    // void setTypeValid(Boolean);
    public void setTypeValid(Boolean b)
    {
        typeValid_ = b;
    }

    // Boolean integrallyStored() const;
    public Boolean integrallyStored()
    {
        return integrallyStored_;
    }

    // void setIntegrallyStored(Boolean);
    public void setIntegrallyStored(Boolean b)
    {
        integrallyStored_ = b;
    }
}
