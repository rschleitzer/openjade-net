// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Bound variable information
public struct BoundVar
{
    public Identifier? ident;
    public uint flags;
    public uint reboundCount;

    public const uint usedFlag = 0x01;
    public const uint assignedFlag = 0x02;
    public const uint uninitFlag = 0x04;
    public const uint sharedFlag = 0x08;

    public BoundVar(Identifier? id, uint f)
    {
        ident = id;
        flags = f;
        reboundCount = 0;
    }

    public bool boxed() { return flagsBoxed(flags); }

    public static bool flagsBoxed(uint flags)
    {
        return (flags & (assignedFlag | sharedFlag)) != 0;
    }
}

// List of bound variables
public class BoundVarList : System.Collections.Generic.List<BoundVar>
{
    public BoundVarList() { }

    public BoundVarList(BoundVarList other)
    {
        AddRange(other);
    }

    public BoundVarList(System.Collections.Generic.List<Identifier?> idents)
    {
        foreach (var id in idents)
            Add(new BoundVar(id, 0));
    }

    public BoundVarList(System.Collections.Generic.List<Identifier?> idents, int n, uint flags = 0)
    {
        for (int i = 0; i < n && i < idents.Count; i++)
            Add(new BoundVar(idents[i], flags & ~BoundVar.usedFlag));
    }

    public void resize(int n)
    {
        if (n < Count)
            RemoveRange(n, Count - n);
        else
        {
            while (Count < n)
                Add(new BoundVar(null, 0));
        }
    }

    public void append(Identifier? id, uint flags)
    {
        Add(new BoundVar(id, flags & ~BoundVar.usedFlag));
    }

    public void removeUnused()
    {
        int j = 0;
        for (int i = 0; i < Count; i++)
        {
            if ((this[i].flags & BoundVar.usedFlag) != 0)
            {
                if (j != i)
                    this[j] = this[i];
                j++;
            }
        }
        RemoveRange(j, Count - j);
    }

    public void mark(Identifier? ident, uint flags)
    {
        int idx = findIndex(ident);
        if (idx >= 0 && this[idx].reboundCount == 0)
        {
            var bv = this[idx];
            bv.flags |= flags;
            this[idx] = bv;
        }
    }

    public void rebind(System.Collections.Generic.List<Identifier?> idents)
    {
        foreach (var id in idents)
        {
            int idx = findIndex(id);
            if (idx >= 0)
            {
                var bv = this[idx];
                bv.reboundCount++;
                this[idx] = bv;
            }
        }
    }

    public void unbind(System.Collections.Generic.List<Identifier?> idents)
    {
        foreach (var id in idents)
        {
            int idx = findIndex(id);
            if (idx >= 0)
            {
                var bv = this[idx];
                bv.reboundCount--;
                this[idx] = bv;
            }
        }
    }

    private int findIndex(Identifier? ident)
    {
        for (int i = 0; i < Count; i++)
            if (this[i].ident == ident)
                return i;
        return -1;
    }

    public BoundVar? find(Identifier? ident)
    {
        int idx = findIndex(ident);
        return idx >= 0 ? this[idx] : (BoundVar?)null;
    }
}

// Frame variable list for environment
public class FrameVarList
{
    public BoundVarList? vars;
    public int stackPos;
    public FrameVarList? next;
}

// Compilation environment
public class Environment
{
    private BoundVarList? closureVars_;
    private FrameVarList? frameVarList_;

    public Environment()
    {
        closureVars_ = null;
        frameVarList_ = null;
    }

    public Environment(BoundVarList frameVars, BoundVarList closureVars)
    {
        closureVars_ = closureVars;
        frameVarList_ = new FrameVarList
        {
            vars = frameVars,
            stackPos = 0,
            next = null
        };
    }

    public bool lookup(Identifier? ident, out bool isFrame, out int index, out uint flags)
    {
        isFrame = false;
        index = 0;
        flags = 0;

        for (var p = frameVarList_; p != null; p = p.next)
        {
            if (p.vars != null)
            {
                for (int i = 0; i < p.vars.Count; i++)
                {
                    if (p.vars[i].ident == ident)
                    {
                        isFrame = true;
                        index = i + p.stackPos;
                        flags = p.vars[i].flags;
                        return true;
                    }
                }
            }
        }

        if (closureVars_ != null)
        {
            for (int i = 0; i < closureVars_.Count; i++)
            {
                if (closureVars_[i].ident == ident)
                {
                    isFrame = false;
                    index = i;
                    flags = closureVars_[i].flags;
                    return true;
                }
            }
        }

        return false;
    }

    public void boundVars(BoundVarList result)
    {
        if (closureVars_ != null)
        {
            foreach (var bv in closureVars_)
                result.append(bv.ident, bv.flags);
        }
        for (var f = frameVarList_; f != null; f = f.next)
        {
            if (f.vars != null)
            {
                foreach (var bv in f.vars)
                    result.append(bv.ident, bv.flags);
            }
        }
    }

    public void augmentFrame(BoundVarList vars, int stackPos)
    {
        frameVarList_ = new FrameVarList
        {
            stackPos = stackPos,
            vars = vars,
            next = frameVarList_
        };
    }
}

// Base expression class
public abstract class Expression
{
    protected Location loc_;

    public Expression(Location loc)
    {
        loc_ = loc;
    }

    public Location location() { return loc_; }

    public abstract InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next);

    public virtual void optimize(Interpreter interp, Environment env, ref Expression expr) { }
    public virtual ELObj? constantValue() { return null; }
    public virtual Identifier? keyword() { return null; }
    public virtual bool canEval(bool maybeCall) { return false; }
    public virtual void markBoundVars(BoundVarList vars, bool shared) { }

    public static InsnPtr optimizeCompile(Expression? expr, Interpreter interp, Environment env,
                                              int stackPos, InsnPtr next)
    {
        if (expr == null)
            return next;
        expr.optimize(interp, env, ref expr);
        return expr.compile(interp, env, stackPos, next);
    }

    protected InsnPtr compilePushVars(Interpreter interp, Environment env, int stackPos,
                                       BoundVarList vars, int varIndex, InsnPtr next)
    {
        if (varIndex >= vars.Count)
            return next;
        bool isFrame;
        int index;
        uint flags;
        if (!env.lookup(vars[varIndex].ident, out isFrame, out index, out flags))
            throw new InvalidOperationException("Cannot find bound variable");
        if (isFrame)
            return new InsnPtr(new FrameRefInsn(index,
                compilePushVars(interp, env, stackPos + 1, vars, varIndex + 1, next)));
        else
            return new InsnPtr(new ClosureRefInsn(index,
                compilePushVars(interp, env, stackPos + 1, vars, varIndex + 1, next)));
    }
}

// Constant expression
public class ConstantExpression : Expression
{
    private ELObj? obj_;

