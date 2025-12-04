// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public struct Transition
{
    public const int invalidIndex = -1;

    // When performing this transition, reset all andState with index >= this.
    public uint clearAndStateStartIndex;
    // This transition is possible only if all AND groups whose AND depth
    // is >= this (and contains the LeafContentToken that this transition is
    // from) have had all their non-nullable members matched.
    public uint andDepth;
    // If this is 1, then this transition requires that the AND group
    // whose AND depth is andDepth - 1 have a non-nullable member unmatched,
    // and thus this transition is not ambiguous with a transition whose
    // AND depth is < andDepth.
    public PackedBoolean isolated;
    // Index in andState that must be clear for this transition to be
    // allowed.
    public uint requireClear;
    // Index in andState that is to be set after performing this transition.
    public uint toSet;
}

public class FirstSet
{
    private Vector<LeafContentToken?> v_ = new Vector<LeafContentToken?>();
    // index of contextually required token or -1 if none
    private nuint requiredIndex_;

    // FirstSet();
    public FirstSet()
    {
        requiredIndex_ = nuint.MaxValue; // size_t(-1)
    }

    // void init(LeafContentToken *);
    public void init(LeafContentToken p)
    {
        v_.assign(1, p);
        v_.reserve(256);
        requiredIndex_ = 0;
    }

    // void append(const FirstSet &);
    public void append(FirstSet set)
    {
        if (set.requiredIndex_ != nuint.MaxValue)
        {
            requiredIndex_ = set.requiredIndex_ + v_.size();
        }
        nuint oldSize = v_.size();
        v_.resize(v_.size() + set.v_.size());
        for (nuint i = 0; i < set.v_.size(); i++)
            v_[oldSize + i] = set.v_[i];
    }

    // size_t size() const;
    public nuint size()
    {
        return v_.size();
    }

    // LeafContentToken *token(size_t i) const;
    public LeafContentToken? token(nuint i)
    {
        return v_[i];
    }

    // size_t requiredIndex() const;
    public nuint requiredIndex()
    {
        return requiredIndex_;
    }

    // void setNotRequired();
    public void setNotRequired()
    {
        requiredIndex_ = nuint.MaxValue;
    }
}

public class LastSet : Vector<LeafContentToken?>
{
    // LastSet() { }
    public LastSet() : base()
    {
    }

    // LastSet(size_t n)
    public LastSet(nuint n) : base(n)
    {
    }

    // void append(const LastSet &);
    public void append(LastSet set)
    {
        nuint oldSize = size();
        resize(size() + set.size());
        for (nuint i = 0; i < set.size(); i++)
            this[oldSize + i] = set[i];
    }
}

public struct ContentModelAmbiguity
{
    public LeafContentToken? from;
    public LeafContentToken? to1;
    public LeafContentToken? to2;
    public uint andDepth;
}

// Struct for model group analysis
public struct GroupInfo
{
    public uint nextLeafIndex;
    public PackedBoolean containsPcdata;
    public uint andStateSize;
    public Vector<uint> nextTypeIndex;

    public GroupInfo(nuint nType)
    {
        nextTypeIndex = new Vector<uint>(nType, 0);
        nextLeafIndex = 0;
        containsPcdata = false;
        andStateSize = 0;
    }
}

public abstract class ContentToken
{
    // Note: C++ uses octal literals (01, 02, 03) but in C# these would be decimal anyway
    public enum OccurrenceIndicator { none = 0, opt = 1, plus = 2, rep = 3 }

    protected PackedBoolean inherentlyOptional_;
    private OccurrenceIndicator occurrenceIndicator_;

    // Default constructor for Vector.append
    protected ContentToken()
    {
        occurrenceIndicator_ = OccurrenceIndicator.none;
    }

    // ContentToken(OccurrenceIndicator);
    protected ContentToken(OccurrenceIndicator oi)
    {
        occurrenceIndicator_ = oi;
    }

    // virtual ~ContentToken();
    // C# handles via GC

    // OccurrenceIndicator occurrenceIndicator() const;
    public OccurrenceIndicator occurrenceIndicator()
    {
        return occurrenceIndicator_;
    }

    // Boolean inherentlyOptional() const;
    public Boolean inherentlyOptional()
    {
        return inherentlyOptional_;
    }

    // static unsigned andDepth(const AndModelGroup *);
    public static uint andDepth(AndModelGroup? andAncestor)
    {
        return andAncestor != null ? andAncestor.andDepth() + 1 : 0;
    }

    // static unsigned andIndex(const AndModelGroup *);
    public static uint andIndex(AndModelGroup? andAncestor)
    {
        return andAncestor != null
            ? andAncestor.andIndex() + andAncestor.nMembers()
            : 0;
    }

