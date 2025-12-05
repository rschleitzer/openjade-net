// Copyright (c) 1996, 1997 James Clark
// See the file copying.txt for copying permission.
// Ported to C# as part of OpenJade-NET

namespace OpenJade.Jade;

using OpenSP;
using OpenJade.Style;
using OpenJade.Grove;
using System.Text;
using Char = System.UInt32;
using Boolean = System.Boolean;

// HTML FOT Builder - produces HTML output with CSS styling
public class HtmlFOTBuilder : FOTBuilder
{
    private StringC basename_;
    private CmdLineApp? app_;
    private int nDocuments_;
    private System.Collections.Generic.List<FlowObjectInfo> flowObjectStack_;
    private FlowObjectInfo nextFlowObject_;
    private System.Collections.Generic.List<DestInfo> destStack_;
    private System.Collections.Generic.List<Item> dest_;
    private Container root_;
    private System.Collections.Generic.List<StringC> giStack_;
    private long topMargin_;
    private System.Collections.Generic.List<long> spaceAfterStack_;
    private System.Collections.Generic.Dictionary<CharPropsKey, CharStyle> charStyleTable_;
    private System.Collections.Generic.Dictionary<ParaPropsKey, ParaStyle> paraStyleTable_;
    private System.Collections.Generic.Dictionary<string, ClassPrefix> prefixTable_;
    private System.Collections.Generic.List<System.Collections.Generic.List<Addressable?>> elements_;
    private System.Collections.Generic.List<System.Collections.Generic.List<nuint>> pendingAddr_;

    // Character properties key for dictionary lookup
    public struct CharPropsKey : System.IEquatable<CharPropsKey>
    {
        public int fontWeight;
        public int fontStyle;
        public uint color;
        public long fontSize;
        public string fontFamily;

        public CharPropsKey(CharProps cp)
        {
            fontWeight = cp.fontWeight;
            fontStyle = cp.fontStyle;
            color = cp.color;
            fontSize = cp.fontSize;
            var sb = new StringBuilder();
            for (nuint i = 0; i < cp.fontFamily.size(); i++)
                sb.Append((char)cp.fontFamily[i]);
            fontFamily = sb.ToString();
        }

        public bool Equals(CharPropsKey other)
        {
            return fontWeight == other.fontWeight
                && fontStyle == other.fontStyle
                && fontSize == other.fontSize
                && color == other.color
                && fontFamily == other.fontFamily;
        }

        public override bool Equals(object? obj) => obj is CharPropsKey key && Equals(key);
        public override int GetHashCode() => System.HashCode.Combine(fontWeight, fontStyle, fontSize, color, fontFamily);
    }

    // Paragraph properties key for dictionary lookup
    public struct ParaPropsKey : System.IEquatable<ParaPropsKey>
    {
        public long leftMargin;
        public long rightMargin;
        public long lineHeight;
        public long textIndent;
        public long topMargin;
        public int align;

        public ParaPropsKey(ParaProps pp)
        {
            leftMargin = pp.leftMargin;
            rightMargin = pp.rightMargin;
            lineHeight = pp.lineHeight;
            textIndent = pp.textIndent;
            topMargin = pp.topMargin;
            align = pp.align;
        }

        public bool Equals(ParaPropsKey other)
        {
            return leftMargin == other.leftMargin
                && rightMargin == other.rightMargin
                && lineHeight == other.lineHeight
                && textIndent == other.textIndent
                && topMargin == other.topMargin
                && align == other.align;
        }

        public override bool Equals(object? obj) => obj is ParaPropsKey key && Equals(key);
        public override int GetHashCode() => System.HashCode.Combine(leftMargin, rightMargin, lineHeight, textIndent, topMargin, align);
    }

    // Character properties for CSS styling
    public class CharProps
    {
        public const int styleNormal = 0;
        public const int styleItalic = 1;
        public const int styleOblique = 2;

        public int fontWeight = 5; // medium
        public int fontStyle = styleNormal;
        public uint color = 0x000000; // black
        public long fontSize = 10000; // 10pt
        public StringC fontFamily = new StringC();