    public ConstantExpression(ELObj? obj, Location loc) : base(loc)
    {
        obj_ = obj;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        return new InsnPtr(new ConstantInsn(obj_, new InsnPtr(new ResolveQuantitiesInsn(location(), next))));
    }

    public override void optimize(Interpreter interp, Environment env, ref Expression expr)
    {
        ELObj? tem = obj_?.resolveQuantities(false, interp, location());
        if (tem != null)
        {
            interp.makePermanent(tem);
            expr = new ResolvedConstantExpression(tem, location());
        }
    }

    public override bool canEval(bool maybeCall) { return false; }

    public override Identifier? keyword()
    {
        KeywordObj? k = obj_?.asKeyword();
        return k?.identifier();
    }
}

// Resolved constant expression
public class ResolvedConstantExpression : Expression
{
    private ELObj? obj_;

    public ResolvedConstantExpression(ELObj? obj, Location loc) : base(loc)
    {
        obj_ = obj;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        return new InsnPtr(new ConstantInsn(obj_, next));
    }

    public override ELObj? constantValue() { return obj_; }
    public override bool canEval(bool maybeCall) { return true; }
}

// Variable expression
public class VariableExpression : Expression
{
    private Identifier ident_;
    private bool isTop_;

    public VariableExpression(Identifier ident, Location loc) : base(loc)
    {
        ident_ = ident;
        isTop_ = false;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        bool isFrame;
        int index;
        uint flags;
        if (env.lookup(ident_, out isFrame, out index, out flags))
        {
            bool boxed = BoundVar.flagsBoxed(flags);
            InsnPtr tem;
            int n;
            InsnPtr nextNext;
            if (isFrame && !next.isNull() && next.pointer()!.isPopBindings(out n, out nextNext)
                && n == 1 && index - stackPos == -1)
            {
                if ((flags & BoundVar.uninitFlag) != 0)
                    tem = new InsnPtr(new CheckInitInsn(ident_, location(), nextNext));
                else
                    tem = nextNext;
                if (boxed)
                    return new InsnPtr(new UnboxInsn(tem));
                else
                    return tem;
            }
            if ((flags & BoundVar.uninitFlag) != 0)
                tem = new InsnPtr(new CheckInitInsn(ident_, location(), next));
            else
                tem = next;
            if (boxed)
                tem = new InsnPtr(new UnboxInsn(tem));
            if (isFrame)
                return new InsnPtr(new StackRefInsn(index - stackPos, index, tem));
            else
                return new InsnPtr(new ClosureRefInsn(index, tem));
        }
        isTop_ = true;
        uint part;
        Location defLoc;
        if (!ident_.defined(out part, out defLoc))
        {
            interp.setNextLocation(location());
            // interp.message(InterpreterMessages.undefinedVariableReference, ...);
            return new InsnPtr(new ErrorInsn());
        }
        ELObj? val = ident_.computeValue(false, interp);
        if (val == null)
            return new InsnPtr(new TopRefInsn(ident_, next));
        if (interp.isError(val))
            return new InsnPtr(new ErrorInsn());
        return new InsnPtr(new ConstantInsn(val, next));
    }

    public override void optimize(Interpreter interp, Environment env, ref Expression expr)
    {
        bool isFrame;
        int index;
        uint flags;
        if (env.lookup(ident_, out isFrame, out index, out flags))
            return;
        isTop_ = true;
        uint part;
        Location defLoc;
        if (ident_.defined(out part, out defLoc))
        {
            ELObj? obj = ident_.computeValue(false, interp);
            if (obj != null && !interp.isError(obj))
            {
                interp.makePermanent(obj);
                expr = new ConstantExpression(obj, location());
                expr.optimize(interp, env, ref expr);
            }
        }
    }

    public override bool canEval(bool maybeCall)
    {
        return !isTop_ || ident_.evaluated();
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        vars.mark(ident_, BoundVar.usedFlag | (shared ? BoundVar.sharedFlag : 0u));
    }
}

// If expression
public class IfExpression : Expression
{
    private Expression test_;
    private Expression consequent_;
    private Expression? alternate_;

    public IfExpression(Expression test, Expression consequent, Expression? alternate, Location loc)
        : base(loc)
    {
        test_ = test;
        consequent_ = consequent;
        alternate_ = alternate;
    }

    public override bool canEval(bool maybeCall)
    {
        return test_.canEval(maybeCall) && consequent_.canEval(maybeCall) && (alternate_?.canEval(maybeCall) ?? true);
    }

    public override void optimize(Interpreter interp, Environment env, ref Expression expr)
    {
        test_.optimize(interp, env, ref test_);
        ELObj? obj = test_.constantValue();
        if (obj != null)
        {
            if (obj.isTrue())
            {
                expr = consequent_;
                expr.optimize(interp, env, ref expr);
            }
            else
            {
                expr = alternate_;
                expr.optimize(interp, env, ref expr);
            }
        }
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        if (alternate_ != null)
        {
            alternate_.optimize(interp, env, ref alternate_);
            if (alternate_.constantValue() == interp.makeFalse())
                return test_.compile(interp, env, stackPos,
                    new InsnPtr(new AndInsn(optimizeCompile(consequent_, interp, env, stackPos, next), next)));
            else
                return test_.compile(interp, env, stackPos,
                    new InsnPtr(new TestInsn(optimizeCompile(consequent_, interp, env, stackPos, next),
                        alternate_.compile(interp, env, stackPos, next))));
        }
        else
        {
            // No alternate: return unspecified value if test is false
            return test_.compile(interp, env, stackPos,
                new InsnPtr(new AndInsn(optimizeCompile(consequent_, interp, env, stackPos, next), next)));
        }
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        test_.markBoundVars(vars, shared);
        consequent_.markBoundVars(vars, shared);
        alternate_?.markBoundVars(vars, shared);
    }
}

// Or expression
public class OrExpression : Expression
{
    private Expression test1_;
    private Expression test2_;

    public OrExpression(Expression test1, Expression test2, Location loc) : base(loc)
    {
        test1_ = test1;
        test2_ = test2;
    }

    public override bool canEval(bool maybeCall)
    {
        return test1_.canEval(maybeCall) && test2_.canEval(maybeCall);
    }

    public override void optimize(Interpreter interp, Environment env, ref Expression expr)
    {
        test1_.optimize(interp, env, ref test1_);
        ELObj? obj = test1_.constantValue();
        if (obj != null)
        {
            if (obj.isTrue())
                expr = test1_;
            else
            {
                expr = test2_;
                expr.optimize(interp, env, ref expr);
            }
        }
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        return test1_.compile(interp, env, stackPos,
            new InsnPtr(new OrInsn(optimizeCompile(test2_, interp, env, stackPos, next), next)));
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        test1_.markBoundVars(vars, shared);
        test2_.markBoundVars(vars, shared);
    }
}

// And expression
public class AndExpression : Expression
{
    private Expression test1_;
    private Expression test2_;

    public AndExpression(Expression test1, Expression test2, Location loc) : base(loc)
    {
        test1_ = test1;
        test2_ = test2;
    }

