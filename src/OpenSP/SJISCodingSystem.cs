// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class SJISDecoder : Decoder
{
    // SJISDecoder();
    public SJISDecoder() : base(1)
    {
    }

    // size_t decode(Char *, const char *, size_t, const char **);
    public override nuint decode(Char[] to, byte[] s, nuint slen, out nuint sUsed)
    {
        nuint toIndex = 0;
        nuint sIndex = 0;

        while (slen > 0)
        {
            byte c = s[sIndex];
            if ((c & 0x80) == 0)
            {
                to[toIndex++] = c;
                sIndex++;
                slen--;
            }
            else if (129 <= c && c <= 159)
            {
                if (slen < 2)
                    break;
                sIndex++;
                slen -= 2;
                byte c2 = s[sIndex++];
                uint n = (uint)(((c - 112) << 9) | c2);
                if (64 <= c2 && c2 <= 127)
                    n -= 31 + (1 << 8);
                else if (c2 <= 158)
                    n -= 32 + (1 << 8);
                else if (c2 <= 252)
                    n -= 126;
                else
                    continue;
                n |= 0x8080;
                to[toIndex++] = n;
            }
            else if (224 <= c && c <= 239)
            {
                if (slen < 2)
                    break;
                sIndex++;
                slen -= 2;
                byte c2 = s[sIndex++];
                uint n = (uint)(((c - 176) << 9) | c2);
                if (64 <= c2 && c2 <= 127)
                    n -= 31 + (1 << 8);
                else if (c2 <= 158)
                    n -= 32 + (1 << 8);
                else if (c2 <= 252)
                    n -= 126;
                else
                    continue;
                n |= 0x8080;
                to[toIndex++] = n;
            }
            else if (161 <= c && c <= 223)
            {
                slen--;
                sIndex++;
                to[toIndex++] = c;
            }
            else
            {
                // 128, 160, 240-255
                slen--;
                sIndex++;
            }
        }

        sUsed = sIndex;
        return toIndex;
    }
}

public class SJISEncoder : RecoveringEncoder
{
    // SJISEncoder();
    public SJISEncoder()
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
                byte c1 = (byte)((c >> 8) & 0x7f);
                byte c2 = (byte)(c & 0x7f);
                byte out1;
                if (c1 < 33)
                    out1 = 0;
                else if (c1 < 95)
                    out1 = (byte)(((c1 + 1) >> 1) + 112);
                else if (c1 < 127)
                    out1 = (byte)(((c1 + 1) >> 1) + 176);
                else
                    out1 = 0;
                if (out1 != 0)
                {
                    byte out2;
                    if ((c1 & 1) != 0)
                    {
                        if (c2 < 33)
                            out2 = 0;
                        else if (c2 <= 95)
                            out2 = (byte)(c2 + 31);
                        else if (c2 <= 126)
                            out2 = (byte)(c2 + 32);
                        else
                            out2 = 0;
                    }
                    else
                    {
                        if (33 <= c2 && c2 <= 126)
                            out2 = (byte)(c2 + 126);
                        else
                            out2 = 0;
                    }
                    if (out2 != 0)
                    {
                        sb.sputc(unchecked((sbyte)out1));
                        sb.sputc(unchecked((sbyte)out2));
                    }
                    else
                        handleUnencodable(c, sb);
                }
                else
                    handleUnencodable(c, sb);
            }
            else if (mask == 0x0080)
            {
                if (161 <= c && c <= 223)
                    sb.sputc(unchecked((sbyte)(c & 0xff)));
                else
                    handleUnencodable(c, sb);
            }
            else
                handleUnencodable(c, sb);
        }
    }
}

public class SJISCodingSystem : CodingSystem
{
    // Decoder *makeDecoder() const;
    public override Decoder makeDecoder()
    {
        return new SJISDecoder();
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        return new SJISEncoder();
    }
}
