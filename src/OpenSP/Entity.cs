// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// TODO: Port from Entity.h

namespace OpenSP;

public class Entity : EntityDecl
{
}

public class InternalEntity : Entity
{
    protected Text text_ = new Text();

    public Text text() => text_;
}
