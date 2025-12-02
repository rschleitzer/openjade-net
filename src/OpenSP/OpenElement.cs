// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class OpenElement : Link
{
    private ElementType? elementType_;
    private PackedBoolean netEnabling_;      // start-tag was net-enabling
    private PackedBoolean included_;
    private MatchState matchState_ = new MatchState();
    private ElementDefinition.DeclaredContent declaredContent_;
    private ShortReferenceMap? map_;
    private Location startLocation_ = new Location();
    private ulong index_;

    public OpenElement(ElementType? type, Boolean net, Boolean included,
                       ShortReferenceMap? map, Location startLocation)
    {
        elementType_ = type;
        netEnabling_ = net;
        included_ = included;
        matchState_ = new MatchState(type!.definition()!.compiledModelGroup());
        map_ = map;
        startLocation_ = new Location(startLocation);
        declaredContent_ = type.definition()!.declaredContent();
    }

    public ElementType? type()
    {
        return elementType_;
    }

    public Boolean netEnabling()
    {
        return netEnabling_;
    }

    public Boolean included()
    {
        return included_;
    }

    public MatchState matchState()
    {
        return matchState_;
    }

    public void setMatchState(MatchState state)
    {
        matchState_ = state;
    }

    public Boolean isFinished()
    {
        return (declaredContent_ != ElementDefinition.DeclaredContent.modelGroup
                || matchState_.isFinished());
    }

    public Boolean tryTransition(ElementType? e)
    {
        switch (declaredContent_)
        {
            case ElementDefinition.DeclaredContent.modelGroup:
                return matchState_.tryTransition(e);
            case ElementDefinition.DeclaredContent.any:
                return (e != elementType_) || e!.definition()!.allowImmediateRecursion();
            default:
                return false;
        }
    }

    public Boolean tryTransitionPcdata()
    {
        return (declaredContent_ == ElementDefinition.DeclaredContent.modelGroup
                ? matchState_.tryTransitionPcdata()
                : true);  // CDATA, RCDATA, ANY all ok
    }

    public LeafContentToken? invalidExclusion(ElementType? e)
    {
        return (declaredContent_ == ElementDefinition.DeclaredContent.modelGroup
                ? matchState_.invalidExclusion(e)
                : null);
    }

    public void doRequiredTransition()
    {
        matchState_.doRequiredTransition();
    }

    public LeafContentToken? impliedStartTag()
    {
        return (declaredContent_ == ElementDefinition.DeclaredContent.modelGroup
                ? matchState_.impliedStartTag()
                : null);
    }

    public ShortReferenceMap? map()
    {
        return map_;
    }

    public void setMap(ShortReferenceMap? map)
    {
        map_ = map;
    }

    public Boolean requiresSpecialParse()
    {
        return (declaredContent_ == ElementDefinition.DeclaredContent.cdata
                || declaredContent_ == ElementDefinition.DeclaredContent.rcdata);
    }

    public Mode mode(Boolean netEnabled)
    {
        return elementType_!.definition()!.mode(netEnabled);
    }

    public Location startLocation()
    {
        return startLocation_;
    }

    public LeafContentToken? currentPosition()
    {
        return (declaredContent_ == ElementDefinition.DeclaredContent.modelGroup
                ? matchState_.currentPosition()
                : null);
    }

    public Boolean declaredEmpty()
    {
        return declaredContent_ == ElementDefinition.DeclaredContent.empty;
    }

    public void setConref()
    {
        declaredContent_ = ElementDefinition.DeclaredContent.empty;
    }

    public ulong index()
    {
        return index_;
    }

    public void setIndex(ulong index)
    {
        index_ = index;
    }
}
