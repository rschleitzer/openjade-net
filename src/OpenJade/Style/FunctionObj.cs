// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Full FunctionObj implementation
public class FunctionObj : ELObj
{
    protected Signature? sig_;

    public FunctionObj() { sig_ = null; }
    public FunctionObj(Signature? sig) { sig_ = sig; }

    public int totalArgs()
    {
        if (sig_ == null) return 0;
        return sig_.nRequiredArgs + sig_.nOptionalArgs + sig_.nKeyArgs + (sig_.restArg ? 1 : 0);
    }

    public int nRequiredArgs() { return sig_?.nRequiredArgs ?? 0; }
    public int nOptionalArgs() { return sig_?.nOptionalArgs ?? 0; }
    public int nKeyArgs() { return sig_?.nKeyArgs ?? 0; }
    public bool restArg() { return sig_?.restArg ?? false; }

    public virtual Insn? call(VM vm, Location loc, Insn? next)
    {
        throw new NotImplementedException();
    }

    public virtual Insn? tailCall(VM vm, Location loc, int nCallerArgs)
    {
        throw new NotImplementedException();
    }

    public virtual InsnPtr makeCallInsn(int nArgs, Interpreter interp, Location loc, InsnPtr next)
    {
        return new InsnPtr(new FunctionCallInsn(nArgs, this, loc, next));
    }

    public virtual InsnPtr makeTailCallInsn(int nArgs, Interpreter interp, Location loc, int nCallerArgs)
    {
        return new InsnPtr(new FunctionTailCallInsn(nArgs, this, loc, nCallerArgs));
    }

    public Signature signature() { return sig_!; }

    public override FunctionObj? asFunction() { return this; }

    public virtual void setArgToCC(VM vm) { }
}

// Primitive (built-in) function
public abstract class PrimitiveObj : FunctionObj
{
    private Identifier? ident_;

    public PrimitiveObj(Signature sig) : base(sig) { }

    public override Insn? call(VM vm, Location loc, Insn? next)
    {
        if (vm.nActualArgs == 0)
            vm.needStack(1);
        int argp = vm.sp - vm.nActualArgs;
        ELObj?[] args = new ELObj?[vm.nActualArgs];
        for (int i = 0; i < vm.nActualArgs; i++)
            args[i] = vm.sbase[argp + i];
        vm.sbase[argp] = primitiveCall(vm.nActualArgs, args, vm, vm.interp, loc);
        vm.sp = argp + 1;
        if (vm.interp.isError(vm.sbase[argp]))
        {
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        else
            return next;
    }

    public override Insn? tailCall(VM vm, Location loc, int nCallerArgs)
    {
        int argp = vm.sp - vm.nActualArgs;
        ELObj?[] args = new ELObj?[vm.nActualArgs];
        for (int i = 0; i < vm.nActualArgs; i++)
            args[i] = vm.sbase[argp + i];
        ELObj? result = primitiveCall(vm.nActualArgs, args, vm, vm.interp, loc);
        if (vm.interp.isError(result))
        {
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        else
        {
            vm.sp = argp - nCallerArgs;
            Insn? next = vm.popFrame();
            vm.needStack(1);
            vm.sbase[vm.sp++] = result;
            return next;
        }
    }

    public override InsnPtr makeCallInsn(int nArgs, Interpreter interp, Location loc, InsnPtr next)
    {
        return new InsnPtr(new PrimitiveCallInsn(nArgs, this, loc, next));
    }

    public abstract ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc);

    public void setIdentifier(Identifier? ident) { ident_ = ident; }
    public Identifier? identifier() { return ident_; }

    protected ELObj? argError(Interpreter interp, Location loc, int index, ELObj? obj)
    {
        // TODO: proper error message
        interp.setNextLocation(loc);
        return interp.makeError();
    }

    protected ELObj? argError(Interpreter interp, Location loc, InterpreterMessages msg, int index, ELObj? obj)
    {
        interp.setNextLocation(loc);
        interp.message(msg, index, obj);
        return interp.makeError();
    }

    protected static ELObj? noCurrentNodeError(Interpreter interp, Location loc)
    {
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.noCurrentNode);
        return interp.makeError();
    }
}

