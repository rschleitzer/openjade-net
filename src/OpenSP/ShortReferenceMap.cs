// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class ShortReferenceMap : Named
{
    private Vector<StringC> nameMap_ = new Vector<StringC>();
    private Vector<ConstPtr<Entity>> entityMap_ = new Vector<ConstPtr<Entity>>();
    private ConstPtr<Entity> nullEntity_ = new ConstPtr<Entity>();
    private Boolean used_;
    private Location defLocation_ = new Location();

    // ShortReferenceMap();
    public ShortReferenceMap()
        : base(new StringC())
    {
        used_ = false;
    }

    // ShortReferenceMap(const StringC &);
    public ShortReferenceMap(StringC name)
        : base(name)
    {
        used_ = false;
    }

    // ~ShortReferenceMap() {}
    // C# GC handles cleanup

    // Boolean defined() const;
    public Boolean defined()
    {
        return nameMap_.size() > 0;
    }

    // void setNameMap(Vector<StringC> &map);
    public void setNameMap(Vector<StringC> map)
    {
        map.swap(nameMap_);
        // Make sure we know it's defined.
        if (nameMap_.size() == 0)
            nameMap_.resize(1);
    }

    // void setEntityMap(Vector<ConstPtr<Entity> > &map);
    public void setEntityMap(Vector<ConstPtr<Entity>> map)
    {
        map.swap(entityMap_);
    }

    // Boolean lookup(int i, const StringC *&) const;
    public Boolean lookup(int i, out StringC? name)
    {
        if ((nuint)i < nameMap_.size() && nameMap_[(nuint)i].size() != 0)
        {
            name = nameMap_[(nuint)i];
            return true;
        }
        name = null;
        return false;
    }

    // const StringC *entityName(size_t i) const;
    public StringC? entityName(nuint i)
    {
        if (i < nameMap_.size() && nameMap_[i].size() != 0)
            return nameMap_[i];
        else
            return null;
    }

    // const ConstPtr<Entity> &entity(size_t i) const;
    public ConstPtr<Entity> entity(nuint i)
    {
        if (i < entityMap_.size())
            return entityMap_[i];
        else
            return nullEntity_;
    }

    // Boolean used() const;
    public Boolean used()
    {
        return used_;
    }

    // void setUsed();
    public void setUsed()
    {
        used_ = true;
    }

    // const Location &defLocation() const;
    public Location defLocation()
    {
        return defLocation_;
    }

    // void setDefLocation(const Location &);
    public void setDefLocation(Location loc)
    {
        defLocation_ = new Location(loc);
    }
}
