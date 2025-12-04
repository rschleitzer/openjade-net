// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Event : Link
{
    public enum Type
    {
        message,
        characterData,
        startElement,
        endElement,
        pi,
        sdataEntity,
        externalDataEntity,
        subdocEntity,
        nonSgmlChar,
        appinfo,
        startDtd,
        endDtd,
        startLpd,
        endLpd,
        endProlog,
        sgmlDecl,
        uselink,
        usemap,
        commentDecl,
        sSep,
        ignoredRs,
        ignoredRe,
        reOrigin,
        ignoredChars,
        markedSectionStart,
        markedSectionEnd,
        entityStart,
        entityEnd,
        notationDecl,
        entityDecl,
        elementDecl,
        attlistDecl,            // not #NOTATION and not in LPD
        attlistNotationDecl,
        linkAttlistDecl,
        linkDecl,
        idLinkDecl,
        shortrefDecl,
        ignoredMarkup,
        entityDefaulted,
        sgmlDeclEntity
    }

    private Type type_;

    // Event(Type);
    public Event(Type type)
    {
        type_ = type;
    }

    // virtual ~Event();
    // C# handles via GC

    // virtual void handle(EventHandler &) = 0;
    public virtual void handle(EventHandler handler)
    {
    }

    // virtual void copyData();
    public virtual void copyData()
    {
    }

    // Type type() const;
    public Type type()
    {
        return type_;
    }
}

public class LocatedEvent : Event
{
    private Location location_ = new Location();

    // LocatedEvent(Type type, const Location &);
    public LocatedEvent(Type type, Location location)
        : base(type)
    {
        location_ = new Location(location);
    }

    // const Location &location() const;
    public Location location()
    {
        return location_;
    }
}

public class MarkupEvent : LocatedEvent
{
    private Markup markup_ = new Markup();

    // MarkupEvent(Type type);
    public MarkupEvent(Type type)
        : base(type, new Location())
    {
    }

    // MarkupEvent(Type type, const Location &, Markup *);
    public MarkupEvent(Type type, Location loc, Markup? markup)
        : base(type, loc)
    {
        if (markup != null)
            markup.swap(markup_);
    }

    // const Markup &markup() const;
    public Markup markup()
    {
        return markup_;
    }
}

public class MessageEvent : Event
{
    private Message message_ = new Message();

    // MessageEvent(Message &);
    public MessageEvent(Message m)
        : base(Type.message)
    {
        m.swap(message_);
    }

    // MessageEvent(const Message &);
    public MessageEvent(Message m, Boolean copy)
        : base(Type.message)
    {
        if (copy)
            message_ = new Message(m);
        else
            m.swap(message_);
    }

    // ~MessageEvent();
    // C# handles via GC

    // const Message &message() const;
    public Message message()
    {
        return message_;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.message(this);
    }
}

public class StartElementEvent : LocatedEvent
{
    private ElementType? elementType_;
    private ConstPtr<Dtd> dtd_ = new ConstPtr<Dtd>();
    private PackedBoolean included_;
    private PackedBoolean copied_;  // has copyData() been called
    private Markup? markup_;
    private AttributeList? attributes_;

    // StartElementEvent(const ElementType *, const ConstPtr<Dtd> &, AttributeList *, const Location &, Markup *);
    public StartElementEvent(ElementType? elementType,
                             ConstPtr<Dtd> dtd,
                             AttributeList? attributes,
                             Location startLocation,
                             Markup? markup)
        : base(Type.startElement, startLocation)
    {
        elementType_ = elementType;
        dtd_ = dtd;
        included_ = false;
        copied_ = false;
        markup_ = markup;
        attributes_ = attributes;
    }

    // ~StartElementEvent();
    // C# handles via GC

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.startElement(this);
    }

    // Boolean mustOmitEnd() const;
    public Boolean mustOmitEnd()
    {
        return (elementType_?.definition()?.declaredContent()
                == ElementDefinition.DeclaredContent.empty)
               || (attributes_?.conref() ?? false);
    }

    // void setIncluded();
    public void setIncluded()
    {
        included_ = true;
    }

    // Boolean included() const;
    public Boolean included()
    {
        return included_;
    }

    // const StringC &name() const;
    public StringC name()
    {
        return elementType_!.name();
    }

    // const ElementType *elementType() const;
    public ElementType? elementType()
    {
        return elementType_;
    }

    // const Markup *markupPtr() const;
    public Markup? markupPtr()
    {
        return markup_;
    }

    // const AttributeList &attributes() const;
    public AttributeList attributes()
    {
        return attributes_!;
    }

    // void copyData();
    public override void copyData()
    {
        if (!copied_)
        {
            if (attributes_ != null)
            {
                AttributeList p = new AttributeList();
                attributes_.swap(p);
                attributes_ = p;
            }
            if (markup_ != null)
            {
                Markup p = new Markup();
                markup_.swap(p);
                markup_ = p;
            }
            copied_ = true;
        }
    }
}

public class EndElementEvent : LocatedEvent
{
    private ElementType? elementType_;
    private ConstPtr<Dtd> dtd_ = new ConstPtr<Dtd>();
    private PackedBoolean included_;
    private PackedBoolean copied_;  // has copyData() been called
    private Markup? markup_;

    // EndElementEvent(const ElementType *, const ConstPtr<Dtd> &, const Location &, Markup *);
    public EndElementEvent(ElementType? elementType,
                           ConstPtr<Dtd> dtd,
                           Location startLocation,
                           Markup? markup)
        : base(Type.endElement, startLocation)
    {
        elementType_ = elementType;
        dtd_ = dtd;
        included_ = false;
        copied_ = false;
        markup_ = markup;
    }

