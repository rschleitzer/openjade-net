// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// ICopyable<T> interface is defined in CopyOwner.cs

public class OwnerTable<T, K, HF, KF> : PointerTable<T, K, HF, KF>
    where T : class?
    where HF : IHashFunction<K>
    where KF : IKeyFunction<T, K>
    where K : notnull
{
    // OwnerTable() { }
    public OwnerTable() : base()
    {
    }

    // ~OwnerTable();
    // In C++, destructor deletes all items
    // In C#, GC handles this but we can implement IDisposable if needed

    // void clear();
    public override void clear()
    {
        // In C++: for (size_t i = 0; i < this->vec_.size(); i++) delete this->vec_[i];
        // In C#, GC handles deletion
        base.clear();
    }

    // void swap(OwnerTable<T, K, HF, KF> &x)
    public void swap(OwnerTable<T, K, HF, KF> x)
    {
        base.swap(x);
    }
}

public class OwnerTableIter<T, K, HF, KF> : PointerTableIter<T, K, HF, KF>
    where T : class?
    where HF : IHashFunction<K>
    where KF : IKeyFunction<T, K>
    where K : notnull
{
    // OwnerTableIter(const OwnerTable<T, K, HF, KF> &table)
    //   : PointerTableIter<T *, K, HF, KF>(table) { }
    public OwnerTableIter(OwnerTable<T, K, HF, KF> table) : base(table)
    {
    }
}

public class CopyOwnerTable<T, K, HF, KF> : OwnerTable<T, K, HF, KF>
    where T : class?, ICopyable<T>
    where HF : IHashFunction<K>
    where KF : IKeyFunction<T, K>
    where K : notnull
{
    // CopyOwnerTable() { }
    public CopyOwnerTable() : base()
    {
    }

    // CopyOwnerTable(const CopyOwnerTable<T, K, HF, KF> &tab) { *this = tab; }
    public CopyOwnerTable(CopyOwnerTable<T, K, HF, KF> tab) : base()
    {
        operatorAssign(tab);
    }

    // void operator=(const CopyOwnerTable<T, K, HF, KF> &tab);
    public void operatorAssign(CopyOwnerTable<T, K, HF, KF> t)
    {
        clear();
        // Copy the underlying pointer table structure
        vec_ = new T?[t.vec_.Length];
        used_ = t.used_;
        usedLimit_ = t.usedLimit_;
        // FIXME This isn't exception safe.
        for (nuint i = 0; i < (nuint)vec_.Length; i++)
        {
            if (t.vec_[i] != null)
                vec_[i] = t.vec_[i]!.copy();
        }
    }
}
