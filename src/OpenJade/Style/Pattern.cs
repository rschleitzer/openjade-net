// Copyright (c) 1997 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Pattern matching for DSSSL style rules
public class Pattern
{
    private System.Collections.Generic.List<Element> ancestors_; // first is self, second is parent ...
    private bool trivial_;

    public Pattern()
    {
        ancestors_ = new System.Collections.Generic.List<Element>();
        trivial_ = true;
    }

    public Pattern(System.Collections.Generic.List<Element> ancestors)
    {
        ancestors_ = ancestors;
        trivial_ = computeTrivial(ancestors_);
    }

    public void swap(Pattern pattern)
    {
        var tempAncestors = ancestors_;
        ancestors_ = pattern.ancestors_;
        pattern.ancestors_ = tempAncestors;

        var tempTrivial = trivial_;
        trivial_ = pattern.trivial_;
        pattern.trivial_ = tempTrivial;
    }

    public bool matches(NodePtr nd, MatchContext context)
    {
        return matchAncestors(ancestors_, nd, context, 0);
    }

    public bool mustHaveGi(out StringC gi)
    {
        gi = new StringC();
        if (ancestors_.Count > 0)
        {
            return ancestors_[0].mustHaveGi(out gi);
        }
        return false;
    }

    public System.Collections.Generic.List<Element> ancestors() { return ancestors_; }

    public bool trivial()
    {
        return trivial_;
    }

    public static int compareSpecificity(Pattern p1, Pattern p2)
    {
        int[] spec1 = new int[nSpecificity];
        int[] spec2 = new int[nSpecificity];
        p1.computeSpecificity(spec1);
        p2.computeSpecificity(spec2);
        for (int i = 0; i < nSpecificity; i++)
        {
            if (spec1[i] != spec2[i])
                return spec1[i] - spec2[i];
        }
        return 0;
    }

    public const int importanceSpecificity = 0;
    public const int idSpecificity = 1;
    public const int classSpecificity = 2;
    public const int giSpecificity = 3;
    public const int repeatSpecificity = 4;
    public const int prioritySpecificity = 5;
    public const int onlySpecificity = 6;
    public const int positionSpecificity = 7;
    public const int attributeSpecificity = 8;
    public const int nSpecificity = 9;

    private void computeSpecificity(int[] spec)
    {
        for (int i = 0; i < nSpecificity; i++)
            spec[i] = 0;
        foreach (var elem in ancestors_)
        {
            elem.contributeSpecificity(spec);
        }
    }

    private static bool computeTrivial(System.Collections.Generic.List<Element> ancestors)
    {
        foreach (var elem in ancestors)
        {
            if (!elem.trivial())
                return false;
        }
        return true;
    }

    private static bool matchAncestors(System.Collections.Generic.List<Element> ancestors, NodePtr node, MatchContext context, int index)
    {
        if (index >= ancestors.Count)
            return true;
        return matchAncestors1(ancestors, node, context, index);
    }

    private static bool matchAncestors1(System.Collections.Generic.List<Element> ancestors, NodePtr node, MatchContext context, int index)
    {
        Element r = ancestors[index];
        NodePtr tem = new NodePtr(node);
        // Match minimum required repetitions
        for (uint i = 0; i < r.minRepeat(); i++)
        {
            if (!tem || !r.matches(tem, context))
                return false;
            NodePtr parent = new NodePtr();
            if (tem.node!.getParent(ref parent) != AccessResult.accessOK)
                tem.clear();
            else
                tem.assign(parent);
        }
        // Try to match rest of pattern
        uint count = r.minRepeat();
        for (;;)
        {
            int nextIndex = index + 1;
            if (nextIndex >= ancestors.Count)
                return true; // All ancestors matched
            if (matchAncestors1(ancestors, tem, context, nextIndex))
                return true;
            // Try matching more repetitions if allowed
            if (count >= r.maxRepeat())
                return false;
            if (!tem || !r.matches(tem, context))
                return false;
            NodePtr parent = new NodePtr();
            if (tem.node!.getParent(ref parent) != AccessResult.accessOK)
                tem.clear();
            else
                tem.assign(parent);
            count++;
        }
    }