        public CharProps() { }
        public CharProps(CharProps other)
        {
            fontWeight = other.fontWeight;
            fontStyle = other.fontStyle;
            color = other.color;
            fontSize = other.fontSize;
            fontFamily = new StringC(other.fontFamily);
        }
    }

    // Inherited paragraph properties
    public class InheritParaProps
    {
        public const int alignLeft = 0;
        public const int alignCenter = 1;
        public const int alignRight = 2;
        public const int alignJustify = 3;

        public long leftMargin = 0;
        public long rightMargin = 0;
        public long lineHeight = 12000; // 12pt
        public long textIndent = 0;
        public int align = alignLeft;

        public InheritParaProps() { }
        public InheritParaProps(InheritParaProps other)
        {
            leftMargin = other.leftMargin;
            rightMargin = other.rightMargin;
            lineHeight = other.lineHeight;
            textIndent = other.textIndent;
            align = other.align;
        }
    }

    // Paragraph properties for CSS styling
    public class ParaProps : InheritParaProps
    {
        public long topMargin = 0;

        public ParaProps() { }
        public ParaProps(InheritParaProps inherit) : base(inherit) { }
        public ParaProps(ParaProps other) : base(other)
        {
            topMargin = other.topMargin;
        }
    }

    // Class prefix for CSS class naming
    public class ClassPrefix
    {
        public StringC prefix;
        public int nCharClasses = 0;
        public int nParaClasses = 0;

        public ClassPrefix(StringC s)
        {
            prefix = new StringC(s);
        }
    }

    // Style class base
    public class StyleClass
    {
        public StringC gi;
        public ClassPrefix prefix;
        public int prefixIndex;

        public StyleClass(StringC g, ClassPrefix pfx)
        {
            gi = new StringC(g);
            prefix = pfx;
            prefixIndex = pfx.nCharClasses + pfx.nParaClasses + 1;
        }

        public void outputName(StringBuilder sb)
        {
            for (nuint i = 0; i < gi.size(); i++)
                sb.Append((char)gi[i]);
            sb.Append(prefixIndex);
        }
    }

    // Character style class
    public class CharStyleClass : StyleClass
    {
        public CharStyle style;

        public CharStyleClass(StringC g, ClassPrefix pfx, CharStyle s) : base(g, pfx)
        {
            style = s;
            pfx.nCharClasses++;
        }
    }

    // Character style with CSS output
    public class CharStyle : CharProps
    {
        public System.Collections.Generic.List<CharStyleClass> classes = new System.Collections.Generic.List<CharStyleClass>();

        public CharStyle(CharProps cp) : base(cp) { }

        public void output(StringBuilder sb)
        {
            bool first = true;
            foreach (var cls in classes)
            {
                if (first) first = false;
                else sb.Append(", ");
                sb.Append("SPAN.");
                cls.outputName(sb);
            }
            if (!first)
            {
                sb.Append(" {\r\n");
                sb.Append("  font-family: ");
                for (nuint i = 0; i < fontFamily.size(); i++)
                    sb.Append((char)fontFamily[i]);
                sb.Append(";\r\n");
                sb.Append("  font-weight: ").Append(fontWeight * 100).Append(";\r\n");
                string[] styleNames = { "normal", "italic", "oblique" };
                sb.Append("  font-style: ").Append(styleNames[fontStyle]).Append(";\r\n");
                sb.Append("  font-size: ");
                outputLength(fontSize, sb);
                sb.Append(";\r\n");
                sb.Append("  color: #").Append(color.ToString("x6")).Append(";\r\n");
                sb.Append("}\r\n");
            }
        }
    }

    // Paragraph style class
    public class ParaStyleClass : StyleClass
    {
        public ParaStyle style;

        public ParaStyleClass(StringC g, ClassPrefix pfx, ParaStyle s) : base(g, pfx)
        {
            style = s;
            pfx.nParaClasses++;
        }
    }

    // Paragraph style with CSS output
    public class ParaStyle : ParaProps
    {
        public System.Collections.Generic.List<ParaStyleClass> classes = new System.Collections.Generic.List<ParaStyleClass>();

        public ParaStyle(ParaProps pp) : base(pp) { }

