// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Note: C# doesn't support multiple inheritance of classes.
// Named and Resource are both classes, so NamedResource inherits from Named
// and implements IResource via composition with a Resource instance.
public class NamedResource : Named, IResource
{
    private Resource resource_ = new Resource();

    // NamedResource(const StringC &str) : Named(str) { }
    public NamedResource(StringC str) : base(str)
    {
    }

    // Resource methods delegated to internal Resource instance
    public int count()
    {
        return resource_.count();
    }

    public int unref()
    {
        return resource_.unref();
    }

    public void @ref()
    {
        resource_.@ref();
    }
}
