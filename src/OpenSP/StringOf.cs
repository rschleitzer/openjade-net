// Copyright (c) 1994, 1996 James Clark
// See the file COPYING for copying permission.

// The file is called StringOf to distinguish it from string.h on
// case-insensitive file systems.

// This offers a subset of the interface offered by the standard C++
// basic_string class as defined in the Jan 96 WP.
// Code in SP currently assumes that size_type is size_t.

using System;

namespace OpenSP;

public class String<T> where T : struct
{
    // typedef size_t size_type;
    // typedef T *iterator;
    // typedef const T *const_iterator;

    private T[]? ptr_;
    private nuint length_;
    private nuint alloc_;

    // String();
    public String()
    {
        ptr_ = null;
        length_ = 0;
        alloc_ = 0;
    }

    // ~String() { if (ptr_) delete [] ptr_; }
    // C# GC handles this

    // String(const T *, size_t);
    public String(T[] ptr, nuint length)
    {
        length_ = length;
        alloc_ = length;
        if (length != 0)
        {
            ptr_ = new T[length];
            Array.Copy(ptr, ptr_, (int)length);
        }
        else
        {
            ptr_ = null;
        }
    }

    // String(const String<T> &);
    public String(String<T> s)
    {
        length_ = s.length_;
        alloc_ = s.length_;
        if (length_ != 0)
        {
            ptr_ = new T[length_];
            Array.Copy(s.ptr_!, ptr_, (int)length_);
        }
        else
        {
            ptr_ = null;
        }
    }

    // String<T> &operator=(const String<T> &);
    public void operatorAssign(String<T> s)
    {
        if (!ReferenceEquals(s, this))
        {
            if (s.length_ > alloc_)
            {
                ptr_ = new T[s.length_];
                alloc_ = s.length_;
            }
            Array.Copy(s.ptr_!, ptr_!, (int)s.length_);
            length_ = s.length_;
        }
    }

    // size_t size() const { return length_; }
    public nuint size()
    {
        return length_;
    }

    // String<T> &assign(const String<T> &s);
    public String<T> assign(String<T> s)
    {
        if (s.ptr_ != null)
            return assign(s.ptr_, s.length_);
        else
        {
            length_ = 0;
            return this;
        }
    }

    // String<T> &assign(string s) - for convenience with C# strings
    public String<T> assign(string s)
    {
        clear();
        if (s != null)
        {
            foreach (char c in s)
            {
                // Handle conversion from char to T
                // Must convert via Convert to handle different numeric types
                T value = (T)Convert.ChangeType((uint)c, typeof(T));
                operatorPlusAssign(value);
            }
        }
        return this;
    }

    // String<T> &assign(const T *, size_t);
    public String<T> assign(T[]? p, nuint n)
    {
        if (p == null || n == 0)
        {
            length_ = 0;
            return this;
        }
        if (alloc_ < n)
        {
            ptr_ = new T[n];
            alloc_ = n;
        }
        length_ = n;
        for (nuint i = 0; i < n; i++)
        {
            ptr_![i] = p[i];
        }
        return this;
    }

    // Overload for assigning from array with offset
    public String<T> assign(T[]? p, nuint start, nuint n)
    {
        if (p == null || n == 0)
        {
            length_ = 0;
            return this;
        }
        if (alloc_ < n)
        {
            ptr_ = new T[n];
            alloc_ = n;
        }
        length_ = n;
        for (nuint i = 0; i < n; i++)
        {
            ptr_![i] = p[start + i];
        }
        return this;
    }

    // String<T> &insert(size_t i, const String<T> &s);
    public String<T> insert(nuint i, String<T> s)
    {
        if (length_ + s.length_ > alloc_)
            grow(s.length_);
        for (nuint n = length_ - i; n > 0; n--)
            ptr_![i + n - 1 + s.length_] = ptr_![i + n - 1];
        length_ += s.length_;
        Array.Copy(s.ptr_!, 0, ptr_!, (int)i, (int)s.length_);
        return this;
    }

    // void swap(String<T> &str);
    public void swap(String<T> to)
    {
        {
            T[]? tem = to.ptr_;
            to.ptr_ = ptr_;
            ptr_ = tem;
        }
        {
            nuint tem = to.length_;
            to.length_ = length_;
            length_ = tem;
        }
        {
            nuint tem = to.alloc_;
            to.alloc_ = alloc_;
            alloc_ = tem;
        }
    }

    // T operator[](size_t i) const { return ptr_[i]; }
    // T &operator[](size_t i) { return ptr_[i]; }
    public T this[nuint i]
    {
        get { return ptr_![i]; }
        set { ptr_![i] = value; }
    }

    // For int index convenience
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

    // const T *data() const { return ptr_; }
    public T[]? data()
    {
        return ptr_;
    }

