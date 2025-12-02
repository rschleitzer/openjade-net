// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class CharsetDeclRange
{
    public enum Type
    {
        number,
        @string,
        unused
    }

    private WideChar descMin_;
    private Number count_;
    private WideChar baseMin_;
    private Type type_;
    private StringC str_ = new StringC();

    // CharsetDeclRange();
    public CharsetDeclRange()
    {
    }

    // ~CharsetDeclRange();
    // C# handles cleanup via GC

    // CharsetDeclRange(WideChar, Number, WideChar);
    public CharsetDeclRange(WideChar descMin, Number count, WideChar baseMin)
    {
        descMin_ = descMin;
        count_ = count;
        type_ = Type.number;
        baseMin_ = baseMin;
    }

    // CharsetDeclRange(WideChar, Number);
    public CharsetDeclRange(WideChar descMin, Number count)
    {
        descMin_ = descMin;
        count_ = count;
        type_ = Type.unused;
    }

    // CharsetDeclRange(WideChar, Number, const StringC &);
    public CharsetDeclRange(WideChar descMin, Number count, StringC str)
    {
        descMin_ = descMin;
        count_ = count;
        type_ = Type.@string;
        str_ = new StringC(str);
    }

    // void rangeDeclared(WideChar min, Number count, ISet<WideChar> &declared) const;
    public void rangeDeclared(WideChar min, Number count, ISet<WideChar> declared)
    {
        if (count > 0 && min + count > descMin_ && min < descMin_ + count_)
        {
            WideChar commMin = (descMin_ > min) ? descMin_ : min;
            WideChar commMax = min + ((min + count < descMin_ + count_
                                       ? count
                                       : descMin_ + count_ - min) - 1);
            // ASSERT(commMin <= commMax);
            declared.addRange(commMin, commMax);
        }
    }

    // void usedSet(ISet<Char> &) const;
    public void usedSet(ISet<Char> set)
    {
        if (type_ != Type.unused && count_ > 0 && descMin_ <= Constant.charMax)
        {
            Char max;
            if (Constant.charMax - descMin_ < count_ - 1)
                max = Constant.charMax;
            else
                max = (Char)(descMin_ + (count_ - 1));
            set.addRange((Char)descMin_, max);
        }
    }

    // Boolean getCharInfo(WideChar fromChar, CharsetDeclRange::Type &type, Number &n, StringC &str, Number &count) const;
    public Boolean getCharInfo(WideChar fromChar, out Type type, out Number n, StringC str, out Number count)
    {
        if (fromChar >= descMin_ && fromChar - descMin_ < count_)
        {
            type = type_;
            if (type_ == Type.number)
                n = baseMin_ + (fromChar - descMin_);
            else if (type_ == Type.@string)
                str.operatorAssign(str_);
            else
                n = 0;
            count = count_ - (fromChar - descMin_);
            n = (type_ == Type.number) ? baseMin_ + (fromChar - descMin_) : 0;
            return true;
        }
        else
        {
            type = default;
            n = 0;
            count = 0;
            return false;
        }
    }

    // void stringToChar(const StringC &str, ISet<WideChar> &to) const;
    public void stringToChar(StringC str, ISet<WideChar> to)
    {
        if (type_ == Type.@string && str_.operatorEqual(str) && count_ > 0)
            to.addRange(descMin_, descMin_ + (count_ - 1));
    }

    // void numberToChar(Number n, ISet<WideChar> &to, Number &count) const;
    public void numberToChar(Number n, ISet<WideChar> to, ref Number count)
    {
        if (type_ == Type.number && n >= baseMin_ && n - baseMin_ < count_)
        {
            Number thisCount = count_ - (n - baseMin_);
            if (to.isEmpty() || thisCount < count)
                count = thisCount;
            to.add(descMin_ + (n - baseMin_));
        }
    }
}

public class CharsetDeclSection
{
    private PublicId baseset_ = new PublicId();
    private Vector<CharsetDeclRange> ranges_ = new Vector<CharsetDeclRange>();

    // CharsetDeclSection();
    public CharsetDeclSection()
    {
    }

    // ~CharsetDeclSection() {}
    // C# handles cleanup via GC

    // void setPublicId(const PublicId &);
    public void setPublicId(PublicId id)
    {
        baseset_ = id;
    }

    // void addRange(const CharsetDeclRange &);
    public void addRange(CharsetDeclRange range)
    {
        ranges_.push_back(range);
    }

    // void rangeDeclared(WideChar min, Number count, ISet<WideChar> &declared) const;
    public void rangeDeclared(WideChar min, Number count, ISet<WideChar> declared)
    {
        for (nuint i = 0; i < ranges_.size(); i++)
            ranges_[i].rangeDeclared(min, count, declared);
    }

    // void usedSet(ISet<Char> &) const;
    public void usedSet(ISet<Char> set)
    {
        for (nuint i = 0; i < ranges_.size(); i++)
            ranges_[i].usedSet(set);
    }

    // Boolean getCharInfo(WideChar fromChar, const PublicId *&id, CharsetDeclRange::Type &type, Number &n, StringC &str, Number &count) const;
    public Boolean getCharInfo(WideChar fromChar, out PublicId? id, out CharsetDeclRange.Type type,
                               out Number n, StringC str, out Number count)
    {
        for (nuint i = 0; i < ranges_.size(); i++)
        {
            if (ranges_[i].getCharInfo(fromChar, out type, out n, str, out count))
            {
                id = baseset_;
                return true;
            }
        }
        id = null;
        type = default;
        n = 0;
        count = 0;
        return false;
    }

