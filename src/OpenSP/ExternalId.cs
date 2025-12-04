// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class PublicId
{
    public enum Type
    {
        informal,
        fpi,
        urn
    }

    public enum TextClass
    {
        CAPACITY,
        CHARSET,
        DOCUMENT,
        DTD,
        ELEMENTS,
        ENTITIES,
        LPD,
        NONSGML,
        NOTATION,
        SD,
        SHORTREF,
        SUBDOC,
        SYNTAX,
        TEXT
    }

    public enum OwnerType
    {
        ISO,
        registered,
        unregistered
    }

    private static readonly string[] textClasses = {
        "CAPACITY",
        "CHARSET",
        "DOCUMENT",
        "DTD",
        "ELEMENTS",
        "ENTITIES",
        "LPD",
        "NONSGML",
        "NOTATION",
        "SD",
        "SHORTREF",
        "SUBDOC",
        "SYNTAX",
        "TEXT",
    };

    private Type type_;
    private OwnerType ownerType_;
    private StringC owner_ = new StringC();
    private TextClass textClass_;
    private PackedBoolean unavailable_;
    private StringC description_ = new StringC();
    private StringC languageOrDesignatingSequence_ = new StringC();
    private PackedBoolean haveDisplayVersion_;
    private StringC displayVersion_ = new StringC();
    private Text text_ = new Text();
    private StringC nid_ = new StringC();
    private StringC nss_ = new StringC();

    // PublicId();
    public PublicId()
    {
        type_ = Type.informal;
    }

    // FPI parts
    // Boolean getOwnerType(OwnerType &) const;
    public Boolean getOwnerType(out OwnerType result)
    {
        if (type_ != Type.fpi)
        {
            result = default;
            return false;
        }
        result = ownerType_;
        return true;
    }

    // Boolean getOwner(StringC &) const;
    public Boolean getOwner(StringC result)
    {
        if (type_ != Type.fpi)
            return false;
        result.operatorAssign(owner_);
        return true;
    }

    // Boolean getTextClass(TextClass &) const;
    public Boolean getTextClass(out TextClass result)
    {
        if (type_ != Type.fpi)
        {
            result = default;
            return false;
        }
        result = textClass_;
        return true;
    }

    // Boolean getUnavailable(Boolean &) const;
    public Boolean getUnavailable(out Boolean result)
    {
        if (type_ != Type.fpi)
        {
            result = false;
            return false;
        }
        result = unavailable_;
        return true;
    }

    // Boolean getDescription(StringC &) const;
    public Boolean getDescription(StringC result)
    {
        if (type_ != Type.fpi)
            return false;
        result.operatorAssign(description_);
        return true;
    }

    // Boolean getLanguage(StringC &) const;
    public Boolean getLanguage(StringC result)
    {
        if (type_ != Type.fpi || textClass_ == TextClass.CHARSET)
            return false;
        result.operatorAssign(languageOrDesignatingSequence_);
        return true;
    }

    // Boolean getDesignatingSequence(StringC &) const;
    public Boolean getDesignatingSequence(StringC result)
    {
        if (type_ != Type.fpi || textClass_ != TextClass.CHARSET)
            return false;
        result.operatorAssign(languageOrDesignatingSequence_);
        return true;
    }

    // Boolean getDisplayVersion(StringC &) const;
    public Boolean getDisplayVersion(StringC result)
    {
        if (type_ != Type.fpi)
            return false;
        if (haveDisplayVersion_)
            result.operatorAssign(displayVersion_);
        return true;
    }

    // URN parts
    // Boolean getNamespaceIdentifier(StringC &) const;
    public Boolean getNamespaceIdentifier(StringC result)
    {
        if (type_ != Type.urn)
            return false;
        result.operatorAssign(nid_);
        return true;
    }

    // Boolean getNamespaceSpecificString(StringC &) const;
    public Boolean getNamespaceSpecificString(StringC result)
    {
        if (type_ != Type.urn)
            return false;
        result.operatorAssign(nss_);
        return true;
    }

    // Type init(Text &, const CharsetInfo &, Char space, const MessageType1 *&fpierror, const MessageType1 *&urnerror);
    public Type init(Text text, CharsetInfo charset, Char space,
                     out MessageType1? fpierror, out MessageType1? urnerror)
    {
        text.swap(text_);
        StringC str = text_.@string();
        type_ = Type.informal;
        fpierror = null;
        urnerror = null;
        if (initFpi(str, charset, space, out fpierror))
            type_ = Type.fpi;
        if (initUrn(str, charset, space, out urnerror))
            type_ = Type.urn;
        return type_;
    }

    // const StringC &string() const;
    public StringC @string()
    {
        return text_.@string();
    }

    // const Text &text() const;
    public Text text()
    {
        return text_;
    }

    // const Type type() const;
    public Type type()
    {
        return type_;
    }

    // Boolean initFpi(const StringC &, const CharsetInfo &, Char, const MessageType1 *&);
    private Boolean initFpi(StringC str, CharsetInfo charset, Char space, out MessageType1? error)
    {
        error = null;
        nuint nextIdx = 0;
        Char[]? data = str.data();
        nuint lim = str.size();
        Char solidus = charset.execToDesc((sbyte)'/');
        Char minus = charset.execToDesc((sbyte)'-');
        Char plus = charset.execToDesc((sbyte)'+');
        nuint fieldStart;
        nuint fieldLength;

        if (!nextField(solidus, data, ref nextIdx, lim, out fieldStart, out fieldLength, true))
        {
            error = ParserMessages.fpiMissingField;
            return false;
        }
        if (fieldLength == 1 && data != null && (data[fieldStart] == minus || data[fieldStart] == plus))
        {
            ownerType_ = (data[fieldStart] == plus ? OwnerType.registered : OwnerType.unregistered);
            if (!nextField(solidus, data, ref nextIdx, lim, out fieldStart, out fieldLength, true))
            {
                error = ParserMessages.fpiMissingField;
                return false;
            }
        }
        else
            ownerType_ = OwnerType.ISO;

        owner_.assign(data, fieldStart, fieldLength);

        if (!nextField(solidus, data, ref nextIdx, lim, out fieldStart, out fieldLength, true))
        {
            error = ParserMessages.fpiMissingField;
            return false;
        }

        nuint i;
        for (i = 0; i < fieldLength; i++)
            if (data != null && data[fieldStart + i] == space)
                break;
        if (i >= fieldLength)
        {
            error = ParserMessages.fpiMissingTextClassSpace;
            return false;
        }

        StringC textClassString = new StringC();
        textClassString.assign(data, fieldStart, i);
        if (!lookupTextClass(textClassString, charset, out textClass_))
        {
            error = ParserMessages.fpiInvalidTextClass;
            return false;
        }

        i++; // skip the space
        fieldStart += i;
        fieldLength -= i;

        if (fieldLength == 1 && data != null && data[fieldStart] == minus)
        {
            unavailable_ = true;
            if (!nextField(solidus, data, ref nextIdx, lim, out fieldStart, out fieldLength, true))
            {
                error = ParserMessages.fpiMissingField;
                return false;
            }
        }
        else
            unavailable_ = false;

        description_.assign(data, fieldStart, fieldLength);

        if (!nextField(solidus, data, ref nextIdx, lim, out fieldStart, out fieldLength, true))
        {
            error = ParserMessages.fpiMissingField;
            return false;
        }

        if (textClass_ != TextClass.CHARSET)
        {
            for (i = 0; i < fieldLength; i++)
            {
                UnivChar c;
                if (data == null || !charset.descToUniv(data[fieldStart + i], out c)
                    || c < UnivCharsetDesc.A || c >= UnivCharsetDesc.A + 26)
                {
                    error = ParserMessages.fpiInvalidLanguage;
                    return false;
                }
            }
            // The public text language must be a name.
            // Names cannot be empty.
            if (fieldLength == 0)
            {
                error = ParserMessages.fpiInvalidLanguage;
                return false;
            }
        }
        languageOrDesignatingSequence_.assign(data, fieldStart, fieldLength);

        if (nextField(solidus, data, ref nextIdx, lim, out fieldStart, out fieldLength, true))
        {
            switch (textClass_)
            {
                case TextClass.CAPACITY:
                case TextClass.CHARSET:
                case TextClass.NOTATION:
                case TextClass.SYNTAX:
                    error = ParserMessages.fpiIllegalDisplayVersion;
                    return false;
                default:
                    break;
            }
            haveDisplayVersion_ = true;
            displayVersion_.assign(data, fieldStart, fieldLength);
        }
        else
            haveDisplayVersion_ = false;

        if (nextIdx < lim) // there's more content
        {
            error = ParserMessages.fpiExtraField;
            return false;
        }
        return true;
    }

    // Boolean initUrn(const StringC &, const CharsetInfo &, Char, const MessageType1 *&);
    private Boolean initUrn(StringC str, CharsetInfo charset, Char space, out MessageType1? error)
    {
        error = null;
        nuint nextIdx = 0;
        Char[]? data = str.data();
        nuint lim = str.size();
        Char sep = charset.execToDesc((sbyte)':');
        Char lcU = charset.execToDesc((sbyte)'u');
        Char ucU = charset.execToDesc((sbyte)'U');
        Char lcR = charset.execToDesc((sbyte)'r');
        Char ucR = charset.execToDesc((sbyte)'R');
        Char lcN = charset.execToDesc((sbyte)'n');
        Char ucN = charset.execToDesc((sbyte)'N');
        nuint fieldStart;
        nuint fieldLength;

        if (!nextField(sep, data, ref nextIdx, lim, out fieldStart, out fieldLength, false))
        {
            error = ParserMessages.urnMissingField;
            return false;
        }
        if (data == null || fieldLength != 3
            || (data[fieldStart] != lcU && data[fieldStart] != ucU)
            || (data[fieldStart + 1] != lcR && data[fieldStart + 1] != ucR)
            || (data[fieldStart + 2] != lcN && data[fieldStart + 2] != ucN))
        {
            error = ParserMessages.urnMissingPrefix;
            return false;
        }

        if (!nextField(sep, data, ref nextIdx, lim, out fieldStart, out fieldLength, false))
        {
            error = ParserMessages.urnMissingField;
            return false;
        }

        if (fieldLength < 1)
        {
            error = ParserMessages.urnInvalidNid;
            return false;
        }

        UnivChar percent, lparen, rparen, plus, comma, minus, period, colon;
        UnivChar equal, at, semicolon, dollar, uline, bang, star, apos;
        charset.descToUniv(charset.execToDesc((sbyte)'%'), out percent);
        charset.descToUniv(charset.execToDesc((sbyte)'('), out lparen);
        charset.descToUniv(charset.execToDesc((sbyte)')'), out rparen);
        charset.descToUniv(charset.execToDesc((sbyte)'+'), out plus);
        charset.descToUniv(charset.execToDesc((sbyte)','), out comma);
        charset.descToUniv(charset.execToDesc((sbyte)'-'), out minus);
        charset.descToUniv(charset.execToDesc((sbyte)'.'), out period);
        charset.descToUniv(charset.execToDesc((sbyte)':'), out colon);
        charset.descToUniv(charset.execToDesc((sbyte)'='), out equal);
        charset.descToUniv(charset.execToDesc((sbyte)'@'), out at);
        charset.descToUniv(charset.execToDesc((sbyte)';'), out semicolon);
        charset.descToUniv(charset.execToDesc((sbyte)'$'), out dollar);
        charset.descToUniv(charset.execToDesc((sbyte)'_'), out uline);
        charset.descToUniv(charset.execToDesc((sbyte)'!'), out bang);
        charset.descToUniv(charset.execToDesc((sbyte)'*'), out star);
        charset.descToUniv(charset.execToDesc((sbyte)'\''), out apos);

        for (nuint i = 0; i < fieldLength; i++)
        {
            UnivChar c;
            if (data == null || !charset.descToUniv(data[fieldStart + i], out c)
                || ((c != minus || i == 0)
                    && !(c >= UnivCharsetDesc.a && c < UnivCharsetDesc.a + 26)
                    && !(c >= UnivCharsetDesc.A && c < UnivCharsetDesc.A + 26)
                    && !(c >= UnivCharsetDesc.zero && c < UnivCharsetDesc.zero + 10)))
            {
                error = ParserMessages.urnInvalidNid;
                return false;
            }
        }

        nid_.assign(data, fieldStart, fieldLength);

        if (nextIdx == 0)
        {
            error = ParserMessages.urnMissingField;
            return false;
        }

        fieldStart = nextIdx;
        fieldLength = lim - nextIdx;

        if (fieldLength < 1)
        {
            error = ParserMessages.urnInvalidNss;
            return false;
        }

        for (nuint i = 0; i < fieldLength; i++)
        {
            UnivChar c;
            if (data == null || !charset.descToUniv(data[fieldStart + i], out c))
            {
                error = ParserMessages.urnInvalidNss;
                return false;
            }
            if (c == percent)
            {
                if (fieldLength - i < 2)
                {
                    error = ParserMessages.urnInvalidNss;
                    return false;
                }
                Boolean zeros = true;
                for (nuint j = 0; j < 2; j++)
                {
                    i++;
                    if (!charset.descToUniv(data[fieldStart + i], out c)
                        || (!(c >= UnivCharsetDesc.a && c < UnivCharsetDesc.a + 6)
                            && !(c >= UnivCharsetDesc.A && c < UnivCharsetDesc.A + 6)
                            && !(c >= UnivCharsetDesc.zero && c < UnivCharsetDesc.zero + 10)))
                    {
                        error = ParserMessages.urnInvalidNss;
                        return false;
                    }
                    if (c != UnivCharsetDesc.zero)
                        zeros = false;
                }
                if (zeros)
                {
                    error = ParserMessages.urnInvalidNss;
                    return false;
                }
            }
            else
            {
                if (!(c >= UnivCharsetDesc.a && c < UnivCharsetDesc.a + 26)
                    && !(c >= UnivCharsetDesc.A && c < UnivCharsetDesc.A + 26)
                    && !(c >= UnivCharsetDesc.zero && c < UnivCharsetDesc.zero + 10)
                    && c != lparen && c != rparen && c != plus && c != comma
                    && c != minus && c != period && c != colon && c != equal
                    && c != at && c != semicolon && c != dollar && c != uline
                    && c != bang && c != star && c != apos)
                {
                    error = ParserMessages.urnInvalidNss;
                    return false;
                }
            }
        }
        nss_.assign(data, fieldStart, fieldLength);
        return true;
    }

    // static Boolean nextField(Char solidus, const Char *&next, const Char *lim, const Char *&fieldStart, size_t &fieldLength, Boolean dup = 1);
    private static Boolean nextField(Char solidus, Char[]? data, ref nuint nextIdx, nuint lim,
                                     out nuint fieldStart, out nuint fieldLength, Boolean dup)
    {
        fieldStart = 0;
        fieldLength = 0;

        if (data == null)
            return false;

        // nextIdx >= lim means we've processed all content
        if (nextIdx >= lim)
            return false;

        fieldStart = nextIdx;
        nuint dupVal = dup ? (nuint)1 : (nuint)0;
        for (nuint i = nextIdx; i < lim; i++)
        {
            if (data[i] == solidus && i + dupVal < lim && data[i + dupVal] == solidus)
            {
                fieldLength = i - fieldStart;
                nextIdx = i + 1 + dupVal;
                return true;
            }
        }
        // No more separators - return the rest as the last field
        fieldLength = lim - fieldStart;
        nextIdx = lim;  // Mark that we've reached the end
        return true;
    }

    // static Boolean lookupTextClass(const StringC &, const CharsetInfo &, TextClass &);
    private static Boolean lookupTextClass(StringC str, CharsetInfo charset, out TextClass textClass)
    {
        for (int i = 0; i < textClasses.Length; i++)
        {
            if (str.operatorEqual(charset.execToDesc(textClasses[i])))
            {
                textClass = (TextClass)i;
                return true;
            }
        }
        textClass = default;
        return false;
    }
}

