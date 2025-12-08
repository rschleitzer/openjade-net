// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Signature for function objects
public class Signature
{
    public int nRequiredArgs;
    public int nOptionalArgs;
    public bool restArg;
    public int nKeyArgs;
    public Identifier?[]? keys;

    public Signature()
    {
        nRequiredArgs = 0;
        nOptionalArgs = 0;
        restArg = false;
        nKeyArgs = 0;
        keys = null;
    }

    public Signature(int required, int optional, bool rest, int keyCount = 0, Identifier?[]? keyNames = null)
    {
        nRequiredArgs = required;
        nOptionalArgs = optional;
        restArg = rest;
        nKeyArgs = keyCount;
        keys = keyNames;
    }
}

// Instruction pointer with reference counting
public class InsnPtr : IResource
{
    private int refCount_ = 0;
    public int count() { return refCount_; }
    public void @ref() { refCount_++; }
    public int unref() { return --refCount_; }

    private Insn? insn_;

    public InsnPtr()
    {
        insn_ = null;
    }

    public InsnPtr(Insn? insn)
    {
        insn_ = insn;
    }

    public Insn? pointer() { return insn_; }
    public Insn? get() { return insn_; }
    public bool isNull() { return insn_ == null; }

    public static implicit operator InsnPtr(Insn? insn) => new InsnPtr(insn);
}

// Base instruction class
public abstract class Insn : Resource
{
    public virtual Insn? execute(VM vm) { throw new NotImplementedException(); }
    public virtual bool isReturn(out int nArgs) { nArgs = 0; return false; }
    public virtual bool isPopBindings(out int n, out InsnPtr next) { n = 0; next = new InsnPtr(); return false; }
}

// Error instruction
public class ErrorInsn : Insn
{
    public override Insn? execute(VM vm)
    {
        vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
        return null;
    }
}

// Cond fail instruction
public class CondFailInsn : ErrorInsn
{
    private Location loc_;

    public CondFailInsn(Location loc)
    {
        loc_ = loc;
    }

    public override Insn? execute(VM vm)
    {
        vm.interp.setNextLocation(loc_);
        Console.Error.WriteLine($"ERROR: no matching cond clause at index {loc_.index()}");
        return base.execute(vm);
    }
}

// Case fail instruction
public class CaseFailInsn : ErrorInsn
{
    private Location loc_;

    public CaseFailInsn(Location loc)
    {
        loc_ = loc;
    }

    public override Insn? execute(VM vm)
    {
        vm.interp.setNextLocation(loc_);
        // vm.interp.message(InterpreterMessages.caseFail, ELObjMessageArg(vm.sbase[vm.sp - 1], vm.interp));
        return base.execute(vm);
    }
}

// Constant instruction - pushes a constant value
public class ConstantInsn : Insn
{
    private ELObj? value_;
    private InsnPtr next_;

    public ConstantInsn(ELObj? obj, InsnPtr next)
    {
        value_ = obj;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        vm.needStack(1);
        vm.sbase[vm.sp++] = value_;
        return next_.pointer();
    }
}

// Resolve quantities instruction
public class ResolveQuantitiesInsn : Insn
{
    private Location loc_;
    private InsnPtr next_;

    public ResolveQuantitiesInsn(Location loc, InsnPtr next)
    {
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        ELObj? tem = vm.sbase[vm.sp - 1]?.resolveQuantities(true, vm.interp, loc_);
        System.Diagnostics.Debug.Assert(tem != null);
        if (vm.interp.isError(tem))
        {
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        vm.sbase[vm.sp - 1] = tem;
        return next_.pointer();
    }
}

// Test instruction - if/then/else
public class TestInsn : Insn
{
    private InsnPtr consequent_;
    private InsnPtr alternative_;

    public TestInsn(InsnPtr consequent, InsnPtr alternative)
    {
        consequent_ = consequent;
        alternative_ = alternative;
    }

    public override Insn? execute(VM vm)
    {
        return vm.sbase[--vm.sp]!.isTrue() ? consequent_.pointer() : alternative_.pointer();
    }
}

// Or instruction
public class OrInsn : Insn
{
    private InsnPtr nextTest_;
    private InsnPtr next_;

