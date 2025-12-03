// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public static class EntityAppMessages
{
    // 4100
    public static readonly MessageType1 cHelp = new MessageType1(
        MessageType.Severity.info, null, 4100, "Use catalog %1.");

    // 4101
    public static readonly MessageType1 CHelp = new MessageType1(
        MessageType.Severity.info, null, 4101, "Arguments are catalogs, use DOCUMENT entry.");

    // 4102
    public static readonly MessageType1 DHelp = new MessageType1(
        MessageType.Severity.info, null, 4102, "Search files in directory %1.");

    // 4103
    public static readonly MessageType1 RHelp = new MessageType1(
        MessageType.Severity.info, null, 4103, "Restrict file reading (for use in Web apps).");

    // 4104
    public static readonly MessageFragment directory = new MessageFragment(
        null, 4104, "DIRECTORY");

    // 4105
    public static readonly MessageFragment sysid = new MessageFragment(
        null, 4105, "SYSID");
}
