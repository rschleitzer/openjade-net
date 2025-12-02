// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class TextItem
{
    public enum Type
    {
        data,
        cdata,
        sdata,
        nonSgml,
        entityStart,
        entityEnd,
        startDelim,
        endDelim,
        endDelimA,
        ignore
    }

    public Type type;
    // char that was ignored
    public Char c;
    // location of this item
    public Location loc;
    // index of character in chars_ to which this applies
    public nuint index;

    // TextItem();
    public TextItem()
    {
        loc = new Location();
    }

    // TextItem(const TextItem &);
    public TextItem(TextItem from)
    {
        type = from.type;
        c = from.c;
        loc = new Location(from.loc);
        index = from.index;
    }

    // ~TextItem();
    // C# handles cleanup via GC

    // TextItem &operator=(const TextItem &);
    public void operatorAssign(TextItem from)
    {
        if (!ReferenceEquals(this, from))
        {
            type = from.type;
            c = from.c;
            loc = new Location(from.loc);
            index = from.index;
        }
    }
}

// This is used to represent literals and attribute values.
public class Text
{
    private StringC chars_ = new StringC();
    private Vector<TextItem> items_ = new Vector<TextItem>();

    // Text();
    public Text()
    {
    }

    // ~Text();
    // C# handles cleanup via GC

    // void clear();
    public void clear()
    {
        chars_.resize(0);
        items_.clear();
    }

    // void swap(Text &to);
    public void swap(Text to)
    {
        items_.swap(to.items_);
        chars_.swap(to.chars_);
    }

    // void addChar(Char c, const Location &);
    public void addChar(Char c, Location loc)
    {
        if (items_.size() == 0
            || items_.back().type != TextItem.Type.data
            || !ReferenceEquals(loc.origin().pointer(), items_.back().loc.origin().pointer())
            || loc.index() != (items_.back().loc.index()
                               + (chars_.size() - items_.back().index)))
        {
            items_.resize(items_.size() + 1);
            items_.back().loc = new Location(loc);
            items_.back().type = TextItem.Type.data;
            items_.back().index = chars_.size();
        }
        chars_.operatorPlusAssign(c);
    }

    // void addChars(const StringC &, const Location &);
    public void addChars(StringC s, Location loc)
    {
        addChars(s.data(), s.size(), loc);
    }

    // void addChars(const Char *, size_t, const Location &);
    public void addChars(Char[]? p, nuint length, Location loc)
    {
        if (p == null || length == 0)
            return;
        if (items_.size() == 0
            || items_.back().type != TextItem.Type.data
            || !ReferenceEquals(loc.origin().pointer(), items_.back().loc.origin().pointer())
            || loc.index() != (items_.back().loc.index()
                               + (chars_.size() - items_.back().index)))
        {
            items_.resize(items_.size() + 1);
            items_.back().loc = new Location(loc);
            items_.back().type = TextItem.Type.data;
            items_.back().index = chars_.size();
        }
        chars_.append(p, length);
    }

    // void insertChars(const StringC &, const Location &);
    public void insertChars(StringC s, Location loc)
    {
        chars_.insert(0, s);
        items_.resize(items_.size() + 1);
        for (nuint i = items_.size() - 1; i > 0; i--)
        {
            items_[i].operatorAssign(items_[i - 1]);
            items_[i].index += s.size();
        }
        items_[0].loc = new Location(loc);
        items_[0].type = TextItem.Type.data;
        items_[0].index = 0;
    }

    // void ignoreChar(Char, const Location &);
    public void ignoreChar(Char c, Location loc)
    {
        items_.resize(items_.size() + 1);
        items_.back().loc = new Location(loc);
        items_.back().type = TextItem.Type.ignore;
        items_.back().c = c;
        items_.back().index = chars_.size();
    }

    // void ignoreLastChar();
    public void ignoreLastChar()
    {
        nuint lastIndex = chars_.size() - 1;
        nuint i;
        for (i = items_.size() - 1; items_[i].index > lastIndex; i--)
            ;
        // lastIndex >= items_[i].index
        if (items_[i].index != lastIndex)
        {
            items_.resize(items_.size() + 1);
            i++;
            for (nuint j = items_.size() - 1; j > i; j--)
                items_[j].operatorAssign(items_[j - 1]);
            items_[i].index = lastIndex;
            items_[i].loc = new Location(items_[i - 1].loc);
            items_[i].loc.operatorPlusAssign((Index)(lastIndex - items_[i - 1].index));
        }

        items_[i].c = chars_[chars_.size() - 1];
        items_[i].type = TextItem.Type.ignore;
        for (nuint j = i + 1; j < items_.size(); j++)
            items_[j].index = lastIndex;
        chars_.resize(chars_.size() - 1);
    }