public class ExternalId
{
    private PackedBoolean haveSystem_;
    private PackedBoolean havePublic_;
    private Text system_ = new Text();
    private PublicId public_ = new PublicId();
    private Location loc_ = new Location();
    private StringC effectiveSystem_ = new StringC();

    // ExternalId();
    public ExternalId()
    {
        haveSystem_ = false;
        havePublic_ = false;
    }

    // const StringC *systemIdString() const;
    public StringC? systemIdString()
    {
        return haveSystem_ ? system_.@string() : null;
    }

    // const StringC *publicIdString() const;
    public StringC? publicIdString()
    {
        return havePublic_ ? public_.@string() : null;
    }

    // const StringC &effectiveSystemId() const;
    public StringC effectiveSystemId()
    {
        return effectiveSystem_;
    }

    // const Text *systemIdText() const;
    public Text? systemIdText()
    {
        return haveSystem_ ? system_ : null;
    }

    // const Text *publicIdText() const;
    public Text? publicIdText()
    {
        return havePublic_ ? public_.text() : null;
    }

    // const PublicId *publicId() const;
    public PublicId? publicId()
    {
        return havePublic_ ? public_ : null;
    }

    // void setSystem(Text &);
    public void setSystem(Text text)
    {
        text.swap(system_);
        haveSystem_ = true;
    }

    // void setEffectiveSystem(StringC &);
    public void setEffectiveSystem(StringC str)
    {
        str.swap(effectiveSystem_);
    }

    // PublicId::Type setPublic(Text &, const CharsetInfo &, Char space, const MessageType1 *&fpierror, const MessageType1 *&urnerror);
    public PublicId.Type setPublic(Text text, CharsetInfo charset, Char space,
                                   out MessageType1? fpierror, out MessageType1? urnerror)
    {
        havePublic_ = true;
        return public_.init(text, charset, space, out fpierror, out urnerror);
    }

    // void setLocation(const Location &);
    public void setLocation(Location loc)
    {
        loc_ = new Location(loc);
    }

    // const Location &location() const;
    public Location location()
    {
        return loc_;
    }
}
