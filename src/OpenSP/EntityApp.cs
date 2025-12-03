// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

using System.Runtime.InteropServices;

namespace OpenSP;

// Entity management application base class
public abstract class EntityApp : CmdLineApp
{
    private Vector<string> searchDirs_ = new Vector<string>();
    private Vector<string> catalogSysids_ = new Vector<string>();
    private Boolean mapCatalogDocument_;
    private Boolean restrictFileReading_;
    private Ptr<ExtendEntityManager> entityManager_ = new Ptr<ExtendEntityManager>();

    public static readonly char PATH_SEPARATOR = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

    // EntityApp(const char *requiredInternalCode = 0);
    public EntityApp(string? requiredInternalCode = null)
        : base(requiredInternalCode)
    {
        mapCatalogDocument_ = false;
        restrictFileReading_ = false;

        registerOption('c', "catalog",
                       EntityAppMessages.sysid, EntityAppMessages.cHelp);
        registerOption('C', "catalogs", EntityAppMessages.CHelp);
        registerOption('D', "directory",
                       EntityAppMessages.directory, EntityAppMessages.DHelp);
        registerOption('R', "restricted", EntityAppMessages.RHelp);
    }

    // void processOption(AppChar opt, const AppChar *arg);
    public override void processOption(char opt, string? arg)
    {
        switch (opt)
        {
            case 'c':
                if (arg != null)
                    catalogSysids_.push_back(arg);
                break;
            case 'C':
                mapCatalogDocument_ = true;
                break;
            case 'D':
                if (arg != null)
                    searchDirs_.push_back(arg);
                break;
            case 'R':
                restrictFileReading_ = true;
                break;
            default:
                base.processOption(opt, arg);
                break;
        }
    }

    // int processArguments(int argc, AppChar **argv);
    public override int processArguments(string[] argv)
    {
        StringC sysid = new StringC();
        if (!makeSystemId(argv, sysid))
            return 1;
        return processSysid(sysid);
    }

    // virtual int processSysid(const StringC &) = 0;
    public abstract int processSysid(StringC sysid);

    // Boolean makeSystemId(int nFiles, AppChar *const *files, StringC &result);
    public Boolean makeSystemId(string[] files, StringC result)
    {
        Vector<StringC> filenames = new Vector<StringC>(files.Length == 0 ? 1 : (nuint)files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            if (files[i] == "-")
                filenames[(nuint)i] = convertInput("<OSFD>0");
            else
                filenames[(nuint)i] = convertInput(files[i]);
        }
        if (files.Length == 0)
            filenames[0] = convertInput("<OSFD>0");
        return entityManager().pointer()!.mergeSystemIds(filenames,
                                                          mapCatalogDocument_,
                                                          systemCharset(),
                                                          this,
                                                          result);
    }

    // Ptr<ExtendEntityManager> &entityManager();
    public Ptr<ExtendEntityManager> entityManager()
    {
        if (!entityManager_.isNull())
            return entityManager_;

        PosixStorageManager sm = new PosixStorageManager("OSFILE",
                                                          systemCharset(),
                                                          codingSystem(),
                                                          5,
                                                          restrictFileReading_);

        for (nuint i = 0; i < searchDirs_.size(); i++)
            sm.addSearchDir(convertInput(searchDirs_[i]));

        // Get search path from environment
        string? searchPath = Environment.GetEnvironmentVariable("SGML_SEARCH_PATH");
        if (!string.IsNullOrEmpty(searchPath))
        {
            string[] dirs = searchPath.Split(PATH_SEPARATOR);
            foreach (string dir in dirs)
            {
                if (!string.IsNullOrEmpty(dir))
                    sm.addSearchDir(convertInput(dir));
            }
        }

        entityManager_ = new Ptr<ExtendEntityManager>(
            ExtendEntityManager.make(sm,
                                     codingSystem()!,
                                     inputCodingSystemKit(),
                                     internalCharsetIsDocCharset_));

        entityManager_.pointer()!.registerStorageManager(
            new PosixFdStorageManager("OSFD", systemCharset()));

        entityManager_.pointer()!.registerStorageManager(
            new URLStorageManager("URL"));

        entityManager_.pointer()!.registerStorageManager(
            new LiteralStorageManager("LITERAL"));

        entityManager_.pointer()!.registerStorageManager(
            new NotationStorageManager("CLSID"));

        entityManager_.pointer()!.registerStorageManager(
            new NotationStorageManager("MIMETYPE"));

        // Build catalog sysid list
        Vector<StringC> v = new Vector<StringC>();
        for (nuint i = 0; i < catalogSysids_.size(); i++)
            // filenames specified on command-line must exist
            v.push_back(convertInput(catalogSysids_[i]));

        // Get catalog files from environment
        string? catalogFiles = Environment.GetEnvironmentVariable("SGML_CATALOG_FILES");
        if (!string.IsNullOrEmpty(catalogFiles))
        {
            string[] catalogs = catalogFiles.Split(PATH_SEPARATOR);
            foreach (string catalog in catalogs)
            {
                if (!string.IsNullOrEmpty(catalog))
                    v.push_back(convertInput(catalog));
            }
        }

        string? useDocCatalogStr = Environment.GetEnvironmentVariable("SP_USE_DOCUMENT_CATALOG");
        Boolean useDocCatalog = true;
        if (useDocCatalogStr != null
            && (stringMatches(useDocCatalogStr, "NO")
                || stringMatches(useDocCatalogStr, "0")))
            useDocCatalog = false;

        entityManager_.pointer()!.setCatalogManager(
            SOCatalogManager.make(v,
                                  catalogSysids_.size(),
                                  systemCharset(),
                                  systemCharset(),
                                  useDocCatalog));
        return entityManager_;
    }

    // void clearEntityManager();
    protected void clearEntityManager()
    {
        resetCodingSystemKit();
        entityManager_.clear();
    }
}
