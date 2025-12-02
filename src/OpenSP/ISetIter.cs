// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

using System;

namespace OpenSP;

public class ISetIter<T> where T : struct, IComparable<T>
{
    private ISet<T> p_;
    private nuint i_;

    // ISetIter(const ISet<T> &s) : p_(&s), i_(0) { }
    public ISetIter(ISet<T> s)
    {
        p_ = s;
        i_ = 0;
    }

    // min and max are not changed if 0 is returned.
    // int next(T &min, T &max)
    public int next(out T min, out T max)
    {
        if (i_ < p_.r_.size())
        {
            min = p_.r_[i_].min;
            max = p_.r_[i_].max;
            i_++;
            return 1;
        }
        else
        {
            min = default;
            max = default;
            return 0;
        }
    }
}
