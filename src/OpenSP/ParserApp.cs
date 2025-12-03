// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Parser application base class
public abstract class ParserApp : EntityApp
{
    public const uint DEFAULT_ERROR_LIMIT = 200;

    protected new ParserOptions options_ = new ParserOptions();
    protected SgmlParser parser_ = new SgmlParser();
    protected uint errorLimit_;
    protected Vector<StringC> arcNames_ = new Vector<StringC>();
    protected Vector<string> activeLinkTypes_ = new Vector<string>();

    // ParserApp(const char *requiredInternalCode = 0);
    public ParserApp(string? requiredInternalCode = null)
        : base(requiredInternalCode)
    {
        errorLimit_ = DEFAULT_ERROR_LIMIT;

        registerOption('a', "activate", ParserAppMessages.name, ParserAppMessages.aHelp);
        registerOption('A', "architecture", ParserAppMessages.name, ParserAppMessages.AHelp);
        registerOption('E', "max-errors", ParserAppMessages.number, ParserAppMessages.EHelp);
        registerOption('e', "open-entities", ParserAppMessages.eHelp);
        registerOption('g', "open-elements", ParserAppMessages.gHelp);
        registerOption('n', "error-numbers", ParserAppMessages.nHelp);
        registerOption('x', "references", ParserAppMessages.xHelp);
        registerOption('i', "include", ParserAppMessages.name, ParserAppMessages.iHelp);
        registerOption('w', "warning", ParserAppMessages.type, ParserAppMessages.wHelp);
    }

    // void processOption(AppChar opt, const AppChar *arg);
    public override void processOption(char opt, string? arg)
    {
        switch (opt)
        {
            case 'a':
                // activate link
                if (arg != null)
                    activeLinkTypes_.push_back(arg);
                break;
            case 'A':
                if (arg != null)
                    arcNames_.push_back(convertInput(arg));
                break;
            case 'E':
                {
                    if (arg == null || !uint.TryParse(arg, out uint n))
                        message(ParserAppMessages.badErrorLimit);
                    else
                        errorLimit_ = n;
                }
                break;
            case 'e':
                // describe open entities in error messages
                addOption(MessageReporter.Option.openEntities);
                break;
            case 'g':
                // show gis of open elements in error messages
                addOption(MessageReporter.Option.openElements);
                break;
            case 'n':
                // show message number in error messages
                addOption(MessageReporter.Option.messageNumbers);
                break;
            case 'x':
                // show relevant clauses in error messages
                addOption(MessageReporter.Option.clauses);
                break;
            case 'i':
                // pretend that arg is defined as INCLUDE
                if (arg != null)
                    options_.includes.push_back(convertInput(arg));
                break;
            case 'w':
                if (arg != null && !enableWarning(arg))
                    message(ParserAppMessages.unknownWarning,
                          new StringMessageArg(convertInput(arg)));
                break;
            default:
                base.processOption(opt, arg);
                break;
        }
    }

    // int processSysid(const StringC &);
    public override int processSysid(StringC sysid)
    {
        initParser(sysid);
        ErrorCountEventHandler? eceh = makeEventHandler();
        if (eceh == null)
            return 1;
        if (errorLimit_ != 0)
            eceh.setErrorLimit(errorLimit_);
        return generateEvents(eceh);
    }

    // virtual ErrorCountEventHandler *makeEventHandler() = 0;
    public abstract ErrorCountEventHandler? makeEventHandler();

