// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class StorageObjectPosition
{
    // the number of RSs preceding line 1 of this storage object
    // or -1 if this hasn't been computed yet.
    public nuint line1RS;
    public Owner<Decoder> decoder = new Owner<Decoder>();
    // Does the storage object start with an RS?
    public PackedBoolean startsWithRS;
    // Were the RSs other than the first in the storage object inserted?
    public PackedBoolean insertedRSs;
    public Offset endOffset;
    public StringC id = new StringC();

    // StorageObjectPosition();
    public StorageObjectPosition()
    {
        line1RS = unchecked((nuint)(-1)); // -1 means not computed yet
        startsWithRS = false;
        insertedRSs = false;
        endOffset = 0;
    }
}