    // ~EndElementEvent();
    // C# handles via GC

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.endElement(this);
    }

    // void setIncluded();
    public void setIncluded()
    {
        included_ = true;
    }

    // Boolean included() const;
    public Boolean included()
    {
        return included_;
    }

    // const StringC &name() const;
    public StringC name()
    {
        return elementType_!.name();
    }

    // const ElementType *elementType() const;
    public ElementType? elementType()
    {
        return elementType_;
    }

    // const Markup *markupPtr() const;
    public Markup? markupPtr()
    {
        return markup_;
    }

    // void copyData();
    public override void copyData()
    {
        if (!copied_)
        {
            if (markup_ != null)
            {
                Markup p = new Markup();
                markup_.swap(p);
                markup_ = p;
            }
            copied_ = true;
        }
    }
}

public class DataEvent : LocatedEvent
{
    protected Char[]? p_;
    protected nuint offset_;  // C# addition: offset into p_ where data starts
    protected nuint length_;

    // DataEvent(Type, const Char *, size_t, const Location &);
    // C# note: Added offset parameter to support array-based access
    public DataEvent(Type type, Char[]? p, nuint offset, nuint length, Location location)
        : base(type, location)
    {
        p_ = p;
        offset_ = offset;
        length_ = length;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.data(this);
    }

    // const Char *data() const;
    public Char[]? data()
    {
        return p_;
    }

    // C# addition: offset into data() where content starts
    public nuint dataOffset()
    {
        return offset_;
    }

    // size_t dataLength() const;
    public nuint dataLength()
    {
        return length_;
    }

    // virtual Boolean isRe(unsigned long &serial) const;
    public virtual Boolean isRe(out ulong serial)
    {
        serial = 0;
        return false;
    }

    // virtual const Entity *entity() const;
    public virtual Entity? entity()
    {
        return null;
    }
}

public class ImmediateDataEvent : DataEvent
{
    private Char[]? alloc_;

    // ImmediateDataEvent(Type type, const Char *, size_t, const Location &, Boolean copy);
    public ImmediateDataEvent(Type type, Char[]? p, nuint length, Location location, Boolean copy)
        : base(type, p, 0, length, location)
    {
        alloc_ = null;
        if (copy)
            copyData();
    }

    // C# addition: constructor with offset
    public ImmediateDataEvent(Type type, Char[]? p, nuint offset, nuint length, Location location, Boolean copy)
        : base(type, p, offset, length, location)
    {
        alloc_ = null;
        if (copy)
            copyData();
    }

    // ~ImmediateDataEvent();
    // C# handles via GC

    // void copyData();
    public override void copyData()
    {
        if (alloc_ == null && p_ != null)
        {
            alloc_ = new Char[length_];
            Array.Copy(p_, (int)offset_, alloc_, 0, (int)length_);
            p_ = alloc_;
            offset_ = 0;  // After copy, data starts at index 0
        }
    }
}

public class DataEntityEvent : DataEvent
{
    // DataEntityEvent(Type type, const InternalEntity *, const ConstPtr<Origin> &);
    public DataEntityEvent(Type type, InternalEntity entity, ConstPtr<Origin> origin)
        : base(type, entity.@string().data(), 0, entity.@string().size(),
               new Location(origin, 0))
    {
    }

    // const Entity *entity() const;
    public override Entity? entity()
    {
        return location().origin().pointer()?.asEntityOrigin()?.entity();
    }
}

public class CdataEntityEvent : DataEntityEvent
{
    // CdataEntityEvent(const InternalEntity *, const ConstPtr<Origin> &);
    public CdataEntityEvent(InternalEntity entity, ConstPtr<Origin> origin)
        : base(Type.characterData, entity, origin)
    {
    }
}

public class SdataEntityEvent : DataEntityEvent
{
    // SdataEntityEvent(const InternalEntity *, const ConstPtr<Origin> &);
    public SdataEntityEvent(InternalEntity entity, ConstPtr<Origin> origin)
        : base(Type.sdataEntity, entity, origin)
    {
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.sdataEntity(this);
    }
}

public class PiEvent : LocatedEvent
{
    private Char[]? data_;
    private nuint dataOffset_;  // C# addition: offset into data_ where content starts
    private nuint dataLength_;

    // PiEvent(const Char *, size_t, const Location &);
    public PiEvent(Char[]? data, nuint dataLength, Location location)
        : base(Type.pi, location)
    {
        data_ = data;
        dataOffset_ = 0;
        dataLength_ = dataLength;
    }

    // C# addition: constructor with offset
    public PiEvent(Char[]? data, nuint dataOffset, nuint dataLength, Location location)
        : base(Type.pi, location)
    {
        data_ = data;
        dataOffset_ = dataOffset;
        dataLength_ = dataLength;
    }

    // const Char *data() const;
    public Char[]? data()
    {
        return data_;
    }

    // C# addition: offset into data() where content starts
    public nuint dataOffset()
    {
        return dataOffset_;
    }

    // size_t dataLength() const;
    public nuint dataLength()
    {
        return dataLength_;
    }

    // virtual const Entity *entity() const;
    public virtual Entity? entity()
    {
        return null;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.pi(this);
    }
}

public class ImmediatePiEvent : PiEvent
{
    private StringC string_ = new StringC();

    // ImmediatePiEvent(StringC &, const Location &);
    public ImmediatePiEvent(StringC str, Location loc)
        : base(str.data(), str.size(), loc)
    {
        str.swap(string_);
    }
}

public class PiEntityEvent : PiEvent
{
    // PiEntityEvent(const PiEntity *entity, const ConstPtr<Origin> &origin);
    public PiEntityEvent(PiEntity entity, ConstPtr<Origin> origin)
        : base(entity.@string().data(), entity.@string().size(),
               new Location(origin, 0))
    {
    }

    // const Entity *entity() const;
    public override Entity? entity()
    {
        return location().origin().pointer()?.asEntityOrigin()?.entity();
    }
}

public class ExternalEntityEvent : Event
{
    private ConstPtr<EntityOrigin> origin_ = new ConstPtr<EntityOrigin>();

    // ExternalEntityEvent(Type type, const ConstPtr<EntityOrigin> &);
    public ExternalEntityEvent(Type type, ConstPtr<EntityOrigin> origin)
        : base(type)
    {
        origin_ = origin;
    }

