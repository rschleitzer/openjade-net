// Copyright (c) 1996, 1997 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using OpenJade.SPGrove;
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
    private List<StringC> defineVars_;
    private SgmlParser? specParser_;
    private Dictionary<StringC, NodePtr> groveTable_;
    private StringC rootSystemId_;
    private bool debugMode_;
    private bool dsssl2_;
    private bool strictMode_;

    public DssslApp(int unitsPerInch)
        : base(null)
    {
        unitsPerInch_ = unitsPerInch;
        defaultOutputBasename_ = new StringC();
        dssslSpecOption_ = false;
        dssslSpecSysid_ = new StringC();
        dssslSpecId_ = new StringC();
        defineVars_ = new List<StringC>();
        specParser_ = null;
        groveTable_ = new Dictionary<StringC, NodePtr>();
        rootSystemId_ = new StringC();
        debugMode_ = false;
        dsssl2_ = false;
        strictMode_ = false;
    }

    public abstract FOTBuilder? makeFOTBuilder(out FOTBuilder.Extension? ext);

    public new int processSysid(StringC sysid)
    {
        throw new NotImplementedException();
    }

    // IGroveManager implementation
    public bool load(StringC sysid, List<StringC> active,
                     NodePtr parent, ref NodePtr rootNode,
                     List<StringC> architecture)
    {
        throw new NotImplementedException();
    }

    public bool readEntity(StringC name, out StringC content)
    {
        content = new StringC();
        throw new NotImplementedException();
    }

    public void mapSysid(ref StringC sysid)
    {
        throw new NotImplementedException();
    }

    protected void processOption(Char opt, StringC arg)
    {
        throw new NotImplementedException();
    }

    protected int init(int argc, StringC[] argv)
    {
        throw new NotImplementedException();
    }

    public override void processGrove()
    {
        throw new NotImplementedException();
    }

    private new int generateEvents(ErrorCountEventHandler? eceh)
    {
        throw new NotImplementedException();
    }

    private Boolean getDssslSpecFromGrove()
    {
        throw new NotImplementedException();
    }

    private Boolean getDssslSpecFromPi(Char[] s, nuint n, Location loc)
    {
        throw new NotImplementedException();
    }

    private static void splitOffId(ref StringC sysid, out StringC id)
    {
        id = new StringC();
        throw new NotImplementedException();
    }

    private Boolean handleSimplePi(Char[] s, nuint n, Location loc)
    {
        throw new NotImplementedException();
    }

    private Boolean handleAttlistPi(Char[] s, nuint n, Location loc)
    {
        throw new NotImplementedException();
    }

    private static void skipS(ref Char[] s, ref nuint n)
    {
        throw new NotImplementedException();
    }

    private static Boolean isS(Char c)
    {
        return c == ' ' || c == '\t' || c == '\r' || c == '\n';
    }

    private static Boolean matchCi(StringC s, string key)
    {
        throw new NotImplementedException();
    }

    private static Boolean matchCi(Char[] s, nuint n, string key)
    {
        throw new NotImplementedException();
    }

    private static Boolean getAttribute(ref Char[] s, ref nuint n,
                                        out StringC name, out StringC value)
    {
        name = new StringC();
        value = new StringC();
        throw new NotImplementedException();
    }

    private Boolean initSpecParser()
    {
        throw new NotImplementedException();
    }
}

// IGroveManager interface
public interface IGroveManager
{
    bool load(StringC sysid, List<StringC> active,
              NodePtr parent, ref NodePtr rootNode,
              List<StringC> architecture);

    bool readEntity(StringC name, out StringC content);

    void mapSysid(ref StringC sysid);
}
