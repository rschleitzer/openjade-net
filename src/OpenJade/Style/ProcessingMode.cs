// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Processing mode for DSSSL style rules
public class ProcessingMode : Named
{
    public enum RuleType
    {
        styleRule,
        constructionRule
    }

    public const int nRuleType = 2;

    private List<Rule>[] rootRules_;
    private List<ElementRule>[] elementRules_;
    private List<GroveRules> groveRules_;
    private ProcessingMode? initial_; // null for initial mode
    private bool defined_;

    public ProcessingMode(StringC name, ProcessingMode? initial = null)
        : base(name)
    {
        rootRules_ = new List<Rule>[nRuleType];
        elementRules_ = new List<ElementRule>[nRuleType];
        for (int i = 0; i < nRuleType; i++)
        {
            rootRules_[i] = new List<Rule>();
            elementRules_[i] = new List<ElementRule>();
        }
        groveRules_ = new List<GroveRules>();
        initial_ = initial;
        defined_ = false;
    }

    public void addRule(bool matchesRoot, List<Pattern> patterns, Expression expr,
                        RuleType ruleType, Location loc, Interpreter interp)
    {
        throw new NotImplementedException();
    }

    public Rule? findMatch(NodePtr nd, Pattern.MatchContext context, Messenger mgr,
                           ref Specificity specificity)
    {
        throw new NotImplementedException();
    }

    public void compile(Interpreter interp)
    {
        throw new NotImplementedException();
    }

    public bool defined() { return defined_; }
    public void setDefined() { defined_ = true; }

    private Rule? findElementMatch(StringC gi, NodePtr nd,
                                   Pattern.MatchContext context, Messenger mgr,
                                   ref Specificity specificity)
    {
        throw new NotImplementedException();
    }

    private Rule? findRootMatch(NodePtr nd, Pattern.MatchContext context, Messenger mgr,
                                ref Specificity specificity)
    {
        throw new NotImplementedException();
    }

    private GroveRules groveRulesForNode(NodePtr nd, Messenger mgr)
    {
        throw new NotImplementedException();
    }

    private static void elementRuleAdvance(NodePtr nd, Pattern.MatchContext context,
                                           Messenger mgr, ref Specificity specificity,
                                           List<ElementRule> vec)
    {
        throw new NotImplementedException();
    }

    // Specificity of a pattern match
    public class Specificity
    {
        private bool toInitial_; // true if match fell through from named to initial mode
        private RuleType ruleType_;
        private nuint nextRuleIndex_;

        public Specificity()
        {
            toInitial_ = false;
            ruleType_ = RuleType.styleRule;
            nextRuleIndex_ = 0;
        }

        public bool isStyle()
        {
            return ruleType_ == RuleType.styleRule;
        }

        internal bool toInitial { get { return toInitial_; } set { toInitial_ = value; } }
        internal RuleType ruleType { get { return ruleType_; } set { ruleType_ = value; } }
        internal nuint nextRuleIndex { get { return nextRuleIndex_; } set { nextRuleIndex_ = value; } }
    }

    // Action for a rule
    public class Action : IResource
    {
        private int refCount_ = 0;
        public int count() { return refCount_; }
        public void @ref() { refCount_++; }
        public int unref() { return --refCount_; }

        private Location defLoc_;
        private Expression? expr_;
        private InsnPtr? insn_;
        private SosofoObj? sosofo_;
        private uint partIndex_;

        public Action(uint partIndex, Expression expr, Location loc)
        {
            partIndex_ = partIndex;
            expr_ = expr;
            defLoc_ = loc;
            insn_ = null;
            sosofo_ = null;
        }

        public void compile(Interpreter interp, RuleType ruleType)
        {
            throw new NotImplementedException();
        }

        public void get(out InsnPtr? insn, out SosofoObj? sosofo)
        {
            insn = insn_;
            sosofo = sosofo_;
        }

        public Location location() { return defLoc_; }
        public uint partIndex() { return partIndex_; }
    }

    // Base rule class
    public class Rule
    {
        private Ptr<Action>? action_;

        public Rule()
        {
            action_ = null;
        }

        public Rule(Ptr<Action> action)
        {
            action_ = action;
        }

        public Action action()
        {
            return action_!.pointer()!;
        }

        public virtual int compareSpecificity(Rule other)
        {
            return 0;
        }

        public Location location()
        {
            return action_!.pointer()!.location();
        }

        public void swap(Rule r)
        {
            var temp = action_;
            action_ = r.action_;
            r.action_ = temp;
        }
    }

