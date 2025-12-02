// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Note: C# doesn't support multiple inheritance of classes.
// String<T> and Resource are both classes, so StringResource inherits from String<T>
// and contains a Resource instance for reference counting.
public class StringResource<T> : String<T> where T : struct
{
    private Resource resource_ = new Resource();

    // StringResource(const String<T> &s) : String<T>(s) { }
    public StringResource(String<T> s) : base(s)
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
