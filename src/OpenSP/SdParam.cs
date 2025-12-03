// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// SdParam - parameter for SGML declaration parsing
public class SdParam
{
    // Type constants - using uint to allow for reservedName + Sd.ReservedName values
    public const uint invalid = 0;
    public const uint eE = 1;
    public const uint minimumLiteral = 2;
    public const uint mdc = 3;
    public const uint minus = 4;
    public const uint number = 5;
    public const uint capacityName = 6;
    public const uint name = 7;
    public const uint paramLiteral = 8;
    public const uint systemIdentifier = 9;
    public const uint generalDelimiterName = 10;
    public const uint referenceReservedName = 11;
    public const uint quantityName = 12;
    public const uint reservedName = 13;  // Sd.ReservedName is added to this

    public uint type;
    public StringC token = new StringC();
    public Text literalText = new Text();
    public String<SyntaxChar> paramLiteralText = new String<SyntaxChar>();

    // Union members (only one is valid at a time based on type)
    public Number n;
    public Sd.Capacity capacityIndex;
    public Syntax.Quantity quantityIndex;
    public Syntax.ReservedName reservedNameIndex;
    public Syntax.DelimGeneral delimGeneralIndex;

    public SdParam()
    {
        type = invalid;
        n = 0;
        capacityIndex = 0;
        quantityIndex = 0;
        reservedNameIndex = 0;
        delimGeneralIndex = 0;
    }
}

// AllowedSdParams - specifies which parameter types are allowed
public class AllowedSdParams
{
    private const int maxAllow = 6;
    private uint[] allow_ = new uint[maxAllow];

    public AllowedSdParams(uint t1,
                           uint t2 = SdParam.invalid,
                           uint t3 = SdParam.invalid,
                           uint t4 = SdParam.invalid,
                           uint t5 = SdParam.invalid,
                           uint t6 = SdParam.invalid)
    {
        allow_[0] = t1;
        allow_[1] = t2;
        allow_[2] = t3;
        allow_[3] = t4;
        allow_[4] = t5;
        allow_[5] = t6;
    }

    public Boolean param(uint t)
    {
        for (int i = 0; i < maxAllow && allow_[i] != SdParam.invalid; i++)
            if (allow_[i] == t)
                return true;
        return false;
    }

    public uint get(int i)
    {
        if (i >= 0 && i < maxAllow)
            return allow_[i];
        return SdParam.invalid;
    }
}

// AllowedSdParamsMessageArg - for error messages showing allowed parameter types
public class AllowedSdParamsMessageArg : OtherMessageArg
{
    private AllowedSdParams allow_;
    private ConstPtr<Sd> sd_;

    public AllowedSdParamsMessageArg(AllowedSdParams allow, ConstPtr<Sd> sd)
    {
        allow_ = allow;
        sd_ = sd;
    }

    public override MessageArg copy()
    {
        return new AllowedSdParamsMessageArg(allow_, sd_);
    }

    public AllowedSdParams allowedParams()
    {
        return allow_;
    }

    public ConstPtr<Sd> sdPtr()
    {
        return sd_;
    }
}