    // Boolean enableWarning(const AppChar *s);
    public Boolean enableWarning(string s)
    {
        // Warning groups
        const byte groupAll = 0x01;
        const byte groupMinTag = 0x02;
        const byte groupXML = 0x04;

        // Parse warning name
        Boolean val = true;
        if (s.StartsWith("no-"))
        {
            s = s.Substring(3);
            val = false;
        }

        // Group warnings
        switch (s)
        {
            case "all":
                setWarningGroup(groupAll, val);
                return true;
            case "min-tag":
                setWarningGroup(groupMinTag, val);
                return true;
            case "xml":
                setWarningGroup(groupXML, val);
                return true;
        }

        // Individual warnings
        switch (s)
        {
            case "mixed": options_.warnMixedContent = val; return true;
            case "should": options_.warnShould = val; return true;
            case "duplicate": options_.warnDuplicateEntity = val; return true;
            case "default": options_.warnDefaultEntityReference = val; return true;
            case "undefined": options_.warnUndefinedElement = val; return true;
            case "sgmldecl": options_.warnSgmlDecl = val; return true;
            case "unclosed": options_.noUnclosedTag = val; return true;
            case "net": options_.noNet = val; return true;
            case "empty": options_.warnEmptyTag = val; return true;
            case "unused-map": options_.warnUnusedMap = val; return true;
            case "unused-param": options_.warnUnusedParam = val; return true;
            case "notation-sysid": options_.warnNotationSystemId = val; return true;
            case "inclusion": options_.warnInclusion = val; return true;
            case "exclusion": options_.warnExclusion = val; return true;
            case "rcdata-content": options_.warnRcdataContent = val; return true;
            case "cdata-content": options_.warnCdataContent = val; return true;
            case "ps-comment": options_.warnPsComment = val; return true;
            case "attlist-group-decl": options_.warnAttlistGroupDecl = val; return true;
            case "element-group-decl": options_.warnElementGroupDecl = val; return true;
            case "pi-entity": options_.warnPiEntity = val; return true;
            case "internal-sdata-entity": options_.warnInternalSdataEntity = val; return true;
            case "internal-cdata-entity": options_.warnInternalCdataEntity = val; return true;
            case "external-sdata-entity": options_.warnExternalSdataEntity = val; return true;
            case "external-cdata-entity": options_.warnExternalCdataEntity = val; return true;
            case "bracket-entity": options_.warnBracketEntity = val; return true;
            case "data-atts": options_.warnDataAttributes = val; return true;
            case "missing-system-id": options_.warnMissingSystemId = val; return true;
            case "conref": options_.warnConref = val; return true;
            case "current": options_.warnCurrent = val; return true;
            case "nutoken-decl-value": options_.warnNutokenDeclaredValue = val; return true;
            case "number-decl-value": options_.warnNumberDeclaredValue = val; return true;
            case "name-decl-value": options_.warnNameDeclaredValue = val; return true;
            case "named-char-ref": options_.warnNamedCharRef = val; return true;
            case "refc": options_.warnRefc = val; return true;
            case "temp-ms": options_.warnTempMarkedSection = val; return true;
            case "rcdata-ms": options_.warnRcdataMarkedSection = val; return true;
            case "instance-include-ms": options_.warnInstanceIncludeMarkedSection = val; return true;
            case "instance-ignore-ms": options_.warnInstanceIgnoreMarkedSection = val; return true;
            case "and-group": options_.warnAndGroup = val; return true;
            case "rank": options_.warnRank = val; return true;
            case "empty-comment-decl": options_.warnEmptyCommentDecl = val; return true;
            case "att-value-not-literal": options_.warnAttributeValueNotLiteral = val; return true;
            case "missing-att-name": options_.warnMissingAttributeName = val; return true;
            case "comment-decl-s": options_.warnCommentDeclS = val; return true;
            case "comment-decl-multiple": options_.warnCommentDeclMultiple = val; return true;
            case "missing-status-keyword": options_.warnMissingStatusKeyword = val; return true;
            case "multiple-status-keyword": options_.warnMultipleStatusKeyword = val; return true;
            case "instance-param-entity": options_.warnInstanceParamEntityRef = val; return true;
            case "min-param": options_.warnMinimizationParam = val; return true;
            case "mixed-content-xml": options_.warnMixedContentRepOrGroup = val; return true;
            case "name-group-not-or": options_.warnNameGroupNotOr = val; return true;
            case "pi-missing-name": options_.warnPiMissingName = val; return true;
            case "instance-status-keyword-s": options_.warnInstanceStatusKeywordSpecS = val; return true;
            case "external-data-entity-ref": options_.warnExternalDataEntityRef = val; return true;
            case "att-value-external-entity-ref": options_.warnAttributeValueExternalEntityRef = val; return true;
            case "data-delim": options_.warnDataDelim = val; return true;
            case "explicit-sgml-decl": options_.warnExplicitSgmlDecl = val; return true;
            case "internal-subset-ms": options_.warnInternalSubsetMarkedSection = val; return true;
            case "default-entity": options_.warnDefaultEntityDecl = val; return true;
            case "non-sgml-char-ref": options_.warnNonSgmlCharRef = val; return true;
            case "internal-subset-ps-param-entity": options_.warnInternalSubsetPsParamEntityRef = val; return true;
            case "internal-subset-ts-param-entity": options_.warnInternalSubsetTsParamEntityRef = val; return true;
            case "internal-subset-literal-param-entity": options_.warnInternalSubsetLiteralParamEntityRef = val; return true;
            case "immediate-recursion": options_.warnImmediateRecursion = val; return true;
            case "fully-declared": options_.fullyDeclared = val; return true;
            case "fully-tagged": options_.fullyTagged = val; return true;
            case "amply-tagged-recursive": options_.amplyTagged = val; return true;
            case "amply-tagged": options_.amplyTaggedAnyother = val; return true;
            case "type-valid": options_.valid = val; return true;
            case "entity-ref": options_.entityRef = val; return true;
            case "external-entity-ref": options_.externalEntityRef = val; return true;
            case "integral": options_.integral = val; return true;
            case "idref": options_.errorIdref = val; return true;
            case "significant": options_.errorSignificant = val; return true;
            case "afdr": options_.errorAfdr = val; return true;
            case "valid": options_.typeValid = val ? (short)1 : (short)0; return true;
        }

        return false;
    }

