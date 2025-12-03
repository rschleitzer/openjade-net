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
}
