// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

internal class LiteralStorageObject : StorageObject
{
    private StringC str_;
    private nuint nBytesRead_;

    // LiteralStorageObject(const StringC &);
    public LiteralStorageObject(StringC str)
    {
        str_ = new StringC(str);
        nBytesRead_ = 0;
    }

    // Boolean read(char *buf, size_t bufSize, Messenger &, size_t &nread);
    public override Boolean read(byte[] buf, nuint bufSize, Messenger mgr, out nuint nread)
    {
        nuint totalBytes = str_.size() * sizeof(Char);
        if (nBytesRead_ >= totalBytes)
        {
            nread = 0;
            return false;
        }
        nread = totalBytes - nBytesRead_;
        if (nread > bufSize)
            nread = bufSize;

        // Copy bytes from StringC data
        nuint charOffset = nBytesRead_ / sizeof(Char);
        nuint byteInChar = nBytesRead_ % sizeof(Char);
        nuint bytesWritten = 0;

        while (bytesWritten < nread)
        {
            Char c = str_[charOffset];
            while (byteInChar < sizeof(Char) && bytesWritten < nread)
            {
                buf[bytesWritten++] = (byte)((c >> (int)(byteInChar * 8)) & 0xFF);
                byteInChar++;
            }
            byteInChar = 0;
            charOffset++;
        }

        nBytesRead_ += nread;
        return true;
    }

    // Boolean rewind(Messenger &);
    public override Boolean rewind(Messenger mgr)
    {
        nBytesRead_ = 0;
        return true;
    }
}

internal class MemoryInputCodingSystem : InputCodingSystem
{
    // Decoder *makeDecoder() const;
    public override Decoder makeDecoder()
    {
        return new MemoryDecoder();
    }
}

internal class MemoryDecoder : Decoder
{
    // MemoryDecoder();
    public MemoryDecoder() : base(sizeof(Char))
    {
    }

    // size_t decode(Char *, const char *, size_t, const char **);
    public override nuint decode(Char[] to, byte[] from, nuint fromLen, out nuint fromUsed)
    {
        nuint nChars = fromLen / sizeof(Char);
        fromUsed = nChars * sizeof(Char);

        // Copy bytes to Char array
        for (nuint i = 0; i < nChars; i++)
        {
            nuint byteOffset = i * sizeof(Char);
            Char c = 0;
            for (nuint j = 0; j < sizeof(Char); j++)
            {
                c |= (Char)from[byteOffset + j] << (int)(j * 8);
            }
            to[i] = c;
        }

        return nChars;
    }
}

public class LiteralStorageManager : StorageManager
{
    private string type_;

    // LiteralStorageManager(const char *type);
    public LiteralStorageManager(string type)
    {
        type_ = type;
    }

    // StorageObject *makeStorageObject(const StringC &id,
    //                                  const StringC &,
    //                                  Boolean,
    //                                  Boolean mayRewind,
    //                                  Messenger &,
    //                                  StringC &found);
    public override StorageObject? makeStorageObject(StringC id,
                                                     StringC baseId,
                                                     Boolean search,
                                                     Boolean mayRewind,
                                                     Messenger mgr,
                                                     StringC foundId)
    {
        foundId.operatorAssign(id);
        return new LiteralStorageObject(id);
    }

    // const InputCodingSystem *requiredCodingSystem() const;
    public override InputCodingSystem? requiredCodingSystem()
    {
        return new MemoryInputCodingSystem();
    }

    // Boolean requiresCr() const;
    public override Boolean requiresCr()
    {
        return true;
    }

    // const char *type() const;
    public override string type()
    {
        return type_;
    }

    // Boolean inheritable() const;
    public override Boolean inheritable()
    {
        return false;
    }
}
