using OpenSP;
// Copyright (c) 1994, 1995 James Clark
// See the file COPYING for copying permission.

// This is based on ISO/IEC 13673, Intermediate Editor's Draft, 1994/8/29,
// together with editing instructions in ISO/IEC JTC1/SC18/WG8 N1777.

namespace Nsgmls;

public class RastPrintable
{
    private byte[] v_ = new byte[256];

    // RastPrintable();
    public RastPrintable()
    {
        string s = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        for (int i = 0; i < v_.Length; i++)
            v_[i] = 0;
        for (int i = 0; i < s.Length; i++)
            v_[(byte)s[i]] = (byte)(32 + i);
    }

    // int operator()(Char c) const;
    public int Invoke(Char c)
    {
        return c <= 255 ? v_[c] : 0;
    }
}

public class RastLinkProcess : LinkProcess
{
    private RastEventHandler? rast_;

    // RastLinkProcess();
    public RastLinkProcess()
        : base()
    {
        rast_ = null;
    }

    // void setHandler(RastEventHandler *);
    public void setHandler(RastEventHandler rast)
    {
        rast_ = rast;
    }

    // Boolean selectLinkRule(const Vector<const AttributeList *> &, const Location &, size_t &);
    public override Boolean selectLinkRule(Vector<AttributeList?> linkAttributes,
                                           Location location,
                                           out nuint selected)
    {
        selected = 0;
        if (!rast_!.linkRuleQueue_.empty())
        {
            LinkRulePi? p = rast_!.linkRuleQueue_.get();
            if (p != null)
            {
                nuint sel;
                if (!selectLinkRulePi(p.pi, p.loc, linkAttributes, out sel))
                    selected = 0;
                else
                    selected = sel;
            }
        }
        else
        {
            if (linkAttributes.size() > 0)
            {
                rast_!.setNextLocation(location);
                rast_!.messengerMessage(RastEventHandlerMessages.multipleLinkRules);
            }
        }
        return true;
    }

    // Return zero for failure (RAST-PI-ERROR).
    private Boolean selectLinkRulePi(StringC value,
                                     Location loc,
                                     Vector<AttributeList?> linkAttributes,
                                     out nuint selected)
    {
        selected = 0;
        Boolean haveSelection = false;
        for (nuint i = 0; i < linkAttributes.size(); i++)
        {
            AttributeList a = linkAttributes[i]!;
            Boolean matchValue = false;
            for (nuint j = 0; j < a.size(); j++)
            {
                Text? textp;
                StringC? strp;
                switch (a.value((uint)j)!.info(out textp, out strp))
                {
                    case AttributeValue.Type.cdata:
                        // What if it contains SDATA entities?
                        if (textp!.@string().Equals(value))
                            matchValue = true;
                        break;
                    case AttributeValue.Type.tokenized:
                        if (strp!.Equals(value))
                            matchValue = true;
                        break;
                    default:
                        break;
                }
                if (matchValue)
                    break;
            }
            if (matchValue)
            {
                if (haveSelection)
                {
                    rast_!.setNextLocation(loc);
                    rast_!.messengerMessage(RastEventHandlerMessages.multipleLinkRuleMatch);
                    return false;
                }
                haveSelection = true;
                selected = i;
            }
        }
        if (!haveSelection)
        {
            rast_!.setNextLocation(loc);
            rast_!.messengerMessage(RastEventHandlerMessages.noLinkRuleMatch);
            return false;
        }
        return true;
    }

    // void swap(RastLinkProcess &);
    public void swap(RastLinkProcess to)
    {
        base.swap(to);
        RastEventHandler? tem = to.rast_;
        to.rast_ = rast_;
        rast_ = tem;
    }
}

public class LinkRulePi : Link
{
    public StringC pi = new StringC();
    public Location loc = new Location();

    // inline virtual ~LinkRulePi() {}
    // C# handles via GC
}

public class RastSubdocState
{
    public SgmlParser? parser_;
    public Boolean hadActiveLpdOrDtd_;
    public Location activeLpdOrDtdLocation_ = new Location();
    public Vector<StringC> activeLinkTypes_ = new Vector<StringC>();
    public Boolean hadDocumentElement_;
    public RastLinkProcess linkProcess_ = new RastLinkProcess();
    public Boolean haveLinkProcess_;
    public Owner<EndPrologEvent> endPrologEvent_ = new Owner<EndPrologEvent>();
    public Vector<Boolean> parseSubdocQueue_ = new Vector<Boolean>();
    public IQueue<LinkRulePi> linkRuleQueue_ = new IQueue<LinkRulePi>();

