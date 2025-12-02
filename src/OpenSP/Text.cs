// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// TODO: Port from Text.h

namespace OpenSP;

public class Text
{
    public StringC @string() => new StringC();

    public Boolean charLocation(Offset off, out Origin? origin, out Index index)
    {
        origin = null;
        index = 0;
        return false;
    }
}
