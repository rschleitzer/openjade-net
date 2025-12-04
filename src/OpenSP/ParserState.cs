// Copyright (c) 1994 James Clark, 2000 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

public class ParserState : ContentState
{
    public enum Phase
    {
        noPhase,
        initPhase,
        prologPhase,
        declSubsetPhase,
        instanceStartPhase,
        contentPhase
    }

    private static readonly Location nullLocation_ = new Location();
#pragma warning disable CS0414 // Field is assigned but never used
    private static int dummyCancel_ = 0;
#pragma warning restore CS0414

    private ParserOptions options_;
    private EventHandler? handler_;
    private Pass1EventHandler pass1Handler_ = new Pass1EventHandler();
    private Boolean allowPass2_;
    private Offset pass2StartOffset_;
    private Boolean hadPass2Start_;
    private EventQueue eventQueue_ = new EventQueue();
    private OutputState outputState_ = new OutputState();
    private ConstPtr<Syntax> prologSyntax_ = new ConstPtr<Syntax>();
    private ConstPtr<Syntax> instanceSyntax_ = new ConstPtr<Syntax>();
    private ConstPtr<Sd> sd_ = new ConstPtr<Sd>();
    private uint subdocLevel_;
    private Ptr<EntityManager> entityManager_ = new Ptr<EntityManager>();
    private ConstPtr<EntityCatalog> entityCatalog_ = new ConstPtr<EntityCatalog>();
    private Phase phase_;
    private Phase finalPhase_;
    private Boolean inInstance_;
    private Boolean inStartTag_;
    private Boolean inEndTag_;
    private Ptr<Dtd> defDtd_ = new Ptr<Dtd>();
    private Ptr<Lpd> defLpd_ = new Ptr<Lpd>();
    private Vector<ConstPtr<Lpd>> allLpd_ = new Vector<ConstPtr<Lpd>>();
    private Vector<ConstPtr<Lpd>> lpd_ = new Vector<ConstPtr<Lpd>>(); // active LPDs
    private Vector<StringC> activeLinkTypes_ = new Vector<StringC>();
    private Boolean activeLinkTypesSubsted_;
    private Boolean hadLpd_;
    private Boolean resultAttributeSpecMode_;
    private Boolean pass2_;
    // OwnerTable for LpdEntityRef
    private Dictionary<StringC, LpdEntityRef> lpdEntityRefs_ = new Dictionary<StringC, LpdEntityRef>();
    // external entity to be referenced at the end of the declaration subset
    private ConstPtr<Entity> dsEntity_ = new ConstPtr<Entity>();
    private Allocator eventAllocator_;
    private Allocator internalAllocator_;
    private Vector<Owner<AttributeList>> attributeLists_ = new Vector<Owner<AttributeList>>();
    private StringC nameBuffer_ = new StringC();
    private Boolean keepingMessages_;
    private IQueue<MessageEvent> keptMessages_ = new IQueue<MessageEvent>();
    private Mode currentMode_;
    private Boolean pcdataRecovering_;
    // if in a special parse (cdata, rcdata, ignore), the input level
    // at which the special parse started.
    private uint specialParseInputLevel_;
    private Mode specialParseMode_;
    private uint markedSectionLevel_;
    private uint markedSectionSpecialLevel_;
    private Vector<Location> markedSectionStartLocation_ = new Vector<Location>();
    private ConstPtr<Recognizer>[] recognizers_ = new ConstPtr<Recognizer>[ModeConstants.nModes];
    private XcharMap<PackedBoolean> normalMap_ = new XcharMap<PackedBoolean>();
    private uint inputLevel_;
    private IList<InputSource> inputStack_ = new IList<InputSource>();
    private Vector<uint> inputLevelElementIndex_ = new Vector<uint>();
    private Ptr<Dtd> currentDtd_ = new Ptr<Dtd>();
    private ConstPtr<Dtd> currentDtdConst_ = new ConstPtr<Dtd>();
    private Vector<Ptr<Dtd>> dtd_ = new Vector<Ptr<Dtd>>();
    private Ptr<Dtd> pass1Dtd_ = new Ptr<Dtd>();
    private uint instantiatedDtds_;
    private ConstPtr<Syntax> syntax_ = new ConstPtr<Syntax>();
    private Vector<StringC> currentRank_ = new Vector<StringC>();
    private NamedTable<Id> idTable_ = new NamedTable<Id>();
    private NamedResourceTable<Entity> instanceDefaultedEntityTable_ = new NamedResourceTable<Entity>();
    private NamedResourceTable<Entity> undefinedEntityTable_ = new NamedResourceTable<Entity>();
    private Vector<ConstPtr<AttributeValue>> currentAttributes_ = new Vector<ConstPtr<AttributeValue>>();
    private Markup? currentMarkup_;
    private Markup markup_ = new Markup();
    private Location markupLocation_ = new Location();
    private Boolean hadAfdrDecl_;
    private Sd.ImplydefElement implydefElement_;
    private Boolean implydefAttlist_;
    private int cancelPtr_;
    private bool useExternalCancel_;

    // ParserState(const Ptr<EntityManager> &em, const ParserOptions &opt,
    //             unsigned subdocLevel, Phase finalPhase);
    public ParserState(Ptr<EntityManager> em,
                       ParserOptions opt,
                       uint subdocLevel,
                       Phase finalPhase)
    {
        entityManager_ = em;
        options_ = opt;
        inInstance_ = false;
        inStartTag_ = false;
        inEndTag_ = false;
        keepingMessages_ = false;
        // Calculate max event size - simplified for C#
        eventAllocator_ = new Allocator(256, 50);
        internalAllocator_ = new Allocator(256, 50);
        handler_ = eventQueue_;
        subdocLevel_ = subdocLevel;
        inputLevel_ = 0;
        specialParseInputLevel_ = 0;
        markedSectionLevel_ = 0;
        markedSectionSpecialLevel_ = 0;
        currentMode_ = Mode.proMode;
        hadLpd_ = false;
        resultAttributeSpecMode_ = false;
        pass2_ = false;
        activeLinkTypesSubsted_ = false;
        allowPass2_ = false;
        hadPass2Start_ = false;
        pcdataRecovering_ = false;
        currentMarkup_ = null;
        cancelPtr_ = 0;
        useExternalCancel_ = false;
        finalPhase_ = finalPhase;
        hadAfdrDecl_ = false;
        instantiatedDtds_ = 0;

        for (int i = 0; i < ModeConstants.nModes; i++)
            recognizers_[i] = new ConstPtr<Recognizer>();
    }

