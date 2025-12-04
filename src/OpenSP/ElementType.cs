// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class ElementDefinition : Resource
{
    public enum DeclaredContent { modelGroup, any, cdata, rcdata, empty }
    public enum OmitFlags { omitStart = 01, omitEnd = 02, omitSpec = 04 }
    public const int undefinedIndex = -1;

    private Location location_ = new Location();
    private nuint index_;
    private byte omitFlags_;
    private DeclaredContent declaredContent_;
    private Boolean allowImmediateRecursion_;
    private Owner<CompiledModelGroup> modelGroup_ = new Owner<CompiledModelGroup>();
    private Vector<ElementType?> inclusions_ = new Vector<ElementType?>();
    private Vector<ElementType?> exclusions_ = new Vector<ElementType?>();
    // rankStems_ contains all the rank stems in the ranked group in this
    // definition.
    private Vector<RankStem?> rankStems_ = new Vector<RankStem?>();
    private StringC rankSuffix_ = new StringC();
    private Mode mode_;
    private Mode netMode_;

    // ElementDefinition(const Location &location, size_t index, unsigned char omitFlags,
    //                   DeclaredContent declaredContent, Boolean allowImmediateRecursion = 1);
    public ElementDefinition(Location location, nuint index, byte omitFlags,
                             DeclaredContent declaredContent,
                             Boolean allowImmediateRecursion = true)
    {
        location_ = new Location(location);
        index_ = index;
        omitFlags_ = omitFlags;
        declaredContent_ = declaredContent;
        allowImmediateRecursion_ = allowImmediateRecursion;
        computeMode();
    }

    // ElementDefinition(const Location &location, size_t index, unsigned char omitFlags,
    //                   DeclaredContent declaredContent, Owner<CompiledModelGroup> &modelGroup);
    public ElementDefinition(Location location, nuint index, byte omitFlags,
                             DeclaredContent declaredContent,
                             Owner<CompiledModelGroup> modelGroup)
    {
        location_ = new Location(location);
        index_ = index;
        omitFlags_ = omitFlags;
        declaredContent_ = declaredContent;
        modelGroup_.operatorAssign(modelGroup.extract());
        computeMode();
    }

    // ~ElementDefinition();
    // C# handles via GC

    // const CompiledModelGroup *compiledModelGroup() const;
    public CompiledModelGroup? compiledModelGroup()
    {
        return modelGroup_.pointer();
    }

    // DeclaredContent declaredContent() const;
    public DeclaredContent declaredContent()
    {
        return declaredContent_;
    }

    // Boolean omittedTagSpec() const;
    public Boolean omittedTagSpec()
    {
        return (omitFlags_ & (byte)OmitFlags.omitSpec) != 0;
    }

    // Boolean canOmitStartTag() const;
    public Boolean canOmitStartTag()
    {
        return (omitFlags_ & (byte)OmitFlags.omitStart) != 0;
    }

    // Boolean canOmitEndTag() const;
    public Boolean canOmitEndTag()
    {
        return (omitFlags_ & (byte)OmitFlags.omitEnd) != 0;
    }

    // size_t nRankStems() const;
    public nuint nRankStems()
    {
        return rankStems_.size();
    }

    // const RankStem *rankStem(size_t i) const;
    public RankStem? rankStem(nuint i)
    {
        return rankStems_[i];
    }

    // const StringC &rankSuffix() const;
    public StringC rankSuffix()
    {
        return rankSuffix_;
    }

    // size_t nInclusions() const;
    public nuint nInclusions()
    {
        return inclusions_.size();
    }

    // const ElementType *inclusion(size_t) const;
    public ElementType? inclusion(nuint i)
    {
        return inclusions_[i];
    }

    // size_t nExclusions() const;
    public nuint nExclusions()
    {
        return exclusions_.size();
    }

    // const ElementType *exclusion(size_t) const;
    public ElementType? exclusion(nuint i)
    {
        return exclusions_[i];
    }

    // Boolean undefined() const;
    public Boolean undefined()
    {
        return index_ == nuint.MaxValue; // size_t(undefinedIndex)
    }

    // Boolean allowImmediateRecursion() const;
    public Boolean allowImmediateRecursion()
    {
        return allowImmediateRecursion_;
    }

    // void setInclusions(Vector<const ElementType *> &inclusions);
    public void setInclusions(Vector<ElementType?> inclusions)
    {
        inclusions.swap(inclusions_);
    }

    // void setExclusions(Vector<const ElementType *> &exclusions);
    public void setExclusions(Vector<ElementType?> exclusions)
    {
        exclusions.swap(exclusions_);
    }

    // void setRank(StringC &suffix, Vector<const RankStem *> &rankStems);
    public void setRank(StringC rankSuffix, Vector<RankStem?> rankStems)
    {
        rankStems.swap(rankStems_);
        rankSuffix.swap(rankSuffix_);
    }

    // Mode mode(Boolean netEnabled) const;
    public Mode mode(Boolean netEnabled)
    {
        return netEnabled ? netMode_ : mode_;
    }

    // const Location &location() const;
    public Location location()
    {
        return location_;
    }

    // void computeMode();
    private void computeMode()
    {
        switch (declaredContent_)
        {
            case DeclaredContent.modelGroup:
                if (modelGroup_.pointer() != null && !modelGroup_.pointer()!.containsPcdata())
                {
                    netMode_ = Mode.econnetMode;
                    mode_ = Mode.econMode;
                    break;
                }
                goto case DeclaredContent.any; // fall through
            case DeclaredContent.any:
                netMode_ = Mode.mconnetMode;
                mode_ = Mode.mconMode;
                break;
            case DeclaredContent.cdata:
                netMode_ = Mode.cconnetMode;
                mode_ = Mode.cconMode;
                break;
            case DeclaredContent.rcdata:
                netMode_ = Mode.rcconnetMode;
                mode_ = Mode.rcconMode;
                break;
            case DeclaredContent.empty:
                break;
            default:
                throw new InvalidOperationException("CANNOT_HAPPEN");
        }
    }
}

