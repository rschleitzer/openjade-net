// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Named
{
    private StringC name_ = new StringC();

    // Named(const StringC &name) : name_(name) { }
    public Named(StringC name)
    {
        name_ = new StringC(name);
    }

    // virtual ~Named() { }
    // C# GC handles this

    // const StringC &name() const { return name_; }
    public StringC name()
    {
        return name_;
    }

    // const StringC *namePointer() const { return &name_; }
    public StringC namePointer()
    {
        return name_;
    }

    // void setName(const StringC &name) { name_ = name; }
    public void setName(StringC name)
    {
        name_ = new StringC(name);
    }

    // void swap(Named &to) { name_.swap(to.name_); }
    public void swap(Named to)
    {
        name_.swap(to.name_);
    }
}