        public void output(StringBuilder sb)
        {
            bool first = true;
            foreach (var cls in classes)
            {
                if (first) first = false;
                else sb.Append(", ");
                sb.Append("DIV.");
                cls.outputName(sb);
            }
            if (!first)
            {
                sb.Append(" {\r\n");
                if (leftMargin != 0)
                {
                    sb.Append("  margin-left: ");
                    outputLength(leftMargin, sb);
                    sb.Append(";\r\n");
                }
                if (rightMargin != 0)
                {
                    sb.Append("  margin-right: ");
                    outputLength(rightMargin, sb);
                    sb.Append(";\r\n");
                }
                if (topMargin != 0)
                {
                    sb.Append("  margin-top: ");
                    outputLength(topMargin, sb);
                    sb.Append(";\r\n");
                }
                string[] alignNames = { "left", "center", "right", "justify" };
                sb.Append("  text-align: ").Append(alignNames[align]).Append(";\r\n");
                sb.Append("  line-height: ");
                outputLength(lineHeight, sb);
                sb.Append(";\r\n");
                sb.Append("  text-indent: ");
                outputLength(textIndent, sb);
                sb.Append(";\r\n");
                sb.Append("}\r\n");
            }
        }
    }

    // Flow object info combining char and para props
    public class FlowObjectInfo : CharProps
    {
        public InheritParaProps paraProps = new InheritParaProps();
        public uint docIndex = uint.MaxValue;
        public StringC? scrollTitle;
        public long parentLeftMargin = 0;
        public long parentRightMargin = 0;

        public FlowObjectInfo() { }
        public FlowObjectInfo(FlowObjectInfo other) : base(other)
        {
            paraProps = new InheritParaProps(other.paraProps);
            docIndex = other.docIndex;
            scrollTitle = other.scrollTitle != null ? new StringC(other.scrollTitle) : null;
            parentLeftMargin = other.parentLeftMargin;
            parentRightMargin = other.parentRightMargin;
        }
    }

    // Destination info for content stack
    public class DestInfo
    {
        public System.Collections.Generic.List<Item> list;
        public DestInfo(System.Collections.Generic.List<Item> p) { list = p; }
    }

    // Abstract item for HTML output
    public abstract class Item
    {
        public abstract void output(StringBuilder sb, OutputState state);
    }

    // Output state for rendering
    public class OutputState
    {
        public StringC basename;
        public StringC styleSheetFilename;
        public uint outputDocIndex = uint.MaxValue;
        public CharStyleClass? curCharStyleClass;

        public OutputState(StringC bn, StringC ssf)
        {
            basename = bn;
            styleSheetFilename = ssf;
        }
    }

    // Addressable element for anchor generation
    public class Addressable : Item
    {
        private nuint groveIndex_;
        private nuint elementIndex_;
        private uint docIndex_ = uint.MaxValue;
        private bool referenced_ = false;

        public Addressable(nuint g, nuint e)
        {
            groveIndex_ = g;
            elementIndex_ = e;
        }

        public bool defined() => docIndex_ != uint.MaxValue;
        public bool referenced() => referenced_;
        public void setDefined(uint docIndex, bool wholeDocument = false)
        {
            docIndex_ = docIndex;
            if (wholeDocument) elementIndex_ = nuint.MaxValue;
        }
        public void setReferenced() { referenced_ = true; }

        public override void output(StringBuilder sb, OutputState state)
        {
            if (referenced_ && state.outputDocIndex == docIndex_)
            {
                sb.Append("<A NAME=\"e").Append(groveIndex_).Append('.').Append(elementIndex_).Append("\"></A>");
            }
        }

        public void outputRef(bool end, StringBuilder sb, OutputState state)
        {
            if (!end)
            {
                sb.Append("<A HREF=\"");
                if (docIndex_ != state.outputDocIndex)
                {
                    // Would output filename here for multi-file
                }
                if (elementIndex_ != nuint.MaxValue)
                    sb.Append("#e").Append(groveIndex_).Append('.').Append(elementIndex_);
                sb.Append("\">");
            }
            else
            {
                sb.Append("</A>");
            }
        }
    }

    // Markup item (raw HTML passthrough)
    public class Markup : Item
    {
        private StringC str_;

