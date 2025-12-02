// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// Suppress unreachable code warnings - the checks against Constant.charMax
// are intentionally preserved from original C++ code for when charMax varies
#pragma warning disable CS0162

namespace OpenSP;

public class UTF8Decoder : Decoder
{
    // Constants for UTF-8 decoding
    private const uint cmask1 = 0x80;
    private const uint cmask2 = 0xe0;
    private const uint cmask3 = 0xf0;
    private const uint cmask4 = 0xf8;
    private const uint cmask5 = 0xfc;
    private const uint cmask6 = 0xfe;

    private const uint cval1 = 0x00;
    private const uint cval2 = 0xc0;
    private const uint cval3 = 0xe0;
    private const uint cval4 = 0xf0;
    private const uint cval5 = 0xf8;
    private const uint cval6 = 0xfc;

    private const uint vmask2 = 0x1f;
    private const uint vmask3 = 0xf;
    private const uint vmask4 = 0x7;
    private const uint vmask5 = 0x3;
    private const uint vmask6 = 0x1;

    private const uint min2 = 0x80;
    private const uint min3 = 0x800;
    private const uint min4 = 0x10000;
    private const uint min5 = 0x200000;
    private const uint min6 = 0x4000000;
    private const uint max6 = 0x7fffffff;

    private const Char invalid = 0xfffd;

    private Boolean recovering_;
    private PackedBoolean hadFirstChar_;
    private PackedBoolean hadByteOrderMark_;

    // UTF8Decoder();
    public UTF8Decoder() : base(1)
    {
        recovering_ = false;
        hadFirstChar_ = false;
        hadByteOrderMark_ = false;
    }

    private void recover(byte[] input, ref nuint sIndex, ref nuint slen)
    {
        for (;;)
        {
            if (slen == 0)
            {
                recovering_ = true;
                return;
            }
            if ((input[sIndex] & 0xc0) != 0x80)
                break;
            sIndex++;
            slen--;
        }
    }

    // size_t decode(Char *, const char *, size_t, const char **);
    public override nuint decode(Char[] to, byte[] input, nuint inputLen, out nuint inputUsed)
    {
        nuint toIndex = 0;
        nuint sIndex = 0;
        nuint slen = inputLen;

        // Check for byte-order mark
        if (!hadFirstChar_ && slen >= 3)
        {
            hadFirstChar_ = true;
            if (input[0] == 0xEF && input[1] == 0xBB && input[2] == 0xBF)
            {
                sIndex += 3;
                slen -= 3;
                hadByteOrderMark_ = true;
            }
        }

        if (recovering_)
        {
            recovering_ = false;
            recover(input, ref sIndex, ref slen);
            if (recovering_)
            {
                inputUsed = sIndex;
                return toIndex;
            }
        }

        while (slen > 0)
        {
            uint c0 = input[sIndex];
            bool isError = false;

            if ((c0 & cmask1) == cval1)
            {
                to[toIndex++] = c0;
                sIndex++;
                slen--;
            }
            else if ((c0 & cmask2) == cval2)
            {
                if (slen < 2)
                    break;
                uint c1 = input[sIndex + 1] ^ 0x80u;
                if ((c1 & 0xc0) != 0)
                    isError = true;
                else
                {
                    uint c = ((c0 & vmask2) << 6) | c1;
                    if (c < min2)
                        c = invalid;
                    to[toIndex++] = c;
                    slen -= 2;
                    sIndex += 2;
                }
            }
            else if ((c0 & cmask3) == cval3)
            {
                if (slen < 3)
                    break;
                uint c1 = input[sIndex + 1] ^ 0x80u;
                uint c2 = input[sIndex + 2] ^ 0x80u;
                if (((c1 | c2) & 0xc0) != 0)
                    isError = true;
                else
                {
                    uint c = ((((c0 & vmask3) << 6) | c1) << 6) | c2;
                    if (c < min3)
                        c = invalid;
                    to[toIndex++] = c;
                    slen -= 3;
                    sIndex += 3;
                }
            }
            else if ((c0 & cmask4) == cval4)
            {
                if (slen < 4)
                    break;
                uint c1 = input[sIndex + 1] ^ 0x80u;
                uint c2 = input[sIndex + 2] ^ 0x80u;
                uint c3 = input[sIndex + 3] ^ 0x80u;
                if (((c1 | c2 | c3) & 0xc0) != 0)
                    isError = true;
                else
                {
                    if (Constant.charMax < min5 - 1)
                        to[toIndex++] = invalid;
                    else
                    {
                        uint c = ((((c0 & vmask4) << 6) | c1) << 6) | c2;
                        c = (c << 6) | c3;
                        if (c < min4)
                            c = invalid;
                        to[toIndex++] = c;
                    }
                    slen -= 4;
                    sIndex += 4;
                }
            }
            else if ((c0 & cmask5) == cval5)
            {
                if (slen < 5)
                    break;
                uint c1 = input[sIndex + 1] ^ 0x80u;
                uint c2 = input[sIndex + 2] ^ 0x80u;
                uint c3 = input[sIndex + 3] ^ 0x80u;
                uint c4 = input[sIndex + 4] ^ 0x80u;
                if (((c1 | c2 | c3 | c4) & 0xc0) != 0)
                    isError = true;
                else
                {
                    if (Constant.charMax < min6 - 1)
                        to[toIndex++] = invalid;
                    else
                    {
                        uint c = ((((c0 & vmask5) << 6) | c1) << 6) | c2;
                        c = (((c << 6) | c3) << 6) | c4;
                        if (c < min5)
                            c = invalid;
                        to[toIndex++] = c;
                    }
                    slen -= 5;
                    sIndex += 5;
                }
            }
            else if ((c0 & cmask6) == cval6)
            {
                if (slen < 6)
                    break;
                uint c1 = input[sIndex + 1] ^ 0x80u;
                uint c2 = input[sIndex + 2] ^ 0x80u;
                uint c3 = input[sIndex + 3] ^ 0x80u;
                uint c4 = input[sIndex + 4] ^ 0x80u;
                uint c5 = input[sIndex + 5] ^ 0x80u;
                if (((c1 | c2 | c3 | c4 | c5) & 0xc0) != 0)
                    isError = true;
                else
                {
                    if (Constant.charMax < max6)
                        to[toIndex++] = invalid;
                    else
                    {
                        uint c = ((((c0 & vmask6) << 6) | c1) << 6) | c2;
                        c = (((((c << 6) | c3) << 6) | c4) << 6) | c5;
                        if (c < min6)
                            c = invalid;
                        to[toIndex++] = c;
                    }
                    slen -= 6;
                    sIndex += 6;
                }
            }
            else
            {
                isError = true;
            }

            if (isError)
            {
                sIndex++;
                slen--;
                to[toIndex++] = invalid;
                recover(input, ref sIndex, ref slen);
                if (recovering_)
                    break;
            }
        }

        inputUsed = sIndex;
        return toIndex;
    }

