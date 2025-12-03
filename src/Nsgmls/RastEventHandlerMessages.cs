using OpenSP;
// Copyright (c) 1994, 1995 James Clark
// See the file COPYING for copying permission.

// This file was automatically generated from RastEventHandlerMessages.msg by msggen.pl.

namespace Nsgmls;

public struct RastEventHandlerMessages
{
    private static readonly MessageModule appModule = new MessageModule();

    // 100
    public static readonly MessageType0 invalidRastPiError = new MessageType0(
        MessageType.Severity.error,
        appModule,
        100,
        "invalid RAST processing instruction"
    );

    // 101
    public static readonly MessageType1 invalidActiveLinkType = new MessageType1(
        MessageType.Severity.error,
        appModule,
        101,
        "invalid link type %1 in rast-active-lpd processing instruction"
    );

    // 102
    public static readonly MessageType1 duplicateActiveLinkType = new MessageType1(
        MessageType.Severity.error,
        appModule,
        102,
        "duplicate link type %1 in rast-active-lpd processing instruction"
    );

    // 103
    public static readonly MessageType0 multipleLinkRuleMatch = new MessageType0(
        MessageType.Severity.error,
        appModule,
        103,
        "rast-link-rule: processing instruction matches more than one link rule"
    );

    // 104
    public static readonly MessageType0 noLinkRuleMatch = new MessageType0(
        MessageType.Severity.error,
        appModule,
        104,
        "rast-link-rule: processing instruction does not match any link rules"
    );

    // 105
    public static readonly MessageType0 multipleLinkRules = new MessageType0(
        MessageType.Severity.error,
        appModule,
        105,
        "multiple applicable link rules without disambiguating rast-link-rule: processing instruction"
    );
}
