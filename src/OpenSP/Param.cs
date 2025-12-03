// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// This describes a markup declaration parameter.

namespace OpenSP;

public class Param
{
    public const byte invalid = 0;
    public const byte silent = 1;
    public const byte dso = 2;
    public const byte mdc = 3;
    public const byte minus = 4;
    public const byte pero = 5;
    public const byte inclusions = 6;
    public const byte exclusions = 7;
    public const byte nameGroup = 8;
    public const byte nameTokenGroup = 9;
    public const byte modelGroup = 10;
    public const byte number = 11;
    public const byte minimumLiteral = 12;
    public const byte attributeValueLiteral = 13;
    public const byte tokenizedAttributeValueLiteral = 14;
    public const byte systemIdentifier = 15;
    public const byte paramLiteral = 16;
    public const byte name = 17;
    public const byte entityName = 18;
    public const byte paramEntityName = 19;
    public const byte attributeValue = 20;
    public const byte reservedName = 21;  // Syntax::ReservedName is added to this
    // this is a reserved name preceded by the RNI delimiter
    public static readonly byte indicatedReservedName = (byte)(reservedName + Syntax.nNames);
    public static readonly int nTypes = indicatedReservedName + Syntax.nNames;

    public byte type;
    public Location startLocation = new Location();
    public Text literalText = new Text();
    public Boolean lita;
    public Owner<ModelGroup> modelGroupPtr = new Owner<ModelGroup>();
    public Vector<NameToken> nameTokenVector = new Vector<NameToken>();
    public StringC token = new StringC();       // name nameToken; with substitution
    public StringC origToken = new StringC();
    public Vector<ElementType?> elementVector = new Vector<ElementType?>();

    public Param()
    {
    }
}

public class AllowedParams
{
    private PackedBoolean silent_;
    private PackedBoolean mdc_;
    private PackedBoolean rni_;
    private PackedBoolean dso_;
    private PackedBoolean inclusions_;
    private PackedBoolean exclusions_;
    // invalid, minus, pero
    private byte extraDelimiter_;
    // invalid, nameGroup, nameTokenGroup, modelGroup
    private byte group_;
    // invalid, reservedName, name, entityName, paramEntityName, attributeValue
    private byte nameStart_;
    // invalid, number, attributeValue
    private byte digit_;
    // invalid, attributeValue
    private byte nmchar_;   // LCNMCHAR or UCNMCHAR
    // invalid, minimumLiteral, systemIdentifier, paramLiteral,
    // (tokenized)attributeValueLiteral
    private byte literal_;
    private PackedBoolean[] reservedNames_ = new PackedBoolean[Syntax.nNames];
    private Mode mainMode_;     // mdMode mdMinusMode mdPeroMode

    public AllowedParams(byte p1,
                         byte p2 = Param.invalid,
                         byte p3 = Param.invalid,
                         byte p4 = Param.invalid,
                         byte p5 = Param.invalid,
                         byte p6 = Param.invalid,
                         byte p7 = Param.invalid,
                         byte p8 = Param.invalid,
                         byte p9 = Param.invalid,
                         byte p10 = Param.invalid)
    {
        init();
        allow(p1);
        allow(p2);
        allow(p3);
        allow(p4);
        allow(p5);
        allow(p6);
        allow(p7);
        allow(p8);
        allow(p9);
        allow(p10);
    }

    public AllowedParams(byte[] v, int n)
    {
        init();
        for (int i = 0; i < n; i++)
            allow(v[i]);
    }

    private void init()
    {
        for (int i = 0; i < Syntax.nNames; i++)
            reservedNames_[i] = false;
        mainMode_ = Mode.mdMode;
        silent_ = false;
        mdc_ = false;
        rni_ = false;
        dso_ = false;
        inclusions_ = false;
        exclusions_ = false;
        extraDelimiter_ = Param.invalid;
        group_ = Param.invalid;
        nameStart_ = Param.invalid;
        digit_ = Param.invalid;
        nmchar_ = Param.invalid;
        literal_ = Param.invalid;
    }

    private void allow(byte p)
    {
        switch (p)
        {
            case Param.invalid:
                break;
            case Param.silent:
                silent_ = true;
                break;
            case Param.dso:
                dso_ = true;
                break;
            case Param.mdc:
                mdc_ = true;
                break;
            case Param.minus:
                // ASSERT(mainMode_ == mdMode);
                mainMode_ = Mode.mdMinusMode;
                extraDelimiter_ = p;
                break;
            case Param.pero:
                // ASSERT(mainMode_ == mdMode);
                mainMode_ = Mode.mdPeroMode;
                extraDelimiter_ = p;
                break;
            case Param.inclusions:
                inclusions_ = true;
                break;
            case Param.exclusions:
                exclusions_ = true;
                break;
            case Param.nameGroup:
            case Param.nameTokenGroup:
            case Param.modelGroup:
                // ASSERT(group_ == Param.invalid);
                group_ = p;
                break;
            case Param.number:
                // ASSERT(digit_ == Param.invalid);
                digit_ = p;
                break;
            case Param.minimumLiteral:
            case Param.tokenizedAttributeValueLiteral:
            case Param.attributeValueLiteral:
            case Param.systemIdentifier:
            case Param.paramLiteral:
                // ASSERT(literal_ == Param.invalid);
                literal_ = p;
                break;
            case Param.name:
            case Param.entityName:
            case Param.paramEntityName:
                // ASSERT(nameStart_ == Param.invalid);
                nameStart_ = p;
                break;
            case Param.attributeValue:
                // ASSERT(nameStart_ == Param.invalid);
                nameStart_ = p;
                // ASSERT(digit_ == Param.invalid);
                digit_ = p;
                // ASSERT(nmchar_ == Param.invalid);
                nmchar_ = p;
                break;
            default:
                if (p < Param.indicatedReservedName)
                {
                    // ASSERT(nameStart_ == Param.invalid || nameStart_ == Param.reservedName);
                    // ASSERT(rni_ == 0);
                    nameStart_ = Param.reservedName;
                    reservedNames_[p - Param.reservedName] = true;
                }
                else
                {
                    // ASSERT(nameStart_ != Param.reservedName);
                    rni_ = true;
                    reservedNames_[p - Param.indicatedReservedName] = true;
                }
                break;
        }
    }

