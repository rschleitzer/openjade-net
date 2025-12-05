// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

using System;

namespace OpenSP;

public abstract class OutputCharStream : Link
{
    public enum Newline { newline }

    // typedef void (*Escaper)(OutputCharStream &, Char);
    public delegate void Escaper(OutputCharStream stream, Char c);

    protected Char[]? ptr_;
    protected nuint ptrIndex_;
    protected nuint endIndex_;

    // OutputCharStream();
    public OutputCharStream()
    {
        ptr_ = null;
        ptrIndex_ = 0;
        endIndex_ = 0;
    }

    // virtual ~OutputCharStream();
    // C# GC handles cleanup

    // OutputCharStream &put(Char);
    public OutputCharStream put(Char c)
    {
        if (ptr_ != null && ptrIndex_ < endIndex_)
            ptr_[ptrIndex_++] = c;
        else
            flushBuf(c);
        return this;
    }

    // OutputCharStream &write(const Char *, size_t);
    public OutputCharStream write(Char[] s, nuint n)
    {
        nuint srcIndex = 0;
        for (;;)
        {
            nuint spare = endIndex_ - ptrIndex_;
            if (n <= spare)
            {
                if (ptr_ != null)
                {
                    Array.Copy(s, (int)srcIndex, ptr_, (int)ptrIndex_, (int)n);
                    ptrIndex_ += n;
                }
                break;
            }
            if (spare > 0 && ptr_ != null)
            {
                Array.Copy(s, (int)srcIndex, ptr_, (int)ptrIndex_, (int)spare);
                ptrIndex_ += spare;
                srcIndex += spare;
                n -= spare;
            }
            n--;
            flushBuf(s[srcIndex++]);
        }
        return this;
    }

    // virtual void flush() = 0;
    public abstract void flush();

    // virtual void setEscaper(Escaper);
    public virtual void setEscaper(Escaper? escaper)
    {
    }

    // OutputCharStream &operator<<(char);
    public OutputCharStream operatorOutput(sbyte c)
    {
        return put((Char)c);
    }

    // OutputCharStream &operator<<(const char *);
    public OutputCharStream operatorOutput(string s)
    {
        foreach (char c in s)
            put((Char)c);
        return this;
    }

    // Convenience method: put(string) - equivalent to operatorOutput
    public OutputCharStream put(string s)
    {
        return operatorOutput(s);
    }

    // Convenience method: write(string) - equivalent to operatorOutput
    public OutputCharStream write(string s)
    {
        return operatorOutput(s);
    }

    // Convenience method: put(StringC) - equivalent to operatorOutput
    public OutputCharStream put(StringC str)
    {
        return operatorOutput(str);
    }

    // OutputCharStream &operator<<(const StringC &);
    public OutputCharStream operatorOutput(StringC str)
    {
        Char[] data = new Char[str.size()];
        for (nuint i = 0; i < str.size(); i++)
            data[i] = str[i];
        return write(data, str.size());
    }

    // OutputCharStream &operator<<(unsigned long);
    public OutputCharStream operatorOutput(ulong n)
    {
        return operatorOutput(n.ToString());
    }

    // OutputCharStream &operator<<(int);
    public OutputCharStream operatorOutput(int n)
    {
        return operatorOutput(n.ToString());
    }

    // OutputCharStream &operator<<(Newline);
    public OutputCharStream operatorOutput(Newline nl)
    {
        // Use system line terminator
        put((Char)'\n');
        return this;
    }

    // virtual void flushBuf(Char) = 0;
    protected abstract void flushBuf(Char c);
}

public class EncodeOutputCharStream : OutputCharStream, IDisposable
{
    private Char[]? buf_;
    private OutputByteStream? byteStream_;
    private Encoder? encoder_;
    private Owner<Encoder> ownedEncoder_ = new Owner<Encoder>();
    private Escaper? escaper_;