    // void analyze(GroupInfo &, const AndModelGroup *, unsigned, FirstSet &, LastSet &);
    public void analyze(ref GroupInfo info, AndModelGroup? andAncestor,
                        uint andGroupIndex, FirstSet first, LastSet last)
    {
        analyze1(ref info, andAncestor, andGroupIndex, first, last);
        if ((occurrenceIndicator_ & OccurrenceIndicator.opt) != 0)
            inherentlyOptional_ = true;
        if (inherentlyOptional_)
            first.setNotRequired();
        if ((occurrenceIndicator_ & OccurrenceIndicator.plus) != 0)
            addTransitions(last, first, false,
                           andIndex(andAncestor), andDepth(andAncestor));
    }

    // static void addTransitions(const LastSet &from, const FirstSet &to, ...);
    public static void addTransitions(LastSet from, FirstSet to,
                                       Boolean maybeRequired,
                                       uint andClearIndex,
                                       uint andDepth,
                                       Boolean isolated = false,
                                       uint requireClear = unchecked((uint)Transition.invalidIndex),
                                       uint toSet = unchecked((uint)Transition.invalidIndex))
    {
        nuint length = from.size();
        for (uint i = 0; i < length; i++)
            from[i]!.addTransitions(to, maybeRequired, andClearIndex, andDepth,
                                   isolated, requireClear, toSet);
    }

    // virtual void finish(...) = 0;
    public abstract void finish(Vector<uint> minAndDepth,
                                 Vector<nuint> elementTransition,
                                 Vector<ContentModelAmbiguity> ambiguities,
                                 ref Boolean pcdataUnreachable);

    // virtual unsigned long grpgtcnt() const;
    public virtual ulong grpgtcnt()
    {
        return 1;
    }

    // virtual void setOrGroupMember();
    public virtual void setOrGroupMember()
    {
    }

    // unsigned andGroupIndex() const;
    public virtual uint andGroupIndex()
    {
        return 0;
    }

    // virtual const ModelGroup *asModelGroup() const;
    public virtual ModelGroup? asModelGroup()
    {
        return null;
    }

    // virtual const LeafContentToken *asLeafContentToken() const;
    public virtual LeafContentToken? asLeafContentToken()
    {
        return null;
    }

    // virtual void analyze1(...) = 0;
    protected abstract void analyze1(ref GroupInfo info, AndModelGroup? andAncestor,
                                      uint andGroupIndex, FirstSet first, LastSet last);
}

public class ModelGroup : ContentToken
{
    public enum Connector { andConnector, orConnector, seqConnector }

    private Vector<Owner<ContentToken>> members_ = new Vector<Owner<ContentToken>>();

    // ModelGroup(NCVector<Owner<ContentToken> > &, OccurrenceIndicator);
    public ModelGroup(Vector<Owner<ContentToken>> v, OccurrenceIndicator oi)
        : base(oi)
    {
        members_.swap(v);
    }

    // virtual Connector connector() const = 0;
    public virtual Connector connector()
    {
        return Connector.seqConnector;
    }

    // unsigned nMembers() const;
    public uint nMembers()
    {
        return (uint)members_.size();
    }

    // void finish(...);
    public override void finish(Vector<uint> minAndDepth,
                                 Vector<nuint> elementTransition,
                                 Vector<ContentModelAmbiguity> ambiguities,
                                 ref Boolean pcdataUnreachable)
    {
        for (uint i = 0; i < nMembers(); i++)
            member(i).finish(minAndDepth, elementTransition, ambiguities,
                             ref pcdataUnreachable);
    }

    // ContentToken &member(unsigned i);
    public ContentToken member(uint i)
    {
        return members_[i].pointer()!;
    }

    // unsigned long grpgtcnt() const;
    public override ulong grpgtcnt()
    {
        ulong cnt = 1;
        for (nuint i = 0; i < members_.size(); i++)
            cnt += members_[i].pointer()!.grpgtcnt();
        return cnt;
    }

    // const ModelGroup *asModelGroup() const;
    public override ModelGroup? asModelGroup()
    {
        return this;
    }

    // void setOrGroup();
    protected void setOrGroup()
    {
        for (nuint i = 0; i < members_.size(); i++)
            members_[i].pointer()!.setOrGroupMember();
    }

    // void analyze1(...);
    protected override void analyze1(ref GroupInfo info, AndModelGroup? andAncestor,
                                      uint andGroupIndex, FirstSet first, LastSet last)
    {
        // Default implementation - subclasses override
    }
}

public class AndModelGroup : ModelGroup
{
    private uint andDepth_;          // number of and groups that contain this
    private uint andIndex_;
    private uint andGroupIndex_;
    private AndModelGroup? andAncestor_;

    // AndModelGroup(NCVector<Owner<ContentToken> > &, OccurrenceIndicator);
    public AndModelGroup(Vector<Owner<ContentToken>> v, OccurrenceIndicator oi)
        : base(v, oi)
    {
    }

    // Connector connector() const;
    public override Connector connector()
    {
        return Connector.andConnector;
    }

