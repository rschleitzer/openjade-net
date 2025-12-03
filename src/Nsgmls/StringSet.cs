using OpenSP;
// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

using System.Collections.Generic;

namespace Nsgmls;

// StringSetKey - originally used with OwnerTable
public class StringSetKey
{
    public static StringC key(StringC str) { return str; }
}

// Simplified StringSet using HashSet internally for efficiency
// The original C++ version used OwnerTable<StringC, StringC, Hash, StringSetKey>
public class StringSet
{
    // Use HashSet<string> internally for simplicity since StringC comparison needs to work
    private HashSet<string> set_ = new HashSet<string>();

    // StringSet();
    public StringSet()
    {
    }

    // Boolean add(const StringC &); - return 1 if already there
    public Boolean add(StringC str)
    {
        // Convert StringC to string for HashSet storage
        string key = str.ToString();
        if (set_.Contains(key))
            return true;
        set_.Add(key);
        return false;
    }

    // void swap(StringSet &);
    public void swap(StringSet to)
    {
        HashSet<string> temp = set_;
        set_ = to.set_;
        to.set_ = temp;
    }

    // void clear();
    public void clear()
    {
        set_.Clear();
    }

    // Prevent copying
    // StringSet(const StringSet &); - undefined
    // void operator=(const StringSet &); - undefined
}
