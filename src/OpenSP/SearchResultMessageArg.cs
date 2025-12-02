// Copyright (c) 1995 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class SearchResultMessageArg : OtherMessageArg
{
    private Vector<StringC> filename_ = new Vector<StringC>();
    private Vector<uint> errno_ = new Vector<uint>();

    // SearchResultMessageArg();
    public SearchResultMessageArg()
    {
    }

    // Copy constructor
    public SearchResultMessageArg(SearchResultMessageArg other)
    {
        filename_ = new Vector<StringC>();
        for (nuint i = 0; i < other.filename_.size(); i++)
        {
            StringC s = new StringC(other.filename_[i]);
            filename_.push_back(s);
        }
        errno_ = new Vector<uint>();
        for (nuint i = 0; i < other.errno_.size(); i++)
        {
            errno_.push_back(other.errno_[i]);
        }
    }

    // MessageArg *copy() const;
    public override MessageArg copy()
    {
        return new SearchResultMessageArg(this);
    }

    // void add(StringC &, int);
    public void add(StringC str, int n)
    {
        filename_.resize(filename_.size() + 1);
        str.swap(filename_.back());
        errno_.push_back((uint)n);
    }

    // size_t nTried() const;
    public nuint nTried()
    {
        return filename_.size();
    }

    // const StringC &filename(size_t) const;
    public StringC filename(nuint i)
    {
        return filename_[i];
    }

    // int errnum(size_t) const;
    public int errnum(nuint i)
    {
        return (int)errno_[i];
    }
}