    // unsigned andDepth() const;
    public uint andDepth()
    {
        return andDepth_;
    }

    // unsigned andIndex() const;
    public uint andIndex()
    {
        return andIndex_;
    }

    // unsigned andGroupIndex() const;
    public override uint andGroupIndex()
    {
        return andGroupIndex_;
    }

    // const AndModelGroup *andAncestor() const;
    public AndModelGroup? andAncestor()
    {
        return andAncestor_;
    }

    // void analyze1(...);
    protected override void analyze1(ref GroupInfo info, AndModelGroup? andAncestor,
                                      uint andGroupIndex, FirstSet first, LastSet last)
    {
        andDepth_ = ContentToken.andDepth(andAncestor);
        andIndex_ = ContentToken.andIndex(andAncestor);
        andAncestor_ = andAncestor;
        andGroupIndex_ = andGroupIndex;
        if (andIndex_ + nMembers() > info.andStateSize)
            info.andStateSize = andIndex_ + nMembers();
        Vector<FirstSet> firstVec = new Vector<FirstSet>(nMembers());
        Vector<LastSet> lastVec = new Vector<LastSet>(nMembers());
        member(0).analyze(ref info, this, 0, firstVec[0], lastVec[0]);
        first.append(firstVec[0]);
        first.setNotRequired();
        last.append(lastVec[0]);
        inherentlyOptional_ = member(0).inherentlyOptional();
        uint i;
        for (i = 1; i < nMembers(); i++)
        {
            member(i).analyze(ref info, this, i, firstVec[i], lastVec[i]);
            first.append(firstVec[i]);
            first.setNotRequired();
            last.append(lastVec[i]);
            inherentlyOptional_ = inherentlyOptional_ && member(i).inherentlyOptional();
        }
        for (i = 0; i < nMembers(); i++)
        {
            for (uint j = 0; j < nMembers(); j++)
                if (j != i)
                    ContentToken.addTransitions(lastVec[i], firstVec[j], false,
                                                 andIndex() + nMembers(),
                                                 andDepth() + 1,
                                                 !member(j).inherentlyOptional(),
                                                 andIndex() + j, andIndex() + i);
        }
    }
}

public class OrModelGroup : ModelGroup
{
    // OrModelGroup(NCVector<Owner<ContentToken> > &, OccurrenceIndicator);
    public OrModelGroup(Vector<Owner<ContentToken>> v, OccurrenceIndicator oi)
        : base(v, oi)
    {
        setOrGroup();
    }

    // Connector connector() const;
    public override Connector connector()
    {
        return Connector.orConnector;
    }

    // void analyze1(...);
    protected override void analyze1(ref GroupInfo info, AndModelGroup? andAncestor,
                                      uint andGroupIndex, FirstSet first, LastSet last)
    {
        member(0).analyze(ref info, andAncestor, andGroupIndex, first, last);
        first.setNotRequired();
        inherentlyOptional_ = member(0).inherentlyOptional();
        for (uint i = 1; i < nMembers(); i++)
        {
            FirstSet tempFirst = new FirstSet();
            LastSet tempLast = new LastSet();
            member(i).analyze(ref info, andAncestor, andGroupIndex, tempFirst, tempLast);
            first.append(tempFirst);
            first.setNotRequired();
            last.append(tempLast);
            inherentlyOptional_ = inherentlyOptional_ || member(i).inherentlyOptional();
        }
    }
}

public class SeqModelGroup : ModelGroup
{
    // SeqModelGroup(NCVector<Owner<ContentToken> > &, OccurrenceIndicator);
    public SeqModelGroup(Vector<Owner<ContentToken>> v, OccurrenceIndicator oi)
        : base(v, oi)
    {
    }

    // Connector connector() const;
    public override Connector connector()
    {
        return Connector.seqConnector;
    }

    // void analyze1(...);
    protected override void analyze1(ref GroupInfo info, AndModelGroup? andAncestor,
                                      uint andGroupIndex, FirstSet first, LastSet last)
    {
        member(0).analyze(ref info, andAncestor, andGroupIndex, first, last);
        inherentlyOptional_ = member(0).inherentlyOptional();
        for (uint i = 1; i < nMembers(); i++)
        {
            FirstSet tempFirst = new FirstSet();
            LastSet tempLast = new LastSet();
            member(i).analyze(ref info, andAncestor, andGroupIndex, tempFirst, tempLast);
            ContentToken.addTransitions(last, tempFirst, true,
                                         ContentToken.andIndex(andAncestor),
                                         ContentToken.andDepth(andAncestor));
            if (inherentlyOptional_)
                first.append(tempFirst);
            if (member(i).inherentlyOptional())
                last.append(tempLast);
            else
                tempLast.swap(last);
            inherentlyOptional_ = inherentlyOptional_ && member(i).inherentlyOptional();
        }
    }
}

public class AndInfo
{
    public AndModelGroup? andAncestor;
    public uint andGroupIndex;
    public Vector<Transition> follow = new Vector<Transition>();

