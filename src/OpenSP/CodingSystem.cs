// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public abstract class Decoder
{
    protected uint minBytesPerChar_;

    // Decoder(unsigned minBytesPerChar = 1);
    public Decoder(uint minBytesPerChar = 1)
    {
        minBytesPerChar_ = minBytesPerChar;
    }

    // virtual ~Decoder();
    // C# GC handles cleanup

    // virtual size_t decode(Char *, const char *, size_t, const char **) = 0;
    public abstract nuint decode(Char[] result, byte[] input, nuint inputLen, out nuint inputUsed);

    // virtual Boolean convertOffset(unsigned long &offset) const;
    public virtual Boolean convertOffset(ref ulong offset)
    {
        return false;
    }

    // unsigned minBytesPerChar() const;
    public uint minBytesPerChar()
    {
        return minBytesPerChar_;
    }
}

public abstract class Encoder
{
    // Nested abstract class for handler
    public abstract class Handler
    {
        // virtual ~Handler();
        // C# GC handles cleanup

        // virtual void handleUnencodable(Char, OutputByteStream *) = 0;
        public abstract void handleUnencodable(Char c, OutputByteStream stream);
    }

    // Encoder();
    public Encoder()
    {
    }

    // virtual ~Encoder();
    // C# GC handles cleanup

    // virtual void output(const Char *, size_t, OutputByteStream *) = 0;
    public abstract void output(Char[] chars, nuint len, OutputByteStream stream);

    // virtual void startFile(OutputByteStream *);
    public virtual void startFile(OutputByteStream stream)
    {
    }

    // virtual void output(Char *, size_t, OutputByteStream *);
    public virtual void outputMutable(Char[] chars, nuint len, OutputByteStream stream)
    {
        output(chars, len, stream);
    }

    // virtual void setUnencodableHandler(Handler *);
    public virtual void setUnencodableHandler(Handler? handler)
    {
    }

    // virtual void handleUnencodable(Char, OutputByteStream *);
    public virtual void handleUnencodable(Char c, OutputByteStream stream)
    {
    }
}

public abstract class RecoveringEncoder : Encoder
{
    private Handler? unencodableHandler_;

    // RecoveringEncoder();
    protected RecoveringEncoder()
    {
        unencodableHandler_ = null;
    }

    // void setUnencodableHandler(Handler *);
    public override void setUnencodableHandler(Handler? handler)
    {
        unencodableHandler_ = handler;
    }

    // void handleUnencodable(Char, OutputByteStream *);
    public override void handleUnencodable(Char c, OutputByteStream stream)
    {
        if (unencodableHandler_ != null)
            unencodableHandler_.handleUnencodable(c, stream);
    }

    // Inherits abstract output() from Encoder - subclasses must implement
}

public abstract class InputCodingSystem
{
    // virtual ~InputCodingSystem();
    // C# GC handles cleanup

    // virtual Decoder *makeDecoder() const { return makeDecoder(1); }
    public virtual Decoder makeDecoder()
    {
        return makeDecoder(true);
    }

    // virtual Decoder *makeDecoder(Boolean lsbFirst) const { return makeDecoder(lsbFirst, 1); }
    public virtual Decoder makeDecoder(Boolean lsbFirst)
    {
        return makeDecoder(lsbFirst, true);
    }

    // virtual Decoder *makeDecoder(Boolean lsbFirst, Boolean lswFirst) const { return makeDecoder(); }
    public virtual Decoder makeDecoder(Boolean lsbFirst, Boolean lswFirst)
    {
        return makeDecoder();
    }

    // StringC convertIn(const char *) const;
    public StringC convertIn(string s)
    {
        Decoder decoder = makeDecoder();
        byte[] bytes = System.Text.Encoding.Latin1.GetBytes(s);
        StringC str = new StringC();
        str.resize((nuint)bytes.Length);
        Char[] result = new Char[bytes.Length];
        nuint inputUsed;
        nuint decoded = decoder.decode(result, bytes, (nuint)bytes.Length, out inputUsed);
        str.resize(decoded);
        for (nuint i = 0; i < decoded; i++)
            str[i] = result[i];
        return str;
    }

    // virtual Boolean isIdentity() const;
    public virtual Boolean isIdentity()
    {
        return false;
    }
}

public abstract class OutputCodingSystem
{
    // virtual ~OutputCodingSystem();
    // C# GC handles cleanup

    // virtual Encoder *makeEncoder() const = 0;
    public abstract Encoder makeEncoder();

    // virtual unsigned fixedBytesPerChar() const;
    public virtual uint fixedBytesPerChar()
    {
        return 0;
    }

    // String<char> convertOut(const StringC &) const;
    public String<sbyte> convertOut(StringC str)
    {
        Encoder encoder = makeEncoder();
        StrOutputByteStream stream = new StrOutputByteStream();
        Char[] chars = new Char[str.size()];
        for (nuint i = 0; i < str.size(); i++)
            chars[i] = str[i];
        encoder.output(chars, str.size(), stream);
        String<sbyte> result = new String<sbyte>();
        stream.extractString(result);
        result.operatorPlusAssign(0); // null terminator
        return result;
    }
}

public abstract class CodingSystem : InputCodingSystem
{
    // Inheriting from both InputCodingSystem and OutputCodingSystem is not possible in C#
    // So we implement OutputCodingSystem methods directly

    // virtual Encoder *makeEncoder() const = 0;
    public abstract Encoder makeEncoder();

    // virtual unsigned fixedBytesPerChar() const;
    public virtual uint fixedBytesPerChar()
    {
        return 0;
    }

    // String<char> convertOut(const StringC &) const;
    public String<sbyte> convertOut(StringC str)
    {
        Encoder encoder = makeEncoder();
        StrOutputByteStream stream = new StrOutputByteStream();
        Char[] chars = new Char[str.size()];
        for (nuint i = 0; i < str.size(); i++)
            chars[i] = str[i];
        encoder.output(chars, str.size(), stream);
        String<sbyte> result = new String<sbyte>();
        stream.extractString(result);
        result.operatorPlusAssign(0); // null terminator
        return result;
    }
}
