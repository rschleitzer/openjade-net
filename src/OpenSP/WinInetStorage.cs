// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

using System.Net.Http;

namespace OpenSP;

// WinInet-style storage manager using .NET HttpClient
public class WinInetStorageManager : IdStorageManager, IDisposable
{
    private string type_;
    private HttpClient? session_;
    private static readonly CharsetInfo iso646Charset_;

    static WinInetStorageManager()
    {
        // ISO 646 charset: descMin=0, descMax=127, univMin=0
        UnivCharsetDesc desc = new UnivCharsetDesc();
        desc.addRange(0, 127, 0);
        iso646Charset_ = new CharsetInfo(desc);
    }

    // WinInetStorageManager(const char *type);
    public WinInetStorageManager(string type)
        : base(iso646Charset_)
    {
        type_ = type;
        session_ = null;
    }

    // ~WinInetStorageManager();
    public void Dispose()
    {
        if (session_ != null)
        {
            session_.Dispose();
            session_ = null;
        }
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
        nuint i = 0;
        // guess other schemes supported by download protocols
        string prefix = "http://";
        foreach (char s in prefix)
        {
            Char c = id[i];
            Char p = charset.execToDesc((sbyte)s);
            Char pu = char.IsLower(s) ? charset.execToDesc((sbyte)char.ToUpper(s)) : p;
            if (c != p && c != pu)
                return false;
            i++;
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
        if (!initSession())
            return null;

        id.assign(specId.data()!, specId.size());
        resolveRelative(baseId, id, false);

        string urlString = toAscii(id);
        
        try
        {
            return new WinInetStorageObject(session_!, urlString, mayRewind, id);
        }
        catch (Exception ex)
        {
            mgr.message(WinInetStorageMessages.cannotOpen,
                new StringMessageArg(id),
                new StringMessageArg(new StringC(ex.Message)));
            return null;
        }
    }

    // Boolean resolveRelative(const StringC &base, StringC &specId, Boolean) const;
    public override Boolean resolveRelative(StringC baseId, StringC id, Boolean syntactic)
    {
        if (baseId.size() == 0 || id.size() == 0)
            return false;

        try
        {
            string baseStr = toAscii(baseId);
            string idStr = toAscii(id);

            // Use Uri class to combine URLs
            Uri baseUri = new Uri(baseStr);
            Uri combinedUri = new Uri(baseUri, idStr);

            string result = combinedUri.ToString();
            id.resize(0);
            foreach (char c in result)
                id.operatorPlusAssign((Char)c);

            return true;
        }
        catch
        {
            return false;
        }
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

    private Boolean initSession()
    {
        if (session_ == null)
        {
            session_ = new HttpClient();
            session_.Timeout = TimeSpan.FromSeconds(30);
            session_.DefaultRequestHeaders.Add("User-Agent", "SP");
        }
        return true;
    }

    private static string toAscii(StringC buf)
    {
        char[] chars = new char[buf.size()];
        for (nuint i = 0; i < buf.size(); i++)
            chars[i] = (char)buf[i];
        return new string(chars);
    }
}

// WinInet storage object using .NET HttpClient
internal class WinInetStorageObject : RewindStorageObject
{
    private HttpClient client_;
    private Stream? responseStream_;
    private Boolean eof_;
    private StringC url_;
    private string urlString_;

    public WinInetStorageObject(HttpClient client, string url, Boolean mayRewind, StringC urlC)
        : base(mayRewind, false)
    {
        client_ = client;
        urlString_ = url;
        url_ = new StringC(urlC);
        eof_ = false;
    }

    // Boolean read(char *buf, size_t bufSize, Messenger &mgr, size_t &nread);
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
                var response = client_.GetAsync(urlString_).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    string reason = response.ReasonPhrase ?? "Unknown error";
                    mgr.message(WinInetStorageMessages.cannotOpen,
                        new StringMessageArg(url_),
                        new StringMessageArg(new StringC(reason)));
                    eof_ = true;
                    return false;
                }
                responseStream_ = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                mgr.message(WinInetStorageMessages.readFailed,
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
        }
        catch (Exception ex)
        {
            mgr.message(WinInetStorageMessages.readFailed,
                new StringMessageArg(url_),
                new StringMessageArg(new StringC(ex.Message)));
            responseStream_?.Dispose();
            responseStream_ = null;
        }
        return false;
    }

    public override nuint getBlockSize()
    {
        return 8192;
    }

    // Boolean seekToStart(Messenger &);
    protected override Boolean seekToStart(Messenger mgr)
    {
        // HTTP streams don't support seeking
        throw new InvalidOperationException("CANNOT_HAPPEN");
    }
}
