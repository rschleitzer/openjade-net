// Copyright (c) 1994, 1995 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Storage messages for StdioStorage
public static class StdioStorageMessages
{
    // E2+openFailed++cannot open %1 (%2)
    public static readonly MessageType2 openFailed = new MessageType2(
        MessageType.Severity.error,
        new MessageModule(),
        2400,
        "cannot open %1 (%2)"
    );

    // E2+readFailed++error reading %1 (%2)
    public static readonly MessageType2 readFailed = new MessageType2(
        MessageType.Severity.error,
        new MessageModule(),
        2401,
        "error reading %1 (%2)"
    );

    // E2+seekFailed++error seeking %1 (%2)
    public static readonly MessageType2 seekFailed = new MessageType2(
        MessageType.Severity.error,
        new MessageModule(),
        2402,
        "error seeking %1 (%2)"
    );
}

public class StdioStorageObject : StorageObject, IDisposable
{
    private System.IO.FileStream? fp_;
    private StringC filename_;
    private String<sbyte> filenameBytes_;
    private bool disposed_ = false;

    // StdioStorageObject(FILE *fp, const StringC &filename);
    public StdioStorageObject(System.IO.FileStream fp, StringC filename)
    {
        fp_ = fp;
        filename_ = filename;
        filenameBytes_ = new String<sbyte>();
    }

    // ~StdioStorageObject();
    ~StdioStorageObject()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed_)
        {
            if (fp_ != null)
            {
                fp_.Close();
                fp_ = null;
            }
            disposed_ = true;
        }
    }

    // Boolean read(char *buf, size_t bufSize, Messenger &mgr, size_t &nread);
    public override Boolean read(byte[] buf, nuint bufSize, Messenger mgr, out nuint nread)
    {
        nread = 0;
        if (fp_ == null)
            return false;

        try
        {
            nuint n = 0;
            while (n < bufSize)
            {
                int c = fp_.ReadByte();
                if (c == -1) // EOF
                {
                    fp_.Close();
                    fp_ = null;
                    break;
                }
                buf[n++] = (byte)c;
            }
            nread = n;
            return n > 0;
        }
        catch (System.IO.IOException ex)
        {
            error(mgr, StdioStorageMessages.readFailed, ex.HResult);
            fp_?.Close();
            fp_ = null;
            return false;
        }
    }

    // Boolean rewind(Messenger &mgr);
    public override Boolean rewind(Messenger mgr)
    {
        if (fp_ != null)
        {
            try
            {
                fp_.Seek(0, System.IO.SeekOrigin.Begin);
                return true;
            }
            catch (System.IO.IOException ex)
            {
                error(mgr, StdioStorageMessages.seekFailed, ex.HResult);
                return false;
            }
        }
        return true;
    }

    // size_t getBlockSize() const;
    public override nuint getBlockSize()
    {
        return 4096; // BUFSIZ equivalent
    }

    // void error(Messenger &mgr, const MessageType2 &, int err);
    private void error(Messenger mgr, MessageType2 msg, int err)
    {
        new ParentLocationMessenger(mgr).message(msg,
                                                  new StringMessageArg(filename_),
                                                  new ErrnoMessageArg(err));
    }
}

public class StdioStorageManager : IdStorageManager
{
    private string type_;
    private OutputCodingSystem? filenameCodingSystem_;

    // StdioStorageManager(const char *type, const CharsetInfo *filenameCharset, const OutputCodingSystem *filenameCodingSystem);
    public StdioStorageManager(string type, CharsetInfo? filenameCharset, OutputCodingSystem? filenameCodingSystem)
        : base(filenameCharset)
    {
        type_ = type;
        filenameCodingSystem_ = filenameCodingSystem;
    }

    // StorageObject *makeStorageObject(const StringC &str, const StringC &, Boolean, Boolean, Messenger &mgr, StringC &filename);
    public override StorageObject? makeStorageObject(StringC specId,
                                                      StringC baseId,
                                                      Boolean search,
                                                      Boolean mayRewind,
                                                      Messenger mgr,
                                                      StringC actualId)
    {
        actualId.assign(specId.data()!, specId.size());

        // Convert StringC filename to byte string
        string filenameStr;
        if (filenameCodingSystem_ != null)
        {
            String<sbyte> filenameBytes = filenameCodingSystem_.convertOut(actualId);
            filenameStr = convertToString(filenameBytes);
        }
        else
        {
            filenameStr = stringCToString(actualId);
        }

        try
        {
            System.IO.FileStream fp = new System.IO.FileStream(filenameStr,
                                                                System.IO.FileMode.Open,
                                                                System.IO.FileAccess.Read);
            return new StdioStorageObject(fp, actualId);
        }
        catch (System.IO.IOException ex)
        {
            new ParentLocationMessenger(mgr).message(StdioStorageMessages.openFailed,
                                                     new StringMessageArg(actualId),
                                                     new ErrnoMessageArg(ex.HResult));
            return null;
        }
    }

    // const char *type() const;
    public override string type()
    {
        return type_;
    }

    // Helper to convert String<sbyte> to C# string
    private static string convertToString(String<sbyte> s)
    {
        if (s.size() == 0)
            return "";
        byte[] bytes = new byte[s.size()];
        for (nuint i = 0; i < s.size(); i++)
        {
            sbyte c = s[i];
            if (c == 0) break; // null terminator
            bytes[i] = (byte)c;
        }
        return System.Text.Encoding.Latin1.GetString(bytes);
    }

    // Helper to convert StringC to string (for simple ASCII filenames)
    private static string stringCToString(StringC s)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (nuint i = 0; i < s.size(); i++)
        {
            Char c = s[i];
            if (c < 128)
                sb.Append((char)c);
            else
                sb.Append('?'); // Non-ASCII
        }
        return sb.ToString();
    }
}
