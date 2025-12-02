// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class IListIter<T> : IListIterBase where T : Link
{
    // IListIter(const IList<T> &list) : IListIterBase(list) { }
    public IListIter(IList<T> list) : base(list)
    {
    }

    // T *cur() const { return (T *)IListIterBase::cur(); }
    public new T? cur()
    {
        return (T?)base.cur();
    }

    // Using base class: next, done
}
