// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Warnings
{
    public PackedBoolean warnSgmlDecl;
    public PackedBoolean warnDuplicateEntity;
    public PackedBoolean warnShould;
    public PackedBoolean warnUndefinedElement;
    public PackedBoolean warnDefaultEntityReference;
    public PackedBoolean warnMixedContent;
    public PackedBoolean warnEmptyTag;
    public PackedBoolean warnUnusedMap;
    public PackedBoolean warnUnusedParam;
    public PackedBoolean warnNotationSystemId;
    public PackedBoolean warnInclusion;
    public PackedBoolean warnExclusion;
    public PackedBoolean warnRcdataContent;
    public PackedBoolean warnCdataContent;
    public PackedBoolean warnPsComment;
    public PackedBoolean warnElementGroupDecl;
    public PackedBoolean warnAttlistGroupDecl;
    public PackedBoolean warnPiEntity;
    public PackedBoolean warnInternalSdataEntity;
    public PackedBoolean warnInternalCdataEntity;
    public PackedBoolean warnExternalSdataEntity;
    public PackedBoolean warnExternalCdataEntity;
    public PackedBoolean warnBracketEntity;
    public PackedBoolean warnDataAttributes;
    public PackedBoolean warnMissingSystemId;
    public PackedBoolean warnConref;
    public PackedBoolean warnCurrent;
    public PackedBoolean warnNutokenDeclaredValue;
    public PackedBoolean warnNumberDeclaredValue;
    public PackedBoolean warnNameDeclaredValue;
    public PackedBoolean warnNamedCharRef;
    public PackedBoolean warnRefc;
    public PackedBoolean warnTempMarkedSection;
    public PackedBoolean warnRcdataMarkedSection;
    public PackedBoolean warnInstanceIncludeMarkedSection;
    public PackedBoolean warnInstanceIgnoreMarkedSection;
    public PackedBoolean warnAndGroup;
    public PackedBoolean warnRank;
    public PackedBoolean warnEmptyCommentDecl;
    public PackedBoolean warnAttributeValueNotLiteral;
    public PackedBoolean warnMissingAttributeName;
    public PackedBoolean warnCommentDeclS;
    public PackedBoolean warnCommentDeclMultiple;
    public PackedBoolean warnMissingStatusKeyword;
    public PackedBoolean warnMultipleStatusKeyword;
    public PackedBoolean warnInstanceParamEntityRef;
    public PackedBoolean warnMinimizationParam;
    public PackedBoolean warnMixedContentRepOrGroup;
    public PackedBoolean warnNameGroupNotOr;
    public PackedBoolean warnPiMissingName;
    public PackedBoolean warnInstanceStatusKeywordSpecS;
    public PackedBoolean warnExternalDataEntityRef;
    public PackedBoolean warnAttributeValueExternalEntityRef;
    public PackedBoolean warnDataDelim;
    public PackedBoolean warnExplicitSgmlDecl;
    public PackedBoolean warnInternalSubsetMarkedSection;
    public PackedBoolean warnDefaultEntityDecl;
    public PackedBoolean warnNonSgmlCharRef;
    public PackedBoolean warnInternalSubsetPsParamEntityRef;
    public PackedBoolean warnInternalSubsetTsParamEntityRef;
    public PackedBoolean warnInternalSubsetLiteralParamEntityRef;
    public PackedBoolean warnImmediateRecursion;

    // Warnings();
    public Warnings()
    {
        // All fields default to false in C#
        warnSgmlDecl = false;
        warnDuplicateEntity = false;
        warnShould = false;
        warnUndefinedElement = false;
        warnDefaultEntityReference = false;
        warnMixedContent = false;
        warnEmptyTag = false;
        warnUnusedMap = false;
        warnUnusedParam = false;
        warnNotationSystemId = false;
        warnInclusion = false;
        warnExclusion = false;
        warnRcdataContent = false;
        warnCdataContent = false;
        warnPsComment = false;
        warnElementGroupDecl = false;
        warnAttlistGroupDecl = false;
        warnPiEntity = false;
        warnInternalSdataEntity = false;
        warnInternalCdataEntity = false;
        warnExternalSdataEntity = false;
        warnExternalCdataEntity = false;
        warnBracketEntity = false;
        warnDataAttributes = false;
        warnMissingSystemId = false;
        warnConref = false;
        warnCurrent = false;
        warnNutokenDeclaredValue = false;
        warnNumberDeclaredValue = false;
        warnNameDeclaredValue = false;
        warnNamedCharRef = false;
        warnRefc = false;
        warnTempMarkedSection = false;
        warnRcdataMarkedSection = false;
        warnInstanceIncludeMarkedSection = false;
        warnInstanceIgnoreMarkedSection = false;
        warnAndGroup = false;
        warnRank = false;
        warnEmptyCommentDecl = false;
        warnAttributeValueNotLiteral = false;
        warnMissingAttributeName = false;
        warnCommentDeclS = false;
        warnCommentDeclMultiple = false;
        warnMissingStatusKeyword = false;
        warnMultipleStatusKeyword = false;
        warnInstanceParamEntityRef = false;
        warnMinimizationParam = false;
        warnMixedContentRepOrGroup = false;
        warnNameGroupNotOr = false;
        warnPiMissingName = false;
        warnInstanceStatusKeywordSpecS = false;
        warnExternalDataEntityRef = false;
        warnAttributeValueExternalEntityRef = false;
        warnDataDelim = false;
        warnExplicitSgmlDecl = false;
        warnInternalSubsetMarkedSection = false;
        warnDefaultEntityDecl = false;
        warnNonSgmlCharRef = false;
        warnInternalSubsetPsParamEntityRef = false;
        warnInternalSubsetTsParamEntityRef = false;
        warnInternalSubsetLiteralParamEntityRef = false;
        warnImmediateRecursion = false;
    }
}