    public override bool canEval(bool maybeCall)
    {
        return test1_.canEval(maybeCall) && test2_.canEval(maybeCall);
    }

    public override void optimize(Interpreter interp, Environment env, ref Expression expr)
    {
        test1_.optimize(interp, env, ref test1_);
        ELObj? obj = test1_.constantValue();
        if (obj != null)
        {
            if (!obj.isTrue())
                expr = new ConstantExpression(interp.makeFalse(), location());
            else
            {
                expr = test2_;
                expr.optimize(interp, env, ref expr);
            }
        }
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        return test1_.compile(interp, env, stackPos,
            new InsnPtr(new AndInsn(optimizeCompile(test2_, interp, env, stackPos, next), next)));
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        test1_.markBoundVars(vars, shared);
        test2_.markBoundVars(vars, shared);
    }
}

// Call expression
public class CallExpression : Expression
{
    private Expression op_;
    private System.Collections.Generic.List<Expression> args_;

    public CallExpression(Expression op, System.Collections.Generic.List<Expression> args, Location loc)
        : base(loc)
    {
        op_ = op;
        args_ = args;
    }

    public override bool canEval(bool maybeCall)
    {
        if (!op_.canEval(true))
            return false;
        foreach (var arg in args_)
            if (!arg.canEval(true))
                return false;
        return true;
    }

    public int nArgs() { return args_.Count; }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        op_.optimize(interp, env, ref op_);
        ELObj? value = op_.constantValue();
        InsnPtr result;
        if (value != null)
        {
            FunctionObj? func = value.asFunction();
            if (func == null)
            {
                interp.setNextLocation(location());
                // interp.message(InterpreterMessages.callNonFunction, ...);
                return new InsnPtr(new ErrorInsn());
            }
            if (nArgs() < func.nRequiredArgs())
            {
                interp.setNextLocation(location());
                // interp.message(InterpreterMessages.missingArg);
                return new InsnPtr(new ErrorInsn());
            }
            if (nArgs() - func.nRequiredArgs() > func.nOptionalArgs())
            {
                if (func.nKeyArgs() > 0)
                {
                    if ((nArgs() - func.nRequiredArgs() - func.nOptionalArgs()) % 2 == 1)
                    {
                        interp.setNextLocation(location());
                        // interp.message(InterpreterMessages.oddKeyArgs);
                    }
                }
                else if (!func.restArg())
                {
                    interp.setNextLocation(location());
                    // interp.message(InterpreterMessages.tooManyArgs);
                }
            }

            int callerArgs;
            if (!next.isNull() && next.pointer()!.isReturn(out callerArgs) && !interp.debugMode())
                result = func.makeTailCallInsn(nArgs(), interp, location(), callerArgs);
            else
                result = func.makeCallInsn(nArgs(), interp, location(), next);
        }
        else
        {
            int n = nArgs();
            int callerArgs;
            if (!next.isNull() && next.pointer()!.isReturn(out callerArgs) && !interp.debugMode())
                result = new InsnPtr(new TailApplyInsn(callerArgs, n, location()));
            else
                result = new InsnPtr(new ApplyInsn(n, location(), next));
            result = op_.compile(interp, env, stackPos + n, result);
        }
        for (int i = args_.Count - 1; i >= 0; i--)
            result = optimizeCompile(args_[i], interp, env, stackPos + i, result);
        return result;
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        op_.markBoundVars(vars, shared);
        foreach (var arg in args_)
            arg.markBoundVars(vars, shared);
    }
}

// Sequence expression
public class SequenceExpression : Expression
{
    private System.Collections.Generic.List<Expression> sequence_;

    public SequenceExpression(System.Collections.Generic.List<Expression> sequence, Location loc)
        : base(loc)
    {
        sequence_ = sequence;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        InsnPtr result = sequence_[sequence_.Count - 1].compile(interp, env, stackPos, next);
        for (int i = sequence_.Count - 2; i >= 0; i--)
            result = sequence_[i].compile(interp, env, stackPos, new InsnPtr(new PopInsn(result)));
        return result;
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        foreach (var expr in sequence_)
            expr.markBoundVars(vars, shared);
    }

    public override bool canEval(bool maybeCall)
    {
        foreach (var expr in sequence_)
            if (!expr.canEval(maybeCall))
                return false;
        return true;
    }

    public override void optimize(Interpreter interp, Environment env, ref Expression expr)
    {
        int j = 0;
        for (int i = 0; i < sequence_.Count; i++)
        {
            if (j != i)
                sequence_[j] = sequence_[i];
            Expression tem = sequence_[j];
            tem.optimize(interp, env, ref tem);
            sequence_[j] = tem;
            if (i == sequence_.Count - 1 || sequence_[j].constantValue() == null)
                j++;
        }
        if (j == 1)
            expr = sequence_[0];
        else
            sequence_.RemoveRange(j, sequence_.Count - j);
    }
}

// Let expression
public class LetExpression : Expression
{
    protected System.Collections.Generic.List<Identifier?> vars_;
    protected System.Collections.Generic.List<Expression> inits_;
    protected Expression body_;

    public LetExpression(System.Collections.Generic.List<Identifier?> vars,
                          System.Collections.Generic.List<Expression> inits,
                          Expression body, Location loc)
        : base(loc)
    {
        vars_ = vars;
        inits_ = inits;
        body_ = body;
    }

    public override bool canEval(bool maybeCall)
    {
        if (!body_.canEval(maybeCall))
            return false;
        foreach (var init in inits_)
            if (!init.canEval(true))
                return false;
        return true;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        int nVars = vars_.Count;
        Environment bodyEnv = new Environment();
        BoundVarList boundVars = new BoundVarList(vars_);
        body_.markBoundVars(boundVars, false);
        bodyEnv.augmentFrame(boundVars, stackPos);
        return compileInits(interp, env, boundVars, 0, stackPos,
            optimizeCompile(body_, interp, bodyEnv, stackPos + nVars,
                PopBindingsInsn.make(nVars, next)));
    }

    protected virtual InsnPtr compileInits(Interpreter interp, Environment env,
                                            BoundVarList initVars, int initIndex,
                                            int stackPos, InsnPtr next)
    {
        if (initIndex >= inits_.Count)
            return next;
        InsnPtr tem = compileInits(interp, env, initVars, initIndex + 1, stackPos + 1, next);
        if (initVars[initIndex].boxed())
            tem = new InsnPtr(new BoxInsn(tem));
        return optimizeCompile(inits_[initIndex], interp, env, stackPos, tem);
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        foreach (var init in inits_)
            init.markBoundVars(vars, shared);
        vars.rebind(vars_);
        body_.markBoundVars(vars, shared);
        vars.unbind(vars_);
    }
}