    // Element rule with pattern matching
    public class ElementRule : Rule
    {
        private Pattern pattern_;

        public ElementRule(Ptr<Action> action, Pattern pattern)
            : base(action)
        {
            pattern_ = pattern;
        }

        public override int compareSpecificity(Rule other)
        {
            if (other is ElementRule otherElement)
            {
                return Pattern.compareSpecificity(pattern_, otherElement.pattern_);
            }
            return base.compareSpecificity(other);
        }

        public Pattern pattern { get { return pattern_; } }
    }

    // Rules for elements by name
    public class ElementRules : Named
    {
        public List<ElementRule>[] rules;

        public ElementRules(StringC name)
            : base(name)
        {
            rules = new List<ElementRule>[nRuleType];
            for (int i = 0; i < nRuleType; i++)
            {
                rules[i] = new List<ElementRule>();
            }
        }
    }

    // Rules for a grove
    public class GroveRules
    {
        public bool built;
        public Dictionary<StringC, ElementRules> elementTable;
        public List<ElementRule>[] otherRules;

        public GroveRules()
        {
            built = false;
            elementTable = new Dictionary<StringC, ElementRules>();
            otherRules = new List<ElementRule>[nRuleType];
            for (int i = 0; i < nRuleType; i++)
            {
                otherRules[i] = new List<ElementRule>();
            }
        }

        public void build(List<ElementRule>? rules, NodePtr nd, Messenger mgr)
        {
            throw new NotImplementedException();
        }

        public static void sortRules(List<ElementRule> v)
        {
            throw new NotImplementedException();
        }
    }
}

// Named base class
public class Named
{
    private StringC name_;

    public Named(StringC name)
    {
        name_ = name;
    }

    public StringC name() { return name_; }
}

// Resource base class
public class Resource
{
    private int refCount_;

    public Resource()
    {
        refCount_ = 0;
    }

    public void ref_() { refCount_++; }
    public void unref() { refCount_--; }
    public int refCount() { return refCount_; }
}

// Instruction pointer
public class InsnPtr
{
    private Insn? insn_;

    public InsnPtr()
    {
        insn_ = null;
    }

    public InsnPtr(Insn insn)
    {
        insn_ = insn;
    }

    public Insn? get() { return insn_; }
    public void assign(Insn? insn) { insn_ = insn; }
}

// Instruction base class
public abstract class Insn
{
    public abstract ELObj? execute(VM vm);
}

// NOTE: SosofoObj is defined in ELObj.cs
// NOTE: StyleObj is defined in ELObj.cs

// Expression base class
public abstract class Expression
{
    public abstract ELObj? evaluate(VM vm);
    public virtual InsnPtr compile(Interpreter interp)
    {
        throw new NotImplementedException();
    }
}

// CurrentNodeSetter - saves and restores current node context
public class CurrentNodeSetter : IDisposable
{
    private VM ec_;
    private NodePtr saveCurrentNode_;
    private ProcessingMode? saveProcessingMode_;

    public CurrentNodeSetter(NodePtr node, ProcessingMode? mode, VM ec)
    {
        ec_ = ec;
        saveCurrentNode_ = ec.currentNode;
        saveProcessingMode_ = ec.processingMode;
        ec.currentNode = node;
        ec.processingMode = mode;
    }

    public void Dispose()
    {
        ec_.currentNode = saveCurrentNode_;
        ec_.processingMode = saveProcessingMode_;
    }
}

// VM (Virtual Machine) for DSSSL execution
public class VM
{
    public Interpreter interp;
    public NodePtr currentNode;
    public ProcessingMode? processingMode;
    public StyleObj? overridingStyle;
    public StyleStack? styleStack;
    public uint specLevel;
    public System.Collections.Generic.List<nuint>? actualDependencies;
    public LanguageObj? currentLanguage;
    private ELObj?[] frame_;
    private int frameSize_;

    public VM(Interpreter interpreter)
    {
        interp = interpreter;
        currentNode = new NodePtr();
        processingMode = null;
        overridingStyle = null;
        styleStack = null;
        specLevel = 0;
        actualDependencies = null;
        currentLanguage = null;
        frame_ = Array.Empty<ELObj?>();
        frameSize_ = 0;
    }

    public Interpreter interpreter() { return interp; }

    public ELObj? eval(Insn? insn)
    {
        if (insn == null) return null;
        return insn.execute(this);
    }

    public ELObj? eval(Insn? insn, ELObj?[]? display, ELObj? arg = null)
    {
        if (insn == null) return null;
        // TODO: Full implementation with display and arg
        return insn.execute(this);
    }
}