    public OrInsn(InsnPtr nextTest, InsnPtr next)
    {
        nextTest_ = nextTest;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        if (vm.sbase[vm.sp - 1]!.isTrue())
            return next_.pointer();
        --vm.sp;
        return nextTest_.pointer();
    }
}

// And instruction
public class AndInsn : Insn
{
    private InsnPtr nextTest_;
    private InsnPtr next_;

    public AndInsn(InsnPtr nextTest, InsnPtr next)
    {
        nextTest_ = nextTest;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        if (!vm.sbase[vm.sp - 1]!.isTrue())
            return next_.pointer();
        --vm.sp;
        return nextTest_.pointer();
    }
}

// Case instruction
public class CaseInsn : Insn
{
    private ELObj? obj_;
    private InsnPtr match_;
    private InsnPtr fail_;

    public CaseInsn(ELObj? obj, InsnPtr match, InsnPtr fail)
    {
        obj_ = obj;
        match_ = match;
        fail_ = fail;
    }

    public override Insn? execute(VM vm)
    {
        if (ELObj.eqv(vm.sbase[vm.sp - 1]!, obj_!))
        {
            --vm.sp;
            return match_.pointer();
        }
        return fail_.pointer();
    }
}

// Pop instruction
public class PopInsn : Insn
{
    private InsnPtr next_;

    public PopInsn(InsnPtr next)
    {
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        --vm.sp;
        return next_.pointer();
    }
}

// Cons instruction - builds a pair
public class ConsInsn : Insn
{
    private InsnPtr next_;

    public ConsInsn(InsnPtr next)
    {
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        vm.sbase[vm.sp - 2] = vm.interp.makePair(vm.sbase[vm.sp - 1], vm.sbase[vm.sp - 2]);
        --vm.sp;
        return next_.pointer();
    }
}

// Append instruction
public class AppendInsn : Insn
{
    private Location loc_;
    private InsnPtr next_;

    public AppendInsn(Location loc, InsnPtr next)
    {
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        ref ELObj? source = ref vm.sbase[vm.sp - 1];
        if (source != null && !source.isNil())
        {
            PairObj? pair = source.asPair();
            if (pair == null)
            {
                vm.interp.setNextLocation(loc_);
                // vm.interp.message(InterpreterMessages.spliceNotList);
                vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
                return null;
            }
            source = pair.cdr();
            PairObj tail = vm.interp.makePair(pair.car(), null);
            ELObj head = tail;
            while (source != null && !source.isNil())
            {
                pair = source.asPair();
                if (pair == null)
                {
                    vm.interp.setNextLocation(loc_);
                    // vm.interp.message(InterpreterMessages.spliceNotList);
                    vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
                    return null;
                }
                PairObj newTail = vm.interp.makePair(pair.car(), null);
                tail.setCdr(newTail);
                tail = newTail;
                source = pair.cdr();
            }
            tail.setCdr(vm.sbase[vm.sp - 2]);
            vm.sbase[vm.sp - 2] = head;
        }
        --vm.sp;
        return next_.pointer();
    }
}

// Apply base instruction
public class ApplyBaseInsn : Insn
{
    protected Location loc_;
    protected int nArgs_;

    public ApplyBaseInsn(int nArgs, Location loc)
    {
        nArgs_ = nArgs;
        loc_ = loc;
    }

