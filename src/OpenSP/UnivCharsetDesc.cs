// Copyright (c) 1994, 1997 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class UnivCharsetDesc
{
    public struct Range
    {
        public WideChar descMin;
        // Note that this is a count, as in the SGML declaration,
        // rather than a maximum.
        public ulong count;
        public UnivChar univMin;
    }

    // enum constants for common characters
    public const UnivChar zero = 48;
    public const UnivChar A = 65;
    public const UnivChar a = 97;
    public const UnivChar tab = 9;
    public const UnivChar rs = 10;
    public const UnivChar re = 13;
    public const UnivChar space = 32;
    public const UnivChar exclamation = 33;
    public const UnivChar lessThan = 60;
    public const UnivChar greaterThan = 62;

    // For characters <= charMax.
    private CharMap<Unsigned32> charMap_;
    // For characters > charMax.
    private RangeMap<WideChar, UnivChar> rangeMap_;

    // UnivCharsetDesc();
    public UnivCharsetDesc()
    {
        charMap_ = new CharMap<Unsigned32>((Unsigned32)1 << 31);
        rangeMap_ = new RangeMap<WideChar, UnivChar>();
    }

    // UnivCharsetDesc(const Range *, size_t);
    public UnivCharsetDesc(Range[] p, nuint n)
    {
        charMap_ = new CharMap<Unsigned32>((Unsigned32)1 << 31);
        rangeMap_ = new RangeMap<WideChar, UnivChar>();
        set(p, n);
    }

    // Copy constructor
    public UnivCharsetDesc(UnivCharsetDesc x)
    {
        // Copy the charMap by swapping (CharMap has swap method)
        charMap_ = new CharMap<Unsigned32>((Unsigned32)1 << 31);
        // Use swap-based copy
        CharMap<Unsigned32> temp = new CharMap<Unsigned32>((Unsigned32)1 << 31);
        temp.swap(x.charMap_);
        charMap_.swap(temp);
        temp.swap(x.charMap_); // restore x's charMap

        // Copy the rangeMap
        rangeMap_ = new RangeMap<WideChar, UnivChar>();
        // Copy ranges manually
        RangeMapIter<WideChar, UnivChar> iter = new RangeMapIter<WideChar, UnivChar>(x.rangeMap_);
        WideChar fromMin, fromMax;
        UnivChar toMin;
        while (iter.next(out fromMin, out fromMax, out toMin))
        {
            rangeMap_.addRange(fromMin, fromMax, toMin);
        }
    }

    // void set(const Range *, size_t);
    public void set(Range[] p, nuint n)
    {
        for (nuint i = 0; i < n; i++)
        {
            Range r = p[i];
            Char max;
            if (r.count > Constant.charMax || r.descMin > Constant.charMax - r.count)
                max = Constant.charMax;
            else
                max = r.descMin + (Char)(r.count - 1);
            if (max - r.descMin > Constant.univCharMax
                || r.univMin > Constant.univCharMax - (max - r.descMin))
                max = r.descMin + (Constant.univCharMax - r.univMin);
            addRange(r.descMin, max, r.univMin);
        }
    }

    // Boolean descToUniv(WideChar from, UnivChar &to) const;
    public Boolean descToUniv(WideChar from, out UnivChar to)
    {
        if (from > Constant.charMax)
        {
            WideChar tem;
            return rangeMap_.map(from, out to, out tem);
        }
        else
        {
            Unsigned32 tem = charMap_[(Char)from];
            if (noDesc(tem))
            {
                to = 0;
                return false;
            }
            else
            {
                to = extractChar(tem, (Char)from);
                return true;
            }
        }
    }

    // Boolean descToUniv(WideChar from, UnivChar &to, WideChar &alsoMax) const;
    public Boolean descToUniv(WideChar from, out UnivChar to, out WideChar alsoMax)
    {
        if (from > Constant.charMax)
            return rangeMap_.map(from, out to, out alsoMax);
        else
        {
            Char max;
            Unsigned32 tem = charMap_.getRange((Char)from, out max);
            alsoMax = max;
            if (noDesc(tem))
            {
                to = 0;
                return false;
            }
            else
            {
                to = extractChar(tem, (Char)from);
                return true;
            }
        }
    }

    // Return 0 for no matches, 1 for 1, 2 for more than 1
    // unsigned univToDesc(UnivChar from, WideChar &to, ISet<WideChar> &toSet) const;
    public uint univToDesc(UnivChar from, out WideChar to, ISet<WideChar> toSet)
    {
        WideChar tem;
        return univToDesc(from, out to, toSet, out tem);
    }

    // unsigned univToDesc(UnivChar from, WideChar &to, ISet<WideChar> &toSet, WideChar &count) const;
    public uint univToDesc(UnivChar to, out WideChar from, ISet<WideChar> fromSet, out WideChar count)
    {
        uint ret = rangeMap_.inverseMap(to, out from, fromSet, out count);
        Char min = 0;
        do
        {
            Char max;
            Unsigned32 tem = charMap_.getRange(min, out max);
            if (!noDesc(tem))
            {
                UnivChar toMin = extractChar(tem, min);
                if (toMin <= to && to <= toMin + (max - min))
                {
                    Char n = (Char)(min + (to - toMin));
                    WideChar thisCount = (WideChar)(max - n + 1);
                    if (ret > 1)
                    {
                        fromSet.add(n);
                        if (thisCount < count)
                            count = thisCount;
                        if (n < from)
                            from = n;
                    }
                    else if (ret == 1)
                    {
                        fromSet.add(from);
                        fromSet.add(n);
                        ret = 2;
                        if (thisCount < count)
                            count = thisCount;
                        if (n < from)
                            from = n;
                    }
                    else
                    {
                        count = thisCount;
                        from = n;
                        ret = 1;
                    }
                }
                else if (ret == 0 && toMin > to && toMin - to < count)
                    count = (WideChar)(toMin - to);
            }
            min = max;
        } while (min++ != Constant.charMax);
        return ret;
    }

    // void addRange(WideChar descMin, WideChar descMax, UnivChar univMin);
    public void addRange(WideChar descMin, WideChar descMax, UnivChar univMin)
    {
        if (descMin <= Constant.charMax)
        {
            Char max = descMax > Constant.charMax ? Constant.charMax : (Char)descMax;
            charMap_.setRange((Char)descMin, max, wrapChar(univMin, (Char)descMin));
        }
        if (descMax > Constant.charMax)
        {
            if (descMin > Constant.charMax)
                rangeMap_.addRange(descMin, descMax, univMin);
            else
                rangeMap_.addRange(Constant.charMax, descMax, univMin + (Constant.charMax - descMin));
        }
    }

    // void addBaseRange(const UnivCharsetDesc &baseSet, WideChar descMin, WideChar descMax, WideChar baseMin, ISet<WideChar> &baseMissing);
    public void addBaseRange(UnivCharsetDesc baseSet, WideChar descMin, WideChar descMax, WideChar baseMin, ISet<WideChar> baseMissing)
    {
        UnivCharsetDescIter iter = new UnivCharsetDescIter(baseSet);
        iter.skipTo(baseMin);
        WideChar baseMax = baseMin + (descMax - descMin);
        WideChar iDescMin, iDescMax;
        UnivChar iBaseMin;
        WideChar missingBaseMin = baseMin;
        Boolean usedAll = false;
        while (iter.next(out iDescMin, out iDescMax, out iBaseMin) && iDescMin <= baseMax)
        {
            //  baseMin   baseMax
            //          iDescMin iDescMax
            if (iDescMax >= baseMin)
            {
                WideChar min = baseMin > iDescMin ? baseMin : iDescMin;
                if (min > missingBaseMin)
                    baseMissing.addRange(missingBaseMin, min - 1);
                WideChar max = baseMax < iDescMax ? baseMax : iDescMax;
                missingBaseMin = max + 1;
                if (missingBaseMin == 0)
                    usedAll = true;
                // ASSERT(min <= max);
                addRange(descMin + (min - baseMin),
                         descMin + (max - baseMin),
                         iBaseMin + (min - iDescMin));
            }
        }
        if (!usedAll && baseMax >= missingBaseMin)
            baseMissing.addRange(missingBaseMin, baseMax);
    }

    // private helper methods
    internal static Boolean noDesc(Unsigned32 n)
    {
        return (n & ((Unsigned32)1 << 31)) != 0;
    }

    internal static UnivChar extractChar(Unsigned32 n, Char ch)
    {
        return (UnivChar)((n + ch) & (((Unsigned32)1 << 31) - 1));
    }

    private static Unsigned32 wrapChar(UnivChar univ, Char ch)
    {
        return (Unsigned32)((univ - ch) & (((Unsigned32)1 << 31) - 1));
    }

    // Expose charMap_ for iterator
    internal CharMap<Unsigned32> charMap()
    {
        return charMap_;
    }

    // Expose rangeMap_ for iterator
    internal RangeMap<WideChar, UnivChar> rangeMap()
    {
        return rangeMap_;
    }
}

