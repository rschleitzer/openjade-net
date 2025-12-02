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
        // Implementation will be added when specific coding systems are ported
        return null;
    }
}
