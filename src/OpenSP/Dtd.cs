// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Document Type Definition
public class Dtd : Resource
{
    // Type aliases to match C++ typedefs
    // typedef NamedTableIter<ElementType> ElementTypeIter;
    // typedef ConstNamedTableIter<ElementType> ConstElementTypeIter;
    // typedef NamedTableIter<RankStem> RankStemIter;
    // typedef ConstNamedTableIter<RankStem> ConstRankStemIter;
    // typedef NamedTableIter<ShortReferenceMap> ShortReferenceMapIter;
    // typedef ConstNamedResourceTableIter<Notation> ConstNotationIter;
    // typedef NamedResourceTableIter<Notation> NotationIter;
    // typedef ConstNamedResourceTableIter<Entity> ConstEntityIter;
    // typedef NamedResourceTableIter<Entity> EntityIter;

    private NamedResourceTable<Entity> generalEntityTable_ = new NamedResourceTable<Entity>();
    private NamedResourceTable<Entity> parameterEntityTable_ = new NamedResourceTable<Entity>();
    private ConstPtr<Entity> defaultEntity_ = new ConstPtr<Entity>();
    private ConstPtr<StringResource<Char>> name_ = new ConstPtr<StringResource<Char>>();
    private NamedTable<ElementType> elementTypeTable_ = new NamedTable<ElementType>();
    private NamedTable<RankStem> rankStemTable_ = new NamedTable<RankStem>();
    private NamedTable<ShortReferenceMap> shortReferenceMapTable_ = new NamedTable<ShortReferenceMap>();
    private NamedResourceTable<Notation> notationTable_ = new NamedResourceTable<Notation>();
    private nuint nCurrentAttribute_;
    private nuint nElementDefinition_;
    private nuint nAttributeDefinitionList_;
    private nuint nElementType_;
    private ElementType? documentElementType_;
    private Vector<StringC> shortrefs_ = new Vector<StringC>();
    private HashTable<int> shortrefTable_ = new HashTable<int>();
    private Boolean isBase_;
    private Boolean isInstantiated_;
    private Ptr<AttributeDefinitionList> implicitElementAttributeDef_ = new Ptr<AttributeDefinitionList>();
    private Ptr<AttributeDefinitionList> implicitNotationAttributeDef_ = new Ptr<AttributeDefinitionList>();

    // Dtd(const StringC &name, Boolean isBase);
    public Dtd(StringC name, Boolean isBase)
    {
        name_ = new ConstPtr<StringResource<Char>>(new StringResource<Char>(name));
        nCurrentAttribute_ = 0;
        nElementDefinition_ = 0;
        nAttributeDefinitionList_ = 0;
        nElementType_ = 0;
        isBase_ = isBase;
        isInstantiated_ = false;
        documentElementType_ = new ElementType(name, allocElementTypeIndex());
        insertElementType(documentElementType_);
    }

    // ~Dtd();
    // C# GC handles cleanup

    // ConstPtr<Entity> lookupEntity(Boolean isParameter, const StringC &) const;
    public ConstPtr<Entity> lookupEntity(Boolean isParameter, StringC name)
    {
        return (isParameter ? parameterEntityTable_ : generalEntityTable_).lookupConst(name);
    }

    // const Entity *lookupEntityTemp(Boolean isParameter, const StringC &) const;
    public Entity? lookupEntityTemp(Boolean isParameter, StringC name)
    {
        return (isParameter ? parameterEntityTable_ : generalEntityTable_).lookupTemp(name);
    }

    // Ptr<Entity> lookupEntity(Boolean isParameter, const StringC &);
    public Ptr<Entity> lookupEntityMut(Boolean isParameter, StringC name)
    {
        return (isParameter ? parameterEntityTable_ : generalEntityTable_).lookup(name);
    }

    // Ptr<Entity> insertEntity(const Ptr<Entity> &, Boolean replace = 0);
    public Ptr<Entity> insertEntity(Ptr<Entity> entity, Boolean replace = false)
    {
        Entity? ent = entity.pointer();
        if (ent != null &&
            (ent.declType() == EntityDecl.DeclType.parameterEntity ||
             ent.declType() == EntityDecl.DeclType.doctype))
        {
            return parameterEntityTable_.insert(entity, replace);
        }
        else
        {
            return generalEntityTable_.insert(entity, replace);
        }
    }

    // Ptr<Entity> removeEntity(Boolean isParameter, const StringC &);
    public Ptr<Entity> removeEntity(Boolean isParameter, StringC name)
    {
        return (isParameter ? parameterEntityTable_ : generalEntityTable_).remove(name);
    }

    // ConstEntityIter generalEntityIter() const;
    public ConstNamedResourceTableIter<Entity> generalEntityIterConst()
    {
        return new ConstNamedResourceTableIter<Entity>(generalEntityTable_);
    }

