// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class IQueueBase
{
    private Link? last_;

    // IQueueBase() : last_(0) { }
    public IQueueBase()
    {
        last_ = null;
    }

    // ~IQueueBase() { }
    // C# GC handles this

    // Boolean empty() const { return last_ == 0; }
    public Boolean empty()
    {
        return last_ == null;
    }

    // Link *get()
    public Link? get()
    {
        Link? tem = last_!.next_;
        if (tem == last_)
            last_ = null;
        else
            last_!.next_ = tem!.next_;
        return tem;
    }

    // void append(Link *p)
    public void append(Link? p)
    {
        if (last_ != null)
        {
            p!.next_ = last_.next_;
            last_ = last_.next_ = p;
        }
        else
            last_ = p!.next_ = p;
    }

    // void swap(IQueueBase &with)
    public void swap(IQueueBase with)
    {
        Link? tem = last_;
        last_ = with.last_;
        with.last_ = tem;
    }
}

public class IQueue<T> : IQueueBase where T : Link
{
    // IQueue() { }
    public IQueue() : base()
    {
    }

    // ~IQueue() { clear(); }
    // C# GC handles this, but we can implement clear()

    // void clear();
    public void clear()
    {
        while (!empty())
            get();  // In C++: delete get(); In C#, GC handles deletion
    }

    // T *get() { return (T *)IQueueBase::get(); }
    public new T? get()
    {
        return (T?)base.get();
    }

    // void append(T *p) { IQueueBase::append(p); }
    public void append(T? p)
    {
        base.append(p);
    }

    // Boolean empty() const { return IQueueBase::empty(); }
    // Inherited from base

    // void swap(IQueue<T> &to) { IQueueBase::swap(to); }
    public void swap(IQueue<T> to)
    {
        base.swap(to);
    }
}
