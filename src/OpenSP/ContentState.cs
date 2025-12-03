// Copyright (c) 1994, 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// In C++, ParserState inherits from both ContentState and AttributeContext.
// Since C# doesn't support multiple inheritance, we have ContentState inherit
// from AttributeContext so that ParserState can inherit from ContentState.
// ContentState is abstract because it doesn't implement all abstract methods
// from AttributeContext/Messenger - ParserState provides those implementations.
public abstract class ContentState : AttributeContext
{
    public static readonly ShortReferenceMap theEmptyMap = new ShortReferenceMap();

    private IList<OpenElement> openElements_ = new IList<OpenElement>();
    private Vector<uint> openElementCount_ = new Vector<uint>();
    private Vector<uint> includeCount_ = new Vector<uint>();
    private Vector<uint> excludeCount_ = new Vector<uint>();
    private uint totalExcludeCount_;
    private uint tagLevel_;
    private uint netEnablingCount_;
    private ulong nextIndex_;
    private ElementType? lastEndedElementType_;
    private ElementType documentElementContainer_;

    public ContentState()
    {
        documentElementContainer_ = new ElementType(new StringC(), nuint.MaxValue);
    }

    public void startContent(Dtd dtd)
    {
        Vector<Owner<ContentToken>> tokens = new Vector<Owner<ContentToken>>(1);
        tokens[0] = new Owner<ContentToken>(new ElementToken(dtd.documentElementType(),
                                            ContentToken.OccurrenceIndicator.none));
        Owner<ModelGroup> model = new Owner<ModelGroup>(new SeqModelGroup(tokens, ContentToken.OccurrenceIndicator.none));
        Owner<CompiledModelGroup> compiledModel = new Owner<CompiledModelGroup>(new CompiledModelGroup(model));
        Vector<ContentModelAmbiguity> ambiguities = new Vector<ContentModelAmbiguity>();
        Boolean pcdataUnreachable = false;
        compiledModel.pointer()!.compile(dtd.nElementTypeIndex(), ambiguities, ref pcdataUnreachable);
        // ASSERT(ambiguities.size() == 0);
        ConstPtr<ElementDefinition> def = new ConstPtr<ElementDefinition>(
            new ElementDefinition(new Location(),
                                  0,
                                  0,
                                  ElementDefinition.DeclaredContent.modelGroup,
                                  compiledModel));
        documentElementContainer_.setElementDefinition(def, 0);
        tagLevel_ = 0;
        while (!openElements_.empty())
        {
            OpenElement? removed = openElements_.get();
            // delete removed - handled by GC
        }
        openElements_.insert(new OpenElement(documentElementContainer_,
                                             false,
                                             false,
                                             theEmptyMap,
                                             new Location()));
        includeCount_.assign(dtd.nElementTypeIndex(), 0);
        excludeCount_.assign(dtd.nElementTypeIndex(), 0);
        openElementCount_.assign(dtd.nElementTypeIndex(), 0);
        netEnablingCount_ = 0;
        totalExcludeCount_ = 0;
        lastEndedElementType_ = null;
        nextIndex_ = 0;
    }

    public void pushElement(OpenElement e)
    {
        tagLevel_++;
        openElementCount_[e.type()!.index()]++;
        ElementDefinition? def = e.type()!.definition();
        if (def != null)
        {
            nuint i;
            for (i = 0; i < def.nInclusions(); i++)
                includeCount_[def.inclusion(i)!.index()]++;
            for (i = 0; i < def.nExclusions(); i++)
            {
                excludeCount_[def.exclusion(i)!.index()]++;
                totalExcludeCount_++;
            }
        }
        if (e.netEnabling())
            netEnablingCount_++;
        e.setIndex(nextIndex_++);
        openElements_.insert(e);
    }

    public OpenElement? popSaveElement()
    {
        // ASSERT(tagLevel_ > 0);
        OpenElement? e = openElements_.get();
        if (e == null) return null;
        tagLevel_--;
        openElementCount_[e.type()!.index()]--;
        ElementDefinition? def = e.type()!.definition();
        if (def != null)
        {
            nuint i;
            for (i = 0; i < def.nInclusions(); i++)
                includeCount_[def.inclusion(i)!.index()]--;
            for (i = 0; i < def.nExclusions(); i++)
            {
                excludeCount_[def.exclusion(i)!.index()]--;
                totalExcludeCount_--;
            }
        }
        if (e.netEnabling())
            netEnablingCount_--;
        lastEndedElementType_ = e.type();
        return e;
    }

    public void popElement()
    {
        OpenElement? e = popSaveElement();
        // delete e - handled by GC
    }

    public OpenElement currentElement()
    {
        return openElements_.head()!;
    }

    public Boolean elementIsOpen(ElementType e)
    {
        return openElementCount_[e.index()] != 0;
    }

    public Boolean elementIsIncluded(ElementType e)
    {
        return includeCount_[e.index()] != 0 && excludeCount_[e.index()] == 0;
    }

    public Boolean elementIsExcluded(ElementType e)
    {
        return excludeCount_[e.index()] != 0;
    }

    public ElementType? lastEndedElementType()
    {
        return lastEndedElementType_;
    }

    public uint tagLevel()
    {
        return tagLevel_;
    }

    public Boolean afterDocumentElement()
    {
        return tagLevel() == 0 && currentElement().isFinished();
    }

    public Mode contentMode()
    {
        return openElements_.head()!.mode(netEnablingCount_ > 0);
    }

    public Boolean checkImplyLoop(uint count)
    {
        IListIter<OpenElement> iter = new IListIter<OpenElement>(openElements_);
        for (; count > 0; iter.next(), count--)
        {
            if (iter.cur()!.type() == openElements_.head()!.type()
                // I'm not sure whether this is necessary.
                && iter.cur()!.matchState().Equals(openElements_.head()!.matchState()))
                return false;
        }
        return true;
    }

    public void getOpenElementInfo(Vector<OpenElementInfo> v, StringC rniPcdata)
    {
        v.clear();
        v.resize(tagLevel_);
        uint i = tagLevel_;
        IListIter<OpenElement> iter = new IListIter<OpenElement>(openElements_);
        for (; iter.done() == 0 && i > 0; iter.next())
        {
            OpenElementInfo e = v[--i];
            e.gi = iter.cur()!.type()!.name();
            LeafContentToken? token = iter.cur()!.currentPosition();
            if (token != null && token.isInitial() == false)
            {
                e.matchIndex = (uint)((int)token.typeIndex() + 1);
                ElementType? type = token.elementType();
                e.matchType = type != null ? type.name() : rniPcdata;
            }
            e.included = iter.cur()!.included();
        }
    }

    public ElementType lookupCreateUndefinedElement(StringC name, Location loc,
                                                    Dtd dtd,
                                                    Boolean allowImmediateRecursion = true)
    {
        ElementType p = new ElementType(name, dtd.allocElementTypeIndex());
        dtd.insertElementType(p);
        p.setElementDefinition(new ConstPtr<ElementDefinition>(
            new ElementDefinition(loc,
                                  nuint.MaxValue, // size_t(undefinedIndex)
                                  (byte)ElementDefinition.OmitFlags.omitEnd,
                                  ElementDefinition.DeclaredContent.any,
                                  allowImmediateRecursion)),
                               0);
        p.setAttributeDef(dtd.implicitElementAttributeDef());

        includeCount_.push_back(0);
        excludeCount_.push_back(0);
        openElementCount_.push_back(0);
        return p;
    }
}