    // void inheritActiveLinkTypes(const ParserState &parent);
    public void inheritActiveLinkTypes(ParserState parent)
    {
        activeLinkTypes_ = parent.activeLinkTypes_;
        activeLinkTypesSubsted_ = parent.activeLinkTypesSubsted_;
    }

    // void allDone();
    public void allDone()
    {
        phase_ = Phase.noPhase;
    }

    // void setPass2Start();
    public void setPass2Start()
    {
        // ASSERT(inputLevel_ == 1);
        if (hadPass2Start_)
            return;
        hadPass2Start_ = true;
        if (!pass2() && sd().link() && activeLinkTypes_.size() > 0)
        {
            allowPass2_ = true;
            pass1Handler_.init(handler_!);
            handler_ = pass1Handler_;
            InputSourceOrigin? p = currentLocation().origin().pointer()?.asInputSourceOrigin();
            if (p != null)
                pass2StartOffset_ = p.startOffset(currentLocation().index());
        }
        else
        {
            allowPass2_ = false;
            currentInput()?.willNotRewind();
        }
    }

    // void allLinkTypesActivated();
    public void allLinkTypesActivated()
    {
        if (activeLinkTypes_.size() == 0 && inputLevel_ == 1)
            currentInput()?.willNotRewind();
    }

    // Boolean maybeStartPass2();
    public Boolean maybeStartPass2()
    {
        if (pass2_ || !allowPass2_)
            return false;
        handler_ = pass1Handler_.origHandler();
        if (nActiveLink() == 0 || pass1Handler_.hadError())
        {
            while (!pass1Handler_.empty())
            {
                if (cancelled())
                    return false;
                pass1Handler_.get()?.handle(handler_!);
            }
            InputSource? top = null;
            IListIter<InputSource> iter = new IListIter<InputSource>(inputStack_);
            for (; iter.done() == 0; iter.next())
                top = iter.cur();
            if (top != null)
                top.willNotRewind();
            return false;
        }
        pass1Handler_.clear();
        while (inputLevel_ > 1)
        {
            InputSource? p = inputStack_.get();
            inputLevel_--;
            // delete p - handled by GC
        }
        // Caller will call allDone() if inputLevel_ is 0.
        if (inputLevel_ == 0)
            return false;
        if (!inputStack_.head()!.rewind(this))
        {
            inputLevel_ = 0;
            inputStack_.get();
            return false;
        }
        inputStack_.head()!.willNotRewind();
        for (; pass2StartOffset_ > 0; pass2StartOffset_--)
            if (inputStack_.head()!.get(messenger()) == InputSource.eE)
            {
                message(ParserMessages.pass2Ee);
                inputLevel_ = 0;
                inputStack_.get();
                return false;
            }
        specialParseInputLevel_ = 0;
        markedSectionLevel_ = 0;
        markedSectionSpecialLevel_ = 0;
        currentMode_ = Mode.proMode;
        hadLpd_ = false;
        allowPass2_ = false;
        hadPass2Start_ = false;
        currentMarkup_ = null;
        inputLevel_ = 1;
        inInstance_ = false;
        inStartTag_ = false;
        inEndTag_ = false;
        defDtd_.clear();
        defLpd_.clear();
        dtd_[0].swap(pass1Dtd_);
        dtd_.clear();
        dsEntity_.clear();
        currentDtd_.clear();
        currentDtdConst_.clear();
        phase_ = Phase.noPhase;
        pass2_ = true;
        lpd_.clear();
        allLpd_.clear();
        return true;
    }

    // Boolean referenceDsEntity(const Location &loc);
    public Boolean referenceDsEntity(Location loc)
    {
        if (dsEntity_.isNull())
            return false;
        Ptr<EntityOrigin> origin = new Ptr<EntityOrigin>(EntityOrigin.make(internalAllocator(), dsEntity_, loc));
        dsEntity_.pointer()!.dsReference(this, origin);
        dsEntity_.clear();
        return inputLevel() > 1;
    }

    // void startDtd(const StringC &name);
    public void startDtd(StringC name)
    {
        defDtd_ = new Ptr<Dtd>(new Dtd(name, dtd_.size() == 0));
        defLpd_.clear();
        for (nuint i = 0; i < options().includes.size(); i++)
        {
            StringC inclName = options().includes[i];
            syntax().entitySubstTable()!.subst(inclName);
            Text text = new Text();
            text.addChars(syntax().reservedName(Syntax.ReservedName.rINCLUDE), new Location());
            Entity entity = new InternalTextEntity(inclName,
                                                   Entity.DeclType.parameterEntity,
                                                   new Location(),
                                                   text,
                                                   InternalTextEntity.Bracketed.none);
            entity.setUsed();
            defDtd_.pointer()!.insertEntity(new Ptr<Entity>(entity));
        }
        nuint nEntities = instanceSyntax_.pointer()!.nEntities();
        for (nuint i = 0; i < nEntities; i++)
        {
            Text text = new Text();
            text.addChar(instanceSyntax_.pointer()!.entityChar(i), new Location());
            Entity entity = new PredefinedEntity(instanceSyntax_.pointer()!.entityName(i),
                                                 new Location(),
                                                 text);
            defDtd_.pointer()!.insertEntity(new Ptr<Entity>(entity));
        }
        currentDtd_ = defDtd_;
        currentDtdConst_ = new ConstPtr<Dtd>(defDtd_.pointer());
        currentMode_ = Mode.dsMode;
    }

    // void enterTag(Boolean start);
    public void enterTag(Boolean start)
    {
        if (start)
            inStartTag_ = true;
        else
            inEndTag_ = true;
    }

    // void leaveTag();
    public void leaveTag()
    {
        inStartTag_ = false;
        inEndTag_ = false;
    }

    // Boolean inTag(Boolean &start) const;
    public Boolean inTag(ref Boolean start)
    {
        start = inStartTag_;
        return inStartTag_ || inEndTag_;
    }

    // void endDtd();
    public void endDtd()
    {
        // In C++, push_back copies the Ptr (value semantics)
        // In C#, Ptr is a reference type, so we need to create an explicit copy
        dtd_.push_back(new Ptr<Dtd>(defDtd_));
        defDtd_.clear();
        currentDtd_.clear();
        currentDtdConst_.clear();
        currentMode_ = Mode.proMode;
    }

