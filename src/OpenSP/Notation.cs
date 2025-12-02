// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Forward declaration - will be implemented when parser is ported
public class ParserState
{
    // Stub - to be implemented later
    public ParserState()
    {
    }
}

public class Notation : EntityDecl
{
    private PackedBoolean defined_;
    private ExternalId externalId_ = new ExternalId();

    // Notation(const StringC &, const ConstPtr<StringResource<Char> > &dtdName, Boolean dtdIsBase);
    public Notation(StringC name, ConstPtr<StringResource<Char>> dtdName, Boolean dtdIsBase)
        : base(name, DeclType.notation, DataType.ndata, new Location())
    {
        defined_ = false;
        setDeclIn(dtdName, dtdIsBase);
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