    public AndInfo()
    {
    }
}

// A LeafContentToken is not quite the same as a primitive content token.
// A data tag group is a primitive content token but not a LeafContentToken.
public class LeafContentToken : ContentToken
{
    protected ElementType? element_;
    private uint leafIndex_;
    private uint typeIndex_;
    private Vector<LeafContentToken?> follow_ = new Vector<LeafContentToken?>();
    private PackedBoolean isFinal_;
    private PackedBoolean orGroupMember_;
    // 0 none, 1 yes - simple, 2 - complex
    private sbyte pcdataTransitionType_;
    private LeafContentToken? simplePcdataTransition_;
    private nuint requiredIndex_;
    private Owner<AndInfo> andInfo_ = new Owner<AndInfo>();

    // Default constructor for Vector.append
    public LeafContentToken()
        : base()
    {
        isFinal_ = false;
        orGroupMember_ = false;
        requiredIndex_ = nuint.MaxValue;
    }

    // LeafContentToken(const ElementType *, OccurrenceIndicator);
    public LeafContentToken(ElementType? element, OccurrenceIndicator oi)
        : base(oi)
    {
        element_ = element;
        isFinal_ = false;
        orGroupMember_ = false;
        requiredIndex_ = nuint.MaxValue;
    }

    // unsigned index() const;
    public uint index()
    {
        return leafIndex_;
    }

    // unsigned typeIndex() const;
    public uint typeIndex()
    {
        return typeIndex_;
    }

    // const ElementType *elementType() const;
    public ElementType? elementType()
    {
        return element_;
    }

    // virtual Boolean isInitial() const;
    public virtual Boolean isInitial()
    {
        return false;
    }

    // void addTransitions(...);
    public void addTransitions(FirstSet to,
                                Boolean maybeRequired,
                                uint andClearIndex,
                                uint andDepth,
                                Boolean isolated,
                                uint requireClear,
                                uint toSet)
    {
        if (maybeRequired && to.requiredIndex() != nuint.MaxValue)
        {
            requiredIndex_ = to.requiredIndex() + follow_.size();
        }
        nuint length = follow_.size();
        nuint n = to.size();
        follow_.resize(length + n);
        for (nuint i = 0; i < n; i++)
            follow_[length + i] = to.token(i);
        if (andInfo_.hasValue())
        {
            andInfo_.pointer()!.follow.resize(length + n);
            for (nuint i = 0; i < n; i++)
            {
                andInfo_.pointer()!.follow[length + i] = new Transition
                {
                    clearAndStateStartIndex = andClearIndex,
                    andDepth = andDepth,
                    isolated = isolated,
                    requireClear = requireClear,
                    toSet = toSet
                };
            }
        }
    }

    // void setFinal();
    public void setFinal()
    {
        isFinal_ = true;
    }

    // void finish(...);
    public override void finish(Vector<uint> minAndDepthVec,
                                 Vector<nuint> elementTransitionVec,
                                 Vector<ContentModelAmbiguity> ambiguities,
                                 ref Boolean pcdataUnreachable)
    {
        if (andInfo_.hasValue())
        {
            andFinish(minAndDepthVec, elementTransitionVec, ambiguities,
                      ref pcdataUnreachable);
            return;
        }
        minAndDepthVec.assign(minAndDepthVec.size(), uint.MaxValue);
        elementTransitionVec.assign(elementTransitionVec.size(), nuint.MaxValue);
        pcdataTransitionType_ = 0;
        simplePcdataTransition_ = null;
        // follow_ is in decreasing order of andDepth because of how it's
        // constructed.
        nuint n = follow_.size();
        nuint j = 0;
        for (nuint i = 0; i < n; i++)
        {
            uint minDepth = minAndDepthVec[follow_[i]!.index()];
            if (minDepth != 0)
            {
                minAndDepthVec[follow_[i]!.index()] = 0;
                if (j != i)
                    follow_[j] = follow_[i];
                if (i == requiredIndex_)
                    requiredIndex_ = j;
                ElementType? e = follow_[i]!.elementType();
                uint ei;
                if (e == null)
                {
                    if (follow_[i]!.andInfo_.pointer() == null)
                    {
                        simplePcdataTransition_ = follow_[i];
                        pcdataTransitionType_ = 1;
                    }
                    else
                        pcdataTransitionType_ = 2;
                    ei = 0;
                }
                else
                    ei = (uint)e.index();
                if (elementTransitionVec[ei] != nuint.MaxValue)
                {
                    LeafContentToken? prev = follow_[elementTransitionVec[ei]];
                    // This might not be true: consider (a & b?)*; after the
                    // a there are two different ways to get to the same b,
                    // with the same and depth.
                    if (!ReferenceEquals(follow_[i], prev))
                    {
                        ambiguities.push_back(new ContentModelAmbiguity
                        {
                            from = this,
                            to1 = prev,
                            to2 = follow_[i],
                            andDepth = 0
                        });
                    }
                }
                elementTransitionVec[ei] = j;
                j++;
            }
        }
        if (pcdataTransitionType_ == 0)
            pcdataUnreachable = true;
        follow_.resize(j);
    }