    // void startLpd(Ptr<Lpd> &lpd);
    public void startLpd(Ptr<Lpd> lpd)
    {
        defLpd_ = lpd;
        defDtd_ = defLpd_.pointer()!.sourceDtd();
        currentDtd_ = defLpd_.pointer()!.sourceDtd();
        currentDtdConst_ = new ConstPtr<Dtd>(defLpd_.pointer()!.sourceDtd().pointer());
        currentMode_ = Mode.dsMode;
    }

    // void endLpd();
    public void endLpd()
    {
        hadLpd_ = true;
        if (defLpd_.pointer()!.active())
            lpd_.push_back(new ConstPtr<Lpd>(defLpd_.pointer()));
        allLpd_.push_back(new ConstPtr<Lpd>(defLpd_.pointer()));
        defLpd_.clear();
        currentDtd_.clear();
        currentDtdConst_.clear();
        currentMode_ = Mode.proMode;
    }

    // void popInputStack();
    public void popInputStack()
    {
        // ASSERT(inputLevel_ > 0);
        InputSource? p = inputStack_.get();

        if (handler_ != null && inputLevel_ > 1)
        {
            handler_.inputClosed(p);
        }

        inputLevel_--;
        // delete p - handled by GC
        if (specialParseInputLevel_ > 0 && inputLevel_ == specialParseInputLevel_)
            currentMode_ = specialParseMode_;
        if (currentMode_ == Mode.dsiMode
            && inputLevel_ == 1
            && markedSectionLevel_ == 0)
            currentMode_ = Mode.dsMode;
        if (inputLevelElementIndex_.size() > 0)
            inputLevelElementIndex_.resize(inputLevelElementIndex_.size() - 1);
    }

    // void setSd(ConstPtr<Sd> sd);
    public void setSd(ConstPtr<Sd> sd)
    {
        sd_ = sd;
        mayDefaultAttribute_ = sd_.pointer()!.omittag() || sd_.pointer()!.attributeDefault();
        validate_ = sd_.pointer()!.typeValid();
        implydefElement_ = sd_.pointer()!.implydefElement();
        implydefAttlist_ = sd_.pointer()!.implydefAttlist();
    }

    // void setSyntax(ConstPtr<Syntax> syntax);
    public void setSyntax(ConstPtr<Syntax> syntax)
    {
        syntax_ = syntax;
        prologSyntax_ = syntax;
        instanceSyntax_ = syntax;
    }

    // void setSyntaxes(ConstPtr<Syntax> prologSyntax, ConstPtr<Syntax> instanceSyntax);
    public void setSyntaxes(ConstPtr<Syntax> prologSyntax, ConstPtr<Syntax> instanceSyntax)
    {
        syntax_ = prologSyntax;
        prologSyntax_ = prologSyntax;
        instanceSyntax_ = instanceSyntax;
    }

    // void pushInput(InputSource *in);
    public void pushInput(InputSource? ins)
    {
        if (ins == null)
            return;

        if (handler_ != null && inputLevel_ > 0)
        {
            handler_.inputOpened(ins);
        }

        if (!syntax_.isNull() && syntax_.pointer()!.multicode())
            ins.setMarkupScanTable(syntax_.pointer()!.markupScanTable());
        inputStack_.insert(ins);
        inputLevel_++;
        if (specialParseInputLevel_ > 0 && inputLevel_ > specialParseInputLevel_)
            currentMode_ = Mode.rcconeMode; // mode for rcdata in an entity
        else if (currentMode_ == Mode.dsMode)
            currentMode_ = Mode.dsiMode;
        if (inInstance_ && sd().integrallyStored())
            inputLevelElementIndex_.push_back(tagLevel() > 0 ? (uint)currentElement().index() : 0);
    }

    // void startMarkedSection(const Location &loc);
    public void startMarkedSection(Location loc)
    {
        markedSectionLevel_++;
        markedSectionStartLocation_.push_back(loc);
        if (currentMode_ == Mode.dsMode)
            currentMode_ = Mode.dsiMode;
        if (markedSectionSpecialLevel_ > 0)
            markedSectionSpecialLevel_++;
    }

    // void startSpecialMarkedSection(Mode mode, const Location &loc);
    public void startSpecialMarkedSection(Mode mode, Location loc)
    {
        markedSectionLevel_++;
        markedSectionStartLocation_.push_back(loc);
        specialParseInputLevel_ = inputLevel_;
        markedSectionSpecialLevel_ = 1;
        specialParseMode_ = currentMode_ = mode;
    }

    // void endMarkedSection();
    public void endMarkedSection()
    {
        // ASSERT(markedSectionLevel_ > 0);
        markedSectionLevel_--;
        markedSectionStartLocation_.resize(markedSectionStartLocation_.size() - 1);
        if (markedSectionSpecialLevel_ > 0)
        {
            markedSectionSpecialLevel_--;
            if (markedSectionSpecialLevel_ > 0)
                return; // remain in imsMode
            specialParseInputLevel_ = 0;
            if (inInstance_)
                currentMode_ = contentMode();
            else
                currentMode_ = Mode.dsiMode;
        }
        if (currentMode_ == Mode.dsiMode
            && inputLevel_ == 1
            && markedSectionLevel_ == 0)
            currentMode_ = Mode.dsMode;
    }

    // void pushElement(OpenElement *e);
    public new void pushElement(OpenElement e)
    {
        base.pushElement(e);
        pcdataRecovering_ = false;
        // the start tag of this element may have been implied by data
        // inside a cdata or rcdata marked section
        if (markedSectionSpecialLevel_ == 0)
        {
            currentMode_ = contentMode();
            if (e.requiresSpecialParse())
            {
                specialParseMode_ = currentMode_;
                specialParseInputLevel_ = inputLevel_;
            }
        }
    }

    // PCDATA was encountered somewhere where it was not allowed.
    // Change the current mode to improve recovery.
    // void pcdataRecover();
    public void pcdataRecover()
    {
        switch (currentMode_)
        {
            case Mode.econMode:
                currentMode_ = Mode.mconMode;
                break;
            case Mode.econnetMode:
                currentMode_ = Mode.mconnetMode;
                break;
            default:
                break;
        }
        pcdataRecovering_ = true;
    }

    // OpenElement *popSaveElement();
    public new OpenElement? popSaveElement()
    {
        OpenElement? e = base.popSaveElement();
        // the end tag of this element may have been implied by data
        // inside a cdata or rcdata marked section
        if (markedSectionSpecialLevel_ == 0)
        {
            currentMode_ = contentMode();
            specialParseInputLevel_ = 0;
        }
        pcdataRecovering_ = false;
        return e;
    }

