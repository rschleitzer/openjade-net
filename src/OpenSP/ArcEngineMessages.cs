// Copyright (c) 1996 James Clark, 2000 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

public static class ArcEngineMessages
{
    // E1+arcGenerateSystemId++no system identifier could be generated for meta-DTD for architecture %1
    public static readonly MessageType1 arcGenerateSystemId = new MessageType1(
        MessageType.Severity.error, null, 3000, "no system identifier could be generated for meta-DTD for architecture %1");

    // E1+undefinedElement++element type %1 not defined in meta-DTD
    public static readonly MessageType1 undefinedElement = new MessageType1(
        MessageType.Severity.error, null, 3001, "element type %1 not defined in meta-DTD");

    // E1+elementExcluded++element %1 invalid in meta-DTD because excluded
    public static readonly MessageType1 elementExcluded = new MessageType1(
        MessageType.Severity.error, null, 3002, "element %1 invalid in meta-DTD because excluded");

    // E1+invalidElement++meta-DTD does not allow element %1 at this point
    public static readonly MessageType1 invalidElement = new MessageType1(
        MessageType.Severity.error, null, 3003, "meta-DTD does not allow element %1 at this point");

    // E1+documentElementNotArc++document element must be instance of %1 element type form
    public static readonly MessageType1 documentElementNotArc = new MessageType1(
        MessageType.Severity.error, null, 3004, "document element must be instance of %1 element type form");

    // E1+unfinishedElement++element %1 unfinished in meta-DTD
    public static readonly MessageType1 unfinishedElement = new MessageType1(
        MessageType.Severity.error, null, 3005, "element %1 unfinished in meta-DTD");

    // E0+renameMissingAttName++missing substitute name
    public static readonly MessageType0 renameMissingAttName = new MessageType0(
        MessageType.Severity.error, null, 3006, "missing substitute name");

    // E1+renameToInvalid++substitute for non-existent architecture attribute %1
    public static readonly MessageType1 renameToInvalid = new MessageType1(
        MessageType.Severity.error, null, 3007, "substitute for non-existent architecture attribute %1");

    // E1+renameToDuplicate++substitute name for %1 already defined
    public static readonly MessageType1 renameToDuplicate = new MessageType1(
        MessageType.Severity.error, null, 3008, "substitute name for %1 already defined");

    // E1+renameFromInvalid++substitute name %1 is not the name of an attribute
    public static readonly MessageType1 renameFromInvalid = new MessageType1(
        MessageType.Severity.error, null, 3009, "substitute name %1 is not the name of an attribute");

    // X1+missingId++reference in architecture to non-existent ID %1
    public static readonly MessageType1 missingId = new MessageType1(
        MessageType.Severity.idrefError, null, 3010, "reference in architecture to non-existent ID %1");

    // E0+invalidArcContent++architectural content specified with #ARCCONT not allowed by meta-DTD
    public static readonly MessageType0 invalidArcContent = new MessageType0(
        MessageType.Severity.error, null, 3011, "architectural content specified with #ARCCONT not allowed by meta-DTD");

    // E1+invalidSuppress+ISO/IEC 10744:1997 A3.5.3+invalid value %1 for ArcSupr attribute
    public static readonly MessageType1 invalidSuppress = new MessageType1(
        MessageType.Severity.error, null, 3012, "invalid value %1 for ArcSupr attribute");

    // E1+arcDtdNotDeclaredParameter+ISO/IEC 10744:1997 A3.4.2+no declaration for meta-DTD parameter entity %1
    public static readonly MessageType1 arcDtdNotDeclaredParameter = new MessageType1(
        MessageType.Severity.error, null, 3013, "no declaration for meta-DTD parameter entity %1");

    // E1+arcDtdNotDeclaredGeneral+ISO/IEC 10744:1997 A3.4.2+no declaration for meta-DTD general entity %1
    public static readonly MessageType1 arcDtdNotDeclaredGeneral = new MessageType1(
        MessageType.Severity.error, null, 3014, "no declaration for meta-DTD general entity %1");

    // E1+arcDtdNotExternal+ISO/IEC 10744:1997 A3.4.2+meta-DTD entity %1 must be external
    public static readonly MessageType1 arcDtdNotExternal = new MessageType1(
        MessageType.Severity.error, null, 3015, "meta-DTD entity %1 must be external");

    // W0+noArcDTDAtt+ISO/IEC 10744:1997 A3.4.2+no ArcDTD architecture support attribute specified
    public static readonly MessageType0 noArcDTDAtt = new MessageType0(
        MessageType.Severity.warning, null, 3016, "no ArcDTD architecture support attribute specified");

    // E1+noArcDataF+ISO/IEC 10744:1997 A3.4.2+ArcDataF notation %1 not defined in meta-DTD
    public static readonly MessageType1 noArcDataF = new MessageType1(
        MessageType.Severity.error, null, 3017, "ArcDataF notation %1 not defined in meta-DTD");

