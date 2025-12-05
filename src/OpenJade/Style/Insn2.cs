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
            vm.sp = 0;
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
            vm.sp = 0;
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
        // TODO: Get char property from current node
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
            vm.sp = 0;
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
            vm.sp = 0;
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
            obj.append((SosofoObj)vm.sbase[tem + i]!);
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
            vm.sp = 0;
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

    public override void process(ProcessContext context)
    {
        // TODO: implement
        throw new NotImplementedException();
    }
}

public class ProcessChildrenSosofoObj : SosofoObj
{
    private ProcessingMode processingMode_;

    public ProcessChildrenSosofoObj(ProcessingMode mode)
    {
        processingMode_ = mode;
    }

    public override void process(ProcessContext context)
    {
        context.processChildren(processingMode_);
    }
}

public class LabelSosofoObj : SosofoObj
{
    private SymbolObj sym_;
    private Location loc_;
    private SosofoObj content_;

    public LabelSosofoObj(SymbolObj sym, Location loc, SosofoObj content)
    {
        sym_ = sym;
        loc_ = loc;
        content_ = content;
    }

    public override void process(ProcessContext context)
    {
        // TODO: implement
        throw new NotImplementedException();
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
        // TODO: implement
        throw new NotImplementedException();
    }
}

// NOTE: ProcessContext is defined in StyleEngine.cs
