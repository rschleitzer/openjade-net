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
    private Expression alternate_;

    public IfExpression(Expression test, Expression consequent, Expression alternate, Location loc)
        : base(loc)
    {
        test_ = test;
        consequent_ = consequent;
        alternate_ = alternate;
    }

    public override bool canEval(bool maybeCall)
    {
        return test_.canEval(maybeCall) && consequent_.canEval(maybeCall) && alternate_.canEval(maybeCall);
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
        alternate_.optimize(interp, env, ref alternate_);
        if (alternate_.constantValue() == interp.makeFalse())
            return test_.compile(interp, env, stackPos,
                new InsnPtr(new AndInsn(optimizeCompile(consequent_, interp, env, stackPos, next), next)));
        else
            return test_.compile(interp, env, stackPos,
                new InsnPtr(new TestInsn(optimizeCompile(consequent_, interp, env, stackPos, next),
                    alternate_.compile(interp, env, stackPos, next))));
    }

    public override void markBoundVars(BoundVarList vars, bool shared)
    {
        test_.markBoundVars(vars, shared);
        consequent_.markBoundVars(vars, shared);
        alternate_.markBoundVars(vars, shared);
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
            if (inits_[i] != null)
                inits_[i]!.markBoundVars(formalVars, false);
            formalVars.append(formals_[sig_.nRequiredArgs + i], 0);
        }
        if (sig_.restArg)
            formalVars.append(formals_[formals_.Count - 1], 0);
        body_.markBoundVars(formalVars, false);
        InsnPtr code = optimizeCompile(body_, interp,
            new Environment(formalVars, boundVars), formals_.Count,
            new InsnPtr(new ReturnInsn(formals_.Count)));
        // TODO: Handle optional/rest/key args varargs code generation
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
