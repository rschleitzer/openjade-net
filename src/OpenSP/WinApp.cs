// Copyright (c) 1997 James Clark
// See the file COPYING for copying permission.

using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace OpenSP;

// Windows application helper for SGML parser configuration
public class WinApp
{
    private const string SP_DEFAULT_ENCODING = "WINDOWS";
    private const string SP_REGISTRY_KEY = "Software\\James Clark\\SP";

    public ParserOptions options = new ParserOptions();
    private StringC extraCatalogs_ = new StringC();
    private StringC defaultCatalogs_ = new StringC();
    private StringC extraDirectories_ = new StringC();
    private StringC defaultDirectories_ = new StringC();
    private StringC encoding_ = new StringC();
    private StringC defaultEncoding_ = new StringC();
    private ConstPtr<InputCodingSystemKit> codingSystemKit_ = new ConstPtr<InputCodingSystemKit>();

    // WinApp();
    public WinApp()
    {
        getRegistry("Catalogs", defaultCatalogs_);
        getRegistry("Directories", defaultDirectories_);
        getRegistry("Encoding", defaultEncoding_);
        if (defaultEncoding_.size() == 0 || getCodingSystem(defaultEncoding_) == null)
        {
            defaultEncoding_ = asStringC(SP_DEFAULT_ENCODING);
        }
    }

    // Boolean setEncoding(StringC &tem);
    public Boolean setEncoding(StringC str)
    {
        if (getCodingSystem(str) == null)
            return false;
        str.swap(encoding_);
        return true;
    }

    // const StringC &encoding() const;
    public StringC encoding()
    {
        return encoding_;
    }

    // Boolean setDefaultEncoding(StringC &tem);
    public Boolean setDefaultEncoding(StringC str)
    {
        if (getCodingSystem(str) == null)
            return false;
        str.swap(defaultEncoding_);
        setRegistry("Encoding", defaultEncoding_);
        return true;
    }

    // const StringC &defaultEncoding() const;
    public StringC defaultEncoding()
    {
        return defaultEncoding_;
    }

    // void setExtraCatalogs(StringC &tem);
    public void setExtraCatalogs(StringC str)
    {
        str.swap(extraCatalogs_);
    }

    // const StringC &extraCatalogs() const;
    public StringC extraCatalogs()
    {
        return extraCatalogs_;
    }

    // void setDefaultCatalogs(StringC &tem);
    public void setDefaultCatalogs(StringC tem)
    {
        if (!tem.operatorEquals(defaultCatalogs_))
        {
            tem.swap(defaultCatalogs_);
            setRegistry("Catalogs", defaultCatalogs_);
        }
    }

    // const StringC &defaultCatalogs() const;
    public StringC defaultCatalogs()
    {
        return defaultCatalogs_;
    }

    // void setExtraDirectories(StringC &tem);
    public void setExtraDirectories(StringC str)
    {
        str.swap(extraDirectories_);
    }

    // const StringC &extraDirectories() const;
    public StringC extraDirectories()
    {
        return extraDirectories_;
    }

    // void setDefaultDirectories(StringC &tem);
    public void setDefaultDirectories(StringC tem)
    {
        if (!tem.operatorEquals(defaultDirectories_))
        {
            tem.swap(defaultDirectories_);
            setRegistry("Directories", defaultDirectories_);
        }
    }

    // const StringC &defaultDirectories() const;
    public StringC defaultDirectories()
    {
        return defaultDirectories_;
    }

    // void initParser(const StringC &, SgmlParser &);
    public void initParser(StringC sysid, SgmlParser parser)
    {
        Ptr<EntityManager> em = new Ptr<EntityManager>();
        initParser(sysid, parser, em);
    }

