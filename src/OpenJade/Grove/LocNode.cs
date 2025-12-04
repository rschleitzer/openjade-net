// Copyright (c) 1997 James Clark
// See the file COPYING for copying permission.

namespace OpenJade.Grove;

using OpenSP;

public abstract class LocNode
{
    public abstract AccessResult getLocation(ref Location location);

    public static readonly string iid = "LocNode";

    public static LocNode? convert(NodePtr nd)
    {
        if (nd && nd.node!.queryInterface(iid, out object? p))
            return (LocNode?)p;
        else
            return null;
    }
}
