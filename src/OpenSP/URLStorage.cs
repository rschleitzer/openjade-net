// Copyright (c) 1995 James Clark
// See the file COPYING for copying permission.

// FIXME This implementation won't work on an EBCDIC machine.

using System.Net.Http;
using System.Net.Sockets;

namespace OpenSP;

// URL storage manager using .NET HttpClient
public class URLStorageManager : IdStorageManager
{
    private string type_;
    private static readonly CharsetInfo iso646Charset_;

    static URLStorageManager()
    {
        // ISO 646 charset: descMin=0, descMax=127, univMin=0
        UnivCharsetDesc desc = new UnivCharsetDesc();
        desc.addRange(0, 127, 0);
        iso646Charset_ = new CharsetInfo(desc);
    }

    // URLStorageManager(const char *type);
    public URLStorageManager(string type)
        : base(iso646Charset_)
    {
        type_ = type;
    }

    // const char *type() const;
    public override string type()
    {
        return type_;
    }

    // Boolean guessIsId(const StringC &, const CharsetInfo &) const;
    public override Boolean guessIsId(StringC id, CharsetInfo charset)
    {
        if (id.size() < 8)
            return false;
        string prefix = "http://";
        for (int i = 0; i < prefix.Length; i++)
        {
            char c = (char)id[(nuint)i];
            char p = prefix[i];
            if (c != p && char.ToLower(c) != p)
                return false;
        }
        return true;
    }

    // StorageObject *makeStorageObject(const StringC &specId, const StringC &baseId, Boolean,
    //                                   Boolean mayRewind, Messenger &, StringC &found);
    public override StorageObject? makeStorageObject(StringC specId,
                                                      StringC baseId,
                                                      Boolean search,
                                                      Boolean mayRewind,
                                                      Messenger mgr,
                                                      StringC id)
    {
        id.assign(specId.data()!, specId.size());
        resolveRelative(baseId, id, false);

        // Validate HTTP URL
        if (id.size() < 5
            || !matchesInsensitive(id, 0, "http:"))
        {
            mgr.message(URLStorageMessages.onlyHTTP);
            return null;
        }

        if (id.size() < 7 || id[5] != '/' || id[6] != '/')
        {
            mgr.message(URLStorageMessages.badRelative, new StringMessageArg(id));
            return null;
        }

        // Parse URL
        nuint i = 7;
        string host = "";
        while (i < id.size())
        {
            if (id[i] == '/' || id[i] == ':')
                break;
            host += (char)id[i];
            i++;
        }

        if (host.Length == 0)
        {
            mgr.message(URLStorageMessages.emptyHost, new StringMessageArg(id));
            return null;
        }

        ushort port = 80;
        if (i < id.size() && id[i] == ':')
        {
            i++;
            string digits = "";
            while (i < id.size() && id[i] != '/')
            {
                digits += (char)id[i];
                i++;
            }
            if (digits.Length == 0)
            {
                mgr.message(URLStorageMessages.emptyPort, new StringMessageArg(id));
                return null;
            }
            if (!ushort.TryParse(digits, out port))
            {
                mgr.message(URLStorageMessages.invalidPort, new StringMessageArg(id));
                return null;
            }
        }

        string path = "";
        if (i < id.size())
        {
            while (i < id.size() && id[i] != '#')
            {
                path += (char)id[i];
                i++;
            }
        }
        if (path.Length == 0)
            path = "/";

        // Build URL string
        string urlString = stringCToString(id);
        int hashPos = urlString.IndexOf('#');
        if (hashPos >= 0)
            urlString = urlString.Substring(0, hashPos);

        try
        {
            return new HttpStorageObject(urlString, mayRewind, id);
        }
        catch (Exception ex)
        {
            mgr.message(URLStorageMessages.cannotConnect,
                new StringMessageArg(new StringC(host)),
                new StringMessageArg(new StringC(ex.Message)));
            return null;
        }
    }

    // Boolean resolveRelative(const StringC &base, StringC &specId, Boolean) const;
    public override Boolean resolveRelative(StringC baseId, StringC id, Boolean syntactic)
    {
        string schemeChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+-.";

        // If it has a scheme, it is absolute
        for (nuint i = 0; i < id.size(); i++)
        {
            if (id[i] == ':')
            {
                if (i == 0)
                    break;
                else
                    return true;
            }
            else if (schemeChars.IndexOf((char)id[i]) < 0)
                break;
        }

        // Count leading slashes
        nuint slashCount = 0;
        for (nuint i = 0; i < id.size(); i++)
        {
            if (id[i] != '/')
                break;
            slashCount++;
        }

        if (slashCount > 0)
        {
            // Find same slash count in base
            bool foundSameSlash = false;
            nuint sameSlashPos = 0;
            for (nuint j = 0; j < baseId.size(); j++)
            {
                nuint thisSlashCount = 0;
                for (nuint k = j; k < baseId.size() && baseId[k] == '/'; k++)
                    thisSlashCount++;
                if (thisSlashCount == slashCount && !foundSameSlash)
                {
                    foundSameSlash = true;
                    sameSlashPos = j;
                }
                else if (thisSlashCount > slashCount)
                    foundSameSlash = false;
            }
            if (foundSameSlash)
            {
                StringC tem = new StringC(baseId.data()!, sameSlashPos);
                for (nuint i = 0; i < id.size(); i++)
                    tem.operatorPlusAssign(id[i]);
                id.assign(tem.data()!, tem.size());
            }
        }
        else
        {
            // Find last slash in base
            nuint j;
            for (j = baseId.size(); j > 0; j--)
                if (baseId[j - 1] == '/')
                    break;
            if (j > 0)
            {
                StringC tem = new StringC(baseId.data()!, j);
                for (nuint i = 0; i < id.size(); i++)
                    tem.operatorPlusAssign(id[i]);
                id.assign(tem.data()!, tem.size());
            }
        }
        // FIXME remove xxx/../, and /.
        return true;
    }

