// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// In C++, Notation also inherits from Attributed for attribute support.
// Since C# doesn't support multiple inheritance, we include Attributed functionality here.
public class Notation : EntityDecl
{
    private PackedBoolean defined_;
    private ExternalId externalId_ = new ExternalId();
    private Ptr<AttributeDefinitionList> attributeDef_ = new Ptr<AttributeDefinitionList>();

    // Notation(const StringC &, const ConstPtr<StringResource<Char> > &dtdName, Boolean dtdIsBase);
    public Notation(StringC name, ConstPtr<StringResource<Char>> dtdName, Boolean dtdIsBase)
        : base(name, DeclType.notation, DataType.ndata, new Location())
    {
        defined_ = false;
        setDeclIn(dtdName, dtdIsBase);
    }

    // Attributed methods - included here since C# doesn't support multiple inheritance
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

    // virtual ~Notation();
    // C# GC handles cleanup

    // void setExternalId(const ExternalId &, const Location &);
    public void setExternalId(ExternalId id, Location defLocation)
    {
        externalId_ = id;
        defined_ = true;
        setDefLocation(defLocation);
    }

    // const ExternalId &externalId() const;
    public ExternalId externalId()
    {
        return externalId_;
    }

    // Boolean defined() const;
    public Boolean defined()
    {
        return defined_;
    }

    // void generateSystemId(ParserState &);
    public void generateSystemId(ParserState parser)
    {
        // TODO: Implement when ParserState is fully ported
        // This method generates system IDs using entity catalog lookup
        // For now, this is a stub
    }

    // const StringC *systemIdPointer() const;
    public override StringC? systemIdPointer()
    {
        return externalId_.systemIdString();
    }

    // const StringC *publicIdPointer() const;
    public override StringC? publicIdPointer()
    {
        return externalId_.publicIdString();
    }
}