// Let* expression
public class LetStarExpression : LetExpression
{
    public LetStarExpression(System.Collections.Generic.List<Identifier?> vars,
                              System.Collections.Generic.List<Expression> inits,
                              Expression body, Location loc)
        : base(vars, inits, body, loc)
    {
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        int nVars = vars_.Count;
        Environment bodyEnv = new Environment();
        BoundVarList vars = new BoundVarList();
        for (int i = 0; i < nVars; i++)
        {
            if (i > 0)
                inits_[i].markBoundVars(vars, false);
            vars.append(vars_[i], 0);
        }
        body_.markBoundVars(vars, false);
        bodyEnv.augmentFrame(vars, stackPos);
        return compileInits(interp, env, vars, 0, stackPos,
            optimizeCompile(body_, interp, bodyEnv, stackPos + vars_.Count,
                PopBindingsInsn.make(nVars, next)));
    }

    protected override InsnPtr compileInits(Interpreter interp, Environment env,
                                             BoundVarList initVars, int initIndex,
                                             int stackPos, InsnPtr next)
    {
        if (initIndex >= inits_.Count)
            return next;
        Environment nextEnv = new Environment();
        BoundVarList vars = new BoundVarList();
        vars.append(initVars[initIndex].ident, initVars[initIndex].flags);
        nextEnv.augmentFrame(vars, stackPos);
        InsnPtr tem = compileInits(interp, nextEnv, initVars, initIndex + 1, stackPos + 1, next);
        if (initVars[initIndex].boxed())
            tem = new InsnPtr(new BoxInsn(tem));
        return optimizeCompile(inits_[initIndex], interp, env, stackPos, tem);
    }
}

// Letrec expression
public class LetrecExpression : Expression
{
    private System.Collections.Generic.List<Identifier?> vars_;
    private System.Collections.Generic.List<Expression> inits_;
    private Expression body_;

    public LetrecExpression(System.Collections.Generic.List<Identifier?> vars,
                             System.Collections.Generic.List<Expression> inits,
                             Expression body, Location loc)
        : base(loc)
    {
        vars_ = vars;
        inits_ = inits;
        body_ = body;
    }

    public override bool canEval(bool maybeCall)
    {
        if (!body_.canEval(maybeCall))
            return false;
        foreach (var init in inits_)
            if (!init.canEval(true))
                return false;
        return true;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        int nVars = vars_.Count;
        BoundVarList vars = new BoundVarList(vars_, nVars, BoundVar.assignedFlag);
        Environment bodyEnv = new Environment();
        for (int i = 0; i < nVars; i++)
            inits_[i].markBoundVars(vars, false);
        body_.markBoundVars(vars, false);
        bodyEnv.augmentFrame(vars, stackPos);
        InsnPtr tem = optimizeCompile(body_, interp, bodyEnv, stackPos + nVars,
            PopBindingsInsn.make(nVars, next));

        for (int i = 0; i < nVars; i++)
        {
            var bv = vars[i];
            bv.flags |= BoundVar.uninitFlag;
            vars[i] = bv;
        }

        for (int i = 0; i < nVars; i++)
        {
            if (vars[i].boxed())
                tem = new InsnPtr(new SetBoxInsn(nVars, tem));
            else
                tem = new InsnPtr(new SetImmediateInsn(nVars, tem));
        }
        tem = compileInits(interp, bodyEnv, 0, stackPos + nVars, tem);
        for (int i = nVars - 1; i >= 0; i--)
        {
            if (vars[i].boxed())
                tem = new InsnPtr(new BoxInsn(tem));
            tem = new InsnPtr(new ConstantInsn(null, tem));
        }
        return tem;
    }

    private InsnPtr compileInits(Interpreter interp, Environment env, int initIndex,
                                  int stackPos, InsnPtr next)
    {
        if (initIndex >= inits_.Count)
            return next;
        return optimizeCompile(inits_[initIndex], interp, env, stackPos,
            compileInits(interp, env, initIndex + 1, stackPos + 1, next));
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        vars.rebind(vars_);
        foreach (var init in inits_)
            init.markBoundVars(vars, shared);
        body_.markBoundVars(vars, shared);
        vars.unbind(vars_);
    }
}

// Cond fail expression - error when all cond clauses fail
public class CondFailExpression : Expression
{
    public CondFailExpression(Location loc) : base(loc) { }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        return new InsnPtr(new CondFailInsn(location()));
    }

    public override bool canEval(bool maybeCall) { return true; }
}

// Case expression
public class CaseExpression : Expression
{
    public struct Case
    {
        public System.Collections.Generic.List<ELObj?> datums;
        public Expression expr;
    }

    private Expression key_;
    private System.Collections.Generic.List<Case> cases_;
    private System.Collections.Generic.List<int> nResolved_;
    private Expression? else_;

    public CaseExpression(Expression key, System.Collections.Generic.List<Case> cases,
                          Expression? elseClause, Location loc)
        : base(loc)
    {
        key_ = key;
        cases_ = cases;
        else_ = elseClause;
        nResolved_ = new System.Collections.Generic.List<int>(cases.Count);
        for (int i = 0; i < cases.Count; i++)
            nResolved_.Add(0);
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        InsnPtr finish;
        if (else_ != null)
            finish = new InsnPtr(new PopInsn(else_.compile(interp, env, stackPos, next)));
        else
            finish = new InsnPtr(new CaseFailInsn(location()));
        for (int i = 0; i < cases_.Count; i++)
        {
            InsnPtr match = cases_[i].expr.compile(interp, env, stackPos, next);
            for (int j = 0; j < nResolved_[i]; j++)
                finish = new InsnPtr(new CaseInsn(cases_[i].datums[j], match, finish));
        }
        return key_.compile(interp, env, stackPos, finish);
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        key_.markBoundVars(vars, shared);
        foreach (var c in cases_)
            c.expr.markBoundVars(vars, shared);
        else_?.markBoundVars(vars, shared);
    }

    public override bool canEval(bool maybeCall)
    {
        if (!key_.canEval(maybeCall))
            return false;
        if (else_ != null && !else_.canEval(maybeCall))
            return false;
        for (int i = 0; i < cases_.Count; i++)
        {
            if (!cases_[i].expr.canEval(maybeCall))
                return false;
            if (nResolved_[i] == cases_[i].datums.Count)
                return false;
        }
        return true;
    }

