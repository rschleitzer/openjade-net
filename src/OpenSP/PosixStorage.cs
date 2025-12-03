// Copyright (c) 1994, 1995 James Clark
// See the file COPYING for copying permission.

using System.Runtime.InteropServices;

namespace OpenSP;

// Storage messages for PosixStorage
public static class PosixStorageMessages
{
    // E2+readSystemCall++error reading %1 (%2)
    public static readonly MessageType2 readSystemCall = new MessageType2(
        MessageType.Severity.error,
        new MessageModule(),
        2200,
        "error reading %1 (%2)"
    );

    // E2+openSystemCall++cannot open %1 (%2)
    public static readonly MessageType2 openSystemCall = new MessageType2(
        MessageType.Severity.error,
        new MessageModule(),
        2201,
        "cannot open %1 (%2)"
    );

    // E2+closeSystemCall++error closing %1 (%2)
    public static readonly MessageType2 closeSystemCall = new MessageType2(
        MessageType.Severity.error,
        new MessageModule(),
        2202,
        "error closing %1 (%2)"
    );

    // E2+lseekSystemCall++error seeking on %1 (%2)
    public static readonly MessageType2 lseekSystemCall = new MessageType2(
        MessageType.Severity.error,
        new MessageModule(),
        2203,
        "error seeking on %1 (%2)"
    );

    // E1+invalidFilename++invalid filename %1
    public static readonly MessageType1 invalidFilename = new MessageType1(
        MessageType.Severity.error,
        new MessageModule(),
        2204,
        "invalid filename %1"
    );

    // E2+fdRead++error reading file descriptor %1 (%2)
    public static readonly MessageType2 fdRead = new MessageType2(
        MessageType.Severity.error,
        new MessageModule(),
        2205,
        "error reading file descriptor %1 (%2)"
    );

    // E2+fdLseek++error seeking on file descriptor %1 (%2)
    public static readonly MessageType2 fdLseek = new MessageType2(
        MessageType.Severity.error,
        new MessageModule(),
        2206,
        "error seeking on file descriptor %1 (%2)"
    );

    // E1+invalidNumber++%1 is not a valid file descriptor number
    public static readonly MessageType1 invalidNumber = new MessageType1(
        MessageType.Severity.error,
        new MessageModule(),
        2207,
        "%1 is not a valid file descriptor number"
    );

    // E2+cannotFind++cannot find %1; tried %2
    public static readonly MessageType2 cannotFind = new MessageType2(
        MessageType.Severity.error,
        new MessageModule(),
        2208,
        "cannot find %1; tried %2"
    );
}

public class PosixStorageObject : RewindStorageObject, IDisposable
{
    private const nuint defaultBlockSize = 8192;

    private System.IO.FileStream? fd_;
    private PackedBoolean eof_;
    private StringC filename_;
    private string cfilename_;
    private bool disposed_ = false;

    // PosixStorageObject(int fd, const StringC &, const String<FChar> &, Boolean mayRewind, DescriptorManager *);
    public PosixStorageObject(System.IO.FileStream fd,
                              StringC filename,
                              string cfilename,
                              Boolean mayRewind)
        : base(mayRewind, canSeek(fd))
    {
        fd_ = fd;
        eof_ = false;
        filename_ = filename;
        cfilename_ = cfilename;
    }