    // void addNonSgmlChar(Char c, const Location &);
    public void addNonSgmlChar(Char c, Location loc)
    {
        addSimple(TextItem.Type.nonSgml, loc);
        chars_.operatorPlusAssign(c);
    }

    // void addEntityStart(const Location &);
    public void addEntityStart(Location loc)
    {
        addSimple(TextItem.Type.entityStart, loc);
    }

    // void addEntityEnd(const Location &);
    public void addEntityEnd(Location loc)
    {
        addSimple(TextItem.Type.entityEnd, loc);
    }

    // void addCdata(const StringC &, const ConstPtr<Origin> &);
    public void addCdata(StringC str, ConstPtr<Origin> origin)
    {
        addSimple(TextItem.Type.cdata, new Location(origin, 0));
        chars_.append(str.data()!, str.size());
    }

    // void addSdata(const StringC &, const ConstPtr<Origin> &);
    public void addSdata(StringC str, ConstPtr<Origin> origin)
    {
        addSimple(TextItem.Type.sdata, new Location(origin, 0));
        chars_.append(str.data()!, str.size());
    }

    // void addStartDelim(const Location &loc);
    public void addStartDelim(Location loc)
    {
        addSimple(TextItem.Type.startDelim, loc);
    }

    // void addEndDelim(const Location &loc, Boolean lita);
    public void addEndDelim(Location loc, Boolean lita)
    {
        addSimple(lita ? TextItem.Type.endDelimA : TextItem.Type.endDelim, loc);
    }

    // void subst(const SubstTable &, Char space);
    public void subst(SubstTable table, Char space)
    {
        for (nuint i = 0; i < items_.size(); i++)
        {
            if (items_[i].type == TextItem.Type.data)
            {
                nuint lim = (i + 1 < items_.size()
                    ? items_[i + 1].index
                    : chars_.size());
                nuint j;
                for (j = items_[i].index; j < lim; j++)
                {
                    Char c = chars_[j];
                    if (c != space && c != table[c])
                        break;
                }
                if (j < lim)
                {
                    nuint start = items_[i].index;
                    Char[] origData = new Char[lim - start];
                    for (nuint k = 0; k < lim - start; k++)
                        origData[k] = chars_[start + k];
                    StringC origChars = new StringC(origData, lim - start);
                    for (; j < lim; j++)
                        if (chars_[j] != space)
                            chars_[j] = table.subst(chars_[j]);
                    items_[i].loc = new Location(new MultiReplacementOrigin(items_[i].loc, origChars), 0);
                }
            }
        }
    }

    // void addCharsTokenize(const Char *, size_t, const Location &loc, Char space);
    public void addCharsTokenize(Char[]? str, nuint n, Location loc, Char space)
    {
        if (str == null)
            return;
        Location loci = new Location(loc);
        for (nuint i = 0; i < n; i++)
        {
            if (str[i] == space && (size() == 0 || lastChar() == space))
                ignoreChar(str[i], loci);
            else
                addChar(str[i], loci);
            loci.operatorPlusAssign(1);
        }
    }

    // void addCharsTokenize(const StringC &, const Location &loc, Char space);
    public void addCharsTokenize(StringC str, Location loc, Char space)
    {
        addCharsTokenize(str.data(), str.size(), loc, space);
    }

    // void tokenize(Char space, Text &text) const;
    public void tokenize(Char space, Text text)
    {
        TextIter iter = new TextIter(this);
        TextItem.Type type;
        Char[]? p;
        nuint n;
        Location? loc;
        while (iter.next(out type, out p, out n, out loc))
        {
            switch (type)
            {
                case TextItem.Type.data:
                    text.addCharsTokenize(p, n, loc!, space);
                    break;
                case TextItem.Type.sdata:
                case TextItem.Type.cdata:
                    {
                        text.addEntityStart(loc!);
                        text.addCharsTokenize(p, n, loc!, space);
                        Location tem = new Location(loc!);
                        tem.operatorPlusAssign((Index)n);
                        text.addEntityEnd(tem);
                    }
                    break;
                case TextItem.Type.ignore:
                    if (p != null && n > 0)
                        text.ignoreChar(p[0], loc!);
                    break;
                default:
                    text.addSimple(type, loc!);
                    break;
            }
        }
        if (text.size() > 0 && text.lastChar() == space)
            text.ignoreLastChar();
    }