    // Private handler class
    private class UnencodableHandler : Encoder.Handler
    {
        private EncodeOutputCharStream parent_;

        public UnencodableHandler(EncodeOutputCharStream parent)
        {
            parent_ = parent;
        }

        public override void handleUnencodable(Char c, OutputByteStream stream)
        {
            parent_.handleUnencodable(c, stream);
        }
    }

    // EncodeOutputCharStream();
    public EncodeOutputCharStream()
    {
        buf_ = null;
        byteStream_ = null;
        escaper_ = null;
    }

    // EncodeOutputCharStream(OutputByteStream *, const OutputCodingSystem *);
    public EncodeOutputCharStream(OutputByteStream byteStream, OutputCodingSystem codingSystem)
    {
        buf_ = null;
        byteStream_ = byteStream;
        escaper_ = null;
        ownedEncoder_ = new Owner<Encoder>(codingSystem.makeEncoder());
        encoder_ = ownedEncoder_.pointer();
        encoder_!.setUnencodableHandler(new UnencodableHandler(this));
        allocBuf((int)codingSystem.fixedBytesPerChar());
        encoder_!.startFile(byteStream_);
    }

    // Overload for CodingSystem (which includes both input and output)
    public EncodeOutputCharStream(OutputByteStream byteStream, CodingSystem codingSystem)
    {
        buf_ = null;
        byteStream_ = byteStream;
        escaper_ = null;
        ownedEncoder_ = new Owner<Encoder>(codingSystem.makeEncoder());
        encoder_ = ownedEncoder_.pointer();
        encoder_!.setUnencodableHandler(new UnencodableHandler(this));
        allocBuf((int)codingSystem.fixedBytesPerChar());
        encoder_!.startFile(byteStream_);
    }

    // ~EncodeOutputCharStream();
    public void Dispose()
    {
        if (byteStream_ != null)
            flush();
        buf_ = null;
        GC.SuppressFinalize(this);
    }

    ~EncodeOutputCharStream()
    {
        if (byteStream_ != null)
            flush();
    }

    // void open(OutputByteStream *, const OutputCodingSystem *);
    public void open(OutputByteStream byteStream, OutputCodingSystem codingSystem)
    {
        if (byteStream_ != null)
            flush();
        byteStream_ = byteStream;
        ownedEncoder_ = new Owner<Encoder>(codingSystem.makeEncoder());
        encoder_ = ownedEncoder_.pointer();
        encoder_!.setUnencodableHandler(new UnencodableHandler(this));
        buf_ = null;
        ptr_ = buf_;
        ptrIndex_ = 0;
        endIndex_ = 0;
        allocBuf((int)codingSystem.fixedBytesPerChar());
        encoder_!.startFile(byteStream_);
    }

    // void flush();
    public override void flush()
    {
        if (buf_ != null && ptrIndex_ > 0 && encoder_ != null)
        {
            encoder_.output(buf_, ptrIndex_, byteStream_!);
            ptrIndex_ = 0;
        }
        byteStream_?.flush();
    }

    // void flushBuf(Char);
    protected override void flushBuf(Char c)
    {
        if (buf_ != null && encoder_ != null)
        {
            encoder_.output(buf_, ptrIndex_, byteStream_!);
            ptrIndex_ = 0;
            buf_[ptrIndex_++] = c;
        }
    }

    // void allocBuf(int bytesPerChar);
    private void allocBuf(int bytesPerChar)
    {
        const int blockSize = 1024;
        nuint bufSize = bytesPerChar > 0 ? (nuint)(blockSize / bytesPerChar) : (nuint)blockSize;
        ptr_ = buf_ = new Char[bufSize];
        ptrIndex_ = 0;
        endIndex_ = bufSize;
    }

    // void setEscaper(Escaper);
    public override void setEscaper(Escaper? f)
    {
        escaper_ = f;
    }

