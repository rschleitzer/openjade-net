// Copyright (c) 2000 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

public class Fixed4Decoder : Decoder
{
    private const Char invalid = 0xfffd;
    private Boolean lsbFirst_;
    private Boolean lswFirst_;

    // Fixed4Decoder(Boolean lsbFirst, Boolean lswFirst);
    public Fixed4Decoder(Boolean lsbFirst, Boolean lswFirst) : base(4)
    {
        lsbFirst_ = lsbFirst;
        lswFirst_ = lswFirst;
    }

    // size_t decode(Char *, const char *, size_t, const char **);
    public override nuint decode(Char[] to, byte[] from, nuint fromLen, out nuint fromUsed)
    {
        fromLen &= ~(nuint)3;
        fromUsed = fromLen;

        //  lsbFirst,  lswFirst: 0123
        //  lsbFirst, !lswFirst: 2301
        // !lsbFirst,  lswFirst: 1032
        // !lsbFirst, !lswFirst: 3210
        int shift0 = 8 * ((!lsbFirst_ ? 1 : 0) + 2 * (!lswFirst_ ? 1 : 0));
        int shift1 = 8 * ((lsbFirst_ ? 1 : 0) + 2 * (!lswFirst_ ? 1 : 0));
        int shift2 = 8 * ((!lsbFirst_ ? 1 : 0) + 2 * (lswFirst_ ? 1 : 0));
        int shift3 = 8 * ((lsbFirst_ ? 1 : 0) + 2 * (lswFirst_ ? 1 : 0));

        nuint toIndex = 0;
        for (nuint i = 0; i < fromLen; i += 4)
        {
            Unsigned32 c = ((Unsigned32)from[i] << shift0)
                         + ((Unsigned32)from[i + 1] << shift1)
                         + ((Unsigned32)from[i + 2] << shift2)
                         + ((Unsigned32)from[i + 3] << shift3);
            to[toIndex++] = Constant.charMax < c ? invalid : c;
        }
        return fromLen / 4;
    }

    // Boolean convertOffset(unsigned long &n) const;
    public override Boolean convertOffset(ref ulong n)
    {
        n *= 4;
        return true;
    }
}

public class Fixed4Encoder : Encoder
{
    private sbyte[]? buf_;
    private nuint bufSize_;

    // Fixed4Encoder();
    public Fixed4Encoder()
    {
        buf_ = null;
        bufSize_ = 0;
    }

    private void allocBuf(nuint n)
    {
        if (bufSize_ < n)
        {
            buf_ = new sbyte[n];
            bufSize_ = n;
        }
    }

    // void output(const Char *, size_t, OutputByteStream *);
    public override void output(Char[] s, nuint n, OutputByteStream sb)
    {
        allocBuf(n * 4);
        for (nuint i = 0; i < n; i++)
        {
            buf_![i * 4] = (sbyte)((s[i] >> 24) & 0xff);
            buf_[i * 4 + 1] = (sbyte)((s[i] >> 16) & 0xff);
            buf_[i * 4 + 2] = (sbyte)((s[i] >> 8) & 0xff);
            buf_[i * 4 + 3] = (sbyte)(s[i] & 0xff);
        }
        sb.sputn(buf_!, n * 4);
    }
}

public class Fixed4CodingSystem : CodingSystem
{
    // Decoder *makeDecoder(Boolean lsbFirst, Boolean lswFirst) const;
    public new Decoder makeDecoder(Boolean lsbFirst, Boolean lswFirst)
    {
        return new Fixed4Decoder(lsbFirst, lswFirst);
    }

    // Decoder *makeDecoder() const - default implementation
    public override Decoder makeDecoder()
    {
        return new Fixed4Decoder(false, false); // Big-endian by default
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        return new Fixed4Encoder();
    }

    // unsigned fixedBytesPerChar() const;
    public override uint fixedBytesPerChar()
    {
        return 4;
    }
}
