// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public static class Priority
{
    public const byte data = 0;
    public const byte dataDelim = 1;
    public const byte function = 2;
    public const byte delim = byte.MaxValue;

    public static byte blank(int n)
    {
        return (byte)(n + function);
    }

    public static Boolean isBlank(byte t)
    {
        return function < t && t < delim;
    }
}
