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

    // Attribute-related messages
    public static readonly MessageType2 normalizedAttributeValueLength = new MessageType2(
        MessageType.Severity.error, null, 100, "normalized length of attribute value exceeds %1 characters");
    public static readonly MessageType0 attributeValueSyntax = new MessageType0(
        MessageType.Severity.error, null, 101, "attribute value syntax error");
    public static readonly MessageType2 attributeValueChar = new MessageType2(
        MessageType.Severity.error, null, 102, "invalid character '%1' in attribute value for '%2'");
    public static readonly MessageType2 attributeValueNumberToken = new MessageType2(
        MessageType.Severity.error, null, 103, "number token attribute '%2' cannot start with '%1'");
    public static readonly MessageType2 attributeValueName = new MessageType2(
        MessageType.Severity.error, null, 104, "name attribute '%2' cannot start with '%1'");
    public static readonly MessageType1 nameTokenLength = new MessageType1(
        MessageType.Severity.error, null, 105, "name token exceeds %1 characters");
    public static readonly MessageType1 attributeValueMultiple = new MessageType1(
        MessageType.Severity.error, null, 106, "attribute '%1' cannot have multiple values");
    public static readonly MessageType3 attributeValueNotInGroup = new MessageType3(
        MessageType.Severity.error, null, 107, "value '%1' not allowed for attribute '%2'");
    public static readonly MessageType1 invalidNotationAttribute = new MessageType1(
        MessageType.Severity.error, null, 108, "unknown notation '%1' in attribute value");
    public static readonly MessageType1 invalidEntityAttribute = new MessageType1(
        MessageType.Severity.error, null, 109, "unknown entity '%1' in attribute value");
    public static readonly MessageType1 notDataOrSubdocEntity = new MessageType1(
        MessageType.Severity.error, null, 110, "entity '%1' is not a data or subdocument entity");
    public static readonly MessageType2L duplicateId = new MessageType2L(
        MessageType.Severity.error, null, 111, "ID '%1' already defined");
    public static readonly MessageType1 requiredAttributeMissing = new MessageType1(
        MessageType.Severity.error, null, 112, "required attribute '%1' not specified");
    public static readonly MessageType1 currentAttributeMissing = new MessageType1(
        MessageType.Severity.error, null, 113, "no current value for attribute '%1'");
    public static readonly MessageType1 attributeMissing = new MessageType1(
        MessageType.Severity.error, null, 114, "attribute '%1' not specified");
    public static readonly MessageType1 notFixedValue = new MessageType1(
        MessageType.Severity.error, null, 115, "value does not match FIXED value for attribute '%1'");
    public static readonly MessageType0 unquotedAttributeValue = new MessageType0(
        MessageType.Severity.warning, null, 116, "unquoted attribute value");
    public static readonly MessageType0 literalClosingDelimiter = new MessageType0(
        MessageType.Severity.error, null, 117, "literal closing delimiter missing");
    public static readonly MessageType1 duplicateAttributeSpec = new MessageType1(
        MessageType.Severity.error, null, 118, "attribute '%1' specified more than once");
    public static readonly MessageType1 idrefGrpcnt = new MessageType1(
        MessageType.Severity.error, null, 119, "number of IDREF values exceeds GRPCNT (%1)");
    public static readonly MessageType1 entityNameGrpcnt = new MessageType1(
        MessageType.Severity.error, null, 120, "number of entity name values exceeds GRPCNT (%1)");
    public static readonly MessageType0 conrefNotation = new MessageType0(
        MessageType.Severity.error, null, 121, "CONREF attribute not allowed with NOTATION");
}