    // Match context for pattern matching
    public class MatchContext : SdataMapper
    {
        protected System.Collections.Generic.List<StringC> classAttributeNames_ = new System.Collections.Generic.List<StringC>();
        protected System.Collections.Generic.List<StringC> idAttributeNames_ = new System.Collections.Generic.List<StringC>();

        public System.Collections.Generic.List<StringC> classAttributeNames() { return classAttributeNames_; }
        public System.Collections.Generic.List<StringC> idAttributeNames() { return idAttributeNames_; }
    }

    // Base class for pattern qualifiers
    public abstract class Qualifier : Link
    {
        public abstract bool satisfies(NodePtr nd, MatchContext context);
        public abstract void contributeSpecificity(int[] spec);
        public virtual bool vacuous() { return false; }

        protected static bool matchAttribute(StringC name, StringC value, NodePtr nd, MatchContext context)
        {
            NamedNodeListPtr atts = new NamedNodeListPtr();
            if (nd.node!.getAttributes(ref atts) != AccessResult.accessOK)
                return false;
            NodePtr att = new NodePtr();
            if (atts.list!.namedNode(new GroveString(name.data(), name.size()), ref att) != AccessResult.accessOK)
                return false;
            if (att.node!.getImplied(out bool implied) == AccessResult.accessOK && implied)
                return false;
            GroveString tokens = new GroveString();
            if (att.node!.tokens(ref tokens) == AccessResult.accessOK)
            {
                if (tokens.size() != value.size())
                    return false;
                // Compare token values
                for (nuint i = 0; i < tokens.size(); i++)
                {
                    if (tokens.data()![i] != value.data()![i])
                        return false;
                }
                return true;
            }
            return false;
        }
    }

    // ID qualifier (#id)
    public class IdQualifier : Qualifier
    {
        private StringC id_;

        public IdQualifier(StringC id)
        {
            id_ = id;
        }

        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            var idAtts = context.idAttributeNames();
            foreach (var idAtt in idAtts)
            {
                if (matchAttribute(idAtt, id_, nd, context))
                    return true;
            }
            return false;
        }