    // void andFinish(...);
    private void andFinish(Vector<uint> minAndDepthVec,
                           Vector<nuint> elementTransitionVec,
                           Vector<ContentModelAmbiguity> ambiguities,
                           ref Boolean pcdataUnreachable)
    {
        minAndDepthVec.assign(minAndDepthVec.size(), uint.MaxValue);
        elementTransitionVec.assign(elementTransitionVec.size(), nuint.MaxValue);
        pcdataTransitionType_ = 0;
        simplePcdataTransition_ = null;
        uint pcdataMinCovered = 0;

        nuint n = follow_.size();
        nuint j = 0;
        Vector<Transition> andFollow = andInfo_.pointer()!.follow;
        for (nuint i = 0; i < n; i++)
        {
            uint minDepth = minAndDepthVec[follow_[i]!.index()];
            if (andFollow[i].andDepth < minDepth)
            {
                minAndDepthVec[follow_[i]!.index()] = andFollow[i].andDepth;
                if (j != i)
                {
                    follow_[j] = follow_[i];
                    andFollow[j] = andFollow[i];
                }
                if (i == requiredIndex_)
                    requiredIndex_ = j;
                ElementType? e = follow_[i]!.elementType();
                uint ei;
                if (e == null)
                {
                    if (pcdataTransitionType_ == 0)
                    {
                        AndModelGroup? andAncestor = andInfo_.pointer()!.andAncestor;
                        uint groupIndex = andInfo_.pointer()!.andGroupIndex;
                        do
                        {
                            Boolean hasNonNull = false;
                            for (uint k = 0; k < andAncestor!.nMembers(); k++)
                                if (k != groupIndex
                                    && !andAncestor.member(k).inherentlyOptional())
                                {
                                    hasNonNull = true;
                                    break;
                                }
                            if (hasNonNull)
                            {
                                if (andFollow[i].andDepth <= andAncestor.andDepth())
                                    pcdataUnreachable = true;
                                break;
                            }
                            groupIndex = andAncestor.andGroupIndex();
                            andAncestor = andAncestor.andAncestor();
                        } while (andAncestor != null);
                        if (andFollow[i].isolated)
                            pcdataMinCovered = andFollow[i].andDepth;
                        pcdataTransitionType_ = 2;
                    }
                    else
                    {
                        if (pcdataMinCovered > andFollow[i].andDepth + 1)
                            pcdataUnreachable = true;
                        pcdataMinCovered = andFollow[i].isolated ? andFollow[i].andDepth : 0;
                    }
                    ei = 0;
                }
                else
                    ei = (uint)e.index();
                nuint previ = elementTransitionVec[ei];
                if (previ != nuint.MaxValue)
                {
                    LeafContentToken? prev = follow_[previ];
                    if (!ReferenceEquals(follow_[i], prev)
                        && (andFollow[previ].andDepth == andFollow[i].andDepth
                            || !andFollow[previ].isolated))
                    {
                        ambiguities.push_back(new ContentModelAmbiguity
                        {
                            from = this,
                            to1 = prev,
                            to2 = follow_[i],
                            andDepth = andFollow[i].andDepth
                        });
                    }
                    if (andFollow[previ].isolated)
                        elementTransitionVec[ei] = j;
                }
                else
                    elementTransitionVec[ei] = j;
                j++;
            }
        }
        if (pcdataMinCovered > 0 || pcdataTransitionType_ == 0)
            pcdataUnreachable = true;
        follow_.resize(j);
        andInfo_.pointer()!.follow.resize(j);
    }

    // Boolean isFinal() const;
    public Boolean isFinal()
    {
        return isFinal_;
    }

    // Boolean tryTransition(...);
    public Boolean tryTransition(ElementType? to,
                                  AndState andState,
                                  ref uint minAndDepth,
                                  out LeafContentToken? newpos)
    {
        for (nuint i = 0; i < follow_.size(); i++)
        {
            if (ReferenceEquals(follow_[i]!.elementType(), to))
            {
                if (!andInfo_.hasValue())
                {
                    newpos = follow_[i];
                    minAndDepth = newpos!.computeMinAndDepth(andState);
                    return true;
                }
                Transition t = andInfo_.pointer()!.follow[i];
                if ((t.requireClear == unchecked((uint)Transition.invalidIndex)
                     || andState.isClear(t.requireClear))
                    && t.andDepth >= minAndDepth)
                {
                    if (t.toSet != unchecked((uint)Transition.invalidIndex))
                        andState.set(t.toSet);
                    andState.clearFrom(t.clearAndStateStartIndex);
                    newpos = follow_[i];
                    minAndDepth = newpos!.computeMinAndDepth(andState);
                    return true;
                }
            }
        }
        newpos = null;
        return false;
    }

