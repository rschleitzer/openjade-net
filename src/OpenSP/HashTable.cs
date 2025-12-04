// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// HashTableItem stores key-value pairs
// Note: HashTableItemBase implements copy() as abstract, so we implement ICopyable
public class HashTableItem<K, V> : HashTableItemBase<K>
{
    public V value;

    // HashTableItem(const K &k, const V &v);
    public HashTableItem(K k, V v) : base(k)
    {
        value = v;
    }

    // HashTableItemBase<K> *copy() const;
    public override HashTableItemBase<K> copy()
    {
        return new HashTableItem<K, V>(key, value);
    }
}

// Hash function adapter for StringC
public struct StringCHashFunction : IHashFunction<StringC>
{
    public static ulong hash(StringC key)
    {
        return Hash.hash(key);
    }
}

// Key function for HashTableItemBase with StringC keys
public struct StringCHashTableKeyFunction : IKeyFunction<HashTableItemBase<StringC>, StringC>
{
    public static StringC key(HashTableItemBase<StringC> item)
    {
        return item.key;
    }
}

// HashTable specialized for StringC keys (as used throughout OpenSP)
public class HashTable<V>
{
    // Use OwnerTable instead of CopyOwnerTable to avoid ICopyable constraint
    private OwnerTable<HashTableItemBase<StringC>, StringC, StringCHashFunction, StringCHashTableKeyFunction> table_ =
        new OwnerTable<HashTableItemBase<StringC>, StringC, StringCHashFunction, StringCHashTableKeyFunction>();

    // HashTable() { }
    public HashTable()
    {
    }

    // void insert(const K &key, const V &value, Boolean replace = 1);
    public void insert(StringC key, V value, Boolean replace = true)
    {
        HashTableItem<StringC, V> newItem = new HashTableItem<StringC, V>(key, value);
        HashTableItemBase<StringC>? existing = table_.lookup(key);
        if (existing != null)
        {
            // Item already exists
            if (replace)
            {
                existing.key = key;
                ((HashTableItem<StringC, V>)existing).value = value;
            }
            // In C++: delete newItem; (C# GC handles)
        }
        else
        {
            table_.insert(newItem);
        }
    }

    // const V *lookup(const K &key) const
    // Note: Returns default(V) when key is not found - use tryLookup to distinguish found vs not found
    public V? lookup(StringC key)
    {
        HashTableItemBase<StringC>? tem = table_.lookup(key);
        if (tem != null)
            return ((HashTableItem<StringC, V>)tem).value;
        return default;
    }

    // Check if value exists and get it
    public bool tryLookup(StringC key, out V value)
    {
        HashTableItemBase<StringC>? tem = table_.lookup(key);
        if (tem != null)
        {
            value = ((HashTableItem<StringC, V>)tem).value;
            return true;
        }
        value = default!;
        return false;
    }

    // size_t count() const { return table_.count(); }
    public nuint count()
    {
        return table_.count();
    }

    // For iterator access
    internal OwnerTable<HashTableItemBase<StringC>, StringC, StringCHashFunction, StringCHashTableKeyFunction> getTable()
    {
        return table_;
    }
}

public class HashTableIter<V>
{
    private OwnerTableIter<HashTableItemBase<StringC>, StringC, StringCHashFunction, StringCHashTableKeyFunction> iter_;

    // HashTableIter(const HashTable<K, V> &table) : iter_(table.table_) { }
    public HashTableIter(HashTable<V> table)
    {
        iter_ = new OwnerTableIter<HashTableItemBase<StringC>, StringC, StringCHashFunction, StringCHashTableKeyFunction>(table.getTable());
    }

    // Boolean next(const K *&key, const V *&value)
    public Boolean next(out StringC? key, out V? value)
    {
        HashTableItemBase<StringC>? p = iter_.next();
        if (p != null)
        {
            key = p.key;
            value = ((HashTableItem<StringC, V>)p).value;
            return true;
        }
        else
        {
            key = null;
            value = default;
            return false;
        }
    }
}
