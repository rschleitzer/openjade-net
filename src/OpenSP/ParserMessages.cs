// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// TODO: Port full ParserMessages from ParserMessages.h/.msg

namespace OpenSP;

public static class ParserMessages
{
    // Placeholder for list separator fragment
    public static readonly MessageFragment listSep = new MessageFragment(null, 0, ", ");

    // FPI (Formal Public Identifier) error messages
    public static readonly MessageType1 fpiMissingField = new MessageType1(
        MessageType.Severity.error, null, 1, "missing field in formal public identifier");
    public static readonly MessageType1 fpiMissingTextClassSpace = new MessageType1(
        MessageType.Severity.error, null, 2, "missing space after text class in formal public identifier");
    public static readonly MessageType1 fpiInvalidTextClass = new MessageType1(
        MessageType.Severity.error, null, 3, "invalid text class in formal public identifier");
    public static readonly MessageType1 fpiInvalidLanguage = new MessageType1(
        MessageType.Severity.error, null, 4, "invalid language in formal public identifier");
    public static readonly MessageType1 fpiIllegalDisplayVersion = new MessageType1(
        MessageType.Severity.error, null, 5, "illegal display version for this text class");
    public static readonly MessageType1 fpiExtraField = new MessageType1(
        MessageType.Severity.error, null, 6, "extra field in formal public identifier");

    // URN (Uniform Resource Name) error messages
    public static readonly MessageType1 urnMissingField = new MessageType1(
        MessageType.Severity.error, null, 7, "missing field in URN");
    public static readonly MessageType1 urnMissingPrefix = new MessageType1(
        MessageType.Severity.error, null, 8, "URN must start with 'urn:'");
    public static readonly MessageType1 urnInvalidNid = new MessageType1(
        MessageType.Severity.error, null, 9, "invalid Namespace Identifier in URN");
    public static readonly MessageType1 urnInvalidNss = new MessageType1(
        MessageType.Severity.error, null, 10, "invalid Namespace Specific String in URN");
}
