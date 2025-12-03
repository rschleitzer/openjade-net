// Copyright (c) 1995 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// MarkupItem - stores a single item in markup
// C++ uses a union for nChars/origin/text/sdText, but C# uses nullable references
public class MarkupItem
{
    public byte type;
    public byte index;
    // Union-like behavior: only one of these is valid depending on type
    public nuint nChars;
    public ConstPtr<Origin>? origin;   // type == entityStart
    public Text? text;                  // type == literal
    public SdText? sdText;              // type == sdLiteral

    // MarkupItem();
    public MarkupItem()
    {
        type = (byte)Markup.Type.delimiter;
        index = 0;
    }

    // ~MarkupItem();
    // C# GC handles cleanup

    // MarkupItem(const MarkupItem &);
    public MarkupItem(MarkupItem item)
    {
        type = item.type;
        index = item.index;
        switch ((Markup.Type)item.type)
        {
            case Markup.Type.entityStart:
                origin = item.origin != null
                    ? new ConstPtr<Origin>(item.origin.pointer())
                    : null;
                break;
            case Markup.Type.literal:
                text = item.text != null ? new Text(item.text) : null;
                break;
            case Markup.Type.sdLiteral:
                sdText = item.sdText != null ? new SdText(item.sdText) : null;
                break;
            case Markup.Type.delimiter:
                break;
            default:
                nChars = item.nChars;
                break;
        }
    }

    // Copy constructor for SdText
    private SdText copyOfSdText(SdText src)
    {
        SdText result = new SdText();
        src.swap(result);
        src.swap(result);  // swap back to restore src
        return result;
    }

    // void operator=(const MarkupItem &);
    public void operatorAssign(MarkupItem item)
    {
        if ((Markup.Type)type == Markup.Type.entityStart &&
            (Markup.Type)item.type == Markup.Type.entityStart)
        {
            if (item.origin != null)
                origin = new ConstPtr<Origin>(item.origin.pointer());
            return;
        }
        if ((Markup.Type)type == Markup.Type.literal &&
            (Markup.Type)item.type == Markup.Type.literal)
        {
            if (item.text != null)
                text = new Text(item.text);
            return;
        }
        if ((Markup.Type)type == Markup.Type.sdLiteral &&
            (Markup.Type)item.type == Markup.Type.sdLiteral)
        {
            if (item.sdText != null)
                sdText = copyOfSdText(item.sdText);
            return;
        }

        // Clear old union fields
        origin = null;
        text = null;
        sdText = null;

        type = item.type;
        index = item.index;
        switch ((Markup.Type)item.type)
        {
            case Markup.Type.entityStart:
                origin = item.origin != null
                    ? new ConstPtr<Origin>(item.origin.pointer())
                    : null;
                break;
            case Markup.Type.literal:
                text = item.text != null ? new Text(item.text) : null;
                break;
            case Markup.Type.sdLiteral:
                sdText = item.sdText != null ? copyOfSdText(item.sdText) : null;
                break;
            case Markup.Type.delimiter:
                break;
            default:
                nChars = item.nChars;
                break;
        }
    }
}

public class Markup
{
    public enum Type
    {
        reservedName,
        sdReservedName,
        name,
        nameToken,
        attributeValue,
        number,
        comment,
        s,
        shortref,
        delimiter,
        refEndRe,
        entityStart,
        entityEnd,
        literal,
        sdLiteral
    }

    private StringC chars_ = new StringC();
    private Vector<MarkupItem> items_ = new Vector<MarkupItem>();

    // Markup();
    public Markup()
    {
    }

    // Markup(const Markup &);
    public Markup(Markup other)
    {
        chars_.operatorAssign(other.chars_);
        items_.resize(other.items_.size());
        for (nuint i = 0; i < other.items_.size(); i++)
            items_[i] = new MarkupItem(other.items_[i]);
    }

    // ~Markup();
    // C# GC handles cleanup

    // size_t size() const;
    public nuint size()
    {
        return items_.size();
    }

    // void clear();
    public void clear()
    {
        chars_.resize(0);
        items_.resize(0);
    }

