// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using Char = System.UInt32;
using Boolean = System.Boolean;
using Offset = System.UInt32;
using Index = System.UInt32;

// Helper class for text-based input source origin
internal class TextInputSourceOrigin : InputSourceOrigin
{
    private Text text_;
    private Location refLocation_ = new Location();

    public TextInputSourceOrigin(Text text)
    {
        text_ = new Text();
        text_.swap(text);
    }

    public TextInputSourceOrigin(TextInputSourceOrigin other)
    {
        text_ = new Text(other.text_);
        refLocation_ = other.refLocation_;
    }

    public override Boolean defLocation(Offset off, out Origin? origin, out Index index)
    {
        return text_.charLocation(off, out origin, out index);
    }

    public Text text() { return text_; }

    public override void noteCharRef(Index replacementIndex, NamedCharRef ncr)
    {
        // Should not happen for DSSSL spec text
    }

    public override void setExternalInfo(ExternalInfo? info)
    {
        // Should not happen for DSSSL spec text
    }

    public override InputSourceOrigin copy()
    {
        return new TextInputSourceOrigin(this);
    }

    public override Location parent()
    {
        return refLocation_;
    }
}

// Event handler for parsing DSSSL specification documents
public class DssslSpecEventHandler : ArcDirector
{
    // Abstract base class for body elements
    public abstract class BodyElement : Link
    {
        public abstract void makeInputSource(DssslSpecEventHandler eh, out InputSource? inputSource);
    }

    // Body element with immediate text content
    public class ImmediateBodyElement : BodyElement
    {
        private Text text_;

        public ImmediateBodyElement(Text text)
        {
            text_ = new Text();
            text_.swap(text);
        }

        public override void makeInputSource(DssslSpecEventHandler eh, out InputSource? inputSource)
        {
            TextInputSourceOrigin origin = new TextInputSourceOrigin(text_);
            StringC str = origin.text().@string();
            inputSource = new InternalInputSource(str, origin);
        }
    }

    // Body element referencing an entity
    public class EntityBodyElement : BodyElement
    {
        private ConstPtr<Entity> entity_;

        public EntityBodyElement(ConstPtr<Entity> entity)
        {
            entity_ = entity;
        }

        public override void makeInputSource(DssslSpecEventHandler eh, out InputSource? inputSource)
        {
            InternalEntity? internalEnt = entity_.pointer()?.asInternalEntity();
            if (internalEnt != null)
            {
                inputSource = new InternalInputSource(internalEnt.@string(),
                    EntityOrigin.make(entity_, new Location()));
                return;
            }

            ExternalEntity? externalEnt = entity_.pointer()?.asExternalEntity();
            if (externalEnt != null)
            {
                StringC sysid = externalEnt.externalId().effectiveSystemId();
                if (sysid.size() > 0 && eh.parser_ != null && eh.charset_ != null)
                {
                    inputSource = eh.parser_.entityManager().open(sysid, eh.charset_,
                        InputSourceOrigin.make(), 0, eh.mgr_!);
                    return;
                }
            }
            inputSource = null;
        }
    }

    // Declaration element types
    public enum DeclarationType
    {
        features,
        basesetEncoding,
        literalDescribedChar,
        addNameChars,
        addSeparatorChars,
        standardChars,
        otherChars,
        combineChar,
        mapSdataEntity,
        charRepertoire,
        sgmlGrovePlan
    }

    // Declaration element for DSSSL declarations
    public class DeclarationElement : Link
    {
        private DeclarationType type_;
        private Text content_;
        private StringC name_;
        private StringC text_;
        private StringC modadd_;
        private StringC desc_;

        public DeclarationElement(DeclarationType type)
        {
            type_ = type;
            content_ = new Text();
            name_ = new StringC();
            text_ = new StringC();
            modadd_ = new StringC();
            desc_ = new StringC();
        }

        public DeclarationType type() { return type_; }

        public void setContent(Text content)
        {
            content_ = new Text();
            content_.swap(content);
        }

        public void setName(StringC s) { name_ = new StringC(s); }
        public StringC name() { return name_; }

        public void setText(StringC s) { text_ = new StringC(s); }
        public StringC text() { return text_; }

        public void setModadd(StringC s) { modadd_ = new StringC(s); }
        public StringC modadd() { return modadd_; }