    // void popElement();
    public new void popElement()
    {
        popSaveElement();
        // delete handled by GC
    }

    // Boolean entityIsOpen(const EntityDecl *entityDecl) const;
    public Boolean entityIsOpen(EntityDecl? entityDecl)
    {
        IListIter<InputSource> iter = new IListIter<InputSource>(inputStack_);
        for (; iter.done() == 0; iter.next())
            if (iter.cur()?.currentLocation().origin().pointer()?.entityDecl() == entityDecl)
                return true;
        return false;
    }

    // void startInstance();
    public void startInstance()
    {
        if (!instanceSyntax_.isNull())
            syntax_ = instanceSyntax_;
        currentMode_ = Mode.econMode;

        currentDtd_.clear();
        for (nuint i = 0; i < dtd_.size(); i++)
        {
            if (shouldActivateLink(dtd_[i].pointer()!.name()))
            {
                if (nActiveLink() > 0)
                {
                    message(ParserMessages.activeDocLink);
                    break;
                }
                else if (!currentDtd_.isNull())
                {
                    message(ParserMessages.sorryActiveDoctypes);
                    break;
                }
                else
                    currentDtd_ = dtd_[i];
            }
        }
        if (currentDtd_.isNull())
            currentDtd_ = dtd_[0];
        currentDtdConst_ = new ConstPtr<Dtd>(currentDtd_.pointer());

        startContent(currentDtd());
        inInstance_ = true;
        if (sd().rank())
            currentRank_.assign(currentDtd().nRankStem(), new StringC());
        currentAttributes_.clear();
        currentAttributes_.resize(currentDtd().nCurrentAttribute());
        idTable_.clear();
    }

    // Id *lookupCreateId(const StringC &name);
    public Id lookupCreateId(StringC name)
    {
        Id? id = idTable_.lookup(name);
        if (id == null)
        {
            id = new Id(name);
            idTable_.insert(id);
        }
        return id;
    }

    // ConstPtr<Entity> lookupEntity(Boolean isParameter, const StringC &name,
    //                               const Location &useLocation, Boolean referenced);
    public ConstPtr<Entity> lookupEntity(Boolean isParameter,
                                         StringC name,
                                         Location useLocation,
                                         Boolean referenced)
    {
        Dtd? dtd;
        if (resultAttributeSpecMode_)
            dtd = defComplexLpd().resultDtd().pointer();
        else
            dtd = currentDtd_.pointer();
        if (dtd != null)
        {
            Ptr<Entity> entity = new Ptr<Entity>(dtd.lookupEntity(isParameter, name));
            // Did we find it in pass1Dtd?
            // Did we look at the defaultEntity?
            if (!inInstance_ && pass2() && dtd.isBase()
                && !resultAttributeSpecMode_
                && (entity.isNull() || !entity.pointer()!.declInActiveLpd()))
            {
                ConstPtr<Entity> entity1 = pass1Dtd_.pointer()?.lookupEntity(isParameter, name) ?? new ConstPtr<Entity>();
                if (!entity1.isNull() && entity1.pointer()!.declInActiveLpd()
                    && !entity1.pointer()!.defaulted())
                {
                    if (referenced)
                        noteReferencedEntity(entity1, true, false);
                    return entity1;
                }
                else if (!entity.isNull())
                {
                    if (referenced)
                        noteReferencedEntity(new ConstPtr<Entity>(entity.pointer()), false, false);
                    entity.pointer()!.setUsed();
                    return new ConstPtr<Entity>(entity.pointer());
                }
            }
            else if (!entity.isNull())
            {
                entity.pointer()!.setUsed();
                eventHandler().entityDefaulted(
                    new EntityDefaultedEvent(new ConstPtr<Entity>(entity.pointer()), useLocation));
                return new ConstPtr<Entity>(entity.pointer());
            }
            if (!isParameter)
            {
                ConstPtr<Entity> defEntity = new ConstPtr<Entity>(dtd.defaultEntity());
                Boolean note = false;
                Boolean usedPass1 = false;
                if (!inInstance_ && pass2() && dtd.isBase()
                    && !resultAttributeSpecMode_
                    && (defEntity.isNull() || !defEntity.pointer()!.declInActiveLpd()))
                {
                    if (referenced)
                        note = true;
                    ConstPtr<Entity> entity1 = pass1Dtd_.pointer()?.defaultEntity() ?? new ConstPtr<Entity>();
                    if (!entity1.isNull() && entity1.pointer()!.declInActiveLpd())
                    {
                        usedPass1 = true;
                        defEntity = entity1;
                    }
                }
                if (!defEntity.isNull())
                {
                    Boolean mustCopy = true;
                    if (inInstance_)
                    {
                        ConstPtr<Entity> tem = new ConstPtr<Entity>(instanceDefaultedEntityTable_.lookupConst(name));
                        if (!tem.isNull())
                        {
                            defEntity = tem;
                            mustCopy = false;
                        }
                    }
                    if (mustCopy)
                    {
                        Ptr<Entity> p = new Ptr<Entity>(defEntity.pointer()!.copy());
                        p.pointer()!.setName(name);
                        p.pointer()!.generateSystemId(this);
                        p.pointer()!.setDefaulted();
                        defEntity = new ConstPtr<Entity>(p.pointer());
                        if (inInstance_)
                        {
                            instanceDefaultedEntityTable_.insert(p);
                            eventHandler().entityDefaulted(
                                new EntityDefaultedEvent(defEntity, useLocation));
                        }
                        else
                            dtd.insertEntity(p);
                    }
                    if (note)
                        noteReferencedEntity(defEntity, usedPass1, true);
                }
                else
                {
                    ConstPtr<Entity> undefinedEntity = undefinedEntityTable_.lookupConst(name);
                    if (!undefinedEntity.isNull())
                        defEntity = undefinedEntity;
                }
                return defEntity;
            }
        }
        return new ConstPtr<Entity>();
    }

    // ConstPtr<Entity> createUndefinedEntity(const StringC &name, const Location &loc);
    public ConstPtr<Entity> createUndefinedEntity(StringC name, Location loc)
    {
        ExternalId extid = new ExternalId();
        Ptr<Entity> entity = new Ptr<Entity>(new ExternalTextEntity(name, EntityDecl.DeclType.generalEntity,
                                                                    loc, extid));
        undefinedEntityTable_.insert(entity);
        entity.pointer()!.generateSystemId(this);
        return new ConstPtr<Entity>(entity.pointer());
    }

