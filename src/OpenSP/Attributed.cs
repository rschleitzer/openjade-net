// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Forward declaration for AttributeDefinitionList
// Will be fully implemented when Attribute.cs is ported
public class AttributeDefinitionList : Resource
{
    // Stub implementation - will be expanded later
    public AttributeDefinitionList()
    {
    }
}

// This is used for things that have attribute definitions
// that notations and elements.

public class Attributed
{
    private Ptr<AttributeDefinitionList> attributeDef_ = new Ptr<AttributeDefinitionList>();

    // Attributed()
    public Attributed()
    {
    }

    // ConstPtr<AttributeDefinitionList> attributeDef() const;
    public ConstPtr<AttributeDefinitionList> attributeDef()
    {
        return new ConstPtr<AttributeDefinitionList>(attributeDef_.pointer());
    }

    // const AttributeDefinitionList *attributeDefTemp() const;
    public AttributeDefinitionList? attributeDefTemp()
    {
        return attributeDef_.pointer();
    }

    // Ptr<AttributeDefinitionList> attributeDef();
    public Ptr<AttributeDefinitionList> attributeDefMutable()
    {
        return attributeDef_;
    }

    // void setAttributeDef(const Ptr<AttributeDefinitionList> &);
    public void setAttributeDef(Ptr<AttributeDefinitionList> def)
    {
        attributeDef_ = def;
    }
}