    // void resize(size_t);
    public void resize(nuint n)
    {
        nuint chopChars = 0;
        for (nuint i = n; i < items_.size(); i++)
        {
            switch ((Type)items_[i].type)
            {
                case Type.reservedName:
                case Type.sdReservedName:
                case Type.name:
                case Type.nameToken:
                case Type.number:
                case Type.attributeValue:
                case Type.s:
                case Type.comment:
                case Type.shortref:
                    chopChars += items_[i].nChars;
                    break;
            }
        }
        items_.resize(n);
        chars_.resize(chars_.size() - chopChars);
    }

    // void addDelim(Syntax::DelimGeneral);
    public void addDelim(Syntax.DelimGeneral d)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        item.type = (byte)Type.delimiter;
        item.index = (byte)d;
    }

    // void addReservedName(Syntax::ReservedName, const InputSource *);
    public void addReservedName(Syntax.ReservedName rn, InputSource @in)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        nuint length = @in.currentTokenLength();
        item.nChars = length;
        item.type = (byte)Type.reservedName;
        item.index = (byte)rn;
        Char[]? start = @in.currentTokenStart();
        if (start != null)
            chars_.append(start, length);
    }

    // void addReservedName(Syntax::ReservedName, const StringC &);
    public void addReservedName(Syntax.ReservedName rn, StringC str)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        item.nChars = str.size();
        item.type = (byte)Type.reservedName;
        item.index = (byte)rn;
        Char[]? data = str.data();
        if (data != null)
            chars_.append(data, str.size());
    }

    // void addSdReservedName(Sd::ReservedName, const InputSource *);
    public void addSdReservedName(Sd.ReservedName rn, InputSource @in)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        nuint length = @in.currentTokenLength();
        item.nChars = length;
        item.type = (byte)Type.sdReservedName;
        item.index = (byte)rn;
        Char[]? start = @in.currentTokenStart();
        if (start != null)
            chars_.append(start, length);
    }

    // void addSdReservedName(Sd::ReservedName, const Char *, size_t);
    public void addSdReservedName(Sd.ReservedName rn, Char[] str, nuint length)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        item.nChars = length;
        item.type = (byte)Type.sdReservedName;
        item.index = (byte)rn;
        chars_.append(str, length);
    }

    // void addS(Char);
    public void addS(Char c)
    {
        if (items_.size() > 0)
        {
            MarkupItem item = items_.back();
            if ((Type)item.type == Type.s)
            {
                item.nChars += 1;
                chars_.operatorPlusAssign(c);
                return;
            }
        }
        items_.resize(items_.size() + 1);
        MarkupItem newItem = items_.back();
        newItem.type = (byte)Type.s;
        newItem.nChars = 1;
        chars_.operatorPlusAssign(c);
    }

    // void addS(const InputSource *);
    public void addS(InputSource @in)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        nuint length = @in.currentTokenLength();
        item.nChars = length;
        item.type = (byte)Type.s;
        Char[]? start = @in.currentTokenStart();
        if (start != null)
            chars_.append(start, length);
    }

    // void addRefEndRe();
    public void addRefEndRe()
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        item.type = (byte)Type.refEndRe;
    }

    // void addShortref(const InputSource *);
    public void addShortref(InputSource @in)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        nuint length = @in.currentTokenLength();
        item.nChars = length;
        item.type = (byte)Type.shortref;
        Char[]? start = @in.currentTokenStart();
        if (start != null)
            chars_.append(start, length);
    }

    // void addCommentStart();
    public void addCommentStart()
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        item.type = (byte)Type.comment;
        item.nChars = 0;
    }

    // void addCommentChar(Char);
    public void addCommentChar(Char c)
    {
        items_.back().nChars += 1;
        chars_.operatorPlusAssign(c);
    }

    // void addName(const InputSource *);
    public void addName(InputSource @in)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        nuint length = @in.currentTokenLength();
        item.nChars = length;
        item.type = (byte)Type.name;
        Char[]? start = @in.currentTokenStart();
        if (start != null)
            chars_.append(start, length);
    }

    // void addName(const Char *, size_t);
    public void addName(Char[] str, nuint length)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        item.nChars = length;
        item.type = (byte)Type.name;
        chars_.append(str, length);
    }

    // void addNameToken(const InputSource *);
    public void addNameToken(InputSource @in)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        nuint length = @in.currentTokenLength();
        item.nChars = length;
        item.type = (byte)Type.nameToken;
        Char[]? start = @in.currentTokenStart();
        if (start != null)
            chars_.append(start, length);
    }

    // void addNumber(const InputSource *);
    public void addNumber(InputSource @in)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        nuint length = @in.currentTokenLength();
        item.nChars = length;
        item.type = (byte)Type.number;
        Char[]? start = @in.currentTokenStart();
        if (start != null)
            chars_.append(start, length);
    }

    // void addAttributeValue(const InputSource *);
    public void addAttributeValue(InputSource @in)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        nuint length = @in.currentTokenLength();
        item.nChars = length;
        item.type = (byte)Type.attributeValue;
        Char[]? start = @in.currentTokenStart();
        if (start != null)
            chars_.append(start, length);
    }

    // void addEntityStart(const Ptr<EntityOrigin> &);
    public void addEntityStart(Ptr<EntityOrigin> origin)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        item.type = (byte)Type.entityStart;
        item.origin = new ConstPtr<Origin>(origin.pointer());
    }

    // void addEntityEnd();
    public void addEntityEnd()
    {
        items_.resize(items_.size() + 1);
        items_.back().type = (byte)Type.entityEnd;
    }

    // void addLiteral(const Text &);
    public void addLiteral(Text text)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        item.type = (byte)Type.literal;
        item.text = new Text(text);
    }

    // void addSdLiteral(const SdText &);
    public void addSdLiteral(SdText sdText)
    {
        items_.resize(items_.size() + 1);
        MarkupItem item = items_.back();
        item.type = (byte)Type.sdLiteral;
        // Create a copy through swap
        SdText copy = new SdText();
        sdText.swap(copy);
        sdText.swap(copy);  // restore original
        item.sdText = copy;
    }

    // void changeToAttributeValue(size_t index);
    public void changeToAttributeValue(nuint index)
    {
        // ASSERT(items_[index].type == Markup::name);
        items_[index].type = (byte)Type.attributeValue;
    }

    // void changeToSdReservedName(size_t index, Sd::ReservedName);
    public void changeToSdReservedName(nuint index, Sd.ReservedName rn)
    {
        // ASSERT(items_[index].type == Markup::name);
        items_[index].type = (byte)Type.sdReservedName;
        items_[index].index = (byte)rn;
    }

    // void swap(Markup &);
    public void swap(Markup to)
    {
        chars_.swap(to.chars_);
        items_.swap(to.items_);
    }

    // Internal access for MarkupIter
    internal StringC chars()
    {
        return chars_;
    }

    internal Vector<MarkupItem> items()
    {
        return items_;
    }
}

