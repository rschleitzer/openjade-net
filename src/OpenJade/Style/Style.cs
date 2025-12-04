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
                             ref ELObj? value, System.Collections.Generic.List<nuint> dependencies);
    public abstract ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies);
    public abstract ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp);

    public uint index() { return index_; }
    public Identifier? identifier() { return ident_; }
    public void setIdentifier(Identifier? ident) { ident_ = ident; }

    protected void invalidValue(Location loc, Interpreter interp)
    {
        // Report invalid value error - implementation would use interp.message()
        // For now, just a no-op as message system is not fully implemented
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
                             ref ELObj? cacheObj, System.Collections.Generic.List<nuint> dependencies)
    {
        if (cacheObj == null)
        {
            using (new CurrentNodeSetter(style?.node() ?? new NodePtr(), null, vm))
            {
                vm.actualDependencies = dependencies;
                cacheObj = vm.eval(code_?.get(), style?.display());
                System.Diagnostics.Debug.Assert(cacheObj != null);
                vm.actualDependencies = null;
            }
        }
        if (!vm.interp.isError(cacheObj))
        {
            ConstPtr<InheritedC>? c = inheritedC_?.pointer()?.make(cacheObj, loc_, vm.interp);
            if (c != null && !c.isNull())
                c.pointer()!.set(vm, null, fotb, ref cacheObj, dependencies);
        }
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        using (new CurrentNodeSetter(style?.node() ?? new NodePtr(), null, vm))
        {
            vm.actualDependencies = dependencies;
            return vm.eval(code_?.get(), style?.display());
        }
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
        style = null;
        for (; vi_ < (nuint)vecs_.Count; vi_++, i_ = 0)
        {
            var vec = vecs_[(int)vi_];
            if (vec != null && i_ < (nuint)vec.Count)
            {
                style = styleVec_[(int)vi_];
                return vec[(int)i_++];
            }
        }
        return null;
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
        appendIterForce(iter);
        appendIterNormal(iter);
    }

    public override void appendIterForce(StyleObjIter iter)
    {
        var spec = styleSpec_?.pointer();
        if (spec != null && spec.forceSpecs.Count > 0)
            iter.append(spec.forceSpecs, this);
    }

    public override void appendIterNormal(StyleObjIter iter)
    {
        var spec = styleSpec_?.pointer();
        if (spec != null && spec.specs.Count > 0)
            iter.append(spec.specs, this);
        if (use_ != null)
            use_.appendIter(iter);
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
        if (basic_ != null)
            basic_.appendIterForce(iter);
        if (override_ != null)
            override_.appendIter(iter);
        if (basic_ != null)
            basic_.appendIterNormal(iter);
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
        for (int i = 0; i < styles_.Count; i++)
            if (styles_[i] != null)
                styles_[i]!.appendIter(iter);
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
    public System.Collections.Generic.List<nuint> dependencies;

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
        dependencies = new System.Collections.Generic.List<nuint>();
    }
}

// Pop list for style stack
public class PopList : IResource
{
    private int refCount_ = 0;
    public int count() { return refCount_; }
    public void @ref() { refCount_++; }
    public int unref() { return --refCount_; }

    public System.Collections.Generic.List<nuint> list;
    public System.Collections.Generic.List<nuint> dependingList;
    public Ptr<PopList>? prev;

    public PopList(Ptr<PopList>? prev)
    {
        this.prev = prev;
        list = new System.Collections.Generic.List<nuint>();
        dependingList = new System.Collections.Generic.List<nuint>();
    }
}

// Style stack for processing
public class StyleStack
{
    private System.Collections.Generic.List<Ptr<InheritedCInfo>?> inheritedCInfo_;
    private uint level_;
    private Ptr<PopList>? popList_;

    public StyleStack()
    {
        inheritedCInfo_ = new System.Collections.Generic.List<Ptr<InheritedCInfo>?>();
        level_ = 0;
        popList_ = null;
    }

