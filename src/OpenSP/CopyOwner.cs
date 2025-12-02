// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public interface ICopyable<T>
{
    T copy();
}

public class CopyOwner<T> : Owner<T> where T : class, ICopyable<T>
{
    // CopyOwner() { }
    public CopyOwner() : base()
    {
    }

    // CopyOwner(T *p) : Owner<T>(p) { }
    public CopyOwner(T? p) : base(p)
    {
    }

    // CopyOwner(const CopyOwner<T> &);
    public CopyOwner(CopyOwner<T> o) : base()
    {
        if (o.pointer() != null)
            operatorAssign(o.pointer()!.copy());
    }

    // void operator=(const CopyOwner<T> &o);
    public void operatorAssign(CopyOwner<T> o)
    {
        if (o.pointer() != null)
            operatorAssign(o.pointer()!.copy());
        else
            clear();
    }
}
