// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Forward declarations
// class Entity;
// class Notation;
// class DeclaredValue;
// class AttributeValue;
// class TokenizedAttributeValue;
// class AttributeSemantics;
// class AttributeContext;
// class Syntax;

public class AttributeValue : Resource
{
    public enum Type
    {
        implied,
        cdata,
        tokenized
    }

    // AttributeValue();
    public AttributeValue()
    {
    }

    // virtual ~AttributeValue();
    // C# GC handles cleanup

    // virtual AttributeSemantics *makeSemantics(const DeclaredValue *,
    //                                           AttributeContext &,
    //                                           const StringC &,
    //                                           unsigned &,
    //                                           unsigned &) const;
    public virtual AttributeSemantics? makeSemantics(DeclaredValue? declaredValue,
                                                      AttributeContext context,
                                                      StringC name,
                                                      ref uint nIdrefs,
                                                      ref uint nEntityNames)
    {
        return null;
    }

    // virtual Type info(const Text *&, const StringC *&) const = 0;
    public virtual Type info(out Text? text, out StringC? str)
    {
        text = null;
        str = null;
        return Type.implied;
    }

    // virtual const Text *text() const;
    public virtual Text? text()
    {
        return null;
    }

    // virtual Boolean recoverUnquoted(const StringC &, const Location &,
    //                                 AttributeContext &, const StringC &);
    public virtual Boolean recoverUnquoted(StringC str, Location strLoc,
                                            AttributeContext context, StringC name)
    {
        return false;
    }

    // static Boolean handleAsUnterminated(const Text &, AttributeContext &);
    public static Boolean handleAsUnterminated(Text text, AttributeContext context)
    {
        TextIter iter = new TextIter(text);
        Char[]? lastStr = null;
        nuint lastLen = 0;
        Location startLoc = new Location();
        TextItem.Type type;
        Char[]? s;
        nuint len;
        Location loc = new Location();
        while (iter.next(out type, out s, out len, out loc!))
        {
            if (startLoc.origin().isNull() && !loc.origin().isNull())
                startLoc = new Location(loc);
            switch (type)
            {
                case TextItem.Type.data:
                    if (len != 1 || s == null || s[0] != context.attributeSyntax().space())
                    {
                        lastStr = s;
                        lastLen = len;
                    }
                    break;
                case TextItem.Type.endDelim:
                case TextItem.Type.endDelimA:
                case TextItem.Type.ignore:
                    break;
                default:
                    lastStr = null;
                    break;
            }
        }
        if (lastStr != null)
        {
            while (lastLen > 0 && lastStr[lastLen - 1] == context.attributeSyntax().space())
                lastLen--;
            StringC vi = context.attributeSyntax().delimGeneral((int)Syntax.DelimGeneral.dVI);
            if (lastLen >= vi.size())
            {
                // Check if ends with vi
                Boolean match = true;
                for (nuint i = 0; i < vi.size() && match; i++)
                {
                    if (lastStr[lastLen - vi.size() + i] != vi[i])
                        match = false;
                }
                if (match)
                {
                    context.setNextLocation(startLoc);
                    context.message(ParserMessages.literalClosingDelimiter);
                    return true;
                }
            }
        }
        return false;
    }
}

public class AttributeDefinitionDesc
{
    public enum DeclaredValue
    {
        cdata,
        name,
        number,
        nmtoken,
        nutoken,
        entity,
        idref,
        names,
        numbers,
        nmtokens,
        nutokens,
        entities,
        idrefs,
        id,
        notation,
        nameTokenGroup
    }

    public enum DefaultValueType
    {
        required,
        current,
        implied,
        conref,
        defaulted,
        @fixed
    }

    public DeclaredValue declaredValue;
    public DefaultValueType defaultValueType;
    public ConstPtr<AttributeValue> defaultValue = new ConstPtr<AttributeValue>();
    public Vector<StringC> allowedValues = new Vector<StringC>();
    public Vector<StringC> origAllowedValues = new Vector<StringC>();
    // Attribute definitions whose default value type is current and
    // which have the same currentIndex share current values.
    public nuint currentIndex;

    // AttributeDefinitionDesc() { }
    public AttributeDefinitionDesc()
    {
    }
}

public abstract class DeclaredValue
{
    // DeclaredValue();
    public DeclaredValue()
    {
    }

    // virtual ~DeclaredValue();
    // C# GC handles cleanup

    // virtual AttributeValue *makeValue(Text &, AttributeContext &,
    //                                   const StringC &name,
    //                                   unsigned &specLength) const = 0;
    public abstract AttributeValue? makeValue(Text text, AttributeContext context,
                                               StringC name, ref uint specLength);

    // virtual AttributeValue *makeValueFromToken(Text &,
    //                                            AttributeContext &,
    //                                            const StringC &name,
    //                                            unsigned &specLength) const;
    public virtual AttributeValue? makeValueFromToken(Text text,
                                                       AttributeContext context,
                                                       StringC name,
                                                       ref uint specLength)
    {
        return makeValue(text, context, name, ref specLength);
    }

    // virtual AttributeSemantics *makeSemantics(const TokenizedAttributeValue &,
    //                                           AttributeContext &,
    //                                           const StringC &,
    //                                           unsigned &nIdrefs,
    //                                           unsigned &nEntityNames) const;
    public virtual AttributeSemantics? makeSemantics(TokenizedAttributeValue value,
                                                      AttributeContext context,
                                                      StringC name,
                                                      ref uint nIdrefs,
                                                      ref uint nEntityNames)
    {
        return null;
    }

    // virtual Boolean containsToken(const StringC &) const;
    public virtual Boolean containsToken(StringC token)
    {
        return false;
    }

    // virtual Boolean tokenized() const = 0;
    public abstract Boolean tokenized();

    // virtual Boolean isNotation() const;
    public virtual Boolean isNotation()
    {
        return false;
    }

    // virtual Boolean isEntity() const;
    public virtual Boolean isEntity()
    {
        return false;
    }

    // virtual Boolean isId() const;
    public virtual Boolean isId()
    {
        return false;
    }

    // virtual Boolean isIdref() const;
    public virtual Boolean isIdref()
    {
        return false;
    }

    // virtual const Vector<StringC> *getTokens() const;
    public virtual Vector<StringC>? getTokens()
    {
        return null;
    }

    // virtual const Vector<StringC> *getOrigTokens() const;
    public virtual Vector<StringC>? getOrigTokens()
    {
        return null;
    }

    // virtual void buildDesc(AttributeDefinitionDesc &) const = 0;
    public abstract void buildDesc(AttributeDefinitionDesc desc);

    // virtual DeclaredValue *copy() const = 0;
    public abstract DeclaredValue copy();
}