    // EntityIter generalEntityIter();
    public NamedResourceTableIter<Entity> generalEntityIter()
    {
        return new NamedResourceTableIter<Entity>(generalEntityTable_);
    }

    // ConstEntityIter parameterEntityIter() const;
    public ConstNamedResourceTableIter<Entity> parameterEntityIterConst()
    {
        return new ConstNamedResourceTableIter<Entity>(parameterEntityTable_);
    }

    // EntityIter parameterEntityIter();
    public NamedResourceTableIter<Entity> parameterEntityIter()
    {
        return new NamedResourceTableIter<Entity>(parameterEntityTable_);
    }

    // ConstPtr<Entity> defaultEntity() const;
    public ConstPtr<Entity> defaultEntity()
    {
        return defaultEntity_;
    }

    // const Entity *defaultEntityTemp() const;
    public Entity? defaultEntityTemp()
    {
        return defaultEntity_.pointer();
    }

    // void setDefaultEntity(const Ptr<Entity> &, ParserState &);
    public void setDefaultEntity(Ptr<Entity> entity, ParserState parser)
    {
        defaultEntity_ = new ConstPtr<Entity>(entity.pointer());

        Entity? ent = entity.pointer();
        if (ent != null && ent.declInActiveLpd())
        {
            NamedResourceTable<Entity> tem = new NamedResourceTable<Entity>();
            {
                NamedResourceTableIter<Entity> iter = new NamedResourceTableIter<Entity>(generalEntityTable_);
                for (;;)
                {
                    Ptr<Entity> old = iter.next();
                    if (old.isNull())
                        break;
                    Entity? oldEnt = old.pointer();
                    if (oldEnt != null && oldEnt.defaulted())
                    {
                        Entity? defEnt = defaultEntity_.pointer();
                        if (defEnt != null)
                        {
                            Entity e = defEnt.copy();
                            e.setDefaulted();
                            e.setName(oldEnt.name());
                            e.generateSystemId(parser);
                            tem.insert(new Ptr<Entity>(e));
                        }
                    }
                }
            }
            {
                NamedResourceTableIter<Entity> iter = new NamedResourceTableIter<Entity>(tem);
                for (;;)
                {
                    Ptr<Entity> e = iter.next();
                    if (e.isNull())
                        break;
                    generalEntityTable_.insert(e, true);
                }
            }
        }
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

    // const ElementType *lookupElementType(const StringC &) const;
    public ElementType? lookupElementType(StringC name)
    {
        return elementTypeTable_.lookup(name);
    }

    // ElementType *removeElementType(const StringC &);
    public ElementType? removeElementType(StringC name)
    {
        return elementTypeTable_.remove(name);
    }

    // ElementType *insertElementType(ElementType *);
    public ElementType? insertElementType(ElementType? e)
    {
        return elementTypeTable_.insert(e);
    }

    // size_t nElementTypeIndex() const;
    public nuint nElementTypeIndex()
    {
        // index 0 is reserved for #pcdata
        return 1 + nElementType_;
    }

    // size_t allocElementTypeIndex();
    public nuint allocElementTypeIndex()
    {
        return 1 + nElementType_++;
    }

    // ConstElementTypeIter elementTypeIter() const;
    public ConstNamedTableIter<ElementType> elementTypeIterConst()
    {
        return new ConstNamedTableIter<ElementType>(elementTypeTable_);
    }

    // ElementTypeIter elementTypeIter();
    public NamedTableIter<ElementType> elementTypeIter()
    {
        return new NamedTableIter<ElementType>(elementTypeTable_);
    }

    // const RankStem *lookupRankStem(const StringC &) const;
    public RankStem? lookupRankStem(StringC name)
    {
        return rankStemTable_.lookup(name);
    }

    // RankStem *insertRankStem(RankStem *);
    public RankStem? insertRankStem(RankStem? e)
    {
        return rankStemTable_.insert(e);
    }

    // size_t nRankStem() const;
    public nuint nRankStem()
    {
        return rankStemTable_.count();
    }

    // ConstRankStemIter rankStemIter() const;
    public ConstNamedTableIter<RankStem> rankStemIterConst()
    {
        return new ConstNamedTableIter<RankStem>(rankStemTable_);
    }

    // RankStemIter rankStemIter();
    public NamedTableIter<RankStem> rankStemIter()
    {
        return new NamedTableIter<RankStem>(rankStemTable_);
    }

    // const ShortReferenceMap *lookupShortReferenceMap(const StringC &) const;
    public ShortReferenceMap? lookupShortReferenceMap(StringC name)
    {
        return shortReferenceMapTable_.lookup(name);
    }

    // ShortReferenceMap *insertShortReferenceMap(ShortReferenceMap *);
    public ShortReferenceMap? insertShortReferenceMap(ShortReferenceMap? map)
    {
        return shortReferenceMapTable_.insert(map);
    }

    // ShortReferenceMapIter shortReferenceMapIter();
    public NamedTableIter<ShortReferenceMap> shortReferenceMapIter()
    {
        return new NamedTableIter<ShortReferenceMap>(shortReferenceMapTable_);
    }

    // Boolean shortrefIndex(const StringC &, const Syntax &, size_t &index);
    public Boolean shortrefIndex(StringC str, Syntax syntax, out nuint index)
    {
        int? indexP = shortrefTable_.lookup(str);
        if (indexP.HasValue)
        {
            index = (nuint)indexP.Value;
            return true;
        }
        if (!syntax.isValidShortref(str))
        {
            index = 0;
            return false;
        }
        shortrefTable_.insert(str, (int)shortrefs_.size());
        index = shortrefs_.size();
        shortrefs_.push_back(str);
        return true;
    }

    // size_t nShortref() const;
    public nuint nShortref()
    {
        return shortrefs_.size();
    }

    // const StringC &shortref(size_t i) const;
    public StringC shortref(nuint i)
    {
        return shortrefs_[i];
    }

    // void addNeededShortref(const StringC &);
    public void addNeededShortref(StringC str)
    {
        int val;
        if (!shortrefTable_.tryLookup(str, out val))
        {
            shortrefTable_.insert(str, (int)shortrefs_.size());
            shortrefs_.push_back(str);
        }
    }

    // ConstPtr<Notation> lookupNotation(const StringC &) const;
    public ConstPtr<Notation> lookupNotationConst(StringC name)
    {
        return notationTable_.lookupConst(name);
    }

    // const Notation *lookupNotationTemp(const StringC &) const;
    public Notation? lookupNotationTemp(StringC name)
    {
        return notationTable_.lookupTemp(name);
    }

    // Ptr<Notation> lookupNotation(const StringC &);
    public Ptr<Notation> lookupNotation(StringC name)
    {
        return notationTable_.lookup(name);
    }

    // Ptr<Notation> insertNotation(const Ptr<Notation> &);
    public Ptr<Notation> insertNotation(Ptr<Notation> nt)
    {
        return notationTable_.insert(nt);
    }

    // Ptr<Notation> removeNotation(const StringC &);
    public Ptr<Notation> removeNotation(StringC name)
    {
        return notationTable_.remove(name);
    }

    // ConstNotationIter notationIter() const;
    public ConstNamedResourceTableIter<Notation> notationIterConst()
    {
        return new ConstNamedResourceTableIter<Notation>(notationTable_);
    }

    // NotationIter notationIter();
    public NamedResourceTableIter<Notation> notationIter()
    {
        return new NamedResourceTableIter<Notation>(notationTable_);
    }

    // size_t allocCurrentAttributeIndex();
    public nuint allocCurrentAttributeIndex()
    {
        return nCurrentAttribute_++;
    }

    // size_t nCurrentAttribute() const;
    public nuint nCurrentAttribute()
    {
        return nCurrentAttribute_;
    }

    // size_t allocElementDefinitionIndex();
    public nuint allocElementDefinitionIndex()
    {
        return nElementDefinition_++;
    }

    // size_t nElementDefinition() const;
    public nuint nElementDefinition()
    {
        return nElementDefinition_;
    }

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

    // const ElementType *documentElementType() const;
    public ElementType? documentElementType()
    {
        return documentElementType_;
    }

    // Boolean isBase() const;
    public Boolean isBase()
    {
        return isBase_;
    }

    // Boolean isInstantiated() const;
    public Boolean isInstantiated()
    {
        return isInstantiated_;
    }

    // void instantiate();
    public void instantiate()
    {
        isInstantiated_ = true;
    }

    // Ptr<AttributeDefinitionList> &implicitElementAttributeDef();
    public Ptr<AttributeDefinitionList> implicitElementAttributeDef()
    {
        return implicitElementAttributeDef_;
    }

    // void setImplicitElementAttributeDef(const Ptr<AttributeDefinitionList> &);
    public void setImplicitElementAttributeDef(Ptr<AttributeDefinitionList> def)
    {
        implicitElementAttributeDef_ = def;
    }

    // Ptr<AttributeDefinitionList> &implicitNotationAttributeDef();
    public Ptr<AttributeDefinitionList> implicitNotationAttributeDef()
    {
        return implicitNotationAttributeDef_;
    }

    // void setImplicitNotationAttributeDef(const Ptr<AttributeDefinitionList> &);
    public void setImplicitNotationAttributeDef(Ptr<AttributeDefinitionList> def)
    {
        implicitNotationAttributeDef_ = def;
    }
}
