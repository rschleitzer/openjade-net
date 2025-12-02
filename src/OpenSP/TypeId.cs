// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class TypeId
{
    // In C++, this is const void *const *bases_
    // It's a null-terminated array of pointers to base class TypeId bases arrays.
    // In C#, we represent this as an array of TypeId objects (each holding its own bases_).
    private TypeId?[]? bases_;

    // TypeId(const void *const *bases) : bases_(bases) { }
    public TypeId(TypeId?[]? bases)
    {
        bases_ = bases;
    }

    // Is this object of type ti?
    // int isA(TypeId ti) const;
    public int isA(TypeId ti)
    {
        if (this == ti)
            return 1;
        if (bases_ == null)
            return 0;
        for (int i = 0; i < bases_.Length && bases_[i] != null; i++)
        {
            if (bases_[i]!.isA(ti) != 0)
                return 1;
        }
        return 0;
    }

    // Can an object with this dynamic type be cast from a static type FROM
    // to a static type TO?
    // int canCast(TypeId to, TypeId from) const;
    public int canCast(TypeId to, TypeId from)
    {
        return (isA(to) != 0 && to.isA(from) != 0) ? 1 : 0;
    }

    // int operator==(TypeId ti) const { return bases_ == ti.bases_; }
    public static bool operator ==(TypeId? a, TypeId? b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null)
            return false;
        return ReferenceEquals(a.bases_, b.bases_);
    }

    // int operator!=(TypeId ti) const { return bases_ != ti.bases_; }
    public static bool operator !=(TypeId? a, TypeId? b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        if (obj is TypeId other)
            return this == other;
        return false;
    }

    public override int GetHashCode()
    {
        return bases_?.GetHashCode() ?? 0;
    }
}