public class CdataDeclaredValue : DeclaredValue
{
    // CdataDeclaredValue();
    public CdataDeclaredValue()
    {
    }

    // Boolean tokenized() const;
    public override Boolean tokenized()
    {
        return false;
    }

    // AttributeValue *makeValue(Text &, AttributeContext &, const StringC &,
    //                           unsigned &) const;
    public override AttributeValue? makeValue(Text text, AttributeContext context,
                                               StringC name, ref uint specLength)
    {
        checkNormalizedLength(text, context, ref specLength);
        return new CdataAttributeValue(text);
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    public override void buildDesc(AttributeDefinitionDesc desc)
    {
        desc.declaredValue = AttributeDefinitionDesc.DeclaredValue.cdata;
    }

    // DeclaredValue *copy() const;
    public override DeclaredValue copy()
    {
        return new CdataDeclaredValue();
    }

    // void checkNormalizedLength(Text &, AttributeContext &, unsigned &) const;
    protected void checkNormalizedLength(Text text, AttributeContext context, ref uint specLength)
    {
        Syntax syntax = context.attributeSyntax();
        nuint normsep = syntax.normsep();
        nuint normalizedLength = text.normalizedLength(normsep);
        specLength += (uint)normalizedLength;
        nuint litlen = syntax.litlen();
        // A length error will already have been given if
        // length > litlen - normsep.
        if (litlen >= normsep && text.size() <= litlen - normsep
            && normalizedLength > litlen)
            context.message(ParserMessages.normalizedAttributeValueLength,
                            new NumberMessageArg(litlen),
                            new NumberMessageArg(normalizedLength));
    }
}

public class TokenizedDeclaredValue : DeclaredValue
{
    // must be in same order as AttributeDefinitionDesc
    public enum TokenType
    {
        name,
        number,
        nameToken,
        numberToken,
        entityName
    }

    protected TokenType type_;
    protected Boolean isList_;
    protected uint initialCategories_;
    protected uint subsequentCategories_;

    // TokenizedDeclaredValue(TokenType type, Boolean isList);
    public TokenizedDeclaredValue(TokenType type, Boolean isList)
    {
        type_ = type;
        isList_ = isList;
        switch (type)
        {
            case TokenType.name:
            case TokenType.entityName:
                initialCategories_ = Syntax.nameStartCategory;
                subsequentCategories_ = (Syntax.nameStartCategory | Syntax.digitCategory
                                         | Syntax.otherNameCategory);
                break;
            case TokenType.number:
                initialCategories_ = Syntax.digitCategory;
                subsequentCategories_ = Syntax.digitCategory;
                break;
            case TokenType.nameToken:
                initialCategories_ = (Syntax.nameStartCategory | Syntax.digitCategory
                                      | Syntax.otherNameCategory);
                subsequentCategories_ = initialCategories_;
                break;
            case TokenType.numberToken:
                initialCategories_ = Syntax.digitCategory;
                subsequentCategories_ = (Syntax.nameStartCategory | Syntax.digitCategory
                                         | Syntax.otherNameCategory);
                break;
        }
    }

    // For derived class copy constructors
    protected TokenizedDeclaredValue(TokenizedDeclaredValue other)
    {
        type_ = other.type_;
        isList_ = other.isList_;
        initialCategories_ = other.initialCategories_;
        subsequentCategories_ = other.subsequentCategories_;
    }

    // Boolean tokenized() const;
    public override Boolean tokenized()
    {
        return true;
    }

    // AttributeValue *makeValue(Text &, AttributeContext &, const StringC &,
    //                           unsigned &) const;
    public override AttributeValue? makeValue(Text text, AttributeContext context,
                                               StringC name, ref uint specLength)
    {
        return makeTokenizedValue(text, context, name, ref specLength);
    }

    // TokenizedAttributeValue *makeTokenizedValue(Text &, AttributeContext &,
    //                                             const StringC &, unsigned &) const;
    public TokenizedAttributeValue? makeTokenizedValue(Text text, AttributeContext context,
                                                        StringC name, ref uint specLength)
    {
        Vector<nuint> spaceIndex = new Vector<nuint>();
        Syntax syntax = context.attributeSyntax();
        Char space = syntax.space();
        text.subst((type_ == TokenType.entityName
                   ? syntax.entitySubstTable()
                   : syntax.generalSubstTable())!,
                   space);
        StringC value = text.@string();
        nuint i = 0;
        nuint length = value.size();

        for (; ; )
        {
            if (i >= length)
            {
                // ends with a space
                if (context.validate())
                    context.message(ParserMessages.attributeValueSyntax);
                break;
            }
            nuint startIndex = i;
            if (context.validate())
            {
                if (((uint)syntax.charCategory((Xchar)value[i]) & initialCategories_) == 0)
                {
                    context.setNextLocation(text.charLocation(i));
                    Char c = value[i];
                    if (((uint)syntax.charCategory((Xchar)value[i]) & subsequentCategories_) == 0)
                        context.message(ParserMessages.attributeValueChar,
                                        new StringMessageArg(new StringC(c)),
                                        new StringMessageArg(name));
                    else if (initialCategories_ == Syntax.digitCategory)
                        context.message(ParserMessages.attributeValueNumberToken,
                                        new StringMessageArg(new StringC(c)),
                                        new StringMessageArg(name));
                    else
                        context.message(ParserMessages.attributeValueName,
                                        new StringMessageArg(new StringC(c)),
                                        new StringMessageArg(name));
                }
                else
                {
                    for (++i;
                         i < length && ((uint)syntax.charCategory((Xchar)value[i]) & subsequentCategories_) != 0;
                         i++)
                        ;
                    if (i < length && value[i] != space)
                    {
                        Char c = value[i];
                        context.setNextLocation(text.charLocation(i));
                        context.message(ParserMessages.attributeValueChar,
                                        new StringMessageArg(new StringC(c)),
                                        new StringMessageArg(name));
                    }
                }
            }
            while (i < length && value[i] != space)
                i++;
            if (i - startIndex > syntax.namelen())
            {
                context.setNextLocation(text.charLocation(i));
                context.message(ParserMessages.nameTokenLength,
                                new NumberMessageArg(syntax.namelen()));
            }
            if (i == length)
                break;
            if (!isList_ && context.validate() && spaceIndex.size() == 0)
            {
                context.setNextLocation(text.charLocation(i));
                context.message(ParserMessages.attributeValueMultiple,
                                new StringMessageArg(name));
            }
            spaceIndex.push_back(i);
            i++;
        }
        nuint normsep = syntax.normsep();
        nuint litlen = syntax.litlen();
        nuint normalizedLength = normsep + length;
        if (isList_)
        {
            normalizedLength += 1;
            if (normsep > 0)
                normalizedLength += (normsep - 1) * (spaceIndex.size() + 1);
            else
                normalizedLength -= spaceIndex.size() + 1;
        }
        specLength += (uint)normalizedLength;
        if (litlen >= normsep && length <= litlen - normsep
            && normalizedLength > litlen)
            context.message(ParserMessages.normalizedAttributeValueLength,
                            new NumberMessageArg(litlen),
                            new NumberMessageArg(normalizedLength));
        return new TokenizedAttributeValue(text, spaceIndex);
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    public override void buildDesc(AttributeDefinitionDesc desc)
    {
        desc.declaredValue = (AttributeDefinitionDesc.DeclaredValue)(
            (int)type_ - (int)TokenType.name + (isList_
                ? (int)AttributeDefinitionDesc.DeclaredValue.names
                : (int)AttributeDefinitionDesc.DeclaredValue.name));
    }

    // DeclaredValue *copy() const;
    public override DeclaredValue copy()
    {
        return new TokenizedDeclaredValue(type_, isList_);
    }
}

public class GroupDeclaredValue : TokenizedDeclaredValue
{
    protected Vector<StringC> allowedValues_ = new Vector<StringC>();
    protected Vector<StringC> origAllowedValues_ = new Vector<StringC>();

