// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// typedef String<Char> StringC;
// In C#, we create a derived class to preserve the name
public class StringC : String<Char>
{
    public StringC() : base()
    {
    }

    public StringC(Char[] ptr, nuint length) : base(ptr, length)
    {
    }

    public StringC(StringC s) : base(s)
    {
    }

    public StringC(String<Char> s) : base(s)
    {
    }
}
