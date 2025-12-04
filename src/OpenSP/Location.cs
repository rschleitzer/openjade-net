// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Origin : Resource
{
    public virtual EntityOrigin? asEntityOrigin() => null;
    public virtual InputSourceOrigin? asInputSourceOrigin() => null;
    public virtual Location parent() => new Location();
    public virtual Index refLength() => 0;
    public virtual Boolean origChars(out Char[]? chars) { chars = null; return false; }
    public virtual Boolean inBracketedTextOpenDelim() => false;
    public virtual Boolean inBracketedTextCloseDelim() => false;
    public virtual Boolean isNumericCharRef(out Markup? markup) { markup = null; return false; }
    public virtual Boolean isNamedCharRef(Index ind, out NamedCharRef? charRef) { charRef = null; return false; }
    public virtual EntityDecl? entityDecl() => null;
    public virtual Boolean defLocation(Offset off, out Origin? origin, out Index index) { origin = null; index = 0; return false; }
    public virtual Markup? markup() => null;
    public virtual Entity? entity() => null;
    public virtual ExternalInfo? externalInfo() => null;
    public virtual Offset startOffset(Index ind) => ind;

    public StringC? entityName()
    {
        EntityDecl? ent = entityDecl();
        if (ent != null)
            return ent.name();
        return null;
    }
}

public class ProxyOrigin : Origin
{
    private Origin? origin_;

    public ProxyOrigin(Origin? origin)
    {
        origin_ = origin;
    }

    public override EntityOrigin? asEntityOrigin() => origin_?.asEntityOrigin();
    public override InputSourceOrigin? asInputSourceOrigin() => origin_?.asInputSourceOrigin();
    public override Location parent() => origin_?.parent() ?? new Location();
    public override Index refLength() => origin_?.refLength() ?? 0;
    public override Boolean origChars(out Char[]? chars) { if (origin_ != null) return origin_.origChars(out chars); chars = null; return false; }
    public override Boolean inBracketedTextOpenDelim() => origin_?.inBracketedTextOpenDelim() ?? false;
    public override Boolean inBracketedTextCloseDelim() => origin_?.inBracketedTextCloseDelim() ?? false;
    public override Boolean isNumericCharRef(out Markup? markup) { if (origin_ != null) return origin_.isNumericCharRef(out markup); markup = null; return false; }
    public override Boolean isNamedCharRef(Index ind, out NamedCharRef? charRef) { if (origin_ != null) return origin_.isNamedCharRef(ind, out charRef); charRef = null; return false; }
    public override EntityDecl? entityDecl() => origin_?.entityDecl();
    public override Boolean defLocation(Offset off, out Origin? origin, out Index index) { if (origin_ != null) return origin_.defLocation(off, out origin, out index); origin = null; index = 0; return false; }
    public override Markup? markup() => origin_?.markup();
    public override Entity? entity() => origin_?.entity();
    public override ExternalInfo? externalInfo() => origin_?.externalInfo();
    public override Offset startOffset(Index ind) => origin_?.startOffset(ind) ?? ind;
}

public class Location
{
    private ConstPtr<Origin> origin_;
    private Index index_;

    // Location();
    public Location()
    {
        origin_ = new ConstPtr<Origin>();
        index_ = 0;
    }

    // Location(const Location&);
    public Location(Location x)
    {
        origin_ = new ConstPtr<Origin>(x.origin_);
        index_ = x.index_;
    }

    // Location(Origin *, Index);
    public Location(Origin? origin, Index i)
    {
        origin_ = new ConstPtr<Origin>(origin);
        index_ = i;
    }

    // Location(ConstPtr<Origin>, Index);
    public Location(ConstPtr<Origin> origin, Index i)
    {
        origin_ = new ConstPtr<Origin>(origin);
        index_ = i;
    }

    // void operator+=(Index i) { index_ += i; }
    public void operatorPlusAssign(Index i) { index_ += i; }

    // void operator-=(Index i) { index_ -= i; }
    public void operatorMinusAssign(Index i) { index_ -= i; }

    // Index index() const { return index_; }
    public Index index() => index_;

    // const ConstPtr<Origin> &origin() const { return origin_; }
    public ConstPtr<Origin> origin()
    {
        return origin_;
    }

    // void clear() { origin_.clear(); }
    public void clear() { origin_.clear(); }

    // void swap(Location &to)
    public void swap(Location to)
    {
        origin_.swap(to.origin_);
        Index tem = to.index_;
        to.index_ = index_;
        index_ = tem;
    }
}

public class NamedCharRef
{
    public enum RefEndType
    {
        endOmitted,
        endRE,
        endRefc
    }

    private Index refStartIndex_;
    private RefEndType refEndType_;
    private StringC origName_ = new StringC();

    // NamedCharRef();
    public NamedCharRef()
    {
    }

