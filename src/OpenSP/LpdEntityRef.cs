// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// Information about a reference to an entity that
// used a definition in an LPD.

namespace OpenSP;

public class LpdEntityRef
{
    public ConstPtr<Entity> entity = new ConstPtr<Entity>();
    public PackedBoolean lookedAtDefault;
    public PackedBoolean foundInPass1Dtd;

    public LpdEntityRef()
    {
    }

    public LpdEntityRef(LpdEntityRef other)
    {
        entity = other.entity;
        lookedAtDefault = other.lookedAtDefault;
        foundInPass1Dtd = other.foundInPass1Dtd;
    }

    // static inline const LpdEntityRef &key(const LpdEntityRef &r) { return r; }
    public static LpdEntityRef key(LpdEntityRef r)
    {
        return r;
    }

    // static inline unsigned long hash(const LpdEntityRef &r)
    public static ulong hash(LpdEntityRef r)
    {
        return Hash.hash(r.entity.pointer()!.name());
    }

    public override bool Equals(object? obj)
    {
        if (obj is LpdEntityRef other)
        {
            return entity.pointer() == other.entity.pointer()
                   && foundInPass1Dtd == other.foundInPass1Dtd
                   && lookedAtDefault == other.lookedAtDefault;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return (int)hash(this);
    }

    public static bool operator ==(LpdEntityRef? r1, LpdEntityRef? r2)
    {
        if (r1 is null && r2 is null) return true;
        if (r1 is null || r2 is null) return false;
        return r1.Equals(r2);
    }

    public static bool operator !=(LpdEntityRef? r1, LpdEntityRef? r2)
    {
        return !(r1 == r2);
    }
}
