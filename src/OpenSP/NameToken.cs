// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class NameToken
{
    public StringC name = new StringC();
    public StringC origName = new StringC();
    public Location loc = new Location();

    public NameToken()
    {
    }

    public NameToken(NameToken other)
    {
        name = new StringC(other.name);
        origName = new StringC(other.origName);
        loc = new Location(other.loc);
    }
}
