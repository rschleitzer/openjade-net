// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// A pointer that owns the object pointed to.
// T must be of class type.
// This is coded so that T need not yet have been defined.

namespace OpenSP;

public class Owner<T> where T : class
{
    private T? p_;

    // Owner() : p_(0) { }
    public Owner()
    {
        p_ = null;
    }

    // Owner(T *p) : p_(p) { }
    public Owner(T? p)
    {
        p_ = p;
    }

    // void operator=(T *p)
    public void operatorAssign(T? p)
    {
        // In C++: if (p_) del();
        // In C#, GC handles the old value
        p_ = p;
    }

    // operator int() const { return p_ != 0; }
    public Boolean hasValue()
    {
        return p_ != null;
    }

    // T *pointer() const { return p_; }
    public T? pointer()
    {
        return p_;
    }

    // void swap(Owner<T> &x)
    public void swap(Owner<T> x)
    {
        T? tem = p_;
        p_ = x.p_;
        x.p_ = tem;
    }

    // T *extract()
    public T? extract()
    {
        T? tem = p_;
        p_ = null;
        return tem;
    }

    // void clear()
    public void clear()
    {
        // In C#, GC handles deletion
        p_ = null;
    }
}