    public override void optimize(Interpreter interp, Environment env, ref Expression expr)
    {
        key_.optimize(interp, env, ref key_);
        ELObj? k = key_.constantValue();
        for (int i = 0; i < nResolved_.Count; i++)
            nResolved_[i] = 0;
        bool unresolved = false;
        for (int i = 0; i < cases_.Count; i++)
        {
            Expression e = cases_[i].expr;
            e.optimize(interp, env, ref e);
            var c = cases_[i];
            c.expr = e;
            cases_[i] = c;
            int nResolved = 0;
            for (int j = 0; j < cases_[i].datums.Count; j++)
            {
                ELObj? tem = cases_[i].datums[j]?.resolveQuantities(false, interp, location());
                if (tem != null)
                {
                    if (k != null && ELObj.eqv(k, tem))
                    {
                        expr = cases_[i].expr;
                        return;
                    }
                    if (j != nResolved)
                        cases_[i].datums[j] = cases_[i].datums[nResolved];
                    cases_[i].datums[nResolved++] = tem;
                }
                else
                    unresolved = true;
            }
            nResolved_[i] = nResolved;
        }
        if (else_ != null)
        {
            else_.optimize(interp, env, ref else_);
            if (k != null && !unresolved)
                expr = else_;
        }
        else if (k != null && !unresolved)
        {
            interp.setNextLocation(location());
            // interp.message(InterpreterMessages.caseFail, ...);
        }
        if (unresolved)
        {
            interp.setNextLocation(location());
            // interp.message(InterpreterMessages.caseUnresolvedQuantities);
        }
    }
}

// Quasiquote expression
public class QuasiquoteExpression : Expression
{
    public enum Type
    {
        listType,
        improperType,
        vectorType
    }

    private System.Collections.Generic.List<Expression> members_;
    private System.Collections.Generic.List<bool> spliced_;
    private Type type_;

    public QuasiquoteExpression(System.Collections.Generic.List<Expression> members,
                                 System.Collections.Generic.List<bool> spliced,
                                 Type type, Location loc)
        : base(loc)
    {
        members_ = members;
        spliced_ = spliced;
        type_ = type;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        InsnPtr tem = next;
        int n = members_.Count;
        if (type_ == Type.vectorType)
        {
            bool splicy = false;
            for (int i = 0; i < n; i++)
            {
                if (spliced_[i])
                {
                    splicy = true;
                    break;
                }
            }
            if (!splicy)
            {
                tem = new InsnPtr(new VectorInsn(n, tem));
                for (int i = n; i > 0; i--)
                    tem = members_[i - 1].compile(interp, env, stackPos + (i - 1), tem);
                return tem;
            }
            tem = new InsnPtr(new ListToVectorInsn(tem));
        }
        else if (type_ == Type.improperType)
            n--;
        for (int i = 0; i < n; i++)
        {
            if (spliced_[i])
                tem = new InsnPtr(new AppendInsn(location(), tem));
            else
                tem = new InsnPtr(new ConsInsn(tem));
            tem = members_[i].compile(interp, env, stackPos + 1, tem);
        }
        if (type_ == Type.improperType)
            tem = members_[members_.Count - 1].compile(interp, env, stackPos, tem);
        else
            tem = new InsnPtr(new ConstantInsn(interp.makeNil(), tem));
        return tem;
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        foreach (var m in members_)
            m.markBoundVars(vars, shared);
    }

    public override bool canEval(bool maybeCall)
    {
        foreach (var m in members_)
            if (!m.canEval(maybeCall))
                return false;
        return true;
    }

    public override void optimize(Interpreter interp, Environment env, ref Expression expr)
    {
        for (int i = 0; i < members_.Count; i++)
        {
            Expression m = members_[i];
            m.optimize(interp, env, ref m);
            members_[i] = m;
        }
        if (type_ == Type.vectorType)
            return;
        if (members_.Count == 0)
        {
            expr = new ResolvedConstantExpression(interp.makeNil(), location());
            return;
        }
        ELObj? tail = members_[members_.Count - 1].constantValue();
        if (tail == null)
            return;
        System.Diagnostics.Debug.Assert(!(spliced_[spliced_.Count - 1] && type_ == Type.improperType));
        if (type_ != Type.improperType && !spliced_[spliced_.Count - 1])
        {
            tail = interp.makePair(tail, interp.makeNil());
            interp.makePermanent(tail);
        }
        for (int i = members_.Count - 1; i-- > 0;)
        {
            ELObj? tem = members_[i].constantValue();
            if (tem == null || spliced_[i])
            {
                var newMembers = new System.Collections.Generic.List<Expression>(members_.GetRange(0, i + 2));
                members_ = newMembers;
                type_ = Type.improperType;
                members_[i + 1] = new ResolvedConstantExpression(tail, location());
                return;
            }
            tail = interp.makePair(tem, tail);
            interp.makePermanent(tail);
        }
        expr = new ResolvedConstantExpression(tail, location());
    }
}

// Assignment expression (set!)
public class AssignmentExpression : Expression
{
    private Identifier var_;
    private Expression value_;

    public AssignmentExpression(Identifier var, Expression value, Location loc)
        : base(loc)
    {
        var_ = var;
        value_ = value;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        bool isFrame;
        int index;
        uint flags;
        if (!env.lookup(var_, out isFrame, out index, out flags))
        {
            interp.setNextLocation(location());
            uint part;
            Location defLoc;
            if (var_.defined(out part, out defLoc))
            {
                // interp.message(InterpreterMessages.topLevelAssignment, ...);
            }
            else
            {
                // interp.message(InterpreterMessages.undefinedVariableReference, ...);
            }
            return new InsnPtr(new ErrorInsn());
        }
        InsnPtr result;
        if ((flags & BoundVar.uninitFlag) != 0)
            result = new InsnPtr(new CheckInitInsn(var_, location(), next));
        else
            result = next;
        if (isFrame)
        {
            if (BoundVar.flagsBoxed(flags))
                result = new InsnPtr(new StackSetBoxInsn(index - (stackPos + 1), index, location(), result));
            else
                result = new InsnPtr(new StackSetInsn(index - (stackPos + 1), index, result));
        }
        else
        {
            System.Diagnostics.Debug.Assert(BoundVar.flagsBoxed(flags));
            result = new InsnPtr(new ClosureSetBoxInsn(index, location(), result));
        }
        return optimizeCompile(value_, interp, env, stackPos, result);
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        vars.mark(var_, BoundVar.usedFlag | BoundVar.assignedFlag | (shared ? BoundVar.sharedFlag : 0u));
        value_.markBoundVars(vars, shared);
    }

    public override bool canEval(bool maybeCall)
    {
        return value_.canEval(maybeCall);
    }
}

// With mode expression
public class WithModeExpression : Expression
{
    private ProcessingMode? mode_;
    private Expression expr_;

    public WithModeExpression(ProcessingMode? mode, Expression expr, Location loc)
        : base(loc)
    {
        mode_ = mode;
        expr_ = expr;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        if (mode_ != null && !mode_.defined())
        {
            interp.setNextLocation(location());
            // interp.message(InterpreterMessages.undefinedMode, ...);
        }
        return new InsnPtr(new PushModeInsn(mode_,
            optimizeCompile(expr_, interp, env, stackPos,
                new InsnPtr(new PopModeInsn(next)))));
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        expr_.markBoundVars(vars, shared);
    }

    public override bool canEval(bool maybeCall)
    {
        return expr_.canEval(maybeCall);
    }
}

// Style expression
public class StyleExpression : Expression
{
    protected System.Collections.Generic.List<Identifier?> keys_;
    protected System.Collections.Generic.List<Expression> exprs_;

