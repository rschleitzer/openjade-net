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
        // Both empty strings are equal regardless of internal ptr_ state
        if (size() == 0) return true;
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

    // Convert to system string for debugging
    public string toSystemString()
    {
        Char[]? srcData = data();
        if (srcData == null || size() == 0)
            return "";
        var sb = new System.Text.StringBuilder();
        for (nuint i = 0; i < size(); i++)
        {
            Char c = srcData[i];
            if (c < 0x10000)
                sb.Append((char)c);
            else
                sb.Append($"\\u{c:X}");
        }
        return sb.ToString();
    }

    public override string ToString()
    {
        return toSystemString();
    }

    // Override Equals for proper value comparison (needed for Dictionary keys)
    public override Boolean Equals(object? obj)
    {
        if (obj is StringC other)
            return operatorEquals(other);
        if (obj is string str)
        {
            if (size() != (nuint)str.Length)
                return false;
            Char[]? srcData = data();
            if (srcData == null)
                return str.Length == 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (srcData[i] != (Char)str[i])
                    return false;
            }
            return true;
        }
        return false;
    }

    // Override GetHashCode for proper Dictionary key behavior
    public override int GetHashCode()
    {
        Char[]? srcData = data();
        if (srcData == null || size() == 0)
            return 0;
        // Simple hash combining all characters
        int hash = 17;
        for (nuint i = 0; i < size(); i++)
        {
            hash = hash * 31 + (int)srcData[i];
        }
        return hash;
    }

    // Implement == and != operators
    public static Boolean operator ==(StringC? left, StringC? right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null)
            return false;
        return left.operatorEquals(right);
    }

    public static Boolean operator !=(StringC? left, StringC? right)
    {
        return !(left == right);
    }
}
