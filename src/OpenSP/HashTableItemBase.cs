// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// All hash tables with the same type of key share object code.
// The cost of this is a virtual dtor in HashTableItemBase.

public abstract class HashTableItemBase<K>
{
    public K key;

    // HashTableItemBase(const K &k);
    public HashTableItemBase(K k)
    {
        key = k;
    }

    // virtual ~HashTableItemBase();
    // C# GC handles this

    // virtual HashTableItemBase<K> *copy() const = 0;
    public abstract HashTableItemBase<K> copy();
}

public static class HashTableKeyFunction<K>
{
    // static inline const K &key(const HashTableItemBase<K> &obj)
    public static K key(HashTableItemBase<K> obj)
    {
        return obj.key;
    }
}
