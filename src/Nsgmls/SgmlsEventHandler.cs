using OpenSP;
// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace Nsgmls;

public class SgmlsSubdocState
{
    public SgmlParser? parser_;
    public StringSet definedEntities_ = new StringSet();
    public StringSet definedNotations_ = new StringSet();
    public Boolean haveLinkProcess_;
    public LinkProcess linkProcess_ = new LinkProcess();

    // SgmlsSubdocState();
    public SgmlsSubdocState()
    {
        parser_ = null;
        haveLinkProcess_ = false;
    }

    // SgmlsSubdocState(const SgmlParser *);
    public SgmlsSubdocState(SgmlParser parser)
    {
        parser_ = parser;
        haveLinkProcess_ = false;
    }

    // void init(const SgmlParser *);
    public void init(SgmlParser parser)
    {
        parser_ = parser;
        definedNotations_.clear();
        definedEntities_.clear();
        haveLinkProcess_ = false;
        linkProcess_.clear();
    }

    // void swap(SgmlsSubdocState &);
    public void swap(SgmlsSubdocState to)
    {
        {
            SgmlParser? tem = to.parser_;
            to.parser_ = parser_;
            parser_ = tem;
        }
        {
            Boolean tem = to.haveLinkProcess_;
            to.haveLinkProcess_ = haveLinkProcess_;
            haveLinkProcess_ = tem;
        }
        linkProcess_.swap(to.linkProcess_);
        definedNotations_.swap(to.definedNotations_);
        definedEntities_.swap(to.definedEntities_);
    }

    // Prevent copying
    // SgmlsSubdocState(const SgmlsSubdocState &); - undefined
    // void operator=(const SgmlsSubdocState &); - undefined
}

public class SgmlsEventHandler : ErrorCountEventHandler, IMessenger, IDisposable
{
    public const uint outputAll = 0x0FFF;
    public const uint outputLine = 0x0001;
    public const uint outputEntity = 0x0002;
    public const uint outputId = 0x0004;
    public const uint outputIncluded = 0x0008;
    public const uint outputNotationSysid = 0x0010;
    public const uint outputNonSgml = 0x0020;
    public const uint outputEmpty = 0x0040;
    public const uint outputDataAtt = 0x0080;
    public const uint outputComment = 0x0100;
    public const uint outputTagOmission = 0x0200;
    public const uint outputAttributeOmission = 0x0400;
    public const uint outputParserInformation = 0x0800;
    public const uint outputPostfix = 0x1000; // note this not in outputAll

    private const char dataCode = '-';
    private const char piCode = '?';
    private const char conformingCode = 'C';
    private const char appinfoCode = '#';
    private const char startElementCode = '(';
    private const char endElementCode = ')';
    private const char referenceEntityCode = '&';
    private const char attributeCode = 'A';
    private const char dataAttributeCode = 'D';
    private const char linkAttributeCode = 'a';
    private const char defineNotationCode = 'N';
    private const char defineExternalEntityCode = 'E';
    private const char defineInternalEntityCode = 'I';
    private const char defineSubdocEntityCode = 'S';
    private const char defineExternalTextEntityCode = 'T';
    private const char pubidCode = 'p';
    private const char sysidCode = 's';
    private const char startSubdocCode = '{';
    private const char endSubdocCode = '}';
    private const char fileCode = 'f';
    private const char locationCode = 'L';
    private const char includedElementCode = 'i';
    private const char emptyElementCode = 'e';
    private const char commentCode = '_';
    private const char omissionCode = 'o';
    private const char featuresCode = 'V';
    private const char featuresSubCodeOptions = 'o';
    private const char featuresSubCodeVersion = 'v';
    private const char featuresSubCodePackage = 'p';
    private const char featuresSubCodePossibleCodes = 'c';

    private const char space = ' ';
    private const char re = '\r';
    private const Char reChar = re;
    private const char escapePrefix = '\\';
    private const Char escapePrefixChar = escapePrefix;
    private const char sdataDelim = '|';
    private const char nonSgmlEscape = '%';
    private const char newlineEscape = 'n';
    private const char numEscape = '#';
    private const char escapeEnd = ';';

