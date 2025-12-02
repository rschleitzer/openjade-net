// Copyright (c) 1995 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public struct SdTextItem
{
    public Location loc;
    public nuint index;

    // SdTextItem();
    public SdTextItem()
    {
        loc = new Location();
        index = 0;
    }

    // SdTextItem(const SdTextItem &);
    public SdTextItem(SdTextItem x)
    {
        loc = new Location(x.loc);
        index = x.index;
    }

    // void operator=(const SdTextItem &);
    public void operatorAssign(SdTextItem x)
    {
        loc = new Location(x.loc);
        index = x.index;
    }

    // ~SdTextItem();
    // C# handles cleanup via GC
}

public class SdText
{
    private Boolean lita_;
    private String<SyntaxChar> chars_ = new String<SyntaxChar>();
    private Vector<SdTextItem> items_ = new Vector<SdTextItem>();

    // SdText();
    public SdText()
    {
    }

    // SdText(const Location &loc, Boolean lita);
    public SdText(Location loc, Boolean lita)
    {
        lita_ = lita;
        items_.resize(items_.size() + 1);
        var back = items_.back();
        back.loc = new Location(loc);
        back.index = 0;
        items_[items_.size() - 1] = back;
    }

    // void swap(SdText &);
    public void swap(SdText to)
    {
        items_.swap(to.items_);
        chars_.swap(to.chars_);
        {
            Boolean tem = to.lita_;
            to.lita_ = lita_;
            lita_ = tem;
        }
    }

    // void addChar(SyntaxChar, const Location &);
    public void addChar(SyntaxChar c, Location loc)
    {
        if (items_.size() == 0
            || !ReferenceEquals(loc.origin().pointer(), items_.back().loc.origin().pointer())
            || loc.index() != (items_.back().loc.index()
                               + (chars_.size() - items_.back().index)))
        {
            items_.resize(items_.size() + 1);
            var back = items_.back();
            back.loc = new Location(loc);
            back.index = chars_.size();
            items_[items_.size() - 1] = back;
        }
        chars_.operatorPlusAssign(c);
    }

    // const String<SyntaxChar> &string() const;
    public String<SyntaxChar> @string()
    {
        return chars_;
    }

    // Boolean lita() const;
    public Boolean lita()
    {
        return lita_;
    }

    // Location endDelimLocation() const;
    public Location endDelimLocation()
    {
        Location loc = new Location(items_.back().loc);
        loc.operatorPlusAssign((Index)(chars_.size() - items_.back().index));
        return loc;
    }

    // Expose items_ for SdTextIter
    internal Vector<SdTextItem> items()
    {
        return items_;
    }

    // Expose chars_ for SdTextIter
    internal String<SyntaxChar> chars()
    {
        return chars_;
    }
}

public class SdTextIter
{
    private SdText ptr_;
    private nuint itemIndex_;

    // SdTextIter(const SdText &);
    public SdTextIter(SdText text)
    {
        ptr_ = text;
        itemIndex_ = 0;
    }

    // Boolean next(const SyntaxChar *&, size_t &, Location &);
    public Boolean next(out SyntaxChar[]? ptr, out nuint length, out Location loc)
    {
        Vector<SdTextItem> items = ptr_.items();
        if (itemIndex_ >= items.size())
        {
            ptr = null;
            length = 0;
            loc = new Location();
            return false;
        }
        loc = new Location(items[itemIndex_].loc);
        String<SyntaxChar> chars = ptr_.chars();
        nuint charsIndex = items[itemIndex_].index;

        // Get the data starting at charsIndex
        SyntaxChar[]? data = chars.data();
        if (data != null && charsIndex < chars.size())
        {
            nuint remaining = chars.size() - charsIndex;
            ptr = new SyntaxChar[remaining];
            for (nuint i = 0; i < remaining; i++)
                ptr[i] = data[charsIndex + i];
        }
        else
        {
            ptr = null;
        }

        if (itemIndex_ + 1 < items.size())
            length = items[itemIndex_ + 1].index - charsIndex;
        else
            length = chars.size() - charsIndex;
        itemIndex_++;
        return true;
    }
}
