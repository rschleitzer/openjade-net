// Copyright (c) 1994 James Clark, 2000 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

public class UnicodeDecoder : Decoder
{
    private const ushort byteOrderMark = 0xfeff;
    private const ushort swappedByteOrderMark = 0xfffe;

    private PackedBoolean hadByteOrderMark_;
    private PackedBoolean swapBytes_;
    private Owner<Decoder> subDecoder_ = new Owner<Decoder>();
    private InputCodingSystem? subCodingSystem_;

    // UnicodeDecoder(const InputCodingSystem *sub);
    public UnicodeDecoder(InputCodingSystem? subCodingSystem) : base((uint)(subCodingSystem != null ? 1 : 2))
    {
        subCodingSystem_ = subCodingSystem;
        hadByteOrderMark_ = false;
        swapBytes_ = false;
    }

    // size_t decode(Char *, const char *, size_t, const char **);
    public override nuint decode(Char[] to, byte[] from, nuint fromLen, out nuint fromUsed)
    {
        if (subDecoder_.pointer() != null)
            return subDecoder_.pointer()!.decode(to, from, fromLen, out fromUsed);

        if (fromLen < 2)
        {
            fromUsed = 0;
            return 0;
        }

        minBytesPerChar_ = 2;
        ushort u = (ushort)((from[0] << 8) | from[1]);
        ushort uSwapped = (ushort)((from[1] << 8) | from[0]);

        nuint startOffset = 0;
        if (u == byteOrderMark || uSwapped == byteOrderMark)
        {
            hadByteOrderMark_ = true;
            startOffset = 2;
            if (uSwapped == byteOrderMark)
                swapBytes_ = true;
        }
        else if (u == swappedByteOrderMark || uSwapped == swappedByteOrderMark)
        {
            hadByteOrderMark_ = true;
            startOffset = 2;
            if (u == swappedByteOrderMark)
                swapBytes_ = true;
        }

        if (hadByteOrderMark_ || subCodingSystem_ == null)
            subCodingSystem_ = new UTF16CodingSystem();

        subDecoder_.operatorAssign(subCodingSystem_.makeDecoder(swapBytes_));
        minBytesPerChar_ = subDecoder_.pointer()!.minBytesPerChar();

        // Decode remaining bytes
        if (startOffset > 0)
        {
            byte[] remaining = new byte[fromLen - startOffset];
            for (nuint i = 0; i < fromLen - startOffset; i++)
                remaining[i] = from[startOffset + i];

            nuint subUsed;
            nuint result = subDecoder_.pointer()!.decode(to, remaining, fromLen - startOffset, out subUsed);
            fromUsed = startOffset + subUsed;
            return result;
        }
        else
        {
            return subDecoder_.pointer()!.decode(to, from, fromLen, out fromUsed);
        }
    }

    // Boolean convertOffset(unsigned long &n) const;
    public override Boolean convertOffset(ref ulong n)
    {
        subDecoder_.pointer()?.convertOffset(ref n);
        if (hadByteOrderMark_)
            n += 2;
        return true;
    }
}

public class UnicodeEncoder : Encoder
{
    private const ushort byteOrderMark = 0xfeff;
    private Owner<Encoder> subEncoder_ = new Owner<Encoder>();

    // UnicodeEncoder();
    public UnicodeEncoder()
    {
        UTF16CodingSystem utf16 = new UTF16CodingSystem();
        subEncoder_.operatorAssign(utf16.makeEncoder());
    }

    // void startFile(OutputByteStream *sb);
    public override void startFile(OutputByteStream sb)
    {
        // Write byte order mark
        sb.sputc(unchecked((sbyte)(byteOrderMark >> 8)));
        sb.sputc(unchecked((sbyte)(byteOrderMark & 0xff)));
    }

    // void output(const Char *, size_t, OutputByteStream *);
    public override void output(Char[] s, nuint n, OutputByteStream sb)
    {
        subEncoder_.pointer()!.output(s, n, sb);
    }
}

public class UnicodeCodingSystem : CodingSystem
{
    private InputCodingSystem? sub_;

    // UnicodeCodingSystem(const InputCodingSystem *sub = 0);
    public UnicodeCodingSystem(InputCodingSystem? sub = null)
    {
        sub_ = sub;
    }

    // Decoder *makeDecoder() const;
    public override Decoder makeDecoder()
    {
        return new UnicodeDecoder(sub_);
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        return new UnicodeEncoder();
    }
}
