// Copyright (c) 1997 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class NotationStorageManager : StorageManager
{
    private string type_;

    // NotationStorageManager(const char *type);
    public NotationStorageManager(string type)
    {
        type_ = type;
    }

    // Boolean inheritable() const;
    public override Boolean inheritable()
    {
        return false;
    }

    // const char *type() const;
    public override string type()
    {
        return type_;
    }

    // StorageObject *makeStorageObject(const StringC &id,
    //                                  const StringC &baseId,
    //                                  Boolean search,
    //                                  Boolean mayRewind,
    //                                  Messenger &,
    //                                  StringC &foundId);
    public override StorageObject? makeStorageObject(StringC id,
                                                     StringC baseId,
                                                     Boolean search,
                                                     Boolean mayRewind,
                                                     Messenger mgr,
                                                     StringC foundId)
    {
        return null;
    }
}
