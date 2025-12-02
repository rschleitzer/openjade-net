// Copyright (c) 1997 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Big5Decoder : Decoder
{
    // Big5Decoder();
    public Big5Decoder() : base(1)
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
                to[toIndex++] = s[sIndex++];
                slen--;
            }
            else
            {
                if (slen < 2)
                    break;
                slen -= 2;
                Char n = (Char)(s[sIndex++] << 8);
                n |= s[sIndex++];
                to[toIndex++] = n;
            }
        }

        sUsed = sIndex;
        return toIndex;
    }
}

public class Big5Encoder : Encoder
{
    // Big5Encoder();
    public Big5Encoder()
    {
    }

    // void output(const Char *, size_t, OutputByteStream *);
    public override void output(Char[] s, nuint n, OutputByteStream sb)
    {
        for (nuint i = 0; i < n; i++)
        {
            Char c = s[i];
            if (c < 0x80)
                sb.sputc((sbyte)c);
            else if ((c & 0x8000) != 0)
            {
                sb.sputc((sbyte)(c >> 8));
                sb.sputc((sbyte)(c & 0xff));
            }
            else
                handleUnencodable(c, sb);
        }
    }
}

public class Big5CodingSystem : CodingSystem
{
    // Decoder *makeDecoder() const;
    public override Decoder makeDecoder()
    {
        return new Big5Decoder();
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        return new Big5Encoder();
    }
}