public class RankStem : Named
{
    private nuint index_;
    private Vector<ConstPtr<ElementDefinition>> def_ = new Vector<ConstPtr<ElementDefinition>>();

    // RankStem(const StringC &, size_t);
    public RankStem(StringC name, nuint index)
        : base(name)
    {
        index_ = index;
    }

    // size_t index() const;
    public nuint index()
    {
        return index_;
    }

    // void addDefinition(const ConstPtr<ElementDefinition> &);
    public void addDefinition(ConstPtr<ElementDefinition> p)
    {
        def_.push_back(p);
    }

    // size_t nDefinitions() const;
    public nuint nDefinitions()
    {
        return def_.size();
    }

    // const ElementDefinition *definition(size_t) const;
    public ElementDefinition? definition(nuint i)
    {
        return def_[i].pointer();
    }
}

public class ElementType : Named, IAttributed
{
    private StringC origName_ = new StringC();
    private nuint index_;
    private nuint defIndex_;  // index in the group having same definition
    private ConstPtr<ElementDefinition> def_ = new ConstPtr<ElementDefinition>();
    private ShortReferenceMap? map_;
    private Attributed attributed_ = new Attributed();

    // Default constructor for Vector.append
    public ElementType()
        : base()
    {
        map_ = null;
        defIndex_ = 0;
    }

    // ElementType(const StringC &, size_t);
    public ElementType(StringC name, nuint index)
        : base(name)
    {
        index_ = index;
        map_ = null;
        defIndex_ = 0;
    }

    // void setElementDefinition(const ConstPtr<ElementDefinition> &, size_t defIndex);
    public void setElementDefinition(ConstPtr<ElementDefinition> def, nuint defIndex)
    {
        def_ = def;
        defIndex_ = defIndex;
    }

    // void setMap(const ShortReferenceMap *);
    public void setMap(ShortReferenceMap? map)
    {
        map_ = map;
    }

    // void setRankStem(RankStem *);
    public void setRankStem(RankStem? rankStem)
    {
        // In the original, this is used to set up rank associations
        // Implementation depends on how ranks are tracked
    }

    // Boolean undefined() const;
    public Boolean undefined()
    {
        return def_.pointer()?.undefined() ?? true;
    }

    // void setOrigName(StringC &origName);
    public void setOrigName(StringC origName)
    {
        origName.swap(origName_);
    }

    // const ElementDefinition *definition() const;
    public ElementDefinition? definition()
    {
        return def_.pointer();
    }

    // Boolean isRankedElement() const;
    public Boolean isRankedElement()
    {
        return def_.pointer()?.nRankStems() > 0;
    }

    // const RankStem *rankedElementRankStem() const;
    public RankStem? rankedElementRankStem()
    {
        return def_.pointer()?.rankStem(defIndex_);
    }

    // size_t index() const;
    public nuint index()
    {
        return index_;
    }

    // const ShortReferenceMap *map() const;
    public ShortReferenceMap? map()
    {
        return map_;
    }

    // void swap(ElementType &);
    public void swap(ElementType to)
    {
        base.swap(to);
        {
            nuint tem = to.index_;
            to.index_ = index_;
            index_ = tem;
        }
        {
            nuint tem = to.defIndex_;
            to.defIndex_ = defIndex_;
            defIndex_ = tem;
        }
        def_.swap(to.def_);
        {
            ShortReferenceMap? tem = to.map_;
            to.map_ = map_;
            map_ = tem;
        }
    }

    // const StringC &origName() const;
    public StringC origName()
    {
        return origName_;
    }

    // Attributed interface implementation
    public ConstPtr<AttributeDefinitionList> attributeDef()
    {
        return attributed_.attributeDef();
    }

    public AttributeDefinitionList? attributeDefTemp()
    {
        return attributed_.attributeDefTemp();
    }

    public Ptr<AttributeDefinitionList> attributeDefMutable()
    {
        return attributed_.attributeDefMutable();
    }

    public void setAttributeDef(Ptr<AttributeDefinitionList> def)
    {
        attributed_.setAttributeDef(def);
    }
}

// Interface to allow multiple inheritance pattern
public interface IAttributed
{
    ConstPtr<AttributeDefinitionList> attributeDef();
    AttributeDefinitionList? attributeDefTemp();
    Ptr<AttributeDefinitionList> attributeDefMutable();
    void setAttributeDef(Ptr<AttributeDefinitionList> def);
}