    protected FunctionObj? decodeArgs(VM vm)
    {
        FunctionObj? func = vm.sbase[--vm.sp]?.asFunction();
        if (func == null)
        {
            vm.interp.setNextLocation(loc_);
            // vm.interp.message(InterpreterMessages.callNonFunction, ...);
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        int nReq = func.nRequiredArgs();
        if (nArgs_ < nReq)
        {
            vm.interp.setNextLocation(loc_);
            // vm.interp.message(InterpreterMessages.missingArg);
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        if (nArgs_ - nReq > func.nOptionalArgs())
        {
            if (func.nKeyArgs() > 0)
            {
                if ((nArgs_ - nReq - func.nOptionalArgs()) % 2 == 1)
                {
                    vm.interp.setNextLocation(loc_);
                    // vm.interp.message(InterpreterMessages.oddKeyArgs);
                    vm.sp -= (nArgs_ - nReq) - func.nOptionalArgs();
                }
            }
            else if (!func.restArg())
            {
                vm.interp.setNextLocation(loc_);
                // vm.interp.message(InterpreterMessages.tooManyArgs);
                vm.sp -= (nArgs_ - nReq) - func.nOptionalArgs();
            }
        }
        return func;
    }
}

// Apply instruction
public class ApplyInsn : ApplyBaseInsn
{
    private InsnPtr next_;

    public ApplyInsn(int nArgs, Location loc, InsnPtr next)
        : base(nArgs, loc)
    {
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        FunctionObj? func = decodeArgs(vm);
        vm.nActualArgs = nArgs_;
        if (func != null)
            return func.call(vm, loc_, next_.pointer());
        else
            return null;
    }
}

// Tail apply instruction
public class TailApplyInsn : ApplyBaseInsn
{
    private int nCallerArgs_;

    public TailApplyInsn(int nCallerArgs, int nArgs, Location loc)
        : base(nArgs, loc)
    {
        nCallerArgs_ = nCallerArgs;
    }

    public override Insn? execute(VM vm)
    {
        FunctionObj? func = decodeArgs(vm);
        vm.nActualArgs = nArgs_;
        if (func != null)
            return func.tailCall(vm, loc_, nCallerArgs_);
        else
            return null;
    }
}

// Frame reference instruction
public class FrameRefInsn : Insn
{
    private int index_;
    private InsnPtr next_;

    public FrameRefInsn(int index, InsnPtr next)
    {
        index_ = index;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        vm.needStack(1);
        vm.sbase[vm.sp++] = vm.sbase[vm.frameStart + index_];
        return next_.pointer();
    }
}

// Stack reference instruction
public class StackRefInsn : Insn
{
    private int index_;        // always negative
    private int frameIndex_;
    private InsnPtr next_;

    public StackRefInsn(int index, int frameIndex, InsnPtr next)
    {
        index_ = index;
        frameIndex_ = frameIndex;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        vm.needStack(1);
        System.Diagnostics.Debug.Assert(vm.sp - vm.frameStart == frameIndex_ - index_);
        vm.sbase[vm.sp] = vm.sbase[vm.sp + index_];
        vm.sp += 1;
        return next_.pointer();
    }
}

// Closure reference instruction
public class ClosureRefInsn : Insn
{
    private int index_;
    private InsnPtr next_;

    public ClosureRefInsn(int index, InsnPtr next)
    {
        index_ = index;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        vm.needStack(1);
        vm.sbase[vm.sp++] = vm.closure?[index_];
        return next_.pointer();
    }
}

// Top reference instruction (global variable)
public class TopRefInsn : Insn
{
    private Identifier var_;
    private InsnPtr next_;
#pragma warning disable CS0169, CS8618 // Field never used (port stub)
    private Location loc_;
#pragma warning restore CS0169, CS8618

    public TopRefInsn(Identifier var, InsnPtr next)
    {
        var_ = var;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        ELObj? tem = var_.computeValue(true, vm.interp);
        if (vm.interp.isError(tem) || tem == null)
        {
            // Undefined variable - return a no-op procedure to allow continuation
            vm.needStack(1);
            vm.sbase[vm.sp++] = new NoOpProcedureObj(var_.name().ToString());
            return next_.pointer();
        }
        else
        {
            vm.needStack(1);
            vm.sbase[vm.sp++] = tem;
            return next_.pointer();
        }
    }
}

// Pop bindings instruction
public class PopBindingsInsn : Insn
{
    private int n_;
    private InsnPtr next_;

    private PopBindingsInsn(int n, InsnPtr next)
    {
        n_ = n;
        next_ = next;
    }

    public static InsnPtr make(int n, InsnPtr next)
    {
        if (!next.isNull())
        {
            int i;
            if (next.pointer()!.isReturn(out i))
                return new InsnPtr(new ReturnInsn(n + i));
            InsnPtr nextNext;
            if (next.pointer()!.isPopBindings(out i, out nextNext))
                return new InsnPtr(new PopBindingsInsn(n + i, nextNext));
        }
        return new InsnPtr(new PopBindingsInsn(n, next));
    }

    public override Insn? execute(VM vm)
    {
        vm.sp -= n_;
        vm.sbase[vm.sp - 1] = vm.sbase[vm.sp + n_ - 1];
        return next_.pointer();
    }

    public override bool isPopBindings(out int n, out InsnPtr next)
    {
        n = n_;
        next = next_;
        return true;
    }
}

// Return instruction
public class ReturnInsn : Insn
{
    private int totalArgs_;

    public ReturnInsn(int totalArgs)
    {
        totalArgs_ = totalArgs;
    }

    public override bool isReturn(out int nArgs)
    {
        nArgs = totalArgs_;
        return true;
    }

    public override Insn? execute(VM vm)
    {
        ELObj? result = vm.sbase[--vm.sp];
        vm.sp -= totalArgs_;
        Insn? next = vm.popFrame();
        vm.sbase[vm.sp++] = result;
        return next;
    }
}

// Primitive call instruction
public class PrimitiveCallInsn : Insn
{
    private int nArgs_;
    private PrimitiveObj prim_;
    private Location loc_;
    private InsnPtr next_;

    public PrimitiveCallInsn(int nArgs, PrimitiveObj prim, Location loc, InsnPtr next)
    {
        nArgs_ = nArgs;
        prim_ = prim;
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        if (nArgs_ == 0)
            vm.needStack(1);
        int argp = vm.sp - nArgs_;
        ELObj?[] args = new ELObj?[nArgs_];
        for (int i = 0; i < nArgs_; i++)
            args[i] = vm.sbase[argp + i];
        vm.sbase[argp] = prim_.primitiveCall(nArgs_, args, vm, vm.interp, loc_);
        vm.sp = argp + 1;
        if (vm.interp.isError(vm.sbase[argp]))
        {
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        else
            return next_.pointer();
    }
}

// Closure instruction
public class ClosureInsn : Insn
{
    private Signature sig_;
    private InsnPtr code_;
    private int displayLength_;
    private InsnPtr next_;

    public ClosureInsn(Signature sig, InsnPtr code, int displayLength, InsnPtr next)
    {
        sig_ = sig;
        code_ = code;
        displayLength_ = displayLength;
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
        vm.sbase[tem++] = new ClosureObj(sig_, code_, display);
        vm.sp = tem;
        return next_.pointer();
    }
}

// Function call instruction
public class FunctionCallInsn : Insn
{
    private int nArgs_;
    private FunctionObj function_;
    private Location loc_;
    private InsnPtr next_;

    public FunctionCallInsn(int nArgs, FunctionObj function, Location loc, InsnPtr next)
    {
        nArgs_ = nArgs;
        function_ = function;
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        vm.nActualArgs = nArgs_;
        return function_.call(vm, loc_, next_.pointer());
    }
}

// Function tail call instruction
public class FunctionTailCallInsn : Insn
{
    private int nArgs_;
    private FunctionObj function_;
    private Location loc_;
    private int nCallerArgs_;

    public FunctionTailCallInsn(int nArgs, FunctionObj function, Location loc, int nCallerArgs)
    {
        nArgs_ = nArgs;
        function_ = function;
        loc_ = loc;
        nCallerArgs_ = nCallerArgs;
    }

    public override Insn? execute(VM vm)
    {
        vm.nActualArgs = nArgs_;
        return function_.tailCall(vm, loc_, nCallerArgs_);
    }
}

// Varargs instruction
public class VarargsInsn : Insn
{
    private Signature sig_;
    private System.Collections.Generic.List<InsnPtr> entryPoints_;
    private Location loc_;

    public VarargsInsn(Signature sig, System.Collections.Generic.List<InsnPtr> entryPoints, Location loc)
    {
        sig_ = sig;
        entryPoints_ = entryPoints;
        loc_ = loc;
    }

    public override Insn? execute(VM vm)
    {
        int n = vm.nActualArgs - sig_.nRequiredArgs;
        if ((sig_.restArg || sig_.nKeyArgs > 0) && n > entryPoints_.Count - 2)
        {
            // cons up the rest args
            ELObj rest = vm.interp.makeNil();
            for (int i = n - (entryPoints_.Count - 2); i > 0; i--)
            {
                rest = new PairObj(vm.sbase[--vm.sp], rest);
            }
            vm.needStack(sig_.nKeyArgs + (sig_.restArg ? 1 : 0));
            if (sig_.restArg)
                vm.sbase[vm.sp++] = rest;
            if (sig_.nKeyArgs > 0)
            {
                for (int i = 0; i < sig_.nKeyArgs; i++)
                    vm.sbase[vm.sp + i] = null;
                ELObj? tem = rest;
                for (int i = n - (entryPoints_.Count - 2); i > 0; i -= 2)
                {
                    KeywordObj? k = ((PairObj)tem!).car()?.asKeyword();
                    tem = ((PairObj)tem).cdr();
                    if (k != null)
                    {
                        for (int j = 0; j < sig_.nKeyArgs; j++)
                        {
                            if (sig_.keys![j] == k.identifier())
                            {
                                if (vm.sbase[vm.sp + j] == null)
                                    vm.sbase[vm.sp + j] = ((PairObj)tem!).car();
                                k = null;
                                break;
                            }
                        }
                        if (k != null && !sig_.restArg)
                        {
                            vm.interp.setNextLocation(loc_);
                            // vm.interp.message(InterpreterMessages.invalidKeyArg, ...);
                        }
                    }
                    else
                    {
                        vm.interp.setNextLocation(loc_);
                        // vm.interp.message(InterpreterMessages.keyArgsNotKey);
                    }
                    tem = ((PairObj)tem!).cdr();
                }
                vm.sp += sig_.nKeyArgs;
            }
            return entryPoints_[entryPoints_.Count - 1].pointer();
        }
        return entryPoints_[n].pointer();
    }
}

// Set key arg instruction
public class SetKeyArgInsn : Insn
{
    private int offset_;
    private InsnPtr next_;

    public SetKeyArgInsn(int offset, InsnPtr next)
    {
        offset_ = offset;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        ELObj? val = vm.sbase[--vm.sp];
        vm.sbase[vm.sp + offset_] = val;
        return next_.pointer();
    }
}

// Test null instruction
public class TestNullInsn : Insn
{
    private int offset_;
    private InsnPtr ifNull_;
    private InsnPtr ifNotNull_;

    public TestNullInsn(int offset, InsnPtr ifNull, InsnPtr ifNotNull)
    {
        offset_ = offset;
        ifNull_ = ifNull;
        ifNotNull_ = ifNotNull;
    }

    public override Insn? execute(VM vm)
    {
        if (vm.sbase[vm.sp + offset_] == null)
            return ifNull_.pointer();
        else
            return ifNotNull_.pointer();
    }
}

// Box instruction - creates a mutable box
public class BoxInsn : Insn
{
    private InsnPtr next_;

    public BoxInsn(InsnPtr next)
    {
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        vm.sbase[vm.sp - 1] = new BoxObj(vm.sbase[vm.sp - 1]);
        return next_.pointer();
    }
}

// Box arg instruction
public class BoxArgInsn : Insn
{
    private int n_;
    private InsnPtr next_;

    public BoxArgInsn(int n, InsnPtr next)
    {
        n_ = n;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        int idx = vm.sp + n_ - vm.nActualArgs;
        vm.sbase[idx] = new BoxObj(vm.sbase[idx]);
        return next_.pointer();
    }
}

// Box stack instruction
public class BoxStackInsn : Insn
{
    private int n_;
    private InsnPtr next_;

    public BoxStackInsn(int n, InsnPtr next)
    {
        n_ = n;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        vm.sbase[vm.sp + n_] = new BoxObj(vm.sbase[vm.sp + n_]);
        return next_.pointer();
    }
}

// Unbox instruction
public class UnboxInsn : Insn
{
    private InsnPtr next_;

    public UnboxInsn(InsnPtr next)
    {
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        BoxObj? box = vm.sbase[vm.sp - 1]?.asBox();
        System.Diagnostics.Debug.Assert(box != null);
        vm.sbase[vm.sp - 1] = box!.value;
        return next_.pointer();
    }
}

// Set box instruction
public class SetBoxInsn : Insn
{
    private int n_;
    private InsnPtr next_;

    public SetBoxInsn(int n, InsnPtr next)
    {
        n_ = n;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        --vm.sp;
        BoxObj? box = vm.sbase[vm.sp - n_]?.asBox();
        System.Diagnostics.Debug.Assert(box != null);
        box!.value = vm.sbase[vm.sp];
        return next_.pointer();
    }
}

// Set immediate instruction
public class SetImmediateInsn : Insn
{
    private int n_;
    private InsnPtr next_;

    public SetImmediateInsn(int n, InsnPtr next)
    {
        n_ = n;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        --vm.sp;
        vm.sbase[vm.sp - n_] = vm.sbase[vm.sp];
        return next_.pointer();
    }
}

// Check init instruction
public class CheckInitInsn : Insn
{
    private Identifier ident_;
    private Location loc_;
    private InsnPtr next_;

    public CheckInitInsn(Identifier ident, Location loc, InsnPtr next)
    {
        ident_ = ident;
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        if (vm.sbase[vm.sp - 1] == null)
        {
            vm.interp.setNextLocation(loc_);
            // vm.interp.message(InterpreterMessages.uninitializedVariableReference, ...);
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        return next_.pointer();
    }
}

// Stack set box instruction
public class StackSetBoxInsn : Insn
{
    private int index_;
    private int frameIndex_;
    private Location loc_;
    private InsnPtr next_;

    public StackSetBoxInsn(int index, int frameIndex, Location loc, InsnPtr next)
    {
        index_ = index;
        frameIndex_ = frameIndex;
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        System.Diagnostics.Debug.Assert(vm.sp - vm.frameStart == frameIndex_ - index_);
        BoxObj? box = vm.sbase[vm.sp + index_]?.asBox();
        System.Diagnostics.Debug.Assert(box != null);
        if (box!.readOnly())
        {
            vm.interp.setNextLocation(loc_);
            // vm.interp.message(InterpreterMessages.readOnly);
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        ELObj? tem = box.value;
        box.value = vm.sbase[vm.sp - 1];
        vm.sbase[vm.sp - 1] = tem;
        return next_.pointer();
    }
}

// Stack set instruction
public class StackSetInsn : Insn
{
    private int index_;
    private int frameIndex_;
    private InsnPtr next_;

    public StackSetInsn(int index, int frameIndex, InsnPtr next)
    {
        index_ = index;
        frameIndex_ = frameIndex;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        System.Diagnostics.Debug.Assert(vm.sp - vm.frameStart == frameIndex_ - index_);
        ELObj? tem = vm.sbase[vm.sp + index_];
        vm.sbase[vm.sp + index_] = vm.sbase[vm.sp - 1];
        vm.sbase[vm.sp - 1] = tem;
        return next_.pointer();
    }
}

// Closure set box instruction
public class ClosureSetBoxInsn : Insn
{
    private int index_;
    private Location loc_;
    private InsnPtr next_;

    public ClosureSetBoxInsn(int index, Location loc, InsnPtr next)
    {
        index_ = index;
        loc_ = loc;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        BoxObj? box = vm.closure?[index_]?.asBox();
        System.Diagnostics.Debug.Assert(box != null);
        if (box!.readOnly())
        {
            vm.interp.setNextLocation(loc_);
            // vm.interp.message(InterpreterMessages.readOnly);
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        ELObj? tem = box.value;
        box.value = vm.sbase[vm.sp - 1];
        vm.sbase[vm.sp - 1] = tem;
        return next_.pointer();
    }
}

// Vector instruction
public class VectorInsn : Insn
{
    private int n_;
    private InsnPtr next_;

    public VectorInsn(int n, InsnPtr next)
    {
        n_ = n;
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        if (n_ == 0)
        {
            vm.needStack(1);
            vm.sbase[vm.sp++] = new VectorObj();
        }
        else
        {
            var v = new System.Collections.Generic.List<ELObj?>(n_);
            int p = vm.sp;
            for (int n = n_; n > 0; n--)
                v.Insert(0, vm.sbase[--p]);
            vm.sbase[p] = new VectorObj(v);
            vm.sp = p + 1;
        }
        return next_.pointer();
    }
}

// List to vector instruction
public class ListToVectorInsn : Insn
{
    private InsnPtr next_;

    public ListToVectorInsn(InsnPtr next)
    {
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        var v = new System.Collections.Generic.List<ELObj?>();
        ELObj? obj = vm.sbase[vm.sp - 1];
        while (obj != null && !obj.isNil())
        {
            PairObj? pair = obj.asPair();
            System.Diagnostics.Debug.Assert(pair != null);
            v.Add(pair!.car());
            obj = pair.cdr();
        }
        vm.sbase[vm.sp - 1] = new VectorObj(v);
        return next_.pointer();
    }
}
