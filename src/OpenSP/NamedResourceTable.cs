// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Key function for NamedResource objects
public struct NamedResourceKeyFunction : IKeyFunction<Ptr<NamedResource>, StringC>
{
    // static inline const StringC &key(const NamedResource &obj) { return obj.name(); }
    public static StringC key(Ptr<NamedResource> obj)
    {
        return obj.pointer()?.name() ?? new StringC();
    }
}

// Hash function adapter for StringC in NamedResourceTable context
public struct NamedResourceTableHashFunction : IHashFunction<StringC>
{
    public static ulong hash(StringC key)
    {
        return Hash.hash(key);
    }
}

public class NamedResourceTable<T> where T : NamedResource
{
    internal PointerTable<Ptr<NamedResource>, StringC, NamedResourceTableHashFunction, NamedResourceKeyFunction> table_ =
        new PointerTable<Ptr<NamedResource>, StringC, NamedResourceTableHashFunction, NamedResourceKeyFunction>();

    // NamedResourceTable() { }
    public NamedResourceTable()
    {
    }

    // Ptr<T> insert(const Ptr<T> &p, Boolean replace = 0);
    public Ptr<T> insert(Ptr<T> p, Boolean replace = false)
    {
        Ptr<NamedResource> pBase = new Ptr<NamedResource>(p.pointer());
        Ptr<NamedResource>? result = table_.insert(pBase, replace);
        return new Ptr<T>((T?)result?.pointer());
    }

    // Ptr<T> lookup(const StringC &str) const;
    public Ptr<T> lookup(StringC str)
    {
        Ptr<NamedResource>? result = table_.lookup(str);
        return new Ptr<T>((T?)result?.pointer());
    }

    // ConstPtr<T> lookupConst(const StringC &str) const;
    public ConstPtr<T> lookupConst(StringC str)
    {
        Ptr<NamedResource>? result = table_.lookup(str);
        return new ConstPtr<T>((T?)result?.pointer());
    }

    // const T *lookupTemp(const StringC &str) const;
    public T? lookupTemp(StringC str)
    {
        Ptr<NamedResource>? result = table_.lookup(str);
        return (T?)result?.pointer();
    }

    // Ptr<T> remove(const StringC &str);
    public Ptr<T> remove(StringC str)
    {
        Ptr<NamedResource>? result = table_.remove(str);
        return new Ptr<T>((T?)result?.pointer());
    }

    // size_t count() const;
    public nuint count()
    {
        return table_.count();
    }

    // void clear();
    public void clear()
    {
        table_.clear();
    }

    // void swap(NamedResourceTable<T> &to);
    public void swap(NamedResourceTable<T> to)
    {
        table_.swap(to.table_);
    }
}

public class NamedResourceTableIter<T> where T : NamedResource
{
    private PointerTableIter<Ptr<NamedResource>, StringC, NamedResourceTableHashFunction, NamedResourceKeyFunction> iter_;

    // NamedResourceTableIter(const NamedResourceTable<T> &table);
    public NamedResourceTableIter(NamedResourceTable<T> table)
    {
        iter_ = new PointerTableIter<Ptr<NamedResource>, StringC, NamedResourceTableHashFunction, NamedResourceKeyFunction>(table.table_);
    }

    // Ptr<T> next();
    public Ptr<T> next()
    {
        Ptr<NamedResource>? result = iter_.next();
        return new Ptr<T>((T?)result?.pointer());
    }
}

public class ConstNamedResourceTableIter<T> where T : NamedResource
{
    private PointerTableIter<Ptr<NamedResource>, StringC, NamedResourceTableHashFunction, NamedResourceKeyFunction> iter_;

    // ConstNamedResourceTableIter(const NamedResourceTable<T> &table);
    public ConstNamedResourceTableIter(NamedResourceTable<T> table)
    {
        iter_ = new PointerTableIter<Ptr<NamedResource>, StringC, NamedResourceTableHashFunction, NamedResourceKeyFunction>(table.table_);
    }

    // ConstPtr<T> next();
    public ConstPtr<T> next()
    {
        Ptr<NamedResource>? result = iter_.next();
        return new ConstPtr<T>((T?)result?.pointer());
    }

    // const T *nextTemp();
    public T? nextTemp()
    {
        Ptr<NamedResource>? result = iter_.next();
        return (T?)result?.pointer();
    }
}