// Apply primitive
public class ApplyPrimitiveObj : FunctionObj
{
    private static readonly Signature signature_ = new Signature(2, 0, true);

    public ApplyPrimitiveObj() : base(signature_) { }

    public override Insn? call(VM vm, Location loc, Insn? next)
    {
        if (!shuffle(vm, loc))
            return null;
        ApplyInsn insn = new ApplyInsn(vm.nActualArgs, loc, new InsnPtr(next));
        return insn.execute(vm);
    }

    public override Insn? tailCall(VM vm, Location loc, int nCallerArgs)
    {
        if (!shuffle(vm, loc))
            return null;
        TailApplyInsn insn = new TailApplyInsn(nCallerArgs, vm.nActualArgs, loc);
        return insn.execute(vm);
    }

    private bool shuffle(VM vm, Location loc)
    {
        int nArgs = vm.nActualArgs;
        ELObj? func = vm.sbase[vm.sp - nArgs];
        for (int i = nArgs - 2; i > 0; i--)
            vm.sbase[vm.sp - i - 2] = vm.sbase[vm.sp - i - 1];
        vm.nActualArgs = nArgs - 2;
        ELObj? list = vm.sbase[--vm.sp];
        --vm.sp;
        while (list != null && !list.isNil())
        {
            PairObj? tem = list.asPair();
            if (tem == null)
            {
                vm.interp.setNextLocation(loc);
                // vm.interp.message(InterpreterMessages.notAList, ...);
                vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
                return false;
            }
            vm.needStack(1);
            vm.nActualArgs++;
            vm.sbase[vm.sp++] = tem.car();
            list = tem.cdr();
        }
        vm.needStack(1);
        vm.sbase[vm.sp++] = func;
        return true;
    }
}

// Call with current continuation primitive
public class CallWithCurrentContinuationPrimitiveObj : FunctionObj
{
    private static readonly Signature signature_ = new Signature(1, 0, false);

    public CallWithCurrentContinuationPrimitiveObj() : base(signature_) { }