    public enum AttributeType
    {
        dtdAttribute,
        linkAttribute,
        resultAttribute,
        simpleAttribute
    }
    public const int nAttributeType = (int)AttributeType.simpleAttribute + 1;
    public Vector<Vector<nuint>>[] attributeSortOrder_ = new Vector<Vector<nuint>>[nAttributeType];

    // RastSubdocState();
    public RastSubdocState()
    {
        for (int i = 0; i < nAttributeType; i++)
            attributeSortOrder_[i] = new Vector<Vector<nuint>>();
        init(null, null);
    }

    // RastSubdocState(SgmlParser *, RastEventHandler *);
    public RastSubdocState(SgmlParser parser, RastEventHandler rast)
    {
        for (int i = 0; i < nAttributeType; i++)
            attributeSortOrder_[i] = new Vector<Vector<nuint>>();
        init(parser, rast);
    }

    // void init(SgmlParser *, RastEventHandler *);
    public void init(SgmlParser? parser, RastEventHandler? rast)
    {
        parser_ = parser;
        hadActiveLpdOrDtd_ = false;
        activeLinkTypes_.clear();
        hadDocumentElement_ = false;
        linkProcess_.clear();
        if (rast != null)
            linkProcess_.setHandler(rast);
        haveLinkProcess_ = false;
        endPrologEvent_.clear();
        parseSubdocQueue_.clear();
        linkRuleQueue_.clear();
        for (int i = 0; i < nAttributeType; i++)
            attributeSortOrder_[i].clear();
    }

    // void swap(RastSubdocState &);
    public void swap(RastSubdocState to)
    {
        {
            SgmlParser? tem = to.parser_;
            to.parser_ = parser_;
            parser_ = tem;
        }
        {
            Boolean tem = to.hadActiveLpdOrDtd_;
            to.hadActiveLpdOrDtd_ = hadActiveLpdOrDtd_;
            hadActiveLpdOrDtd_ = tem;
        }
        {
            Boolean tem = to.hadDocumentElement_;
            to.hadDocumentElement_ = hadDocumentElement_;
            hadDocumentElement_ = tem;
        }
        activeLpdOrDtdLocation_.swap(to.activeLpdOrDtdLocation_);
        activeLinkTypes_.swap(to.activeLinkTypes_);
        linkProcess_.swap(to.linkProcess_);
        endPrologEvent_.swap(to.endPrologEvent_);
        parseSubdocQueue_.swap(to.parseSubdocQueue_);
        linkRuleQueue_.swap(to.linkRuleQueue_);
        for (int i = 0; i < nAttributeType; i++)
            attributeSortOrder_[i].swap(to.attributeSortOrder_[i]);
    }

    // Prevent copying
    // RastSubdocState(const RastSubdocState &); - undefined
    // void operator=(const RastSubdocState &); - undefined
}

public class RastEventHandler : ErrorCountEventHandler, IMessenger
{
    private enum LineType { dataLine = '|', markupLine = '!' }
    private const int maxLineLength = 60;
    private const Char RS = '\n';
    private const Char RE = '\r';
    private const Char TAB = '\t';

    private static RastPrintable printable = new RastPrintable();

    private int lineLength_;
    private OutputCharStream? os_;
    private StringC rastParseSubdocYesString_ = new StringC();
    private StringC rastParseSubdocNoString_ = new StringC();
    private StringC rastActiveLpdString_ = new StringC();
    private StringC rastLinkRuleString_ = new StringC();
    private uint piErrorCount_;
    private Messenger? mgr_;

    // State from RastSubdocState (composition instead of multiple inheritance)
    private RastSubdocState subdocState_;

    // Expose linkRuleQueue_ for RastLinkProcess
    internal IQueue<LinkRulePi> linkRuleQueue_ => subdocState_.linkRuleQueue_;

    // RastEventHandler(SgmlParser *, Messenger *);
    public RastEventHandler(SgmlParser parser, Messenger mgr)
        : base()
    {
        lineLength_ = 0;
        os_ = null;
        piErrorCount_ = 0;
        subdocState_ = new RastSubdocState(parser, this);
        mgr_ = mgr;
        subdocState_.init(parser, this);
    }

    // void end();
    public void end()
    {
        if (errorCount() != 0)
        {
            truncateOutput();
            os().put(piErrorCount_ != 0 ? "#RAST-PI-ERROR" : "#ERROR");
            os().put('\n');
        }
    }

