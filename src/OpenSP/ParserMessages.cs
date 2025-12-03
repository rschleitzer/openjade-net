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
    public static readonly MessageType1 noSuchAttributeToken = new MessageType1(
        MessageType.Severity.error, null, 122, "%1 is not a member of a group specified for any attribute");
    public static readonly MessageType0 attributeNameShorttag = new MessageType0(
        MessageType.Severity.error, null, 123, "the name and VI delimiter can be omitted from an attribute specification only if SHORTTAG YES is specified");
    public static readonly MessageType1 attributeTokenNotUnique = new MessageType1(
        MessageType.Severity.error, null, 124, "token %1 can be value for multiple attributes so attribute name required");
    public static readonly MessageType0 missingAttributeName = new MessageType0(
        MessageType.Severity.warning, null, 125, "attribute name missing");
    public static readonly MessageType0 attributeSpecEntityEnd = new MessageType0(
        MessageType.Severity.error, null, 129, "entity end not allowed in attribute specification list except in attribute value literal");
    public static readonly MessageType0 attributeSpecNameTokenExpected = new MessageType0(
        MessageType.Severity.error, null, 130, "an attribute specification must start with a name or name token");
    public static readonly MessageType0 attributeSpecLiteral = new MessageType0(
        MessageType.Severity.error, null, 131, "an attribute value literal can occur in an attribute specification list only after a VI delimiter");
    public static readonly MessageType0 unclosedStartTagShorttag = new MessageType0(
        MessageType.Severity.error, null, 132, "unclosed start-tag requires SHORTTAG YES");
    public static readonly MessageType0 netEnablingStartTagShorttag = new MessageType0(
        MessageType.Severity.error, null, 138, "NET-enabling start-tag requires SHORTTAG YES");
    public static readonly MessageType0 nestcWithoutNet = new MessageType0(
        MessageType.Severity.error, null, 139, "NET-enabling start-tag not immediately followed by null end-tag");
    public static readonly MessageType1 noSuchAttribute = new MessageType1(
        MessageType.Severity.error, null, 140, "there is no attribute %1");
    public static readonly MessageType0 attributeValueExpected = new MessageType0(
        MessageType.Severity.error, null, 141, "an attribute value specification must start with a literal or a name character");
    public static readonly MessageType0 attributeValueShorttag = new MessageType0(
        MessageType.Severity.error, null, 142, "an attribute value specification must be an attribute value literal unless SHORTTAG YES is specified");
    public static readonly MessageType1 attributeSpecCharacter = new MessageType1(
        MessageType.Severity.error, null, 143, "character %1 not allowed in attribute specification list");
    public static readonly MessageType2 attsplen = new MessageType2(
        MessageType.Severity.quantityError, null, 144, "normalized length of attribute specification list must not exceed ATTSPLEN (%1); length was %2");
    public static readonly MessageType0 startTagMissingName = new MessageType0(
        MessageType.Severity.error, null, 145, "generic identifier specification missing after document type specification in start-tag");
    public static readonly MessageType0 endTagMissingName = new MessageType0(
        MessageType.Severity.error, null, 146, "generic identifier specification missing after document type specification in end-tag");
    public static readonly MessageType0 startTagGroupNet = new MessageType0(
        MessageType.Severity.error, null, 147, "a NET-enabling start-tag cannot include a document type specification");

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
    public static readonly MessageType0L commentEntityEnd = new MessageType0L(
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

    // parseDecl messages
    public static readonly MessageType0 emptyCommentDecl = new MessageType0(
        MessageType.Severity.warning, null, 386, "empty comment declaration");
    public static readonly MessageType0 commentDeclS = new MessageType0(
        MessageType.Severity.warning, null, 387, "S separator in comment declaration");
    public static readonly MessageType0 commentDeclMultiple = new MessageType0(
        MessageType.Severity.warning, null, 388, "multiple comments in comment declaration");
    public static readonly MessageType0 declarationLevel = new MessageType0(
        MessageType.Severity.error, null, 248, "entity ended within declaration");
    public static readonly MessageType1L commentDeclarationCharacter = new MessageType1L(
        MessageType.Severity.error, null, 137, "invalid comment declaration: found character %1 outside comment but inside comment declaration");
    public static readonly MessageType1L commentDeclInvalidToken = new MessageType1L(
        MessageType.Severity.error, null, 53, "invalid comment declaration: found %1 outside comment but inside comment declaration");

    // parseInstance messages
    public static readonly MessageType0 instanceStartOmittag = new MessageType0(
        MessageType.Severity.error, null, 300, "start of instance requires element; OMITTAG NO prevents implicit document element");
    public static readonly MessageType0 specialParseEntityEnd = new MessageType0(
        MessageType.Severity.error, null, 301, "entity ended in special parse");
    public static readonly MessageType0 afterDocumentElementEntityEnd = new MessageType0(
        MessageType.Severity.warning, null, 302, "entity reference after document element");
    public static readonly MessageType0 contentAsyncEntityRef = new MessageType0(
        MessageType.Severity.error, null, 303, "entity referenced in content asynchronously");
    public static readonly MessageType0 characterReferenceAfterDocumentElement = new MessageType0(
        MessageType.Severity.warning, null, 304, "character reference after document element");
    public static readonly MessageType0 entityReferenceAfterDocumentElement = new MessageType0(
        MessageType.Severity.warning, null, 305, "entity reference after document element");
    public static readonly MessageType1 declarationAfterDocumentElement = new MessageType1(
        MessageType.Severity.warning, null, 306, "%1 declaration after document element");
    public static readonly MessageType1 instanceDeclaration = new MessageType1(
        MessageType.Severity.error, null, 307, "%1 declaration not allowed in document instance");
    public static readonly MessageType1 noSuchDeclarationType = new MessageType1(
        MessageType.Severity.error, null, 308, "unknown declaration type %1");
    public static readonly MessageType0 markedSectionAfterDocumentElement = new MessageType0(
        MessageType.Severity.warning, null, 309, "marked section after document element");
    public static readonly MessageType0 emptyStartTag = new MessageType0(
        MessageType.Severity.warning, null, 310, "empty start tag");
    public static readonly MessageType0 emptyStartTagBaseDtd = new MessageType0(
        MessageType.Severity.error, null, 311, "empty start tag not allowed except in base document type");
    public static readonly MessageType1 endTagCharacter = new MessageType1(
        MessageType.Severity.error, null, 312, "character '%1' not allowed in end tag");
    public static readonly MessageType0 endTagEntityEnd = new MessageType0(
        MessageType.Severity.error, null, 313, "entity ended within end tag");
    public static readonly MessageType0 unclosedEndTagShorttag = new MessageType0(
        MessageType.Severity.warning, null, 314, "unclosed end tag requires SHORTTAG YES");
    public static readonly MessageType1 endTagInvalidToken = new MessageType1(
        MessageType.Severity.error, null, 315, "invalid token %1 in end tag");
    public static readonly MessageType0 emptyEndTag = new MessageType0(
        MessageType.Severity.warning, null, 316, "empty end tag");
    public static readonly MessageType0 emptyEndTagBaseDtd = new MessageType0(
        MessageType.Severity.error, null, 317, "empty end tag not allowed except in base document type");
    public static readonly MessageType0 emptyEndTagNoOpenElements = new MessageType0(
        MessageType.Severity.error, null, 318, "empty end tag requires open element");
    public static readonly MessageType1 elementNotFinished = new MessageType1(
        MessageType.Severity.warning, null, 319, "element '%1' not finished");
    public static readonly MessageType1 elementEndTagNotFinished = new MessageType1(
        MessageType.Severity.warning, null, 320, "element '%1' not finished but end tag found");
    public static readonly MessageType1 elementNotFinishedDocumentEnd = new MessageType1(
        MessageType.Severity.warning, null, 321, "element '%1' not finished at end of document");
    public static readonly MessageType0 noDocumentElement = new MessageType0(
        MessageType.Severity.error, null, 322, "document element not found");
    public static readonly MessageType1 unclosedMarkedSection = new MessageType1(
        MessageType.Severity.error, null, 323, "marked section not closed");
    public static readonly MessageType1 missingId = new MessageType1(
        MessageType.Severity.error, null, 324, "referenced ID '%1' not defined");
    public static readonly MessageType1 taglen = new MessageType1(
        MessageType.Severity.error, null, 325, "tag length exceeds TAGLEN (%1)");
    public static readonly MessageType1 elementNotOpen = new MessageType1(
        MessageType.Severity.error, null, 326, "end tag for element '%1' which is not open");
    public static readonly MessageType2 omitEndTagOmittag = new MessageType2(
        MessageType.Severity.warning, null, 327, "end tag for '%1' omitted, but OMITTAG NO");
    public static readonly MessageType2 omitEndTagDeclare = new MessageType2(
        MessageType.Severity.warning, null, 328, "end tag for '%1' omitted, but end tag minimization is - (MINUS)");
    public static readonly MessageType0 pcdataNotAllowed = new MessageType0(
        MessageType.Severity.error, null, 329, "#PCDATA not allowed here");
    public static readonly MessageType1 startTagEmptyElement = new MessageType1(
        MessageType.Severity.warning, null, 330, "start tag implied for element '%1' but it cannot have content");
    public static readonly MessageType3 requiredElementExcluded = new MessageType3(
        MessageType.Severity.error, null, 331, "required element '%2' at position %1 in content model for '%3' is excluded");
    public static readonly MessageType1 omitStartTagDeclaredContent = new MessageType1(
        MessageType.Severity.error, null, 332, "start tag omitted for element '%1' with non-mixed content");
    public static readonly MessageType1 undefinedElement = new MessageType1(
        MessageType.Severity.error, null, 333, "element '%1' undefined");
    public static readonly MessageType1 omitStartTagDeclare = new MessageType1(
        MessageType.Severity.warning, null, 334, "start tag for '%1' omitted, but start tag minimization is - (MINUS)");
    public static readonly MessageType1 taglvlOpenElements = new MessageType1(
        MessageType.Severity.error, null, 335, "number of open elements exceeds TAGLVL (%1)");
    public static readonly MessageType0 immediateRecursion = new MessageType0(
        MessageType.Severity.warning, null, 336, "immediately recursive element");
    public static readonly MessageType2 missingElementInferred = new MessageType2(
        MessageType.Severity.warning, null, 337, "element '%1' requires '%2'; inserting '%2'");
    public static readonly MessageType2 missingElementMultiple = new MessageType2(
        MessageType.Severity.error, null, 338, "element '%1' not allowed here; missing one of: %2");
    public static readonly MessageType1 elementNotAllowed = new MessageType1(
        MessageType.Severity.error, null, 339, "element '%1' not allowed here");
    public static readonly MessageType3 invalidExclusion = new MessageType3(
        MessageType.Severity.warning, null, 340, "element '%2' at position %1 in '%3' excluded but required");
    public static readonly MessageType1 noCurrentRank = new MessageType1(
        MessageType.Severity.error, null, 341, "no current rank for rank stem '%1'");

    // parseDecl messages
    public static readonly MessageType0 notSgml = new MessageType0(
        MessageType.Severity.error, null, 400, "this does not look like an SGML document");
    public static readonly MessageType1 prologCharacter = new MessageType1(
        MessageType.Severity.error, null, 401, "invalid character '%1' in prolog");
    public static readonly MessageType0 documentEndProlog = new MessageType0(
        MessageType.Severity.error, null, 402, "document end in prolog");
    public static readonly MessageType1 prologDeclaration = new MessageType1(
        MessageType.Severity.error, null, 403, "%1 declaration not allowed in prolog");
    public static readonly MessageType1 declSubsetCharacter = new MessageType1(
        MessageType.Severity.error, null, 404, "invalid character '%1' in declaration subset");
    public static readonly MessageType0 documentEndDtdSubset = new MessageType0(
        MessageType.Severity.error, null, 405, "document end in DTD subset");
    public static readonly MessageType0 documentEndLpdSubset = new MessageType0(
        MessageType.Severity.error, null, 406, "document end in LPD subset");
    public static readonly MessageType1 dtdSubsetDeclaration = new MessageType1(
        MessageType.Severity.error, null, 407, "%1 declaration not allowed in DTD subset");
    public static readonly MessageType1 lpdSubsetDeclaration = new MessageType1(
        MessageType.Severity.error, null, 408, "%1 declaration not allowed in LPD subset");
    public static readonly MessageType0 dataMarkedSectionDeclSubset = new MessageType0(
        MessageType.Severity.error, null, 409, "data in marked section in declaration subset");

    // parseDoctypeDecl messages
    public static readonly MessageType0 multipleDtds = new MessageType0(
        MessageType.Severity.error, null, 410, "DTDs other than base allowed only if CONCUR YES or EXPLICIT YES");
    public static readonly MessageType0 dtdAfterLpd = new MessageType0(
        MessageType.Severity.error, null, 411, "DTD not allowed after an LPD");
    public static readonly MessageType0 impliedDoctypeConcurLink = new MessageType0(
        MessageType.Severity.error, null, 412, "can't use #IMPLICIT doctype unless CONCUR NO and LINK EXPLICIT NO");
    public static readonly MessageType0 sorryImpliedDoctype = new MessageType0(
        MessageType.Severity.error, null, 413, "Sorry, #IMPLIED doctypes not implemented");
    public static readonly MessageType1 duplicateDtd = new MessageType1(
        MessageType.Severity.error, null, 414, "document type %1 already defined");
    public static readonly MessageType0 noDtdSubset = new MessageType0(
        MessageType.Severity.error, null, 415, "no internal or external document type declaration subset; will parse without validation");
    public static readonly MessageType0 omittedProlog = new MessageType0(
        MessageType.Severity.error, null, 416, "prolog can't be omitted unless CONCUR NO and LINK EXPLICIT NO and either IMPLYDEF ELEMENT YES or IMPLYDEF DOCTYPE YES");
    public static readonly MessageType0 noDtd = new MessageType0(
        MessageType.Severity.error, null, 417, "no document type declaration; will parse without validation");
    public static readonly MessageType1 implyingDtd = new MessageType1(
        MessageType.Severity.error, null, 418, "no document type declaration; implying %1");

    // checkDtd messages
    public static readonly MessageType0 documentElementUndefined = new MessageType0(
        MessageType.Severity.error, null, 420, "DTD did not contain element declaration for document type name");
    public static readonly MessageType1 dtdUndefinedElement = new MessageType1(
        MessageType.Severity.warning, null, 421, "generic identifier %1 used in DTD but not defined");
    public static readonly MessageType2 undefinedShortrefMapDtd = new MessageType2(
        MessageType.Severity.error, null, 422, "short reference map %1 for element %2 not defined in DTD");
    public static readonly MessageType2 mapEntityUndefined = new MessageType2(
        MessageType.Severity.error, null, 423, "entity %1 undefined in short reference map %2");
    public static readonly MessageType2 mapDefaultEntity = new MessageType2(
        MessageType.Severity.warning, null, 424, "entity %1 in short reference map %2 uses default entity");
    public static readonly MessageType1 unusedMap = new MessageType1(
        MessageType.Severity.warning, null, 425, "short reference map %1 not used");
    public static readonly MessageType1 unusedParamEntity = new MessageType1(
        MessageType.Severity.warning, null, 426, "parameter entity %1 defined but not used");
    public static readonly MessageType1 cannotGenerateSystemIdEntity = new MessageType1(
        MessageType.Severity.error, null, 427, "cannot generate system identifier for entity %1");

    // parseMarkedSectionDeclStart messages
    public static readonly MessageType1 markedSectionLevel = new MessageType1(
        MessageType.Severity.error, null, 430, "number of open marked sections must not exceed TAGLVL (%1)");
    public static readonly MessageType0 internalSubsetMarkedSection = new MessageType0(
        MessageType.Severity.warning, null, 431, "marked section in internal DTD subset");
    public static readonly MessageType0 missingStatusKeyword = new MessageType0(
        MessageType.Severity.warning, null, 432, "no status keyword");
    public static readonly MessageType0 rcdataMarkedSection = new MessageType0(
        MessageType.Severity.warning, null, 433, "RCDATA marked section");
    public static readonly MessageType0 instanceIgnoreMarkedSection = new MessageType0(
        MessageType.Severity.warning, null, 434, "ignored marked section in the instance");
    public static readonly MessageType0 instanceIncludeMarkedSection = new MessageType0(
        MessageType.Severity.warning, null, 435, "included marked section in the instance");
    public static readonly MessageType0 tempMarkedSection = new MessageType0(
        MessageType.Severity.warning, null, 436, "TEMP marked section");
    public static readonly MessageType0 multipleStatusKeyword = new MessageType0(
        MessageType.Severity.warning, null, 437, "multiple status keywords");
    public static readonly MessageType0 parameterEntityNotEnded = new MessageType0(
        MessageType.Severity.error, null, 438, "a parameter entity referenced in a parameter separator must end in the same declaration");
    public static readonly MessageType0 instanceStatusKeywordSpecS = new MessageType0(
        MessageType.Severity.warning, null, 439, "S separator in status keyword specification in document instance");

    // handleMarkedSectionEnd message
    public static readonly MessageType0 markedSectionEnd = new MessageType0(
        MessageType.Severity.error, null, 440, "marked section end not in marked section declaration");

    // duplicate DTD/LPD
    public static readonly MessageType1 duplicateDtdLpd = new MessageType1(
        MessageType.Severity.error, null, 442, "both document type and link type %1");

    // checkDtd - notation related messages
    public static readonly MessageType2 parameterEntityNotationUndefined = new MessageType2(
        MessageType.Severity.error, null, 443, "notation %1 for parameter entity %2 undefined");
    public static readonly MessageType1 dsEntityNotationUndefined = new MessageType1(
        MessageType.Severity.error, null, 444, "notation %1 for document type external subset entity undefined");
    public static readonly MessageType2 entityNotationUndefined = new MessageType2(
        MessageType.Severity.error, null, 445, "notation %1 for entity %2 undefined");
    public static readonly MessageType1 attlistNotationUndefined = new MessageType1(
        MessageType.Severity.error, null, 446, "notation %1 for which an ATTLIST was defined is not defined");

    // parseParam.cxx messages
    public static readonly MessageType2 markupDeclarationCharacter = new MessageType2(
        MessageType.Severity.error, null, 8, "character %1 invalid: only %2 and parameter separators allowed");
    public static readonly MessageType0 psComment = new MessageType0(
        MessageType.Severity.warning, null, 382, "comment in parameter separator");
    public static readonly MessageType2 groupCharacter = new MessageType2(
        MessageType.Severity.error, null, 6, "character %1 invalid: only %2 and token separators allowed");
    public static readonly MessageType0 groupLevel = new MessageType0(
        MessageType.Severity.error, null, 5, "an entity end in a token separator must terminate an entity referenced in the same group");
    public static readonly MessageType0 groupEntityEnd = new MessageType0(
        MessageType.Severity.error, null, 10, "an entity end is not allowed in a token separator that does not follow a token");
    public static readonly MessageType0 groupEntityReference = new MessageType0(
        MessageType.Severity.error, null, 12, "a parameter entity reference can only occur in a group where a token could occur");
    public static readonly MessageType1 grplvl = new MessageType1(
        MessageType.Severity.error, null, 248, "content model nesting level exceeds GRPLVL (%1)");
    public static readonly MessageType1 groupCount = new MessageType1(
        MessageType.Severity.error, null, 14, "the number of tokens in a group must not exceed GRPCNT (%1)");
    public static readonly MessageType1 duplicateGroupToken = new MessageType1(
        MessageType.Severity.error, null, 13, "token %1 has already occurred in this group");
    // peroGrpoProlog already defined above
    public static readonly MessageType0 peroGrpoStartTag = new MessageType0(
        MessageType.Severity.error, null, 447, "a name group is not allowed in a parameter entity reference in a start tag");
    public static readonly MessageType0 peroGrpoEndTag = new MessageType0(
        MessageType.Severity.warning, null, 448, "name group in a parameter entity reference in an end tag (SGML forbids them in start tags)");
    public static readonly MessageType0 instanceParamEntityRef = new MessageType0(
        MessageType.Severity.warning, null, 394, "parameter entity reference in document instance");
    public static readonly MessageType0 internalSubsetPsParamEntityRef = new MessageType0(
        MessageType.Severity.warning, null, 415, "reference to parameter entity in parameter separator in internal subset");
    public static readonly MessageType0 internalSubsetTsParamEntityRef = new MessageType0(
        MessageType.Severity.warning, null, 416, "reference to parameter entity in token separator in internal subset");
    public static readonly MessageType0 groupParameterEntityNotEnded = new MessageType0(
        MessageType.Severity.error, null, 195, "a parameter entity referenced in a token separator must end in the same group");
    public static readonly MessageType2 paramInvalidToken = new MessageType2(
        MessageType.Severity.error, null, 39, "%1 invalid: only %2 and parameter separators are allowed");
    public static readonly MessageType2 groupTokenInvalidToken = new MessageType2(
        MessageType.Severity.error, null, 40, "%1 invalid: only %2 and token separators are allowed");
    public static readonly MessageType2 connectorInvalidToken = new MessageType2(
        MessageType.Severity.error, null, 41, "%1 invalid: only %2 and token separators are allowed");
    public static readonly MessageType0 datatagNotImplemented = new MessageType0(
        MessageType.Severity.error, null, 322, "DATATAG feature not implemented");
    public static readonly MessageType0 datatagBaseDtd = new MessageType0(
        MessageType.Severity.error, null, 429, "datatag group can only be specified in base document type");
    public static readonly MessageType1 invalidToken = new MessageType1(
        MessageType.Severity.error, null, 11, "%1 is not a valid token here");
    public static readonly MessageType0 sorryAllImplicit = new MessageType0(
        MessageType.Severity.error, null, 451, "Sorry, #ALL and #IMPLICIT content tokens not implemented");
    public static readonly MessageType0 mixedConnectors = new MessageType0(
        MessageType.Severity.warning, null, 30, "only one type of connector should be used in a single group");
    public static readonly MessageType0 nameGroupNotOr = new MessageType0(
        MessageType.Severity.warning, null, 402, "name group or name token group used connector other than OR");
    public static readonly MessageType0 andGroup = new MessageType0(
        MessageType.Severity.warning, null, 384, "AND group");
    public static readonly MessageType0 pcdataNotFirstInGroup = new MessageType0(
        MessageType.Severity.warning, null, 398, "#PCDATA not first in model group");
    public static readonly MessageType0 pcdataInSeqGroup = new MessageType0(
        MessageType.Severity.warning, null, 399, "#PCDATA in SEQ group");
    public static readonly MessageType0 pcdataInNestedModelGroup = new MessageType0(
        MessageType.Severity.warning, null, 400, "#PCDATA in nested model group");
    public static readonly MessageType0 pcdataGroupMemberModelGroup = new MessageType0(
        MessageType.Severity.warning, null, 422, "member of model group containing #PCDATA is a model group");
    public static readonly MessageType0 pcdataGroupMemberOccurrenceIndicator = new MessageType0(
        MessageType.Severity.warning, null, 421, "member of model group containing #PCDATA has occurrence indicator");
    public static readonly MessageType0 pcdataGroupNotRep = new MessageType0(
        MessageType.Severity.warning, null, 401, "#PCDATA in model group that does not have REP occurrence indicator");
    public static readonly MessageType1 minimumLiteralLength = new MessageType1(
        MessageType.Severity.error, null, 33, "length of interpreted minimum literal must not exceed reference LITLEN (%1)");
    public static readonly MessageType1 systemIdentifierLength = new MessageType1(
        MessageType.Severity.error, null, 35, "length of system identifier must not exceed LITLEN (%1)");
    public static readonly MessageType1 parameterLiteralLength = new MessageType1(
        MessageType.Severity.error, null, 36, "length of interpreted parameter literal must not exceed LITLEN (%1)");
    public static readonly MessageType1 dataTagPatternLiteralLength = new MessageType1(
        MessageType.Severity.error, null, 37, "length of interpreted parameter literal in data tag pattern must not exceed DTEMPLEN (%1)");
    public static readonly MessageType0 rniNameStart = new MessageType0(
        MessageType.Severity.error, null, 26, "RNI delimiter must be followed by name start character");
    public static readonly MessageType1 noSuchReservedName = new MessageType1(
        MessageType.Severity.error, null, 31, "%1 is not a reserved name");
    public static readonly MessageType1 invalidReservedName = new MessageType1(
        MessageType.Severity.error, null, 32, "%1 is not allowed as a reserved name here");
    public static readonly MessageType1 attributeValueLength = new MessageType1(
        MessageType.Severity.error, null, 3, "length of attribute value must not exceed LITLEN less NORMSEP (%1)");
    public static readonly MessageType1 tokenizedAttributeValueLength = new MessageType1(
        MessageType.Severity.quantityError, null, 126, "length of tokenized attribute value must not exceed LITLEN less NORMSEP (%1)");
    public static readonly MessageType1 attributeValueLengthNeg = new MessageType1(
        MessageType.Severity.quantityError, null, 127, "length of attribute value must not exceed LITLEN less NORMSEP (-%1)");
    public static readonly MessageType1 tokenizedAttributeValueLengthNeg = new MessageType1(
        MessageType.Severity.quantityError, null, 128, "length of tokenized attribute value must not exceed LITLEN less NORMSEP (-%1)");
    // nameTokenLength already defined above
    public static readonly MessageType1 noSuchSourceElement = new MessageType1(
        MessageType.Severity.error, null, 476, "no such source element '%1'");
    public static readonly MessageType0 wwwRequired = new MessageType0(
        MessageType.Severity.error, null, 355, "Web SGML adaptations were used but minimum literal was not \"ISO 8879:1986 (WWW)\"");
    public static readonly MessageType0 missingSystemId = new MessageType0(
        MessageType.Severity.warning, null, 381, "no system id specified");

    // parseElementDecl messages
    public static readonly MessageType0 elementGroupDecl = new MessageType0(
        MessageType.Severity.warning, null, 387, "element declaration for group of element types");
    public static readonly MessageType0 rank = new MessageType0(
        MessageType.Severity.warning, null, 380, "rank stem");
    public static readonly MessageType1 genericIdentifierLength = new MessageType1(
        MessageType.Severity.error, null, 246, "length of rank stem plus length of rank suffix must not exceed NAMELEN (%1)");
    public static readonly MessageType1 rankStemGenericIdentifier = new MessageType1(
        MessageType.Severity.error, null, 58, "%1 used both a rank stem and generic identifier");
    public static readonly MessageType0 psRequired = new MessageType0(
        MessageType.Severity.error, null, 7, "a parameter separator is required after a number that is followed by a name start character");
    public static readonly MessageType0 minimizationParam = new MessageType0(
        MessageType.Severity.warning, null, 396, "element type minimization parameter");
    public static readonly MessageType0 missingTagMinimization = new MessageType0(
        MessageType.Severity.error, null, 50, "omitted tag minimization parameter can be omitted only if OMITTAG NO is specified");
    public static readonly MessageType0 cdataContent = new MessageType0(
        MessageType.Severity.warning, null, 360, "CDATA declared content");
    public static readonly MessageType0 rcdataContent = new MessageType0(
        MessageType.Severity.warning, null, 361, "RCDATA declared content");
    public static readonly MessageType0 emptyOmitEndTag = new MessageType0(
        MessageType.Severity.warning, null, 230, "end-tag minimization should be O for element with declared content of EMPTY");
    public static readonly MessageType1 grpgtcnt = new MessageType1(
        MessageType.Severity.error, null, 249, "grand total of content tokens exceeds GRPGTCNT (%1)");
    public static readonly MessageType0 pcdataUnreachable = new MessageType0(
        MessageType.Severity.warning, null, 329, "content model is mixed but does not allow #PCDATA everywhere");
    public static readonly MessageType1 duplicateElementDefinition = new MessageType1(
        MessageType.Severity.error, null, 60, "element type %1 already defined");
    public static readonly MessageType3 ambiguousModelInitial = new MessageType3(
        MessageType.Severity.warning, null, 133, "content model is ambiguous: when no tokens have been matched, both occurrence %2 and occurrence %3 of %1 are possible");
    public static readonly MessageType5 ambiguousModel = new MessageType5(
        MessageType.Severity.warning, null, 134, "content model is ambiguous: when the current token is occurrence %2 of %1, both occurrence %4 and occurrence %5 of %3 are possible");
    public static readonly MessageType5 ambiguousModelSingleAnd = new MessageType5(
        MessageType.Severity.warning, null, 135, "content model is ambiguous: when current token is occurrence %2 of %1 and an and-group has been matched, both occurrence %4 and occurrence %5 of %3 are possible");
    public static readonly MessageType6 ambiguousModelMultipleAnd = new MessageType6(
        MessageType.Severity.warning, null, 136, "content model is ambiguous: when current token is occurrence %2 of %1 and %3 and-groups have been matched, both occurrence %5 and occurrence %6 of %4 are possible");
    public static readonly MessageType0 exclusion = new MessageType0(
        MessageType.Severity.warning, null, 383, "exclusion");
    public static readonly MessageType0 inclusion = new MessageType0(
        MessageType.Severity.warning, null, 385, "inclusion");
    public static readonly MessageType1 excludeIncludeSame = new MessageType1(
        MessageType.Severity.error, null, 449, "element %1 appears in both inclusion and exclusion");
    public static readonly MessageType1 notationEmpty = new MessageType1(
        MessageType.Severity.error, null, 450, "notation attribute for element %1 with EMPTY declared content");
    public static readonly MessageType1 conrefEmpty = new MessageType1(
        MessageType.Severity.error, null, 451, "CONREF attribute for element %1 with EMPTY declared content");

    // parseNotationDecl messages
    public static readonly MessageType1 duplicateNotationDeclaration = new MessageType1(
        MessageType.Severity.error, null, 61, "notation %1 already defined");
    public static readonly MessageType1 notationMustNotBeDeclared = new MessageType1(
        MessageType.Severity.error, null, 452, "notation %1 was specified with #IMPLICIT and so must not be declared");
    public static readonly MessageType0 notationIdentifierTextClass = new MessageType0(
        MessageType.Severity.error, null, 453, "public identifier text class for notation must be NOTATION");

    // parseEntityDecl messages
    public static readonly MessageType0 implydefEntityDefault = new MessageType0(
        MessageType.Severity.error, null, 425, "declaration of default entity conflicts with IMPLYDEF ENTITY YES");
    public static readonly MessageType0 defaultEntityDecl = new MessageType0(
        MessageType.Severity.warning, null, 414, "declaration of default entity");
    public static readonly MessageType0 internalCdataEntity = new MessageType0(
        MessageType.Severity.warning, null, 375, "internal CDATA entity");
    public static readonly MessageType0 internalSdataEntity = new MessageType0(
        MessageType.Severity.warning, null, 376, "internal SDATA entity");
    public static readonly MessageType0 piEntity = new MessageType0(
        MessageType.Severity.warning, null, 373, "processing instruction entity");
    public static readonly MessageType0 bracketEntity = new MessageType0(
        MessageType.Severity.warning, null, 374, "bracketed text entity");
    public static readonly MessageType1 bracketedLitlen = new MessageType1(
        MessageType.Severity.quantityError, null, 245, "length of interpreted parameter literal in bracketed text plus the length of the bracketing delimiters must not exceed LITLEN (%1)");
    public static readonly MessageType1 internalParameterDataEntity = new MessageType1(
        MessageType.Severity.error, null, 86, "internal parameter entity %1 cannot be CDATA or SDATA");
    public static readonly MessageType1 subdocEntity = new MessageType1(
        MessageType.Severity.error, null, 185, "entity %1 declared SUBDOC, but SUBDOC NO specified in SGML declaration");
    public static readonly MessageType0 externalCdataEntity = new MessageType0(
        MessageType.Severity.warning, null, 377, "external CDATA entity");
    public static readonly MessageType0 externalSdataEntity = new MessageType0(
        MessageType.Severity.warning, null, 378, "external SDATA entity");
    public static readonly MessageType1 notationNoAttributes = new MessageType1(
        MessageType.Severity.error, null, 241, "no attributes defined for notation %1");
    public static readonly MessageType0 emptyDataAttributeSpec = new MessageType0(
        MessageType.Severity.error, null, 121, "data attribute specification must be omitted if attribute specification list is empty");
    public static readonly MessageType1 externalParameterDataSubdocEntity = new MessageType1(
        MessageType.Severity.error, null, 90, "external parameter entity %1 cannot be CDATA, SDATA, NDATA or SUBDOC");
    public static readonly MessageType1 duplicateEntityDeclaration = new MessageType1(
        MessageType.Severity.warning, null, 91, "duplicate declaration of entity %1");
    public static readonly MessageType1 duplicateParameterEntityDeclaration = new MessageType1(
        MessageType.Severity.warning, null, 92, "duplicate declaration of parameter entity %1");
    public static readonly MessageType1 defaultedEntityDefined = new MessageType1(
        MessageType.Severity.warning, null, 334, "earlier reference to entity %1 used default entity");

    // parseAttlistDecl messages
    public static readonly MessageType1 duplicateAttributeDef = new MessageType1(
        MessageType.Severity.warning, null, 120, "duplicate definition of attribute %1");
    public static readonly MessageType1 multipleIdAttributes = new MessageType1(
        MessageType.Severity.error, null, 238, "the attribute definition list already declared attribute %1 as the ID attribute");
    public static readonly MessageType1 multipleNotationAttributes = new MessageType1(
        MessageType.Severity.error, null, 239, "the attribute definition list already declared attribute %1 as the NOTATION attribute");
    public static readonly MessageType1 duplicateAttributeToken = new MessageType1(
        MessageType.Severity.error, null, 240, "token %1 occurs more than once in attribute definition list");
    public static readonly MessageType0 simpleLinkFixedAttribute = new MessageType0(
        MessageType.Severity.error, null, 290, "only fixed attributes can be defined in simple LPD");
    public static readonly MessageType2 attcnt = new MessageType2(
        MessageType.Severity.quantityError, null, 236, "number of attribute names and name tokens (%1) exceeds ATTCNT (%2)");
    public static readonly MessageType1 duplicateAttlistElement = new MessageType1(
        MessageType.Severity.error, null, 67, "duplicate attribute definition list for element %1");
    public static readonly MessageType1 simpleLinkAttlistElement = new MessageType1(
        MessageType.Severity.error, null, 285, "attributes can only be defined for base document element (not %1) in simple link declaration subset");
    public static readonly MessageType1 duplicateAttlistNotation = new MessageType1(
        MessageType.Severity.error, null, 66, "duplicate attribute definition list for notation %1");
    public static readonly MessageType0 missingAfdrDecl = new MessageType0(
        MessageType.Severity.error, null, 348, "AFDR declaration required before use of AFDR extensions");
    public static readonly MessageType1 specifiedAttributeRedeclared = new MessageType1(
        MessageType.Severity.error, null, 445, "attribute %1 can't be redeclared");
    public static readonly MessageType0 dataAttributes = new MessageType0(
        MessageType.Severity.warning, null, 379, "attribute definition list declaration for notation");
    public static readonly MessageType0 attlistGroupDecl = new MessageType0(
        MessageType.Severity.warning, null, 388, "attribute definition list declaration for group of element types");

    // parseDeclaredValue messages
    public static readonly MessageType0 nameDeclaredValue = new MessageType0(
        MessageType.Severity.warning, null, 365, "NAME or NAMES declared value");
    public static readonly MessageType0 numberDeclaredValue = new MessageType0(
        MessageType.Severity.warning, null, 364, "NUMBER or NUMBERS declared value");
    public static readonly MessageType0 nutokenDeclaredValue = new MessageType0(
        MessageType.Severity.warning, null, 366, "NUTOKEN or NUTOKENS declared value");
    public static readonly MessageType0 dataAttributeDeclaredValue = new MessageType0(
        MessageType.Severity.error, null, 234, "declared value of data attribute cannot be ENTITY, ENTITIES, ID, IDREF, IDREFS or NOTATION");
    public static readonly MessageType0 linkAttributeDeclaredValue = new MessageType0(
        MessageType.Severity.error, null, 289, "declared value of link attribute cannot be ID, IDREF, IDREFS or NOTATION");

    // parseDefaultValue messages
    public static readonly MessageType0 idDeclaredValue = new MessageType0(
        MessageType.Severity.error, null, 237, "if the declared value is ID the default value must be IMPLIED or REQUIRED");
    public static readonly MessageType0 attributeValueNotLiteral = new MessageType0(
        MessageType.Severity.warning, null, 385, "attribute value not a literal");
    public static readonly MessageType0 dataAttributeDefaultValue = new MessageType0(
        MessageType.Severity.error, null, 235, "default value of data attribute cannot be CONREF or CURRENT");
    public static readonly MessageType0 linkAttributeDefaultValue = new MessageType0(
        MessageType.Severity.error, null, 288, "default value of link attribute cannot be CURRENT or CONREF");
    public static readonly MessageType0 currentAttribute = new MessageType0(
        MessageType.Severity.warning, null, 368, "CURRENT attribute");
    public static readonly MessageType0 notationConref = new MessageType0(
        MessageType.Severity.warning, null, 450, "if the declared value is NOTATION a default value of CONREF is useless");
    public static readonly MessageType0 conrefAttribute = new MessageType0(
        MessageType.Severity.warning, null, 367, "CONREF attribute");

    // parseShortrefDecl messages
    public static readonly MessageType0 shortrefOnlyInBaseDtd = new MessageType0(
        MessageType.Severity.error, null, 286, "a short reference mapping declaration is allowed only in the base DTD");
    public static readonly MessageType1L duplicateShortrefDeclaration = new MessageType1L(
        MessageType.Severity.error, null, 264, "short reference map %1 already defined", "first defined here");
    public static readonly MessageType1 unknownShortrefDelim = new MessageType1(
        MessageType.Severity.error, null, 105, "%1 is not a short reference delimiter");
    public static readonly MessageType1 delimDuplicateMap = new MessageType1(
        MessageType.Severity.error, null, 106, "short reference delimiter %1 already mapped in this declaration");

    // parseUsemapDecl messages
    public static readonly MessageType0 usemapOnlyInBaseDtd = new MessageType0(
        MessageType.Severity.error, null, 287, "a short reference use declaration is allowed only in the base DTD");
    public static readonly MessageType1 undefinedShortrefMapInstance = new MessageType1(
        MessageType.Severity.error, null, 100, "short reference map %1 not defined");
    public static readonly MessageType0 usemapAssociatedElementTypeDtd = new MessageType0(
        MessageType.Severity.error, null, 101, "short reference map in DTD must specify associated element type");
    public static readonly MessageType0 usemapAssociatedElementTypeInstance = new MessageType0(
        MessageType.Severity.error, null, 102, "short reference map in document instance cannot specify associated element type");

    // parseUselinkDecl messages
    public static readonly MessageType1 uselinkBadLinkType = new MessageType1(
        MessageType.Severity.error, null, 306, "no link type %1");
    public static readonly MessageType1 uselinkSimpleLpd = new MessageType1(
        MessageType.Severity.error, null, 305, "link set use declaration for simple link process");
    public static readonly MessageType2 uselinkBadLinkSet = new MessageType2(
        MessageType.Severity.error, null, 304, "link type %1 does not have a link set %2");

    // parseAfdrDecl messages
    public static readonly MessageType1 afdrVersion = new MessageType1(
        MessageType.Severity.error, null, 347, "minimum data of AFDR declaration must be \"ISO/IEC 10744:1997\" not %1");

    // parseLinktypeDeclStart/End messages
    public static readonly MessageType0 lpdBeforeBaseDtd = new MessageType0(
        MessageType.Severity.error, null, 298, "LPD not allowed before first DTD");
    // duplicateDtdLpd already defined above
    public static readonly MessageType1 duplicateLpd = new MessageType1(
        MessageType.Severity.error, null, 308, "link type %1 already defined");
    public static readonly MessageType1 noSuchDtd = new MessageType1(
        MessageType.Severity.error, null, 274, "no DTD %1 declared");
    public static readonly MessageType0 simpleLinkFeature = new MessageType0(
        MessageType.Severity.error, null, 295, "simple link requires SIMPLE YES");
    public static readonly MessageType0 implicitLinkFeature = new MessageType0(
        MessageType.Severity.error, null, 296, "implicit link requires IMPLICIT YES");
    public static readonly MessageType0 simpleLinkResultNotImplied = new MessageType0(
        MessageType.Severity.error, null, 294, "result document type in simple link specification must be implied");
    public static readonly MessageType0 explicitLinkFeature = new MessageType0(
        MessageType.Severity.error, null, 297, "explicit link requires EXPLICIT YES");
    public static readonly MessageType1 simpleLinkCount = new MessageType1(
        MessageType.Severity.error, null, 312, "number of active simple link processes exceeds quantity specified for SIMPLE parameter in SGML declaration (%1)");
    public static readonly MessageType0 oneImplicitLink = new MessageType0(
        MessageType.Severity.error, null, 315, "only one implicit link process can be active");
    public static readonly MessageType1 explicitNoRequiresSourceTypeBase = new MessageType1(
        MessageType.Severity.error, null, 318, "source document type name for link type %1 must be base document type since EXPLICIT NO");
    public static readonly MessageType1 explicit1RequiresSourceTypeBase = new MessageType1(
        MessageType.Severity.error, null, 314, "source document type name for link type %1 must be base document type since EXPLICIT YES 1");
    public static readonly MessageType0 duplicateExplicitChain = new MessageType0(
        MessageType.Severity.error, null, 313, "only one chain of explicit link processes can be active");
    public static readonly MessageType1 sorryLink = new MessageType1(
        MessageType.Severity.warning, null, 316, "sorry, link type %1 not activated: only one implicit or explicit link process can be active (with base document type as source document type)");
    public static readonly MessageType1 noLpdSubset = new MessageType1(
        MessageType.Severity.warning, null, 275, "LPD %1 has neither internal nor external subset");
    public static readonly MessageType1 noInitialLinkSet = new MessageType1(
        MessageType.Severity.error, null, 292, "no initial link set defined for LPD %1");
    public static readonly MessageType1 undefinedLinkSet = new MessageType1(
        MessageType.Severity.error, null, 310, "link set %1 used in LPD but not defined");

    // parseLinkDecl/parseIdlinkDecl/parseLinkSet messages
    public static readonly MessageType0 linkDeclSimple = new MessageType0(
        MessageType.Severity.error, null, 284, "link set declaration not allowed in simple link declaration subset");
    public static readonly MessageType0 idlinkDeclSimple = new MessageType0(
        MessageType.Severity.error, null, 283, "ID link set declaration not allowed in simple link declaration subset");
    public static readonly MessageType0 duplicateIdLinkSet = new MessageType0(
        MessageType.Severity.error, null, 291, "only one ID link set declaration allowed in an LPD subset");
    public static readonly MessageType1 duplicateLinkSet = new MessageType1(
        MessageType.Severity.error, null, 277, "link set %1 already defined");

    // SGML Declaration (parseSd) messages
    public static readonly MessageType1 ambiguousDocCharacter = new MessageType1(
        MessageType.Severity.warning, null, 500, "character numbers %1 in the document character set have been assigned the same meaning, but this is the meaning of a significant character");
    public static readonly MessageType1 missingSyntaxChar = new MessageType1(
        MessageType.Severity.error, null, 501, "the character with number %1 in ISO 646 is significant but has no representation in the syntax reference character set");
    public static readonly MessageType1 translateSyntaxCharDoc = new MessageType1(
        MessageType.Severity.error, null, 502, "there is no unique character in the document character set corresponding to character number %1 in the syntax reference character set");
    public static readonly MessageType1 translateSyntaxCharInternal = new MessageType1(
        MessageType.Severity.error, null, 503, "there is no unique character in the internal character set corresponding to character number %1 in the syntax reference character set");
    public static readonly MessageType1 oneFunction = new MessageType1(
        MessageType.Severity.error, null, 504, "character number %1 assigned to more than one function");
    public static readonly MessageType1 switchNotMarkup = new MessageType1(
        MessageType.Severity.error, null, 505, "character number %1 in the syntax reference character set was specified as a character to be switched but is not a markup character");
    public static readonly MessageType1 switchNotInCharset = new MessageType1(
        MessageType.Severity.error, null, 506, "character number %1 was specified as a character to be switched but is not in the syntax reference character set");
    public static readonly MessageType1 missingSignificant646 = new MessageType1(
        MessageType.Severity.error, null, 507, "characters with the following numbers in ISO 646 are significant in the concrete syntax but are not in the document character set: %1");
    public static readonly MessageType1 generalDelimAllFunction = new MessageType1(
        MessageType.Severity.error, null, 508, "general delimiter %1 consists solely of function characters");
    public static readonly MessageType1 nmcharLetter = new MessageType1(
        MessageType.Severity.error, null, 509, "letters assigned to LCNMCHAR, UCNMCHAR, LCNMSTRT or UCNMSTRT: %1");
    public static readonly MessageType1 nmcharDigit = new MessageType1(
        MessageType.Severity.error, null, 510, "digits assigned to LCNMCHAR, UCNMCHAR, LCNMSTRT or UCNMSTRT: %1");
    public static readonly MessageType1 nmcharRe = new MessageType1(
        MessageType.Severity.error, null, 511, "character number %1 cannot be assigned to LCNMCHAR, UCNMCHAR, LCNMSTRT or UCNMSTRT because it is RE");
    public static readonly MessageType1 nmcharRs = new MessageType1(
        MessageType.Severity.error, null, 512, "character number %1 cannot be assigned to LCNMCHAR, UCNMCHAR, LCNMSTRT or UCNMSTRT because it is RS");
    public static readonly MessageType1 nmcharSpace = new MessageType1(
        MessageType.Severity.error, null, 513, "character number %1 cannot be assigned to LCNMCHAR, UCNMCHAR, LCNMSTRT or UCNMSTRT because it is SPACE");
    public static readonly MessageType1 nmcharSepchar = new MessageType1(
        MessageType.Severity.error, null, 514, "separator characters assigned to LCNMCHAR, UCNMCHAR, LCNMSTRT or UCNMSTRT: %1");
    public static readonly MessageType1 switchLetterDigit = new MessageType1(
        MessageType.Severity.error, null, 515, "character number %1 cannot be switched because it is a Digit, LC Letter or UC Letter");
    public static readonly MessageType1 nameReferenceReservedName = new MessageType1(
        MessageType.Severity.error, null, 516, "%1 cannot be the replacement for a reference reserved name because it is another reference reserved name");
    public static readonly MessageType1 duplicateDelimShortref = new MessageType1(
        MessageType.Severity.error, null, 517, "short reference delimiter %1 already specified");
    public static readonly MessageType1 sdMissingCharacters = new MessageType1(
        MessageType.Severity.error, null, 518, "SGML declaration cannot be parsed because the character set does not contain characters having the following numbers in ISO 646: %1");
    public static readonly MessageType0 explicitSgmlDecl = new MessageType0(
        MessageType.Severity.warning, null, 519, "SGML declaration was not implied");
    public static readonly MessageType0 badDefaultSgmlDecl = new MessageType0(
        MessageType.Severity.error, null, 520, "invalid default SGML declaration");

    // SGML declaration parsing messages
    public static readonly MessageType1 sdEntityEnd = new MessageType1(
        MessageType.Severity.error, null, 530, "unexpected entity end in SGML declaration; expected %1");
    public static readonly MessageType2 sdInvalidNameToken = new MessageType2(
        MessageType.Severity.error, null, 531, "\"%1\" is not a valid SGML declaration token; expected %2");
    public static readonly MessageType1 numberTooBig = new MessageType1(
        MessageType.Severity.error, null, 532, "number \"%1\" is too large");
    public static readonly MessageType2 sdParamInvalidToken = new MessageType2(
        MessageType.Severity.error, null, 533, "%1 invalid in SGML declaration; expected %2");
    public static readonly MessageType0 cannotGenerateSystemIdSgml = new MessageType0(
        MessageType.Severity.error, null, 534, "cannot generate system identifier for SGML declaration");
    public static readonly MessageType1 standardVersion = new MessageType1(
        MessageType.Severity.warning, null, 535, "unknown SGML declaration version \"%1\"");
    public static readonly MessageType0 sgmlDeclRefRequiresWww = new MessageType0(
        MessageType.Severity.error, null, 536, "reference to external SGML declaration requires WWW variant");
    public static readonly MessageType1 invalidSgmlChar = new MessageType1(
        MessageType.Severity.error, null, 537, "characters with the following numbers in the document character set are significant in the concrete syntax but are not in SGML character set: %1");
    public static readonly MessageType1 sdTextClass = new MessageType1(
        MessageType.Severity.warning, null, 538, "public identifier \"%1\" has wrong text class for SGML declaration");
    public static readonly MessageType1 unknownCapacitySet = new MessageType1(
        MessageType.Severity.error, null, 539, "capacity set %1 is unknown");
    public static readonly MessageType1 duplicateCapacity = new MessageType1(
        MessageType.Severity.warning, null, 540, "capacity %1 already specified");
    public static readonly MessageType1 capacityExceedsTotalcap = new MessageType1(
        MessageType.Severity.error, null, 541, "value of capacity %1 exceeds value of TOTALCAP");
    public static readonly MessageType1 unknownPublicSyntax = new MessageType1(
        MessageType.Severity.error, null, 542, "syntax %1 is unknown");
    public static readonly MessageType1 capacityTextClass = new MessageType1(
        MessageType.Severity.error, null, 543, "public text class of formal public identifier of capacity set must be CAPACITY");
    public static readonly MessageType1 syntaxTextClass = new MessageType1(
        MessageType.Severity.error, null, 544, "public text class of formal public identifier of concrete syntax must be SYNTAX");
    public static readonly MessageType0 immednetRequiresEmptynrm = new MessageType0(
        MessageType.Severity.error, null, 545, "NETENABL IMMEDNET requires EMPTYNRM YES");
    public static readonly MessageType1 basesetTextClass = new MessageType1(
        MessageType.Severity.error, null, 546, "public text class of formal public identifier of base character set must be CHARSET");
    public static readonly MessageType1 unknownBaseset = new MessageType1(
        MessageType.Severity.error, null, 547, "base character set %1 is unknown");
    public static readonly MessageType0 zeroNumberOfCharacters = new MessageType0(
        MessageType.Severity.warning, null, 548, "number of characters declared is zero");
    public static readonly MessageType1 documentCharMax = new MessageType1(
        MessageType.Severity.error, null, 549, "character numbers declared exceed document character set maximum of %1");
    public static readonly MessageType1 basesetCharsMissing = new MessageType1(
        MessageType.Severity.warning, null, 550, "base character set characters missing from document character set: %1");
    public static readonly MessageType0 tooManyCharsMinimumLiteral = new MessageType0(
        MessageType.Severity.error, null, 551, "more than 256 character numbers cannot be assigned with minimum literal");
    public static readonly MessageType1 duplicateCharNumbers = new MessageType1(
        MessageType.Severity.error, null, 552, "character numbers declared more than once: %1");
    public static readonly MessageType1 codeSetHoles = new MessageType1(
        MessageType.Severity.warning, null, 553, "character numbers not declared: %1");
    public static readonly MessageType0 scopeInstanceSyntaxCharset = new MessageType0(
        MessageType.Severity.error, null, 554, "syntax reference character set must be ISO 646 when scope is INSTANCE");
    public static readonly MessageType1 missingMinimumChars = new MessageType1(
        MessageType.Severity.error, null, 555, "missing minimum data characters in document character set: %1");
    public static readonly MessageType1 scopeInstanceQuantity = new MessageType1(
        MessageType.Severity.error, null, 556, "quantity %1 in declared syntax is less than in prolog syntax");
    public static readonly MessageType1 entityNameSyntax = new MessageType1(
        MessageType.Severity.error, null, 557, "entity name \"%1\" is not a valid name in the declared syntax");

    // Additional SGML declaration parsing messages
    public static readonly MessageType1 namingBeforeLcnmstrt = new MessageType1(
        MessageType.Severity.error, null, 558, "parameter before LCNMSTRT must be NAMING not %1");
    public static readonly MessageType0 msocharRequiresMsichar = new MessageType0(
        MessageType.Severity.error, null, 559, "when there is an MSOCHAR there must also be an MSICHAR");
    public static readonly MessageType1 duplicateFunctionName = new MessageType1(
        MessageType.Severity.error, null, 560, "%1 is already a function name");
    public static readonly MessageType0 nmstrtLength = new MessageType0(
        MessageType.Severity.error, null, 561, "UCNMSTRT must have the same number of characters as LCNMSTRT");
    public static readonly MessageType0 nmcharLength = new MessageType0(
        MessageType.Severity.error, null, 562, "UCNMCHAR must have the same number of characters as LCNMCHAR");
    public static readonly MessageType0 enrRequired = new MessageType0(
        MessageType.Severity.error, null, 563, "ENR extensions were used but minimum literal was not \"ISO 8879:1986 (ENR)\" or \"ISO 8879:1986 (WWW)\"");
    public static readonly MessageType0 sdInvalidRange = new MessageType0(
        MessageType.Severity.error, null, 564, "number of first character in range must not exceed number of second character in range");
    public static readonly MessageType1 nmcharNmstrt = new MessageType1(
        MessageType.Severity.error, null, 565, "character numbers assigned to both LCNMCHAR or UCNMCHAR and LCNMSTRT or UCNMSTRT: %1");
    public static readonly MessageType1 duplicateDelimGeneral = new MessageType1(
        MessageType.Severity.error, null, 566, "general delimiter role %1 already defined");
    public static readonly MessageType0 sdEmptyDelimiter = new MessageType0(
        MessageType.Severity.error, null, 567, "delimiter cannot be an empty string");
    public static readonly MessageType1 duplicateDelimShortrefSet = new MessageType1(
        MessageType.Severity.error, null, 568, "single character short references were already specified for character numbers: %1");
    public static readonly MessageType0 sdRangeNotSingleChar = new MessageType0(
        MessageType.Severity.error, null, 569, "start or end of range must specify a single character");
    public static readonly MessageType1 ambiguousReservedName = new MessageType1(
        MessageType.Severity.error, null, 570, "%1 cannot be the replacement for a reference reserved name because it is the replacement of another reference reserved name");
    public static readonly MessageType1 duplicateReservedName = new MessageType1(
        MessageType.Severity.error, null, 571, "replacement for reserved name %1 already specified");
    public static readonly MessageType1 reservedNameSyntax = new MessageType1(
        MessageType.Severity.warning, null, 572, "%1 is not a valid name in the declared concrete syntax");
    public static readonly MessageType1 multipleBSequence = new MessageType1(
        MessageType.Severity.error, null, 573, "%1 is not a valid short reference delimiter because it has more than one B sequence");
    public static readonly MessageType1 blankAdjacentBSequence = new MessageType1(
        MessageType.Severity.error, null, 574, "%1 is not a valid short reference delimiter because it is adjacent to a character that can occur in a blank sequence");
    public static readonly MessageType2 delimiterLength = new MessageType2(
        MessageType.Severity.error, null, 575, "length of delimiter %1 exceeds NAMELEN (%2)");
    public static readonly MessageType2 reservedNameLength = new MessageType2(
        MessageType.Severity.warning, null, 576, "length of reserved name %1 exceeds NAMELEN (%2)");
}