    // State from SgmlsSubdocState (composition instead of multiple inheritance)
    private SgmlsSubdocState subdocState_ = new SgmlsSubdocState();

    private Messenger? messenger_;
    private Location currentLocation_ = new Location();
    private OutputCharStream? os_;
    private Boolean inDocument_;
    private Boolean haveData_;
    private ConstPtr<Sd> sd_ = new ConstPtr<Sd>();
    private ConstPtr<Syntax> syntax_ = new ConstPtr<Syntax>();
    private StorageObjectSpec? lastSos_;
    private ulong lastLineno_;
    private Location lastLoc_ = new Location();
    private Boolean outputLine_;
    private Boolean outputIncluded_;
    private Boolean outputEntity_;
    private Boolean outputId_;
    private Boolean outputNotationSysid_;
    private Boolean outputNonSgml_;
    private Boolean outputEmpty_;
    private Boolean outputDataAtt_;
    private Boolean outputComment_;
    private Boolean outputTagOmission_;
    private Boolean outputAttributeOmission_;
    private Boolean outputParserInformation_;

    // SgmlsEventHandler(const SgmlParser *, OutputCharStream *, Messenger *, unsigned outputFlags);
    public SgmlsEventHandler(SgmlParser parser,
                             OutputCharStream os,
                             Messenger messenger,
                             uint outputFlags)
        : base()
    {
        subdocState_ = new SgmlsSubdocState(parser);
        os_ = os;
        messenger_ = messenger;
        outputLine_ = (outputFlags & outputLine) != 0;
        outputEntity_ = (outputFlags & outputEntity) != 0;
        outputId_ = (outputFlags & outputId) != 0;
        outputNotationSysid_ = (outputFlags & outputNotationSysid) != 0;
        outputIncluded_ = (outputFlags & outputIncluded) != 0;
        outputNonSgml_ = (outputFlags & outputNonSgml) != 0;
        outputEmpty_ = (outputFlags & outputEmpty) != 0;
        outputDataAtt_ = (outputFlags & outputDataAtt) != 0;
        outputComment_ = (outputFlags & outputComment) != 0;
        outputTagOmission_ = (outputFlags & outputTagOmission) != 0;
        outputAttributeOmission_ = (outputFlags & outputAttributeOmission) != 0;
        outputParserInformation_ = (outputFlags & outputParserInformation) != 0;
        haveData_ = false;
        lastSos_ = null;
        inDocument_ = false;

        os_.setEscaper(escaper);
        if (outputParserInformation_)
            features(outputFlags);
    }

    // ~SgmlsEventHandler();
    ~SgmlsEventHandler()
    {
        Dispose();
    }

    public void Dispose()
    {
        flushData();
        if (errorCount() == 0)
        {
            os().put(conformingCode);
            os().put('\n');
        }
        // Flush the output stream
        os().flush();
    }

    private void startData()
    {
        if (!haveData_)
        {
            os().put(dataCode);
            haveData_ = true;
        }
    }

    private void flushData()
    {
        if (haveData_)
        {
            os().put('\n');
            haveData_ = false;
        }
    }

    private void outputLocation(Location loc)
    {
        if (outputLine_)
            outputLocation1(loc);
    }

    private OutputCharStream os()
    {
        return os_!;
    }

    private void outputString(StringC str)
    {
        outputString(str.data(), str.size());
    }

    // void message(MessageEvent *);
    public override void message(MessageEvent? ev)
    {
        if (ev != null)
        {
            messenger_!.dispatchMessage(ev.message());
            base.message(ev);
        }
    }