    // void noteReferencedEntity(const ConstPtr<Entity> &entity, Boolean foundInPass1Dtd, Boolean lookedAtDefault);
    public void noteReferencedEntity(ConstPtr<Entity> entity,
                                     Boolean foundInPass1Dtd,
                                     Boolean lookedAtDefault)
    {
        LpdEntityRef entityRef = new LpdEntityRef();
        entityRef.entity = entity;
        entityRef.lookedAtDefault = lookedAtDefault;
        entityRef.foundInPass1Dtd = foundInPass1Dtd;
        StringC key = entity.pointer()!.name();
        if (!lpdEntityRefs_.ContainsKey(key))
            lpdEntityRefs_[key] = entityRef;
    }

    // Compare entity definitions.
    private static Boolean sameEntityDef(Entity e1, Entity e2)
    {
        if (e1.dataType() != e2.dataType())
            return false;
        InternalEntity? i1 = e1.asInternalEntity();
        InternalEntity? i2 = e2.asInternalEntity();
        if (i1 != null)
        {
            if (i2 == null)
                return false;
            if (!i1.@string().Equals(i2.@string()))
                return false;
            return true;
        }
        else if (i2 != null)
            return false;
        ExternalEntity? x1 = e1.asExternalEntity();
        ExternalEntity? x2 = e2.asExternalEntity();
        if (x1 == null || x2 == null)
            return false;
        StringC? s1 = x1.externalId().systemIdString();
        StringC? s2 = x2.externalId().systemIdString();
        if (s1 != null)
        {
            if (s2 == null)
                return false;
            if (!s1.Equals(s2))
                return false;
        }
        else if (s2 != null)
            return false;
        s1 = x1.externalId().publicIdString();
        s2 = x2.externalId().publicIdString();
        if (s1 != null)
        {
            if (s2 == null)
                return false;
            if (!s1.Equals(s2))
                return false;
        }
        else if (s2 != null)
            return false;
        return true;
    }

    // void checkEntityStability();
    public void checkEntityStability()
    {
        foreach (var pair in lpdEntityRefs_)
        {
            LpdEntityRef entityRef = pair.Value;
            ConstPtr<Entity> entity = dtd_[0].pointer()?.lookupEntity(
                entityRef.entity.pointer()!.declType() == Entity.DeclType.parameterEntity,
                entityRef.entity.pointer()!.name()) ?? new ConstPtr<Entity>();
            if (entity.isNull() && entityRef.lookedAtDefault)
                entity = dtd_[0].pointer()?.defaultEntity() ?? new ConstPtr<Entity>();
            if (entity.isNull()
                ? entityRef.foundInPass1Dtd
                : !sameEntityDef(entityRef.entity.pointer()!, entity.pointer()!))
                message((entityRef.entity.pointer()!.declType() == Entity.DeclType.parameterEntity)
                        ? ParserMessages.unstableLpdParameterEntity
                        : ParserMessages.unstableLpdGeneralEntity,
                        new StringMessageArg(entityRef.entity.pointer()!.name()));
        }
        // Ensure that the memory is released.
        lpdEntityRefs_.Clear();
    }

    // Boolean appendCurrentRank(StringC &str, const RankStem *stem) const;
    public Boolean appendCurrentRank(StringC str, RankStem stem)
    {
        StringC suffix = currentRank_[stem.index()];
        if (suffix.size() > 0)
        {
            str.operatorPlusAssign(suffix);
            return true;
        }
        return false;
    }

    // void setCurrentRank(const RankStem *stem, const StringC &suffix);
    public void setCurrentRank(RankStem stem, StringC suffix)
    {
        currentRank_[stem.index()] = suffix;
    }

    // void getCurrentToken(const SubstTable *subst, StringC &str) const;
    public void getCurrentToken(SubstTable? subst, StringC str)
    {
        InputSource? ins = currentInput();
        if (ins == null) return;
        Char[]? p = ins.currentTokenStart();
        if (p == null) return;
        nuint startIdx = ins.currentTokenStartIndex();
        nuint count = ins.currentTokenLength();
        str.resize(count);
        for (nuint i = 0; i < count; i++)
            str[i] = subst != null ? subst[p[startIdx + i]] : p[startIdx + i];
    }

    // void queueMessage(MessageEvent *event);
    public void queueMessage(MessageEvent? evt)
    {
        if (cancelled())
        {
            // delete event - handled by GC
            return;
        }
        if (keepingMessages_)
            keptMessages_.append(evt);
        else
            handler_?.message(evt);
    }

    // void releaseKeptMessages();
    public void releaseKeptMessages()
    {
        keepingMessages_ = false;
        while (!keptMessages_.empty())
        {
            if (cancelled())
            {
                allDone();
                return;
            }
            handler_?.message(keptMessages_.get());
        }
    }

    // void discardKeptMessages();
    public void discardKeptMessages()
    {
        keepingMessages_ = false;
        keptMessages_.clear();
    }

    // void initMessage(Message &msg);
    public override void initMessage(Message msg)
    {
        if (inInstance())
        {
            StringC rniPcdata = syntax().delimGeneral((int)Syntax.DelimGeneral.dRNI);
            rniPcdata.operatorPlusAssign(syntax().reservedName(Syntax.ReservedName.rPCDATA));
            getOpenElementInfo(msg.openElementInfo, rniPcdata);
        }
        msg.loc = new Location(currentLocation());
    }

    // void dispatchMessage(Message &msg);
    public override void dispatchMessage(Message msg)
    {
        queueMessage(new MessageEvent(msg));
    }

    // AttributeList *allocAttributeList(const ConstPtr<AttributeDefinitionList> &def, unsigned i);
    public AttributeList allocAttributeList(ConstPtr<AttributeDefinitionList> def, uint i)
    {
        if (i < attributeLists_.size())
            attributeLists_[i].pointer()!.init(def);
        else
        {
            attributeLists_.resize(i + 1);
            attributeLists_[i] = new Owner<AttributeList>(new AttributeList(def));
        }
        return attributeLists_[i].pointer()!;
    }

    // void activateLinkType(const StringC &name);
    public void activateLinkType(StringC name)
    {
        if (!hadPass2Start_ && !pass2_)
            activeLinkTypes_.push_back(name);
        else
            message(ParserMessages.linkActivateTooLate);
    }

