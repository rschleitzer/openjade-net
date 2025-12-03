// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class LinkProcessOpenElement : Link
{
    public LinkSet? current;
    public LinkSet? restore;
    public LinkSet? post;
    public Boolean postRestore;

    // LinkProcessOpenElement(const LinkSet *current, const SourceLinkRule &);
    public LinkProcessOpenElement(LinkSet? cur, SourceLinkRuleResource rule)
    {
        current = rule.uselink();
        if (current == null)
            current = cur;
        restore = cur;
        post = rule.postlink();
        postRestore = rule.postlinkRestore();
    }

    // Overload for IdLinkRule
    public LinkProcessOpenElement(LinkSet? cur, IdLinkRule rule)
    {
        current = rule.uselink();
        if (current == null)
            current = cur;
        restore = cur;
        post = rule.postlink();
        postRestore = rule.postlinkRestore();
    }

    // LinkProcessOpenElement(const LinkSet *current);
    public LinkProcessOpenElement(LinkSet? cur)
    {
        restore = current = cur;
        post = null;
        postRestore = false;
    }
}

public class LinkProcess
{
    private IList<LinkProcessOpenElement> open_ = new IList<LinkProcessOpenElement>();
    private ConstPtr<ComplexLpd> lpd_ = new ConstPtr<ComplexLpd>();
    private Vector<ConstPtr<Lpd>> activeLpds_ = new Vector<ConstPtr<Lpd>>();
    private Vector<AttributeList?> linkAttributes_ = new Vector<AttributeList?>();

    // LinkProcess();
    public LinkProcess()
    {
    }

    // void init(const ConstPtr<ComplexLpd> &lpd);
    public void init(ConstPtr<ComplexLpd> lpd)
    {
        lpd_ = lpd;
        open_.clear();
        open_.insert(new LinkProcessOpenElement(lpd_.pointer()!.initialLinkSet()));
    }

    // Boolean startElement(const ElementType *, const AttributeList &, const Location &,
    //                      Messenger &, const AttributeList *&, const ResultElementSpec *&);
    public Boolean startElement(ElementType? element,
                                AttributeList attributes,
                                Location location,
                                Messenger mgr,
                                out AttributeList? linkAttributes,
                                out ResultElementSpec? resultElementSpec)
    {
        if (lpd_.isNull())
        {
            linkAttributes = null;
            resultElementSpec = null;
            return true;
        }

        StringC? id = attributes.getId();
        if (id != null)
        {
            IdLinkRuleGroup? p = lpd_.pointer()!.lookupIdLink(id);
            if (p != null)
            {
                nuint selected;
                if (p.nLinkRules() > 1)
                {
                    linkAttributes_.resize(p.nLinkRules());
                    for (nuint i = 0; i < linkAttributes_.size(); i++)
                        linkAttributes_[i] = p.linkRule(i).attributes();
                    if (!selectLinkRule(linkAttributes_, location, out selected))
                    {
                        linkAttributes = null;
                        resultElementSpec = null;
                        return false;
                    }
                }
                else
                    selected = 0;

                IdLinkRule rule = p.linkRule(selected);
                open_.insert(new LinkProcessOpenElement(open_.head()!.current, rule));
                linkAttributes = rule.attributes();
                resultElementSpec = rule.resultElementSpec();

                if (!rule.isAssociatedWith(element))
                {
                    mgr.setNextLocation(location);
                    mgr.message(ParserMessages.idlinkElementType,
                                new StringMessageArg(element!.name()),
                                new StringMessageArg(id));
                }
                return true;
            }
        }

        LinkSet? currentLinkSet = open_.head()!.current;
        nuint nRules = currentLinkSet!.nLinkRules(element);
        if (nRules > 0)
        {
            nuint selected;
            if (nRules > 1)
            {
                linkAttributes_.resize(nRules);
                for (nuint i = 0; i < nRules; i++)
                    linkAttributes_[i] = currentLinkSet.linkRule(element, i).attributes();
                if (!selectLinkRule(linkAttributes_, location, out selected))
                {
                    linkAttributes = null;
                    resultElementSpec = null;
                    return false;
                }
            }
            else
                selected = 0;

            SourceLinkRuleResource? rule = currentLinkSet.linkRule(element, selected);
            open_.insert(new LinkProcessOpenElement(open_.head()!.current, rule!));
            linkAttributes = rule.attributes();
            resultElementSpec = rule.resultElementSpec();
            return true;
        }

        // FIXME construct attributes from attribute definition list
        linkAttributes = null;
        resultElementSpec = null;
        open_.insert(new LinkProcessOpenElement(open_.head()!.current));
        return true;
    }

    // void endElement();
    public void endElement()
    {
        if (lpd_.isNull())
            return;
        LinkProcessOpenElement? top = open_.get();
        if (top != null)
        {
            if (top.post != null)
                open_.head()!.current = top.post;
            else if (top.postRestore)
                open_.head()!.current = open_.head()!.restore;
            // C# GC handles deletion
        }
    }

    // void uselink(const LinkSet *linkSet, Boolean restore, const Lpd *);
    public void uselink(LinkSet? linkSet, Boolean restore, Lpd? lpd)
    {
        if (lpd_.isNull())
            return;
        if (lpd != lpd_.pointer())
            return;
        if (restore)
            open_.head()!.current = open_.head()!.restore;
        else if (linkSet != null)
            open_.head()!.current = linkSet;
    }

    // virtual Boolean selectLinkRule(const Vector<const AttributeList *> &,
    //                                const Location &, size_t &);
    public virtual Boolean selectLinkRule(Vector<AttributeList?> linkAttributes,
                                          Location location,
                                          out nuint selected)
    {
        selected = 0;
        return true;
    }

    // size_t nImpliedLinkRules() const;
    public nuint nImpliedLinkRules()
    {
        if (open_.head() == null)
            return 0;
        return open_.head()!.current!.nImpliedLinkRules();
    }

    // const ResultElementSpec &impliedLinkRule(size_t) const;
    public ResultElementSpec impliedLinkRule(nuint i)
    {
        return open_.head()!.current!.impliedLinkRule(i);
    }

    // const StringC &name() const;
    public StringC name()
    {
        return lpd_.pointer()!.name();
    }

    // Boolean isExplicit() const;
    public Boolean isExplicit()
    {
        return lpd_.pointer()!.type() == Lpd.Type.explicitLink;
    }

    // void clear();
    public void clear()
    {
        open_.clear();
        lpd_.clear();
        linkAttributes_.clear();
    }

    // void swap(LinkProcess &);
    public void swap(LinkProcess to)
    {
        open_.swap(to.open_);
        lpd_.swap(to.lpd_);
        linkAttributes_.swap(to.linkAttributes_);
    }
}