        public Markup(StringC s) { str_ = new StringC(s); }

        public override void output(StringBuilder sb, OutputState state)
        {
            for (nuint i = 0; i < str_.size(); i++)
                sb.Append((char)str_[i]);
        }
    }

    // PCDATA item with character styling
    public class Pcdata : Item
    {
        protected CharStyleClass? styleClass_;
        protected Char[] data_;

        public Pcdata(Char[] data, CharStyleClass? styleClass)
        {
            data_ = data;
            styleClass_ = styleClass;
        }

        public override void output(StringBuilder sb, OutputState state)
        {
            bool needSpan = styleClass_ != null && styleClass_ != state.curCharStyleClass;
            if (needSpan)
            {
                sb.Append("<SPAN CLASS=\"");
                styleClass_!.outputName(sb);
                sb.Append("\">");
                state.curCharStyleClass = styleClass_;
            }
            outputCdata(data_, sb);
            if (needSpan)
            {
                sb.Append("</SPAN>");
            }
        }
    }

    // Container for nested content
    public class Container : Item
    {
        protected System.Collections.Generic.List<Item> content_ = new System.Collections.Generic.List<Item>();

        public System.Collections.Generic.List<Item> contentPtr() => content_;

        public override void output(StringBuilder sb, OutputState state)
        {
            foreach (var item in content_)
                item.output(sb, state);
        }

        public void reverse()
        {
            content_.Reverse();
        }
    }

    // Reference container (for links)
    public class Ref : Container
    {
        private Addressable? aref_;

        public Ref(Addressable? aref) { aref_ = aref; }

        public override void output(StringBuilder sb, OutputState state)
        {
            if (aref_ != null)
                aref_.outputRef(false, sb, state);
            base.output(sb, state);
            if (aref_ != null)
                aref_.outputRef(true, sb, state);
        }
    }

    // Block container (paragraph with styling)
    public class Block : Container
    {
        private ParaStyleClass? styleClass_;

        public Block(ParaStyleClass? styleClass) { styleClass_ = styleClass; }

        public override void output(StringBuilder sb, OutputState state)
        {
            sb.Append("<DIV");
            if (styleClass_ != null)
            {
                sb.Append(" CLASS=\"");
                styleClass_.outputName(sb);
                sb.Append("\"");
            }
            sb.Append(">");
            base.output(sb, state);
            sb.Append("</DIV>\r\n");
        }
    }

    // Document container
    public class Document : Container
    {
        private uint index_;
        private StringC? title_;

        public Document(uint index, StringC? title)
        {
            index_ = index;
            title_ = title;
        }

        public override void output(StringBuilder sb, OutputState state)
        {
            uint oldIndex = state.outputDocIndex;
            state.outputDocIndex = index_;
            sb.Append("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\">\r\n");
            sb.Append("<HTML>\r\n<HEAD>\r\n");
            if (title_ != null && title_.size() > 0)
            {
                sb.Append("<TITLE>");
                for (nuint i = 0; i < title_.size(); i++)
                    sb.Append((char)title_[i]);
                sb.Append("</TITLE>\r\n");
            }
            if (state.styleSheetFilename.size() > 0)
            {
                sb.Append("<LINK REL=\"stylesheet\" TYPE=\"text/css\" HREF=\"");
                for (nuint i = 0; i < state.styleSheetFilename.size(); i++)
                    sb.Append((char)state.styleSheetFilename[i]);
                sb.Append("\">\r\n");
            }
            sb.Append("</HEAD>\r\n<BODY>\r\n");
            base.output(sb, state);
            sb.Append("</BODY>\r\n</HTML>\r\n");
            state.outputDocIndex = oldIndex;
        }
    }

