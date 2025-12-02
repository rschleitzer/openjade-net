// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// This owns the objects that are put in it.

public class IList<T> : IListBase where T : Link
{
    // IList() { }
    public IList() : base()
    {
    }

    // IList(T *p) : IListBase(p) { }
    public IList(T? p) : base(p)
    {
    }

    // ~IList() { clear(); }
    // C# GC handles this, but we can call clear() for consistency

    // void append(T *p) { IListBase::append(p); }
    public void append(T? p)
    {
        base.append(p);
    }

    // void insert(T *p) { IListBase::insert(p); }
    public void insert(T? p)
    {
        base.insert(p);
    }

    // void remove(T *p) { IListBase::remove(p); }
    public void remove(T? p)
    {
        base.remove(p);
    }

    // void swap(IList<T> &list) { IListBase::swap(list); }
    public void swap(IList<T> list)
    {
        base.swap(list);
    }

    // T *head() const { return (T *)IListBase::head(); }
    public new T? head()
    {
        return (T?)base.head();
    }

    // T *get() { return (T *)IListBase::get(); }
    public new T? get()
    {
        return (T?)base.get();
    }

    // Using base class: clear, empty
}
