// Copyright (c) 1996 James Clark, 1999 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

public static class CmdLineAppMessages
{
    // 4000
    public static readonly MessageType1 invalidOptionError = new MessageType1(
        MessageType.Severity.error, null, 4000, "invalid option %1");

    // 4001
    public static readonly MessageType1 missingOptionArgError = new MessageType1(
        MessageType.Severity.error, null, 4001, "missing argument for option %1");

    // 4002
    public static readonly MessageType1 ambiguousOptionError = new MessageType1(
        MessageType.Severity.error, null, 4002, "option %1 is ambiguous");

    // 4003
    public static readonly MessageType1 erroneousOptionArgError = new MessageType1(
        MessageType.Severity.error, null, 4003, "option %1 doesn't allow an argument");

    // 4004
    public static readonly MessageType1 usage = new MessageType1(
        MessageType.Severity.info, null, 4004, "Usage: %1");

    // 4005
    public static readonly MessageType1 usageCont = new MessageType1(
        MessageType.Severity.info, null, 4005, "or: %1");

    // 4006
    public static readonly MessageType1 defaultUsage = new MessageType1(
        MessageType.Severity.info, null, 4006, "%1 [OPTION] SYSID...");

    // 4007
    public static readonly MessageType1 usageStart = new MessageType1(
        MessageType.Severity.info, null, 4007, "Short options need the same arguments as their long forms.");

    // 4008
    public static readonly MessageType2 versionInfo = new MessageType2(
        MessageType.Severity.info, null, 4008, "%1 version %2");

    // 4009
    public static readonly MessageType1 unknownBctf = new MessageType1(
        MessageType.Severity.error, null, 4009, "unknown BCTF %1");

    // 4010
    public static readonly MessageType1 unknownEncoding = new MessageType1(
        MessageType.Severity.error, null, 4010, "unknown encoding %1");

    // 4011
    public static readonly MessageType2 openFileError = new MessageType2(
        MessageType.Severity.error, null, 4011, "cannot open output file %1 (%2)");

    // 4012
    public static readonly MessageType2 closeFileError = new MessageType2(
        MessageType.Severity.error, null, 4012, "cannot close output file %1 (%2)");

    // 4013
    public static readonly MessageType1 bHelp = new MessageType1(
        MessageType.Severity.info, null, 4013, "Use bctf %1 for output.");

    // 4014
    public static readonly MessageType1 eHelp = new MessageType1(
        MessageType.Severity.info, null, 4014, "Use encoding %1 for output.");

    // 4015
    public static readonly MessageType1 fHelp = new MessageType1(
        MessageType.Severity.info, null, 4015, "Append error messages to file %1.");

    // 4016
    public static readonly MessageType1 vHelp = new MessageType1(
        MessageType.Severity.info, null, 4016, "Display the program version.");

    // 4017
    public static readonly MessageType1 hHelp = new MessageType1(
        MessageType.Severity.info, null, 4017, "Show this help text.");

    // 4018
    public static readonly MessageFragment name = new MessageFragment(
        null, 4018, "NAME");

    // 4019
    public static readonly MessageFragment file = new MessageFragment(
        null, 4019, "FILE");

    // 4020
    public static readonly MessageFragment noArg = new MessageFragment(
        null, 4020, "NOTHING");

    // 4021
    public static readonly MessageType0 tryHelpOptionForInfo = new MessageType0(
        MessageType.Severity.info, null, 4021, "Try the \"--help\" option for more information.");

    // 4022
    public static readonly MessageFragment someArg = new MessageFragment(
        null, 4022, "ARG");

    // 4023
    public static readonly MessageType1 undocOption = new MessageType1(
        MessageType.Severity.info, null, 4023, "Undocumented option (check the application's manual).");
}
