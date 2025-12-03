// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class TokenInfo
{
    public enum Type
    {
        delimType,
        setType,
        functionType,
        delimDelimType,
        delimSetType
    }

    public Type type;
    public byte priority;
    public Token token;
    public Syntax.DelimGeneral delim1;
    // Union members - only one is valid at a time depending on type
    public Syntax.DelimGeneral delim2;
    public Syntax.Set set;
    public Syntax.StandardFunction function;

    public TokenInfo()
    {
    }
}

public class PackedTokenInfo
{
    public Token token;         // token to be returned
    public uint flags;
    public byte[] contents = new byte[2];    // components of the delimiter or d-i-c
    public byte[] modes = new byte[25];      // list of modes in which it is recognized,
                                              // terminated by EOM
    // a bit vector computed from modes (lo to hi)
    private const int ULONG_BIT = 64;        // sizeof(ulong) * 8
    public ulong[] modeBits = new ulong[(ModeConstants.nModes + ULONG_BIT - 1) / ULONG_BIT];

    public PackedTokenInfo(Token tok, uint fl, byte c0, byte c1, byte[] modeList)
    {
        token = tok;
        flags = fl;
        contents[0] = c0;
        contents[1] = c1;
        Array.Copy(modeList, modes, Math.Min(modeList.Length, modes.Length));
        computeModeBits();
    }

    public void computeModeBits()
    {
        for (int i = 0; i < modes.Length && modes[i] != ModeInfo.EOM; i++)
        {
            int modeIndex = modes[i];
            modeBits[modeIndex / ULONG_BIT] |= 1UL << (modeIndex % ULONG_BIT);
        }
    }

    public Boolean inMode(Mode mode)
    {
        int m = (int)mode;
        return ((modeBits[m / ULONG_BIT] & (1UL << (m % ULONG_BIT))) != 0);
    }
}

public class ModeInfo
{
    public const uint REQUIRE_EMPTY_STARTTAG = 01;
    public const uint REQUIRE_EMPTY_ENDTAG = 02;
    public const uint REQUIRE_CONCUR = 04;
    public const uint REQUIRE_LINK_OR_CONCUR = 010;
    public const uint REQUIRE_NOT_KEEPRSRE = 020;
    public const uint REQUIRE_FLAGS = 037;

    public const byte SET = (byte)Syntax.nDelimGeneral;
    public const byte FUNCTION = (byte)(SET + Syntax.nSet);
    public const byte NOTHING = 255;
    public const byte EOM = 255;  // end of modes

    private Mode mode_;
    private int pIndex_;         // index into tokenTable
    private nuint count_;
    private uint missingRequirements_;

    private static PackedTokenInfo[]? tokenTable_;
    private static bool initialized_ = false;

    public ModeInfo(Mode mode, Sd sd)
    {
        if (!initialized_)
            initTokenTable();

        mode_ = mode;
        pIndex_ = 0;
        count_ = (nuint)tokenTable_!.Length;
        missingRequirements_ = REQUIRE_FLAGS;

        if (sd.startTagEmpty())
            missingRequirements_ &= unchecked((uint)~REQUIRE_EMPTY_STARTTAG);
        if (sd.endTagEmpty())
            missingRequirements_ &= unchecked((uint)~REQUIRE_EMPTY_ENDTAG);
        if (sd.concur() != 0)
            missingRequirements_ &= unchecked((uint)~(REQUIRE_CONCUR | REQUIRE_LINK_OR_CONCUR));
        if (sd.link())
            missingRequirements_ &= unchecked((uint)~REQUIRE_LINK_OR_CONCUR);
        if (!sd.keeprsre())
            missingRequirements_ &= unchecked((uint)~REQUIRE_NOT_KEEPRSRE);
    }

    public Boolean includesShortref()
    {
        return (int)mode_ >= ModeConstants.minShortrefMode;
    }

