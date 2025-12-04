// Copyright (c) 1994, 1995 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Catalog message definitions (from CatalogMessages.msg)
// Message IDs start at 2100
public static class CatalogMessages
{
    // E0+nameExpected++name expected
    public static readonly MessageType0 nameExpected =
        new MessageType0(MessageType.Severity.error, null, 2100, "name expected");

    // E0+literalExpected++literal expected
    public static readonly MessageType0 literalExpected =
        new MessageType0(MessageType.Severity.error, null, 2101, "literal expected");

    // E0+nameOrLiteralExpected++name or literal expected
    public static readonly MessageType0 nameOrLiteralExpected =
        new MessageType0(MessageType.Severity.error, null, 2102, "name or literal expected");

    // E0+nulChar++nul character
    public static readonly MessageType0 nulChar =
        new MessageType0(MessageType.Severity.error, null, 2103, "nul character");

    // E0+minimumData++not a minimum data character
    public static readonly MessageType0 minimumData =
        new MessageType0(MessageType.Severity.error, null, 2104, "not a minimum data character");

    // E0+eofInComment++end of entity in comment
    public static readonly MessageType0 eofInComment =
        new MessageType0(MessageType.Severity.error, null, 2105, "end of entity in comment");

    // E0+eofInLiteral++end of entity in literal
    public static readonly MessageType0 eofInLiteral =
        new MessageType0(MessageType.Severity.error, null, 2106, "end of entity in literal");

    // E0+overrideYesOrNo++OVERRIDE requires argument of YES or NO
    public static readonly MessageType0 overrideYesOrNo =
        new MessageType0(MessageType.Severity.error, null, 2107, "OVERRIDE requires argument of YES or NO");

    // E0+inLoop++CATALOG entries cause loop
    public static readonly MessageType0 inLoop =
        new MessageType0(MessageType.Severity.error, null, 2108, "CATALOG entries cause loop");

    // W0+systemShouldQuote++second argument for SYSTEM entry should be quoted to avoid ambiguity
    public static readonly MessageType0 systemShouldQuote =
        new MessageType0(MessageType.Severity.warning, null, 2109, "second argument for SYSTEM entry should be quoted to avoid ambiguity");

    // E1+noDocumentEntry++no DOCUMENT entry in catalog %1
    public static readonly MessageType1 noDocumentEntry =
        new MessageType1(MessageType.Severity.error, null, 2110, "no DOCUMENT entry in catalog %1");

    // E2+noPublicEntry++no entry for public identifier %1 in catalog %2
    public static readonly MessageType2 noPublicEntry =
        new MessageType2(MessageType.Severity.error, null, 2111, "no entry for public identifier %1 in catalog %2");
}
