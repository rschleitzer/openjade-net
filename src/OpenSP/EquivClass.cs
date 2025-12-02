// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class EquivClass : Link
{
    public ISet<Char> set = new ISet<Char>();
    public uint inSets;

    // EquivClass(unsigned in = 0) : inSets(in) { }
    public EquivClass(uint inSets = 0)
    {
        this.inSets = inSets;
    }
}
