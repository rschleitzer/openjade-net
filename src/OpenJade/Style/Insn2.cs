// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Check sosofo instruction
public class CheckSosofoInsn : Insn
{
    private Location loc_;
    private InsnPtr next_;

    public CheckSosofoInsn(Location loc, InsnPtr next)
    {
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        if (vm.sbase[vm.sp - 1]?.asSosofo() == null)
        {
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            vm.interp.setNextLocation(loc_);
            // vm.interp.message(InterpreterMessages.sosofoContext);
            return null;
        }
        return next_.pointer();
    }
}

// Check style instruction
public class CheckStyleInsn : Insn
{
    private Location loc_;
    private InsnPtr next_;

    public CheckStyleInsn(Location loc, InsnPtr next)
    {
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        if (vm.sbase[vm.sp - 1]?.asStyle() == null)
        {
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            vm.interp.setNextLocation(loc_);
            // vm.interp.message(InterpreterMessages.styleContext);
            return null;
        }
        return next_.pointer();
    }
}

// Push mode instruction
public class PushModeInsn : Insn
{
    private ProcessingMode? mode_;
    private InsnPtr next_;

    public PushModeInsn(ProcessingMode? mode, InsnPtr next)
    {
        mode_ = mode;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        vm.modeStack.Add(vm.processingMode);
        vm.processingMode = mode_;
        return next_.pointer();
    }
}

// Pop mode instruction
public class PopModeInsn : Insn
{
    private InsnPtr next_;

    public PopModeInsn(InsnPtr next)
    {
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        vm.processingMode = vm.modeStack[vm.modeStack.Count - 1];
        vm.modeStack.RemoveAt(vm.modeStack.Count - 1);
        return next_.pointer();
    }
}

// Set non-inherited characteristic sosofo instruction
public class SetNonInheritedCsSosofoInsn : Insn
{
    private InsnPtr code_;
    private int displayLength_;
    private InsnPtr next_;

    public SetNonInheritedCsSosofoInsn(InsnPtr code, int displayLength, InsnPtr next)
    {
        code_ = code;
        displayLength_ = displayLength;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        ELObj?[]? display = displayLength_ > 0 ? new ELObj?[displayLength_ + 1] : null;
        int tem = vm.sp - displayLength_;
        for (int i = 0; i < displayLength_; i++)
        {
            display![i] = vm.sbase[tem + i];
            System.Diagnostics.Debug.Assert(display[i] != null);
        }
        if (displayLength_ > 0)
            display![displayLength_] = null;
        FlowObj flowObj = (FlowObj)vm.sbase[--tem]!;
        System.Diagnostics.Debug.Assert(vm.sbase[tem]!.asSosofo() != null);
        vm.sbase[tem++] = new SetNonInheritedCsSosofoObj(flowObj, code_, display, vm.currentNode);
        vm.sp = tem;
        return next_.pointer();
    }
}

// Copy flow object instruction
public class CopyFlowObjInsn : Insn
{
    private FlowObj flowObj_;
    private InsnPtr next_;

    public CopyFlowObjInsn(FlowObj flowObj, InsnPtr next)
    {
        flowObj_ = flowObj;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        vm.needStack(1);
        vm.sbase[vm.sp++] = flowObj_.copy(vm.interp);
        return next_.pointer();
    }
}

// Set pseudo non-inherited characteristic instruction
public class SetPseudoNonInheritedCInsn : Insn
{
    protected Location loc_;
    protected Identifier? nic_;
    protected InsnPtr next_;

    public SetPseudoNonInheritedCInsn(Identifier? nic, Location loc, InsnPtr next)
    {
        nic_ = nic;
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        System.Diagnostics.Debug.Assert(vm.sbase[vm.sp - 2]!.asSosofo() != null);
        ((FlowObj)vm.sbase[vm.sp - 2]!).setNonInheritedC(nic_, vm.sbase[vm.sp - 1], loc_, vm.interp);
        vm.sp--;
        return next_.pointer();
    }
}

