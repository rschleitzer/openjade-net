// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public static class Constant
{
    // restrict Char to the UTF-16 range for now (SP_MULTI_BYTE defined)
    public const Char charMax = 0x10ffff;
    public const WideChar wideCharMax = WideChar.MaxValue;
    public const UnivChar univCharMax = UnivChar.MaxValue;
    public const SyntaxChar syntaxCharMax = SyntaxChar.MaxValue;
}