    // virtual void truncateOutput();
    public virtual void truncateOutput()
    {
        // This must be handled by derived classes to get conforming output.
    }

    // void setOutputStream(OutputCharStream *os);
    public void setOutputStream(OutputCharStream os)
    {
        os_ = os;
    }

    private OutputCharStream os()
    {
        return os_!;
    }

    private void flushLine(LineType type)
    {
        if (lineLength_ > 0)
        {
            os().put((char)type);
            os().put('\n');
            lineLength_ = 0;
        }
    }

    // void sgmlDecl(SgmlDeclEvent *);
    public override void sgmlDecl(SgmlDeclEvent? ev)
    {
        if (ev == null) return;
        rastParseSubdocYesString_ = ev.sd().execToInternal("rast-parse-subdoc:yes");
        rastParseSubdocNoString_ = ev.sd().execToInternal("rast-parse-subdoc:no");
        rastActiveLpdString_ = ev.sd().execToInternal("rast-active-lpd:");
        rastLinkRuleString_ = ev.sd().execToInternal("rast-link-rule:");
    }

    // void startElement(StartElementEvent *);
    public override void startElement(StartElementEvent? ev)
    {
        if (ev == null) return;
        flushLine(LineType.dataLine);
        if (!subdocState_.hadDocumentElement_)
        {
            if (subdocState_.activeLinkTypes_.size() > 0)
            {
                activeLinks();
                simpleLinkInfo();
            }
            subdocState_.hadDocumentElement_ = true;
        }
        os().put('[');
        os().put(ev.name());
        Boolean hadNewline;
        if (ev.attributes()!.size() > 0)
        {
            hadNewline = true;
            os().put('\n');
            attributeInfo(ev.attributes()!, RastSubdocState.AttributeType.dtdAttribute);
        }
        else
            hadNewline = false;
        if (subdocState_.haveLinkProcess_)
        {
            AttributeList? linkAttributes;
            ResultElementSpec? resultElementSpec;
            EventHandlerMessenger messenger = new EventHandlerMessenger(this);
            subdocState_.linkProcess_.startElement(ev.elementType(),
                                                   ev.attributes()!,
                                                   ev.location(),
                                                   messenger,
                                                   out linkAttributes,
                                                   out resultElementSpec);
            if (subdocState_.linkProcess_.nImpliedLinkRules() > 0)
            {
                if (!hadNewline)
                {
                    os().put('\n');
                    hadNewline = true;
                }
                os().put("#LINK-SET-INFO");
                os().put('\n');
                impliedSourceLinkRules();
            }
            if (linkAttributes != null)
            {
                if (!hadNewline)
                {
                    os().put('\n');
                    hadNewline = true;
                }
                os().put("#LINK-RULE");
                os().put('\n');
                attributeInfo(linkAttributes, RastSubdocState.AttributeType.linkAttribute);
                if (subdocState_.linkProcess_.isExplicit())
                {
                    os().put("#RESULT=");
                    if (resultElementSpec != null && resultElementSpec.elementType != null)
                    {
                        os().put(resultElementSpec.elementType.name());
                        os().put('\n');
                        attributeInfo(resultElementSpec.attributeList!, RastSubdocState.AttributeType.resultAttribute);
                    }
                    else
                    {
                        os().put("#IMPLIED");
                        os().put('\n');
                    }
                }
            }
            else
                hadNewline = false;
        }
        os().put(']');
        os().put('\n');
    }

    private void activeLinks()
    {
        for (nuint i = 0; i < subdocState_.activeLinkTypes_.size(); i++)
        {
            os().put("#ACTIVE-LINK=");
            os().put(subdocState_.activeLinkTypes_[i]);
            os().put('\n');
            Boolean found = false;
            if (subdocState_.haveLinkProcess_ &&
                subdocState_.linkProcess_.name().Equals(subdocState_.activeLinkTypes_[i]))
            {
                found = true;
                if (subdocState_.linkProcess_.nImpliedLinkRules() > 0)
                {
                    os().put("#INITIAL");
                    os().put('\n');
                    impliedSourceLinkRules();
                }
            }
            if (!found)
            {
                EndPrologEvent? epe = subdocState_.endPrologEvent_.pointer();
                if (epe != null)
                {
                    for (nuint j = 0; j < epe.simpleLinkNames().size(); j++)
                        if (epe.simpleLinkNames()[j].Equals(subdocState_.activeLinkTypes_[i]))
                        {
                            found = true;
                            break;
                        }
                }
                if (!found)
                {
                    setNextLocation(subdocState_.activeLpdOrDtdLocation_);
                    messengerMessage(RastEventHandlerMessages.invalidActiveLinkType,
                                     new StringMessageArg(subdocState_.activeLinkTypes_[i]));
                }
            }
            os().put("#END-ACTIVE-LINK");
            os().put('\n');
        }
    }

