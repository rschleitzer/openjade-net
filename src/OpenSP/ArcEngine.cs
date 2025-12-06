// Copyright (c) 1996 James Clark, 2000 Matthias Clasen
// Copyright (c) 2001 Epremis Corp.
// See the file COPYING for copying permission.

namespace OpenSP;

// Constants for attribute indices
internal static class ArcConstants
{
    public const uint invalidAtt = unchecked((uint)-1);
    public const uint contentPseudoAtt = unchecked((uint)-2);
}

// Director interface for architectural form processing
public abstract class ArcDirector
{
    // virtual EventHandler *arcEventHandler(const StringC *arcPublicId,
    //                                       const Notation *,
    //                                       const Vector<StringC> &,
    //                                       const SubstTable *) = 0;
    public abstract EventHandler? arcEventHandler(StringC? arcPublicId,
                                                   Notation? notation,
                                                   Vector<StringC> name,
                                                   SubstTable? table);

    // virtual void dispatchMessage(const Message &) = 0;
    public virtual void dispatchMessage(Message msg) { }
}

// Simple director that selects one architecture
public class SelectOneArcDirector : ArcDirector
{
    private Vector<StringC> select_ = new Vector<StringC>();
    private EventHandler? eh_;

    // SelectOneArcDirector(const Vector<StringC> &select, EventHandler &eh)
    public SelectOneArcDirector(Vector<StringC> select, EventHandler eh)
    {
        select_ = select;
        eh_ = eh;
    }

    // EventHandler *arcEventHandler(const StringC *, const Notation *,
    //                               const Vector<StringC> &, const SubstTable *);
    public override EventHandler? arcEventHandler(StringC? arcPublicId,
                                                   Notation? notation,
                                                   Vector<StringC> name,
                                                   SubstTable? table)
    {
        if (name.size() != select_.size())
            return null;
        for (nuint i = 0; i < name.size(); i++)
        {
            StringC tem = new StringC(select_[i]);
            table?.subst(tem);
            if (!name[i].Equals(tem))
                return null;
        }
        return eh_;
    }

    // void dispatchMessage(const Message &);
    public override void dispatchMessage(Message msg)
    {
        eh_?.message(new MessageEvent(msg));
    }
}

// Main ArcEngine static class
public class ArcEngine
{
    // static void parseAll(SgmlParser &, Messenger &, ArcDirector &, const volatile sig_atomic_t *cancelPtr = 0);
    public static void parseAll(SgmlParser parser,
                                Messenger mgr,
                                ArcDirector director,
                                int cancelPtr = 0)
    {
        ArcEngineImpl wrap = new ArcEngineImpl(mgr, parser, director, cancelPtr,
                                                null, null, new Vector<StringC>(), null);
        parser.parseAll(wrap, cancelPtr);
    }

    private ArcEngine() { }
}

// Delegate event handler - passes events to another handler
internal class DelegateEventHandler : EventHandler
{
    protected EventHandler? delegateTo_;

    public override void data(DataEvent? ev) { delegateTo_?.data(ev); }
    public override void sSep(SSepEvent? ev) { delegateTo_?.sSep(ev); }
    public override void startElement(StartElementEvent? ev) { delegateTo_?.startElement(ev); }
    public override void endElement(EndElementEvent? ev) { delegateTo_?.endElement(ev); }
    public override void pi(PiEvent? ev) { delegateTo_?.pi(ev); }
    public override void sdataEntity(SdataEntityEvent? ev) { delegateTo_?.sdataEntity(ev); }
    public override void externalDataEntity(ExternalDataEntityEvent? ev) { delegateTo_?.externalDataEntity(ev); }
    public override void subdocEntity(SubdocEntityEvent? ev) { delegateTo_?.subdocEntity(ev); }
    public override void appinfo(AppinfoEvent? ev) { delegateTo_?.appinfo(ev); }
    public override void uselink(UselinkEvent? ev) { delegateTo_?.uselink(ev); }
    public override void usemap(UsemapEvent? ev) { delegateTo_?.usemap(ev); }
    public override void startDtd(StartDtdEvent? ev) { delegateTo_?.startDtd(ev); }
    public override void endDtd(EndDtdEvent? ev) { delegateTo_?.endDtd(ev); }
    public override void startLpd(StartLpdEvent? ev) { delegateTo_?.startLpd(ev); }
    public override void endLpd(EndLpdEvent? ev) { delegateTo_?.endLpd(ev); }
    public override void endProlog(EndPrologEvent? ev) { delegateTo_?.endProlog(ev); }
    public override void sgmlDecl(SgmlDeclEvent? ev) { delegateTo_?.sgmlDecl(ev); }
    public override void commentDecl(CommentDeclEvent? ev) { delegateTo_?.commentDecl(ev); }
    public override void message(MessageEvent? ev) { delegateTo_?.message(ev); }
}

// Queue event handler - queues events for later processing
internal class QueueEventHandler : EventHandler
{
    private IQueue<Event> queue_ = new IQueue<Event>();

