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

    // String constructor - converts from C# string to StringC
    public StringC(string s) : base()
    {
        if (s != null)
        {
            foreach (char c in s)
                operatorPlusAssign((Char)c);
        }
    }

    // operator== comparison
    public Boolean operatorEquals(StringC other)
    {
        if (other == null) return false;
        if (size() != other.size()) return false;
        Char[]? data1 = data();
        Char[]? data2 = other.data();
        if (data1 == null && data2 == null) return true;
        if (data1 == null || data2 == null) return false;
        for (nuint i = 0; i < size(); i++)
        {
            if (data1[i] != data2[i])
                return false;
        }
        return true;
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
