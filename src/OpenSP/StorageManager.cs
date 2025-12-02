// Copyright (c) 1994, 1995 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public abstract class StorageObject
{
    private const nuint DEFAULT_BLOCK_SIZE = 1024;

    // StorageObject();
    public StorageObject()
    {
    }

    // virtual ~StorageObject();
    // C# GC handles cleanup

    // virtual Boolean read(char *buf, size_t bufSize, Messenger &, size_t &nread) = 0;
    public abstract Boolean read(byte[] buf, nuint bufSize, Messenger mgr, out nuint nread);

    // virtual Boolean rewind(Messenger &) = 0;
    public abstract Boolean rewind(Messenger mgr);

    // virtual void willNotRewind();
    public virtual void willNotRewind()
    {
    }

    // virtual size_t getBlockSize() const;
    public virtual nuint getBlockSize()
    {
        return DEFAULT_BLOCK_SIZE;
    }
}

public abstract class StorageManager
{
    // StorageManager();
    public StorageManager()
    {
    }

    // virtual ~StorageManager();
    // C# GC handles cleanup

    // virtual StorageObject *makeStorageObject(...) = 0;
    public abstract StorageObject? makeStorageObject(StringC specId,
                                                     StringC baseId,
                                                     Boolean search,
                                                     Boolean mayRewind,
                                                     Messenger mgr,
                                                     StringC actualId);

    // virtual const char *type() const = 0;
    public abstract string type();

    // virtual Boolean inheritable() const;
    public virtual Boolean inheritable()
    {
        return true;
    }

    // virtual Boolean transformNeutral(StringC &, Boolean fold, Messenger &) const;
    public virtual Boolean transformNeutral(StringC str, Boolean fold, Messenger mgr)
    {
        return false;
    }

    // virtual Boolean resolveRelative(const StringC &base, StringC &specId, Boolean search) const;
    public virtual Boolean resolveRelative(StringC @base, StringC specId, Boolean search)
    {
        return true;
    }

    // virtual Boolean guessIsId(const StringC &, const CharsetInfo &) const;
    public virtual Boolean guessIsId(StringC str, CharsetInfo charset)
    {
        return false;
    }

    // virtual const InputCodingSystem *requiredCodingSystem() const;
    public virtual InputCodingSystem? requiredCodingSystem()
    {
        return null;
    }

    // virtual Boolean requiresCr() const;
    public virtual Boolean requiresCr()
    {
        return false;
    }

    // virtual const CharsetInfo *idCharset() const;
    public virtual CharsetInfo? idCharset()
    {
        return null;
    }

    // virtual const StringC *reString() const;
    public virtual StringC? reString()
    {
        return null;
    }
}

public abstract class IdStorageManager : StorageManager
{
    protected StringC reString_ = new StringC();
    private CharsetInfo? idCharset_;

    // IdStorageManager(const CharsetInfo *idCharset);
    public IdStorageManager(CharsetInfo? idCharset)
    {
        idCharset_ = idCharset;
    }

    // const CharsetInfo *idCharset() const;
    public override CharsetInfo? idCharset()
    {
        return idCharset_;
    }

    // const StringC *reString() const;
    public override StringC? reString()
    {
        return reString_;
    }
}