    // Constructor
    public HtmlFOTBuilder(StringC basename, CmdLineApp? app)
    {
        basename_ = new StringC(basename);
        app_ = app;
        nDocuments_ = 0;
        flowObjectStack_ = new System.Collections.Generic.List<FlowObjectInfo>();
        nextFlowObject_ = new FlowObjectInfo();
        destStack_ = new System.Collections.Generic.List<DestInfo>();
        root_ = new Container();
        dest_ = root_.contentPtr();
        giStack_ = new System.Collections.Generic.List<StringC> { new StringC("S") };
        topMargin_ = 0;
        spaceAfterStack_ = new System.Collections.Generic.List<long>();
        charStyleTable_ = new System.Collections.Generic.Dictionary<CharPropsKey, CharStyle>();
        paraStyleTable_ = new System.Collections.Generic.Dictionary<ParaPropsKey, ParaStyle>();
        prefixTable_ = new System.Collections.Generic.Dictionary<string, ClassPrefix>();
        elements_ = new System.Collections.Generic.List<System.Collections.Generic.List<Addressable?>>();
        pendingAddr_ = new System.Collections.Generic.List<System.Collections.Generic.List<nuint>>();
        flowObjectStack_.Add(new FlowObjectInfo(nextFlowObject_));
    }

    // Output CSS length
    public static void outputLength(long n, StringBuilder sb)
    {
        double pts = n / 1000.0;
        sb.Append(pts.ToString("F3").TrimEnd('0').TrimEnd('.'));
        sb.Append("pt");
    }

    // Output CDATA with entity escaping
    public static void outputCdata(Char[] data, StringBuilder sb)
    {
        foreach (Char c in data)
        {
            switch (c)
            {
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '&': sb.Append("&amp;"); break;
                case '"': sb.Append("&quot;"); break;
                default:
                    if (c < 128)
                        sb.Append((char)c);
                    else
                        sb.Append("&#").Append(c).Append(';');
                    break;
            }
        }
    }

    public override void start()
    {
        flowObjectStack_.Add(new FlowObjectInfo(nextFlowObject_));
    }

    public override void end()
    {
        if (flowObjectStack_.Count > 0)
            flowObjectStack_.RemoveAt(flowObjectStack_.Count - 1);
        if (flowObjectStack_.Count > 0)
            nextFlowObject_ = new FlowObjectInfo(flowObjectStack_[flowObjectStack_.Count - 1]);
    }

    public override void atomic()
    {
        if (flowObjectStack_.Count > 0)
            nextFlowObject_ = new FlowObjectInfo(flowObjectStack_[flowObjectStack_.Count - 1]);
    }

    public override void characters(Char[] data, nuint size)
    {
        flushPendingAddresses();
        Char[] copy = new Char[size];
        Array.Copy(data, copy, (int)size);
        dest_.Insert(0, new Pcdata(copy, makeCharStyleClass()));
    }

    public override void charactersFromNode(NodePtr nd, Char[] data, nuint size)
    {
        characters(data, size);
    }

    public override void startParagraph(ParagraphNIC nic)
    {
        startDisplay(nic);
        Block block = new Block(makeParaStyleClass());
        nextFlowObject_.parentLeftMargin += nextFlowObject_.paraProps.leftMargin;
        nextFlowObject_.parentRightMargin += nextFlowObject_.paraProps.rightMargin;
        nextFlowObject_.paraProps.leftMargin = 0;
        nextFlowObject_.paraProps.rightMargin = 0;
        dest_.Insert(0, block);
        destStack_.Insert(0, new DestInfo(dest_));
        dest_ = block.contentPtr();
        start();
    }

    public override void endParagraph()
    {
        dest_.Reverse();
        if (destStack_.Count > 0)
        {
            dest_ = destStack_[0].list;
            destStack_.RemoveAt(0);
        }
        end();
        endDisplay();
    }

    public override void startScroll()
    {
        nextFlowObject_.docIndex = (uint)nDocuments_++;
        start();
        Document doc = new Document(nextFlowObject_.docIndex, nextFlowObject_.scrollTitle);
        dest_.Insert(0, doc);
        destStack_.Insert(0, new DestInfo(dest_));
        dest_ = doc.contentPtr();
    }

    public override void endScroll()
    {
        dest_.Reverse();
        if (destStack_.Count > 0)
        {
            dest_ = destStack_[0].list;
            destStack_.RemoveAt(0);
        }
        end();
    }

