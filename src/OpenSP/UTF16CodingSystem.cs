// Copyright (c) 2000 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

public class UTF16Decoder : Decoder
{
    private const Char invalid = 0xfffd;
    private Boolean lsbFirst_;

    // UTF16Decoder(Boolean lsbFirst);
    public UTF16Decoder(Boolean lsbFirst) : base(2)
    {
        lsbFirst_ = lsbFirst;
    }

    // size_t decode(Char *, const char *, size_t, const char **);
    public override nuint decode(Char[] to, byte[] input, nuint inputLen, out nuint inputUsed)
    {
        nuint toIndex = 0;
        nuint sIndex = 0;
        nuint slen = inputLen;

        for (;;)
        {
            if (slen < 2)
                break;

            Unsigned32 x = lsbFirst_
                ? ((Unsigned32)input[sIndex + 1] << 8) + input[sIndex]
                : ((Unsigned32)input[sIndex] << 8) + input[sIndex + 1];

            if (x < 0xd800 || x > 0xdfff)
            {
                to[toIndex++] = x;
                sIndex += 2;
                slen -= 2;
                continue;
            }

            if (x > 0xdbff)
            {
                // Unpaired RC element
                to[toIndex++] = invalid;
                sIndex += 2;
                slen -= 2;
                continue;
            }

            if (slen < 4)
                break;

            Unsigned32 y = lsbFirst_
                ? ((Unsigned32)input[sIndex + 3] << 8) + input[sIndex + 2]
                : ((Unsigned32)input[sIndex + 2] << 8) + input[sIndex + 3];

            if (y < 0xd800 || y > 0xdfff)
            {
                // Unpaired RC element
                to[toIndex++] = invalid;
                to[toIndex++] = y;
                sIndex += 4;
                slen -= 4;
                continue;
            }

            if (y < 0xdc00)
            {
                // Unpaired RC element
                to[toIndex++] = invalid;
                sIndex += 2;
                slen -= 2;
                continue;
            }

            to[toIndex++] = ((x - 0xd800) * 0x400 + (y - 0xdc00)) + 0x10000;
            sIndex += 4;
            slen -= 4;
        }

        inputUsed = sIndex;
        return toIndex;
    }
}

public class UTF16Encoder : Encoder
{
    // UTF16Encoder();
    public UTF16Encoder()
    {
    }

    // void output(const Char *, size_t, OutputByteStream *);
    public override void output(Char[] chars, nuint len, OutputByteStream sb)
    {
        for (nuint i = 0; i < len; i++)
        {
            Char c = chars[i];
            if (c < 0x10000)
            {
                sb.sputc((sbyte)((c >> 8) & 0xff));
                sb.sputc((sbyte)(c & 0xff));
            }
            else
            {
                Unsigned32 y = ((c - 0x10000) / 0x400) + 0xd800;
                Unsigned32 z = ((c - 0x10000) % 0x400) + 0xdc00;
                sb.sputc((sbyte)((y >> 8) & 0xff));
                sb.sputc((sbyte)(y & 0xff));
                sb.sputc((sbyte)((z >> 8) & 0xff));
                sb.sputc((sbyte)(z & 0xff));
            }
        }
    }
}

public class UTF16CodingSystem : CodingSystem
{
    // Decoder *makeDecoder(Boolean lsbFirst) const;
    public new Decoder makeDecoder(Boolean lsbFirst)
    {
        return new UTF16Decoder(lsbFirst);
    }

    // Decoder *makeDecoder() const - default implementation
    public override Decoder makeDecoder()
    {
        return new UTF16Decoder(false); // Big-endian by default
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        return new UTF16Encoder();
    }
}
