// Copyright (c) 1994, 1995 James Clark
// See the file COPYING for copying permission.

using System.IO;
using System.Runtime.InteropServices;

namespace OpenSP;

// File storage manager using .NET file I/O
public class PosixStorageManager : IdStorageManager
{
    private DescriptorManager descriptorManager_;
    private Boolean restrictFileReading_;
    private CodingSystem? filenameCodingSystem_;
    private string type_;
    private Vector<StringC> searchDirs_ = new Vector<StringC>();

    // PosixStorageManager(const char *type, const CharsetInfo *filenameCharset,
    //                     const OutputCodingSystem *filenameCodingSystem, int maxFDs, Boolean restrictFileReading = 0);
    public PosixStorageManager(string type,
                               CharsetInfo filenameCharset,
                               CodingSystem? filenameCodingSystem,
                               int maxFDs,
                               Boolean restrictFileReading = false)
        : base(filenameCharset)
    {
        type_ = type;
        filenameCodingSystem_ = filenameCodingSystem;
        descriptorManager_ = new DescriptorManager(maxFDs);
        restrictFileReading_ = restrictFileReading;

        Char newline = idCharset()!.execToDesc((sbyte)'\n');
        StringC reStr = new StringC();
        reStr.operatorPlusAssign(newline);
        reString_ = reStr;
    }

    // StorageObject *makeStorageObject(const StringC &id, const StringC &baseId, Boolean search,
    //                                   Boolean mayRewind, Messenger &, StringC &foundId);
    public override StorageObject? makeStorageObject(StringC spec,
                                                      StringC @base,
                                                      Boolean search,
                                                      Boolean mayRewind,
                                                      Messenger mgr,
                                                      StringC found)
    {
        if (spec.size() == 0)
        {
            mgr.message(PosixStorageMessages.invalidFilename, new StringMessageArg(spec));
            return null;
        }

        descriptorManager_.acquireD();
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
                filename = combineDir(searchDirs_[i - 1], spec);

            if (restrictFileReading_ && !isSafe(filename))
                continue;

            string cfilename = stringCToString(filename);

            try
            {
                if (File.Exists(cfilename))
                {
                    FileStream? fs = null;
                    try
                    {
                        fs = new FileStream(cfilename, FileMode.Open, FileAccess.Read, FileShare.Read);
                        found.assign(filename.data()!, filename.size());
                        return new PosixStorageObject(fs, filename, mayRewind, descriptorManager_);
                    }
                    catch (Exception ex)
                    {
                        fs?.Dispose();
                        if ((absolute || !search || searchDirs_.size() == 0) && !restrictFileReading_)
                        {
                            new ParentLocationMessenger(mgr).message(PosixStorageMessages.openSystemCall,
                                new StringMessageArg(filename),
                                new StringMessageArg(new StringC(ex.Message)));
                            descriptorManager_.releaseD();
                            return null;
                        }
                        if (!restrictFileReading_)
                            sr.add(filename, 0);
                    }
                }
                else
                {
                    if ((absolute || !search || searchDirs_.size() == 0) && !restrictFileReading_)
                    {
                        new ParentLocationMessenger(mgr).message(PosixStorageMessages.openSystemCall,
                            new StringMessageArg(filename),
                            new StringMessageArg(new StringC("File not found")));
                        descriptorManager_.releaseD();
                        return null;
                    }
                    if (!restrictFileReading_)
                        sr.add(filename, 0);
                }
            }
            catch (Exception ex)
            {
                if ((absolute || !search || searchDirs_.size() == 0) && !restrictFileReading_)
                {
                    new ParentLocationMessenger(mgr).message(PosixStorageMessages.openSystemCall,
                        new StringMessageArg(filename),
                        new StringMessageArg(new StringC(ex.Message)));
                    descriptorManager_.releaseD();
                    return null;
                }
                if (!restrictFileReading_)
                    sr.add(filename, 0);
            }
        }