    // String<T> &operator+=(T c)
    public String<T> operatorPlusAssign(T c)
    {
        if (length_ >= alloc_)
            grow(1);
        ptr_![length_++] = c;
        return this;
    }

    // String<T> &operator+=(const String<T> &s)
    public String<T> operatorPlusAssign(String<T> s)
    {
        if (s.ptr_ != null)
            append(s.ptr_, s.length_);
        return this;
    }

    // String<T> &append(const String<T> &s);
    public String<T> append(String<T> s)
    {
        if (s.ptr_ != null)
            return append(s.ptr_, s.length_);
        return this;
    }

    // String<T> &append(const T *, size_t);
    public String<T> append(T[] p, nuint length)
    {
        if (length == 0)
            return this;
        if (length_ + length > alloc_)
            grow(length);
        Array.Copy(p, 0, ptr_!, (int)length_, (int)length);
        length_ += length;
        return this;
    }

    // Overload for appending from array with offset
    public String<T> append(T[] p, nuint start, nuint length)
    {
        if (length_ + length > alloc_)
            grow(length);
        for (nuint i = 0; i < length; i++)
            ptr_![length_ + i] = p[start + i];
        length_ += length;
        return this;
    }

    // String<T> &append(string s) - for convenience with C# strings
    public String<T> append(string s)
    {
        if (s != null)
        {
            foreach (char c in s)
            {
                // Convert char to the element type T (typically uint for StringC)
                if (typeof(T) == typeof(uint))
                    operatorPlusAssign((T)(object)(uint)c);
                else
                    operatorPlusAssign((T)(object)c);
            }
        }
        return this;
    }

    // Boolean operator==(const String<T> &s) const
    public Boolean operatorEqual(String<T> s)
    {
        if (length_ != s.length_)
            return false;
        if (length_ == 0)
            return true;
        if (!ptr_![0].Equals(s.ptr_![0]))
            return false;
        for (nuint i = 1; i < length_; i++)
        {
            if (!ptr_![i].Equals(s.ptr_![i]))
                return false;
        }
        return true;
    }

    // Boolean operator!=(const String<T> &str) const
    public Boolean operatorNotEqual(String<T> str)
    {
        return !operatorEqual(str);
    }

    // Override Object.Equals to use content comparison (required for HashTable lookups)
    public override bool Equals(object? obj)
    {
        if (obj is String<T> other)
            return operatorEqual(other);
        if (obj is string str)
            return EqualsString(str);
        return false;
    }

    // Compare with a C# string (case-insensitive for SGML compatibility)
    public bool EqualsString(string str)
    {
        if (str == null)
            return length_ == 0;
        if (length_ != (nuint)str.Length)
            return false;
        for (nuint i = 0; i < length_; i++)
        {
            // Compare as uppercase for case-insensitivity
            uint c1 = Convert.ToUInt32(ptr_![i]);
            uint c2 = (uint)char.ToUpperInvariant(str[(int)i]);
            if (c1 >= 'a' && c1 <= 'z')
                c1 = c1 - 'a' + 'A';
            if (c1 != c2)
                return false;
        }
        return true;
    }

    // Override GetHashCode (required when overriding Equals)
    public override int GetHashCode()
    {
        if (length_ == 0 || ptr_ == null)
            return 0;

        // Simple hash combining first few characters
        int hash = 17;
        nuint count = length_ < 8 ? length_ : 8;
        for (nuint i = 0; i < count; i++)
        {
            hash = hash * 31 + ptr_![i].GetHashCode();
        }
        return hash;
    }

    // Operator == for value comparison
    public static bool operator ==(String<T>? left, String<T>? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        return left.operatorEqual(right);
    }

    // Operator != for value comparison
    public static bool operator !=(String<T>? left, String<T>? right)
    {
        return !(left == right);
    }

    // void clear();
    public void clear()
    {
        length_ = 0;
    }

    // void resize(size_t n);
    public void resize(nuint n)
    {
        if (alloc_ < n)
        {
            T[] oldPtr = ptr_!;
            ptr_ = new T[n];
            alloc_ = n;
            if (length_ > 0)
            {
                Array.Copy(oldPtr, ptr_, (int)length_);
            }
        }
        length_ = n;
    }

    // void grow(size_t);
    private void grow(nuint n)
    {
        nuint newAlloc = alloc_;
        if (alloc_ < n)
            newAlloc += n + 16;
        else
            newAlloc += alloc_;
        T[] s = new T[newAlloc];
        if (ptr_ != null)
            Array.Copy(ptr_, s, (int)length_);
        ptr_ = s;
        alloc_ = newAlloc;
    }

    // Helper to create String<uint> from C# string (for StringC alias)
    // This is used for catalog keywords which are ASCII
    public static String<uint> FromString(string s)
    {
        String<uint> result = new String<uint>();
        foreach (char c in s)
        {
            result.operatorPlusAssign((uint)c);
        }
        return result;
    }
}
