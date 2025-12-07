// Copyright (c) 1994, 1997 James Clark, 2000 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

class XMLDecoder : Decoder
{
    private const Char ISO646_TAB = 0x9;
    private const Char ISO646_LF = 0xA;
    private const Char ISO646_CR = 0xD;
    private const Char ISO646_SPACE = 0x20;
    private const Char ISO646_QUOT = 0x22;
    private const Char ISO646_APOS = 0x27;
    private const Char ISO646_LT = 0x3C;
    private const Char ISO646_EQUAL = 0x3D;
    private const Char ISO646_GT = 0x3E;
    private const Char ISO646_QUEST = 0x3F;
    private const Char ISO646_LETTER_a = 0x61;
    private const Char ISO646_LETTER_c = 0x63;
    private const Char ISO646_LETTER_d = 0x64;
    private const Char ISO646_LETTER_e = 0x65;
    private const Char ISO646_LETTER_g = 0x67;
    private const Char ISO646_LETTER_i = 0x69;
    private const Char ISO646_LETTER_l = 0x6C;
    private const Char ISO646_LETTER_m = 0x6D;
    private const Char ISO646_LETTER_n = 0x6E;
    private const Char ISO646_LETTER_o = 0x6F;
    private const Char ISO646_LETTER_x = 0x78;

    // Don't keep parsing a PI longer than this.
    // We want to avoid reading some enormous file into memory just because
    // some quote was left off.
    private const int piMaxSize = 1024 * 32;

    private enum DetectPhase
    {
        phaseInit,
        phasePI,
        phaseFinish
    }

    private DetectPhase phase_;
    private Boolean byteOrderMark_;
    private Boolean lsbFirst_;
    private Boolean lswFirst_;
    private int guessBytesPerChar_;
    private Decoder? subDecoder_;
    // Contains all the characters passed to caller that were
    // not produced by subDecoder_.
    private StringC pi_ = new StringC();
    private Char piLiteral_;
    private InputCodingSystemKit kit_;

    // XMLDecoder(const InputCodingSystemKit *);
    public XMLDecoder(InputCodingSystemKit kit) : base(1)
    {
        kit_ = kit;
        phase_ = DetectPhase.phaseInit;
        byteOrderMark_ = false;
        lsbFirst_ = false;
        lswFirst_ = false;
        guessBytesPerChar_ = 1;
        piLiteral_ = 0;
    }