    public override void startLink(Address addr)
    {
        start();
        Addressable? aref = null;
        if (addr.type == Address.Type.resolvedNode && addr.node != null)
        {
            ulong n = 0;
            if (addr.node.elementIndex(ref n) == AccessResult.accessOK)
            {
                aref = elementAddress(addr.node.groveIndex(), (nuint)n);
                aref.setReferenced();
            }
        }
        Ref rf = new Ref(aref);
        dest_.Insert(0, rf);
        destStack_.Insert(0, new DestInfo(dest_));
        dest_ = rf.contentPtr();
    }

    public override void endLink()
    {
        dest_.Reverse();
        if (destStack_.Count > 0)
        {
            dest_ = destStack_[0].list;
            destStack_.RemoveAt(0);
        }
        end();
    }

    public override void startNode(NodePtr node, StringC processingMode)
    {
        pendingAddr_.Add(new System.Collections.Generic.List<nuint>());
        if (processingMode.size() == 0 && pendingAddr_.Count > 1)
        {
            pendingAddr_[pendingAddr_.Count - 1].AddRange(pendingAddr_[pendingAddr_.Count - 2]);
        }
        giStack_.Add(new StringC());
        GroveString str = new GroveString();
        if (node.getGi(str) == AccessResult.accessOK)
            giStack_[giStack_.Count - 1].assign(str.data(), str.size());
        else if (giStack_.Count >= 2)
            giStack_[giStack_.Count - 1] = new StringC(giStack_[giStack_.Count - 2]);
    }

    public override void endNode()
    {
        if (pendingAddr_.Count > 0)
            pendingAddr_.RemoveAt(pendingAddr_.Count - 1);
        if (giStack_.Count > 0)
            giStack_.RemoveAt(giStack_.Count - 1);
    }

    public override void formattingInstruction(StringC s)
    {
        dest_.Insert(0, new Markup(s));
        atomic();
    }

    // Inherited characteristic setters
    public override void setFontSize(long size)
    {
        nextFlowObject_.fontSize = size;
    }

    public override void setFontFamilyName(StringC name)
    {
        nextFlowObject_.fontFamily = new StringC(name);
    }

    public override void setFontWeight(Symbol weight)
    {
        nextFlowObject_.fontWeight = weight switch
        {
            Symbol.symbolUltraLight => 1,
            Symbol.symbolExtraLight => 2,
            Symbol.symbolLight => 3,
            Symbol.symbolSemiLight => 4,
            Symbol.symbolMedium => 5,
            Symbol.symbolSemiBold => 6,
            Symbol.symbolBold => 7,
            Symbol.symbolExtraBold => 8,
            Symbol.symbolUltraBold => 9,
            _ => 5
        };
    }

    public override void setFontPosture(Symbol posture)
    {
        nextFlowObject_.fontStyle = posture switch
        {
            Symbol.symbolItalic => CharProps.styleItalic,
            Symbol.symbolOblique => CharProps.styleOblique,
            _ => CharProps.styleNormal
        };
    }

    public override void setColor(DeviceRGBColor color)
    {
        nextFlowObject_.color = (uint)((color.red << 16) | (color.green << 8) | color.blue);
    }

    public override void setQuadding(Symbol quadding)
    {
        nextFlowObject_.paraProps.align = quadding switch
        {
            Symbol.symbolStart => InheritParaProps.alignLeft,
            Symbol.symbolEnd => InheritParaProps.alignRight,
            Symbol.symbolCenter => InheritParaProps.alignCenter,
            Symbol.symbolJustify => InheritParaProps.alignJustify,
            _ => InheritParaProps.alignLeft
        };
    }

    public override void setLineSpacing(LengthSpec spacing)
    {
        nextFlowObject_.paraProps.lineHeight = spacing.length;
    }

    public override void setFirstLineStartIndent(LengthSpec indent)
    {
        nextFlowObject_.paraProps.textIndent = indent.length;
    }

    public override void setStartIndent(LengthSpec indent)
    {
        nextFlowObject_.paraProps.leftMargin = indent.length - nextFlowObject_.parentLeftMargin;
    }

    public override void setEndIndent(LengthSpec indent)
    {
        nextFlowObject_.paraProps.rightMargin = indent.length - nextFlowObject_.parentRightMargin;
    }

