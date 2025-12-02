// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class NumericCharRefOrigin : Origin
{
    private Location start_ = new Location();
    private Index refLength_;
    private Owner<Markup> markup_ = new Owner<Markup>();

    // NumericCharRefOrigin(const Location &start, Index endIndex, Owner<Markup> &markup);
    public NumericCharRefOrigin(Location start, Index refLength, Owner<Markup> markup)
    {
        start_ = start;
        refLength_ = refLength;
        markup_.swap(markup);
    }

    // const Location &parent() const;
    public override Location parent()
    {
        return start_;
    }

    // Index refLength() const;
    public override Index refLength()
    {
        return refLength_;
    }

    // Boolean isNumericCharRef(const Markup *&) const;
    public override Boolean isNumericCharRef(out Markup? markup)
    {
        markup = markup_.pointer();
        return true;
    }
}
