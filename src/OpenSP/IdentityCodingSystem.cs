// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class IdentityDecoder : Decoder
{
    // IdentityDecoder();
    public IdentityDecoder() : base(1)
    {
    }

    // size_t decode(Char *to, const char *from, size_t fromLen, const char **rest);
    public override nuint decode(Char[] result, byte[] input, nuint inputLen, out nuint inputUsed)
    {
        for (nuint i = 0; i < inputLen; i++)
            result[i] = input[i]; // zero extend
        inputUsed = inputLen;
        return inputLen;
    }

    // Boolean convertOffset(unsigned long &offset) const;
    public override Boolean convertOffset(ref ulong offset)
    {
        return true;
    }
}

public class IdentityEncoder : RecoveringEncoder
{
    // IdentityEncoder();
    public IdentityEncoder()
    {
    }

    // void output(const Char *, size_t, OutputByteStream *);
    public override void output(Char[] chars, nuint len, OutputByteStream stream)
    {
        for (nuint i = 0; i < len; i++)
        {
            Char c = chars[i];
            if (c > 255)
                handleUnencodable(c, stream);
            else
                stream.sputc((sbyte)c);
        }
    }
}

public class IdentityCodingSystem : CodingSystem
{
    // IdentityCodingSystem();
    public IdentityCodingSystem()
    {
    }

    // Decoder *makeDecoder() const;
    public override Decoder makeDecoder()
    {
        return new IdentityDecoder();
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        return new IdentityEncoder();
    }

    // Boolean isIdentity() const;
    public override Boolean isIdentity()
    {
        return true;
    }
}