        descriptorManager_.releaseD();
        new ParentLocationMessenger(mgr).message(PosixStorageMessages.cannotFind,
            new StringMessageArg(spec), sr);
        return null;
    }

    // Boolean resolveRelative(const StringC &, StringC &, Boolean syntactic = 0) const;
    public override Boolean resolveRelative(StringC baseId, StringC specId, Boolean syntactic = false)
    {
        if (isAbsolute(specId))
            return true;
        if (!syntactic || searchDirs_.size() == 0)
        {
            StringC combined = combineDir(extractDir(baseId), specId);
            specId.assign(combined.data()!, combined.size());
            return true;
        }
        return false;
    }

    // const char *type() const;
    public override string type()
    {
        return type_;
    }

    // void addSearchDir(const StringC &);
    public void addSearchDir(StringC dir)
    {
        searchDirs_.push_back(new StringC(dir));
    }

    // Boolean transformNeutral(StringC &, Boolean fold, Messenger &) const;
    public override Boolean transformNeutral(StringC str, Boolean fold, Messenger mgr)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Convert forward slashes to backslashes on Windows
            for (nuint i = 0; i < str.size(); i++)
            {
                if (str[i] == '/')
                    str[i] = '\\';
            }
        }
        else if (fold)
        {
            // Fold case on Unix
            for (nuint i = 0; i < str.size(); i++)
            {
                Char c = str[i];
                if (c <= 255)
                    str[i] = (Char)char.ToLower((char)c);
            }
        }
        return true;
    }

    // Boolean isAbsolute(const StringC &) const;
    private Boolean isAbsolute(StringC s)
    {
        if (s.size() == 0)
            return false;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: starts with / or \ or has drive letter
            return s[0] == '/' || s[0] == '\\'
                || (s.size() > 1 && s[1] == ':');
        }
        else
        {
            // Unix: starts with /
            return s[0] == '/';
        }
    }

    // Boolean isSafe(const StringC &) const;
    private Boolean isSafe(StringC file)
    {
        for (nuint i = 0; i < file.size(); i++)
        {
            // Reject .. sequences
            if (file[i] == '.' && i > 0 && file[i - 1] == '.')
                return false;

            Char c = file[i];
            bool isSafeChar = (c >= 'a' && c <= 'z')
                           || (c >= 'A' && c <= 'Z')
                           || (c >= '0' && c <= '9')
                           || c == '/'
                           || c == '.'
                           || c == '-'
                           || c == '_';

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                isSafeChar = isSafeChar || c == '\\' || c == ':';

            if (!isSafeChar)
                return false;
        }

        StringC dir = extractDir(file);

        // Check if file is within search directories
        for (nuint i = 0; i < searchDirs_.size(); i++)
        {
            StringC searchDir = searchDirs_[i];
            if (dir.size() >= searchDir.size())
            {
                bool match = true;
                for (nuint j = 0; j < searchDir.size(); j++)
                {
                    Char dc = dir[j];
                    Char sc = searchDir[j];
                    if (sc != dc)
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            // Allow / and \ to match on Windows
                            if (!((sc == '/' || sc == '\\') && (dc == '/' || dc == '\\')))
                            {
                                match = false;
                                break;
                            }
                        }
                        else
                        {
                            match = false;
                            break;
                        }
                    }
                }
                if (match)
                {
                    if (dir.size() == searchDir.size())
                        return true;
                    Char nextChar = dir[searchDir.size()];
                    if (nextChar == '/')
                        return true;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && nextChar == '\\')
                        return true;
                }
            }
        }
        return false;
    }

    // StringC extractDir(const StringC &) const;
    private StringC extractDir(StringC str)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            for (nuint i = str.size(); i > 0; i--)
            {
                if (str[i - 1] == '/' || str[i - 1] == '\\'
                    || (i == 2 && str[i - 1] == ':'))
                    return new StringC(str.data()!, i);
            }
        }
        else
        {
            for (nuint i = str.size(); i > 0; i--)
            {
                if (str[i - 1] == '/')
                    return new StringC(str.data()!, i);
            }
        }
        return new StringC();
    }

    // StringC combineDir(const StringC &, const StringC &) const;
    private StringC combineDir(StringC dir, StringC @base)
    {
        StringC result = new StringC(dir);
        if (dir.size() > 0)
        {
            Char lastChar = dir[dir.size() - 1];
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (lastChar != '/' && lastChar != '\\'
                    && !(dir.size() == 2 && lastChar == ':'))
                    result.operatorPlusAssign((Char)'\\');
            }
            else
            {
                if (lastChar != '/')
                    result.operatorPlusAssign((Char)'/');
            }
        }
        for (nuint i = 0; i < @base.size(); i++)
            result.operatorPlusAssign(@base[i]);
        return result;
    }

    private static string stringCToString(StringC s)
    {
        char[] chars = new char[s.size()];
        for (nuint i = 0; i < s.size(); i++)
            chars[i] = (char)s[i];
        return new string(chars);
    }
}

// File storage object using .NET FileStream
internal class PosixStorageObject : RewindStorageObject, IDisposable
{
    private FileStream? stream_;
    private StringC filename_;
    private Boolean eof_;
    private long startOffset_;
    private DescriptorManager? descriptorManager_;
    private bool disposed_ = false;

    public PosixStorageObject(FileStream stream, StringC filename, Boolean mayRewind, DescriptorManager dm)
        : base(mayRewind, mayRewind && stream.CanSeek)
    {
        stream_ = stream;
        filename_ = new StringC(filename);
        eof_ = false;
        descriptorManager_ = dm;

        if (mayRewind && stream.CanSeek)
            startOffset_ = stream.Position;
        else
            startOffset_ = 0;
    }

