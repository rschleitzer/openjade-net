// Copyright (c) 1997 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;

// MacroFlowObj - User-defined flow object built from expressions
public class MacroFlowObj : CompoundFlowObj
{
    private Definition def_;
    private ELObj?[] charicVals_;

    public MacroFlowObj(System.Collections.Generic.List<Identifier?> nics,
                        System.Collections.Generic.List<Expression?> inits,
                        Identifier? contentsId,
                        Expression body)
    {
        def_ = new Definition(nics, inits, contentsId, body);
        int n = def_.nics().Count;
        charicVals_ = new ELObj?[n];
        for (int i = 0; i < n; i++)
            charicVals_[i] = null;
    }

    public MacroFlowObj(MacroFlowObj obj) : base()
    {
        setStyle(obj.style());
        setContent(obj.content());
        def_ = obj.def_;
        int n = def_.nics().Count;
        charicVals_ = new ELObj?[n];
        for (int i = 0; i < n; i++)
            charicVals_[i] = obj.charicVals_[i];
    }

    public override FlowObj copy(Interpreter interp)
    {
        return new MacroFlowObj(this);
    }

    public override CompoundFlowObj? asCompoundFlowObj()
    {
        if (def_.isCompound())
            return this;
        else
            return null;
    }

    public override bool hasNonInheritedC(Identifier? id)
    {
        var nics = def_.nics();
        for (int i = 0; i < nics.Count; i++)
            if (nics[i] == id)
                return true;
        return false;
    }

    public override void setNonInheritedC(Identifier? id, ELObj? obj, Location loc, Interpreter interp)
    {
        var nics = def_.nics();
        for (int i = 0; i < nics.Count; i++)
        {
            if (nics[i] == id)
            {
                charicVals_[i] = obj;
                return;
            }
        }
    }

    public override void traceSubObjects(Collector c)
    {
        int n = def_.nics().Count;
        for (int i = 0; i < n; i++)
            c.trace(charicVals_[i]);
        base.traceSubObjects(c);
    }

    public void unpack(VM vm)
    {
        int n = def_.nics().Count;
        int extra = def_.isCompound() ? 1 : 0;
        vm.needStack(n + 1 + extra);
        for (int i = 0; i < n; i++)
            vm.sbase[vm.sp++] = charicVals_[i];
        if (def_.isCompound())
        {
            ELObj? tem = content();
            if (tem == null)
                tem = new ProcessChildrenSosofoObj(vm.interp.initialProcessingMode());
            vm.sbase[vm.sp++] = tem;
        }
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startSequence();
        def_.process(context, this);
        fotb.endSequence();
    }

    // Definition for macro flow objects
    public class Definition
    {
        private System.Collections.Generic.List<Identifier?> charics_;
        private System.Collections.Generic.List<Expression?> charicInits_;
        private Identifier? contentsId_;
        private Expression? body_;
        private InsnPtr? code_;

        public Definition(System.Collections.Generic.List<Identifier?> charics,
                          System.Collections.Generic.List<Expression?> charicInits,
                          Identifier? contentsId,
                          Expression body)
        {
            charics_ = new System.Collections.Generic.List<Identifier?>(charics);
            charicInits_ = new System.Collections.Generic.List<Expression?>(charicInits);
            // Resize inits to match charics size
            while (charicInits_.Count < charics_.Count)
                charicInits_.Add(null);
            contentsId_ = contentsId;
            body_ = body;
            code_ = null;
        }

        public bool isCompound()
        {
            return contentsId_ != null;
        }

        public System.Collections.Generic.List<Identifier?> nics()
        {
            return charics_;
        }

        public void process(ProcessContext context, MacroFlowObj macro)
        {
            VM vm = context.vm();
            Interpreter interp = vm.interp;
            if (code_ == null)
                compile(interp);
            StyleStack? saveStyleStack = vm.styleStack;
            vm.styleStack = context.currentStyleStack();
            uint saveSpecLevel = vm.specLevel;
            vm.specLevel = vm.styleStack?.level() ?? 0;
            var dep = new System.Collections.Generic.List<nuint>();
            vm.actualDependencies = dep;
            ELObj? obj = context.vm().eval(code_?.pointer(), null, macro);
            vm.styleStack = saveStyleStack;
            vm.specLevel = saveSpecLevel;
            if (!interp.isError(obj))
            {
                SosofoObj? sosofo = obj?.asSosofo();
                if (sosofo != null)
                    sosofo.process(context);
            }
        }

        public void compile(Interpreter interp)
        {
            // Build the instruction sequence bottom-up
            int nPush = charics_.Count + (contentsId_ != null ? 1 : 0);

            // Build frame variables list
            var frameVars = new BoundVarList();
            for (int i = 0; i < charics_.Count; i++)
            {
                if (i > 0 && charicInits_[i] != null)
                    charicInits_[i]!.markBoundVars(frameVars, false);
                frameVars.append(charics_[i], 0);
            }
            if (contentsId_ != null)
                frameVars.append(contentsId_, 0);
            body_?.markBoundVars(frameVars, false);

            // Start building instruction chain from the end
            InsnPtr result = new InsnPtr(); // Empty initially
            result = new InsnPtr(new CheckSosofoInsn(body_?.location() ?? new Location(), result));
            result = PopBindingsInsn.make(nPush, result);
            result = Expression.optimizeCompile(body_, interp,
                                                new Environment(frameVars, new BoundVarList()),
                                                nPush, result);

            // Build initialization code for each characteristic
            for (int i = charics_.Count; i > 0; i--)
            {
                int stackOffset = i - nPush - 1;
                if (frameVars[i - 1].boxed())
                    result = new InsnPtr(new BoxStackInsn(stackOffset, result));

                InsnPtr ifNull = new InsnPtr(new SetKeyArgInsn(stackOffset, result));
                if (charicInits_[i - 1] != null)
                {
                    var f = new BoundVarList();
                    for (int j = 0; j < i - 1; j++)
                        f.append(frameVars[j].ident, frameVars[j].boxed() ? 1u : 0u);
                    ifNull = Expression.optimizeCompile(charicInits_[i - 1], interp,
                                                        new Environment(f, new BoundVarList()),
                                                        nPush, ifNull);
                }
                else
                    ifNull = new InsnPtr(new ConstantInsn(interp.makeFalse(), ifNull));
                result = new InsnPtr(new TestNullInsn(stackOffset, ifNull, result));
            }

            code_ = new InsnPtr(new UnpackMacroFlowObjInsn(result));
        }
    }
}

// Instruction to unpack macro flow object onto the stack
public class UnpackMacroFlowObjInsn : Insn
{
    private InsnPtr next_;

    public UnpackMacroFlowObjInsn(InsnPtr next)
    {
        next_ = next;
    }

    public override Insn? execute(VM vm)
    {
        ((MacroFlowObj)vm.sbase[--vm.sp]!).unpack(vm);
        return next_.pointer();
    }
}

// Empty style object for use in macro evaluation
public class EmptyStyleObj : StyleObj
{
    public override void appendIter(StyleObjIter iter) { }
}
