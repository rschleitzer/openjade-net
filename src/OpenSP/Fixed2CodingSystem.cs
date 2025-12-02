// Copyright (c) 1994 James Clark, 2000 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

public class Fixed2Decoder : Decoder
{
    private Boolean lsbFirst_;

    // Fixed2Decoder(Boolean lsbFirst);
    public Fixed2Decoder(Boolean lsbFirst) : base(2)
    {
        lsbFirst_ = lsbFirst;
    }

    // size_t decode(Char *, const char *, size_t, const char **);
    public override nuint decode(Char[] to, byte[] from, nuint fromLen, out nuint fromUsed)
    {
        fromLen &= ~(nuint)1;
        fromUsed = fromLen;

        nuint toIndex = 0;
        for (nuint i = 0; i < fromLen; i += 2)
        {
            to[toIndex++] = lsbFirst_
                ? ((Char)from[i + 1] << 8) + from[i]
                : ((Char)from[i] << 8) + from[i + 1];
        }
        return fromLen / 2;
    }

    // Boolean convertOffset(unsigned long &n) const;
    public override Boolean convertOffset(ref ulong n)
    {
        n *= 2;
        return true;
    }
}

public class Fixed2Encoder : RecoveringEncoder
{
    // Fixed2Encoder();
    public Fixed2Encoder()
    {
    }

    // void output(const Char *, size_t, OutputByteStream *);
    public override void output(Char[] s, nuint n, OutputByteStream sb)
    {
        for (nuint i = 0; i < n; i++)
        {
            Char c = s[i];
            if (c > 0xffff)
                handleUnencodable(c, sb);
            else
            {
                sb.sputc((sbyte)((c >> 8) & 0xff));
                sb.sputc((sbyte)(c & 0xff));
            }
        }
    }
}

public class Fixed2CodingSystem : CodingSystem
{
    // Decoder *makeDecoder(Boolean lsbFirst) const;
    public new Decoder makeDecoder(Boolean lsbFirst)
    {
        return new Fixed2Decoder(lsbFirst);
    }

    // Decoder *makeDecoder() const - default implementation
    public override Decoder makeDecoder()
    {
        return new Fixed2Decoder(false); // Big-endian by default
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        return new Fixed2Encoder();
    }

    // unsigned fixedBytesPerChar() const;
    public override uint fixedBytesPerChar()
    {
        return 2;
    }
}