    public override Boolean read(byte[] buf, nuint bufSize, Messenger mgr, out nuint nread)
    {
        nread = 0;

        if (readSaved(buf, bufSize, out nread))
            return true;

        if (stream_ == null || eof_)
            return false;

        try
        {
            int n = stream_.Read(buf, 0, (int)bufSize);
            if (n > 0)
            {
                nread = (nuint)n;
                saveBytes(buf, nread);
                return true;
            }
            eof_ = true;
            if (!mayRewind_)
            {
                stream_.Dispose();
                stream_ = null;
                descriptorManager_?.releaseD();
            }
        }
        catch (Exception ex)
        {
            new ParentLocationMessenger(mgr).message(PosixStorageMessages.readSystemCall,
                new StringMessageArg(filename_),
                new StringMessageArg(new StringC(ex.Message)));
            stream_?.Dispose();
            stream_ = null;
            descriptorManager_?.releaseD();
        }
        return false;
    }

    public override nuint getBlockSize()
    {
        return 8192;
    }

    protected override Boolean seekToStart(Messenger mgr)
    {
        if (stream_ == null)
            return false;

        try
        {
            eof_ = false;
            stream_.Seek(startOffset_, SeekOrigin.Begin);
            return true;
        }
        catch (Exception ex)
        {
            new ParentLocationMessenger(mgr).message(PosixStorageMessages.lseekSystemCall,
                new StringMessageArg(filename_),
                new StringMessageArg(new StringC(ex.Message)));
            return false;
        }
    }

    public void Dispose()
    {
        if (!disposed_)
        {
            stream_?.Dispose();
            stream_ = null;
            descriptorManager_?.releaseD();
            disposed_ = true;
        }
    }
}

// File descriptor storage manager
public class PosixFdStorageManager : IdStorageManager
{
    private string type_;

    // PosixFdStorageManager(const char *type, const CharsetInfo *filenameCharset);
    public PosixFdStorageManager(string type, CharsetInfo filenameCharset)
        : base(filenameCharset)
    {
        type_ = type;
    }

    // StorageObject *makeStorageObject(const StringC &id, const StringC &baseId,
    //                                   Boolean, Boolean mayRewind, Messenger &mgr, StringC &foundId);
    public override StorageObject? makeStorageObject(StringC id,
                                                      StringC baseId,
                                                      Boolean search,
                                                      Boolean mayRewind,
                                                      Messenger mgr,
                                                      StringC foundId)
    {
        int n = 0;
        nuint i;
        for (i = 0; i < id.size(); i++)
        {
            UnivChar ch;
            if (!idCharset()!.descToUniv(id[i], out ch))
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
            mgr.message(PosixStorageMessages.invalidNumber, new StringMessageArg(id));
            return null;
        }

        foundId.assign(id.data()!, id.size());

        // Handle standard streams
        Stream? stream = null;
        if (n == 0)
            stream = Console.OpenStandardInput();
        else if (n == 1)
            stream = Console.OpenStandardOutput();
        else if (n == 2)
            stream = Console.OpenStandardError();

        if (stream != null)
            return new PosixFdStorageObject(stream, n, mayRewind);

        mgr.message(PosixStorageMessages.invalidNumber, new StringMessageArg(id));
        return null;
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

// File descriptor storage object
internal class PosixFdStorageObject : RewindStorageObject
{
    private Stream? stream_;
    private int fd_;
    private Boolean eof_;
    private long startOffset_;

    public PosixFdStorageObject(Stream stream, int fd, Boolean mayRewind)
        : base(mayRewind, mayRewind && stream.CanSeek)
    {
        stream_ = stream;
        fd_ = fd;
        eof_ = false;

        if (mayRewind && stream.CanSeek)
            startOffset_ = stream.Position;
        else
            startOffset_ = 0;
    }

    public override Boolean read(byte[] buf, nuint bufSize, Messenger mgr, out nuint nread)
    {
        nread = 0;

        if (readSaved(buf, bufSize, out nread))
            return true;

        if (stream_ == null || eof_)
            return false;

        try
        {
            int n = stream_.Read(buf, 0, (int)bufSize);
            if (n > 0)
            {
                nread = (nuint)n;
                saveBytes(buf, nread);
                return true;
            }
            eof_ = true;
        }
        catch (Exception ex)
        {
            new ParentLocationMessenger(mgr).message(PosixStorageMessages.fdRead,
                new NumberMessageArg((ulong)fd_),
                new StringMessageArg(new StringC(ex.Message)));
            stream_ = null;
        }
        return false;
    }

    public override nuint getBlockSize()
    {
        return 8192;
    }

    protected override Boolean seekToStart(Messenger mgr)
    {
        if (stream_ == null || !stream_.CanSeek)
            return false;

        try
        {
            eof_ = false;
            stream_.Seek(startOffset_, SeekOrigin.Begin);
            return true;
        }
        catch (Exception ex)
        {
            new ParentLocationMessenger(mgr).message(PosixStorageMessages.fdLseek,
                new NumberMessageArg((ulong)fd_),
                new StringMessageArg(new StringC(ex.Message)));
            return false;
        }
    }
}