// Set non-inherited characteristic instruction
public class SetNonInheritedCInsn : SetPseudoNonInheritedCInsn
{
    public SetNonInheritedCInsn(Identifier? nic, Location loc, InsnPtr next)
        : base(nic, loc, next)
    {
    }

    public override Insn? execute(VM vm)
    {
        vm.actualDependencies?.Clear();
        return base.execute(vm);
    }
}

// Set implicit char instruction
public class SetImplicitCharInsn : Insn
{
    private Location loc_;
    private InsnPtr next_;

    public SetImplicitCharInsn(Location loc, InsnPtr next)
    {
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        System.Diagnostics.Debug.Assert(vm.sbase[vm.sp - 1]!.asSosofo() != null);
        if (vm.currentNode.node != null)
        {
            ELObjPropertyValue value = new ELObjPropertyValue(vm.interp!, false);
            Grove.SdataMapper mapper = new Grove.SdataMapper();
            AccessResult ret = vm.currentNode.node.property(ComponentName.Id.idChar, mapper, value);
            if (ret == AccessResult.accessOK && value.obj != null)
            {
                ((FlowObj)vm.sbase[vm.sp - 1]!).setImplicitChar(value.obj, loc_, vm.interp!);
            }
        }
        return next_.pointer();
    }
}

// Set content instruction
public class SetContentInsn : Insn
{
    private CompoundFlowObj? flowObj_;
    private InsnPtr next_;

    public SetContentInsn(CompoundFlowObj? flowObj, InsnPtr next)
    {
        flowObj_ = flowObj;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        CompoundFlowObj copy = (CompoundFlowObj)flowObj_!.copy(vm.interp);
        copy.setContent((SosofoObj?)vm.sbase[vm.sp - 1]);
        vm.sbase[vm.sp - 1] = copy;
        return next_.pointer();
    }
}

// Set default content instruction
public class SetDefaultContentInsn : Insn
{
    private CompoundFlowObj? flowObj_;
    private Location loc_;
    private InsnPtr next_;

    public SetDefaultContentInsn(CompoundFlowObj? flowObj, Location loc, InsnPtr next)
    {
        flowObj_ = flowObj;
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        if (vm.processingMode == null)
        {
            vm.interp.setNextLocation(loc_);
            // vm.interp.message(InterpreterMessages.noCurrentProcessingMode);
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        vm.needStack(1);
        vm.sbase[vm.sp++] = flowObj_!.copy(vm.interp);
        ((CompoundFlowObj)vm.sbase[vm.sp - 1]!).setContent(
            new ProcessChildrenSosofoObj(vm.processingMode));
        return next_.pointer();
    }
}

// Make default content instruction
public class MakeDefaultContentInsn : Insn
{
    private Location loc_;
    private InsnPtr next_;

    public MakeDefaultContentInsn(Location loc, InsnPtr next)
    {
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        if (vm.processingMode == null)
        {
            vm.interp.setNextLocation(loc_);
            // vm.interp.message(InterpreterMessages.noCurrentProcessingMode);
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        vm.needStack(1);
        vm.sbase[vm.sp++] = new ProcessChildrenSosofoObj(vm.processingMode);
        return next_.pointer();
    }
}

// Sosofo append instruction
public class SosofoAppendInsn : Insn
{
    private int n_;
    private InsnPtr next_;

    public SosofoAppendInsn(int n, InsnPtr next)
    {
        n_ = n;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        AppendSosofoObj obj = new AppendSosofoObj();
        int tem = vm.sp - n_;
        for (int i = 0; i < n_; i++)
        {
            System.Diagnostics.Debug.Assert(vm.sbase[tem + i]!.asSosofo() != null);
            var sosofo = (SosofoObj)vm.sbase[tem + i]!;
            obj.append(sosofo);
        }
        vm.sp -= n_ - 1;
        vm.sbase[vm.sp - 1] = obj;
        return next_.pointer();
    }
}

// Variable style instruction
public class VarStyleInsn : Insn
{
    private int displayLength_;
    private bool hasUse_;
    private ConstPtr<StyleSpec>? styleSpec_;
    private InsnPtr next_;

