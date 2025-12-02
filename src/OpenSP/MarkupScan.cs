// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public struct MarkupScan
{
    public enum Type
    {
        normal,
        @in,
        @out,
        suppress
    }

    // Static constants for convenient access (matches C++ MarkupScan::normal syntax)
    public const byte normal = (byte)Type.normal;
    public const byte @in = (byte)Type.@in;
    public const byte @out = (byte)Type.@out;
    public const byte suppress = (byte)Type.suppress;
}