        public void setDesc(StringC s) { desc_ = new StringC(s); }
        public StringC desc() { return desc_; }

        public void makeInputSource(DssslSpecEventHandler eh, out InputSource? inputSource)
        {
            TextInputSourceOrigin origin = new TextInputSourceOrigin(content_);
            inputSource = new InternalInputSource(origin.text().@string(), origin);
        }
    }

    // Part header - reference to a specification part
    public class PartHeader : Link
    {
        private Doc doc_;
        private StringC id_;
        private Location refLoc_;
        private SpecPart? specPart_;

        public PartHeader(Doc doc, StringC id)
        {
            doc_ = doc;
            id_ = new StringC(id);
            refLoc_ = new Location();
            specPart_ = null;
        }

        public StringC id() { return id_; }

        public void setPart(SpecPart part)
        {
            specPart_ = part;
        }

        public void setRefLoc(Location loc)
        {
            if (refLoc_.origin().isNull())
                refLoc_ = loc;
        }

        public Part? resolve(DssslSpecEventHandler eh)
        {
            doc_.load(eh);
            if (specPart_ == null)
            {
                eh.mgr_?.setNextLocation(refLoc_);
                eh.message(InterpreterMessages.missingPart, id_);
                return null;
            }
            return specPart_.resolve(eh);
        }
    }

    // Abstract base class for specification parts
    public abstract class SpecPart
    {
        public abstract Part? resolve(DssslSpecEventHandler eh);
    }

    // External part referencing another part header
    public class ExternalPart : SpecPart
    {
        private PartHeader header_;

        public ExternalPart(PartHeader header)
        {
            header_ = header;
        }

        public override Part? resolve(DssslSpecEventHandler eh)
        {
            return header_.resolve(eh);
        }
    }

    // External first part - resolves to first part of a document
    public class ExternalFirstPart : SpecPart
    {
        private Doc doc_;

        public ExternalFirstPart(Doc doc)
        {
            doc_ = doc;
        }

        public override Part? resolve(DssslSpecEventHandler eh)
        {
            return doc_.resolveFirstPart(eh);
        }
    }

    // Concrete part containing body elements and declarations
    public class Part : SpecPart
    {
        private IList<DeclarationElement> declarations_;
        private IList<BodyElement> bodyElements_;
        private System.Collections.Generic.List<PartHeader> use_;
        private bool mark_;
        private Doc doc_;

        public Part(Doc doc)
        {
            declarations_ = new IList<DeclarationElement>();
            bodyElements_ = new IList<BodyElement>();
            use_ = new System.Collections.Generic.List<PartHeader>();
            mark_ = false;
            doc_ = doc;
        }

        public override Part? resolve(DssslSpecEventHandler eh)
        {
            return this;
        }

        public IListIter<BodyElement> iter()
        {
            return new IListIter<BodyElement>(bodyElements_);
        }

        public IListIter<DeclarationElement> diter()
        {
            return new IListIter<DeclarationElement>(declarations_);
        }

        public Doc doc() { return doc_; }

        public System.Collections.Generic.List<PartHeader> use() { return use_; }

        public void addUse(PartHeader header)
        {
            use_.Add(header);
        }

        public void append(BodyElement element)
        {
            bodyElements_.append(element);
        }

        public void append(DeclarationElement element)
        {
            declarations_.append(element);
        }

        public bool setMark(bool b = true)
        {
            bool tem = mark_;
            mark_ = b;
            return tem;
        }
    }

    // Document containing parts
    public class Doc : Link
    {
        private bool loaded_;
        private StringC sysid_;
        private IList<DeclarationElement> declarations_;
        private IList<PartHeader> headers_;
        private Location loc_;

        public Doc()
        {
            loaded_ = false;
            sysid_ = new StringC();
            declarations_ = new IList<DeclarationElement>();
            headers_ = new IList<PartHeader>();
            loc_ = new Location();
        }

        public Doc(StringC sysid)
        {
            loaded_ = false;
            sysid_ = new StringC(sysid);
            declarations_ = new IList<DeclarationElement>();
            headers_ = new IList<PartHeader>();
            loc_ = new Location();
        }

        public StringC sysid() { return sysid_; }