        public override void contributeSpecificity(int[] spec)
        {
            spec[idSpecificity]++;
        }
    }

    // Class qualifier (.class)
    public class ClassQualifier : Qualifier
    {
        private StringC class_;

        public ClassQualifier(StringC cls)
        {
            class_ = cls;
        }

        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            var classAtts = context.classAttributeNames();
            foreach (var classAtt in classAtts)
            {
                if (matchAttribute(classAtt, class_, nd, context))
                    return true;
            }
            return false;
        }

        public override void contributeSpecificity(int[] spec)
        {
            spec[classSpecificity]++;
        }
    }

    // Attribute has value qualifier
    public class AttributeHasValueQualifier : Qualifier
    {
        private StringC name_;

        public AttributeHasValueQualifier(StringC name)
        {
            name_ = name;
        }

        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            NamedNodeListPtr atts = new NamedNodeListPtr();
            if (nd.node!.getAttributes(ref atts) != AccessResult.accessOK)
                return false;
            NodePtr att = new NodePtr();
            if (atts.list!.namedNode(new GroveString(name_.data(), name_.size()), ref att) != AccessResult.accessOK)
                return false;
            if (att.node!.getImplied(out bool implied) == AccessResult.accessOK && implied)
                return false;
            return true;
        }

        public override void contributeSpecificity(int[] spec)
        {
            spec[attributeSpecificity]++;
        }
    }

    // Attribute missing value qualifier
    public class AttributeMissingValueQualifier : Qualifier
    {
        private StringC name_;

        public AttributeMissingValueQualifier(StringC name)
        {
            name_ = name;
        }

        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            NamedNodeListPtr atts = new NamedNodeListPtr();
            if (nd.node!.getAttributes(ref atts) != AccessResult.accessOK)
                return true; // No attributes = missing
            NodePtr att = new NodePtr();
            if (atts.list!.namedNode(new GroveString(name_.data(), name_.size()), ref att) != AccessResult.accessOK)
                return true; // Attribute not found = missing
            if (att.node!.getImplied(out bool implied2) == AccessResult.accessOK && implied2)
                return true; // Implied = missing
            return false;
        }

        public override void contributeSpecificity(int[] spec)
        {
            spec[attributeSpecificity]++;
        }
    }

    // Attribute value qualifier
    public class AttributeQualifier : Qualifier
    {
        private StringC name_;
        private StringC value_;

        public AttributeQualifier(StringC name, StringC value)
        {
            name_ = name;
            value_ = value;
        }

        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            return matchAttribute(name_, value_, nd, context);
        }

        public override void contributeSpecificity(int[] spec)
        {
            spec[attributeSpecificity]++;
        }
    }

    // Position qualifier base class
    public abstract class PositionQualifier : Qualifier
    {
        public override void contributeSpecificity(int[] spec)
        {
            spec[positionSpecificity]++;
        }
    }

    // First of type qualifier
    public class FirstOfTypeQualifier : PositionQualifier
    {
        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            GroveString ndType = new GroveString();
            nd.getGi(ndType);
            NodePtr tem = new NodePtr();
            if (nd.node!.firstSibling(ref tem) != AccessResult.accessOK)
                return true; // Must be document element
            while (tem.node != nd.node)
            {
                GroveString temType = new GroveString();
                if (tem.getGi(temType) == AccessResult.accessOK)
                {
                    if (temType.size() == ndType.size() && sameString(temType, ndType))
                        return false;
                }
                NodePtr next = new NodePtr();
                if (tem.node!.nextSibling(ref next) != AccessResult.accessOK)
                    break;
                tem.assign(next);
            }
            return true;
        }
    }

    // Last of type qualifier
    public class LastOfTypeQualifier : PositionQualifier
    {
        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            GroveString ndType = new GroveString();
            nd.getGi(ndType);
            NodePtr tem = new NodePtr();
            if (nd.node!.nextSibling(ref tem) != AccessResult.accessOK)
                return true; // No next sibling = last
            do
            {
                GroveString temType = new GroveString();
                if (tem.getGi(temType) == AccessResult.accessOK)
                {
                    if (temType.size() == ndType.size() && sameString(temType, ndType))
                        return false;
                }
                NodePtr next = new NodePtr();
                if (tem.node!.nextSibling(ref next) != AccessResult.accessOK)
                    break;
                tem.assign(next);
            } while (true);
            return true;
        }
    }

    // First of any qualifier
    public class FirstOfAnyQualifier : PositionQualifier
    {
        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            NodePtr tem = new NodePtr();
            if (nd.node!.firstSibling(ref tem) != AccessResult.accessOK)
                return true;
            return tem.node == nd.node;
        }
    }

    // Last of any qualifier
    public class LastOfAnyQualifier : PositionQualifier
    {
        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            NodePtr tem = new NodePtr();
            return nd.node!.nextSibling(ref tem) != AccessResult.accessOK;
        }
    }

    private static bool sameString(GroveString a, GroveString b)
    {
        if (a.size() != b.size())
            return false;
        for (nuint i = 0; i < a.size(); i++)
        {
            if (a.data()![i] != b.data()![i])
                return false;
        }
        return true;
    }

    // Only qualifier base class
    public abstract class OnlyQualifier : Qualifier
    {
        public override void contributeSpecificity(int[] spec)
        {
            spec[onlySpecificity]++;
        }
    }

    // Only of type qualifier
    public class OnlyOfTypeQualifier : OnlyQualifier
    {
        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            GroveString ndType = new GroveString();
            nd.getGi(ndType);
            // Check for any sibling with same type
            NodePtr tem = new NodePtr();
            if (nd.node!.firstSibling(ref tem) != AccessResult.accessOK)
                return true; // Document element
            while (tem.node != null)
            {
                if (tem.node != nd.node)
                {
                    GroveString temType = new GroveString();
                    if (tem.getGi(temType) == AccessResult.accessOK)
                    {
                        if (temType.size() == ndType.size() && sameString(temType, ndType))
                            return false;
                    }
                }
                NodePtr next = new NodePtr();
                if (tem.node!.nextSibling(ref next) != AccessResult.accessOK)
                    break;
                tem.assign(next);
            }
            return true;
        }
    }

    // Only of any qualifier
    public class OnlyOfAnyQualifier : OnlyQualifier
    {
        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            NodePtr tem = new NodePtr();
            // First of any
            if (nd.node!.firstSibling(ref tem) != AccessResult.accessOK)
                return true;
            if (tem.node != nd.node)
                return false;
            // Last of any
            NodePtr next = new NodePtr();
            return nd.node!.nextSibling(ref next) != AccessResult.accessOK;
        }
    }

    // Vacuous qualifier base class
    public abstract class VacuousQualifier : Qualifier
    {
        public override bool vacuous() { return true; }
    }

    // Priority qualifier
    public class PriorityQualifier : VacuousQualifier
    {
        private long n_;

        public PriorityQualifier(long n)
        {
            n_ = n;
        }

        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            return true;
        }

        public override void contributeSpecificity(int[] spec)
        {
            spec[prioritySpecificity] += (int)n_;
        }
    }

    // Importance qualifier
    public class ImportanceQualifier : VacuousQualifier
    {
        private long n_;

        public ImportanceQualifier(long n)
        {
            n_ = n;
        }

        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            return true;
        }

        public override void contributeSpecificity(int[] spec)
        {
            spec[importanceSpecificity] += (int)n_;
        }
    }

    // Pattern element
    public class Element : Link
    {
        private StringC gi_;
        private uint minRepeat_;
        private uint maxRepeat_;
        private System.Collections.Generic.List<Qualifier> qualifiers_;

        public Element(StringC gi)
        {
            gi_ = gi;
            minRepeat_ = 1;
            maxRepeat_ = 1;
            qualifiers_ = new System.Collections.Generic.List<Qualifier>();
        }

        public bool matches(NodePtr nd, MatchContext context)
        {
            // Match GI if specified
            if (gi_.size() > 0)
            {
                GroveString ndGi = new GroveString();
                if (nd.getGi(ndGi) != AccessResult.accessOK)
                    return false;
                if (ndGi.size() != gi_.size())
                    return false;
                for (nuint i = 0; i < gi_.size(); i++)
                {
                    if (ndGi.data()![i] != gi_.data()![i])
                        return false;
                }
            }
            // Check all qualifiers
            foreach (var q in qualifiers_)
            {
                if (!q.satisfies(nd, context))
                    return false;
            }
            return true;
        }

        public void contributeSpecificity(int[] spec)
        {
            if (gi_.size() > 0)
                spec[giSpecificity]++;
            if (minRepeat_ > 1 || maxRepeat_ > 1)
                spec[repeatSpecificity]++;
            foreach (var q in qualifiers_)
            {
                q.contributeSpecificity(spec);
            }
        }

        public void addQualifier(Qualifier q)
        {
            qualifiers_.Add(q);
        }

        public void setRepeat(uint minRepeat, uint maxRepeat)
        {
            minRepeat_ = minRepeat;
            maxRepeat_ = maxRepeat;
        }

        public uint minRepeat() { return minRepeat_; }
        public uint maxRepeat() { return maxRepeat_; }

        public bool mustHaveGi(out StringC gi)
        {
            gi = new StringC();
            if (minRepeat_ == 0)
                return false;
            if (gi_.size() > 0)
            {
                gi = gi_;
                return true;
            }
            return false;
        }

        public bool trivial()
        {
            if (qualifiers_.Count > 0)
                return false;
            return true;
        }
    }

    // Children qualifier
    public class ChildrenQualifier : Qualifier
    {
        private System.Collections.Generic.List<Element> children_;

        public ChildrenQualifier(System.Collections.Generic.List<Element> children)
        {
            children_ = children;
        }

        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            if (children_.Count == 0)
                return true;
            NodePtr child = new NodePtr();
            if (nd.node!.firstChild(ref child) != AccessResult.accessOK)
                return false;
            // Build list of elements to match
            var toMatch = new System.Collections.Generic.List<Element>(children_);
            do
            {
                int j = 0;
                for (int i = 0; i < toMatch.Count; i++)
                {
                    if (!toMatch[i].matches(child, context))
                    {
                        if (j != i)
                            toMatch[j] = toMatch[i];
                        j++;
                    }
                }
                if (j == 0)
                    return true; // All matched
                toMatch.RemoveRange(j, toMatch.Count - j);
                NodePtr next = new NodePtr();
                if (child.node!.nextChunkSibling(ref next) != AccessResult.accessOK)
                    break;
                child.assign(next);
            } while (true);
            return false;
        }

        public override void contributeSpecificity(int[] spec)
        {
            foreach (var elem in children_)
            {
                elem.contributeSpecificity(spec);
            }
        }
    }
}

