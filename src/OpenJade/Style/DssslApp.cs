// Copyright (c) 1996, 1997 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using OpenJade.SPGrove;
using OpenJade.Jade;
using Char = System.UInt32;
using Boolean = System.Boolean;

// DSSSL Application base class
public abstract class DssslApp : GroveApp, IGroveManager
{
    protected int unitsPerInch_;
    protected StringC defaultOutputBasename_;
    private Boolean dssslSpecOption_;
    private StringC dssslSpecSysid_; // system ID of doc
    private StringC dssslSpecId_;    // unique ID in doc
    private System.Collections.Generic.List<StringC> defineVars_;
    private SgmlParser? specParser_;
    private System.Collections.Generic.Dictionary<string, NodePtr> groveTable_;
    private StringC rootSystemId_;
    private bool debugMode_;
    private bool dsssl2_;
    private bool strictMode_;

    public DssslApp(int unitsPerInch)
        : base("unicode")
    {
        unitsPerInch_ = unitsPerInch;
        defaultOutputBasename_ = new StringC();
        dssslSpecOption_ = false;
        dssslSpecSysid_ = new StringC();
        dssslSpecId_ = new StringC();
        defineVars_ = new System.Collections.Generic.List<StringC>();
        specParser_ = null;
        groveTable_ = new System.Collections.Generic.Dictionary<string, NodePtr>();
        rootSystemId_ = new StringC();
        debugMode_ = false;
        dsssl2_ = false;
        strictMode_ = false;
        registerOption('G', null);
        registerOption('2', null);
        registerOption('d', "dsssl_spec");
        registerOption('V', "variable[=value]");
        registerOption('s', null);
    }

    public override void processOption(char opt, string? arg)
    {
        switch (opt)
        {
            case 'G':
                // Grove output mode (enable SGML output)
                break;
            case '2':
                dsssl2_ = true;
                break;
            case 'd':
                dssslSpecId_.resize(0);
                dssslSpecSysid_.assign(arg ?? "");
                dssslSpecOption_ = true;
                splitOffId(ref dssslSpecSysid_, out dssslSpecId_);
                break;
            case 'V':
                {
                    StringC var = new StringC();
                    var.assign(arg ?? "");
                    defineVars_.Add(var);
                }
                break;
            case 's':
                strictMode_ = true;
                break;
            default:
                base.processOption(opt, arg);
                break;
        }
    }

    public abstract FOTBuilder? makeFOTBuilder(out FOTBuilder.ExtensionTableEntry[]? extensions);

    public override int processSysid(StringC sysid)
    {
        rootSystemId_ = sysid;
        return base.processSysid(sysid);
    }

    // IGroveManager implementation
    public bool load(StringC sysid, System.Collections.Generic.List<StringC> active,
                     NodePtr parent, ref NodePtr rootNode,
                     System.Collections.Generic.List<StringC> architecture)
    {
        // Check if grove already loaded
        string sysidKey = sysid.ToString();
        if (groveTable_.TryGetValue(sysidKey, out NodePtr existingNode))
        {
            rootNode = existingNode;
            return true;
        }

        // Create params for parsing
        SgmlParser.Params parms = new SgmlParser.Params();
        parms.sysid = sysid;

        // Create grove builder
        var groveBuilder = GroveBuilder.make((uint)(groveTable_.Count + 1), null, null, false, ref rootNode);

        // Parse the document
        SgmlParser docParser = new SgmlParser(parms);
        foreach (var linkType in active)
            docParser.activateLinkType(linkType);
        docParser.allLinkTypesActivated();
        docParser.parseAll(groveBuilder);

        // Mark the grove as complete
        if (groveBuilder is GroveBuilderMessageEventHandler gbeh)
            gbeh.markComplete();

        // Store in grove table
        groveTable_[sysidKey] = rootNode;
        return true;
    }

    public bool readEntity(StringC sysid, out StringC content)
    {
        content = new StringC();

        // Try to resolve the sysid - check sgml directory next to executable
        string sysidStr = sysid.ToString();

        // Get the directory where the executable is located
        string? exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string? exeDir = System.IO.Path.GetDirectoryName(exePath);

        // Try paths in order:
        // 1. sgml subdirectory next to executable
        // 2. Current directory
        string[] paths = new string[]
        {
            exeDir != null ? System.IO.Path.Combine(exeDir, "sgml", sysidStr) : "",
            sysidStr,
            System.IO.Path.Combine("sgml", sysidStr)
        };

        foreach (string path in paths)
        {
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                try
                {
                    string fileContent = System.IO.File.ReadAllText(path);
                    content = new StringC(fileContent);
                    return true;
                }
                catch
                {
                    // Continue trying other paths
                }
            }
        }

