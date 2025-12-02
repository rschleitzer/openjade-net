// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

using System;

namespace OpenSP;

public struct RangeMapRange<From, To>
    where From : struct, IComparable<From>
    where To : struct
{
    public From fromMin;
    public From fromMax;
    public To toMin;
}

public class RangeMap<From, To>
    where From : struct, IComparable<From>
    where To : struct, IComparable<To>
{
    internal Vector<RangeMapRange<From, To>> ranges_ = new Vector<RangeMapRange<From, To>>();

    // RangeMap();
    public RangeMap()
    {
    }

    // Boolean map(From, To &, From &alsoMax) const;
    public Boolean map(From from, out To to, out From alsoMax)
    {
        // FIXME use binary search
        for (nuint i = 0; i < ranges_.size(); i++)
        {
            RangeMapRange<From, To> r = ranges_[i];
            if (r.fromMin.CompareTo(from) <= 0 && from.CompareTo(r.fromMax) <= 0)
            {
                to = Add(r.toMin, Subtract(from, r.fromMin));
                alsoMax = r.fromMax;
                return true;
            }
            if (r.fromMin.CompareTo(from) > 0)
            {
                alsoMax = SubtractOne(r.fromMin);
                to = default;
                return false;
            }
        }
        alsoMax = MaxValue<From>();
        to = default;
        return false;
    }

    // Return 0 for no matches, 1 for 1, 2 for more than 1.
    // unsigned inverseMap(To, From &, ISet<WideChar> &, WideChar &count) const;
    public uint inverseMap(To to, out From from, ISet<WideChar> fromSet, out WideChar count)
    {
        // FIXME use binary search
        uint ret = 0;
        count = Constant.wideCharMax;
        from = default;
        for (nuint i = 0; i < ranges_.size(); i++)
        {
            RangeMapRange<From, To> r = ranges_[i];
            To rangeEnd = Add(r.toMin, Subtract(r.fromMax, r.fromMin));
            if (r.toMin.CompareTo(to) <= 0 && to.CompareTo(rangeEnd) <= 0)
            {
                From n = Add(r.fromMin, SubtractTo(to, r.toMin));
                WideChar thisCount = (WideChar)(SubtractToWide(r.fromMax, n) + 1);
                if (ret > 1)
                {
                    fromSet.add(ToWideChar(n));
                    if (thisCount < count)
                        count = thisCount;
                }
                else if (ret == 1)
                {
                    fromSet.add(ToWideChar(from));
                    fromSet.add(ToWideChar(n));
                    ret = 2;
                    if (thisCount < count)
                        count = thisCount;
                }
                else
                {
                    count = thisCount;
                    from = n;
                    ret = 1;
                }
            }
            else if (ret == 0 && r.toMin.CompareTo(to) > 0)
            {
                WideChar diff = SubtractToWide(r.toMin, to);
                if (diff < count)
                    count = diff;
            }
        }
        return ret;
    }

    // If the new range overlaps an existing one, the new
    // one takes precedence.
    // void addRange(From, From, To);
    public void addRange(From fromMin, From fromMax, To toMin)
    {
        // FIXME use binary search
        nuint i;
        for (i = ranges_.size(); i > 0; i--)
        {
            if (fromMin.CompareTo(ranges_[(int)(i - 1)].fromMax) > 0)
                break;
        }
        // fromMin <= ranges[i].fromMax
        Boolean coalesced = false;
        if (i > 0)
        {
            From prevMax = ranges_[(int)(i - 1)].fromMax;
            From prevMin = ranges_[(int)(i - 1)].fromMin;
            To prevToMin = ranges_[(int)(i - 1)].toMin;
            if (AddOne(prevMax).CompareTo(fromMin) == 0)
            {
                To expectedTo = Add(prevToMin, Subtract(fromMin, prevMin));
                if (expectedTo.CompareTo(toMin) == 0)
                {
                    // coalesce with previous
                    RangeMapRange<From, To> r = ranges_[(int)(i - 1)];
                    r.fromMax = fromMax;
                    ranges_[(int)(i - 1)] = r;
                    i--;
                    coalesced = true;
                }
            }
        }
        if (!coalesced && i < ranges_.size())
        {
            From curMin = ranges_[i].fromMin;
            if (fromMax.CompareTo(SubtractOne(curMin)) >= 0)
            {
                // overlap
                if (fromMin.CompareTo(curMin) <= 0)
                {
                    To expectedTo = Add(toMin, Subtract(curMin, fromMin));
                    if (expectedTo.CompareTo(ranges_[i].toMin) == 0)
                    {
                        RangeMapRange<From, To> r = ranges_[i];
                        r.fromMin = fromMin;
                        if (fromMax.CompareTo(r.fromMax) <= 0)
                        {
                            ranges_[i] = r;
                            return;
                        }
                        r.fromMax = fromMax;
                        ranges_[i] = r;
                        coalesced = true;
                    }
                }
                else
                {
                    // fromMin > ranges_[i].fromMin
                    To expectedTo = Add(ranges_[i].toMin, Subtract(fromMin, ranges_[i].fromMin));
                    if (expectedTo.CompareTo(toMin) == 0)
                    {
                        if (fromMax.CompareTo(ranges_[i].fromMax) < 0)
                            return;
                        RangeMapRange<From, To> r = ranges_[i];
                        r.fromMax = fromMax;
                        ranges_[i] = r;
                        coalesced = true;
                    }
                }
            }
        }
        if (!coalesced)
        {
            // insert
            ranges_.resize(ranges_.size() + 1);
            for (nuint j = ranges_.size() - 1; j > i; j--)
                ranges_[j] = ranges_[j - 1];
            RangeMapRange<From, To> newRange = new RangeMapRange<From, To>();
            newRange.fromMin = fromMin;
            newRange.fromMax = fromMax;
            newRange.toMin = toMin;
            ranges_[i] = newRange;
        }
        // Delete overlapping ranges starting at i + 1.
        nuint j2;
        for (j2 = i + 1; j2 < ranges_.size(); j2++)
        {
            if (fromMax.CompareTo(ranges_[j2].fromMax) < 0)
            {
                if (fromMax.CompareTo(ranges_[j2].fromMin) >= 0)
                {
                    RangeMapRange<From, To> r = ranges_[j2];
                    r.fromMin = AddOne(fromMax);
                    ranges_[j2] = r;
                }
                break;
            }
        }
        if (j2 > i + 1)
        {
            // delete i + 1 ... j2 - 1
            nuint count = ranges_.size() - j2;
            for (nuint k = 0; k < count; k++)
                ranges_[i + 1 + k] = ranges_[j2 + k];
            ranges_.resize(ranges_.size() - (j2 - (i + 1)));
        }
    }

    // Helper methods for arithmetic on generic types
    private static T Add<T>(T value, long amount) where T : struct
    {
        if (typeof(T) == typeof(uint))
            return (T)(object)(uint)((uint)(object)value + amount);
        if (typeof(T) == typeof(int))
            return (T)(object)(int)((int)(object)value + amount);
        if (typeof(T) == typeof(ushort))
            return (T)(object)(ushort)((ushort)(object)value + amount);
        if (typeof(T) == typeof(short))
            return (T)(object)(short)((short)(object)value + amount);
        if (typeof(T) == typeof(ulong))
            return (T)(object)(ulong)((ulong)(object)value + (ulong)amount);
        if (typeof(T) == typeof(long))
            return (T)(object)((long)(object)value + amount);
        throw new NotSupportedException($"Type {typeof(T)} not supported");
    }

    private static From Add(From value, long amount)
    {
        return Add<From>(value, amount);
    }

    private static To Add(To value, long amount)
    {
        return Add<To>(value, amount);
    }

    private static From AddOne(From value)
    {
        return Add<From>(value, 1);
    }

    private static From SubtractOne(From value)
    {
        return Add<From>(value, -1);
    }

    private static long Subtract(From a, From b)
    {
        if (typeof(From) == typeof(uint))
            return (long)(uint)(object)a - (long)(uint)(object)b;
        if (typeof(From) == typeof(int))
            return (long)(int)(object)a - (long)(int)(object)b;
        if (typeof(From) == typeof(ushort))
            return (long)(ushort)(object)a - (long)(ushort)(object)b;
        if (typeof(From) == typeof(short))
            return (long)(short)(object)a - (long)(short)(object)b;
        if (typeof(From) == typeof(ulong))
            return (long)((ulong)(object)a - (ulong)(object)b);
        if (typeof(From) == typeof(long))
            return (long)(object)a - (long)(object)b;
        throw new NotSupportedException($"Type {typeof(From)} not supported");
    }

    private static long SubtractTo(To a, To b)
    {
        if (typeof(To) == typeof(uint))
            return (long)(uint)(object)a - (long)(uint)(object)b;
        if (typeof(To) == typeof(int))
            return (long)(int)(object)a - (long)(int)(object)b;
        if (typeof(To) == typeof(ushort))
            return (long)(ushort)(object)a - (long)(ushort)(object)b;
        if (typeof(To) == typeof(short))
            return (long)(short)(object)a - (long)(short)(object)b;
        if (typeof(To) == typeof(ulong))
            return (long)((ulong)(object)a - (ulong)(object)b);
        if (typeof(To) == typeof(long))
            return (long)(object)a - (long)(object)b;
        throw new NotSupportedException($"Type {typeof(To)} not supported");
    }

    private static WideChar SubtractToWide<T>(T a, T b) where T : struct
    {
        if (typeof(T) == typeof(uint))
            return (WideChar)((uint)(object)a - (uint)(object)b);
        if (typeof(T) == typeof(int))
            return (WideChar)((int)(object)a - (int)(object)b);
        if (typeof(T) == typeof(ushort))
            return (WideChar)((ushort)(object)a - (ushort)(object)b);
        if (typeof(T) == typeof(short))
            return (WideChar)((short)(object)a - (short)(object)b);
        if (typeof(T) == typeof(ulong))
            return (WideChar)((ulong)(object)a - (ulong)(object)b);
        if (typeof(T) == typeof(long))
            return (WideChar)((long)(object)a - (long)(object)b);
        throw new NotSupportedException($"Type {typeof(T)} not supported");
    }

    private static WideChar ToWideChar(From value)
    {
        if (typeof(From) == typeof(uint))
            return (WideChar)(uint)(object)value;
        if (typeof(From) == typeof(int))
            return (WideChar)(int)(object)value;
        if (typeof(From) == typeof(ushort))
            return (WideChar)(ushort)(object)value;
        if (typeof(From) == typeof(short))
            return (WideChar)(short)(object)value;
        if (typeof(From) == typeof(ulong))
            return (WideChar)(ulong)(object)value;
        if (typeof(From) == typeof(long))
            return (WideChar)(long)(object)value;
        throw new NotSupportedException($"Type {typeof(From)} not supported");
    }

    private static T MaxValue<T>() where T : struct
    {
        if (typeof(T) == typeof(uint))
            return (T)(object)uint.MaxValue;
        if (typeof(T) == typeof(int))
            return (T)(object)int.MaxValue;
        if (typeof(T) == typeof(ushort))
            return (T)(object)ushort.MaxValue;
        if (typeof(T) == typeof(short))
            return (T)(object)short.MaxValue;
        if (typeof(T) == typeof(ulong))
            return (T)(object)ulong.MaxValue;
        if (typeof(T) == typeof(long))
            return (T)(object)long.MaxValue;
        throw new NotSupportedException($"Type {typeof(T)} not supported");
    }
}

public class RangeMapIter<From, To>
    where From : struct, IComparable<From>
    where To : struct, IComparable<To>
{
    private nuint count_;
    private nuint ptr_;
    private RangeMap<From, To> map_;

    // RangeMapIter(const RangeMap<From,To> &map);
    public RangeMapIter(RangeMap<From, To> map)
    {
        map_ = map;
        count_ = map.ranges_.size();
        ptr_ = 0;
    }

    // Boolean next(From &fromMin, From &fromMax, To &toMin)
    public Boolean next(out From fromMin, out From fromMax, out To toMin)
    {
        if (count_ == 0)
        {
            fromMin = default;
            fromMax = default;
            toMin = default;
            return false;
        }
        else
        {
            fromMin = map_.ranges_[ptr_].fromMin;
            fromMax = map_.ranges_[ptr_].fromMax;
            toMin = map_.ranges_[ptr_].toMin;
            ptr_++;
            count_--;
            return true;
        }
    }
}