    // const ConstPtr<EntityOrigin> &entityOrigin() const;
    public ConstPtr<EntityOrigin> entityOrigin()
    {
        return origin_;
    }

    // const Location &location() const;
    public Location location()
    {
        return origin_.pointer()!.parent();
    }
}

public class ExternalDataEntityEvent : ExternalEntityEvent
{
    private ExternalDataEntity? dataEntity_;

    // ExternalDataEntityEvent(const ExternalDataEntity *, const ConstPtr<EntityOrigin> &);
    public ExternalDataEntityEvent(ExternalDataEntity? entity, ConstPtr<EntityOrigin> origin)
        : base(Type.externalDataEntity, origin)
    {
        dataEntity_ = entity;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.externalDataEntity(this);
    }

    // const ExternalDataEntity *entity() const;
    public ExternalDataEntity? entity()
    {
        return dataEntity_;
    }
}

public class SubdocEntityEvent : ExternalEntityEvent
{
    private SubdocEntity? subdocEntity_;

    // SubdocEntityEvent(const SubdocEntity *, const ConstPtr<EntityOrigin> &);
    public SubdocEntityEvent(SubdocEntity? entity, ConstPtr<EntityOrigin> origin)
        : base(Type.subdocEntity, origin)
    {
        subdocEntity_ = entity;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.subdocEntity(this);
    }

    // const SubdocEntity *entity() const;
    public SubdocEntity? entity()
    {
        return subdocEntity_;
    }
}

public class NonSgmlCharEvent : LocatedEvent
{
    private Char c_;

    // NonSgmlCharEvent(Char c, const Location &);
    public NonSgmlCharEvent(Char c, Location location)
        : base(Type.nonSgmlChar, location)
    {
        c_ = c;
    }

    // Char character() const;
    public Char character()
    {
        return c_;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.nonSgmlChar(this);
    }
}

public class AppinfoEvent : LocatedEvent
{
    private Boolean appinfoNone_;
    private Text appinfo_ = new Text();

    // AppinfoEvent(const Location &);
    public AppinfoEvent(Location location)
        : base(Type.appinfo, location)
    {
        appinfoNone_ = true;
    }

    // AppinfoEvent(const Text &, const Location &);
    public AppinfoEvent(Text text, Location location)
        : base(Type.appinfo, location)
    {
        appinfoNone_ = false;
        appinfo_ = text;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.appinfo(this);
    }

    // Boolean literal(const StringC *&) const;
    public Boolean literal(out StringC? p)
    {
        if (appinfoNone_)
        {
            p = null;
            return false;
        }
        p = appinfo_.@string();
        return true;
    }
}

public class UselinkEvent : MarkupEvent
{
    private ConstPtr<Lpd> lpd_ = new ConstPtr<Lpd>();
    private LinkSet? linkSet_;
    private Boolean restore_;

    // UselinkEvent(const ConstPtr<Lpd> &, const LinkSet *, Boolean restore, const Location &, Markup *);
    public UselinkEvent(ConstPtr<Lpd> lpd, LinkSet? linkSet, Boolean restore,
                        Location loc, Markup? markup)
        : base(Type.uselink, loc, markup)
    {
        lpd_ = lpd;
        linkSet_ = linkSet;
        restore_ = restore;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.uselink(this);
    }

    // const ConstPtr<Lpd> &lpd() const;
    public ConstPtr<Lpd> lpd()
    {
        return lpd_;
    }

    // const LinkSet *linkSet() const;
    public LinkSet? linkSet()
    {
        return linkSet_;
    }

    // Boolean restore() const;
    public Boolean restore()
    {
        return restore_;
    }
}

public class UsemapEvent : MarkupEvent
{
    private ConstPtr<Dtd> dtd_ = new ConstPtr<Dtd>();
    private Vector<ElementType?> elements_ = new Vector<ElementType?>();
    private ShortReferenceMap? map_;

    // UsemapEvent(const ShortReferenceMap *, Vector<const ElementType *> &, const ConstPtr<Dtd> &, const Location &, Markup *);
    public UsemapEvent(ShortReferenceMap? map, Vector<ElementType?> elements,
                       ConstPtr<Dtd> dtd, Location loc, Markup? markup)
        : base(Type.usemap, loc, markup)
    {
        map_ = map;
        dtd_ = dtd;
        elements.swap(elements_);
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.usemap(this);
    }

    // const ShortReferenceMap *map() const;
    public ShortReferenceMap? map()
    {
        return map_;
    }

    // const Vector<const ElementType *> &elements() const;
    public Vector<ElementType?> elements()
    {
        return elements_;
    }
}

public class StartSubsetEvent : MarkupEvent
{
    private StringC name_ = new StringC();
    private ConstPtr<Entity> entity_ = new ConstPtr<Entity>();
    private Boolean hasInternalSubset_;

    // StartSubsetEvent(Type, const StringC &, const ConstPtr<Entity> &entity, Boolean hasInternalSubset, const Location &, Markup *);
    public StartSubsetEvent(Type type, StringC name, ConstPtr<Entity> entity,
                            Boolean hasInternalSubset, Location loc, Markup? markup)
        : base(type, loc, markup)
    {
        name_ = name;
        entity_ = entity;
        hasInternalSubset_ = hasInternalSubset;
    }

    // const StringC &name() const;
    public StringC name()
    {
        return name_;
    }

    // const ConstPtr<Entity> &entity() const;
    public ConstPtr<Entity> entity()
    {
        return entity_;
    }

    // Boolean hasInternalSubset() const;
    public Boolean hasInternalSubset()
    {
        return hasInternalSubset_;
    }
}

public class StartDtdEvent : StartSubsetEvent
{
    // StartDtdEvent(const StringC &, const ConstPtr<Entity> &entity, Boolean hasInternalSubset, const Location &, Markup *);
    public StartDtdEvent(StringC name, ConstPtr<Entity> entity,
                         Boolean hasInternalSubset, Location loc, Markup? markup)
        : base(Type.startDtd, name, entity, hasInternalSubset, loc, markup)
    {
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.startDtd(this);
    }
}