    // ~PosixStorageObject();
    ~PosixStorageObject()
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
            if (fd_ != null)
            {
                fd_.Close();
                fd_ = null;
            }
            disposed_ = true;
        }
    }

    private static Boolean canSeek(System.IO.FileStream fd)
    {
        return fd.CanSeek;
    }

    // Boolean seekToStart(Messenger &);
    protected override Boolean seekToStart(Messenger mgr)
    {
        eof_ = false;
        if (fd_ == null)
            return false;
        try
        {
            fd_.Seek(0, System.IO.SeekOrigin.Begin);
            return true;
        }
        catch (System.IO.IOException)
        {
            return false;
        }
    }

    // Boolean read(char *buf, size_t bufSize, Messenger &mgr, size_t &nread);
    public override Boolean read(byte[] buf, nuint bufSize, Messenger mgr, out nuint nread)
    {
        if (readSaved(buf, bufSize, out nread))
            return true;
        if (fd_ == null || eof_)
        {
            nread = 0;
            return false;
        }
        try
        {
            int n = fd_.Read(buf, 0, (int)bufSize);
            if (n > 0)
            {
                nread = (nuint)n;
                saveBytes(buf, nread);
                return true;
            }
            else
            {
                eof_ = true;
                if (!mayRewind_)
                {
                    fd_.Close();
                    fd_ = null;
                }
                nread = 0;
                return false;
            }
        }
        catch (System.IO.IOException ex)
        {
            systemError(mgr, PosixStorageMessages.readSystemCall, ex.HResult);
            fd_?.Close();
            fd_ = null;
            nread = 0;
            return false;
        }
    }

    // size_t getBlockSize() const;
    public override nuint getBlockSize()
    {
        return defaultBlockSize;
    }

    // void willNotRewind();
    public new void willNotRewind()
    {
        mayRewind_ = false;
        if (eof_ && fd_ != null)
        {
            fd_.Close();
            fd_ = null;
        }
    }

    // void systemError(Messenger &, const MessageType2 &, int);
    private void systemError(Messenger mgr, MessageType2 msg, int err)
    {
        new ParentLocationMessenger(mgr).message(msg,
                                                  new StringMessageArg(filename_),
                                                  new ErrnoMessageArg(err));
    }
}

public class PosixStorageManager : IdStorageManager
{
    private string type_;
    private OutputCodingSystem? filenameCodingSystem_;
    private Vector<StringC> searchDirs_ = new Vector<StringC>();
    private Boolean restrictFileReading_;

    // PosixStorageManager(const char *type, const CharsetInfo *filenameCharset, const OutputCodingSystem *filenameCodingSystem, int maxFDs, Boolean restrictFileReading);
    public PosixStorageManager(string type,
                               CharsetInfo? filenameCharset,
                               OutputCodingSystem? filenameCodingSystem,
                               int maxFDs,
                               Boolean restrictFileReading)
        : base(filenameCharset)
    {
        type_ = type;
        filenameCodingSystem_ = filenameCodingSystem;
        restrictFileReading_ = restrictFileReading;

        // Set newline in reString
        if (idCharset() != null)
        {
            Char newline = idCharset()!.execToDesc((sbyte)'\n');
            Char[] newlineArr = new Char[] { newline };
            reString_.assign(newlineArr, 1);
        }
    }

    // const char *type() const;
    public override string type()
    {
        return type_;
    }

    // void addSearchDir(const StringC &str);
    public void addSearchDir(StringC str)
    {
        searchDirs_.push_back(str);
    }

    // Boolean isAbsolute(const StringC &) const;
    public Boolean isAbsolute(StringC file)
    {
        if (file.size() == 0)
            return false;
        // Cross-platform: use .NET's Path class
        string path = stringCToString(file);
        return System.IO.Path.IsPathRooted(path);
    }