    private void simpleLinkInfo()
    {
        EndPrologEvent? epe = subdocState_.endPrologEvent_.pointer();
        if (epe == null)
            return;
        for (nuint i = 0; i < subdocState_.activeLinkTypes_.size(); i++)
        {
            for (nuint j = 0; j < epe.simpleLinkNames().size(); j++)
            {
                StringC name = epe.simpleLinkNames()[j];
                if (name.Equals(subdocState_.activeLinkTypes_[i]))
                {
                    os().put("#SIMPLE-LINK=");
                    os().put(name);
                    os().put('\n');
                    if (epe.simpleLinkAttributes()[j].size() > 0)
                        attributeInfo(epe.simpleLinkAttributes()[j],
                                      RastSubdocState.AttributeType.simpleAttribute);
                    os().put("#END-SIMPLE-LINK");
                    os().put('\n');
                    break;
                }
            }
        }
    }

    private void impliedSourceLinkRules()
    {
        nuint n = subdocState_.linkProcess_.nImpliedLinkRules();
        Vector<nuint> sortOrder = new Vector<nuint>(n);
        for (nuint i = 0; i < n; i++)
            sortOrder.push_back(i);
        for (nuint i = 1; i < n; i++)
        {
            nuint tem = sortOrder[i];
            StringC name = subdocState_.linkProcess_.impliedLinkRule(tem).elementType!.name();
            nuint j;
            for (j = i; j > 0; j--)
            {
                if (lexCmp(subdocState_.linkProcess_.impliedLinkRule(sortOrder[j - 1]).elementType!.name(),
                           name) <= 0)
                    break;
                sortOrder[j] = sortOrder[j - 1];
            }
            sortOrder[j] = tem;
        }
        for (nuint i = 0; i < n; i++)
        {
            ResultElementSpec result = subdocState_.linkProcess_.impliedLinkRule(sortOrder[i]);
            os().put('[');
            os().put(result.elementType!.name());
            if (result.attributeList!.size() > 0)
            {
                os().put('\n');
                attributeInfo(result.attributeList!, RastSubdocState.AttributeType.resultAttribute);
            }
            os().put(']');
            os().put('\n');
        }
    }

    // void endElement(EndElementEvent *);
    public override void endElement(EndElementEvent? ev)
    {
        if (ev == null) return;
        if (subdocState_.haveLinkProcess_)
            subdocState_.linkProcess_.endElement();
        flushLine(LineType.dataLine);
        os().put("[/");
        os().put(ev.name());
        os().put(']');
        os().put('\n');
        if (subdocState_.haveLinkProcess_ && subdocState_.linkProcess_.nImpliedLinkRules() > 0)
        {
            os().put("#LINK-SET-INFO");
            os().put('\n');
            impliedSourceLinkRules();
            os().put("#END-LINK-SET-INFO");
            os().put('\n');
        }
    }

    // void data(DataEvent *);
    public override void data(DataEvent? ev)
    {
        if (ev == null) return;
        lines(LineType.dataLine, ev.data(), ev.dataLength());
    }

    // void pi(PiEvent *);
    public override void pi(PiEvent? ev)
    {
        if (ev == null) return;
        flushLine(LineType.dataLine);
        os().put("[?");
        nuint dataLength = ev.dataLength();
        if (dataLength > 0)
        {
            Char[] data = ev.data();
            if (dataLength >= 4
                && equal(data, 4, rastParseSubdocYesString_)
                && !interpretRastPi(data, dataLength, ev.location()))
            {
                setNextLocation(ev.location());
                messengerMessage(RastEventHandlerMessages.invalidRastPiError);
            }
            os().put('\n');
            lines(LineType.dataLine, ev.data(), dataLength);
            flushLine(LineType.dataLine);
        }
        os().put(']');
        os().put('\n');
    }

    private static Boolean equal(Char[] s1, nuint n1, StringC s2)
    {
        if (n1 != s2.size())
            return false;
        for (nuint i = 0; i < n1; i++)
            if (s1[i] != s2[i])
                return false;
        return true;
    }

    private static Boolean prefix(Char[] s1, nuint n1, StringC s2)
    {
        if (n1 < s2.size())
            return false;
        for (nuint i = 0; i < s2.size(); i++)
            if (s1[i] != s2[i])
                return false;
        return true;
    }

