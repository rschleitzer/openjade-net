// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// InheritedC represents the specification of a value
// of an inherited characteristic.
public abstract class InheritedC : IResource
{
    private int refCount_ = 0;
    public int count() { return refCount_; }
    public void @ref() { refCount_++; }
    public int unref() { return --refCount_; }
    private Identifier? ident_;
    private uint index_;

    public InheritedC(Identifier? ident, uint index)
    {
        ident_ = ident;
        index_ = index;
    }

    public abstract void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? value, List<nuint> dependencies);
    public abstract ELObj? value(VM vm, VarStyleObj? style, List<nuint> dependencies);
    public abstract ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp);

    public uint index() { return index_; }
    public Identifier? identifier() { return ident_; }
    public void setIdentifier(Identifier? ident) { ident_ = ident; }

    protected void invalidValue(Location loc, Interpreter interp)
    {
        throw new NotImplementedException();
    }
}

// Variable inherited characteristic
public class VarInheritedC : InheritedC
{
    private ConstPtr<InheritedC>? inheritedC_;
    private InsnPtr? code_;
    private Location loc_;

    public VarInheritedC(ConstPtr<InheritedC>? ic, InsnPtr? code, Location loc)
        : base(ic?.pointer()?.identifier(), ic?.pointer()?.index() ?? 0)
    {
        inheritedC_ = ic;
        code_ = code;
        loc_ = loc;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? value, List<nuint> dependencies)
    {
        throw new NotImplementedException();
    }

    public override ELObj? value(VM vm, VarStyleObj? style, List<nuint> dependencies)
    {
        throw new NotImplementedException();
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        return inheritedC_?.pointer()?.make(obj, loc, interp);
    }
}

// Style object iterator
public class StyleObjIter
{
    private nuint i_;
    private nuint vi_;
    private System.Collections.Generic.List<VarStyleObj?> styleVec_;
    private System.Collections.Generic.List<System.Collections.Generic.List<ConstPtr<InheritedC>>?> vecs_;

    public StyleObjIter()
    {
        i_ = 0;
        vi_ = 0;
        styleVec_ = new System.Collections.Generic.List<VarStyleObj?>();
        vecs_ = new System.Collections.Generic.List<System.Collections.Generic.List<ConstPtr<InheritedC>>?>();
    }

    public void append(System.Collections.Generic.List<ConstPtr<InheritedC>>? vec, VarStyleObj? style)
    {
        vecs_.Add(vec);
        styleVec_.Add(style);
    }

    public ConstPtr<InheritedC>? next(out VarStyleObj? style)
    {
        throw new NotImplementedException();
    }
}

// Style specification
public class StyleSpec : IResource
{
    private int refCount_ = 0;
    public int count() { return refCount_; }
    public void @ref() { refCount_++; }
    public int unref() { return --refCount_; }

    public System.Collections.Generic.List<ConstPtr<InheritedC>> forceSpecs;
    public System.Collections.Generic.List<ConstPtr<InheritedC>> specs;

    public StyleSpec(System.Collections.Generic.List<ConstPtr<InheritedC>> force, System.Collections.Generic.List<ConstPtr<InheritedC>> normal)
    {
        forceSpecs = force;
        specs = normal;
    }
}

// Basic style object
public abstract class BasicStyleObj : StyleObj
{
    public abstract void appendIterForce(StyleObjIter iter);
    public abstract void appendIterNormal(StyleObjIter iter);
}

// Variable style object
public class VarStyleObj : BasicStyleObj
{
    private ConstPtr<StyleSpec>? styleSpec_;
    private StyleObj? use_;
    private ELObj?[]? display_;
    private NodePtr node_;

    public VarStyleObj(ConstPtr<StyleSpec>? spec, StyleObj? use, ELObj?[]? display, NodePtr node)
    {
        styleSpec_ = spec;
        use_ = use;
        display_ = display;
        node_ = node;
    }

    public override void appendIter(StyleObjIter iter)
    {
        throw new NotImplementedException();
    }

    public override void appendIterForce(StyleObjIter iter)
    {
        throw new NotImplementedException();
    }

    public override void appendIterNormal(StyleObjIter iter)
    {
        throw new NotImplementedException();
    }

    public NodePtr node() { return node_; }
    public ELObj?[]? display() { return display_; }
}

