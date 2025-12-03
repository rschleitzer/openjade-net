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
    private const uint modeUsedInSd = 01;
    private const uint modeUsedInProlog = 02;
    private const uint modeUsedInInstance = 04;
    private const uint modeUsesSr = 010;

    private static readonly (Mode mode, uint flags)[] modeTable_ = new[]
    {
        (Mode.grpMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.alitMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.alitaMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.aliteMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.talitMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.talitaMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.taliteMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.mdMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.mdMinusMode, modeUsedInProlog),
        (Mode.mdPeroMode, modeUsedInProlog),
        (Mode.sdMode, modeUsedInSd),
        (Mode.comMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.sdcomMode, modeUsedInSd),
        (Mode.piMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.refMode, modeUsedInProlog | modeUsedInInstance | modeUsedInSd),
        (Mode.imsMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.cmsMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.rcmsMode, modeUsedInProlog | modeUsedInInstance),
        (Mode.proMode, modeUsedInProlog),
        (Mode.dsMode, modeUsedInProlog),
        (Mode.dsiMode, modeUsedInProlog),
        (Mode.plitMode, modeUsedInProlog),
        (Mode.plitaMode, modeUsedInProlog),
        (Mode.pliteMode, modeUsedInProlog),
        (Mode.sdplitMode, modeUsedInSd),
        (Mode.sdplitaMode, modeUsedInSd),
        (Mode.grpsufMode, modeUsedInProlog),
        (Mode.mlitMode, modeUsedInProlog | modeUsedInSd),
        (Mode.mlitaMode, modeUsedInProlog | modeUsedInSd),
        (Mode.asMode, modeUsedInProlog),
        (Mode.piPasMode, modeUsedInProlog),
        (Mode.slitMode, modeUsedInProlog),
        (Mode.slitaMode, modeUsedInProlog),
        (Mode.sdslitMode, modeUsedInSd),
        (Mode.sdslitaMode, modeUsedInSd),
        (Mode.cconMode, modeUsedInInstance),
        (Mode.rcconMode, modeUsedInInstance),
        (Mode.cconnetMode, modeUsedInInstance),
        (Mode.rcconnetMode, modeUsedInInstance),
        (Mode.rcconeMode, modeUsedInInstance),
        (Mode.tagMode, modeUsedInInstance),
        (Mode.econMode, modeUsedInInstance | modeUsesSr),
        (Mode.mconMode, modeUsedInInstance | modeUsesSr),
        (Mode.econnetMode, modeUsedInInstance | modeUsesSr),
        (Mode.mconnetMode, modeUsedInInstance | modeUsesSr),
    };

    // void compileSdModes();
    protected void compileSdModes()
    {
        Mode[] modes = new Mode[ModeConstants.nModes];
        int n = 0;
        for (int i = 0; i < modeTable_.Length; i++)
            if ((modeTable_[i].flags & modeUsedInSd) != 0)
                modes[n++] = modeTable_[i].mode;
        compileModes(modes, n, null);
    }

    // void compilePrologModes();
    protected void compilePrologModes()
    {
        Boolean scopeInstance = sd().scopeInstance();
        Boolean haveSr = syntax().hasShortrefs();
        Mode[] modes = new Mode[ModeConstants.nModes];
        int n = 0;
        for (int i = 0; i < modeTable_.Length; i++)
        {
            if (scopeInstance)
            {
                if ((modeTable_[i].flags & modeUsedInProlog) != 0)
                    modes[n++] = modeTable_[i].mode;
            }
            else if (haveSr)
            {
                if ((modeTable_[i].flags & (modeUsedInInstance | modeUsedInProlog)) != 0
                    && (modeTable_[i].flags & modeUsesSr) == 0)
                    modes[n++] = modeTable_[i].mode;
            }
            else
            {
                if ((modeTable_[i].flags & (modeUsedInInstance | modeUsedInProlog)) != 0)
                    modes[n++] = modeTable_[i].mode;
            }
        }
        compileModes(modes, n, null);
    }

    // void compileInstanceModes();
    protected void compileInstanceModes()
    {
        Boolean scopeInstance = sd().scopeInstance();
        compileNormalMap();
        if (!scopeInstance && !syntax().hasShortrefs())
            return;
        Mode[] modes = new Mode[ModeConstants.nModes];
        int n = 0;
        for (int i = 0; i < modeTable_.Length; i++)
        {
            if (scopeInstance)
            {
                if ((modeTable_[i].flags & modeUsedInInstance) != 0)
                    modes[n++] = modeTable_[i].mode;
            }
            else
            {
                if ((modeTable_[i].flags & modeUsesSr) != 0)
                    modes[n++] = modeTable_[i].mode;
            }
        }
        compileModes(modes, n, currentDtdPointer().pointer());
    }

    // void compileModes(const Mode *modes, int n, const Dtd *dtd);
    protected void compileModes(Mode[] modes, int n, Dtd? dtd)
    {
        PackedBoolean[] sets = new PackedBoolean[Syntax.nSet];
        PackedBoolean[] delims = new PackedBoolean[Syntax.nDelimGeneral];
        PackedBoolean[] functions = new PackedBoolean[3];
        int i;
        Boolean includesShortref = false;

        for (i = 0; i < Syntax.nSet; i++)
            sets[i] = false;
        for (i = 0; i < Syntax.nDelimGeneral; i++)
            delims[i] = false;
        for (i = 0; i < 3; i++)
            functions[i] = false;

        for (i = 0; i < n; i++)
        {
            ModeInfo iter = new ModeInfo(modes[i], sd());
            TokenInfo ti = new TokenInfo();
            while (iter.nextToken(ti))
            {
                switch (ti.type)
                {
                    case TokenInfo.Type.delimType:
                        delims[(int)ti.delim1] = true;
                        break;
                    case TokenInfo.Type.delimDelimType:
                        delims[(int)ti.delim1] = true;
                        delims[(int)ti.delim2] = true;
                        break;
                    case TokenInfo.Type.delimSetType:
                        delims[(int)ti.delim1] = true;
                        sets[(int)ti.set] = true;
                        break;
                    case TokenInfo.Type.setType:
                        sets[(int)ti.set] = true;
                        break;
                    case TokenInfo.Type.functionType:
                        functions[(int)ti.function] = true;
                        break;
                }
            }
            if (!includesShortref && iter.includesShortref())
                includesShortref = true;
        }

        ISet<Char> chars = new ISet<Char>();

        for (i = 0; i < 3; i++)
            if (functions[i])
                chars.add(syntax().standardFunction(i));
        for (i = 0; i < Syntax.nDelimGeneral; i++)
            if (delims[i])
            {
                StringC str = syntax().delimGeneral(i);
                for (nuint j = 0; j < str.size(); j++)
                    chars.add(str[j]);
            }
        if (includesShortref && dtd != null)
        {
            nuint nsr = dtd.nShortref();
            for (nuint si = 0; si < nsr; si++)
            {
                StringC delim = dtd.shortref(si);
                nuint len = delim.size();
                for (nuint j = 0; j < len; j++)
                    if (delim[j] == sd().execToInternal((sbyte)'B'))
                        sets[(int)Syntax.Set.blank] = true;
                    else
                        chars.add(delim[j]);
            }
        }

        ISet<Char>[] csets = new ISet<Char>[Syntax.nSet];
        int usedSets = 0;
        for (i = 0; i < Syntax.nSet; i++)
            if (sets[i])
                csets[usedSets++] = syntax().charSet(i)!;

        Partition partition = new Partition(chars, csets, usedSets, syntax().generalSubstTable()!);

        String<EquivCode>[] setCodes = new String<EquivCode>[Syntax.nSet];
        for (i = 0; i < Syntax.nSet; i++)
            setCodes[i] = new String<EquivCode>();

        int nCodes = 0;
        for (i = 0; i < Syntax.nSet; i++)
            if (sets[i])
                setCodes[i] = partition.setCodes(nCodes++);

        String<EquivCode>[] delimCodes = new String<EquivCode>[Syntax.nDelimGeneral];
        for (i = 0; i < Syntax.nDelimGeneral; i++)
        {
            delimCodes[i] = new String<EquivCode>();
            if (delims[i])
            {
                StringC str = syntax().delimGeneral(i);
                for (nuint j = 0; j < str.size(); j++)
                    delimCodes[i].operatorPlusAssign(partition.charCode(str[j]));
            }
        }

        String<EquivCode>[] functionCode = new String<EquivCode>[3];
        for (i = 0; i < 3; i++)
        {
            functionCode[i] = new String<EquivCode>();
            if (functions[i])
                functionCode[i].operatorPlusAssign(partition.charCode(syntax().standardFunction(i)));
        }

        Vector<SrInfo> srInfo = new Vector<SrInfo>();
        int nShortref;
        if (!includesShortref || dtd == null)
            nShortref = 0;
        else
        {
            nShortref = (int)dtd.nShortref();
            srInfo.resize((nuint)nShortref);

            for (i = 0; i < nShortref; i++)
            {
                StringC delim = dtd.shortref((nuint)i);
                SrInfo p = srInfo[(nuint)i];
                nuint j;
                for (j = 0; j < delim.size(); j++)
                {
                    if (delim[j] == sd().execToInternal((sbyte)'B'))
                        break;
                    p.chars.operatorPlusAssign(partition.charCode(delim[j]));
                }
                if (j < delim.size())
                {
                    p.bSequenceLength = 1;
                    for (++j; j < delim.size(); j++)
                    {
                        if (delim[j] != sd().execToInternal((sbyte)'B'))
                            break;
                        p.bSequenceLength += 1;
                    }
                    for (; j < delim.size(); j++)
                        p.chars2.operatorPlusAssign(partition.charCode(delim[j]));
                }
                else
                    p.bSequenceLength = 0;
            }
        }

        String<EquivCode> emptyString = new String<EquivCode>();
        Boolean multicode = syntax().multicode();
        for (i = 0; i < n; i++)
        {
            TrieBuilder tb = new TrieBuilder((int)partition.maxCode() + 1);
            Vector<Token> ambiguities = new Vector<Token>();
            Vector<Token> suppressTokens = new Vector<Token>();
            if (multicode)
            {
                suppressTokens.assign((nuint)(partition.maxCode() + 1), 0);
                suppressTokens[(nuint)partition.eECode()] = Tokens.tokenEe;
            }
            tb.recognizeEE(partition.eECode(), Tokens.tokenEe);
            ModeInfo iter = new ModeInfo(modes[i], sd());
            TokenInfo ti = new TokenInfo();
            while (iter.nextToken(ti))
            {
                switch (ti.type)
                {
                    case TokenInfo.Type.delimType:
                        if (delimCodes[(int)ti.delim1].size() > 0)
                            tb.recognize(delimCodes[(int)ti.delim1], ti.token, ti.priority, ambiguities);
                        break;
                    case TokenInfo.Type.delimDelimType:
                        {
                            String<EquivCode> str = new String<EquivCode>(delimCodes[(int)ti.delim1]);
                            if (str.size() > 0 && delimCodes[(int)ti.delim2].size() > 0)
                            {
                                str.operatorPlusAssign(delimCodes[(int)ti.delim2]);
                                tb.recognize(str, ti.token, ti.priority, ambiguities);
                            }
                        }
                        break;
                    case TokenInfo.Type.delimSetType:
                        if (delimCodes[(int)ti.delim1].size() > 0)
                            tb.recognize(delimCodes[(int)ti.delim1], setCodes[(int)ti.set], ti.token, ti.priority, ambiguities);
                        break;
                    case TokenInfo.Type.setType:
                        tb.recognize(emptyString, setCodes[(int)ti.set], ti.token, ti.priority, ambiguities);
                        if (multicode)
                        {
                            String<EquivCode> equivCodes = setCodes[(int)ti.set];
                            for (nuint j = 0; j < equivCodes.size(); j++)
                                suppressTokens[(nuint)equivCodes[j]] = ti.token;
                        }
                        break;
                    case TokenInfo.Type.functionType:
                        tb.recognize(functionCode[(int)ti.function], ti.token, ti.priority, ambiguities);
                        if (multicode)
                            suppressTokens[(nuint)functionCode[(int)ti.function][0]] = ti.token;
                        break;
                }
            }
            if (iter.includesShortref())
            {
                for (int j = 0; j < nShortref; j++)
                {
                    SrInfo p = srInfo[(nuint)j];
                    if (p.bSequenceLength > 0)
                        tb.recognizeB(p.chars, p.bSequenceLength,
                                      syntax().quantity(Syntax.Quantity.qBSEQLEN),
                                      setCodes[(int)Syntax.Set.blank],
                                      p.chars2, Tokens.tokenFirstShortref + (uint)j,
                                      ambiguities);
                    else
                        tb.recognize(p.chars, Tokens.tokenFirstShortref + (uint)j,
                                     Priority.delim, ambiguities);
                }
            }
            setRecognizer(modes[i],
                          multicode
                          ? new ConstPtr<Recognizer>(new Recognizer(tb.extractTrie()!, partition.map(), suppressTokens))
                          : new ConstPtr<Recognizer>(new Recognizer(tb.extractTrie()!, partition.map())));
            for (nuint j = 0; j < ambiguities.size(); j += 2)
                message(ParserMessages.lexicalAmbiguity,
                        new TokenMessageArg(ambiguities[j], modes[i], syntaxPointer(), sdPointer()),
                        new TokenMessageArg(ambiguities[j + 1], modes[i], syntaxPointer(), sdPointer()));
        }
    }

    // void compileNormalMap();
    protected void compileNormalMap()
    {
        XcharMap<PackedBoolean> map = new XcharMap<PackedBoolean>(false);
        ISetIter<Char> sgmlCharIter = new ISetIter<Char>(syntax().charSet((int)Syntax.Set.sgmlChar)!);
        Char min, max;
        while (sgmlCharIter.next(out min, out max) != 0)
            map.setRange(min, max, true);
        ModeInfo iter = new ModeInfo(Mode.mconnetMode, sd());
        TokenInfo ti = new TokenInfo();
        while (iter.nextToken(ti))
        {
            switch (ti.type)
            {
                case TokenInfo.Type.delimType:
                case TokenInfo.Type.delimDelimType:
                case TokenInfo.Type.delimSetType:
                    {
                        StringC delim = syntax().delimGeneral((int)ti.delim1);
                        if (delim.size() == 0)
                            break;
                        Char c = delim[0];
                        map.setChar(c, false);
                        StringC str = syntax().generalSubstTable()!.inverse(c);
                        for (nuint i = 0; i < str.size(); i++)
                            map.setChar(str[i], false);
                    }
                    break;
                case TokenInfo.Type.setType:
                    if (ti.token != Tokens.tokenChar)
                    {
                        ISetIter<Char> setIter = new ISetIter<Char>(syntax().charSet((int)ti.set)!);
                        while (setIter.next(out min, out max) != 0)
                            map.setRange(min, max, false);
                    }
                    break;
                case TokenInfo.Type.functionType:
                    if (ti.token != Tokens.tokenChar)
                        map.setChar(syntax().standardFunction((int)ti.function), false);
                    break;
            }
        }
        int nShortref = (int)currentDtd().nShortref();
        for (int i = 0; i < nShortref; i++)
        {
            Char c = currentDtd().shortref((nuint)i)[0];
            if (c == sd().execToInternal((sbyte)'B'))
            {
                ISetIter<Char> setIter = new ISetIter<Char>(syntax().charSet((int)Syntax.Set.blank)!);
                while (setIter.next(out min, out max) != 0)
                    map.setRange(min, max, false);
            }
            else
            {
                map.setChar(c, false);
                StringC str = syntax().generalSubstTable()!.inverse(c);
                for (nuint j = 0; j < str.size(); j++)
                    map.setChar(str[j], false);
            }
        }
        setNormalMap(map);
    }

    // void addNeededShortrefs(Dtd &dtd, const Syntax &syntax);
    protected void addNeededShortrefs(Dtd dtd, Syntax syn)
    {
        if (!syn.hasShortrefs())
            return;
        PackedBoolean[] delimRelevant = new PackedBoolean[Syntax.nDelimGeneral];
        nuint i;
        for (i = 0; i < (nuint)Syntax.nDelimGeneral; i++)
            delimRelevant[i] = false;
        ModeInfo iter = new ModeInfo(Mode.mconnetMode, sd());
        TokenInfo ti = new TokenInfo();
        while (iter.nextToken(ti))
        {
            switch (ti.type)
            {
                case TokenInfo.Type.delimType:
                case TokenInfo.Type.delimDelimType:
                case TokenInfo.Type.delimSetType:
                    delimRelevant[(int)ti.delim1] = true;
                    break;
                default:
                    break;
            }
        }

        if (syn.isValidShortref(syn.delimGeneral((int)Syntax.DelimGeneral.dPIO)))
            dtd.addNeededShortref(syn.delimGeneral((int)Syntax.DelimGeneral.dPIO));
        if (syn.isValidShortref(syn.delimGeneral((int)Syntax.DelimGeneral.dNET)))
            dtd.addNeededShortref(syn.delimGeneral((int)Syntax.DelimGeneral.dNET));

        nuint nShortrefComplex = (nuint)syn.nDelimShortrefComplex();

        for (i = 0; i < nShortrefComplex; i++)
        {
            nuint j;
            for (j = 0; j < (nuint)Syntax.nDelimGeneral; j++)
                if (delimRelevant[j]
                    && shortrefCanPreemptDelim(syn.delimShortrefComplex(i),
                                               syn.delimGeneral((int)j),
                                               false,
                                               syn))
                {
                    dtd.addNeededShortref(syn.delimShortrefComplex(i));
                    break;
                }
            for (j = 0; j < dtd.nShortref(); j++)
                if (shortrefCanPreemptDelim(syn.delimShortrefComplex(i),
                                            dtd.shortref(j),
                                            true,
                                            syn))
                {
                    dtd.addNeededShortref(syn.delimShortrefComplex(i));
                    break;
                }
        }
    }

    // Boolean shortrefCanPreemptDelim(const StringC &sr, const StringC &d, Boolean dIsSr, const Syntax &syntax);
    protected Boolean shortrefCanPreemptDelim(StringC sr, StringC d, Boolean dIsSr, Syntax syn)
    {
        Char letterB = sd().execToInternal((sbyte)'B');
        for (nuint i = 0; i < sr.size(); i++)
        {
            nuint j = 0;
            nuint k = i;
            for (;;)
            {
                if (j == d.size())
                    return true;
                if (k >= sr.size())
                    break;
                if (sr[k] == letterB)
                {
                    if (dIsSr && d[j] == letterB)
                    {
                        j++;
                        k++;
                    }
                    else if (syn.isB((Xchar)d[j]))
                    {
                        j++;
                        k++;
                        if (k == sr.size() || sr[k] != letterB)
                        {
                            while (j < d.size() && syn.isB((Xchar)d[j]))
                                j++;
                        }
                    }
                    else
                        break;
                }
                else if (dIsSr && d[j] == letterB)
                {
                    if (syn.isB((Xchar)sr[k]))
                    {
                        ++j;
                        ++k;
                        if (j < d.size() && d[j] != letterB)
                        {
                            while (k < sr.size() && syn.isB((Xchar)sr[k]))
                                k++;
                        }
                    }
                    else
                        break;
                }
                else if (d[j] == sr[k])
                {
                    j++;
                    k++;
                }
                else
                    break;
            }
        }
        return false;
    }

    // Notation *lookupCreateNotation(const StringC &name);
    protected ConstPtr<Notation> lookupCreateNotation(StringC name)
    {
        Ptr<Notation> nt = defDtd().lookupNotation(name);
        if (nt.isNull())
        {
            nt = new Ptr<Notation>(new Notation(name, defDtd().namePointer(), defDtd().isBase()));
            defDtd().insertNotation(nt);
        }
        return new ConstPtr<Notation>(nt.pointer());
    }

    // ElementType *lookupCreateElement(const StringC &name);
    protected ElementType? lookupCreateElement(StringC name)
    {
        ElementType? e = defDtd().lookupElementType(name);
        if (e == null)
        {
            if (haveDefLpd())
                message(ParserMessages.noSuchSourceElement, new StringMessageArg(name));
            else
            {
                e = new ElementType(name, defDtd().allocElementTypeIndex());
                defDtd().insertElementType(e);
            }
        }
        return e;
    }

    // RankStem *lookupCreateRankStem(const StringC &name);
    protected RankStem? lookupCreateRankStem(StringC name)
    {
        RankStem? r = defDtd().lookupRankStem(name);
        if (r == null)
        {
            r = new RankStem(name, defDtd().nRankStem());
            defDtd().insertRankStem(r);
            ElementType? e = defDtd().lookupElementType(name);
            if (e != null && e.definition() != null)
                message(ParserMessages.rankStemGenericIdentifier, new StringMessageArg(name));
        }
        return r;
    }

    // From parseCommon.cxx
    protected virtual void doInit() { throw new NotImplementedException(); }

    // void doProlog();
    protected void doProlog()
    {
        const uint maxTries = 10;
        uint tries = 0;
        do
        {
            if (cancelled())
            {
                allDone();
                return;
            }
            Token token = getToken(Mode.proMode);
            switch (token)
            {
                case Tokens.tokenUnrecognized:
                    if (reportNonSgmlCharacter())
                        break;
                    if (hadDtd())
                    {
                        currentInput()!.ungetToken();
                        endProlog();
                        return;
                    }
                    {
                        StringC gi = new StringC();
                        if (lookingAtStartTag(gi))
                        {
                            currentInput()!.ungetToken();
                            implyDtd(gi);
                            return;
                        }
                    }
                    if (++tries >= maxTries)
                    {
                        message(ParserMessages.notSgml);
                        giveUp();
                        return;
                    }
                    message(ParserMessages.prologCharacter, new StringMessageArg(currentToken()));
                    prologRecover();
                    break;
                case Tokens.tokenEe:
                    if (hadDtd())
                    {
                        endProlog();
                        return;
                    }
                    message(ParserMessages.documentEndProlog);
                    allDone();
                    return;
                case Tokens.tokenMdoMdc:
                    // empty comment
                    emptyCommentDecl();
                    break;
                case Tokens.tokenMdoCom:
                    if (!parseCommentDecl())
                        prologRecover();
                    break;
                case Tokens.tokenMdoNameStart:
                    setPass2Start();
                    if (startMarkup(eventsWanted().wantPrologMarkup(), currentLocation()) != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dMDO);
                    Syntax.ReservedName name;
                    if (parseDeclarationName(out name))
                    {
                        switch (name)
                        {
                            case Syntax.ReservedName.rDOCTYPE:
                                if (!parseDoctypeDeclStart())
                                    giveUp();
                                return;
                            case Syntax.ReservedName.rLINKTYPE:
                                if (!parseLinktypeDeclStart())
                                    giveUp();
                                return;
                            case Syntax.ReservedName.rELEMENT:
                            case Syntax.ReservedName.rATTLIST:
                            case Syntax.ReservedName.rENTITY:
                            case Syntax.ReservedName.rNOTATION:
                            case Syntax.ReservedName.rSHORTREF:
                            case Syntax.ReservedName.rUSEMAP:
                            case Syntax.ReservedName.rUSELINK:
                            case Syntax.ReservedName.rLINK:
                            case Syntax.ReservedName.rIDLINK:
                                message(ParserMessages.prologDeclaration,
                                        new StringMessageArg(syntax().reservedName(name)));
                                if (!hadDtd())
                                    tries++;
                                prologRecover();
                                break;
                            default:
                                message(ParserMessages.noSuchDeclarationType,
                                        new StringMessageArg(syntax().reservedName(name)));
                                prologRecover();
                                break;
                        }
                    }
                    else
                        prologRecover();
                    break;
                case Tokens.tokenPio:
                    if (!parseProcessingInstruction())
                        prologRecover();
                    break;
                case Tokens.tokenS:
                    if (eventsWanted().wantPrologMarkup())
                    {
                        extendS();
                        eventHandler().sSep(new SSepEvent(currentInput()!.currentTokenStart(),
                                                          currentInput()!.currentTokenLength(),
                                                          currentLocation(),
                                                          true));
                    }
                    break;
                default:
                    // CANNOT_HAPPEN();
                    break;
            }
        } while (eventQueueEmpty());
    }

    // void doDeclSubset();
    protected void doDeclSubset()
    {
        do
        {
            if (cancelled())
            {
                allDone();
                return;
            }
            Token token = getToken(currentMode());
            uint startLevel = inputLevel();
            Boolean inDtd = !haveDefLpd();
            switch (token)
            {
                case Tokens.tokenUnrecognized:
                    if (reportNonSgmlCharacter())
                        break;
                    message(ParserMessages.declSubsetCharacter, new StringMessageArg(currentToken()));
                    declSubsetRecover(startLevel);
                    break;
                case Tokens.tokenEe:
                    if (inputLevel() == specialParseInputLevel())
                    {
                        // FIXME have separate messages for each type of special parse
                        message(ParserMessages.specialParseEntityEnd);
                    }
                    if (eventsWanted().wantPrologMarkup())
                        eventHandler().entityEnd(new EntityEndEvent(currentLocation()));
                    if (inputLevel() == 2)
                    {
                        EntityDecl? e = currentLocation().origin().pointer()?.entityDecl();
                        if (e != null
                            && (e.declType() == EntityDecl.DeclType.doctype
                                || e.declType() == EntityDecl.DeclType.linktype))
                        {
                            // popInputStack may destroy e
                            Boolean fake = e.defLocation().origin().isNull();
                            popInputStack();
                            if (inDtd)
                                parseDoctypeDeclEnd(fake);
                            else
                                parseLinktypeDeclEnd();
                            setPhase(Phase.prologPhase);
                            return;
                        }
                    }
                    if (inputLevel() == 1)
                    {
                        if (finalPhase() == Phase.declSubsetPhase)
                        {
                            checkDtd(defDtd());
                            endDtd();
                        }
                        else
                            // Give message before popping stack.
                            message(inDtd
                                    ? ParserMessages.documentEndDtdSubset
                                    : ParserMessages.documentEndLpdSubset);
                        popInputStack();
                        allDone();
                    }
                    else
                        popInputStack();
                    return;
                case Tokens.tokenDsc: // end of declaration subset
                    // FIXME what's the right location?
                    if (!referenceDsEntity(currentLocation()))
                    {
                        if (inDtd)
                            parseDoctypeDeclEnd();
                        else
                            parseLinktypeDeclEnd();
                        setPhase(Phase.prologPhase);
                    }
                    return;
                case Tokens.tokenMdoNameStart: // named markup declaration
                    if (startMarkup(eventsWanted().wantPrologMarkup(), currentLocation()) != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dMDO);
                    Syntax.ReservedName declName;
                    Boolean result;
                    if (parseDeclarationName(out declName, inDtd && !options().errorAfdr))
                    {
                        switch (declName)
                        {
                            case Syntax.ReservedName.rANY: // used for <!AFDR
                                result = parseAfdrDecl();
                                break;
                            case Syntax.ReservedName.rELEMENT:
                                if (inDtd)
                                    result = parseElementDecl();
                                else
                                {
                                    message(ParserMessages.lpdSubsetDeclaration,
                                            new StringMessageArg(syntax().reservedName(declName)));
                                    result = false;
                                }
                                break;
                            case Syntax.ReservedName.rATTLIST:
                                result = parseAttlistDecl();
                                break;
                            case Syntax.ReservedName.rENTITY:
                                result = parseEntityDecl();
                                break;
                            case Syntax.ReservedName.rNOTATION:
                                result = parseNotationDecl();
                                if (!inDtd && !sd().www())
                                    message(ParserMessages.lpdSubsetDeclaration,
                                            new StringMessageArg(syntax().reservedName(declName)));
                                break;
                            case Syntax.ReservedName.rSHORTREF:
                                if (inDtd)
                                    result = parseShortrefDecl();
                                else
                                {
                                    message(ParserMessages.lpdSubsetDeclaration,
                                            new StringMessageArg(syntax().reservedName(declName)));
                                    result = false;
                                }
                                break;
                            case Syntax.ReservedName.rUSEMAP:
                                if (inDtd)
                                    result = parseUsemapDecl();
                                else
                                {
                                    message(ParserMessages.lpdSubsetDeclaration,
                                            new StringMessageArg(syntax().reservedName(declName)));
                                    result = false;
                                }
                                break;
                            case Syntax.ReservedName.rLINK:
                                if (inDtd)
                                {
                                    message(ParserMessages.dtdSubsetDeclaration,
                                            new StringMessageArg(syntax().reservedName(declName)));
                                    result = false;
                                }
                                else
                                    result = parseLinkDecl();
                                break;
                            case Syntax.ReservedName.rIDLINK:
                                if (inDtd)
                                {
                                    message(ParserMessages.dtdSubsetDeclaration,
                                            new StringMessageArg(syntax().reservedName(declName)));
                                    result = false;
                                }
                                else
                                    result = parseIdlinkDecl();
                                break;
                            case Syntax.ReservedName.rDOCTYPE:
                            case Syntax.ReservedName.rLINKTYPE:
                            case Syntax.ReservedName.rUSELINK:
                                result = false;
                                message(inDtd
                                        ? ParserMessages.dtdSubsetDeclaration
                                        : ParserMessages.lpdSubsetDeclaration,
                                        new StringMessageArg(syntax().reservedName(declName)));
                                break;
                            default:
                                result = false;
                                message(ParserMessages.noSuchDeclarationType,
                                        new StringMessageArg(syntax().reservedName(declName)));
                                break;
                        }
                    }
                    else
                        result = false;
                    if (!result)
                        declSubsetRecover(startLevel);
                    break;
                case Tokens.tokenMdoMdc: // empty comment declaration
                    emptyCommentDecl();
                    break;
                case Tokens.tokenMdoCom: // comment declaration
                    if (!parseCommentDecl())
                        declSubsetRecover(startLevel);
                    break;
                case Tokens.tokenMdoDso: // marked section declaration
                    if (!parseMarkedSectionDeclStart())
                        declSubsetRecover(startLevel);
                    break;
                case Tokens.tokenMscMdc:
                    handleMarkedSectionEnd();
                    break;
                case Tokens.tokenPeroGrpo: // parameter entity reference with name group
                    message(ParserMessages.peroGrpoProlog);
                    goto case Tokens.tokenPeroNameStart;
                case Tokens.tokenPeroNameStart: // parameter entity reference
                    {
                        ConstPtr<Entity> entity = new ConstPtr<Entity>();
                        Ptr<EntityOrigin> origin = new Ptr<EntityOrigin>();
                        if (parseEntityReference(true, token == Tokens.tokenPeroGrpo ? 1 : 0, entity, origin))
                        {
                            if (!entity.isNull())
                                entity.pointer()!.dsReference(this, origin);
                        }
                        else
                            declSubsetRecover(startLevel);
                    }
                    break;
                case Tokens.tokenPio: // processing instruction
                    if (!parseProcessingInstruction())
                        declSubsetRecover(startLevel);
                    break;
                case Tokens.tokenS: // white space
                    if (eventsWanted().wantPrologMarkup())
                    {
                        extendS();
                        eventHandler().sSep(new SSepEvent(currentInput()!.currentTokenStart(),
                                                          currentInput()!.currentTokenLength(),
                                                          currentLocation(),
                                                          true));
                    }
                    break;
                case Tokens.tokenIgnoredChar:
                    // from an ignored marked section
                    if (eventsWanted().wantPrologMarkup())
                        eventHandler().ignoredChars(new IgnoredCharsEvent(currentInput()!.currentTokenStart(),
                                                                          currentInput()!.currentTokenLength(),
                                                                          currentLocation(),
                                                                          true));
                    break;
                case Tokens.tokenRe:
                case Tokens.tokenRs:
                case Tokens.tokenCroNameStart:
                case Tokens.tokenCroDigit:
                case Tokens.tokenHcroHexDigit:
                case Tokens.tokenEroNameStart:
                case Tokens.tokenEroGrpo:
                case Tokens.tokenChar:
                    // these can occur in a cdata or rcdata marked section
                    message(ParserMessages.dataMarkedSectionDeclSubset);
                    declSubsetRecover(startLevel);
                    break;
                default:
                    // CANNOT_HAPPEN();
                    break;
            }
        } while (eventQueueEmpty());
    }
    // void doInstanceStart();
    protected virtual void doInstanceStart()
    {
        if (cancelled())
        {
            allDone();
            return;
        }
        // FIXME check here that we have a valid dtd
        compileInstanceModes();
        setPhase(Phase.contentPhase);
        Token token = getToken(currentMode());
        switch (token)
        {
            case Tokens.tokenEe:
            case Tokens.tokenStagoNameStart:
            case Tokens.tokenStagoTagc:
            case Tokens.tokenStagoGrpo:
            case Tokens.tokenEtagoNameStart:
            case Tokens.tokenEtagoTagc:
            case Tokens.tokenEtagoGrpo:
                break;
            default:
                if (sd().omittag())
                {
                    uint startImpliedCount = 0;
                    uint attributeListIndex = 0;
                    IList<Undo> undoList = new IList<Undo>();
                    IList<Event> eventList = new IList<Event>();
                    if (!tryImplyTag(currentLocation(),
                                     ref startImpliedCount,
                                     ref attributeListIndex,
                                     undoList,
                                     eventList))
                    {
                        // CANNOT_HAPPEN();
                    }
                    queueElementEvents(eventList);
                }
                else
                    message(ParserMessages.instanceStartOmittag);
                break;
        }
        currentInput()!.ungetToken();
    }
    // void doContent();
    protected void doContent()
    {
        do
        {
            if (cancelled())
            {
                allDone();
                return;
            }
            Token token = getToken(currentMode());
            switch (token)
            {
                case Tokens.tokenEe:
                    if (inputLevel() == 1)
                    {
                        endInstance();
                        return;
                    }
                    if (inputLevel() == specialParseInputLevel())
                    {
                        // FIXME have separate messages for each type of special parse
                        // perhaps force end of marked section or element
                        message(ParserMessages.specialParseEntityEnd);
                    }
                    if (eventsWanted().wantInstanceMarkup())
                        eventHandler().entityEnd(new EntityEndEvent(currentLocation()));
                    if (afterDocumentElement())
                        message(ParserMessages.afterDocumentElementEntityEnd);
                    if (sd().integrallyStored()
                        && tagLevel() > 0
                        && currentElement().index() != currentInputElementIndex())
                        message(ParserMessages.contentAsyncEntityRef);
                    popInputStack();
                    break;
                case Tokens.tokenCroDigit:
                case Tokens.tokenHcroHexDigit:
                    {
                        if (afterDocumentElement())
                            message(ParserMessages.characterReferenceAfterDocumentElement);
                        Char ch = 0;
                        Location loc = new Location();
                        if (parseNumericCharRef(token == Tokens.tokenHcroHexDigit, ref ch, ref loc))
                        {
                            acceptPcdata(loc);
                            noteData();
                            Boolean isSgmlChar = false;
                            if (!translateNumericCharRef(ref ch, ref isSgmlChar))
                                break;
                            if (!isSgmlChar)
                            {
                                eventHandler().nonSgmlChar(new NonSgmlCharEvent(ch, loc));
                                break;
                            }
                            Char[] charData = new Char[] { ch };
                            eventHandler().data(new ImmediateDataEvent(Event.Type.characterData,
                                                                       charData, 1, loc, true));
                            break;
                        }
                    }
                    break;
                case Tokens.tokenCroNameStart:
                    if (afterDocumentElement())
                        message(ParserMessages.characterReferenceAfterDocumentElement);
                    parseNamedCharRef();
                    break;
                case Tokens.tokenEroGrpo:
                case Tokens.tokenEroNameStart:
                    {
                        if (afterDocumentElement())
                            message(ParserMessages.entityReferenceAfterDocumentElement);
                        ConstPtr<Entity> entity = new ConstPtr<Entity>();
                        Ptr<EntityOrigin> origin = new Ptr<EntityOrigin>();
                        if (parseEntityReference(false, token == Tokens.tokenEroGrpo ? 1 : 0, entity, origin))
                        {
                            if (!entity.isNull())
                            {
                                if (entity.pointer()!.isCharacterData())
                                    acceptPcdata(new Location(origin.pointer(), 0));
                                if (inputLevel() == specialParseInputLevel())
                                    entity.pointer()!.rcdataReference(this, origin);
                                else
                                    entity.pointer()!.contentReference(this, origin);
                            }
                        }
                    }
                    break;
                case Tokens.tokenEtagoNameStart:
                    acceptEndTag(parseEndTag());
                    break;
                case Tokens.tokenEtagoTagc:
                    parseEmptyEndTag();
                    break;
                case Tokens.tokenEtagoGrpo:
                    parseGroupEndTag();
                    break;
                case Tokens.tokenMdoNameStart:
                    if (startMarkup(eventsWanted().wantInstanceMarkup(), currentLocation()) != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dMDO);
                    Syntax.ReservedName name;
                    Boolean result;
                    uint startLevel;
                    startLevel = inputLevel();
                    if (parseDeclarationName(out name))
                    {
                        switch (name)
                        {
                            case Syntax.ReservedName.rUSEMAP:
                                if (afterDocumentElement())
                                    message(ParserMessages.declarationAfterDocumentElement,
                                            new StringMessageArg(syntax().reservedName(name)));
                                result = parseUsemapDecl();
                                break;
                            case Syntax.ReservedName.rUSELINK:
                                if (afterDocumentElement())
                                    message(ParserMessages.declarationAfterDocumentElement,
                                            new StringMessageArg(syntax().reservedName(name)));
                                result = parseUselinkDecl();
                                break;
                            case Syntax.ReservedName.rDOCTYPE:
                            case Syntax.ReservedName.rLINKTYPE:
                            case Syntax.ReservedName.rELEMENT:
                            case Syntax.ReservedName.rATTLIST:
                            case Syntax.ReservedName.rENTITY:
                            case Syntax.ReservedName.rNOTATION:
                            case Syntax.ReservedName.rSHORTREF:
                            case Syntax.ReservedName.rLINK:
                            case Syntax.ReservedName.rIDLINK:
                                message(ParserMessages.instanceDeclaration,
                                        new StringMessageArg(syntax().reservedName(name)));
                                result = false;
                                break;
                            default:
                                message(ParserMessages.noSuchDeclarationType,
                                        new StringMessageArg(syntax().reservedName(name)));
                                result = false;
                                break;
                        }
                    }
                    else
                        result = false;
                    if (!result)
                        skipDeclaration(startLevel);
                    noteMarkup();
                    break;
                case Tokens.tokenMdoMdc:
                    // empty comment
                    emptyCommentDecl();
                    noteMarkup();
                    break;
                case Tokens.tokenMdoCom:
                    parseCommentDecl();
                    noteMarkup();
                    break;
                case Tokens.tokenMdoDso:
                    if (afterDocumentElement())
                        message(ParserMessages.markedSectionAfterDocumentElement);
                    parseMarkedSectionDeclStart();
                    noteMarkup();
                    break;
                case Tokens.tokenMscMdc:
                    handleMarkedSectionEnd();
                    noteMarkup();
                    break;
                case Tokens.tokenNet:
                    parseNullEndTag();
                    break;
                case Tokens.tokenPio:
                    parseProcessingInstruction();
                    break;
                case Tokens.tokenStagoNameStart:
                    parseStartTag();
                    break;
                case Tokens.tokenStagoTagc:
                    parseEmptyStartTag();
                    break;
                case Tokens.tokenStagoGrpo:
                    parseGroupStartTag();
                    break;
                case Tokens.tokenRe:
                    acceptPcdata(currentLocation());
                    queueRe(currentLocation());
                    break;
                case Tokens.tokenRs:
                    acceptPcdata(currentLocation());
                    noteRs();
                    if (eventsWanted().wantInstanceMarkup())
                        eventHandler().ignoredRs(new IgnoredRsEvent(currentChar(),
                                                                    currentLocation()));
                    break;
                case Tokens.tokenS:
                    extendContentS();
                    if (eventsWanted().wantInstanceMarkup())
                        eventHandler().sSep(new SSepEvent(currentInput()!.currentTokenStart(),
                                                          currentInput()!.currentTokenLength(),
                                                          currentLocation(),
                                                          false));
                    break;
                case Tokens.tokenIgnoredChar:
                    extendData();
                    if (eventsWanted().wantMarkedSections())
                        eventHandler().ignoredChars(new IgnoredCharsEvent(currentInput()!.currentTokenStart(),
                                                                          currentInput()!.currentTokenLength(),
                                                                          currentLocation(),
                                                                          false));
                    break;
                case Tokens.tokenUnrecognized:
                    reportNonSgmlCharacter();
                    parsePcdata();
                    break;
                case Tokens.tokenCharDelim:
                    message(ParserMessages.dataCharDelim,
                            new StringMessageArg(new StringC(currentInput()!.currentTokenStart(),
                                                             currentInput()!.currentTokenLength())));
                    goto case Tokens.tokenChar;
                case Tokens.tokenChar:
                    parsePcdata();
                    break;
                default:
                    if (token >= Tokens.tokenFirstShortref)
                        handleShortref((int)(token - Tokens.tokenFirstShortref));
                    break;
            }
        } while (eventQueueEmpty());
    }

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

    // void extendData();
    protected void extendData()
    {
        XcharMap<PackedBoolean> isNormal = normalMap();
        InputSource? ins = currentInput();
        if (ins == null) return;
        nuint length = ins.currentTokenLength();
        // This is one of the parser's inner loops, so it needs to be fast.
        while (isNormal[ins.tokenCharInBuffer(messenger())])
            length++;
        ins.endToken(length);
    }

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

    // void extendContentS();
    protected void extendContentS()
    {
        InputSource? ins = currentInput();
        if (ins == null) return;
        nuint length = ins.currentTokenLength();
        XcharMap<PackedBoolean> isNormal = normalMap();
        for (;;)
        {
            Xchar ch = ins.tokenChar(messenger());
            if (!syntax().isS(ch) || !isNormal[ch])
                break;
            length++;
        }
        ins.endToken(length);
    }

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

    // void declSubsetRecover(unsigned startLevel);
    protected void declSubsetRecover(uint startLevel)
    {
        for (;;)
        {
            Token token = getToken(currentMode());
            switch (token)
            {
                case Tokens.tokenUnrecognized:
                    getChar();
                    break;
                case Tokens.tokenEe:
                    if (inputLevel() <= startLevel)
                        return;
                    popInputStack();
                    break;
                case Tokens.tokenMdoCom:
                case Tokens.tokenDsc:
                case Tokens.tokenMdoNameStart:
                case Tokens.tokenMdoMdc:
                case Tokens.tokenMdoDso:
                case Tokens.tokenMscMdc:
                case Tokens.tokenPio:
                    if (inputLevel() == startLevel)
                    {
                        currentInput()!.ungetToken();
                        return;
                    }
                    break;
                default:
                    break;
            }
        }
    }

    // void prologRecover();
    protected void prologRecover()
    {
        uint skipCount = 0;
        const uint skipMax = 250;
        for (;;)
        {
            Token token = getToken(Mode.proMode);
            skipCount++;
            if (token == Tokens.tokenUnrecognized)
            {
                token = getToken(Mode.mdMode);
                if (token == Tokens.tokenMdc)
                {
                    token = getToken(Mode.proMode);
                    if (token == Tokens.tokenS)
                        return;
                }
            }
            switch (token)
            {
                case Tokens.tokenUnrecognized:
                    getChar();
                    break;
                case Tokens.tokenEe:
                    return;
                case Tokens.tokenMdoMdc:
                case Tokens.tokenMdoCom:
                case Tokens.tokenMdoNameStart:
                case Tokens.tokenPio:
                    currentInput()!.ungetToken();
                    return;
                case Tokens.tokenS:
                    if (currentChar() == syntax().standardFunction((int)Syntax.StandardFunction.fRE)
                        && skipCount >= skipMax)
                        return;
                    break;
                default:
                    break;
            }
        }
    }
    // void skipDeclaration(unsigned startLevel);
    protected void skipDeclaration(uint startLevel)
    {
        const uint skipMax = 250;
        uint skipCount = 0;
        for (;;)
        {
            Token token = getToken(Mode.mdMode);
            if (inputLevel() == startLevel)
                skipCount++;
            switch (token)
            {
                case Tokens.tokenUnrecognized:
                    getChar();
                    break;
                case Tokens.tokenEe:
                    if (inputLevel() <= startLevel)
                        return;
                    popInputStack();
                    return;
                case Tokens.tokenMdc:
                    if (inputLevel() == startLevel)
                        return;
                    break;
                case Tokens.tokenS:
                    if (inputLevel() == startLevel && skipCount >= skipMax
                        && currentChar() == syntax().standardFunction((int)Syntax.StandardFunction.fRE))
                        return;
                    break;
                default:
                    break;
            }
        }
    }
    // Boolean parseElementDecl();
    protected virtual Boolean parseElementDecl()
    {
        uint declInputLevel = inputLevel();
        Param parm = new Param();
        AllowedParams allowNameNameGroup = new AllowedParams(Param.name, Param.nameGroup);
        if (!parseParam(allowNameNameGroup, declInputLevel, parm))
            return false;
        Vector<NameToken> nameVector = new Vector<NameToken>();
        if (parm.type == Param.nameGroup)
        {
            parm.nameTokenVector.swap(nameVector);
            if (options().warnElementGroupDecl)
                message(ParserMessages.elementGroupDecl);
        }
        else
        {
            nameVector.resize(1);
            parm.token.swap(nameVector[0].name);
            parm.origToken.swap(nameVector[0].origName);
        }
        AllowedParams allowRankOmissionContent = new AllowedParams(
            Param.number,
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rO),
            Param.minus,
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA),
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rRCDATA),
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rEMPTY),
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rANY),
            Param.modelGroup);
        if (!parseParam(allowRankOmissionContent, declInputLevel, parm))
            return false;
        StringC rankSuffix = new StringC();
        Vector<ElementType?> elements = new Vector<ElementType?>(nameVector.size());
        Vector<RankStem?> rankStems = new Vector<RankStem?>();
        Vector<RankStem?> constRankStems = new Vector<RankStem?>();
        nuint i;
        if (parm.type == Param.number)
        {
            if (options().warnRank)
                message(ParserMessages.rank);
            parm.token.swap(rankSuffix);
            rankStems.resize(nameVector.size());
            constRankStems.resize(nameVector.size());
            for (i = 0; i < elements.size(); i++)
            {
                StringC name = new StringC(nameVector[(int)i].name);
                name.operatorPlusAssign(rankSuffix);
                if (name.size() > syntax().namelen()
                    && nameVector[(int)i].name.size() <= syntax().namelen())
                    message(ParserMessages.genericIdentifierLength,
                            new NumberMessageArg(syntax().namelen()));
                elements[(int)i] = lookupCreateElement(name);
                rankStems[(int)i] = lookupCreateRankStem(nameVector[(int)i].name);
                constRankStems[(int)i] = rankStems[(int)i];
            }
            AllowedParams allowOmissionContent = new AllowedParams(
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rO),
                Param.minus,
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA),
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rRCDATA),
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rEMPTY),
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rANY),
                Param.modelGroup);
            Token token = getToken(Mode.mdMinusMode);
            if (token == Tokens.tokenNameStart)
                message(ParserMessages.psRequired);
            currentInput()!.ungetToken();
            if (!parseParam(allowOmissionContent, declInputLevel, parm))
                return false;
        }
        else
        {
            for (i = 0; i < elements.size(); i++)
            {
                elements[(int)i] = lookupCreateElement(nameVector[(int)i].name);
                elements[(int)i]!.setOrigName(nameVector[(int)i].origName);
            }
        }
        for (i = 0; i < elements.size(); i++)
            if (defDtd().lookupRankStem(elements[(int)i]!.name()) != null && validate())
                message(ParserMessages.rankStemGenericIdentifier,
                        new StringMessageArg(elements[(int)i]!.name()));
        byte omitFlags = 0;
        if (parm.type == Param.minus
            || parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rO))
        {
            if (options().warnMinimizationParam)
                message(ParserMessages.minimizationParam);
            omitFlags |= (byte)ElementDefinition.OmitFlags.omitSpec;
            if (parm.type != Param.minus)
                omitFlags |= (byte)ElementDefinition.OmitFlags.omitStart;
            AllowedParams allowOmission = new AllowedParams(
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rO),
                Param.minus);
            if (!parseParam(allowOmission, declInputLevel, parm))
                return false;
            if (parm.type != Param.minus)
                omitFlags |= (byte)ElementDefinition.OmitFlags.omitEnd;
            AllowedParams allowContent = new AllowedParams(
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA),
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rRCDATA),
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rEMPTY),
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rANY),
                Param.modelGroup);
            if (!parseParam(allowContent, declInputLevel, parm))
                return false;
        }
        else
        {
            if (sd().omittag())
                message(ParserMessages.missingTagMinimization);
        }
        Ptr<ElementDefinition> def = new Ptr<ElementDefinition>();
        AllowedParams allowMdc = new AllowedParams(Param.mdc);
        if (parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA))
        {
            def = new Ptr<ElementDefinition>(new ElementDefinition(markupLocation(),
                defDtd().allocElementDefinitionIndex(),
                omitFlags,
                ElementDefinition.DeclaredContent.cdata));
            if (!parseParam(allowMdc, declInputLevel, parm))
                return false;
            if (options().warnCdataContent)
                message(ParserMessages.cdataContent);
        }
        else if (parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rRCDATA))
        {
            def = new Ptr<ElementDefinition>(new ElementDefinition(markupLocation(),
                defDtd().allocElementDefinitionIndex(),
                omitFlags,
                ElementDefinition.DeclaredContent.rcdata));
            if (!parseParam(allowMdc, declInputLevel, parm))
                return false;
            if (options().warnRcdataContent)
                message(ParserMessages.rcdataContent);
        }
        else if (parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rEMPTY))
        {
            def = new Ptr<ElementDefinition>(new ElementDefinition(markupLocation(),
                defDtd().allocElementDefinitionIndex(),
                omitFlags,
                ElementDefinition.DeclaredContent.empty));
            if ((omitFlags & (byte)ElementDefinition.OmitFlags.omitSpec) != 0
                && (omitFlags & (byte)ElementDefinition.OmitFlags.omitEnd) == 0
                && options().warnShould)
                message(ParserMessages.emptyOmitEndTag);
            if (!parseParam(allowMdc, declInputLevel, parm))
                return false;
        }
        else if (parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rANY))
        {
            def = new Ptr<ElementDefinition>(new ElementDefinition(markupLocation(),
                defDtd().allocElementDefinitionIndex(),
                omitFlags,
                ElementDefinition.DeclaredContent.any));
            if (!parseExceptions(declInputLevel, def))
                return false;
        }
        else if (parm.type == Param.modelGroup)
        {
            nuint cnt = (nuint)parm.modelGroupPtr.pointer()!.grpgtcnt();
            // The outermost model group isn't formally a content token.
            if (cnt - 1 > syntax().grpgtcnt())
                message(ParserMessages.grpgtcnt, new NumberMessageArg(syntax().grpgtcnt()));
            Owner<CompiledModelGroup> modelGroup = new Owner<CompiledModelGroup>(
                new CompiledModelGroup(parm.modelGroupPtr));
            Vector<ContentModelAmbiguity> ambiguities = new Vector<ContentModelAmbiguity>();
            Boolean pcdataUnreachable = false;
            modelGroup.pointer()!.compile(currentDtd().nElementTypeIndex(), ambiguities,
                                          ref pcdataUnreachable);
            if (pcdataUnreachable && options().warnMixedContent)
                message(ParserMessages.pcdataUnreachable);
            if (validate())
            {
                for (i = 0; i < ambiguities.size(); i++)
                {
                    ContentModelAmbiguity a = ambiguities[(int)i];
                    reportAmbiguity(a.from, a.to1, a.to2, a.andDepth);
                }
            }
            def = new Ptr<ElementDefinition>(new ElementDefinition(markupLocation(),
                defDtd().allocElementDefinitionIndex(),
                omitFlags,
                ElementDefinition.DeclaredContent.modelGroup,
                modelGroup));
            if (!parseExceptions(declInputLevel, def))
                return false;
        }
        if (rankSuffix.size() > 0)
            def.pointer()!.setRank(rankSuffix, constRankStems);
        ConstPtr<ElementDefinition> constDef = new ConstPtr<ElementDefinition>(def.pointer());
        for (i = 0; i < elements.size(); i++)
        {
            if (elements[(int)i]!.definition() != null)
            {
                if (validate())
                    message(ParserMessages.duplicateElementDefinition,
                            new StringMessageArg(elements[(int)i]!.name()));
            }
            else
            {
                elements[(int)i]!.setElementDefinition(constDef, i);
                if (!elements[(int)i]!.attributeDef().isNull())
                    checkElementAttribute(elements[(int)i]);
            }
            if (rankStems.size() > 0)
                rankStems[(int)i]!.addDefinition(constDef);
        }
        if (currentMarkup() != null)
        {
            Vector<ElementType?> v = new Vector<ElementType?>(elements.size());
            for (i = 0; i < elements.size(); i++)
                v[(int)i] = elements[(int)i];
            eventHandler().elementDecl(new ElementDeclEvent(v, currentDtdPointer(),
                                                            markupLocation(),
                                                            currentMarkup()));
        }
        return true;
    }

    // Boolean parseExceptions(unsigned declInputLevel, Ptr<ElementDefinition> &def);
    protected Boolean parseExceptions(uint declInputLevel, Ptr<ElementDefinition> def)
    {
        Param parm = new Param();
        AllowedParams allowExceptionsMdc = new AllowedParams(Param.mdc, Param.exclusions, Param.inclusions);
        if (!parseParam(allowExceptionsMdc, declInputLevel, parm))
            return false;
        if (parm.type == Param.exclusions)
        {
            if (options().warnExclusion)
                message(ParserMessages.exclusion);
            def.pointer()!.setExclusions(parm.elementVector);
            AllowedParams allowInclusionsMdc = new AllowedParams(Param.mdc, Param.inclusions);
            if (!parseParam(allowInclusionsMdc, declInputLevel, parm))
                return false;
        }
        if (parm.type == Param.inclusions)
        {
            if (options().warnInclusion)
                message(ParserMessages.inclusion);
            def.pointer()!.setInclusions(parm.elementVector);
            nuint nI = def.pointer()!.nInclusions();
            nuint nE = def.pointer()!.nExclusions();
            if (nE > 0)
            {
                for (nuint j = 0; j < nI; j++)
                {
                    ElementType? e = def.pointer()!.inclusion(j);
                    for (nuint k = 0; k < nE; k++)
                        if (def.pointer()!.exclusion(k) == e)
                            message(ParserMessages.excludeIncludeSame,
                                    new StringMessageArg(e!.name()));
                }
            }
            AllowedParams allowMdc = new AllowedParams(Param.mdc);
            if (!parseParam(allowMdc, declInputLevel, parm))
                return false;
        }
        return true;
    }

    // void reportAmbiguity(const LeafContentToken *from, const LeafContentToken *to1,
    //                      const LeafContentToken *to2, unsigned ambigAndDepth);
    protected void reportAmbiguity(LeafContentToken? from, LeafContentToken? to1,
                                   LeafContentToken? to2, uint ambigAndDepth)
    {
        StringC toName = new StringC();
        ElementType? toType = to1!.elementType();
        if (toType != null)
            toName = toType.name();
        else
        {
            toName = syntax().delimGeneral((int)Syntax.DelimGeneral.dRNI);
            toName.operatorPlusAssign(syntax().reservedName(Syntax.ReservedName.rPCDATA));
        }
        uint to1Index = to1.typeIndex() + 1;
        uint to2Index = to2!.typeIndex() + 1;
        if (from!.isInitial())
            message(ParserMessages.ambiguousModelInitial,
                    new StringMessageArg(toName),
                    new OrdinalMessageArg(to1Index),
                    new OrdinalMessageArg(to2Index));
        else
        {
            StringC fromName = new StringC();
            ElementType? fromType = from.elementType();
            if (fromType != null)
                fromName = fromType.name();
            else
            {
                fromName = syntax().delimGeneral((int)Syntax.DelimGeneral.dRNI);
                fromName.operatorPlusAssign(syntax().reservedName(Syntax.ReservedName.rPCDATA));
            }
            uint fromIndex = from.typeIndex() + 1;
            uint andMatches = from.andDepth() - ambigAndDepth;
            if (andMatches == 0)
                message(ParserMessages.ambiguousModel,
                        new StringMessageArg(fromName),
                        new OrdinalMessageArg(fromIndex),
                        new StringMessageArg(toName),
                        new OrdinalMessageArg(to1Index),
                        new OrdinalMessageArg(to2Index));
            else if (andMatches == 1)
                message(ParserMessages.ambiguousModelSingleAnd,
                        new StringMessageArg(fromName),
                        new OrdinalMessageArg(fromIndex),
                        new StringMessageArg(toName),
                        new OrdinalMessageArg(to1Index),
                        new OrdinalMessageArg(to2Index));
            else
                message(ParserMessages.ambiguousModelMultipleAnd,
                        new StringMessageArg(fromName),
                        new OrdinalMessageArg(fromIndex),
                        new NumberMessageArg(andMatches),
                        new StringMessageArg(toName),
                        new OrdinalMessageArg(to1Index),
                        new OrdinalMessageArg(to2Index));
        }
    }

    // void checkElementAttribute(const ElementType *e, size_t checkFrom = 0);
    protected void checkElementAttribute(ElementType? e, nuint checkFrom = 0)
    {
        if (!validate())
            return;
        AttributeDefinitionList? attDef = e!.attributeDef().pointer();
        Boolean conref = false;
        ElementDefinition? edef = e.definition();
        nuint attDefLength = attDef!.size();
        for (nuint j = checkFrom; j < attDefLength; j++)
        {
            AttributeDefinition? p = attDef.def(j);
            if (p!.isConref())
                conref = true;
            if (p.isNotation()
                && edef!.declaredContent() == ElementDefinition.DeclaredContent.empty)
                message(ParserMessages.notationEmpty, new StringMessageArg(e.name()));
        }
        if (conref)
        {
            if (edef!.declaredContent() == ElementDefinition.DeclaredContent.empty)
                message(ParserMessages.conrefEmpty, new StringMessageArg(e.name()));
        }
    }
    // Boolean parseAttlistDecl();
    protected virtual Boolean parseAttlistDecl()
    {
        uint declInputLevel = inputLevel();
        Param parm = new Param();
        nuint attcnt = 0;
        nuint idIndex = nuint.MaxValue;
        nuint notationIndex = nuint.MaxValue;
        Boolean anyCurrent = false;

        Boolean isNotation;
        Vector<IAttributed?> attributed = new Vector<IAttributed?>();
        if (!parseAttributed(declInputLevel, parm, attributed, out isNotation))
            return false;

        Vector<AttributeDefinition?> defs = new Vector<AttributeDefinition?>();
        AllowedParams allowNameMdc = new AllowedParams(Param.name, Param.mdc);
        AllowedParams allowName = new AllowedParams(Param.name);
        if (!parseParam(sd().www() ? allowNameMdc : allowName, declInputLevel, parm))
            return false;

        while (parm.type != Param.mdc)
        {
            StringC attributeName = new StringC();
            StringC origAttributeName = new StringC();
            parm.token.swap(attributeName);
            parm.origToken.swap(origAttributeName);
            attcnt++;
            Boolean duplicate = false;
            nuint i;
            for (i = 0; i < defs.size(); i++)
            {
                if (defs[i]!.name() == attributeName)
                {
                    message(ParserMessages.duplicateAttributeDef,
                            new StringMessageArg(attributeName));
                    duplicate = true;
                    break;
                }
            }

            Owner<DeclaredValue> declaredValue = new Owner<DeclaredValue>();
            if (!parseDeclaredValue(declInputLevel, isNotation, parm, declaredValue))
                return false;

            if (!duplicate)
            {
                if (declaredValue.pointer()!.isId())
                {
                    if (idIndex != nuint.MaxValue)
                        message(ParserMessages.multipleIdAttributes,
                                new StringMessageArg(defs[idIndex]!.name()));
                    idIndex = defs.size();
                }
                else if (declaredValue.pointer()!.isNotation())
                {
                    if (notationIndex != nuint.MaxValue)
                        message(ParserMessages.multipleNotationAttributes,
                                new StringMessageArg(defs[notationIndex]!.name()));
                    notationIndex = defs.size();
                }
            }

            Vector<StringC>? tokensPtr = declaredValue.pointer()!.getTokens();
            if (tokensPtr != null)
            {
                nuint nTokens = tokensPtr.size();
                if (!sd().www())
                {
                    for (i = 0; i < nTokens; i++)
                    {
                        for (nuint j = 0; j < defs.size(); j++)
                        {
                            if (defs[j]!.containsToken(tokensPtr[i]))
                            {
                                message(ParserMessages.duplicateAttributeToken,
                                        new StringMessageArg(tokensPtr[i]));
                                break;
                            }
                        }
                    }
                }
                attcnt += nTokens;
            }

            Owner<AttributeDefinition> def = new Owner<AttributeDefinition>();
            if (!parseDefaultValue(declInputLevel, isNotation, parm, attributeName,
                                   declaredValue, def, ref anyCurrent))
                return false;

            if (haveDefLpd() && defLpd().type() == Lpd.Type.simpleLink && !def.pointer()!.isFixed())
                message(ParserMessages.simpleLinkFixedAttribute);

            def.pointer()!.setOrigName(origAttributeName);
            if (!duplicate)
            {
                defs.resize(defs.size() + 1);
                defs[defs.size() - 1] = def.extract();
            }

            AllowedParams allowNameMdcLoop = new AllowedParams(Param.name, Param.mdc);
            if (!parseParam(allowNameMdcLoop, declInputLevel, parm))
                return false;
        }

        if (attcnt > syntax().attcnt())
            message(ParserMessages.attcnt,
                    new NumberMessageArg(attcnt),
                    new NumberMessageArg(syntax().attcnt()));

        if (haveDefLpd() && !isNotation)
        {
            if (defLpd().type() == Lpd.Type.simpleLink)
            {
                for (nuint i = 0; i < attributed.size(); i++)
                {
                    ElementType? e = attributed[i] as ElementType;
                    if (e != null)
                    {
                        if (e.name() == defLpd().sourceDtd().pointer()!.name())
                        {
                            SimpleLpd lpd = (SimpleLpd)defLpd();
                            if (lpd.attributeDef().isNull())
                                lpd.setAttributeDef(new Ptr<AttributeDefinitionList>(
                                    new AttributeDefinitionList(defs, 0)));
                            else
                                message(ParserMessages.duplicateAttlistElement,
                                        new StringMessageArg(e.name()));
                        }
                        else
                            message(ParserMessages.simpleLinkAttlistElement,
                                    new StringMessageArg(e.name()));
                    }
                }
            }
            else
            {
                Ptr<AttributeDefinitionList> adl = new Ptr<AttributeDefinitionList>(
                    new AttributeDefinitionList(defs,
                                                defComplexLpd().allocAttributeDefinitionListIndex()));
                for (nuint i = 0; i < attributed.size(); i++)
                {
                    ElementType? e = attributed[i] as ElementType;
                    if (e != null)
                    {
                        if (defComplexLpd().attributeDef(e).isNull())
                            defComplexLpd().setAttributeDef(e, new ConstPtr<AttributeDefinitionList>(adl.pointer()));
                        else
                            message(ParserMessages.duplicateAttlistElement,
                                    new StringMessageArg(e.name()));
                    }
                }
            }
        }
        else
        {
            Ptr<AttributeDefinitionList> adl = new Ptr<AttributeDefinitionList>(
                new AttributeDefinitionList(defs,
                                            defDtd().allocAttributeDefinitionListIndex(),
                                            anyCurrent,
                                            idIndex,
                                            notationIndex));
            for (nuint i = 0; i < attributed.size(); i++)
            {
                if (attributed[i]!.attributeDef().isNull())
                {
                    attributed[i]!.setAttributeDef(new Ptr<AttributeDefinitionList>(adl.pointer()));
                    if (!isNotation)
                    {
                        ElementType? e = attributed[i] as ElementType;
                        if (e != null && e.definition() != null)
                            checkElementAttribute(e);
                    }
                }
                else if (options().errorAfdr && !sd().www())
                {
                    if (isNotation)
                        message(ParserMessages.duplicateAttlistNotation,
                                new StringMessageArg(((Notation)attributed[i]!).name()));
                    else
                        message(ParserMessages.duplicateAttlistElement,
                                new StringMessageArg(((ElementType)attributed[i]!).name()));
                }
                else
                {
                    if (!hadAfdrDecl() && !sd().www())
                    {
                        message(ParserMessages.missingAfdrDecl);
                        setHadAfdrDecl();
                    }
                    AttributeDefinitionList? curAdl;
                    {
                        // Use block to make sure temporary gets destroyed.
                        curAdl = attributed[i]!.attributeDef().pointer();
                    }
                    nuint oldSize = curAdl!.size();
                    if (curAdl.count() != 1)
                    {
                        Vector<AttributeDefinition?> copy = new Vector<AttributeDefinition?>(oldSize);
                        for (nuint j = 0; j < oldSize; j++)
                            copy[j] = curAdl.def(j)?.copy();
                        Ptr<AttributeDefinitionList> adlCopy = new Ptr<AttributeDefinitionList>(
                            new AttributeDefinitionList(copy,
                                                        defDtd().allocAttributeDefinitionListIndex(),
                                                        curAdl.anyCurrent(),
                                                        curAdl.idIndex(),
                                                        curAdl.notationIndex()));
                        attributed[i]!.setAttributeDef(adlCopy);
                        curAdl = adlCopy.pointer();
                    }
                    for (nuint j = 0; j < adl.pointer()!.size(); j++)
                    {
                        uint index;
                        if (!curAdl!.attributeIndex(adl.pointer()!.def(j)!.name(), out index))
                        {
                            nuint checkIndex = curAdl.idIndex();
                            if (checkIndex != nuint.MaxValue && adl.pointer()!.def(j)!.isId())
                                message(ParserMessages.multipleIdAttributes,
                                        new StringMessageArg(curAdl.def(checkIndex)!.name()));
                            checkIndex = curAdl.notationIndex();
                            if (checkIndex != nuint.MaxValue && adl.pointer()!.def(j)!.isNotation())
                                message(ParserMessages.multipleNotationAttributes,
                                        new StringMessageArg(curAdl.def(checkIndex)!.name()));
                            curAdl.append(adl.pointer()!.def(j)?.copy());
                        }
                        else
                        {
                            Boolean tem;
                            if (curAdl.def(index)!.isSpecified(out tem))
                                message(ParserMessages.specifiedAttributeRedeclared,
                                        new StringMessageArg(adl.pointer()!.def(j)!.name()));
                        }
                    }
                    if (!isNotation)
                    {
                        ElementType? e = attributed[i] as ElementType;
                        if (e != null && e.definition() != null)
                            checkElementAttribute(e, oldSize);
                    }
                }
            }
        }

        if (currentMarkup() != null)
        {
            if (isNotation)
            {
                Vector<ConstPtr<Notation>> v = new Vector<ConstPtr<Notation>>(attributed.size());
                for (nuint i = 0; i < attributed.size(); i++)
                    v[i] = new ConstPtr<Notation>((Notation?)attributed[i]);
                eventHandler().attlistNotationDecl(new AttlistNotationDeclEvent(v,
                                                                                 markupLocation(),
                                                                                 currentMarkup()));
            }
            else
            {
                Vector<ElementType?> v = new Vector<ElementType?>(attributed.size());
                for (nuint i = 0; i < attributed.size(); i++)
                    v[i] = attributed[i] as ElementType;
                if (haveDefLpd())
                    eventHandler().linkAttlistDecl(new LinkAttlistDeclEvent(v,
                                                                             new ConstPtr<Lpd>(defLpdPointer().pointer()),
                                                                             markupLocation(),
                                                                             currentMarkup()));
                else
                    eventHandler().attlistDecl(new AttlistDeclEvent(v,
                                                                     currentDtdPointer(),
                                                                     markupLocation(),
                                                                     currentMarkup()));
            }
        }

        if (isNotation)
        {
            NamedResourceTableIter<Entity> entityIter = defDtd().generalEntityIter();
            for (;;)
            {
                Ptr<Entity> entity = new Ptr<Entity>(entityIter.next());
                if (entity.isNull())
                    break;
                ExternalDataEntity? external = entity.pointer()!.asExternalDataEntity();
                if (external != null)
                {
                    Notation? entityNotation = external.notation();
                    for (nuint i = 0; i < attributed.size(); i++)
                    {
                        if (attributed[i] == entityNotation)
                        {
                            Notation? attrNotation = attributed[i] as Notation;
                            AttributeList attributes = new AttributeList(attrNotation!.attributeDef());
                            attributes.finish(this);
                            external.setNotation(new ConstPtr<Notation>(attrNotation), attributes);
                        }
                    }
                }
            }
        }
        return true;
    }

    // Boolean parseAttributed(unsigned declInputLevel, Param &parm,
    //                         Vector<Attributed *> &attributed, Boolean &isNotation);
    protected Boolean parseAttributed(uint declInputLevel, Param parm,
                                       Vector<IAttributed?> attributed, out Boolean isNotation)
    {
        byte indNOTATION = (byte)(Param.indicatedReservedName + (byte)Syntax.ReservedName.rNOTATION);
        byte indALL = (byte)(Param.indicatedReservedName + (byte)Syntax.ReservedName.rALL);
        byte indIMPLICIT = (byte)(Param.indicatedReservedName + (byte)Syntax.ReservedName.rIMPLICIT);

        AllowedParams allowNameGroupNotation = new AllowedParams(Param.name, Param.nameGroup, indNOTATION);
        AllowedParams allowNameGroupNotationAll = new AllowedParams(Param.name, Param.nameGroup,
                                                                     indNOTATION, indALL, indIMPLICIT);

        if (!parseParam(haveDefLpd() ? allowNameGroupNotation : allowNameGroupNotationAll,
                       declInputLevel, parm))
        {
            isNotation = false;
            return false;
        }

        if (parm.type == indNOTATION)
        {
            if (options().warnDataAttributes)
                message(ParserMessages.dataAttributes);
            isNotation = true;
            AllowedParams allowNameNameGroup = new AllowedParams(Param.name, Param.nameGroup);
            AllowedParams allowNameGroupAll = new AllowedParams(Param.name, Param.nameGroup, indALL, indIMPLICIT);
            if (!parseParam(haveDefLpd() ? allowNameNameGroup : allowNameGroupAll,
                           declInputLevel, parm))
                return false;
            if (parm.type == Param.nameGroup)
            {
                attributed.resize(parm.nameTokenVector.size());
                for (nuint i = 0; i < attributed.size(); i++)
                    attributed[i] = lookupCreateNotation(parm.nameTokenVector[(int)i].name).pointer();
            }
            else
            {
                if (parm.type != Param.name && !hadAfdrDecl() && !sd().www())
                {
                    message(ParserMessages.missingAfdrDecl);
                    setHadAfdrDecl();
                }
                attributed.resize(1);
                attributed[0] = lookupCreateNotation(parm.type == Param.name
                                                     ? parm.token
                                                     : syntax().rniReservedName((Syntax.ReservedName)(parm.type - Param.indicatedReservedName))).pointer();
            }
        }
        else
        {
            isNotation = false;
            if (parm.type == Param.nameGroup)
            {
                if (options().warnAttlistGroupDecl)
                    message(ParserMessages.attlistGroupDecl);
                attributed.resize(parm.nameTokenVector.size());
                for (nuint i = 0; i < attributed.size(); i++)
                    attributed[i] = lookupCreateElement(parm.nameTokenVector[(int)i].name);
            }
            else
            {
                if (parm.type != Param.name && !hadAfdrDecl() && !sd().www())
                {
                    message(ParserMessages.missingAfdrDecl);
                    setHadAfdrDecl();
                }
                attributed.resize(1);
                attributed[0] = lookupCreateElement(parm.type == Param.name
                                                    ? parm.token
                                                    : syntax().rniReservedName((Syntax.ReservedName)(parm.type - Param.indicatedReservedName)));
            }
        }
        return true;
    }

    // Boolean parseDeclaredValue(unsigned declInputLevel, Boolean isNotation,
    //                            Param &parm, Owner<DeclaredValue> &declaredValue);
    protected Boolean parseDeclaredValue(uint declInputLevel, Boolean isNotation, Param parm,
                                          Owner<DeclaredValue> declaredValue)
    {
        byte rCDATA = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA);
        byte rENTITY = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rENTITY);
        byte rENTITIES = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rENTITIES);
        byte rID = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rID);
        byte rIDREF = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rIDREF);
        byte rIDREFS = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rIDREFS);
        byte rNAME = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNAME);
        byte rNAMES = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNAMES);
        byte rNMTOKEN = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNMTOKEN);
        byte rNMTOKENS = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNMTOKENS);
        byte rNUMBER = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNUMBER);
        byte rNUMBERS = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNUMBERS);
        byte rNUTOKEN = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNUTOKEN);
        byte rNUTOKENS = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNUTOKENS);
        byte rNOTATION = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNOTATION);
        byte rDATA = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rDATA);

        byte[] declaredValues = new byte[]
        {
            rCDATA, rENTITY, rENTITIES, rID, rIDREF, rIDREFS,
            rNAME, rNAMES, rNMTOKEN, rNMTOKENS, rNUMBER, rNUMBERS,
            rNUTOKEN, rNUTOKENS, rNOTATION, Param.nameTokenGroup, rDATA
        };

        AllowedParams allowDeclaredValue = new AllowedParams(declaredValues, declaredValues.Length - 1);
        AllowedParams allowDeclaredValueData = new AllowedParams(declaredValues, declaredValues.Length);

        if (!parseParam(sd().www() ? allowDeclaredValueData : allowDeclaredValue,
                       declInputLevel, parm))
            return false;

        const int asDataAttribute = 0x01;
        const int asLinkAttribute = 0x02;
        int allowedFlags = asDataAttribute | asLinkAttribute;

        if (parm.type == rCDATA)
        {
            declaredValue.operatorAssign(new CdataDeclaredValue());
        }
        else if (parm.type == rENTITY)
        {
            declaredValue.operatorAssign(new EntityDeclaredValue(false));
            allowedFlags = asLinkAttribute;
        }
        else if (parm.type == rENTITIES)
        {
            declaredValue.operatorAssign(new EntityDeclaredValue(true));
            allowedFlags = asLinkAttribute;
        }
        else if (parm.type == rID)
        {
            declaredValue.operatorAssign(new IdDeclaredValue());
            allowedFlags = 0;
        }
        else if (parm.type == rIDREF)
        {
            declaredValue.operatorAssign(new IdrefDeclaredValue(false));
            allowedFlags = 0;
        }
        else if (parm.type == rIDREFS)
        {
            declaredValue.operatorAssign(new IdrefDeclaredValue(true));
            allowedFlags = 0;
        }
        else if (parm.type == rNAME)
        {
            declaredValue.operatorAssign(new TokenizedDeclaredValue(TokenizedDeclaredValue.TokenType.name, false));
            if (options().warnNameDeclaredValue)
                message(ParserMessages.nameDeclaredValue);
        }
        else if (parm.type == rNAMES)
        {
            declaredValue.operatorAssign(new TokenizedDeclaredValue(TokenizedDeclaredValue.TokenType.name, true));
            if (options().warnNameDeclaredValue)
                message(ParserMessages.nameDeclaredValue);
        }
        else if (parm.type == rNMTOKEN)
        {
            declaredValue.operatorAssign(new TokenizedDeclaredValue(TokenizedDeclaredValue.TokenType.nameToken, false));
        }
        else if (parm.type == rNMTOKENS)
        {
            declaredValue.operatorAssign(new TokenizedDeclaredValue(TokenizedDeclaredValue.TokenType.nameToken, true));
        }
        else if (parm.type == rNUMBER)
        {
            declaredValue.operatorAssign(new TokenizedDeclaredValue(TokenizedDeclaredValue.TokenType.number, false));
            if (options().warnNumberDeclaredValue)
                message(ParserMessages.numberDeclaredValue);
        }
        else if (parm.type == rNUMBERS)
        {
            declaredValue.operatorAssign(new TokenizedDeclaredValue(TokenizedDeclaredValue.TokenType.number, true));
            if (options().warnNumberDeclaredValue)
                message(ParserMessages.numberDeclaredValue);
        }
        else if (parm.type == rNUTOKEN)
        {
            declaredValue.operatorAssign(new TokenizedDeclaredValue(TokenizedDeclaredValue.TokenType.numberToken, false));
            if (options().warnNutokenDeclaredValue)
                message(ParserMessages.nutokenDeclaredValue);
        }
        else if (parm.type == rNUTOKENS)
        {
            declaredValue.operatorAssign(new TokenizedDeclaredValue(TokenizedDeclaredValue.TokenType.numberToken, true));
            if (options().warnNutokenDeclaredValue)
                message(ParserMessages.nutokenDeclaredValue);
        }
        else if (parm.type == rNOTATION)
        {
            AllowedParams allowNameGroup = new AllowedParams(Param.nameGroup);
            if (!parseParam(allowNameGroup, declInputLevel, parm))
                return false;
            Vector<StringC> group = new Vector<StringC>(parm.nameTokenVector.size());
            for (nuint i = 0; i < group.size(); i++)
                parm.nameTokenVector[(int)i].name.swap(group[i]);
            declaredValue.operatorAssign(new NotationDeclaredValue(group));
            allowedFlags = 0;
        }
        else if (parm.type == Param.nameTokenGroup)
        {
            Vector<StringC> group = new Vector<StringC>(parm.nameTokenVector.size());
            Vector<StringC> origGroup = new Vector<StringC>(parm.nameTokenVector.size());
            for (nuint i = 0; i < group.size(); i++)
            {
                parm.nameTokenVector[(int)i].name.swap(group[i]);
                parm.nameTokenVector[(int)i].origName.swap(origGroup[i]);
            }
            GroupDeclaredValue grpVal = new NameTokenGroupDeclaredValue(group);
            grpVal.setOrigAllowedValues(origGroup);
            declaredValue.operatorAssign(grpVal);
        }
        else if (parm.type == rDATA)
        {
            AllowedParams allowName = new AllowedParams(Param.name);
            if (!parseParam(allowName, declInputLevel, parm))
                return false;
            Ptr<Notation> notation = new Ptr<Notation>(lookupCreateNotation(parm.token).pointer());
            AllowedParams allowDsoSilent = new AllowedParams(Param.dso, Param.silent);
            AttributeList attributes = new AttributeList(notation.pointer()!.attributeDef());
            if (parseParam(allowDsoSilent, declInputLevel, parm)
                && parm.type == Param.dso)
            {
                if (attributes.size() == 0 && !sd().www())
                    message(ParserMessages.notationNoAttributes,
                            new StringMessageArg(notation.pointer()!.name()));
                Boolean netEnabling;
                Ptr<AttributeDefinitionList> newAttDef = new Ptr<AttributeDefinitionList>();
                if (!parseAttributeSpec(Mode.asMode, attributes, out netEnabling, newAttDef))
                    return false;
                if (!newAttDef.isNull())
                {
                    newAttDef.pointer()!.setIndex(defDtd().allocAttributeDefinitionListIndex());
                    notation.pointer()!.setAttributeDef(newAttDef);
                }
                if (attributes.nSpec() == 0)
                    message(ParserMessages.emptyDataAttributeSpec);
            }
            else
            {
                attributes.finish(this);
                // unget the first token of the default value
                currentInput()!.ungetToken();
            }
            ConstPtr<Notation> nt = new ConstPtr<Notation>(notation.pointer());
            declaredValue.operatorAssign(new DataDeclaredValue(nt, attributes));
        }
        else
        {
            // CANNOT_HAPPEN()
            throw new InvalidOperationException("Unexpected parameter type in parseDeclaredValue");
        }

        if (isNotation)
        {
            if ((allowedFlags & asDataAttribute) == 0)
                message(ParserMessages.dataAttributeDeclaredValue);
        }
        else if (haveDefLpd() && !isNotation && (allowedFlags & asLinkAttribute) == 0)
            message(ParserMessages.linkAttributeDeclaredValue);

        return true;
    }

    // Boolean parseDefaultValue(unsigned declInputLevel, Boolean isNotation, Param &parm,
    //                           const StringC &attributeName, Owner<DeclaredValue> &declaredValue,
    //                           Owner<AttributeDefinition> &def, Boolean &anyCurrent);
    protected Boolean parseDefaultValue(uint declInputLevel, Boolean isNotation, Param parm,
                                         StringC attributeName, Owner<DeclaredValue> declaredValue,
                                         Owner<AttributeDefinition> def, ref Boolean anyCurrent)
    {
        byte indFIXED = (byte)(Param.indicatedReservedName + (byte)Syntax.ReservedName.rFIXED);
        byte indREQUIRED = (byte)(Param.indicatedReservedName + (byte)Syntax.ReservedName.rREQUIRED);
        byte indCURRENT = (byte)(Param.indicatedReservedName + (byte)Syntax.ReservedName.rCURRENT);
        byte indCONREF = (byte)(Param.indicatedReservedName + (byte)Syntax.ReservedName.rCONREF);
        byte indIMPLIED = (byte)(Param.indicatedReservedName + (byte)Syntax.ReservedName.rIMPLIED);

        AllowedParams allowDefaultValue = new AllowedParams(indFIXED, indREQUIRED, indCURRENT,
                                                             indCONREF, indIMPLIED,
                                                             Param.attributeValue, Param.attributeValueLiteral);
        AllowedParams allowTokenDefaultValue = new AllowedParams(indFIXED, indREQUIRED, indCURRENT,
                                                                  indCONREF, indIMPLIED,
                                                                  Param.attributeValue, Param.tokenizedAttributeValueLiteral);

        if (!parseParam(declaredValue.pointer()!.tokenized() ? allowTokenDefaultValue : allowDefaultValue,
                       declInputLevel, parm))
            return false;

        if (parm.type == indFIXED)
        {
            AllowedParams allowValue = new AllowedParams(Param.attributeValue, Param.attributeValueLiteral);
            AllowedParams allowTokenValue = new AllowedParams(Param.attributeValue, Param.tokenizedAttributeValueLiteral);
            if (!parseParam(declaredValue.pointer()!.tokenized() ? allowTokenValue : allowValue,
                           declInputLevel, parm))
                return false;
            uint specLength = 0;
            AttributeValue? value = declaredValue.pointer()!.makeValue(parm.literalText, this,
                                                                        attributeName, ref specLength);
            if (declaredValue.pointer()!.isId())
                message(ParserMessages.idDeclaredValue);
            def.operatorAssign(new FixedAttributeDefinition(attributeName, declaredValue.extract(), value));
        }
        else if (parm.type == Param.attributeValue)
        {
            if (options().warnAttributeValueNotLiteral)
                message(ParserMessages.attributeValueNotLiteral);
            // fall through to common handling with attributeValueLiteral
            uint specLength = 0;
            AttributeValue? value = declaredValue.pointer()!.makeValue(parm.literalText, this,
                                                                        attributeName, ref specLength);
            if (declaredValue.pointer()!.isId())
                message(ParserMessages.idDeclaredValue);
            def.operatorAssign(new DefaultAttributeDefinition(attributeName, declaredValue.extract(), value));
        }
        else if (parm.type == Param.attributeValueLiteral || parm.type == Param.tokenizedAttributeValueLiteral)
        {
            uint specLength = 0;
            AttributeValue? value = declaredValue.pointer()!.makeValue(parm.literalText, this,
                                                                        attributeName, ref specLength);
            if (declaredValue.pointer()!.isId())
                message(ParserMessages.idDeclaredValue);
            def.operatorAssign(new DefaultAttributeDefinition(attributeName, declaredValue.extract(), value));
        }
        else if (parm.type == indREQUIRED)
        {
            def.operatorAssign(new RequiredAttributeDefinition(attributeName, declaredValue.extract()));
        }
        else if (parm.type == indCURRENT)
        {
            anyCurrent = true;
            if (declaredValue.pointer()!.isId())
                message(ParserMessages.idDeclaredValue);
            def.operatorAssign(new CurrentAttributeDefinition(attributeName, declaredValue.extract(),
                                                               defDtd().allocCurrentAttributeIndex()));
            if (isNotation)
                message(ParserMessages.dataAttributeDefaultValue);
            else if (haveDefLpd())
                message(ParserMessages.linkAttributeDefaultValue);
            else if (options().warnCurrent)
                message(ParserMessages.currentAttribute);
        }
        else if (parm.type == indCONREF)
        {
            if (declaredValue.pointer()!.isId())
                message(ParserMessages.idDeclaredValue);
            if (declaredValue.pointer()!.isNotation())
                message(ParserMessages.notationConref);
            def.operatorAssign(new ConrefAttributeDefinition(attributeName, declaredValue.extract()));
            if (isNotation)
                message(ParserMessages.dataAttributeDefaultValue);
            else if (haveDefLpd())
                message(ParserMessages.linkAttributeDefaultValue);
            else if (options().warnConref)
                message(ParserMessages.conrefAttribute);
        }
        else if (parm.type == indIMPLIED)
        {
            def.operatorAssign(new ImpliedAttributeDefinition(attributeName, declaredValue.extract()));
        }
        else
        {
            // CANNOT_HAPPEN()
            throw new InvalidOperationException("Unexpected parameter type in parseDefaultValue");
        }
        return true;
    }

    // Boolean parseNotationDecl();
    protected virtual Boolean parseNotationDecl()
    {
        uint declInputLevel = inputLevel();
        Param parm = new Param();
        AllowedParams allowName = new AllowedParams(Param.name);
        if (!parseParam(allowName, declInputLevel, parm))
            return false;
        ConstPtr<Notation> nt = lookupCreateNotation(parm.token);
        if (validate() && nt.pointer()!.defined())
            message(ParserMessages.duplicateNotationDeclaration,
                    new StringMessageArg(parm.token));
        AttributeDefinitionList? atts = nt.pointer()!.attributeDef().pointer();
        if (atts != null)
        {
            for (nuint i = 0; i < atts.size(); i++)
            {
                Boolean @implicit;
                if (atts.def(i)!.isSpecified(out @implicit) && @implicit)
                {
                    message(ParserMessages.notationMustNotBeDeclared,
                            new StringMessageArg(parm.token));
                    break;
                }
            }
        }
        AllowedParams allowPublicSystem = new AllowedParams(
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rPUBLIC),
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSYSTEM));
        if (!parseParam(allowPublicSystem, declInputLevel, parm))
            return false;

        AllowedParams allowSystemIdentifierMdc = new AllowedParams(Param.systemIdentifier, Param.mdc);
        AllowedParams allowMdc = new AllowedParams(Param.mdc);

        ExternalId id = new ExternalId();
        if (!parseExternalId(allowSystemIdentifierMdc, allowMdc,
                             parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSYSTEM),
                             declInputLevel, parm, id))
            return false;
        if (validate() && sd().formal())
        {
            PublicId.TextClass textClass;
            PublicId? publicId = id.publicId();
            if (publicId != null
                && publicId.getTextClass(out textClass)
                && textClass != PublicId.TextClass.NOTATION)
                message(ParserMessages.notationIdentifierTextClass);
        }
        if (!nt.pointer()!.defined())
        {
            // Cast away const - Notation needs to be mutable here
            Notation ntMut = (Notation)nt.pointer()!;
            ntMut.setExternalId(id, markupLocation());
            ntMut.generateSystemId(this);
            if (currentMarkup() != null)
                eventHandler().notationDecl(new NotationDeclEvent(
                    new ConstPtr<Notation>(ntMut), markupLocation(), currentMarkup()));
        }
        return true;
    }

    // Boolean parseEntityDecl();
    protected virtual Boolean parseEntityDecl()
    {
        uint declInputLevel = inputLevel();
        Param parm = new Param();

        byte indDEFAULT = (byte)(Param.indicatedReservedName + (byte)Syntax.ReservedName.rDEFAULT);
        AllowedParams allowEntityNamePero = new AllowedParams(Param.entityName, indDEFAULT, Param.pero);

        if (!parseParam(allowEntityNamePero, declInputLevel, parm))
            return false;

        Entity.DeclType declType;
        StringC name = new StringC();  // empty for default entity
        if (parm.type == Param.pero)
        {
            declType = Entity.DeclType.parameterEntity;
            AllowedParams allowParamEntityName = new AllowedParams(Param.paramEntityName);
            if (!parseParam(allowParamEntityName, declInputLevel, parm))
                return false;
            parm.token.swap(name);
        }
        else
        {
            declType = Entity.DeclType.generalEntity;
            if (parm.type == Param.entityName)
                parm.token.swap(name);
            else if (sd().implydefEntity())
                message(ParserMessages.implydefEntityDefault);
            else if (options().warnDefaultEntityDecl)
                message(ParserMessages.defaultEntityDecl);
        }

        byte rCDATA = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA);
        byte rSDATA = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSDATA);
        byte rPI = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rPI);
        byte rSTARTTAG = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSTARTTAG);
        byte rENDTAG = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rENDTAG);
        byte rMS = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rMS);
        byte rMD = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rMD);
        byte rSYSTEM = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSYSTEM);
        byte rPUBLIC = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rPUBLIC);

        AllowedParams allowEntityTextType = new AllowedParams(
            Param.paramLiteral, rCDATA, rSDATA, rPI, rSTARTTAG, rENDTAG, rMS, rMD, rSYSTEM, rPUBLIC);

        if (!parseParam(allowEntityTextType, declInputLevel, parm))
            return false;

        Location typeLocation = new Location(currentLocation());
        Entity.DataType dataType = Entity.DataType.sgmlText;
        InternalTextEntity.Bracketed bracketed = InternalTextEntity.Bracketed.none;

        if (parm.type == rSYSTEM || parm.type == rPUBLIC)
            return parseExternalEntity(name, declType, declInputLevel, parm);

        if (parm.type == rCDATA)
        {
            dataType = Entity.DataType.cdata;
            if (options().warnInternalCdataEntity)
                message(ParserMessages.internalCdataEntity);
        }
        else if (parm.type == rSDATA)
        {
            dataType = Entity.DataType.sdata;
            if (options().warnInternalSdataEntity)
                message(ParserMessages.internalSdataEntity);
        }
        else if (parm.type == rPI)
        {
            dataType = Entity.DataType.pi;
            if (options().warnPiEntity)
                message(ParserMessages.piEntity);
        }
        else if (parm.type == rSTARTTAG)
        {
            bracketed = InternalTextEntity.Bracketed.starttag;
            if (options().warnBracketEntity)
                message(ParserMessages.bracketEntity);
        }
        else if (parm.type == rENDTAG)
        {
            bracketed = InternalTextEntity.Bracketed.endtag;
            if (options().warnBracketEntity)
                message(ParserMessages.bracketEntity);
        }
        else if (parm.type == rMS)
        {
            bracketed = InternalTextEntity.Bracketed.ms;
            if (options().warnBracketEntity)
                message(ParserMessages.bracketEntity);
        }
        else if (parm.type == rMD)
        {
            bracketed = InternalTextEntity.Bracketed.md;
            if (options().warnBracketEntity)
                message(ParserMessages.bracketEntity);
        }

        if (parm.type != Param.paramLiteral)
        {
            AllowedParams allowParamLiteral = new AllowedParams(Param.paramLiteral);
            if (!parseParam(allowParamLiteral, declInputLevel, parm))
                return false;
        }

        Text text = new Text();
        parm.literalText.swap(text);
        if (bracketed != InternalTextEntity.Bracketed.none)
        {
            StringC open = new StringC();
            StringC close = new StringC();
            switch (bracketed)
            {
                case InternalTextEntity.Bracketed.starttag:
                    open = instanceSyntax().delimGeneral((int)Syntax.DelimGeneral.dSTAGO);
                    close = instanceSyntax().delimGeneral((int)Syntax.DelimGeneral.dTAGC);
                    break;
                case InternalTextEntity.Bracketed.endtag:
                    open = instanceSyntax().delimGeneral((int)Syntax.DelimGeneral.dETAGO);
                    close = instanceSyntax().delimGeneral((int)Syntax.DelimGeneral.dTAGC);
                    break;
                case InternalTextEntity.Bracketed.ms:
                    {
                        Syntax syn = (declType == Entity.DeclType.parameterEntity) ? syntax() : instanceSyntax();
                        open = syn.delimGeneral((int)Syntax.DelimGeneral.dMDO);
                        open.operatorPlusAssign(syn.delimGeneral((int)Syntax.DelimGeneral.dDSO));
                        close = syn.delimGeneral((int)Syntax.DelimGeneral.dMSC);
                        close.operatorPlusAssign(syn.delimGeneral((int)Syntax.DelimGeneral.dMDC));
                    }
                    break;
                case InternalTextEntity.Bracketed.md:
                    {
                        Syntax syn = (declType == Entity.DeclType.parameterEntity) ? syntax() : instanceSyntax();
                        open = syn.delimGeneral((int)Syntax.DelimGeneral.dMDO);
                        close = syn.delimGeneral((int)Syntax.DelimGeneral.dMDC);
                    }
                    break;
                default:
                    throw new InvalidOperationException("CANNOT_HAPPEN");
            }
            text.insertChars(open, new Location(new BracketOrigin(typeLocation, BracketOrigin.Position.open), 0));
            text.addChars(close, new Location(new BracketOrigin(typeLocation, BracketOrigin.Position.close), 0));
            if (text.size() > syntax().litlen()
                && text.size() - open.size() - close.size() <= syntax().litlen())
                message(ParserMessages.bracketedLitlen,
                        new NumberMessageArg(syntax().litlen()));
        }

        AllowedParams allowMdc = new AllowedParams(Param.mdc);
        if (!parseParam(allowMdc, declInputLevel, parm))
            return false;

        if (declType == Entity.DeclType.parameterEntity
            && (dataType == Entity.DataType.cdata || dataType == Entity.DataType.sdata))
        {
            message(ParserMessages.internalParameterDataEntity,
                    new StringMessageArg(name));
            return true;
        }

        Ptr<Entity> entity = new Ptr<Entity>();
        switch (dataType)
        {
            case Entity.DataType.cdata:
                entity.operatorAssign(new InternalCdataEntity(name, markupLocation(), text));
                break;
            case Entity.DataType.sdata:
                entity.operatorAssign(new InternalSdataEntity(name, markupLocation(), text));
                break;
            case Entity.DataType.pi:
                entity.operatorAssign(new PiEntity(name, declType, markupLocation(), text));
                break;
            case Entity.DataType.sgmlText:
                entity.operatorAssign(new InternalTextEntity(name, declType, markupLocation(), text, bracketed));
                break;
            default:
                throw new InvalidOperationException("CANNOT_HAPPEN");
        }
        maybeDefineEntity(entity);
        return true;
    }

    // Boolean parseExternalEntity(StringC &name, Entity::DeclType declType, unsigned declInputLevel, Param &parm);
    protected Boolean parseExternalEntity(StringC name, Entity.DeclType declType,
                                          uint declInputLevel, Param parm)
    {
        byte rSUBDOC = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSUBDOC);
        byte rCDATA = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA);
        byte rSDATA = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSDATA);
        byte rNDATA = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNDATA);

        AllowedParams allowSystemIdentifierEntityTypeMdc = new AllowedParams(
            Param.systemIdentifier, rSUBDOC, rCDATA, rSDATA, rNDATA, Param.mdc);
        AllowedParams allowEntityTypeMdc = new AllowedParams(
            rSUBDOC, rCDATA, rSDATA, rNDATA, Param.mdc);

        ExternalId id = new ExternalId();
        if (!parseExternalId(allowSystemIdentifierEntityTypeMdc, allowEntityTypeMdc,
                             true, declInputLevel, parm, id))
            return false;

        if (parm.type == Param.mdc)
        {
            maybeDefineEntity(new Ptr<Entity>(new ExternalTextEntity(name, declType, markupLocation(), id)));
            return true;
        }

        Ptr<Entity> entity = new Ptr<Entity>();
        if (parm.type == rSUBDOC)
        {
            if (sd().subdoc() == 0)
                message(ParserMessages.subdocEntity, new StringMessageArg(name));
            AllowedParams allowMdc = new AllowedParams(Param.mdc);
            if (!parseParam(allowMdc, declInputLevel, parm))
                return false;
            entity.operatorAssign(new SubdocEntity(name, markupLocation(), id));
        }
        else
        {
            Entity.DataType dataType;
            if (parm.type == rCDATA)
            {
                dataType = Entity.DataType.cdata;
                if (options().warnExternalCdataEntity)
                    message(ParserMessages.externalCdataEntity);
            }
            else if (parm.type == rSDATA)
            {
                dataType = Entity.DataType.sdata;
                if (options().warnExternalSdataEntity)
                    message(ParserMessages.externalSdataEntity);
            }
            else if (parm.type == rNDATA)
            {
                dataType = Entity.DataType.ndata;
            }
            else
            {
                throw new InvalidOperationException("CANNOT_HAPPEN");
            }

            AllowedParams allowName = new AllowedParams(Param.name);
            if (!parseParam(allowName, declInputLevel, parm))
                return false;
            ConstPtr<Notation> notation = lookupCreateNotation(parm.token);

            AllowedParams allowDsoMdc = new AllowedParams(Param.dso, Param.mdc);
            if (!parseParam(allowDsoMdc, declInputLevel, parm))
                return false;

            AttributeList attributes = new AttributeList(notation.pointer()!.attributeDef());
            if (parm.type == Param.dso)
            {
                if (attributes.size() == 0 && !sd().www())
                    message(ParserMessages.notationNoAttributes,
                            new StringMessageArg(notation.pointer()!.name()));
                Boolean netEnabling;
                Ptr<AttributeDefinitionList> newAttDef = new Ptr<AttributeDefinitionList>();
                if (!parseAttributeSpec(Mode.asMode, attributes, out netEnabling, newAttDef))
                    return false;
                if (!newAttDef.isNull())
                {
                    newAttDef.pointer()!.setIndex(defDtd().allocAttributeDefinitionListIndex());
                    ((Notation)notation.pointer()!).setAttributeDef(newAttDef);
                }
                if (attributes.nSpec() == 0)
                    message(ParserMessages.emptyDataAttributeSpec);
                AllowedParams allowMdc2 = new AllowedParams(Param.mdc);
                if (!parseParam(allowMdc2, declInputLevel, parm))
                    return false;
            }
            else
            {
                attributes.finish(this);
            }
            entity.operatorAssign(new ExternalDataEntity(name, dataType, markupLocation(), id,
                                   notation, attributes,
                                   declType == Entity.DeclType.parameterEntity
                                   ? Entity.DeclType.parameterEntity
                                   : Entity.DeclType.generalEntity));
        }

        if (declType == Entity.DeclType.parameterEntity && !sd().www())
        {
            message(ParserMessages.externalParameterDataSubdocEntity,
                    new StringMessageArg(name));
            return true;
        }
        maybeDefineEntity(entity);
        return true;
    }

    // void maybeDefineEntity(const Ptr<Entity> &entity);
    protected void maybeDefineEntity(Ptr<Entity> entity)
    {
        Dtd dtd = defDtd();
        if (haveDefLpd())
            entity.pointer()!.setDeclIn(dtd.namePointer(),
                                         dtd.isBase(),
                                         defLpd().namePointer(),
                                         defLpd().active());
        else
            entity.pointer()!.setDeclIn(dtd.namePointer(), dtd.isBase());

        Boolean ignored = false;
        if (entity.pointer()!.name().size() == 0)
        {
            Entity? oldEntity = dtd.defaultEntity().pointer();
            if (oldEntity == null
                || (!oldEntity.declInActiveLpd() && entity.pointer()!.declInActiveLpd()))
                dtd.setDefaultEntity(entity, this);
            else
            {
                ignored = true;
                if (options().warnDuplicateEntity)
                    message(ParserMessages.duplicateEntityDeclaration,
                            new StringMessageArg(syntax().rniReservedName(Syntax.ReservedName.rDEFAULT)));
            }
        }
        else
        {
            Ptr<Entity> oldEntity = dtd.insertEntity(entity);
            if (oldEntity.isNull())
                entity.pointer()!.generateSystemId(this);
            else if (oldEntity.pointer()!.defaulted())
            {
                dtd.insertEntity(entity, true);
                message(ParserMessages.defaultedEntityDefined,
                        new StringMessageArg(entity.pointer()!.name()));
                entity.pointer()!.generateSystemId(this);
            }
            else
            {
                if (entity.pointer()!.declInActiveLpd() && !oldEntity.pointer()!.declInActiveLpd())
                {
                    dtd.insertEntity(entity, true);
                    entity.pointer()!.generateSystemId(this);
                }
                else
                {
                    ignored = true;
                    if (options().warnDuplicateEntity)
                        message(entity.pointer()!.declType() == Entity.DeclType.parameterEntity
                                ? ParserMessages.duplicateParameterEntityDeclaration
                                : ParserMessages.duplicateEntityDeclaration,
                                new StringMessageArg(entity.pointer()!.name()));
                }
            }
        }
        if (currentMarkup() != null)
            eventHandler().entityDecl(new EntityDeclEvent(new ConstPtr<Entity>(entity.pointer()), ignored,
                                       markupLocation(), currentMarkup()));
    }

    protected virtual Boolean parseShortrefDecl() { throw new NotImplementedException(); }
    protected virtual Boolean parseUsemapDecl() { throw new NotImplementedException(); }
    // Boolean parseDeclarationName(Syntax::ReservedName *result, Boolean allowAfdr = 0);
    protected Boolean parseDeclarationName(out Syntax.ReservedName result, Boolean allowAfdr = false)
    {
        currentInput()!.discardInitial();
        extendNameToken(syntax().namelen(), ParserMessages.nameLength);
        StringC name = nameBuffer();
        getCurrentToken(syntax().generalSubstTable(), name);
        if (!syntax().lookupReservedName(name, out result))
        {
            if (allowAfdr && name == sd().execToInternal("AFDR"))
            {
                result = Syntax.ReservedName.rANY;
                if (currentMarkup() != null)
                    currentMarkup()!.addName(currentInput()!);
            }
            else
            {
                result = default;
                message(ParserMessages.noSuchDeclarationType, new StringMessageArg(name));
                return false;
            }
        }
        else if (currentMarkup() != null)
            currentMarkup()!.addReservedName(result, currentInput()!);
        return true;
    }
    protected virtual Boolean parseUselinkDecl() { throw new NotImplementedException(); }

    // Boolean parseDoctypeDeclStart();
    protected virtual Boolean parseDoctypeDeclStart()
    {
        if (hadDtd() && sd().concur() == 0 && sd().explicitLink() == 0)
            message(ParserMessages.multipleDtds);
        if (hadLpd())
            message(ParserMessages.dtdAfterLpd);
        uint declInputLevel = inputLevel();
        Param parm = new Param();
        AllowedParams allowImpliedName = new AllowedParams(
            (byte)(Param.indicatedReservedName + (byte)Syntax.ReservedName.rIMPLIED),
            Param.name);
        AllowedParams allowName = new AllowedParams(Param.name);
        if (!parseParam(sd().www() ? allowImpliedName : allowName, declInputLevel, parm))
            return false;
        if (parm.type == (byte)(Param.indicatedReservedName + (byte)Syntax.ReservedName.rIMPLIED))
        {
            if (sd().concur() > 0 || sd().explicitLink() > 0)
                message(ParserMessages.impliedDoctypeConcurLink);
            message(ParserMessages.sorryImpliedDoctype);
            return false;
        }
        StringC name = new StringC();
        parm.token.swap(name);
        if (!lookupDtd(name).isNull())
            message(ParserMessages.duplicateDtd, new StringMessageArg(name));
        AllowedParams allowPublicSystemDsoMdc = new AllowedParams(
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rPUBLIC),
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSYSTEM),
            Param.dso,
            Param.mdc);
        if (!parseParam(allowPublicSystemDsoMdc, declInputLevel, parm))
            return false;
        ConstPtr<Entity> entity = new ConstPtr<Entity>();
        StringC notation = new StringC();
        EntityDecl.DataType data = EntityDecl.DataType.sgmlText;
        ExternalId id = new ExternalId();
        if (parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rPUBLIC)
            || parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSYSTEM))
        {
            AllowedParams allowSystemIdentifierDsoMdc = new AllowedParams(
                Param.systemIdentifier,
                Param.dso, Param.mdc);
            AllowedParams allowSystemIdentifierDsoMdcData = new AllowedParams(
                Param.systemIdentifier,
                Param.dso, Param.mdc,
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA),
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSDATA));
            AllowedParams allowDsoMdcData = new AllowedParams(
                Param.dso, Param.mdc,
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA),
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSDATA),
                (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNDATA));
            AllowedParams allowDsoMdc = new AllowedParams(Param.dso, Param.mdc);
            if (!parseExternalId(sd().www() ? allowSystemIdentifierDsoMdcData : allowSystemIdentifierDsoMdc,
                                 sd().www() ? allowDsoMdcData : allowDsoMdc,
                                 true, declInputLevel, parm, id))
                return false;
            if (parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA))
                data = EntityDecl.DataType.cdata;
            else if (parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rSDATA))
                data = EntityDecl.DataType.sdata;
            else if (parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rNDATA))
                data = EntityDecl.DataType.ndata;
            else
                data = EntityDecl.DataType.sgmlText;
            if (data == EntityDecl.DataType.sgmlText)
            {
                Ptr<Entity> tem = new Ptr<Entity>(
                    new ExternalTextEntity(name, EntityDecl.DeclType.doctype, markupLocation(), id));
                tem.pointer()!.generateSystemId(this);
                entity = new ConstPtr<Entity>(tem.pointer());
            }
            else
            {
                // external subset uses some DTD notation
                AllowedParams allowNameOnly = new AllowedParams(Param.name);
                if (!parseParam(allowNameOnly, declInputLevel, parm))
                    return false;
                parm.token.swap(notation);
                AllowedParams allowDsoMdcOnly = new AllowedParams(Param.dso, Param.mdc);
                if (!parseParam(allowDsoMdcOnly, declInputLevel, parm))
                    return false;
            }
        }
        else
        {
            // no external subset specified
            if (sd().implydefDoctype())
            {
                // FIXME this fails for #IMPLIED, since name isn't yet known
                Ptr<Entity> tem = new Ptr<Entity>(
                    new ExternalTextEntity(name, EntityDecl.DeclType.doctype, markupLocation(), id));
                tem.pointer()!.generateSystemId(this);
                entity = new ConstPtr<Entity>(tem.pointer());
            }
            else if (parm.type == Param.mdc)
            {
                if (sd().implydefElement() == Sd.ImplydefElement.implydefElementNo)
                {
                    message(ParserMessages.noDtdSubset);
                    enableImplydef();
                }
            }
        }
        // Discard mdc or dso
        if (currentMarkup() != null)
            currentMarkup()!.resize(currentMarkup()!.size() - 1);
        eventHandler().startDtd(new StartDtdEvent(name, entity, parm.type == Param.dso,
                                                   markupLocation(),
                                                   currentMarkup()));
        startDtd(name);
        if (notation.size() > 0)
        {
            // FIXME this case has the wrong entity in the event
            // this should be fixed by moving startDtd() call and this code up
            ConstPtr<Notation> nt = lookupCreateNotation(notation);

            AttributeList attrs = new AttributeList(nt.pointer()!.attributeDef());
            attrs.finish(this);
            Ptr<Entity> tem = new Ptr<Entity>(
                new ExternalDataEntity(name, data, markupLocation(), id, nt, attrs,
                                        EntityDecl.DeclType.doctype));
            tem.pointer()!.generateSystemId(this);
            // FIXME This is a hack; we need the entity to have the doctype name to
            // have generateSystemId() work properly, but have an empty name to add
            // it as a parameter entity, which is needed to check the notation
            StringC entname = new StringC();
            tem.pointer()!.setName(entname);
            defDtd().insertEntity(tem);
            entity = new ConstPtr<Entity>(tem.pointer());
        }
        if (parm.type == Param.mdc)
        {
            // unget the mdc
            currentInput()!.ungetToken();
            if (entity.isNull())
            {
                parseDoctypeDeclEnd();
                return true;
            }
            // reference the entity
            Ptr<EntityOrigin> origin = new Ptr<EntityOrigin>(EntityOrigin.make(internalAllocator(), entity, currentLocation()));
            entity.pointer()!.dsReference(this, origin);
            if (inputLevel() == 1)
            {
                // reference failed
                parseDoctypeDeclEnd();
                return true;
            }
        }
        else if (!entity.isNull())
            setDsEntity(entity);
        setPhase(Phase.declSubsetPhase);
        return true;
    }

    // Boolean parseDoctypeDeclEnd(Boolean fake = 0);
    protected virtual Boolean parseDoctypeDeclEnd(Boolean fake = false)
    {
        checkDtd(defDtd());
        Ptr<Dtd> tem = defDtdPointer();
        endDtd();
        if (fake)
        {
            startMarkup(eventsWanted().wantPrologMarkup(), currentLocation());
            // if (currentMarkup() != null)
            //     currentMarkup()!.addDelim(Syntax.DelimGeneral.dMDC);
        }
        else
        {
            startMarkup(eventsWanted().wantPrologMarkup(), currentLocation());
            Param parm = new Param();
            AllowedParams allowMdc = new AllowedParams(Param.mdc);
            // End DTD before parsing final param so parameter entity reference
            // not allowed between ] and >.
            if (!parseParam(allowMdc, inputLevel(), parm))
                return false;
        }
        eventHandler().endDtd(new EndDtdEvent(new ConstPtr<Dtd>(tem.pointer()),
                                               markupLocation(),
                                               currentMarkup()));
        return true;
    }

    // Boolean parseMarkedSectionDeclStart();
    protected virtual Boolean parseMarkedSectionDeclStart()
    {
        if (markedSectionLevel() == syntax().taglvl())
            message(ParserMessages.markedSectionLevel,
                    new NumberMessageArg(syntax().taglvl()));
        if (!inInstance()
            && options().warnInternalSubsetMarkedSection
            && inputLevel() == 1)
            message(ParserMessages.internalSubsetMarkedSection);
        if (markedSectionSpecialLevel() > 0)
        {
            startMarkedSection(markupLocation());
            if (inInstance()
                ? eventsWanted().wantMarkedSections()
                : eventsWanted().wantPrologMarkup())
                eventHandler().ignoredChars(new IgnoredCharsEvent(currentInput()!.currentTokenStart(),
                                                                   currentInput()!.currentTokenLength(),
                                                                   currentLocation(),
                                                                   false));
            return true;
        }
        Boolean discardMarkup;
        if (startMarkup(inInstance()
                        ? eventsWanted().wantMarkedSections()
                        : eventsWanted().wantPrologMarkup(),
                        currentLocation()) != null)
        {
            currentMarkup()!.addDelim(Syntax.DelimGeneral.dMDO);
            currentMarkup()!.addDelim(Syntax.DelimGeneral.dDSO);
            discardMarkup = false;
        }
        else if (options().warnInstanceStatusKeywordSpecS && inInstance())
        {
            startMarkup(true, currentLocation());
            discardMarkup = true;
        }
        else
            discardMarkup = false;
        uint declInputLevel = inputLevel();
        AllowedParams allowStatusDso = new AllowedParams(
            Param.dso,
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA),
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rRCDATA),
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rIGNORE),
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rINCLUDE),
            (byte)(Param.reservedName + (byte)Syntax.ReservedName.rTEMP));
        Param parm = new Param();
        MarkedSectionEvent.Status status = MarkedSectionEvent.Status.include;
        if (!parseParam(allowStatusDso, declInputLevel, parm))
            return false;
        if (options().warnMissingStatusKeyword && parm.type == Param.dso)
            message(ParserMessages.missingStatusKeyword);
        while (parm.type != Param.dso)
        {
            byte typeCDATA = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rCDATA);
            byte typeRCDATA = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rRCDATA);
            byte typeIGNORE = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rIGNORE);
            byte typeINCLUDE = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rINCLUDE);
            byte typeTEMP = (byte)(Param.reservedName + (byte)Syntax.ReservedName.rTEMP);
            if (parm.type == typeCDATA)
            {
                if (status < MarkedSectionEvent.Status.cdata)
                    status = MarkedSectionEvent.Status.cdata;
            }
            else if (parm.type == typeRCDATA)
            {
                if (status < MarkedSectionEvent.Status.rcdata)
                    status = MarkedSectionEvent.Status.rcdata;
                if (options().warnRcdataMarkedSection)
                    message(ParserMessages.rcdataMarkedSection);
            }
            else if (parm.type == typeIGNORE)
            {
                if (status < MarkedSectionEvent.Status.ignore)
                    status = MarkedSectionEvent.Status.ignore;
                if (inInstance() && options().warnInstanceIgnoreMarkedSection)
                    message(ParserMessages.instanceIgnoreMarkedSection);
            }
            else if (parm.type == typeINCLUDE)
            {
                if (inInstance() && options().warnInstanceIncludeMarkedSection)
                    message(ParserMessages.instanceIncludeMarkedSection);
            }
            else if (parm.type == typeTEMP)
            {
                if (options().warnTempMarkedSection)
                    message(ParserMessages.tempMarkedSection);
            }
            if (!parseParam(allowStatusDso, declInputLevel, parm))
                return false;
            if (options().warnMultipleStatusKeyword && parm.type != Param.dso)
                message(ParserMessages.multipleStatusKeyword);
        }
        // FIXME this disallows
        // <!entity % e "include [ stuff ">
        // ...
        // <![ %e; ]]>
        // which I think is legal.

        if (inputLevel() > declInputLevel)
            message(ParserMessages.parameterEntityNotEnded);
        switch (status)
        {
            case MarkedSectionEvent.Status.include:
                startMarkedSection(markupLocation());
                break;
            case MarkedSectionEvent.Status.cdata:
                startSpecialMarkedSection(Mode.cmsMode, markupLocation());
                break;
            case MarkedSectionEvent.Status.rcdata:
                startSpecialMarkedSection(Mode.rcmsMode, markupLocation());
                break;
            case MarkedSectionEvent.Status.ignore:
                startSpecialMarkedSection(Mode.imsMode, markupLocation());
                break;
        }
        if (currentMarkup() != null)
        {
            if (options().warnInstanceStatusKeywordSpecS && inInstance())
            {
                Location loc = new Location(markupLocation());
                for (MarkupIter iter = new MarkupIter(currentMarkup()!); iter.valid(); iter.advance(ref loc, syntaxPointer()))
                {
                    if (iter.type() == Markup.Type.s)
                    {
                        setNextLocation(loc);
                        message(ParserMessages.instanceStatusKeywordSpecS);
                    }
                }
                if (discardMarkup)
                    startMarkup(false, markupLocation());
            }
            eventHandler().markedSectionStart(new MarkedSectionStartEvent(status,
                                                                           markupLocation(),
                                                                           currentMarkup()));
        }
        return true;
    }

    // void handleMarkedSectionEnd();
    protected virtual void handleMarkedSectionEnd()
    {
        if (markedSectionLevel() == 0)
            message(ParserMessages.markedSectionEnd);
        else
        {
            if (inInstance()
                ? eventsWanted().wantMarkedSections()
                : eventsWanted().wantPrologMarkup())
            {
                if (markedSectionSpecialLevel() > 1)
                    eventHandler().ignoredChars(new IgnoredCharsEvent(currentInput()!.currentTokenStart(),
                                                                       currentInput()!.currentTokenLength(),
                                                                       currentLocation(),
                                                                       false));
                else
                {
                    MarkedSectionEvent.Status status;
                    switch (currentMode())
                    {
                        case Mode.cmsMode:
                            status = MarkedSectionEvent.Status.cdata;
                            break;
                        case Mode.rcmsMode:
                            status = MarkedSectionEvent.Status.rcdata;
                            break;
                        case Mode.imsMode:
                            status = MarkedSectionEvent.Status.ignore;
                            break;
                        default:
                            status = MarkedSectionEvent.Status.include;
                            break;
                    }
                    startMarkup(true, currentLocation());
                    currentMarkup()!.addDelim(Syntax.DelimGeneral.dMSC);
                    currentMarkup()!.addDelim(Syntax.DelimGeneral.dMDC);
                    eventHandler().markedSectionEnd(new MarkedSectionEndEvent(status,
                                                                               markupLocation(),
                                                                               currentMarkup()));
                }
            }
            endMarkedSection();
        }
    }
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

    // Boolean parseParam(const AllowedParams &allow, unsigned declInputLevel, Param &parm);
    protected virtual Boolean parseParam(AllowedParams allow, uint declInputLevel, Param parm)
    {
        for (;;)
        {
            Token token = getToken(allow.mainMode());
            switch (token)
            {
                case Tokens.tokenUnrecognized:
                    if (reportNonSgmlCharacter())
                        break;
                    message(ParserMessages.markupDeclarationCharacter,
                            new StringMessageArg(currentToken()),
                            new AllowedParamsMessageArg(allow, syntaxPointer()));
                    return false;
                case Tokens.tokenEe:
                    if (inputLevel() <= declInputLevel)
                    {
                        message(ParserMessages.declarationLevel);
                        return false;
                    }
                    if (currentMarkup() != null)
                        currentMarkup()!.addEntityEnd();
                    popInputStack();
                    break;
                case Tokens.tokenCom:
                    if (!parseComment(Mode.comMode))
                        return false;
                    if (options().warnPsComment)
                        message(ParserMessages.psComment);
                    break;
                case Tokens.tokenDso:
                    if (!allow.dso())
                    {
                        paramInvalidToken(token, allow);
                        return false;
                    }
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dDSO);
                    parm.type = Param.dso;
                    return true;
                case Tokens.tokenGrpo:
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dGRPO);
                    switch (allow.group())
                    {
                        case Param.invalid:
                            paramInvalidToken(token, allow);
                            return false;
                        case Param.modelGroup:
                            {
                                ModelGroup? group;
                                if (!parseModelGroup(1, declInputLevel, out group, Mode.grpsufMode))
                                    return false;
                                parm.type = Param.modelGroup;
                                parm.modelGroupPtr = new Owner<ModelGroup>(group);
                            }
                            break;
                        case Param.nameGroup:
                            if (!parseNameGroup(declInputLevel, parm))
                                return false;
                            break;
                        case Param.nameTokenGroup:
                            if (!parseNameTokenGroup(declInputLevel, parm))
                                return false;
                            break;
                        default:
                            throw new InvalidOperationException("CANNOT_HAPPEN");
                    }
                    parm.type = allow.group();
                    return true;
                case Tokens.tokenLita:
                case Tokens.tokenLit:
                    parm.type = allow.literal();
                    parm.lita = token == Tokens.tokenLita;
                    switch (allow.literal())
                    {
                        case Param.invalid:
                            paramInvalidToken(token, allow);
                            return false;
                        case Param.minimumLiteral:
                            if (!parseMinimumLiteral(parm.lita, parm.literalText))
                                return false;
                            break;
                        case Param.attributeValueLiteral:
                            if (!parseAttributeValueLiteral(parm.lita, parm.literalText))
                                return false;
                            break;
                        case Param.tokenizedAttributeValueLiteral:
                            if (!parseTokenizedAttributeValueLiteral(parm.lita, parm.literalText))
                                return false;
                            break;
                        case Param.systemIdentifier:
                            if (!parseSystemIdentifier(parm.lita, parm.literalText))
                                return false;
                            break;
                        case Param.paramLiteral:
                            if (!parseParameterLiteral(parm.lita, parm.literalText))
                                return false;
                            break;
                    }
                    if (currentMarkup() != null)
                        currentMarkup()!.addLiteral(parm.literalText);
                    return true;
                case Tokens.tokenMdc:
                    if (!allow.mdc())
                    {
                        paramInvalidToken(token, allow);
                        return false;
                    }
                    if (inputLevel() > declInputLevel)
                        message(ParserMessages.parameterEntityNotEnded);
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dMDC);
                    parm.type = Param.mdc;
                    return true;
                case Tokens.tokenMinus:
                    parm.type = Param.minus;
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dMINUS);
                    return true;
                case Tokens.tokenMinusGrpo:
                    if (!allow.exclusions())
                    {
                        paramInvalidToken(token, allow);
                        return false;
                    }
                    if (currentMarkup() != null)
                    {
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dMINUS);
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dGRPO);
                    }
                    parm.type = Param.exclusions;
                    return parseElementNameGroup(declInputLevel, parm);
                case Tokens.tokenPero:
                    parm.type = Param.pero;
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dPERO);
                    return true;
                case Tokens.tokenPeroGrpo:
                    if (!inInstance())
                        message(ParserMessages.peroGrpoProlog);
                    goto case Tokens.tokenPeroNameStart;
                case Tokens.tokenPeroNameStart:
                    {
                        if (inInstance())
                        {
                            if (options().warnInstanceParamEntityRef)
                                message(ParserMessages.instanceParamEntityRef);
                        }
                        else
                        {
                            if (options().warnInternalSubsetPsParamEntityRef && inputLevel() == 1)
                                message(ParserMessages.internalSubsetPsParamEntityRef);
                        }
                        ConstPtr<Entity> entity = new ConstPtr<Entity>();
                        Ptr<EntityOrigin> origin = new Ptr<EntityOrigin>();
                        if (!parseEntityReference(true, token == Tokens.tokenPeroGrpo ? 1 : 0, entity, origin))
                            return false;
                        if (!entity.isNull())
                            entity.pointer()!.declReference(this, origin);
                    }
                    break;
                case Tokens.tokenPlusGrpo:
                    if (!allow.inclusions())
                    {
                        paramInvalidToken(token, allow);
                        return false;
                    }
                    if (currentMarkup() != null)
                    {
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dPLUS);
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dGRPO);
                    }
                    parm.type = Param.inclusions;
                    return parseElementNameGroup(declInputLevel, parm);
                case Tokens.tokenRni:
                    if (!allow.rni())
                    {
                        paramInvalidToken(token, allow);
                        return false;
                    }
                    return parseIndicatedReservedName(allow, parm);
                case Tokens.tokenS:
                    if (currentMarkup() != null)
                        currentMarkup()!.addS(currentChar());
                    break;
                case Tokens.tokenNameStart:
                    switch (allow.nameStart())
                    {
                        case Param.invalid:
                            paramInvalidToken(token, allow);
                            return false;
                        case Param.reservedName:
                            return parseReservedName(allow, parm);
                        case Param.name:
                            {
                                extendNameToken(syntax().namelen(), ParserMessages.nameLength);
                                parm.type = Param.name;
                                getCurrentToken(parm.origToken);
                                parm.token = new StringC(parm.origToken);
                                SubstTable? subst = syntax().generalSubstTable();
                                nuint count = parm.token.size();
                                Char[]? tokenData = parm.token.data();
                                for (nuint i = 0; i < count; i++)
                                    parm.token[i] = subst![tokenData![i]];
                                if (currentMarkup() != null)
                                    currentMarkup()!.addName(currentInput()!);
                                return true;
                            }
                        case Param.entityName:
                            extendNameToken(syntax().namelen(), ParserMessages.nameLength);
                            parm.type = Param.entityName;
                            getCurrentToken(syntax().entitySubstTable(), parm.token);
                            if (currentMarkup() != null)
                                currentMarkup()!.addName(currentInput()!);
                            return true;
                        case Param.paramEntityName:
                            extendNameToken(syntax().penamelen(), ParserMessages.parameterEntityNameLength);
                            parm.type = Param.paramEntityName;
                            getCurrentToken(syntax().entitySubstTable(), parm.token);
                            if (currentMarkup() != null)
                                currentMarkup()!.addName(currentInput()!);
                            return true;
                        case Param.attributeValue:
                            return parseAttributeValueParam(parm);
                    }
                    break;
                case Tokens.tokenDigit:
                    switch (allow.digit())
                    {
                        case Param.invalid:
                            paramInvalidToken(token, allow);
                            return false;
                        case Param.number:
                            extendNumber(syntax().namelen(), ParserMessages.numberLength);
                            parm.type = Param.number;
                            getCurrentToken(parm.token);
                            if (currentMarkup() != null)
                                currentMarkup()!.addNumber(currentInput()!);
                            return true;
                        case Param.attributeValue:
                            return parseAttributeValueParam(parm);
                    }
                    break;
                case Tokens.tokenLcUcNmchar:
                    switch (allow.nmchar())
                    {
                        case Param.invalid:
                            paramInvalidToken(token, allow);
                            return false;
                        case Param.attributeValue:
                            return parseAttributeValueParam(parm);
                    }
                    break;
                default:
                    throw new InvalidOperationException("CANNOT_HAPPEN");
            }
        }
    }

    // void paramInvalidToken(Token token, const AllowedParams &allow);
    protected void paramInvalidToken(Token token, AllowedParams allow)
    {
        if (!allow.silent())
            message(ParserMessages.paramInvalidToken,
                    new TokenMessageArg(token, allow.mainMode(), syntaxPointer(), sdPointer()),
                    new AllowedParamsMessageArg(allow, syntaxPointer()));
    }

    // Boolean parseIndicatedReservedName(const AllowedParams &allow, Param &parm);
    protected Boolean parseIndicatedReservedName(AllowedParams allow, Param parm)
    {
        Syntax.ReservedName rn;
        if (!getIndicatedReservedName(out rn))
            return false;
        if (!allow.reservedName(rn))
        {
            message(ParserMessages.invalidReservedName,
                    new StringMessageArg(currentToken()));
            return false;
        }
        parm.type = (byte)(Param.indicatedReservedName + (byte)rn);
        return true;
    }

    // Boolean parseReservedName(const AllowedParams &allow, Param &parm);
    protected Boolean parseReservedName(AllowedParams allow, Param parm)
    {
        Syntax.ReservedName rn;
        if (!getReservedName(out rn))
            return false;
        if (!allow.reservedName(rn))
        {
            message(ParserMessages.invalidReservedName,
                    new StringMessageArg(syntax().reservedName(rn)));
            return false;
        }
        parm.type = (byte)(Param.reservedName + (byte)rn);
        return true;
    }

    // Boolean getIndicatedReservedName(Syntax::ReservedName *result);
    protected Boolean getIndicatedReservedName(out Syntax.ReservedName result)
    {
        result = Syntax.ReservedName.rALL;
        if (currentMarkup() != null)
            currentMarkup()!.addDelim(Syntax.DelimGeneral.dRNI);
        InputSource? inSrc = currentInput();
        inSrc!.startToken();
        if (!syntax().isNameStartCharacter(inSrc.tokenChar(messenger())))
        {
            message(ParserMessages.rniNameStart);
            return false;
        }
        extendNameToken(syntax().namelen(), ParserMessages.nameLength);
        StringC buffer = nameBuffer();
        getCurrentToken(syntax().generalSubstTable(), buffer);
        if (!syntax().lookupReservedName(buffer, out result))
        {
            message(ParserMessages.noSuchReservedName, new StringMessageArg(buffer));
            return false;
        }
        if (currentMarkup() != null)
            currentMarkup()!.addReservedName(result, currentInput()!);
        return true;
    }

    // Boolean getReservedName(Syntax::ReservedName *result);
    protected Boolean getReservedName(out Syntax.ReservedName result)
    {
        result = Syntax.ReservedName.rALL;
        extendNameToken(syntax().namelen(), ParserMessages.nameLength);
        StringC buffer = nameBuffer();
        getCurrentToken(syntax().generalSubstTable(), buffer);
        if (!syntax().lookupReservedName(buffer, out result))
        {
            message(ParserMessages.noSuchReservedName, new StringMessageArg(buffer));
            return false;
        }
        if (currentMarkup() != null)
            currentMarkup()!.addReservedName(result, currentInput()!);
        return true;
    }

    // Boolean parseAttributeValueParam(Param &parm);
    protected Boolean parseAttributeValueParam(Param parm)
    {
        nuint maxLen = syntax().litlen() > syntax().normsep()
                       ? syntax().litlen() - syntax().normsep()
                       : 0;
        extendNameToken(maxLen, ParserMessages.attributeValueLength);
        parm.type = Param.attributeValue;
        Text text = new Text();
        text.addChars(currentInput()!.currentTokenStart(),
                      currentInput()!.currentTokenLength(),
                      currentLocation());
        text.swap(parm.literalText);
        if (currentMarkup() != null)
            currentMarkup()!.addAttributeValue(currentInput()!);
        return true;
    }

    // Boolean parseMinimumLiteral(Boolean lita, Text &text);
    protected virtual Boolean parseMinimumLiteral(Boolean lita, Text text)
    {
        return parseLiteral(lita ? Mode.mlitaMode : Mode.mlitMode, Mode.mlitMode,
                            (nuint)Syntax.referenceQuantity(Syntax.Quantity.qLITLEN),
                            ParserMessages.minimumLiteralLength,
                            (uint)(literalSingleSpace | literalMinimumData
                                   | (eventsWanted().wantPrologMarkup()
                                      ? literalDelimInfo
                                      : 0)),
                            text);
    }

    // Boolean parseSystemIdentifier(Boolean lita, Text &text);
    protected virtual Boolean parseSystemIdentifier(Boolean lita, Text text)
    {
        return parseLiteral(lita ? Mode.slitaMode : Mode.slitMode, Mode.slitMode,
                            syntax().litlen(),
                            ParserMessages.systemIdentifierLength,
                            (uint)(eventsWanted().wantPrologMarkup()
                                   ? literalDelimInfo
                                   : 0),
                            text);
    }

    // Boolean parseParameterLiteral(Boolean lita, Text &text);
    protected virtual Boolean parseParameterLiteral(Boolean lita, Text text)
    {
        return parseLiteral(lita ? Mode.plitaMode : Mode.plitMode, Mode.pliteMode,
                            syntax().litlen(),
                            ParserMessages.parameterLiteralLength,
                            (uint)(eventsWanted().wantPrologMarkup()
                                   ? literalDelimInfo
                                   : 0),
                            text);
    }

    // Boolean parseDataTagParameterLiteral(Boolean lita, Text &text);
    protected virtual Boolean parseDataTagParameterLiteral(Boolean lita, Text text)
    {
        return parseLiteral(lita ? Mode.plitaMode : Mode.plitMode, Mode.pliteMode,
                            syntax().dtemplen(),
                            ParserMessages.dataTagPatternLiteralLength,
                            (uint)(literalDataTag
                                   | (eventsWanted().wantPrologMarkup()
                                      ? literalDelimInfo
                                      : 0)),
                            text);
    }

    // Boolean parseAttributeValueLiteral(Boolean lita, Text &text);
    protected virtual Boolean parseAttributeValueLiteral(Boolean lita, Text text)
    {
        throw new NotImplementedException();
    }

    // Boolean parseTokenizedAttributeValueLiteral(Boolean lita, Text &text);
    protected virtual Boolean parseTokenizedAttributeValueLiteral(Boolean lita, Text text)
    {
        throw new NotImplementedException();
    }

    // Boolean parseExternalId(const AllowedParams &sysidAllow, const AllowedParams &endAllow,
    //                          Boolean maybeWarnMissingSystemId, unsigned declInputLevel, Param &parm, ExternalId &id);
    // parm contains either system or public
    protected virtual Boolean parseExternalId(AllowedParams sysidAllow, AllowedParams endAllow,
                                               Boolean maybeWarnMissingSystemId, uint declInputLevel,
                                               Param parm, ExternalId id)
    {
        id.setLocation(currentLocation());
        if (parm.type == (byte)(Param.reservedName + (byte)Syntax.ReservedName.rPUBLIC))
        {
            AllowedParams allowMinimumLiteral = new AllowedParams(Param.minimumLiteral);
            if (!parseParam(allowMinimumLiteral, declInputLevel, parm))
                return false;
            MessageType1? fpierr;
            MessageType1? urnerr;
            switch (id.setPublic(parm.literalText, sd().internalCharset(),
                                 syntax().space(), out fpierr, out urnerr))
            {
                case PublicId.Type.fpi:
                    {
                        PublicId.TextClass textClass;
                        if (sd().formal() && id.publicId()!.getTextClass(out textClass)
                            && textClass == PublicId.TextClass.SD)
                            message(ParserMessages.wwwRequired);
                        if (sd().urn() && !sd().formal())
                            message(urnerr!, new StringMessageArg(id.publicIdString()!));
                    }
                    break;
                case PublicId.Type.urn:
                    if (sd().formal() && !sd().urn())
                        message(fpierr!, new StringMessageArg(id.publicIdString()!));
                    break;
                case PublicId.Type.informal:
                    if (sd().formal())
                        message(fpierr!, new StringMessageArg(id.publicIdString()!));
                    if (sd().urn())
                        message(urnerr!, new StringMessageArg(id.publicIdString()!));
                    break;
            }
        }
        if (!parseParam(sysidAllow, declInputLevel, parm))
            return false;
        if (parm.type == Param.systemIdentifier)
        {
            id.setSystem(parm.literalText);
            if (!parseParam(endAllow, declInputLevel, parm))
                return false;
        }
        else if (options().warnMissingSystemId && maybeWarnMissingSystemId)
            message(ParserMessages.missingSystemId);
        return true;
    }

    // Group parsing methods from parseParam.cxx

    // Boolean parseGroupToken(const AllowedGroupTokens &allow, unsigned nestingLevel,
    //                         unsigned declInputLevel, unsigned groupInputLevel, GroupToken &gt);
    protected Boolean parseGroupToken(AllowedGroupTokens allow, uint nestingLevel,
                                      uint declInputLevel, uint groupInputLevel, GroupToken gt)
    {
        for (;;)
        {
            Token token = getToken(Mode.grpMode);
            switch (token)
            {
                case Tokens.tokenEe:
                    if (inputLevel() <= groupInputLevel)
                    {
                        message(ParserMessages.groupLevel);
                        if (inputLevel() <= declInputLevel)
                            return false;
                    }
                    else if (!sd().www())
                        message(ParserMessages.groupEntityEnd);
                    if (currentMarkup() != null)
                        currentMarkup()!.addEntityEnd();
                    popInputStack();
                    break;
                case Tokens.tokenPeroGrpo:
                    {
                        if (!inInstance())
                            message(ParserMessages.peroGrpoProlog);
                        Boolean start = false;
                        if (inTag(ref start))
                            message(start
                                    ? ParserMessages.peroGrpoStartTag
                                    : ParserMessages.peroGrpoEndTag);
                    }
                    goto case Tokens.tokenPeroNameStart;
                case Tokens.tokenPeroNameStart:
                    {
                        if (options().warnInternalSubsetTsParamEntityRef && inputLevel() == 1)
                            message(ParserMessages.internalSubsetTsParamEntityRef);
                        ConstPtr<Entity> entity = new ConstPtr<Entity>();
                        Ptr<EntityOrigin> origin = new Ptr<EntityOrigin>();
                        if (!parseEntityReference(true, token == Tokens.tokenPeroGrpo ? 1 : 0, entity, origin))
                            return false;
                        if (!entity.isNull())
                            entity.pointer()!.declReference(this, origin);
                    }
                    break;
                case Tokens.tokenUnrecognized:
                    if (reportNonSgmlCharacter())
                        break;
                    message(ParserMessages.groupCharacter,
                            new StringMessageArg(currentToken()),
                            new AllowedGroupTokensMessageArg(allow, syntaxPointer()));
                    return false;
                case Tokens.tokenDtgo:
                    if (!allow.groupToken(GroupToken.Type.dataTagGroup))
                    {
                        groupTokenInvalidToken(token, allow);
                        return false;
                    }
                    if (sd().datatag())
                        message(ParserMessages.datatagNotImplemented);
                    if (!defDtd().isBase())
                        message(ParserMessages.datatagBaseDtd);
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dDTGO);
                    return parseDataTagGroup(nestingLevel + 1, declInputLevel, gt);
                case Tokens.tokenGrpo:
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dGRPO);
                    switch (allow.group())
                    {
                        case GroupToken.Type.modelGroup:
                            {
                                ModelGroup? modelGroup;
                                if (!parseModelGroup(nestingLevel + 1, declInputLevel, out modelGroup, Mode.grpMode))
                                    return false;
                                gt.model = new Owner<ModelGroup>(modelGroup);
                                gt.type = GroupToken.Type.modelGroup;
                                return true;
                            }
                        case GroupToken.Type.dataTagTemplateGroup:
                            return parseDataTagTemplateGroup(nestingLevel + 1, declInputLevel, gt);
                        default:
                            groupTokenInvalidToken(token, allow);
                            return false;
                    }
                case Tokens.tokenRni:
                    if (!allow.groupToken(GroupToken.Type.pcdata)
                        && !allow.groupToken(GroupToken.Type.all)
                        && !allow.groupToken(GroupToken.Type.@implicit))
                    {
                        groupTokenInvalidToken(token, allow);
                        return false;
                    }
                    Syntax.ReservedName rn;
                    if (!getIndicatedReservedName(out rn))
                        return false;
                    if (rn == Syntax.ReservedName.rPCDATA && allow.groupToken(GroupToken.Type.pcdata))
                    {
                        gt.type = GroupToken.Type.pcdata;
                        gt.contentToken = new Owner<ContentToken>(new PcdataToken());
                        return true;
                    }
                    else if (rn == Syntax.ReservedName.rALL && allow.groupToken(GroupToken.Type.all))
                    {
                        message(ParserMessages.sorryAllImplicit);
                        return false;
                    }
                    else if (rn == Syntax.ReservedName.rIMPLICIT && allow.groupToken(GroupToken.Type.@implicit))
                    {
                        message(ParserMessages.sorryAllImplicit);
                        return false;
                    }
                    else
                    {
                        StringC tok = new StringC(syntax().delimGeneral((int)Syntax.DelimGeneral.dRNI));
                        tok.operatorPlusAssign(syntax().reservedName(rn));
                        message(ParserMessages.invalidToken, new StringMessageArg(tok));
                        return false;
                    }
                case Tokens.tokenS:
                    if (currentMarkup() != null)
                    {
                        extendS();
                        currentMarkup()!.addS(currentInput()!);
                    }
                    break;
                case Tokens.tokenNameStart:
                    switch (allow.nameStart())
                    {
                        case GroupToken.Type.elementToken:
                            {
                                extendNameToken(syntax().namelen(), ParserMessages.nameLength);
                                gt.type = GroupToken.Type.elementToken;
                                StringC buffer = nameBuffer();
                                getCurrentToken(syntax().generalSubstTable(), buffer);
                                if (currentMarkup() != null)
                                    currentMarkup()!.addName(currentInput()!);
                                ElementType? e = lookupCreateElement(buffer);
                                ContentToken.OccurrenceIndicator oi = getOccurrenceIndicator(Mode.grpMode);
                                gt.contentToken = new Owner<ContentToken>(new ElementToken(e, oi));
                                return true;
                            }
                        case GroupToken.Type.name:
                        case GroupToken.Type.nameToken:
                            extendNameToken(syntax().namelen(),
                                            allow.nameStart() == GroupToken.Type.name
                                            ? ParserMessages.nameLength
                                            : ParserMessages.nameTokenLength);
                            getCurrentToken(syntax().generalSubstTable(), gt.token);
                            gt.type = allow.nameStart();
                            if (currentMarkup() != null)
                            {
                                if (gt.type == GroupToken.Type.nameToken)
                                    currentMarkup()!.addNameToken(currentInput()!);
                                else
                                    currentMarkup()!.addName(currentInput()!);
                            }
                            return true;
                        default:
                            groupTokenInvalidToken(token, allow);
                            return false;
                    }
                case Tokens.tokenDigit:
                case Tokens.tokenLcUcNmchar:
                    if (!allow.groupToken(GroupToken.Type.nameToken))
                    {
                        groupTokenInvalidToken(token, allow);
                        return false;
                    }
                    extendNameToken(syntax().namelen(), ParserMessages.nameTokenLength);
                    getCurrentToken(syntax().generalSubstTable(), gt.token);
                    gt.type = GroupToken.Type.nameToken;
                    if (currentMarkup() != null)
                        currentMarkup()!.addNameToken(currentInput()!);
                    return true;
                case Tokens.tokenLit:
                case Tokens.tokenLita:
                    // parameter literal in data tag pattern
                    if (!allow.groupToken(GroupToken.Type.dataTagLiteral))
                    {
                        groupTokenInvalidToken(token, allow);
                        return false;
                    }
                    if (!parseDataTagParameterLiteral(token == Tokens.tokenLita, gt.text))
                        return false;
                    gt.type = GroupToken.Type.dataTagLiteral;
                    if (currentMarkup() != null)
                        currentMarkup()!.addLiteral(gt.text);
                    return true;
                case Tokens.tokenAnd:
                case Tokens.tokenSeq:
                case Tokens.tokenOr:
                case Tokens.tokenDtgc:
                case Tokens.tokenGrpc:
                case Tokens.tokenOpt:
                case Tokens.tokenPlus:
                case Tokens.tokenRep:
                    groupTokenInvalidToken(token, allow);
                    return false;
            }
        }
    }

    // void groupTokenInvalidToken(Token token, const AllowedGroupTokens &allow);
    protected void groupTokenInvalidToken(Token token, AllowedGroupTokens allow)
    {
        message(ParserMessages.groupTokenInvalidToken,
                new TokenMessageArg(token, Mode.grpMode, syntaxPointer(), sdPointer()),
                new AllowedGroupTokensMessageArg(allow, syntaxPointer()));
    }

    // Boolean parseGroupConnector(const AllowedGroupConnectors &allow, unsigned declInputLevel,
    //                             unsigned groupInputLevel, GroupConnector &gc);
    protected Boolean parseGroupConnector(AllowedGroupConnectors allow, uint declInputLevel,
                                          uint groupInputLevel, ref GroupConnector gc)
    {
        for (;;)
        {
            Token token = getToken(Mode.grpMode);
            switch (token)
            {
                case Tokens.tokenEe:
                    if (inputLevel() <= groupInputLevel)
                    {
                        message(ParserMessages.groupLevel);
                        if (inputLevel() <= declInputLevel)
                            return false;
                    }
                    if (currentMarkup() != null)
                        currentMarkup()!.addEntityEnd();
                    popInputStack();
                    break;
                case Tokens.tokenS:
                    if (currentMarkup() != null)
                    {
                        extendS();
                        currentMarkup()!.addS(currentInput()!);
                    }
                    break;
                case Tokens.tokenPeroGrpo:
                    if (inInstance())
                    {
                        message(ParserMessages.peroGrpoProlog);
                        break;
                    }
                    goto case Tokens.tokenPeroNameStart;
                case Tokens.tokenPeroNameStart:
                    if (!sd().www())
                        message(ParserMessages.groupEntityReference);
                    else
                    {
                        ConstPtr<Entity> entity = new ConstPtr<Entity>();
                        Ptr<EntityOrigin> origin = new Ptr<EntityOrigin>();
                        if (!parseEntityReference(true, token == Tokens.tokenPeroGrpo ? 1 : 0, entity, origin))
                            return false;
                        if (!entity.isNull())
                            entity.pointer()!.declReference(this, origin);
                    }
                    break;
                case Tokens.tokenUnrecognized:
                    if (reportNonSgmlCharacter())
                        break;
                    message(ParserMessages.groupCharacter,
                            new StringMessageArg(currentToken()),
                            new AllowedGroupConnectorsMessageArg(allow, syntaxPointer()));
                    return false;
                case Tokens.tokenAnd:
                    if (!allow.groupConnector(GroupConnector.Type.andGC))
                    {
                        groupConnectorInvalidToken(token, allow);
                        return false;
                    }
                    gc.type = GroupConnector.Type.andGC;
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dAND);
                    return true;
                case Tokens.tokenSeq:
                    if (!allow.groupConnector(GroupConnector.Type.seqGC))
                    {
                        groupConnectorInvalidToken(token, allow);
                        return false;
                    }
                    gc.type = GroupConnector.Type.seqGC;
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dSEQ);
                    return true;
                case Tokens.tokenOr:
                    if (!allow.groupConnector(GroupConnector.Type.orGC))
                    {
                        groupConnectorInvalidToken(token, allow);
                        return false;
                    }
                    gc.type = GroupConnector.Type.orGC;
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dOR);
                    return true;
                case Tokens.tokenDtgc:
                    if (!allow.groupConnector(GroupConnector.Type.dtgcGC))
                    {
                        groupConnectorInvalidToken(token, allow);
                        return false;
                    }
                    gc.type = GroupConnector.Type.dtgcGC;
                    if (inputLevel() > groupInputLevel)
                        message(ParserMessages.groupParameterEntityNotEnded);
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dDTGC);
                    return true;
                case Tokens.tokenGrpc:
                    if (!allow.groupConnector(GroupConnector.Type.grpcGC))
                    {
                        groupConnectorInvalidToken(token, allow);
                        return false;
                    }
                    gc.type = GroupConnector.Type.grpcGC;
                    if (inputLevel() > groupInputLevel)
                        message(ParserMessages.groupParameterEntityNotEnded);
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dGRPC);
                    return true;
                default:
                    groupConnectorInvalidToken(token, allow);
                    return false;
            }
        }
    }

    // void groupConnectorInvalidToken(Token token, const AllowedGroupConnectors &allow);
    protected void groupConnectorInvalidToken(Token token, AllowedGroupConnectors allow)
    {
        message(ParserMessages.connectorInvalidToken,
                new TokenMessageArg(token, Mode.grpMode, syntaxPointer(), sdPointer()),
                new AllowedGroupConnectorsMessageArg(allow, syntaxPointer()));
    }

    // Static AllowedGroupTokens for name
    private static readonly AllowedGroupTokens allowName_ = new AllowedGroupTokens(GroupToken.Type.name);

    // Boolean parseElementNameGroup(unsigned declInputLevel, Param &parm);
    protected Boolean parseElementNameGroup(uint declInputLevel, Param parm)
    {
        AllowedGroupTokens allowCommonName = new AllowedGroupTokens(
            GroupToken.Type.name, GroupToken.Type.all, GroupToken.Type.@implicit);
        if (!parseGroup(sd().www() ? allowCommonName : allowName_, declInputLevel, parm))
            return false;
        parm.elementVector.resize(parm.nameTokenVector.size());
        for (nuint i = 0; i < parm.nameTokenVector.size(); i++)
            parm.elementVector[(int)i] = lookupCreateElement(parm.nameTokenVector[(int)i].name);
        return true;
    }

    // Boolean parseNameGroup(unsigned declInputLevel, Param &parm);
    protected Boolean parseNameGroup(uint declInputLevel, Param parm)
    {
        return parseGroup(allowName_, declInputLevel, parm);
    }

    // Boolean parseNameTokenGroup(unsigned declInputLevel, Param &parm);
    protected Boolean parseNameTokenGroup(uint declInputLevel, Param parm)
    {
        AllowedGroupTokens allowNameToken = new AllowedGroupTokens(GroupToken.Type.nameToken);
        return parseGroup(allowNameToken, declInputLevel, parm);
    }

    // static Boolean groupContains(const Vector<NameToken> &vec, const StringC &str);
    private static Boolean groupContains(Vector<NameToken> vec, StringC str)
    {
        for (nuint i = 0; i < vec.size(); i++)
            if (vec[(int)i].name == str)
                return true;
        return false;
    }

    // Boolean parseGroup(const AllowedGroupTokens &allowToken, unsigned declInputLevel, Param &parm);
    protected Boolean parseGroup(AllowedGroupTokens allowToken, uint declInputLevel, Param parm)
    {
        uint groupInputLevel = inputLevel();
        int nDuplicates = 0;
        Vector<NameToken> vec = parm.nameTokenVector;
        vec.clear();
        GroupConnector.Type connector = GroupConnector.Type.grpcGC;
        GroupToken gt = new GroupToken();
        for (;;)
        {
            if (!parseGroupToken(allowToken, 0, declInputLevel, groupInputLevel, gt))
                return false;
            if (groupContains(vec, gt.token))
            {
                nDuplicates++;
                message(ParserMessages.duplicateGroupToken, new StringMessageArg(gt.token));
            }
            else
            {
                vec.resize(vec.size() + 1);
                gt.token.swap(vec[(int)(vec.size() - 1)].name);
                getCurrentToken(vec[(int)(vec.size() - 1)].origName);
                vec[(int)(vec.size() - 1)].loc = currentLocation();
            }
            GroupConnector gc = new GroupConnector();
            AllowedGroupConnectors allowAnyConnectorGrpc = new AllowedGroupConnectors(
                GroupConnector.Type.orGC, GroupConnector.Type.andGC,
                GroupConnector.Type.seqGC, GroupConnector.Type.grpcGC);
            if (!parseGroupConnector(allowAnyConnectorGrpc, declInputLevel, groupInputLevel, ref gc))
                return false;
            if (gc.type == GroupConnector.Type.grpcGC)
                break;
            if (options().warnNameGroupNotOr)
            {
                if (gc.type != GroupConnector.Type.orGC)
                    message(ParserMessages.nameGroupNotOr);
            }
            else if (options().warnShould)
            {
                if (connector == GroupConnector.Type.grpcGC)
                    connector = gc.type;
                else if (gc.type != connector)
                {
                    message(ParserMessages.mixedConnectors);
                    connector = gc.type;
                }
            }
        }
        if ((nuint)(nDuplicates) + vec.size() > syntax().grpcnt())
            message(ParserMessages.groupCount, new NumberMessageArg(syntax().grpcnt()));
        return true;
    }

    // Boolean parseModelGroup(unsigned nestingLevel, unsigned declInputLevel,
    //                         ModelGroup *&group, Mode oiMode);
    protected Boolean parseModelGroup(uint nestingLevel, uint declInputLevel,
                                      out ModelGroup? group, Mode oiMode)
    {
        group = null;
        if (nestingLevel - 1 == syntax().grplvl())
            message(ParserMessages.grplvl, new NumberMessageArg(syntax().grplvl()));
        uint groupInputLevel = inputLevel();
        GroupToken gt = new GroupToken();
        Vector<Owner<ContentToken>> tokenVector = new Vector<Owner<ContentToken>>();
        GroupConnector.Type connector = GroupConnector.Type.grpcGC;

        AllowedGroupTokens allowContentToken = new AllowedGroupTokens(
            GroupToken.Type.pcdata, GroupToken.Type.dataTagGroup,
            GroupToken.Type.elementToken, GroupToken.Type.modelGroup);
        AllowedGroupTokens allowCommonContentToken = new AllowedGroupTokens(
            GroupToken.Type.pcdata, GroupToken.Type.all, GroupToken.Type.@implicit,
            GroupToken.Type.dataTagGroup, GroupToken.Type.elementToken, GroupToken.Type.modelGroup);
        AllowedGroupConnectors allowAnyConnectorGrpc = new AllowedGroupConnectors(
            GroupConnector.Type.orGC, GroupConnector.Type.andGC,
            GroupConnector.Type.seqGC, GroupConnector.Type.grpcGC);
        AllowedGroupConnectors allowOrGrpc = new AllowedGroupConnectors(
            GroupConnector.Type.orGC, GroupConnector.Type.grpcGC);
        AllowedGroupConnectors allowAndGrpc = new AllowedGroupConnectors(
            GroupConnector.Type.andGC, GroupConnector.Type.grpcGC);
        AllowedGroupConnectors allowSeqGrpc = new AllowedGroupConnectors(
            GroupConnector.Type.seqGC, GroupConnector.Type.grpcGC);
        AllowedGroupConnectors connectorp = allowAnyConnectorGrpc;

        GroupConnector gc = new GroupConnector();
        Boolean pcdataCheck = false;
        do
        {
            if (!parseGroupToken(sd().www() ? allowCommonContentToken : allowContentToken,
                                 nestingLevel, declInputLevel, groupInputLevel, gt))
                return false;
            ContentToken? contentToken;
            if (gt.type == GroupToken.Type.modelGroup)
                contentToken = gt.model.extract();
            else
                contentToken = gt.contentToken.extract();
            if (tokenVector.size() == syntax().grpcnt())
                message(ParserMessages.groupCount, new NumberMessageArg(syntax().grpcnt()));
            tokenVector.resize(tokenVector.size() + 1);
            tokenVector[(int)(tokenVector.size() - 1)] = new Owner<ContentToken>(contentToken);
            if (!parseGroupConnector(connectorp, declInputLevel, groupInputLevel, ref gc))
                return false;
            if (options().warnMixedContentRepOrGroup && gt.type == GroupToken.Type.pcdata)
            {
                if (tokenVector.size() != 1)
                    message(ParserMessages.pcdataNotFirstInGroup);
                else if (gc.type == GroupConnector.Type.seqGC)
                    message(ParserMessages.pcdataInSeqGroup);
                else
                    pcdataCheck = true;
                if (nestingLevel != 1)
                    message(ParserMessages.pcdataInNestedModelGroup);
            }
            else if (pcdataCheck)
            {
                if (gt.type == GroupToken.Type.modelGroup)
                    message(ParserMessages.pcdataGroupMemberModelGroup);
                if (contentToken!.occurrenceIndicator() != ContentToken.OccurrenceIndicator.none)
                    message(ParserMessages.pcdataGroupMemberOccurrenceIndicator);
            }
            if (tokenVector.size() == 1)
            {
                connector = gc.type;
                switch (gc.type)
                {
                    case GroupConnector.Type.orGC:
                        connectorp = allowOrGrpc;
                        break;
                    case GroupConnector.Type.seqGC:
                        connectorp = allowSeqGrpc;
                        break;
                    case GroupConnector.Type.andGC:
                        connectorp = allowAndGrpc;
                        if (options().warnAndGroup)
                            message(ParserMessages.andGroup);
                        break;
                    default:
                        break;
                }
            }
        } while (gc.type != GroupConnector.Type.grpcGC);

        ContentToken.OccurrenceIndicator oi = getOccurrenceIndicator(oiMode);
        switch (connector)
        {
            case GroupConnector.Type.orGC:
                group = new OrModelGroup(tokenVector, oi);
                if (pcdataCheck && oi != ContentToken.OccurrenceIndicator.rep)
                    message(ParserMessages.pcdataGroupNotRep);
                break;
            case GroupConnector.Type.grpcGC:
                if (pcdataCheck && oi != ContentToken.OccurrenceIndicator.rep && oi != ContentToken.OccurrenceIndicator.none)
                    message(ParserMessages.pcdataGroupNotRep);
                goto case GroupConnector.Type.seqGC;
            case GroupConnector.Type.seqGC:
                group = new SeqModelGroup(tokenVector, oi);
                break;
            case GroupConnector.Type.andGC:
                group = new AndModelGroup(tokenVector, oi);
                break;
            default:
                break;
        }
        return true;
    }

    // ContentToken::OccurrenceIndicator getOccurrenceIndicator(Mode oiMode);
    protected ContentToken.OccurrenceIndicator getOccurrenceIndicator(Mode oiMode)
    {
        Token token = getToken(oiMode);
        switch (token)
        {
            case Tokens.tokenPlus:
                if (currentMarkup() != null)
                    currentMarkup()!.addDelim(Syntax.DelimGeneral.dPLUS);
                return ContentToken.OccurrenceIndicator.plus;
            case Tokens.tokenOpt:
                if (currentMarkup() != null)
                    currentMarkup()!.addDelim(Syntax.DelimGeneral.dOPT);
                return ContentToken.OccurrenceIndicator.opt;
            case Tokens.tokenRep:
                if (currentMarkup() != null)
                    currentMarkup()!.addDelim(Syntax.DelimGeneral.dREP);
                return ContentToken.OccurrenceIndicator.rep;
            default:
                currentInput()!.ungetToken();
                return ContentToken.OccurrenceIndicator.none;
        }
    }

    // Boolean parseDataTagGroup(unsigned nestingLevel, unsigned declInputLevel, GroupToken &result);
    protected Boolean parseDataTagGroup(uint nestingLevel, uint declInputLevel, GroupToken result)
    {
        throw new NotImplementedException();
    }

    // Boolean parseDataTagTemplateGroup(unsigned nestingLevel, unsigned declInputLevel, GroupToken &result);
    protected Boolean parseDataTagTemplateGroup(uint nestingLevel, uint declInputLevel, GroupToken result)
    {
        throw new NotImplementedException();
    }

    // Boolean parseLiteral(Mode litMode, Mode liteMode, size_t maxLength,
    //                       const MessageType1 &tooLongMessage, unsigned flags, Text &text);
    protected Boolean parseLiteral(Mode litMode, Mode liteMode, nuint maxLength,
                                   MessageType1 tooLongMessage, uint flags, Text text)
    {
        uint startLevel = inputLevel();
        Mode currentMode = litMode;
        // If the literal gets to be longer than this, then we assume
        // that the closing delimiter has been omitted if we're at the end
        // of a line and at the starting input level.
        nuint reallyMaxLength = (maxLength > nuint.MaxValue / 2
                                 ? nuint.MaxValue
                                 : maxLength * 2);
        text.clear();
        Location startLoc = currentLocation();
        if ((flags & literalDelimInfo) != 0)
            text.addStartDelim(currentLocation());
        for (;;)
        {
            Token token = getToken(currentMode);
            switch (token)
            {
                case Tokens.tokenEe:
                    if (inputLevel() == startLevel)
                    {
                        message(ParserMessages.literalLevel);
                        return false;
                    }
                    text.addEntityEnd(currentLocation());
                    popInputStack();
                    if (inputLevel() == startLevel)
                        currentMode = litMode;
                    break;
                case Tokens.tokenUnrecognized:
                    if (reportNonSgmlCharacter())
                        break;
                    message(ParserMessages.literalMinimumData,
                            new StringMessageArg(currentToken()));
                    break;
                case Tokens.tokenRs:
                    text.ignoreChar(currentChar(), currentLocation());
                    break;
                case Tokens.tokenRe:
                    if (text.size() > reallyMaxLength && inputLevel() == startLevel)
                    {
                        // guess that the closing delimiter has been omitted
                        setNextLocation(startLoc);
                        message(ParserMessages.literalClosingDelimiter);
                        return false;
                    }
                    goto case Tokens.tokenSepchar;
                case Tokens.tokenSepchar:
                    if ((flags & literalSingleSpace) != 0
                        && (text.size() == 0 || text.lastChar() == syntax().space()))
                        text.ignoreChar(currentChar(), currentLocation());
                    else
                        text.addChar(syntax().space(),
                                     new Location(new ReplacementOrigin(currentLocation(),
                                                                        currentChar()),
                                                  0));
                    break;
                case Tokens.tokenSpace:
                    if ((flags & literalSingleSpace) != 0
                        && (text.size() == 0 || text.lastChar() == syntax().space()))
                        text.ignoreChar(currentChar(), currentLocation());
                    else
                        text.addChar(currentChar(), currentLocation());
                    break;
                case Tokens.tokenCroDigit:
                case Tokens.tokenHcroHexDigit:
                    {
                        Char c = 0;
                        Location loc = new Location();
                        if (!parseNumericCharRef(token == Tokens.tokenHcroHexDigit, ref c, ref loc))
                            return false;
                        Boolean isSgmlChar = false;
                        if (!translateNumericCharRef(ref c, ref isSgmlChar))
                            break;
                        if (!isSgmlChar)
                        {
                            if ((flags & literalNonSgml) != 0)
                                text.addNonSgmlChar(c, loc);
                            else
                                message(ParserMessages.numericCharRefLiteralNonSgml,
                                        new NumberMessageArg(c));
                            break;
                        }
                        if ((flags & literalDataTag) != 0)
                        {
                            if (!syntax().isSgmlChar((Xchar)c))
                                message(ParserMessages.dataTagPatternNonSgml);
                            else if (syntax().charSet((int)Syntax.Set.functionChar)!.contains(c))
                                message(ParserMessages.dataTagPatternFunction);
                        }
                        if ((flags & literalSingleSpace) != 0
                            && c == syntax().space()
                            && (text.size() == 0 || text.lastChar() == syntax().space()))
                            text.ignoreChar(c, loc);
                        else
                            text.addChar(c, loc);
                    }
                    break;
                case Tokens.tokenCroNameStart:
                    if (!parseNamedCharRef())
                        return false;
                    break;
                case Tokens.tokenEroGrpo:
                    message(inInstance() ? ParserMessages.eroGrpoStartTag : ParserMessages.eroGrpoProlog);
                    break;
                case Tokens.tokenLit:
                case Tokens.tokenLita:
                    if ((flags & literalDelimInfo) != 0)
                        text.addEndDelim(currentLocation(), token == Tokens.tokenLita);
                    goto done;
                case Tokens.tokenPeroNameStart:
                    if (options().warnInternalSubsetLiteralParamEntityRef
                        && inputLevel() == 1)
                        message(ParserMessages.internalSubsetLiteralParamEntityRef);
                    goto case Tokens.tokenEroNameStart;
                case Tokens.tokenEroNameStart:
                    {
                        ConstPtr<Entity> entity = new ConstPtr<Entity>();
                        Ptr<EntityOrigin> origin = new Ptr<EntityOrigin>();
                        if (!parseEntityReference(token == Tokens.tokenPeroNameStart,
                                                  (flags & literalNoProcess) != 0 ? 2 : 0,
                                                  entity, origin))
                            return false;
                        if (!entity.isNull())
                            entity.pointer()!.litReference(text, this, origin,
                                                          (flags & literalSingleSpace) != 0);
                        if (inputLevel() > startLevel)
                            currentMode = liteMode;
                    }
                    break;
                case Tokens.tokenPeroGrpo:
                    message(ParserMessages.peroGrpoProlog);
                    break;
                case Tokens.tokenCharDelim:
                    message(ParserMessages.dataCharDelim,
                            new StringMessageArg(new StringC(currentInput()!.currentTokenStart(),
                                                             currentInput()!.currentTokenLength())));
                    goto case Tokens.tokenChar;
                case Tokens.tokenChar:
                    if (text.size() > reallyMaxLength && inputLevel() == startLevel
                        && currentChar() == syntax().standardFunction((int)Syntax.StandardFunction.fRE))
                    {
                        // guess that the closing delimiter has been omitted
                        setNextLocation(startLoc);
                        message(ParserMessages.literalClosingDelimiter);
                        return false;
                    }
                    text.addChar(currentChar(), currentLocation());
                    break;
            }
        }
    done:
        if ((flags & literalSingleSpace) != 0
            && text.size() > 0
            && text.lastChar() == syntax().space())
            text.ignoreLastChar();
        if (text.size() > maxLength)
        {
            switch (litMode)
            {
                case Mode.alitMode:
                case Mode.alitaMode:
                case Mode.talitMode:
                case Mode.talitaMode:
                    if (AttributeValue.handleAsUnterminated(text, this))
                        return false;
                    break;
                default:
                    break;
            }
            message(tooLongMessage, new NumberMessageArg(maxLength));
        }
        return true;
    }

    // Boolean parseNumericCharRef(Boolean isHex, Char &ch, Location &loc);
    protected Boolean parseNumericCharRef(Boolean isHex, ref Char ch, ref Location loc)
    {
        InputSource? ins = currentInput();
        if (ins == null) return false;
        Location startLocation = currentLocation();
        ins.discardInitial();
        Boolean valid = true;
        Char c = 0;
        if (isHex)
        {
            extendHexNumber();
            Char[]? lim = ins.currentTokenEnd();
            Char[]? start = ins.currentTokenStart();
            if (start != null && lim != null)
            {
                nuint startIdx = 0;
                nuint limIdx = ins.currentTokenLength();
                for (nuint p = startIdx; p < limIdx; p++)
                {
                    int val = sd().hexDigitWeight(start[p]);
                    if (c <= Constant.charMax / 16 && (c *= 16) <= Constant.charMax - (uint)val)
                        c += (uint)val;
                    else
                    {
                        message(ParserMessages.characterNumber, new StringMessageArg(currentToken()));
                        valid = false;
                        break;
                    }
                }
            }
        }
        else
        {
            extendNumber(syntax().namelen(), ParserMessages.numberLength);
            Char[]? lim = ins.currentTokenEnd();
            Char[]? start = ins.currentTokenStart();
            if (start != null && lim != null)
            {
                nuint startIdx = 0;
                nuint limIdx = ins.currentTokenLength();
                for (nuint p = startIdx; p < limIdx; p++)
                {
                    int val = sd().digitWeight(start[p]);
                    if (c <= Constant.charMax / 10 && (c *= 10) <= Constant.charMax - (uint)val)
                        c += (uint)val;
                    else
                    {
                        message(ParserMessages.characterNumber, new StringMessageArg(currentToken()));
                        valid = false;
                        break;
                    }
                }
            }
        }
        if (valid && !sd().docCharsetDecl().charDeclared(c))
        {
            valid = false;
            message(ParserMessages.characterNumber, new StringMessageArg(currentToken()));
        }
        Owner<Markup> markupPtr = new Owner<Markup>();
        if (wantMarkup())
        {
            markupPtr = new Owner<Markup>(new Markup());
            markupPtr.pointer()!.addDelim(isHex ? Syntax.DelimGeneral.dHCRO : Syntax.DelimGeneral.dCRO);
            markupPtr.pointer()!.addNumber(ins);
            switch (getToken(Mode.refMode))
            {
                case Tokens.tokenRefc:
                    markupPtr.pointer()!.addDelim(Syntax.DelimGeneral.dREFC);
                    break;
                case Tokens.tokenRe:
                    markupPtr.pointer()!.addRefEndRe();
                    if (options().warnRefc)
                        message(ParserMessages.refc);
                    break;
                default:
                    if (options().warnRefc)
                        message(ParserMessages.refc);
                    break;
            }
        }
        else if (options().warnRefc)
        {
            if (getToken(Mode.refMode) != Tokens.tokenRefc)
                message(ParserMessages.refc);
        }
        else
            getToken(Mode.refMode);
        if (valid)
        {
            ch = c;
            loc = new Location(new NumericCharRefOrigin(startLocation,
                                                        currentLocation().index()
                                                        + (Index)ins.currentTokenLength()
                                                        - startLocation.index(),
                                                        markupPtr),
                               0);
        }
        return valid;
    }
    // Boolean translateNumericCharRef(Char &ch, Boolean &isSgmlChar);
    // Translate a character number in the document character set
    // into the internal character set.
    // If it's a non-SGML char (ie described as UNUSED in SGML declaration),
    // return true and set isSgmlChar to false.
    protected Boolean translateNumericCharRef(ref Char ch, ref Boolean isSgmlChar)
    {
        if (sd().internalCharsetIsDocCharset())
        {
            if (options().warnNonSgmlCharRef && !syntax().isSgmlChar((Xchar)ch))
                message(ParserMessages.nonSgmlCharRef);
            isSgmlChar = true;
            return true;
        }
        UnivChar univChar;
        if (!sd().docCharset().descToUniv(ch, out univChar))
        {
            PublicId? pubid;
            CharsetDeclRange.Type type;
            Number n;
            StringC desc = new StringC();
            if (sd().docCharsetDecl().getCharInfo(ch, out pubid, out type, out n, desc))
            {
                if (type == CharsetDeclRange.Type.unused)
                {
                    if (options().warnNonSgmlCharRef)
                        message(ParserMessages.nonSgmlCharRef);
                    isSgmlChar = false;
                    return true;
                }
            }
            else
            {
                // CANNOT_HAPPEN();
            }
            if (type == CharsetDeclRange.Type.@string)
                message(ParserMessages.numericCharRefUnknownDesc,
                        new NumberMessageArg(ch),
                        new StringMessageArg(desc));
            else
                message(ParserMessages.numericCharRefUnknownBase,
                        new NumberMessageArg(ch),
                        new NumberMessageArg(n),
                        new StringMessageArg(pubid?.@string() ?? new StringC()));
        }
        else
        {
            WideChar resultChar;
            ISet<WideChar> resultChars = new ISet<WideChar>();
            switch (sd().internalCharset().univToDesc(univChar,
                                                      out resultChar,
                                                      resultChars))
            {
                case 1:
                    if (resultChar <= Constant.charMax)
                    {
                        isSgmlChar = true;
                        ch = (Char)resultChar;
                        return true;
                    }
                    goto case 2;
                case 2:
                    message(ParserMessages.numericCharRefBadInternal,
                            new NumberMessageArg(ch));
                    break;
                default:
                    message(ParserMessages.numericCharRefNoInternal,
                            new NumberMessageArg(ch));
                    break;
            }
        }
        return false;
    }
    // Boolean parseNamedCharRef();
    protected Boolean parseNamedCharRef()
    {
        if (options().warnNamedCharRef)
            message(ParserMessages.namedCharRef);
        InputSource? ins = currentInput();
        if (ins == null) return false;
        Index startIndex = currentLocation().index();
        ins.discardInitial();
        extendNameToken(syntax().namelen(), ParserMessages.nameLength);
        Char c = 0;
        Boolean valid;
        StringC name = new StringC();
        getCurrentToken(syntax().generalSubstTable(), name);
        if (!syntax().lookupFunctionChar(name, out c))
        {
            message(ParserMessages.functionName, new StringMessageArg(name));
            valid = false;
        }
        else
        {
            valid = true;
            if (wantMarkup())
                getCurrentToken(name);  // the original name
        }
        NamedCharRef.RefEndType refEndType;
        switch (getToken(Mode.refMode))
        {
            case Tokens.tokenRefc:
                refEndType = NamedCharRef.RefEndType.endRefc;
                break;
            case Tokens.tokenRe:
                refEndType = NamedCharRef.RefEndType.endRE;
                if (options().warnRefc)
                    message(ParserMessages.refc);
                break;
            default:
                refEndType = NamedCharRef.RefEndType.endOmitted;
                if (options().warnRefc)
                    message(ParserMessages.refc);
                break;
        }
        ins.startToken();
        if (valid)
            ins.pushCharRef(c, new NamedCharRef(startIndex, refEndType, name));
        return true;
    }
    // Boolean parseEntityReference(Boolean isParameter, int ignoreLevel,
    //                               ConstPtr<Entity> &entity, Ptr<EntityOrigin> &origin);
    // ignoreLevel: 0 means don't ignore;
    // 1 means parse name group and ignore if inactive
    // 2 means ignore
    protected Boolean parseEntityReference(Boolean isParameter, int ignoreLevel,
                                           ConstPtr<Entity> entity, Ptr<EntityOrigin> origin)
    {
        InputSource? ins = currentInput();
        if (ins == null) return false;
        Location startLocation = ins.currentLocation();
        Owner<Markup> markupPtr = new Owner<Markup>();
        if (wantMarkup())
        {
            markupPtr = new Owner<Markup>(new Markup());
            markupPtr.pointer()!.addDelim(isParameter ? Syntax.DelimGeneral.dPERO : Syntax.DelimGeneral.dERO);
        }
        if (ignoreLevel == 1)
        {
            Markup savedMarkup = new Markup();
            Markup? savedCurrentMarkup = currentMarkup();
            if (savedCurrentMarkup != null)
                savedCurrentMarkup.swap(savedMarkup);
            Location savedMarkupLocation = markupLocation();
            startMarkup(markupPtr.pointer() != null, startLocation);
            if (markupPtr.pointer() != null)
            {
                markupPtr.pointer()!.addDelim(Syntax.DelimGeneral.dGRPO);
                markupPtr.pointer()!.swap(currentMarkup()!);
            }
            Boolean ignore = false;
            if (!parseEntityReferenceNameGroup(ref ignore))
                return false;
            if (markupPtr.pointer() != null)
                currentMarkup()!.swap(markupPtr.pointer()!);
            startMarkup(savedCurrentMarkup != null, savedMarkupLocation);
            if (savedCurrentMarkup != null)
                savedMarkup.swap(currentMarkup()!);
            if (!ignore)
                ignoreLevel = 0;
            ins.startToken();
            Xchar c = ins.tokenChar(messenger());
            if (!syntax().isNameStartCharacter(c))
            {
                message(ParserMessages.entityReferenceMissingName);
                return false;
            }
        }
        ins.discardInitial();
        if (isParameter)
            extendNameToken(syntax().penamelen(), ParserMessages.parameterEntityNameLength);
        else
            extendNameToken(syntax().namelen(), ParserMessages.nameLength);
        StringC name = nameBuffer();
        getCurrentToken(syntax().entitySubstTable(), name);
        if (ignoreLevel != 0)
            entity.operatorAssign(new IgnoredEntity(name,
                                                    isParameter
                                                    ? Entity.DeclType.parameterEntity
                                                    : Entity.DeclType.generalEntity));
        else
        {
            entity.operatorAssign(lookupEntity(isParameter, name, startLocation, true));
            if (entity.isNull())
            {
                if (haveApplicableDtd())
                {
                    if (!isParameter)
                    {
                        entity.operatorAssign(createUndefinedEntity(name, startLocation));
                        if (!sd().implydefEntity())
                            message(ParserMessages.entityUndefined, new StringMessageArg(name));
                    }
                    else
                        message(ParserMessages.parameterEntityUndefined,
                                new StringMessageArg(name));
                }
                else
                    message(ParserMessages.entityApplicableDtd);
            }
            else if (entity.pointer()!.defaulted() && options().warnDefaultEntityReference)
                message(ParserMessages.defaultEntityReference, new StringMessageArg(name));
        }
        if (markupPtr.pointer() != null)
        {
            markupPtr.pointer()!.addName(ins);
            switch (getToken(Mode.refMode))
            {
                case Tokens.tokenRefc:
                    markupPtr.pointer()!.addDelim(Syntax.DelimGeneral.dREFC);
                    break;
                case Tokens.tokenRe:
                    markupPtr.pointer()!.addRefEndRe();
                    if (options().warnRefc)
                        message(ParserMessages.refc);
                    break;
                default:
                    if (options().warnRefc)
                        message(ParserMessages.refc);
                    break;
            }
        }
        else if (options().warnRefc)
        {
            if (getToken(Mode.refMode) != Tokens.tokenRefc)
                message(ParserMessages.refc);
        }
        else
            getToken(Mode.refMode);
        if (!entity.isNull())
            origin.operatorAssign(EntityOrigin.make(internalAllocator(),
                                                    entity,
                                                    startLocation,
                                                    currentLocation().index()
                                                    + (Index)ins.currentTokenLength()
                                                    - startLocation.index(),
                                                    markupPtr));
        else
            origin.clear();
        return true;
    }
    protected virtual Boolean parseEntityReferenceNameGroup(ref Boolean ignore) { throw new NotImplementedException(); }

    // From parseInstance.cxx
    // void parsePcdata();
    protected void parsePcdata()
    {
        extendData();
        acceptPcdata(currentLocation());
        noteData();
        eventHandler().data(new ImmediateDataEvent(Event.Type.characterData,
                                                   currentInput()!.currentTokenStart(),
                                                   currentInput()!.currentTokenLength(),
                                                   currentLocation(),
                                                   false));
    }
    // void parseStartTag();
    protected void parseStartTag()
    {
        InputSource? ins = currentInput();
        if (ins == null) return;
        Markup? markup = startMarkup(eventsWanted().wantInstanceMarkup(),
                                     ins.currentLocation());
        if (markup != null)
            markup.addDelim(Syntax.DelimGeneral.dSTAGO);
        Boolean netEnabling = false;
        StartElementEvent? @event = doParseStartTag(ref netEnabling);
        if (@event != null)
            acceptStartTag(@event.elementType(), @event, netEnabling);
    }

    // StartElementEvent *doParseStartTag(Boolean &netEnabling);
    protected StartElementEvent? doParseStartTag(ref Boolean netEnabling)
    {
        Markup? markup = currentMarkup();
        InputSource? ins = currentInput();
        if (ins == null) return null;
        ins.discardInitial();
        extendNameToken(syntax().namelen(), ParserMessages.nameLength);
        if (markup != null)
            markup.addName(ins);
        StringC name = nameBuffer();
        getCurrentToken(syntax().generalSubstTable(), name);
        ElementType? e = currentDtdNonConst().lookupElementType(name);
        if (sd().rank())
        {
            if (e == null)
                e = completeRankStem(name);
            else if (e.isRankedElement())
                handleRankedElement(e);
        }
        if (e == null)
            e = lookupCreateUndefinedElement(name, currentLocation(), currentDtdNonConst(), implydefElement() != Sd.ImplydefElement.implydefElementAnyother);
        AttributeList? attributes = allocAttributeList(e?.attributeDef(), 0);
        Token closeToken = getToken(Mode.tagMode);
        if (closeToken == Tokens.tokenTagc)
        {
            if (name.size() > syntax().taglen())
                checkTaglen(markupLocation().index());
            attributes?.finish(this);
            netEnabling = false;
            if (markup != null)
                markup.addDelim(Syntax.DelimGeneral.dTAGC);
        }
        else
        {
            ins.ungetToken();
            Ptr<AttributeDefinitionList> newAttDef = new Ptr<AttributeDefinitionList>();
            if (parseAttributeSpec(Mode.tagMode, attributes!, out netEnabling, newAttDef))
            {
                // The difference between the indices will be the difference
                // in offsets plus 1 for each named character reference.
                if (ins.currentLocation().index() - markupLocation().index()
                    > syntax().taglen())
                    checkTaglen(markupLocation().index());
            }
            else
                netEnabling = false;
            if (!newAttDef.isNull() && e != null)
            {
                newAttDef.pointer()!.setIndex(currentDtdNonConst().allocAttributeDefinitionListIndex());
                e.setAttributeDef(newAttDef);
            }
        }
        return new StartElementEvent(e,
                                     currentDtdPointer(),
                                     attributes,
                                     markupLocation(),
                                     markup);
    }

    // void handleRankedElement(const ElementType *e);
    protected void handleRankedElement(ElementType e)
    {
        StringC rankSuffix = new StringC(e.definition()!.rankSuffix());
        RankStem? rankStem = e.rankedElementRankStem();
        if (rankStem == null) return;
        for (nuint i = 0; i < rankStem.nDefinitions(); i++)
        {
            ElementDefinition? def = rankStem.definition(i);
            if (def == null) continue;
            for (nuint j = 0; j < def.nRankStems(); j++)
                setCurrentRank(def.rankStem(j), rankSuffix);
        }
    }

    // void parseEmptyStartTag();
    protected void parseEmptyStartTag()
    {
        if (options().warnEmptyTag)
            message(ParserMessages.emptyStartTag);
        if (!currentDtd().isBase())
            message(ParserMessages.emptyStartTagBaseDtd);
        ElementType? e = null;
        if (!sd().omittag())
            e = lastEndedElementType();
        else if (tagLevel() > 0)
            e = currentElement().type();
        if (e == null)
            e = currentDtd().documentElementType();
        AttributeList? attributes = allocAttributeList(e?.attributeDef(), 0);
        attributes?.finish(this);
        Markup? markup = startMarkup(eventsWanted().wantInstanceMarkup(),
                                     currentLocation());
        if (markup != null)
        {
            markup.addDelim(Syntax.DelimGeneral.dSTAGO);
            markup.addDelim(Syntax.DelimGeneral.dTAGC);
        }
        acceptStartTag(e,
                       new StartElementEvent(e,
                                             currentDtdPointer(),
                                             attributes,
                                             markupLocation(),
                                             markup),
                       false);
    }

    // void parseGroupStartTag();
    protected void parseGroupStartTag()
    {
        // Stub implementation - full implementation requires parseTagNameGroup
        throw new NotImplementedException();
    }

    // void parseGroupEndTag();
    protected void parseGroupEndTag()
    {
        // Stub implementation - full implementation requires parseTagNameGroup
        throw new NotImplementedException();
    }
    // EndElementEvent *parseEndTag();
    protected EndElementEvent? parseEndTag()
    {
        Markup? markup = startMarkup(eventsWanted().wantInstanceMarkup(),
                                     currentLocation());
        if (markup != null)
            markup.addDelim(Syntax.DelimGeneral.dETAGO);
        return doParseEndTag();
    }

    // EndElementEvent *doParseEndTag();
    protected EndElementEvent doParseEndTag()
    {
        Markup? markup = currentMarkup();
        currentInput()!.discardInitial();
        extendNameToken(syntax().namelen(), ParserMessages.nameLength);
        if (markup != null)
            markup.addName(currentInput()!);
        StringC name = nameBuffer();
        getCurrentToken(syntax().generalSubstTable(), name);
        ElementType? e = currentDtd().lookupElementType(name);
        if (sd().rank())
        {
            if (e == null)
                e = completeRankStem(name);
        }
        if (e == null)
            e = lookupCreateUndefinedElement(name, currentLocation(), currentDtdNonConst(), implydefElement() != Sd.ImplydefElement.implydefElementAnyother);
        parseEndTagClose();
        return new EndElementEvent(e,
                                   currentDtdPointer(),
                                   markupLocation(),
                                   markup);
    }

    // void parseEndTagClose();
    protected void parseEndTagClose()
    {
        for (;;)
        {
            Token token = getToken(Mode.tagMode);
            switch (token)
            {
                case Tokens.tokenUnrecognized:
                    if (!reportNonSgmlCharacter())
                        message(ParserMessages.endTagCharacter, new StringMessageArg(currentToken()));
                    return;
                case Tokens.tokenEe:
                    message(ParserMessages.endTagEntityEnd);
                    return;
                case Tokens.tokenEtago:
                case Tokens.tokenStago:
                    if (!sd().endTagUnclosed())
                        message(ParserMessages.unclosedEndTagShorttag);
                    currentInput()!.ungetToken();
                    return;
                case Tokens.tokenTagc:
                    if (currentMarkup() != null)
                        currentMarkup()!.addDelim(Syntax.DelimGeneral.dTAGC);
                    return;
                case Tokens.tokenS:
                    if (currentMarkup() != null)
                        currentMarkup()!.addS(currentChar());
                    break;
                default:
                    message(ParserMessages.endTagInvalidToken,
                            new TokenMessageArg(token, Mode.tagMode, syntaxPointer(), sdPointer()));
                    return;
            }
        }
    }

    // void parseEmptyEndTag();
    protected void parseEmptyEndTag()
    {
        if (options().warnEmptyTag)
            message(ParserMessages.emptyEndTag);
        if (!currentDtd().isBase())
            message(ParserMessages.emptyEndTagBaseDtd);
        if (tagLevel() == 0)
            message(ParserMessages.emptyEndTagNoOpenElements);
        else
        {
            Markup? markup = startMarkup(eventsWanted().wantInstanceMarkup(),
                                         currentLocation());
            if (markup != null)
            {
                markup.addDelim(Syntax.DelimGeneral.dETAGO);
                markup.addDelim(Syntax.DelimGeneral.dTAGC);
            }
            acceptEndTag(new EndElementEvent(currentElement().type(),
                                             currentDtdPointer(),
                                             currentLocation(),
                                             markup));
        }
    }

    // void parseNullEndTag();
    protected void parseNullEndTag()
    {
        // If a null end tag was recognized, then there must be a net enabling
        // element on the stack.
        for (;;)
        {
            if (tagLevel() <= 0) break; // ASSERT(tagLevel() > 0);
            if (currentElement().netEnabling())
                break;
            if (!currentElement().isFinished() && validate())
                message(ParserMessages.elementNotFinished,
                        new StringMessageArg(currentElement().type()!.name()));
            implyCurrentElementEnd(currentLocation());
        }
        if (!currentElement().isFinished() && validate())
            message(ParserMessages.elementEndTagNotFinished,
                    new StringMessageArg(currentElement().type()!.name()));
        Markup? markup = startMarkup(eventsWanted().wantInstanceMarkup(),
                                     currentLocation());
        if (markup != null)
            markup.addDelim(Syntax.DelimGeneral.dNET);
        acceptEndTag(new EndElementEvent(currentElement().type(),
                                         currentDtdPointer(),
                                         currentLocation(),
                                         markup));
    }
    // void endAllElements();
    protected void endAllElements()
    {
        while (tagLevel() > 0)
        {
            if (!currentElement().isFinished())
                message(ParserMessages.elementNotFinishedDocumentEnd,
                        new StringMessageArg(currentElement().type()!.name()));
            implyCurrentElementEnd(currentLocation());
        }
        if (!currentElement().isFinished() && validate())
            message(ParserMessages.noDocumentElement);
    }

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

    // void handleShortref(int index);
    protected void handleShortref(int index)
    {
        ConstPtr<Entity> entity = currentElement().map()!.entity((nuint)index);
        if (!entity.isNull())
        {
            Owner<Markup> markupPtr = new Owner<Markup>();
            if (eventsWanted().wantInstanceMarkup())
            {
                markupPtr = new Owner<Markup>(new Markup());
                markupPtr.pointer()!.addShortref(currentInput()!);
            }
            EntityOrigin originEntity = EntityOrigin.make(internalAllocator(),
                                                          entity,
                                                          currentLocation(),
                                                          (uint)currentInput()!.currentTokenLength(),
                                                          markupPtr);
            Ptr<EntityOrigin> origin = new Ptr<EntityOrigin>(originEntity);
            entity.pointer()!.contentReference(this, origin);
            return;
        }
        InputSource? ins = currentInput();
        if (ins == null) return;
        nuint length = ins.currentTokenLength();
        Char[]? s = ins.currentTokenStart();
        nuint i = 0;
        if (currentMode() == Mode.econMode || currentMode() == Mode.econnetMode)
        {
            // FIXME do this in advance (what about B sequence?)
            for (i = 0; i < length && s != null && syntax().isS((Xchar)s[i]); i++)
                ;
            if (i > 0 && eventsWanted().wantInstanceMarkup())
                eventHandler().sSep(new SSepEvent(s, i, currentLocation(), false));
        }
        if (i < length)
        {
            Location location = new Location(currentLocation());
            location.operatorPlusAssign((Index)i);
            acceptPcdata(location);
            if (sd().keeprsre())
            {
                noteData();
                if (s != null)
                {
                    Char[] subArray = new Char[length - i];
                    Array.Copy(s, (int)i, subArray, 0, (int)(length - i));
                    eventHandler().data(new ImmediateDataEvent(Event.Type.characterData, subArray, length - i,
                                                               location, false));
                }
                return;
            }
            // FIXME speed this up
            for (; length > 0 && s != null; location.operatorPlusAssign(1), length--, i++)
            {
                if (s[i] == syntax().standardFunction((int)Syntax.StandardFunction.fRS))
                {
                    noteRs();
                    if (eventsWanted().wantInstanceMarkup())
                        eventHandler().ignoredRs(new IgnoredRsEvent(s[i], location));
                }
                else if (s[i] == syntax().standardFunction((int)Syntax.StandardFunction.fRE))
                    queueRe(location);
                else
                {
                    noteData();
                    Char[] singleChar = new Char[] { s[i] };
                    eventHandler().data(new ImmediateDataEvent(Event.Type.characterData, singleChar, 1,
                                                               location, false));
                }
            }
        }
    }
    // void endInstance();
    protected void endInstance()
    {
        // Do checking before popping entity stack so that there's a
        // current location for error messages.
        endAllElements();
        while (markedSectionLevel() > 0)
        {
            // TODO: auxiliary location support - currentMarkedSectionStartLocation()
            message(ParserMessages.unclosedMarkedSection,
                    new StringMessageArg(new StringC()));
            endMarkedSection();
        }
        checkIdrefs();
        popInputStack();
        allDone();
    }

    // void checkIdrefs();
    protected void checkIdrefs()
    {
        NamedTableIter<Id> iter = idTableIter();
        Id? id;
        while ((id = iter.next()) != null)
        {
            for (nuint i = 0; i < id.pendingRefs().size(); i++)
            {
                setNextLocation(id.pendingRefs()[(int)i]);
                message(ParserMessages.missingId, new StringMessageArg(id.name()));
            }
        }
    }
    // void checkTaglen(Index tagStartIndex);
    protected void checkTaglen(Index tagStartIndex)
    {
        InputSourceOrigin? origin = currentLocation().origin().pointer()?.asInputSourceOrigin();
        if (origin == null) return;
        if (origin.startOffset(currentLocation().index())
            - origin.startOffset(tagStartIndex
                                 + (Index)syntax().delimGeneral((int)Syntax.DelimGeneral.dSTAGO).size())
            > syntax().taglen())
            message(ParserMessages.taglen, new NumberMessageArg(syntax().taglen()));
    }
    // void endProlog();
    protected void endProlog()
    {
        if (baseDtd().isNull())
        {
            // We could continue, but there's not a lot of point.
            giveUp();
            return;
        }
        if (maybeStartPass2())
            setPhase(Phase.prologPhase);
        else
        {
            if (inputLevel() == 0)
            {
                allDone();
                return;
            }
            if (pass2())
                checkEntityStability();
            setPhase(Phase.instanceStartPhase);
            startInstance();
            ConstPtr<ComplexLpd> lpd = new ConstPtr<ComplexLpd>();
            Vector<AttributeList> simpleLinkAtts = new Vector<AttributeList>();
            Vector<StringC> simpleLinkNames = new Vector<StringC>();
            for (nuint i = 0; i < nActiveLink(); i++)
            {
                if (activeLpd(i).type() == Lpd.Type.simpleLink)
                {
                    SimpleLpd slpd = (SimpleLpd)activeLpd(i);
                    simpleLinkNames.push_back(slpd.name());
                    simpleLinkAtts.resize(simpleLinkAtts.size() + 1);
                    simpleLinkAtts.back().init(slpd.attributeDef());
                    simpleLinkAtts.back().finish(this);
                }
                else
                    lpd = new ConstPtr<ComplexLpd>((ComplexLpd)activeLpd(i));
            }
            eventHandler().endProlog(new EndPrologEvent(currentDtdPointer(),
                                                        lpd,
                                                        simpleLinkNames,
                                                        simpleLinkAtts,
                                                        currentLocation()));
        }
    }

    // From parseAttribute.cxx
    protected virtual Boolean parseAttributeSpec(Mode mode, AttributeList atts, out Boolean netEnabling,
                                                  Ptr<AttributeDefinitionList> newAttDefList)
    { throw new NotImplementedException(); }
    protected virtual Boolean handleAttributeNameToken(Text text, AttributeList atts, ref uint specLength) { throw new NotImplementedException(); }

    // Helper methods from parseDecl.cxx

    // Boolean lookingAtStartTag(StringC &gi);
    protected virtual Boolean lookingAtStartTag(StringC gi) { throw new NotImplementedException(); }

    // void implyDtd(const StringC &gi);
    protected virtual void implyDtd(StringC gi)
    {
        startMarkup(eventsWanted().wantPrologMarkup(), currentLocation());
        if (sd().concur() > 0 || sd().explicitLink() > 0
            || (sd().implydefElement() == Sd.ImplydefElement.implydefElementNo
                && !sd().implydefDoctype()))
            message(ParserMessages.omittedProlog);

        if ((sd().implydefElement() != Sd.ImplydefElement.implydefElementNo) && !sd().implydefDoctype())
        {
            eventHandler().startDtd(new StartDtdEvent(gi, new ConstPtr<Entity>(), false,
                                                       markupLocation(),
                                                       currentMarkup()));
            startDtd(gi);
            parseDoctypeDeclEnd(true);
            return;
        }
        ExternalId id = new ExternalId();
        // The null location indicates that this is a fake entity.
        Entity tem = new ExternalTextEntity(gi, EntityDecl.DeclType.doctype, new Location(), id);
        ConstPtr<Entity> entity = new ConstPtr<Entity>(tem);
        if (sd().implydefDoctype())
            tem.generateSystemId(this);
        else
        {
            // Don't use Entity.generateSystemId because we don't want an error
            // if it fails.
            StringC str = new StringC();
            if (!entityCatalog().lookup(entity.pointer()!, new EntityCatalog.SyntaxAdapter(syntax()), sd().internalCharset(),
                                         (Messenger)this, str))
            {
                message(ParserMessages.noDtd);
                enableImplydef();
                eventHandler().startDtd(new StartDtdEvent(gi, new ConstPtr<Entity>(), false,
                                                           markupLocation(),
                                                           currentMarkup()));
                startDtd(gi);
                parseDoctypeDeclEnd(true);
                return;
            }
            id.setEffectiveSystem(str);
            entity = new ConstPtr<Entity>(new ExternalTextEntity(gi,
                                                                  EntityDecl.DeclType.doctype,
                                                                  new Location(),
                                                                  id));
            StringC declStr = new StringC();
            declStr.operatorPlusAssign(syntax().delimGeneral((int)Syntax.DelimGeneral.dMDO));
            declStr.operatorPlusAssign(syntax().reservedName(Syntax.ReservedName.rDOCTYPE));
            Char[] spaceChar = new Char[] { syntax().space() };
            declStr.append(spaceChar, 1);
            declStr.operatorPlusAssign(gi);
            declStr.append(spaceChar, 1);
            declStr.operatorPlusAssign(syntax().reservedName(Syntax.ReservedName.rSYSTEM));
            declStr.operatorPlusAssign(syntax().delimGeneral((int)Syntax.DelimGeneral.dMDC));
            message(ParserMessages.implyingDtd, new StringMessageArg(declStr));
        }
        Ptr<EntityOrigin> origin = new Ptr<EntityOrigin>(EntityOrigin.make(internalAllocator(), entity, currentLocation()));
        eventHandler().startDtd(new StartDtdEvent(gi, entity, false,
                                                   markupLocation(),
                                                   currentMarkup()));
        startDtd(gi);
        entity.pointer()!.dsReference(this, origin);
        if (inputLevel() == 1)
            parseDoctypeDeclEnd(true);
        else
            setPhase(Phase.declSubsetPhase);
    }

    // void checkDtd(Dtd &dtd);
    protected virtual void checkDtd(Dtd dtd)
    {
        if (dtd.isBase())
            addNeededShortrefs(dtd, instanceSyntax());
        if (sd().www() || !options().errorAfdr)
            addCommonAttributes(dtd);
        NamedTableIter<ElementType> elementIter = dtd.elementTypeIter();
        ElementType? p;
        ConstPtr<ElementDefinition> def = new ConstPtr<ElementDefinition>();
        int i = 0;
        while ((p = elementIter.next()) != null)
        {
            if (p.definition() == null)
            {
                if (p.name() == dtd.name())
                {
                    if (validate() && (implydefElement() == Sd.ImplydefElement.implydefElementNo))
                        message(ParserMessages.documentElementUndefined);
                }
                else if (options().warnUndefinedElement)
                    message(ParserMessages.dtdUndefinedElement, new StringMessageArg(p.name()));
                if (def.isNull())
                    def = new ConstPtr<ElementDefinition>(new ElementDefinition(currentLocation(),
                                                                                  nuint.MaxValue,
                                                                                  (byte)ElementDefinition.OmitFlags.omitEnd,
                                                                                  ElementDefinition.DeclaredContent.any,
                                                                                  (implydefElement() != Sd.ImplydefElement.implydefElementAnyother)));
                p.setElementDefinition(def, (nuint)(i++));
            }
            ShortReferenceMap? map = p.map();
            if (map != null && map != ContentState.theEmptyMap && !map.defined())
            {
                if (validate())
                    message(ParserMessages.undefinedShortrefMapDtd,
                            new StringMessageArg(map.name()),
                            new StringMessageArg(p.name()));
                p.setMap(null);
            }
        }
        NamedTableIter<ShortReferenceMap> mapIter = dtd.shortReferenceMapIter();
        nuint nShortref = dtd.nShortref();
        for (; ; )
        {
            ShortReferenceMap? map = mapIter.next();
            if (map == null)
                break;
            Vector<ConstPtr<Entity>> entityMap = new Vector<ConstPtr<Entity>>(nShortref);
            for (nuint j = 0; j < nShortref; j++)
            {
                StringC? entityName = map.entityName(j);
                if (entityName != null)
                {
                    ConstPtr<Entity> entity
                        = lookupEntity(false, entityName, map.defLocation(), false);
                    if (entity.isNull())
                    {
                        setNextLocation(map.defLocation());
                        message(ParserMessages.mapEntityUndefined,
                                new StringMessageArg(entityName),
                                new StringMessageArg(map.name()));
                    }
                    else
                    {
                        if (entity.pointer()!.defaulted() && options().warnDefaultEntityReference)
                        {
                            setNextLocation(map.defLocation());
                            message(ParserMessages.mapDefaultEntity,
                                    new StringMessageArg(entityName),
                                    new StringMessageArg(map.name()));
                        }
                        entityMap[j] = entity;
                    }
                }
            }
            map.setEntityMap(entityMap);
            if (options().warnUnusedMap && !map.used())
            {
                setNextLocation(map.defLocation());
                message(ParserMessages.unusedMap, new StringMessageArg(map.name()));
            }
        }
        if (options().warnUnusedParam)
        {
            ConstNamedResourceTableIter<Entity> entityIter = dtd.parameterEntityIterConst();
            for (; ; )
            {
                ConstPtr<Entity> entity = entityIter.next();
                if (entity.isNull())
                    break;
                if (!entity.pointer()!.used() && !maybeStatusKeyword(entity.pointer()!))
                {
                    setNextLocation(entity.pointer()!.defLocation());
                    message(ParserMessages.unusedParamEntity,
                            new StringMessageArg(entity.pointer()!.name()));
                }
            }
        }
        ConstNamedResourceTableIter<Entity> gEntityIter = dtd.generalEntityIterConst();
        ConstNamedResourceTableIter<Entity> pEntityIter = dtd.parameterEntityIterConst();
        for (i = 0; i < (sd().www() ? 2 : 1); i++)
        {
            for (; ; )
            {
                ConstPtr<Entity> entity = (i == 0) ? gEntityIter.next() : pEntityIter.next();
                if (entity.isNull())
                    break;
                ExternalDataEntity? external = entity.pointer()!.asExternalDataEntity();
                if (external != null)
                {
                    Notation? notation = external.notation() as Notation;
                    if (notation != null && !notation.defined())
                    {
                        if (sd().implydefNotation())
                        {
                            ExternalId id = new ExternalId();
                            notation.setExternalId(id, new Location());
                            notation.generateSystemId(this);
                        }
                        else if (validate())
                        {
                            setNextLocation(external.defLocation());
                            switch (external.declType())
                            {
                                case EntityDecl.DeclType.parameterEntity:
                                    message(ParserMessages.parameterEntityNotationUndefined,
                                            new StringMessageArg(notation.name()),
                                            new StringMessageArg(external.name()));
                                    break;
                                case EntityDecl.DeclType.doctype:
                                    message(ParserMessages.dsEntityNotationUndefined,
                                            new StringMessageArg(notation.name()));
                                    break;
                                default:
                                    message(ParserMessages.entityNotationUndefined,
                                            new StringMessageArg(notation.name()),
                                            new StringMessageArg(external.name()));
                                    break;
                            }
                        }
                    }
                }
            }
        }
        NamedResourceTableIter<Notation> notationIter = dtd.notationIter();
        for (; ; )
        {
            Ptr<Notation> notation = notationIter.next();
            if (notation.isNull())
                break;
            if (!notation.pointer()!.defined() && !notation.pointer()!.attributeDef().isNull())
            {
                if (sd().implydefNotation())
                {
                    ExternalId id = new ExternalId();
                    notation.pointer()!.setExternalId(id, new Location());
                    notation.pointer()!.generateSystemId(this);
                }
                else if (validate())
                    message(ParserMessages.attlistNotationUndefined,
                            new StringMessageArg(notation.pointer()!.name()));
            }
        }
    }

    // Boolean maybeStatusKeyword(const Entity &entity);
    protected Boolean maybeStatusKeyword(Entity entity)
    {
        InternalEntity? @internal = entity.asInternalEntity();
        if (@internal == null)
            return false;
        StringC text = @internal.@string();
        Syntax.ReservedName[] statusKeywords = new Syntax.ReservedName[]
        {
            Syntax.ReservedName.rINCLUDE, Syntax.ReservedName.rIGNORE
        };
        foreach (var keyword in statusKeywords)
        {
            StringC keywordStr = instanceSyntax().reservedName(keyword);
            nuint j = 0;
            while (j < text.size() && instanceSyntax().isS((Xchar)text[j]))
                j++;
            nuint k = 0;
            while (j < text.size()
                   && k < keywordStr.size()
                   && (instanceSyntax().generalSubstTable()![text[j]]
                       == keywordStr[k]))
            {
                j++;
                k++;
            }
            if (k == keywordStr.size())
            {
                while (j < text.size() && instanceSyntax().isS((Xchar)text[j]))
                    j++;
                if (j == text.size())
                    return true;
            }
        }
        return false;
    }

    // void addCommonAttributes(Dtd &dtd);
    protected void addCommonAttributes(Dtd dtd)
    {
        // Simplified implementation - full implementation is complex
        // This handles the #ALL and #IMPLICIT element/notation attribute definitions
        // For now, just a stub that does nothing
    }

    // From parseSd.cxx
    protected virtual Boolean implySgmlDecl() { throw new NotImplementedException(); }
    protected virtual Boolean scanForSgmlDecl(CharsetInfo initCharset) { throw new NotImplementedException(); }
    protected virtual Boolean parseSgmlDecl() { throw new NotImplementedException(); }

    // Additional helper methods from parseInstance.cxx

    // void acceptEndTag(EndElementEvent *event);
    protected void acceptEndTag(EndElementEvent? @event)
    {
        if (@event == null) return;
        ElementType? e = @event.elementType();
        if (e == null || !elementIsOpen(e))
        {
            message(ParserMessages.elementNotOpen, new StringMessageArg(e?.name() ?? new StringC()));
            return;
        }
        for (;;)
        {
            if (currentElement().type() == e)
                break;
            if (!currentElement().isFinished() && validate())
                message(ParserMessages.elementNotFinished,
                        new StringMessageArg(currentElement().type()!.name()));
            implyCurrentElementEnd(@event.location());
        }
        if (!currentElement().isFinished() && validate())
            message(ParserMessages.elementEndTagNotFinished,
                    new StringMessageArg(currentElement().type()!.name()));
        if (currentElement().included())
            @event.setIncluded();
        noteEndElement(@event.included());
        eventHandler().endElement(@event);
        popElement();
    }

    // void implyCurrentElementEnd(const Location &loc);
    protected void implyCurrentElementEnd(Location loc)
    {
        if (!sd().omittag())
            message(ParserMessages.omitEndTagOmittag,
                    new StringMessageArg(currentElement().type()!.name()),
                    new LocationMessageArg(currentElement().startLocation()));
        else
        {
            ElementDefinition? def = currentElement().type()?.definition();
            if (def != null && !def.canOmitEndTag())
                message(ParserMessages.omitEndTagDeclare,
                        new StringMessageArg(currentElement().type()!.name()),
                        new LocationMessageArg(currentElement().startLocation()));
        }
        EndElementEvent @event = new EndElementEvent(currentElement().type(),
                                                     currentDtdPointer(),
                                                     loc,
                                                     null);
        if (currentElement().included())
            @event.setIncluded();
        noteEndElement(@event.included());
        eventHandler().endElement(@event);
        popElement();
    }

    // void acceptPcdata(const Location &startLocation);
    protected void acceptPcdata(Location startLocation)
    {
        if (currentElement().tryTransitionPcdata())
            return;
        // Need to test here since implying tags may turn off pcdataRecovering.
        if (pcdataRecovering())
            return;
        IList<Undo> undoList = new IList<Undo>();
        IList<Event> eventList = new IList<Event>();
        uint startImpliedCount = 0;
        uint attributeListIndex = 0;
        keepMessages();
        while (tryImplyTag(startLocation, ref startImpliedCount, ref attributeListIndex,
                           undoList, eventList))
            if (currentElement().tryTransitionPcdata())
            {
                queueElementEvents(eventList);
                return;
            }
        discardKeptMessages();
        undo(undoList);
        if (validate() || afterDocumentElement())
            message(ParserMessages.pcdataNotAllowed);
        pcdataRecover();
    }

    // void undo(IList<Undo> &undoList);
    protected void undo(IList<Undo> undoList)
    {
        while (!undoList.empty())
        {
            Undo? p = undoList.get();
            if (p != null)
                p.undo(this);
        }
    }

    // void queueElementEvents(IList<Event> &events);
    protected void queueElementEvents(IList<Event> events)
    {
        releaseKeptMessages();
        // FIXME provide IList<T>::reverse function
        // reverse it
        IList<Event> tem = new IList<Event>();
        while (!events.empty())
            tem.insert(events.get());
        while (!tem.empty())
        {
            Event? e = tem.get();
            if (e == null) continue;
            if (e.type() == Event.Type.startElement)
            {
                StartElementEvent? se = e as StartElementEvent;
                if (se != null)
                {
                    noteStartElement(se.included());
                    eventHandler().startElement(se);
                }
            }
            else
            {
                EndElementEvent? ee = e as EndElementEvent;
                if (ee != null)
                {
                    noteEndElement(ee.included());
                    eventHandler().endElement(ee);
                }
            }
        }
    }

    // Boolean tryImplyTag(const Location &loc, unsigned &startImpliedCount,
    //                     unsigned &attributeListIndex, IList<Undo> &undo, IList<Event> &eventList);
    protected Boolean tryImplyTag(Location loc,
                                  ref uint startImpliedCount,
                                  ref uint attributeListIndex,
                                  IList<Undo> undoList,
                                  IList<Event> eventList)
    {
        if (!sd().omittag())
            return false;
        if (currentElement().isFinished())
        {
            if (tagLevel() == 0)
                return false;
            ElementDefinition? def = currentElement().type()?.definition();
            if (def != null && !def.canOmitEndTag())
                return false;
            // imply an end tag
            if (startImpliedCount > 0)
            {
                message(ParserMessages.startTagEmptyElement,
                        new StringMessageArg(currentElement().type()!.name()));
                startImpliedCount--;
            }
            EndElementEvent @event = new EndElementEvent(currentElement().type(),
                                                         currentDtdPointer(),
                                                         loc,
                                                         null);
            eventList.insert(@event);
            undoList.insert(new UndoEndTag(popSaveElement()));
            return true;
        }
        LeafContentToken? token = currentElement().impliedStartTag();
        if (token == null)
            return false;
        ElementType? e = token.elementType();
        if (e == null) return false;
        if (elementIsExcluded(e))
            message(ParserMessages.requiredElementExcluded,
                    new OrdinalMessageArg(token.typeIndex() + 1),
                    new StringMessageArg(e.name()),
                    new StringMessageArg(currentElement().type()!.name()));
        if (tagLevel() != 0)
            undoList.insert(new UndoTransition(currentElement().matchState()));
        currentElement().doRequiredTransition();
        ElementDefinition? eDef = e.definition();
        if (eDef == null) return false;
        if (eDef.declaredContent() != ElementDefinition.DeclaredContent.modelGroup
            && eDef.declaredContent() != ElementDefinition.DeclaredContent.any)
            message(ParserMessages.omitStartTagDeclaredContent,
                    new StringMessageArg(e.name()));
        if (eDef.undefined())
            message(ParserMessages.undefinedElement, new StringMessageArg(e.name()));
        else if (!eDef.canOmitStartTag())
            message(ParserMessages.omitStartTagDeclare, new StringMessageArg(e.name()));
        AttributeList? attributes = allocAttributeList(e.attributeDef(),
                                                       attributeListIndex++);
        // this will give an error if the element has a required attribute
        attributes?.finish(this);
        startImpliedCount++;
        StartElementEvent startEvent = new StartElementEvent(e,
                                                             currentDtdPointer(),
                                                             attributes,
                                                             loc,
                                                             null);
        pushElementCheck(e, startEvent, undoList, eventList);
        const int implyCheckLimit = 30; // this is fairly arbitrary
        if (startImpliedCount > implyCheckLimit
            && !checkImplyLoop(startImpliedCount))
            return false;
        return true;
    }

    // void pushElementCheck(const ElementType *e, StartElementEvent *event, Boolean netEnabling);
    protected void pushElementCheck(ElementType? e, StartElementEvent? @event, Boolean netEnabling)
    {
        if (e == null || @event == null) return;
        if (tagLevel() == syntax().taglvl())
            message(ParserMessages.taglvlOpenElements, new NumberMessageArg(syntax().taglvl()));
        noteStartElement(@event.included());
        if (@event.mustOmitEnd())
        {
            if (sd().emptyElementNormal())
            {
                Boolean included = @event.included();
                Location loc = new Location(@event.location());
                eventHandler().startElement(@event);
                endTagEmptyElement(e, netEnabling, included, loc);
            }
            else
            {
                EndElementEvent end = new EndElementEvent(e,
                                                          currentDtdPointer(),
                                                          @event.location(),
                                                          null);
                if (@event.included())
                {
                    end.setIncluded();
                    noteEndElement(true);
                }
                else
                    noteEndElement(false);
                eventHandler().startElement(@event);
                eventHandler().endElement(end);
            }
        }
        else
        {
            ShortReferenceMap? map = e.map();
            if (map == null)
                map = currentElement().map();
            if (options().warnImmediateRecursion
                && e == currentElement().type())
                message(ParserMessages.immediateRecursion);
            pushElement(new OpenElement(e,
                                        netEnabling,
                                        @event.included(),
                                        map,
                                        @event.location()));
            // Can't access event after it's passed to the event handler.
            eventHandler().startElement(@event);
        }
    }

    // void pushElementCheck(const ElementType *e, StartElementEvent *event,
    //                       IList<Undo> &undoList, IList<Event> &eventList);
    protected void pushElementCheck(ElementType? e, StartElementEvent? @event,
                                    IList<Undo> undoList, IList<Event> eventList)
    {
        if (e == null || @event == null) return;
        if (tagLevel() == syntax().taglvl())
            message(ParserMessages.taglvlOpenElements, new NumberMessageArg(syntax().taglvl()));
        eventList.insert(@event);
        if (@event.mustOmitEnd())
        {
            EndElementEvent end = new EndElementEvent(e,
                                                      currentDtdPointer(),
                                                      @event.location(),
                                                      null);
            if (@event.included())
                end.setIncluded();
            eventList.insert(end);
        }
        else
        {
            undoList.insert(new UndoStartTag());
            ShortReferenceMap? map = e.map();
            if (map == null)
                map = currentElement().map();
            pushElement(new OpenElement(e,
                                        false,
                                        @event.included(),
                                        map,
                                        @event.location()));
        }
    }

    // void endTagEmptyElement(const ElementType *e, Boolean netEnabling,
    //                         Boolean included, const Location &startLoc);
    protected void endTagEmptyElement(ElementType e, Boolean netEnabling,
                                      Boolean included, Location startLoc)
    {
        Token token = getToken(netEnabling ? Mode.econnetMode : Mode.econMode);
        switch (token)
        {
            case Tokens.tokenNet:
                if (netEnabling)
                {
                    Markup? markup = startMarkup(eventsWanted().wantInstanceMarkup(),
                                                 currentLocation());
                    if (markup != null)
                        markup.addDelim(Syntax.DelimGeneral.dNET);
                    EndElementEvent end = new EndElementEvent(e,
                                                              currentDtdPointer(),
                                                              currentLocation(),
                                                              markup);
                    if (included)
                        end.setIncluded();
                    eventHandler().endElement(end);
                    noteEndElement(included);
                    return;
                }
                break;
            case Tokens.tokenEtagoTagc:
                {
                    if (options().warnEmptyTag)
                        message(ParserMessages.emptyEndTag);
                    Markup? markup = startMarkup(eventsWanted().wantInstanceMarkup(),
                                                 currentLocation());
                    if (markup != null)
                    {
                        markup.addDelim(Syntax.DelimGeneral.dETAGO);
                        markup.addDelim(Syntax.DelimGeneral.dTAGC);
                    }
                    EndElementEvent end = new EndElementEvent(e,
                                                              currentDtdPointer(),
                                                              currentLocation(),
                                                              markup);
                    if (included)
                        end.setIncluded();
                    eventHandler().endElement(end);
                    noteEndElement(included);
                    return;
                }
            case Tokens.tokenEtagoNameStart:
                {
                    EndElementEvent? end = parseEndTag();
                    if (end != null && end.elementType() == e)
                    {
                        if (included)
                            end.setIncluded();
                        eventHandler().endElement(end);
                        noteEndElement(included);
                        return;
                    }
                    if (end != null && !elementIsOpen(end.elementType()))
                    {
                        message(ParserMessages.elementNotOpen,
                                new StringMessageArg(end.elementType()?.name() ?? new StringC()));
                        break;
                    }
                    implyEmptyElementEnd(e, included, startLoc);
                    acceptEndTag(end);
                    return;
                }
            default:
                break;
        }
        implyEmptyElementEnd(e, included, startLoc);
        currentInput()?.ungetToken();
    }

    // void implyEmptyElementEnd(const ElementType *e, Boolean included, const Location &startLoc);
    protected void implyEmptyElementEnd(ElementType e, Boolean included, Location startLoc)
    {
        if (!sd().omittag())
            message(ParserMessages.omitEndTagOmittag,
                    new StringMessageArg(e.name()),
                    new LocationMessageArg(startLoc));
        else
        {
            ElementDefinition? def = e.definition();
            if (def != null && !def.canOmitEndTag())
                message(ParserMessages.omitEndTagDeclare,
                        new StringMessageArg(e.name()),
                        new LocationMessageArg(startLoc));
        }
        EndElementEvent end = new EndElementEvent(e,
                                                  currentDtdPointer(),
                                                  currentLocation(),
                                                  null);
        if (included)
            end.setIncluded();
        noteEndElement(included);
        eventHandler().endElement(end);
    }

    // void acceptStartTag(const ElementType *e, StartElementEvent *event, Boolean netEnabling);
    protected void acceptStartTag(ElementType? e, StartElementEvent? @event, Boolean netEnabling)
    {
        if (e == null || @event == null) return;
        if (e.definition()!.undefined() && implydefElement() == Sd.ImplydefElement.implydefElementNo)
            message(ParserMessages.undefinedElement, new StringMessageArg(e.name()));
        if (elementIsExcluded(e))
        {
            keepMessages();
            if (validate())
                checkExclusion(e);
        }
        else
        {
            if (currentElement().tryTransition(e))
            {
                pushElementCheck(e, @event, netEnabling);
                return;
            }
            if (elementIsIncluded(e))
            {
                @event.setIncluded();
                pushElementCheck(e, @event, netEnabling);
                return;
            }
            keepMessages();
        }
        IList<Undo> undoList = new IList<Undo>();
        IList<Event> eventList = new IList<Event>();
        uint startImpliedCount = 0;
        uint attributeListIndex = 1;
        while (tryImplyTag(@event.location(), ref startImpliedCount,
                           ref attributeListIndex, undoList, eventList))
            if (tryStartTag(e, @event, netEnabling, eventList))
                return;
        discardKeptMessages();
        undo(undoList);
        if (validate() && !e.definition()!.undefined())
            handleBadStartTag(e, @event, netEnabling);
        else
        {
            if (validate() ? (implydefElement() != Sd.ImplydefElement.implydefElementNo) : afterDocumentElement())
                message(ParserMessages.elementNotAllowed, new StringMessageArg(e.name()));
            // If element couldn't occur because it was excluded, then
            // do the transition here.
            currentElement().tryTransition(e);
            pushElementCheck(e, @event, netEnabling);
        }
    }

    // Boolean tryStartTag(const ElementType *e, StartElementEvent *event,
    //                     Boolean netEnabling, IList<Event> &impliedEvents);
    protected Boolean tryStartTag(ElementType? e, StartElementEvent? @event,
                                  Boolean netEnabling, IList<Event> impliedEvents)
    {
        if (e == null || @event == null) return false;
        if (elementIsExcluded(e))
        {
            checkExclusion(e);
            return false;
        }
        if (currentElement().tryTransition(e))
        {
            queueElementEvents(impliedEvents);
            pushElementCheck(e, @event, netEnabling);
            return true;
        }
        if (elementIsIncluded(e))
        {
            queueElementEvents(impliedEvents);
            @event.setIncluded();
            pushElementCheck(e, @event, netEnabling);
            return true;
        }
        return false;
    }

    // void checkExclusion(const ElementType *e);
    protected void checkExclusion(ElementType? e)
    {
        LeafContentToken? token = currentElement().invalidExclusion(e);
        if (token != null)
            message(ParserMessages.invalidExclusion,
                    new OrdinalMessageArg(token.typeIndex() + 1),
                    new StringMessageArg(token.elementType()!.name()),
                    new StringMessageArg(currentElement().type()!.name()));
    }

    // void handleBadStartTag(const ElementType *e, StartElementEvent *event, Boolean netEnabling);
    protected void handleBadStartTag(ElementType e, StartElementEvent @event, Boolean netEnabling)
    {
        IList<Undo> undoList = new IList<Undo>();
        IList<Event> eventList = new IList<Event>();
        keepMessages();
        for (;;)
        {
            Vector<ElementType?> missing = new Vector<ElementType?>();
            findMissingTag(e, missing);
            if (missing.size() == 1)
            {
                queueElementEvents(eventList);
                ElementType? m = missing[0];
                if (m != null)
                {
                    message(ParserMessages.missingElementInferred,
                            new StringMessageArg(e.name()),
                            new StringMessageArg(m.name()));
                    AttributeList? attributes = allocAttributeList(m.attributeDef(), 1);
                    // this will give an error if the element has a required attribute
                    attributes?.finish(this);
                    StartElementEvent inferEvent = new StartElementEvent(m,
                                                                         currentDtdPointer(),
                                                                         attributes,
                                                                         @event.location(),
                                                                         null);
                    if (!currentElement().tryTransition(m))
                        inferEvent.setIncluded();
                    pushElementCheck(m, inferEvent, false);
                    if (!currentElement().tryTransition(e))
                        @event.setIncluded();
                    pushElementCheck(e, @event, netEnabling);
                }
                return;
            }
            if (missing.size() > 0)
            {
                queueElementEvents(eventList);
                Vector<StringC> missingNames = new Vector<StringC>();
                for (nuint i = 0; i < missing.size(); i++)
                    if (missing[i] != null)
                        missingNames.push_back(missing[i]!.name());
                message(ParserMessages.missingElementMultiple,
                        new StringMessageArg(e.name()),
                        new StringVectorMessageArg(missingNames));
                pushElementCheck(e, @event, netEnabling);
                return;
            }
            if (!sd().omittag()
                || !currentElement().isFinished()
                || tagLevel() == 0
                || !currentElement().type()!.definition()!.canOmitEndTag())
                break;
            EndElementEvent endEvent = new EndElementEvent(currentElement().type(),
                                                           currentDtdPointer(),
                                                           @event.location(),
                                                           null);
            eventList.insert(endEvent);
            undoList.insert(new UndoEndTag(popSaveElement()));
        }
        discardKeptMessages();
        undo(undoList);
        message(ParserMessages.elementNotAllowed, new StringMessageArg(e.name()));
        // If element couldn't occur because it was excluded, then
        // do the transition here.
        currentElement().tryTransition(e);
        pushElementCheck(e, @event, netEnabling);
    }

    // void findMissingTag(const ElementType *e, Vector<const ElementType *> &v);
    protected void findMissingTag(ElementType? e, Vector<ElementType?> v)
    {
        if (currentElement().currentPosition() == null)
        {
            if (e == null)
                v.push_back(null);
            return;
        }
        if (elementIsExcluded(e))
            return;
        nuint newSize = 0;
        currentElement().matchState().possibleTransitions(v);
        // FIXME also get currentInclusions
        for (nuint i = 0; i < v.size(); i++)
        {
            if (v[i] != null && !elementIsExcluded(v[i]))
            {
                Boolean success = false;
                ElementDefinition? def = v[i]!.definition();
                if (def == null) continue;
                switch (def.declaredContent())
                {
                    case ElementDefinition.DeclaredContent.modelGroup:
                        {
                            CompiledModelGroup? grp = def.compiledModelGroup();
                            if (grp == null) continue;
                            MatchState state = new MatchState(grp);
                            if (e == null)
                            {
                                if (state.tryTransitionPcdata())
                                    success = true;
                            }
                            else
                            {
                                if (state.tryTransition(e))
                                    success = true;
                                if (!success)
                                {
                                    for (nuint j = 0; j < def.nInclusions(); j++)
                                        if (def.inclusion(j) == e)
                                        {
                                            success = true;
                                            break;
                                        }
                                }
                                if (success)
                                {
                                    for (nuint j = 0; j < def.nExclusions(); j++)
                                        if (def.exclusion(j) == e)
                                        {
                                            success = false;
                                            break;
                                        }
                                }
                            }
                        }
                        break;
                    case ElementDefinition.DeclaredContent.cdata:
                    case ElementDefinition.DeclaredContent.rcdata:
                        if (e == null)
                            success = true;
                        break;
                    default:
                        break;
                }
                if (success)
                    v[newSize++] = v[i];
            }
        }
        v.resize(newSize);
        // Sort them according to the order of their occurrence in the DTD.
        // Do an insertion sort.
        for (nuint i = 1; i < v.size(); i++)
        {
            ElementType? tem = v[i];
            nuint j;
            for (j = i; j > 0 && v[j - 1] != null && tem != null && v[j - 1]!.index() > tem.index(); j--)
                v[j] = v[j - 1];
            v[j] = tem;
        }
    }

    // ElementType *completeRankStem(const StringC &name);
    protected ElementType? completeRankStem(StringC name)
    {
        RankStem? rankStem = currentDtd().lookupRankStem(name);
        if (rankStem != null)
        {
            StringC fullName = new StringC(rankStem.name());
            if (!appendCurrentRank(fullName, rankStem))
                message(ParserMessages.noCurrentRank, new StringMessageArg(fullName));
            else
                return currentDtdNonConst().lookupElementType(fullName);
        }
        return null;
    }
}