    private Boolean interpretRastPi(Char[] data, nuint dataLength, Location loc)
    {
        if (equal(data, dataLength, rastParseSubdocNoString_))
        {
            queueParseSubdoc(false);
            return true;
        }
        if (equal(data, dataLength, rastParseSubdocYesString_))
        {
            queueParseSubdoc(true);
            return true;
        }
        if (prefix(data, dataLength, rastActiveLpdString_))
        {
            if (subdocState_.hadActiveLpdOrDtd_)
                return true;
            subdocState_.hadActiveLpdOrDtd_ = true;
            subdocState_.activeLpdOrDtdLocation_ = loc;
            nuint offset = rastActiveLpdString_.size();
            nuint n = dataLength - offset;
            StringC name = new StringC();
            for (nuint i = 0; ; i++)
            {
                if (i >= n || data[offset + i] == ',')
                {
                    if (name.size() == 0)
                        return false;
                    for (nuint k = 0; k < subdocState_.activeLinkTypes_.size(); k++)
                        if (name.Equals(subdocState_.activeLinkTypes_[k]))
                        {
                            setNextLocation(subdocState_.activeLpdOrDtdLocation_);
                            messengerMessage(RastEventHandlerMessages.duplicateActiveLinkType,
                                             new StringMessageArg(name));
                        }
                    subdocState_.activeLinkTypes_.push_back(new StringC(name));
                    name.clear();
                    if (i >= n)
                        break;
                }
                else
                    name.operatorPlusAssign(data[offset + i]);
            }
            for (nuint i = 0; i < subdocState_.activeLinkTypes_.size(); i++)
                subdocState_.parser_!.activateLinkType(subdocState_.activeLinkTypes_[i]);
            return true;
        }
        if (prefix(data, dataLength, rastLinkRuleString_))
        {
            LinkRulePi p = new LinkRulePi();
            p.pi.assign(data, rastLinkRuleString_.size(), dataLength - rastLinkRuleString_.size());
            p.loc = loc;
            subdocState_.linkRuleQueue_.append(p);
            return true;
        }
        return false;
    }

    // void sdataEntity(SdataEntityEvent *);
    public override void sdataEntity(SdataEntityEvent? ev)
    {
        if (ev == null) return;
        flushLine(LineType.dataLine);
        os().put("#SDATA-TEXT");
        os().put('\n');
        lines(LineType.markupLine, ev.data(), ev.dataLength());
        flushLine(LineType.markupLine);
        os().put("#END-SDATA");
        os().put('\n');
    }

    // void externalDataEntity(ExternalDataEntityEvent *);
    public override void externalDataEntity(ExternalDataEntityEvent? ev)
    {
        if (ev == null) return;
        ExternalDataEntity? entity = ev.entity();
        if (entity == null)
            return;
        flushLine(LineType.dataLine);
        os().put("[&");
        os().put(entity.name());
        os().put('\n');
        externalEntityInfo(entity, RastSubdocState.AttributeType.dtdAttribute);
        os().put(']');
        os().put('\n');
    }

    private void externalEntityInfo(ExternalDataEntity entity, RastSubdocState.AttributeType attributeType)
    {
        char c;
        switch (entity.dataType())
        {
            case Entity.DataType.cdata:
                c = 'C';
                break;
            case Entity.DataType.sdata:
                c = 'S';
                break;
            case Entity.DataType.ndata:
                c = 'N';
                break;
            default:
                return;
        }
        os().put('#');
        os().put(c);
        os().put("DATA-EXTERNAL");
        os().put('\n');
        externalIdInfo(entity.externalId());
        os().put("#NOTATION=");
        os().put(entity.notation()!.name());
        os().put('\n');
        externalIdInfo(entity.notation()!.externalId());
        attributeInfo(entity.attributes()!,
                      attributeType == RastSubdocState.AttributeType.resultAttribute
                          ? RastSubdocState.AttributeType.resultAttribute
                          : RastSubdocState.AttributeType.dtdAttribute);
    }

    // void subdocEntity(SubdocEntityEvent *);
    public override void subdocEntity(SubdocEntityEvent? ev)
    {
        if (ev == null) return;
        SubdocEntity? entity = ev.entity();
        if (entity == null)
            return;
        flushLine(LineType.dataLine);
        os().put("[&");
        os().put(entity.name());
        os().put('\n');
        Ptr<InputSourceOrigin> origin = new Ptr<InputSourceOrigin>(ev.entityOrigin().pointer());
        subdocEntityInfo(entity, origin, true);
        os().put(']');
        os().put('\n');
    }

