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

    private System.Collections.Generic.List<Rule>[] rootRules_;
    private System.Collections.Generic.List<ElementRule>[] elementRules_;
    private System.Collections.Generic.List<GroveRules> groveRules_;
    private ProcessingMode? initial_; // null for initial mode
    private bool defined_;

    public ProcessingMode(StringC name, ProcessingMode? initial = null)
        : base(name)
    {
        rootRules_ = new System.Collections.Generic.List<Rule>[nRuleType];
        elementRules_ = new System.Collections.Generic.List<ElementRule>[nRuleType];
        for (int i = 0; i < nRuleType; i++)
        {
            rootRules_[i] = new System.Collections.Generic.List<Rule>();
            elementRules_[i] = new System.Collections.Generic.List<ElementRule>();
        }
        groveRules_ = new System.Collections.Generic.List<GroveRules>();
        initial_ = initial;
        defined_ = false;
    }

    public ProcessingMode(StringC name, Location loc, ProcessingMode? initial = null)
        : base(name)
    {
        rootRules_ = new System.Collections.Generic.List<Rule>[nRuleType];
        elementRules_ = new System.Collections.Generic.List<ElementRule>[nRuleType];
        for (int i = 0; i < nRuleType; i++)
        {
            rootRules_[i] = new System.Collections.Generic.List<Rule>();
            elementRules_[i] = new System.Collections.Generic.List<ElementRule>();
        }
        groveRules_ = new System.Collections.Generic.List<GroveRules>();
        initial_ = initial;
        defined_ = true;  // Mark as defined when created with location
    }

    public void addRule(bool matchesRoot, System.Collections.Generic.List<Pattern> patterns, Expression expr,
                        RuleType ruleType, Location loc, Interpreter interp)
    {
        // Create action for the rule
        Ptr<Action> action = new Ptr<Action>(new Action(0, expr, loc));

        // Add element rules for each pattern
        for (int i = 0; i < patterns.Count; i++)
            elementRules_[(int)ruleType].Add(new ElementRule(action, patterns[i]));

        // If matches root, add to root rules
        if (!matchesRoot)
            return;
        var rules = rootRules_[(int)ruleType];
        rules.Add(new Rule(action));
        // Sort by specificity (bubble sort last element into place)
        for (int i = rules.Count - 1; i > 0; i--)
        {
            int cmp = rules[i - 1].compareSpecificity(rules[i]);
            if (cmp <= 0)
                break;
            rules[i].swap(rules[i - 1]);
        }
    }

    // Simplified addRule for single pattern (called by SchemeParser)
    public void addRule(bool orRule, Pattern pattern, Expression expr, uint part, Location loc, Interpreter interp)
    {
        // For now, treat as styleRule type
        RuleType ruleType = RuleType.styleRule;

        // Determine if pattern matches root
        bool matchesRoot = pattern is RootPattern;

        // Create action for the rule
        Ptr<Action> action = new Ptr<Action>(new Action(part, expr, loc));

        // Add element rule
        elementRules_[(int)ruleType].Add(new ElementRule(action, pattern));

        // If it's a root pattern, add to root rules
        if (matchesRoot)
        {
            var rules = rootRules_[(int)ruleType];
            rules.Add(new Rule(action));
            // Sort by specificity (bubble sort last element into place)
            for (int i = rules.Count - 1; i > 0; i--)
            {
                int cmp = rules[i - 1].compareSpecificity(rules[i]);
                if (cmp <= 0)
                    break;
                rules[i].swap(rules[i - 1]);
            }
        }
    }

    public Rule? findMatch(NodePtr nd, Pattern.MatchContext context, IMessenger? mgr,
                           ref Specificity specificity)
    {
        GroveString gi = new GroveString();
        if (nd.getGi(ref gi) == AccessResult.accessOK)
        {
            var giStr = new StringC(gi.data(), gi.size());
            return findElementMatch(giStr, nd, context, mgr, ref specificity);
        }
        NodePtr tem = new NodePtr();
        if (nd.getOrigin(ref tem) != AccessResult.accessOK)
            return findRootMatch(nd, context, mgr, ref specificity);
        return null;
    }

    public void compile(Interpreter interp)
    {
        for (int i = 0; i < nRuleType; i++)
        {
            foreach (var rule in rootRules_[i])
                rule.action().compile(interp, (RuleType)i);
            foreach (var rule in elementRules_[i])
                rule.action().compile(interp, (RuleType)i);
        }
    }

    public bool defined() { return defined_; }
    public void setDefined() { defined_ = true; }

    private Rule? findElementMatch(StringC gi, NodePtr nd,
                                   Pattern.MatchContext context, IMessenger? mgr,
                                   ref Specificity specificity)
    {
        GroveRules gr = groveRulesForNode(nd, mgr);

        // First try element-specific rules
        // Use lowercase for lookup since SGML element names are case-insensitive
        string giKey = gi.ToString().ToLowerInvariant();
        for (int ruleType = (int)specificity.ruleType; ruleType < nRuleType; ruleType++)
        {
            if (gr.elementTable.TryGetValue(giKey, out ElementRules? er))
            {
                var vec = er.rules[ruleType];
                if (vec.Count > 0)
                {
                    elementRuleAdvance(nd, context, mgr, ref specificity, vec);
                    if (specificity.nextRuleIndex < (nuint)vec.Count)
                    {
                        specificity.ruleType = (RuleType)ruleType;
                        return vec[(int)specificity.nextRuleIndex];
                    }
                }
            }

            // Try other rules (wildcard patterns)
            var otherVec = gr.otherRules[ruleType];
            if (otherVec.Count > 0)
            {
                elementRuleAdvance(nd, context, mgr, ref specificity, otherVec);
                if (specificity.nextRuleIndex < (nuint)otherVec.Count)
                {
                    specificity.ruleType = (RuleType)ruleType;
                    return otherVec[(int)specificity.nextRuleIndex];
                }
            }
            specificity.nextRuleIndex = 0;
        }

        // Fall through to initial mode if we have one
        if (initial_ != null)
        {
            specificity.toInitial = true;
            return initial_.findElementMatch(gi, nd, context, mgr, ref specificity);
        }
        return null;
    }

    private Rule? findRootMatch(NodePtr nd, Pattern.MatchContext context, IMessenger? mgr,
                                ref Specificity specificity)
    {
        for (int ruleType = (int)specificity.ruleType; ruleType < nRuleType; ruleType++)
        {
            var rules = rootRules_[ruleType];
            if (rules.Count > 0)
            {
                specificity.ruleType = (RuleType)ruleType;
                return rules[0];
            }
        }

        // Fall through to initial mode if we have one
        if (initial_ != null)
        {
            specificity.toInitial = true;
            return initial_.findRootMatch(nd, context, mgr, ref specificity);
        }
        return null;
    }

    private GroveRules groveRulesForNode(NodePtr nd, IMessenger? mgr)
    {
        uint groveIndex = nd.groveIndex();
        // Ensure we have enough GroveRules
        while (groveRules_.Count <= groveIndex)
            groveRules_.Add(new GroveRules());

        GroveRules gr = groveRules_[(int)groveIndex];
        if (!gr.built)
            gr.build(elementRules_, nd, mgr);
        return gr;
    }

    private static void elementRuleAdvance(NodePtr nd, Pattern.MatchContext context,
                                           IMessenger? mgr, ref Specificity specificity,
                                           System.Collections.Generic.List<ElementRule> vec)
    {
        // Find next matching rule starting from specificity.nextRuleIndex
        for (nuint i = specificity.nextRuleIndex; i < (nuint)vec.Count; i++)
        {
            if (vec[(int)i].pattern.matches(nd, context))
            {
                specificity.nextRuleIndex = i;
                return;
            }
        }
        // No match found
        specificity.nextRuleIndex = (nuint)vec.Count;
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
            if (expr_ == null)
                return;
            Expression e = expr_;
            e.optimize(interp, new Environment(), ref e);
            expr_ = e;
            ELObj? tem = expr_.constantValue();
            if (tem != null)
            {
                if (ruleType == RuleType.constructionRule)
                {
                    sosofo_ = tem.asSosofo();
                    if (sosofo_ != null)
                        return;
                }
            }
            InsnPtr check = new InsnPtr();
            if (ruleType == RuleType.constructionRule)
                check = new InsnPtr(new CheckSosofoInsn(defLoc_, check));
            insn_ = expr_.compile(interp, new Environment(), 0, check);
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
        public System.Collections.Generic.List<ElementRule>[] rules;

        public ElementRules(StringC name)
            : base(name)
        {
            rules = new System.Collections.Generic.List<ElementRule>[nRuleType];
            for (int i = 0; i < nRuleType; i++)
            {
                rules[i] = new System.Collections.Generic.List<ElementRule>();
            }
        }
    }

    // Rules for a grove
    public class GroveRules
    {
        public bool built;
        public Dictionary<string, ElementRules> elementTable;
        public System.Collections.Generic.List<ElementRule>[] otherRules;

        public GroveRules()
        {
            built = false;
            elementTable = new Dictionary<string, ElementRules>();
            otherRules = new System.Collections.Generic.List<ElementRule>[nRuleType];
            for (int i = 0; i < nRuleType; i++)
            {
                otherRules[i] = new System.Collections.Generic.List<ElementRule>();
            }
        }

        public void build(System.Collections.Generic.List<ElementRule>[] rules, NodePtr nd, IMessenger? mgr)
        {
            built = true;
            for (int ruleType = 0; ruleType < nRuleType; ruleType++)
            {
                foreach (var rule in rules[ruleType])
                {
                    StringC gi;
                    if (rule.pattern.mustHaveGi(out gi))
                    {
                        // Rule requires specific GI
                        string key = gi.ToString();
                        if (!elementTable.TryGetValue(key, out ElementRules? er))
                        {
                            er = new ElementRules(gi);
                            elementTable[key] = er;
                        }
                        er.rules[ruleType].Add(rule);
                    }
                    else
                    {
                        // Wildcard rule
                        otherRules[ruleType].Add(rule);
                    }
                }
                // Sort rules by specificity
                foreach (var er in elementTable.Values)
                    sortRules(er.rules[ruleType]);
                sortRules(otherRules[ruleType]);
            }
        }

        public static void sortRules(System.Collections.Generic.List<ElementRule> v)
        {
            // Simple insertion sort by specificity
            for (int i = 1; i < v.Count; i++)
            {
                for (int j = i; j > 0; j--)
                {
                    if (v[j - 1].compareSpecificity(v[j]) <= 0)
                        break;
                    v[j].swap(v[j - 1]);
                }
            }
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

// NOTE: InsnPtr is defined in Insn.cs
// NOTE: Insn is defined in Insn.cs
// NOTE: SosofoObj is defined in ELObj.cs
// NOTE: StyleObj is defined in ELObj.cs
// NOTE: VM is defined in VM.cs
// NOTE: Expression is defined in Expression.cs

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