    // Boolean shouldActivateLink(const StringC &name) const;
    public Boolean shouldActivateLink(StringC name)
    {
        if (!activeLinkTypesSubsted_)
        {
            for (nuint i = 0; i < activeLinkTypes_.size(); i++)
                syntax().generalSubstTable()!.subst(activeLinkTypes_[i]);
            activeLinkTypesSubsted_ = true;
        }
        for (nuint i = 0; i < activeLinkTypes_.size(); i++)
            if (name.Equals(activeLinkTypes_[i]))
                return true;
        return false;
    }

    // Ptr<Dtd> lookupDtd(const StringC &name);
    public Ptr<Dtd> lookupDtd(StringC name)
    {
        for (nuint i = 0; i < dtd_.size(); i++)
            if (dtd_[i].pointer()!.name().Equals(name))
                return dtd_[i];
        return new Ptr<Dtd>();
    }

    // ConstPtr<Lpd> lookupLpd(const StringC &name) const;
    public ConstPtr<Lpd> lookupLpd(StringC name)
    {
        for (nuint i = 0; i < allLpd_.size(); i++)
            if (allLpd_[i].pointer()!.name().Equals(name))
                return allLpd_[i];
        return new ConstPtr<Lpd>();
    }

    // ConstPtr<Notation> getAttributeNotation(const StringC &name, const Location &);
    public override ConstPtr<Notation> getAttributeNotation(StringC name, Location loc)
    {
        ConstPtr<Notation> notation = new ConstPtr<Notation>();
        if (haveCurrentDtd())
        {
            Ptr<Notation> nt1 = currentDtd().lookupNotation(name);
            notation = new ConstPtr<Notation>(nt1.pointer());
            if (notation.isNull() && sd().implydefNotation())
            {
                Ptr<Notation> nt = new Ptr<Notation>(new Notation(name,
                                                                  currentDtd().namePointer(),
                                                                  currentDtd().isBase()));
                ExternalId id = new ExternalId();
                nt.pointer()!.setExternalId(id, new Location());
                nt.pointer()!.generateSystemId(this);
                nt.pointer()!.setAttributeDef(currentDtdNonConst().implicitNotationAttributeDef());
                currentDtdNonConst().insertNotation(nt);
                Ptr<Notation> nt2 = currentDtd().lookupNotation(name);
                notation = new ConstPtr<Notation>(nt2.pointer());
            }
        }
        else if (resultAttributeSpecMode_)
        {
            Dtd? resultDtd = defComplexLpd().resultDtd().pointer();
            if (resultDtd == null)
                return new ConstPtr<Notation>();
            Ptr<Notation> nt3 = resultDtd.lookupNotation(name);
            notation = new ConstPtr<Notation>(nt3.pointer());
        }
        return notation;
    }

    // ConstPtr<Entity> getAttributeEntity(const StringC &str, const Location &loc);
    public override ConstPtr<Entity> getAttributeEntity(StringC str, Location loc)
    {
        ConstPtr<Entity> entity = lookupEntity(false, str, loc, false);
        if (!entity.isNull()
            && entity.pointer()!.defaulted()
            && options().warnDefaultEntityReference)
        {
            setNextLocation(loc);
            message(ParserMessages.defaultEntityInAttribute, new StringMessageArg(str));
        }
        return entity;
    }

    // Boolean defineId(const StringC &str, const Location &loc, Location &prevLoc);
    public override Boolean defineId(StringC str, Location loc, ref Location prevLoc)
    {
        if (!inInstance() || !validate())
            return true;
        Id id = lookupCreateId(str);
        if (id.defined())
        {
            prevLoc = id.defLocation();
            return false;
        }
        id.define(loc);
        return true;
    }

    // void noteIdref(const StringC &str, const Location &loc);
    public override void noteIdref(StringC str, Location loc)
    {
        if (!inInstance() || !options().errorIdref || !validate())
            return;
        Id id = lookupCreateId(str);
        if (!id.defined())
            id.addPendingRef(loc);
    }

    // void noteCurrentAttribute(size_t i, AttributeValue *value);
    public override void noteCurrentAttribute(nuint i, AttributeValue? value)
    {
        if (inInstance())
            currentAttributes_[i] = new ConstPtr<AttributeValue>(value);
    }

    // ConstPtr<AttributeValue> getCurrentAttribute(size_t i) const;
    public override ConstPtr<AttributeValue> getCurrentAttribute(nuint i)
    {
        if (!inInstance())
            return new ConstPtr<AttributeValue>();
        return currentAttributes_[i];
    }

    // const Syntax &attributeSyntax() const;
    public override Syntax attributeSyntax()
    {
        return syntax();
    }

    // unsigned instantiateDtd(Ptr<Dtd> &dtd);
    public uint instantiateDtd(Ptr<Dtd> dtd)
    {
        if (!dtd.pointer()!.isInstantiated())
        {
            dtd.pointer()!.instantiate();
            if (instantiatedDtds_ == sd().concur())
                message(ParserMessages.concurrentInstances,
                        new NumberMessageArg(sd().concur()));
            instantiatedDtds_++;
        }
        return instantiatedDtds_;
    }

    // Inline methods from header

    // Messenger &messenger();
    public Messenger messenger()
    {
        return this;
    }

    // Boolean wantMarkup() const;
    public Boolean wantMarkup()
    {
        return inInstance_
               ? options_.eventsWanted.wantInstanceMarkup()
               : options_.eventsWanted.wantPrologMarkup();
    }

    // const EventsWanted &eventsWanted() const;
    public EventsWanted eventsWanted()
    {
        return options_.eventsWanted;
    }

    // InputSource *currentInput() const;
    public InputSource? currentInput()
    {
        return inputStack_.head();
    }

    // const Location &currentLocation() const;
    public Location currentLocation()
    {
        InputSource? ins = currentInput();
        Location loc = ins != null ? ins.currentLocation() : nullLocation_;
        return loc;
    }

    // Boolean pcdataRecovering() const;
    public Boolean pcdataRecovering()
    {
        return pcdataRecovering_;
    }

    // unsigned inputLevel() const;
    public uint inputLevel()
    {
        return inputLevel_;
    }

    // unsigned specialParseInputLevel() const;
    public uint specialParseInputLevel()
    {
        return specialParseInputLevel_;
    }

    // unsigned markedSectionLevel() const;
    public uint markedSectionLevel()
    {
        return markedSectionLevel_;
    }

    // unsigned markedSectionSpecialLevel() const;
    public uint markedSectionSpecialLevel()
    {
        return markedSectionSpecialLevel_;
    }