        public void setLocation(Location loc)
        {
            loc_ = loc;
        }

        public PartHeader refPart(StringC id)
        {
            for (IListIter<PartHeader> iter = new IListIter<PartHeader>(headers_); iter.done() == 0; iter.next())
            {
                if (iter.cur()!.id().Equals(id))
                    return iter.cur()!;
            }
            PartHeader header = new PartHeader(this, id);
            headers_.insert(header);
            return header;
        }

        public PartHeader refPart(StringC id, Location refLoc)
        {
            PartHeader header = refPart(id);
            header.setRefLoc(refLoc);
            return header;
        }

        public Part? resolveFirstPart(DssslSpecEventHandler eh)
        {
            load(eh);
            PartHeader? header = null;
            for (IListIter<PartHeader> iter = new IListIter<PartHeader>(headers_); iter.done() == 0; iter.next())
                header = iter.cur();
            if (header == null)
            {
                if (!loc_.origin().isNull())
                {
                    eh.mgr_?.setNextLocation(loc_);
                    eh.message(InterpreterMessages.noParts);
                }
                return null;
            }
            return header.resolve(eh);
        }

        public IListIter<DeclarationElement> diter()
        {
            return new IListIter<DeclarationElement>(declarations_);
        }

        public void load(DssslSpecEventHandler eh)
        {
            if (loaded_)
                return;
            loaded_ = true;
            if (sysid_.size() > 0)
            {
                SgmlParser.Params parms = new SgmlParser.Params();
                parms.parent = eh.parser_;
                parms.sysid = sysid_;
                SgmlParser specParser = new SgmlParser(parms);
                eh.loadDoc(specParser, this);
            }
            else if (eh.parser_ != null)
            {
                eh.loadDoc(eh.parser_, this);
            }
        }

        public void append(DeclarationElement decl)
        {
            declarations_.append(decl);
        }
    }

    // Mapping table entry type
    private struct MappingEntry
    {
        public string gi;
        public Action<DssslSpecEventHandler, StartElementEvent> start;
        public Action<DssslSpecEventHandler, EndElementEvent> end;
    }

    // Mapping table for element handling
    private static readonly MappingEntry[] mappingTable_ = new MappingEntry[]
    {
        new MappingEntry { gi = "STYLE-SPECIFICATION",
            start = (h, e) => h.styleSpecificationStart(e),
            end = (h, e) => h.styleSpecificationEnd(e) },
        new MappingEntry { gi = "STYLE-SPECIFICATION-BODY",
            start = (h, e) => h.styleSpecificationBodyStart(e),
            end = (h, e) => h.styleSpecificationBodyEnd(e) },
        new MappingEntry { gi = "EXTERNAL-SPECIFICATION",
            start = (h, e) => h.externalSpecificationStart(e),
            end = (h, e) => h.externalSpecificationEnd(e) },
        new MappingEntry { gi = "FEATURES",
            start = (h, e) => h.declarationStart(e),
            end = (h, e) => h.declarationEnd(e) },
        new MappingEntry { gi = "BASESET-ENCODING",
            start = (h, e) => h.declarationStart(e),
            end = (h, e) => h.declarationEnd(e) },
        new MappingEntry { gi = "LITERAL-DESCRIBED-CHAR",
            start = (h, e) => h.declarationStart(e),
            end = (h, e) => h.declarationEnd(e) },
        new MappingEntry { gi = "ADD-NAME-CHARS",
            start = (h, e) => h.declarationStart(e),
            end = (h, e) => h.declarationEnd(e) },
        new MappingEntry { gi = "ADD-SEPARATOR-CHARS",
            start = (h, e) => h.declarationStart(e),
            end = (h, e) => h.declarationEnd(e) },
        new MappingEntry { gi = "STANDARD-CHARS",
            start = (h, e) => h.declarationStart(e),
            end = (h, e) => h.declarationEnd(e) },
        new MappingEntry { gi = "OTHER-CHARS",
            start = (h, e) => h.declarationStart(e),
            end = (h, e) => h.declarationEnd(e) },
        new MappingEntry { gi = "COMBINE-CHAR",
            start = (h, e) => h.declarationStart(e),
            end = (h, e) => h.declarationEnd(e) },
        new MappingEntry { gi = "MAP-SDATA-ENTITY",
            start = (h, e) => h.declarationStart(e),
            end = (h, e) => h.declarationEnd(e) },
        new MappingEntry { gi = "CHAR-REPERTOIRE",
            start = (h, e) => h.declarationStart(e),
            end = (h, e) => h.declarationEnd(e) },
        new MappingEntry { gi = "SGML-GROVE-PLAN",
            start = (h, e) => h.declarationStart(e),
            end = (h, e) => h.declarationEnd(e) },
    };