public class StartLpdEvent : StartSubsetEvent
{
    private Boolean active_;

    // StartLpdEvent(Boolean active, const StringC &, const ConstPtr<Entity> &entity, Boolean hasInternalSubset, const Location &, Markup *);
    public StartLpdEvent(Boolean active, StringC name, ConstPtr<Entity> entity,
                         Boolean hasInternalSubset, Location loc, Markup? markup)
        : base(Type.startLpd, name, entity, hasInternalSubset, loc, markup)
    {
        active_ = active;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.startLpd(this);
    }

    // Boolean active() const;
    public Boolean active()
    {
        return active_;
    }
}

public class EndDtdEvent : MarkupEvent
{
    private ConstPtr<Dtd> dtd_ = new ConstPtr<Dtd>();

    // EndDtdEvent(const ConstPtr<Dtd> &, const Location &, Markup *);
    public EndDtdEvent(ConstPtr<Dtd> dtd, Location loc, Markup? markup)
        : base(Type.endDtd, loc, markup)
    {
        dtd_ = dtd;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.endDtd(this);
    }

    // const Dtd &dtd() const;
    public Dtd dtd()
    {
        return dtd_.pointer()!;
    }

    // const ConstPtr<Dtd> &dtdPointer() const;
    public ConstPtr<Dtd> dtdPointer()
    {
        return dtd_;
    }
}

public class EndLpdEvent : MarkupEvent
{
    private ConstPtr<Lpd> lpd_ = new ConstPtr<Lpd>();

    // EndLpdEvent(const ConstPtr<Lpd> &, const Location &, Markup *);
    public EndLpdEvent(ConstPtr<Lpd> lpd, Location loc, Markup? markup)
        : base(Type.endLpd, loc, markup)
    {
        lpd_ = lpd;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.endLpd(this);
    }

    // const Lpd &lpd() const;
    public Lpd lpd()
    {
        return lpd_.pointer()!;
    }

    // const ConstPtr<Lpd> &lpdPointer() const;
    public ConstPtr<Lpd> lpdPointer()
    {
        return lpd_;
    }
}

public class EndPrologEvent : LocatedEvent
{
    private ConstPtr<Dtd> dtd_ = new ConstPtr<Dtd>();
    private ConstPtr<ComplexLpd> lpd_ = new ConstPtr<ComplexLpd>();
    private Vector<StringC> simpleLinkNames_ = new Vector<StringC>();
    private Vector<AttributeList> simpleLinkAttributes_ = new Vector<AttributeList>();

    // EndPrologEvent(const ConstPtr<Dtd> &dtd, const ConstPtr<ComplexLpd> &lpd,
    //                Vector<StringC> &simpleLinkNames, Vector<AttributeList> &simpleLinkAttributes, const Location &);
    public EndPrologEvent(ConstPtr<Dtd> dtd, ConstPtr<ComplexLpd> lpd,
                          Vector<StringC> simpleLinkNames,
                          Vector<AttributeList> simpleLinkAttributes,
                          Location location)
        : base(Type.endProlog, location)
    {
        dtd_ = dtd;
        lpd_ = lpd;
        simpleLinkAttributes.swap(simpleLinkAttributes_);
        simpleLinkNames.swap(simpleLinkNames_);
    }

    // EndPrologEvent(const ConstPtr<Dtd> &dtd, const Location &);
    public EndPrologEvent(ConstPtr<Dtd> dtd, Location location)
        : base(Type.endProlog, location)
    {
        dtd_ = dtd;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.endProlog(this);
    }

    // const Dtd &dtd() const;
    public Dtd dtd()
    {
        return dtd_.pointer()!;
    }

    // const ConstPtr<Dtd> &dtdPointer() const;
    public ConstPtr<Dtd> dtdPointer()
    {
        return dtd_;
    }

    // const ConstPtr<ComplexLpd> &lpdPointer() const;
    public ConstPtr<ComplexLpd> lpdPointer()
    {
        return lpd_;
    }

    // const Vector<StringC> &simpleLinkNames() const;
    public Vector<StringC> simpleLinkNames()
    {
        return simpleLinkNames_;
    }

    // const Vector<AttributeList> &simpleLinkAttributes() const;
    public Vector<AttributeList> simpleLinkAttributes()
    {
        return simpleLinkAttributes_;
    }
}

public class SgmlDeclEvent : MarkupEvent
{
    private ConstPtr<Sd> sd_ = new ConstPtr<Sd>();
    private ConstPtr<Syntax> prologSyntax_ = new ConstPtr<Syntax>();
    private ConstPtr<Syntax> instanceSyntax_ = new ConstPtr<Syntax>();
    private ConstPtr<Sd> refSd_ = new ConstPtr<Sd>();
    private ConstPtr<Syntax> refSyntax_ = new ConstPtr<Syntax>();
    private Index nextIndex_;
    private StringC implySystemId_ = new StringC();

    // SgmlDeclEvent(const ConstPtr<Sd> &, const ConstPtr<Syntax> &syntax);
    // for an implied SGML declaration
    public SgmlDeclEvent(ConstPtr<Sd> sd, ConstPtr<Syntax> syntax)
        : base(Type.sgmlDecl)
    {
        sd_ = sd;
        prologSyntax_ = syntax;
        instanceSyntax_ = syntax;
        nextIndex_ = 0;
    }