    // void stringToChar(const StringC &str, ISet<WideChar> &to) const;
    public void stringToChar(StringC str, ISet<WideChar> to)
    {
        for (nuint i = 0; i < ranges_.size(); i++)
            ranges_[i].stringToChar(str, to);
    }

    // void numberToChar(const PublicId *id, Number n, ISet<WideChar> &to, Number &count) const;
    public void numberToChar(PublicId? id, Number n, ISet<WideChar> to, ref Number count)
    {
        PublicId.OwnerType ownerType;
        StringC seq1 = new StringC();
        StringC seq2 = new StringC();
        if (id != null &&
            (id.@string().operatorEqual(baseset_.@string())
             // Assume that 2 ISO character sets are the same if
             // their designating sequences are the same.
             || (id.getOwnerType(out ownerType)
                 && ownerType == PublicId.OwnerType.ISO
                 && baseset_.getOwnerType(out ownerType)
                 && ownerType == PublicId.OwnerType.ISO
                 && id.getDesignatingSequence(seq1)
                 && baseset_.getDesignatingSequence(seq2)
                 && seq1.operatorEqual(seq2))))
        {
            for (nuint i = 0; i < ranges_.size(); i++)
                ranges_[i].numberToChar(n, to, ref count);
        }
    }
}

public class CharsetDecl
{
    private Vector<CharsetDeclSection> sections_ = new Vector<CharsetDeclSection>();
    private ISet<WideChar> declaredSet_ = new ISet<WideChar>();

    // CharsetDecl();
    public CharsetDecl()
    {
    }

    // void addSection(const PublicId &);
    public void addSection(PublicId id)
    {
        sections_.resize(sections_.size() + 1);
        sections_.back().setPublicId(id);
    }

    // void swap(CharsetDecl &);
    public void swap(CharsetDecl to)
    {
        sections_.swap(to.sections_);
        declaredSet_.swap(to.declaredSet_);
    }

    // void clear();
    public void clear()
    {
        sections_.clear();
    }

    // void usedSet(ISet<Char> &) const;
    public void usedSet(ISet<Char> set)
    {
        for (nuint i = 0; i < sections_.size(); i++)
            sections_[i].usedSet(set);
    }

    // void declaredSet(ISet<WideChar> &set) const;
    public void declaredSet(ISet<WideChar> set)
    {
        set.operatorAssign(declaredSet_);
    }

    // Boolean charDeclared(WideChar) const;
    public Boolean charDeclared(WideChar c)
    {
        return declaredSet_.contains(c);
    }

    // void rangeDeclared(WideChar min, Number count, ISet<WideChar> &declared) const;
    public void rangeDeclared(WideChar min, Number count, ISet<WideChar> declared)
    {
        for (nuint i = 0; i < sections_.size(); i++)
            sections_[i].rangeDeclared(min, count, declared);
    }

    // void addRange(WideChar, Number, WideChar);
    public void addRange(WideChar min, Number count, WideChar baseMin)
    {
        if (count > 0)
            declaredSet_.addRange(min, min + (count - 1));
        CharsetDeclRange range = new CharsetDeclRange(min, count, baseMin);
        sections_.back().addRange(range);
    }

    // void addRange(WideChar, Number);
    public void addRange(WideChar min, Number count)
    {
        if (count > 0)
            declaredSet_.addRange(min, min + (count - 1));
        CharsetDeclRange range = new CharsetDeclRange(min, count);
        sections_.back().addRange(range);
    }

    // void addRange(WideChar, Number, const StringC &);
    public void addRange(WideChar min, Number count, StringC str)
    {
        if (count > 0)
            declaredSet_.addRange(min, min + (count - 1));
        CharsetDeclRange range = new CharsetDeclRange(min, count, str);
        sections_.back().addRange(range);
    }

    // Boolean getCharInfo(WideChar fromChar, const PublicId *&id, CharsetDeclRange::Type &type, Number &n, StringC &str) const;
    public Boolean getCharInfo(WideChar fromChar, out PublicId? id, out CharsetDeclRange.Type type,
                               out Number n, StringC str)
    {
        Number tem;
        return getCharInfo(fromChar, out id, out type, out n, str, out tem);
    }

    // Boolean getCharInfo(WideChar fromChar, const PublicId *&id, CharsetDeclRange::Type &type, Number &n, StringC &str, Number &count) const;
    public Boolean getCharInfo(WideChar fromChar, out PublicId? id, out CharsetDeclRange.Type type,
                               out Number n, StringC str, out Number count)
    {
        for (nuint i = 0; i < sections_.size(); i++)
        {
            if (sections_[i].getCharInfo(fromChar, out id, out type, out n, str, out count))
                return true;
        }
        id = null;
        type = default;
        n = 0;
        count = 0;
        return false;
    }

    // void stringToChar(const StringC &str, ISet<WideChar> &to) const;
    public void stringToChar(StringC str, ISet<WideChar> to)
    {
        for (nuint i = 0; i < sections_.size(); i++)
            sections_[i].stringToChar(str, to);
    }

    // void numberToChar(const PublicId *id, Number n, ISet<WideChar> &to, Number &count) const;
    public void numberToChar(PublicId? id, Number n, ISet<WideChar> to, ref Number count)
    {
        for (nuint i = 0; i < sections_.size(); i++)
            sections_[i].numberToChar(id, n, to, ref count);
    }

    // void numberToChar(const PublicId *id, Number n, ISet<WideChar> &to) const;
    public void numberToChar(PublicId? id, Number n, ISet<WideChar> to)
    {
        Number tem = 0;
        numberToChar(id, n, to, ref tem);
    }
}