    // Boolean tryTransitionPcdata(...);
    public Boolean tryTransitionPcdata(AndState andState,
                                        ref uint minAndDepth,
                                        out LeafContentToken? newpos)
    {
        if (pcdataTransitionType_ == 1)
        {
            newpos = simplePcdataTransition_;
            return true;
        }
        else if (pcdataTransitionType_ == 0)
        {
            newpos = null;
            return false;
        }
        else
            return tryTransition(null, andState, ref minAndDepth, out newpos);
    }

    // void possibleTransitions(...);
    public void possibleTransitions(AndState andState, uint minAndDepth,
                                     Vector<ElementType?> v)
    {
        if (!andInfo_.hasValue())
        {
            for (nuint i = 0; i < follow_.size(); i++)
                v.push_back(follow_[i]!.elementType());
        }
        else
        {
            for (nuint i = 0; i < follow_.size(); i++)
            {
                Transition t = andInfo_.pointer()!.follow[i];
                if ((t.requireClear == unchecked((uint)Transition.invalidIndex)
                     || andState.isClear(t.requireClear))
                    && t.andDepth >= minAndDepth)
                    v.push_back(follow_[i]!.elementType());
            }
        }
    }

    // const LeafContentToken *impliedStartTag(...);
    public LeafContentToken? impliedStartTag(AndState andState, uint minAndDepth)
    {
        if (requiredIndex_ != nuint.MaxValue)
        {
            if (!andInfo_.hasValue())
                return follow_[requiredIndex_];
            Transition t = andInfo_.pointer()!.follow[requiredIndex_];
            if ((t.requireClear == unchecked((uint)Transition.invalidIndex)
                 || andState.isClear(t.requireClear))
                && t.andDepth >= minAndDepth)
                return follow_[requiredIndex_];
        }
        return null;
    }

    // const LeafContentToken *transitionToken(...);
    public LeafContentToken? transitionToken(ElementType? to,
                                              AndState andState,
                                              uint minAndDepth)
    {
        if (!andInfo_.hasValue())
        {
            for (nuint i = 0; i < follow_.size(); i++)
                if (ReferenceEquals(follow_[i]!.elementType(), to))
                    return follow_[i];
        }
        else
        {
            for (nuint i = 0; i < follow_.size(); i++)
            {
                Transition t = andInfo_.pointer()!.follow[i];
                if (ReferenceEquals(follow_[i]!.elementType(), to)
                    && (t.requireClear == unchecked((uint)Transition.invalidIndex)
                        || andState.isClear(t.requireClear))
                    && t.andDepth >= minAndDepth)
                    return follow_[i];
            }
        }
        return null;
    }

    // void doRequiredTransition(...);
    public void doRequiredTransition(AndState andState,
                                      ref uint minAndDepth,
                                      out LeafContentToken? newpos)
    {
        if (andInfo_.hasValue())
        {
            Transition t = andInfo_.pointer()!.follow[requiredIndex_];
            if (t.toSet != unchecked((uint)Transition.invalidIndex))
                andState.set(t.toSet);
            andState.clearFrom(t.clearAndStateStartIndex);
        }
        newpos = follow_[requiredIndex_];
        minAndDepth = newpos!.computeMinAndDepth(andState);
    }

    // unsigned computeMinAndDepth(const AndState&) const;
    public uint computeMinAndDepth(AndState andState)
    {
        return andInfo_.hasValue() ? computeMinAndDepth1(andState) : 0;
    }

    // unsigned computeMinAndDepth1(const AndState&) const;
    private uint computeMinAndDepth1(AndState andState)
    {
        uint groupIndex = andInfo_.pointer()!.andGroupIndex;
        for (AndModelGroup? group = andInfo_.pointer()!.andAncestor;
             group != null;
             groupIndex = group.andGroupIndex(), group = group.andAncestor())
        {
            for (uint i = 0; i < group.nMembers(); i++)
                if (i != groupIndex && !group.member(i).inherentlyOptional()
                    && andState.isClear(group.andIndex() + i))
                    return group.andDepth() + 1;
        }
        return 0;
    }

    // Boolean orGroupMember() const;
    public Boolean orGroupMember()
    {
        return orGroupMember_;
    }

    // void setOrGroupMember();
    public override void setOrGroupMember()
    {
        orGroupMember_ = true;
    }

    // const AndModelGroup *andAncestor() const;
    public AndModelGroup? andAncestor()
    {
        return andInfo_.hasValue() ? andInfo_.pointer()!.andAncestor : null;
    }

    // unsigned andDepth() const;
    public uint andDepth()
    {
        return andInfo_.hasValue() ? ContentToken.andDepth(andInfo_.pointer()!.andAncestor) : 0;
    }