    // NamedCharRef(Index, RefEndType, const StringC &);
    public NamedCharRef(Index refStartIndex, RefEndType refEndType, StringC origName)
    {
        refStartIndex_ = refStartIndex;
        refEndType_ = refEndType;
        origName_ = new StringC(origName);
    }

    public Index refStartIndex() => refStartIndex_;
    public RefEndType refEndType() => refEndType_;
    public StringC origName() => origName_;

    // void set(Index, RefEndType, const Char *, size_t);
    public void set(Index refStartIndex, RefEndType refEndType, Char[] s, nuint n)
    {
        refStartIndex_ = refStartIndex;
        refEndType_ = refEndType;
        origName_.assign(s, n);
    }
}

public struct InputSourceOriginNamedCharRef
{
    public Index replacementIndex;
    public nuint origNameOffset;
    public Index refStartIndex;
    public NamedCharRef.RefEndType refEndType;
}

public abstract class InputSourceOrigin : Origin
{
    public abstract void noteCharRef(Index replacementIndex, NamedCharRef charRef);
    public abstract void setExternalInfo(ExternalInfo? info);
    public abstract InputSourceOrigin copy();

    public static InputSourceOrigin make()
    {
        return new InputSourceOriginImpl();
    }

    public static InputSourceOrigin make(Location refLocation)
    {
        return new InputSourceOriginImpl(refLocation);
    }
}

// a delimiter specified in bracketed text
public class BracketOrigin : Origin
{
    public enum Position { open, close }

    private Position pos_;
    private Location loc_;

    public BracketOrigin(Location loc, Position pos)
    {
        loc_ = new Location(loc);
        pos_ = pos;
    }

    public override Location parent() => loc_;
    public override Boolean inBracketedTextOpenDelim() => pos_ == Position.open;
    public override Boolean inBracketedTextCloseDelim() => pos_ == Position.close;
}

public class ReplacementOrigin : Origin
{
    private Location loc_;
    private Char origChar_;

    public ReplacementOrigin(Location loc, Char origChar)
    {
        loc_ = new Location(loc);
        origChar_ = origChar;
    }

    public override Location parent() => loc_;

    public override Boolean origChars(out Char[]? s)
    {
        Char[]? parentChars;
        if (loc_.origin().isNull() || !loc_.origin().pointer()!.origChars(out parentChars))
            s = new Char[] { origChar_ };
        else
            s = parentChars;
        return true;
    }
}

public class MultiReplacementOrigin : Origin
{
    private Location loc_;
    private StringC origChars_ = new StringC();

    public MultiReplacementOrigin(Location loc, StringC origChars)
    {
        loc_ = new Location(loc);
        origChars_.swap(origChars);
    }

    public override Location parent() => loc_;

    public override Boolean origChars(out Char[]? s)
    {
        Char[]? parentChars;
        if (loc_.origin().isNull() || !loc_.origin().pointer()!.origChars(out parentChars))
            s = origChars_.data();
        else
            s = parentChars;
        return true;
    }
}

// Implementation class for InputSourceOrigin
internal class InputSourceOriginImpl : InputSourceOrigin
{
    private Vector<InputSourceOriginNamedCharRef> charRefs_ = new Vector<InputSourceOriginNamedCharRef>();
    private StringC charRefOrigNames_ = new StringC();
    private Owner<ExternalInfo> externalInfo_ = new Owner<ExternalInfo>();
    private Location refLocation_ = new Location();
    private Mutex mutex_ = new Mutex();

    public InputSourceOriginImpl()
    {
    }

    public InputSourceOriginImpl(Location refLocation)
    {
        refLocation_ = new Location(refLocation);
    }

    public override InputSourceOrigin? asInputSourceOrigin() => this;
    public override ExternalInfo? externalInfo() => externalInfo_.pointer();
    public override Location parent() => refLocation_;

    public override InputSourceOrigin copy()
    {
        return new InputSourceOriginImpl(refLocation_);
    }

    public override void setExternalInfo(ExternalInfo? info)
    {
        externalInfo_.operatorAssign(info);
    }

    public override void noteCharRef(Index replacementIndex, NamedCharRef charRef)
    {
        using (var @lock = new Mutex.Lock(mutex_))
        {
            charRefs_.resize(charRefs_.size() + 1);
            var back = charRefs_.back();
            back.replacementIndex = replacementIndex;
            back.refStartIndex = charRef.refStartIndex();
            back.refEndType = charRef.refEndType();
            back.origNameOffset = charRefOrigNames_.size();
            charRefs_[charRefs_.size() - 1] = back;
            charRefOrigNames_.operatorPlusAssign(charRef.origName());
        }
    }

    private nuint nPrecedingCharRefs(Index ind)
    {
        nuint i;
        if (charRefs_.size() == 0 || ind > charRefs_.back().replacementIndex)
            i = charRefs_.size();
        else
        {
            i = 0;
            nuint lim = charRefs_.size();
            while (i < lim)
            {
                nuint mid = i + (lim - i) / 2;
                if (charRefs_[mid].replacementIndex >= ind)
                    lim = mid;
                else
                    i = mid + 1;
            }
        }
        return i;
    }

