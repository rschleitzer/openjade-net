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
        // Note: This is a simplified version of the token table.
        // The full table is very large and would need to be fully ported.
        // For now, we create an empty table that can be expanded.
        tokenTable_ = Array.Empty<PackedTokenInfo>();
        initialized_ = true;

        // TODO: Port the full token table from ModeInfo.cxx
        // The full table contains all delimiter and character set
        // recognition rules for SGML parsing modes.
    }
}