    // size_t decode(Char *to, const char *from, size_t fromLen, const char **rest);
    public override nuint decode(Char[] to, byte[] from, nuint fromLen, out nuint fromUsed)
    {
        nuint toIndex = 0;
        nuint fromIndex = 0;

        if (phase_ == DetectPhase.phaseFinish)
        {
            return subDecoder_!.decode(to, from, fromLen, out fromUsed);
        }

        if (phase_ == DetectPhase.phaseInit)
        {
            if (fromLen == 0)
            {
                fromUsed = 0;
                return 0;
            }

            switch (from[0])
            {
                case 0x00:
                case 0x3C:
                case 0xFF:
                case 0xFE:
                    if (fromLen < 2)
                    {
                        fromUsed = 0;
                        return 0;
                    }
                    uint twoBytes = ((uint)from[0] << 8) | from[1];
                    switch (twoBytes)
                    {
                        case 0xFEFF:
                            phase_ = DetectPhase.phasePI;
                            byteOrderMark_ = true;
                            guessBytesPerChar_ = 2;
                            fromIndex = 2;
                            fromLen -= 2;
                            break;
                        case 0xFFFE:
                            lsbFirst_ = true;
                            phase_ = DetectPhase.phasePI;
                            byteOrderMark_ = true;
                            guessBytesPerChar_ = 2;
                            fromIndex = 2;
                            fromLen -= 2;
                            break;
                        case 0x3C3F:
                            phase_ = DetectPhase.phasePI;
                            break;
                        case 0x0000:
                        case 0x3C00:
                        case 0x003C:
                            if (fromLen < 4)
                            {
                                fromUsed = 0;
                                return 0;
                            }
                            uint fourBytes = ((uint)from[0] << 24)
                                           | ((uint)from[1] << 16)
                                           | ((uint)from[2] << 8)
                                           | from[3];
                            switch (fourBytes)
                            {
                                case 0x0000003C:
                                    lsbFirst_ = false;
                                    lswFirst_ = false;
                                    phase_ = DetectPhase.phasePI;
                                    guessBytesPerChar_ = 4;
                                    break;
                                case 0x00003C00:
                                    lsbFirst_ = true;
                                    lswFirst_ = false;
                                    phase_ = DetectPhase.phasePI;
                                    guessBytesPerChar_ = 4;
                                    break;
                                case 0x003C0000:
                                    lsbFirst_ = false;
                                    lswFirst_ = true;
                                    phase_ = DetectPhase.phasePI;
                                    guessBytesPerChar_ = 4;
                                    break;
                                case 0x3C000000:
                                    lsbFirst_ = true;
                                    lswFirst_ = true;
                                    phase_ = DetectPhase.phasePI;
                                    guessBytesPerChar_ = 4;
                                    break;
                                case 0x003C003F:
                                    lsbFirst_ = true;
                                    phase_ = DetectPhase.phasePI;
                                    guessBytesPerChar_ = 2;
                                    break;
                                case 0x3C003F00:
                                    lsbFirst_ = false;
                                    phase_ = DetectPhase.phasePI;
                                    guessBytesPerChar_ = 2;
                                    break;
                                default:
                                    break;
                            }
                            if (phase_ == DetectPhase.phasePI)
                                break;
                            goto default;
                        default:
                            break;
                    }
                    if (phase_ == DetectPhase.phasePI)
                        break;
                    goto default;
                default:
                    phase_ = DetectPhase.phaseFinish;
                    guessBytesPerChar_ = 1;
                    initDecoderDefault();
                    return subDecoder_!.decode(to, from, fromLen, out fromUsed);
            }
        }

        System.Diagnostics.Debug.Assert(phase_ == DetectPhase.phasePI);

        Char[] startBytes = { ISO646_LT, ISO646_QUEST, ISO646_LETTER_x, ISO646_LETTER_m, ISO646_LETTER_l };

        while (fromLen > (nuint)guessBytesPerChar_)
        {
            if (piLiteral_ == 0 && pi_.size() > 0 && pi_[pi_.size() - 1] == ISO646_GT)
            {
                initDecoderPI();
                phase_ = DetectPhase.phaseFinish;
                // Create remaining array
                byte[] remaining = new byte[fromLen];
                for (nuint i = 0; i < fromLen; i++)
                    remaining[i] = from[fromIndex + i];
                // Decode into a temporary buffer, then copy to correct position
                Char[] temp = new Char[(nuint)to.Length - toIndex];
                nuint subUsed;
                nuint subDecoded = subDecoder_!.decode(temp, remaining, fromLen, out subUsed);
                // Copy decoded characters to correct position in output
                for (nuint i = 0; i < subDecoded && toIndex + i < (nuint)to.Length; i++)
                    to[toIndex + i] = temp[i];
                fromUsed = fromIndex + subUsed;
                return toIndex + subDecoded;
            }

            Char c;
            switch (guessBytesPerChar_)
            {
                case 1:
                    c = from[fromIndex];
                    break;
                case 2:
                    c = lsbFirst_
                        ? (Char)(((uint)from[fromIndex + 1] << 8) | from[fromIndex])
                        : (Char)(((uint)from[fromIndex] << 8) | from[fromIndex + 1]);
                    break;
                case 4:
                    {
                        int shift0 = 8 * ((!lsbFirst_ ? 1 : 0) + 2 * (!lswFirst_ ? 1 : 0));
                        int shift1 = 8 * ((lsbFirst_ ? 1 : 0) + 2 * (!lswFirst_ ? 1 : 0));
                        int shift2 = 8 * ((!lsbFirst_ ? 1 : 0) + 2 * (lswFirst_ ? 1 : 0));
                        int shift3 = 8 * ((lsbFirst_ ? 1 : 0) + 2 * (lswFirst_ ? 1 : 0));
                        c = (Char)(((uint)from[fromIndex] << shift0)
                                 | ((uint)from[fromIndex + 1] << shift1)
                                 | ((uint)from[fromIndex + 2] << shift2)
                                 | ((uint)from[fromIndex + 3] << shift3));
                    }
                    break;
                default:
                    throw new System.InvalidOperationException("Invalid guessBytesPerChar_");
            }

            // Stop accumulating the PI if we get characters that are illegal in the PI.
            if (c == 0
                || c >= 0x7F
                || (pi_.size() > 0 && c == ISO646_LT)
                || pi_.size() > piMaxSize
                || (pi_.size() < 5 && c != startBytes[pi_.size()])
                || (pi_.size() == 5 && !isWS(c)))
            {
                initDecoderDefault();
                phase_ = DetectPhase.phaseFinish;
                break;
            }

            to[toIndex++] = c;
            pi_.operatorPlusAssign(c);

            if (piLiteral_ != 0)
            {
                if (c == piLiteral_)
                    piLiteral_ = 0;
            }
            else if (c == ISO646_QUOT || c == ISO646_APOS)
            {
                piLiteral_ = c;
            }

            fromIndex += (nuint)guessBytesPerChar_;
            fromLen -= (nuint)guessBytesPerChar_;
        }

        if (phase_ == DetectPhase.phaseFinish && fromLen > 0)
        {
            // Create remaining array
            byte[] remaining = new byte[fromLen];
            for (nuint i = 0; i < fromLen; i++)
                remaining[i] = from[fromIndex + i];
            // Decode into a temporary buffer, then copy to correct position
            Char[] temp = new Char[(nuint)to.Length - toIndex];
            nuint subUsed;
            nuint n = subDecoder_!.decode(temp, remaining, fromLen, out subUsed);
            // Copy decoded characters to correct position in output
            for (nuint i = 0; i < n && toIndex + i < (nuint)to.Length; i++)
                to[toIndex + i] = temp[i];
            fromUsed = fromIndex + subUsed;
            return toIndex + n;
        }

        fromUsed = fromIndex;
        return toIndex;
    }