    // SgmlDeclEvent(const ConstPtr<Sd> &, const ConstPtr<Syntax> &syntax,
    //               const ConstPtr<Syntax> &instanceSyntax, const ConstPtr<Sd> &refSd,
    //               const ConstPtr<Syntax> &refSyntax, Index nextIndex,
    //               const StringC &implySystemId, const Location &, Markup *);
    // for an explicit SGML declaration
    public SgmlDeclEvent(ConstPtr<Sd> sd, ConstPtr<Syntax> prologSyntax,
                         ConstPtr<Syntax> instanceSyntax, ConstPtr<Sd> refSd,
                         ConstPtr<Syntax> refSyntax, Index nextIndex,
                         StringC implySystemId, Location loc, Markup? markup)
        : base(Type.sgmlDecl, loc, markup)
    {
        sd_ = sd;
        prologSyntax_ = prologSyntax;
        instanceSyntax_ = instanceSyntax;
        refSd_ = refSd;
        refSyntax_ = refSyntax;
        nextIndex_ = nextIndex;
        implySystemId_ = implySystemId;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.sgmlDecl(this);
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

    // const Syntax &prologSyntax() const;
    public Syntax prologSyntax()
    {
        return prologSyntax_.pointer()!;
    }

    // const ConstPtr<Syntax> &prologSyntaxPointer() const;
    public ConstPtr<Syntax> prologSyntaxPointer()
    {
        return prologSyntax_;
    }

    // const Syntax &instanceSyntax() const;
    public Syntax instanceSyntax()
    {
        return instanceSyntax_.pointer()!;
    }

    // const ConstPtr<Syntax> &instanceSyntaxPointer() const;
    public ConstPtr<Syntax> instanceSyntaxPointer()
    {
        return instanceSyntax_;
    }

    // const ConstPtr<Sd> &refSdPointer() const;
    public ConstPtr<Sd> refSdPointer()
    {
        return refSd_;
    }

    // const ConstPtr<Syntax> &refSyntaxPointer() const;
    public ConstPtr<Syntax> refSyntaxPointer()
    {
        return refSyntax_;
    }

    // const StringC &implySystemId() const;
    public StringC implySystemId()
    {
        return implySystemId_;
    }
}

public class CommentDeclEvent : MarkupEvent
{
    // CommentDeclEvent(const Location &, Markup *);
    public CommentDeclEvent(Location loc, Markup? markup)
        : base(Type.commentDecl, loc, markup)
    {
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.commentDecl(this);
    }
}

public class SSepEvent : ImmediateDataEvent
{
    // SSepEvent(const Char *, size_t, const Location &, Boolean copy);
    public SSepEvent(Char[]? p, nuint length, Location location, Boolean copy)
        : base(Type.sSep, p, length, location, copy)
    {
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.sSep(this);
    }
}

public class IgnoredRsEvent : LocatedEvent
{
    private Char c_;

    // IgnoredRsEvent(Char c, const Location &);
    public IgnoredRsEvent(Char c, Location location)
        : base(Type.ignoredRs, location)
    {
        c_ = c;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.ignoredRs(this);
    }

    // Char rs() const;
    public Char rs()
    {
        return c_;
    }
}

public class IgnoredReEvent : LocatedEvent
{
    private ulong serial_;
    private Char c_;

    // IgnoredReEvent(Char c, const Location &, unsigned long serial);
    public IgnoredReEvent(Char c, Location location, ulong serial)
        : base(Type.ignoredRe, location)
    {
        c_ = c;
        serial_ = serial;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.ignoredRe(this);
    }

    // Char re() const;
    public Char re()
    {
        return c_;
    }

    // unsigned long serial() const;
    public ulong serial()
    {
        return serial_;
    }
}

public class ReEvent : ImmediateDataEvent
{
    private ulong serial_;

    // ReEvent(const Char *, const Location &, unsigned long serial);
    public ReEvent(Char[]? p, Location location, ulong serial)
        : base(Type.characterData, p, 1, location, false)
    {
        serial_ = serial;
    }

    // Boolean isRe(unsigned long &serial) const;
    public override Boolean isRe(out ulong serial)
    {
        serial = serial_;
        return true;
    }
}

public class ReOriginEvent : LocatedEvent
{
    private ulong serial_;
    private Char c_;

    // ReOriginEvent(Char c, const Location &, unsigned long serial);
    public ReOriginEvent(Char c, Location location, ulong serial)
        : base(Type.reOrigin, location)
    {
        c_ = c;
        serial_ = serial;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.reOrigin(this);
    }

    // Char re() const;
    public Char re()
    {
        return c_;
    }

    // unsigned long serial() const;
    public ulong serial()
    {
        return serial_;
    }
}

public class IgnoredCharsEvent : ImmediateDataEvent
{
    // IgnoredCharsEvent(const Char *, size_t, const Location &, Boolean copy);
    public IgnoredCharsEvent(Char[]? p, nuint length, Location location, Boolean copy)
        : base(Type.ignoredChars, p, length, location, copy)
    {
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.ignoredChars(this);
    }
}

public class MarkedSectionEvent : MarkupEvent
{
    public enum Status { include, rcdata, cdata, ignore } // in priority order

    private Status status_;

    // MarkedSectionEvent(Type, Status, const Location &, Markup *);
    public MarkedSectionEvent(Type type, Status status, Location loc, Markup? markup)
        : base(type, loc, markup)
    {
        status_ = status;
    }

    // Status status() const;
    public Status status()
    {
        return status_;
    }
}

public class MarkedSectionStartEvent : MarkedSectionEvent
{
    // MarkedSectionStartEvent(Status, const Location &, Markup *);
    public MarkedSectionStartEvent(Status status, Location loc, Markup? markup)
        : base(Type.markedSectionStart, status, loc, markup)
    {
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.markedSectionStart(this);
    }
}

public class MarkedSectionEndEvent : MarkedSectionEvent
{
    // MarkedSectionEndEvent(Status, const Location &, Markup *);
    public MarkedSectionEndEvent(Status status, Location loc, Markup? markup)
        : base(Type.markedSectionEnd, status, loc, markup)
    {
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.markedSectionEnd(this);
    }
}

public class EntityStartEvent : Event
{
    private ConstPtr<EntityOrigin> origin_ = new ConstPtr<EntityOrigin>();

    // EntityStartEvent(const ConstPtr<EntityOrigin> &origin);
    public EntityStartEvent(ConstPtr<EntityOrigin> origin)
        : base(Type.entityStart)
    {
        origin_ = origin;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.entityStart(this);
    }

    // const Entity *entity() const;
    public Entity? entity()
    {
        return origin_.pointer()?.entity();
    }

