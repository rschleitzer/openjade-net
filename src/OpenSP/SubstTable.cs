// Copyright (c) 1994 James Clark, 2000 Matthias Clasen
// See the file COPYING for copying permission.

using System;

namespace OpenSP;

public class SubstTable
{
    public struct Pair
    {
        public Char from;
        public Char to;

        public Pair()
        {
            from = 0;
            to = 0;
        }

        public Pair(Char f, Char t)
        {
            from = f;
            to = t;
        }
    }

    private Char[] lo_ = new Char[256];
    private Vector<Pair> map_ = new Vector<Pair>();
    private bool isSorted_;

    // SubstTable();
    public SubstTable()
    {
        isSorted_ = true;
        for (nuint i = 0; i < 256; i++)
            lo_[i] = (Char)i;
    }

    // void addSubst(Char from, Char to);
    public void addSubst(Char from, Char to)
    {
        if (from < 256)
            lo_[from] = to;
        else
        {
            for (nuint i = 0; i < map_.size(); i++)
            {
                if (map_[i].from == from)
                {
                    Pair p = map_[i];
                    p.to = to;
                    map_[i] = p;
                    return;
                }
            }
            if (from != to)
            {
                isSorted_ = isSorted_ && (map_.size() == 0 || map_.back().from < from);
                map_.push_back(new Pair(from, to));
            }
        }
    }

    // void subst(Char &) const;
    public void subst(ref Char c)
    {
        c = this[c];
    }

    // void subst(StringC &str) const;
    public void subst(StringC str)
    {
        for (nuint i = 0; i < str.size(); i++)
        {
            Char c = str[i];
            subst(ref c);
            str[i] = c;
        }
    }

    // Char operator[](Char from) const;
    public Char this[Char t]
    {
        get
        {
            if (t < 256)
                return lo_[t];
            else
                return at(t);
        }
    }

    // void sort() const;
    public void sort()
    {
        if (map_.size() == 0)
            return;
        // Simple insertion sort (qsort replacement)
        for (nuint i = 1; i < map_.size(); i++)
        {
            Pair key = map_[i];
            nuint j = i;
            while (j > 0 && map_[j - 1].from > key.from)
            {
                map_[j] = map_[j - 1];
                j--;
            }
            map_[j] = key;
        }
    }

    // Char at(Char from) const;
    public Char at(Char t)
    {
        if (!isSorted_)
        {
            sort();
            isSorted_ = true;
        }
        if (map_.size() == 0)
            return t;
        nuint min = 0;
        nuint max = map_.size() - 1;
        if (t < map_[min].from || t > map_[max].from)
            return t;
        if (t == map_[min].from)
            return map_[min].to;
        if (t == map_[max].from)
            return map_[max].to;
        for (;;)
        {
            nuint mid = (min + max) / 2;
            if (mid == min || mid == max)
                return t;
            if (t == map_[mid].from)
                return map_[mid].to;
            if (t < map_[mid].from)
                max = mid;
            else
                min = mid;
        }
    }

    // StringC inverse(Char to) const;
    public StringC inverse(Char c)
    {
        StringC res = new StringC();
        bool cSeen = (c < 256);
        for (nuint i = 0; i < 256; i++)
        {
            if (lo_[i] == c)
                res.operatorPlusAssign((Char)i);
        }
        for (nuint i = 0; i < map_.size(); i++)
        {
            cSeen = cSeen || (map_[i].from == c);
            if (map_[i].to == c)
                res.operatorPlusAssign(map_[i].from);
        }
        if (!cSeen)
            res.operatorPlusAssign(c);
        return res;
    }

    // void inverseTable(SubstTable &) const;
    public void inverseTable(SubstTable inverse)
    {
        for (nuint i = 0; i < 256; i++)
            inverse.lo_[i] = (Char)i;
        inverse.map_.resize(0);
        inverse.isSorted_ = true;
        for (nuint i = 0; i < 256; i++)
            inverse.addSubst(lo_[i], (Char)i);
        for (nuint i = 0; i < map_.size(); i++)
            inverse.addSubst(map_[i].to, map_[i].from);
    }
}
