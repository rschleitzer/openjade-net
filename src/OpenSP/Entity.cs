// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public abstract class Entity : EntityDecl
{
    private PackedBoolean used_;
    private PackedBoolean defaulted_;

    // Entity(const StringC &name, DeclType declType, DataType dataType, const Location &defLocation);
    public Entity(StringC name, DeclType declType, DataType dataType, Location defLocation)
        : base(name, declType, dataType, defLocation)
    {
        used_ = false;
        defaulted_ = false;
    }

    // virtual Boolean isDataOrSubdoc() const;
    public virtual Boolean isDataOrSubdoc()
    {
        return false;
    }

    // virtual Boolean isCharacterData() const;
    public virtual Boolean isCharacterData()
    {
        return false;
    }

    // virtual const ExternalDataEntity *asExternalDataEntity() const;
    public virtual ExternalDataEntity? asExternalDataEntity()
    {
        return null;
    }

    // virtual const SubdocEntity *asSubdocEntity() const;
    public virtual SubdocEntity? asSubdocEntity()
    {
        return null;
    }

    // virtual const InternalEntity *asInternalEntity() const;
    public virtual InternalEntity? asInternalEntity()
    {
        return null;
    }

    // virtual const ExternalEntity *asExternalEntity() const;
    public virtual ExternalEntity? asExternalEntity()
    {
        return null;
    }

    // virtual Entity *copy() const = 0;
    public abstract Entity copy();

    // Boolean used() const;
    public Boolean used()
    {
        return used_;
    }

    // void setUsed();
    public void setUsed()
    {
        used_ = true;
    }

    // Boolean defaulted() const;
    public Boolean defaulted()
    {
        return defaulted_;
    }

    // void setDefaulted();
    public void setDefaulted()
    {
        defaulted_ = true;
    }

    // virtual void generateSystemId(ParserState &);
    public virtual void generateSystemId(ParserState parser)
    {
        // Default implementation - subclasses may override
    }

    // virtual void dsReference(ParserState &, const Ptr<EntityOrigin> &) const;
    public virtual void dsReference(ParserState parser, Ptr<EntityOrigin> origin)
    {
        normalReference(parser, origin, true);
    }

    // virtual void contentReference(ParserState &, const Ptr<EntityOrigin> &) const;
    public virtual void contentReference(ParserState parser, Ptr<EntityOrigin> origin)
    {
        normalReference(parser, origin, true);
    }

    // virtual void litReference(Text &, ParserState &, const Ptr<EntityOrigin> &, Boolean) const;
    public virtual void litReference(Text text, ParserState parser, Ptr<EntityOrigin> origin, Boolean squeezeSpaces)
    {
        normalReference(parser, origin, false);
    }

    // virtual void declReference(ParserState &, const Ptr<EntityOrigin> &) const;
    public virtual void declReference(ParserState parser, Ptr<EntityOrigin> origin)
    {
        normalReference(parser, origin, false);
        if (parser.currentMarkup() != null)
            parser.currentMarkup()!.addEntityStart(origin);
    }

    // virtual void rcdataReference(ParserState &, const Ptr<EntityOrigin> &) const;
    public virtual void rcdataReference(ParserState parser, Ptr<EntityOrigin> origin)
    {
        normalReference(parser, origin, true);
    }

    // virtual void normalReference(ParserState &, const Ptr<EntityOrigin> &, Boolean) const;
    protected virtual void normalReference(ParserState parser, Ptr<EntityOrigin> origin, Boolean generateEvent)
    {
        // Default implementation - subclasses override
    }

    // void checkRef(ParserState &) const;
    protected virtual void checkRef(ParserState parser)
    {
        // Default implementation - subclasses may override
    }

    // static void checkEntlvl(ParserState &);
    protected static void checkEntlvl(ParserState parser)
    {
        if (parser.inputLevel() >= parser.syntax().quantity(Syntax.Quantity.qENTLVL))
            parser.message(ParserMessages.entlvl, new NumberMessageArg(parser.syntax().quantity(Syntax.Quantity.qENTLVL)));
    }

    // Boolean checkNotOpen(ParserState &) const;
    protected Boolean checkNotOpen(ParserState parser)
    {
        if (parser.entityIsOpen(this))
        {
            parser.message(ParserMessages.recursiveEntityReference, new StringMessageArg(name()));
            return false;
        }
        return true;
    }
}

public abstract class InternalEntity : Entity
{
    protected Text text_ = new Text();

    // InternalEntity(const StringC &, DeclType declType, DataType dataType, const Location &, Text &);
    public InternalEntity(StringC name, DeclType declType, DataType dataType, Location defLocation, Text text)
        : base(name, declType, dataType, defLocation)
    {
        text_.swap(text);
    }

    // const StringC &string() const;
    public StringC @string()
    {
        return text_.@string();
    }

    // const Text &text() const;
    public Text text()
    {
        return text_;
    }

    // const InternalEntity *asInternalEntity() const;
    public override InternalEntity? asInternalEntity()
    {
        return this;
    }
}