    // Location charLocation(size_t i) const;
    public Location charLocation(nuint i)
    {
        ConstPtr<Origin>? originP;
        Index index;
        if (charLocation(i, out originP, out index))
            return new Location(originP!, index);
        else
            return new Location();
    }

    // Boolean charLocation(size_t, const Origin *&, Index &) const;
    public Boolean charLocation(nuint i, out Origin? origin, out Index index)
    {
        ConstPtr<Origin>? originP;
        if (charLocation(i, out originP, out index))
        {
            origin = originP?.pointer();
            return true;
        }
        else
        {
            origin = null;
            return false;
        }
    }

    // Boolean charLocation(size_t i, const ConstPtr<Origin> *&, Index &) const;
    public Boolean charLocation(nuint ind, out ConstPtr<Origin>? origin, out Index index)
    {
        // Find the last item whose index <= ind.
        nuint i = 1;
        nuint lim = items_.size();
        while (i < lim)
        {
            nuint mid = i + (lim - i) / 2;
            if (items_[mid].index > ind)
                lim = mid;
            else
                i = mid + 1;
        }
        i--;
        // If items_.size() == 0, then i == lim.
        if (i < items_.size())
        {
            origin = items_[i].loc.origin();
            index = items_[i].loc.index() + (Index)(ind - items_[i].index);
            return true;
        }
        origin = null;
        index = 0;
        return true;
    }

    // size_t size() const;
    public nuint size()
    {
        return chars_.size();
    }

    // Char lastChar() const;
    public Char lastChar()
    {
        return chars_[chars_.size() - 1];
    }

    // const StringC &string() const;
    public StringC @string()
    {
        return chars_;
    }

    // size_t normalizedLength(size_t normsep) const;
    public nuint normalizedLength(nuint normsep)
    {
        nuint n = size();
        n += normsep;
        for (nuint i = 0; i < items_.size(); i++)
        {
            switch (items_[i].type)
            {
                case TextItem.Type.sdata:
                case TextItem.Type.cdata:
                    n += normsep;
                    break;
                default:
                    break;
            }
        }
        return n;
    }

    // Boolean fixedEqual(const Text &) const;
    public Boolean fixedEqual(Text text)
    {
        if (!@string().operatorEqual(text.@string()))
            return false;
        nuint j = 0;
        for (nuint i = 0; i < items_.size(); i++)
        {
            switch (items_[i].type)
            {
                case TextItem.Type.cdata:
                case TextItem.Type.sdata:
                    for (; ; )
                    {
                        if (j >= text.items_.size())
                            return false;
                        if (text.items_[j].type == TextItem.Type.nonSgml)
                            return false;
                        if (text.items_[j].type == TextItem.Type.cdata
                            || text.items_[j].type == TextItem.Type.sdata)
                            break;
                        j++;
                    }
                    if (text.items_[j].index != items_[i].index
                        || (text.items_[j].loc.origin().pointer()?.entityDecl()
                            != items_[i].loc.origin().pointer()?.entityDecl()))
                        return false;
                    break;
                case TextItem.Type.nonSgml:
                    for (; ; )
                    {
                        if (j >= text.items_.size())
                            return false;
                        if (text.items_[j].type == TextItem.Type.cdata
                            || text.items_[j].type == TextItem.Type.sdata)
                            return false;
                        if (text.items_[j].type == TextItem.Type.nonSgml)
                            break;
                        j++;
                    }
                    if (text.items_[j].index != items_[i].index)
                        return false;
                    break;
                default:
                    break;
            }
        }
        for (; j < text.items_.size(); j++)
        {
            switch (text.items_[j].type)
            {
                case TextItem.Type.cdata:
                case TextItem.Type.sdata:
                case TextItem.Type.nonSgml:
                    return false;
                default:
                    break;
            }
        }
        return true;
    }

