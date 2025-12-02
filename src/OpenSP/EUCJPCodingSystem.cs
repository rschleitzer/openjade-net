// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class EUCJPDecoder : Decoder
{
    // EUCJPDecoder();
    public EUCJPDecoder() : base(1)
    {
    }

    // size_t decode(Char *, const char *, size_t, const char **);
    public override nuint decode(Char[] to, byte[] s, nuint slen, out nuint sUsed)
    {
        nuint toIndex = 0;
        nuint sIndex = 0;

        while (slen > 0)
        {
            if ((s[sIndex] & 0x80) == 0)
            {
                // G0
                to[toIndex++] = s[sIndex++];
                slen--;
            }
            else if (s[sIndex] == 0x8e)
            {
                // G2
                if (slen < 2)
                    break;
                slen -= 2;
                sIndex++;
                to[toIndex++] = (Char)(s[sIndex++] | 0x80);
            }
            else if (s[sIndex] == 0x8f)
            {
                // G3
                if (slen < 3)
                    break;
                slen -= 3;
                sIndex++;
                Char n = (Char)((s[sIndex++] | 0x80) << 8);
                n |= (Char)(s[sIndex++] & ~0x80);
                to[toIndex++] = n;
            }
            else
            {
                // G1
                if (slen < 2)
                    break;
                slen -= 2;
                Char n = (Char)(s[sIndex++] << 8);
                n |= (Char)(s[sIndex++] | 0x80);
                to[toIndex++] = n;
            }
        }

        sUsed = sIndex;
        return toIndex;
    }
}

public class EUCJPEncoder : Encoder
{
    // EUCJPEncoder();
    public EUCJPEncoder()
    {
    }

    // void output(const Char *, size_t, OutputByteStream *);
    public override void output(Char[] s, nuint n, OutputByteStream sb)
    {
        for (nuint i = 0; i < n; i++)
        {
            Char c = s[i];
            uint mask = (uint)(c & 0x8080);
            if (mask == 0)
                sb.sputc((sbyte)(c & 0xff));
            else if (mask == 0x8080)
            {
                sb.sputc((sbyte)((c >> 8) & 0xff));
                sb.sputc((sbyte)(c & 0xff));
            }
            else if (mask == 0x0080)
            {
                sb.sputc(unchecked((sbyte)0x8e));
                sb.sputc((sbyte)(c & 0xff));
            }
            else
            {
                // mask == 0x8000
                sb.sputc(unchecked((sbyte)0x8f));
                sb.sputc((sbyte)((c >> 8) & 0xff));
                sb.sputc((sbyte)(c & 0x7f));
            }
        }
    }
}

public class EUCJPCodingSystem : CodingSystem
{
    // Decoder *makeDecoder() const;
    public override Decoder makeDecoder()
    {
        return new EUCJPDecoder();
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        return new EUCJPEncoder();
    }
}
