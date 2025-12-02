// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

using System.Runtime.InteropServices;

namespace OpenSP;

public static class ConsoleOutput
{
    // Returns null if fd is not a console.
    // static OutputCharStream *makeOutputCharStream(int fd);
    public static OutputCharStream? makeOutputCharStream(int fd)
    {
        // On Windows, we could check if fd is a console and return a special stream.
        // On Unix/Mac, we just return null (same as the non-SP_WIDE_SYSTEM case).
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // For Windows, we could create a ConsoleOutputCharStream
            // but for now, return null to match Unix behavior
            return null;
        }
        return null;
    }
}