    // Helper methods
    private void startDisplay(DisplayNIC nic)
    {
        long spaceBefore = nic.spaceBefore.nominal.length;
        if (spaceBefore > topMargin_)
            topMargin_ = spaceBefore;
        spaceAfterStack_.Add(nic.spaceAfter.nominal.length);
    }

    private void endDisplay()
    {
        if (spaceAfterStack_.Count > 0)
        {
            long spaceAfter = spaceAfterStack_[spaceAfterStack_.Count - 1];
            if (spaceAfter > topMargin_)
                topMargin_ = spaceAfter;
            spaceAfterStack_.RemoveAt(spaceAfterStack_.Count - 1);
        }
    }

    private void flushPendingAddresses()
    {
        // Flush any pending address anchors
    }

    private Addressable elementAddress(nuint g, nuint e)
    {
        while (elements_.Count <= (int)g)
            elements_.Add(new System.Collections.Generic.List<Addressable?>());
        var v = elements_[(int)g];
        while (v.Count <= (int)e)
            v.Add(null);
        if (v[(int)e] == null)
            v[(int)e] = new Addressable(g, e);
        return v[(int)e]!;
    }

    private CharStyleClass makeCharStyleClass()
    {
        CharPropsKey key = new CharPropsKey(nextFlowObject_);
        if (!charStyleTable_.TryGetValue(key, out CharStyle? style))
        {
            style = new CharStyle(nextFlowObject_);
            charStyleTable_[key] = style;
        }
        StringC currentGi = giStack_.Count > 0 ? giStack_[giStack_.Count - 1] : new StringC();
        foreach (var cls in style.classes)
            if (cls.gi.Equals(currentGi))
                return cls;
        ClassPrefix prefix = makeClassPrefix(currentGi);
        CharStyleClass sc = new CharStyleClass(currentGi, prefix, style);
        style.classes.Add(sc);
        return sc;
    }

    private ParaStyleClass makeParaStyleClass()
    {
        ParaProps props = new ParaProps(nextFlowObject_.paraProps);
        props.topMargin = topMargin_;
        topMargin_ = 0;
        ParaPropsKey key = new ParaPropsKey(props);
        if (!paraStyleTable_.TryGetValue(key, out ParaStyle? style))
        {
            style = new ParaStyle(props);
            paraStyleTable_[key] = style;
        }
        StringC currentGi = giStack_.Count > 0 ? giStack_[giStack_.Count - 1] : new StringC();
        foreach (var cls in style.classes)
            if (cls.gi.Equals(currentGi))
                return cls;
        ClassPrefix prefix = makeClassPrefix(currentGi);
        ParaStyleClass sc = new ParaStyleClass(currentGi, prefix, style);
        style.classes.Add(sc);
        return sc;
    }

    private ClassPrefix makeClassPrefix(StringC gi)
    {
        StringBuilder sb = new StringBuilder();
        for (nuint i = 0; i < gi.size(); i++)
            sb.Append((char)gi[i]);
        string key = sb.ToString();
        if (!prefixTable_.TryGetValue(key, out ClassPrefix? prefix))
        {
            prefix = new ClassPrefix(gi);
            prefixTable_[key] = prefix;
        }
        return prefix;
    }

    // Generate the CSS stylesheet
    public string generateStyleSheet()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var style in charStyleTable_.Values)
            style.output(sb);
        sb.Append("DIV { margin-top: 0pt; margin-bottom: 0pt; margin-left: 0pt; margin-right: 0pt }\r\n");
        foreach (var style in paraStyleTable_.Values)
            style.output(sb);
        return sb.ToString();
    }

    // Generate the HTML output
    public string generateHtml()
    {
        root_.reverse();
        StringBuilder sb = new StringBuilder();
        StringC ssf = new StringC();
        // Generate stylesheet filename from basename
        StringBuilder ssfb = new StringBuilder();
        for (nuint i = 0; i < basename_.size(); i++)
            ssfb.Append((char)basename_[i]);
        ssfb.Append(".css");
        ssf.assign(ssfb.ToString());
        OutputState state = new OutputState(basename_, ssf);
        root_.output(sb, state);
        return sb.ToString();
    }
}
