// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class ErrnoMessageArg : OtherMessageArg
{
    private int errno_;

    // ErrnoMessageArg(int errnum);
    public ErrnoMessageArg(int errnum)
    {
        errno_ = errnum;
    }

    // Copy constructor
    public ErrnoMessageArg(ErrnoMessageArg other)
    {
        errno_ = other.errno_;
    }

    // MessageArg *copy() const;
    public override MessageArg copy()
    {
        return new ErrnoMessageArg(this);
    }

    // int errnum() const;
    public int errnum()
    {
        return errno_;
    }
}