    private void setWarningGroup(byte group, Boolean val)
    {
        const byte groupAll = 0x01;
        const byte groupMinTag = 0x02;
        const byte groupXML = 0x04;

        if ((group & groupAll) != 0)
        {
            options_.warnMixedContent = val;
            options_.warnShould = val;
            options_.warnDefaultEntityReference = val;
            options_.warnUndefinedElement = val;
            options_.warnSgmlDecl = val;
            options_.warnEmptyTag = val;
            options_.warnUnusedMap = val;
            options_.warnUnusedParam = val;
        }
        if ((group & groupMinTag) != 0)
        {
            options_.noUnclosedTag = val;
            options_.noNet = val;
            options_.warnEmptyTag = val;
        }
        if ((group & groupXML) != 0)
        {
            options_.warnInclusion = val;
            options_.warnExclusion = val;
            options_.warnRcdataContent = val;
            options_.warnCdataContent = val;
            options_.warnPsComment = val;
            options_.warnAttlistGroupDecl = val;
            options_.warnElementGroupDecl = val;
            options_.warnPiEntity = val;
            options_.warnInternalSdataEntity = val;
            options_.warnInternalCdataEntity = val;
            options_.warnExternalSdataEntity = val;
            options_.warnExternalCdataEntity = val;
            options_.warnBracketEntity = val;
            options_.warnDataAttributes = val;
            options_.warnMissingSystemId = val;
            options_.warnConref = val;
            options_.warnCurrent = val;
            options_.warnNutokenDeclaredValue = val;
            options_.warnNumberDeclaredValue = val;
            options_.warnNameDeclaredValue = val;
            options_.warnNamedCharRef = val;
            options_.warnRefc = val;
            options_.warnTempMarkedSection = val;
            options_.warnRcdataMarkedSection = val;
            options_.warnInstanceIncludeMarkedSection = val;
            options_.warnInstanceIgnoreMarkedSection = val;
            options_.warnAndGroup = val;
            options_.warnRank = val;
            options_.warnEmptyCommentDecl = val;
            options_.warnAttributeValueNotLiteral = val;
            options_.warnMissingAttributeName = val;
            options_.warnCommentDeclS = val;
            options_.warnCommentDeclMultiple = val;
            options_.warnMissingStatusKeyword = val;
            options_.warnMultipleStatusKeyword = val;
            options_.warnInstanceParamEntityRef = val;
            options_.warnMinimizationParam = val;
            options_.warnMixedContentRepOrGroup = val;
            options_.warnNameGroupNotOr = val;
            options_.warnPiMissingName = val;
            options_.warnInstanceStatusKeywordSpecS = val;
            options_.warnExternalDataEntityRef = val;
            options_.warnAttributeValueExternalEntityRef = val;
            options_.warnDataDelim = val;
            options_.warnExplicitSgmlDecl = val;
            options_.warnInternalSubsetMarkedSection = val;
            options_.warnDefaultEntityDecl = val;
            options_.warnNonSgmlCharRef = val;
            options_.warnInternalSubsetPsParamEntityRef = val;
            options_.warnInternalSubsetTsParamEntityRef = val;
            options_.warnInternalSubsetLiteralParamEntityRef = val;
        }
    }

    // void initParser(const StringC &sysid);
    public void initParser(StringC sysid)
    {
        SgmlParser.Params @params = new SgmlParser.Params();
        @params.sysid = sysid;
        @params.entityManager.operatorAssign(entityManager().pointer());
        @params.options = options_;
        parser_.init(@params);

        if (arcNames_.size() > 0)
            parser_.activateLinkType(arcNames_[0]);

        for (nuint i = 0; i < activeLinkTypes_.size(); i++)
            parser_.activateLinkType(convertInput(activeLinkTypes_[i]));

        allLinkTypesActivated();
    }

    // SgmlParser &parser();
    public SgmlParser parser()
    {
        return parser_;
    }

    // void parseAll(SgmlParser &, EventHandler &, const volatile sig_atomic_t *cancelPtr);
    public void parseAll(SgmlParser parser, EventHandler eh, int cancelPtr)
    {
        if (arcNames_.size() > 0)
        {
            SelectOneArcDirector director = new SelectOneArcDirector(arcNames_, eh);
            // Use 'this' as Messenger since ParserApp inherits from Messenger
            ArcEngine.parseAll(parser, this, director, cancelPtr);
        }
        else
        {
            parser.parseAll(eh, cancelPtr);
        }
    }

    // virtual void allLinkTypesActivated();
    public virtual void allLinkTypesActivated()
    {
        parser_.allLinkTypesActivated();
    }

    // virtual int generateEvents(ErrorCountEventHandler *);
    protected virtual int generateEvents(ErrorCountEventHandler eceh)
    {
        Owner<EventHandler> eh = new Owner<EventHandler>(eceh);
        parseAll(parser_, eh.pointer()!, eceh.cancelPtr());
        uint errorCount = eceh.errorCount();
        if (errorLimit_ != 0 && errorCount >= errorLimit_)
            message(ParserAppMessages.errorLimitExceeded,
                  new NumberMessageArg(errorLimit_));
        return errorCount > 0 ? 1 : 0;
    }
}