    // Instance fields
    internal Messenger? mgr_;
    private bool gotArc_;
    private bool gatheringBody_;
    private Part? currentPart_;
    private Text currentBody_;
    private Doc? currentDoc_;
    private IList<Doc> docs_;
    internal SgmlParser? parser_;
    internal CharsetInfo? charset_;
    private DeclarationElement? currentDecl_;
    private ErrorCountEventHandler errorHandler_;

    public DssslSpecEventHandler(Messenger mgr)
    {
        mgr_ = mgr;
        gotArc_ = false;
        gatheringBody_ = false;
        currentPart_ = null;
        currentBody_ = new Text();
        currentDoc_ = null;
        docs_ = new IList<Doc>();
        parser_ = null;
        charset_ = null;
        currentDecl_ = null;
        errorHandler_ = new ErrorCountEventHandler();
    }

    public int cancelPtr() { return errorHandler_.cancelPtr(); }

    // Helper to send InterpreterMessages via the messenger
    private void message(InterpreterMessages msg)
    {
        string text = msg switch
        {
            InterpreterMessages.useLoop => "circular use of specification parts",
            InterpreterMessages.specNotArc => "specification document does not have the DSSSL architecture as a base architecture",
            InterpreterMessages.noParts => "document did not contain any style-specifications or external-specifications",
            _ => msg.ToString()
        };
        mgr_?.message(MessageType.Severity.error, mgr_.nextLocation(), text);
    }

    private void message(InterpreterMessages msg, StringC arg)
    {
        string text = msg switch
        {
            InterpreterMessages.missingPart => $"no style-specification or external-specification with ID {arg}",
            _ => $"{msg}: {arg}"
        };
        mgr_?.message(MessageType.Severity.error, mgr_.nextLocation(), text);
    }

    public void load(SgmlParser specParser, CharsetInfo charset, StringC id, System.Collections.Generic.List<Part> parts)
    {
        parser_ = specParser;
        charset_ = charset;
        Doc doc = findDoc(new StringC());
        // Load it now so that we can get the concrete syntax
        doc.load(this);
        Part? tem;
        if (id.size() == 0)
        {
            tem = doc.resolveFirstPart(this);
        }
        else
        {
            StringC normId = new StringC(id);
            ConstPtr<Syntax> syn = parser_.instanceSyntax();
            if (!syn.isNull())
                syn.pointer()!.generalSubstTable()?.subst(normId);
            tem = doc.refPart(normId).resolve(this);
        }
        resolveParts(tem, parts);
    }

    private Doc findDoc(StringC sysid)
    {
        for (IListIter<Doc> iter = new IListIter<Doc>(docs_); iter.done() == 0; iter.next())
        {
            if (sysid.Equals(iter.cur()!.sysid()))
                return iter.cur()!;
        }
        Doc doc = new Doc(sysid);
        docs_.insert(doc);
        return doc;
    }

    private void resolveParts(Part? part, System.Collections.Generic.List<Part> parts)
    {
        if (part == null)
            return;
        parts.Add(part);
        if (part.setMark())
        {
            message(InterpreterMessages.useLoop);
            return;
        }
        System.Collections.Generic.List<PartHeader> use = part.use();
        for (int i = 0; i < use.Count; i++)
        {
            Part? tem = use[i].resolve(this);
            resolveParts(tem, parts);
        }
        part.setMark(false);
    }

    internal void loadDoc(SgmlParser parser, Doc doc)
    {
        currentDoc_ = doc;
        gotArc_ = true; // Assume DSSSL architecture

        // Parse directly - the ArcEngine architecture processing is incomplete
        // For DSSSL specs, we parse the document directly and handle the elements
        parser.parseAll(eventHandler_, cancelPtr());
    }