    private void features(uint outputFlags)
    {
        os().put(featuresCode);
        os().put(featuresSubCodePackage);
        os().put(space);
        os().put(CmdLineApp.SP_PACKAGE);
        os().put('\n');

        os().put(featuresCode);
        os().put(featuresSubCodeVersion);
        os().put(space);
        os().put(CmdLineApp.SP_VERSION);
        os().put('\n');

        os().put(featuresCode);
        os().put(featuresSubCodePossibleCodes);
        os().put(space);
        os().put(dataCode);
        os().put(piCode);
        os().put(conformingCode);
        os().put(appinfoCode);
        os().put(startElementCode);
        os().put(endElementCode);
        os().put(referenceEntityCode);
        os().put(attributeCode);
        os().put(dataAttributeCode);
        os().put(linkAttributeCode);
        os().put(defineNotationCode);
        os().put(defineExternalEntityCode);
        os().put(defineInternalEntityCode);
        os().put(defineSubdocEntityCode);
        os().put(defineExternalTextEntityCode);
        os().put(pubidCode);
        os().put(sysidCode);
        os().put(startSubdocCode);
        os().put(endSubdocCode);
        os().put(fileCode);
        os().put(locationCode);
        os().put(includedElementCode);
        os().put(emptyElementCode);
        os().put(commentCode);
        os().put(omissionCode);
        os().put(featuresCode);
        os().put('\n');

        os().put(featuresCode);
        os().put(featuresSubCodeOptions);
        os().put(space);
        os().put("esis");
        for (nuint i = 1; NsgmlsApp.outputOptions[i].flag != 0; i++)
        {
            if (NsgmlsApp.outputOptions[i].flag == 0)
                break;
            if ((outputFlags & NsgmlsApp.outputOptions[i].flag) != 0)
            {
                os().put(space);
                os().put(NsgmlsApp.outputOptions[i].name);
            }
        }
        os().put('\n');
    }

    // void appinfo(AppinfoEvent *);
    public override void appinfo(AppinfoEvent? ev)
    {
        if (ev == null) return;
        StringC? str;
        if (ev.literal(out str) && str != null)
        {
            outputLocation(ev.location());
            flushData();
            os().put(appinfoCode);
            outputString(str);
            os().put('\n');
        }
    }

    // void endProlog(EndPrologEvent *);
    public override void endProlog(EndPrologEvent? ev)
    {
        if (ev == null) return;
        if (outputEntity_)
        {
            flushData();
            Dtd dtd = ev.dtd();
            var iter = dtd.generalEntityIter();
            for (; ; )
            {
                Ptr<Entity> entityPtr = iter.next();
                if (entityPtr.isNull())
                    break;
                defineEntity(entityPtr.pointer()!);
            }
        }
        if (outputComment_)
            inDocument_ = true;
        if (!ev.lpdPointer().isNull())
        {
            subdocState_.linkProcess_.init(ev.lpdPointer());
            subdocState_.haveLinkProcess_ = true;
            flushData();
        }
        for (nuint i = 0; i < ev.simpleLinkNames().size(); i++)
        {
            flushData();
            attributes(ev.simpleLinkAttributes()[i],
                       linkAttributeCode,
                       ev.simpleLinkNames()[i]);
        }
    }

    // void entityDefaulted(EntityDefaultedEvent *event);
    public override void entityDefaulted(EntityDefaultedEvent? ev)
    {
        if (ev == null) return;
        if (outputEntity_)
        {
            flushData();
            defineEntity(ev.entityPointer().pointer()!);
        }
    }

    // void uselink(UselinkEvent *);
    public override void uselink(UselinkEvent? ev)
    {
        if (ev == null) return;
        subdocState_.linkProcess_.uselink(ev.linkSet(),
                                          ev.restore(),
                                          ev.lpd().pointer());
    }

    // void sgmlDecl(SgmlDeclEvent *);
    public override void sgmlDecl(SgmlDeclEvent? ev)
    {
        if (ev == null) return;
        sd_ = ev.sdPointer();
        syntax_ = ev.instanceSyntaxPointer(); // FIXME which syntax?
    }

    // void data(DataEvent *);
    public override void data(DataEvent? ev)
    {
        if (ev == null) return;
        outputLocation(ev.location());
        startData();
        outputString(ev.data(), ev.dataOffset(), ev.dataLength());
    }