// Forward declarations for entity subclasses
public abstract class ExternalEntity : Entity
{
    private ExternalId externalId_ = new ExternalId();

    public ExternalEntity(StringC name, DeclType declType, DataType dataType, Location defLocation, ExternalId externalId)
        : base(name, declType, dataType, defLocation)
    {
        externalId_ = externalId;
    }

    public ExternalId externalId()
    {
        return externalId_;
    }

    public override ExternalEntity? asExternalEntity()
    {
        return this;
    }

    public override StringC? systemIdPointer()
    {
        return externalId_.systemIdString();
    }

    public override StringC? publicIdPointer()
    {
        return externalId_.publicIdString();
    }

    // StringC *effectiveSystemIdPointer();
    public override StringC? effectiveSystemIdPointer()
    {
        return externalId_.effectiveSystemId();
    }

    // void generateSystemId(ParserState &);
    public override void generateSystemId(ParserState parser)
    {
        StringC str = new StringC();
        if (parser.entityCatalog().lookup(this,
                                          new EntityCatalog.SyntaxAdapter(parser.syntax()),
                                          parser.sd().docCharset(),
                                          parser.messenger(),
                                          str))
        {
            externalId_.setEffectiveSystem(str);
        }
        // Don't generate warning when declType == sgml.
        else if (externalId_.publicIdString() != null && externalId_.publicIdString()!.size() > 0)
        {
            if (declType() != DeclType.sgml)
                parser.message(ParserMessages.cannotGenerateSystemIdPublic,
                               new StringMessageArg(externalId_.publicIdString()!));
        }
        else
        {
            switch (declType())
            {
                case DeclType.generalEntity:
                    parser.message(ParserMessages.cannotGenerateSystemIdGeneral,
                                   new StringMessageArg(name()));
                    break;
                case DeclType.parameterEntity:
                    parser.message(ParserMessages.cannotGenerateSystemIdParameter,
                                   new StringMessageArg(name()));
                    break;
                case DeclType.doctype:
                    parser.message(ParserMessages.cannotGenerateSystemIdDoctype,
                                   new StringMessageArg(name()));
                    break;
                case DeclType.linktype:
                    parser.message(ParserMessages.cannotGenerateSystemIdLinktype,
                                   new StringMessageArg(name()));
                    break;
                default:
                    break;
            }
        }
    }
}

public class ExternalDataEntity : ExternalNonTextEntity
{
    private ConstPtr<Notation> notation_ = new ConstPtr<Notation>();
    private AttributeList attributes_ = new AttributeList();

    public ExternalDataEntity(StringC name, DataType dataType, Location defLocation, ExternalId externalId,
                              ConstPtr<Notation> notation, AttributeList attributes,
                              DeclType declType = DeclType.generalEntity)
        : base(name, declType, dataType, defLocation, externalId)
    {
        notation_ = notation;
        attributes_ = attributes;
    }

    public AttributeList attributes()
    {
        return attributes_;
    }

    public Notation? notation()
    {
        return notation_.pointer();
    }

    public override ExternalDataEntity? asExternalDataEntity()
    {
        return this;
    }

    public override Entity copy()
    {
        AttributeList attrCopy = new AttributeList(attributes_);
        return new ExternalDataEntity(name(), dataType(), defLocation(), externalId(), notation_, attrCopy, declType());
    }

    public void setNotation(ConstPtr<Notation> notation, AttributeList attributes)
    {
        notation_ = notation;
        attributes_ = attributes;
    }
}

public class SubdocEntity : ExternalNonTextEntity
{
    public SubdocEntity(StringC name, Location defLocation, ExternalId externalId)
        : base(name, DeclType.generalEntity, DataType.subdoc, defLocation, externalId)
    {
    }

    public override SubdocEntity? asSubdocEntity()
    {
        return this;
    }

    public override Entity copy()
    {
        return new SubdocEntity(name(), defLocation(), externalId());
    }
}

// Processing Instruction Entity
public class PiEntity : InternalEntity
{
    // PiEntity(const StringC &, DeclType, const Location &, Text &);
    public PiEntity(StringC name, DeclType declType, Location defLocation, Text text)
        : base(name, declType, DataType.pi, defLocation, text)
    {
    }

    public override Entity copy()
    {
        Text textCopy = new Text(text_);
        return new PiEntity(name(), declType(), defLocation(), textCopy);
    }
}

// Internal Data Entity (abstract base for cdata/sdata)
public abstract class InternalDataEntity : InternalEntity
{
    protected InternalDataEntity(StringC name, DataType dataType, Location defLocation, Text text)
        : base(name, DeclType.generalEntity, dataType, defLocation, text)
    {
    }

    public override Boolean isDataOrSubdoc()
    {
        return true;
    }
}

// Internal CDATA Entity
public class InternalCdataEntity : InternalDataEntity
{
    public InternalCdataEntity(StringC name, Location defLocation, Text text)
        : base(name, DataType.cdata, defLocation, text)
    {
    }

    public override Entity copy()
    {
        Text textCopy = new Text(text_);
        return new InternalCdataEntity(name(), defLocation(), textCopy);
    }