    // Boolean startDelimLocation(Location &) const;
    public Boolean startDelimLocation(out Location loc)
    {
        if (items_.size() == 0 || items_[0].type != TextItem.Type.startDelim)
        {
            loc = new Location();
            return false;
        }
        loc = new Location(items_[0].loc);
        return true;
    }

    // Boolean endDelimLocation(Location &) const;
    public Boolean endDelimLocation(out Location loc)
    {
        if (items_.size() == 0)
        {
            loc = new Location();
            return false;
        }
        switch (items_.back().type)
        {
            case TextItem.Type.endDelim:
            case TextItem.Type.endDelimA:
                break;
            default:
                loc = new Location();
                return false;
        }
        loc = new Location(items_.back().loc);
        return true;
    }

    // Boolean delimType(Boolean &lita) const;
    public Boolean delimType(out Boolean lita)
    {
        if (items_.size() == 0)
        {
            lita = false;
            return false;
        }
        switch (items_.back().type)
        {
            case TextItem.Type.endDelim:
                lita = false;
                return true;
            case TextItem.Type.endDelimA:
                lita = true;
                return true;
            default:
                break;
        }
        lita = false;
        return false;
    }

    // private helper
    internal void addSimple(TextItem.Type type, Location loc)
    {
        items_.resize(items_.size() + 1);
        items_.back().loc = new Location(loc);
        items_.back().type = type;
        items_.back().index = chars_.size();
    }

    // Expose items_ for TextIter
    internal Vector<TextItem> items()
    {
        return items_;
    }

    // Expose chars_ for TextIter
    internal StringC chars()
    {
        return chars_;
    }
}

public class TextIter
{
    private nuint ptr_;
    private Text text_;

    // TextIter(const Text &);
    public TextIter(Text text)
    {
        text_ = text;
        ptr_ = 0;
    }

    // void rewind();
    public void rewind()
    {
        ptr_ = 0;
    }

    // Boolean next(TextItem::Type &, const Char *&, size_t &, const Location *&);
    public Boolean next(out TextItem.Type type, out Char[]? str, out nuint length, out Location? loc)
    {
        Vector<TextItem> items = text_.items();
        nuint end = items.size();
        if (ptr_ == end)
        {
            type = default;
            str = null;
            length = 0;
            loc = null;
            return false;
        }
        type = items[ptr_].type;
        loc = items[ptr_].loc;
        if (type == TextItem.Type.ignore)
        {
            str = new Char[] { items[ptr_].c };
            length = 1;
        }
        else
        {
            StringC chars = text_.chars();
            nuint charsIndex = items[ptr_].index;
            if (ptr_ + 1 != end)
                length = items[ptr_ + 1].index - charsIndex;
            else
                length = chars.size() - charsIndex;
            Char[]? data = chars.data();
            if (data != null && charsIndex < chars.size())
            {
                str = new Char[length];
                for (nuint i = 0; i < length; i++)
                    str[i] = data[charsIndex + i];
            }
            else
            {
                str = null;
            }
        }
        ptr_++;
        return true;
    }

    // Boolean valid() const;
    public Boolean valid()
    {
        return ptr_ != text_.items().size();
    }

    // void advance();
    public void advance()
    {
        ptr_++;
    }

    // TextItem::Type type() const;
    public TextItem.Type type()
    {
        return text_.items()[ptr_].type;
    }

    // const Location &location() const;
    public Location location()
    {
        return text_.items()[ptr_].loc;
    }

    // const Char *chars(size_t &length) const;
    public Char[]? chars(out nuint length)
    {
        Vector<TextItem> items = text_.items();
        if (items[ptr_].type == TextItem.Type.ignore)
        {
            length = 1;
            return new Char[] { items[ptr_].c };
        }
        else
        {
            StringC chars = text_.chars();
            nuint charsIndex = items[ptr_].index;
            if (ptr_ + 1 != items.size())
                length = items[ptr_ + 1].index - charsIndex;
            else
                length = chars.size() - charsIndex;
            Char[]? data = chars.data();
            if (data != null && charsIndex < chars.size())
            {
                Char[] result = new Char[length];
                for (nuint i = 0; i < length; i++)
                    result[i] = data[charsIndex + i];
                return result;
            }
            return null;
        }
    }
}
