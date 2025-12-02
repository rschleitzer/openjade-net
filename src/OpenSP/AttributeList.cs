// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class AttributeDefinitionList : Resource
{
    private Vector<AttributeDefinition?> defs_ = new Vector<AttributeDefinition?>();
    private nuint index_;
    private nuint idIndex_;           // -1 if no ID attribute
    private nuint notationIndex_;     // -1 if no notation attribute
    private Boolean anyCurrent_;
    private ConstPtr<AttributeDefinitionList> prev_ = new ConstPtr<AttributeDefinitionList>();

    // AttributeDefinitionList(Vector<CopyOwner<AttributeDefinition> > &,
    //                         size_t listIndex,
    //                         Boolean anyCurrent = 0,
    //                         size_t idIndex = size_t(-1),
    //                         size_t notationIndex = size_t(-1));
    public AttributeDefinitionList(Vector<AttributeDefinition?> vec, nuint index)
        : this(vec, index, false, nuint.MaxValue, nuint.MaxValue)
    {
    }

    public AttributeDefinitionList(Vector<AttributeDefinition?> vec, nuint index, Boolean anyCurrent)
        : this(vec, index, anyCurrent, nuint.MaxValue, nuint.MaxValue)
    {
    }

    public AttributeDefinitionList(Vector<AttributeDefinition?> vec,
                                    nuint index,
                                    Boolean anyCurrent,
                                    nuint idIndex,
                                    nuint notationIndex)
    {
        index_ = index;
        anyCurrent_ = anyCurrent;
        idIndex_ = idIndex;
        notationIndex_ = notationIndex;
        defs_.swap(vec);
    }

    // AttributeDefinitionList(const ConstPtr<AttributeDefinitionList> &);
    public AttributeDefinitionList(ConstPtr<AttributeDefinitionList> def)
    {
        prev_ = def;
        index_ = nuint.MaxValue;
        if (def.isNull())
        {
            anyCurrent_ = false;
            notationIndex_ = nuint.MaxValue;
            idIndex_ = nuint.MaxValue;
        }
        else
        {
            anyCurrent_ = def.pointer()!.anyCurrent_;
            notationIndex_ = def.pointer()!.notationIndex_;
            idIndex_ = def.pointer()!.idIndex_;
            // Copy definitions
            defs_ = new Vector<AttributeDefinition?>(def.pointer()!.defs_.size());
            for (nuint i = 0; i < def.pointer()!.defs_.size(); i++)
                defs_[i] = def.pointer()!.defs_[i]?.copy();
        }
    }

    // Default constructor
    public AttributeDefinitionList()
    {
        index_ = nuint.MaxValue;
        idIndex_ = nuint.MaxValue;
        notationIndex_ = nuint.MaxValue;
        anyCurrent_ = false;
    }

    // ~AttributeDefinitionList();
    // C# GC handles cleanup

    // size_t size() const;
    public nuint size()
    {
        return defs_.size();
    }

    // AttributeDefinition *def(size_t);
    public AttributeDefinition? def(nuint i)
    {
        return defs_[i];
    }

    // const AttributeDefinition *def(size_t) const;
    public AttributeDefinition? defConst(nuint i)
    {
        return defs_[i];
    }

    // Boolean tokenIndex(const StringC &, unsigned &) const;
    public Boolean tokenIndex(StringC token, out uint index)
    {
        for (nuint i = 0; i < defs_.size(); i++)
        {
            if (defs_[i] != null && defs_[i]!.containsToken(token))
            {
                index = (uint)i;
                return true;
            }
        }
        index = 0;
        return false;
    }

    // Boolean tokenIndexUnique(const StringC &, unsigned) const;
    public Boolean tokenIndexUnique(StringC token, uint i)
    {
        for (nuint j = i + 1; j < defs_.size(); j++)
        {
            if (defs_[j] != null && defs_[j]!.containsToken(token))
                return false;
        }
        return true;
    }

    // Boolean attributeIndex(const StringC &, unsigned &) const;
    public Boolean attributeIndex(StringC name, out uint index)
    {
        for (nuint i = 0; i < defs_.size(); i++)
        {
            if (defs_[i] != null && defs_[i]!.name() == name)
            {
                index = (uint)i;
                return true;
            }
        }
        index = 0;
        return false;
    }

    // size_t index() const;
    public nuint index()
    {
        return index_;
    }

    // void setIndex(size_t);
    public void setIndex(nuint index)
    {
        index_ = index;
    }

    // size_t idIndex() const;
    public nuint idIndex()
    {
        return idIndex_;
    }

    // size_t notationIndex() const;
    public nuint notationIndex()
    {
        return notationIndex_;
    }

    // Boolean anyCurrent() const;
    public Boolean anyCurrent()
    {
        return anyCurrent_;
    }

    // void append(AttributeDefinition *);
    public void append(AttributeDefinition? def)
    {
        if (def != null)
        {
            if (def.isId() && idIndex_ == nuint.MaxValue)
                idIndex_ = defs_.size();
            if (def.isNotation() && notationIndex_ == nuint.MaxValue)
                notationIndex_ = defs_.size();
            if (def.isCurrent())
                anyCurrent_ = true;
        }
        defs_.resize(defs_.size() + 1);
        defs_[defs_.size() - 1] = def;
    }
}

