// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Syntax : Resource
{
    public enum ReservedName
    {
        rALL,
        rANY,
        rATTLIST,
        rCDATA,
        rCONREF,
        rCURRENT,
        rDATA,
        rDEFAULT,
        rDOCTYPE,
        rELEMENT,
        rEMPTY,
        rENDTAG,
        rENTITIES,
        rENTITY,
        rFIXED,
        rID,
        rIDLINK,
        rIDREF,
        rIDREFS,
        rIGNORE,
        rIMPLICIT,
        rIMPLIED,
        rINCLUDE,
        rINITIAL,
        rLINK,
        rLINKTYPE,
        rMD,
        rMS,
        rNAME,
        rNAMES,
        rNDATA,
        rNMTOKEN,
        rNMTOKENS,
        rNOTATION,
        rNUMBER,
        rNUMBERS,
        rNUTOKEN,
        rNUTOKENS,
        rO,
        rPCDATA,
        rPI,
        rPOSTLINK,
        rPUBLIC,
        rRCDATA,
        rRE,
        rREQUIRED,
        rRESTORE,
        rRS,
        rSDATA,
        rSHORTREF,
        rSIMPLE,
        rSPACE,
        rSTARTTAG,
        rSUBDOC,
        rSYSTEM,
        rTEMP,
        rUSELINK,
        rUSEMAP
    }

    public const int nNames = (int)ReservedName.rUSEMAP + 1;

    public enum Quantity
    {
        qATTCNT,
        qATTSPLEN,
        qBSEQLEN,
        qDTAGLEN,
        qDTEMPLEN,
        qENTLVL,
        qGRPCNT,
        qGRPGTCNT,
        qGRPLVL,
        qLITLEN,
        qNAMELEN,
        qNORMSEP,
        qPILEN,
        qTAGLEN,
        qTAGLVL
    }

    public const int nQuantity = (int)Quantity.qTAGLVL + 1;
    public const int unlimited = 100000000;

    public enum DelimGeneral
    {
        dAND,
        dCOM,
        dCRO,
        dDSC,
        dDSO,
        dDTGC,
        dDTGO,
        dERO,
        dETAGO,
        dGRPC,
        dGRPO,
        dHCRO,    // WWW TC addition
        dLIT,
        dLITA,
        dMDC,
        dMDO,
        dMINUS,
        dMSC,
        dNET,
        dNESTC,   // WWW TC addition
        dOPT,
        dOR,
        dPERO,
        dPIC,
        dPIO,
        dPLUS,
        dREFC,
        dREP,
        dRNI,
        dSEQ,
        dSTAGO,
        dTAGC,
        dVI
    }

    public const int nDelimGeneral = (int)DelimGeneral.dVI + 1;

    public enum StandardFunction
    {
        fRE,
        fRS,
        fSPACE
    }

    public enum FunctionClass
    {
        cFUNCHAR,
        cSEPCHAR,
        cMSOCHAR,
        cMSICHAR,
        cMSSCHAR
    }

    public enum Set
    {
        nameStart,
        digit,
        hexDigit,
        nmchar,           // LCNMCHAR or UCNMCHAR
        s,
        blank,
        sepchar,
        minimumData,
        significant,
        functionChar,     // function character
        sgmlChar
    }

    public const int nSet = (int)Set.sgmlChar + 1;

    public enum Category
    {
        otherCategory = 0,
        sCategory = 1,
        nameStartCategory = 2,
        digitCategory = 4,
        otherNameCategory = 8
    }

    // Static constants for category bit masks (for use in Attribute.cs)
    public const uint nameStartCategory = (uint)Category.nameStartCategory;
    public const uint digitCategory = (uint)Category.digitCategory;
    public const uint otherNameCategory = (uint)Category.otherNameCategory;

    // Static reference quantities
    private static readonly int[] referenceQuantity_ = {
        40,   // qATTCNT
        960,  // qATTSPLEN
        960,  // qBSEQLEN
        16,   // qDTAGLEN
        16,   // qDTEMPLEN
        16,   // qENTLVL
        32,   // qGRPCNT
        96,   // qGRPGTCNT
        16,   // qGRPLVL
        240,  // qLITLEN
        8,    // qNAMELEN
        2,    // qNORMSEP
        240,  // qPILEN
        960,  // qTAGLEN
        24    // qTAGLVL
    };

    // Private fields
    private ISet<Char> shunchar_ = new ISet<Char>();
#pragma warning disable CS0414 // Field is assigned but never used (preserved from upstream C++ code)
    private PackedBoolean shuncharControls_;
#pragma warning restore CS0414
    private ISet<Char>[] set_ = new ISet<Char>[nSet];
    private Char[] standardFunction_ = new Char[3];
    private PackedBoolean[] standardFunctionValid_ = new PackedBoolean[3];
    private Boolean namecaseGeneral_;
    private Boolean namecaseEntity_;
    private StringC[] delimGeneral_ = new StringC[nDelimGeneral];
    private Vector<StringC> delimShortrefComplex_ = new Vector<StringC>();
    private ISet<Char> delimShortrefSimple_ = new ISet<Char>();
    private StringC[] names_ = new StringC[nNames];
    private Number[] quantity_ = new Number[nQuantity];
    private HashTable<int> nameTable_ = new HashTable<int>();
    private HashTable<Char> functionTable_ = new HashTable<Char>();
    private SubstTable upperSubst_ = new SubstTable();
    private SubstTable identitySubst_ = new SubstTable();
    private SubstTable? generalSubst_;
    private SubstTable? entitySubst_;
    private XcharMap<byte> categoryTable_;
    private Boolean multicode_;
    private XcharMap<byte> markupScanTable_;
    private Boolean hasMarkupScanTable_;
    private Vector<StringC> entityNames_ = new Vector<StringC>();
    private StringC entityChars_ = new StringC();

    // Syntax(const Sd &sd);
    public Syntax(Sd sd)
    {
        // Initialize arrays
        for (int i = 0; i < nSet; i++)
            set_[i] = new ISet<Char>();
        for (int i = 0; i < nDelimGeneral; i++)
            delimGeneral_[i] = new StringC();
        for (int i = 0; i < nNames; i++)
            names_[i] = new StringC();

        categoryTable_ = new XcharMap<byte>((byte)Category.otherCategory);
        markupScanTable_ = new XcharMap<byte>(MarkupScan.normal);
        shuncharControls_ = false;
        multicode_ = false;
        hasMarkupScanTable_ = false;
        generalSubst_ = null;
        entitySubst_ = null;

        string lcletter = "abcdefghijklmnopqrstuvwxyz";
        string ucletter = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        for (int i = 0; i < 26; i++)
        {
            Char lc = sd.execToInternal((sbyte)lcletter[i]);
            Char uc = sd.execToInternal((sbyte)ucletter[i]);
            set_[(int)Set.nameStart].add(lc);
            set_[(int)Set.nameStart].add(uc);
            set_[(int)Set.minimumData].add(lc);
            set_[(int)Set.minimumData].add(uc);
            set_[(int)Set.significant].add(lc);
            set_[(int)Set.significant].add(uc);
            if (i < 6)
            {
                set_[(int)Set.hexDigit].add(lc);
                set_[(int)Set.hexDigit].add(uc);
            }
            categoryTable_.setChar(lc, (byte)Category.nameStartCategory);
            categoryTable_.setChar(uc, (byte)Category.nameStartCategory);
            subst(lc, uc);
        }

        string digits = "0123456789";
        for (int i = 0; i < 10; i++)
        {
            Char c = sd.execToInternal((sbyte)digits[i]);
            set_[(int)Set.digit].add(c);
            set_[(int)Set.hexDigit].add(c);
            set_[(int)Set.minimumData].add(c);
            set_[(int)Set.significant].add(c);
            categoryTable_.setChar(c, (byte)Category.digitCategory);
        }

        string special = "'()+,-./:=?";
        for (int i = 0; i < special.Length; i++)
        {
            Char c = sd.execToInternal((sbyte)special[i]);
            set_[(int)Set.minimumData].add(c);
            set_[(int)Set.significant].add(c);
        }

        if (sd.www())
        {
            sbyte[] wwwSpecial = { 33, 35, 36, 37, 42, 59, 64, 95 };
            for (int i = 0; i < wwwSpecial.Length; i++)
            {
                CharsetInfo charset = sd.internalCharset();
                WideChar c;
                ISet<WideChar> set = new ISet<WideChar>();
                if (charset.univToDesc((UnivChar)wwwSpecial[i], out c, set) > 0
                    && c <= Constant.charMax)
                {
                    set_[(int)Set.minimumData].add((Char)c);
                    set_[(int)Set.significant].add((Char)c);
                }
            }
        }

        for (int i = 0; i < nQuantity; i++)
            quantity_[i] = (Number)referenceQuantity_[i];
        for (int i = 0; i < 3; i++)
            standardFunctionValid_[i] = false;
    }

    // Default constructor for stub compatibility
    public Syntax()
    {
        // Initialize arrays
        for (int i = 0; i < nSet; i++)
            set_[i] = new ISet<Char>();
        for (int i = 0; i < nDelimGeneral; i++)
            delimGeneral_[i] = new StringC();
        for (int i = 0; i < nNames; i++)
            names_[i] = new StringC();

        categoryTable_ = new XcharMap<byte>((byte)Category.otherCategory);
        markupScanTable_ = new XcharMap<byte>(MarkupScan.normal);

        for (int i = 0; i < nQuantity; i++)
            quantity_[i] = (Number)referenceQuantity_[i];
    }

    // virtual ~Syntax();
    // C# handles via GC

    // Number quantity(Quantity q) const;
    public Number quantity(Quantity q)
    {
        return quantity_[(int)q];
    }

    // void setQuantity(int i, Number n);
    public void setQuantity(int i, Number n)
    {
        quantity_[i] = n;
    }

    // const SubstTable *generalSubstTable() const;
    public SubstTable? generalSubstTable()
    {
        return generalSubst_;
    }

    // const SubstTable *entitySubstTable() const;
    public SubstTable? entitySubstTable()
    {
        return entitySubst_;
    }

    // const SubstTable &upperSubstTable() const;
    public SubstTable upperSubstTable()
    {
        return upperSubst_;
    }

    // int nDelimShortrefComplex() const;
    public int nDelimShortrefComplex()
    {
        return (int)delimShortrefComplex_.size();
    }

    // const StringC &delimGeneral(int i) const;
    public StringC delimGeneral(int i)
    {
        return delimGeneral_[i];
    }

    // const StringC &delimShortrefComplex(size_t i) const;
    public StringC delimShortrefComplex(nuint i)
    {
        return delimShortrefComplex_[i];
    }

    // const ISet<Char> &delimShortrefSimple() const;
    public ISet<Char> delimShortrefSimple()
    {
        return delimShortrefSimple_;
    }

    // Boolean hasShortrefs() const;
    public Boolean hasShortrefs()
    {
        return delimShortrefComplex_.size() > 0 || !delimShortrefSimple_.isEmpty();
    }

    // Char standardFunction(int i) const;
    public Char standardFunction(int i)
    {
        return standardFunction_[i];
    }

    // Boolean getStandardFunction(int i, Char &result) const;
    public Boolean getStandardFunction(int i, out Char result)
    {
        if (standardFunctionValid_[i])
        {
            result = standardFunction_[i];
            return true;
        }
        result = 0;
        return false;
    }

    // const ISet<Char> *charSet(int i) const;
    public ISet<Char> charSet(int i)
    {
        return set_[i];
    }

    // Boolean isNameCharacter(Xchar c) const;
    public Boolean isNameCharacter(Xchar c)
    {
        return categoryTable_[c] >= (byte)Category.nameStartCategory;
    }

    // Boolean isNameStartCharacter(Xchar c) const;
    public Boolean isNameStartCharacter(Xchar c)
    {
        return categoryTable_[c] == (byte)Category.nameStartCategory;
    }

    // Boolean isDigit(Xchar c) const;
    public Boolean isDigit(Xchar c)
    {
        return categoryTable_[c] == (byte)Category.digitCategory;
    }

    // Boolean isS(Xchar c) const;
    public Boolean isS(Xchar c)
    {
        return categoryTable_[c] == (byte)Category.sCategory;
    }

    // Boolean isB(Xchar c) const;
    public Boolean isB(Xchar c)
    {
        return categoryTable_[c] == (byte)Category.sCategory
               && !(standardFunctionValid_[(int)StandardFunction.fRE]
                    && c == standardFunction_[(int)StandardFunction.fRE])
               && !(standardFunctionValid_[(int)StandardFunction.fRS]
                    && c == standardFunction_[(int)StandardFunction.fRS]);
    }

    // Category charCategory(Xchar c) const;
    public Category charCategory(Xchar c)
    {
        return (Category)categoryTable_[c];
    }

    // Boolean isSgmlChar(Xchar c) const;
    public Boolean isSgmlChar(Xchar c)
    {
        return c >= 0 && set_[(int)Set.sgmlChar].contains((Char)c);
    }

    // const StringC &reservedName(ReservedName i) const;
    public StringC reservedName(ReservedName i)
    {
        return names_[(int)i];
    }

    // size_t attcnt() const;
    public nuint attcnt()
    {
        return quantity(Quantity.qATTCNT);
    }

    // size_t attsplen() const;
    public nuint attsplen()
    {
        return quantity(Quantity.qATTSPLEN);
    }

    // size_t namelen() const;
    public nuint namelen()
    {
        return quantity(Quantity.qNAMELEN);
    }

    // size_t penamelen() const;
    public nuint penamelen()
    {
        return quantity(Quantity.qNAMELEN) - delimGeneral((int)DelimGeneral.dPERO).size();
    }

    // size_t litlen() const;
    public nuint litlen()
    {
        return quantity(Quantity.qLITLEN);
    }

    // size_t normsep() const;
    public nuint normsep()
    {
        return quantity(Quantity.qNORMSEP);
    }

    // size_t dtemplen() const;
    public nuint dtemplen()
    {
        return quantity(Quantity.qDTEMPLEN);
    }

    // size_t grpcnt() const;
    public nuint grpcnt()
    {
        return quantity(Quantity.qGRPCNT);
    }

    // size_t grpgtcnt() const;
    public nuint grpgtcnt()
    {
        return quantity(Quantity.qGRPGTCNT);
    }

    // size_t grplvl() const;
    public nuint grplvl()
    {
        return quantity(Quantity.qGRPLVL);
    }

    // size_t taglvl() const;
    public nuint taglvl()
    {
        return quantity(Quantity.qTAGLVL);
    }

    // size_t taglen() const;
    public nuint taglen()
    {
        return quantity(Quantity.qTAGLEN);
    }

    // size_t entlvl() const;
    public nuint entlvl()
    {
        return quantity(Quantity.qENTLVL);
    }

    // size_t pilen() const;
    public nuint pilen()
    {
        return quantity(Quantity.qPILEN);
    }

    // Char space() const;
    public Char space()
    {
        return standardFunction((int)StandardFunction.fSPACE);
    }

    // void setSgmlChar(const ISet<Char> &set);
    public void setSgmlChar(ISet<Char> set)
    {
        set_[(int)Set.sgmlChar] = set;
    }

    // static int referenceQuantity(Quantity i);
    public static int referenceQuantity(Quantity i)
    {
        return referenceQuantity_[(int)i];
    }

    // void setShuncharControls();
    public void setShuncharControls()
    {
        shuncharControls_ = true;
    }

    // const XcharMap<unsigned char> &markupScanTable() const;
    public XcharMap<byte> markupScanTable()
    {
        return markupScanTable_;
    }

    // Boolean multicode() const;
    public Boolean multicode()
    {
        return multicode_;
    }

    // Boolean namecaseGeneral() const;
    public Boolean namecaseGeneral()
    {
        return namecaseGeneral_;
    }

    // Boolean namecaseEntity() const;
    public Boolean namecaseEntity()
    {
        return namecaseEntity_;
    }

    // size_t nEntities() const;
    public nuint nEntities()
    {
        return entityNames_.size();
    }

    // const StringC &entityName(size_t i) const;
    public StringC entityName(nuint i)
    {
        return entityNames_[i];
    }

    // Char entityChar(size_t i) const;
    public Char entityChar(nuint i)
    {
        return entityChars_[i];
    }

    // void addNameCharacters(const ISet<Char> &set);
    public void addNameCharacters(ISet<Char> set)
    {
        ISetIter<Char> iter = new ISetIter<Char>(set);
        Char min, max;
        while (iter.next(out min, out max) != 0)
        {
            set_[(int)Set.nmchar].addRange(min, max);
            set_[(int)Set.significant].addRange(min, max);
            categoryTable_.setRange(min, max, (byte)Category.otherNameCategory);
        }
    }

    // void addNameStartCharacters(const ISet<Char> &set);
    public void addNameStartCharacters(ISet<Char> set)
    {
        ISetIter<Char> iter = new ISetIter<Char>(set);
        Char min, max;
        while (iter.next(out min, out max) != 0)
        {
            set_[(int)Set.nameStart].addRange(min, max);
            set_[(int)Set.significant].addRange(min, max);
            categoryTable_.setRange(min, max, (byte)Category.nameStartCategory);
        }
    }

    // void addSubst(Char lc, Char uc);
    public void addSubst(Char lc, Char uc)
    {
        subst(lc, uc);
    }

    // void setStandardFunction(StandardFunction f, Char c);
    public void setStandardFunction(StandardFunction f, Char c)
    {
        standardFunction_[(int)f] = c;
        standardFunctionValid_[(int)f] = true;
        set_[(int)Set.minimumData].add(c);
        set_[(int)Set.s].add(c);
        categoryTable_.setChar(c, (byte)Category.sCategory);
        set_[(int)Set.functionChar].add(c);
        set_[(int)Set.significant].add(c);
        switch (f)
        {
            case StandardFunction.fSPACE:
                set_[(int)Set.blank].add(c);
                break;
            case StandardFunction.fRE:
            case StandardFunction.fRS:
                break;
        }
    }

    // void enterStandardFunctionNames();
    public void enterStandardFunctionNames()
    {
        ReservedName[] name = { ReservedName.rRE, ReservedName.rRS, ReservedName.rSPACE };
        for (int i = 0; i < 3; i++)
        {
            if (standardFunctionValid_[i])
                functionTable_.insert(reservedName(name[i]), standardFunction_[i]);
        }
    }

    // void setDelimGeneral(int i, const StringC &str);
    public void setDelimGeneral(int i, StringC str)
    {
        delimGeneral_[i] = str;
        for (nuint j = 0; j < str.size(); j++)
            set_[(int)Set.significant].add(str[j]);
    }

    // void addDelimShortref(const StringC &str, const CharsetInfo &charset);
    public void addDelimShortref(StringC str, CharsetInfo charset)
    {
        if (str.size() == 1 && str[0] != charset.execToDesc((sbyte)'B') && !isB((Xchar)str[0]))
            delimShortrefSimple_.add(str[0]);
        else
            delimShortrefComplex_.push_back(str);
        for (nuint i = 0; i < str.size(); i++)
            set_[(int)Set.significant].add(str[i]);
    }

    // void addFunctionChar(const StringC &str, FunctionClass fun, Char c);
    public void addFunctionChar(StringC str, FunctionClass fun, Char c)
    {
        switch (fun)
        {
            case FunctionClass.cFUNCHAR:
                break;
            case FunctionClass.cSEPCHAR:
                set_[(int)Set.s].add(c);
                categoryTable_.setChar(c, (byte)Category.sCategory);
                set_[(int)Set.blank].add(c);
                set_[(int)Set.sepchar].add(c);
                break;
            case FunctionClass.cMSOCHAR:
                multicode_ = true;
                if (!hasMarkupScanTable_)
                {
                    markupScanTable_ = new XcharMap<byte>(MarkupScan.normal);
                    hasMarkupScanTable_ = true;
                }
                markupScanTable_.setChar(c, MarkupScan.@out);
                break;
            case FunctionClass.cMSICHAR:
                if (!hasMarkupScanTable_)
                {
                    markupScanTable_ = new XcharMap<byte>(MarkupScan.normal);
                    hasMarkupScanTable_ = true;
                }
                markupScanTable_.setChar(c, MarkupScan.@in);
                break;
            case FunctionClass.cMSSCHAR:
                multicode_ = true;
                if (!hasMarkupScanTable_)
                {
                    markupScanTable_ = new XcharMap<byte>(MarkupScan.normal);
                    hasMarkupScanTable_ = true;
                }
                markupScanTable_.setChar(c, MarkupScan.suppress);
                break;
        }
        set_[(int)Set.functionChar].add(c);
        set_[(int)Set.significant].add(c);
        functionTable_.insert(str, c);
    }

    // void setName(int i, const StringC &str);
    public void setName(int i, StringC str)
    {
        names_[i] = str;
        nameTable_.insert(str, i);
    }

    // void setNamecaseGeneral(Boolean b);
    public void setNamecaseGeneral(Boolean b)
    {
        namecaseGeneral_ = b;
        generalSubst_ = b ? upperSubst_ : identitySubst_;
    }

    // void setNamecaseEntity(Boolean b);
    public void setNamecaseEntity(Boolean b)
    {
        namecaseEntity_ = b;
        entitySubst_ = b ? upperSubst_ : identitySubst_;
    }

    // void addShunchar(Char c);
    public void addShunchar(Char c)
    {
        shunchar_.add(c);
    }

    // Boolean lookupReservedName(const StringC &str, ReservedName *result) const;
    public Boolean lookupReservedName(StringC str, out ReservedName result)
    {
        int? tem = nameTable_.lookup(str);
        if (tem.HasValue)
        {
            result = (ReservedName)tem.Value;
            return true;
        }
        result = 0;
        return false;
    }

    // Boolean lookupFunctionChar(const StringC &name, Char *result) const;
    public Boolean lookupFunctionChar(StringC name, out Char result)
    {
        Char? p = functionTable_.lookup(name);
        if (p.HasValue)
        {
            result = p.Value;
            return true;
        }
        result = 0;
        return false;
    }

    // Boolean charFunctionName(Char c, const StringC *&name) const;
    public Boolean charFunctionName(Char c, out StringC? name)
    {
        HashTableIter<Char> iter = new HashTableIter<Char>(functionTable_);
        StringC? key;
        Char value;
        while (iter.next(out key, out value))
        {
            if (value == c)
            {
                name = key;
                return true;
            }
        }
        name = null;
        return false;
    }

    // Boolean isValidShortref(const StringC &str) const;
    public Boolean isValidShortref(StringC str)
    {
        if (str.size() == 1 && delimShortrefSimple_.contains(str[0]))
            return true;
        for (nuint i = 0; i < delimShortrefComplex_.size(); i++)
            if (str.operatorEqual(delimShortrefComplex_[i]))
                return true;
        return false;
    }

    // StringC rniReservedName(ReservedName i) const;
    public StringC rniReservedName(ReservedName i)
    {
        StringC result = new StringC(delimGeneral((int)DelimGeneral.dRNI));
        result.append(reservedName(i).data()!, reservedName(i).size());
        return result;
    }

    // const StringC &peroDelim() const;
    public StringC peroDelim()
    {
        return delimGeneral((int)DelimGeneral.dPERO);
    }

    // Boolean isHexDigit(Xchar c) const;
    public Boolean isHexDigit(Xchar c)
    {
        switch ((Category)categoryTable_[c])
        {
            case Category.digitCategory:
                return true;
            case Category.nameStartCategory:
                break;
            default:
                return false;
        }
        return set_[(int)Set.hexDigit].contains((Char)c);
    }

    // void addEntity(const StringC &name, Char c);
    public void addEntity(StringC name, Char c)
    {
        entityNames_.push_back(name);
        entityChars_.operatorPlusAssign(c);
    }

    // Private helper
    private void subst(Char from, Char to)
    {
        upperSubst_.addSubst(from, to);
    }
}