    // const LeafContentToken *asLeafContentToken() const;
    public override LeafContentToken? asLeafContentToken()
    {
        return this;
    }

    // void analyze1(...);
    protected override void analyze1(ref GroupInfo info, AndModelGroup? andAncestor,
                                      uint andGroupIndex, FirstSet first, LastSet last)
    {
        leafIndex_ = info.nextLeafIndex++;
        typeIndex_ = info.nextTypeIndex[element_ != null ? element_.index() : 0]++;
        if (andAncestor != null)
        {
            andInfo_.operatorAssign(new AndInfo
            {
                andAncestor = andAncestor,
                andGroupIndex = andGroupIndex
            });
        }
        first.init(this);
        last.assign(1, this);
        inherentlyOptional_ = false;
    }
}

public class PcdataToken : LeafContentToken
{
    // PcdataToken();
    public PcdataToken()
        : base(null, OccurrenceIndicator.rep)
    {
    }

    // void analyze1(...);
    protected override void analyze1(ref GroupInfo info, AndModelGroup? andAncestor,
                                      uint andGroupIndex, FirstSet first, LastSet last)
    {
        info.containsPcdata = true;
        base.analyze1(ref info, andAncestor, andGroupIndex, first, last);
    }
}

public class InitialPseudoToken : LeafContentToken
{
    // InitialPseudoToken();
    public InitialPseudoToken()
        : base(null, OccurrenceIndicator.none)
    {
    }

    // Boolean isInitial() const;
    public override Boolean isInitial()
    {
        return true;
    }
}

public class ElementToken : LeafContentToken
{
    // ElementToken(const ElementType *, OccurrenceIndicator);
    public ElementToken(ElementType? element, OccurrenceIndicator oi)
        : base(element, oi)
    {
    }
}

public class DataTagGroup : SeqModelGroup
{
    // first content token is a DataTagElementToken, second is PcdataToken
    // DataTagGroup(NCVector<Owner<ContentToken> > &, OccurrenceIndicator);
    public DataTagGroup(Vector<Owner<ContentToken>> vec, OccurrenceIndicator oi)
        : base(vec, oi)
    {
    }
}

public class DataTagElementToken : ElementToken
{
    private Vector<Text> templates_ = new Vector<Text>();
#pragma warning disable CS0414 // Field is assigned but never used (preserved from upstream C++ code)
    private Boolean havePaddingTemplate_;
#pragma warning restore CS0414
    private Text paddingTemplate_ = new Text();

    // DataTagElementToken(const ElementType *, Vector<Text> &templates);
    public DataTagElementToken(ElementType? element, Vector<Text> templates)
        : base(element, OccurrenceIndicator.none)
    {
        havePaddingTemplate_ = false;
        templates.swap(templates_);
    }

    // DataTagElementToken(const ElementType *, Vector<Text> &templates, Text &paddingTemplate);
    public DataTagElementToken(ElementType? element, Vector<Text> templates, Text paddingTemplate)
        : base(element, OccurrenceIndicator.none)
    {
        havePaddingTemplate_ = true;
        templates.swap(templates_);
        paddingTemplate.swap(paddingTemplate_);
    }
}

public class CompiledModelGroup
{
    private Owner<ModelGroup> modelGroup_ = new Owner<ModelGroup>();
    private Owner<LeafContentToken> initial_ = new Owner<LeafContentToken>();
    private uint andStateSize_;
    private Boolean containsPcdata_;

    // CompiledModelGroup(Owner<ModelGroup> &);
    public CompiledModelGroup(Owner<ModelGroup> modelGroup)
    {
        modelGroup_.operatorAssign(modelGroup.extract());
    }

    // void compile(...);
    public void compile(nuint nElementTypeIndex,
                        Vector<ContentModelAmbiguity> ambiguities,
                        ref Boolean pcdataUnreachable)
    {
        FirstSet first = new FirstSet();
        LastSet last = new LastSet();
        GroupInfo info = new GroupInfo(nElementTypeIndex);
        modelGroup_.pointer()!.analyze(ref info, null, 0, first, last);
        for (nuint i = 0; i < last.size(); i++)
            last[i]!.setFinal();
        andStateSize_ = info.andStateSize;
        containsPcdata_ = info.containsPcdata;
        initial_.operatorAssign(new InitialPseudoToken());
        LastSet initialSet = new LastSet(1);
        initialSet[0] = initial_.pointer();
        ContentToken.addTransitions(initialSet, first, true, 0, 0);
        if (modelGroup_.pointer()!.inherentlyOptional())
            initial_.pointer()!.setFinal();
        pcdataUnreachable = false;
        Vector<uint> minAndDepth = new Vector<uint>(info.nextLeafIndex);
        Vector<nuint> elementTransition = new Vector<nuint>(nElementTypeIndex);
        initial_.pointer()!.finish(minAndDepth, elementTransition, ambiguities,
                                   ref pcdataUnreachable);
        modelGroup_.pointer()!.finish(minAndDepth, elementTransition, ambiguities,
                                      ref pcdataUnreachable);
        if (!containsPcdata_)
            pcdataUnreachable = false;
    }

