// Copyright (c) 1994, 1995 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public static class PosixStorageMessages
{
    // 2200
    public static readonly MessageType2 readSystemCall = new MessageType2(
        MessageType.Severity.error, null, 2200, "error reading %1 (%2)");

    // 2201
    public static readonly MessageType2 openSystemCall = new MessageType2(
        MessageType.Severity.error, null, 2201, "cannot open %1 (%2)");

    // 2202
    public static readonly MessageType2 closeSystemCall = new MessageType2(
        MessageType.Severity.error, null, 2202, "error closing %1 (%2)");

    // 2203
    public static readonly MessageType2 lseekSystemCall = new MessageType2(
        MessageType.Severity.error, null, 2203, "error seeking on %1 (%2)");

    // 2204
    public static readonly MessageType1 invalidFilename = new MessageType1(
        MessageType.Severity.error, null, 2204, "invalid filename %1");

    // 2205
    public static readonly MessageType2 fdRead = new MessageType2(
        MessageType.Severity.error, null, 2205, "error reading file descriptor %1 (%2)");

    // 2206
    public static readonly MessageType2 fdLseek = new MessageType2(
        MessageType.Severity.error, null, 2206, "error seeking on file descriptor %1 (%2)");

    // 2207
    public static readonly MessageType1 invalidNumber = new MessageType1(
        MessageType.Severity.error, null, 2207, "%1 is not a valid file descriptor number");

    // 2208
    public static readonly MessageType2 cannotFind = new MessageType2(
        MessageType.Severity.error, null, 2208, "cannot find %1; tried %2");
}
