// Copyright (c) 1994, 1995 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public abstract class RewindStorageObject : StorageObject
{
    protected PackedBoolean mayRewind_;
    private PackedBoolean savingBytes_;
    private PackedBoolean readingSaved_;
    private PackedBoolean canSeek_;
    private String<sbyte> savedBytes_ = new String<sbyte>();
    private nuint nBytesRead_;

    // RewindStorageObject(Boolean mayRewind, Boolean canSeek);
    public RewindStorageObject(Boolean mayRewind, Boolean canSeek)
    {
        mayRewind_ = mayRewind;
        canSeek_ = canSeek;
        savingBytes_ = mayRewind && !canSeek;
        readingSaved_ = false;
    }

    // void saveBytes(const char *, size_t);
    protected void saveBytes(sbyte[] s, nuint n)
    {
        if (savingBytes_)
            savedBytes_.append(s, n);
    }

    // Overload for byte array
    protected void saveBytes(byte[] s, nuint n)
    {
        if (savingBytes_)
        {
            sbyte[] sb = new sbyte[n];
            for (nuint i = 0; i < n; i++)
                sb[i] = (sbyte)s[i];
            savedBytes_.append(sb, n);
        }
    }

    // Boolean readSaved(char *, size_t, size_t &);
    protected Boolean readSaved(byte[] buf, nuint bufSize, out nuint nread)
    {
        if (!readingSaved_)
        {
            nread = 0;
            return false;
        }
        if (nBytesRead_ >= savedBytes_.size())
        {
            if (!mayRewind_)
            {
                // Ensure that memory is released now.
                String<sbyte> tem = new String<sbyte>();
                tem.swap(savedBytes_);
            }
            readingSaved_ = false;
            nread = 0;
            return false;
        }
        nread = savedBytes_.size() - nBytesRead_;
        if (nread > bufSize)
            nread = bufSize;
        for (nuint i = 0; i < nread; i++)
            buf[i] = (byte)savedBytes_[(nuint)(nBytesRead_ + i)];
        nBytesRead_ += nread;
        return true;
    }

    // Boolean rewind(Messenger &);
    public override Boolean rewind(Messenger mgr)
    {
        if (canSeek_)
            return seekToStart(mgr);
        else
        {
            readingSaved_ = true;
            nBytesRead_ = 0;
            return true;
        }
    }

    // void willNotRewind();
    protected new void willNotRewind()
    {
        mayRewind_ = false;
        savingBytes_ = false;
        if (!readingSaved_)
        {
            // Ensure that memory is released now.
            String<sbyte> tem = new String<sbyte>();
            tem.swap(savedBytes_);
        }
    }

    // void unread(const char *s, size_t n);
    protected void unread(sbyte[] s, nuint n)
    {
        savedBytes_.append(s, n);
        if (!readingSaved_)
        {
            readingSaved_ = true;
            nBytesRead_ = 0;
        }
    }

    // Overload for byte array
    protected void unread(byte[] s, nuint n)
    {
        sbyte[] sb = new sbyte[n];
        for (nuint i = 0; i < n; i++)
            sb[i] = (sbyte)s[i];
        savedBytes_.append(sb, n);
        if (!readingSaved_)
        {
            readingSaved_ = true;
            nBytesRead_ = 0;
        }
    }

    // virtual Boolean seekToStart(Messenger &) = 0;
    protected abstract Boolean seekToStart(Messenger mgr);
}
