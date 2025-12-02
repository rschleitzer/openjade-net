// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class ListItem<T> : Link
{
    public T value;

    // ListItem(const T &v) : value(v) { }
    public ListItem(T v)
    {
        value = v;
    }
}

public class List<T>
{
    internal IList<ListItem<T>> list_ = new IList<ListItem<T>>();

    // List() { }
    public List()
    {
    }

    // void append(const T &item) { list_.append(new ListItem<T>(item)); }
    public void append(T item)
    {
        list_.append(new ListItem<T>(item));
    }

    // void insert(const T &item) { list_.insert(new ListItem<T>(item)); }
    public void insert(T item)
    {
        list_.insert(new ListItem<T>(item));
    }

    // const T &head() const { return list_.head()->value; }
    public T head()
    {
        return list_.head()!.value;
    }

    // void remove(const T &);
    public void remove(T value)
    {
        IListIter<ListItem<T>> iter = new IListIter<ListItem<T>>(list_);
        while (iter.done() == 0)
        {
            if (iter.cur()!.value!.Equals(value))
            {
                list_.remove(iter.cur());
                // In C++: delete iter.cur();
                // C# GC handles this
                break;
            }
            iter.next();
        }
    }

    // T get();
    public T get()
    {
        ListItem<T>? p = list_.get();
        T temp = p!.value;
        // In C++: delete p;
        // C# GC handles this
        return temp;
    }

    // int empty() const { return list_.empty(); }
    public int empty()
    {
        return list_.empty() ? 1 : 0;
    }
}
