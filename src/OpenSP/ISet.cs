// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

using System;

namespace OpenSP;

public struct ISetRange<T> where T : struct, IComparable<T>
{
    public T min;
    public T max;

    public ISetRange()
    {
        min = default;
        max = default;
    }

    public ISetRange(T minVal, T maxVal)
    {
        min = minVal;
        max = maxVal;
    }
}

public class ISet<T> where T : struct, IComparable<T>
{
    internal Vector<ISetRange<T>> r_ = new Vector<ISetRange<T>>();

    // ISet();
    public ISet()
    {
    }

    // ISet(const T *, size_t);
    public ISet(T[] v, nuint n)
    {
        for (nuint i = 0; i < n; i++)
            add(v[i]);
    }

    // ~ISet();
    // C# GC handles this

    // Boolean contains(T) const;
    public Boolean contains(T x)
    {
        for (nuint i = 0; i < r_.size(); i++)
        {
            if (r_[i].max.CompareTo(x) >= 0)
                return r_[i].min.CompareTo(x) <= 0;
        }
        return false;
    }

    // void remove(T);
    public void remove(T c)
    {
        for (nuint i = 0; i < r_.size(); i++)
        {
            if (r_[i].max.CompareTo(c) >= 0)
            {
                if (r_[i].min.CompareTo(c) <= 0)
                {
                    if (r_[i].min.CompareTo(r_[i].max) == 0)
                    {
                        nuint j = i + 1;
                        while (j < r_.size())
                        {
                            r_[j - 1] = r_[j];
                            j++;
                        }
                        r_.resize(r_.size() - 1);
                    }
                    else if (c.CompareTo(r_[i].min) == 0)
                    {
                        ISetRange<T> range = r_[i];
                        range.min = Add(range.min, 1);
                        r_[i] = range;
                    }
                    else if (c.CompareTo(r_[i].max) == 0)
                    {
                        ISetRange<T> range = r_[i];
                        range.max = Subtract(range.max, 1);
                        r_[i] = range;
                    }
                    else
                    {
                        r_.resize(r_.size() + 1);
                        // split the range
                        // subtracting 2 is safe since we know that the length is >= 2
                        for (nuint j = r_.size() - 2; j > i; j--)
                            r_[j + 1] = r_[j];
                        ISetRange<T> newRange = new ISetRange<T>();
                        newRange.max = r_[i].max;
                        newRange.min = Add(c, 1);
                        r_[i + 1] = newRange;
                        ISetRange<T> oldRange = r_[i];
                        oldRange.max = Subtract(c, 1);
                        r_[i] = oldRange;
                    }
                }
                break;
            }
        }
    }

    // void add(T x) { addRange(x, x); }
    public void add(T x)
    {
        addRange(x, x);
    }

    // void addRange(T, T);
    public void addRange(T min, T max)
    {
        nuint i;
        if (IsZero(min))
            i = 0;
        else
        {
            for (i = r_.size(); i > 0 && Subtract(min, 1).CompareTo(r_[i - 1].max) <= 0; i--)
                ;
        }
        // r_[i - 1].max < min - 1 <= r_[i].max
        if (i < r_.size() && (IsZero(r_[i].min) || max.CompareTo(Subtract(r_[i].min, 1)) >= 0))
        {
            // we can coalesce
            ISetRange<T> range = r_[i];
            if (min.CompareTo(range.min) < 0)
                range.min = min;
            if (max.CompareTo(range.max) > 0)
            {
                range.max = max;
                r_[i] = range;
                nuint j;
                for (j = i + 1; j < r_.size() && r_[i].max.CompareTo(Subtract(r_[j].min, 1)) >= 0; j++)
                {
                    ISetRange<T> r = r_[i];
                    r.max = r_[j].max;
                    r_[i] = r;
                }
                // get rid of i + 1 ... j - 1
                if (j > i + 1)
                {
                    for (nuint k = j; k < r_.size(); k++)
                        r_[k - (j - i - 1)] = r_[k];
                    r_.resize(r_.size() - (j - i - 1));
                }
            }
            else
            {
                r_[i] = range;
            }
        }
        else
        {
            // r_[i - 1].max < min - 1
            // max + 1 < r_[i].min
            r_.resize(r_.size() + 1);
            for (nuint j = r_.size() - 1; j > i; j--)
                r_[j] = r_[j - 1];
            ISetRange<T> newRange = new ISetRange<T>();
            newRange.max = max;
            newRange.min = min;
            r_[i] = newRange;
        }
    }

