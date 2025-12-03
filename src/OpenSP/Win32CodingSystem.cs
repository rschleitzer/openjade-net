// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

using System.Text;

namespace OpenSP;

// Windows code page based coding system using .NET Encoding class
public class Win32CodingSystem : CodingSystem
{
    public enum SpecialCodePage
    {
        codePageOEM,
        codePageAnsi
    }

    private int codePage_;
    private Char defaultChar_;
    private Encoding? encoding_;

    // Win32CodingSystem(unsigned int codePage, Char defaultChar = 0xfffd);
    public Win32CodingSystem(int codePage, Char defaultChar = 0xfffd)
    {
        codePage_ = codePage;
        defaultChar_ = defaultChar;
        try
        {
            encoding_ = Encoding.GetEncoding(codePage);
        }
        catch
        {
            encoding_ = null;
        }
    }

    // Win32CodingSystem(SpecialCodePage, Char defaultChar = 0xfffd);
    public Win32CodingSystem(SpecialCodePage codePage, Char defaultChar = 0xfffd)
    {
        defaultChar_ = defaultChar;
        if (codePage == SpecialCodePage.codePageAnsi)
        {
            // Windows ANSI code page - typically 1252 on Western systems
            codePage_ = Encoding.Default.CodePage;
        }
        else
        {
            // OEM code page - typically 437 or 850
            codePage_ = 437;
        }
        try
        {
            encoding_ = Encoding.GetEncoding(codePage_);
        }
        catch
        {
            encoding_ = null;
        }
    }

    // Boolean isValid() const;
    public Boolean isValid()
    {
        return encoding_ != null;
    }

    // Decoder *makeDecoder() const;
    public override Decoder makeDecoder()
    {
        if (encoding_ != null && encoding_.IsSingleByte)
            return new SingleByteWin32Decoder(encoding_, defaultChar_);
        else
            return new MultiByteWin32Decoder(encoding_, defaultChar_);
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        return new Win32Encoder(encoding_);
    }
}

// Single-byte decoder for Windows code pages
internal class SingleByteWin32Decoder : Decoder
{
    private Char[] map_;

    public SingleByteWin32Decoder(Encoding? encoding, Char defaultChar)
    {
        map_ = new Char[256];
        for (int i = 0; i < 256; i++)
        {
            byte[] bytes = new byte[] { (byte)i };
            try
            {
                if (encoding != null)
                {
                    char[] chars = encoding.GetChars(bytes);
                    if (chars.Length > 0)
                        map_[i] = chars[0];
                    else
                        map_[i] = defaultChar;
                }
                else
                {
                    map_[i] = defaultChar;
                }
            }
            catch
            {
                map_[i] = defaultChar;
            }
        }
    }

    // size_t decode(Char *to, const char *from, size_t fromLen, const char **rest);
    public override nuint decode(Char[] result, byte[] input, nuint inputLen, out nuint inputUsed)
    {
        nuint count = 0;
        for (nuint i = 0; i < inputLen; i++)
        {
            result[count] = map_[input[i]];
            count++;
        }
        inputUsed = inputLen;
        return count;
    }

    // Boolean convertOffset(unsigned long &offset) const;
    public override Boolean convertOffset(ref ulong offset)
    {
        return true;
    }
}

// Multi-byte decoder for Windows code pages
internal class MultiByteWin32Decoder : Decoder
{
    private Encoding? encoding_;
    private Char defaultChar_;

    public MultiByteWin32Decoder(Encoding? encoding, Char defaultChar)
    {
        encoding_ = encoding;
        defaultChar_ = defaultChar;
    }

    // size_t decode(Char *to, const char *from, size_t fromLen, const char **rest);
    public override nuint decode(Char[] result, byte[] input, nuint inputLen, out nuint inputUsed)
    {
        if (encoding_ == null || inputLen == 0)
        {
            inputUsed = 0;
            return 0;
        }

        try
        {
            char[] chars = encoding_.GetChars(input, 0, (int)inputLen);
            nuint count = (nuint)chars.Length;
            for (nuint i = 0; i < count; i++)
                result[i] = chars[i];

            inputUsed = inputLen;
            return count;
        }
        catch
        {
            // Fall back to byte-by-byte decoding
            nuint count = 0;
            for (nuint i = 0; i < inputLen; i++)
            {
                try
                {
                    byte[] bytes = new byte[] { input[i] };
                    char[] chars = encoding_.GetChars(bytes);
                    if (chars.Length > 0)
                        result[count++] = chars[0];
                    else
                        result[count++] = defaultChar_;
                }
                catch
                {
                    result[count++] = defaultChar_;
                }
            }
            inputUsed = inputLen;
            return count;
        }
    }
}

// Encoder for Windows code pages
internal class Win32Encoder : Encoder
{
    private Encoding? encoding_;

    public Win32Encoder(Encoding? encoding)
    {
        encoding_ = encoding;
    }

    // void output(const Char *, size_t, OutputByteStream *);
    public override void output(Char[] chars, nuint len, OutputByteStream stream)
    {
        if (len == 0 || encoding_ == null)
            return;

        char[] charArray = new char[len];
        for (nuint i = 0; i < len; i++)
            charArray[i] = (char)chars[i];

        byte[] bytes = encoding_.GetBytes(charArray);
        // Convert byte[] to sbyte[] for sputn
        sbyte[] sbytes = new sbyte[bytes.Length];
        Buffer.BlockCopy(bytes, 0, sbytes, 0, bytes.Length);
        stream.sputn(sbytes, (nuint)sbytes.Length);
    }
}
