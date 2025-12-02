// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Result element specification
public class ResultElementSpec
{
    public ElementType? elementType;
    public AttributeList attributeList = new AttributeList();

    // ResultElementSpec();
    public ResultElementSpec()
    {
        elementType = null;
    }

    // void swap(ResultElementSpec &);
    public void swap(ResultElementSpec to)
    {
        attributeList.swap(to.attributeList);
        {
            ElementType? tem = to.elementType;
            to.elementType = elementType;
            elementType = tem;
        }
    }
}

// Link Process Definition (base class)
public class Lpd : Resource
{
    public enum Type { simpleLink, implicitLink, explicitLink }

    private Type type_;
    private Location location_ = new Location();
    private Boolean active_;
    private Ptr<Dtd> sourceDtd_ = new Ptr<Dtd>();
    private ConstPtr<StringResource<Char>> name_ = new ConstPtr<StringResource<Char>>();

    // Lpd(const StringC &, Type, const Location &, const Ptr<Dtd> &sourceDtd);
    public Lpd(StringC name, Type type, Location location, Ptr<Dtd> sourceDtd)
    {
        name_ = new ConstPtr<StringResource<Char>>(new StringResource<Char>(name));
        type_ = type;
        location_ = new Location(location);
        active_ = false;
        sourceDtd_ = sourceDtd;
    }

    // For derived classes that need different initialization
    protected Lpd()
    {
    }

    // virtual ~Lpd();
    // C# GC handles cleanup

    // Type type() const;
    public Type type()
    {
        return type_;
    }

    // const Location &location() const;
    public Location location()
    {
        return location_;
    }

    // Boolean active() const;
    public Boolean active()
    {
        return active_;
    }

    // void activate();
    public void activate()
    {
        active_ = true;
    }

    // ConstPtr<Dtd> sourceDtd() const;
    public ConstPtr<Dtd> sourceDtdConst()
    {
        return new ConstPtr<Dtd>(sourceDtd_.pointer());
    }

    // const Ptr<Dtd> &sourceDtd();
    public Ptr<Dtd> sourceDtd()
    {
        return sourceDtd_;
    }

    // const ConstPtr<StringResource<Char> > &namePointer() const;
    public ConstPtr<StringResource<Char>> namePointer()
    {
        return name_;
    }

    // const StringC &name() const;
    public StringC name()
    {
        StringResource<Char>? res = name_.pointer();
        if (res != null)
            return new StringC(res);
        return new StringC();
    }
}

// Simple Link Process Definition
public class SimpleLpd : Lpd, IAttributed
{
    private Ptr<AttributeDefinitionList> attributeDef_ = new Ptr<AttributeDefinitionList>();

    // SimpleLpd(const StringC &, const Location &, const Ptr<Dtd> &sourceDtd);
    public SimpleLpd(StringC name, Location location, Ptr<Dtd> sourceDtd)
        : base(name, Type.simpleLink, location, sourceDtd)
    {
    }

    // IAttributed implementation
    public ConstPtr<AttributeDefinitionList> attributeDef()
    {
        return new ConstPtr<AttributeDefinitionList>(attributeDef_.pointer());
    }

    public AttributeDefinitionList? attributeDefTemp()
    {
        return attributeDef_.pointer();
    }

    public Ptr<AttributeDefinitionList> attributeDefMutable()
    {
        return attributeDef_;
    }

    public void setAttributeDef(Ptr<AttributeDefinitionList> def)
    {
        attributeDef_ = def;
    }
}

// A link rule whose source element specification is not implied.
public class SourceLinkRule
{
    private LinkSet? uselink_;
    private LinkSet? postlink_;
    private Boolean postlinkRestore_;
    private AttributeList linkAttributes_ = new AttributeList();
    private ResultElementSpec resultElementSpec_ = new ResultElementSpec();

    // SourceLinkRule();
    public SourceLinkRule()
    {
        uselink_ = null;
        postlink_ = null;
        postlinkRestore_ = false;
    }

    // void setLinkAttributes(AttributeList &);
    public void setLinkAttributes(AttributeList attributes)
    {
        attributes.swap(linkAttributes_);
    }

    // void setResult(const ElementType *, AttributeList &);
    public void setResult(ElementType? element, AttributeList attributes)
    {
        resultElementSpec_.elementType = element;
        attributes.swap(resultElementSpec_.attributeList);
    }

