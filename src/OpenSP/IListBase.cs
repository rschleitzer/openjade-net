// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class IListBase
{
    internal Link? head_;

    // IListBase();
    public IListBase()
    {
        head_ = null;
    }

    // IListBase(Link *);
    public IListBase(Link? head)
    {
        head_ = head;
    }

    // void append(Link *);
    public void append(Link? p)
    {
        if (head_ == null)
        {
            head_ = p;
            return;
        }
        Link? pp = head_;
        while (pp.next_ != null)
            pp = pp.next_;
        pp.next_ = p;
    }

    // void insert(Link *);
    public void insert(Link? p)
    {
        if (p != null)
            p.next_ = head_;
        head_ = p;
    }

    // Link *head() const;
    public Link? head()
    {
        return head_;
    }

    // Boolean empty() const;
    public Boolean empty()
    {
        return head_ == null;
    }

    // Link *get();
    public Link? get()
    {
        Link? tem = head_;
        if (head_ != null)
            head_ = head_.next_;
        return tem;
    }

    // void remove(Link *);
    public void remove(Link? p)
    {
        if (head_ == null)
            return;
        if (head_ == p)
        {
            head_ = p!.next_;
            return;
        }
        for (Link? pp = head_; pp.next_ != null; pp = pp.next_)
        {
            if (pp.next_ == p)
            {
                pp.next_ = p!.next_;
                break;
            }
        }
    }

    // void swap(IListBase &);
    public void swap(IListBase list)
    {
        Link? tem = head_;
        head_ = list.head_;
        list.head_ = tem;
    }

    // void clear();
    public void clear()
    {
        while (!empty())
            get();  // In C++, this also deletes; in C#, GC handles it
    }
}
