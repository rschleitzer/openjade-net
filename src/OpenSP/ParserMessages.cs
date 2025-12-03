// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// TODO: Port full ParserMessages from ParserMessages.h/.msg

namespace OpenSP;

public static class ParserMessages
{
    // Message fragments (starting at 1000 in original)
    public static readonly MessageFragment delimStart = new MessageFragment(null, 1000, "delimiter ");
    public static readonly MessageFragment digit = new MessageFragment(null, 1002, "digit");
    public static readonly MessageFragment nameStartCharacter = new MessageFragment(null, 1003, "name start character");
    public static readonly MessageFragment sepchar = new MessageFragment(null, 1004, "sepchar");
    public static readonly MessageFragment separator = new MessageFragment(null, 1005, "separator");
    public static readonly MessageFragment nameCharacter = new MessageFragment(null, 1006, "name character");
    public static readonly MessageFragment dataCharacter = new MessageFragment(null, 1007, "data character");
    public static readonly MessageFragment minimumDataCharacter = new MessageFragment(null, 1008, "minimum data character");
    public static readonly MessageFragment significantCharacter = new MessageFragment(null, 1009, "significant character");
    public static readonly MessageFragment recordEnd = new MessageFragment(null, 1010, "record end character");
    public static readonly MessageFragment recordStart = new MessageFragment(null, 1011, "record start character");
    public static readonly MessageFragment space = new MessageFragment(null, 1012, "space character");
    public static readonly MessageFragment listSep = new MessageFragment(null, 1013, ", ");
    public static readonly MessageFragment rangeSep = new MessageFragment(null, 1014, "-");
    public static readonly MessageFragment parameterLiteral = new MessageFragment(null, 1015, "parameter literal");
    public static readonly MessageFragment dataTagGroup = new MessageFragment(null, 1016, "data tag group");
    public static readonly MessageFragment modelGroup = new MessageFragment(null, 1017, "model group");
    public static readonly MessageFragment dataTagTemplateGroup = new MessageFragment(null, 1018, "data tag template group");
    public static readonly MessageFragment name = new MessageFragment(null, 1019, "name");
    public static readonly MessageFragment nameToken = new MessageFragment(null, 1020, "name token");
    public static readonly MessageFragment elementToken = new MessageFragment(null, 1021, "element token");
    public static readonly MessageFragment inclusions = new MessageFragment(null, 1022, "inclusions");
    public static readonly MessageFragment exclusions = new MessageFragment(null, 1023, "exclusions");
    public static readonly MessageFragment minimumLiteral = new MessageFragment(null, 1024, "minimum literal");
    public static readonly MessageFragment attributeValueLiteral = new MessageFragment(null, 1025, "attribute value literal");
    public static readonly MessageFragment systemIdentifier = new MessageFragment(null, 1026, "system identifier");
    public static readonly MessageFragment number = new MessageFragment(null, 1027, "number");
    public static readonly MessageFragment attributeValue = new MessageFragment(null, 1028, "attribute value");
    public static readonly MessageFragment capacityName = new MessageFragment(null, 1029, "name of capacity");
    public static readonly MessageFragment generalDelimiterRoleName = new MessageFragment(null, 1030, "name of general delimiter role");
    public static readonly MessageFragment referenceReservedName = new MessageFragment(null, 1031, "reference reserved name");
    public static readonly MessageFragment quantityName = new MessageFragment(null, 1032, "name of quantity");
    public static readonly MessageFragment entityEnd = new MessageFragment(null, 1033, "entity end");
    public static readonly MessageFragment shortrefDelim = new MessageFragment(null, 1034, "short reference delimiter");

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

    // Parser state related messages
    public static readonly MessageType0 pass2Ee = new MessageType0(
        MessageType.Severity.error, null, 200, "unexpected entity end during second pass");
    public static readonly MessageType0 activeDocLink = new MessageType0(
        MessageType.Severity.error, null, 201, "cannot have both active document type and link type");
    public static readonly MessageType0 sorryActiveDoctypes = new MessageType0(
        MessageType.Severity.error, null, 202, "sorry, only one active document type supported");
    public static readonly MessageType1 unstableLpdParameterEntity = new MessageType1(
        MessageType.Severity.error, null, 203, "definition of parameter entity '%1' changed in LPD");
    public static readonly MessageType1 unstableLpdGeneralEntity = new MessageType1(
        MessageType.Severity.error, null, 204, "definition of general entity '%1' changed in LPD");
    public static readonly MessageType0 linkActivateTooLate = new MessageType0(
        MessageType.Severity.error, null, 205, "too late to activate link type");
    public static readonly MessageType1 defaultEntityInAttribute = new MessageType1(
        MessageType.Severity.warning, null, 206, "reference to default entity '%1' in attribute value");
    public static readonly MessageType1 concurrentInstances = new MessageType1(
        MessageType.Severity.error, null, 207, "number of concurrent document instances exceeds %1");

    // Additional parser messages
    public static readonly MessageType1 subdocLevel = new MessageType1(
        MessageType.Severity.warning, null, 208, "subdocument nesting level exceeds %1");
    public static readonly MessageType0 subdocGiveUp = new MessageType0(
        MessageType.Severity.error, null, 209, "cannot continue with subdocument");
    public static readonly MessageType0 giveUp = new MessageType0(
        MessageType.Severity.error, null, 210, "cannot continue");