    public override void data(DataEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void sSep(SSepEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void startElement(StartElementEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void endElement(EndElementEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void pi(PiEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void sdataEntity(SdataEntityEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void externalDataEntity(ExternalDataEntityEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void message(MessageEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }

    public Event? get() { return queue_.get(); }
    public Boolean empty() { return queue_.empty(); }
    public void swap(IQueue<Event> other) { queue_.swap(other); }
}

// Null event handler - just passes messages
internal class NullEventHandler : EventHandler
{
    private Messenger mgr_;

    public NullEventHandler(Messenger mgr)
    {
        mgr_ = mgr;
    }

    public override void message(MessageEvent? ev)
    {
        if (ev != null)
        {
            mgr_.dispatchMessage(ev.message());
        }
    }
}

// ArcProcessor - processor for a single architecture
internal class ArcProcessor : ContentState
{
    // MetaMap struct for attribute mapping
    public class MetaMap
    {
        public IAttributed? attributed;
        public uint suppressFlags;
        public Vector<uint> attMapFrom = new Vector<uint>();
        public Vector<uint> attMapTo = new Vector<uint>();
        public Vector<nuint> attTokenMapBase = new Vector<nuint>();
        public Vector<StringC> tokenMapFrom = new Vector<StringC>();
        public Vector<StringC> tokenMapTo = new Vector<StringC>();

        public MetaMap()
        {
            attributed = null;
            suppressFlags = 0;
            attTokenMapBase.push_back(0);
        }

        public void clear()
        {
            attMapFrom.clear();
            attMapTo.clear();
            attTokenMapBase.clear();
            tokenMapFrom.clear();
            tokenMapTo.clear();
            attributed = null;
            attTokenMapBase.push_back(0);
        }
    }

    // MetaMapCache for caching metamap results
    public class MetaMapCache
    {
        public const int nNoSpec = 4;
        public MetaMap map = new MetaMap();
        public uint[] noSpec = new uint[nNoSpec];
        public uint suppressFlags;
        public AttributeList? linkAtts;

        public MetaMapCache()
        {
            for (int i = 0; i < nNoSpec; i++)
                noSpec[i] = ArcConstants.invalidAtt;
            linkAtts = null;
        }

        public void clear()
        {
            for (int i = 0; i < nNoSpec; i++)
                noSpec[i] = ArcConstants.invalidAtt;
            linkAtts = null;
            map.clear();
        }
    }

    // Suppress flags
    private const uint isArc = 0x01;
    private const uint suppressForm = 0x02;
    private const uint suppressSupr = 0x04;
    private const uint ignoreData = 0x08;
    private const uint condIgnoreData = 0x10;
    private const uint recoverData = 0x20;

    // Reserved names enum
    private enum ReservedName
    {
        rArcName,
        rArcPubid,
        rArcFormA,
        rArcNamrA,
        rArcSuprA,
        rArcIgnDA,
        rArcDocF,
        rArcSuprF,
        rArcBridF,
        rArcDataF,
        rArcAuto,
        rArcDTD,
        rArcDtdPubid,
        rArcDtdSysid,
        rArcQuant,
        nReserve
    }

    private Boolean valid_;
    private StringC name_ = new StringC();
    private Messenger? mgr_;
    private ConstPtr<Dtd> docDtd_ = new ConstPtr<Dtd>();
    private Ptr<Dtd> metaDtd_ = new Ptr<Dtd>();
    private ConstPtr<Syntax> docSyntax_ = new ConstPtr<Syntax>();
    private ConstPtr<Syntax> metaSyntax_ = new ConstPtr<Syntax>();
    private ConstPtr<Sd> docSd_ = new ConstPtr<Sd>();
    private StringC[] supportAtts_ = new StringC[(int)ReservedName.nReserve];
    private Text?[] supportAttsText_ = new Text?[(int)ReservedName.nReserve];
    private Boolean piDecl_;
    private Location declLoc_ = new Location();
    private StringC piDeclAttspecText_ = new StringC();
    private Index piDeclAttspecIndex_;
    private ConstPtr<AttributeDefinitionList> archPiAttributeDefs_ = new ConstPtr<AttributeDefinitionList>();
#pragma warning disable CS0414 // Field is assigned but its value is never used
    private Boolean arcDtdIsParam_;
#pragma warning restore CS0414
    private Boolean arcAuto_;
#pragma warning disable CS0414 // Field is assigned but its value is never used
    private new Boolean mayDefaultAttribute_;
#pragma warning restore CS0414
    private Vector<StringC> arcOpts_ = new Vector<StringC>();
    private StringC rniContent_ = new StringC();
    private StringC rniArcCont_ = new StringC();
    private StringC rniDefault_ = new StringC();
    private StringC rniMaptoken_ = new StringC();
    private Vector<uint> openElementFlags_ = new Vector<uint>();
    private AttributeList attributeList_ = new AttributeList();
    private Vector<Owner<MetaMapCache>> metaMapCache_ = new Vector<Owner<MetaMapCache>>();
    private MetaMap noCacheMetaMap_ = new MetaMap();
    private NamedTable<Id> idTable_ = new NamedTable<Id>();
    private Vector<ConstPtr<AttributeValue>> currentAttributes_ = new Vector<ConstPtr<AttributeValue>>();
    private ConstPtr<Notation> defaultNotation_ = new ConstPtr<Notation>();
    private Boolean errorIdref_;
    private ArcDirector? director_;
    private EventHandler? docHandler_;
    private Owner<EventHandler> ownEventHandler_ = new Owner<EventHandler>();

    // ArcProcessor();
    public ArcProcessor()
    {
        valid_ = false;
        piDecl_ = false;
        arcDtdIsParam_ = false;
        arcAuto_ = true;
        errorIdref_ = true;
        mayDefaultAttribute_ = true;

        for (int i = 0; i < (int)ReservedName.nReserve; i++)
        {
            supportAtts_[i] = new StringC();
            supportAttsText_[i] = null;
        }
    }

    // void setName(const StringC &name, const Location &loc);
    public void setName(StringC name, Location loc)
    {
        piDecl_ = false;
        name_ = new StringC(name);
        declLoc_ = loc;
    }

    // Boolean valid() const;
    public Boolean valid() { return valid_; }

    // const StringC &name() const;
    public StringC name() { return name_; }

    // Boolean piDecl() const;
    public Boolean piDecl() { return piDecl_; }

    // EventHandler &docHandler() const;
    public EventHandler docHandler() { return docHandler_!; }

    // ConstPtr<Dtd> dtdPointer() const;
    public ConstPtr<Dtd> dtdPointer() { return new ConstPtr<Dtd>(metaDtd_.pointer()); }

    // const Syntax &attributeSyntax() const;
    public override Syntax attributeSyntax()
    {
        return docSyntax_.pointer()!;
    }

    // ConstPtr<Notation> getAttributeNotation(const StringC &, const Location &);
    public new ConstPtr<Notation> getAttributeNotation(StringC name, Location loc)
    {
        if (!metaDtd_.isNull())
            return metaDtd_.pointer()!.lookupNotationConst(name);
        return new ConstPtr<Notation>();
    }

    // ConstPtr<Entity> getAttributeEntity(const StringC &, const Location &);
    public new ConstPtr<Entity> getAttributeEntity(StringC name, Location loc)
    {
        if (!metaDtd_.isNull())
            return metaDtd_.pointer()!.lookupEntity(false, name);
        return new ConstPtr<Entity>();
    }

    // void noteCurrentAttribute(size_t i, AttributeValue *value);
    public new void noteCurrentAttribute(nuint i, AttributeValue? value)
    {
        if (valid_)
        {
            if (i >= currentAttributes_.size())
                currentAttributes_.resize(i + 1);
            currentAttributes_[i] = new ConstPtr<AttributeValue>(value);
        }
    }

    // ConstPtr<AttributeValue> getCurrentAttribute(size_t i) const;
    public new ConstPtr<AttributeValue> getCurrentAttribute(nuint i)
    {
        if (i < currentAttributes_.size())
            return currentAttributes_[i];
        return new ConstPtr<AttributeValue>();
    }

    // Boolean defineId(const StringC &str, const Location &loc, Location &prevLoc);
    public Boolean defineId(StringC str, Location loc, out Location prevLoc)
    {
        prevLoc = new Location();
        if (!valid_)
            return true;
        Id? id = lookupCreateId(str);
        if (id == null)
            return true;
        if (id.defined())
        {
            prevLoc = id.defLocation();
            return false;
        }
        id.define(loc);
        return true;
    }

    // void noteIdref(const StringC &str, const Location &loc);
    public new void noteIdref(StringC str, Location loc)
    {
        if (!valid_ || !errorIdref_)
            return;
        Id? id = lookupCreateId(str);
        if (id != null && !id.defined())
            id.addPendingRef(loc);
    }

    // Id *lookupCreateId(const StringC &name);
    private Id? lookupCreateId(StringC name)
    {
        Id? id = idTable_.lookup(name);
        if (id == null)
        {
            id = new Id(name);
            idTable_.insert(id);
        }
        return id;
    }

    // void checkIdrefs();
    public void checkIdrefs()
    {
        NamedTableIter<Id> iter = new NamedTableIter<Id>(idTable_);
        Id? id;
        while ((id = iter.next()) != null)
        {
            for (nuint i = 0; i < id.pendingRefs().size(); i++)
            {
                setNextLocation(id.pendingRefs()[i]);
                message(ArcEngineMessages.missingId, new StringMessageArg(id.name()));
            }
        }
    }

    // void setPiDecl(const Location &loc, const StringC &attspecText, Index attspecIndex,
    //                const ConstPtr<AttributeDefinitionList> &archPiAttributeDefs);
    public void setPiDecl(Location loc, StringC attspecText, Index attspecIndex,
                          ConstPtr<AttributeDefinitionList> archPiAttributeDefs)
    {
        piDecl_ = true;
        declLoc_ = loc;
        piDeclAttspecText_ = new StringC(attspecText);
        piDeclAttspecIndex_ = attspecIndex;
        archPiAttributeDefs_ = archPiAttributeDefs;
    }

    // void init(...);
    public void init(EndPrologEvent ev,
                     ConstPtr<Sd> sd,
                     ConstPtr<Syntax> syntax,
                     SgmlParser? parser,
                     Messenger? mgr,
                     Vector<StringC> superName,
                     Vector<ArcProcessor> arcProcessors,
                     ArcDirector director,
                     int cancelPtr)
    {
        director_ = director;
        mgr_ = mgr;
        docSyntax_ = syntax;
        docSd_ = sd;
        valid_ = false;
        docDtd_ = ev.dtdPointer();
        metaSyntax_ = docSyntax_;
        mayDefaultAttribute_ = true;

        if (docDtd_.isNull())
            return;

        ConstPtr<Notation> notation = new ConstPtr<Notation>();

        if (!piDecl())
            docSyntax_.pointer()?.generalSubstTable()?.subst(name_);

        // Check for duplicate declarations
        for (nuint i = 0; i < arcProcessors.size(); i++)
        {
            ArcProcessor p = arcProcessors[i];
            if (object.ReferenceEquals(p, this))
                break;
            if (name_.Equals(p.name()))
            {
                if ((piDecl() && p.piDecl()) || (!piDecl() && !p.piDecl()))
                {
                    setNextLocation(declLoc_);
                    message(ArcEngineMessages.duplicateArcDecl,
                            new StringMessageArg(name_),
                            p.declLoc_);
                    return;
                }
            }
        }

        StringC? arcPublicId = null;
        if (!piDecl())
        {
            notation = docDtd_.pointer()!.lookupNotationConst(name_);
            if (!notation.isNull())
            {
                ConstPtr<AttributeDefinitionList> notAttDef = notation.pointer()!.attributeDef();
                attributeList_.init(notAttDef);
                attributeList_.finish(this);
                supportAttributes(attributeList_, false);
                arcPublicId = notation.pointer()?.publicIdPointer();
            }
            else
            {
                setNextLocation(declLoc_);
                message(ArcEngineMessages.noArcNotation, new StringMessageArg(name_));
            }
        }

        Vector<StringC> docName = new Vector<StringC>(superName);
        docName.push_back(name_);

        ArcEngineImpl engine = new ArcEngineImpl(mgr ?? new NullMessenger(), parser!, director, cancelPtr,
                                                  arcPublicId, notation.pointer(),
                                                  docName,
                                                  docSyntax_.pointer()?.generalSubstTable());
        docHandler_ = engine;
        ownEventHandler_ = new Owner<EventHandler>(engine);

        // Set default form attributes
        if (supportAtts_[(int)ReservedName.rArcDocF].size() == 0)
            supportAtts_[(int)ReservedName.rArcDocF] = new StringC(name_);
        if (supportAtts_[(int)ReservedName.rArcFormA].size() == 0)
            supportAtts_[(int)ReservedName.rArcFormA] = new StringC(name_);

        // Set up RNI strings
        Syntax? syn = docSyntax_.pointer();
        Sd? sdp = sd.pointer();
        if (syn != null && sdp != null)
        {
            rniContent_ = new StringC(syn.delimGeneral((int)Syntax.DelimGeneral.dRNI));
            rniContent_.operatorPlusAssign(sdp.execToInternal("CONTENT"));
            rniDefault_ = new StringC(syn.delimGeneral((int)Syntax.DelimGeneral.dRNI));
            rniDefault_.operatorPlusAssign(syn.reservedName(Syntax.ReservedName.rDEFAULT));
            rniArcCont_ = new StringC(syn.delimGeneral((int)Syntax.DelimGeneral.dRNI));
            rniArcCont_.operatorPlusAssign(sdp.execToInternal("ARCCONT"));
            rniMaptoken_ = new StringC(syn.delimGeneral((int)Syntax.DelimGeneral.dRNI));
            rniMaptoken_.operatorPlusAssign(sdp.execToInternal("MAPTOKEN"));
        }

        // For now, set up a minimal meta-DTD (simplified - full version would parse the arc DTD)
        metaDtd_ = new Ptr<Dtd>(new Dtd(name_, true));
        metaMapCache_.resize(docDtd_.pointer()!.nElementTypeIndex());

        // Initialize ContentState with the meta-DTD
        startContent(metaDtd_.pointer()!);

        valid_ = true;
        currentAttributes_.resize(metaDtd_.pointer()!.nCurrentAttribute());
    }

    // void supportAttributes(const AttributeList &atts, Boolean piDecl);
    private void supportAttributes(AttributeList atts, Boolean piDeclMode)
    {
        string?[][] s = new string?[][]
        {
            new string?[] { null, "name" },
            new string?[] { null, "public-id" },
            new string?[] { "ArcFormA", "form-att" },
            new string?[] { "ArcNamrA", "renamer-att" },
            new string?[] { "ArcSuprA", "suppressor-att" },
            new string?[] { "ArcIgnDA", "ignore-data-att" },
            new string?[] { "ArcDocF", "doc-elem-form" },
            new string?[] { "ArcSuprF", null },
            new string?[] { "ArcBridF", "bridge-form" },
            new string?[] { "ArcDataF", "data-form" },
            new string?[] { "ArcAuto", "auto" },
            new string?[] { "ArcDTD", null },
            new string?[] { null, "dtd-public-id" },
            new string?[] { null, "dtd-system-id" },
            new string?[] { "ArcQuant", "quantity" }
        };

        int column = piDeclMode ? 1 : 0;
        for (int i = 0; i < (int)ReservedName.nReserve; i++)
            supportAttsText_[i] = null;

        Sd? sd = docSd_.pointer();
        SubstTable? substTable = docSyntax_.pointer()?.generalSubstTable();

        for (int i = 0; i < s.Length && i < (int)ReservedName.nReserve; i++)
        {
            if (s[i][column] != null && sd != null)
            {
                StringC attName = sd.execToInternal(s[i][column]!);
                substTable?.subst(attName);
                uint ind;
                if (atts.attributeIndex(attName, out ind))
                {
                    AttributeValue? value = atts.value(ind);
                    if (value != null)
                    {
                        Text? textP = value.text();
                        if (textP != null)
                        {
                            supportAttsText_[i] = textP;
                            supportAtts_[i] = new StringC(textP.@string());

                            switch ((ReservedName)i)
                            {
                                case ReservedName.rArcName:
                                    name_ = new StringC(supportAtts_[i]);
                                    break;
                                case ReservedName.rArcAuto:
                                    if (!piDeclMode)
                                        substTable?.subst(supportAtts_[i]);
                                    StringC ArcAuto = sd.execToInternal("ArcAuto");
                                    substTable?.subst(ArcAuto);
                                    if (supportAtts_[i].Equals(ArcAuto))
                                        arcAuto_ = true;
                                    else
                                    {
                                        StringC nArcAuto = sd.execToInternal("nArcAuto");
                                        substTable?.subst(nArcAuto);
                                        if (supportAtts_[i].Equals(nArcAuto))
                                            arcAuto_ = false;
                                    }
                                    break;
                                case ReservedName.rArcFormA:
                                case ReservedName.rArcNamrA:
                                case ReservedName.rArcSuprA:
                                case ReservedName.rArcIgnDA:
                                    if (!piDeclMode)
                                        substTable?.subst(supportAtts_[i]);
                                    break;
                                case ReservedName.rArcDocF:
                                case ReservedName.rArcSuprF:
                                case ReservedName.rArcBridF:
                                case ReservedName.rArcDataF:
                                    metaSyntax_.pointer()?.generalSubstTable()?.subst(supportAtts_[i]);
                                    break;
                                case ReservedName.rArcDTD:
                                    Syntax? docSyn = docSyntax_.pointer();
                                    if (docSyn != null)
                                    {
                                        StringC pero = docSyn.delimGeneral((int)Syntax.DelimGeneral.dPERO);
                                        if (supportAtts_[i].size() >= pero.size())
                                        {
                                            StringC tem = supportAtts_[i].substr(0, pero.size());
                                            substTable?.subst(tem);
                                            if (tem.Equals(pero))
                                            {
                                                arcDtdIsParam_ = true;
                                                StringC rest = supportAtts_[i].substr(pero.size(),
                                                                           supportAtts_[i].size() - pero.size());
                                                supportAtts_[i] = rest;
                                            }
                                        }
                                        docSyn.entitySubstTable()?.subst(supportAtts_[i]);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }

    // Boolean processStartElement(const StartElementEvent &, const AttributeList *, const Text *, Allocator &);
    public Boolean processStartElement(StartElementEvent ev,
                                        AttributeList? linkAttributes,
                                        Text? content,
                                        Allocator alloc)
    {
        if (!valid_)
            return true;

        uint suppressFlags = openElementFlags_.size() > 0
            ? (openElementFlags_.back() & ~isArc)
            : condIgnoreData;

        if ((suppressFlags & suppressForm) != 0 && (suppressFlags & suppressSupr) != 0)
        {
            openElementFlags_.push_back(suppressFlags);
            return true;
        }

        AttributeList atts = ev.attributes();
        MetaMap map = buildMetaMap(ev.elementType(), null, atts, linkAttributes, suppressFlags);

        IAttributed? metaType;
        ConstPtr<AttributeValue> arcContent = new ConstPtr<AttributeValue>();

        if (map.attributed == null)
        {
            if (!(tagLevel() == 0 && !currentElement().isFinished()))
            {
                openElementFlags_.push_back(map.suppressFlags);
                return true;
            }
            metaType = metaDtd_.pointer()?.documentElementType();
            if (metaType != null)
            {
                attributeList_.init(metaType.attributeDef());
                attributeList_.finish(this);
            }
        }
        else
        {
            if (!mapAttributes(atts, linkAttributes, content, attributeList_, ref arcContent, map))
                return false;
            metaType = map.attributed;
            suppressFlags = map.suppressFlags;
        }

        if (metaType is ElementType elementType)
        {
            StartElementEvent genEvent = new StartElementEvent(elementType,
                                                                new ConstPtr<Dtd>(metaDtd_),
                                                                attributeList_,
                                                                ev.location(),
                                                                null);

            if (elementType.definition()?.undefined() == true)
                message(ArcEngineMessages.undefinedElement, new StringMessageArg(elementType.name()));
            else if (elementIsExcluded(elementType))
                message(ArcEngineMessages.elementExcluded, new StringMessageArg(elementType.name()));
            else if (elementIsIncluded(elementType))
                genEvent.setIncluded();
            else if (!currentElement().tryTransition(elementType))
                message(ArcEngineMessages.invalidElement, new StringMessageArg(elementType.name()));

            pushElement(new OpenElement(elementType, false, genEvent.included(), null, ev.location()));
            docHandler_?.startElement(genEvent);

            if (attributeList_.conref())
                currentElement().setConref();

            if (!arcContent.isNull() && arcContent.pointer()?.text() != null)
            {
                if (currentElement().declaredEmpty() || !currentElement().tryTransitionPcdata())
                    message(ArcEngineMessages.invalidArcContent);
                else
                    emitArcContent(arcContent.pointer()!.text()!, docHandler()!, alloc);
                suppressFlags |= (suppressForm | suppressSupr | ignoreData);
            }
        }

        suppressFlags &= ~recoverData;
        openElementFlags_.push_back(suppressFlags | isArc);
        return true;
    }

    // void emitArcContent(const Text &text, EventHandler &handler, Allocator &alloc);
    private void emitArcContent(Text text, EventHandler handler, Allocator alloc)
    {
        TextIter iter = new TextIter(text);
        TextItem.Type type;
        Char[]? s;
        nuint n;
        Location? loc;
        while (iter.next(out type, out s, out n, out loc))
        {
            switch (type)
            {
                case TextItem.Type.data:
                case TextItem.Type.cdata:
                    if (type == TextItem.Type.data)
                    {
                        ImmediateDataEvent dataEvent = new ImmediateDataEvent(
                            Event.Type.characterData, s!, n, loc!, false);
                        handler.data(dataEvent);
                    }
                    else if (loc?.origin().pointer()?.asEntityOrigin()?.entity()?.asInternalEntity() != null)
                    {
                        CdataEntityEvent cdataEvent = new CdataEntityEvent(
                            loc.origin().pointer()!.asEntityOrigin()!.entity()!.asInternalEntity()!,
                            loc.origin());
                        handler.data(cdataEvent);
                    }
                    break;
                case TextItem.Type.sdata:
                    if (loc?.origin().pointer()?.asEntityOrigin()?.entity()?.asInternalEntity() != null)
                    {
                        SdataEntityEvent sdataEvent = new SdataEntityEvent(
                            loc.origin().pointer()!.asEntityOrigin()!.entity()!.asInternalEntity()!,
                            loc.origin());
                        handler.sdataEntity(sdataEvent);
                    }
                    break;
            }
        }
    }

    // Boolean processData();
    public Boolean processData()
    {
        if (openElementFlags_.size() > 0 && (openElementFlags_.back() & ignoreData) != 0)
            return false;
        if (!currentElement().declaredEmpty() && currentElement().tryTransitionPcdata())
            return true;
        else if (openElementFlags_.size() > 0 && (openElementFlags_.back() & condIgnoreData) != 0)
            return false;
        else
        {
            if (openElementFlags_.size() > 0)
            {
                if ((openElementFlags_.back() & recoverData) != 0)
                    return true;
                openElementFlags_[openElementFlags_.size() - 1] |= recoverData;
            }
            message(ArcEngineMessages.invalidData);
            return true;
        }
    }

    // void processEndElement(const EndElementEvent &event, Allocator &alloc);
    public void processEndElement(EndElementEvent ev, Allocator alloc)
    {
        if (openElementFlags_.size() == 0)
            return;

        Boolean wasArc = (openElementFlags_.back() & isArc) != 0;
        openElementFlags_.resize(openElementFlags_.size() - 1);

        if (wasArc)
        {
            EndElementEvent genEvent = new EndElementEvent(currentElement().type(),
                                                            new ConstPtr<Dtd>(metaDtd_),
                                                            ev.location(),
                                                            null);
            if (currentElement().included())
                genEvent.setIncluded();
            docHandler_?.endElement(genEvent);

            if (!currentElement().isFinished())
                message(ArcEngineMessages.unfinishedElement,
                       new StringMessageArg(currentElement().type()?.name() ?? new StringC()));

            popElement();
        }
    }

    // const MetaMap &buildMetaMap(...);
    private MetaMap buildMetaMap(ElementType? docElementType,
                                  Notation? notation,
                                  AttributeList atts,
                                  AttributeList? linkAtts,
                                  uint suppressFlags)
    {
        Boolean isNotation;
        IAttributed? attributed = docElementType;
        StringC? nameP;

        if (attributed == null)
        {
            attributed = notation;
            isNotation = true;
            nameP = notation?.name();
        }
        else
        {
            isNotation = false;
            nameP = docElementType?.name();
        }

        Boolean inhibitCache = false;
        nuint cacheIndex;

        if (isNotation || docElementType?.definition()?.undefined() == true)
        {
            inhibitCache = true;
            cacheIndex = unchecked((nuint)(uint)-1);
        }
        else
        {
            cacheIndex = docElementType!.index();
            if (cacheIndex < metaMapCache_.size())
            {
                MetaMapCache? cache = metaMapCache_[cacheIndex].pointer();
                if (cache != null && cache.suppressFlags == suppressFlags && cache.linkAtts == linkAtts)
                {
                    for (int i = 0; ; i++)
                    {
                        if (i == MetaMapCache.nNoSpec)
                            return cache.map;
                        uint attIndex = cache.noSpec[i];
                        if (attIndex != ArcConstants.invalidAtt && atts.specified(attIndex))
                            break;
                    }
                }
            }
        }

        // No valid cached MetaMap
        uint oldSuppressFlags = suppressFlags;
        uint newSuppressFlags = suppressFlags;
        uint arcSuprIndex = ArcConstants.invalidAtt;
        uint arcIgnDIndex = ArcConstants.invalidAtt;
        uint arcFormIndex = ArcConstants.invalidAtt;
        uint arcNamerIndex = ArcConstants.invalidAtt;

        if (!isNotation)
        {
            considerSupr(atts, linkAtts, ref suppressFlags, ref newSuppressFlags, ref inhibitCache, out arcSuprIndex);
            considerIgnD(atts, linkAtts, suppressFlags, ref newSuppressFlags, ref inhibitCache, out arcIgnDIndex);
        }

        IAttributed? metaAttributed = considerForm(atts, linkAtts, nameP ?? new StringC(), isNotation,
                                                    suppressFlags, ref newSuppressFlags, ref inhibitCache, out arcFormIndex);

        Text? namerText = null;
        if (metaAttributed != null)
            namerText = considerNamer(atts, ref inhibitCache, out arcNamerIndex);

        MetaMap mapP;
        if (inhibitCache)
        {
            noCacheMetaMap_.clear();
            mapP = noCacheMetaMap_;
        }
        else
        {
            if (cacheIndex >= metaMapCache_.size())
                metaMapCache_.resize(cacheIndex + 1);

            MetaMapCache? cache = metaMapCache_[cacheIndex].pointer();
            if (cache != null)
                cache.clear();
            else
            {
                cache = new MetaMapCache();
                metaMapCache_[cacheIndex] = new Owner<MetaMapCache>(cache);
            }
            cache.noSpec[0] = arcFormIndex;
            cache.noSpec[1] = arcNamerIndex;
            cache.noSpec[2] = arcSuprIndex;
            cache.noSpec[3] = arcIgnDIndex;
            cache.suppressFlags = oldSuppressFlags;
            cache.linkAtts = linkAtts;
            mapP = cache.map;
        }

        mapP.attributed = metaAttributed;
        mapP.suppressFlags = newSuppressFlags;

        if (metaAttributed != null)
        {
            ConstPtr<AttributeDefinitionList> metaAttDef = metaAttributed.attributeDef();
            nuint metaDefSize = metaAttDef.isNull() ? 1 : metaAttDef.pointer()!.size() + 1;
            Vector<PackedBoolean> renamed = new Vector<PackedBoolean>();
            for (nuint i = 0; i < metaDefSize; i++)
                renamed.push_back(false);

            nuint attsSize = atts.def().isNull() ? 1 : atts.def().pointer()!.size() + 1;
            nuint linkAttsSize = (linkAtts != null && !linkAtts.def().isNull()) ? linkAtts.def().pointer()!.size() : 0;
            Vector<PackedBoolean> substituted = new Vector<PackedBoolean>();
            for (nuint i = 0; i < attsSize + linkAttsSize; i++)
                substituted.push_back(false);

            if (linkAtts != null)
            {
                Boolean specified;
                uint index;
                Text? linkNamerText = considerNamerSpec(linkAtts, out specified, out index);
                if (linkNamerText != null)
                    buildAttributeMapRename(mapP, linkNamerText, atts, linkAtts, renamed, substituted, isNotation);
            }

            if (namerText != null)
                buildAttributeMapRename(mapP, namerText, atts, null, renamed, substituted, isNotation);

            buildAttributeMapRest(mapP, atts, linkAtts, renamed);
        }

        return mapP;
    }

    // void considerSupr(...);
    private void considerSupr(AttributeList atts, AttributeList? linkAtts,
                               ref uint thisSuppressFlags, ref uint newSuppressFlags,
                               ref Boolean inhibitCache, out uint arcSuprIndex)
    {
        arcSuprIndex = ArcConstants.invalidAtt;
        if ((thisSuppressFlags & suppressSupr) != 0)
            return;
        if (supportAtts_[(int)ReservedName.rArcSuprA].size() == 0)
            return;

        AttributeValue? val = null;
        uint tem;
        if (linkAtts != null && linkAtts.attributeIndex(supportAtts_[(int)ReservedName.rArcSuprA], out tem))
            val = linkAtts.value(tem);
        else if (atts.attributeIndex(supportAtts_[(int)ReservedName.rArcSuprA], out arcSuprIndex))
        {
            if (atts.current(arcSuprIndex) || atts.specified(arcSuprIndex))
                inhibitCache = true;
            val = atts.value(arcSuprIndex);
        }
        else
            return;

        if (val == null)
            return;
        Text? textP = val.text();
        if (textP == null)
            return;

        StringC token = new StringC(textP.@string());
        docSyntax_.pointer()?.generalSubstTable()?.subst(token);

        thisSuppressFlags &= ~suppressForm;
        newSuppressFlags &= ~(suppressForm | suppressSupr);

        if (matchName(token, "sArcForm"))
            newSuppressFlags |= suppressForm;
        else if (matchName(token, "sArcAll"))
            newSuppressFlags |= (suppressSupr | suppressForm);
        else if (!matchName(token, "sArcNone"))
        {
            setNextLocation(textP.charLocation(0));
            message(ArcEngineMessages.invalidSuppress, new StringMessageArg(token));
        }
    }

    // void considerIgnD(...);
    private void considerIgnD(AttributeList atts, AttributeList? linkAtts,
                               uint thisSuppressFlags, ref uint newSuppressFlags,
                               ref Boolean inhibitCache, out uint arcIgnDIndex)
    {
        arcIgnDIndex = ArcConstants.invalidAtt;
        if ((thisSuppressFlags & suppressSupr) != 0)
            return;
        if (supportAtts_[(int)ReservedName.rArcIgnDA].size() == 0)
            return;

        AttributeValue? val = null;
        uint tem;
        if (linkAtts != null && linkAtts.attributeIndex(supportAtts_[(int)ReservedName.rArcIgnDA], out tem))
            val = linkAtts.value(tem);
        else if (atts.attributeIndex(supportAtts_[(int)ReservedName.rArcIgnDA], out arcIgnDIndex))
        {
            if (atts.current(arcIgnDIndex) || atts.specified(arcIgnDIndex))
                inhibitCache = true;
            val = atts.value(arcIgnDIndex);
        }
        else
            return;

        if (val == null)
            return;
        Text? textP = val.text();
        if (textP == null)
            return;

        StringC token = new StringC(textP.@string());
        docSyntax_.pointer()?.generalSubstTable()?.subst(token);

        newSuppressFlags &= ~(ignoreData | condIgnoreData);

        if (matchName(token, "ArcIgnD"))
            newSuppressFlags |= ignoreData;
        else if (matchName(token, "cArcIgnD"))
            newSuppressFlags |= condIgnoreData;
        else if (!matchName(token, "nArcIgnD"))
        {
            setNextLocation(textP.charLocation(0));
            message(ArcEngineMessages.invalidIgnD, new StringMessageArg(token));
        }
    }

    // const Attributed *considerForm(...);
    private IAttributed? considerForm(AttributeList atts, AttributeList? linkAtts,
                                       StringC name, Boolean isNotation,
                                       uint thisSuppressFlags, ref uint newSuppressFlags,
                                       ref Boolean inhibitCache, out uint arcFormIndex)
    {
        arcFormIndex = ArcConstants.invalidAtt;

        if ((thisSuppressFlags & suppressForm) != 0 &&
            (supportAtts_[(int)ReservedName.rArcSuprF].size() == 0 ||
             (thisSuppressFlags & suppressSupr) != 0 ||
             isNotation))
            return null;

        uint tem;
        AttributeValue? val = null;
        if (linkAtts != null && linkAtts.attributeIndex(supportAtts_[(int)ReservedName.rArcFormA], out tem))
            val = linkAtts.value(tem);
        else if (atts.attributeIndex(supportAtts_[(int)ReservedName.rArcFormA], out arcFormIndex))
        {
            if (atts.current(arcFormIndex) || atts.specified(arcFormIndex))
                inhibitCache = true;
            val = atts.value(arcFormIndex);
        }
        else
            return autoForm(atts, name, isNotation, thisSuppressFlags, ref newSuppressFlags,
                           ref inhibitCache, out arcFormIndex);

        if (val == null)
            return null;
        Text? textP = val.text();
        if (textP == null)
            return null;

        StringC metaName = new StringC(textP.@string());
        metaSyntax_.pointer()?.generalSubstTable()?.subst(metaName);

        if (!isNotation)
        {
            IAttributed? metaAttributed = metaDtd_.pointer()?.lookupElementType(metaName);
            if (metaAttributed == null)
                metaAttributed = lookupCreateUndefinedElement(metaName, new Location(), metaDtd_.pointer()!);
            if (metaName.Equals(supportAtts_[(int)ReservedName.rArcSuprF]))
            {
                newSuppressFlags |= suppressForm;
                return metaAttributed;
            }
            if ((thisSuppressFlags & suppressForm) != 0)
                return null;
            return metaAttributed;
        }
        else
            return metaDtd_.pointer()?.lookupNotationConst(metaName).pointer();
    }

    // const Attributed *autoForm(...);
    private IAttributed? autoForm(AttributeList atts, StringC name, Boolean isNotation,
                                   uint thisSuppressFlags, ref uint newSuppressFlags,
                                   ref Boolean inhibitCache, out uint idIndex)
    {
        idIndex = ArcConstants.invalidAtt;

        if (!isNotation)
        {
            IAttributed? metaAttributed;
            if (openElementFlags_.size() == 0)
            {
                metaAttributed = metaDtd_.pointer()?.documentElementType();
                inhibitCache = true;
            }
            else
            {
                metaAttributed = null;
                if (arcAuto_)
                    metaAttributed = metaDtd_.pointer()?.lookupElementType(name);
                if (metaAttributed == null && supportAtts_[(int)ReservedName.rArcBridF].size() > 0)
                {
                    if (atts.idIndex(out idIndex) && atts.specified(idIndex))
                    {
                        inhibitCache = true;
                        metaAttributed = metaDtd_.pointer()?.lookupElementType(supportAtts_[(int)ReservedName.rArcBridF]);
                    }
                }
            }

            if (metaAttributed != null && name.Equals(supportAtts_[(int)ReservedName.rArcSuprF]))
                newSuppressFlags = suppressForm | ignoreData;
            else if ((thisSuppressFlags & suppressForm) != 0)
                return null;

            return metaAttributed;
        }
        else if ((thisSuppressFlags & suppressForm) != 0)
            return null;
        else
        {
            IAttributed? metaAttributed = null;
            if (arcAuto_)
                metaAttributed = metaDtd_.pointer()?.lookupNotationConst(name).pointer();
            if (metaAttributed == null && supportAtts_[(int)ReservedName.rArcDataF].size() > 0)
                metaAttributed = metaDtd_.pointer()?.lookupNotationConst(supportAtts_[(int)ReservedName.rArcDataF]).pointer();
            return metaAttributed;
        }
    }

    // const Text *considerNamer(const AttributeList &, Boolean &, unsigned &);
    private Text? considerNamer(AttributeList atts, ref Boolean inhibitCache, out uint arcNamerIndex)
    {
        arcNamerIndex = ArcConstants.invalidAtt;
        if (supportAtts_[(int)ReservedName.rArcNamrA].size() == 0 ||
            !atts.attributeIndex(supportAtts_[(int)ReservedName.rArcNamrA], out arcNamerIndex))
            return null;
        if (atts.current(arcNamerIndex) || atts.specified(arcNamerIndex))
            inhibitCache = true;
        AttributeValue? val = atts.value(arcNamerIndex);
        if (val == null)
            return null;
        return val.text();
    }

    // Overload for link attributes
    private Text? considerNamerSpec(AttributeList atts, out Boolean specified, out uint index)
    {
        Boolean inhibit = false;
        Text? result = considerNamer(atts, ref inhibit, out index);
        specified = inhibit;
        return result;
    }

    // void buildAttributeMapRename(...);
    private void buildAttributeMapRename(MetaMap map, Text rename,
                                          AttributeList atts, AttributeList? linkAtts,
                                          Vector<PackedBoolean> attRenamed,
                                          Vector<PackedBoolean> attSubstituted,
                                          Boolean isNotation)
    {
        Vector<StringC> tokens = new Vector<StringC>();
        Vector<nuint> tokensPos = new Vector<nuint>();
        split(rename, docSyntax_.pointer()!.space(), tokens, tokensPos);

        ConstPtr<AttributeDefinitionList> metaAttDef = new ConstPtr<AttributeDefinitionList>();
        if (map.attributed != null)
            metaAttDef = map.attributed.attributeDef();

        for (nuint i = 0; i < tokens.size(); i += 2)
        {
            uint fromIndex = ArcConstants.invalidAtt;
            uint toIndex = ArcConstants.invalidAtt;
            metaSyntax_.pointer()?.generalSubstTable()?.subst(tokens[i]);

            if (!isNotation && tokens[i].Equals(rniArcCont_))
            {
                if (attRenamed[0])
                {
                    setNextLocation(rename.charLocation(tokensPos[i]));
                    message(ArcEngineMessages.arcContDuplicate);
                }
                else
                    toIndex = ArcConstants.contentPseudoAtt;
            }
            else if (metaAttDef.isNull() || !metaAttDef.pointer()!.attributeIndex(tokens[i], out toIndex))
            {
                setNextLocation(rename.charLocation(tokensPos[i]));
                message(ArcEngineMessages.renameToInvalid, new StringMessageArg(tokens[i]));
            }
            else if (attRenamed[toIndex + 1])
            {
                toIndex = ArcConstants.invalidAtt;
                setNextLocation(rename.charLocation(tokensPos[i]));
                message(ArcEngineMessages.renameToDuplicate, new StringMessageArg(tokens[i]));
            }

            if (i + 1 >= tokens.size())
            {
                setNextLocation(rename.charLocation(tokensPos[i]));
                message(ArcEngineMessages.renameMissingAttName);
            }
            else
            {
                docSyntax_.pointer()?.generalSubstTable()?.subst(tokens[i + 1]);

                if (!isNotation && tokens[i + 1].Equals(rniContent_))
                {
                    if (toIndex == ArcConstants.contentPseudoAtt)
                    {
                        setNextLocation(rename.charLocation(tokensPos[i + 1]));
                        message(ArcEngineMessages.arcContInvalid, new StringMessageArg(tokens[i + 1]));
                    }
                    else if (attSubstituted[0])
                    {
                        setNextLocation(rename.charLocation(tokensPos[i + 1]));
                        message(ArcEngineMessages.contentDuplicate);
                    }
                    else
                        fromIndex = ArcConstants.contentPseudoAtt;
                }
                else if (tokens[i + 1].Equals(rniDefault_))
                {
                    if (toIndex == ArcConstants.contentPseudoAtt)
                    {
                        setNextLocation(rename.charLocation(tokensPos[i + 1]));
                        message(ArcEngineMessages.arcContInvalid, new StringMessageArg(tokens[i + 1]));
                    }
                    else if (toIndex != ArcConstants.invalidAtt)
                        attRenamed[toIndex + 1] = true;
                }
                else if (linkAtts != null && linkAtts.attributeIndex(tokens[i + 1], out fromIndex))
                {
                    fromIndex += (uint)atts.size();
                    if (attSubstituted[fromIndex + 1])
                    {
                        fromIndex = ArcConstants.invalidAtt;
                        setNextLocation(rename.charLocation(tokensPos[i + 1]));
                        message(ArcEngineMessages.renameFromDuplicate, new StringMessageArg(tokens[i + 1]));
                    }
                }
                else if (!atts.attributeIndex(tokens[i + 1], out fromIndex))
                {
                    setNextLocation(rename.charLocation(tokensPos[i + 1]));
                    message(ArcEngineMessages.renameFromInvalid, new StringMessageArg(tokens[i + 1]));
                }
                else if (attSubstituted[fromIndex + 1])
                {
                    fromIndex = ArcConstants.invalidAtt;
                    setNextLocation(rename.charLocation(tokensPos[i + 1]));
                    message(ArcEngineMessages.renameFromDuplicate, new StringMessageArg(tokens[i + 1]));
                }
            }

            if (fromIndex != ArcConstants.invalidAtt && toIndex != ArcConstants.invalidAtt &&
                fromIndex != ArcConstants.contentPseudoAtt && toIndex != ArcConstants.contentPseudoAtt)
            {
                map.attMapFrom.push_back(fromIndex);
                map.attMapTo.push_back(toIndex);
                attRenamed[toIndex + 1] = true;
                attSubstituted[fromIndex + 1] = true;
                map.attTokenMapBase.push_back(map.tokenMapFrom.size());
            }
        }
    }

    // void buildAttributeMapRest(...);
    private void buildAttributeMapRest(MetaMap map, AttributeList atts,
                                        AttributeList? linkAtts,
                                        Vector<PackedBoolean> attRenamed)
    {
        ConstPtr<AttributeDefinitionList> metaAttDef = map.attributed?.attributeDef() ?? new ConstPtr<AttributeDefinitionList>();
        if (metaAttDef.isNull())
            return;

        for (uint i = 0; i < metaAttDef.pointer()!.size(); i++)
        {
            if (!attRenamed[i + 1])
            {
                uint fromIndex;
                if (metaAttDef.pointer()!.def(i)?.isId() == true)
                {
                    for (uint j = 0; j < atts.size(); j++)
                    {
                        if (atts.id(j))
                        {
                            map.attMapFrom.push_back(j);
                            map.attMapTo.push_back(i);
                            map.attTokenMapBase.push_back(map.tokenMapFrom.size());
                            break;
                        }
                    }
                }
                else if (linkAtts != null && linkAtts.attributeIndex(metaAttDef.pointer()!.def(i)!.name(), out fromIndex))
                {
                    map.attMapFrom.push_back(fromIndex + (uint)atts.size());
                    map.attMapTo.push_back(i);
                    map.attTokenMapBase.push_back((uint)map.tokenMapFrom.size());
                }
                else if (atts.attributeIndex(metaAttDef.pointer()!.def(i)!.name(), out fromIndex))
                {
                    map.attMapFrom.push_back(fromIndex);
                    map.attMapTo.push_back(i);
                    map.attTokenMapBase.push_back(map.tokenMapFrom.size());
                }
            }
        }
    }

    // Boolean mapAttributes(...);
    private Boolean mapAttributes(AttributeList from, AttributeList? fromLink,
                                   Text? content, AttributeList to,
                                   ref ConstPtr<AttributeValue> arcContent,
                                   MetaMap map)
    {
        arcContent = new ConstPtr<AttributeValue>();
        if (map.attributed != null)
            to.init(map.attributed.attributeDef());

        for (nuint i = 0; i < map.attMapFrom.size(); i++)
        {
            uint fromIndex = map.attMapFrom[i];
            AttributeList fromList = from;

            if (fromIndex != ArcConstants.contentPseudoAtt && fromIndex >= (uint)fromList.size())
            {
                if (fromLink == null)
                    continue;
                fromList = fromLink;
                fromIndex -= (uint)from.size();
            }

            if (map.attMapTo[i] == ArcConstants.contentPseudoAtt)
            {
                arcContent = fromList.valuePointer(fromIndex);
            }
            else
            {
                Text? fromText = null;
                Boolean fromTextTokenized = false;

                if (map.attMapFrom[i] == ArcConstants.contentPseudoAtt)
                {
                    if (content == null)
                        return false;
                    fromText = content;
                    if (arcContent.isNull())
                    {
                        Text empty = new Text();
                        arcContent = new ConstPtr<AttributeValue>(new CdataAttributeValue(empty));
                    }
                }
                else
                {
                    AttributeValue? value = fromList.value(fromIndex);
                    if (value != null)
                    {
                        fromText = value.text();
                        fromTextTokenized = fromList.tokenized(fromIndex);
                    }
                }

                if (fromText != null)
                {
                    Text tem = new Text();
                    if (!fromTextTokenized && to.tokenized(map.attMapTo[i]))
                        fromText.tokenize(docSyntax_.pointer()!.space(), tem);
                    else
                        tem = new Text(fromText);
                    to.setSpec(map.attMapTo[i], this);
                    uint specLength = 0;
                    to.setValue(map.attMapTo[i], tem, this, ref specLength);
                }
            }
        }

        if (map.attributed != null)
            to.finish(this);

        return true;
    }

    // Boolean matchName(const StringC &name, const char *key);
    private Boolean matchName(StringC name, string key)
    {
        if (name.size() != (nuint)key.Length)
            return false;
        Sd? sd = docSd_.pointer();
        if (sd == null)
            return false;
        StringC tem = sd.execToInternal(key);
        docSyntax_.pointer()?.generalSubstTable()?.subst(tem);
        return name.Equals(tem);
    }

    // void split(const Text &text, Char space, Vector<StringC> &tokens, Vector<size_t> &tokensPos);
    private void split(Text text, Char space, Vector<StringC> tokens, Vector<nuint> tokensPos)
    {
        StringC str = text.@string();
        for (nuint i = 0; ; )
        {
            while (i < str.size() && str[i] == space)
                i++;
            if (i >= str.size())
                break;
            nuint start = i;
            while (i < str.size() && str[i] != space)
                i++;
            tokens.push_back(str.substr(start, i - start));
            tokensPos.push_back(start);
        }
    }

    // Messenger implementation
    public override void dispatchMessage(Message msg)
    {
        mgr_?.dispatchMessage(msg);
    }

    public override void initMessage(Message msg)
    {
        mgr_?.initMessage(msg);
        if (valid_)
        {
            StringC rniPcdata = new StringC(metaSyntax_.pointer()!.delimGeneral((int)Syntax.DelimGeneral.dRNI));
            rniPcdata.operatorPlusAssign(metaSyntax_.pointer()!.reservedName(Syntax.ReservedName.rPCDATA));
            getOpenElementInfo(msg.openElementInfo, rniPcdata);
        }
    }
}

// Main implementation class
internal class ArcEngineImpl : DelegateEventHandler
{
    private EventHandler? eventHandler_;
    private Vector<ArcProcessor> arcProcessors_ = new Vector<ArcProcessor>();
    private ConstPtr<Sd> sd_ = new ConstPtr<Sd>();
    private ConstPtr<Syntax> syntax_ = new ConstPtr<Syntax>();
    private StringC is10744_ = new StringC();
    private StringC arcBase_ = new StringC();
    private StringC namespaceDelim_ = new StringC();
    private StringC arch_ = new StringC();
    private StringC uselex_ = new StringC();
    private ConstPtr<AttributeDefinitionList> archPiAttributeDefs_ = new ConstPtr<AttributeDefinitionList>();
    private int stage_;
    private QueueEventHandler eventQueue_ = new QueueEventHandler();
    private NullEventHandler nullHandler_;
    private SgmlParser? parser_;
    private Location currentLocation_ = new Location();
    private uint gatheringContent_;
    private Text content_ = new Text();
    private uint startAgain_;
    private Allocator alloc_;
    private StringC appinfo_ = new StringC();
    private AttributeList? linkAttributes_;
    private LinkProcess linkProcess_ = new LinkProcess();
    private Boolean haveLinkProcess_;
    private Vector<StringC> docName_ = new Vector<StringC>();
    private ArcDirector? director_;
    private Messenger? mgr_;
    private int cancelPtr_;

    // ArcEngineImpl constructor
    public ArcEngineImpl(Messenger mgr,
                         SgmlParser parser,
                         ArcDirector director,
                         int cancelPtr,
                         StringC? arcPublicId,
                         Notation? notation,
                         Vector<StringC> docName,
                         SubstTable? table)
    {
        director_ = director;
        mgr_ = mgr;
        cancelPtr_ = cancelPtr;
        parser_ = parser;
        stage_ = 0;
        gatheringContent_ = 0;
        startAgain_ = 0;
        haveLinkProcess_ = false;
        alloc_ = new Allocator(256, 50);
        nullHandler_ = new NullEventHandler(mgr);
        docName_ = docName;

        eventHandler_ = director.arcEventHandler(arcPublicId, notation, docName, table);
        if (eventHandler_ == null)
            eventHandler_ = nullHandler_;
        delegateTo_ = eventHandler_;
    }

    public nuint nBases() { return arcProcessors_.size(); }
    public EventHandler? delegateHandler() { return eventHandler_; }

    public override void sgmlDecl(SgmlDeclEvent? ev)
    {
        if (ev == null) return;

        currentLocation_ = ev.location();
        sd_ = ev.sdPointer();
        syntax_ = ev.instanceSyntaxPointer();

        Sd? sd = sd_.pointer();
        if (sd != null)
        {
            arcBase_ = sd.execToInternal("ArcBase");
            syntax_.pointer()?.generalSubstTable()?.subst(arcBase_);
            is10744_ = sd.execToInternal("IS10744");
            arch_ = sd.execToInternal("arch");
            syntax_.pointer()?.generalSubstTable()?.subst(arch_);
            uselex_ = sd.execToInternal("USELEX");
            namespaceDelim_ = sd.execToInternal(":");
        }

        // Check appinfo for ArcBase
        Boolean atStart = true;
        for (nuint i = 0; i < appinfo_.size(); i++)
        {
            Syntax? syn = syntax_.pointer();
            if (syn != null && syn.isS((int)appinfo_[i]))
                atStart = true;
            else if (atStart)
            {
                if (i + 7 > appinfo_.size())
                    break;
                StringC tem = appinfo_.substr(i, 7);
                syn?.generalSubstTable()?.subst(tem);
                if (tem.Equals(arcBase_))
                {
                    if (i + 7 == appinfo_.size() || (syn != null && syn.isS((int)appinfo_[i + 7])))
                        break;
                    if (sd != null && appinfo_[i + 7] == sd.execToInternal((sbyte)'='))
                    {
                        arcBase_.resize(0);
                        for (nuint j = i + 8; j < appinfo_.size(); j++)
                        {
                            if (syn != null && syn.isS((int)appinfo_[j]))
                                break;
                            arcBase_.operatorPlusAssign(appinfo_[j]);
                        }
                        syn?.generalSubstTable()?.subst(arcBase_);
                        break;
                    }
                }
                atStart = false;
            }
        }

        base.sgmlDecl(ev);
    }

    public override void appinfo(AppinfoEvent? ev)
    {
        if (ev == null) return;

        StringC? str;
        if (ev.literal(out str) && str != null)
            appinfo_ = new StringC(str);
        base.appinfo(ev);
    }

    public override void startDtd(StartDtdEvent? ev)
    {
        stage_++;
        base.startDtd(ev);
    }

    public override void endDtd(EndDtdEvent? ev)
    {
        stage_++;
        base.endDtd(ev);
    }

    public override void startLpd(StartLpdEvent? ev)
    {
        if (ev != null && ev.active())
            stage_ = 1;
        base.startLpd(ev);
    }

    public override void endLpd(EndLpdEvent? ev)
    {
        stage_++;
        base.endLpd(ev);
    }

    public override void endProlog(EndPrologEvent? ev)
    {
        if (ev == null) return;

        currentLocation_ = ev.location();

        for (nuint i = 0; i < arcProcessors_.size(); i++)
        {
            arcProcessors_[i].init(ev, sd_, syntax_, parser_, mgr_,
                                   docName_, arcProcessors_, director_!, cancelPtr_);
        }

        if (!ev.lpdPointer().isNull())
        {
            haveLinkProcess_ = true;
            linkProcess_.init(ev.lpdPointer());
        }

        base.endProlog(ev);
    }

    public override void startElement(StartElementEvent? ev)
    {
        if (ev == null) return;

        if (gatheringContent_ > 0)
        {
            gatheringContent_++;
            base.startElement(ev);
            return;
        }

        currentLocation_ = ev.location();
        Text? contentP;
        nuint start;

        if (startAgain_ > 0)
        {
            start = startAgain_ - 1;
            contentP = content_;
            startAgain_ = 0;
        }
        else
        {
            contentP = null;
            start = 0;
            if (haveLinkProcess_)
            {
                ResultElementSpec? resultElementSpec;
                linkProcess_.startElement(ev.elementType(), ev.attributes(), ev.location(),
                                          mgr_!, out linkAttributes_, out resultElementSpec);
            }
            else
                linkAttributes_ = null;
        }

        for (nuint i = start; i < arcProcessors_.size(); i++)
        {
            if (arcProcessors_[i].valid())
            {
                if (!arcProcessors_[i].processStartElement(ev, linkAttributes_, contentP, alloc_))
                {
                    startAgain_ = (uint)(i + 1);
                    gatheringContent_ = 1;
                    delegateTo_ = eventQueue_;
                    base.startElement(ev);
                    return;
                }
            }
        }

        content_.clear();
        base.startElement(ev);
    }

    public override void endElement(EndElementEvent? ev)
    {
        if (ev == null) return;

        while (gatheringContent_ > 0)
        {
            if (--gatheringContent_ > 0)
            {
                base.endElement(ev);
                return;
            }
            delegateTo_ = delegateHandler();
            IQueue<Event> tem = new IQueue<Event>();
            eventQueue_.swap(tem);
            while (!tem.empty())
            {
                Event? e = tem.get();
                e?.handle(this);
            }
        }

        currentLocation_ = ev.location();
        for (nuint i = 0; i < arcProcessors_.size(); i++)
        {
            if (arcProcessors_[i].valid())
                arcProcessors_[i].processEndElement(ev, alloc_);
        }

        base.endElement(ev);
        if (haveLinkProcess_)
            linkProcess_.endElement();
    }

    public override void data(DataEvent? ev)
    {
        if (ev == null) return;

        if (gatheringContent_ > 0)
        {
            Entity? entity = ev.entity();
            if (entity != null)
            {
                content_.addCdata(entity.asInternalEntity()!.@string(), ev.location().origin()!);
            }
            else
            {
                Location loc = new Location(ev.location());
                Syntax? syn = syntax_.pointer();
                nuint dataOffset = ev.dataOffset();
                for (nuint i = 0; i < ev.dataLength(); i++)
                {
                    Char ch = ev.data()![dataOffset + i];
                    if (syn != null && syn.isS((int)ch) && ch != syn.space())
                    {
                        if (ch == syn.standardFunction((int)Syntax.StandardFunction.fRS))
                            content_.ignoreChar(ch, loc);
                        else
                            content_.addChar(syn.space(), loc);
                    }
                    else
                        content_.addChar(ch, loc);
                }
            }
        }
        else
        {
            currentLocation_ = ev.location();
            for (nuint i = 0; i < arcProcessors_.size(); i++)
            {
                if (arcProcessors_[i].valid() && arcProcessors_[i].processData())
                {
                    Entity? entity = ev.entity();
                    if (entity != null)
                    {
                        CdataEntityEvent cdataEv = new CdataEntityEvent(entity.asInternalEntity()!,
                                                                         ev.location().origin()!);
                        arcProcessors_[i].docHandler().data(cdataEv);
                    }
                    else
                    {
                        ImmediateDataEvent dataEv = new ImmediateDataEvent(ev.type(),
                                                                            ev.data(),
                                                                            ev.dataOffset(),
                                                                            ev.dataLength(),
                                                                            ev.location(),
                                                                            false);
                        arcProcessors_[i].docHandler().data(dataEv);
                    }
                }
            }
        }

        base.data(ev);
    }

    public override void sdataEntity(SdataEntityEvent? ev)
    {
        if (ev == null) return;

        if (gatheringContent_ > 0)
        {
            content_.addSdata(ev.entity()!.asInternalEntity()!.@string(), ev.location().origin()!);
            return;
        }
        else
        {
            currentLocation_ = ev.location();
            for (nuint i = 0; i < arcProcessors_.size(); i++)
            {
                if (arcProcessors_[i].valid() && arcProcessors_[i].processData())
                {
                    Entity? entity = ev.entity();
                    SdataEntityEvent sdataEv = new SdataEntityEvent(entity!.asInternalEntity()!,
                                                                     ev.location().origin()!);
                    arcProcessors_[i].docHandler().sdataEntity(sdataEv);
                }
            }
        }

        base.sdataEntity(ev);
    }

    public override void externalDataEntity(ExternalDataEntityEvent? ev)
    {
        if (ev == null) return;

        if (gatheringContent_ == 0)
        {
            currentLocation_ = ev.location();
            for (nuint i = 0; i < arcProcessors_.size(); i++)
            {
                if (arcProcessors_[i].valid() && arcProcessors_[i].processData())
                {
                    ConstPtr<Entity> entity = arcProcessors_[i].dtdPointer().pointer()!
                        .lookupEntity(false, ev.entity()!.name());
                    if (!entity.isNull())
                    {
                        ConstPtr<EntityOrigin> oldOrigin = ev.entityOrigin();
                        Owner<Markup>? markup = null;
                        if (oldOrigin.pointer()?.markup() != null)
                            markup = new Owner<Markup>(new Markup(oldOrigin.pointer()!.markup()!));
                        ConstPtr<EntityOrigin> newOrigin = new ConstPtr<EntityOrigin>(
                            EntityOrigin.make(entity,
                                              oldOrigin.pointer()!.parent(),
                                              oldOrigin.pointer()!.refLength(),
                                              markup!));
                        ExternalDataEntityEvent extEv = new ExternalDataEntityEvent(
                            entity.pointer()!.asExternalDataEntity()!, newOrigin);
                        arcProcessors_[i].docHandler().externalDataEntity(extEv);
                    }
                }
            }
        }

        base.externalDataEntity(ev);
    }

    public override void pi(PiEvent? ev)
    {
        if (ev == null) return;

        currentLocation_ = ev.location();

        if (stage_ == 1 && ev.dataLength() > is10744_.size() + 1)
        {
            Boolean match = true;
            nuint i = 0;
            nuint dataOffset = ev.dataOffset();
            Char[]? data = ev.data();
            SubstTable? substTable = syntax_.pointer()?.generalSubstTable();

            for (nuint j = 0; j < is10744_.size() && match; i++, j++)
            {
                Char c = substTable != null ? substTable[data![dataOffset + i]] : data![dataOffset + i];
                if (c != is10744_[j])
                    match = false;
            }

            if (match)
            {
                Syntax? syn = syntax_.pointer();
                if ((ev.dataLength() - i) < namespaceDelim_.size())
                    match = false;
                else
                {
                    for (nuint j = 0; j < namespaceDelim_.size() && match; j++)
                    {
                        Char c = substTable != null ? substTable[data![dataOffset + i + j]] : data![dataOffset + i + j];
                        if (c != namespaceDelim_[j])
                            match = false;
                    }
                }

                if (match || (syn != null && syn.isS((int)data![dataOffset + i])))
                {
                    if (match)
                        i += namespaceDelim_.size();
                    else
                    {
                        do { i++; } while (i < ev.dataLength() && syn!.isS((int)data![dataOffset + i]));
                    }

                    if (i < ev.dataLength())
                    {
                        StringC token = new StringC();
                        while (i < ev.dataLength() && (syn == null || !syn.isS((int)data![dataOffset + i])))
                        {
                            Char c = substTable != null ? substTable[data![dataOffset + i]] : data![dataOffset + i];
                            token.operatorPlusAssign(c);
                            i++;
                        }

                        if (!match && token.Equals(arcBase_))
                        {
                            nuint dataLength = ev.dataLength();
                            // Note: data and dataOffset were already defined above
                            for (; ; )
                            {
                                while (i < dataLength && syn!.isS((int)data![dataOffset + i]))
                                    i++;
                                if (i >= dataLength)
                                    break;
                                nuint start = i++;
                                while (i < dataLength && !syn!.isS((int)data![dataOffset + i]))
                                    i++;
                                // Create StringC from substring of the data array
                                StringC name = new StringC();
                                for (nuint k = start; k < i; k++)
                                    name.operatorPlusAssign(data![dataOffset + k]);
                                substTable?.subst(name);
                                arcProcessors_.resize(arcProcessors_.size() + 1);
                                Location loc = new Location(ev.location());
                                loc.operatorPlusAssign((uint)start);
                                arcProcessors_.back().setName(name, loc);
                            }
                        }
                    }
                }
            }
        }

        base.pi(ev);
    }

    public override void uselink(UselinkEvent? ev)
    {
        if (ev != null && gatheringContent_ == 0 && haveLinkProcess_)
        {
            linkProcess_.uselink(ev.linkSet(), ev.restore(), ev.lpd().pointer());
        }
        base.uselink(ev);
    }

    public void dispatchMessage(Message msg)
    {
        mgr_?.dispatchMessage(msg);
    }
}