    // void setUselink(const LinkSet *);
    public void setUselink(LinkSet? linkSet)
    {
        uselink_ = linkSet;
    }

    // void setPostlink(const LinkSet *);
    public void setPostlink(LinkSet? linkSet)
    {
        postlink_ = linkSet;
    }

    // void setPostlinkRestore();
    public void setPostlinkRestore()
    {
        postlinkRestore_ = true;
    }

    // void swap(SourceLinkRule &);
    public virtual void swap(SourceLinkRule to)
    {
        linkAttributes_.swap(to.linkAttributes_);
        resultElementSpec_.swap(to.resultElementSpec_);
        {
            LinkSet? tem = to.uselink_;
            to.uselink_ = uselink_;
            uselink_ = tem;
        }
        {
            LinkSet? tem = to.postlink_;
            to.postlink_ = postlink_;
            postlink_ = tem;
        }
        {
            Boolean tem = to.postlinkRestore_;
            to.postlinkRestore_ = postlinkRestore_;
            postlinkRestore_ = tem;
        }
    }

    // const AttributeList &attributes() const;
    public AttributeList attributes()
    {
        return linkAttributes_;
    }

    // const ResultElementSpec &resultElementSpec() const;
    public ResultElementSpec resultElementSpec()
    {
        return resultElementSpec_;
    }

    // const LinkSet *uselink() const;
    public LinkSet? uselink()
    {
        return uselink_;
    }

    // const LinkSet *postlink() const;
    public LinkSet? postlink()
    {
        return postlink_;
    }

    // Boolean postlinkRestore() const;
    public Boolean postlinkRestore()
    {
        return postlinkRestore_;
    }
}

// SourceLinkRule as a Resource for ref-counting
public class SourceLinkRuleResource : Resource
{
    private SourceLinkRule rule_ = new SourceLinkRule();

    // SourceLinkRuleResource();
    public SourceLinkRuleResource()
    {
    }

    // Delegate to inner rule
    public void setLinkAttributes(AttributeList attributes)
    {
        rule_.setLinkAttributes(attributes);
    }

    public void setResult(ElementType? element, AttributeList attributes)
    {
        rule_.setResult(element, attributes);
    }

    public void setUselink(LinkSet? linkSet)
    {
        rule_.setUselink(linkSet);
    }

    public void setPostlink(LinkSet? linkSet)
    {
        rule_.setPostlink(linkSet);
    }

    public void setPostlinkRestore()
    {
        rule_.setPostlinkRestore();
    }

    public void swap(SourceLinkRuleResource to)
    {
        rule_.swap(to.rule_);
    }

    public AttributeList attributes()
    {
        return rule_.attributes();
    }

    public ResultElementSpec resultElementSpec()
    {
        return rule_.resultElementSpec();
    }

    public LinkSet? uselink()
    {
        return rule_.uselink();
    }

    public LinkSet? postlink()
    {
        return rule_.postlink();
    }

    public Boolean postlinkRestore()
    {
        return rule_.postlinkRestore();
    }
}

// Link Set
public class LinkSet : Named
{
    private Boolean defined_;
    // indexed by typeIndex of source elements
    private Vector<Vector<ConstPtr<SourceLinkRuleResource>>> linkRules_ =
        new Vector<Vector<ConstPtr<SourceLinkRuleResource>>>();
    private Vector<ResultElementSpec> impliedSourceLinkRules_ =
        new Vector<ResultElementSpec>();

    // LinkSet(const StringC &, const Dtd *);
    public LinkSet(StringC name, Dtd? dtd)
        : base(name)
    {
        defined_ = false;
        nuint size = dtd != null ? dtd.nElementTypeIndex() : 0;
        linkRules_.resize(size);
        for (nuint i = 0; i < size; i++)
            linkRules_[i] = new Vector<ConstPtr<SourceLinkRuleResource>>();
    }

    // virtual ~LinkSet();
    // C# GC handles cleanup

    // void setDefined();
    public void setDefined()
    {
        defined_ = true;
    }

    // Boolean defined() const;
    public Boolean defined()
    {
        return defined_;
    }

    // void addImplied(const ElementType *, AttributeList &);
    public void addImplied(ElementType? element, AttributeList attributes)
    {
        impliedSourceLinkRules_.resize(impliedSourceLinkRules_.size() + 1);
        ResultElementSpec result = impliedSourceLinkRules_.back();
        result.elementType = element;
        result.attributeList = attributes;
    }