    public ELObj? actual(ConstPtr<InheritedC>? ic, Location loc, Interpreter interp,
                         System.Collections.Generic.List<nuint> dependencies)
    {
        if (ic == null || ic.isNull()) return null;
        nuint ind = ic.pointer()!.index();
        for (int i = 0; i < dependencies.Count; i++)
        {
            if (dependencies[i] == ind)
            {
                // Circular dependency - actual loop error
                interp.setNodeLocation(new NodePtr());
                return interp.makeError();
            }
        }
        dependencies.Add(ind);
        ConstPtr<InheritedC>? spec = null;
        VarStyleObj? style = null;
        if (ind >= (nuint)inheritedCInfo_.Count)
            spec = ic;
        else
        {
            var p = inheritedCInfo_[(int)ind]?.pointer();
            if (p == null)
                spec = ic;
            else if (p.cachedValue != null)
            {
                var dep = p.dependencies;
                for (int i = 0; i < dep.Count; i++)
                    dependencies.Add(dep[i]);
                return p.cachedValue;
            }
            else
            {
                style = p.style;
                spec = p.spec;
            }
        }
        VM vm = new VM(interp);
        vm.styleStack = this;
        vm.specLevel = level_;
        return spec?.pointer()?.value(vm, style, dependencies);
    }

    public ELObj? actual(ConstPtr<InheritedC>? ic, Interpreter interp,
                         System.Collections.Generic.List<nuint> dependencies)
    {
        return actual(ic, new Location(), interp, dependencies);
    }

    public ELObj? inherited(ConstPtr<InheritedC>? ic, uint specLevel, Interpreter interp,
                            System.Collections.Generic.List<nuint> dependencies)
    {
        if (ic == null || ic.isNull()) return null;
        System.Diagnostics.Debug.Assert(specLevel != uint.MaxValue);
        nuint ind = ic.pointer()!.index();
        ConstPtr<InheritedC>? spec = null;
        VarStyleObj? style = null;
        uint newSpecLevel = uint.MaxValue;
        if (ind >= (nuint)inheritedCInfo_.Count)
            spec = ic;
        else
        {
            var p = inheritedCInfo_[(int)ind]?.pointer();
            while (p != null)
            {
                if (p.specLevel < specLevel)
                    break;
                p = p.prev?.pointer();
            }
            if (p == null)
                spec = ic;
            else
            {
                if (p.cachedValue != null)
                {
                    // We can only use the cached value if none of the values
                    // we depended on changed since we computed it.
                    bool cacheOk = true;
                    for (int i = 0; i < p.dependencies.Count; i++)
                    {
                        nuint d = p.dependencies[i];
                        if (d < (nuint)inheritedCInfo_.Count &&
                            inheritedCInfo_[(int)d]?.pointer()?.valLevel > p.valLevel)
                        {
                            cacheOk = false;
                            break;
                        }
                    }
                    if (cacheOk)
                        return p.cachedValue;
                }
                style = p.style;
                spec = p.spec;
                newSpecLevel = p.specLevel;
            }
        }
        VM vm = new VM(interp);
        vm.styleStack = this;
        vm.specLevel = newSpecLevel;
        return spec?.pointer()?.value(vm, style, dependencies);
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
        if (style == null) return;
        StyleObjIter iter = new StyleObjIter();
        style.appendIter(iter);
        for (;;)
        {
            VarStyleObj? varStyle;
            ConstPtr<InheritedC>? spec = iter.next(out varStyle);
            if (spec == null || spec.isNull())
                break;
            nuint ind = spec.pointer()!.index();
            if (ind >= (nuint)inheritedCInfo_.Count)
            {
                while ((nuint)inheritedCInfo_.Count <= ind)
                    inheritedCInfo_.Add(null);
            }
            var info = inheritedCInfo_[(int)ind];
            if (info != null && info.pointer() != null && info.pointer()!.valLevel == level_)
            {
                if (rule != null)
                {
                    System.Diagnostics.Debug.Assert(info.pointer()!.rule != null);
                    if (rule.compareSpecificity(info.pointer()!.rule!) == 0)
                    {
                        // Ambiguous style - would normally report error via mgr
                    }
                }
            }
            else
            {
                popList_!.pointer()!.list.Add(ind);
                inheritedCInfo_[(int)ind] = new Ptr<InheritedCInfo>(
                    new InheritedCInfo(spec, varStyle, level_, level_, rule, info));
            }
        }
    }

