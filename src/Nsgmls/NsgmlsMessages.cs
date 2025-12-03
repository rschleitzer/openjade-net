using OpenSP;
// Copyright (c) 1994, 1995 James Clark
// See the file COPYING for copying permission.

// This file was automatically generated from NsgmlsMessages.msg by msggen.pl.

namespace Nsgmls;

public struct NsgmlsMessages
{
    private static readonly MessageModule appModule = new MessageModule();

    // 0
    public static readonly MessageType1 unknownOutputOption = new MessageType1(
        MessageType.Severity.error,
        appModule,
        0,
        "unknown output option %1"
    );

    // 1
    public static readonly MessageType1 BHelp = new MessageType1(
        MessageType.Severity.info,
        appModule,
        1,
        "Enable batch mode."
    );

    // 2
    public static readonly MessageType1 oHelp = new MessageType1(
        MessageType.Severity.info,
        appModule,
        2,
        "Produce output according to %1."
    );

    // 3
    public static readonly MessageType1 pHelp = new MessageType1(
        MessageType.Severity.info,
        appModule,
        3,
        "Stop after the document prolog."
    );

    // 4
    public static readonly MessageType1 sHelp = new MessageType1(
        MessageType.Severity.info,
        appModule,
        4,
        "Suppress output."
    );

    // 5
    public static readonly MessageType1 tHelp = new MessageType1(
        MessageType.Severity.info,
        appModule,
        5,
        "Send RAST output to file %1."
    );

    // 6
    public static readonly MessageType1 dHelp = new MessageType1(
        MessageType.Severity.info,
        appModule,
        6,
        "Same as -wduplicate."
    );

    // 7
    public static readonly MessageType1 lHelp = new MessageType1(
        MessageType.Severity.info,
        appModule,
        7,
        "Same as -oline."
    );

    // 8
    public static readonly MessageType1 mHelp = new MessageType1(
        MessageType.Severity.info,
        appModule,
        8,
        "Same as -c."
    );

    // 9
    public static readonly MessageType1 rHelp = new MessageType1(
        MessageType.Severity.info,
        appModule,
        9,
        "Same as -wdefault."
    );

    // 10
    public static readonly MessageType1 uHelp = new MessageType1(
        MessageType.Severity.info,
        appModule,
        10,
        "Same as -wundef."
    );

    // 11
    public static readonly MessageType1 info1 = new MessageType1(
        MessageType.Severity.info,
        appModule,
        11,
        "%1 parses and validates the SGML document whose document entity is"
    );

    // 12
    public static readonly MessageType1 info2 = new MessageType1(
        MessageType.Severity.info,
        appModule,
        12,
        "specified by the system identifiers SYSID... and prints on the standard"
    );

    // 13
    public static readonly MessageType1 info3 = new MessageType1(
        MessageType.Severity.info,
        appModule,
        13,
        "output a simple text representation of its Element Structure Information Set."
    );

    // 14
    public static readonly MessageType1 info4 = new MessageType1(
        MessageType.Severity.info,
        appModule,
        14,
        "If more than one system identifier is specified, then the corresponding"
    );

    // 15
    public static readonly MessageType1 info5 = new MessageType1(
        MessageType.Severity.info,
        appModule,
        15,
        "entities will be concatenated to form the document entity. If no system"
    );

    // 16
    public static readonly MessageType1 info6 = new MessageType1(
        MessageType.Severity.info,
        appModule,
        16,
        "identifiers are specified, then %1 will read the document entity from"
    );

    // 17
    public static readonly MessageType1 info7 = new MessageType1(
        MessageType.Severity.info,
        appModule,
        17,
        "the standard input. A command line system identifier of - can be used"
    );

    // 18
    public static readonly MessageType1 info8 = new MessageType1(
        MessageType.Severity.info,
        appModule,
        18,
        "to refer to the standard input."
    );

    // 19
    public static readonly MessageFragment file = new MessageFragment(
        appModule,
        19,
        "FILE"
    );

    // 20
    public static readonly MessageFragment option = new MessageFragment(
        appModule,
        20,
        "OPTION"
    );

    // 21
    public static readonly MessageFragment sysid = new MessageFragment(
        appModule,
        21,
        "SYSID"
    );
}