    // GroupDeclaredValue(TokenType, Vector<StringC> &);
    public GroupDeclaredValue(TokenType type, Vector<StringC> vec)
        : base(type, false)
    {
        vec.swap(allowedValues_);
    }

    // For derived class copy constructors
    protected GroupDeclaredValue(GroupDeclaredValue other)
        : base(other)
    {
        allowedValues_ = new Vector<StringC>(other.allowedValues_.size());
        for (nuint i = 0; i < other.allowedValues_.size(); i++)
            allowedValues_[i] = new StringC(other.allowedValues_[i]);
        origAllowedValues_ = new Vector<StringC>(other.origAllowedValues_.size());
        for (nuint i = 0; i < other.origAllowedValues_.size(); i++)
            origAllowedValues_[i] = new StringC(other.origAllowedValues_[i]);
    }

    // Boolean containsToken(const StringC &) const;
    public override Boolean containsToken(StringC token)
    {
        for (nuint i = 0; i < allowedValues_.size(); i++)
            if (allowedValues_[i] == token)
                return true;
        return false;
    }

    // AttributeValue *makeValue(Text &, AttributeContext &, const StringC &,
    //                           unsigned &) const;
    public override AttributeValue? makeValue(Text text, AttributeContext context,
                                               StringC name, ref uint specLength)
    {
        TokenizedAttributeValue? val = makeTokenizedValue(text, context, name, ref specLength);
        if (val == null || !context.validate())
            return val;
        for (nuint i = 0; i < allowedValues_.size(); i++)
            if (val.@string() == allowedValues_[i])
                return val;
        context.message(ParserMessages.attributeValueNotInGroup,
                        new StringMessageArg(val.@string()),
                        new StringMessageArg(name),
                        new StringVectorMessageArg(allowedValues_));
        return val;
    }

    // AttributeValue *makeValueFromToken(Text &,
    //                                    AttributeContext &,
    //                                    const StringC &name,
    //                                    unsigned &) const;
    public override AttributeValue? makeValueFromToken(Text text,
                                                        AttributeContext context,
                                                        StringC name,
                                                        ref uint specLength)
    {
        Syntax syntax = context.attributeSyntax();
        nuint litlen = syntax.litlen();
        nuint normsep = syntax.normsep();
        if (normsep > litlen || text.size() > litlen - normsep)
            context.message(ParserMessages.normalizedAttributeValueLength,
                            new NumberMessageArg(litlen),
                            new NumberMessageArg(text.size() + normsep));
        specLength += (uint)(text.size() + normsep);
        return new TokenizedAttributeValue(text, new Vector<nuint>());
    }

    // const Vector<StringC> *getTokens() const;
    public override Vector<StringC>? getTokens()
    {
        return allowedValues_;
    }

    // const Vector<StringC> *getOrigTokens() const;
    public override Vector<StringC>? getOrigTokens()
    {
        return origAllowedValues_;
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    public override void buildDesc(AttributeDefinitionDesc desc)
    {
        desc.allowedValues = allowedValues_;
        desc.origAllowedValues = origAllowedValues_;
    }

    // DeclaredValue *copy() const;
    public override DeclaredValue copy()
    {
        return new GroupDeclaredValue(this);
    }

    // void setOrigAllowedValues(Vector<StringC> &origAllowedValues);
    public void setOrigAllowedValues(Vector<StringC> origAllowedValues)
    {
        origAllowedValues.swap(origAllowedValues_);
    }
}

public class NameTokenGroupDeclaredValue : GroupDeclaredValue
{
    // NameTokenGroupDeclaredValue(Vector<StringC> &);
    public NameTokenGroupDeclaredValue(Vector<StringC> vec)
        : base(TokenType.nameToken, vec)
    {
    }

    // Copy constructor
    protected NameTokenGroupDeclaredValue(NameTokenGroupDeclaredValue other)
        : base(other)
    {
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    public override void buildDesc(AttributeDefinitionDesc desc)
    {
        base.buildDesc(desc);
        desc.declaredValue = AttributeDefinitionDesc.DeclaredValue.nameTokenGroup;
    }

    // DeclaredValue *copy() const;
    public override DeclaredValue copy()
    {
        return new NameTokenGroupDeclaredValue(this);
    }
}

public class NotationDeclaredValue : GroupDeclaredValue
{
    // NotationDeclaredValue(Vector<StringC> &);
    public NotationDeclaredValue(Vector<StringC> vec)
        : base(TokenType.name, vec)
    {
    }

    // Copy constructor
    protected NotationDeclaredValue(NotationDeclaredValue other)
        : base(other)
    {
    }

    // Boolean isNotation() const;
    public override Boolean isNotation()
    {
        return true;
    }

    // AttributeSemantics *makeSemantics(const TokenizedAttributeValue &,
    //                                   AttributeContext &,
    //                                   const StringC &,
    //                                   unsigned &nIdrefs,
    //                                   unsigned &nEntityNames) const;
    public override AttributeSemantics? makeSemantics(TokenizedAttributeValue value,
                                                       AttributeContext context,
                                                       StringC name,
                                                       ref uint nIdrefs,
                                                       ref uint nEntityNames)
    {
        ConstPtr<Notation> notation = context.getAttributeNotation(value.@string(),
                                                                    value.tokenLocation(0));
        if (notation.isNull())
        {
            if (context.validate())
            {
                context.setNextLocation(value.tokenLocation(0));
                context.message(ParserMessages.invalidNotationAttribute,
                                new StringMessageArg(value.@string()));
            }
            return null;
        }
        return new NotationAttributeSemantics(notation);
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    public override void buildDesc(AttributeDefinitionDesc desc)
    {
        base.buildDesc(desc);
        desc.declaredValue = AttributeDefinitionDesc.DeclaredValue.notation;
    }

    // DeclaredValue *copy() const;
    public override DeclaredValue copy()
    {
        return new NotationDeclaredValue(this);
    }
}

public class EntityDeclaredValue : TokenizedDeclaredValue
{
    // EntityDeclaredValue(Boolean isList);
    public EntityDeclaredValue(Boolean isList)
        : base(TokenType.entityName, isList)
    {
    }