// SdataMapper interface for pattern matching
public abstract class SdataMapper
{
}

// ElementPattern - matches element by GI
public class ElementPattern : Pattern
{
    public ElementPattern(StringC gi)
        : base(new System.Collections.Generic.List<Pattern.Element> { new Pattern.Element(gi) })
    {
    }
}

// DefaultPattern - matches any element (default rule)
public class DefaultPattern : Pattern
{
    public DefaultPattern()
        : base(new System.Collections.Generic.List<Pattern.Element> { new Pattern.Element(new StringC()) })
    {
    }

    public new bool matches(NodePtr nd, MatchContext context)
    {
        // Default pattern matches any element
        return true;
    }
}

// RootPattern - matches the root element
public class RootPattern : Pattern
{
    public RootPattern()
        : base(new System.Collections.Generic.List<Pattern.Element>())
    {
    }

    public new bool matches(NodePtr nd, MatchContext context)
    {
        // Root pattern matches when node has no parent element
        NodePtr parent = new NodePtr();
        if (nd.node!.getParent(ref parent) != AccessResult.accessOK)
            return true;
        // Check if parent is document node (not element)
        GroveString gi = new GroveString();
        return parent.getGi(gi) != AccessResult.accessOK;
    }
}

// IdPattern - matches element by ID attribute
public class IdPattern : Pattern
{
    private StringC id_;