    // parseCommon messages
    public static readonly MessageType0 processingInstructionEntityEnd = new MessageType0(
        MessageType.Severity.error, null, 211, "entity ended within processing instruction");
    public static readonly MessageType1 processingInstructionLength = new MessageType1(
        MessageType.Severity.error, null, 212, "length of processing instruction exceeds %1");
    public static readonly MessageType0 processingInstructionClose = new MessageType0(
        MessageType.Severity.error, null, 213, "missing PIC delimiter");
    public static readonly MessageType0 piMissingName = new MessageType0(
        MessageType.Severity.warning, null, 214, "processing instruction does not start with name");
    public static readonly MessageType0 literalLevel = new MessageType0(
        MessageType.Severity.error, null, 215, "literal closed by entity end in different entity");
    public static readonly MessageType1 literalMinimumData = new MessageType1(
        MessageType.Severity.error, null, 216, "'%1' invalid in minimum data");
    public static readonly MessageType0 namedCharRef = new MessageType0(
        MessageType.Severity.warning, null, 217, "named character reference");
    public static readonly MessageType1 functionName = new MessageType1(
        MessageType.Severity.error, null, 218, "'%1' is not a function name");
    public static readonly MessageType0 refc = new MessageType0(
        MessageType.Severity.warning, null, 219, "missing REFC delimiter");
    public static readonly MessageType1 characterNumber = new MessageType1(
        MessageType.Severity.error, null, 220, "'%1' is not a valid character number");
    public static readonly MessageType1 hexNumberLength = new MessageType1(
        MessageType.Severity.error, null, 221, "hex number length exceeds %1");
    public static readonly MessageType0 nonSgmlCharRef = new MessageType0(
        MessageType.Severity.warning, null, 222, "numeric character reference to non-SGML character");
    public static readonly MessageType1 numericCharRefLiteralNonSgml = new MessageType1(
        MessageType.Severity.error, null, 223, "numeric character reference to %1 not allowed in literal");
    public static readonly MessageType0 dataTagPatternNonSgml = new MessageType0(
        MessageType.Severity.error, null, 224, "numeric character reference to non-SGML character in data tag pattern");
    public static readonly MessageType0 dataTagPatternFunction = new MessageType0(
        MessageType.Severity.error, null, 225, "numeric character reference to function character in data tag pattern");
    public static readonly MessageType0 eroGrpoStartTag = new MessageType0(
        MessageType.Severity.error, null, 226, "ERO GRPO not allowed in start tag");
    public static readonly MessageType0 eroGrpoProlog = new MessageType0(
        MessageType.Severity.error, null, 227, "ERO GRPO not allowed in prolog");
    public static readonly MessageType0 internalSubsetLiteralParamEntityRef = new MessageType0(
        MessageType.Severity.warning, null, 228, "parameter entity reference in internal subset literal");
    public static readonly MessageType0 peroGrpoProlog = new MessageType0(
        MessageType.Severity.error, null, 229, "PERO GRPO not allowed in prolog");
    public static readonly MessageType1 dataCharDelim = new MessageType1(
        MessageType.Severity.warning, null, 230, "'%1' is first character of delimiter in content");
    public static readonly MessageType0 entityReferenceMissingName = new MessageType0(
        MessageType.Severity.error, null, 231, "missing entity name after entity reference delimiter");
    public static readonly MessageType1 parameterEntityNameLength = new MessageType1(
        MessageType.Severity.error, null, 232, "parameter entity name length exceeds %1");
    public static readonly MessageType1 nameLength = new MessageType1(
        MessageType.Severity.error, null, 233, "name length exceeds %1");
    public static readonly MessageType1 entityUndefined = new MessageType1(
        MessageType.Severity.error, null, 234, "entity '%1' not defined");
    public static readonly MessageType1 parameterEntityUndefined = new MessageType1(
        MessageType.Severity.error, null, 235, "parameter entity '%1' not defined");
    public static readonly MessageType0 entityApplicableDtd = new MessageType0(
        MessageType.Severity.error, null, 236, "no applicable DTD");
    public static readonly MessageType1 defaultEntityReference = new MessageType1(
        MessageType.Severity.warning, null, 237, "reference to default entity '%1'");
    public static readonly MessageType1L commentEntityEnd = new MessageType1L(
        MessageType.Severity.error, null, 238, "comment closed by entity end started here");
    public static readonly MessageType1 sdCommentSignificant = new MessageType1(
        MessageType.Severity.error, null, 239, "'%1' invalid in SGML declaration comment");
    public static readonly MessageType1 numberLength = new MessageType1(
        MessageType.Severity.error, null, 240, "number length exceeds %1");
    public static readonly MessageType1 nonSgmlCharacter = new MessageType1(
        MessageType.Severity.error, null, 241, "non-SGML character %1");
    public static readonly MessageType2 numericCharRefUnknownDesc = new MessageType2(
        MessageType.Severity.error, null, 242, "character %1 has unknown description '%2'");
    public static readonly MessageType3 numericCharRefUnknownBase = new MessageType3(
        MessageType.Severity.error, null, 243, "character %1 unknown in base character set %3 at position %2");
    public static readonly MessageType1 numericCharRefBadInternal = new MessageType1(
        MessageType.Severity.error, null, 244, "character %1 cannot be represented in internal character set");
    public static readonly MessageType1 numericCharRefNoInternal = new MessageType1(
        MessageType.Severity.error, null, 245, "no internal representation for character %1");

    // parseMode messages
    public static readonly MessageType2 lexicalAmbiguity = new MessageType2(
        MessageType.Severity.warning, null, 246, "lexical ambiguity between '%1' and '%2'");
}