    // Boolean isEntity() const;
    public override Boolean isEntity()
    {
        return true;
    }

    // AttributeSemantics *makeSemantics(const TokenizedAttributeValue &,
    //                                   AttributeContext &,
    //                                   const StringC &,
    //                                   unsigned &nIdrefs,
    //                                   unsigned &nEntityNames) const;
    public override AttributeSemantics? makeSemantics(TokenizedAttributeValue value,
                                                       AttributeContext context,
                                                       StringC name,
                                                       ref uint nIdrefs,
                                                       ref uint nEntityNames)
    {
        Boolean valid = true;
        nuint nTokens = value.nTokens();
        nEntityNames += (uint)nTokens;
        Vector<ConstPtr<Entity>> entities = new Vector<ConstPtr<Entity>>(nTokens);
        for (nuint i = 0; i < nTokens; i++)
        {
            entities[i] = context.getAttributeEntity(value.token(i), value.tokenLocation(i));
            if (entities[i].isNull())
            {
                if (context.validate())
                {
                    context.setNextLocation(value.tokenLocation(i));
                    context.message(ParserMessages.invalidEntityAttribute,
                                    new StringMessageArg(value.token(i)));
                }
                valid = false;
            }
            else if (entities[i].pointer() != null && !entities[i].pointer()!.isDataOrSubdoc())
            {
                if (context.validate())
                {
                    context.setNextLocation(value.tokenLocation(i));
                    context.message(ParserMessages.notDataOrSubdocEntity,
                                    new StringMessageArg(value.token(i)));
                }
                valid = false;
            }
        }
        if (valid)
            return new EntityAttributeSemantics(entities);
        else
            return null;
    }

    // DeclaredValue *copy() const;
    public override DeclaredValue copy()
    {
        return new EntityDeclaredValue(isList_);
    }
}

public class IdDeclaredValue : TokenizedDeclaredValue
{
    // IdDeclaredValue();
    public IdDeclaredValue()
        : base(TokenType.name, false)
    {
    }

    // Boolean isId() const;
    public override Boolean isId()
    {
        return true;
    }

    // AttributeSemantics *makeSemantics(const TokenizedAttributeValue &,
    //                                   AttributeContext &,
    //                                   const StringC &,
    //                                   unsigned &nIdrefs,
    //                                   unsigned &nEntityNames) const;
    public override AttributeSemantics? makeSemantics(TokenizedAttributeValue value,
                                                       AttributeContext context,
                                                       StringC name,
                                                       ref uint nIdrefs,
                                                       ref uint nEntityNames)
    {
        Location prevLoc = new Location();
        if (!context.defineId(value.@string(), value.tokenLocation(0), ref prevLoc))
        {
            context.setNextLocation(value.tokenLocation(0));
            context.message(ParserMessages.duplicateId,
                            new StringMessageArg(value.@string()),
                            new LocationMessageArg(prevLoc));
        }
        return null;
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    public override void buildDesc(AttributeDefinitionDesc desc)
    {
        desc.declaredValue = AttributeDefinitionDesc.DeclaredValue.id;
    }

    // DeclaredValue *copy() const;
    public override DeclaredValue copy()
    {
        return new IdDeclaredValue();
    }
}

public class IdrefDeclaredValue : TokenizedDeclaredValue
{
    // IdrefDeclaredValue(Boolean isList);
    public IdrefDeclaredValue(Boolean isList)
        : base(TokenType.name, isList)
    {
    }

    // Boolean isIdref() const;
    public override Boolean isIdref()
    {
        return true;
    }

    // AttributeSemantics *makeSemantics(const TokenizedAttributeValue &,
    //                                   AttributeContext &,
    //                                   const StringC &,
    //                                   unsigned &nIdrefs,
    //                                   unsigned &nEntityNames) const;
    public override AttributeSemantics? makeSemantics(TokenizedAttributeValue value,
                                                       AttributeContext context,
                                                       StringC name,
                                                       ref uint nIdrefs,
                                                       ref uint nEntityNames)
    {
        nuint nTokens = value.nTokens();
        nIdrefs += (uint)nTokens;
        for (nuint i = 0; i < nTokens; i++)
            context.noteIdref(value.token(i), value.tokenLocation(i));
        return null;
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    public override void buildDesc(AttributeDefinitionDesc desc)
    {
        base.buildDesc(desc);
        if (desc.declaredValue == AttributeDefinitionDesc.DeclaredValue.name)
            desc.declaredValue = AttributeDefinitionDesc.DeclaredValue.idref;
        else
            desc.declaredValue = AttributeDefinitionDesc.DeclaredValue.idrefs;
    }

    // DeclaredValue *copy() const;
    public override DeclaredValue copy()
    {
        return new IdrefDeclaredValue(isList_);
    }
}

public abstract class AttributeDefinition
{
    private PackedBoolean implicit_;
    private PackedBoolean all_;
    private StringC name_ = new StringC();
    private StringC origName_ = new StringC();
    private DeclaredValue? declaredValue_;

    // AttributeDefinition(const StringC &, DeclaredValue *);
    public AttributeDefinition(StringC name, DeclaredValue? value)
    {
        name_ = name;
        declaredValue_ = value;
        implicit_ = false;
        all_ = false;
    }

    // virtual ~AttributeDefinition();
    // C# GC handles cleanup

    // virtual ConstPtr<AttributeValue> makeMissingValue(AttributeContext &) const = 0;
    public abstract ConstPtr<AttributeValue> makeMissingValue(AttributeContext context);

    // virtual Boolean missingValueWouldMatch(const Text &,
    //                                        const AttributeContext &) const;
    public virtual Boolean missingValueWouldMatch(Text text, AttributeContext context)
    {
        return false;
    }

    // virtual const AttributeValue *defaultValue(const AttributeValue *impliedValue) const;
    public virtual AttributeValue? defaultValue(AttributeValue? impliedValue)
    {
        return null;
    }

    // AttributeValue *makeValue(Text &, AttributeContext &, unsigned &) const;
    public AttributeValue? makeValue(Text text, AttributeContext context, ref uint specLength)
    {
        return checkValue(declaredValue_?.makeValue(text, context, name_, ref specLength), context);
    }