    public Boolean nextToken(TokenInfo t)
    {
        for (; count_ > 0; count_--, pIndex_++)
        {
            PackedTokenInfo p = tokenTable_![pIndex_];
            if (p.inMode(mode_) && (p.flags & missingRequirements_) == 0)
            {
                t.token = p.token;
                t.priority = Priority.delim;
                byte c = p.contents[0];
                count_--;
                pIndex_++;
                if (c < SET)
                {
                    t.delim1 = (Syntax.DelimGeneral)c;
                }
                else if (c < SET + Syntax.nSet)
                {
                    t.set = (Syntax.Set)(c - SET);
                    t.type = TokenInfo.Type.setType;
                    switch (t.set)
                    {
                        case Syntax.Set.sepchar:
                        case Syntax.Set.s:
                        case Syntax.Set.blank:
                            t.priority = Priority.function;
                            break;
                        default:
                            t.priority = Priority.data;
                            break;
                    }
                    return true;
                }
                else
                {
                    t.function = (Syntax.StandardFunction)(c - FUNCTION);
                    t.priority = Priority.function;
                    t.type = TokenInfo.Type.functionType;
                    return true;
                }
                c = p.contents[1];
                if (c == NOTHING)
                {
                    t.type = TokenInfo.Type.delimType;
                    return true;
                }
                if (c < SET)
                {
                    t.delim2 = (Syntax.DelimGeneral)c;
                    t.type = TokenInfo.Type.delimDelimType;
                    return true;
                }
                if (c < SET + Syntax.nSet)
                {
                    t.set = (Syntax.Set)(c - SET);
                    t.type = TokenInfo.Type.delimSetType;
                    return true;
                }
                throw new InvalidOperationException("Invalid token table entry");
            }
        }
        return false;
    }