    public override Boolean isCharacterData()
    {
        return true;
    }
}

// Predefined Entity (extends InternalCdataEntity)
public class PredefinedEntity : InternalCdataEntity
{
    public PredefinedEntity(StringC name, Location defLocation, Text text)
        : base(name, defLocation, text)
    {
    }
}

// Internal SDATA Entity
public class InternalSdataEntity : InternalDataEntity
{
    public InternalSdataEntity(StringC name, Location defLocation, Text text)
        : base(name, DataType.sdata, defLocation, text)
    {
    }

    public override Entity copy()
    {
        Text textCopy = new Text(text_);
        return new InternalSdataEntity(name(), defLocation(), textCopy);
    }

    public override Boolean isCharacterData()
    {
        return true;
    }
}

// Internal Text Entity
public class InternalTextEntity : InternalEntity
{
    public enum Bracketed
    {
        none,
        starttag,
        endtag,
        ms,
        md
    }

    private Bracketed bracketed_;

    public InternalTextEntity(StringC name, DeclType declType, Location defLocation, Text text, Bracketed bracketed)
        : base(name, declType, DataType.sgmlText, defLocation, text)
    {
        bracketed_ = bracketed;
    }

    public override Entity copy()
    {
        Text textCopy = new Text(text_);
        return new InternalTextEntity(name(), declType(), defLocation(), textCopy, bracketed_);
    }

    // void normalReference(ParserState &, const Ptr<EntityOrigin> &, Boolean) const;
    protected override void normalReference(ParserState parser, Ptr<EntityOrigin> origin, Boolean generateEvent)
    {
        checkRef(parser);
        checkEntlvl(parser);
        if (checkNotOpen(parser))
        {
            if (generateEvent && parser.wantMarkup())
                parser.eventHandler().entityStart(new EntityStartEvent(new ConstPtr<EntityOrigin>(origin.pointer())));
            parser.pushInput(new InternalInputSource(text_.@string(), origin.pointer()));
        }
    }

    // void litReference(Text &, ParserState &, const Ptr<EntityOrigin> &, Boolean) const;
    public override void litReference(Text text, ParserState parser, Ptr<EntityOrigin> origin, Boolean squeezeSpaces)
    {
        text.addEntityStart(new Location(origin.pointer(), 0));
        normalReference(parser, origin, false);
    }
}

// External Text Entity
public class ExternalTextEntity : ExternalEntity
{
    public ExternalTextEntity(StringC name, DeclType declType, Location defLocation, ExternalId externalId)
        : base(name, declType, DataType.sgmlText, defLocation, externalId)
    {
    }

    public override Entity copy()
    {
        return new ExternalTextEntity(name(), declType(), defLocation(), externalId());
    }

    // void normalReference(ParserState &, const Ptr<EntityOrigin> &, Boolean) const;
    protected override void normalReference(ParserState parser, Ptr<EntityOrigin> origin, Boolean generateEvent)
    {
        checkRef(parser);
        checkEntlvl(parser);
        if (checkNotOpen(parser))
        {
            if (generateEvent && parser.wantMarkup())
                parser.eventHandler().entityStart(new EntityStartEvent(new ConstPtr<EntityOrigin>(origin.pointer())));
            StringC effectiveSysId = externalId().effectiveSystemId();
            if (effectiveSysId.size() > 0)
                parser.pushInput(parser.entityManager()!.open(effectiveSysId,
                                                               parser.sd().docCharset(),
                                                               origin.pointer(),
                                                               0, // flags
                                                               parser.messenger()));
            else
                parser.message(ParserMessages.nonExistentEntityRef,
                               new StringMessageArg(name()),
                               defLocation());
        }
    }

    // void litReference(Text &, ParserState &, const Ptr<EntityOrigin> &, Boolean) const;
    public override void litReference(Text text, ParserState parser, Ptr<EntityOrigin> origin, Boolean squeezeSpaces)
    {
        if (parser.options().warnAttributeValueExternalEntityRef && declType() == DeclType.generalEntity)
            parser.message(ParserMessages.externalEntityRefInAttributeValue, new StringMessageArg(name()));
        text.addEntityStart(new Location(origin.pointer(), 0));
        normalReference(parser, origin, false);
    }
}

// External Non-Text Entity (abstract base for data and subdoc entities)
public abstract class ExternalNonTextEntity : ExternalEntity
{
    protected ExternalNonTextEntity(StringC name, DeclType declType, DataType dataType, Location defLocation, ExternalId externalId)
        : base(name, declType, dataType, defLocation, externalId)
    {
    }

    public override Boolean isDataOrSubdoc()
    {
        return true;
    }

    public override Boolean isCharacterData()
    {
        return false;
    }
}

// Ignored Entity (for conditional sections)
public class IgnoredEntity : Entity
{
    public IgnoredEntity(StringC name, DeclType declType)
        : base(name, declType, DataType.sgmlText, new Location())
    {
    }

    public override Entity copy()
    {
        return new IgnoredEntity(name(), declType());
    }
}