    // E1+idMismatch+ISO/IEC 10744:1997 A3.6.4+ID attribute %1 in meta-DTD not declared as ID in DTD
    public static readonly MessageType1 idMismatch = new MessageType1(
        MessageType.Severity.error, null, 3018, "ID attribute %1 in meta-DTD not declared as ID in DTD");

    // E1+invalidArcAuto+ISO/IEC 10744:1997 A3.4.2+invalid value %1 for ArcAuto architectural support attribute
    public static readonly MessageType1 invalidArcAuto = new MessageType1(
        MessageType.Severity.error, null, 3019, "invalid value %1 for ArcAuto architectural support attribute");

    // E1+noArcNotation+ISO/IEC 10744:1997 A3.4.1+no notation declaration for architecture %1
    public static readonly MessageType1 noArcNotation = new MessageType1(
        MessageType.Severity.error, null, 3020, "no notation declaration for architecture %1");

    // E0+invalidData++meta-DTD does not allow data at this point
    public static readonly MessageType0 invalidData = new MessageType0(
        MessageType.Severity.error, null, 3021, "meta-DTD does not allow data at this point");

    // E1+invalidIgnD+ISO/IEC 10744:1997 A3.5.4+invalid value %1 for ArcIgnD attribute
    public static readonly MessageType1 invalidIgnD = new MessageType1(
        MessageType.Severity.error, null, 3022, "invalid value %1 for ArcIgnD attribute");

    // E1+invalidQuantity++unrecognized quantity name %1
    public static readonly MessageType1 invalidQuantity = new MessageType1(
        MessageType.Severity.error, null, 3024, "unrecognized quantity name %1");

    // E1+missingQuantityValue++no value specified for quantity %1
    public static readonly MessageType1 missingQuantityValue = new MessageType1(
        MessageType.Severity.error, null, 3025, "no value specified for quantity %1");

    // E1+quantityValueTooLong++length of value %1 for quantity is too long
    public static readonly MessageType1 quantityValueTooLong = new MessageType1(
        MessageType.Severity.error, null, 3026, "length of value %1 for quantity is too long");

    // E1+invalidDigit++invalid digit %1
    public static readonly MessageType1 invalidDigit = new MessageType1(
        MessageType.Severity.error, null, 3027, "invalid digit %1");

    // E0+arcIndrNotSupported++only value of nArcIndr for ArcIndr attribute supported
    public static readonly MessageType0 arcIndrNotSupported = new MessageType0(
        MessageType.Severity.error, null, 3028, "only value of nArcIndr for ArcIndr attribute supported");

    // E0+arcContDuplicate+ISO/IEC 10744:1997 A3.5.2+#ARCCONT attribute already specified
    public static readonly MessageType0 arcContDuplicate = new MessageType0(
        MessageType.Severity.error, null, 3029, "#ARCCONT attribute already specified");

    // E1+arcContInvalid+ISO/IEC 10744:1997 A3.5.2+invalid value %1 for #ARCCONT
    public static readonly MessageType1 arcContInvalid = new MessageType1(
        MessageType.Severity.error, null, 3030, "invalid value %1 for #ARCCONT");

    // E1+renameFromDuplicate+ISO/IEC 10744:1997 A3.5.2+%1 already used as a substitute name
    public static readonly MessageType1 renameFromDuplicate = new MessageType1(
        MessageType.Severity.error, null, 3031, "%1 already used as a substitute name");

    // E0+contentDuplicate+ISO/IEC 10744:1997 A3.5.2+substitute name #CONTENT already specified
    public static readonly MessageType0 contentDuplicate = new MessageType0(
        MessageType.Severity.error, null, 3032, "substitute name #CONTENT already specified");

    // E0+is10744PiKeywordMissing++IS10744 PI keyword missing
    public static readonly MessageType0 is10744PiKeywordMissing = new MessageType0(
        MessageType.Severity.error, null, 3033, "IS10744 PI keyword missing");

    // E1+is10744PiKeywordInvalid++invalid IS10744 PI keyword %1
    public static readonly MessageType1 is10744PiKeywordInvalid = new MessageType1(
        MessageType.Severity.error, null, 3034, "invalid IS10744 PI keyword %1");

    // E1+duplicateArcDecl++architecture %1 already defined+the first definition
    public static readonly MessageType1L duplicateArcDecl = new MessageType1L(
        MessageType.Severity.error, null, 3035, "architecture %1 already defined", "the first definition");

    // W1+ignoringPiArcDecl++ignoring PI declaration of architecture %1+the ArcBase definition
    public static readonly MessageType1L ignoringPiArcDecl = new MessageType1L(
        MessageType.Severity.warning, null, 3036, "ignoring PI declaration of architecture %1", "the ArcBase definition");

    // W1+ignoringArcBaseArcDecl++ignoring ArcBase declaration of architecture %1+the PI definition
    public static readonly MessageType1L ignoringArcBaseArcDecl = new MessageType1L(
        MessageType.Severity.warning, null, 3037, "ignoring ArcBase declaration of architecture %1", "the PI definition");
}
