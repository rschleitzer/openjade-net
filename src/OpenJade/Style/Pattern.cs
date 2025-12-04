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
        throw new NotImplementedException();
    }

    // Match context for pattern matching
    public class MatchContext : SdataMapper
    {
        protected List<StringC> classAttributeNames_ = new List<StringC>();
        protected List<StringC> idAttributeNames_ = new List<StringC>();

        public List<StringC> classAttributeNames() { return classAttributeNames_; }
        public List<StringC> idAttributeNames() { return idAttributeNames_; }
    }

    // Base class for pattern qualifiers
    public abstract class Qualifier : Link
    {
        public abstract bool satisfies(NodePtr nd, MatchContext context);
        public abstract void contributeSpecificity(int[] spec);
        public virtual bool vacuous() { return false; }

        protected static bool matchAttribute(StringC name, StringC value, NodePtr nd, MatchContext context)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }

    // Last of type qualifier
    public class LastOfTypeQualifier : PositionQualifier
    {
        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            throw new NotImplementedException();
        }
    }

    // First of any qualifier
    public class FirstOfAnyQualifier : PositionQualifier
    {
        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            throw new NotImplementedException();
        }
    }

    // Last of any qualifier
    public class LastOfAnyQualifier : PositionQualifier
    {
        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            throw new NotImplementedException();
        }
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
            throw new NotImplementedException();
        }
    }

    // Only of any qualifier
    public class OnlyOfAnyQualifier : OnlyQualifier
    {
        public override bool satisfies(NodePtr nd, MatchContext context)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
