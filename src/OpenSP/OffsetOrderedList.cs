// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class OffsetOrderedListBlock
{
    public Offset offset;       // next Offset
    public nuint nextIndex;     // index of first item in next block
    public const int size = 200;
    public byte[] bytes = new byte[size];
}

// This is an ordered list of Offsets with no duplicates.

public class OffsetOrderedList
{
    // bytes used in current block
    private int blockUsed_;
    private System.Collections.Generic.List<Owner<OffsetOrderedListBlock>> blocks_ = new System.Collections.Generic.List<Owner<OffsetOrderedListBlock>>();
    private Mutex mutex_ = new Mutex();

    // OffsetOrderedList();
    public OffsetOrderedList()
    {
        blockUsed_ = OffsetOrderedListBlock.size;
    }

    // off must be > the last offset added.
    // void append(Offset off);
    public void append(Offset off)
    {
        // At any position in the list there's a current offset.
        // The offset is initially zero.
        // A byte of 255 says add 255 to the current offset.
        // A byte B < 255, says that there's an item in the list whose
        // offset is the current offset + B, and that B + 1 should be
        // added to the current offset.
        Offset curOffset = blocks_.Count > 0 ? blocks_[blocks_.Count - 1].pointer()!.offset : 0;
        System.Diagnostics.Debug.Assert(off >= curOffset);
        Offset count = off - curOffset;
        while (count >= 255)
        {
            addByte(255);
            count -= 255;
        }
        addByte((byte)count);
    }

    // Find the last offset in the list <= off.
    // Boolean findPreceding(Offset off, size_t &foundIndex, Offset &foundOffset) const;
    public Boolean findPreceding(Offset off, out nuint foundIndex, out Offset foundOffset)
    {
        Mutex.Lock @lock = new Mutex.Lock(mutex_);

        // Invariant:
        // blocks with index < i have offset <= off
        // blocks with index >= lim have offset > off
        int i = 0;
        int lim = blocks_.Count;
        // Most commonly we'll want to know the about positions near the end,
        // so optimize this case.
        if (lim > 0 && blocks_[lim - 1].pointer()!.offset <= off)
            i = lim;
        else if (lim > 1 && blocks_[lim - 2].pointer()!.offset <= off)
            i = lim - 1;
        else
        {
            // Do a binary search.
            while (i < lim)
            {
                int mid = i + (lim - i) / 2;
                if (blocks_[mid].pointer()!.offset > off)
                    lim = mid;
                else
                    i = mid + 1;
            }
        }

        if (i == blocks_.Count)
        {
            if (i == 0)
            {
                foundIndex = 0;
                foundOffset = 0;
                return false;
            }
            foundIndex = blocks_[blocks_.Count - 1].pointer()!.nextIndex - 1;
            foundOffset = blocks_[blocks_.Count - 1].pointer()!.offset - 1;
            return true;
        }

        // Note that an item with offset X can only occur in a block with offset > X
        // i is now the first block with offset > off
        Offset curOff = blocks_[i].pointer()!.offset;
        nuint curIndex = blocks_[i].pointer()!.nextIndex;
        byte[] bytes = blocks_[i].pointer()!.bytes;
        int j = (i == blocks_.Count - 1
                 ? blockUsed_
                 : OffsetOrderedListBlock.size);
        for (;;)
        {
            j--;
            if (bytes[j] != 255)
            {
                curIndex -= 1;
                curOff -= 1;
                if (curOff <= off)
                    break;
            }
            curOff -= bytes[j];
            if (j == 0)
            {
                if (i == 0)
                {
                    foundIndex = 0;
                    foundOffset = 0;
                    return false;
                }
                i--;
                j = OffsetOrderedListBlock.size;
                curOff = blocks_[i].pointer()!.offset;
                curIndex = blocks_[i].pointer()!.nextIndex;
                bytes = blocks_[i].pointer()!.bytes;
            }
        }
        foundIndex = curIndex;
        foundOffset = curOff;
        return true;
    }

    // size_t size() const;
    public nuint size()
    {
        return blocks_.Count == 0 ? 0 : blocks_[blocks_.Count - 1].pointer()!.nextIndex;
    }

    private void addByte(byte b)
    {
        if (blockUsed_ >= OffsetOrderedListBlock.size)
        {
            Mutex.Lock @lock = new Mutex.Lock(mutex_);
            Owner<OffsetOrderedListBlock> last = new Owner<OffsetOrderedListBlock>(new OffsetOrderedListBlock());
            if (blocks_.Count == 0)
            {
                last.pointer()!.nextIndex = 0;
                last.pointer()!.offset = 0;
            }
            else
            {
                OffsetOrderedListBlock lastButOne = blocks_[blocks_.Count - 1].pointer()!;
                last.pointer()!.nextIndex = lastButOne.nextIndex;
                last.pointer()!.offset = lastButOne.offset;
            }
            blocks_.Add(last);
            blockUsed_ = 0;
        }
        blocks_[blocks_.Count - 1].pointer()!.bytes[blockUsed_] = b;
        if (b == 255)
            blocks_[blocks_.Count - 1].pointer()!.offset += 255;
        else
        {
            blocks_[blocks_.Count - 1].pointer()!.offset += (Offset)(b + 1);
            blocks_[blocks_.Count - 1].pointer()!.nextIndex += 1;
        }
        blockUsed_++;
    }
}
