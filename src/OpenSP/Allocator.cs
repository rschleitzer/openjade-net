// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// In C#, the garbage collector handles memory allocation.
// This class preserves the interface but delegates to the runtime.

namespace OpenSP;

public class Allocator
{
    private nuint maxSize_;
    private uint blocksPerSegment_;

    // Allocator(size_t maxSize, unsigned blocksPerSegment);
    public Allocator(nuint maxSize, uint blocksPerSegment)
    {
        maxSize_ = maxSize;
        blocksPerSegment_ = blocksPerSegment;
    }

    // void *alloc(size_t);
    // In C#, allocation is handled by the runtime.
    // This method is kept for interface compatibility but
    // actual allocation happens via 'new' at call sites.
    public nuint alloc(nuint size)
    {
        // In C#, we don't have void* allocation like C++
        // Callers should use 'new' directly
        return size;
    }

    // static void *allocSimple(size_t);
    public static nuint allocSimple(nuint size)
    {
        return size;
    }

    // static void free(void *);
    public static void free(object? p)
    {
        // No-op in C# - GC handles deallocation
    }
}
