// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Interface for reference counting - allows Ptr<T> to work with
// classes that have Resource-like semantics but can't inherit from Resource
public interface IResource
{
    int count();
    int unref();
    void @ref();
}

public class Resource : IResource
{
    private int count_;

    // Resource();
    public Resource()
    {
        count_ = 0;
    }

    // Resource(const Resource &);
    public Resource(Resource other)
    {
        count_ = 0;
    }

    // int count() const;
    public int count()
    {
        return count_;
    }

    // int unref(); - return 1 if it should be deleted
    public int unref()
    {
        return --count_ <= 0 ? 1 : 0;
    }

    // void ref();
    public void @ref()
    {
        ++count_;
    }
}
