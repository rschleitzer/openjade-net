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