    private void subdocEntityInfo(SubdocEntity entity,
                                  Ptr<InputSourceOrigin> entityOrigin,
                                  Boolean referenced)
    {
        os().put("#SUBDOC");
        os().put('\n');
        externalIdInfo(entity.externalId());
        if (parseNextSubdoc())
        {
            // FIXME subdocuments in entity attributes shouldn't count against
            // SUBDOC quantity limit.
            os().put("#PARSED-SUBDOCUMENT");
            os().put('\n');
            SgmlParser.Params @params = new SgmlParser.Params();
            @params.entityType = SgmlParser.Params.EntityType.subdoc;
            @params.subdocInheritActiveLinkTypes = false;
            @params.subdocReferenced = referenced;
            @params.parent = subdocState_.parser_;
            @params.sysid = entity.externalId().effectiveSystemId();
            @params.origin = entityOrigin;
            SgmlParser parser = new SgmlParser(@params);
            RastSubdocState oldSubdocState = new RastSubdocState();
            subdocState_.swap(oldSubdocState);
            subdocState_.init(parser, this);
            parser.parseAll(this);
            oldSubdocState.swap(subdocState_);
        }
    }

    private void queueParseSubdoc(Boolean parseSubdoc)
    {
        subdocState_.parseSubdocQueue_.push_back(parseSubdoc);
    }

    private Boolean parseNextSubdoc()
    {
        if (subdocState_.parseSubdocQueue_.size() == 0)
            return false;
        Boolean result = subdocState_.parseSubdocQueue_[0];
        if (subdocState_.parseSubdocQueue_.size() > 1)
        {
            for (nuint i = 1; i < subdocState_.parseSubdocQueue_.size(); i++)
                subdocState_.parseSubdocQueue_[i - 1] = subdocState_.parseSubdocQueue_[i];
        }
        subdocState_.parseSubdocQueue_.resize(subdocState_.parseSubdocQueue_.size() - 1);
        return result;
    }

    private void externalIdInfo(ExternalId id)
    {
        StringC? systemId = id.systemIdString();
        StringC? publicId = id.publicIdString();
        if (publicId != null)
        {
            os().put("#PUBLIC");
            os().put('\n');
            if (publicId.size() == 0)
            {
                os().put("#EMPTY");
                os().put('\n');
            }
            else
            {
                lines(LineType.markupLine, publicId.data(), publicId.size());
                flushLine(LineType.markupLine);
            }
        }
        if (systemId != null || publicId == null)
        {
            os().put("#SYSTEM");
            os().put('\n');
            if (systemId == null)
            {
                os().put("#NONE");
                os().put('\n');
            }
            else if (systemId.size() == 0)
            {
                os().put("#EMPTY");
                os().put('\n');
            }
            else
            {
                lines(LineType.markupLine, systemId.data(), systemId.size());
                flushLine(LineType.markupLine);
            }
        }
    }

    // void nonSgmlChar(NonSgmlCharEvent *);
    public override void nonSgmlChar(NonSgmlCharEvent? ev)
    {
        if (ev == null) return;
        flushLine(LineType.dataLine);
        os().put('#');
        os().put(((ulong)ev.character()).ToString());
        os().put('\n');
    }

    private void lines(LineType type, Char[] p, nuint length)
    {
        nuint i = 0;
        while (i < length)
        {
            if (printable.Invoke(p[i]) != 0)
            {
                int lim;
                switch (lineLength_)
                {
                    case maxLineLength:
                        os().put((char)type);
                        os().put('\n');
                        lineLength_ = 0;
                        lim = maxLineLength;
                        os().put((char)type);
                        break;
                    case 0:
                        os().put((char)type);
                        lim = maxLineLength;
                        break;
                    default:
                        lim = maxLineLength - lineLength_;
                        break;
                }
                if ((nuint)lim > length - i)
                    lim = (int)(length - i);
                int n = lim;
                for (; ; )
                {
                    os().put(p[i]);
                    i++;
                    if (--n == 0)
                        break;
                    if (printable.Invoke(p[i]) == 0)
                    {
                        lim -= n;
                        break;
                    }
                }
                lineLength_ += lim;
            }
            else
            {
                // *p is an unprintable character print it
                flushLine(type);
                switch (p[i])
                {
                    case RS:
                        os().put("#RS");
                        os().put('\n');
                        break;
                    case RE:
                        os().put("#RE");
                        os().put('\n');
                        break;
                    case TAB:
                        os().put("#TAB");
                        os().put('\n');
                        break;
                    default:
                        os().put('#');
                        os().put(((ulong)p[i]).ToString());
                        os().put('\n');
                        break;
                }
                i++;
            }
        }
    }