// Attribute list for elements
public class AttributeList
{
    private PackedBoolean conref_;
    private uint nIdrefs_;
    private uint nEntityNames_;
    private nuint nSpec_;
    private Vector<Attribute> vec_ = new Vector<Attribute>();
    private ConstPtr<AttributeDefinitionList> def_ = new ConstPtr<AttributeDefinitionList>();

    // AttributeList();
    public AttributeList()
    {
        nSpec_ = 0;
        conref_ = false;
        nIdrefs_ = 0;
        nEntityNames_ = 0;
    }

    // AttributeList(const ConstPtr<AttributeDefinitionList> &);
    public AttributeList(ConstPtr<AttributeDefinitionList> def)
    {
        def_ = def;
        nuint size = def.isNull() ? 0 : def.pointer()!.size();
        vec_ = new Vector<Attribute>(size);
        for (nuint i = 0; i < size; i++)
            vec_[i] = new Attribute();
        nSpec_ = 0;
        conref_ = false;
        nIdrefs_ = 0;
        nEntityNames_ = 0;
    }

    // inline ~AttributeList() {}
    // C# GC handles cleanup

    // void init(const ConstPtr<AttributeDefinitionList> &);
    public void init(ConstPtr<AttributeDefinitionList> def)
    {
        def_ = def;
        nSpec_ = 0;
        conref_ = false;
        nIdrefs_ = 0;
        nEntityNames_ = 0;
        if (def_.isNull())
            vec_.resize(0);
        else
        {
            nuint newLength = def_.pointer()!.size();
            nuint clearLim = vec_.size();
            if (clearLim > newLength)
                clearLim = newLength;
            vec_.resize(newLength);
            for (nuint i = 0; i < clearLim; i++)
                vec_[i].clear();
            for (nuint i = clearLim; i < newLength; i++)
                vec_[i] = new Attribute();
        }
    }

    // Boolean conref() const;
    public Boolean conref()
    {
        return conref_;
    }

    // size_t size() const;
    public nuint size()
    {
        return vec_.size();
    }

    // const StringC &name(unsigned) const;
    public StringC name(uint i)
    {
        return defPrivate((nuint)i)?.name() ?? new StringC();
    }

    // const AttributeValue *value(unsigned) const;
    public AttributeValue? value(uint i)
    {
        return vec_[(nuint)i].value();
    }

    // size_t specIndex(size_t) const;
    public nuint specIndex(nuint i)
    {
        return vec_[i].specIndex();
    }

    // const ConstPtr<AttributeValue> &valuePointer(unsigned) const;
    public ConstPtr<AttributeValue> valuePointer(uint i)
    {
        return vec_[(nuint)i].valuePointer();
    }

    // const AttributeSemantics *semantics(unsigned) const;
    public AttributeSemantics? semantics(uint i)
    {
        return vec_[(nuint)i].semantics();
    }

    // Boolean tokenized(unsigned index) const;
    public Boolean tokenized(uint i)
    {
        return defPrivate((nuint)i)?.tokenized() ?? false;
    }

    // Boolean tokenIndex(const StringC &, unsigned &) const;
    public Boolean tokenIndex(StringC name, out uint index)
    {
        if (!def_.isNull())
            return def_.pointer()!.tokenIndex(name, out index);
        index = 0;
        return false;
    }

