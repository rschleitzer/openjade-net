// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Key function for Named objects
public struct NamedTableKeyFunction : IKeyFunction<Named, StringC>
{
    // static inline const StringC &key(const Named &obj) { return obj.name(); }
    public static StringC key(Named obj)
    {
        return obj.name();
    }
}

// Hash function adapter for StringC in NamedTable context
public struct NamedTableHashFunction : IHashFunction<StringC>
{
    public static ulong hash(StringC key)
    {
        return Hash.hash(key);
    }
}

public class NamedTable<T> where T : Named
{
    internal OwnerTable<Named, StringC, NamedTableHashFunction, NamedTableKeyFunction> table_ =
        new OwnerTable<Named, StringC, NamedTableHashFunction, NamedTableKeyFunction>();

    // NamedTable() { }
    public NamedTable()
    {
    }

    // T *insert(T *p) { return (T *)table_.insert(p); }
    public T? insert(T? p)
    {
        return (T?)table_.insert(p);
    }

    // T *lookup(const StringC &str) const { return (T *)table_.lookup(str); }
    public T? lookup(StringC str)
    {
        return (T?)table_.lookup(str);
    }

    // T *remove(const StringC &str) { return (T *)table_.remove(str); }
    public T? remove(StringC str)
    {
        return (T?)table_.remove(str);
    }

    // size_t count() const { return table_.count(); }
    public nuint count()
    {
        return table_.count();
    }

    // void clear() { table_.clear(); }
    public void clear()
    {
        table_.clear();
    }

    // void swap(NamedTable<T> &to) { table_.swap(to.table_); }
    public void swap(NamedTable<T> to)
    {
        table_.swap(to.table_);
    }
}

public class NamedTableIter<T> where T : Named
{
    private OwnerTableIter<Named, StringC, NamedTableHashFunction, NamedTableKeyFunction> iter_;

    // NamedTableIter(const NamedTable<T> &table) : iter_(table.table_) { }
    public NamedTableIter(NamedTable<T> table)
    {
        iter_ = new OwnerTableIter<Named, StringC, NamedTableHashFunction, NamedTableKeyFunction>(table.table_);
    }

    // T *next() { return (T *)iter_.next(); }
    public T? next()
    {
        return (T?)iter_.next();
    }
}

public class ConstNamedTableIter<T> where T : Named
{
    private OwnerTableIter<Named, StringC, NamedTableHashFunction, NamedTableKeyFunction> iter_;

    // ConstNamedTableIter(const NamedTable<T> &table) : iter_(table.table_) { }
    public ConstNamedTableIter(NamedTable<T> table)
    {
        iter_ = new OwnerTableIter<Named, StringC, NamedTableHashFunction, NamedTableKeyFunction>(table.table_);
    }

    // const T *next() { return (T *)iter_.next(); }
    public T? next()
    {
        return (T?)iter_.next();
    }
}