    // Boolean transformNeutral(StringC &str, Boolean fold, Messenger &) const;
    public override Boolean transformNeutral(StringC str, Boolean fold, Messenger mgr)
    {
        if (fold)
        {
            for (nuint i = 0; i < str.size(); i++)
            {
                Char c = str[i];
                if (c <= 255)
                    str[i] = (Char)char.ToLower((char)c);
            }
        }
        return true;
    }

    private static bool matchesInsensitive(StringC s, nuint start, string prefix)
    {
        if (s.size() - start < (nuint)prefix.Length)
            return false;
        for (int i = 0; i < prefix.Length; i++)
        {
            char sc = (char)s[start + (nuint)i];
            char pc = prefix[i];
            if (char.ToLower(sc) != char.ToLower(pc))
                return false;
        }
        return true;
    }

    private static string stringCToString(StringC s)
    {
        char[] chars = new char[s.size()];
        for (nuint i = 0; i < s.size(); i++)
            chars[i] = (char)s[i];
        return new string(chars);
    }
}

// HTTP storage object using .NET HttpClient
internal class HttpStorageObject : RewindStorageObject
{
    private HttpClient? client_;
    private Stream? responseStream_;
    private StringC url_;
    private Boolean eof_;
    private string urlString_;

    public HttpStorageObject(string url, Boolean mayRewind, StringC urlC)
        : base(mayRewind, false) // HTTP streams generally don't support seeking
    {
        urlString_ = url;
        url_ = new StringC(urlC);
        eof_ = false;

        client_ = new HttpClient();
        // Set timeout and user agent
        client_.Timeout = TimeSpan.FromSeconds(30);
        client_.DefaultRequestHeaders.Add("User-Agent", "OpenSP.NET/1.0");
    }

    public override Boolean read(byte[] buf, nuint bufSize, Messenger mgr, out nuint nread)
    {
        nread = 0;

        if (readSaved(buf, bufSize, out nread))
            return true;

        if (eof_)
            return false;

        // Lazy initialization of stream
        if (responseStream_ == null)
        {
            try
            {
                // Synchronous HTTP GET - blocking call
                var response = client_!.GetAsync(urlString_).GetAwaiter().GetResult();
                
                if (!response.IsSuccessStatusCode)
                {
                    string reason = response.ReasonPhrase ?? "Unknown error";
                    string path = new Uri(urlString_).PathAndQuery;
                    string host = new Uri(urlString_).Host;
                    new ParentLocationMessenger(mgr).message(URLStorageMessages.getFailed,
                        new StringMessageArg(new StringC(host)),
                        new StringMessageArg(new StringC(path)),
                        new StringMessageArg(new StringC(reason)));
                    eof_ = true;
                    return false;
                }

                responseStream_ = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            }
            catch (HttpRequestException ex)
            {
                new ParentLocationMessenger(mgr).message(URLStorageMessages.cannotConnect,
                    new StringMessageArg(url_),
                    new StringMessageArg(new StringC(ex.Message)));
                eof_ = true;
                return false;
            }
            catch (Exception ex)
            {
                new ParentLocationMessenger(mgr).message(URLStorageMessages.readError,
                    new StringMessageArg(url_),
                    new StringMessageArg(new StringC(ex.Message)));
                eof_ = true;
                return false;
            }
        }

        try
        {
            int n = responseStream_!.Read(buf, 0, (int)bufSize);
            if (n > 0)
            {
                nread = (nuint)n;
                saveBytes(buf, nread);
                return true;
            }
            eof_ = true;
            responseStream_.Dispose();
            responseStream_ = null;
            client_?.Dispose();
            client_ = null;
        }
        catch (Exception ex)
        {
            new ParentLocationMessenger(mgr).message(URLStorageMessages.readError,
                new StringMessageArg(url_),
                new StringMessageArg(new StringC(ex.Message)));
            responseStream_?.Dispose();
            responseStream_ = null;
            client_?.Dispose();
            client_ = null;
        }
        return false;
    }

    public override nuint getBlockSize()
    {
        return 8192;
    }

    protected override Boolean seekToStart(Messenger mgr)
    {
        // HTTP streams don't support seeking
        throw new InvalidOperationException("CANNOT_HAPPEN");
    }
}
