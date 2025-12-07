// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenJade.Grove;

using GroveChar = System.UInt32;

// Supports the following modules:
// baseabs prlgabs0 prlgabs1 instabs basesds0 instsds0 subdcabs

public enum AccessResult
{
    accessOK,           // success
    accessNull,         // value is null
    accessTimeout,      // timed out waiting for property
    accessNotInClass    // property is not defined for class
}

public struct ComponentName
{
    public enum Id
    {
        noId = -1,
        idAllPropertyNames,
        idAnd,
        idAny,
        idApplicationInfo,
        idAttributeAssignment,
        idAttributeDef,
        idAttributeDefs,
        idAttributes,
        idAttributeValueToken,
        idCdata,
        idChar,
        idChildrenPropertyName,
        idClassName,
        idConnector,
        idConref,
        idContent,
        idContentTokens,
        idContentType,
        idCurrent,
        idCurrentAttributeIndex,
        idCurrentGroup,
        idDataChar,
        idDataPropertyName,
        idDataSepPropertyName,
        idDeclValueType,
        idDefaulted,
        idDefaultedEntities,
        idDefaultEntity,
        idDefaultValue,
        idDefaultValueType,
        idDoctypesAndLinktypes,
        idDocumentElement,
        idDocumentType,
        idElement,
        idElements,
        idElementToken,
        idElementType,
        idElementTypes,
        idEmpty,
        idEntities,
        idEntity,
        idEntityName,
        idEntityType,
        idEpilog,
        idExclusions,
        idExternalData,
        idExternalId,
        idFixed,
        idGeneralEntities,
        idGeneratedSystemId,
        idGi,
        idGoverning,
        idGoverningDoctype,
        idGroveRoot,
        idId,
        idIdref,
        idIdrefs,
        idImplied,
        idIncluded,
        idInclusions,
        idModelGroup,
        idMustOmitEndTag,
        idName,
        idNames,
        idNdata,
        idNmtkgrp,
        idNmtoken,
        idNmtokens,
        idNotation,
        idNotationName,
        idNotations,
        idNumber,
        idNumbers,
        idNutoken,
        idNutokens,
        idOccurenceIndicator,
        idOmitEndTag,
        idOmitStartTag,
        idOpt,
        idOr,
        idOrigin,
        idOriginToSubnodeRelPropertyName,
        idParameterEntities,
        idParent,
        idPcdataToken,
        idPi,
        idPlus,
        idProlog,
        idPublicId,
        idRcdata,
        idReferent,
        idRep,
        idRequired,
        idSdata,
        idSeq,
        idSgmlConstants,
        idSgmlDocument,
        idSubdocument,
        idSubnodePropertyNames,
        idSystemData,
        idSystemId,
        idText,
        idToken,
        idTokens,
        idTokenSep,
        idTreeRoot,
        idValue
    }

    public const int nIds = (int)Id.idValue + 1;

    private static readonly string[] rcsNames = {
        "allpns",
        "and",
        "any",
        "appinfo",
        "attasgn",
        "attdef",
        "attdefs",
        "atts",
        "attvaltk",
        "cdata",
        "char",
        "childpn",
        "classnm",
        "connect",
        "conref",
        "content",
        "tokens",
        "contype",
        "current",
        "curattix",
        "curgrp",
        "datachar",
        "datapn",
        "dseppn",
        "dcltype",
        "dflted",
        "dfltents",
        "dfltent",
        "dfltval",
        "dflttype",
        "dtlts",
        "docelem",
        "doctype",
        "element",
        "elements",
        "elemtk",
        "elemtype",
        "elemtps",
        "empty",
        "entities",
        "entity",
        "entname",
        "enttype",
        "epilog",
        "excls",
        "extdata",
        "extid",
        "fixed",
        "genents",
        "gensysid",
        "gi",
        "govrning",
        "govdt",
        "grovroot",
        "id",
        "idref",
        "idrefs",
        "implied",
        "included",
        "incls",
        "modelgrp",
        "momitend",
        "name",
        "names",
        "ndata",
        "nmtkgrp",
        "nmtoken",
        "nmtokens",
        "notation",
        "notname",
        "nots",
        "number",
        "numbers",
        "nutoken",
        "nutokens",
        "occur",
        "omitend",
        "omitstrt",
        "opt",
        "or",
        "origin",
        "otsrelpn",
        "parments",
        "parent",
        "pcdatatk",
        "pi",
        "plus",
        "prolog",
        "pubid",
        "rcdata",
        "referent",
        "rep",
        "required",
        "sdata",
        "seq",
        "sgmlcsts",
        "sgmldoc",
        "subdoc",
        "subpns",
        "sysdata",
        "sysid",
        "text",
        "token",
        "tokens",
        "tokensep",
        "treeroot",
        "value"
    };

    private static readonly string[] sdqlNames = {
        "all-property-names",
        "and",
        "any",
        "application-info",
        "attribute-assignment",
        "attribute-def",
        "attribute-defs",
        "attributes",
        "attribute-value-token",
        "cdata",
        "char",
        "children-property-name",
        "class-name",
        "connector",
        "conref",
        "content",
        "content-tokens",
        "content-type",
        "current",
        "current-attribute-index",
        "current-group",
        "data-char",
        "data-property-name",
        "data-sep-property-name",
        "decl-value-type",
        "defaulted?",
        "defaulted-entities",
        "default-entity",
        "default-value",
        "default-value-type",
        "doctypes-and-linktypes",
        "document-element",
        "document-type",
        "element",
        "elements",
        "element-token",
        "element-type",
        "element-types",
        "empty",
        "entities",
        "entity",
        "entity-name",
        "entity-type",
        "epilog",
        "exclusions",
        "external-data",
        "external-id",
        "fixed",
        "general-entities",
        "generated-system-id",
        "gi",
        "governing?",
        "governing-doctype",
        "grove-root",
        "id",
        "idref",
        "idrefs",
        "implied?",
        "included?",
        "inclusions",
        "model-group",
        "must-omit-end-tag?",
        "name",
        "names",
        "ndata",
        "name-token-group",
        "nmtoken",
        "nmtokens",
        "notation",
        "notation-name",
        "notations",
        "number",
        "numbers",
        "nutoken",
        "nutokens",
        "occur-indicator",
        "omit-end-tag?",
        "omit-start-tag?",
        "opt",
        "or",
        "origin",
        "origin-to-subnode-rel-property-name",
        "parameter-entities",
        "parent",
        "pcdata-token",
        "pi",
        "plus",
        "prolog",
        "public-id",
        "rcdata",
        "referent",
        "rep",
        "required",
        "sdata",
        "seq",
        "sgml-constants",
        "sgml-document",
        "subdocument",
        "subnode-property-names",
        "system-data",
        "system-id",
        "text",
        "token",
        "tokens",
        "token-sep",
        "tree-root",
        "value"
    };