    // ArcDirector implementation
    public override EventHandler? arcEventHandler(StringC? arcPublicId, Notation? notation,
                                                   Vector<StringC> name, SubstTable? table)
    {
        if (notation == null)
            return null;
        StringC? pubid = notation.externalId().publicIdString();
        string dssslArc = "ISO/IEC 10179:1996//NOTATION DSSSL Architecture Definition Document//EN";
        if (pubid == null || pubid.size() != (nuint)dssslArc.Length)
            return null;
        for (int i = 0; i < dssslArc.Length; i++)
        {
            if (dssslArc[i] != pubid[i])
                return null;
        }
        gotArc_ = true;
        return eventHandler_;
    }

    // EventHandler for DSSSL spec processing
    private EventHandler eventHandler_ => new DssslSpecEventHandlerImpl(this);

    private class DssslSpecEventHandlerImpl : EventHandler
    {
        private DssslSpecEventHandler parent_;

        public DssslSpecEventHandlerImpl(DssslSpecEventHandler parent)
        {
            parent_ = parent;
        }

        public override void message(MessageEvent? ev)
        {
            if (ev != null)
                parent_.mgr_?.dispatchMessage(ev.message());
        }

        public override void endProlog(EndPrologEvent? ev)
        {
            if (ev != null && parent_.currentDoc_ != null)
                parent_.currentDoc_.setLocation(ev.location());
        }

        public override void startElement(StartElementEvent? ev)
        {
            if (ev == null)
                return;
            for (int i = 0; i < mappingTable_.Length; i++)
            {
                if (ev.name().Equals(mappingTable_[i].gi))
                {
                    mappingTable_[i].start(parent_, ev);
                    break;
                }
            }
        }

        public override void endElement(EndElementEvent? ev)
        {
            if (ev == null)
                return;
            for (int i = 0; i < mappingTable_.Length; i++)
            {
                if (ev.name().Equals(mappingTable_[i].gi))
                {
                    mappingTable_[i].end(parent_, ev);
                    break;
                }
            }
        }

        public override void data(DataEvent? ev)
        {
            if (ev != null && parent_.gatheringBody_)
            {
                ulong serial;
                bool isRe = ev.isRe(out serial);
                parent_.currentBody_.addChars(ev.data(), ev.dataOffset(), ev.dataLength(), ev.location());
            }
        }
    }

    private Text? attributeText(StartElementEvent ev, string attName)
    {
        AttributeList atts = ev.attributes();
        StringC attNameString = new StringC();
        for (int i = 0; i < attName.Length; i++)
            attNameString.operatorPlusAssign((Char)attName[i]);
        uint index;
        if (atts.attributeIndex(attNameString, out index))
        {
            AttributeValue? val = atts.value(index);
            if (val != null)
                return val.text();
        }
        return null;
    }

    private StringC? attributeString(StartElementEvent ev, string attName)
    {
        Text? text = attributeText(ev, attName);
        if (text != null)
            return text.@string();
        return null;
    }

    private ConstPtr<Entity> attributeEntity(StartElementEvent ev, string attName)
    {
        AttributeList atts = ev.attributes();
        StringC attNameString = new StringC();
        for (int i = 0; i < attName.Length; i++)
            attNameString.operatorPlusAssign((Char)attName[i]);
        uint index;
        if (!atts.attributeIndex(attNameString, out index))
            return new ConstPtr<Entity>();
        AttributeSemantics? sem = atts.semantics(index);
        if (sem == null || sem.nEntities() != 1)
            return new ConstPtr<Entity>();
        return sem.entity(0);
    }

    public void externalSpecificationStart(StartElementEvent ev)
    {
        StringC empty = new StringC();
        StringC? idP = attributeString(ev, "ID");
        if (idP == null)
            idP = empty;
        PartHeader header = currentDoc_!.refPart(idP);
        Entity? ent = attributeEntity(ev, "DOCUMENT").pointer();
        if (ent == null)
            return;
        ExternalEntity? ext = ent.asExternalEntity();
        if (ext == null)
            return;
        StringC sysid = ext.externalId().effectiveSystemId();
        if (sysid.size() > 0)
        {
            Doc doc = findDoc(sysid);
            StringC? specidP = attributeString(ev, "SPECID");
            if (specidP == null)
                header.setPart(new ExternalFirstPart(doc));
            else
                header.setPart(new ExternalPart(doc.refPart(specidP, ev.location())));
        }
    }

