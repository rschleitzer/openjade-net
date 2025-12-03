// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Parser : ParserState
{
    // Literal parsing flags
    public const int literalSingleSpace = 0x01;
    public const int literalDataTag = 0x02;
    public const int literalMinimumData = 0x04;
    public const int literalDelimInfo = 0x08;
    public const int literalNoProcess = 0x10;
    public const int literalNonSgml = 0x20;

    // Attribute parameter types
    public enum AttributeParameterType
    {
        end,
        name,
        nameToken,
        vi,
        recoverUnquoted
    }

    private StringC sysid_ = new StringC();

    // Parser(const SgmlParser::Params &);
    public Parser(SgmlParser.Params @params)
        : base(@params.parent != null
                   ? @params.parent.parser_!.entityManagerPtr()
                   : @params.entityManager,
               @params.options != null
                   ? @params.options
                   : @params.parent!.parser_!.options(),
               paramsSubdocLevel(@params),
               @params.entityType == SgmlParser.Params.EntityType.dtd
                   ? Phase.declSubsetPhase
                   : Phase.contentPhase)
    {
        sysid_ = @params.sysid;

        Parser? parent = null;
        if (@params.parent != null)
            parent = @params.parent.parser_;

        if (@params.entityType == SgmlParser.Params.EntityType.document)
        {
            Sd sd = new Sd(entityManagerPtr());
            ParserOptions opt = options();
            sd.setBooleanFeature(Sd.BooleanFeature.fDATATAG, opt.datatag);
            sd.setBooleanFeature(Sd.BooleanFeature.fOMITTAG, opt.omittag);
            sd.setBooleanFeature(Sd.BooleanFeature.fRANK, opt.rank);
            sd.setShorttag(opt.shorttag);
            sd.setBooleanFeature(Sd.BooleanFeature.fEMPTYNRM, opt.emptynrm);
            sd.setNumberFeature(Sd.NumberFeature.fSIMPLE, opt.linkSimple);
            sd.setBooleanFeature(Sd.BooleanFeature.fIMPLICIT, opt.linkImplicit);
            sd.setNumberFeature(Sd.NumberFeature.fEXPLICIT, opt.linkExplicit);
            sd.setNumberFeature(Sd.NumberFeature.fCONCUR, opt.concur);
            sd.setNumberFeature(Sd.NumberFeature.fSUBDOC, opt.subdoc);
            sd.setBooleanFeature(Sd.BooleanFeature.fFORMAL, opt.formal);
            setSdOverrides(sd);
            PublicId publicId = new PublicId();
            CharsetDecl docCharsetDecl = new CharsetDecl();
            docCharsetDecl.addSection(publicId);
            docCharsetDecl.addRange(0, Constant.charMax > 99999999 ? 99999999 : Constant.charMax + 1, 0);
            sd.setDocCharsetDecl(docCharsetDecl);
            setSd(new ConstPtr<Sd>(sd));
        }
        else if (@params.sd.isNull())
        {
            setSd(parent!.sdPointer());
            setSyntaxes(parent.prologSyntaxPointer(), parent.instanceSyntaxPointer());
        }
        else
        {
            setSd(@params.sd);
            setSyntaxes(@params.prologSyntax, @params.instanceSyntax);
        }

        // Make catalog
        StringC sysid = new StringC(@params.sysid);
        ConstPtr<EntityCatalog> catalog = entityManager().makeCatalog(sysid,
                                                                       sd().docCharset(),
                                                                       messenger());
        if (!catalog.isNull())
            setEntityCatalog(catalog);
        else if (parent != null)
            setEntityCatalog(parent.entityCatalogPtr());
        else
        {
            allDone();
            return;
        }

        // Set up the input stack.
        if (sysid.size() == 0)
        {
            allDone();
            return;
        }

        Ptr<InputSourceOrigin> origin;
        if (@params.origin.isNull())
            origin = new Ptr<InputSourceOrigin>(InputSourceOrigin.make());
        else
            origin = @params.origin;

        pushInput(entityManager().open(sysid,
                                        sd().docCharset(),
                                        origin.pointer(),
                                        EntityManager.mayRewind | EntityManager.maySetDocCharset,
                                        messenger()));

        if (inputLevel() == 0)
        {
            allDone();
            return;
        }

        switch (@params.entityType)
        {
            case SgmlParser.Params.EntityType.document:
                setPhase(Phase.initPhase);
                break;
            case SgmlParser.Params.EntityType.subdoc:
                if (@params.subdocInheritActiveLinkTypes && parent != null)
                    inheritActiveLinkTypes(parent);
                if (subdocLevel() == sd().subdoc() + 1)
                    message(ParserMessages.subdocLevel, new NumberMessageArg(sd().subdoc()));
                if (sd().www())
                    setPhase(Phase.initPhase);
                else
                {
                    setPhase(Phase.prologPhase);
                    compilePrologModes();
                }
                break;
            case SgmlParser.Params.EntityType.dtd:
                compilePrologModes();
                startDtd(@params.doctypeName);
                setPhase(Phase.declSubsetPhase);
                break;
        }
    }

    // void setSdOverrides(Sd &sd);
    private void setSdOverrides(Sd sd)
    {
        // FIXME overriding behaviour when using multiple -w options
        if (options().typeValid != ParserOptions.sgmlDeclTypeValid)
        {
            sd.setTypeValid(options().typeValid != 0);
            sd.setBooleanFeature(Sd.BooleanFeature.fIMPLYDEFATTLIST, options().typeValid == 0);
            sd.setImplydefElement(options().typeValid != 0
                                  ? Sd.ImplydefElement.implydefElementNo
                                  : Sd.ImplydefElement.implydefElementYes);
            sd.setBooleanFeature(Sd.BooleanFeature.fIMPLYDEFENTITY, options().typeValid == 0);
            sd.setBooleanFeature(Sd.BooleanFeature.fIMPLYDEFNOTATION, options().typeValid == 0);
        }
        if (options().fullyDeclared)
        {
            sd.setBooleanFeature(Sd.BooleanFeature.fIMPLYDEFATTLIST, false);
            sd.setImplydefElement(Sd.ImplydefElement.implydefElementNo);
            sd.setBooleanFeature(Sd.BooleanFeature.fIMPLYDEFENTITY, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fIMPLYDEFNOTATION, false);
        }
        if (options().fullyTagged)
        {
            sd.setBooleanFeature(Sd.BooleanFeature.fDATATAG, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fRANK, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fOMITTAG, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fSTARTTAGEMPTY, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fATTRIBOMITNAME, false);
        }
        if (options().amplyTagged)
        {
            sd.setBooleanFeature(Sd.BooleanFeature.fDATATAG, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fRANK, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fOMITTAG, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fATTRIBOMITNAME, false);
            sd.setImplydefElement(Sd.ImplydefElement.implydefElementYes);
        }
        if (options().amplyTaggedAnyother)
        {
            sd.setBooleanFeature(Sd.BooleanFeature.fDATATAG, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fRANK, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fOMITTAG, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fATTRIBOMITNAME, false);
            sd.setImplydefElement(Sd.ImplydefElement.implydefElementAnyother);
        }
        if (options().valid)
        {
            sd.setTypeValid(true);
        }
        if (options().entityRef)
        {
            sd.setEntityRef(Sd.EntityRef.entityRefNone);
        }
        if (options().externalEntityRef)
        {
            sd.setEntityRef(Sd.EntityRef.entityRefInternal);
        }
        if (options().integral)
        {
            sd.setIntegrallyStored(true);
        }
        if (options().noUnclosedTag)
        {
            sd.setBooleanFeature(Sd.BooleanFeature.fSTARTTAGUNCLOSED, false);
            sd.setBooleanFeature(Sd.BooleanFeature.fENDTAGUNCLOSED, false);
        }
        if (options().noNet)
            sd.setStartTagNetEnable(Sd.NetEnable.netEnableNo);
    }

    // void giveUp();
    private void giveUp()
    {
        if (subdocLevel() > 0)  // FIXME might be subdoc if level == 0
            message(ParserMessages.subdocGiveUp);
        else
            message(ParserMessages.giveUp);
        allDone();
    }

    // static unsigned paramsSubdocLevel(const SgmlParser::Params &params);
    private static uint paramsSubdocLevel(SgmlParser.Params @params)
    {
        if (@params.parent == null)
            return 0;
        uint n = @params.parent.parser_!.subdocLevel();
        if (@params.subdocReferenced)
            return n + 1;
        else
            return n;
    }

    // Event *nextEvent();
    public Event? nextEvent()
    {
        while (eventQueueEmpty())
        {
            switch (phase())
            {
                case Phase.noPhase:
                    return null;
                case Phase.initPhase:
                    doInit();
                    break;
                case Phase.prologPhase:
                    doProlog();
                    break;
                case Phase.declSubsetPhase:
                    doDeclSubset();
                    break;
                case Phase.instanceStartPhase:
                    doInstanceStart();
                    break;
                case Phase.contentPhase:
                    doContent();
                    break;
            }
        }
        return eventQueueGet();
    }

    // void parseAll(EventHandler &handler, const volatile sig_atomic_t *cancelPtr);
    public void parseAll(EventHandler handler, int cancelPtr)
    {
        while (!eventQueueEmpty())
            eventQueueGet()?.handle(handler);
        // FIXME catch exceptions and reset handler.
        setHandler(handler, cancelPtr);
        for (;;)
        {
            switch (phase())
            {
                case Phase.noPhase:
                    unsetHandler();
                    return;
                case Phase.initPhase:
                    doInit();
                    break;
                case Phase.prologPhase:
                    doProlog();
                    break;
                case Phase.declSubsetPhase:
                    doDeclSubset();
                    break;
                case Phase.instanceStartPhase:
                    doInstanceStart();
                    break;
                case Phase.contentPhase:
                    doContent();
                    break;
            }
        }
    }

    // The following methods are defined in separate files in C++.
    // They will be implemented as the port progresses.

    // From parseMode.cxx
    protected virtual void compileSdModes() { throw new NotImplementedException(); }
    protected virtual void compilePrologModes() { throw new NotImplementedException(); }
    protected virtual void compileInstanceModes() { throw new NotImplementedException(); }
    protected virtual void compileModes(Mode[] modes, int n, Dtd? dtd) { throw new NotImplementedException(); }
    protected virtual void compileNormalMap() { throw new NotImplementedException(); }
    protected virtual void addNeededShortrefs(Dtd dtd, Syntax syntax) { throw new NotImplementedException(); }
    protected virtual Boolean shortrefCanPreemptDelim(StringC sr, StringC d, Boolean dIsSr, Syntax syntax) { throw new NotImplementedException(); }

    // From parseCommon.cxx
    protected virtual void doInit() { throw new NotImplementedException(); }
    protected virtual void doProlog() { throw new NotImplementedException(); }
    protected virtual void doDeclSubset() { throw new NotImplementedException(); }
    protected virtual void doInstanceStart() { throw new NotImplementedException(); }
    protected virtual void doContent() { throw new NotImplementedException(); }

    // void extendNameToken(size_t maxLength, const MessageType1 &tooLongMessage);
    protected void extendNameToken(nuint maxLength, MessageType1 tooLongMessage)
    {
        InputSource? ins = currentInput();
        if (ins == null) return;
        nuint length = ins.currentTokenLength();
        Syntax syn = syntax();
        while (syn.isNameCharacter(ins.tokenChar(messenger())))
            length++;
        if (length > maxLength)
            message(tooLongMessage, new NumberMessageArg(maxLength));
        ins.endToken(length);
    }

    // void extendNumber(size_t maxLength, const MessageType1 &tooLongMessage);
    protected void extendNumber(nuint maxLength, MessageType1 tooLongMessage)
    {
        InputSource? ins = currentInput();
        if (ins == null) return;
        nuint length = ins.currentTokenLength();
        while (syntax().isDigit(ins.tokenChar(messenger())))
            length++;
        if (length > maxLength)
            message(tooLongMessage, new NumberMessageArg(maxLength));
        ins.endToken(length);
    }

    // void extendHexNumber();
    protected void extendHexNumber()
    {
        InputSource? ins = currentInput();
        if (ins == null) return;
        nuint length = ins.currentTokenLength();
        while (syntax().isHexDigit(ins.tokenChar(messenger())))
            length++;
        if (length > syntax().namelen())
            message(ParserMessages.hexNumberLength, new NumberMessageArg(syntax().namelen()));
        ins.endToken(length);
    }

    protected virtual void extendData() { throw new NotImplementedException(); }

    // void extendS();
    protected void extendS()
    {
        InputSource? ins = currentInput();
        if (ins == null) return;
        nuint length = ins.currentTokenLength();
        while (syntax().isS(ins.tokenChar(messenger())))
            length++;
        ins.endToken(length);
    }

    protected virtual void extendContentS() { throw new NotImplementedException(); }

    // Boolean reportNonSgmlCharacter();
    protected Boolean reportNonSgmlCharacter()
    {
        InputSource? ins = currentInput();
        if (ins == null) return false;
        // In scanSuppress mode the non-SGML character will have been read.
        Xchar c = ins.currentTokenLength() > 0 ? (Xchar)currentChar() : getChar();
        if (!syntax().isSgmlChar(c))
        {
            message(ParserMessages.nonSgmlCharacter, new NumberMessageArg((Char)c));
            return true;
        }
        return false;
    }

    // Boolean parseComment(Mode mode);
    protected Boolean parseComment(Mode mode)
    {
        Location startLoc = currentLocation();
        Markup? markup = currentMarkup();
        if (markup != null)
            markup.addCommentStart();
        Token token;
        while ((token = getToken(mode)) != Tokens.tokenCom)
        {
            switch (token)
            {
                case Tokens.tokenUnrecognized:
                    if (!reportNonSgmlCharacter())
                        message(ParserMessages.sdCommentSignificant,
                                new StringMessageArg(currentToken()));
                    break;
                case Tokens.tokenEe:
                    message(ParserMessages.commentEntityEnd, startLoc);
                    return false;
                default:
                    if (markup != null)
                        markup.addCommentChar(currentChar());
                    break;
            }
        }
        return true;
    }

    // From parseDecl.cxx
    protected virtual void declSubsetRecover(uint startLevel) { throw new NotImplementedException(); }
    protected virtual void prologRecover() { throw new NotImplementedException(); }
    protected virtual void skipDeclaration(uint startLevel) { throw new NotImplementedException(); }
    protected virtual Boolean parseElementDecl() { throw new NotImplementedException(); }
    protected virtual Boolean parseAttlistDecl() { throw new NotImplementedException(); }
    protected virtual Boolean parseNotationDecl() { throw new NotImplementedException(); }
    protected virtual Boolean parseEntityDecl() { throw new NotImplementedException(); }
    protected virtual Boolean parseShortrefDecl() { throw new NotImplementedException(); }
    protected virtual Boolean parseUsemapDecl() { throw new NotImplementedException(); }
    protected virtual Boolean parseUselinkDecl() { throw new NotImplementedException(); }
    protected virtual Boolean parseDoctypeDeclStart() { throw new NotImplementedException(); }
    protected virtual Boolean parseDoctypeDeclEnd(Boolean fake = false) { throw new NotImplementedException(); }
    protected virtual Boolean parseMarkedSectionDeclStart() { throw new NotImplementedException(); }
    protected virtual void handleMarkedSectionEnd() { throw new NotImplementedException(); }
    // Boolean parseCommentDecl();
    protected Boolean parseCommentDecl()
    {
        if (startMarkup(inInstance()
                        ? eventsWanted().wantCommentDecls()
                        : eventsWanted().wantPrologMarkup(),
                        currentLocation()) != null)
            currentMarkup()!.addDelim(Syntax.DelimGeneral.dMDO);
        if (!parseComment(Mode.comMode))
            return false;
        for (;;)
        {
            Token token = getToken(Mode.mdMode);
            switch (token)
            {
                case Tokens.tokenS:
                    if (currentMarkup() != null)
                        currentMarkup()!.addS(currentChar());
                    if (options().warnCommentDeclS)
                        message(ParserMessages.commentDeclS);
                    break;
                case Tokens.tokenCom:
                    if (!parseComment(Mode.comMode))
                        return false;
                    if (options().warnCommentDeclMultiple)
                        message(ParserMessages.commentDeclMultiple);
                    break;
                case Tokens.tokenMdc:
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dMDC);
                    goto done;
                case Tokens.tokenEe:
                    message(ParserMessages.declarationLevel);
                    return false;
                case Tokens.tokenUnrecognized:
                    if (reportNonSgmlCharacter())
                        break;
                    message(ParserMessages.commentDeclarationCharacter,
                            new StringMessageArg(currentToken()),
                            markupLocation());
                    return false;
                default:
                    message(ParserMessages.commentDeclInvalidToken,
                            new TokenMessageArg(token, Mode.mdMode, syntaxPointer(), sdPointer()),
                            markupLocation());
                    return false;
            }
        }
    done:
        if (currentMarkup() != null)
            eventHandler().commentDecl(new CommentDeclEvent(markupLocation(), currentMarkup()));
        return true;
    }

    // void emptyCommentDecl();
    protected void emptyCommentDecl()
    {
        if (startMarkup(eventsWanted().wantCommentDecls(), currentLocation()) != null)
        {
            currentMarkup()!.addDelim(Syntax.DelimGeneral.dMDO);
            currentMarkup()!.addDelim(Syntax.DelimGeneral.dMDC);
            eventHandler().commentDecl(new CommentDeclEvent(markupLocation(), currentMarkup()));
        }
        if (options().warnEmptyCommentDecl)
            message(ParserMessages.emptyCommentDecl);
    }
    protected virtual Boolean parseLinktypeDeclStart() { throw new NotImplementedException(); }
    protected virtual Boolean parseLinktypeDeclEnd() { throw new NotImplementedException(); }
    protected virtual Boolean parseLinkDecl() { throw new NotImplementedException(); }
    protected virtual Boolean parseIdlinkDecl() { throw new NotImplementedException(); }
    protected virtual Boolean parseLinkSet(Boolean idlink) { throw new NotImplementedException(); }
    protected virtual Boolean parseAfdrDecl() { throw new NotImplementedException(); }

    // From parseParam.cxx
    protected virtual Boolean parseParam(AllowedParams allow, uint tok, Param parm) { throw new NotImplementedException(); }
    protected virtual Boolean parseExternalId(AllowedParams systemIdAllow, AllowedParams publicIdAllow,
                                               Boolean optional, uint tok, Param parm, ExternalId id) { throw new NotImplementedException(); }
    protected virtual Boolean parseMinimumLiteral(Boolean lita, Text text) { throw new NotImplementedException(); }
    protected virtual Boolean parseAttributeValueLiteral(Boolean lita, Text text) { throw new NotImplementedException(); }
    protected virtual Boolean parseTokenizedAttributeValueLiteral(Boolean lita, Text text) { throw new NotImplementedException(); }
    protected virtual Boolean parseSystemIdentifier(Boolean lita, Text text) { throw new NotImplementedException(); }
    protected virtual Boolean parseParameterLiteral(Boolean lita, Text text) { throw new NotImplementedException(); }
    protected virtual Boolean parseDataTagParameterLiteral(Boolean lita, Text text) { throw new NotImplementedException(); }
    protected virtual Boolean parseLiteral(Mode litMode, Mode liteMode, nuint maxLength,
                                            MessageType1 tooLongMessage, uint flags, Text text) { throw new NotImplementedException(); }

    // Character reference parsing stubs - to be implemented
    protected virtual Boolean parseNumericCharRef(Boolean isHex, ref Char ch, ref Location loc) { throw new NotImplementedException(); }
    protected virtual Boolean translateNumericCharRef(ref Char ch, ref Boolean isSgmlChar) { throw new NotImplementedException(); }
    protected virtual Boolean parseNamedCharRef() { throw new NotImplementedException(); }
    protected virtual Boolean parseEntityReference(Boolean isParameter, int ignoreLevel,
                                                   ConstPtr<Entity> entity, Ptr<EntityOrigin> origin) { throw new NotImplementedException(); }
    protected virtual Boolean parseEntityReferenceNameGroup(ref Boolean ignore) { throw new NotImplementedException(); }

    // From parseInstance.cxx
    protected virtual void parsePcdata() { throw new NotImplementedException(); }
    protected virtual void parseStartTag() { throw new NotImplementedException(); }
    protected virtual void parseEmptyStartTag() { throw new NotImplementedException(); }
    protected virtual EndElementEvent? parseEndTag() { throw new NotImplementedException(); }
    protected virtual void parseEndTagClose() { throw new NotImplementedException(); }
    protected virtual void parseEmptyEndTag() { throw new NotImplementedException(); }
    protected virtual void parseNullEndTag() { throw new NotImplementedException(); }
    protected virtual void endAllElements() { throw new NotImplementedException(); }

    // Boolean parseProcessingInstruction();
    protected Boolean parseProcessingInstruction()
    {
        InputSource? ins = currentInput();
        if (ins == null) return false;
        ins.startToken();
        Location location = currentLocation();
        StringC buf = new StringC();
        for (;;)
        {
            Token token = getToken(Mode.piMode);
            if (token == Tokens.tokenPic)
                break;
            switch (token)
            {
                case Tokens.tokenEe:
                    message(ParserMessages.processingInstructionEntityEnd);
                    return false;
                case Tokens.tokenUnrecognized:
                    reportNonSgmlCharacter();
                    goto case Tokens.tokenChar;
                case Tokens.tokenChar:
                    Char[]? start = ins.currentTokenStart();
                    if (start != null)
                        buf.operatorPlusAssign(start[0]);
                    if (buf.size() / 2 > syntax().pilen())
                    {
                        message(ParserMessages.processingInstructionLength,
                                new NumberMessageArg(syntax().pilen()));
                        message(ParserMessages.processingInstructionClose);
                        return false;
                    }
                    break;
            }
        }
        if (buf.size() > syntax().pilen())
            message(ParserMessages.processingInstructionLength,
                    new NumberMessageArg(syntax().pilen()));
        if (options().warnPiMissingName)
        {
            nuint i = 0;
            if (buf.size() > 0 && syntax().isNameStartCharacter((Xchar)buf[0]))
            {
                for (i = 1; i < buf.size(); i++)
                    if (!syntax().isNameCharacter((Xchar)buf[i]))
                        break;
            }
            if (i == 0 || (i < buf.size() && !syntax().isS((Xchar)buf[i])))
                message(ParserMessages.piMissingName);
        }
        noteMarkup();
        eventHandler().pi(new ImmediatePiEvent(buf, location));
        return true;
    }

    protected virtual void handleShortref(int index) { throw new NotImplementedException(); }
    protected virtual void endInstance() { throw new NotImplementedException(); }
    protected virtual void checkIdrefs() { throw new NotImplementedException(); }
    protected virtual void checkTaglen(Index tagStartIndex) { throw new NotImplementedException(); }
    protected virtual void endProlog() { throw new NotImplementedException(); }

    // From parseAttribute.cxx
    protected virtual Boolean parseAttributeSpec(Mode mode, AttributeList atts, out Boolean netEnabling,
                                                  Ptr<AttributeDefinitionList> newAttDefList)
    { throw new NotImplementedException(); }
    protected virtual Boolean handleAttributeNameToken(Text text, AttributeList atts, ref uint specLength) { throw new NotImplementedException(); }

    // From parseSd.cxx
    protected virtual Boolean implySgmlDecl() { throw new NotImplementedException(); }
    protected virtual Boolean scanForSgmlDecl(CharsetInfo initCharset) { throw new NotImplementedException(); }
    protected virtual Boolean parseSgmlDecl() { throw new NotImplementedException(); }
}