    // size_t nLinkRules(const ElementType *) const;
    public nuint nLinkRules(ElementType e)
    {
        if (e.index() >= linkRules_.size())
            return 0;
        return linkRules_[e.index()].size();
    }

    // const SourceLinkRule &linkRule(const ElementType *, size_t) const;
    public SourceLinkRuleResource? linkRule(ElementType e, nuint i)
    {
        return linkRules_[e.index()][i].pointer();
    }

    // void addLinkRule(const ElementType *, const ConstPtr<SourceLinkRuleResource> &);
    public void addLinkRule(ElementType element, ConstPtr<SourceLinkRuleResource> rule)
    {
        linkRules_[element.index()].push_back(rule);
    }

    // size_t nImpliedLinkRules() const;
    public nuint nImpliedLinkRules()
    {
        return impliedSourceLinkRules_.size();
    }

    // const ResultElementSpec &impliedLinkRule(size_t) const;
    public ResultElementSpec impliedLinkRule(nuint i)
    {
        return impliedSourceLinkRules_[i];
    }

    // Boolean impliedResultAttributes(const ElementType *, const AttributeList *&);
    public Boolean impliedResultAttributes(ElementType resultType, out AttributeList? attributes)
    {
        for (nuint i = 0; i < impliedSourceLinkRules_.size(); i++)
        {
            if (impliedSourceLinkRules_[i].elementType == resultType)
            {
                attributes = impliedSourceLinkRules_[i].attributeList;
                return true;
            }
        }
        attributes = null;
        return false;
    }
}

// ID link rule - extends SourceLinkRule with associated element types
public class IdLinkRule : SourceLinkRule
{
    private Vector<ElementType?> assocElementTypes_ = new Vector<ElementType?>();

    // IdLinkRule();
    public IdLinkRule()
    {
    }

    // Boolean isAssociatedWith(const ElementType *) const;
    public Boolean isAssociatedWith(ElementType e)
    {
        for (nuint i = 0; i < assocElementTypes_.size(); i++)
        {
            if (assocElementTypes_[i] == e)
                return true;
        }
        return false;
    }

    // void setAssocElementTypes(Vector<const ElementType *> &);
    public void setAssocElementTypes(Vector<ElementType?> v)
    {
        v.swap(assocElementTypes_);
    }

    // void swap(IdLinkRule &);
    public void swap(IdLinkRule to)
    {
        base.swap(to);
        assocElementTypes_.swap(to.assocElementTypes_);
    }
}

// A collection of link rules in an ID link set that are
// associated with the same name (unique identifier).
public class IdLinkRuleGroup : Named
{
    private Vector<IdLinkRule> linkRules_ = new Vector<IdLinkRule>();

    // IdLinkRuleGroup(const StringC &);
    public IdLinkRuleGroup(StringC name)
        : base(name)
    {
    }

    // size_t nLinkRules() const;
    public nuint nLinkRules()
    {
        return linkRules_.size();
    }

    // const IdLinkRule &linkRule(size_t) const;
    public IdLinkRule linkRule(nuint i)
    {
        return linkRules_[i];
    }

    // void addLinkRule(IdLinkRule &);
    public void addLinkRule(IdLinkRule rule)
    {
        linkRules_.resize(linkRules_.size() + 1);
        rule.swap(linkRules_.back());
    }
}

// An implicit or explicit LPD.
public class ComplexLpd : Lpd
{
    private Ptr<Dtd> resultDtd_ = new Ptr<Dtd>();
    private Vector<ConstPtr<AttributeDefinitionList>> linkAttributeDefs_ =
        new Vector<ConstPtr<AttributeDefinitionList>>();
    private NamedTable<LinkSet> linkSetTable_ = new NamedTable<LinkSet>();
    private LinkSet initialLinkSet_;
    private LinkSet emptyLinkSet_;
    private Boolean hadIdLinkSet_;
    private NamedTable<IdLinkRuleGroup> idLinkTable_ = new NamedTable<IdLinkRuleGroup>();
    private nuint nAttributeDefinitionList_;