public class UnivCharsetDescIter
{
    private CharMap<Unsigned32> charMap_;
    private Char nextChar_;
    private Boolean doneCharMap_;
    private RangeMapIter<WideChar, UnivChar> rangeMapIter_;

    // UnivCharsetDescIter(const UnivCharsetDesc &);
    public UnivCharsetDescIter(UnivCharsetDesc desc)
    {
        charMap_ = desc.charMap();
        doneCharMap_ = false;
        nextChar_ = 0;
        rangeMapIter_ = new RangeMapIter<WideChar, UnivChar>(desc.rangeMap());
    }

    // Boolean next(WideChar &descMin, WideChar &descMax, UnivChar &univMin);
    public Boolean next(out WideChar descMin, out WideChar descMax, out UnivChar univMin)
    {
        while (!doneCharMap_)
        {
            Char ch = nextChar_;
            Char outMax;
            Unsigned32 tem = charMap_.getRange(nextChar_, out outMax);
            nextChar_ = outMax;
            descMax = nextChar_;
            if (!UnivCharsetDesc.noDesc(tem))
            {
                descMin = ch;
                descMax = nextChar_;
                univMin = UnivCharsetDesc.extractChar(tem, ch);
                if (nextChar_ == Constant.charMax)
                    doneCharMap_ = true;
                else
                    nextChar_++;
                return true;
            }
            if (nextChar_ == Constant.charMax)
                doneCharMap_ = true;
            else
                nextChar_++;
        }
        return rangeMapIter_.next(out descMin, out descMax, out univMin);
    }

    // void skipTo(WideChar);
    public void skipTo(WideChar ch)
    {
        if (ch > Constant.charMax)
            doneCharMap_ = true;
        else
            nextChar_ = (Char)ch;
    }
}
