// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class IListIterBase
{
    private Link? p_;

    // IListIterBase(const IListBase &);
    public IListIterBase(IListBase list)
    {
        p_ = list.head_;
    }

    // int done() const;
    public int done()
    {
        return p_ == null ? 1 : 0;
    }

    // Link *cur() const;
    public Link? cur()
    {
        return p_;
    }

    // void next();
    public void next()
    {
        if (p_ != null)
            p_ = p_.next_;
    }
}