    // const ConstPtr<EntityOrigin> &entityOrigin() const;
    public ConstPtr<EntityOrigin> entityOrigin()
    {
        return origin_;
    }
}

public class EntityEndEvent : LocatedEvent
{
    // EntityEndEvent(const Location &);
    public EntityEndEvent(Location location)
        : base(Type.entityEnd, location)
    {
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.entityEnd(this);
    }
}

public class EntityDeclEvent : MarkupEvent
{
    private Boolean ignored_;
    private ConstPtr<Entity> entity_ = new ConstPtr<Entity>();

    // EntityDeclEvent(const ConstPtr<Entity> &, Boolean ignored, const Location &, Markup *);
    public EntityDeclEvent(ConstPtr<Entity> entity, Boolean ignored,
                           Location loc, Markup? markup)
        : base(Type.entityDecl, loc, markup)
    {
        entity_ = entity;
        ignored_ = ignored;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.entityDecl(this);
    }

    // const Entity &entity() const;
    public Entity entity()
    {
        return entity_.pointer()!;
    }

    // const ConstPtr<Entity> &entityPointer() const;
    public ConstPtr<Entity> entityPointer()
    {
        return entity_;
    }

    // Boolean ignored() const;
    public Boolean ignored()
    {
        return ignored_;
    }
}

public class NotationDeclEvent : MarkupEvent
{
    private ConstPtr<Notation> notation_ = new ConstPtr<Notation>();

    // NotationDeclEvent(const ConstPtr<Notation> &, const Location &, Markup *);
    public NotationDeclEvent(ConstPtr<Notation> notation, Location loc, Markup? markup)
        : base(Type.notationDecl, loc, markup)
    {
        notation_ = notation;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.notationDecl(this);
    }

    // const Notation &notation() const;
    public Notation notation()
    {
        return notation_.pointer()!;
    }

    // const ConstPtr<Notation> &notationPointer() const;
    public ConstPtr<Notation> notationPointer()
    {
        return notation_;
    }
}

public class ElementDeclEvent : MarkupEvent
{
    private Vector<ElementType?> elements_ = new Vector<ElementType?>();
    private ConstPtr<Dtd> dtd_ = new ConstPtr<Dtd>();

    // ElementDeclEvent(Vector<const ElementType *> &elements, const ConstPtr<Dtd> &, const Location &, Markup *);
    public ElementDeclEvent(Vector<ElementType?> elements, ConstPtr<Dtd> dtd,
                            Location loc, Markup? markup)
        : base(Type.elementDecl, loc, markup)
    {
        dtd_ = dtd;
        elements.swap(elements_);
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.elementDecl(this);
    }

    // const Vector<const ElementType *> &elements() const;
    public Vector<ElementType?> elements()
    {
        return elements_;
    }
}

public class AttlistDeclEvent : MarkupEvent
{
    private Vector<ElementType?> elements_ = new Vector<ElementType?>();
    private ConstPtr<Dtd> dtd_ = new ConstPtr<Dtd>();

    // AttlistDeclEvent(Vector<const ElementType *> &elements, const ConstPtr<Dtd> &, const Location &, Markup *);
    public AttlistDeclEvent(Vector<ElementType?> elements, ConstPtr<Dtd> dtd,
                            Location loc, Markup? markup)
        : base(Type.attlistDecl, loc, markup)
    {
        dtd_ = dtd;
        elements.swap(elements_);
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.attlistDecl(this);
    }

    // const Vector<const ElementType *> &elements() const;
    public Vector<ElementType?> elements()
    {
        return elements_;
    }
}

public class AttlistNotationDeclEvent : MarkupEvent
{
    private Vector<ConstPtr<Notation>> notations_ = new Vector<ConstPtr<Notation>>();

    // AttlistNotationDeclEvent(Vector<ConstPtr<Notation> > &notations, const Location &, Markup *);
    public AttlistNotationDeclEvent(Vector<ConstPtr<Notation>> notations,
                                     Location loc, Markup? markup)
        : base(Type.attlistNotationDecl, loc, markup)
    {
        notations.swap(notations_);
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.attlistNotationDecl(this);
    }

    // const Vector<ConstPtr<Notation> > &notations() const;
    public Vector<ConstPtr<Notation>> notations()
    {
        return notations_;
    }
}

public class LinkAttlistDeclEvent : MarkupEvent
{
    private Vector<ElementType?> elements_ = new Vector<ElementType?>();
    private ConstPtr<Lpd> lpd_ = new ConstPtr<Lpd>();

    // LinkAttlistDeclEvent(Vector<const ElementType *> &elements, const ConstPtr<Lpd> &, const Location &, Markup *);
    public LinkAttlistDeclEvent(Vector<ElementType?> elements, ConstPtr<Lpd> lpd,
                                 Location loc, Markup? markup)
        : base(Type.linkAttlistDecl, loc, markup)
    {
        lpd_ = lpd;
        elements.swap(elements_);
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.linkAttlistDecl(this);
    }

    // const Vector<const ElementType *> &elements() const;
    public Vector<ElementType?> elements()
    {
        return elements_;
    }

    // const Lpd &lpd() const;
    public Lpd lpd()
    {
        return lpd_.pointer()!;
    }
}

public class LinkDeclEvent : MarkupEvent
{
    private LinkSet? linkSet_;
    private ConstPtr<ComplexLpd> lpd_ = new ConstPtr<ComplexLpd>();

    // LinkDeclEvent(const LinkSet *linkSet, const ConstPtr<ComplexLpd> &, const Location &, Markup *);
    public LinkDeclEvent(LinkSet? linkSet, ConstPtr<ComplexLpd> lpd,
                         Location loc, Markup? markup)
        : base(Type.linkDecl, loc, markup)
    {
        lpd_ = lpd;
        linkSet_ = linkSet;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.linkDecl(this);
    }

    // const LinkSet *linkSet() const;
    public LinkSet? linkSet()
    {
        return linkSet_;
    }

    // const ComplexLpd &lpd() const;
    public ComplexLpd lpd()
    {
        return lpd_.pointer()!;
    }
}

public class IdLinkDeclEvent : MarkupEvent
{
    private ConstPtr<ComplexLpd> lpd_ = new ConstPtr<ComplexLpd>();

