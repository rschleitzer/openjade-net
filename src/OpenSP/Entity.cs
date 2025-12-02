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
}

public abstract class ExternalDataEntity : ExternalEntity
{
    protected ExternalDataEntity(StringC name, DataType dataType, Location defLocation, ExternalId externalId)
        : base(name, DeclType.generalEntity, dataType, defLocation, externalId)
    {
    }

    public override ExternalDataEntity? asExternalDataEntity()
    {
        return this;
    }
}

public abstract class SubdocEntity : ExternalEntity
{
    protected SubdocEntity(StringC name, Location defLocation, ExternalId externalId)
        : base(name, DeclType.generalEntity, DataType.subdoc, defLocation, externalId)
    {
    }

    public override SubdocEntity? asSubdocEntity()
    {
        return this;
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
        Text textCopy = new Text();
        text_.swap(textCopy);
        return new PiEntity(name(), declType(), defLocation(), textCopy);
    }
}
