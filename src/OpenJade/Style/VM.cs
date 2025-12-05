// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Control stack entry for VM
public struct ControlStackEntry
{
    public ELObj?[]? closure;
    public ClosureObj? protectClosure;
    public Insn? next;
    public int frameSize;
    public Location closureLoc;
    public ContinuationObj? continuation;
}

// Full VM implementation for DSSSL execution
public partial class VM : EvalContext
{
    public Interpreter interp;

    // Evaluation stack
    public ELObj?[] sbase = Array.Empty<ELObj?>();
    public int sp;        // stack pointer (index into sbase)
    public int slim;      // stack limit
    public ELObj?[]? frame;  // current frame (reference to sbase slice)
    public int frameStart;   // index in sbase where frame starts

    // Closure and display
    public ELObj?[]? closure;
    public ClosureObj? protectClosure;
    public Location closureLoc = new Location();

    // Control stack
    public ControlStackEntry[] csbase = Array.Empty<ControlStackEntry>();
    public int csp;       // control stack pointer
    public int cslim;     // control stack limit

    // Processing mode stack
    public System.Collections.Generic.List<ProcessingMode?> modeStack = new();

    // Call arguments
    public int nActualArgs;

    public VM(Interpreter interpreter)
    {
        interp = interpreter;
        init();
    }

    public VM(EvalContext context, Interpreter interpreter)
    {
        copyFrom(context);
        interp = interpreter;
        init();
    }

    private void init()
    {
        slim = 0;
        sbase = Array.Empty<ELObj?>();
        sp = 0;
        closure = null;
        frame = null;
        frameStart = 0;
        protectClosure = null;
        csp = 0;
        cslim = 0;
        csbase = Array.Empty<ControlStackEntry>();
        closureLoc = new Location();
        modeStack = new System.Collections.Generic.List<ProcessingMode?>();
        nActualArgs = 0;
    }

    public void initStack()
    {
        sp = 0;
        frameStart = 0;
        frame = sbase;
        csp = 0;
        modeStack.Clear();
    }

    public void needStack(int n)
    {
        if (sp + n > slim)
            growStack(n);
    }

    public void growStack(int n)
    {
        int newSize = sp;
        if (n > newSize)
            newSize += (n + 15) & ~15;
        else
            newSize += newSize;
        if (newSize < 64)
            newSize = 64;
        ELObj?[] newStack = new ELObj?[newSize];
        slim = newSize;
        if (sbase.Length > 0)
            Array.Copy(sbase, newStack, sp);
        sbase = newStack;
        frame = sbase;
    }

    public void trace(Collector c)
    {
        if (sbase != null)
        {
            for (int i = 0; i < sp; i++)
                if (sbase[i] != null)
                    c.trace(sbase[i]);
        }
        for (int i = 0; i < csp; i++)
        {
            if (csbase[i].protectClosure != null)
                c.trace(csbase[i].protectClosure);
            if (csbase[i].continuation != null)
                c.trace(csbase[i].continuation);
        }
        if (protectClosure != null)
            c.trace(protectClosure);
    }

    public void pushFrame(Insn? next, int argsPushed)
    {
        if (csp >= cslim)
        {
            int newSize = csbase.Length > 0 ? csbase.Length * 2 : 8;
            ControlStackEntry[] newBase = new ControlStackEntry[newSize];
            cslim = newSize;
            for (int i = 0; i < csp; i++)
                newBase[i] = csbase[i];
            csbase = newBase;
        }
        csbase[csp].closure = closure;
        csbase[csp].protectClosure = protectClosure;
        csbase[csp].next = next;
        csbase[csp].frameSize = sp - frameStart - argsPushed;
        csbase[csp].closureLoc = closureLoc;
        csbase[csp].continuation = null;
        csp++;
    }

    public Insn? popFrame()
    {
        System.Diagnostics.Debug.Assert(csp > 0);
        --csp;
        if (csbase[csp].continuation != null)
            csbase[csp].continuation.kill();
        closure = csbase[csp].closure;
        protectClosure = csbase[csp].protectClosure;
        frameStart = sp - csbase[csp].frameSize;
        frame = sbase;
        closureLoc = csbase[csp].closureLoc;
        return csbase[csp].next;
    }

    public void setClosureArgToCC()
    {
        System.Diagnostics.Debug.Assert(nActualArgs == 1);
        ContinuationObj cc = (ContinuationObj)sbase[sp - 1]!;
        csbase[csp - 1].continuation = cc;
        cc.set(sp, csp);
    }

    public ELObj? eval(Insn? insn, ELObj?[]? display = null, ELObj? arg = null)
    {
        initStack();
        if (arg != null)
        {
            needStack(1);
            sbase[sp++] = arg;
        }
        closure = display;
        protectClosure = null;
        closureLoc = new Location();

        // The inner execution loop
        while (insn != null)
            insn = insn.execute(this);

        ELObj? result;
        if (sp > 0)
        {
            sp--;
            System.Diagnostics.Debug.Assert(sp == 0);
            System.Diagnostics.Debug.Assert(csp == 0);
            result = sbase[sp];
            System.Diagnostics.Debug.Assert(result != null);
        }
        else
        {
            if (interp.debugMode())
                stackTrace();
            result = interp.makeError();
        }
        return result;
    }

    public void stackTrace()
    {
        ulong count = 0;
        if (protectClosure != null)
        {
            interp.setNextLocation(closureLoc);
            // interp.message(InterpreterMessages.stackTrace);
            count++;
        }
        int lim = 0;
        if (csp != 0 && csbase[0].protectClosure == null)
            lim++;
        for (int p = csp; p != lim; p--)
        {
            interp.setNextLocation(csbase[p - 1].closureLoc);
            count++;
            if (count == 5 && p - lim > 7)
            {
                // interp.message(InterpreterMessages.stackTraceEllipsis, ...);
                p = lim + 6;
            }
            // else
            //     interp.message(InterpreterMessages.stackTrace);
        }
    }

    public Interpreter interpreter() { return interp; }
}

// EvalContext - evaluation context for DSSSL
public class EvalContext : IEvalContext
{
    public NodePtr currentNode;
    public ProcessingMode? processingMode;
    public StyleObj? overridingStyle;
    public StyleStack? styleStack;
    public uint specLevel;
    public System.Collections.Generic.List<nuint>? actualDependencies;
    public LanguageObj? currentLanguage;

    public EvalContext()
    {
        currentNode = new NodePtr();
        processingMode = null;
        overridingStyle = null;
        styleStack = null;
        specLevel = 0;
        actualDependencies = null;
        currentLanguage = null;
    }

    protected void copyFrom(EvalContext other)
    {
        currentNode = other.currentNode;
        processingMode = other.processingMode;
        overridingStyle = other.overridingStyle;
        styleStack = other.styleStack;
        specLevel = other.specLevel;
        actualDependencies = other.actualDependencies;
        currentLanguage = other.currentLanguage;
    }
}