    // Boolean tokenIndexUnique(const StringC &, unsigned) const;
    public Boolean tokenIndexUnique(StringC name, uint index)
    {
        return !def_.isNull() && def_.pointer()!.tokenIndexUnique(name, index);
    }

    // Boolean attributeIndex(const StringC &, unsigned &) const;
    public Boolean attributeIndex(StringC name, out uint index)
    {
        if (!def_.isNull())
            return def_.pointer()!.attributeIndex(name, out index);
        index = 0;
        return false;
    }

    // void finish(AttributeContext &);
    public void finish(AttributeContext context)
    {
        for (nuint i = 0; i < vec_.size(); i++)
        {
            if (!vec_[i].specified())
            {
                AttributeDefinition? d = defPrivate(i);
                ConstPtr<AttributeValue> value = d != null
                    ? d.makeMissingValue(context)
                    : new ConstPtr<AttributeValue>();
                if (!conref_ || def_.pointer()?.notationIndex() != i)
                {
                    vec_[i].setValue(value);
                    if (!value.isNull() && d != null)
                        vec_[i].setSemantics(d.makeSemantics(value.pointer(),
                                                              context,
                                                              ref nIdrefs_,
                                                              ref nEntityNames_));
                }
            }
        }
        Syntax syntax = context.attributeSyntax();
        if (nIdrefs_ > syntax.grpcnt())
            context.message(ParserMessages.idrefGrpcnt,
                            new NumberMessageArg(syntax.grpcnt()));
        if (nEntityNames_ > syntax.grpcnt())
            context.message(ParserMessages.entityNameGrpcnt,
                            new NumberMessageArg(syntax.grpcnt()));
        if (context.validate()
            && conref_
            && def_.pointer()?.notationIndex() != nuint.MaxValue
            && specified((uint)def_.pointer()!.notationIndex()))
            context.message(ParserMessages.conrefNotation);
    }

    // Boolean setValue(unsigned index, Text &, AttributeContext &, unsigned &specLength);
    public Boolean setValue(uint index, Text text, AttributeContext context, ref uint specLength)
    {
        AttributeDefinition? d = defPrivate((nuint)index);
        AttributeValue? value = d?.makeValue(text, context, ref specLength);
        if (d != null && d.isConref())
            conref_ = true;
        vec_[(nuint)index].setValue(value);
        if (value != null && d != null)
            vec_[(nuint)index].setSemantics(d.makeSemantics(value, context,
                                                             ref nIdrefs_, ref nEntityNames_));
        else if (AttributeValue.handleAsUnterminated(text, context))
            return false;
        return true;
    }

    // void setValueToken(unsigned index, Text &, AttributeContext &, unsigned &specLength);
    public void setValueToken(uint index, Text text, AttributeContext context, ref uint specLength)
    {
        AttributeDefinition? d = defPrivate((nuint)index);
        AttributeValue? value = d?.makeValueFromToken(text, context, ref specLength);
        if (d != null && d.isConref())
            conref_ = true;
        vec_[(nuint)index].setValue(value);
        if (value != null && d != null)
            vec_[(nuint)index].setSemantics(d.makeSemantics(value, context,
                                                             ref nIdrefs_, ref nEntityNames_));
    }

    // void setSpec(unsigned index, AttributeContext &);
    public void setSpec(uint index, AttributeContext context)
    {
        if (vec_[(nuint)index].specified())
            context.message(ParserMessages.duplicateAttributeSpec,
                            new StringMessageArg(defPrivate((nuint)index)?.name() ?? new StringC()));
        else
            vec_[(nuint)index].setSpec(nSpec_++);
    }

    // Boolean recoverUnquoted(const StringC &, const Location &, AttributeContext &);
    public Boolean recoverUnquoted(StringC str, Location strLoc, AttributeContext context)
    {
        if (nSpec_ > 0)
        {
            for (nuint i = 0; i < vec_.size(); i++)
            {
                if (vec_[i].specified() && vec_[i].specIndex() == nSpec_ - 1)
                {
                    AttributeValue? val = vec_[i].value();
                    if (val != null)
                        return val.recoverUnquoted(str, strLoc, context, name((uint)i));
                    break;
                }
            }
            return true;
        }
        return false;
    }

