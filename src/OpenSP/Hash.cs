// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public static class Hash
{
    // static unsigned long hash(const StringC &);
    public static ulong hash(StringC str)
    {
        Char[]? p = str.data();
        ulong h = 0;
        for (nuint n = str.size(); n > 0; n--)
        {
            h = (h << 5) + h + p![str.size() - n];  // from Chris Torek
        }
        return h;
    }
}