    private int lexCmp(StringC s1, StringC s2)
    {
        Char[] p1 = s1.data();
        nuint n1 = s1.size();
        Char[] p2 = s2.data();
        nuint n2 = s2.size();
        nuint i1 = 0, i2 = 0;
        for (; ; )
        {
            if (i1 >= n1)
                return i2 >= n2 ? 0 : -1;
            if (i2 >= n2)
                return 1;
            if (p1[i1] != p2[i2])
            {
                // printable characters precede non-printable characters;
                // printable characters are in ASCII order
                // non-printable characters are in document character set order
                int a1 = printable.Invoke(p1[i1]);
                int a2 = printable.Invoke(p2[i2]);
                if (a1 == 0)
                {
                    if (a2 == 0)
                        return p1[i1] < p2[i2] ? -1 : 1;
                    else
                        return 1;
                }
                else if (a2 == 0)
                    return -1;
                else
                    return a1 - a2;
            }
            i1++;
            i2++;
        }
    }

    private void attributeInfo(AttributeList attributes, RastSubdocState.AttributeType attributeType)
    {
        nuint length = attributes.size();
        if (length == 0)
            return;
        nuint defIndex = attributes.defIndex();
        if (defIndex >= subdocState_.attributeSortOrder_[(int)attributeType].size())
            subdocState_.attributeSortOrder_[(int)attributeType].resize(defIndex + 1);
        Vector<nuint> sortOrder = subdocState_.attributeSortOrder_[(int)attributeType][defIndex];
        if (sortOrder.size() != length
            || attributeType == RastSubdocState.AttributeType.simpleAttribute)
        {
            sortOrder.resize(length);
            for (nuint i = 0; i < length; i++)
                sortOrder[i] = i;
            // insertion sort
            for (nuint i = 1; i < length; i++)
            {
                nuint tem = sortOrder[i];
                nuint j;
                for (j = i; j > 0; j--)
                {
                    if (lexCmp(attributes.name((uint)sortOrder[j - 1]),
                               attributes.name((uint)tem)) <= 0)
                        break;
                    sortOrder[j] = sortOrder[j - 1];
                }
                sortOrder[j] = tem;
            }
            subdocState_.attributeSortOrder_[(int)attributeType][defIndex] = sortOrder;
        }
        for (nuint jj = 0; jj < length; jj++)
        {
            // Don't use sortOrder because attributeSortOrder_ may be grown
            // because of data attributes.
            nuint ii = subdocState_.attributeSortOrder_[(int)attributeType][defIndex][jj];
            os().put(attributes.name((uint)ii));
            os().put('=');
            os().put('\n');
            AttributeValue? value = attributes.value((uint)ii);
            if (value != null)
            {
                Text? text;
                StringC? @string;
                switch (value.info(out text, out @string))
                {
                    case AttributeValue.Type.implied:
                        os().put("#IMPLIED");
                        os().put('\n');
                        break;
                    case AttributeValue.Type.tokenized:
                        lines(LineType.markupLine, @string!.data(), @string!.size());
                        flushLine(LineType.markupLine);
                        break;
                    case AttributeValue.Type.cdata:
                        {
                            TextIter iter = new TextIter(text!);
                            TextItem.Type type;
                            Char[] p;
                            nuint len;
                            Location? loc;
                            while (iter.next(out type, out p, out len, out loc))
                                switch (type)
                                {
                                    case TextItem.Type.data:
                                    case TextItem.Type.cdata:
                                        lines(LineType.markupLine, p, len);
                                        break;
                                    case TextItem.Type.sdata:
                                        flushLine(LineType.markupLine);
                                        os().put("#SDATA-TEXT");
                                        os().put('\n');
                                        lines(LineType.markupLine, p, len);
                                        flushLine(LineType.markupLine);
                                        os().put("#END-SDATA");
                                        os().put('\n');
                                        break;
                                    case TextItem.Type.nonSgml:
                                        flushLine(LineType.markupLine);
                                        os().put('#');
                                        os().put(((ulong)p[0]).ToString());
                                        os().put('\n');
                                        break;
                                    default:
                                        break;
                                }
                            flushLine(LineType.markupLine);
                        }
                        break;
                }
            }
            AttributeSemantics? semantics = attributes.semantics((uint)ii);
            if (semantics != null)
            {
                ConstPtr<Notation> notation = semantics.notation();
                if (!notation.isNull())
                    externalIdInfo(notation.pointer()!.externalId());
                nuint nEntities = semantics.nEntities();
                for (nuint i = 0; i < nEntities; i++)
                {
                    ConstPtr<Entity> entity = semantics.entity(i);
                    if (!entity.isNull())
                    {
                        ExternalDataEntity? externalDataEntity = entity.pointer()!.asExternalDataEntity();
                        if (externalDataEntity != null)
                            externalEntityInfo(externalDataEntity,
                                               attributeType == RastSubdocState.AttributeType.resultAttribute
                                                   ? RastSubdocState.AttributeType.resultAttribute
                                                   : RastSubdocState.AttributeType.dtdAttribute);
                        else
                        {
                            SubdocEntity? subdocEntity = entity.pointer()!.asSubdocEntity();
                            if (subdocEntity != null)
                            {
                                TokenizedAttributeValue? tav = value as TokenizedAttributeValue;
                                Ptr<InputSourceOrigin> entityOrigin = new Ptr<InputSourceOrigin>(
                                    EntityOrigin.make(entity, tav?.tokenLocation(i) ?? new Location()) as InputSourceOrigin);
                                subdocEntityInfo(subdocEntity, entityOrigin, false);
                            }
                            else
                            {
                                InternalEntity? internalEntity = entity.pointer()!.asInternalEntity();
                                if (internalEntity != null)
                                    internalEntityInfo(internalEntity);
                            }
                        }
                    }
                    os().put("#END-ENTITY");
                    os().put('\n');
                }
            }
        }
    }