    public Mode mainMode()
    {
        return mainMode_;
    }

    public Boolean mdc()
    {
        return mdc_;
    }

    public Boolean rni()
    {
        return rni_;
    }

    public Boolean dso()
    {
        return dso_;
    }

    public Boolean inclusions()
    {
        return inclusions_;
    }

    public Boolean exclusions()
    {
        return exclusions_;
    }

    public Boolean reservedName(Syntax.ReservedName i)
    {
        return reservedNames_[(int)i];
    }

    public byte group()
    {
        return group_;
    }

    public byte nameStart()
    {
        return nameStart_;
    }

    public byte digit()
    {
        return digit_;
    }

    public byte nmchar()
    {
        return nmchar_;
    }

    public byte literal()
    {
        return literal_;
    }

    public Boolean silent()
    {
        return silent_;
    }
}

public class AllowedParamsMessageArg : MessageArg
{
    private AllowedParams allow_;
    private ConstPtr<Syntax> syntax_;

    public AllowedParamsMessageArg(AllowedParams allow, ConstPtr<Syntax> syntax)
    {
        allow_ = allow;
        syntax_ = syntax;
    }

    public MessageArg copy()
    {
        return new AllowedParamsMessageArg(allow_, syntax_);
    }

    public void append(MessageBuilder builder)
    {
        Syntax.DelimGeneral[] delims = new Syntax.DelimGeneral[3];
        int nDelims = 0;
        if (allow_.mdc())
            delims[nDelims++] = Syntax.DelimGeneral.dMDC;
        if (allow_.dso())
            delims[nDelims++] = Syntax.DelimGeneral.dDSO;
        switch (allow_.mainMode())
        {
            case Mode.mdMinusMode:
                delims[nDelims++] = Syntax.DelimGeneral.dMINUS;
                break;
            case Mode.mdPeroMode:
                delims[nDelims++] = Syntax.DelimGeneral.dPERO;
                break;
            default:
                break;
        }
        Boolean first = true;
        int i;
        for (i = 0; i < nDelims; i++)
        {
            if (!first)
                builder.appendFragment(ParserMessages.listSep);
            else
                first = false;
            StringC delim = syntax_.pointer()!.delimGeneral((int)delims[i]);
            builder.appendFragment(ParserMessages.delimStart);
            builder.appendChars(delim.data(), delim.size());
        }
        MessageFragment?[] fragment = new MessageFragment?[5];
        int nFragments = 0;
        if (allow_.inclusions())
            fragment[nFragments++] = ParserMessages.inclusions;
        if (allow_.exclusions())
            fragment[nFragments++] = ParserMessages.exclusions;
        switch (allow_.literal())
        {
            case Param.minimumLiteral:
                fragment[nFragments++] = ParserMessages.minimumLiteral;
                break;
            case Param.attributeValueLiteral:
            case Param.tokenizedAttributeValueLiteral:
                fragment[nFragments++] = ParserMessages.attributeValueLiteral;
                break;
            case Param.systemIdentifier:
                fragment[nFragments++] = ParserMessages.systemIdentifier;
                break;
            case Param.paramLiteral:
                fragment[nFragments++] = ParserMessages.parameterLiteral;
                break;
        }
        switch (allow_.nameStart())
        {
            case Param.name:
            case Param.entityName:
            case Param.paramEntityName:
                fragment[nFragments++] = ParserMessages.name;
                break;
            case Param.attributeValue:
                fragment[nFragments++] = ParserMessages.attributeValue;
                break;
        }
        if (allow_.digit() == Param.number)
            fragment[nFragments++] = ParserMessages.number;

        for (i = 0; i < nFragments; i++)
        {
            if (!first)
                builder.appendFragment(ParserMessages.listSep);
            else
                first = false;
            builder.appendFragment(fragment[i]!);
        }
        if (allow_.rni() || allow_.nameStart() == Param.reservedName)
        {
            for (int j = 0; j < Syntax.nNames; j++)
            {
                if (allow_.reservedName((Syntax.ReservedName)j))
                {
                    if (!first)
                        builder.appendFragment(ParserMessages.listSep);
                    else
                        first = false;
                    StringC str = new StringC();
                    if (allow_.rni())
                        str = new StringC(syntax_.pointer()!.delimGeneral((int)Syntax.DelimGeneral.dRNI));
                    str.operatorPlusAssign(syntax_.pointer()!.reservedName((Syntax.ReservedName)j));
                    builder.appendChars(str.data(), str.size());
                }
            }
        }
    }

    public void appendToStringC(StringC result)
    {
        StringCMessageBuilder builder = new StringCMessageBuilder(result);
        append(builder);
    }
}
