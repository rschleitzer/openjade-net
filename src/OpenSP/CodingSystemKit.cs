// Copyright (c) 1997 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public abstract class InputCodingSystemKit : Resource
{
    protected CharsetInfo systemCharset_ = new CharsetInfo();

    // virtual ~InputCodingSystemKit();
    // C# GC handles cleanup

    // virtual const InputCodingSystem *identityInputCodingSystem() const = 0;
    public abstract InputCodingSystem identityInputCodingSystem();

    // virtual const InputCodingSystem *makeInputCodingSystem(const StringC &,
    //                                                        const CharsetInfo &,
    //                                                        Boolean isBctf,
    //                                                        const char *&staticName) const = 0;
    public abstract InputCodingSystem? makeInputCodingSystem(StringC name,
                                                             CharsetInfo charset,
                                                             Boolean isBctf,
                                                             out string? staticName);

    // virtual Char replacementChar() const = 0;
    public abstract Char replacementChar();

    // const CharsetInfo &systemCharset() const;
    public CharsetInfo systemCharset()
    {
        return systemCharset_;
    }
}

public abstract class CodingSystemKit : InputCodingSystemKit
{
    // virtual ~CodingSystemKit() = 0;
    // C# GC handles cleanup

    // virtual CodingSystemKit *copy() const = 0;
    public abstract CodingSystemKit copy();

    // virtual const CodingSystem *identityCodingSystem() const = 0;
    public abstract CodingSystem identityCodingSystem();

    // virtual const CodingSystem *makeCodingSystem(const char *, Boolean isBctf) const = 0;
    public abstract CodingSystem? makeCodingSystem(string name, Boolean isBctf);

    // static CodingSystemKit *make(const char *);
    public static CodingSystemKit? make(string? systemCharsetName)
    {
        return new CodingSystemKitImpl();
    }
}

// Implementation of CodingSystemKit
internal class CodingSystemKitImpl : CodingSystemKit
{
    private IdentityCodingSystem identityCodingSystem_ = new IdentityCodingSystem();

    public CodingSystemKitImpl()
    {
        // Initialize system charset with basic ASCII/Latin-1 mappings
        UnivCharsetDesc desc = new UnivCharsetDesc();
        // Map 0-255 to Unicode (identity mapping for Latin-1)
        desc.addRange(0, 255, 0);
        systemCharset_.set(desc);
    }

    public override CodingSystemKit copy()
    {
        return new CodingSystemKitImpl();
    }

    public override CodingSystem identityCodingSystem()
    {
        return identityCodingSystem_;
    }

    public override InputCodingSystem identityInputCodingSystem()
    {
        return identityCodingSystem_;
    }

    public override CodingSystem? makeCodingSystem(string name, Boolean isBctf)
    {
        // For now, return identity coding system for most encodings
        string upperName = name.ToUpperInvariant();
        if (upperName == "IDENTITY" || upperName == "UTF-8" || upperName == "XML" ||
            upperName.StartsWith("IS8859") || upperName.StartsWith("ISO-8859"))
        {
            return identityCodingSystem_;
        }
        return identityCodingSystem_;
    }

    public override InputCodingSystem? makeInputCodingSystem(StringC name,
                                                              CharsetInfo charset,
                                                              Boolean isBctf,
                                                              out string? staticName)
    {
        staticName = "IDENTITY";
        return identityCodingSystem_;
    }

    public override Char replacementChar()
    {
        return 0xFFFD; // Unicode replacement character
    }
}