    // const Location &currentMarkedSectionStartLocation() const;
    public Location currentMarkedSectionStartLocation()
    {
        return markedSectionStartLocation_.back();
    }

    // unsigned currentInputElementIndex() const;
    public uint currentInputElementIndex()
    {
        return inputLevelElementIndex_.back();
    }

    // Char currentChar() const;
    public Char currentChar()
    {
        InputSource? ins = currentInput();
        if (ins == null) return 0;
        Char[]? start = ins.currentTokenStart();
        if (start == null) return 0;
        nuint startIdx = ins.currentTokenStartIndex();
        return start[startIdx];
    }

    // StringC currentToken() const;
    public StringC currentToken()
    {
        InputSource? ins = currentInput();
        if (ins == null) return new StringC();
        Char[]? start = ins.currentTokenStart();
        if (start == null) return new StringC();
        nuint startIdx = ins.currentTokenStartIndex();
        nuint length = ins.currentTokenLength();
        // Copy the token portion to a new array
        Char[] tokenData = new Char[length];
        for (nuint i = 0; i < length; i++)
            tokenData[i] = start[startIdx + i];
        return new StringC(tokenData, length);
    }

    // void getCurrentToken(StringC &str) const;
    public void getCurrentToken(StringC str)
    {
        InputSource? ins = currentInput();
        if (ins == null) return;
        Char[]? start = ins.currentTokenStart();
        if (start == null) return;
        nuint startIdx = ins.currentTokenStartIndex();
        str.assign(start, startIdx, ins.currentTokenLength());
    }

    // Helper to get the current token data as an array (for event constructors)
    public Char[]? getCurrentTokenData()
    {
        InputSource? ins = currentInput();
        if (ins == null) return null;
        Char[]? start = ins.currentTokenStart();
        if (start == null) return null;
        nuint startIdx = ins.currentTokenStartIndex();
        nuint length = ins.currentTokenLength();
        if (length == 0) return Array.Empty<Char>();
        Char[] result = new Char[length];
        for (nuint i = 0; i < length; i++)
            result[i] = start[startIdx + i];
        return result;
    }

    // void setRecognizer(Mode mode, ConstPtr<Recognizer> p);
    public void setRecognizer(Mode mode, ConstPtr<Recognizer> p)
    {
        recognizers_[(int)mode] = p;
    }

    // void setNormalMap(const XcharMap<PackedBoolean> &map);
    public void setNormalMap(XcharMap<PackedBoolean> map)
    {
        normalMap_ = map;
    }

    // const XcharMap<PackedBoolean> &normalMap() const;
    public XcharMap<PackedBoolean> normalMap()
    {
        return normalMap_;
    }

    // Boolean haveDefLpd() const;
    public Boolean haveDefLpd()
    {
        return !defLpd_.isNull();
    }

    // Boolean haveCurrentDtd() const;
    public Boolean haveCurrentDtd()
    {
        return !currentDtd_.isNull();
    }

    // Dtd &defDtd();
    public Dtd defDtd()
    {
        return defDtd_.pointer()!;
    }

    // const Dtd &currentDtd() const;
    public Dtd currentDtd()
    {
        return currentDtd_.pointer()!;
    }

    // Dtd &currentDtdNonConst() const;
    public Dtd currentDtdNonConst()
    {
        return currentDtd_.pointer()!;
    }

    // const Ptr<Dtd> &defDtdPointer() const;
    public Ptr<Dtd> defDtdPointer()
    {
        return defDtd_;
    }

    // const ConstPtr<Dtd> &currentDtdPointer() const;
    public ConstPtr<Dtd> currentDtdPointer()
    {
        return currentDtdConst_;
    }

    // Boolean inInstance() const;
    public Boolean inInstance()
    {
        return inInstance_;
    }

    // const Syntax &syntax() const;
    public Syntax syntax()
    {
        return syntax_.pointer()!;
    }

    // const Syntax &instanceSyntax() const;
    public Syntax instanceSyntax()
    {
        return instanceSyntax_.pointer()!;
    }

    // const ConstPtr<Syntax> &syntaxPointer() const;
    public ConstPtr<Syntax> syntaxPointer()
    {
        return syntax_;
    }

    // const ConstPtr<Syntax> &instanceSyntaxPointer() const;
    public ConstPtr<Syntax> instanceSyntaxPointer()
    {
        return instanceSyntax_;
    }

    // const ConstPtr<Syntax> &prologSyntaxPointer() const;
    public ConstPtr<Syntax> prologSyntaxPointer()
    {
        return prologSyntax_;
    }

    // const Sd &sd() const;
    public Sd sd()
    {
        return sd_.pointer()!;
    }

    // const ConstPtr<Sd> &sdPointer() const;
    public ConstPtr<Sd> sdPointer()
    {
        return sd_;
    }

    // void setPhase(Phase phase);
    public void setPhase(Phase phase)
    {
        phase_ = phase;
    }

    // Mode currentMode() const;
    public Mode currentMode()
    {
        return currentMode_;
    }

    // Xchar getChar();
    public Xchar getChar()
    {
        return inputStack_.head()!.get(messenger());
    }

    // void skipChar();
    public void skipChar()
    {
        getChar();
    }

    // Token getToken(Mode mode);
    public Token getToken(Mode mode)
    {
        return recognizers_[(int)mode].pointer()!.recognize(inputStack_.head()!, messenger());
    }

    // Boolean hadDtd() const;
    public Boolean hadDtd()
    {
        return dtd_.size() > 0;
    }

    // Boolean eventQueueEmpty() const;
    public Boolean eventQueueEmpty()
    {
        return eventQueue_.empty();
    }

    // Event *eventQueueGet();
    public Event? eventQueueGet()
    {
        return eventQueue_.get();
    }

    // Phase phase() const;
    public Phase phase()
    {
        return phase_;
    }

    // Phase finalPhase() const;
    public Phase finalPhase()
    {
        return finalPhase_;
    }

    // EntityManager &entityManager() const;
    public EntityManager entityManager()
    {
        return entityManager_.pointer()!;
    }

    // Ptr<EntityManager> entityManagerPtr() const;
    public Ptr<EntityManager> entityManagerPtr()
    {
        return entityManager_;
    }

    // const EntityCatalog &entityCatalog() const;
    public EntityCatalog entityCatalog()
    {
        return entityCatalog_.pointer()!;
    }