    public static string? rcsName(Id id)
    {
        int idx = (int)id;
        if (idx < 0 || idx >= rcsNames.Length)
            return null;
        return rcsNames[idx];
    }

    public static string? sdqlName(Id id)
    {
        int idx = (int)id;
        if (idx < 0 || idx >= sdqlNames.Length)
            return null;
        return sdqlNames[idx];
    }
}

public class ClassDef
{
    public ComponentName.Id className;
    public ComponentName.Id[] allPropertyNames;
    public ComponentName.Id[] subnodePropertyNames;
    public ComponentName.Id childrenPropertyName;
    public ComponentName.Id dataPropertyName;
    public ComponentName.Id dataSepPropertyName;

    public ClassDef(
        ComponentName.Id className,
        ComponentName.Id[] allPropertyNames,
        ComponentName.Id[] subnodePropertyNames,
        ComponentName.Id childrenPropertyName,
        ComponentName.Id dataPropertyName,
        ComponentName.Id dataSepPropertyName)
    {
        this.className = className;
        this.allPropertyNames = allPropertyNames;
        this.subnodePropertyNames = subnodePropertyNames;
        this.childrenPropertyName = childrenPropertyName;
        this.dataPropertyName = dataPropertyName;
        this.dataSepPropertyName = dataSepPropertyName;
    }

    private static readonly ComponentName.Id[] noProps = { ComponentName.Id.noId };

    private static readonly ComponentName.Id[] intrinsicProps = {
        ComponentName.Id.idClassName,
        ComponentName.Id.idGroveRoot,
        ComponentName.Id.idSubnodePropertyNames,
        ComponentName.Id.idAllPropertyNames,
        ComponentName.Id.idChildrenPropertyName,
        ComponentName.Id.idDataPropertyName,
        ComponentName.Id.idDataSepPropertyName,
        ComponentName.Id.idParent,
        ComponentName.Id.idTreeRoot,
        ComponentName.Id.idOrigin,
        ComponentName.Id.idOriginToSubnodeRelPropertyName
    };

    private static ComponentName.Id[] withIntrinsic(params ComponentName.Id[] props)
    {
        var result = new ComponentName.Id[intrinsicProps.Length + props.Length];
        intrinsicProps.CopyTo(result, 0);
        props.CopyTo(result, intrinsicProps.Length);
        return result;
    }