    public StyleExpression(System.Collections.Generic.List<Identifier?> keys,
                           System.Collections.Generic.List<Expression> exprs,
                           Location loc)
        : base(loc)
    {
        keys_ = keys;
        exprs_ = exprs;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        var ics = new System.Collections.Generic.List<ConstPtr<InheritedC>?>();
        var forceIcs = new System.Collections.Generic.List<ConstPtr<InheritedC>?>();
        var forceKeys = new System.Collections.Generic.List<Identifier?>(keys_.Count);
        for (int i = 0; i < keys_.Count; i++)
        {
            forceKeys.Add(null);
            if (keys_[i] != null && keys_[i]!.name().size() > 6)
            {
                StringC prefix = new StringC(keys_[i]!.name().data(), (nuint)6);
                if (prefix.Equals(interp.makeStringC("force!")))
                {
                    StringC fullName = keys_[i]!.name();
                    StringC name = new StringC();
                    for (nuint idx = 6; idx < fullName.size(); idx++)
                        name.operatorPlusAssign(fullName[idx]);
                    forceKeys[i] = interp.lookup(name);
                }
            }
        }
        bool hasUse = false;
        int useIndex = 0;
        BoundVarList boundVars = new BoundVarList();
        env.boundVars(boundVars);
        for (int i = 0; i < keys_.Count; i++)
        {
            Identifier.SyntacticKey sk;
            if (forceKeys[i] != null
                && maybeStyleKeyword(forceKeys[i])
                && forceKeys[i]!.inheritedC() != null)
            {
                forceIcs.Add(null);
                exprs_[i].markBoundVars(boundVars, false);
            }
            else if (maybeStyleKeyword(keys_[i])
                && !(keys_[i]!.syntacticKey(out sk) && sk == Identifier.SyntacticKey.keyUse)
                && keys_[i]!.inheritedC() != null)
            {
                ics.Add(null);
                exprs_[i].markBoundVars(boundVars, false);
            }
        }
        boundVars.removeUnused();
        BoundVarList noVars = new BoundVarList();
        Environment newEnv = new Environment(noVars, boundVars);
        int j = 0;
        int k = 0;
        for (int i = 0; i < keys_.Count; i++)
        {
            Identifier.SyntacticKey sk;
            if (forceKeys[i] != null
                && maybeStyleKeyword(forceKeys[i])
                && forceKeys[i]!.inheritedC() != null)
            {
                Expression e = exprs_[i];
                e.optimize(interp, newEnv, ref e);
                exprs_[i] = e;
                ELObj? val = exprs_[i].constantValue();
                if (val != null)
                {
                    interp.makePermanent(val);
                    forceIcs[k] = forceKeys[i]!.inheritedC()!.pointer()!.make(val, exprs_[i].location(), interp);
                    if (forceIcs[k] == null)
                        forceIcs.RemoveAt(forceIcs.Count - 1);
                    else
                        k++;
                }
                else
                {
                    forceIcs[k++] = new ConstPtr<InheritedC>(new VarInheritedC(forceKeys[i]!.inheritedC()!,
                        exprs_[i].compile(interp, newEnv, 0, new InsnPtr()),
                        exprs_[i].location()));
                }
            }
            else if (!maybeStyleKeyword(keys_[i]))
            { /* skip non-style-keywords */ }
            else if (keys_[i]!.syntacticKey(out sk) && sk == Identifier.SyntacticKey.keyUse)
            {
                if (!hasUse)
                {
                    hasUse = true;
                    useIndex = i;
                }
            }
            else if (keys_[i]!.inheritedC() != null)
            {
                Expression e = exprs_[i];
                e.optimize(interp, newEnv, ref e);
                exprs_[i] = e;
                ELObj? val = exprs_[i].constantValue();
                if (val != null)
                {
                    interp.makePermanent(val);
                    ics[j] = keys_[i]!.inheritedC()!.pointer()!.make(val, exprs_[i].location(), interp);
                    if (ics[j] == null)
                        ics.RemoveAt(ics.Count - 1);
                    else
                        j++;
                }
                else
                {
                    ics[j++] = new ConstPtr<InheritedC>(new VarInheritedC(keys_[i]!.inheritedC()!,
                        exprs_[i].compile(interp, newEnv, 0, new InsnPtr()),
                        exprs_[i].location()));
                }
            }
            else
                unknownStyleKeyword(keys_[i]!, interp, location());
        }
        InsnPtr result = compilePushVars(interp, env, stackPos + (hasUse ? 1 : 0), boundVars, 0,
            new InsnPtr(new VarStyleInsn(new ConstPtr<StyleSpec>(new StyleSpec(forceIcs, ics)),
                boundVars.Count, hasUse,
                new InsnPtr(new MaybeOverrideStyleInsn(next)))));
        if (!hasUse)
            return result;
        else
        {
            result = new InsnPtr(new CheckStyleInsn(location(), result));
            return optimizeCompile(exprs_[useIndex], interp, env, stackPos, result);
        }
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        foreach (var expr in exprs_)
            expr.markBoundVars(vars, true);
    }

    public override bool canEval(bool maybeCall)
    {
        foreach (var expr in exprs_)
            if (!expr.canEval(maybeCall))
                return false;
        return true;
    }

    protected virtual void unknownStyleKeyword(Identifier ident, Interpreter interp, Location loc)
    {
        interp.setNextLocation(loc);
        StringC tem = new StringC(ident.name());
        tem.operatorPlusAssign((Char)':');
        // interp.message(InterpreterMessages.invalidStyleKeyword, ...);
    }

    protected virtual bool maybeStyleKeyword(Identifier? ident)
    {
        return true;
    }
}

// Make expression (make flow-object-class ...)
public class MakeExpression : StyleExpression
{
    private Identifier foc_;