    public VarStyleInsn(ConstPtr<StyleSpec>? styleSpec, int displayLength, bool hasUse, InsnPtr next)
    {
        styleSpec_ = styleSpec;
        displayLength_ = displayLength;
        hasUse_ = hasUse;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        ELObj?[]? display = displayLength_ > 0 ? new ELObj?[displayLength_ + 1] : null;
        int tem = vm.sp - displayLength_;
        for (int i = 0; i < displayLength_; i++)
            display![i] = vm.sbase[tem + i];
        if (displayLength_ == 0)
        {
            vm.needStack(1);
            tem = vm.sp;
        }
        else
            display![displayLength_] = null;
        StyleObj? use = null;
        if (hasUse_)
            use = (StyleObj?)vm.sbase[--tem];
        vm.sbase[tem++] = new VarStyleObj(styleSpec_, use, display, vm.currentNode);
        vm.sp = tem;
        vm.interp.makePermanent(vm.sbase[tem - 1]!);
        return next_.pointer();
    }
}

// Maybe override style instruction
public class MaybeOverrideStyleInsn : Insn
{
    private InsnPtr next_;

    public MaybeOverrideStyleInsn(InsnPtr next)
    {
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        if (vm.overridingStyle != null)
            vm.sbase[vm.sp - 1] = new OverriddenStyleObj(
                (BasicStyleObj?)vm.sbase[vm.sp - 1], vm.overridingStyle);
        return next_.pointer();
    }
}

// Set style instruction
public class SetStyleInsn : Insn
{
    private InsnPtr next_;

    public SetStyleInsn(InsnPtr next)
    {
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        ((FlowObj)vm.sbase[vm.sp - 2]!).setStyle((StyleObj?)vm.sbase[vm.sp - 1]);
        vm.sp--;
        return next_.pointer();
    }
}

// Label sosofo instruction
public class LabelSosofoInsn : Insn
{
    private Location loc_;
    private InsnPtr next_;

    public LabelSosofoInsn(Location loc, InsnPtr next)
    {
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        SymbolObj? sym = vm.sbase[vm.sp - 1]?.asSymbol();
        if (sym == null)
        {
            vm.interp.setNextLocation(loc_);
            // vm.interp.message(InterpreterMessages.labelNotASymbol);
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        System.Diagnostics.Debug.Assert(vm.sbase[vm.sp - 2]!.asSosofo() != null);
        vm.sbase[vm.sp - 2] = new LabelSosofoObj(sym, loc_, (SosofoObj)vm.sbase[vm.sp - 2]!);
        vm.sp--;
        return next_.pointer();
    }
}

// Content map sosofo instruction
public class ContentMapSosofoInsn : Insn
{
    private Location loc_;
    private InsnPtr next_;

    public ContentMapSosofoInsn(Location loc, InsnPtr next)
    {
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        System.Diagnostics.Debug.Assert(vm.sbase[vm.sp - 2]!.asSosofo() != null);
        vm.sbase[vm.sp - 2] = new ContentMapSosofoObj(vm.sbase[vm.sp - 1], loc_, (SosofoObj)vm.sbase[vm.sp - 2]!);
        vm.sp--;
        return next_.pointer();
    }
}

// Sosofo objects for special processing
public class SetNonInheritedCsSosofoObj : SosofoObj
{
    private FlowObj flowObj_;
    private InsnPtr? code_;
    private ELObj?[]? display_;
    private NodePtr node_;

    public SetNonInheritedCsSosofoObj(FlowObj flowObj, InsnPtr? code, ELObj?[]? display, NodePtr node)
    {
        flowObj_ = flowObj;
        code_ = code;
        display_ = display;
        node_ = node;
    }

