// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public static class ParserAppMessages
{
    // 4200
    public static readonly MessageType1 unknownWarning = new MessageType1(
        MessageType.Severity.error, null, 4200, "unknown warning type %1");

    // 4201
    public static readonly MessageType0 badErrorLimit = new MessageType0(
        MessageType.Severity.error, null, 4201, "invalid error limit");

    // 4202
    public static readonly MessageType1 errorLimitExceeded = new MessageType1(
        MessageType.Severity.info, null, 4202, "maximum number of errors (%1) reached; change with -E option");

    // 4203
    public static readonly MessageType1 aHelp = new MessageType1(
        MessageType.Severity.info, null, 4203, "Make doctype or linktype %1 active.");

    // 4204
    public static readonly MessageType1 AHelp = new MessageType1(
        MessageType.Severity.info, null, 4204, "Parse wrt. architecture %1.");

    // 4205
    public static readonly MessageType1 EHelp = new MessageType1(
        MessageType.Severity.info, null, 4205, "Give up after %1 errors.");

    // 4206
    public static readonly MessageType1 eHelp = new MessageType1(
        MessageType.Severity.info, null, 4206, "Show open entities in error messages.");

    // 4207
    public static readonly MessageType1 gHelp = new MessageType1(
        MessageType.Severity.info, null, 4207, "Show open elements in error messages.");

    // 4208
    public static readonly MessageType1 nHelp = new MessageType1(
        MessageType.Severity.info, null, 4208, "Show error numbers in error messages.");

    // 4209
    public static readonly MessageType1 xHelp = new MessageType1(
        MessageType.Severity.info, null, 4209, "Show references in error messages.");

    // 4210
    public static readonly MessageType1 iHelp = new MessageType1(
        MessageType.Severity.info, null, 4210, "Define parameter entity %1 as \"INCLUDE\".");

    // 4211
    public static readonly MessageType1 wHelp = new MessageType1(
        MessageType.Severity.info, null, 4211, "Enable warning %1.");

    // 4212
    public static readonly MessageFragment type = new MessageFragment(
        null, 4212, "TYPE");

    // 4213
    public static readonly MessageFragment name = new MessageFragment(
        null, 4213, "NAME");

    // 4214
    public static readonly MessageFragment number = new MessageFragment(
        null, 4214, "NUMBER");
}