    // Boolean convertOffset(unsigned long &n) const;
    public override Boolean convertOffset(ref ulong n)
    {
        if (hadByteOrderMark_)
            n += 3;
        return true;
    }
}

public class UTF8Encoder : Encoder
{
    private const uint min2 = 0x80;
    private const uint min3 = 0x800;
    private const uint min4 = 0x10000;
    private const uint min5 = 0x200000;
    private const uint min6 = 0x4000000;
    private const uint max6 = 0x7fffffff;

    private const uint cval2 = 0xc0;
    private const uint cval3 = 0xe0;
    private const uint cval4 = 0xf0;
    private const uint cval5 = 0xf8;
    private const uint cval6 = 0xfc;

    // UTF8Encoder();
    public UTF8Encoder()
    {
    }

    // void output(const Char *, size_t, OutputByteStream *);
    public override void output(Char[] chars, nuint len, OutputByteStream sb)
    {
        for (nuint i = 0; i < len; i++)
        {
            Char c = chars[i];
            if (c < min2)
                sb.sputc((sbyte)c);
            else if (c < min3)
            {
                sb.sputc((sbyte)((c >> 6) | cval2));
                sb.sputc((sbyte)((c & 0x3f) | 0x80));
            }
            else if (c < min4)
            {
                sb.sputc((sbyte)((c >> 12) | cval3));
                sb.sputc((sbyte)(((c >> 6) & 0x3f) | 0x80));
                sb.sputc((sbyte)((c & 0x3f) | 0x80));
            }
            else if (c < min5)
            {
                sb.sputc((sbyte)((c >> 18) | cval4));
                sb.sputc((sbyte)(((c >> 12) & 0x3f) | 0x80));
                sb.sputc((sbyte)(((c >> 6) & 0x3f) | 0x80));
                sb.sputc((sbyte)((c & 0x3f) | 0x80));
            }
            else if (c < min6)
            {
                sb.sputc((sbyte)((c >> 24) | cval5));
                sb.sputc((sbyte)(((c >> 18) & 0x3f) | 0x80));
                sb.sputc((sbyte)(((c >> 12) & 0x3f) | 0x80));
                sb.sputc((sbyte)(((c >> 6) & 0x3f) | 0x80));
                sb.sputc((sbyte)((c & 0x3f) | 0x80));
            }
            else if (c <= max6)
            {
                sb.sputc((sbyte)((c >> 30) | cval6));
                sb.sputc((sbyte)(((c >> 24) & 0x3f) | 0x80));
                sb.sputc((sbyte)(((c >> 18) & 0x3f) | 0x80));
                sb.sputc((sbyte)(((c >> 12) & 0x3f) | 0x80));
                sb.sputc((sbyte)(((c >> 6) & 0x3f) | 0x80));
                sb.sputc((sbyte)((c & 0x3f) | 0x80));
            }
        }
    }
}

public class UTF8CodingSystem : CodingSystem
{
    // Decoder *makeDecoder() const;
    public override Decoder makeDecoder()
    {
        return new UTF8Decoder();
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        return new UTF8Encoder();
    }
}