    public ELObj? resolve(ProcessContext context)
    {
        VM vm = context.vm();
        using var cns = new CurrentNodeSetter(node_, null, vm);
        StyleStack? saveStyleStack = vm.styleStack;
        vm.styleStack = context.currentStyleStack();
        uint saveSpecLevel = vm.specLevel;
        vm.specLevel = vm.styleStack?.level() ?? 0;
        var dep = new System.Collections.Generic.List<nuint>();
        vm.actualDependencies = dep;
        ELObj? obj = vm.eval(code_?.pointer(), display_, flowObj_.copy(vm.interp));
        vm.styleStack = saveStyleStack;
        vm.specLevel = saveSpecLevel;
        if (vm.interp.isError(obj))
            return null;
        return obj;
    }

    public override void process(ProcessContext context)
    {
        context.startFlowObj();
        uint flags = 0;
        flowObj_.pushStyle(context, ref flags);
        ELObj? obj = resolve(context);
        if (obj != null)
            ((FlowObj)obj).processInner(context);
        flowObj_.popStyle(context, flags);
        context.endFlowObj();
    }

    public override bool isCharacter()
    {
        return flowObj_.isCharacter();
    }

    public override bool isRule()
    {
        return flowObj_.isRule();
    }

    public override bool characterStyle(ProcessContext context, out StyleObj? style, FOTBuilder.CharacterNIC nic)
    {
        ELObj? obj = resolve(context);
        style = null;
        if (obj != null)
            return ((SosofoObj)obj).characterStyle(context, out style, nic);
        return false;
    }

    public override bool ruleStyle(ProcessContext context, out StyleObj? style)
    {
        ELObj? obj = resolve(context);
        style = null;
        if (obj != null)
            return ((SosofoObj)obj).ruleStyle(context, out style);
        return false;
    }
}

public class ProcessChildrenSosofoObj : SosofoObj
{
    private ProcessingMode? mode_;

    public ProcessChildrenSosofoObj(ProcessingMode? mode)
    {
        mode_ = mode;
    }

    public override void process(ProcessContext context)
    {
        // Use copy constructor to create a separate copy (matching C++ copy semantics)
        NodePtr node = new NodePtr(context.vm().currentNode);
        context.processChildren(mode_);
        context.vm().currentNode = node;
    }
}

public class LabelSosofoObj : SosofoObj
{
    private SymbolObj label_;
    private Location loc_;
    private SosofoObj content_;

    public LabelSosofoObj(SymbolObj label, Location loc, SosofoObj content)
    {
        label_ = label;
        loc_ = loc;
        content_ = content;
    }

    public override void process(ProcessContext context)
    {
        context.startConnection(label_, loc_);
        content_.process(context);
        context.endConnection();
    }
}

public class ContentMapSosofoObj : SosofoObj
{
    private ELObj? contentMap_;
    private Location loc_;
    private SosofoObj content_;

    public ContentMapSosofoObj(ELObj? contentMap, Location loc, SosofoObj content)
    {
        contentMap_ = contentMap;
        loc_ = loc;
        content_ = content;
    }

    public override void process(ProcessContext context)
    {
        ((ProcessContextImpl)context).startMapContent(contentMap_, loc_);
        content_.process(context);
        ((ProcessContextImpl)context).endMapContent();
    }
}

// LiteralSosofoObj - outputs literal text
public class LiteralSosofoObj : SosofoObj
{
    private ELObj str_;

    public LiteralSosofoObj(ELObj str)
    {
        str_ = str;
    }

    public override void process(ProcessContext context)
    {
        Char[]? s = null;
        nuint n = 0;
        if (str_.stringData(out s, out n) && s != null)
            context.currentFOTBuilder().characters(s, n);
    }
}

// ProcessChildrenTrimSosofoObj - processes children trimming whitespace
public class ProcessChildrenTrimSosofoObj : SosofoObj
{
    private ProcessingMode? mode_;

    public ProcessChildrenTrimSosofoObj(ProcessingMode? mode)
    {
        mode_ = mode;
    }

