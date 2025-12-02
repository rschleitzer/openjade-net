// Copyright (c) 1997 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class CharsetRegistry
{
    // class SP_API Iter
    public abstract class Iter
    {
        // virtual ~Iter();
        // C# GC handles cleanup

        // virtual Boolean next(WideChar &min, WideChar &max, UnivChar &) = 0;
        public abstract Boolean next(out WideChar min, out WideChar max, out UnivChar univ);
    }

    // enum ISORegistrationNumber
    public enum ISORegistrationNumber
    {
        UNREGISTERED = 0,
        ISO646_ASCII_G0 = 6,
        ISO646_C0 = 1,
        ISO6429 = 77,
        ISO8859_1 = 100,
        ISO8859_2 = 101,
        ISO8859_3 = 109,
        ISO8859_4 = 110,
        ISO8859_5 = 144,
        ISO8859_6 = 127,
        ISO8859_7 = 126,
        ISO8859_8 = 138,
        ISO8859_9 = 148,
        ISO646_JIS_G0 = 14,
        JIS0201 = 13,
        JIS0208 = 168,
        JIS0212 = 159,
        KSC5601 = 149,
        GB2312 = 58,
        ISO10646_UCS2 = 176,
        ISO10646_UCS4 = 177,
        KOI8_R = 65534, // not registered
        BIG5 = 65535 // not registered
    }

    // Range iterator for simple ranges
    private class RangeIter : Iter
    {
        private UnivCharsetDesc.Range[] ranges_;
        private nuint index_;
        private nuint count_;

        public RangeIter(UnivCharsetDesc.Range[] ranges, nuint count)
        {
            ranges_ = ranges;
            count_ = count;
            index_ = 0;
        }

        public override Boolean next(out WideChar min, out WideChar max, out UnivChar univ)
        {
            if (index_ < count_)
            {
                min = ranges_[index_].descMin;
                max = ranges_[index_].descMin + (WideChar)(ranges_[index_].count - 1);
                univ = ranges_[index_].univMin;
                index_++;
                return true;
            }
            else
            {
                min = 0;
                max = 0;
                univ = 0;
                return false;
            }
        }
    }

    // Static range tables
    private static readonly UnivCharsetDesc.Range[] iso646_ascii = new UnivCharsetDesc.Range[]
    {
        new UnivCharsetDesc.Range { descMin = 0, count = 128, univMin = 0 },
    };

    private static readonly UnivCharsetDesc.Range[] iso646_C0 = new UnivCharsetDesc.Range[]
    {
        new UnivCharsetDesc.Range { descMin = 0, count = 32, univMin = 0 },
        new UnivCharsetDesc.Range { descMin = 127, count = 1, univMin = 127 },
    };

    private static readonly UnivCharsetDesc.Range[] iso6429 = new UnivCharsetDesc.Range[]
    {
        new UnivCharsetDesc.Range { descMin = 0, count = 32, univMin = 128 },
    };

    private static readonly UnivCharsetDesc.Range[] iso8859_1 = new UnivCharsetDesc.Range[]
    {
        new UnivCharsetDesc.Range { descMin = 32, count = 96, univMin = 160 },
    };

    private static readonly UnivCharsetDesc.Range[] iso10646_ucs2 = new UnivCharsetDesc.Range[]
    {
        new UnivCharsetDesc.Range { descMin = 0, count = 65536, univMin = 0 },
    };

    private static readonly UnivCharsetDesc.Range[] iso10646_ucs4 = new UnivCharsetDesc.Range[]
    {
        new UnivCharsetDesc.Range { descMin = 0, count = 0x80000000, univMin = 0 },
    };

    // static ISORegistrationNumber getRegistrationNumber(const StringC &desig, const CharsetInfo &);
    public static ISORegistrationNumber getRegistrationNumber(StringC sequence, CharsetInfo charset)
    {
        // Canonicalize the escape sequence by mapping esc -> ESC,
        // removing leading zeros from escape sequences, and removing
        // initial spaces.
        StringC s = new StringC();
        for (nuint i = 0; i < sequence.size(); i++)
        {
            Char c = sequence[i];
            if (c == charset.execToDesc((sbyte)'e'))
                s.operatorPlusAssign(charset.execToDesc((sbyte)'E'));
            else if (c == charset.execToDesc((sbyte)'s'))
                s.operatorPlusAssign(charset.execToDesc((sbyte)'S'));
            else if (c == charset.execToDesc((sbyte)'c'))
                s.operatorPlusAssign(charset.execToDesc((sbyte)'C'));
            else if (charset.digitWeight(c) >= 0
                     && s.size() > 0
                     && s[s.size() - 1] == charset.execToDesc((sbyte)'0')
                     && (s.size() == 1
                         || charset.digitWeight(s[s.size() - 2]) >= 0))
                s[s.size() - 1] = c;
            else if (c != charset.execToDesc((sbyte)' ') || s.size() > 0)
                s.operatorPlusAssign(c);
        }

        // Check escape sequences for common charsets
        // ESC 02/08 04/02 = ASCII
        StringC asciiEsc = buildEscSequence(charset, new byte[] { 0x1B, 0x28, 0x42 });
        if (stringEquals(s, asciiEsc))
            return ISORegistrationNumber.ISO646_ASCII_G0;

        // ESC 02/13 04/01 = ISO 8859-1
        StringC iso8859_1Esc = buildEscSequence(charset, new byte[] { 0x1B, 0x2D, 0x41 });
        if (stringEquals(s, iso8859_1Esc))
            return ISORegistrationNumber.ISO8859_1;

        // ESC 02/05 02/15 04/00 = UCS-2
        StringC ucs2Esc = buildEscSequence(charset, new byte[] { 0x1B, 0x25, 0x2F, 0x40 });
        if (stringEquals(s, ucs2Esc))
            return ISORegistrationNumber.ISO10646_UCS2;

        // ESC 02/05 02/15 04/01 = UCS-4
        StringC ucs4Esc = buildEscSequence(charset, new byte[] { 0x1B, 0x25, 0x2F, 0x41 });
        if (stringEquals(s, ucs4Esc))
            return ISORegistrationNumber.ISO10646_UCS4;

        return ISORegistrationNumber.UNREGISTERED;
    }

    // static Iter *makeIter(ISORegistrationNumber);
    public static Iter? makeIter(ISORegistrationNumber number)
    {
        switch (number)
        {
            case ISORegistrationNumber.ISO646_ASCII_G0:
                return new RangeIter(iso646_ascii, (nuint)iso646_ascii.Length);
            case ISORegistrationNumber.ISO646_C0:
                return new RangeIter(iso646_C0, (nuint)iso646_C0.Length);
            case ISORegistrationNumber.ISO6429:
                return new RangeIter(iso6429, (nuint)iso6429.Length);
            case ISORegistrationNumber.ISO8859_1:
                return new RangeIter(iso8859_1, (nuint)iso8859_1.Length);
            case ISORegistrationNumber.ISO10646_UCS2:
                return new RangeIter(iso10646_ucs2, (nuint)iso10646_ucs2.Length);
            case ISORegistrationNumber.ISO10646_UCS4:
                return new RangeIter(iso10646_ucs4, (nuint)iso10646_ucs4.Length);
            // TODO: Add other charsets (ISO8859-2 through ISO8859-9, JIS, etc.)
            // These require more complex descriptor tables
            default:
                return null;
        }
    }

    // Helper to build escape sequence StringC
    private static StringC buildEscSequence(CharsetInfo charset, byte[] bytes)
    {
        StringC result = new StringC();
        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            if (b == 0x1B)
            {
                // ESC
                result.operatorPlusAssign(charset.execToDesc((sbyte)'E'));
                result.operatorPlusAssign(charset.execToDesc((sbyte)'S'));
                result.operatorPlusAssign(charset.execToDesc((sbyte)'C'));
            }
            else
            {
                // Encode as two hex digits separated by /
                int hi = (b >> 4);
                int lo = (b & 0xF);
                sbyte[] digits = { (sbyte)'0', (sbyte)'1', (sbyte)'2', (sbyte)'3', (sbyte)'4',
                                   (sbyte)'5', (sbyte)'6', (sbyte)'7', (sbyte)'8', (sbyte)'9' };
                if (hi >= 10)
                    result.operatorPlusAssign(charset.execToDesc((sbyte)'1'));
                result.operatorPlusAssign(charset.execToDesc(digits[hi % 10]));
                result.operatorPlusAssign(charset.execToDesc((sbyte)'/'));
                if (lo >= 10)
                    result.operatorPlusAssign(charset.execToDesc((sbyte)'1'));
                result.operatorPlusAssign(charset.execToDesc(digits[lo % 10]));
            }
            if (i + 1 < bytes.Length)
                result.operatorPlusAssign(charset.execToDesc((sbyte)' '));
        }
        return result;
    }

    // Helper to compare StringC
    private static Boolean stringEquals(StringC a, StringC b)
    {
        if (a.size() != b.size())
            return false;
        for (nuint i = 0; i < a.size(); i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }
}