    // ComplexLpd(const StringC &, Type, const Location &, const Syntax &,
    //            const Ptr<Dtd> &sourceDtd, const Ptr<Dtd> &resultDtd);
    public ComplexLpd(StringC name, Type type, Location location, Syntax syntax,
                      Ptr<Dtd> sourceDtd, Ptr<Dtd> resultDtd)
        : base(name, type, location, sourceDtd)
    {
        resultDtd_ = resultDtd;
        hadIdLinkSet_ = false;
        nAttributeDefinitionList_ = 0;
        initialLinkSet_ = new LinkSet(syntax.rniReservedName(Syntax.ReservedName.rINITIAL),
                                       sourceDtd.pointer());
        emptyLinkSet_ = new LinkSet(syntax.rniReservedName(Syntax.ReservedName.rEMPTY),
                                     sourceDtd.pointer());
        nuint size = sourceDtd.isNull() ? 0 : sourceDtd.pointer()!.nElementTypeIndex();
        linkAttributeDefs_.resize(size);
    }

    // virtual ~ComplexLpd();
    // C# GC handles cleanup

    // size_t allocAttributeDefinitionListIndex();
    public nuint allocAttributeDefinitionListIndex()
    {
        return nAttributeDefinitionList_++;
    }

    // size_t nAttributeDefinitionList() const;
    public nuint nAttributeDefinitionList()
    {
        return nAttributeDefinitionList_;
    }

    // LinkSet *initialLinkSet();
    public LinkSet initialLinkSet()
    {
        return initialLinkSet_;
    }

    // const LinkSet *initialLinkSet() const;
    public LinkSet initialLinkSetConst()
    {
        return initialLinkSet_;
    }

    // const LinkSet *emptyLinkSet() const;
    public LinkSet emptyLinkSet()
    {
        return emptyLinkSet_;
    }

    // const LinkSet *lookupLinkSet(const StringC &) const;
    public LinkSet? lookupLinkSetConst(StringC name)
    {
        return linkSetTable_.lookup(name);
    }

    // LinkSet *lookupLinkSet(const StringC &);
    public LinkSet? lookupLinkSet(StringC name)
    {
        return linkSetTable_.lookup(name);
    }

    // LinkSet *insertLinkSet(LinkSet *);
    public LinkSet? insertLinkSet(LinkSet? e)
    {
        return linkSetTable_.insert(e);
    }

    // const IdLinkRuleGroup *lookupIdLink(const StringC &) const;
    public IdLinkRuleGroup? lookupIdLink(StringC id)
    {
        return idLinkTable_.lookup(id);
    }

    // IdLinkRuleGroup *lookupCreateIdLink(const StringC &);
    public IdLinkRuleGroup lookupCreateIdLink(StringC id)
    {
        IdLinkRuleGroup? group = idLinkTable_.lookup(id);
        if (group == null)
        {
            group = new IdLinkRuleGroup(id);
            idLinkTable_.insert(group);
        }
        return group;
    }

    // void insertIdLink(IdLinkRuleGroup *);
    public void insertIdLink(IdLinkRuleGroup? group)
    {
        idLinkTable_.insert(group);
    }

    // ConstLinkSetIter linkSetIter() const;
    public ConstNamedTableIter<LinkSet> linkSetIter()
    {
        return new ConstNamedTableIter<LinkSet>(linkSetTable_);
    }

    // Boolean hadIdLinkSet() const;
    public Boolean hadIdLinkSet()
    {
        return hadIdLinkSet_;
    }

    // void setHadIdLinkSet();
    public void setHadIdLinkSet()
    {
        hadIdLinkSet_ = true;
    }

    // const Ptr<Dtd> &resultDtd();
    public Ptr<Dtd> resultDtd()
    {
        return resultDtd_;
    }

    // ConstPtr<Dtd> resultDtd() const;
    public ConstPtr<Dtd> resultDtdConst()
    {
        return new ConstPtr<Dtd>(resultDtd_.pointer());
    }

    // const ConstPtr<AttributeDefinitionList> &attributeDef(const ElementType *) const;
    public ConstPtr<AttributeDefinitionList> attributeDef(ElementType e)
    {
        return linkAttributeDefs_[e.index()];
    }

    // void setAttributeDef(const ElementType *, const ConstPtr<AttributeDefinitionList> &);
    public void setAttributeDef(ElementType e, ConstPtr<AttributeDefinitionList> attdef)
    {
        linkAttributeDefs_[e.index()] = attdef;
    }
}
