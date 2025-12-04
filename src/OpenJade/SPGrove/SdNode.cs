// Copyright (c) 1997 James Clark
// See the file COPYING for copying permission.

namespace OpenJade.SPGrove;

using OpenSP;
using OpenJade.Grove;

public abstract class SdNode
{
    public abstract AccessResult getSd(
        out ConstPtr<Sd> sd,
        out ConstPtr<Syntax> prologSyntax,
        out ConstPtr<Syntax> instanceSyntax);

    public static readonly string iid = "SdNode";

    public static SdNode? convert(NodePtr nd)
    {
        if (nd && nd.node!.queryInterface(iid, out object? p))
            return (SdNode?)p;
        else
            return null;
    }
}
