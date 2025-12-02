// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class EntityDecl : NamedResource
{
    public enum DeclType
    {
        generalEntity,
        parameterEntity,
        doctype,
        linktype,
        notation,
        sgml
    }

    public enum DataType
    {
        sgmlText,
        pi,
        cdata,
        sdata,
        ndata,
        subdoc
    }

    private DeclType declType_;
    private DataType dataType_;
    private PackedBoolean dtdIsBase_;
    private PackedBoolean lpdIsActive_;
    private Location defLocation_ = new Location();
    private ConstPtr<StringResource<Char>> dtdName_ = new ConstPtr<StringResource<Char>>();
    private ConstPtr<StringResource<Char>> lpdName_ = new ConstPtr<StringResource<Char>>();

    // EntityDecl(const StringC &, DeclType declType, DataType dataType, const Location &defLocation);
    public EntityDecl(StringC name, DeclType declType, DataType dataType, Location defLocation)
        : base(name)
    {
        declType_ = declType;
        dataType_ = dataType;
        defLocation_ = new Location(defLocation);
    }

    // DataType dataType() const;
    public DataType dataType()
    {
        return dataType_;
    }

    // DeclType declType() const;
    public DeclType declType()
    {
        return declType_;
    }

    // const Location &defLocation() const;
    public Location defLocation()
    {
        return defLocation_;
    }

    // Boolean declInDtdIsBase() const;
    public Boolean declInDtdIsBase()
    {
        return dtdIsBase_;
    }

    // Boolean declInActiveLpd() const;
    public Boolean declInActiveLpd()
    {
        return lpdIsActive_;
    }

    // const StringC *declInDtdNamePointer() const;
    public StringResource<Char>? declInDtdNamePointer()
    {
        return dtdName_.pointer();
    }

    // const StringC *declInLpdNamePointer() const;
    public StringResource<Char>? declInLpdNamePointer()
    {
        return lpdName_.pointer();
    }

    // void setDeclIn(const ConstPtr<StringResource<Char> > &dtdName, Boolean dtdIsBase,
    //                const ConstPtr<StringResource<Char> > &lpdName, Boolean lpdIsActive);
    public void setDeclIn(ConstPtr<StringResource<Char>> dtdName, Boolean dtdIsBase,
                          ConstPtr<StringResource<Char>> lpdName, Boolean lpdIsActive)
    {
        dtdName_ = new ConstPtr<StringResource<Char>>(dtdName);
        dtdIsBase_ = dtdIsBase;
        lpdName_ = new ConstPtr<StringResource<Char>>(lpdName);
        lpdIsActive_ = lpdIsActive;
    }

    // void setDeclIn(const ConstPtr<StringResource<Char> > &dtdName, Boolean dtdIsBase);
    public void setDeclIn(ConstPtr<StringResource<Char>> dtdName, Boolean dtdIsBase)
    {
        dtdName_ = new ConstPtr<StringResource<Char>>(dtdName);
        dtdIsBase_ = dtdIsBase;
    }

    // void setDefLocation(const Location &);
    public void setDefLocation(Location loc)
    {
        defLocation_ = new Location(loc);
    }

    // virtual const StringC *systemIdPointer() const;
    public virtual StringC? systemIdPointer()
    {
        return null;
    }

    // virtual const StringC *publicIdPointer() const;
    public virtual StringC? publicIdPointer()
    {
        return null;
    }

    // virtual const StringC *effectiveSystemIdPointer() const;
    public virtual StringC? effectiveSystemIdPointer()
    {
        return null;
    }
}
