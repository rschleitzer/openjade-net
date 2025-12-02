// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// T must have Resource as a public base class
// T may be an incomplete type

namespace OpenSP;

public class Ptr<T> where T : class, IResource
{
    private T? ptr_;

    // Ptr() : ptr_(0) { }
    public Ptr()
    {
        ptr_ = null;
    }

    // Ptr(T *ptr);
    public Ptr(T? ptr)
    {
        ptr_ = ptr;
        if (ptr_ != null)
            ptr_.@ref();
    }

    // Ptr(const Ptr<T> &);
    public Ptr(Ptr<T> p)
    {
        ptr_ = p.ptr_;
        if (ptr_ != null)
            ptr_.@ref();
    }

    // Ptr<T> &operator=(const Ptr<T> &);
    public void operatorAssign(Ptr<T> p)
    {
        if (p.ptr_ != null)
            p.ptr_.@ref();
        if (ptr_ != null)
            ptr_.unref();
        ptr_ = p.ptr_;
    }

    // Ptr<T> &operator=(T *);
    public void operatorAssign(T? p)
    {
        if (p != null)
            p.@ref();
        if (ptr_ != null)
            ptr_.unref();
        ptr_ = p;
    }

    // T *pointer() const { return ptr_; }
    public T? pointer()
    {
        return ptr_;
    }

    // void swap(Ptr<T> &p)
    public void swap(Ptr<T> p)
    {
        T? tem = p.ptr_;
        p.ptr_ = ptr_;
        ptr_ = tem;
    }

    // Boolean isNull() const { return ptr_ == 0; }
    public Boolean isNull()
    {
        return ptr_ == null;
    }

    // void clear();
    public void clear()
    {
        if (ptr_ != null)
        {
            ptr_.unref();
            ptr_ = null;
        }
    }

    // Boolean operator==(const Ptr<T> &p) const
    public Boolean operatorEqual(Ptr<T> p)
    {
        return ReferenceEquals(ptr_, p.ptr_);
    }

    // Boolean operator!=(const Ptr<T> &p) const
    public Boolean operatorNotEqual(Ptr<T> p)
    {
        return !ReferenceEquals(ptr_, p.ptr_);
    }

    // Boolean operator==(const T *p) const
    public Boolean operatorEqual(T? p)
    {
        return ReferenceEquals(ptr_, p);
    }

    // Boolean operator!=(const T *p) const
    public Boolean operatorNotEqual(T? p)
    {
        return !ReferenceEquals(ptr_, p);
    }
}

public class ConstPtr<T> : Ptr<T> where T : class, IResource
{
    // ConstPtr() { }
    public ConstPtr() : base()
    {
    }

    // ConstPtr(T *ptr) : Ptr<T>(ptr) { }
    public ConstPtr(T? ptr) : base(ptr)
    {
    }

    // ConstPtr(const Ptr<T> &p) : Ptr<T>(p) { }
    public ConstPtr(Ptr<T> p) : base(p)
    {
    }

    // ConstPtr(const ConstPtr<T> &p) : Ptr<T>(p) { }
    public ConstPtr(ConstPtr<T> p) : base(p)
    {
    }

    // Note: In C#, the const-correctness is handled differently
    // The pointer() method returns T? (non-const), but in usage
    // code should treat it as readonly through ConstPtr
}