    public override Insn? call(VM vm, Location loc, Insn? next)
    {
        FunctionObj? f = vm.sbase[vm.sp - 1]?.asFunction();
        if (f == null)
        {
            vm.interp.setNextLocation(loc);
            // vm.interp.message(InterpreterMessages.notAProcedure, ...);
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        vm.sbase[vm.sp - 1] = new ContinuationObj();
        Insn? insn = f.call(vm, loc, next);
        f.setArgToCC(vm);
        return insn;
    }

    public override Insn? tailCall(VM vm, Location loc, int nCallerArgs)
    {
        FunctionObj? f = vm.sbase[vm.sp - 1]?.asFunction();
        if (f == null)
        {
            vm.interp.setNextLocation(loc);
            // vm.interp.message(InterpreterMessages.notAProcedure, ...);
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        vm.sbase[vm.sp - 1] = new ContinuationObj();
        Insn? insn = f.tailCall(vm, loc, nCallerArgs);
        f.setArgToCC(vm);
        return insn;
    }
}

// Closure object - user-defined function
public class ClosureObj : FunctionObj
{
    private InsnPtr code_;
    private ELObj?[]? display_;

    public ClosureObj(Signature sig, InsnPtr code, ELObj?[]? display)
        : base(sig)
    {
        code_ = code;
        display_ = display;
        hasSubObjects_ = (char)1;
    }

    public ELObj?[]? display() { return display_; }

    public override Insn? call(VM vm, Location loc, Insn? next)
    {
        vm.needStack(1);
        vm.pushFrame(next, vm.nActualArgs);
        vm.frameStart = vm.sp - vm.nActualArgs;
        vm.frame = vm.sbase;
        vm.closure = display_;
        vm.protectClosure = this;
        vm.closureLoc = loc;
        return code_.pointer();
    }

    public override Insn? tailCall(VM vm, Location loc, int nCallerArgs)
    {
        vm.needStack(1);
        int nArgs = vm.nActualArgs;
        if (nCallerArgs > 0)
        {
            int oldFrame = vm.sp - nArgs;
            int newFrame = oldFrame - nCallerArgs;
            for (int i = 0; i < nArgs; i++)
                vm.sbase[newFrame + i] = vm.sbase[oldFrame + i];
            vm.frameStart = newFrame;
            vm.sp = newFrame + nArgs;
        }
        else
            vm.frameStart = vm.sp - nArgs;
        vm.frame = vm.sbase;
        vm.closure = display_;
        vm.protectClosure = this;
        vm.closureLoc = loc;
        return code_.pointer();
    }

    public override void setArgToCC(VM vm)
    {
        vm.setClosureArgToCC();
    }

    public override void traceSubObjects(Collector c)
    {
        if (display_ != null)
        {
            for (int i = 0; display_[i] != null; i++)
                c.trace(display_[i]);
        }
    }

    public ELObj? display(int i) { return display_?[i]; }
}

// Continuation object
public class ContinuationObj : FunctionObj
{
    private static readonly Signature signature_ = new Signature(1, 0, false);

    private int stackSize_;
    private int controlStackSize_;

    public ContinuationObj() : base(signature_)
    {
        controlStackSize_ = 0;
    }

    public override Insn? call(VM vm, Location loc, Insn? next)
    {
        if (!live() || readOnly())
        {
            vm.interp.setNextLocation(loc);
            // vm.interp.message(InterpreterMessages.continuationDead);
            vm.sp = -1;  // -1 signals error (C++ uses NULL pointer)
            return null;
        }
        ELObj? result = vm.sbase[vm.sp - 1];
        System.Diagnostics.Debug.Assert(vm.sp >= stackSize_);
        System.Diagnostics.Debug.Assert(vm.csp >= controlStackSize_);
        System.Diagnostics.Debug.Assert(vm.csbase[controlStackSize_ - 1].continuation == this);
        while (vm.csp > controlStackSize_)
        {
            vm.csp--;
            if (vm.csbase[vm.csp].continuation != null)
                vm.csbase[vm.csp].continuation.kill();
        }
        vm.sp = stackSize_;
        --vm.sp;
        Insn? nextInsn = vm.popFrame();
        vm.sbase[vm.sp++] = result;
        return nextInsn;
    }

    public override Insn? tailCall(VM vm, Location loc, int nCallerArgs)
    {
        return call(vm, loc, null);
    }

    public void set(int stackSize, int controlStackSize)
    {
        stackSize_ = stackSize;
        controlStackSize_ = controlStackSize;
    }

    public void kill() { controlStackSize_ = 0; }
    public bool live() { return controlStackSize_ > 0; }
}

// Box object - mutable cell
public class BoxObj : ELObj
{
    public ELObj? value;

    public BoxObj()
    {
        value = null;
        hasSubObjects_ = (char)1;
    }

    public BoxObj(ELObj? obj)
    {
        value = obj;
        hasSubObjects_ = (char)1;
    }

    public override BoxObj? asBox() { return this; }

    public override void traceSubObjects(Collector c)
    {
        c.trace(value);
    }
}

// No-op procedure that accepts any arguments and returns empty sosofo
// Used as fallback for undefined external procedures
public class NoOpProcedureObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);  // rest arg accepts any number
    private string name_;

    public NoOpProcedureObj(string name) : base(sig)
    {
        name_ = name;
    }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        // Return the first argument (identity function behavior)
        // This is important for functions like debug that should pass through their argument
        if (args.Length > 0)
            return args[0];
        return interp.makeFalse();
    }
}