public class MarkupIter
{
    private Char[]? chars_;
    private Vector<MarkupItem> items_;
    private nuint nItems_;
    private nuint index_;
    private nuint charIndex_;

    // MarkupIter(const Markup &);
    public MarkupIter(Markup m)
    {
        chars_ = m.chars().data();
        items_ = m.items();
        nItems_ = m.items().size();
        index_ = 0;
        charIndex_ = 0;
    }

    // Markup::Type type() const;
    public Markup.Type type()
    {
        return (Markup.Type)items_[index_].type;
    }

    // Boolean valid() const;
    public Boolean valid()
    {
        return index_ < nItems_;
    }

    // void advance();
    public void advance()
    {
        switch ((Markup.Type)items_[index_].type)
        {
            case Markup.Type.reservedName:
            case Markup.Type.sdReservedName:
            case Markup.Type.name:
            case Markup.Type.nameToken:
            case Markup.Type.number:
            case Markup.Type.attributeValue:
            case Markup.Type.s:
            case Markup.Type.comment:
            case Markup.Type.shortref:
                charIndex_ += items_[index_].nChars;
                break;
        }
        index_++;
    }

    // void advance(Location &, const ConstPtr<Syntax> &);
    public void advance(ref Location loc, ConstPtr<Syntax> syntax)
    {
        Syntax? syn = syntax.pointer();
        switch ((Markup.Type)items_[index_].type)
        {
            case Markup.Type.delimiter:
                if (syn != null)
                    loc.operatorPlusAssign((Offset)syn.delimGeneral((int)delimGeneral()).size());
                break;
            case Markup.Type.refEndRe:
                loc.operatorPlusAssign(1);
                break;
            case Markup.Type.reservedName:
            case Markup.Type.sdReservedName:
            case Markup.Type.name:
            case Markup.Type.nameToken:
            case Markup.Type.number:
            case Markup.Type.attributeValue:
            case Markup.Type.s:
            case Markup.Type.shortref:
                loc.operatorPlusAssign((Offset)items_[index_].nChars);
                charIndex_ += items_[index_].nChars;
                break;
            case Markup.Type.comment:
                if (syn != null)
                    loc.operatorPlusAssign((Offset)(items_[index_].nChars + (2 * syn.delimGeneral((int)Syntax.DelimGeneral.dCOM).size())));
                charIndex_ += items_[index_].nChars;
                break;
            case Markup.Type.entityStart:
                if (items_[index_].origin != null)
                    loc = new Location(items_[index_].origin!, 0);
                break;
            case Markup.Type.entityEnd:
                {
                    ConstPtr<Origin> origin = new ConstPtr<Origin>(loc.origin().pointer());
                    Origin? o = origin.pointer();
                    if (o != null)
                    {
                        loc = o.parent();
                        loc.operatorPlusAssign((Offset)o.refLength());
                    }
                }
                break;
            case Markup.Type.literal:
                {
                    Text? txt = items_[index_].text;
                    if (txt != null && syn != null)
                    {
                        txt.endDelimLocation(out loc);
                        Boolean lita;
                        txt.delimType(out lita);
                        loc.operatorPlusAssign((Offset)syn.delimGeneral((int)(lita ? Syntax.DelimGeneral.dLITA : Syntax.DelimGeneral.dLIT)).size());
                    }
                }
                break;
            case Markup.Type.sdLiteral:
                {
                    SdText? sd = items_[index_].sdText;
                    if (sd != null)
                    {
                        loc = sd.endDelimLocation();
                        loc.operatorPlusAssign(1);
                    }
                }
                break;
        }
        index_++;
    }

