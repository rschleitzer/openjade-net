// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Link
{
    internal Link? next_;

    // Link();
    public Link()
    {
        next_ = null;
    }

    // Link(Link *);
    public Link(Link? next)
    {
        next_ = next;
    }

    // virtual ~Link();
    // C# GC handles this
}
