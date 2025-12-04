// Copyright (c) 1994, 1997 James Clark
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

    // Descriptor iterator for charset descriptor tables
    private class DescIter : Iter
    {
        private ushort[] data_;
        private int index_;
        private int n_;
        private WideChar c_;

        public DescIter(ushort[] data)
        {
            data_ = data;
            index_ = 2;
            n_ = data[0];
            c_ = data[1];
        }

        public override Boolean next(out WideChar min, out WideChar max, out UnivChar univ)
        {
            if (n_ == 0)
            {
                n_ = data_[index_];
                if (n_ == 0)
                {
                    min = 0;
                    max = 0;
                    univ = 0;
                    return false;
                }
                index_++;
                c_ = data_[index_++];
            }
            int i = 1;
            for (; i < n_; i++)
            {
                if (data_[index_ + i] != data_[index_ + i - 1] + 1)
                    break;
            }
            min = c_;
            max = min + (WideChar)(i - 1);
            univ = data_[index_];
            index_ += i;
            c_ += (WideChar)i;
            n_ -= i;
            return true;
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

    // ISO 8859-2 (Latin-2, Central European)
    private static readonly ushort[] iso8859_2 = {
        96, 0x0020,
        0x00a0, 0x0104, 0x02d8, 0x0141, 0x00a4, 0x013d, 0x015a, 0x00a7,
        0x00a8, 0x0160, 0x015e, 0x0164, 0x0179, 0x00ad, 0x017d, 0x017b,
        0x00b0, 0x0105, 0x02db, 0x0142, 0x00b4, 0x013e, 0x015b, 0x02c7,
        0x00b8, 0x0161, 0x015f, 0x0165, 0x017a, 0x02dd, 0x017e, 0x017c,
        0x0154, 0x00c1, 0x00c2, 0x0102, 0x00c4, 0x0139, 0x0106, 0x00c7,
        0x010c, 0x00c9, 0x0118, 0x00cb, 0x011a, 0x00cd, 0x00ce, 0x010e,
        0x0110, 0x0143, 0x0147, 0x00d3, 0x00d4, 0x0150, 0x00d6, 0x00d7,
        0x0158, 0x016e, 0x00da, 0x0170, 0x00dc, 0x00dd, 0x0162, 0x00df,
        0x0155, 0x00e1, 0x00e2, 0x0103, 0x00e4, 0x013a, 0x0107, 0x00e7,
        0x010d, 0x00e9, 0x0119, 0x00eb, 0x011b, 0x00ed, 0x00ee, 0x010f,
        0x0111, 0x0144, 0x0148, 0x00f3, 0x00f4, 0x0151, 0x00f6, 0x00f7,
        0x0159, 0x016f, 0x00fa, 0x0171, 0x00fc, 0x00fd, 0x0163, 0x02d9,
        0
    };

    // ISO 8859-3 (Latin-3, South European)
    private static readonly ushort[] iso8859_3 = {
        5, 0x0020,
        0x00a0, 0x0126, 0x02d8, 0x00a3, 0x00a4,
        8, 0x0026,
        0x0124, 0x00a7, 0x00a8, 0x0130, 0x015e, 0x011e, 0x0134, 0x00ad,
        15, 0x002f,
        0x017b, 0x00b0, 0x0127, 0x00b2, 0x00b3, 0x00b4, 0x00b5, 0x0125,
        0x00b7, 0x00b8, 0x0131, 0x015f, 0x011f, 0x0135, 0x00bd,
        4, 0x003f,
        0x017c, 0x00c0, 0x00c1, 0x00c2,
        12, 0x0044,
        0x00c4, 0x010a, 0x0108, 0x00c7, 0x00c8, 0x00c9, 0x00ca, 0x00cb,
        0x00cc, 0x00cd, 0x00ce, 0x00cf,
        18, 0x0051,
        0x00d1, 0x00d2, 0x00d3, 0x00d4, 0x0120, 0x00d6, 0x00d7, 0x011c,
        0x00d9, 0x00da, 0x00db, 0x00dc, 0x016c, 0x015c, 0x00df, 0x00e0,
        0x00e1, 0x00e2,
        12, 0x0064,
        0x00e4, 0x010b, 0x0109, 0x00e7, 0x00e8, 0x00e9, 0x00ea, 0x00eb,
        0x00ec, 0x00ed, 0x00ee, 0x00ef,
        15, 0x0071,
        0x00f1, 0x00f2, 0x00f3, 0x00f4, 0x0121, 0x00f6, 0x00f7, 0x011d,
        0x00f9, 0x00fa, 0x00fb, 0x00fc, 0x016d, 0x015d, 0x02d9,
        0
    };

    // ISO 8859-4 (Latin-4, North European)
    private static readonly ushort[] iso8859_4 = {
        96, 0x0020,
        0x00a0, 0x0104, 0x0138, 0x0156, 0x00a4, 0x0128, 0x013b, 0x00a7,
        0x00a8, 0x0160, 0x0112, 0x0122, 0x0166, 0x00ad, 0x017d, 0x00af,
        0x00b0, 0x0105, 0x02db, 0x0157, 0x00b4, 0x0129, 0x013c, 0x02c7,
        0x00b8, 0x0161, 0x0113, 0x0123, 0x0167, 0x014a, 0x017e, 0x014b,
        0x0100, 0x00c1, 0x00c2, 0x00c3, 0x00c4, 0x00c5, 0x00c6, 0x012e,
        0x010c, 0x00c9, 0x0118, 0x00cb, 0x0116, 0x00cd, 0x00ce, 0x012a,
        0x0110, 0x0145, 0x014c, 0x0136, 0x00d4, 0x00d5, 0x00d6, 0x00d7,
        0x00d8, 0x0172, 0x00da, 0x00db, 0x00dc, 0x0168, 0x016a, 0x00df,
        0x0101, 0x00e1, 0x00e2, 0x00e3, 0x00e4, 0x00e5, 0x00e6, 0x012f,
        0x010d, 0x00e9, 0x0119, 0x00eb, 0x0117, 0x00ed, 0x00ee, 0x012b,
        0x0111, 0x0146, 0x014d, 0x0137, 0x00f4, 0x00f5, 0x00f6, 0x00f7,
        0x00f8, 0x0173, 0x00fa, 0x00fb, 0x00fc, 0x0169, 0x016b, 0x02d9,
        0
    };

    // ISO 8859-5 (Cyrillic)
    private static readonly ushort[] iso8859_5 = {
        96, 0x0020,
        0x00a0, 0x0401, 0x0402, 0x0403, 0x0404, 0x0405, 0x0406, 0x0407,
        0x0408, 0x0409, 0x040a, 0x040b, 0x040c, 0x00ad, 0x040e, 0x040f,
        0x0410, 0x0411, 0x0412, 0x0413, 0x0414, 0x0415, 0x0416, 0x0417,
        0x0418, 0x0419, 0x041a, 0x041b, 0x041c, 0x041d, 0x041e, 0x041f,
        0x0420, 0x0421, 0x0422, 0x0423, 0x0424, 0x0425, 0x0426, 0x0427,
        0x0428, 0x0429, 0x042a, 0x042b, 0x042c, 0x042d, 0x042e, 0x042f,
        0x0430, 0x0431, 0x0432, 0x0433, 0x0434, 0x0435, 0x0436, 0x0437,
        0x0438, 0x0439, 0x043a, 0x043b, 0x043c, 0x043d, 0x043e, 0x043f,
        0x0440, 0x0441, 0x0442, 0x0443, 0x0444, 0x0445, 0x0446, 0x0447,
        0x0448, 0x0449, 0x044a, 0x044b, 0x044c, 0x044d, 0x044e, 0x044f,
        0x2116, 0x0451, 0x0452, 0x0453, 0x0454, 0x0455, 0x0456, 0x0457,
        0x0458, 0x0459, 0x045a, 0x045b, 0x045c, 0x00a7, 0x045e, 0x045f,
        0
    };

    // ISO 8859-6 (Arabic)
    private static readonly ushort[] iso8859_6 = {
        1, 0x0020,
        0x00a0,
        1, 0x0024,
        0x00a4,
        2, 0x002c,
        0x060c, 0x00ad,
        1, 0x003b,
        0x061b,
        1, 0x003f,
        0x061f,
        26, 0x0041,
        0x0621, 0x0622, 0x0623, 0x0624, 0x0625, 0x0626, 0x0627, 0x0628,
        0x0629, 0x062a, 0x062b, 0x062c, 0x062d, 0x062e, 0x062f, 0x0630,
        0x0631, 0x0632, 0x0633, 0x0634, 0x0635, 0x0636, 0x0637, 0x0638,
        0x0639, 0x063a,
        19, 0x0060,
        0x0640, 0x0641, 0x0642, 0x0643, 0x0644, 0x0645, 0x0646, 0x0647,
        0x0648, 0x0649, 0x064a, 0x064b, 0x064c, 0x064d, 0x064e, 0x064f,
        0x0650, 0x0651, 0x0652,
        0
    };

    // ISO 8859-7 (Greek)
    private static readonly ushort[] iso8859_7 = {
        4, 0x0020,
        0x00a0, 0x02bd, 0x02bc, 0x00a3,
        4, 0x0026,
        0x00a6, 0x00a7, 0x00a8, 0x00a9,
        3, 0x002b,
        0x00ab, 0x00ac, 0x00ad,
        35, 0x002f,
        0x2015, 0x00b0, 0x00b1, 0x00b2, 0x00b3, 0x0384, 0x0385, 0x0386,
        0x00b7, 0x0388, 0x0389, 0x038a, 0x00bb, 0x038c, 0x00bd, 0x038e,
        0x038f, 0x0390, 0x0391, 0x0392, 0x0393, 0x0394, 0x0395, 0x0396,
        0x0397, 0x0398, 0x0399, 0x039a, 0x039b, 0x039c, 0x039d, 0x039e,
        0x039f, 0x03a0, 0x03a1,
        44, 0x0053,
        0x03a3, 0x03a4, 0x03a5, 0x03a6, 0x03a7, 0x03a8, 0x03a9, 0x03aa,
        0x03ab, 0x03ac, 0x03ad, 0x03ae, 0x03af, 0x03b0, 0x03b1, 0x03b2,
        0x03b3, 0x03b4, 0x03b5, 0x03b6, 0x03b7, 0x03b8, 0x03b9, 0x03ba,
        0x03bb, 0x03bc, 0x03bd, 0x03be, 0x03bf, 0x03c0, 0x03c1, 0x03c2,
        0x03c3, 0x03c4, 0x03c5, 0x03c6, 0x03c7, 0x03c8, 0x03c9, 0x03ca,
        0x03cb, 0x03cc, 0x03cd, 0x03ce,
        0
    };

    // ISO 8859-8 (Hebrew)
    private static readonly ushort[] iso8859_8 = {
        1, 0x0020,
        0x00a0,
        29, 0x0022,
        0x00a2, 0x00a3, 0x00a4, 0x00a5, 0x00a6, 0x00a7, 0x00a8, 0x00a9,
        0x00d7, 0x00ab, 0x00ac, 0x00ad, 0x00ae, 0x203e, 0x00b0, 0x00b1,
        0x00b2, 0x00b3, 0x00b4, 0x00b5, 0x00b6, 0x00b7, 0x00b8, 0x00b9,
        0x00f7, 0x00bb, 0x00bc, 0x00bd, 0x00be,
        28, 0x005f,
        0x2017, 0x05d0, 0x05d1, 0x05d2, 0x05d3, 0x05d4, 0x05d5, 0x05d6,
        0x05d7, 0x05d8, 0x05d9, 0x05da, 0x05db, 0x05dc, 0x05dd, 0x05de,
        0x05df, 0x05e0, 0x05e1, 0x05e2, 0x05e3, 0x05e4, 0x05e5, 0x05e6,
        0x05e7, 0x05e8, 0x05e9, 0x05ea,
        0
    };

    // ISO 8859-9 (Latin-5, Turkish)
    private static readonly ushort[] iso8859_9 = {
        96, 0x0020,
        0x00a0, 0x00a1, 0x00a2, 0x00a3, 0x00a4, 0x00a5, 0x00a6, 0x00a7,
        0x00a8, 0x00a9, 0x00aa, 0x00ab, 0x00ac, 0x00ad, 0x00ae, 0x00af,
        0x00b0, 0x00b1, 0x00b2, 0x00b3, 0x00b4, 0x00b5, 0x00b6, 0x00b7,
        0x00b8, 0x00b9, 0x00ba, 0x00bb, 0x00bc, 0x00bd, 0x00be, 0x00bf,
        0x00c0, 0x00c1, 0x00c2, 0x00c3, 0x00c4, 0x00c5, 0x00c6, 0x00c7,
        0x00c8, 0x00c9, 0x00ca, 0x00cb, 0x00cc, 0x00cd, 0x00ce, 0x00cf,
        0x011e, 0x00d1, 0x00d2, 0x00d3, 0x00d4, 0x00d5, 0x00d6, 0x00d7,
        0x00d8, 0x00d9, 0x00da, 0x00db, 0x00dc, 0x0130, 0x015e, 0x00df,
        0x00e0, 0x00e1, 0x00e2, 0x00e3, 0x00e4, 0x00e5, 0x00e6, 0x00e7,
        0x00e8, 0x00e9, 0x00ea, 0x00eb, 0x00ec, 0x00ed, 0x00ee, 0x00ef,
        0x011f, 0x00f1, 0x00f2, 0x00f3, 0x00f4, 0x00f5, 0x00f6, 0x00f7,
        0x00f8, 0x00f9, 0x00fa, 0x00fb, 0x00fc, 0x0131, 0x015f, 0x00ff,
        0
    };

    // Escape sequence table
    private static readonly (byte[] esc, ISORegistrationNumber number)[] escTable = {
        (new byte[] { 0x1B, 0x25, 0x40 }, ISORegistrationNumber.ISO646_ASCII_G0),
        (new byte[] { 0x1B, 0x28, 0x40 }, ISORegistrationNumber.ISO646_ASCII_G0),
        (new byte[] { 0x1B, 0x28, 0x42 }, ISORegistrationNumber.ISO646_ASCII_G0), // ASCII
        (new byte[] { 0x1B, 0x21, 0x40 }, ISORegistrationNumber.ISO646_C0),
        (new byte[] { 0x1B, 0x2D, 0x41 }, ISORegistrationNumber.ISO8859_1),
        (new byte[] { 0x1B, 0x2D, 0x42 }, ISORegistrationNumber.ISO8859_2),
        (new byte[] { 0x1B, 0x2D, 0x43 }, ISORegistrationNumber.ISO8859_3),
        (new byte[] { 0x1B, 0x2D, 0x44 }, ISORegistrationNumber.ISO8859_4),
        (new byte[] { 0x1B, 0x2D, 0x4C }, ISORegistrationNumber.ISO8859_5),
        (new byte[] { 0x1B, 0x2D, 0x47 }, ISORegistrationNumber.ISO8859_6),
        (new byte[] { 0x1B, 0x2D, 0x46 }, ISORegistrationNumber.ISO8859_7),
        (new byte[] { 0x1B, 0x2D, 0x48 }, ISORegistrationNumber.ISO8859_8),
        (new byte[] { 0x1B, 0x2D, 0x4D }, ISORegistrationNumber.ISO8859_9),
        (new byte[] { 0x1B, 0x28, 0x4A }, ISORegistrationNumber.ISO646_JIS_G0),
        (new byte[] { 0x1B, 0x28, 0x49 }, ISORegistrationNumber.JIS0201),
        (new byte[] { 0x1B, 0x24, 0x42 }, ISORegistrationNumber.JIS0208),
        (new byte[] { 0x1B, 0x26, 0x40, 0x1B, 0x24, 0x42 }, ISORegistrationNumber.JIS0208),
        (new byte[] { 0x1B, 0x24, 0x28, 0x44 }, ISORegistrationNumber.JIS0212),
        (new byte[] { 0x1B, 0x24, 0x41 }, ISORegistrationNumber.GB2312),
        (new byte[] { 0x1B, 0x24, 0x28, 0x43 }, ISORegistrationNumber.KSC5601),
        (new byte[] { 0x1B, 0x25, 0x2F, 0x40 }, ISORegistrationNumber.ISO10646_UCS2),
        (new byte[] { 0x1B, 0x25, 0x2F, 0x41 }, ISORegistrationNumber.ISO10646_UCS4),
        (new byte[] { 0x1B, 0x25, 0x2F, 0x43 }, ISORegistrationNumber.ISO10646_UCS2),
        (new byte[] { 0x1B, 0x25, 0x2F, 0x44 }, ISORegistrationNumber.ISO10646_UCS4),
        (new byte[] { 0x1B, 0x25, 0x2F, 0x45 }, ISORegistrationNumber.ISO10646_UCS2),
        (new byte[] { 0x1B, 0x25, 0x2F, 0x46 }, ISORegistrationNumber.ISO10646_UCS4),
    };

    // static ISORegistrationNumber getRegistrationNumber(const StringC &desig, const CharsetInfo &);
    public static ISORegistrationNumber getRegistrationNumber(StringC sequence, CharsetInfo charset)
    {
        // First try direct ASCII matching as a fallback (handles uninitialized charsets)
        ISORegistrationNumber directResult = getRegistrationNumberDirect(sequence);
        if (directResult != ISORegistrationNumber.UNREGISTERED)
            return directResult;

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

        // Check escape sequences
        foreach (var entry in escTable)
        {
            StringC esc = buildEscSequence(charset, entry.esc);
            if (stringEquals(s, esc))
                return entry.number;
        }

        return ISORegistrationNumber.UNREGISTERED;
    }

    // Direct ASCII matching for charset identifiers (fallback when charset isn't properly initialized)
    private static ISORegistrationNumber getRegistrationNumberDirect(StringC sequence)
    {
        // Canonicalize: convert lowercase to uppercase, remove leading spaces, compress whitespace
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        Boolean inWhitespace = true;  // Start true to skip leading whitespace
        for (nuint i = 0; i < sequence.size(); i++)
        {
            Char c = sequence[i];
            // Check for whitespace (space, tab, newline, CR)
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
            {
                if (!inWhitespace && sb.Length > 0)
                {
                    sb.Append(' ');
                    inWhitespace = true;
                }
                continue;
            }
            inWhitespace = false;
            // Convert lowercase to uppercase
            if (c >= 'a' && c <= 'z')
                c = (Char)(c - 'a' + 'A');
            sb.Append((char)c);
        }
        // Remove trailing space
        string canonical = sb.ToString().TrimEnd();

        // Check for "ISO Registration Number NNN" format at the start
        // This is the standard format for base charset identifiers
        if (canonical.StartsWith("ISO REGISTRATION NUMBER "))
        {
            // Extract the registration number
            int numStart = "ISO REGISTRATION NUMBER ".Length;
            int numEnd = numStart;
            while (numEnd < canonical.Length && char.IsDigit(canonical[numEnd]))
                numEnd++;
            if (numEnd > numStart)
            {
                string numStr = canonical.Substring(numStart, numEnd - numStart);
                if (int.TryParse(numStr, out int regNum))
                {
                    return regNum switch
                    {
                        6 => ISORegistrationNumber.ISO646_ASCII_G0,
                        1 => ISORegistrationNumber.ISO646_C0,
                        77 => ISORegistrationNumber.ISO6429,
                        100 => ISORegistrationNumber.ISO8859_1,
                        101 => ISORegistrationNumber.ISO8859_2,
                        109 => ISORegistrationNumber.ISO8859_3,
                        110 => ISORegistrationNumber.ISO8859_4,
                        144 => ISORegistrationNumber.ISO8859_5,
                        127 => ISORegistrationNumber.ISO8859_6,
                        126 => ISORegistrationNumber.ISO8859_7,
                        138 => ISORegistrationNumber.ISO8859_8,
                        148 => ISORegistrationNumber.ISO8859_9,
                        14 => ISORegistrationNumber.ISO646_JIS_G0,
                        13 => ISORegistrationNumber.JIS0201,
                        168 => ISORegistrationNumber.JIS0208,
                        159 => ISORegistrationNumber.JIS0212,
                        149 => ISORegistrationNumber.KSC5601,
                        58 => ISORegistrationNumber.GB2312,
                        176 => ISORegistrationNumber.ISO10646_UCS2,
                        177 => ISORegistrationNumber.ISO10646_UCS4,
                        _ => ISORegistrationNumber.UNREGISTERED
                    };
                }
            }
        }

        // Check known escape sequences
        // ISO10646 UCS-4 variants
        if (canonical == "ESC 2/5 2/15 4/6" || canonical == "ESC 2/5 2/15 4/4" || canonical == "ESC 2/5 2/15 4/1")
            return ISORegistrationNumber.ISO10646_UCS4;
        // ISO10646 UCS-2 variants
        if (canonical == "ESC 2/5 2/15 4/5" || canonical == "ESC 2/5 2/15 4/3" || canonical == "ESC 2/5 2/15 4/0")
            return ISORegistrationNumber.ISO10646_UCS2;
        // ASCII
        if (canonical == "ESC 2/5 4/0" || canonical == "ESC 2/8 4/0" || canonical == "ESC 2/8 4/2")
            return ISORegistrationNumber.ISO646_ASCII_G0;
        // ISO 8859-1
        if (canonical == "ESC 2/13 4/1")
            return ISORegistrationNumber.ISO8859_1;
        // ISO 8859-2
        if (canonical == "ESC 2/13 4/2")
            return ISORegistrationNumber.ISO8859_2;

        return ISORegistrationNumber.UNREGISTERED;
    }

    // static Iter *makeIter(ISORegistrationNumber);
    public static Iter? makeIter(ISORegistrationNumber number)
    {
        switch (number)
        {
            // Range-based charsets
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

            // Descriptor-based charsets
            case ISORegistrationNumber.ISO8859_2:
                return new DescIter(iso8859_2);
            case ISORegistrationNumber.ISO8859_3:
                return new DescIter(iso8859_3);
            case ISORegistrationNumber.ISO8859_4:
                return new DescIter(iso8859_4);
            case ISORegistrationNumber.ISO8859_5:
                return new DescIter(iso8859_5);
            case ISORegistrationNumber.ISO8859_6:
                return new DescIter(iso8859_6);
            case ISORegistrationNumber.ISO8859_7:
                return new DescIter(iso8859_7);
            case ISORegistrationNumber.ISO8859_8:
                return new DescIter(iso8859_8);
            case ISORegistrationNumber.ISO8859_9:
                return new DescIter(iso8859_9);

            // JIS and other multi-byte charsets would require additional data tables
            // For now, return null for unsupported charsets
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
