// This file was automatically generated from MessageReporterMessages.msg by msggen.pl.
// Copyright (c) 1994 James Clark

namespace OpenSP;

public static class MessageReporterMessages
{
    private static readonly MessageModule module = new MessageModule("MessageReporter");

    // 5000
    public static readonly MessageFragment infoTag =
        new MessageFragment(module, 0, "I");
    // 5001
    public static readonly MessageFragment warningTag =
        new MessageFragment(module, 1, "W");
    // 5002
    public static readonly MessageFragment quantityErrorTag =
        new MessageFragment(module, 2, "Q");
    // 5003
    public static readonly MessageFragment idrefErrorTag =
        new MessageFragment(module, 3, "X");
    // 5004
    public static readonly MessageFragment errorTag =
        new MessageFragment(module, 4, "E");
    // 5005
    public static readonly MessageFragment openElements =
        new MessageFragment(module, 5, "open elements");
    // 5006
    public static readonly MessageFragment inNamedEntity =
        new MessageFragment(module, 6, "In entity %1 included from %2");
    // 5007
    public static readonly MessageFragment inUnnamedEntity =
        new MessageFragment(module, 7, "In entity included from %2");
    // 5008
    public static readonly MessageFragment invalidLocation =
        new MessageFragment(module, 8, "(invalid location)");
    // 5009
    public static readonly MessageFragment offset =
        new MessageFragment(module, 9, "offset ");
    // 5010
    public static readonly MessageFragment relevantClauses =
        new MessageFragment(module, 10, "relevant clauses: ");
}