    private void internalEntityInfo(InternalEntity entity)
    {
        os().put('#');
        os().put(entity.dataType() == Entity.DataType.cdata ? 'C' : 'S');
        os().put("DATA-INTERNAL");
        os().put('\n');
        StringC str = entity.@string();
        lines(LineType.markupLine, str.data(), str.size());
        flushLine(LineType.markupLine);
    }

    // void endProlog(EndPrologEvent *);
    public override void endProlog(EndPrologEvent? ev)
    {
        if (ev == null) return;
        if (!ev.lpdPointer().isNull())
        {
            subdocState_.linkProcess_.init(ev.lpdPointer());
            subdocState_.haveLinkProcess_ = true;
        }
        if (ev.simpleLinkNames().size() > 0)
            subdocState_.endPrologEvent_ = new Owner<EndPrologEvent>(ev);
    }

    // void uselink(UselinkEvent *);
    public override void uselink(UselinkEvent? ev)
    {
        if (ev == null) return;
        subdocState_.linkProcess_.uselink(ev.linkSet(),
                                          ev.restore(),
                                          ev.lpd().pointer());
        if (subdocState_.haveLinkProcess_ && subdocState_.linkProcess_.nImpliedLinkRules() > 0)
        {
            flushLine(LineType.dataLine);
            os().put("#LINK-SET-INFO");
            os().put('\n');
            impliedSourceLinkRules();
            os().put("#END-LINK-SET-INFO");
            os().put('\n');
        }
    }

    // IMessenger implementation
    private Location nextLocation_ = new Location();

    public void setNextLocation(Location loc)
    {
        nextLocation_ = loc;
    }

    public void initMessage(Message msg)
    {
        mgr_!.initMessage(msg);
    }

    public void dispatchMessage(Message msg)
    {
        if (msg.isError())
            piErrorCount_++;
        if (!cancelled())
        {
            noteMessage(msg);
            mgr_!.dispatchMessage(msg);
        }
    }

    public void messengerMessage(MessageType0 type)
    {
        Message msg = new Message();
        msg.type = type;
        msg.loc = nextLocation_;
        dispatchMessage(msg);
    }

    public void messengerMessage(MessageType1 type, MessageArg arg)
    {
        Message msg = new Message();
        msg.type = type;
        msg.loc = nextLocation_;
        msg.args.push_back(new CopyOwner<MessageArg>(arg.copy()));
        dispatchMessage(msg);
    }
}

// Helper class for event handler messaging
public class EventHandlerMessenger : Messenger
{
    private OpenSP.EventHandler eh_;

    public EventHandlerMessenger(OpenSP.EventHandler eh)
    {
        eh_ = eh;
    }

    public override void dispatchMessage(Message message)
    {
        eh_.message(new MessageEvent(message, true));
    }
}