    // CompiledModelGroup *copy() const;
    // Not implemented - would require deep copy

    // const LeafContentToken *initial() const;
    public LeafContentToken? initial()
    {
        return initial_.pointer();
    }

    // unsigned andStateSize() const;
    public uint andStateSize()
    {
        return andStateSize_;
    }

    // Boolean containsPcdata() const;
    public Boolean containsPcdata()
    {
        return containsPcdata_;
    }

    // const ModelGroup *modelGroup() const;
    public ModelGroup? modelGroup()
    {
        return modelGroup_.pointer();
    }
}

public class AndState
{
    private uint clearFrom_;
    private Vector<PackedBoolean> v_ = new Vector<PackedBoolean>();

    // AndState(unsigned);
    public AndState(uint n)
    {
        v_ = new Vector<PackedBoolean>(n, false);
        clearFrom_ = 0;
    }

    // Boolean isClear(unsigned) const;
    public Boolean isClear(uint i)
    {
        return !v_[i];
    }

    // void clearFrom(unsigned);
    public void clearFrom(uint i)
    {
        if (i < clearFrom_)
            clearFrom1(i);
    }

    // void set(unsigned);
    public void set(uint i)
    {
        v_[i] = true;
        if (i >= clearFrom_)
            clearFrom_ = i + 1;
    }

    // Boolean operator==(const AndState &) const;
    public Boolean operatorEqual(AndState state)
    {
        for (nuint i = 0; i < v_.size(); i++)
        {
            if (i >= clearFrom_ && i >= state.clearFrom_)
                break;
            if (v_[i] != state.v_[i])
                return false;
        }
        return true;
    }

    // Boolean operator!=(const AndState &) const;
    public Boolean operatorNotEqual(AndState state)
    {
        return !operatorEqual(state);
    }

    // void clearFrom1(unsigned);
    private void clearFrom1(uint i)
    {
        while (clearFrom_ > i)
            v_[--clearFrom_] = false;
    }
}

public class MatchState
{
    private LeafContentToken? pos_;
    private AndState andState_;
    private uint minAndDepth_;

    // MatchState();
    public MatchState()
    {
        andState_ = new AndState(0);
    }

    // MatchState(const CompiledModelGroup *); // may be 0
    public MatchState(CompiledModelGroup? model)
    {
        pos_ = model?.initial();
        andState_ = new AndState(model?.andStateSize() ?? 0);
        minAndDepth_ = 0;
    }

    // Boolean tryTransition(const ElementType *);
    public Boolean tryTransition(ElementType? to)
    {
        LeafContentToken? newpos;
        return pos_!.tryTransition(to, andState_, ref minAndDepth_, out newpos) &&
               (pos_ = newpos) != null || pos_ == newpos;
    }

    // Boolean tryTransitionPcdata();
    public Boolean tryTransitionPcdata()
    {
        LeafContentToken? newpos;
        Boolean result = pos_!.tryTransitionPcdata(andState_, ref minAndDepth_, out newpos);
        if (result)
            pos_ = newpos;
        return result;
    }

    // void possibleTransitions(Vector<const ElementType *> &) const;
    public void possibleTransitions(Vector<ElementType?> v)
    {
        pos_!.possibleTransitions(andState_, minAndDepth_, v);
    }

    // Boolean isFinished() const;
    public Boolean isFinished()
    {
        return pos_!.isFinal() && minAndDepth_ == 0;
    }

    // const LeafContentToken *impliedStartTag() const;
    public LeafContentToken? impliedStartTag()
    {
        return pos_!.impliedStartTag(andState_, minAndDepth_);
    }

    // const LeafContentToken *invalidExclusion(const ElementType *) const;
    public LeafContentToken? invalidExclusion(ElementType? e)
    {
        LeafContentToken? token = pos_!.transitionToken(e, andState_, minAndDepth_);
        if (token != null && !token.inherentlyOptional() && !token.orGroupMember())
            return token;
        else
            return null;
    }

    // void doRequiredTransition();
    public void doRequiredTransition()
    {
        LeafContentToken? newpos;
        pos_!.doRequiredTransition(andState_, ref minAndDepth_, out newpos);
        pos_ = newpos;
    }

    // const LeafContentToken *currentPosition() const;
    public LeafContentToken? currentPosition()
    {
        return pos_;
    }

    // Boolean operator==(const MatchState &) const;
    public Boolean operatorEqual(MatchState state)
    {
        return ReferenceEquals(pos_, state.pos_) && andState_.operatorEqual(state.andState_)
               && minAndDepth_ == state.minAndDepth_;
    }

    // Boolean operator!=(const MatchState &) const;
    public Boolean operatorNotEqual(MatchState state)
    {
        return !operatorEqual(state);
    }
}