    public void pushEnd(VM vm, FOTBuilder fotb)
    {
        var oldPopList = popList_?.pointer()?.prev?.pointer();
        if (oldPopList != null)
        {
            for (int i = 0; i < oldPopList.dependingList.Count; i++)
            {
                nuint d = oldPopList.dependingList[i];
                // d is the index of a characteristic that depends on the actual
                // value of another characteristic
                if ((int)d < inheritedCInfo_.Count &&
                    inheritedCInfo_[(int)d]?.pointer()?.valLevel != level_)
                {
                    var dependencies = inheritedCInfo_[(int)d]?.pointer()?.dependencies;
                    bool changed = false;
                    if (dependencies != null)
                    {
                        for (int j = 0; j < dependencies.Count; j++)
                        {
                            var p = (int)dependencies[j] < inheritedCInfo_.Count
                                ? inheritedCInfo_[(int)dependencies[j]]?.pointer() : null;
                            if (p != null && p.valLevel == level_)
                            {
                                var oldInfo = inheritedCInfo_[(int)d]?.pointer();
                                inheritedCInfo_[(int)d] = new Ptr<InheritedCInfo>(
                                    new InheritedCInfo(oldInfo?.spec, oldInfo?.style,
                                                       level_, oldInfo?.specLevel ?? 0,
                                                       oldInfo?.rule, inheritedCInfo_[(int)d]));
                                popList_!.pointer()!.list.Add(d);
                                changed = true;
                                break;
                            }
                        }
                    }
                    // If it changed, then doing set() on the new value will add
                    // it to the dependingList for this level.
                    if (!changed)
                        popList_!.pointer()!.dependingList.Add(d);
                }
            }
        }
        vm.styleStack = this;
        var popListPtr = popList_?.pointer();
        if (popListPtr != null)
        {
            for (int i = 0; i < popListPtr.list.Count; i++)
            {
                var info = inheritedCInfo_[(int)popListPtr.list[i]]?.pointer();
                if (info != null)
                {
                    vm.specLevel = info.specLevel;
                    info.spec?.pointer()?.set(vm, info.style, fotb,
                                              ref info.cachedValue, info.dependencies);
                    if (info.dependencies.Count > 0)
                        popListPtr.dependingList.Add(popListPtr.list[i]);
                }
            }
        }
        vm.styleStack = null;
    }

    public void pop()
    {
        if (popList_ != null)
        {
            var popListPtr = popList_.pointer();
            if (popListPtr != null)
            {
                for (int i = 0; i < popListPtr.list.Count; i++)
                {
                    nuint ind = popListPtr.list[i];
                    if (ind < (nuint)inheritedCInfo_.Count)
                    {
                        var info = inheritedCInfo_[(int)ind];
                        if (info != null && info.pointer() != null)
                        {
                            inheritedCInfo_[(int)ind] = info.pointer()!.prev;
                        }
                    }
                }
                level_--;
                popList_ = popListPtr.prev;
            }
        }
    }

    public void pushEmpty() { level_++; }
    public void popEmpty() { level_--; }
    public uint level() { return level_; }

    public void trace(Collector collector)
    {
        for (int i = 0; i < inheritedCInfo_.Count; i++)
        {
            for (var p = inheritedCInfo_[i]?.pointer(); p != null; p = p.prev?.pointer())
            {
                collector.trace(p.style);
                collector.trace(p.cachedValue);
            }
        }
    }
}

// NOTE: Identifier is defined in ELObj.cs
// NOTE: Collector is defined in FOTBuilder.cs
// NOTE: FunctionObj is defined in ELObj.cs