// Overridden style object
public class OverriddenStyleObj : StyleObj
{
    private BasicStyleObj? basic_;
    private StyleObj? override_;

    public OverriddenStyleObj(BasicStyleObj? basic, StyleObj? ov)
    {
        basic_ = basic;
        override_ = ov;
    }

    public override void appendIter(StyleObjIter iter)
    {
        throw new NotImplementedException();
    }
}

// Merge style object
public class MergeStyleObj : StyleObj
{
    private System.Collections.Generic.List<StyleObj?> styles_;

    public MergeStyleObj()
    {
        styles_ = new System.Collections.Generic.List<StyleObj?>();
    }

    public void append(StyleObj? style)
    {
        styles_.Add(style);
    }

    public override void appendIter(StyleObjIter iter)
    {
        throw new NotImplementedException();
    }
}

// NOTE: ColorObj, ColorSpaceObj and derived classes are defined in ELObj.cs

// Inherited characteristic info
public class InheritedCInfo : IResource
{
    private int refCount_ = 0;
    public int count() { return refCount_; }
    public void @ref() { refCount_++; }
    public int unref() { return --refCount_; }

    public ConstPtr<InheritedC>? spec;
    public Ptr<InheritedCInfo>? prev;
    public uint valLevel;
    public uint specLevel;
    public ProcessingMode.Rule? rule;
    public ELObj? cachedValue;
    public VarStyleObj? style;
    public List<nuint> dependencies;

    public InheritedCInfo(ConstPtr<InheritedC>? spec, VarStyleObj? style,
                          uint valLevel, uint specLevel, ProcessingMode.Rule? rule,
                          Ptr<InheritedCInfo>? prev)
    {
        this.spec = spec;
        this.style = style;
        this.valLevel = valLevel;
        this.specLevel = specLevel;
        this.rule = rule;
        this.prev = prev;
        cachedValue = null;
        dependencies = new List<nuint>();
    }
}

// Pop list for style stack
public class PopList : IResource
{
    private int refCount_ = 0;
    public int count() { return refCount_; }
    public void @ref() { refCount_++; }
    public int unref() { return --refCount_; }

    public List<nuint> list;
    public List<nuint> dependingList;
    public Ptr<PopList>? prev;

    public PopList(Ptr<PopList>? prev)
    {
        this.prev = prev;
        list = new List<nuint>();
        dependingList = new List<nuint>();
    }
}

// Style stack for processing
public class StyleStack
{
    private List<Ptr<InheritedCInfo>> inheritedCInfo_;
    private uint level_;
    private Ptr<PopList>? popList_;

    public StyleStack()
    {
        inheritedCInfo_ = new List<Ptr<InheritedCInfo>>();
        level_ = 0;
        popList_ = null;
    }

    public ELObj? actual(ConstPtr<InheritedC>? ic, Location loc, Interpreter interp,
                         List<nuint> dependencies)
    {
        throw new NotImplementedException();
    }

    public ELObj? actual(ConstPtr<InheritedC>? ic, Interpreter interp,
                         List<nuint> dependencies)
    {
        return actual(ic, new Location(), interp, dependencies);
    }

    public ELObj? inherited(ConstPtr<InheritedC>? ic, uint specLevel, Interpreter interp,
                            List<nuint> dependencies)
    {
        throw new NotImplementedException();
    }

    public void push(StyleObj? style, VM vm, FOTBuilder fotb)
    {
        pushStart();
        pushContinue(style, null, new NodePtr(), null);
        pushEnd(vm, fotb);
    }

    public void pushStart()
    {
        level_++;
        popList_ = new Ptr<PopList>(new PopList(popList_));
    }

    public void pushContinue(StyleObj? style, ProcessingMode.Rule? rule,
                             NodePtr node, Messenger? mgr)
    {
        throw new NotImplementedException();
    }

    public void pushEnd(VM vm, FOTBuilder fotb)
    {
        throw new NotImplementedException();
    }

    public void pop()
    {
        throw new NotImplementedException();
    }

    public void pushEmpty() { level_++; }
    public void popEmpty() { level_--; }
    public uint level() { return level_; }

    public void trace(Collector collector)
    {
        throw new NotImplementedException();
    }
}

// NOTE: Identifier is defined in ELObj.cs
// NOTE: Collector is defined in FOTBuilder.cs
// NOTE: FunctionObj is defined in ELObj.cs