    // ConstPtr<EntityCatalog> entityCatalogPtr() const;
    public ConstPtr<EntityCatalog> entityCatalogPtr()
    {
        return entityCatalog_;
    }

    // void setEntityCatalog(const ConstPtr<EntityCatalog> &catalog);
    public void setEntityCatalog(ConstPtr<EntityCatalog> catalog)
    {
        entityCatalog_ = catalog;
    }

    // void setDsEntity(const ConstPtr<Entity> &entity);
    public void setDsEntity(ConstPtr<Entity> entity)
    {
        dsEntity_ = entity;
    }

    // Allocator &eventAllocator();
    public Allocator eventAllocator()
    {
        return eventAllocator_;
    }

    // Allocator &internalAllocator();
    public Allocator internalAllocator()
    {
        return internalAllocator_;
    }

    // StringC &nameBuffer();
    public StringC nameBuffer()
    {
        return nameBuffer_;
    }

    // void setHandler(EventHandler *handler, const volatile sig_atomic_t *cancelPtr);
    public void setHandler(EventHandler? handler, int cancelPtr)
    {
        handler_ = handler;
        cancelPtr_ = cancelPtr;
        useExternalCancel_ = true;
    }

    // void unsetHandler();
    public void unsetHandler()
    {
        handler_ = eventQueue_;
        cancelPtr_ = 0;
        useExternalCancel_ = false;
    }

    // void queueRe(const Location &location);
    public void queueRe(Location location)
    {
        outputState_.handleRe(handler_!, eventAllocator_, options_.eventsWanted,
                              syntax().standardFunction((int)Syntax.StandardFunction.fRE),
                              location);
    }

    // void noteMarkup();
    public void noteMarkup()
    {
        if (inInstance_)
            outputState_.noteMarkup(handler_!, eventAllocator_, options_.eventsWanted);
    }

    // void noteRs();
    public void noteRs()
    {
        outputState_.noteRs(handler_!, eventAllocator_, options_.eventsWanted);
    }

    // void noteStartElement(Boolean included);
    public void noteStartElement(Boolean included)
    {
        outputState_.noteStartElement(included, handler_!, eventAllocator_,
                                      options_.eventsWanted);
    }

    // void noteEndElement(Boolean included);
    public void noteEndElement(Boolean included)
    {
        outputState_.noteEndElement(included, handler_!, eventAllocator_,
                                    options_.eventsWanted);
    }

    // void noteData();
    public void noteData()
    {
        outputState_.noteData(handler_!, eventAllocator_, options_.eventsWanted);
    }

    // unsigned subdocLevel() const;
    public uint subdocLevel()
    {
        return subdocLevel_;
    }

    // EventHandler &eventHandler();
    public EventHandler eventHandler()
    {
        return handler_!;
    }

    // IdTableIter idTableIter();
    public NamedTableIter<Id> idTableIter()
    {
        return new NamedTableIter<Id>(idTable_);
    }

    // const ParserOptions &options() const;
    public ParserOptions options()
    {
        return options_;
    }

    // Sd::ImplydefElement implydefElement();
    public Sd.ImplydefElement implydefElement()
    {
        return implydefElement_;
    }

    // Boolean implydefAttlist();
    public Boolean implydefAttlist()
    {
        return implydefAttlist_;
    }

    // void enableImplydef();
    public void enableImplydef()
    {
        implydefElement_ = Sd.ImplydefElement.implydefElementYes;
        implydefAttlist_ = true;
    }

    // void keepMessages();
    public void keepMessages()
    {
        keepingMessages_ = true;
    }

    // Boolean haveApplicableDtd() const;
    public Boolean haveApplicableDtd()
    {
        return !currentDtd_.isNull();
    }

    // Boolean hadLpd() const;
    public Boolean hadLpd()
    {
        return hadLpd_;
    }

    // Boolean pass2() const;
    public Boolean pass2()
    {
        return pass2_;
    }

    // size_t nActiveLink() const;
    public nuint nActiveLink()
    {
        return lpd_.size();
    }

    // const Lpd &activeLpd(size_t i) const;
    public Lpd activeLpd(nuint i)
    {
        return lpd_[i].pointer()!;
    }

    // Lpd &defLpd();
    public Lpd defLpd()
    {
        return defLpd_.pointer()!;
    }

    // Ptr<Lpd> &defLpdPointer();
    public Ptr<Lpd> defLpdPointer()
    {
        return defLpd_;
    }

    // Ptr<ComplexLpd> defComplexLpdPointer();
    public Ptr<ComplexLpd> defComplexLpdPointer()
    {
        return new Ptr<ComplexLpd>((ComplexLpd?)defLpd_.pointer());
    }

    // ComplexLpd &defComplexLpd();
    public ComplexLpd defComplexLpd()
    {
        return (ComplexLpd)defLpd_.pointer()!;
    }

    // Ptr<Dtd> baseDtd();
    public Ptr<Dtd> baseDtd()
    {
        if (dtd_.size() > 0)
            return dtd_[0];
        else
            return new Ptr<Dtd>();
    }

    // void setResultAttributeSpecMode();
    public void setResultAttributeSpecMode()
    {
        resultAttributeSpecMode_ = true;
    }

    // void clearResultAttributeSpecMode();
    public void clearResultAttributeSpecMode()
    {
        resultAttributeSpecMode_ = false;
    }

    // Markup *currentMarkup();
    public Markup? currentMarkup()
    {
        return currentMarkup_;
    }

    // const Location &markupLocation() const;
    public Location markupLocation()
    {
        return markupLocation_;
    }

    // Markup *startMarkup(Boolean storing, const Location &loc);
    public Markup? startMarkup(Boolean storing, Location loc)
    {
        markupLocation_ = loc;
        if (storing)
        {
            markup_.clear();
            return currentMarkup_ = markup_;
        }
        else
            return currentMarkup_ = null;
    }

    // Boolean cancelled() const;
    public Boolean cancelled()
    {
        return useExternalCancel_ ? cancelPtr_ != 0 : false;
    }

    // void setHadAfdrDecl();
    public void setHadAfdrDecl()
    {
        hadAfdrDecl_ = true;
    }

    // Boolean hadAfdrDecl() const;
    public Boolean hadAfdrDecl()
    {
        return hadAfdrDecl_;
    }

    // const ConstPtr<Entity> &dsEntity() const;
    public ConstPtr<Entity> dsEntity()
    {
        return dsEntity_;
    }

    // Boolean validate() const (from AttributeContext)
    public new Boolean validate()
    {
        return validate_;
    }
}
