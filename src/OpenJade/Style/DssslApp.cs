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
    private System.Collections.Generic.List<StringC> defineVars_;
    private SgmlParser? specParser_;
    private System.Collections.Generic.Dictionary<string, NodePtr> groveTable_;
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
        defineVars_ = new System.Collections.Generic.List<StringC>();
        specParser_ = null;
        groveTable_ = new System.Collections.Generic.Dictionary<string, NodePtr>();
        rootSystemId_ = new StringC();
        debugMode_ = false;
        dsssl2_ = false;
        strictMode_ = false;
    }

    public abstract FOTBuilder? makeFOTBuilder(out FOTBuilder.Extension? ext);

    public new int processSysid(StringC sysid)
    {
        rootSystemId_ = sysid;
        return base.processSysid(sysid);
    }

    // IGroveManager implementation
    public bool load(StringC sysid, System.Collections.Generic.List<StringC> active,
                     NodePtr parent, ref NodePtr rootNode,
                     System.Collections.Generic.List<StringC> architecture)
    {
        // TODO: Full implementation
        throw new NotImplementedException();
    }

    public bool readEntity(StringC sysid, out StringC content)
    {
        content = new StringC();
        // TODO: Full implementation
        throw new NotImplementedException();
    }

    public void mapSysid(ref StringC sysid)
    {
        // No-op for now
    }

    public override void processGrove()
    {
        // TODO: Full implementation
        throw new NotImplementedException();
    }

    private Boolean initSpecParser()
    {
        // TODO: Full implementation
        return false;
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