    // AttributeValue *makeValueFromToken(Text &, AttributeContext &, unsigned &) const;
    public AttributeValue? makeValueFromToken(Text text, AttributeContext context, ref uint specLength)
    {
        return checkValue(declaredValue_?.makeValueFromToken(text, context, name_, ref specLength), context);
    }

    // virtual Boolean isConref() const;
    public virtual Boolean isConref()
    {
        return false;
    }

    // virtual Boolean isCurrent() const;
    public virtual Boolean isCurrent()
    {
        return false;
    }

    // virtual Boolean isFixed() const;
    public virtual Boolean isFixed()
    {
        return false;
    }

    // AttributeSemantics *makeSemantics(const AttributeValue *,
    //                                   AttributeContext &,
    //                                   unsigned &nIdrefs,
    //                                   unsigned &nEntityNames) const;
    public AttributeSemantics? makeSemantics(AttributeValue? value,
                                              AttributeContext context,
                                              ref uint nIdrefs,
                                              ref uint nEntityNames)
    {
        return value?.makeSemantics(declaredValue_, context, name_, ref nIdrefs, ref nEntityNames);
    }

    // Boolean tokenized() const;
    public Boolean tokenized()
    {
        return declaredValue_?.tokenized() ?? false;
    }

    // const StringC &name() const;
    public StringC name()
    {
        return name_;
    }

    // const StringC &origName() const;
    public StringC origName()
    {
        return origName_;
    }

    // Boolean containsToken(const StringC &) const;
    public Boolean containsToken(StringC token)
    {
        return declaredValue_?.containsToken(token) ?? false;
    }

    // Boolean isNotation() const;
    public Boolean isNotation()
    {
        return declaredValue_?.isNotation() ?? false;
    }

    // Boolean isEntity() const;
    public Boolean isEntity()
    {
        return declaredValue_?.isEntity() ?? false;
    }

    // Boolean isId() const;
    public Boolean isId()
    {
        return declaredValue_?.isId() ?? false;
    }

    // Boolean isIdref() const;
    public Boolean isIdref()
    {
        return declaredValue_?.isIdref() ?? false;
    }

    // void getDesc(AttributeDefinitionDesc &) const;
    public void getDesc(AttributeDefinitionDesc desc)
    {
        desc.allowedValues.clear();
        desc.defaultValue = new ConstPtr<AttributeValue>();
        desc.currentIndex = 0;
        buildDesc(desc);
        declaredValue_?.buildDesc(desc);
    }

    // const Vector<StringC> *getTokens() const;
    public Vector<StringC>? getTokens()
    {
        return declaredValue_?.getTokens();
    }

    // const Vector<StringC> *getOrigTokens() const;
    public Vector<StringC>? getOrigTokens()
    {
        return declaredValue_?.getOrigTokens();
    }

    // virtual AttributeDefinition *copy() const = 0;
    public abstract AttributeDefinition copy();

    // void setDeclaredValue(DeclaredValue *);
    public void setDeclaredValue(DeclaredValue? declaredValue)
    {
        declaredValue_ = declaredValue;
    }

    // void setSpecified(Boolean implicit);
    public void setSpecified(Boolean @implicit)
    {
        if (@implicit)
            implicit_ = true;
        else
            all_ = true;
    }

    // Boolean isSpecified(Boolean &implicit);
    public Boolean isSpecified(out Boolean @implicit)
    {
        @implicit = implicit_;
        return implicit_ || all_;
    }

    // void setOrigName(StringC &origName);
    public void setOrigName(StringC origName)
    {
        origName.swap(origName_);
    }

    // virtual void buildDesc(AttributeDefinitionDesc &) const = 0;
    protected abstract void buildDesc(AttributeDefinitionDesc desc);

    // virtual AttributeValue *checkValue(AttributeValue *, AttributeContext &) const;
    protected virtual AttributeValue? checkValue(AttributeValue? value, AttributeContext context)
    {
        return value;
    }
}

public class RequiredAttributeDefinition : AttributeDefinition
{
    // RequiredAttributeDefinition(const StringC &, DeclaredValue *);
    public RequiredAttributeDefinition(StringC name, DeclaredValue? value)
        : base(name, value)
    {
    }

    // ConstPtr<AttributeValue> makeMissingValue(AttributeContext &) const;
    public override ConstPtr<AttributeValue> makeMissingValue(AttributeContext context)
    {
        if (context.validate())
            context.message(ParserMessages.requiredAttributeMissing,
                            new StringMessageArg(name()));
        return new ConstPtr<AttributeValue>();
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    protected override void buildDesc(AttributeDefinitionDesc desc)
    {
        desc.defaultValueType = AttributeDefinitionDesc.DefaultValueType.required;
    }

    // AttributeDefinition *copy() const;
    public override AttributeDefinition copy()
    {
        return new RequiredAttributeDefinition(name(), null); // Note: declaredValue not copied properly
    }
}

public class CurrentAttributeDefinition : AttributeDefinition
{
    private nuint currentIndex_;

    // CurrentAttributeDefinition(const StringC &, DeclaredValue *, size_t index);
    public CurrentAttributeDefinition(StringC name, DeclaredValue? value, nuint index)
        : base(name, value)
    {
        currentIndex_ = index;
    }

    // ConstPtr<AttributeValue> makeMissingValue(AttributeContext &) const;
    public override ConstPtr<AttributeValue> makeMissingValue(AttributeContext context)
    {
        if (context.mayDefaultAttribute())
        {
            ConstPtr<AttributeValue> currentValue = context.getCurrentAttribute(currentIndex_);
            if (currentValue.isNull() && context.validate())
                context.message(ParserMessages.currentAttributeMissing,
                                new StringMessageArg(name()));
            return currentValue;
        }
        if (context.validate())
            context.message(ParserMessages.attributeMissing,
                            new StringMessageArg(name()));
        return new ConstPtr<AttributeValue>();
    }

    // Boolean missingValueWouldMatch(const Text &, const AttributeContext &) const;
    public override Boolean missingValueWouldMatch(Text text, AttributeContext context)
    {
        if (!context.mayDefaultAttribute())
            return false;
        ConstPtr<AttributeValue> currentValue = context.getCurrentAttribute(currentIndex_);
        if (currentValue.isNull())
            return false;
        Text? currentText = currentValue.pointer()?.text();
        if (currentText == null)
            return false;
        return text.fixedEqual(currentText);
    }