    public static readonly ClassDef sgmlDocument = new ClassDef(
        ComponentName.Id.idSgmlDocument,
        withIntrinsic(
            ComponentName.Id.idSgmlConstants,
            ComponentName.Id.idApplicationInfo,
            ComponentName.Id.idProlog,
            ComponentName.Id.idEpilog,
            ComponentName.Id.idGoverningDoctype,
            ComponentName.Id.idDoctypesAndLinktypes,
            ComponentName.Id.idDocumentElement,
            ComponentName.Id.idElements,
            ComponentName.Id.idEntities,
            ComponentName.Id.idDefaultedEntities,
            ComponentName.Id.noId),
        new[] {
            ComponentName.Id.idSgmlConstants,
            ComponentName.Id.idProlog,
            ComponentName.Id.idEpilog,
            ComponentName.Id.idDoctypesAndLinktypes,
            ComponentName.Id.idDocumentElement,
            ComponentName.Id.idDefaultedEntities,
            ComponentName.Id.noId },
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef sgmlConstants = new ClassDef(
        ComponentName.Id.idSgmlConstants,
        noProps,
        noProps,
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef dataChar = new ClassDef(
        ComponentName.Id.idDataChar,
        withIntrinsic(ComponentName.Id.idChar, ComponentName.Id.noId),
        noProps,
        ComponentName.Id.noId,
        ComponentName.Id.idChar,
        ComponentName.Id.noId);

    public static readonly ClassDef element = new ClassDef(
        ComponentName.Id.idElement,
        withIntrinsic(
            ComponentName.Id.idGi,
            ComponentName.Id.idId,
            ComponentName.Id.idAttributes,
            ComponentName.Id.idContent,
            ComponentName.Id.idIncluded,
            ComponentName.Id.idMustOmitEndTag,
            ComponentName.Id.idElementType,
            ComponentName.Id.noId),
        new[] {
            ComponentName.Id.idAttributes,
            ComponentName.Id.idContent,
            ComponentName.Id.noId },
        ComponentName.Id.idContent,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef attributeAssignment = new ClassDef(
        ComponentName.Id.idAttributeAssignment,
        withIntrinsic(
            ComponentName.Id.idAttributeDef,
            ComponentName.Id.idValue,
            ComponentName.Id.idName,
            ComponentName.Id.idImplied,
            ComponentName.Id.idTokenSep,
            ComponentName.Id.noId),
        new[] { ComponentName.Id.idValue, ComponentName.Id.noId },
        ComponentName.Id.idValue,
        ComponentName.Id.noId,
        ComponentName.Id.idTokenSep);

    public static readonly ClassDef attributeDef = new ClassDef(
        ComponentName.Id.idAttributeDef,
        withIntrinsic(
            ComponentName.Id.idCurrentAttributeIndex,
            ComponentName.Id.idCurrentGroup,
            ComponentName.Id.idDeclValueType,
            ComponentName.Id.idDefaultValueType,
            ComponentName.Id.idDefaultValue,
            ComponentName.Id.idName,
            ComponentName.Id.idTokens,
            ComponentName.Id.noId),
        new[] { ComponentName.Id.idDefaultValue, ComponentName.Id.noId },
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef attributeValueToken = new ClassDef(
        ComponentName.Id.idAttributeValueToken,
        withIntrinsic(
            ComponentName.Id.idToken,
            ComponentName.Id.idEntity,
            ComponentName.Id.idNotation,
            ComponentName.Id.idReferent,
            ComponentName.Id.noId),
        noProps,
        ComponentName.Id.noId,
        ComponentName.Id.idToken,
        ComponentName.Id.noId);

    public static readonly ClassDef pi = new ClassDef(
        ComponentName.Id.idPi,
        withIntrinsic(
            ComponentName.Id.idSystemData,
            ComponentName.Id.idEntityName,
            ComponentName.Id.idEntity,
            ComponentName.Id.noId),
        noProps,
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef sdata = new ClassDef(
        ComponentName.Id.idSdata,
        withIntrinsic(
            ComponentName.Id.idSystemData,
            ComponentName.Id.idChar,
            ComponentName.Id.idEntityName,
            ComponentName.Id.idEntity,
            ComponentName.Id.noId),
        noProps,
        ComponentName.Id.noId,
        ComponentName.Id.idChar,
        ComponentName.Id.noId);

    public static readonly ClassDef documentType = new ClassDef(
        ComponentName.Id.idDocumentType,
        withIntrinsic(
            ComponentName.Id.idName,
            ComponentName.Id.idElementTypes,
            ComponentName.Id.idGoverning,
            ComponentName.Id.idGeneralEntities,
            ComponentName.Id.idNotations,
            ComponentName.Id.idDefaultEntity,
            ComponentName.Id.idParameterEntities,
            ComponentName.Id.noId),
        new[] {
            ComponentName.Id.idGeneralEntities,
            ComponentName.Id.idNotations,
            ComponentName.Id.idElementTypes,
            ComponentName.Id.idDefaultEntity,
            ComponentName.Id.idParameterEntities,
            ComponentName.Id.noId },
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef entity = new ClassDef(
        ComponentName.Id.idEntity,
        withIntrinsic(
            ComponentName.Id.idName,
            ComponentName.Id.idEntityType,
            ComponentName.Id.idText,
            ComponentName.Id.idExternalId,
            ComponentName.Id.idAttributes,
            ComponentName.Id.idNotationName,
            ComponentName.Id.idNotation,
            ComponentName.Id.idDefaulted,
            ComponentName.Id.noId),
        new[] {
            ComponentName.Id.idExternalId,
            ComponentName.Id.idAttributes,
            ComponentName.Id.noId },
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef notation = new ClassDef(
        ComponentName.Id.idNotation,
        withIntrinsic(
            ComponentName.Id.idName,
            ComponentName.Id.idExternalId,
            ComponentName.Id.idAttributeDefs,
            ComponentName.Id.noId),
        new[] {
            ComponentName.Id.idExternalId,
            ComponentName.Id.idAttributeDefs,
            ComponentName.Id.noId },
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef externalId = new ClassDef(
        ComponentName.Id.idExternalId,
        withIntrinsic(
            ComponentName.Id.idPublicId,
            ComponentName.Id.idSystemId,
            ComponentName.Id.idGeneratedSystemId,
            ComponentName.Id.noId),
        noProps,
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef externalData = new ClassDef(
        ComponentName.Id.idExternalData,
        withIntrinsic(
            ComponentName.Id.idEntityName,
            ComponentName.Id.idEntity,
            ComponentName.Id.noId),
        noProps,
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef subdocument = new ClassDef(
        ComponentName.Id.idSubdocument,
        withIntrinsic(
            ComponentName.Id.idEntityName,
            ComponentName.Id.idEntity,
            ComponentName.Id.noId),
        noProps,
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef nonSgml = new ClassDef(
        ComponentName.Id.noId,
        noProps,
        noProps,
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef message = new ClassDef(
        ComponentName.Id.noId,
        noProps,
        noProps,
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef elementType = new ClassDef(
        ComponentName.Id.idElementType,
        withIntrinsic(
            ComponentName.Id.idAttributeDefs,
            ComponentName.Id.idContentType,
            ComponentName.Id.idExclusions,
            ComponentName.Id.idGi,
            ComponentName.Id.idInclusions,
            ComponentName.Id.idModelGroup,
            ComponentName.Id.idOmitEndTag,
            ComponentName.Id.idOmitStartTag,
            ComponentName.Id.noId),
        new[] {
            ComponentName.Id.idAttributeDefs,
            ComponentName.Id.idModelGroup,
            ComponentName.Id.noId },
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef modelGroup = new ClassDef(
        ComponentName.Id.idModelGroup,
        withIntrinsic(
            ComponentName.Id.idConnector,
            ComponentName.Id.idOccurenceIndicator,
            ComponentName.Id.idContentTokens,
            ComponentName.Id.noId),
        new[] { ComponentName.Id.idContentTokens, ComponentName.Id.noId },
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef elementToken = new ClassDef(
        ComponentName.Id.idElementToken,
        withIntrinsic(
            ComponentName.Id.idGi,
            ComponentName.Id.idOccurenceIndicator,
            ComponentName.Id.noId),
        new[] { ComponentName.Id.noId },
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef pcdataToken = new ClassDef(
        ComponentName.Id.idPcdataToken,
        withIntrinsic(ComponentName.Id.noId),
        new[] { ComponentName.Id.noId },
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);

    public static readonly ClassDef defaultEntity = new ClassDef(
        ComponentName.Id.idDefaultEntity,
        withIntrinsic(
            ComponentName.Id.idName,
            ComponentName.Id.idEntityType,
            ComponentName.Id.idText,
            ComponentName.Id.idExternalId,
            ComponentName.Id.idAttributes,
            ComponentName.Id.idNotationName,
            ComponentName.Id.idNotation,
            ComponentName.Id.noId),
        new[] {
            ComponentName.Id.idExternalId,
            ComponentName.Id.idAttributes,
            ComponentName.Id.noId },
        ComponentName.Id.noId,
        ComponentName.Id.noId,
        ComponentName.Id.noId);
}

public abstract class Node
{
    public struct OccurIndicator
    {
        public enum Enum { opt, plus, rep }
    }

    public struct EntityType
    {
        public enum Enum { text, cdata, sdata, ndata, subdocument, pi }
    }

    public struct DeclValueType
    {
        public enum Enum
        {
            cdata, entity, entities, id, idref, idrefs, name, names, nmtoken,
            nmtokens, number, numbers, nutoken, nutokens, notation, nmtkgrp
        }
    }

    public struct DefaultValueType
    {
        public enum Enum { value, @fixed, required, current, conref, implied }
    }

    public struct ContentType
    {
        public enum Enum { cdata, rcdata, empty, any, modelgrp }
    }

    public struct Connector
    {
        public enum Enum { and_, or_, seq }
    }

    public enum Severity { info, warning, error }


    // Intrinsic properties
    public virtual AccessResult getOrigin(ref NodePtr ptr) { return AccessResult.accessNull; }
    public virtual AccessResult getParent(ref NodePtr ptr) { return AccessResult.accessNull; }
    public virtual AccessResult getGroveRoot(ref NodePtr ptr) { return AccessResult.accessNull; }

    public virtual AccessResult getTreeRoot(ref NodePtr nd)
    {
        nd.assign(this);
        for (;;)
        {
            AccessResult res = nd.node!.getParent(ref nd);
            if (res != AccessResult.accessOK)
            {
                if (res == AccessResult.accessTimeout)
                    return res;
                break;
            }
        }
        return AccessResult.accessOK;
    }

    public abstract AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id id);

    public AccessResult getClassName(out ComponentName.Id name)
    {
        name = classDef().className;
        return AccessResult.accessOK;
    }

    public AccessResult getChildrenPropertyName(out ComponentName.Id name)
    {
        var def = classDef();
        if (def.childrenPropertyName == ComponentName.Id.noId)
        {
            name = ComponentName.Id.noId;
            return AccessResult.accessNull;
        }
        name = def.childrenPropertyName;
        return AccessResult.accessOK;
    }

    public AccessResult getDataPropertyName(out ComponentName.Id name)
    {
        var def = classDef();
        if (def.dataPropertyName == ComponentName.Id.noId)
        {
            name = ComponentName.Id.noId;
            return AccessResult.accessNull;
        }
        name = def.dataPropertyName;
        return AccessResult.accessOK;
    }

    public AccessResult getDataSepPropertyName(out ComponentName.Id name)
    {
        var def = classDef();
        if (def.dataSepPropertyName == ComponentName.Id.noId)
        {
            name = ComponentName.Id.noId;
            return AccessResult.accessNull;
        }
        name = def.dataSepPropertyName;
        return AccessResult.accessOK;
    }

    public AccessResult getSubnodePropertyNames(out ComponentName.Id[] names)
    {
        names = classDef().subnodePropertyNames;
        return AccessResult.accessOK;
    }

    public AccessResult getAllPropertyNames(out ComponentName.Id[] names)
    {
        names = classDef().allPropertyNames;
        return AccessResult.accessOK;
    }

    // Abstract methods
    public abstract void accept(NodeVisitor visitor);
    public abstract ClassDef classDef();
    public abstract AccessResult children(ref NodeListPtr ptr);
    public abstract AccessResult follow(ref NodeListPtr ptr);
    public abstract uint groveIndex();
    public abstract void addRef();
    public abstract void release();

    // Virtual methods with default implementations
    public virtual AccessResult nextSibling(ref NodePtr ptr) { return AccessResult.accessNotInClass; }

    public virtual AccessResult nextChunkSibling(ref NodePtr ptr) { return AccessResult.accessNotInClass; }

    public virtual AccessResult nextChunkAfter(ref NodePtr nd)
    {
        AccessResult ret = firstChild(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
            case AccessResult.accessTimeout:
                return ret;
            default:
                break;
        }
        for (;;)
        {
            ret = nextChunkSibling(ref nd);
            switch (ret)
            {
                case AccessResult.accessOK:
                case AccessResult.accessTimeout:
                    return ret;
                default:
                    break;
            }
            ret = getParent(ref nd);
            if (ret != AccessResult.accessOK)
                break;
        }
        return ret;
    }

    public virtual AccessResult charChunk(SdataMapper mapper, ref GroveString str)
    {
        return AccessResult.accessNotInClass;
    }

    public virtual AccessResult firstChild(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult firstSibling(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult siblingsIndex(out uint index) { index = 0; return AccessResult.accessNotInClass; }

    public virtual AccessResult attributeRef(uint n, ref NodePtr ptr)
    {
        var atts = new NamedNodeListPtr();
        AccessResult ret = getAttributes(ref atts);
        if (ret != AccessResult.accessOK)
            return ret;
        return atts.list!.nodeList().list!.@ref(n, ref ptr);
    }

    public virtual AccessResult followSiblingRef(uint n, ref NodePtr ptr)
    {
        if (n == 0)
            return nextSibling(ref ptr);
        var tem = new NodePtr();
        AccessResult ret = nextSibling(ref tem);
        if (ret != AccessResult.accessOK)
            return ret;
        while (--n > 0)
        {
            ret = tem.assignNextSibling();
            if (ret != AccessResult.accessOK)
                return ret;
        }
        return tem.node!.nextSibling(ref ptr);
    }

    public virtual AccessResult tokens(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult elementIndex(out uint index) { index = 0; return AccessResult.accessNotInClass; }

    public virtual uint hash() { return 0; }

    public abstract bool Equals(Node node);
    public bool NotEquals(Node node) { return !Equals(node); }

    public virtual bool chunkContains(Node nd) { return Equals(nd); }

    public bool sameGrove(Node node) { return groveIndex() == node.groveIndex(); }

    public virtual bool queryInterface(string iid, out object? ptr) { ptr = null; return false; }

    public virtual AccessResult getMessages(ref NodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getSeverity(out Severity severity) { severity = Severity.info; return AccessResult.accessNotInClass; }

    public AccessResult getChar(SdataMapper mapper, out GroveChar c)
    {
        var str = new GroveString();
        AccessResult ret = charChunk(mapper, ref str);
        if (ret == AccessResult.accessOK)
            c = str[0];
        else
            c = 0;
        return ret;
    }

    // Properties common to several node classes
    public virtual AccessResult getAttributes(ref NamedNodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getName(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getSystemData(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getEntity(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getEntityName(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getExternalId(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getNotation(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getGi(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getOccurIndicator(out OccurIndicator.Enum indicator) { indicator = OccurIndicator.Enum.opt; return AccessResult.accessNotInClass; }
    public virtual AccessResult getAttributeDefs(ref NamedNodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getText(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getNotationName(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getEntityType(out EntityType.Enum type) { type = EntityType.Enum.text; return AccessResult.accessNotInClass; }
    public virtual AccessResult getDefaulted(out bool defaulted) { defaulted = false; return AccessResult.accessNotInClass; }
    public virtual AccessResult getPublicId(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getSystemId(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getGeneratedSystemId(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getValue(ref NodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getTokenSep(out GroveChar sep) { sep = 0; return AccessResult.accessNotInClass; }
    public virtual AccessResult getImplied(out bool implied) { implied = false; return AccessResult.accessNotInClass; }
    public virtual AccessResult getAttributeDef(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getCurrentAttributeIndex(out long index) { index = 0; return AccessResult.accessNotInClass; }
    public virtual AccessResult getCurrentGroup(ref NodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getDeclValueType(out DeclValueType.Enum type) { type = DeclValueType.Enum.cdata; return AccessResult.accessNotInClass; }
    public virtual AccessResult getDefaultValueType(out DefaultValueType.Enum type) { type = DefaultValueType.Enum.value; return AccessResult.accessNotInClass; }
    public virtual AccessResult getDefaultValue(ref NodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getTokens(ref GroveStringListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual bool hasGi(GroveString gi) { return false; }
    public virtual AccessResult getId(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getContent(ref NodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getIncluded(out bool included) { included = false; return AccessResult.accessNotInClass; }
    public virtual AccessResult getMustOmitEndTag(out bool mustOmit) { mustOmit = false; return AccessResult.accessNotInClass; }
    public virtual AccessResult getElementType(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getContentType(out ContentType.Enum type) { type = ContentType.Enum.any; return AccessResult.accessNotInClass; }
    public virtual AccessResult getExclusions(ref GroveStringListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getInclusions(ref GroveStringListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getModelGroup(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getOmitEndTag(out bool omit) { omit = false; return AccessResult.accessNotInClass; }
    public virtual AccessResult getOmitStartTag(out bool omit) { omit = false; return AccessResult.accessNotInClass; }
    public virtual AccessResult getConnector(out Connector.Enum connector) { connector = Connector.Enum.seq; return AccessResult.accessNotInClass; }
    public virtual AccessResult getContentTokens(ref NodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getToken(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getReferent(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getGoverning(out bool governing) { governing = false; return AccessResult.accessNotInClass; }
    public virtual AccessResult getGeneralEntities(ref NamedNodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getNotations(ref NamedNodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getElementTypes(ref NamedNodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getDefaultEntity(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getParameterEntities(ref NamedNodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getSgmlConstants(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getApplicationInfo(ref GroveString str) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getProlog(ref NodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getEpilog(ref NodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getDocumentElement(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getElements(ref NamedNodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getEntities(ref NamedNodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getDefaultedEntities(ref NamedNodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getGoverningDoctype(ref NodePtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getDoctypesAndLinktypes(ref NamedNodeListPtr ptr) { return AccessResult.accessNotInClass; }
    public virtual AccessResult getNonSgml(out uint value) { value = 0; return AccessResult.accessNotInClass; }

    public AccessResult property(ComponentName.Id id, SdataMapper mapper, PropertyValue value)
    {
        AccessResult ret;
        switch (id)
        {
            case ComponentName.Id.idEntityType:
                {
                    ret = getEntityType(out EntityType.Enum type);
                    if (ret == AccessResult.accessOK)
                    {
                        switch (type)
                        {
                            case EntityType.Enum.text: value.set(ComponentName.Id.idText); break;
                            case EntityType.Enum.cdata: value.set(ComponentName.Id.idCdata); break;
                            case EntityType.Enum.sdata: value.set(ComponentName.Id.idSdata); break;
                            case EntityType.Enum.ndata: value.set(ComponentName.Id.idNdata); break;
                            case EntityType.Enum.pi: value.set(ComponentName.Id.idPi); break;
                            case EntityType.Enum.subdocument: value.set(ComponentName.Id.idSubdocument); break;
                            default: ret = AccessResult.accessNotInClass; break;
                        }
                    }
                    break;
                }
            case ComponentName.Id.idDeclValueType:
                {
                    ret = getDeclValueType(out DeclValueType.Enum type);
                    if (ret == AccessResult.accessOK)
                    {
                        switch (type)
                        {
                            case DeclValueType.Enum.cdata: value.set(ComponentName.Id.idCdata); break;
                            case DeclValueType.Enum.entity: value.set(ComponentName.Id.idEntity); break;
                            case DeclValueType.Enum.entities: value.set(ComponentName.Id.idEntities); break;
                            case DeclValueType.Enum.id: value.set(ComponentName.Id.idId); break;
                            case DeclValueType.Enum.idref: value.set(ComponentName.Id.idIdref); break;
                            case DeclValueType.Enum.idrefs: value.set(ComponentName.Id.idIdrefs); break;
                            case DeclValueType.Enum.name: value.set(ComponentName.Id.idName); break;
                            case DeclValueType.Enum.names: value.set(ComponentName.Id.idNames); break;
                            case DeclValueType.Enum.nmtoken: value.set(ComponentName.Id.idNmtoken); break;
                            case DeclValueType.Enum.nmtokens: value.set(ComponentName.Id.idNmtokens); break;
                            case DeclValueType.Enum.number: value.set(ComponentName.Id.idNumber); break;
                            case DeclValueType.Enum.numbers: value.set(ComponentName.Id.idNumbers); break;
                            case DeclValueType.Enum.nutoken: value.set(ComponentName.Id.idNutoken); break;
                            case DeclValueType.Enum.nutokens: value.set(ComponentName.Id.idNutokens); break;
                            case DeclValueType.Enum.notation: value.set(ComponentName.Id.idNotation); break;
                            case DeclValueType.Enum.nmtkgrp: value.set(ComponentName.Id.idNmtkgrp); break;
                            default: ret = AccessResult.accessNotInClass; break;
                        }
                    }
                    break;
                }
            case ComponentName.Id.idDefaultValueType:
                {
                    ret = getDefaultValueType(out DefaultValueType.Enum type);
                    if (ret == AccessResult.accessOK)
                    {
                        switch (type)
                        {
                            case DefaultValueType.Enum.value: value.set(ComponentName.Id.idValue); break;
                            case DefaultValueType.Enum.@fixed: value.set(ComponentName.Id.idFixed); break;
                            case DefaultValueType.Enum.required: value.set(ComponentName.Id.idRequired); break;
                            case DefaultValueType.Enum.current: value.set(ComponentName.Id.idCurrent); break;
                            case DefaultValueType.Enum.conref: value.set(ComponentName.Id.idConref); break;
                            case DefaultValueType.Enum.implied: value.set(ComponentName.Id.idImplied); break;
                            default: ret = AccessResult.accessNotInClass; break;
                        }
                    }
                    break;
                }
            case ComponentName.Id.idContentType:
                {
                    ret = getContentType(out ContentType.Enum type);
                    if (ret == AccessResult.accessOK)
                    {
                        switch (type)
                        {
                            case ContentType.Enum.cdata: value.set(ComponentName.Id.idCdata); break;
                            case ContentType.Enum.rcdata: value.set(ComponentName.Id.idRcdata); break;
                            case ContentType.Enum.empty: value.set(ComponentName.Id.idEmpty); break;
                            case ContentType.Enum.any: value.set(ComponentName.Id.idAny); break;
                            case ContentType.Enum.modelgrp: value.set(ComponentName.Id.idModelGroup); break;
                            default: ret = AccessResult.accessNotInClass; break;
                        }
                    }
                    break;
                }
            case ComponentName.Id.idConnector:
                {
                    ret = getConnector(out Connector.Enum type);
                    if (ret == AccessResult.accessOK)
                    {
                        switch (type)
                        {
                            case Connector.Enum.and_: value.set(ComponentName.Id.idAnd); break;
                            case Connector.Enum.or_: value.set(ComponentName.Id.idOr); break;
                            case Connector.Enum.seq: value.set(ComponentName.Id.idSeq); break;
                            default: ret = AccessResult.accessNotInClass; break;
                        }
                    }
                    break;
                }
            case ComponentName.Id.idChar:
                {
                    ret = getChar(mapper, out GroveChar tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idClassName:
                {
                    ret = getClassName(out ComponentName.Id tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idChildrenPropertyName:
                {
                    ret = getChildrenPropertyName(out ComponentName.Id tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idDataPropertyName:
                {
                    ret = getDataPropertyName(out ComponentName.Id tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idDataSepPropertyName:
                {
                    ret = getDataSepPropertyName(out ComponentName.Id tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idOriginToSubnodeRelPropertyName:
                {
                    ret = getOriginToSubnodeRelPropertyName(out ComponentName.Id tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idSubnodePropertyNames:
                {
                    ret = getSubnodePropertyNames(out ComponentName.Id[] tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idAllPropertyNames:
                {
                    ret = getAllPropertyNames(out ComponentName.Id[] tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idDefaulted:
                {
                    ret = getDefaulted(out bool tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idGoverning:
                {
                    ret = getGoverning(out bool tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idImplied:
                {
                    ret = getImplied(out bool tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idIncluded:
                {
                    ret = getIncluded(out bool tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idMustOmitEndTag:
                {
                    ret = getMustOmitEndTag(out bool tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idOmitEndTag:
                {
                    ret = getOmitEndTag(out bool tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idOmitStartTag:
                {
                    ret = getOmitStartTag(out bool tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idTokenSep:
                {
                    ret = getTokenSep(out GroveChar tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            case ComponentName.Id.idCurrentAttributeIndex:
                {
                    ret = getCurrentAttributeIndex(out long tem);
                    if (ret == AccessResult.accessOK)
                        value.set(tem);
                    break;
                }
            default:
                ret = AccessResult.accessNotInClass;
                break;
        }
        return ret;
    }
}

public abstract class NodeList
{
    public abstract AccessResult first(ref NodePtr ptr);
    public abstract AccessResult rest(ref NodeListPtr ptr);
    public abstract AccessResult chunkRest(ref NodeListPtr ptr);
    public abstract void release();
    public abstract void addRef();

    public virtual AccessResult @ref(uint i, ref NodePtr ptr)
    {
        if (i == 0)
            return first(ref ptr);
        var tem = new NodeListPtr();
        AccessResult ret = rest(ref tem);
        if (ret != AccessResult.accessOK)
            return ret;
        while (--i > 0)
        {
            ret = tem.assignRest();
            if (ret != AccessResult.accessOK)
                return ret;
        }
        return tem.list!.first(ref ptr);
    }
}

public abstract class NamedNodeList
{
    public enum Type
    {
        elements,
        attributes,
        entities,
        notations,
        doctypesAndLinktypes,
        elementTypes,
        attributeDefs
    }

    public abstract AccessResult namedNode(GroveString name, ref NodePtr ptr);
    public abstract nuint normalize(GroveChar[] chars, nuint size);
    public abstract NodeListPtr nodeList();
    public virtual NodeListPtr nodeListNoOrder() { return nodeList(); }
    public abstract Type type();
    public abstract void release();
    public abstract void addRef();

    public AccessResult nodeName(NodePtr node, ref GroveString name)
    {
        var v = new NodeNameNodeVisitor(type(), ref name);
        node.node!.accept(v);
        return v.ret;
    }

    private class NodeNameNodeVisitor : NodeVisitor
    {
        public AccessResult ret = AccessResult.accessNotInClass;
        private GroveString nameP;
        private Type _type;

        public NodeNameNodeVisitor(Type t, ref GroveString name)
        {
            _type = t;
            nameP = name;
        }

        public override void element(Node nd)
        {
            if (_type == Type.elements)
                ret = nd.getId(ref nameP);
        }

        public override void attributeAssignment(Node nd)
        {
            if (_type == Type.attributes)
                ret = nd.getName(ref nameP);
        }

        public override void documentType(Node nd)
        {
            if (_type == Type.doctypesAndLinktypes)
                ret = nd.getName(ref nameP);
        }

        public override void entity(Node nd)
        {
            if (_type == Type.entities)
                ret = nd.getName(ref nameP);
        }

        public override void notation(Node nd)
        {
            if (_type == Type.notations)
                ret = nd.getName(ref nameP);
        }

        public override void elementType(Node nd)
        {
            if (_type == Type.elementTypes)
                ret = nd.getGi(ref nameP);
        }

        public override void attributeDef(Node nd)
        {
            if (_type == Type.attributeDefs)
                ret = nd.getName(ref nameP);
        }
    }
}

public class NodePtr
{
    internal Node? node;

    public NodePtr() { node = null; }
    public NodePtr(Node? node) { this.node = node; addRef(); }

    public void Dispose() { release(); }

    public NodePtr(NodePtr ptr) { node = ptr.node; addRef(); }

    public void assign(NodePtr ptr)
    {
        ptr.addRef();
        release();
        node = ptr.node;
    }

    public void assign(Node? n)
    {
        if (n != null)
            n.addRef();
        release();
        node = n;
    }

    public void clear() { release(); node = null; }

    public static implicit operator bool(NodePtr ptr) => ptr.node != null;

    public AccessResult assignOrigin()
    {
        var temp = new NodePtr();
        var result = node!.getOrigin(ref temp);
        if (result == AccessResult.accessOK)
            assign(temp.node);
        return result;
    }

    public AccessResult assignFirstChild()
    {
        var temp = new NodePtr();
        var result = node!.firstChild(ref temp);
        if (result == AccessResult.accessOK)
            assign(temp.node);
        return result;
    }

    public AccessResult assignNextSibling()
    {
        var temp = new NodePtr();
        var result = node!.nextSibling(ref temp);
        if (result == AccessResult.accessOK)
            assign(temp.node);
        return result;
    }

    public AccessResult assignNextChunkSibling()
    {
        var temp = new NodePtr();
        var result = node!.nextChunkSibling(ref temp);
        if (result == AccessResult.accessOK)
            assign(temp.node);
        return result;
    }

    public AccessResult assignNextChunkAfter()
    {
        var temp = new NodePtr();
        var result = node!.nextChunkAfter(ref temp);
        if (result == AccessResult.accessOK)
            assign(temp.node);
        return result;
    }

    public AccessResult assignFirstSibling()
    {
        var temp = new NodePtr();
        var result = node!.firstSibling(ref temp);
        if (result == AccessResult.accessOK)
            assign(temp.node);
        return result;
    }

    public AccessResult getDocumentElement(ref NodePtr result)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.getDocumentElement(ref result);
    }

    public AccessResult elementIndex(ref ulong index)
    {
        if (node == null)
            return AccessResult.accessNull;
        uint idx;
        var result = node.elementIndex(out idx);
        index = idx;
        return result;
    }

    public uint groveIndex()
    {
        if (node == null)
            return 0;
        return node.groveIndex();
    }

    public AccessResult getGi(ref GroveString str)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.getGi(ref str);
    }

    public AccessResult getId(ref GroveString str)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.getId(ref str);
    }

    public AccessResult getOrigin(ref NodePtr ptr)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.getOrigin(ref ptr);
    }

    public AccessResult getParent(ref NodePtr ptr)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.getParent(ref ptr);
    }

    public AccessResult children(ref NodeListPtr ptr)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.children(ref ptr);
    }

    public AccessResult charChunk(SdataMapper? mapper, ref GroveString str)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.charChunk(mapper ?? new SdataMapper(), ref str);
    }

    public AccessResult nextChunkSibling(ref NodePtr ptr)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.nextChunkSibling(ref ptr);
    }

    public AccessResult firstChild(ref NodePtr ptr)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.firstChild(ref ptr);
    }

    public AccessResult getGroveRoot(ref NodePtr ptr)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.getGroveRoot(ref ptr);
    }

    public AccessResult getAttributes(ref NamedNodeListPtr ptr)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.getAttributes(ref ptr);
    }

    public AccessResult tokens(ref GroveString str)
    {
        if (node == null)
            return AccessResult.accessNull;
        return node.tokens(ref str);
    }

    public AccessResult getImplied(out bool implied)
    {
        implied = false;
        if (node == null)
            return AccessResult.accessNull;
        return node.getImplied(out implied);
    }

    public AccessResult assignParent()
    {
        var temp = new NodePtr();
        var result = node!.getParent(ref temp);
        if (result == AccessResult.accessOK)
            assign(temp.node);
        return result;
    }

    public AccessResult assignPreviousSibling()
    {
        // Get parent and find previous sibling by iteration
        var parent = new NodePtr();
        if (node!.getParent(ref parent) != AccessResult.accessOK)
            return AccessResult.accessNotInClass;
        var first = new NodePtr();
        if (parent.node!.firstChild(ref first) != AccessResult.accessOK)
            return AccessResult.accessNotInClass;
        if (first.node == node)
            return AccessResult.accessNotInClass; // No previous sibling
        var prev = first;
        var next = new NodePtr();
        while (prev.node!.nextChunkSibling(ref next) == AccessResult.accessOK)
        {
            if (next.node == node)
            {
                assign(prev.node);
                return AccessResult.accessOK;
            }
            prev.assign(next);
        }
        return AccessResult.accessNotInClass;
    }

    public AccessResult getElementWithId(GroveString id, ref NodePtr ptr)
    {
        if (node == null)
            return AccessResult.accessNull;
        // Get elements named node list
        var elements = new NamedNodeListPtr();
        if (node.getElements(ref elements) != AccessResult.accessOK)
            return AccessResult.accessNotInClass;
        // Look up by ID
        return elements.namedNode(id, ref ptr);
    }

    public bool isNull() { return node == null; }

    private void addRef() { if (node != null) node.addRef(); }
    private void release() { if (node != null) node.release(); }
}

public class NodeListPtr
{
    internal NodeList? list;

    public NodeListPtr() { list = null; }
    public NodeListPtr(NodeList? list) { this.list = list; addRef(); }

    public void Dispose() { release(); }

    public NodeListPtr(NodeListPtr ptr) { list = ptr.list; addRef(); }

    public void assign(NodeListPtr ptr)
    {
        ptr.addRef();
        release();
        list = ptr.list;
    }

    public void assign(NodeList? l)
    {
        if (l != null)
            l.addRef();
        release();
        list = l;
    }

    public AccessResult assignRest()
    {
        var temp = new NodeListPtr();
        var result = list!.rest(ref temp);
        if (result == AccessResult.accessOK)
            assign(temp.list);
        return result;
    }

    public AccessResult assignChunkRest()
    {
        var temp = new NodeListPtr();
        var result = list!.chunkRest(ref temp);
        if (result == AccessResult.accessOK)
            assign(temp.list);
        return result;
    }

    public void clear() { release(); list = null; }

    public static implicit operator bool(NodeListPtr ptr) => ptr.list != null;

    public AccessResult first(ref NodePtr ptr)
    {
        if (list == null)
            return AccessResult.accessNull;
        return list.first(ref ptr);
    }

    public AccessResult rest(ref NodeListPtr ptr)
    {
        if (list == null)
            return AccessResult.accessNull;
        return list.rest(ref ptr);
    }

    private void addRef() { if (list != null) list.addRef(); }
    private void release() { if (list != null) list.release(); }
}

public class NamedNodeListPtr
{
    internal NamedNodeList? list;

    public NamedNodeListPtr() { list = null; }
    public NamedNodeListPtr(NamedNodeList? list) { this.list = list; addRef(); }

    public void Dispose() { release(); }

    public NamedNodeListPtr(NamedNodeListPtr ptr) { list = ptr.list; addRef(); }

    public void assign(NamedNodeListPtr ptr)
    {
        ptr.addRef();
        release();
        list = ptr.list;
    }

    public void assign(NamedNodeList? l)
    {
        if (l != null)
            l.addRef();
        release();
        list = l;
    }

    public void clear() { release(); list = null; }

    public static implicit operator bool(NamedNodeListPtr ptr) => ptr.list != null;

    public AccessResult namedNode(GroveString name, ref NodePtr ptr)
    {
        if (list == null)
            return AccessResult.accessNull;
        return list.namedNode(name, ref ptr);
    }

    public NodeListPtr nodeList()
    {
        if (list == null)
            return new NodeListPtr();
        return list.nodeList();
    }

    private void addRef() { if (list != null) list.addRef(); }
    private void release() { if (list != null) list.release(); }
}

public class GroveString
{
    private GroveChar[]? data_;
    private nuint offset_;
    private nuint size_;

    public GroveString()
    {
        data_ = null;
        offset_ = 0;
        size_ = 0;
    }

    public GroveString(GroveChar[] data, nuint size)
    {
        data_ = data;
        offset_ = 0;
        size_ = size;
    }

    public GroveString(GroveChar[] data, nuint offset, nuint size)
    {
        data_ = data;
        offset_ = offset;
        size_ = size;
    }

    public nuint size() { return size_; }
    public GroveChar[]? data() { return data_; }

    public void assign(GroveChar[] data, nuint size)
    {
        data_ = data;
        offset_ = 0;
        size_ = size;
    }

    public void assign(GroveChar[] data, nuint offset, nuint size)
    {
        data_ = data;
        offset_ = offset;
        size_ = size;
    }

    public GroveChar this[nuint i] => data_![offset_ + i];

    public bool Equals(GroveString str)
    {
        if (size() != str.size())
            return false;
        if (size() == 0)
            return true;
        for (nuint i = 0; i < size_; i++)
        {
            if (this[i] != str[i])
                return false;
        }
        return true;
    }

    public bool NotEquals(GroveString str) { return !Equals(str); }

    public IEnumerable<GroveChar> GetEnumerator()
    {
        for (nuint i = 0; i < size_; i++)
            yield return this[i];
    }
}

public class GroveStringLink
{
    internal GroveString data_;
    internal GroveStringLink? next_;

    public GroveStringLink(GroveString gs)
    {
        data_ = gs;
        next_ = null;
    }
}

public class ConstGroveStringListIter
{
    protected GroveStringLink? link_;

    public ConstGroveStringListIter() { link_ = null; }

    public ConstGroveStringListIter(GroveStringList list)
    {
        link_ = list.head_;
    }

    public bool done() { return link_ == null; }
    public GroveString cur() { return link_!.data_; }
    public void next() { link_ = link_!.next_; }

    internal void attach(GroveStringLink? link) { link_ = link; }
}

public class GroveStringList
{
    internal GroveStringLink? head_;
    private uint refCount_;
    private ConstGroveStringListIter iter_;

    public GroveStringList()
    {
        head_ = null;
        refCount_ = 0;
        iter_ = new ConstGroveStringListIter();
    }

    ~GroveStringList()
    {
        var ptr = head_;
        while (ptr != null)
        {
            var tmpPtr = ptr;
            ptr = ptr.next_;
            // tmpPtr will be garbage collected
        }
    }

    public void append(GroveString gs)
    {
        GroveStringLink? prev = null;
        var ptr = head_;
        while (ptr != null)
        {
            prev = ptr;
            ptr = ptr.next_;
        }
        var newLink = new GroveStringLink(gs);
        if (prev == null)
            head_ = newLink;
        else
            prev.next_ = newLink;
        if (iter_.done())
            iter_.attach(head_);
    }

    public AccessResult first(out GroveString str)
    {
        if (head_ == null)
        {
            str = new GroveString();
            return AccessResult.accessNull;
        }
        str = head_.data_;
        return AccessResult.accessOK;
    }

    public AccessResult rest(ref GroveStringListPtr ptr)
    {
        if (canReuse(ptr))
        {
            if (iter_.done())
                return AccessResult.accessNull;
            iter_.next();
            return AccessResult.accessOK;
        }
        ptr.assign(new GroveStringList());
        var iter = new ConstGroveStringListIter(this);
        if (iter.done())
            return AccessResult.accessNull;
        iter.next();
        while (!iter.done())
        {
            ptr.list!.append(iter.cur());
            iter.next();
        }
        return AccessResult.accessOK;
    }

    public void release()
    {
        if (--refCount_ == 0)
        {
            // Will be garbage collected
        }
    }

    public void addRef()
    {
        ++refCount_;
    }

    public bool canReuse(GroveStringListPtr ptr)
    {
        return ptr.list == this && refCount_ == 1;
    }

    public ConstGroveStringListIter iter() { return iter_; }
}

public class GroveStringListPtr
{
    internal GroveStringList? list;

    public GroveStringListPtr() { list = null; }
    public GroveStringListPtr(GroveStringList? list) { this.list = list; addRef(); }

    public void Dispose() { release(); }

    public GroveStringListPtr(GroveStringListPtr ptr) { list = ptr.list; addRef(); }

    public void assign(GroveStringListPtr ptr)
    {
        ptr.addRef();
        release();
        list = ptr.list;
    }

    public void assign(GroveStringList? l)
    {
        if (l != null)
            l.addRef();
        release();
        list = l;
    }

    public void clear() { release(); list = null; }

    public static implicit operator bool(GroveStringListPtr ptr) => ptr.list != null;

    private void addRef() { if (list != null) list.addRef(); }
    private void release() { if (list != null) list.release(); }
}

public class SdataMapper
{
    public virtual bool sdataMap(GroveString name, GroveString text, out GroveChar c)
    {
        c = 0;
        return false;
    }
}

public class NodeVisitor
{
    public virtual void sgmlDocument(Node nd) { }
    public virtual void sgmlConstants(Node nd) { }
    public virtual void dataChar(Node nd) { }
    public virtual void element(Node nd) { }
    public virtual void attributeAssignment(Node nd) { }
    public virtual void attributeDef(Node nd) { }
    public virtual void attributeValueToken(Node nd) { }
    public virtual void pi(Node nd) { }
    public virtual void sdata(Node nd) { }
    public virtual void documentType(Node nd) { }
    public virtual void entity(Node nd) { }
    public virtual void notation(Node nd) { }
    public virtual void externalId(Node nd) { }
    public virtual void externalData(Node nd) { }
    public virtual void subdocument(Node nd) { }
    public virtual void nonSgml(Node nd) { }
    public virtual void message(Node nd) { }
    public virtual void elementType(Node nd) { }
    public virtual void modelGroup(Node nd) { }
    public virtual void elementToken(Node nd) { }
    public virtual void pcdataToken(Node nd) { }
    public virtual void defaultEntity(Node nd) { }
}

public abstract class PropertyValue
{
    public abstract void set(NodePtr ptr);
    public abstract void set(NodeListPtr ptr);
    public abstract void set(NamedNodeListPtr ptr);
    public abstract void set(bool value);
    public abstract void set(GroveChar value);
    public abstract void set(GroveString value);
    public abstract void set(ComponentName.Id value);
    public abstract void set(GroveStringListPtr ptr);
    public abstract void set(ComponentName.Id[] values);
    public abstract void set(long value);
}