    // void handleUnencodable(Char c, OutputByteStream *);
    private void handleUnencodable(Char c, OutputByteStream stream)
    {
        EncodeOutputCharStream tem = new EncodeOutputCharStream();
        tem.byteStream_ = byteStream_;
        tem.encoder_ = encoder_;
        tem.allocBuf(0);
        if (escaper_ != null)
            escaper_(tem, c);
    }
}

public class StrOutputCharStream : OutputCharStream
{
    private Char[]? buf_;
    private nuint bufSize_;

    // StrOutputCharStream();
    public StrOutputCharStream()
    {
        buf_ = null;
        bufSize_ = 0;
        sync(0);
    }

    // ~StrOutputCharStream();
    // C# GC handles cleanup

    // void extractString(StringC &);
    public void extractString(StringC str)
    {
        str.assign(buf_!, 0, ptrIndex_);
        sync(0);
    }

    // void flush();
    public override void flush()
    {
    }

    // void flushBuf(Char);
    protected override void flushBuf(Char c)
    {
        nuint used = ptrIndex_;
        nuint oldSize = bufSize_;
        bufSize_ = oldSize > 0 ? 2 * oldSize : 10;
        Char[] oldBuf = buf_!;
        buf_ = new Char[bufSize_];
        if (oldSize > 0 && oldBuf != null)
        {
            Array.Copy(oldBuf, buf_, (int)oldSize);
        }
        sync(used);
        buf_![ptrIndex_++] = c;
    }

    // void sync(size_t length);
    private void sync(nuint length)
    {
        ptr_ = buf_;
        ptrIndex_ = length;
        endIndex_ = bufSize_;
    }
}

public class RecordOutputCharStream : OutputCharStream, IDisposable
{
    private OutputCharStream? os_;
    private const int bufSize_ = 1024;
    private Char[] buf_ = new Char[bufSize_];

    // RecordOutputCharStream(OutputCharStream *);
    public RecordOutputCharStream(OutputCharStream os)
    {
        os_ = os;
        ptr_ = buf_;
        ptrIndex_ = 0;
        endIndex_ = (nuint)bufSize_;
    }

    // ~RecordOutputCharStream();
    public void Dispose()
    {
        outputBuf();
        os_ = null;
        GC.SuppressFinalize(this);
    }

    ~RecordOutputCharStream()
    {
        outputBuf();
    }

    // void setEscaper(Escaper);
    public override void setEscaper(Escaper? f)
    {
        os_?.setEscaper(f);
    }

    // void flush();
    public override void flush()
    {
        outputBuf();
        os_?.flush();
    }

    // void flushBuf(Char);
    protected override void flushBuf(Char c)
    {
        outputBuf();
        buf_[ptrIndex_++] = c;
    }

    // void outputBuf();
    private void outputBuf()
    {
        if (os_ == null) return;

        nuint start = 0;
        nuint p = 0;
        while (p < ptrIndex_)
        {
            switch (buf_[p])
            {
                case '\r': // translate RE to newline
                    if (start < p)
                    {
                        Char[] temp = new Char[p - start];
                        Array.Copy(buf_, (int)start, temp, 0, (int)(p - start));
                        os_.write(temp, p - start);
                    }
                    start = ++p;
                    os_.operatorOutput(Newline.newline);
                    break;
                case '\n': // ignore RS
                    if (start < p)
                    {
                        Char[] temp = new Char[p - start];
                        Array.Copy(buf_, (int)start, temp, 0, (int)(p - start));
                        os_.write(temp, p - start);
                    }
                    start = ++p;
                    break;
                default:
                    ++p;
                    break;
            }
        }
        if (start < p)
        {
            Char[] temp = new Char[p - start];
            Array.Copy(buf_, (int)start, temp, 0, (int)(p - start));
            os_.write(temp, p - start);
        }
        ptr_ = buf_;
        ptrIndex_ = 0;
        endIndex_ = (nuint)bufSize_;
    }
}
