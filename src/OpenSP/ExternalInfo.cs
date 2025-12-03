// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Abstract base class for external entity information
// RTTI_CLASS in C++ - C# has built-in RTTI via GetType()
// virtual ~ExternalInfo() in C++ - implemented as IDisposable pattern
public class ExternalInfo : IDisposable
{
    private bool disposed_ = false;

    // virtual ~ExternalInfo();
    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed_)
        {
            disposed_ = true;
        }
    }

    ~ExternalInfo()
    {
        Dispose(false);
    }
}
