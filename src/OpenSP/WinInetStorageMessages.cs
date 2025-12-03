// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public static class WinInetStorageMessages
{
    // 2500
    public static readonly MessageType2 cannotOpen = new MessageType2(
        MessageType.Severity.error, null, 2500, "cannot open URL %1 (%2)");

    // 2501
    public static readonly MessageType2 readFailed = new MessageType2(
        MessageType.Severity.error, null, 2501, "error reading URL %1 (%2)");
}