    // AttributeValue *checkValue(AttributeValue *, AttributeContext &) const;
    protected override AttributeValue? checkValue(AttributeValue? value, AttributeContext context)
    {
        context.noteCurrentAttribute(currentIndex_, value);
        return value;
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    protected override void buildDesc(AttributeDefinitionDesc desc)
    {
        desc.defaultValueType = AttributeDefinitionDesc.DefaultValueType.current;
        desc.currentIndex = currentIndex_;
    }

    // Boolean isCurrent() const;
    public override Boolean isCurrent()
    {
        return true;
    }

    // AttributeDefinition *copy() const;
    public override AttributeDefinition copy()
    {
        return new CurrentAttributeDefinition(name(), null, currentIndex_);
    }
}

public class ImpliedAttributeDefinition : AttributeDefinition
{
    // ImpliedAttributeDefinition(const StringC &, DeclaredValue *);
    public ImpliedAttributeDefinition(StringC name, DeclaredValue? value)
        : base(name, value)
    {
    }

    // ConstPtr<AttributeValue> makeMissingValue(AttributeContext &) const;
    public override ConstPtr<AttributeValue> makeMissingValue(AttributeContext context)
    {
        return context.makeImpliedAttributeValue();
    }

    // const AttributeValue *defaultValue(const AttributeValue *) const;
    public override AttributeValue? defaultValue(AttributeValue? impliedValue)
    {
        return impliedValue;
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    protected override void buildDesc(AttributeDefinitionDesc desc)
    {
        desc.defaultValueType = AttributeDefinitionDesc.DefaultValueType.implied;
    }

    // AttributeDefinition *copy() const;
    public override AttributeDefinition copy()
    {
        return new ImpliedAttributeDefinition(name(), null);
    }
}

public class ConrefAttributeDefinition : ImpliedAttributeDefinition
{
    // ConrefAttributeDefinition(const StringC &, DeclaredValue *);
    public ConrefAttributeDefinition(StringC name, DeclaredValue? value)
        : base(name, value)
    {
    }

    // Boolean isConref() const;
    public override Boolean isConref()
    {
        return true;
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    protected override void buildDesc(AttributeDefinitionDesc desc)
    {
        desc.defaultValueType = AttributeDefinitionDesc.DefaultValueType.conref;
    }

    // AttributeDefinition *copy() const;
    public override AttributeDefinition copy()
    {
        return new ConrefAttributeDefinition(name(), null);
    }
}

public class DefaultAttributeDefinition : AttributeDefinition
{
    protected ConstPtr<AttributeValue> value_ = new ConstPtr<AttributeValue>();

    // DefaultAttributeDefinition(const StringC &, DeclaredValue *, AttributeValue *);
    public DefaultAttributeDefinition(StringC name, DeclaredValue? declaredValue, AttributeValue? defaultValue)
        : base(name, declaredValue)
    {
        value_ = new ConstPtr<AttributeValue>(defaultValue);
    }

    // ConstPtr<AttributeValue> makeMissingValue(AttributeContext &) const;
    public override ConstPtr<AttributeValue> makeMissingValue(AttributeContext context)
    {
        if (context.mayDefaultAttribute())
            return value_;
        if (context.validate())
            context.message(ParserMessages.attributeMissing,
                            new StringMessageArg(name()));
        return new ConstPtr<AttributeValue>();
    }

    // Boolean missingValueWouldMatch(const Text &, const AttributeContext &) const;
    public override Boolean missingValueWouldMatch(Text text, AttributeContext context)
    {
        Text? valueText = value_.pointer()?.text();
        return context.mayDefaultAttribute() && valueText != null && text.fixedEqual(valueText);
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    protected override void buildDesc(AttributeDefinitionDesc desc)
    {
        desc.defaultValueType = AttributeDefinitionDesc.DefaultValueType.defaulted;
        desc.defaultValue = value_;
    }

    // AttributeDefinition *copy() const;
    public override AttributeDefinition copy()
    {
        return new DefaultAttributeDefinition(name(), null, value_.pointer());
    }

    // const AttributeValue *defaultValue(const AttributeValue *) const;
    public override AttributeValue? defaultValue(AttributeValue? impliedValue)
    {
        return value_.pointer();
    }
}

public class FixedAttributeDefinition : DefaultAttributeDefinition
{
    // FixedAttributeDefinition(const StringC &, DeclaredValue *, AttributeValue *);
    public FixedAttributeDefinition(StringC name, DeclaredValue? declaredValue, AttributeValue? defaultValue)
        : base(name, declaredValue, defaultValue)
    {
    }

    // Boolean isFixed() const;
    public override Boolean isFixed()
    {
        return true;
    }

    // AttributeValue *checkValue(AttributeValue *, AttributeContext &) const;
    protected override AttributeValue? checkValue(AttributeValue? value, AttributeContext context)
    {
        AttributeValue? fixedValue = base.defaultValue(null);
        if (value != null && fixedValue != null && context.validate())
        {
            Text? text;
            StringC? str;
            Text? fixedText;
            StringC? fixedStr;
            switch (value.info(out text, out str))
            {
                case AttributeValue.Type.implied:
                    // Should not happen
                    break;
                case AttributeValue.Type.cdata:
                    if (fixedValue.info(out fixedText, out fixedStr) == AttributeValue.Type.cdata)
                    {
                        if (text != null && fixedText != null && !text.fixedEqual(fixedText))
                            context.message(ParserMessages.notFixedValue, new StringMessageArg(name()));
                    }
                    break;
                case AttributeValue.Type.tokenized:
                    if (fixedValue.info(out fixedText, out fixedStr) == AttributeValue.Type.tokenized)
                    {
                        if (str != null && fixedStr != null && str != fixedStr)
                            context.message(ParserMessages.notFixedValue, new StringMessageArg(name()));
                    }
                    break;
            }
        }
        return value;
    }

    // void buildDesc(AttributeDefinitionDesc &) const;
    protected override void buildDesc(AttributeDefinitionDesc desc)
    {
        base.buildDesc(desc);
        desc.defaultValueType = AttributeDefinitionDesc.DefaultValueType.@fixed;
    }

    // AttributeDefinition *copy() const;
    public override AttributeDefinition copy()
    {
        return new FixedAttributeDefinition(name(), null, value_.pointer());
    }
}

public abstract class AttributeSemantics
{
    // AttributeSemantics();
    public AttributeSemantics()
    {
    }

    // virtual ~AttributeSemantics();
    // C# GC handles cleanup

    // virtual size_t nEntities() const;
    public virtual nuint nEntities()
    {
        return 0;
    }

    // virtual ConstPtr<Entity> entity(size_t) const;
    public virtual ConstPtr<Entity> entity(nuint i)
    {
        return new ConstPtr<Entity>();
    }

    // virtual ConstPtr<Notation> notation() const;
    public virtual ConstPtr<Notation> notation()
    {
        return new ConstPtr<Notation>();
    }