    // IdLinkDeclEvent(const ConstPtr<ComplexLpd> &, const Location &, Markup *);
    public IdLinkDeclEvent(ConstPtr<ComplexLpd> lpd, Location loc, Markup? markup)
        : base(Type.linkDecl, loc, markup)
    {
        lpd_ = lpd;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.idLinkDecl(this);
    }

    // const ComplexLpd &lpd() const;
    public ComplexLpd lpd()
    {
        return lpd_.pointer()!;
    }
}

public class ShortrefDeclEvent : MarkupEvent
{
    private ShortReferenceMap? map_;
    private ConstPtr<Dtd> dtd_ = new ConstPtr<Dtd>();

    // ShortrefDeclEvent(const ShortReferenceMap *, const ConstPtr<Dtd> &, const Location &, Markup *);
    public ShortrefDeclEvent(ShortReferenceMap? map, ConstPtr<Dtd> dtd,
                             Location loc, Markup? markup)
        : base(Type.shortrefDecl, loc, markup)
    {
        map_ = map;
        dtd_ = dtd;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.shortrefDecl(this);
    }

    // const ShortReferenceMap *map() const;
    public ShortReferenceMap? map()
    {
        return map_;
    }
}

public class IgnoredMarkupEvent : MarkupEvent
{
    // IgnoredMarkupEvent(const Location &, Markup *);
    public IgnoredMarkupEvent(Location loc, Markup? markup)
        : base(Type.ignoredMarkup, loc, markup)
    {
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.ignoredMarkup(this);
    }
}

public class EntityDefaultedEvent : LocatedEvent
{
    private ConstPtr<Entity> entity_ = new ConstPtr<Entity>();

    // EntityDefaultedEvent(const ConstPtr<Entity> &, const Location &);
    public EntityDefaultedEvent(ConstPtr<Entity> entity, Location loc)
        : base(Type.entityDefaulted, loc)
    {
        entity_ = entity;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.entityDefaulted(this);
    }

    // const Entity &entity() const;
    public Entity entity()
    {
        return entity_.pointer()!;
    }

    // const ConstPtr<Entity> &entityPointer() const;
    public ConstPtr<Entity> entityPointer()
    {
        return entity_;
    }
}

public class SgmlDeclEntityEvent : LocatedEvent
{
    private PublicId publicId_ = new PublicId();
    private PublicId.TextClass entityType_;
    private StringC effectiveSystemId_ = new StringC();

    // SgmlDeclEntityEvent(const PublicId &publicId, PublicId::TextClass entityType,
    //                     const StringC &effectiveSystemId, const Location &);
    public SgmlDeclEntityEvent(PublicId publicId, PublicId.TextClass entityType,
                               StringC effectiveSystemId, Location loc)
        : base(Type.sgmlDeclEntity, loc)
    {
        publicId_ = publicId;
        entityType_ = entityType;
        effectiveSystemId_ = effectiveSystemId;
    }

    // void handle(EventHandler &);
    public override void handle(EventHandler handler)
    {
        handler.sgmlDeclEntity(this);
    }

    // const PublicId &publicId() const;
    public PublicId publicId()
    {
        return publicId_;
    }

    // PublicId::TextClass entityType() const;
    public PublicId.TextClass entityType()
    {
        return entityType_;
    }

    // const StringC &effectiveSystemId() const;
    public StringC effectiveSystemId()
    {
        return effectiveSystemId_;
    }
}

// EventHandler abstract base class
public abstract class EventHandler
{
    // virtual ~EventHandler();
    // C# handles via GC

    // virtual void message(MessageEvent *) = 0;
    public abstract void message(MessageEvent? ev);

    // Default implementations - delete the event
    public virtual void data(DataEvent? ev) { }
    public virtual void startElement(StartElementEvent? ev) { }
    public virtual void endElement(EndElementEvent? ev) { }
    public virtual void pi(PiEvent? ev) { }
    public virtual void sdataEntity(SdataEntityEvent? ev) { }
    public virtual void externalDataEntity(ExternalDataEntityEvent? ev) { }
    public virtual void subdocEntity(SubdocEntityEvent? ev) { }
    public virtual void nonSgmlChar(NonSgmlCharEvent? ev) { }
    public virtual void appinfo(AppinfoEvent? ev) { }
    public virtual void uselink(UselinkEvent? ev) { }
    public virtual void usemap(UsemapEvent? ev) { }
    public virtual void startDtd(StartDtdEvent? ev) { }
    public virtual void endDtd(EndDtdEvent? ev) { }
    public virtual void startLpd(StartLpdEvent? ev) { }
    public virtual void endLpd(EndLpdEvent? ev) { }
    public virtual void endProlog(EndPrologEvent? ev) { }
    public virtual void sgmlDecl(SgmlDeclEvent? ev) { }
    public virtual void commentDecl(CommentDeclEvent? ev) { }
    public virtual void sSep(SSepEvent? ev) { }
    public virtual void ignoredRs(IgnoredRsEvent? ev) { }
    public virtual void ignoredRe(IgnoredReEvent? ev) { }
    public virtual void reOrigin(ReOriginEvent? ev) { }
    public virtual void ignoredChars(IgnoredCharsEvent? ev) { }
    public virtual void markedSectionStart(MarkedSectionStartEvent? ev) { }
    public virtual void markedSectionEnd(MarkedSectionEndEvent? ev) { }
    public virtual void entityStart(EntityStartEvent? ev) { }
    public virtual void entityEnd(EntityEndEvent? ev) { }
    public virtual void notationDecl(NotationDeclEvent? ev) { }
    public virtual void entityDecl(EntityDeclEvent? ev) { }
    public virtual void elementDecl(ElementDeclEvent? ev) { }
    public virtual void attlistDecl(AttlistDeclEvent? ev) { }
    public virtual void linkAttlistDecl(LinkAttlistDeclEvent? ev) { }
    public virtual void attlistNotationDecl(AttlistNotationDeclEvent? ev) { }
    public virtual void linkDecl(LinkDeclEvent? ev) { }
    public virtual void idLinkDecl(IdLinkDeclEvent? ev) { }
    public virtual void shortrefDecl(ShortrefDeclEvent? ev) { }
    public virtual void ignoredMarkup(IgnoredMarkupEvent? ev) { }
    public virtual void entityDefaulted(EntityDefaultedEvent? ev) { }
    public virtual void inputClosed(InputSource? inSource) { }
    public virtual void inputOpened(InputSource? inSource) { }
    public virtual void sgmlDeclEntity(SgmlDeclEntityEvent? ev) { }
}

// EventQueue - queues events for later processing
public class EventQueue : EventHandler
{
    private IQueue<Event> queue_ = new IQueue<Event>();