    public void externalSpecificationEnd(EndElementEvent ev)
    {
        // Nothing to do
    }

    public void styleSpecificationStart(StartElementEvent ev)
    {
        StringC empty = new StringC();
        StringC? idP = attributeString(ev, "ID");
        if (idP == null)
            idP = empty;
        PartHeader header = currentDoc_!.refPart(idP);
        Text? useP = attributeText(ev, "USE");
        currentPart_ = new Part(currentDoc_!);
        header.setPart(currentPart_);
        if (useP != null)
        {
            StringC use = useP.@string();
            nuint i = 0;
            for (; ; )
            {
                nuint j;
                for (j = i; j < use.size() && use[j] != ' '; j++)
                    ;
                if (j > i)
                    currentPart_.addUse(currentDoc_!.refPart(use.substr(i, j - i),
                                                              useP.charLocation(i)));
                if (j >= use.size())
                    break;
                i = j + 1;
            }
        }
    }

    public void styleSpecificationEnd(EndElementEvent ev)
    {
        currentPart_ = null;
    }

    public void styleSpecificationBodyStart(StartElementEvent ev)
    {
        if (currentPart_ != null)
        {
            currentBody_.clear();
            ConstPtr<Entity> entity = attributeEntity(ev, "CONTENT");
            if (entity.isNull())
                gatheringBody_ = true;
            else
                currentPart_.append(new EntityBodyElement(entity));
        }
    }

    public void styleSpecificationBodyEnd(EndElementEvent ev)
    {
        if (gatheringBody_)
        {
            if (currentPart_ != null)
                currentPart_.append(new ImmediateBodyElement(currentBody_));
            gatheringBody_ = false;
        }
    }

    public void declarationStart(StartElementEvent ev)
    {
        if (currentPart_ != null || currentDoc_ != null)
        {
            currentBody_.clear();
            gatheringBody_ = true;
            DeclarationType type;
            string name = ev.name().ToString();
            switch (name)
            {
                case "FEATURES":
                    type = DeclarationType.features;
                    break;
                case "BASESET-ENCODING":
                    type = DeclarationType.basesetEncoding;
                    break;
                case "LITERAL-DESCRIBED-CHAR":
                    type = DeclarationType.literalDescribedChar;
                    break;
                case "ADD-NAME-CHARS":
                    type = DeclarationType.addNameChars;
                    break;
                case "ADD-SEPARATOR-CHARS":
                    type = DeclarationType.addSeparatorChars;
                    break;
                case "STANDARD-CHARS":
                    type = DeclarationType.standardChars;
                    break;
                case "OTHER-CHARS":
                    type = DeclarationType.otherChars;
                    break;
                case "COMBINE-CHAR":
                    type = DeclarationType.combineChar;
                    break;
                case "MAP-SDATA-ENTITY":
                    type = DeclarationType.mapSdataEntity;
                    break;
                case "CHAR-REPERTOIRE":
                    type = DeclarationType.charRepertoire;
                    break;
                case "SGML-GROVE-PLAN":
                    type = DeclarationType.sgmlGrovePlan;
                    break;
                default:
                    type = DeclarationType.features;
                    break;
            }
            currentDecl_ = new DeclarationElement(type);
            StringC? str;
            if ((str = attributeString(ev, "NAME")) != null)
                currentDecl_.setName(str);
            if ((str = attributeString(ev, "TEXT")) != null)
                currentDecl_.setText(str);
            if ((str = attributeString(ev, "MODADD")) != null)
                currentDecl_.setModadd(str);
            if ((str = attributeString(ev, "DESC")) != null)
                currentDecl_.setDesc(str);
        }
    }

    public void declarationEnd(EndElementEvent ev)
    {
        if (gatheringBody_ && currentDecl_ != null)
        {
            currentDecl_.setContent(currentBody_);
            if (currentPart_ != null)
                currentPart_.append(currentDecl_);
            else if (currentDoc_ != null)
                currentDoc_.append(currentDecl_);
            gatheringBody_ = false;
            currentDecl_ = null;
        }
    }
}