    // virtual AttributeSemantics *copy() const = 0;
    public abstract AttributeSemantics copy();
}

public class EntityAttributeSemantics : AttributeSemantics
{
    private Vector<ConstPtr<Entity>> entity_ = new Vector<ConstPtr<Entity>>();

    // EntityAttributeSemantics(Vector<ConstPtr<Entity> > &);
    public EntityAttributeSemantics(Vector<ConstPtr<Entity>> entity)
    {
        entity.swap(entity_);
    }

    // size_t nEntities() const;
    public override nuint nEntities()
    {
        return entity_.size();
    }

    // ConstPtr<Entity> entity(size_t) const;
    public override ConstPtr<Entity> entity(nuint i)
    {
        return entity_[i];
    }

    // AttributeSemantics *copy() const;
    public override AttributeSemantics copy()
    {
        Vector<ConstPtr<Entity>> entityCopy = new Vector<ConstPtr<Entity>>(entity_.size());
        for (nuint i = 0; i < entity_.size(); i++)
            entityCopy[i] = entity_[i];
        return new EntityAttributeSemantics(entityCopy);
    }
}

public class NotationAttributeSemantics : AttributeSemantics
{
    private ConstPtr<Notation> notation_ = new ConstPtr<Notation>();

    // NotationAttributeSemantics(const ConstPtr<Notation> &);
    public NotationAttributeSemantics(ConstPtr<Notation> notation)
    {
        notation_ = notation;
    }

    // ConstPtr<Notation> notation() const;
    public override ConstPtr<Notation> notation()
    {
        return notation_;
    }

    // AttributeSemantics *copy() const;
    public override AttributeSemantics copy()
    {
        return new NotationAttributeSemantics(notation_);
    }
}

public class ImpliedAttributeValue : AttributeValue
{
    // ImpliedAttributeValue();
    public ImpliedAttributeValue()
    {
    }

    // Type info(const Text *&, const StringC *&) const;
    public override Type info(out Text? text, out StringC? str)
    {
        text = null;
        str = null;
        return Type.implied;
    }
}

public class CdataAttributeValue : AttributeValue
{
    protected Text text_ = new Text();

    // CdataAttributeValue(Text &);
    public CdataAttributeValue(Text text)
    {
        text.swap(text_);
    }

    // Type info(const Text *&, const StringC *&) const;
    public override Type info(out Text? text, out StringC? str)
    {
        text = text_;
        str = null;
        return Type.cdata;
    }

    // const Text *text() const;
    public override Text? text()
    {
        return text_;
    }

    // Boolean recoverUnquoted(const StringC &, const Location &,
    //                         AttributeContext &, const StringC &);
    public override Boolean recoverUnquoted(StringC str, Location strLoc,
                                             AttributeContext context, StringC name)
    {
        TextIter iter = new TextIter(text_);
        TextItem.Type type;
        Char[]? s;
        nuint len;
        Location loc = new Location();
        if (iter.next(out type, out s, out len, out loc!)
            && type == TextItem.Type.data
            && len == text_.size()
            && loc.origin().pointer() == strLoc.origin().pointer()
            && loc.index() + len == strLoc.index()
            && !iter.next(out type, out s, out len, out loc!))
        {
            text_.addChars(str, strLoc);
            context.setNextLocation(strLoc);
            context.message(ParserMessages.unquotedAttributeValue);
            return true;
        }
        return false;
    }

    // virtual const Notation *notation() const;
    public virtual Notation? notationPointer()
    {
        return null;
    }
}

public class TokenizedAttributeValue : AttributeValue
{
    private Text text_ = new Text();
    // index into value of each space
    // length is number of tokens - 1
    private Vector<nuint> spaceIndex_ = new Vector<nuint>();

    // TokenizedAttributeValue(Text &, const Vector<size_t> &);
    public TokenizedAttributeValue(Text text, Vector<nuint> spaceIndex)
    {
        spaceIndex_ = spaceIndex;
        text.swap(text_);
    }

    // size_t nTokens() const;
    public nuint nTokens()
    {
        return spaceIndex_.size() + 1;
    }

    // AttributeSemantics *makeSemantics(const DeclaredValue *,
    //                                   AttributeContext &,
    //                                   const StringC &,
    //                                   unsigned &,
    //                                   unsigned &) const;
    public override AttributeSemantics? makeSemantics(DeclaredValue? value,
                                                       AttributeContext context,
                                                       StringC name,
                                                       ref uint nIdrefs,
                                                       ref uint nEntityNames)
    {
        if (text_.size() == 0)
            return null;
        return value?.makeSemantics(this, context, name, ref nIdrefs, ref nEntityNames);
    }

    // Type info(const Text *&, const StringC *&) const;
    public override Type info(out Text? text, out StringC? str)
    {
        text = null;
        str = text_.@string();
        return Type.tokenized;
    }

    // const Text *text() const;
    public override Text? text()
    {
        return text_;
    }

    // const StringC &string() const;
    public StringC @string()
    {
        return text_.@string();
    }

    // StringC token(size_t) const;
    public StringC token(nuint i)
    {
        nuint startIndex = i == 0 ? 0 : spaceIndex_[i - 1] + 1;
        nuint len = (i == spaceIndex_.size() ? text_.size() : spaceIndex_[i]) - startIndex;
        return text_.@string().substr(startIndex, len);
    }

    // void token(size_t, const Char *&, size_t &) const;
    public void token(nuint i, out Char[]? ptr, out nuint len)
    {
        nuint startIndex = i == 0 ? 0 : spaceIndex_[i - 1] + 1;
        Char[]? data = text_.@string().data();
        if (data != null)
        {
            len = (i == spaceIndex_.size() ? text_.size() : spaceIndex_[i]) - startIndex;
            ptr = new Char[len];
            for (nuint j = 0; j < len; j++)
                ptr[j] = data[startIndex + j];
        }
        else
        {
            ptr = null;
            len = 0;
        }
    }

    // Location tokenLocation(size_t) const;
    public Location tokenLocation(nuint i)
    {
        return text_.charLocation(i == 0 ? 0 : spaceIndex_[i - 1] + 1);
    }

    // Boolean tokenLocation(size_t, const ConstPtr<Origin> *&, Index &) const;
    public Boolean tokenLocation(nuint i, out ConstPtr<Origin>? origin, out Index index)
    {
        return text_.charLocation(i == 0 ? 0 : spaceIndex_[i - 1] + 1, out origin, out index);
    }