public class ParserOptions : Warnings
{
    public enum Quantity
    {
        ATTCNT,
        ATTSPLEN,
        BSEQLEN,
        DTAGLEN,
        DTEMPLEN,
        ENTLVL,
        GRPCNT,
        GRPGTCNT,
        GRPLVL,
        LITLEN,
        NAMELEN,
        NORMSEP,
        PILEN,
        TAGLEN,
        TAGLVL
    }

    public const int nQuantity = (int)Quantity.TAGLVL + 1;
    public const short sgmlDeclTypeValid = -1;

    public EventsWanted eventsWanted = new EventsWanted();
    public PackedBoolean datatag;
    public PackedBoolean omittag;
    public PackedBoolean rank;
    public PackedBoolean shorttag;
    public PackedBoolean emptynrm;
    public Number linkSimple;
    public PackedBoolean linkImplicit;
    public Number linkExplicit;
    public Number concur;
    public Number subdoc;
    public PackedBoolean formal;
    public PackedBoolean shortref;
    public short typeValid;
    public Number[] quantity = new Number[nQuantity];
    public PackedBoolean errorIdref;
    public PackedBoolean errorSignificant;
    public PackedBoolean errorAfdr; // error if AFDR extensions are used
    // These override what was specified in the SGML declaration.
    public PackedBoolean noUnclosedTag;
    public PackedBoolean noNet;
    public PackedBoolean fullyDeclared;
    public PackedBoolean fullyTagged;
    public PackedBoolean amplyTagged;
    public PackedBoolean amplyTaggedAnyother;
    public PackedBoolean valid;
    public PackedBoolean entityRef;
    public PackedBoolean externalEntityRef;
    public PackedBoolean integral;
    public Vector<StringC> includes = new Vector<StringC>();

    // ParserOptions();
    public ParserOptions() : base()
    {
        datatag = false;
        omittag = true;
        rank = true;
        shorttag = true;
        emptynrm = false;
        linkSimple = 1000;
        linkImplicit = true;
        linkExplicit = 1;
        concur = 0;
        subdoc = 99999999;
        formal = true;
        typeValid = sgmlDeclTypeValid;
        shortref = true;
        // TODO: Original openjade doesn't report IDREF errors for these documents.
        // Disabled to match original behavior. The ID tracking implementation may have bugs.
        errorIdref = false;
        errorSignificant = true;
        errorAfdr = true;
        noUnclosedTag = false;
        noNet = false;
        fullyDeclared = false;
        fullyTagged = false;
        amplyTagged = false;
        amplyTaggedAnyother = false;
        valid = false;
        entityRef = false;
        externalEntityRef = false;
        integral = false;

        for (int i = 0; i < nQuantity; i++)
            quantity[i] = 99999999;
        quantity[(int)Quantity.BSEQLEN] = 960;
        quantity[(int)Quantity.NORMSEP] = 2;
        quantity[(int)Quantity.LITLEN] = 24000;
        quantity[(int)Quantity.PILEN] = 24000;
        quantity[(int)Quantity.DTEMPLEN] = 24000;
    }
}
