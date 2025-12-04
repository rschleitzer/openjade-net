// Copyright (c) 1994, 1997 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class CharsetInfo
{
    // Use unchecked to allow negative values in uint conversion (like C++)
    private static readonly Unsigned32 NoMapping = unchecked((Unsigned32)(-1));
    private static readonly Unsigned32 MultipleMapping = unchecked((Unsigned32)(-2));

    private UnivCharsetDesc desc_;
    private CharMap<Unsigned32> inverse_;
    private Char[] execToDesc_ = new Char[256]; // UCHAR_MAX + 1

    // CharsetInfo();
    public CharsetInfo()
    {
        desc_ = new UnivCharsetDesc();
        inverse_ = new CharMap<Unsigned32>(NoMapping);
        inverse_.setAll(NoMapping);

        // Initialize execToDesc_ with identity mapping for ASCII characters
        // This is needed so that digitWeight() and other methods work even
        // before the charset is fully set up (e.g., when parsing SGML declarations)
        for (int i = 0; i < 256; i++)
            execToDesc_[i] = (Char)i;
    }

    // CharsetInfo(const UnivCharsetDesc &);
    public CharsetInfo(UnivCharsetDesc desc)
    {
        desc_ = new UnivCharsetDesc(desc);
        inverse_ = new CharMap<Unsigned32>(NoMapping);
        // FIXME remove mappings from desc for characters greater charMax
        init();
    }

    // void set(const UnivCharsetDesc &);
    public void set(UnivCharsetDesc desc)
    {
        desc_ = new UnivCharsetDesc(desc);
        init();
    }

    // Use only for characters guaranteed to be in the C basic execution
    // character set and which have been verified to be in this
    // character set.
    // Char execToDesc(char) const;
    public Char execToDesc(sbyte c)
    {
        return execToDesc_[(byte)c];
    }

    // StringC execToDesc(const char *s) const;
    public StringC execToDesc(string s)
    {
        StringC result = new StringC();
        foreach (char ch in s)
            result.operatorPlusAssign(execToDesc((sbyte)ch));
        return result;
    }

    // Boolean descToUniv(WideChar from, UnivChar &to) const;
    public Boolean descToUniv(WideChar from, out UnivChar to)
    {
        return desc_.descToUniv(from, out to);
    }

    // Boolean descToUniv(WideChar from, UnivChar &to, WideChar &alsoMax) const;
    public Boolean descToUniv(WideChar from, out UnivChar to, out WideChar alsoMax)
    {
        return desc_.descToUniv(from, out to, out alsoMax);
    }

    // Return 0 for no matches, 1 for 1, 2 for more than 1
    // to gets the first character; toSet gets all the characters
    // if there's more than 1.
    // unsigned univToDesc(UnivChar from, WideChar &to, ISet<WideChar> &toSet) const;
    public uint univToDesc(UnivChar from, out WideChar to, ISet<WideChar> toSet)
    {
        if (from <= Constant.charMax)
        {
            Unsigned32 n = inverse_[(Char)from];
            if (n == NoMapping)
            {
                to = 0;
                return 0;
            }
            if (n != MultipleMapping)
            {
                to = (WideChar)((n + from) & (((Unsigned32)1 << 31) - 1));
                return 1;
            }
        }
        return desc_.univToDesc(from, out to, toSet);
    }

    // unsigned univToDesc(UnivChar from, WideChar &to, ISet<WideChar> &toSet, WideChar &count) const;
    public uint univToDesc(UnivChar from, out WideChar to, ISet<WideChar> toSet, out WideChar count)
    {
        if (from <= Constant.charMax)
        {
            Char fromMax;
            Unsigned32 n = inverse_.getRange((Char)from, out fromMax);
            if (n == NoMapping)
            {
                count = (WideChar)((fromMax - from) + 1);
                to = 0;
                return 0;
            }
            if (n != MultipleMapping)
            {
                to = (WideChar)((n + from) & (((Unsigned32)1 << 31) - 1));
                count = (WideChar)((fromMax - from) + 1);
                return 1;
            }
        }
        return desc_.univToDesc(from, out to, toSet, out count);
    }

    // void getDescSet(ISet<Char> &) const;
    public void getDescSet(ISet<Char> set)
    {
        UnivCharsetDescIter iter = new UnivCharsetDescIter(desc_);
        WideChar descMin, descMax;
        UnivChar univMin;
        while (iter.next(out descMin, out descMax, out univMin))
        {
            if (descMin > Constant.charMax)
                break;
            if (descMax > Constant.charMax)
                descMax = Constant.charMax;
            set.addRange((Char)descMin, (Char)descMax);
        }
    }

    // int digitWeight(Char) const;
    public int digitWeight(Char c)
    {
        for (int i = 0; i < 10; i++)
            if (c == execToDesc((sbyte)('0' + i)))
                return i;
        return -1;
    }

    // int hexDigitWeight(Char) const;
    public int hexDigitWeight(Char c)
    {
        for (int i = 0; i < 10; i++)
            if (c == execToDesc((sbyte)('0' + i)))
                return i;
        for (int i = 0; i < 6; i++)
            if (c == execToDesc((sbyte)('a' + i)) || c == execToDesc((sbyte)('A' + i)))
                return i + 10;
        return -1;
    }

    // const UnivCharsetDesc &desc() const;
    public UnivCharsetDesc desc()
    {
        return desc_;
    }

    private void init()
    {
        inverse_.setAll(NoMapping);

        UnivCharsetDescIter iter = new UnivCharsetDescIter(desc_);

        WideChar descMin, descMax;
        UnivChar univMin;
        while (iter.next(out descMin, out descMax, out univMin))
        {
            if (univMin <= Constant.charMax)
            {
                Char univMax;
                if (Constant.charMax - univMin < descMax - descMin)
                    univMax = Constant.charMax;
                else
                    univMax = (Char)(univMin + (descMax - descMin));
                Unsigned32 diff = (Unsigned32)((descMin - univMin) & (((Unsigned32)1 << 31) - 1));
                for (;;)
                {
                    Char max;
                    Unsigned32 n = inverse_.getRange((Char)univMin, out max);
                    if (max > univMax)
                        max = univMax;
                    if (n == NoMapping)
                        inverse_.setRange((Char)univMin, max, diff);
                    else if (n != MultipleMapping)
                        inverse_.setRange((Char)univMin, max, MultipleMapping);
                    if (max == univMax)
                        break;
                    univMin = max + 1;
                }
            }
        }
        // These are the characters that the ANSI C
        // standard guarantees will be in the basic execution
        // character set.
        string execChars =
            "\t\n\r " +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "abcdefghijklmnopqrstuvwxyz" +
            "0123456789" +
            "!\"#%&'()*+,-./:" +
            ";<=>?[\\]^_{|}~";
        // These are the corresponding ISO 646 codes.
        byte[] univCodes = {
            9, 10, 13, 32,
            65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77,
            78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90,
            97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109,
            110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122,
            48, 49, 50, 51, 52, 53, 54, 55, 56, 57,
            33, 34, 35, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 58,
            59, 60, 61, 62, 63, 91, 92, 93, 94, 95, 123, 124, 125, 126,
        };
        for (int i = 0; i < execChars.Length; i++)
        {
            WideChar c;
            ISet<WideChar> set = new ISet<WideChar>();
            if (univToDesc(univCodes[i], out c, set) > 0 && c <= Constant.charMax)
                execToDesc_[(byte)execChars[i]] = (Char)c;
        }
    }
}