    private static void initTokenTable()
    {
        tokenTable_ = new PackedTokenInfo[]
        {
            // Delimiters and delimiters in context
            new PackedTokenInfo(Tokens.tokenAnd, 0, (byte)Syntax.DelimGeneral.dAND, NOTHING, new byte[] { (byte)Mode.grpMode, EOM }),
            new PackedTokenInfo(Tokens.tokenCom, 0, (byte)Syntax.DelimGeneral.dCOM, NOTHING, new byte[] {
                (byte)Mode.mdMode, (byte)Mode.mdMinusMode, (byte)Mode.mdPeroMode, (byte)Mode.sdMode, (byte)Mode.comMode, (byte)Mode.sdcomMode, (byte)Mode.piPasMode, EOM }),
            new PackedTokenInfo(Tokens.tokenCroDigit, 0, (byte)Syntax.DelimGeneral.dCRO, (byte)(SET + (byte)Syntax.Set.digit), new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.rcconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.rcconnetMode,
                (byte)Mode.rcconeMode, (byte)Mode.plitMode, (byte)Mode.plitaMode, (byte)Mode.pliteMode, (byte)Mode.sdplitMode, (byte)Mode.sdplitaMode,
                (byte)Mode.alitMode, (byte)Mode.alitaMode, (byte)Mode.aliteMode,
                (byte)Mode.talitMode, (byte)Mode.talitaMode, (byte)Mode.taliteMode, (byte)Mode.rcmsMode, EOM }),
            new PackedTokenInfo(Tokens.tokenCroNameStart, 0, (byte)Syntax.DelimGeneral.dCRO, (byte)(SET + (byte)Syntax.Set.nameStart), new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.rcconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.rcconnetMode,
                (byte)Mode.rcconeMode, (byte)Mode.plitMode, (byte)Mode.plitaMode, (byte)Mode.pliteMode, (byte)Mode.sdplitMode, (byte)Mode.sdplitaMode,
                (byte)Mode.alitMode, (byte)Mode.alitaMode, (byte)Mode.aliteMode,
                (byte)Mode.talitMode, (byte)Mode.talitaMode, (byte)Mode.taliteMode, (byte)Mode.rcmsMode, EOM }),
            new PackedTokenInfo(Tokens.tokenDsc, 0, (byte)Syntax.DelimGeneral.dDSC, NOTHING, new byte[] { (byte)Mode.asMode, (byte)Mode.dsMode, EOM }),
            new PackedTokenInfo(Tokens.tokenDso, 0, (byte)Syntax.DelimGeneral.dDSO, NOTHING, new byte[] { (byte)Mode.mdMode, EOM }),
            new PackedTokenInfo(Tokens.tokenDtgc, 0, (byte)Syntax.DelimGeneral.dDTGC, NOTHING, new byte[] { (byte)Mode.grpMode, EOM }),
            new PackedTokenInfo(Tokens.tokenDtgo, 0, (byte)Syntax.DelimGeneral.dDTGO, NOTHING, new byte[] { (byte)Mode.grpMode, EOM }),
            new PackedTokenInfo(Tokens.tokenEroNameStart, 0, (byte)Syntax.DelimGeneral.dERO, (byte)(SET + (byte)Syntax.Set.nameStart), new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.rcconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.rcconnetMode,
                (byte)Mode.rcconeMode, (byte)Mode.alitMode, (byte)Mode.alitaMode, (byte)Mode.aliteMode, (byte)Mode.talitMode, (byte)Mode.talitaMode,
                (byte)Mode.taliteMode, (byte)Mode.rcmsMode, EOM }),
            new PackedTokenInfo(Tokens.tokenEroGrpo, REQUIRE_LINK_OR_CONCUR, (byte)Syntax.DelimGeneral.dERO, (byte)Syntax.DelimGeneral.dGRPO, new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.rcconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.rcconnetMode,
                (byte)Mode.rcconeMode, (byte)Mode.alitMode, (byte)Mode.alitaMode, (byte)Mode.aliteMode, (byte)Mode.talitMode, (byte)Mode.talitaMode,
                (byte)Mode.taliteMode, (byte)Mode.rcmsMode, EOM }),
            new PackedTokenInfo(Tokens.tokenEtago, 0, (byte)Syntax.DelimGeneral.dETAGO, NOTHING, new byte[] { (byte)Mode.tagMode, EOM }),
            new PackedTokenInfo(Tokens.tokenEtagoNameStart, 0, (byte)Syntax.DelimGeneral.dETAGO, (byte)(SET + (byte)Syntax.Set.nameStart), new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.cconMode, (byte)Mode.rcconMode,
                (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.cconnetMode, (byte)Mode.rcconnetMode, EOM }),
            new PackedTokenInfo(Tokens.tokenEtagoTagc, REQUIRE_EMPTY_ENDTAG, (byte)Syntax.DelimGeneral.dETAGO, (byte)Syntax.DelimGeneral.dTAGC, new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.cconMode, (byte)Mode.rcconMode,
                (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.cconnetMode, (byte)Mode.rcconnetMode, EOM }),
            new PackedTokenInfo(Tokens.tokenEtagoGrpo, REQUIRE_CONCUR, (byte)Syntax.DelimGeneral.dETAGO, (byte)Syntax.DelimGeneral.dGRPO, new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.cconMode, (byte)Mode.rcconMode,
                (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.cconnetMode, (byte)Mode.rcconnetMode, EOM }),
            new PackedTokenInfo(Tokens.tokenGrpc, 0, (byte)Syntax.DelimGeneral.dGRPC, NOTHING, new byte[] { (byte)Mode.grpMode, EOM }),
            new PackedTokenInfo(Tokens.tokenGrpo, 0, (byte)Syntax.DelimGeneral.dGRPO, NOTHING, new byte[] { (byte)Mode.mdMode, (byte)Mode.mdMinusMode, (byte)Mode.grpMode, EOM }),
            new PackedTokenInfo(Tokens.tokenHcroHexDigit, 0, (byte)Syntax.DelimGeneral.dHCRO, (byte)(SET + (byte)Syntax.Set.hexDigit), new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.rcconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.rcconnetMode,
                (byte)Mode.rcconeMode, (byte)Mode.plitMode, (byte)Mode.plitaMode, (byte)Mode.pliteMode,
                (byte)Mode.alitMode, (byte)Mode.alitaMode, (byte)Mode.aliteMode,
                (byte)Mode.talitMode, (byte)Mode.talitaMode, (byte)Mode.taliteMode, (byte)Mode.rcmsMode, EOM }),
            new PackedTokenInfo(Tokens.tokenLit, 0, (byte)Syntax.DelimGeneral.dLIT, NOTHING, new byte[] {
                (byte)Mode.alitMode, (byte)Mode.talitMode, (byte)Mode.plitMode, (byte)Mode.sdplitMode, (byte)Mode.mlitMode, (byte)Mode.slitMode, (byte)Mode.sdslitMode,
                (byte)Mode.asMode, (byte)Mode.piPasMode, (byte)Mode.tagMode, (byte)Mode.mdMode, (byte)Mode.sdMode, (byte)Mode.grpMode, EOM }),
            new PackedTokenInfo(Tokens.tokenLita, 0, (byte)Syntax.DelimGeneral.dLITA, NOTHING, new byte[] {
                (byte)Mode.alitaMode, (byte)Mode.talitaMode, (byte)Mode.plitaMode, (byte)Mode.sdplitaMode, (byte)Mode.mlitaMode, (byte)Mode.slitaMode, (byte)Mode.sdslitaMode,
                (byte)Mode.asMode, (byte)Mode.piPasMode, (byte)Mode.tagMode, (byte)Mode.mdMode, (byte)Mode.sdMode, (byte)Mode.grpMode, EOM }),
            new PackedTokenInfo(Tokens.tokenMdc, 0, (byte)Syntax.DelimGeneral.dMDC, NOTHING, new byte[] { (byte)Mode.mdMode, (byte)Mode.sdMode, EOM }),
            new PackedTokenInfo(Tokens.tokenMdoNameStart, 0, (byte)Syntax.DelimGeneral.dMDO, (byte)(SET + (byte)Syntax.Set.nameStart), new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode,
                (byte)Mode.proMode, (byte)Mode.dsMode, (byte)Mode.dsiMode, EOM }),
            new PackedTokenInfo(Tokens.tokenMdoMdc, 0, (byte)Syntax.DelimGeneral.dMDO, (byte)Syntax.DelimGeneral.dMDC, new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode,
                (byte)Mode.proMode, (byte)Mode.dsMode, (byte)Mode.dsiMode, EOM }),
            new PackedTokenInfo(Tokens.tokenMdoCom, 0, (byte)Syntax.DelimGeneral.dMDO, (byte)Syntax.DelimGeneral.dCOM, new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode,
                (byte)Mode.proMode, (byte)Mode.dsMode, (byte)Mode.dsiMode, EOM }),
            new PackedTokenInfo(Tokens.tokenMdoDso, 0, (byte)Syntax.DelimGeneral.dMDO, (byte)Syntax.DelimGeneral.dDSO, new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode,
                (byte)Mode.dsMode, (byte)Mode.dsiMode, (byte)Mode.imsMode, EOM }),
            new PackedTokenInfo(Tokens.tokenMinus, 0, (byte)Syntax.DelimGeneral.dMINUS, NOTHING, new byte[] { (byte)Mode.mdMinusMode, (byte)Mode.sdMode, EOM }),
            new PackedTokenInfo(Tokens.tokenMinusGrpo, 0, (byte)Syntax.DelimGeneral.dMINUS, (byte)Syntax.DelimGeneral.dGRPO, new byte[] { (byte)Mode.mdMode, EOM }),
            new PackedTokenInfo(Tokens.tokenMscMdc, 0, (byte)Syntax.DelimGeneral.dMSC, (byte)Syntax.DelimGeneral.dMDC, new byte[] {
                (byte)Mode.imsMode, (byte)Mode.cmsMode, (byte)Mode.rcmsMode,
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.dsMode, (byte)Mode.dsiMode, EOM }),
            new PackedTokenInfo(Tokens.tokenNestc, 0, (byte)Syntax.DelimGeneral.dNESTC, NOTHING, new byte[] { (byte)Mode.tagMode, EOM }),
            new PackedTokenInfo(Tokens.tokenNet, 0, (byte)Syntax.DelimGeneral.dNET, NOTHING, new byte[] {
                (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.cconnetMode, (byte)Mode.rcconnetMode, EOM }),
            new PackedTokenInfo(Tokens.tokenOpt, 0, (byte)Syntax.DelimGeneral.dOPT, NOTHING, new byte[] { (byte)Mode.grpMode, (byte)Mode.grpsufMode, EOM }),
            new PackedTokenInfo(Tokens.tokenOr, 0, (byte)Syntax.DelimGeneral.dOR, NOTHING, new byte[] { (byte)Mode.grpMode, EOM }),
            new PackedTokenInfo(Tokens.tokenPero, 0, (byte)Syntax.DelimGeneral.dPERO, NOTHING, new byte[] { (byte)Mode.mdPeroMode, EOM }),
            new PackedTokenInfo(Tokens.tokenPeroNameStart, 0, (byte)Syntax.DelimGeneral.dPERO, (byte)(SET + (byte)Syntax.Set.nameStart), new byte[] {
                (byte)Mode.mdMode, (byte)Mode.mdMinusMode, (byte)Mode.mdPeroMode, (byte)Mode.dsMode, (byte)Mode.dsiMode, (byte)Mode.grpMode,
                (byte)Mode.plitMode, (byte)Mode.plitaMode, (byte)Mode.pliteMode, (byte)Mode.sdplitMode, (byte)Mode.sdplitaMode, EOM }),
            new PackedTokenInfo(Tokens.tokenPeroGrpo, REQUIRE_LINK_OR_CONCUR, (byte)Syntax.DelimGeneral.dPERO, (byte)Syntax.DelimGeneral.dGRPO, new byte[] {
                (byte)Mode.mdMode, (byte)Mode.mdMinusMode, (byte)Mode.mdPeroMode, (byte)Mode.dsMode, (byte)Mode.dsiMode, (byte)Mode.grpMode,
                (byte)Mode.plitMode, (byte)Mode.plitaMode, (byte)Mode.pliteMode, (byte)Mode.sdplitMode, (byte)Mode.sdplitaMode, EOM }),
            new PackedTokenInfo(Tokens.tokenPic, 0, (byte)Syntax.DelimGeneral.dPIC, NOTHING, new byte[] { (byte)Mode.piMode, EOM }),
            new PackedTokenInfo(Tokens.tokenPio, 0, (byte)Syntax.DelimGeneral.dPIO, NOTHING, new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.proMode,
                (byte)Mode.dsMode, (byte)Mode.dsiMode, EOM }),
            new PackedTokenInfo(Tokens.tokenPlus, 0, (byte)Syntax.DelimGeneral.dPLUS, NOTHING, new byte[] { (byte)Mode.grpMode, (byte)Mode.grpsufMode, EOM }),
            new PackedTokenInfo(Tokens.tokenPlusGrpo, 0, (byte)Syntax.DelimGeneral.dPLUS, (byte)Syntax.DelimGeneral.dGRPO, new byte[] { (byte)Mode.mdMode, EOM }),
            new PackedTokenInfo(Tokens.tokenRefc, 0, (byte)Syntax.DelimGeneral.dREFC, NOTHING, new byte[] { (byte)Mode.refMode, EOM }),
            new PackedTokenInfo(Tokens.tokenRep, 0, (byte)Syntax.DelimGeneral.dREP, NOTHING, new byte[] { (byte)Mode.grpMode, (byte)Mode.grpsufMode, EOM }),
            new PackedTokenInfo(Tokens.tokenRni, 0, (byte)Syntax.DelimGeneral.dRNI, NOTHING, new byte[] { (byte)Mode.grpMode, (byte)Mode.mdMode, (byte)Mode.mdPeroMode, EOM }),
            new PackedTokenInfo(Tokens.tokenSeq, 0, (byte)Syntax.DelimGeneral.dSEQ, NOTHING, new byte[] { (byte)Mode.grpMode, EOM }),
            new PackedTokenInfo(Tokens.tokenStago, 0, (byte)Syntax.DelimGeneral.dSTAGO, NOTHING, new byte[] { (byte)Mode.tagMode, EOM }),
            new PackedTokenInfo(Tokens.tokenStagoNameStart, 0, (byte)Syntax.DelimGeneral.dSTAGO, (byte)(SET + (byte)Syntax.Set.nameStart), new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode, EOM }),
            new PackedTokenInfo(Tokens.tokenStagoTagc, REQUIRE_EMPTY_STARTTAG, (byte)Syntax.DelimGeneral.dSTAGO, (byte)Syntax.DelimGeneral.dTAGC, new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode, EOM }),
            new PackedTokenInfo(Tokens.tokenStagoGrpo, REQUIRE_CONCUR, (byte)Syntax.DelimGeneral.dSTAGO, (byte)Syntax.DelimGeneral.dGRPO, new byte[] {
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.econnetMode, (byte)Mode.mconnetMode, EOM }),
            new PackedTokenInfo(Tokens.tokenTagc, 0, (byte)Syntax.DelimGeneral.dTAGC, NOTHING, new byte[] { (byte)Mode.tagMode, EOM }),
            new PackedTokenInfo(Tokens.tokenVi, 0, (byte)Syntax.DelimGeneral.dVI, NOTHING, new byte[] { (byte)Mode.tagMode, (byte)Mode.asMode, (byte)Mode.piPasMode, EOM }),
            // Other tokens
            new PackedTokenInfo(Tokens.tokenRe, REQUIRE_NOT_KEEPRSRE, (byte)(FUNCTION + (byte)Syntax.StandardFunction.fRE), NOTHING, new byte[] {
                (byte)Mode.mconMode, (byte)Mode.cconMode, (byte)Mode.rcconMode,
                (byte)Mode.mconnetMode, (byte)Mode.cconnetMode, (byte)Mode.rcconnetMode,
                (byte)Mode.rcconeMode, (byte)Mode.cmsMode, (byte)Mode.rcmsMode, EOM }),
            new PackedTokenInfo(Tokens.tokenRe, 0, (byte)(FUNCTION + (byte)Syntax.StandardFunction.fRE), NOTHING, new byte[] {
                (byte)Mode.refMode,
                (byte)Mode.mlitMode, (byte)Mode.mlitaMode, (byte)Mode.alitMode, (byte)Mode.alitaMode, (byte)Mode.aliteMode,
                (byte)Mode.talitMode, (byte)Mode.talitaMode, (byte)Mode.taliteMode, EOM }),
            new PackedTokenInfo(Tokens.tokenRs, REQUIRE_NOT_KEEPRSRE, (byte)(FUNCTION + (byte)Syntax.StandardFunction.fRS), NOTHING, new byte[] {
                (byte)Mode.mconMode, (byte)Mode.cconMode, (byte)Mode.rcconMode,
                (byte)Mode.mconnetMode, (byte)Mode.cconnetMode, (byte)Mode.rcconnetMode,
                (byte)Mode.rcconeMode, (byte)Mode.cmsMode, (byte)Mode.rcmsMode, EOM }),
            new PackedTokenInfo(Tokens.tokenRs, 0, (byte)(FUNCTION + (byte)Syntax.StandardFunction.fRS), NOTHING, new byte[] {
                (byte)Mode.mlitMode, (byte)Mode.mlitaMode, (byte)Mode.alitMode, (byte)Mode.alitaMode, (byte)Mode.aliteMode,
                (byte)Mode.talitMode, (byte)Mode.talitaMode, (byte)Mode.taliteMode, EOM }),
            new PackedTokenInfo(Tokens.tokenSpace, 0, (byte)(FUNCTION + (byte)Syntax.StandardFunction.fSPACE), NOTHING, new byte[] {
                (byte)Mode.mlitMode, (byte)Mode.mlitaMode, (byte)Mode.talitMode, (byte)Mode.talitaMode, (byte)Mode.taliteMode, EOM }),
            new PackedTokenInfo(Tokens.tokenSepchar, 0, (byte)(SET + (byte)Syntax.Set.sepchar), NOTHING, new byte[] {
                (byte)Mode.alitMode, (byte)Mode.alitaMode, (byte)Mode.aliteMode,
                (byte)Mode.talitMode, (byte)Mode.talitaMode, (byte)Mode.taliteMode, EOM }),
            new PackedTokenInfo(Tokens.tokenS, 0, (byte)(SET + (byte)Syntax.Set.s), NOTHING, new byte[] {
                (byte)Mode.econMode, (byte)Mode.econnetMode, (byte)Mode.grpMode, (byte)Mode.mdMode, (byte)Mode.mdMinusMode, (byte)Mode.mdPeroMode, (byte)Mode.sdMode,
                (byte)Mode.proMode, (byte)Mode.dsMode, (byte)Mode.dsiMode, (byte)Mode.asMode, (byte)Mode.piPasMode, (byte)Mode.tagMode, EOM }),
            new PackedTokenInfo(Tokens.tokenNameStart, 0, (byte)(SET + (byte)Syntax.Set.nameStart), NOTHING, new byte[] {
                (byte)Mode.grpMode, (byte)Mode.mdMode, (byte)Mode.mdMinusMode, (byte)Mode.mdPeroMode, (byte)Mode.sdMode,
                (byte)Mode.asMode, (byte)Mode.piPasMode, (byte)Mode.tagMode, EOM }),
            new PackedTokenInfo(Tokens.tokenDigit, 0, (byte)(SET + (byte)Syntax.Set.digit), NOTHING, new byte[] {
                (byte)Mode.grpMode, (byte)Mode.mdMode, (byte)Mode.mdMinusMode, (byte)Mode.sdMode, (byte)Mode.asMode, (byte)Mode.piPasMode, (byte)Mode.tagMode, EOM }),
            new PackedTokenInfo(Tokens.tokenLcUcNmchar, 0, (byte)(SET + (byte)Syntax.Set.nmchar), NOTHING, new byte[] {
                (byte)Mode.grpMode, (byte)Mode.mdMode, (byte)Mode.asMode, (byte)Mode.piPasMode, (byte)Mode.tagMode, EOM }),
            new PackedTokenInfo(Tokens.tokenIgnoredChar, 0, (byte)(SET + (byte)Syntax.Set.sgmlChar), NOTHING, new byte[] { (byte)Mode.imsMode, EOM }),
            new PackedTokenInfo(Tokens.tokenChar, 0, (byte)(SET + (byte)Syntax.Set.sgmlChar), NOTHING, new byte[] {
                // Note that character data is recognized in element content,
                // and will cause #PCDATA to begin.
                (byte)Mode.alitMode, (byte)Mode.alitaMode, (byte)Mode.aliteMode,
                (byte)Mode.talitMode, (byte)Mode.talitaMode, (byte)Mode.taliteMode,
                (byte)Mode.comMode, (byte)Mode.piMode,
                (byte)Mode.cmsMode, (byte)Mode.rcmsMode,
                (byte)Mode.plitMode, (byte)Mode.plitaMode, (byte)Mode.pliteMode,
                (byte)Mode.slitMode, (byte)Mode.slitaMode,
                (byte)Mode.econMode, (byte)Mode.mconMode, (byte)Mode.cconMode, (byte)Mode.rcconMode,
                (byte)Mode.econnetMode, (byte)Mode.mconnetMode, (byte)Mode.cconnetMode, (byte)Mode.rcconnetMode, (byte)Mode.rcconeMode, EOM }),
            new PackedTokenInfo(Tokens.tokenChar, 0, (byte)(SET + (byte)Syntax.Set.minimumData), NOTHING, new byte[] {
                (byte)Mode.mlitMode, (byte)Mode.mlitaMode, EOM }),
            new PackedTokenInfo(Tokens.tokenChar, 0, (byte)(SET + (byte)Syntax.Set.significant), NOTHING, new byte[] {
                (byte)Mode.sdplitMode, (byte)Mode.sdplitaMode, (byte)Mode.sdslitMode, (byte)Mode.sdslitaMode, (byte)Mode.sdcomMode, EOM }),
        };
        initialized_ = true;
    }
}
