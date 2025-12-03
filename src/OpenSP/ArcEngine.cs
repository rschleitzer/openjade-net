// Copyright (c) 1996 James Clark, 2000 Matthias Clasen
// Copyright (c) 2001 Epremis Corp.
// See the file COPYING for copying permission.

namespace OpenSP;

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
    public void dispatchMessage(Message msg)
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

    // Private constructor - cannot instantiate
    private ArcEngine()
    {
    }
}

// Delegate event handler - passes events to another handler
internal class DelegateEventHandler : EventHandler
{
    protected EventHandler? delegateTo_;

    public override void data(DataEvent? ev) { delegateTo_?.data(ev); }
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
    public override void startElement(StartElementEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void endElement(EndElementEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void pi(PiEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void sdataEntity(SdataEntityEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void externalDataEntity(ExternalDataEntityEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }
    public override void message(MessageEvent? ev) { if (ev != null) { ev.copyData(); queue_.append(ev); } }

    public Event? get() { return queue_.get(); }
    public Boolean empty() { return queue_.empty(); }
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
            // C# GC handles deletion
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
        }

        public void clear()
        {
            attributed = null;
            suppressFlags = 0;
            attMapFrom.clear();
            attMapTo.clear();
            attTokenMapBase.clear();
            tokenMapFrom.clear();
            tokenMapTo.clear();
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
                noSpec[i] = unchecked((uint)-1);
            suppressFlags = 0;
            linkAtts = null;
        }

        public void clear()
        {
            map.clear();
            for (int i = 0; i < nNoSpec; i++)
                noSpec[i] = unchecked((uint)-1);
            suppressFlags = 0;
            linkAtts = null;
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
#pragma warning disable CS0414 // Field is assigned but never used
    private Boolean arcDtdIsParam_;
    private Boolean arcAuto_;
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
#pragma warning disable CS0414 // Field is assigned but never used
    private nuint docIndex_;
#pragma warning restore CS0414

    // ArcProcessor();
    public ArcProcessor()
    {
        valid_ = false;
        piDecl_ = false;
        arcDtdIsParam_ = false;
        arcAuto_ = false;
        errorIdref_ = false;
        docIndex_ = 0;

        for (int i = 0; i < (int)ReservedName.nReserve; i++)
        {
            supportAtts_[i] = new StringC();
            supportAttsText_[i] = null;
        }
    }

    // void setName(const StringC &name, const Location &loc);
    public void setName(StringC name, Location loc)
    {
        name_ = new StringC(name);
        declLoc_ = loc;
        piDecl_ = false;
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

    // void setPiDecl(const Location &loc, const StringC &attspecText, Index attspecIndex,
    //                const ConstPtr<AttributeDefinitionList> &archPiAttributeDefs);
    public void setPiDecl(Location loc, StringC attspecText, Index attspecIndex,
                          ConstPtr<AttributeDefinitionList> archPiAttributeDefs)
    {
        declLoc_ = loc;
        piDecl_ = true;
        piDeclAttspecText_ = new StringC(attspecText);
        piDeclAttspecIndex_ = attspecIndex;
        archPiAttributeDefs_ = archPiAttributeDefs;
    }

    // void init(const EndPrologEvent &, const ConstPtr<Sd> &, const ConstPtr<Syntax> &,
    //           const SgmlParser *, Messenger *, const Vector<StringC> &,
    //           const NCVector<ArcProcessor> &, ArcDirector &, const volatile sig_atomic_t *);
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
        mgr_ = mgr;
        docSd_ = sd;
        docSyntax_ = syntax;
        director_ = director;
        valid_ = true;

        // Initialize reserved name strings
        initReservedNames();

        // Get the document DTD
        docDtd_ = ev.dtdPointer();
        if (docDtd_.isNull())
        {
            valid_ = false;
            return;
        }

        // Look for architecture notation
        ConstPtr<Notation> notation = docDtd_.pointer()!.lookupNotationConst(name_);
        if (notation.isNull())
        {
            if (mgr_ != null)
            {
                mgr_.setNextLocation(declLoc_);
                mgr_.message(ArcEngineMessages.noArcNotation, new StringMessageArg(name_));
            }
            valid_ = false;
            return;
        }

        // Get event handler from director
        Vector<StringC> fullName = new Vector<StringC>(superName);
        fullName.push_back(name_);
        docHandler_ = director.arcEventHandler(null, notation.pointer(), fullName, syntax.pointer()?.generalSubstTable());
        if (docHandler_ == null)
        {
            docHandler_ = new NullEventHandler(mgr ?? new NullMessenger());
        }
    }

    private void initReservedNames()
    {
        if (docSd_.isNull() || docSyntax_.isNull())
            return;

        Sd sd = docSd_.pointer()!;
        SubstTable? substTable = docSyntax_.pointer()?.generalSubstTable();

        string[] names = {
            "ArcName", "ArcPubid", "ArcFormA", "ArcNamrA", "ArcSuprA",
            "ArcIgnDA", "ArcDocF", "ArcSuprF", "ArcBridF", "ArcDataF",
            "ArcAuto", "ArcDTD", "ArcDtdPubid", "ArcDtdSysid", "ArcQuant"
        };

        for (int i = 0; i < names.Length && i < (int)ReservedName.nReserve; i++)
        {
            supportAtts_[i] = sd.execToInternal(names[i]);
            substTable?.subst(supportAtts_[i]);
        }

        rniContent_ = sd.execToInternal("#CONTENT");
        substTable?.subst(rniContent_);
        rniArcCont_ = sd.execToInternal("#ARCCONT");
        substTable?.subst(rniArcCont_);
        rniDefault_ = sd.execToInternal("#DEFAULT");
        substTable?.subst(rniDefault_);
        rniMaptoken_ = sd.execToInternal("#MAPTOKEN");
        substTable?.subst(rniMaptoken_);
    }

    // Boolean processStartElement(const StartElementEvent &, const AttributeList *,
    //                             const Text *, Allocator &);
    public Boolean processStartElement(StartElementEvent ev,
                                       AttributeList? linkAttributes,
                                       Text? content,
                                       Allocator alloc)
    {
        if (!valid_)
            return true;

        // Process the element for this architecture
        // This is a simplified implementation
        return true;
    }

    // void processEndElement(const EndElementEvent &, Allocator &);
    public void processEndElement(EndElementEvent ev, Allocator alloc)
    {
        if (!valid_)
            return;

        // Process end element for this architecture
    }

    // Boolean processData();
    public Boolean processData()
    {
        if (!valid_)
            return false;

        // Check if data is architectural
        return openElementFlags_.size() > 0 && (openElementFlags_.back() & isArc) != 0;
    }

    // void checkIdrefs();
    public void checkIdrefs()
    {
        if (!valid_ || errorIdref_)
            return;

        // Check all IDREF references
        IdTableIter iter = new IdTableIter(idTable_);
        Id? id;
        while ((id = iter.next()) != null)
        {
            if (!id.defined())
            {
                if (mgr_ != null)
                {
                    mgr_.setNextLocation(id.firstRefLocation());
                    mgr_.message(ArcEngineMessages.missingId, new StringMessageArg(id.name()));
                }
            }
        }
    }

    // AttributeContext implementation
    public override Syntax attributeSyntax()
    {
        return metaSyntax_.pointer() ?? docSyntax_.pointer()!;
    }

    // Messenger implementation
    public override void dispatchMessage(Message msg)
    {
        mgr_?.dispatchMessage(msg);
    }

    public new ConstPtr<Notation> getAttributeNotation(StringC name, Location loc)
    {
        if (metaDtd_.isNull())
            return new ConstPtr<Notation>();
        return metaDtd_.pointer()!.lookupNotationConst(name);
    }

    public new ConstPtr<Entity> getAttributeEntity(StringC name, Location loc)
    {
        if (metaDtd_.isNull())
            return new ConstPtr<Entity>();
        return metaDtd_.pointer()!.lookupEntity(false, name);
    }

    public new void noteCurrentAttribute(nuint i, AttributeValue? value)
    {
        if (i >= currentAttributes_.size())
            currentAttributes_.resize(i + 1);
        currentAttributes_[i] = new ConstPtr<AttributeValue>(value);
    }

    public new ConstPtr<AttributeValue> getCurrentAttribute(nuint i)
    {
        if (i < currentAttributes_.size())
            return currentAttributes_[i];
        return new ConstPtr<AttributeValue>();
    }

    public Boolean defineId(StringC str, Location loc, out Location prevLoc)
    {
        prevLoc = new Location();
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

    public new void noteIdref(StringC str, Location loc)
    {
        Id? id = lookupCreateId(str);
        id?.noteIdref(loc);
    }

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
}

// Main implementation class - implements Messenger via delegation
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
#pragma warning disable CS0414 // Field is assigned but never used
    private uint gatheringContent_;
#pragma warning restore CS0414
    private Text content_ = new Text();
#pragma warning disable CS0414 // Field is assigned but never used
    private uint startAgain_;
#pragma warning restore CS0414
    private Allocator alloc_;
    private StringC appinfo_ = new StringC();
#pragma warning disable CS0649 // Field is never assigned
    private AttributeList? linkAttributes_;
#pragma warning restore CS0649
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
        alloc_ = new Allocator(256, 50);  // Approximate sizes
        nullHandler_ = new NullEventHandler(mgr);
        docName_ = docName;

        eventHandler_ = director.arcEventHandler(arcPublicId, notation, docName, table);
        if (eventHandler_ == null)
            eventHandler_ = nullHandler_;
        delegateTo_ = eventHandler_;
    }

    // nuint nBases() const;
    public nuint nBases() { return arcProcessors_.size(); }

    // EventHandler *delegateHandler();
    public EventHandler? delegateHandler() { return eventHandler_; }

    // void sgmlDecl(SgmlDeclEvent *);
    public override void sgmlDecl(SgmlDeclEvent? ev)
    {
        if (ev == null) return;

        currentLocation_ = ev.location();
        sd_ = ev.sdPointer();
        syntax_ = ev.instanceSyntaxPointer();

        // Initialize architecture keywords
        Sd sd = sd_.pointer()!;
        arcBase_ = sd.execToInternal("ArcBase");
        syntax_.pointer()?.generalSubstTable()?.subst(arcBase_);
        is10744_ = sd.execToInternal("IS10744");
        arch_ = sd.execToInternal("arch");
        syntax_.pointer()?.generalSubstTable()?.subst(arch_);
        uselex_ = sd.execToInternal("USELEX");
        namespaceDelim_ = sd.execToInternal(":");

        base.sgmlDecl(ev);
    }

    // void appinfo(AppinfoEvent *);
    public override void appinfo(AppinfoEvent? ev)
    {
        if (ev == null) return;

        StringC? str;
        if (ev.literal(out str) && str != null)
            appinfo_ = new StringC(str);
        base.appinfo(ev);
    }

    // void startDtd(StartDtdEvent *);
    public override void startDtd(StartDtdEvent? ev)
    {
        stage_++;
        base.startDtd(ev);
    }

    // void endDtd(EndDtdEvent *);
    public override void endDtd(EndDtdEvent? ev)
    {
        stage_++;
        base.endDtd(ev);
    }

    // void startLpd(StartLpdEvent *);
    public override void startLpd(StartLpdEvent? ev)
    {
        if (ev != null && ev.active())
            stage_ = 1;
        base.startLpd(ev);
    }

    // void endLpd(EndLpdEvent *);
    public override void endLpd(EndLpdEvent? ev)
    {
        stage_++;
        base.endLpd(ev);
    }

    // void endProlog(EndPrologEvent *);
    public override void endProlog(EndPrologEvent? ev)
    {
        if (ev == null) return;

        currentLocation_ = ev.location();

        // Initialize all arc processors
        for (nuint i = 0; i < arcProcessors_.size(); i++)
        {
            arcProcessors_[i].init(ev, sd_, syntax_, parser_, mgr_,
                                   docName_, arcProcessors_, director_!, cancelPtr_);
        }

        // Initialize link processing if available
        if (!ev.lpdPointer().isNull())
        {
            haveLinkProcess_ = true;
            linkProcess_.init(ev.lpdPointer());
        }

        base.endProlog(ev);
    }

    // void startElement(StartElementEvent *);
    public override void startElement(StartElementEvent? ev)
    {
        if (ev == null) return;

        currentLocation_ = ev.location();

        // Process through all arc processors
        for (nuint i = 0; i < arcProcessors_.size(); i++)
        {
            if (arcProcessors_[i].valid())
            {
                arcProcessors_[i].processStartElement(ev, linkAttributes_, null, alloc_);
            }
        }

        base.startElement(ev);
    }

    // void endElement(EndElementEvent *);
    public override void endElement(EndElementEvent? ev)
    {
        if (ev == null) return;

        // Process through all arc processors
        for (nuint i = 0; i < arcProcessors_.size(); i++)
        {
            if (arcProcessors_[i].valid())
            {
                arcProcessors_[i].processEndElement(ev, alloc_);
            }
        }

        base.endElement(ev);
    }

    // void data(DataEvent *);
    public override void data(DataEvent? ev)
    {
        if (ev == null) return;

        // Check if any processor wants the data
        for (nuint i = 0; i < arcProcessors_.size(); i++)
        {
            if (arcProcessors_[i].processData())
            {
                // Data is architectural for this processor
            }
        }

        base.data(ev);
    }

    // void pi(PiEvent *);
    public override void pi(PiEvent? ev)
    {
        if (ev == null) return;

        currentLocation_ = ev.location();
        // PI processing for architecture declarations
        // This is a simplified implementation

        base.pi(ev);
    }

    // void uselink(UselinkEvent *);
    public override void uselink(UselinkEvent? ev)
    {
        if (ev != null && haveLinkProcess_)
        {
            linkProcess_.uselink(ev.linkSet(), ev.restore(), ev.lpd().pointer());
        }
        base.uselink(ev);
    }

    // Messenger-like functionality via delegation
    public void dispatchMessage(Message msg)
    {
        mgr_?.dispatchMessage(msg);
    }
}