    // StringC extractDir(const StringC &) const;
    public StringC extractDir(StringC str)
    {
        string path = stringCToString(str);
        string? dir = System.IO.Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir))
            return new StringC();
        return stringToStringC(dir + System.IO.Path.DirectorySeparatorChar);
    }

    // StringC combineDir(const StringC &dir, const StringC &base) const;
    public StringC combineDir(StringC dir, StringC baseName)
    {
        string dirStr = stringCToString(dir);
        string baseStr = stringCToString(baseName);
        string combined = System.IO.Path.Combine(dirStr, baseStr);
        return stringToStringC(combined);
    }

    // Boolean transformNeutral(StringC &str, Boolean fold, Messenger &) const;
    public override Boolean transformNeutral(StringC str, Boolean fold, Messenger mgr)
    {
        if (fold)
        {
            for (nuint i = 0; i < str.size(); i++)
            {
                Char c = str[i];
                if (c <= 255 && c >= 'A' && c <= 'Z')
                    str[i] = (Char)(c + 32); // to lowercase
            }
        }
        return true;
    }

    // Boolean resolveRelative(const StringC &baseId, StringC &specId, Boolean search) const;
    public override Boolean resolveRelative(StringC baseId, StringC specId, Boolean search)
    {
        if (isAbsolute(specId))
            return true;
        if (!search || searchDirs_.size() == 0)
        {
            StringC combined = combineDir(extractDir(baseId), specId);
            specId.assign(combined.data()!, combined.size());
            return true;
        }
        return false;
    }

    // Boolean isSafe(const StringC &file) const;
    private Boolean isSafe(StringC file)
    {
        // Check for directory traversal attempts
        for (nuint i = 0; i < file.size(); i++)
        {
            if (file[i] == '.' && i > 0 && file[(nuint)(i - 1)] == '.')
                return false;
        }

        // Validate characters are safe
        for (nuint i = 0; i < file.size(); i++)
        {
            Char c = file[i];
            bool valid = (c >= 'a' && c <= 'z')
                      || (c >= 'A' && c <= 'Z')
                      || (c >= '0' && c <= '9')
                      || c == '/'
                      || c == '.'
                      || c == '-'
                      || c == '_'
                      || c == System.IO.Path.DirectorySeparatorChar
                      || c == ':';
            if (!valid)
                return false;
        }

        StringC dir = extractDir(file);

        // Check if within search directories
        for (nuint i = 0; i < searchDirs_.size(); i++)
        {
            StringC searchDir = searchDirs_[i];
            if (dir.size() >= searchDir.size())
            {
                nuint j;
                for (j = 0; j < searchDir.size(); j++)
                {
                    if (searchDir[j] != dir[j])
                        break;
                }
                if (j == searchDir.size() &&
                    (dir.size() == searchDir.size() ||
                     dir[j] == '/' ||
                     dir[j] == System.IO.Path.DirectorySeparatorChar))
                    return true;
            }
        }

        return false;
    }

    // StorageObject *makeStorageObject(const StringC &spec, const StringC &base, Boolean search, Boolean mayRewind, Messenger &mgr, StringC &found);
    public override StorageObject? makeStorageObject(StringC spec,
                                                      StringC @base,
                                                      Boolean search,
                                                      Boolean mayRewind,
                                                      Messenger mgr,
                                                      StringC found)
    {
        if (spec.size() == 0)
        {
            mgr.message(PosixStorageMessages.invalidFilename,
                        new StringMessageArg(spec));
            return null;
        }

        Boolean absolute = isAbsolute(spec);
        SearchResultMessageArg sr = new SearchResultMessageArg();

        for (nuint i = 0; i < searchDirs_.size() + 1; i++)
        {
            StringC filename;
            if (absolute)
                filename = new StringC(spec);
            else if (i == 0)
                filename = combineDir(extractDir(@base), spec);
            else
                filename = combineDir(searchDirs_[(nuint)(i - 1)], spec);

            if (restrictFileReading_ && !isSafe(filename))
                continue;

            string cfilename = stringCToString(filename);

            try
            {
                System.IO.FileStream fd = new System.IO.FileStream(cfilename,
                                                                    System.IO.FileMode.Open,
                                                                    System.IO.FileAccess.Read,
                                                                    System.IO.FileShare.Read);
                found.assign(filename.data()!, filename.size());
                return new PosixStorageObject(fd, filename, cfilename, mayRewind);
            }
            catch (System.IO.IOException ex)
            {
                int savedErrno = ex.HResult;
                if ((absolute || !search || searchDirs_.size() == 0) && !restrictFileReading_)
                {
                    new ParentLocationMessenger(mgr).message(PosixStorageMessages.openSystemCall,
                                                             new StringMessageArg(filename),
                                                             new ErrnoMessageArg(savedErrno));
                    return null;
                }
                if (!restrictFileReading_)
                    sr.add(filename, savedErrno);
            }
        }

        new ParentLocationMessenger(mgr).message(PosixStorageMessages.cannotFind,
                                                 new StringMessageArg(spec), sr);
        return null;
    }

    // Helper to convert StringC to string
    private static string stringCToString(StringC s)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (nuint i = 0; i < s.size(); i++)
        {
            Char c = s[i];
            if (c < 128)
                sb.Append((char)c);
            else if (c < 0x10000)
                sb.Append((char)c);
            else
                sb.Append('?'); // Character outside BMP
        }
        return sb.ToString();
    }

    // Helper to convert string to StringC
    private static StringC stringToStringC(string s)
    {
        StringC result = new StringC();
        foreach (char c in s)
        {
            result.operatorPlusAssign((Char)c);
        }
        return result;
    }
}