    public EventQueue()
    {
    }

    public override void message(MessageEvent? ev) { if (ev != null) append(ev); }
    public override void data(DataEvent? ev) { if (ev != null) append(ev); }
    public override void startElement(StartElementEvent? ev) { if (ev != null) append(ev); }
    public override void endElement(EndElementEvent? ev) { if (ev != null) append(ev); }
    public override void pi(PiEvent? ev) { if (ev != null) append(ev); }
    public override void sdataEntity(SdataEntityEvent? ev) { if (ev != null) append(ev); }
    public override void externalDataEntity(ExternalDataEntityEvent? ev) { if (ev != null) append(ev); }
    public override void subdocEntity(SubdocEntityEvent? ev) { if (ev != null) append(ev); }
    public override void nonSgmlChar(NonSgmlCharEvent? ev) { if (ev != null) append(ev); }
    public override void appinfo(AppinfoEvent? ev) { if (ev != null) append(ev); }
    public override void uselink(UselinkEvent? ev) { if (ev != null) append(ev); }
    public override void usemap(UsemapEvent? ev) { if (ev != null) append(ev); }
    public override void startDtd(StartDtdEvent? ev) { if (ev != null) append(ev); }
    public override void endDtd(EndDtdEvent? ev) { if (ev != null) append(ev); }
    public override void startLpd(StartLpdEvent? ev) { if (ev != null) append(ev); }
    public override void endLpd(EndLpdEvent? ev) { if (ev != null) append(ev); }
    public override void endProlog(EndPrologEvent? ev) { if (ev != null) append(ev); }
    public override void sgmlDecl(SgmlDeclEvent? ev) { if (ev != null) append(ev); }
    public override void commentDecl(CommentDeclEvent? ev) { if (ev != null) append(ev); }
    public override void sSep(SSepEvent? ev) { if (ev != null) append(ev); }
    public override void ignoredRs(IgnoredRsEvent? ev) { if (ev != null) append(ev); }
    public override void ignoredRe(IgnoredReEvent? ev) { if (ev != null) append(ev); }
    public override void reOrigin(ReOriginEvent? ev) { if (ev != null) append(ev); }
    public override void ignoredChars(IgnoredCharsEvent? ev) { if (ev != null) append(ev); }
    public override void markedSectionStart(MarkedSectionStartEvent? ev) { if (ev != null) append(ev); }
    public override void markedSectionEnd(MarkedSectionEndEvent? ev) { if (ev != null) append(ev); }
    public override void entityStart(EntityStartEvent? ev) { if (ev != null) append(ev); }
    public override void entityEnd(EntityEndEvent? ev) { if (ev != null) append(ev); }
    public override void notationDecl(NotationDeclEvent? ev) { if (ev != null) append(ev); }
    public override void entityDecl(EntityDeclEvent? ev) { if (ev != null) append(ev); }
    public override void elementDecl(ElementDeclEvent? ev) { if (ev != null) append(ev); }
    public override void attlistDecl(AttlistDeclEvent? ev) { if (ev != null) append(ev); }
    public override void linkAttlistDecl(LinkAttlistDeclEvent? ev) { if (ev != null) append(ev); }
    public override void attlistNotationDecl(AttlistNotationDeclEvent? ev) { if (ev != null) append(ev); }
    public override void linkDecl(LinkDeclEvent? ev) { if (ev != null) append(ev); }
    public override void idLinkDecl(IdLinkDeclEvent? ev) { if (ev != null) append(ev); }
    public override void shortrefDecl(ShortrefDeclEvent? ev) { if (ev != null) append(ev); }
    public override void ignoredMarkup(IgnoredMarkupEvent? ev) { if (ev != null) append(ev); }
    public override void entityDefaulted(EntityDefaultedEvent? ev) { if (ev != null) append(ev); }
    public override void sgmlDeclEntity(SgmlDeclEntityEvent? ev) { if (ev != null) append(ev); }

    // IQueue implementation
    public void append(Event ev)
    {
        queue_.append(ev);
    }

    public Event? get()
    {
        return queue_.get();
    }

    public Boolean empty()
    {
        return queue_.empty();
    }

    public void clear()
    {
        queue_.clear();
    }
}

// Pass1EventHandler - for two-pass parsing
public class Pass1EventHandler : EventHandler
{
    private Boolean hadError_;
    private EventHandler? origHandler_;
    private IQueue<Event> queue_ = new IQueue<Event>();

    public Pass1EventHandler()
    {
        hadError_ = false;
        origHandler_ = null;
    }

    public void init(EventHandler origHandler)
    {
        hadError_ = false;
        origHandler_ = origHandler;
    }

    public override void message(MessageEvent? ev)
    {
        if (ev != null && ev.message().isError())
        {
            hadError_ = true;
            origHandler_?.message(ev);
        }
        else if (ev != null)
        {
            queue_.append(ev);
        }
    }

    public Boolean hadError()
    {
        return hadError_;
    }

    // IQueue implementation
    public void append(Event ev)
    {
        queue_.append(ev);
    }

    public Event? get()
    {
        return queue_.get();
    }

    public Boolean empty()
    {
        return queue_.empty();
    }

    public void clear()
    {
        queue_.clear();
    }

    public EventHandler? origHandler()
    {
        return origHandler_;
    }
}