    // Boolean recoverUnquoted(const StringC &, const Location &,
    //                         AttributeContext &, const StringC &);
    public override Boolean recoverUnquoted(StringC str, Location strLoc,
                                             AttributeContext context, StringC name)
    {
        TextIter iter = new TextIter(text_);
        TextItem.Type type;
        Char[]? s;
        nuint len;
        Location loc = new Location();
        if (iter.next(out type, out s, out len, out loc!)
            && type == TextItem.Type.data
            && len == text_.size()
            && loc.origin().pointer() == strLoc.origin().pointer()
            && loc.index() + len == strLoc.index()
            && !iter.next(out type, out s, out len, out loc!))
        {
            context.setNextLocation(strLoc);
            context.message(ParserMessages.attributeValueChar,
                            new StringMessageArg(new StringC(str.data()!, 1)),
                            new StringMessageArg(name));
            return true;
        }
        return false;
    }
}

public class Attribute
{
    private nuint specIndexPlus_;
    private ConstPtr<AttributeValue> value_ = new ConstPtr<AttributeValue>();
    private AttributeSemantics? semantics_;

    // Attribute();
    public Attribute()
    {
        specIndexPlus_ = 0;
    }

    // Attribute(const Attribute&);
    public Attribute(Attribute x)
    {
        specIndexPlus_ = x.specIndexPlus_;
        value_ = x.value_;
        semantics_ = x.semantics_?.copy();
    }

    // ~Attribute();
    // C# GC handles cleanup

    // Attribute& operator=(const Attribute&);
    public void operatorAssign(Attribute x)
    {
        specIndexPlus_ = x.specIndexPlus_;
        value_ = x.value_;
        semantics_ = x.semantics_?.copy();
    }

    // Boolean specified() const;
    public Boolean specified()
    {
        return specIndexPlus_ != 0;
    }

    // size_t specIndex() const;
    public nuint specIndex()
    {
        return specIndexPlus_ - 1;
    }

    // const AttributeValue *value() const;
    public AttributeValue? value()
    {
        return value_.pointer();
    }

    // const ConstPtr<AttributeValue> &valuePointer() const;
    public ConstPtr<AttributeValue> valuePointer()
    {
        return value_;
    }

    // const AttributeSemantics *semantics() const;
    public AttributeSemantics? semantics()
    {
        return semantics_;
    }

    // void setSpec(size_t);
    public void setSpec(nuint index)
    {
        specIndexPlus_ = index + 1;
    }

    // void setValue(const ConstPtr<AttributeValue> &);
    public void setValue(ConstPtr<AttributeValue> value)
    {
        value_ = value;
    }

    public void setValue(AttributeValue? value)
    {
        value_ = new ConstPtr<AttributeValue>(value);
    }

    // void setSemantics(AttributeSemantics *);
    public void setSemantics(AttributeSemantics? semantics)
    {
        semantics_ = semantics;
    }

    // void clear();
    public void clear()
    {
        specIndexPlus_ = 0;
        value_ = new ConstPtr<AttributeValue>();
        semantics_ = null;
    }
}

public abstract class AttributeContext : Messenger
{
    protected Boolean mayDefaultAttribute_;
    protected Boolean validate_;
    private ConstPtr<AttributeValue> impliedAttributeValue_ = new ConstPtr<AttributeValue>();

    // AttributeContext();
    public AttributeContext()
    {
        mayDefaultAttribute_ = false;
        validate_ = true;
    }

    // virtual ~AttributeContext();
    // C# GC handles cleanup

    // virtual Boolean defineId(const StringC &, const Location &, Location &);
    public virtual Boolean defineId(StringC id, Location loc, ref Location prevLoc)
    {
        return true;
    }

    // virtual void noteIdref(const StringC &, const Location &);
    public virtual void noteIdref(StringC id, Location loc)
    {
    }

    // virtual void noteCurrentAttribute(size_t, AttributeValue *);
    public virtual void noteCurrentAttribute(nuint index, AttributeValue? value)
    {
    }

    // virtual ConstPtr<AttributeValue> getCurrentAttribute(size_t) const;
    public virtual ConstPtr<AttributeValue> getCurrentAttribute(nuint index)
    {
        return new ConstPtr<AttributeValue>();
    }

    // virtual ConstPtr<Entity> getAttributeEntity(const StringC &, const Location &);
    public virtual ConstPtr<Entity> getAttributeEntity(StringC name, Location loc)
    {
        return new ConstPtr<Entity>();
    }

    // virtual ConstPtr<Notation> getAttributeNotation(const StringC &, const Location &);
    public virtual ConstPtr<Notation> getAttributeNotation(StringC name, Location loc)
    {
        return new ConstPtr<Notation>();
    }

    // virtual const Syntax &attributeSyntax() const = 0;
    public abstract Syntax attributeSyntax();

    // ConstPtr<AttributeValue> makeImpliedAttributeValue();
    public ConstPtr<AttributeValue> makeImpliedAttributeValue()
    {
        if (impliedAttributeValue_.isNull())
            impliedAttributeValue_ = new ConstPtr<AttributeValue>(new ImpliedAttributeValue());
        return impliedAttributeValue_;
    }

    // Boolean mayDefaultAttribute() const;
    public Boolean mayDefaultAttribute()
    {
        return mayDefaultAttribute_;
    }

    // Boolean validate() const;
    public Boolean validate()
    {
        return validate_;
    }
}

// DataDeclaredValue and DataAttributeValue - for DATA declared value
public class DataDeclaredValue : CdataDeclaredValue
{
    private ConstPtr<Notation> notation_ = new ConstPtr<Notation>();
    private AttributeList attributes_ = new AttributeList();

    // DataDeclaredValue(const ConstPtr<Notation> &, AttributeList &);
    public DataDeclaredValue(ConstPtr<Notation> nt, AttributeList attributes)
    {
        notation_ = nt;
        attributes.swap(attributes_);
    }

    // AttributeValue *makeValue(Text &, AttributeContext &, const StringC &,
    //                           unsigned &) const;
    public override AttributeValue? makeValue(Text text, AttributeContext context,
                                               StringC name, ref uint specLength)
    {
        checkNormalizedLength(text, context, ref specLength);
        return new DataAttributeValue(text, notation_, attributes_);
    }

    // DeclaredValue *copy() const;
    public override DeclaredValue copy()
    {
        AttributeList attrCopy = new AttributeList();
        // Note: This is a shallow copy
        return new DataDeclaredValue(notation_, attrCopy);
    }
}

public class DataAttributeValue : CdataAttributeValue
{
    private ConstPtr<Notation> notation_ = new ConstPtr<Notation>();
    private AttributeList attributes_;

    // DataAttributeValue(Text &, const ConstPtr<Notation> &, const AttributeList &);
    public DataAttributeValue(Text text, ConstPtr<Notation> nt, AttributeList attributes)
        : base(text)
    {
        notation_ = nt;
        attributes_ = attributes;
    }

    // const AttributeList &attributes() const;
    public AttributeList attributes()
    {
        return attributes_;
    }

    // const Notation *notation() const;
    public override Notation? notationPointer()
    {
        return notation_.pointer();
    }
}