// File descriptor storage manager (for stdin, etc.)
public class PosixFdStorageManager : IdStorageManager
{
    private string type_;

    // PosixFdStorageManager(const char *type, const CharsetInfo *idCharset);
    public PosixFdStorageManager(string type, CharsetInfo? charset)
        : base(charset)
    {
        type_ = type;
    }

    // Boolean inheritable() const;
    public override Boolean inheritable()
    {
        return false;
    }

    // const char *type() const;
    public override string type()
    {
        return type_;
    }

    // StorageObject *makeStorageObject(const StringC &id, const StringC &, Boolean, Boolean mayRewind, Messenger &mgr, StringC &foundId);
    public override StorageObject? makeStorageObject(StringC id,
                                                      StringC baseId,
                                                      Boolean search,
                                                      Boolean mayRewind,
                                                      Messenger mgr,
                                                      StringC foundId)
    {
        // Parse file descriptor number
        int n = 0;
        nuint i;
        for (i = 0; i < id.size(); i++)
        {
            UnivChar ch;
            if (idCharset() == null || !idCharset()!.descToUniv(id[i], out ch))
                break;
            if (ch < UnivCharsetDesc.zero || ch > UnivCharsetDesc.zero + 9)
                break;
            int digit = (int)(ch - UnivCharsetDesc.zero);
            if (n > int.MaxValue / 10)
                break;
            n *= 10;
            if (n > int.MaxValue - digit)
                break;
            n += digit;
        }
        if (i < id.size() || i == 0)
        {
            mgr.message(PosixStorageMessages.invalidNumber,
                        new StringMessageArg(id));
            return null;
        }
        foundId.assign(id.data()!, id.size());

        // Open the file descriptor (primarily for stdin = 0)
        System.IO.Stream? stream = null;
        if (n == 0)
            stream = Console.OpenStandardInput();
        else if (n == 1)
            stream = Console.OpenStandardOutput();
        else if (n == 2)
            stream = Console.OpenStandardError();

        if (stream == null)
        {
            mgr.message(PosixStorageMessages.invalidNumber,
                        new StringMessageArg(id));
            return null;
        }

        return new PosixFdStorageObject(stream, n, mayRewind);
    }
}

public class PosixFdStorageObject : RewindStorageObject
{
    private System.IO.Stream? fd_;
    private PackedBoolean eof_;
    private int origFd_;

    // PosixFdStorageObject(int fd, Boolean mayRewind);
    public PosixFdStorageObject(System.IO.Stream fd, int origFd, Boolean mayRewind)
        : base(mayRewind, fd.CanSeek)
    {
        fd_ = fd;
        origFd_ = origFd;
        eof_ = false;
    }

    // Boolean seekToStart(Messenger &);
    protected override Boolean seekToStart(Messenger mgr)
    {
        if (fd_ == null || !fd_.CanSeek)
            return false;
        try
        {
            eof_ = false;
            fd_.Seek(0, System.IO.SeekOrigin.Begin);
            return true;
        }
        catch (System.IO.IOException ex)
        {
            new ParentLocationMessenger(mgr).message(PosixStorageMessages.fdLseek,
                                                     new NumberMessageArg((ulong)origFd_),
                                                     new ErrnoMessageArg(ex.HResult));
            return false;
        }
    }

    // Boolean read(char *buf, size_t bufSize, Messenger &mgr, size_t &nread);
    public override Boolean read(byte[] buf, nuint bufSize, Messenger mgr, out nuint nread)
    {
        if (readSaved(buf, bufSize, out nread))
            return true;
        if (fd_ == null || eof_)
        {
            nread = 0;
            return false;
        }
        try
        {
            int n = fd_.Read(buf, 0, (int)bufSize);
            if (n > 0)
            {
                nread = (nuint)n;
                saveBytes(buf, nread);
                return true;
            }
            else
            {
                eof_ = true;
                nread = 0;
                return false;
            }
        }
        catch (System.IO.IOException ex)
        {
            new ParentLocationMessenger(mgr).message(PosixStorageMessages.fdRead,
                                                     new NumberMessageArg((ulong)origFd_),
                                                     new ErrnoMessageArg(ex.HResult));
            fd_ = null;
            nread = 0;
            return false;
        }
    }
}