    public MakeExpression(Identifier foc, System.Collections.Generic.List<Identifier?> keys,
                          System.Collections.Generic.List<Expression> exprs, Location loc)
        : base(keys, exprs, loc)
    {
        foc_ = foc;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        FlowObj? flowObj = foc_.flowObj();
        if (flowObj == null)
        {
            interp.setNextLocation(location());
            // interp.message(InterpreterMessages.unknownFlowObjectClass, ...);
            flowObj = new SequenceFlowObj();
            interp.makePermanent(flowObj);
        }
        Expression? contentMapExpr = null;
        InsnPtr rest = next;
        for (int i = 0; i < keys_.Count; i++)
        {
            Identifier.SyntacticKey syn;
            if (!flowObj.hasNonInheritedC(keys_[i]) && keys_[i] != null && keys_[i]!.syntacticKey(out syn))
            {
                if (syn == Identifier.SyntacticKey.keyLabel)
                    rest = optimizeCompile(exprs_[i], interp, env, stackPos + 1,
                        new InsnPtr(new LabelSosofoInsn(exprs_[i].location(), rest)));
                else if (syn == Identifier.SyntacticKey.keyContentMap)
                    contentMapExpr = exprs_[i];
            }
        }
        flowObj = applyConstNonInheritedCs(flowObj, interp, env);
        int nContent = exprs_.Count - keys_.Count;
        CompoundFlowObj? cFlowObj = flowObj.asCompoundFlowObj();
        if (cFlowObj == null && nContent > 0)
        {
            interp.setNextLocation(location());
            // interp.message(InterpreterMessages.atomicContent, ...);
            nContent = 0;
        }
        rest = compileNonInheritedCs(interp, env, stackPos + 1, rest);
        for (int i = 0; i < keys_.Count; i++)
        {
            if (flowObj.hasPseudoNonInheritedC(keys_[i])
                && exprs_[i].constantValue() == null)
            {
                rest = exprs_[i].compile(interp, env, stackPos + 1,
                    new InsnPtr(new SetPseudoNonInheritedCInsn(keys_[i],
                        exprs_[i].location(), rest)));
            }
        }
        rest = base.compile(interp, env, stackPos + 1, new InsnPtr(new SetStyleInsn(rest)));
        if (nContent == 0 && contentMapExpr == null)
        {
            if (cFlowObj != null)
                return new InsnPtr(new SetDefaultContentInsn(cFlowObj, location(), rest));
            else
                return new InsnPtr(new CopyFlowObjInsn(flowObj, rest));
        }
        rest = new InsnPtr(new SetContentInsn(cFlowObj, rest));
        if (contentMapExpr != null)
        {
            rest = optimizeCompile(contentMapExpr, interp, env, stackPos + 1,
                new InsnPtr(new ContentMapSosofoInsn(contentMapExpr.location(), rest)));
            if (nContent == 0)
                return new InsnPtr(new MakeDefaultContentInsn(location(), rest));
        }
        if (nContent == 1)
            return optimizeCompile(exprs_[exprs_.Count - 1], interp, env, stackPos,
                new InsnPtr(new CheckSosofoInsn(exprs_[exprs_.Count - 1].location(), rest)));
        rest = new InsnPtr(new SosofoAppendInsn(nContent, rest));
        for (int i = 1; i <= nContent; i++)
            rest = optimizeCompile(exprs_[exprs_.Count - i], interp, env, stackPos + nContent - i,
                new InsnPtr(new CheckSosofoInsn(exprs_[exprs_.Count - i].location(), rest)));
        return rest;
    }

    private FlowObj applyConstNonInheritedCs(FlowObj flowObj, Interpreter interp, Environment env)
    {
        FlowObj result = flowObj;
        for (int i = 0; i < keys_.Count; i++)
        {
            if (flowObj.hasNonInheritedC(keys_[i]) || flowObj.hasPseudoNonInheritedC(keys_[i]))
            {
                Expression e = exprs_[i];
                e.optimize(interp, env, ref e);
                exprs_[i] = e;
                ELObj? val = exprs_[i].constantValue();
                if (val != null)
                {
                    if (result == flowObj)
                    {
                        result = flowObj.copy(interp);
                        interp.makePermanent(result);
                    }
                    result.setNonInheritedC(keys_[i], val, exprs_[i].location(), interp);
                }
            }
        }
        return result;
    }

    private InsnPtr compileNonInheritedCs(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        FlowObj? flowObj = foc_.flowObj();
        if (flowObj == null)
            return next;
        bool gotOne = flowObj.isCharacter();
        BoundVarList boundVars = new BoundVarList();
        env.boundVars(boundVars);
        for (int i = 0; i < keys_.Count; i++)
        {
            if (flowObj.hasNonInheritedC(keys_[i]) && exprs_[i].constantValue() == null)
            {
                exprs_[i].markBoundVars(boundVars, false);
                gotOne = true;
            }
        }
        if (!gotOne)
            return next;
        boundVars.removeUnused();
        BoundVarList noVars = new BoundVarList();
        Environment newEnv = new Environment(noVars, boundVars);
        InsnPtr code = new InsnPtr();
        for (int i = 0; i < keys_.Count; i++)
            if (flowObj.hasNonInheritedC(keys_[i]) && exprs_[i].constantValue() == null)
                code = exprs_[i].compile(interp, newEnv, 1,
                    new InsnPtr(new SetNonInheritedCInsn(keys_[i], exprs_[i].location(), code)));
        InsnPtr rest = new InsnPtr(new SetNonInheritedCsSosofoInsn(code, boundVars.Count, next));
        if (flowObj.isCharacter())
            rest = new InsnPtr(new SetImplicitCharInsn(new Location(), rest));
        return compilePushVars(interp, env, stackPos, boundVars, 0, rest);
    }

    protected override void unknownStyleKeyword(Identifier ident, Interpreter interp, Location loc)
    {
        FlowObj? flowObj = foc_.flowObj();
        if (flowObj == null)
            return;
        Identifier.SyntacticKey key;
        if (ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyLabel:
                case Identifier.SyntacticKey.keyContentMap:
                    return;
                default:
                    break;
            }
        }
        if (flowObj.hasNonInheritedC(ident) || flowObj.hasPseudoNonInheritedC(ident))
            return;
        interp.setNextLocation(loc);
        StringC tem = new StringC(ident.name());
        tem.operatorPlusAssign((Char)':');
        // interp.message(InterpreterMessages.invalidMakeKeyword, ...);
    }

    protected override bool maybeStyleKeyword(Identifier? ident)
    {
        if (ident == null) return true;
        FlowObj? flowObj = foc_.flowObj();
        if (flowObj == null)
            return true;
        return !flowObj.hasNonInheritedC(ident) && !flowObj.hasPseudoNonInheritedC(ident);
    }
}

// Lambda expression
public class LambdaExpression : Expression
{
    private System.Collections.Generic.List<Identifier?> formals_;
    private System.Collections.Generic.List<Expression?> inits_;
    private Expression body_;
    private Signature sig_;

    public LambdaExpression(System.Collections.Generic.List<Identifier?> formals,
                             System.Collections.Generic.List<Expression?> inits,
                             int nOptional, bool hasRest, int nKey,
                             Expression body, Location loc)
        : base(loc)
    {
        formals_ = formals;
        inits_ = inits;
        body_ = body;
        sig_ = new Signature
        {
            nRequiredArgs = formals.Count - nOptional - nKey - (hasRest ? 1 : 0),
            nOptionalArgs = nOptional,
            restArg = hasRest,
            nKeyArgs = nKey,
            keys = nKey > 0 ? new Identifier?[nKey] : null
        };
        if (nKey > 0)
        {
            for (int i = 0; i < nKey; i++)
                sig_.keys![i] = formals[formals.Count - nKey + i];
        }
    }

    public override bool canEval(bool maybeCall)
    {
        if (!maybeCall)
            return true;
        if (!body_.canEval(true))
            return false;
        foreach (var init in inits_)
            if (init != null && !init.canEval(true))
                return false;
        return true;
    }