    // void sdataEntity(SdataEntityEvent *);
    public override void sdataEntity(SdataEntityEvent? ev)
    {
        if (ev == null) return;
        outputLocation(ev.location());
        startData();
        os().put(escapePrefix);
        os().put(sdataDelim);
        outputString(ev.data(), ev.dataOffset(), ev.dataLength());
        os().put(escapePrefix);
        os().put(sdataDelim);
    }

    // void pi(PiEvent *);
    public override void pi(PiEvent? ev)
    {
        if (ev == null) return;
        outputLocation(ev.location());
        flushData();
        os().put(piCode);
        outputString(ev.data(), ev.dataOffset(), ev.dataLength());
        os().put('\n');
    }

    // void commentDecl(CommentDeclEvent *);
    public override void commentDecl(CommentDeclEvent? ev)
    {
        if (ev == null) return;
        if (inDocument_)
        {
            // only receive this event if outputComment_ true
            outputLocation(ev.location());
            flushData();
            MarkupIter iter = new MarkupIter(ev.markup());
            for (; iter.valid(); iter.advance())
            {
                if (iter.type() == Markup.Type.comment)
                {
                    os().put(commentCode);
                    outputString(iter.charsPointer(), iter.charsLength());
                    os().put('\n');
                }
            }
        }
    }

    // void nonSgmlChar(NonSgmlCharEvent *);
    public override void nonSgmlChar(NonSgmlCharEvent? ev)
    {
        if (ev == null) return;
        if (outputNonSgml_)
        {
            outputLocation(ev.location());
            startData();
            os().put(escapePrefix);
            os().put(nonSgmlEscape);
            os().put(((ulong)ev.character()).ToString());
            os().put(escapeEnd);
        }
    }

    // void startElement(StartElementEvent *);
    public override void startElement(StartElementEvent? ev)
    {
        if (ev == null) return;
        flushData();
        currentLocation_ = ev.location();
        if (subdocState_.haveLinkProcess_)
        {
            AttributeList? linkAttributes;
            ResultElementSpec? resultElementSpec;
            // Use a wrapper to convert IMessenger to Messenger
            EventHandlerMessenger wrapper = new EventHandlerMessenger(this);
            subdocState_.linkProcess_.startElement(ev.elementType(),
                                                   ev.attributes()!,
                                                   ev.location(),
                                                   wrapper,
                                                   out linkAttributes,
                                                   out resultElementSpec);
            if (linkAttributes != null)
                attributes(linkAttributes, linkAttributeCode, subdocState_.linkProcess_.name());
        }
        attributes(ev.attributes()!, attributeCode, null);
        currentLocation_.clear();
        if (outputTagOmission_ && ev.markupPtr() == null)
        {
            os().put(omissionCode);
            os().put('\n');
        }
        if (outputIncluded_ && ev.included())
        {
            os().put(includedElementCode);
            os().put('\n');
        }
        if (outputEmpty_ && ev.mustOmitEnd())
        {
            os().put(emptyElementCode);
            os().put('\n');
        }
        outputLocation(ev.location());
        os().put(startElementCode);
        os().put(ev.name());
        os().put('\n');
    }