    // Boolean convertOffset(unsigned long &n) const;
    public override Boolean convertOffset(ref ulong n)
    {
        if (n <= (ulong)pi_.size())
        {
            n *= (ulong)guessBytesPerChar_;
        }
        else
        {
            if (subDecoder_ == null)
                return false;
            ulong tem = n - (ulong)pi_.size();
            if (!subDecoder_.convertOffset(ref tem))
                return false;
            n = tem + (ulong)pi_.size() * (ulong)guessBytesPerChar_;
        }
        if (byteOrderMark_)
            n += 2;
        return true;
    }

    private void initDecoderDefault()
    {
        switch (guessBytesPerChar_)
        {
            case 1:
                {
                    UTF8CodingSystem utf8 = new UTF8CodingSystem();
                    subDecoder_ = utf8.makeDecoder();
                }
                break;
            case 2:
                {
                    UTF16CodingSystem utf16 = new UTF16CodingSystem();
                    subDecoder_ = utf16.makeDecoder(lsbFirst_);
                }
                break;
            case 4:
                {
                    Fixed4CodingSystem utf32 = new Fixed4CodingSystem();
                    subDecoder_ = utf32.makeDecoder(lsbFirst_, lswFirst_);
                }
                break;
            default:
                throw new System.InvalidOperationException("Invalid guessBytesPerChar_");
        }
        minBytesPerChar_ = subDecoder_.minBytesPerChar();
    }