    public override InsnPtr compile(Interpreter interp, Environment env, int stackPos, InsnPtr next)
    {
        BoundVarList boundVars = new BoundVarList();
        env.boundVars(boundVars);
        markBoundVars(boundVars, false);
        boundVars.removeUnused();
        BoundVarList formalVars = new BoundVarList(formals_, sig_.nRequiredArgs);
        for (int i = 0; i < sig_.nOptionalArgs + sig_.nKeyArgs; i++)
        {
            if (i < inits_.Count && inits_[i] != null)
                inits_[i]!.markBoundVars(formalVars, false);
            formalVars.append(formals_[sig_.nRequiredArgs + i], 0);
        }
        if (sig_.restArg)
            formalVars.append(formals_[formals_.Count - 1], 0);
        body_.markBoundVars(formalVars, false);
        InsnPtr code = optimizeCompile(body_, interp,
            new Environment(formalVars, boundVars), formals_.Count,
            new InsnPtr(new ReturnInsn(formals_.Count)));

        // Handle optional/rest/key args varargs code generation
        if (sig_.nOptionalArgs > 0 || sig_.restArg || sig_.nKeyArgs > 0)
        {
            var entryPoints = new System.Collections.Generic.List<InsnPtr>(
                sig_.nOptionalArgs + (sig_.restArg || sig_.nKeyArgs > 0 ? 1 : 0) + 1);
            for (int i = 0; i < entryPoints.Capacity; i++)
                entryPoints.Add(new InsnPtr());

            // Last entry point is for all optional args supplied, and other args
            entryPoints[entryPoints.Count - 1] = code;

            // Box the rest arg if necessary
            if (sig_.restArg && formalVars[formalVars.Count - 1].boxed())
                entryPoints[entryPoints.Count - 1] = new InsnPtr(
                    new BoxStackInsn(-1 - sig_.nKeyArgs, entryPoints[entryPoints.Count - 1]));

            if (sig_.nKeyArgs > 0)
            {
                // For each keyword argument test whether it is null, and if so initialize it
                for (int i = sig_.nOptionalArgs + sig_.nKeyArgs - 1; i >= sig_.nOptionalArgs; i--)
                {
                    int offset = i - (sig_.nOptionalArgs + sig_.nKeyArgs);
                    InsnPtr set = new InsnPtr(new SetKeyArgInsn(offset, entryPoints[entryPoints.Count - 1]));
                    if (formalVars[sig_.nRequiredArgs + i].boxed())
                        set = new InsnPtr(new BoxInsn(set));
                    if (i < inits_.Count && inits_[i] != null)
                    {
                        BoundVarList f = new BoundVarList(formalVars);
                        f.resize(sig_.nRequiredArgs + i + (sig_.restArg ? 1 : 0));
                        set = optimizeCompile(inits_[i]!, interp,
                            new Environment(f, boundVars), formals_.Count, set);
                    }
                    else
                        set = new InsnPtr(new ConstantInsn(interp.makeFalse(), set));
                    entryPoints[entryPoints.Count - 1] = new InsnPtr(
                        new TestNullInsn(offset, set, entryPoints[entryPoints.Count - 1]));
                }
            }

            if (sig_.restArg || sig_.nKeyArgs > 0)
            {
                // Build code for when no rest/key args supplied
                var codeNoRest = code;
                for (int i = sig_.nOptionalArgs + sig_.nKeyArgs - 1; i >= sig_.nOptionalArgs; i--)
                {
                    if (formalVars[sig_.nRequiredArgs + i].boxed())
                        codeNoRest = new InsnPtr(new BoxInsn(codeNoRest));
                    if (i < inits_.Count && inits_[i] != null)
                    {
                        BoundVarList f = new BoundVarList(formalVars);
                        f.resize(sig_.nRequiredArgs + i + (sig_.restArg ? 1 : 0));
                        codeNoRest = optimizeCompile(inits_[i]!, interp,
                            new Environment(f, boundVars), f.Count, codeNoRest);
                    }
                    else
                        codeNoRest = new InsnPtr(new ConstantInsn(interp.makeFalse(), codeNoRest));
                }
                if (sig_.restArg)
                {
                    if (formalVars[formalVars.Count - 1].boxed())
                        codeNoRest = new InsnPtr(new BoxInsn(codeNoRest));
                    codeNoRest = new InsnPtr(new ConstantInsn(interp.makeNil(), codeNoRest));
                }
                entryPoints[sig_.nOptionalArgs] = codeNoRest;
            }

            // Build entry points for optional args
            for (int i = sig_.nOptionalArgs - 1; i >= 0; i--)
            {
                InsnPtr tem = entryPoints[i + 1];
                if (formalVars[sig_.nRequiredArgs + i].boxed())
                    tem = new InsnPtr(new BoxInsn(tem));
                if (i < inits_.Count && inits_[i] != null)
                {
                    BoundVarList f = new BoundVarList(formalVars);
                    f.resize(sig_.nRequiredArgs + i);
                    entryPoints[i] = optimizeCompile(inits_[i]!, interp,
                        new Environment(f, boundVars), f.Count, tem);
                }
                else
                    entryPoints[i] = new InsnPtr(new ConstantInsn(interp.makeFalse(), tem));
            }

            // Box optional args if needed in higher entry points
            for (int i = 0; i < sig_.nOptionalArgs; i++)
            {
                if (formalVars[sig_.nRequiredArgs + i].boxed())
                {
                    for (int j = i; j < sig_.nOptionalArgs; j++)
                        entryPoints[j + 1] = new InsnPtr(
                            new BoxArgInsn(i + sig_.nRequiredArgs, entryPoints[j + 1]));
                    if (sig_.nKeyArgs > 0 || sig_.restArg)
                        entryPoints[entryPoints.Count - 1] = new InsnPtr(
                            new BoxStackInsn(i - sig_.nKeyArgs - (sig_.restArg ? 1 : 0) - sig_.nOptionalArgs,
                                entryPoints[entryPoints.Count - 1]));
                }
            }

            code = new InsnPtr(new VarargsInsn(sig_, entryPoints, location()));
        }

        for (int i = 0; i < sig_.nRequiredArgs; i++)
            if (formalVars[i].boxed())
                code = new InsnPtr(new BoxArgInsn(i, code));
        return compilePushVars(interp, env, stackPos, boundVars, 0,
            new InsnPtr(new ClosureInsn(sig_, code, boundVars.Count, next)));
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        for (int i = 0; i < sig_.nOptionalArgs + sig_.nKeyArgs; i++)
        {
            if (inits_[i] != null)
            {
                var f = new System.Collections.Generic.List<Identifier?>(formals_);
                f.RemoveRange(sig_.nRequiredArgs + i, f.Count - sig_.nRequiredArgs - i);
                vars.rebind(f);
                inits_[i]!.markBoundVars(vars, true);
                vars.unbind(f);
            }
        }
        vars.rebind(formals_);
        body_.markBoundVars(vars, true);
        vars.unbind(formals_);
    }
}