    // size_t index() const;
    public nuint index()
    {
        return index_;
    }

    // const Char *charsPointer() const;
    public Char[]? charsPointer()
    {
        if (chars_ == null)
            return null;
        // Return a slice starting at charIndex_
        nuint length = (nuint)chars_.Length - charIndex_;
        Char[] result = new Char[length];
        for (nuint i = 0; i < length; i++)
            result[i] = chars_[charIndex_ + i];
        return result;
    }

    // Raw pointer access for performance
    public Char charAt(nuint offset)
    {
        if (chars_ != null && charIndex_ + offset < (nuint)chars_.Length)
            return chars_[charIndex_ + offset];
        return 0;
    }

    // size_t charsLength() const;
    public nuint charsLength()
    {
        return items_[index_].nChars;
    }

    // const Text &text() const;
    public Text text()
    {
        return items_[index_].text ?? new Text();
    }

    // const SdText &sdText() const;
    public SdText sdText()
    {
        return items_[index_].sdText ?? new SdText();
    }

    // const EntityOrigin *entityOrigin() const;
    public EntityOrigin? entityOrigin()
    {
        ConstPtr<Origin>? orig = items_[index_].origin;
        if (orig != null)
            return orig.pointer()?.asEntityOrigin();
        return null;
    }

    // Syntax::DelimGeneral delimGeneral() const;
    public Syntax.DelimGeneral delimGeneral()
    {
        return (Syntax.DelimGeneral)items_[index_].index;
    }

    // Syntax::ReservedName reservedName() const;
    public Syntax.ReservedName reservedName()
    {
        return (Syntax.ReservedName)items_[index_].index;
    }

    // Sd::ReservedName sdReservedName() const;
    public Sd.ReservedName sdReservedName()
    {
        return (Sd.ReservedName)items_[index_].index;
    }
}