    public IdPattern(StringC id)
        : base(new System.Collections.Generic.List<Pattern.Element>())
    {
        id_ = id;
    }

    public new bool matches(NodePtr nd, MatchContext context)
    {
        var idAtts = context.idAttributeNames();
        foreach (var idAtt in idAtts)
        {
            NamedNodeListPtr atts = new NamedNodeListPtr();
            if (nd.node!.getAttributes(ref atts) != AccessResult.accessOK)
                continue;
            NodePtr att = new NodePtr();
            if (atts.list!.namedNode(new GroveString(idAtt.data(), idAtt.size()), ref att) != AccessResult.accessOK)
                continue;
            if (att.node!.getImplied(out bool implied) == AccessResult.accessOK && implied)
                continue;
            GroveString tokens = new GroveString();
            if (att.node!.tokens(ref tokens) == AccessResult.accessOK)
            {
                if (tokens.size() == id_.size())
                {
                    bool match = true;
                    for (nuint i = 0; i < tokens.size(); i++)
                    {
                        if (tokens.data()![i] != id_.data()![i])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                        return true;
                }
            }
        }
        return false;
    }
}

// MatchPattern - matches element with ancestor path
public class MatchPattern : Pattern
{
    private StringC elementName_;
    private System.Collections.Generic.List<StringC> ancestors_;
    private bool hasDirectParent_;

    public MatchPattern(StringC elementName, System.Collections.Generic.List<StringC> ancestors, bool hasDirectParent)
        : base(buildElements(elementName, ancestors, hasDirectParent))
    {
        elementName_ = elementName;
        ancestors_ = ancestors;
        hasDirectParent_ = hasDirectParent;
    }

    private static System.Collections.Generic.List<Pattern.Element> buildElements(
        StringC elementName, System.Collections.Generic.List<StringC> ancestors, bool hasDirectParent)
    {
        var elems = new System.Collections.Generic.List<Pattern.Element>();

        // First element matches the target element
        elems.Add(new Pattern.Element(elementName));

        if (ancestors.Count > 0)
        {
            if (hasDirectParent)
            {
                // Direct parent relationship - each ancestor is exactly one parent
                foreach (var anc in ancestors)
                {
                    var elem = new Pattern.Element(anc);
                    elem.setRepeat(1, 1);
                    elems.Add(elem);
                }
            }
            else
            {
                // Ancestor relationship - can have any number of intermediate elements
                // First add "any element" with 0+ repetition
                var anyElem = new Pattern.Element(new StringC());
                anyElem.setRepeat(0, uint.MaxValue);
                elems.Add(anyElem);

                // Then add each ancestor
                foreach (var anc in ancestors)
                {
                    elems.Add(new Pattern.Element(anc));
                    // Add "any element" between ancestors
                    var anyBetween = new Pattern.Element(new StringC());
                    anyBetween.setRepeat(0, uint.MaxValue);
                    elems.Add(anyBetween);
                }
            }
        }

        return elems;
    }
}
