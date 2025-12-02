// Copyright (c) 1995 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public abstract class EntityManager : Resource
{
    // enum { mayRewind = 01, maySetDocCharset = 02 };
    public const int mayRewind = 0x01;
    public const int maySetDocCharset = 0x02;

    // virtual ~EntityManager();
    // C# GC handles cleanup

    // virtual Boolean internalCharsetIsDocCharset() const = 0;
    public abstract Boolean internalCharsetIsDocCharset();

    // virtual const CharsetInfo &charset() const = 0;
    public abstract CharsetInfo charset();

    // virtual InputSource *open(const StringC &sysid,
    //                           const CharsetInfo &docCharset,
    //                           InputSourceOrigin *,
    //                           unsigned flags,
    //                           Messenger &) = 0;
    public abstract InputSource? open(StringC sysid,
                                      CharsetInfo docCharset,
                                      InputSourceOrigin? origin,
                                      uint flags,
                                      Messenger mgr);

    // virtual ConstPtr<EntityCatalog>
    //   makeCatalog(StringC &systemId, const CharsetInfo &, Messenger &) = 0;
    public abstract ConstPtr<EntityCatalog> makeCatalog(StringC systemId,
                                                        CharsetInfo charset,
                                                        Messenger mgr);
}