        return false;
    }

    public void mapSysid(ref StringC sysid)
    {
        // Map a sysid according to SYSTEM catalog entries
        // Simplified for now - no-op
    }

    public override void processGrove()
    {
        if (!initSpecParser())
        {
            Console.Error.WriteLine("ERROR: initSpecParser failed");
            return;
        }

        // Create FOT builder
        FOTBuilder? fotb = makeFOTBuilder(out FOTBuilder.ExtensionTableEntry[]? extensions);
        if (fotb == null)
        {
            Console.Error.WriteLine("ERROR: makeFOTBuilder returned null");
            return;
        }

        // Create style engine adapter for GroveManager
        GroveManagerAdapter groveManagerAdapter = new GroveManagerAdapter(this);

        // Create and configure style engine
        StyleEngine se = new StyleEngine(this, groveManagerAdapter, unitsPerInch_, debugMode_,
                                          dsssl2_, strictMode_, extensions);

        // Define variables from command line
        foreach (var varDef in defineVars_)
            se.defineVariable(varDef);

        // Parse the DSSSL specification
        se.parseSpec(specParser_!, systemCharset(), dssslSpecId_, this);

        // Process the document
        se.process(rootNode_, fotb);

        // Close FOT builder if it has a close method
        if (fotb is SgmlFOTBuilder sgmlFotb)
            sgmlFotb.close();
    }

    private Boolean initSpecParser()
    {
        // Check if we have a spec or can get one from the grove
        if (!dssslSpecOption_ && !getDssslSpecFromGrove() && dssslSpecSysid_.size() == 0)
        {
            // message(DssslAppMessages::noSpec);
            return false;
        }

        // Create spec parser params
        SgmlParser.Params parms = new SgmlParser.Params();
        parms.sysid = dssslSpecSysid_;
        parms.entityManager.operatorAssign(entityManager().pointer());
        parms.options = options_;

        specParser_ = new SgmlParser(parms);
        specParser_.allLinkTypesActivated();
        return true;
    }

    private bool getDssslSpecFromGrove()
    {
        // Try to get DSSSL specification from processing instruction in grove
        // For now, return false - would need to search grove for <?xml-stylesheet?>
        return false;
    }

    // Adapter class to bridge IGroveManager to GroveManager
    private class GroveManagerAdapter : GroveManager
    {
        private DssslApp app_;

        public GroveManagerAdapter(DssslApp app)
        {
            app_ = app;
        }

        public override bool load(StringC sysid, System.Collections.Generic.List<StringC> active,
                                   NodePtr parent, ref NodePtr rootNode,
                                   System.Collections.Generic.List<StringC> architecture)
        {
            return app_.load(sysid, active, parent, ref rootNode, architecture);
        }

        public override bool readEntity(StringC name, out StringC content)
        {
            return app_.readEntity(name, out content);
        }

        public override void mapSysid(ref StringC sysid)
        {
            app_.mapSysid(ref sysid);
        }
    }

    private static void splitOffId(ref StringC sysid, out StringC id)
    {
        id = new StringC();
        for (nuint i = sysid.size(); i > 0; i--)
        {
            if (sysid[i - 1] == '#')
            {
                id.assign(sysid.data(), i, sysid.size() - i);
                sysid.resize(i - 1);
                break;
            }
        }
    }

    private static Boolean isS(Char c)
    {
        return c == ' ' || c == '\t' || c == '\r' || c == '\n';
    }

    private static Boolean matchCi(StringC s, string key)
    {
        var data = s.data();
        return data != null && matchCi(data, s.size(), key);
    }

    private static Boolean matchCi(Char[] s, nuint n, string key)
    {
        nuint i = 0;
        foreach (char c in key)
        {
            if (i >= n)
                return false;
            Char sc = s[i];
            if (sc != char.ToLower(c) && sc != char.ToUpper(c))
                return false;
            i++;
        }
        return i == n;
    }
}

// IGroveManager interface
public interface IGroveManager
{
    bool load(StringC sysid, System.Collections.Generic.List<StringC> active,
              NodePtr parent, ref NodePtr rootNode,
              System.Collections.Generic.List<StringC> architecture);

    bool readEntity(StringC name, out StringC content);

    void mapSysid(ref StringC sysid);
}
