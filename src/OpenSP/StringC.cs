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

    // Single character constructor
    public StringC(Char c) : base()
    {
        operatorPlusAssign(c);
    }

    // StringC substr(size_t start, size_t length) const
    public StringC substr(nuint start, nuint length)
    {
        Char[]? srcData = data();
        if (srcData == null || start >= size())
            return new StringC();
        nuint actualLength = length;
        if (start + length > size())
            actualLength = size() - start;
        Char[] result = new Char[actualLength];
        for (nuint i = 0; i < actualLength; i++)
            result[i] = srcData[start + i];
        return new StringC(result, actualLength);
    }
}