    private void initDecoderPI()
    {
        StringC name = new StringC();
        if (!extractEncoding(name))
        {
            initDecoderDefault();
            return;
        }

        UnivCharsetDesc.Range range = new UnivCharsetDesc.Range { descMin = 0, count = 128, univMin = 0 };
        UnivCharsetDesc.Range[] ranges = { range };
        CharsetInfo piCharset = new CharsetInfo(new UnivCharsetDesc(ranges, 1));

        string? dummy;
        InputCodingSystem? ics = kit_.makeInputCodingSystem(name, piCharset, false, out dummy);

        if (ics != null)
        {
            subDecoder_ = ics.makeDecoder(lsbFirst_, lswFirst_);
            minBytesPerChar_ = subDecoder_.minBytesPerChar();
        }

        if (subDecoder_ == null)
            initDecoderDefault();
    }

    private static Boolean isWS(Char c)
    {
        switch (c)
        {
            case ISO646_CR:
            case ISO646_LF:
            case ISO646_SPACE:
            case ISO646_TAB:
                return true;
        }
        return false;
    }

    private Boolean extractEncoding(StringC name)
    {
        Char lit = 0;
        for (nuint i = 5; i < pi_.size(); i++)
        {
            if (lit == 0)
            {
                if (pi_[i] == ISO646_APOS || pi_[i] == ISO646_QUOT)
                {
                    lit = pi_[i];
                }
                else if (pi_[i] == ISO646_EQUAL)
                {
                    nuint j = i;
                    for (; j > 0; j--)
                    {
                        if (!isWS(pi_[j - 1]))
                            break;
                    }
                    nuint nameEnd = j;
                    for (; j > 0; j--)
                    {
                        if (isWS(pi_[j - 1]) || pi_[j - 1] == ISO646_QUOT || pi_[j - 1] == ISO646_APOS)
                            break;
                    }
                    Char[] encodingName = { ISO646_LETTER_e, ISO646_LETTER_n, ISO646_LETTER_c, ISO646_LETTER_o,
                                            ISO646_LETTER_d, ISO646_LETTER_i, ISO646_LETTER_n, ISO646_LETTER_g };
                    nuint sIdx = 0;
                    for (; sIdx < (nuint)encodingName.Length && j < nameEnd; j++, sIdx++)
                    {
                        if (pi_[j] != encodingName[sIdx])
                            break;
                    }
                    if (j == nameEnd && sIdx == (nuint)encodingName.Length)
                    {
                        j = i + 1;
                        for (; j < pi_.size(); j++)
                        {
                            if (!isWS(pi_[j]))
                                break;
                        }
                        if (pi_[j] == ISO646_QUOT || pi_[j] == ISO646_APOS)
                        {
                            Char litChar = pi_[j];
                            nuint nameStart = j + 1;
                            for (j++; j < pi_.size(); j++)
                            {
                                if (pi_[j] == litChar)
                                {
                                    if (j > nameStart)
                                    {
                                        // Extract name
                                        name.resize(0);
                                        for (nuint k = nameStart; k < j; k++)
                                            name.operatorPlusAssign(pi_[k]);
                                        return true;
                                    }
                                    break;
                                }
                            }
                        }
                        return false;
                    }
                }
            }
            else if (pi_[i] == lit)
            {
                lit = 0;
            }
        }
        return false;
    }
}

public class XMLCodingSystem : CodingSystem
{
    private InputCodingSystemKit kit_;

    // XMLCodingSystem(const InputCodingSystemKit *);
    public XMLCodingSystem(InputCodingSystemKit kit)
    {
        kit_ = kit;
    }

    // Decoder *makeDecoder() const;
    public override Decoder makeDecoder()
    {
        return new XMLDecoder(kit_);
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        UTF8CodingSystem utf8 = new UTF8CodingSystem();
        return utf8.makeEncoder();
    }
}