    public override Offset startOffset(Index ind)
    {
        using (var @lock = new Mutex.Lock(mutex_))
        {
            nuint n = nPrecedingCharRefs(ind);
            if (n < charRefs_.size() && ind == charRefs_[n].replacementIndex)
            {
                for (;;)
                {
                    ind = charRefs_[n].refStartIndex;
                    if (n == 0 || charRefs_[n - 1].replacementIndex != ind)
                        break;
                    --n;
                }
            }
            return (Offset)(ind - n);
        }
    }

    public override Boolean isNamedCharRef(Index ind, out NamedCharRef? charRef)
    {
        using (var @lock = new Mutex.Lock(mutex_))
        {
            nuint n = nPrecedingCharRefs(ind);
            if (n < charRefs_.size() && ind == charRefs_[n].replacementIndex)
            {
                nuint nameLen = (n + 1 < charRefs_.size()
                    ? charRefs_[n + 1].origNameOffset
                    : charRefOrigNames_.size()) - charRefs_[n].origNameOffset;

                Char[] nameChars = new Char[nameLen];
                for (nuint j = 0; j < nameLen; j++)
                    nameChars[j] = charRefOrigNames_[charRefs_[n].origNameOffset + j];

                charRef = new NamedCharRef();
                charRef.set(charRefs_[n].refStartIndex, charRefs_[n].refEndType, nameChars, nameLen);
                return true;
            }
            charRef = null;
            return false;
        }
    }
}

// EntityOrigin - abstract base for entity origins
public abstract class EntityOrigin : InputSourceOrigin
{
    public static nuint allocSize = 0; // Not used in C#

    public static EntityOrigin make(Allocator alloc, ConstPtr<Entity> entity)
    {
        return new EntityOriginImpl(entity);
    }

    public static EntityOrigin make(Allocator alloc, ConstPtr<Entity> entity, Location refLocation)
    {
        return new EntityOriginImpl(entity, refLocation);
    }

    public static EntityOrigin make(Allocator alloc, ConstPtr<Entity> entity, Location refLocation, Index refLength, Owner<Markup> markup)
    {
        return new EntityOriginImpl(entity, refLocation, refLength, markup);
    }

    public static EntityOrigin make(ConstPtr<Entity> entity, Location refLocation, Index refLength, Owner<Markup> markup)
    {
        return new EntityOriginImpl(entity, refLocation, refLength, markup);
    }

    public static EntityOrigin make(ConstPtr<Entity> entity, Location refLocation)
    {
        return new EntityOriginImpl(entity, refLocation);
    }
}

// Implementation class for EntityOrigin
internal class EntityOriginImpl : EntityOrigin
{
    private ConstPtr<Entity> entity_ = new ConstPtr<Entity>();
    private Index refLength_;
    private Owner<Markup> markup_ = new Owner<Markup>();
    private Location refLocation_ = new Location();

    public EntityOriginImpl(ConstPtr<Entity> entity)
    {
        entity_ = new ConstPtr<Entity>(entity);
        refLength_ = 0;
    }

    public EntityOriginImpl(ConstPtr<Entity> entity, Location refLocation)
    {
        entity_ = new ConstPtr<Entity>(entity);
        refLocation_ = new Location(refLocation);
        refLength_ = 0;
    }

    public EntityOriginImpl(ConstPtr<Entity> entity, Location refLocation, Index refLength, Owner<Markup> markup)
    {
        entity_ = new ConstPtr<Entity>(entity);
        refLocation_ = new Location(refLocation);
        refLength_ = refLength;
        markup_.swap(markup);
    }

    public override Location parent() => refLocation_;
    public override Index refLength() => refLength_;
    public override Entity? entity() => entity_.pointer();
    public override EntityDecl? entityDecl() => entity_.pointer();
    public override Markup? markup() => markup_.pointer();
    public override EntityOrigin? asEntityOrigin() => this;

    public override InputSourceOrigin copy()
    {
        Owner<Markup> m = new Owner<Markup>();
        if (markup_.hasValue())
            m.operatorAssign(new Markup(markup_.pointer()!));
        return new EntityOriginImpl(entity_, refLocation_, refLength_, m);
    }

    public override void noteCharRef(Index replacementIndex, NamedCharRef charRef)
    {
        // Delegate to base implementation if needed
    }

    public override void setExternalInfo(ExternalInfo? info)
    {
        // Delegate to base implementation if needed
    }

    public override Boolean defLocation(Offset off, out Origin? origin, out Index index)
    {
        origin = null;
        index = 0;
        if (entity_.isNull())
            return false;
        InternalEntity? @internal = entity_.pointer() as InternalEntity;
        if (@internal == null)
            return false;
        return @internal.text().charLocation(off, out origin, out index);
    }
}
