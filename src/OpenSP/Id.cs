// Copyright (c) 1994, 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Id : Named
{
    private Location defLocation_ = new Location();
    private Vector<Location> pendingRefs_ = new Vector<Location>();

    public Id(StringC name) : base(name)
    {
    }

    public void define(Location loc)
    {
        defLocation_ = new Location(loc);
        // release memory for pendingRefs_
        Vector<Location> tem = new Vector<Location>();
        pendingRefs_.swap(tem);
    }

    public Boolean defined()
    {
        return !defLocation_.origin().isNull();
    }

    public Location defLocation()
    {
        return defLocation_;
    }

    public Vector<Location> pendingRefs()
    {
        return pendingRefs_;
    }

    public void addPendingRef(Location loc)
    {
        pendingRefs_.push_back(new Location(loc));
    }
}

// IdTableIter is used to iterate over IdTable (which is a NamedTable<Id>)
public class IdTableIter : NamedTableIter<Id>
{
    public IdTableIter(NamedTable<Id> table) : base(table)
    {
    }
}