    // void initParser(const StringC &sysid, SgmlParser &, Ptr<EntityManager> &);
    public void initParser(StringC sysid, SgmlParser parser, Ptr<EntityManager> em)
    {
        InputCodingSystem? inputCodingSystem = getCodingSystem(encoding_);
        if (inputCodingSystem == null)
            inputCodingSystem = getCodingSystem(defaultEncoding_);

        // Cast to CodingSystem if possible (most coding systems are full CodingSystem)
        CodingSystem? codingSystem = inputCodingSystem as CodingSystem;

        ConstPtr<InputCodingSystemKit> icsk = new ConstPtr<InputCodingSystemKit>();
        icsk.swap(codingSystemKit_);
        CharsetInfo systemCharset = icsk.pointer()!.systemCharset();

        PosixStorageManager sm = new PosixStorageManager("OSFILE",
                                                          systemCharset,
                                                          codingSystem,
                                                          5,
                                                          false);

        Vector<StringC> dirs = new Vector<StringC>();
        split(extraDirectories_, ';', dirs);
        split(defaultDirectories_, ';', dirs);
        for (nuint i = 0; i < dirs.size(); i++)
            sm.addSearchDir(dirs[i]);

        ExtendEntityManager xem = ExtendEntityManager.make(sm,
                                                            inputCodingSystem,
                                                            icsk,
                                                            false);
        em.operatorAssign(xem);
        xem.registerStorageManager(new PosixFdStorageManager("OSFD", systemCharset));
        xem.registerStorageManager(new WinInetStorageManager("URL"));
        xem.registerStorageManager(new LiteralStorageManager("LITERAL"));

        Vector<StringC> catalogSysids = new Vector<StringC>();
        split(extraCatalogs_, ';', catalogSysids);
        nuint nExtra = catalogSysids.size();
        split(defaultCatalogs_, ';', catalogSysids);
        xem.setCatalogManager(SOCatalogManager.make(catalogSysids,
                                                     nExtra,
                                                     systemCharset,
                                                     systemCharset,
                                                     true));
        SgmlParser.Params @params = new SgmlParser.Params();
        @params.sysid = sysid;
        @params.entityManager.operatorAssign(em.pointer());
        @params.options = options;
        parser.init(@params);
    }

    private static StringC asStringC(string s)
    {
        StringC tem = new StringC();
        if (s != null)
        {
            foreach (char c in s)
                tem.operatorPlusAssign((byte)c);
        }
        return tem;
    }

    private static void split(StringC str, Char sep, Vector<StringC> result)
    {
        Boolean started = false;
        for (nuint i = 0; i < str.size(); i++)
        {
            if (str[i] == sep)
                started = false;
            else
            {
                if (!started)
                {
                    result.resize(result.size() + 1);
                    started = true;
                }
                result.back().operatorPlusAssign(str[i]);
            }
        }
    }

    private InputCodingSystem? getCodingSystem(StringC name)
    {
        if (name.size() == 0)
            return null;
        if (codingSystemKit_.isNull())
            codingSystemKit_ = new ConstPtr<InputCodingSystemKit>(CodingSystemKit.make(null));
        string tem;
        return codingSystemKit_.pointer()!.makeInputCodingSystem(name,
                                                                   codingSystemKit_.pointer()!.systemCharset(),
                                                                   false,
                                                                   out tem);
    }

    private static void setRegistry(string name, StringC value)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(SP_REGISTRY_KEY))
            {
                if (key == null)
                    return;

                // Convert StringC to string
                char[] chars = new char[value.size()];
                for (nuint i = 0; i < value.size(); i++)
                    chars[i] = (char)value[i];
                string strValue = new string(chars);

                key.SetValue(name, strValue, RegistryValueKind.String);
            }
        }
        catch
        {
            // Ignore registry errors
        }
    }

    private static Boolean getRegistry(string name, StringC value)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(SP_REGISTRY_KEY))
            {
                if (key == null)
                    return false;

                object? obj = key.GetValue(name);
                if (obj == null)
                    return false;

                string strValue = obj.ToString()!;
                value.resize(0);
                foreach (char c in strValue)
                    value.operatorPlusAssign((Char)c);

                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