    // Boolean handleAsUnterminated(AttributeContext &context);
    public Boolean handleAsUnterminated(AttributeContext context)
    {
        if (nSpec_ > 0)
        {
            for (nuint i = 0; i < vec_.size(); i++)
            {
                if (vec_[i].specified() && vec_[i].specIndex() == nSpec_ - 1)
                {
                    AttributeValue? val = vec_[i].value();
                    Text? ptr;
                    if (val != null && (ptr = val.text()) != null
                        && AttributeValue.handleAsUnterminated(ptr, context))
                        return true;
                    break;
                }
            }
        }
        return false;
    }

    // void swap(AttributeList &);
    public void swap(AttributeList to)
    {
        vec_.swap(to.vec_);
        {
            ConstPtr<AttributeDefinitionList> tem = to.def_;
            to.def_ = def_;
            def_ = tem;
        }
        {
            uint tem = to.nIdrefs_;
            to.nIdrefs_ = nIdrefs_;
            nIdrefs_ = tem;
        }
        {
            uint tem = to.nEntityNames_;
            to.nEntityNames_ = nEntityNames_;
            nEntityNames_ = tem;
        }
        {
            nuint tem = to.nSpec_;
            to.nSpec_ = nSpec_;
            nSpec_ = tem;
        }
        {
            PackedBoolean tem = to.conref_;
            to.conref_ = conref_;
            conref_ = tem;
        }
    }

    // size_t nSpec() const;
    public nuint nSpec()
    {
        return nSpec_;
    }

    // size_t defIndex() const;
    public nuint defIndex()
    {
        return def_.isNull() ? nuint.MaxValue : def_.pointer()!.index();
    }

    // Boolean current(unsigned) const;
    public Boolean current(uint i)
    {
        return defPrivate((nuint)i)?.isCurrent() ?? false;
    }

    // Boolean anyCurrent() const;
    public Boolean anyCurrent()
    {
        return !def_.isNull() && def_.pointer()!.anyCurrent();
    }

    // Boolean specified(unsigned) const;
    public Boolean specified(uint i)
    {
        return vec_[(nuint)i].specified();
    }

    // Boolean id(unsigned) const;
    public Boolean id(uint i)
    {
        return defPrivate((nuint)i)?.isId() ?? false;
    }

    // Boolean idref(unsigned) const;
    public Boolean idref(uint i)
    {
        return defPrivate((nuint)i)?.isIdref() ?? false;
    }

    // const Vector<StringC> *getAllowedTokens(unsigned) const;
    public Vector<StringC>? getAllowedTokens(uint i)
    {
        return defPrivate((nuint)i)?.getTokens();
    }

    // const StringC *getId() const;  // null if none
    public StringC? getId()
    {
        // Check for no attributes
        if (def_.isNull())
            return null;
        // Check for no ID declared
        nuint i = def_.pointer()!.idIndex();
        if (i == nuint.MaxValue)
            return null;
        // Check for invalid value
        AttributeValue? v = value((uint)i);
        if (v == null)
            return null;
        // Check for implied value
        Text? t = v.text();
        if (t == null)
            return null;
        return t.@string();
    }

    // Boolean idIndex(unsigned &) const;
    public Boolean idIndex(out uint ind)
    {
        if (def_.isNull() || def_.pointer()!.idIndex() == nuint.MaxValue)
        {
            ind = 0;
            return false;
        }
        else
        {
            ind = (uint)def_.pointer()!.idIndex();
            return true;
        }
    }

    // void noteInvalidSpec();
    public void noteInvalidSpec()
    {
        // This is needed for error recovery.
        // We don't want nSpec_ to be > 0, if there is no attribute definition.
        if (nSpec_ > 0)
            nSpec_++;
    }

    // void changeDef(const ConstPtr<AttributeDefinitionList> &);
    public void changeDef(ConstPtr<AttributeDefinitionList> def)
    {
        vec_.resize(def.isNull() ? 0 : def.pointer()!.size());
        def_ = def;
    }

    // const ConstPtr<AttributeDefinitionList> &def() const;
    public ConstPtr<AttributeDefinitionList> def()
    {
        return def_;
    }

    // const AttributeDefinition *def(size_t) const;
    private AttributeDefinition? defPrivate(nuint i)
    {
        return def_.pointer()?.def(i);
    }
}
