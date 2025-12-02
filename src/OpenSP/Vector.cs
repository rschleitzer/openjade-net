// Copyright (c) 1994, 1996 James Clark
// See the file COPYING for copying permission.

// This offers a subset of the interface offered by the standard C++
// vector class as defined in the Jan 96 WP.
// Code in SP currently assumes that size_type is size_t.

using System;

namespace OpenSP;

public class Vector<T>
{
    // typedef size_t size_type;
    // typedef T *iterator;
    // typedef const T *const_iterator;

    private nuint size_;
    private T[]? ptr_;
    private nuint alloc_;  // allocated size

    // Vector() : size_(0), ptr_(0), alloc_(0) { }
    public Vector()
    {
        size_ = 0;
        ptr_ = null;
        alloc_ = 0;
    }

    // Vector(size_t n) : ptr_(0), size_(0), alloc_(0) { append(n); }
    public Vector(nuint n)
    {
        ptr_ = null;
        size_ = 0;
        alloc_ = 0;
        append(n);
    }

    // Vector(size_t, const T &);
    public Vector(nuint n, T t)
    {
        ptr_ = null;
        size_ = 0;
        alloc_ = 0;
        insert(size_, n, t);
    }

    // Vector(const Vector<T> &);
    public Vector(Vector<T> v)
    {
        ptr_ = null;
        size_ = 0;
        alloc_ = 0;
        insert(size_, v.ptr_, 0, v.size_);
    }

    // virtual ~Vector();
    // C# GC handles cleanup

    // void resize(size_t n)
    public void resize(nuint n)
    {
        if (n < size_)
            erase(n, size_);
        else if (n > size_)
            append(n - size_);
    }

    // Vector<T> &operator=(const Vector<T> &);
    public void operatorAssign(Vector<T> v)
    {
        if (!ReferenceEquals(v, this))
        {
            nuint n = v.size_;
            if (n > size_)
            {
                n = size_;
                insert(size_, v.ptr_, size_, v.size_);
            }
            else if (n < size_)
                erase(n, size_);
            while (n-- > 0)
                ptr_![n] = v.ptr_![n];
        }
    }

    // void assign(size_t, const T &);
    public void assign(nuint n, T t)
    {
        nuint sz = n;
        if (n > size_)
        {
            sz = size_;
            insert(size_, n - size_, t);
        }
        else if (n < size_)
            erase(n, size_);
        while (sz-- > 0)
            ptr_![sz] = t;
    }

    // void push_back(const T &t)
    public void push_back(T t)
    {
        reserve(size_ + 1);
        ptr_![size_] = t;
        size_++;
    }

    // void insert(const_iterator p, size_t n, const T &t);
    public void insert(nuint i, nuint n, T t)
    {
        reserve(size_ + n);
        if (i != size_)
        {
            // memmove equivalent - copy backwards to handle overlap
            for (nuint j = size_; j > i; j--)
                ptr_![j + n - 1] = ptr_![j - 1];
        }
        for (nuint j = 0; j < n; j++)
        {
            ptr_![i + j] = t;
            size_++;
        }
    }

    // void insert(const_iterator p, const_iterator q1, const_iterator q2);
    public void insert(nuint i, T[]? src, nuint q1, nuint q2)
    {
        nuint n = q2 - q1;
        reserve(size_ + n);
        if (i != size_)
        {
            // memmove equivalent - copy backwards to handle overlap
            for (nuint j = size_; j > i; j--)
                ptr_![j + n - 1] = ptr_![j - 1];
        }
        for (nuint j = 0; q1 != q2; q1++, j++)
        {
            ptr_![i + j] = src![q1];
            size_++;
        }
    }

    // void swap(Vector<T> &);
    public void swap(Vector<T> v)
    {
        {
            T[]? tem = ptr_;
            ptr_ = v.ptr_;
            v.ptr_ = tem;
        }
        {
            nuint tem = size_;
            size_ = v.size_;
            v.size_ = tem;
        }
        {
            nuint tem = alloc_;
            alloc_ = v.alloc_;
            v.alloc_ = tem;
        }
    }

    // void clear() { erase(ptr_, ptr_ + size_); }
    public void clear()
    {
        erase(0, size_);
    }

    // size_t size() const { return size_; }
    public nuint size()
    {
        return size_;
    }

    // T &operator[](size_t i) { return ptr_[i]; }
    // const T &operator[](size_t i) const { return ptr_[i]; }
    public T this[nuint i]
    {
        get { return ptr_![i]; }
        set { ptr_![i] = value; }
    }

    public T this[int i]
    {
        get { return ptr_![i]; }
        set { ptr_![i] = value; }
    }

    // iterator begin() { return ptr_; }
    // const_iterator begin() const { return ptr_; }
    public T[]? begin()
    {
        return ptr_;
    }

    // T &back() { return ptr_[size_ - 1]; }
    // const T &back() const { return ptr_[size_ - 1]; }
    public T back()
    {
        return ptr_![size_ - 1];
    }

    // void reserve(size_t n) {  if (n > alloc_) reserve1(n); }
    public void reserve(nuint n)
    {
        if (n > alloc_)
            reserve1(n);
    }

    // iterator erase(const_iterator, const_iterator);
    public nuint erase(nuint p1, nuint p2)
    {
        // In C++, destructors are called explicitly here
        // In C#, we just clear references for GC
        for (nuint p = p1; p != p2; p++)
            ptr_![p] = default!;
        if (p2 != size_)
        {
            // memmove equivalent
            nuint remaining = size_ - p2;
            for (nuint j = 0; j < remaining; j++)
                ptr_![p1 + j] = ptr_![p2 + j];
        }
        size_ -= p2 - p1;
        return p1;
    }

    // void append(size_t);
    private void append(nuint n)
    {
        reserve(size_ + n);
        while (n-- > 0)
            ptr_![size_++] = default!;
    }

    // void reserve1(size_t);
    private void reserve1(nuint size)
    {
        // Try to preserve a consistent start in the
        // event of an out of memory exception.
        nuint newAlloc = alloc_ * 2;
        if (size > newAlloc)
            newAlloc += size;
        T[] p = new T[newAlloc];
        alloc_ = newAlloc;
        if (ptr_ != null)
        {
            Array.Copy(ptr_, p, (int)size_);
        }
        ptr_ = p;
    }
}