    private void attributes(AttributeList attributes, char code, StringC? ownerName)
    {
        nuint nAttributes = attributes.size();
        for (nuint i = 0; i < nAttributes; i++)
        {
            AttributeValue? value = attributes.value((uint)i);
            if (value != null)
            {
                if (outputAttributeOmission_)
                {
                    if (!attributes.specified((uint)i))
                    {
                        os().put(omissionCode);
                        os().put('\n');
                    }
                }
                Text? text;
                StringC? @string;
                switch (value.info(out text, out @string))
                {
                    case AttributeValue.Type.implied:
                        startAttribute(attributes.name((uint)i), code, ownerName);
                        os().put("IMPLIED");
                        os().put('\n');
                        break;
                    case AttributeValue.Type.tokenized:
                        {
                            string typeString = "TOKEN";
                            AttributeSemantics? semantics = attributes.semantics((uint)i);
                            if (semantics != null)
                            {
                                ConstPtr<Notation> notation = semantics.notation();
                                if (!notation.isNull())
                                {
                                    defineNotation(notation.pointer()!);
                                    typeString = "NOTATION";
                                }
                                else
                                {
                                    nuint nEntities = semantics.nEntities();
                                    if (nEntities > 0)
                                    {
                                        typeString = "ENTITY";
                                        if (!outputEntity_)
                                            for (nuint j = 0; j < nEntities; j++)
                                            {
                                                Entity? entity = semantics.entity(j).pointer();
                                                if (entity != null && !markEntity(entity))
                                                    defineEntity(entity);
                                            }
                                    }
                                }
                            }
                            if (outputId_ && attributes.id((uint)i))
                                typeString = "ID";
                            startAttribute(attributes.name((uint)i), code, ownerName);
                            os().put(typeString);
                            os().put(space);
                            os().put(@string!);
                            os().put('\n');
                        }
                        break;
                    case AttributeValue.Type.cdata:
                        {
                            startAttribute(attributes.name((uint)i), code, ownerName);
                            DataAttributeValue? dataValue = value as DataAttributeValue;
                            if (outputDataAtt_ && dataValue?.notationPointer() != null)
                            {
                                os().put("DATA ");
                                os().put(dataValue.notationPointer()!.name());
                                os().put(" ");
                            }
                            else
                                os().put("CDATA ");
                            TextIter iter = new TextIter(text!);
                            TextItem.Type type;
                            Char[]? p;
                            nuint length;
                            Location? loc;
                            while (iter.next(out type, out p, out length, out loc))
                                switch (type)
                                {
                                    case TextItem.Type.data:
                                    case TextItem.Type.cdata:
                                        if (p != null) outputString(p, length);
                                        break;
                                    case TextItem.Type.sdata:
                                        os().put(escapePrefix);
                                        os().put(sdataDelim);
                                        if (p != null) outputString(p, length);
                                        os().put(escapePrefix);
                                        os().put(sdataDelim);
                                        break;
                                    case TextItem.Type.nonSgml:
                                        if (outputNonSgml_ && p != null)
                                        {
                                            os().put(escapePrefix);
                                            os().put(nonSgmlEscape);
                                            os().put(((ulong)p[0]).ToString());
                                            os().put(escapeEnd);
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            os().put('\n');
                            if (outputDataAtt_ && dataValue?.notationPointer() != null)
                            {
                                defineNotation(dataValue.notationPointer()!);
                                this.attributes(dataValue.attributes(),
                                               dataAttributeCode,
                                               attributes.name((uint)i));
                            }
                        }
                        break;
                }
            }
        }
    }

    private void startAttribute(StringC name, char code, StringC? ownerName)
    {
        os().put(code);
        if (ownerName != null)
        {
            os().put(ownerName);
            os().put(space);
        }
        os().put(name);
        os().put(space);
    }

    // void endElement(EndElementEvent *);
    public override void endElement(EndElementEvent? ev)
    {
        if (ev == null) return;
        flushData();
        if (subdocState_.haveLinkProcess_)
            subdocState_.linkProcess_.endElement();
        outputLocation(ev.location());
        if (outputTagOmission_ && ev.markupPtr() == null)
        {
            os().put(omissionCode);
            os().put('\n');
        }
        os().put(endElementCode);
        os().put(ev.name());
        os().put('\n');
    }

    // void externalDataEntity(ExternalDataEntityEvent *);
    public override void externalDataEntity(ExternalDataEntityEvent? ev)
    {
        if (ev == null) return;
        currentLocation_ = ev.location();
        outputLocation(ev.location());
        flushData();
        if (!outputEntity_ && !markEntity(ev.entity()!))
            defineExternalDataEntity(ev.entity()!);
        currentLocation_.clear();
        os().put(referenceEntityCode);
        os().put(ev.entity()!.name());
        os().put('\n');
    }

    // void subdocEntity(SubdocEntityEvent *);
    public override void subdocEntity(SubdocEntityEvent? ev)
    {
        if (ev == null) return;
        currentLocation_ = ev.location();
        outputLocation(ev.location());
        flushData();
        SubdocEntity entity = ev.entity()!;
        if (!outputEntity_ && !markEntity(entity))
            defineSubdocEntity(entity);
        currentLocation_.clear();
        os().put(startSubdocCode);
        os().put(entity.name());
        os().put('\n');
        SgmlParser.Params @params = new SgmlParser.Params();
        @params.subdocInheritActiveLinkTypes = true;
        @params.subdocReferenced = true;
        @params.origin = new Ptr<InputSourceOrigin>(ev.entityOrigin().pointer());
        @params.parent = subdocState_.parser_;
        @params.sysid = entity.externalId().effectiveSystemId();
        @params.entityType = SgmlParser.Params.EntityType.subdoc;
        SgmlParser parser = new SgmlParser(@params);
        SgmlsSubdocState oldState = new SgmlsSubdocState();
        subdocState_.swap(oldState);
        subdocState_.init(parser);
        parser.parseAll(this);
        oldState.swap(subdocState_);
        os().put(endSubdocCode);
        os().put(entity.name());
        os().put('\n');
    }

    private void defineEntity(Entity entity)
    {
        InternalEntity? internalEntity = entity.asInternalEntity();
        if (internalEntity != null)
            defineInternalEntity(internalEntity);
        else
        {
            switch (entity.dataType())
            {
                case Entity.DataType.cdata:
                case Entity.DataType.sdata:
                case Entity.DataType.ndata:
                    defineExternalDataEntity(entity.asExternalDataEntity()!);
                    break;
                case Entity.DataType.subdoc:
                    defineSubdocEntity(entity.asSubdocEntity()!);
                    break;
                case Entity.DataType.sgmlText:
                    defineExternalTextEntity(entity.asExternalEntity()!);
                    break;
                default:
                    throw new InvalidOperationException("CANNOT_HAPPEN");
            }
        }
    }

    private void defineExternalDataEntity(ExternalDataEntity entity)
    {
        Notation? notation = entity.notation();
        defineNotation(notation!);
        externalId(entity.externalId());
        string typeString;
        switch (entity.dataType())
        {
            case Entity.DataType.cdata:
                typeString = "CDATA";
                break;
            case Entity.DataType.sdata:
                typeString = "SDATA";
                break;
            case Entity.DataType.ndata:
                typeString = "NDATA";
                break;
            default:
                throw new InvalidOperationException("CANNOT_HAPPEN");
        }
        os().put(defineExternalEntityCode);
        os().put(entity.name());
        os().put(space);
        os().put(typeString);
        os().put(space);
        os().put(notation!.name());
        os().put('\n');
        attributes(entity.attributes()!, dataAttributeCode, entity.name());
    }

    private void defineSubdocEntity(SubdocEntity entity)
    {
        externalId(entity.externalId());
        os().put(defineSubdocEntityCode);
        os().put(entity.name());
        os().put('\n');
    }

    private void defineExternalTextEntity(ExternalEntity entity)
    {
        externalId(entity.externalId());
        os().put(defineExternalTextEntityCode);
        os().put(entity.name());
        os().put('\n');
    }

    private void defineInternalEntity(InternalEntity entity)
    {
        os().put(defineInternalEntityCode);
        os().put(entity.name());
        os().put(space);
        string s;
        switch (entity.dataType())
        {
            case Entity.DataType.sdata:
                s = "SDATA";
                break;
            case Entity.DataType.cdata:
                s = "CDATA";
                break;
            case Entity.DataType.sgmlText:
                s = "TEXT";
                break;
            case Entity.DataType.pi:
                s = "PI";
                break;
            default:
                throw new InvalidOperationException("CANNOT_HAPPEN");
        }
        os().put(s);
        os().put(space);
        outputString(entity.@string());
        os().put('\n');
    }

    private void defineNotation(Notation notation)
    {
        if (markNotation(notation))
            return;
        externalId(notation.externalId(), outputNotationSysid_);
        os().put(defineNotationCode);
        os().put(notation.name());
        os().put('\n');
    }

    private void externalId(ExternalId id, Boolean outputFile = true)
    {
        StringC? str = id.publicIdString();
        if (str != null)
        {
            os().put(pubidCode);
            outputString(str);
            os().put('\n');
        }
        str = id.systemIdString();
        if (str != null)
        {
            os().put(sysidCode);
            outputString(str);
            os().put('\n');
        }
        if (outputFile && id.effectiveSystemId().size() > 0)
        {
            os().put(fileCode);
            outputString(id.effectiveSystemId());
            os().put('\n');
        }
    }

    private Boolean markEntity(Entity entity)
    {
        return subdocState_.definedEntities_.add(entity.name());
    }

    private Boolean markNotation(Notation notation)
    {
        return subdocState_.definedNotations_.add(notation.name());
    }

    private void outputString(Char[]? p, nuint n)
    {
        outputString(p, 0, n);
    }

    private void outputString(Char[]? p, nuint offset, nuint n)
    {
        if (p == null) return;
        for (nuint i = 0; i < n; i++)
        {
            switch (p[offset + i])
            {
                case escapePrefixChar:
                    os().put(escapePrefix);
                    os().put(escapePrefix);
                    break;
                case reChar:
                    os().put(escapePrefix);
                    os().put(newlineEscape);
                    if (outputLine_ && haveData_)
                        lastLineno_++;
                    break;
                default:
                    // FIXME not clear what to do here given possibility of wide characters
                    ulong c = p[offset + i];
                    // Note: C++ uses octal 040 which is 32. C# doesn't have octal literals.
                    if (c < 32)  // 32 = space, which should NOT be escaped
                    {
                        os().put(escapePrefix);
                        os().put('0');
                        os().put((char)('0' + ((c / 8) % 8)));
                        os().put((char)('0' + (c % 8)));
                    }
                    else
                        os().put((Char)p[offset + i]);
                    break;
            }
        }
    }

    private static void escaper(OutputCharStream s, Char c)
    {
        s.put(escapePrefix);
        s.put(numEscape);
        s.put(((ulong)c).ToString());
        s.put(escapeEnd);
    }

    private void outputLocation1(Location loc)
    {
        Origin? origin = loc.origin().pointer();
        InputSourceOrigin? inputSourceOrigin;
        ExternalInfo? info;
        Index index = loc.index();
        for (; ; )
        {
            if (origin == null)
                return;
            inputSourceOrigin = origin.asInputSourceOrigin();
            if (inputSourceOrigin != null)
            {
                info = inputSourceOrigin.externalInfo();
                if (info != null)
                    break;
            }
            Location parentLoc = origin.parent();
            index = parentLoc.index();
            origin = parentLoc.origin().pointer();
        }
        Offset off = inputSourceOrigin.startOffset(index);
        StorageObjectLocation soLoc = new StorageObjectLocation();
        if (!ExtendEntityManager.externalize(info, off, soLoc))
            return;
        if (soLoc.lineNumber == unchecked((ulong)-1))
            return;
        if (soLoc.storageObjectSpec == lastSos_)
        {
            if (soLoc.lineNumber == lastLineno_)
                return;
            flushData();
            os().put(locationCode);
            os().put(soLoc.lineNumber.ToString());
            os().put('\n');
            lastLineno_ = soLoc.lineNumber;
        }
        else
        {
            flushData();
            os().put(locationCode);
            os().put(soLoc.lineNumber.ToString());
            os().put(space);
            outputString(soLoc.actualStorageId);
            os().put('\n');
            lastLineno_ = soLoc.lineNumber;
            lastSos_ = soLoc.storageObjectSpec;
            lastLoc_ = loc;  // make sure lastSos_ doesn't get freed
        }
    }

    // IMessenger implementation
    public void dispatchMessage(Message msg)
    {
        if (!cancelled())
        {
            noteMessage(msg);
            messenger_!.dispatchMessage(msg);
        }
    }

    public void initMessage(Message msg)
    {
        msg.loc = currentLocation_;
    }

    public void setNextLocation(Location loc)
    {
        currentLocation_ = loc;
    }
}