    // void check();
    public void check()
    {
        for (nuint i = 0; i < r_.size(); i++)
        {
            if (r_[i].min.CompareTo(r_[i].max) > 0)
                throw new InvalidOperationException("ISet check failed: min > max");
            // adjacent ranges must be coalesced
            if (i > 0 && Subtract(r_[i].min, 1).CompareTo(r_[i - 1].max) <= 0)
                throw new InvalidOperationException("ISet check failed: adjacent ranges not coalesced");
        }
    }

    // void operator+=(T x) { addRange(x, x); }
    public void operatorPlusAssign(T x)
    {
        addRange(x, x);
    }

    // void clear();
    public void clear()
    {
        r_.resize(0);
    }

    // Boolean isSingleton() const
    public Boolean isSingleton()
    {
        return r_.size() == 1 && r_[0].min.CompareTo(r_[0].max) == 0;
    }

    // Boolean isEmpty() const { return r_.size() == 0; }
    public Boolean isEmpty()
    {
        return r_.size() == 0;
    }

    // void swap(ISet<T> &x) { r_.swap(x.r_); }
    public void swap(ISet<T> x)
    {
        r_.swap(x.r_);
    }

    // Helper methods for arithmetic on generic types
    private static T Add(T value, int amount)
    {
        if (typeof(T) == typeof(uint))
            return (T)(object)((uint)(object)value + (uint)amount);
        if (typeof(T) == typeof(int))
            return (T)(object)((int)(object)value + amount);
        if (typeof(T) == typeof(ushort))
            return (T)(object)(ushort)((ushort)(object)value + amount);
        if (typeof(T) == typeof(short))
            return (T)(object)(short)((short)(object)value + amount);
        if (typeof(T) == typeof(byte))
            return (T)(object)(byte)((byte)(object)value + amount);
        if (typeof(T) == typeof(sbyte))
            return (T)(object)(sbyte)((sbyte)(object)value + amount);
        if (typeof(T) == typeof(ulong))
            return (T)(object)((ulong)(object)value + (ulong)amount);
        if (typeof(T) == typeof(long))
            return (T)(object)((long)(object)value + amount);
        throw new NotSupportedException($"Type {typeof(T)} not supported for arithmetic");
    }

    private static T Subtract(T value, int amount)
    {
        if (typeof(T) == typeof(uint))
            return (T)(object)((uint)(object)value - (uint)amount);
        if (typeof(T) == typeof(int))
            return (T)(object)((int)(object)value - amount);
        if (typeof(T) == typeof(ushort))
            return (T)(object)(ushort)((ushort)(object)value - amount);
        if (typeof(T) == typeof(short))
            return (T)(object)(short)((short)(object)value - amount);
        if (typeof(T) == typeof(byte))
            return (T)(object)(byte)((byte)(object)value - amount);
        if (typeof(T) == typeof(sbyte))
            return (T)(object)(sbyte)((sbyte)(object)value - amount);
        if (typeof(T) == typeof(ulong))
            return (T)(object)((ulong)(object)value - (ulong)amount);
        if (typeof(T) == typeof(long))
            return (T)(object)((long)(object)value - amount);
        throw new NotSupportedException($"Type {typeof(T)} not supported for arithmetic");
    }

    private static bool IsZero(T value)
    {
        return value.CompareTo(default(T)) == 0;
    }
}
