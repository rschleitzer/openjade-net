// Copyright (c) 1997 James Clark
// See the file COPYING for copying permission.

using System;
using System.IO;
using System.Text;

namespace OpenSP;

public abstract class OutputByteStream : Link
{
    protected sbyte[]? ptr_;
    protected nuint ptrIndex_;
    protected nuint endIndex_;

    // OutputByteStream();
    public OutputByteStream()
    {
        ptr_ = null;
        ptrIndex_ = 0;
        endIndex_ = 0;
    }

    // virtual ~OutputByteStream();
    // C# GC handles cleanup

    // virtual void flush() = 0;
    public abstract void flush();

    // void sputc(char c);
    public void sputc(sbyte c)
    {
        if (ptr_ != null && ptrIndex_ < endIndex_)
            ptr_[ptrIndex_++] = c;
        else
            flushBuf(c);
    }

    // void sputn(const char *, size_t);
    public void sputn(sbyte[] s, nuint n)
    {
        for (nuint i = 0; i < n; i++)
            sputc(s[i]);
    }

    // OutputByteStream &operator<<(char);
    public OutputByteStream operatorOutput(sbyte c)
    {
        sputc(c);
        return this;
    }

    // OutputByteStream &operator<<(unsigned char);
    public OutputByteStream operatorOutput(byte c)
    {
        sputc((sbyte)c);
        return this;
    }

    // OutputByteStream &operator<<(const char *);
    public OutputByteStream operatorOutput(string s)
    {
        foreach (char c in s)
            sputc((sbyte)c);
        return this;
    }

    // OutputByteStream &operator<<(int);
    public OutputByteStream operatorOutput(int n)
    {
        return operatorOutput((long)n);
    }

    // OutputByteStream &operator<<(unsigned);
    public OutputByteStream operatorOutput(uint n)
    {
        return operatorOutput((ulong)n);
    }

    // OutputByteStream &operator<<(long);
    public OutputByteStream operatorOutput(long n)
    {
        return operatorOutput(n.ToString());
    }

    // OutputByteStream &operator<<(unsigned long);
    public OutputByteStream operatorOutput(ulong n)
    {
        return operatorOutput(n.ToString());
    }

    // OutputByteStream &operator<<(const String<char> &);
    public OutputByteStream operatorOutput(String<sbyte> s)
    {
        if (s.data() != null)
            sputn(s.data()!, s.size());
        return this;
    }

    // char *getBufferPtr() const;
    public nuint getBufferPtrIndex()
    {
        return ptrIndex_;
    }

    // size_t getBufferSize() const;
    public nuint getBufferSize()
    {
        return endIndex_ - ptrIndex_;
    }

    // void usedBuffer(size_t);
    public void usedBuffer(nuint n)
    {
        ptrIndex_ += n;
    }

    // virtual void flushBuf(char) = 0;
    public abstract void flushBuf(sbyte c);
}

public class StrOutputByteStream : OutputByteStream
{
    private String<sbyte> buf_ = new String<sbyte>();

    // StrOutputByteStream();
    public StrOutputByteStream()
    {
    }

    // virtual ~StrOutputByteStream() { /* no-op */ };
    // C# GC handles cleanup

    // void extractString(String<char> &);
    public void extractString(String<sbyte> str)
    {
        if (ptr_ != null)
            buf_.resize(ptrIndex_);
        str.resize(0);
        buf_.swap(str);
        ptr_ = null;
        ptrIndex_ = 0;
        endIndex_ = 0;
    }

    // void flush();
    public override void flush()
    {
    }

    // void flushBuf(char);
    public override void flushBuf(sbyte c)
    {
        if (ptr_ == null)
        {
            buf_.resize(16);
            ptr_ = buf_.data();
            ptrIndex_ = 0;
        }
        else
        {
            nuint i = ptrIndex_;
            buf_.resize(buf_.size() * 2);
            ptr_ = buf_.data();
            ptrIndex_ = i;
        }
        endIndex_ = buf_.size();
        ptr_![ptrIndex_++] = c;
    }
}

public class FileOutputByteStream : OutputByteStream, IDisposable
{
    private const int bufSize = 8192;
    private String<sbyte> buf_ = new String<sbyte>();
    private Stream? stream_;
    private Boolean closeStream_;

    // FileOutputByteStream();
    public FileOutputByteStream()
    {
        stream_ = null;
        closeStream_ = false;
    }

    // FileOutputByteStream(int fd, Boolean closeFd = 1);
    public FileOutputByteStream(Stream stream, Boolean closeStream = true)
    {
        stream_ = null;
        closeStream_ = false;
        attach(stream, closeStream);
    }


    // virtual ~FileOutputByteStream();
    public void Dispose()
    {
        close();
        GC.SuppressFinalize(this);
    }

    ~FileOutputByteStream()
    {
        close();
    }

    // Boolean open(const char *);
    public Boolean open(string filename)
    {
        try
        {
            var stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
            return attach(stream);
        }
        catch
        {
            return false;
        }
    }

    // Boolean attach(int fd, Boolean closeFd = 1);
    public Boolean attach(Stream stream, Boolean closeStream = true)
    {
        close();
        stream_ = stream;
        closeStream_ = closeStream;
        return stream_ != null;
    }

    // Boolean close();
    public Boolean close()
    {
        if (stream_ == null)
            return false;
        flush();
        var stream = stream_;
        stream_ = null;
        if (!closeStream_)
            return true;
        try
        {
            stream.Close();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // void flush();
    public override void flush()
    {
        if (buf_.size() == 0)
        {
            if (stream_ == null)
                return;
            buf_.resize((nuint)bufSize);
            ptr_ = buf_.data();
            ptrIndex_ = 0;
            endIndex_ = buf_.size();
        }
        nuint n = ptrIndex_;
        if (n > 0 && stream_ != null)
        {
            byte[] bytes = new byte[n];
            for (nuint i = 0; i < n; i++)
                bytes[i] = (byte)buf_[i];
            stream_.Write(bytes, 0, (int)n);
        }
        ptrIndex_ = 0;
    }

    // void flushBuf(char);
    public override void flushBuf(sbyte c)
    {
        flush();
        if (ptr_ != null)
            ptr_[ptrIndex_++] = c;
    }
}
