// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class ListIter<T>
{
    private IListIter<ListItem<T>> iter_;

    // ListIter(const List<T> &list) : iter_(list.list_) { }
    public ListIter(List<T> list)
    {
        iter_ = new IListIter<ListItem<T>>(list.list_);
    }

    // const T &cur() const { return iter_.cur()->value; }
    public T cur()
    {
        return iter_.cur()!.value;
    }

    // int done() const { return iter_.done(); }
    public int done()
    {
        return iter_.done();
    }

    // void next() { iter_.next(); }
    public void next()
    {
        iter_.next();
    }
}