    public override void process(ProcessContext context)
    {
        // Use copy constructor to create a separate copy (matching C++ copy semantics)
        NodePtr node = new NodePtr(context.vm().currentNode);
        context.processChildrenTrim(mode_);
        context.vm().currentNode = node;
    }
}

// NextMatchSosofoObj - processes next matching rule
public class NextMatchSosofoObj : SosofoObj
{
    private StyleObj? style_;

    public NextMatchSosofoObj(StyleObj? style)
    {
        style_ = style;
    }

    public override void process(ProcessContext context)
    {
        context.nextMatch(style_);
    }
}

// ProcessNodeSosofoObj - processes a specific node
public class ProcessNodeSosofoObj : SosofoObj
{
    private NodePtr node_;
    private ProcessingMode? mode_;

    public ProcessNodeSosofoObj(NodePtr node, ProcessingMode? mode)
    {
        node_ = node;
        mode_ = mode;
    }

    public override void process(ProcessContext context)
    {
        context.processNode(node_, mode_);
    }
}

// ProcessNodeListSosofoObj - processes a node list
public class ProcessNodeListSosofoObj : SosofoObj
{
    private NodeListObj nodeList_;
    private ProcessingMode? mode_;

    public ProcessNodeListSosofoObj(NodeListObj nodeList, ProcessingMode? mode)
    {
        nodeList_ = nodeList;
        mode_ = mode;
    }

    public override void process(ProcessContext context)
    {
        NodeListObj nl = nodeList_;
        Interpreter interp = context.vm().interp;
        for (;;)
        {
            NodePtr? node = nl.nodeListFirst(context.vm(), interp);
            if (node == null)
                break;
            bool chunk = true;
            nl = nl.nodeListChunkRest(context.vm(), interp, ref chunk);
            ((ProcessContextImpl)context).processNodeSafe(node, mode_, chunk);
        }
    }
}

// CurrentNodePageNumberSosofoObj - outputs page number of a node
public class CurrentNodePageNumberSosofoObj : SosofoObj
{
    private NodePtr node_;

    public CurrentNodePageNumberSosofoObj(NodePtr node)
    {
        node_ = node;
    }

    public override void process(ProcessContext context)
    {
        context.currentFOTBuilder().currentNodePageNumber(node_);
    }
}

// PageNumberSosofoObj - outputs current page number
public class PageNumberSosofoObj : SosofoObj
{
    public override void process(ProcessContext context)
    {
        context.currentFOTBuilder().pageNumber();
    }
}

// DiscardLabeledSosofoObj - discards labeled content
public class DiscardLabeledSosofoObj : SosofoObj
{
    private SymbolObj? label_;
    private SosofoObj content_;

    public DiscardLabeledSosofoObj(SymbolObj? label, SosofoObj content)
    {
        label_ = label;
        content_ = content;
    }

    public override void process(ProcessContext context)
    {
        ((ProcessContextImpl)context).startDiscardLabeled(label_);
        content_.process(context);
        ((ProcessContextImpl)context).endDiscardLabeled();
    }
}

// PageTypeSosofoObj - conditional sosofo based on page type
public class PageTypeSosofoObj : SosofoObj
{
    private uint pageTypeFlag_;
    private SosofoObj match_;
    private SosofoObj noMatch_;

    public PageTypeSosofoObj(uint pageTypeFlag, SosofoObj match, SosofoObj noMatch)
    {
        pageTypeFlag_ = pageTypeFlag;
        match_ = match;
        noMatch_ = noMatch;
    }

    public override void process(ProcessContext context)
    {
        uint pageType;
        if (((ProcessContextImpl)context).getPageType(out pageType))
        {
            if ((pageType & pageTypeFlag_) != 0)
                match_.process(context);
            else
                noMatch_.process(context);
        }
    }
}

// NOTE: ProcessContext is defined in StyleEngine.cs
// NOTE: MergeStyleObj is defined in Style.cs
