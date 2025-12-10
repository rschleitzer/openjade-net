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

// RTF FOT Builder - produces Rich Text Format output
public class RtfFOTBuilder : FOTBuilder
{
    private OutputByteStream? stream_;
    private System.Collections.Generic.List<StringC>? options_;
    private Ptr<ExtendEntityManager>? entityManager_;
    private CharsetInfo? charsetInfo_;
    private Messenger? messenger_;

    // RTF state
    private OutputFormat outputFormat_;
    private Format specFormat_;
    private ParaFormat paraFormat_;
    private System.Collections.Generic.List<Format> specFormatStack_;
    private System.Collections.Generic.List<ParaFormat> paraStack_;
    private System.Collections.Generic.List<DisplayInfo> displayStack_;
    private PageFormat pageFormat_;
    private InlineState inlineState_;
    private bool continuePar_;
    private int accumSpace_;
    private bool keepWithNext_;
    private long displaySize_;
    private bool hadSection_;
    private bool hyphenateSuppressed_;

    // Font management
    private System.Collections.Generic.Dictionary<string, int> fontFamilyNameTable_;
    private int nextRtfFontNumber_;

    // Color table
    private System.Collections.Generic.List<DeviceRGBColor> colorTable_;

    public enum InlineState
    {
        inlineFirst,
        inlineStart,
        inlineField,
        inlineFieldEnd,
        inlineMiddle,
        inlineTable
    }

    public enum UnderlineType
    {
        noUnderline,
        underlineSingle,
        underlineDouble,
        underlineWords
    }

    public enum BreakType
    {
        breakNone,
        breakPage,
        breakColumn
    }

    // Common format properties shared by char and para
    public class CommonFormat
    {
        public bool isBold = false;
        public bool isItalic = false;
        public bool isSmallCaps = false;
        public UnderlineType underline = UnderlineType.noUnderline;
        public bool isStrikethrough = false;
        public int fontFamily = 0;
        public int fontSize = 24; // in half-points (12pt default)
        public int color = 0;
        public int charBackgroundColor = 0;
        public int positionPointShift = 0;
        public uint language = 0;
        public uint country = 0;
        public bool kern = false;
        public bool charBorder = false;
        public int charBorderColor = 0;
        public long charBorderThickness = 0;
        public bool charBorderDouble = false;

        public CommonFormat() { }
        public CommonFormat(CommonFormat other)
        {
            isBold = other.isBold;
            isItalic = other.isItalic;
            isSmallCaps = other.isSmallCaps;
            underline = other.underline;
            isStrikethrough = other.isStrikethrough;
            fontFamily = other.fontFamily;
            fontSize = other.fontSize;
            color = other.color;
            charBackgroundColor = other.charBackgroundColor;
            positionPointShift = other.positionPointShift;
            language = other.language;
            country = other.country;
            kern = other.kern;
            charBorder = other.charBorder;
            charBorderColor = other.charBorderColor;
            charBorderThickness = other.charBorderThickness;
            charBorderDouble = other.charBorderDouble;
        }
    }

    // Output format extends common with charset info
    public class OutputFormat : CommonFormat
    {
        public int charset = 0;
        public uint lang = 1033; // US English
        public uint langCharsets = 0;

        public OutputFormat() { }
        public OutputFormat(OutputFormat other) : base(other)
        {
            charset = other.charset;
            lang = other.lang;
            langCharsets = other.langCharsets;
        }
    }

    // Paragraph format
    public class ParaFormat
    {
        public const int widowControl = 1;
        public const int orphanControl = 2;

        public int leftIndent = 0;
        public int rightIndent = 0;
        public int firstLineIndent = 0;
        public int lineSpacing = 0;
        public bool lineSpacingAtLeast = false;
        public char quadding = 'l';
        public Symbol lines = Symbol.symbolWrap;
        public int widowOrphanControl = 0;
        public int headingLevel = 0;

        public ParaFormat() { }
        public ParaFormat(ParaFormat other)
        {
            leftIndent = other.leftIndent;
            rightIndent = other.rightIndent;
            firstLineIndent = other.firstLineIndent;
            lineSpacing = other.lineSpacing;
            lineSpacingAtLeast = other.lineSpacingAtLeast;
            quadding = other.quadding;
            lines = other.lines;
            widowOrphanControl = other.widowOrphanControl;
            headingLevel = other.headingLevel;
        }
    }

    // Full format extends both para and common
    public class Format : CommonFormat
    {
        public ParaFormat para = new ParaFormat();
        public bool hyphenate = true;
        public int fieldWidth = 0;
        public Symbol fieldAlign = Symbol.symbolStart;
        public Symbol inputWhitespaceTreatment = Symbol.symbolPreserve;
        public long expandTabs = 8;
        public char displayAlignment = 'l';
        public long lineThickness = 1000; // 1pt
        public bool lineDouble = false;
        public bool scoreSpaces = true;
        public bool boxHasBorder = false;
        public bool boxHasBackground = false;
        public int backgroundColor = 0;
        public bool borderPresent = true;
        public bool borderOmitAtBreak = false;
        public bool cellBackground = false;
        public long borderPriority = 0;
        public long cellTopMargin = 0;
        public long cellBottomMargin = 0;
        public long cellLeftMargin = 0;
        public long cellRightMargin = 0;
        public char cellVerticalAlignment = 't';
        public bool mathInline = false;
        public bool mathPosture = false;
        public int superscriptHeight = 0;
        public int subscriptDepth = 0;
        public int overMarkHeight = 0;
        public int underMarkDepth = 0;
        public int gridRowSep = 0;
        public int gridColumnSep = 0;
        public bool span = false;

        public Format() { }
        public Format(Format other) : base(other)
        {
            para = new ParaFormat(other.para);
            hyphenate = other.hyphenate;
            fieldWidth = other.fieldWidth;
            fieldAlign = other.fieldAlign;
            inputWhitespaceTreatment = other.inputWhitespaceTreatment;
            expandTabs = other.expandTabs;
            displayAlignment = other.displayAlignment;
            lineThickness = other.lineThickness;
            lineDouble = other.lineDouble;
            scoreSpaces = other.scoreSpaces;
            boxHasBorder = other.boxHasBorder;
            boxHasBackground = other.boxHasBackground;
            backgroundColor = other.backgroundColor;
            borderPresent = other.borderPresent;
            borderOmitAtBreak = other.borderOmitAtBreak;
            cellBackground = other.cellBackground;
            borderPriority = other.borderPriority;
            cellTopMargin = other.cellTopMargin;
            cellBottomMargin = other.cellBottomMargin;
            cellLeftMargin = other.cellLeftMargin;
            cellRightMargin = other.cellRightMargin;
            cellVerticalAlignment = other.cellVerticalAlignment;
            mathInline = other.mathInline;
            mathPosture = other.mathPosture;
            superscriptHeight = other.superscriptHeight;
            subscriptDepth = other.subscriptDepth;
            overMarkHeight = other.overMarkHeight;
            underMarkDepth = other.underMarkDepth;
            gridRowSep = other.gridRowSep;
            gridColumnSep = other.gridColumnSep;
            span = other.span;
        }
    }

    // Page format
    public class PageFormat
    {
        public long pageWidth = 12240; // 8.5in in twips
        public long pageHeight = 15840; // 11in in twips
        public long leftMargin = 1800; // 1.25in
        public long rightMargin = 1800;
        public long headerMargin = 720; // 0.5in
        public long footerMargin = 720;
        public long topMargin = 1440; // 1in
        public long bottomMargin = 1440;
        public bool pageNumberRestart = false;
        public string pageNumberFormat = "decimal";
        public long nColumns = 1;
        public long columnSep = 720; // 0.5in
        public bool balance = false;

        public PageFormat() { }
        public PageFormat(PageFormat other)
        {
            pageWidth = other.pageWidth;
            pageHeight = other.pageHeight;
            leftMargin = other.leftMargin;
            rightMargin = other.rightMargin;
            headerMargin = other.headerMargin;
            footerMargin = other.footerMargin;
            topMargin = other.topMargin;
            bottomMargin = other.bottomMargin;
            pageNumberRestart = other.pageNumberRestart;
            pageNumberFormat = other.pageNumberFormat;
            nColumns = other.nColumns;
            columnSep = other.columnSep;
            balance = other.balance;
        }
    }

    // Display info for nested flow objects
    public class DisplayInfo
    {
        public int spaceAfter = 0;
        public bool keepWithNext = false;
        public bool saveKeep = false;
        public BreakType breakAfter = BreakType.breakNone;
    }

    // Constructor
    public RtfFOTBuilder(
        OutputByteStream stream,
        System.Collections.Generic.List<StringC> options,
        Ptr<ExtendEntityManager> entityManager,
        CharsetInfo charsetInfo,
        Messenger messenger)
    {
        stream_ = stream;
        options_ = options;
        entityManager_ = entityManager;
        charsetInfo_ = charsetInfo;
        messenger_ = messenger;

        outputFormat_ = new OutputFormat();
        specFormat_ = new Format();
        paraFormat_ = new ParaFormat();
        specFormatStack_ = new System.Collections.Generic.List<Format>();
        paraStack_ = new System.Collections.Generic.List<ParaFormat>();
        displayStack_ = new System.Collections.Generic.List<DisplayInfo>();
        pageFormat_ = new PageFormat();
        inlineState_ = InlineState.inlineFirst;
        continuePar_ = false;
        accumSpace_ = 0;
        keepWithNext_ = false;
        displaySize_ = 0;
        hadSection_ = false;
        fontFamilyNameTable_ = new System.Collections.Generic.Dictionary<string, int>();
        nextRtfFontNumber_ = 0;
        colorTable_ = new System.Collections.Generic.List<DeviceRGBColor>();

        // Initialize fonts to match header (f0=Times, f1=Helvetica)
        fontFamilyNameTable_["Times New Roman"] = nextRtfFontNumber_++;
        fontFamilyNameTable_["Helvetica"] = nextRtfFontNumber_++;
        fontFamilyNameTable_["iso-serif"] = 0;  // Map generic serif to Times
        fontFamilyNameTable_["iso-sanserif"] = 1;  // Map generic sans to Helvetica

        // Write RTF header
        writeHeader();
    }

    private void writeHeader()
    {
        if (stream_ == null) return;
        os("{\\rtf1\\ansi\\deff0\n");
        // Font table - f0=Times, f1=Helvetica (matching original OpenJade)
        os("{\\fonttbl{\\f1\\fnil\\fcharset0 Helvetica;}\n");
        os("{\\f0\\fnil\\fcharset0 Times New Roman;}\n");
        os("}\n");
        // Color table (starts with auto color)
        os("{\\colortbl;}");
        // Stylesheet definitions
        os("{\\stylesheet");
        for (int i = 1; i <= 9; i++)
            os($"{{\\s{i} Heading {i};}}");
        os("}\n");
        // Document settings
        os("\\deflang1024\\notabind\\facingp\\hyphauto1\\widowctrl\n");
    }

    private void os(string s)
    {
        if (stream_ == null) return;
        foreach (char c in s)
            stream_.sputc((sbyte)c);
    }

    private void os(int n)
    {
        os(n.ToString());
    }

    // Convert points to twips (20 twips per point, length is in millipoints)
    private static int twips(long millipoints)
    {
        return (int)((millipoints * 20) / 1000);
    }

    // Convert half-points (RTF font size unit)
    private static int halfPoints(long millipoints)
    {
        return (int)((millipoints * 2) / 1000);
    }

    public override void start()
    {
        specFormatStack_.Add(new Format(specFormat_));
    }

    public override void end()
    {
        if (specFormatStack_.Count > 0)
        {
            // Remove top of stack first, then restore a COPY from new top (C++ copies by value)
            specFormatStack_.RemoveAt(specFormatStack_.Count - 1);
            if (specFormatStack_.Count > 0)
                specFormat_ = new Format(specFormatStack_[specFormatStack_.Count - 1]);
        }
    }

    public override void atomic()
    {
        if (specFormatStack_.Count > 0)
            specFormat_ = new Format(specFormatStack_[specFormatStack_.Count - 1]);
    }

    public override void characters(Char[] data, nuint size)
    {
        syncCharFormat();
        for (nuint i = 0; i < size; i++)
        {
            Char c = data[i];
            if (c == '\\' || c == '{' || c == '}')
            {
                os("\\");
                os(((char)c).ToString());
            }
            else if (c == '\n')
            {
                os("\\line ");
            }
            else if (c < 128)
            {
                os(((char)c).ToString());
            }
            else if (c < 256)
            {
                os("\\'");
                os(((int)c).ToString("x2"));
            }
            else
            {
                os("\\u");
                os((int)c);
                os("?");
            }
        }
    }

    private void syncCharFormat()
    {
        // Sync output format with spec format
        bool changed = false;
        if (specFormat_.isBold != outputFormat_.isBold)
        {
            os(specFormat_.isBold ? "\\b" : "\\b0");
            outputFormat_.isBold = specFormat_.isBold;
            changed = true;
        }
        if (specFormat_.isItalic != outputFormat_.isItalic)
        {
            os(specFormat_.isItalic ? "\\i" : "\\i0");
            outputFormat_.isItalic = specFormat_.isItalic;
            changed = true;
        }
        if (specFormat_.fontSize != outputFormat_.fontSize)
        {
            os("\\fs");
            os(specFormat_.fontSize);
            outputFormat_.fontSize = specFormat_.fontSize;
            changed = true;
        }
        if (specFormat_.fontFamily != outputFormat_.fontFamily)
        {
            os("\\f");
            os(specFormat_.fontFamily);
            outputFormat_.fontFamily = specFormat_.fontFamily;
            changed = true;
        }
        if (changed)
            os(" ");
    }

    public override void startParagraph(ParagraphNIC nic)
    {
        startDisplay(nic);
        paraStack_.Add(new ParaFormat(paraFormat_));
        newPar();
        start();
    }

    public override void endParagraph()
    {
        if (hyphenateSuppressed_)
        {
            os("\\hyphpar0");
            hyphenateSuppressed_ = false;
        }
        os("\\par\n");
        if (paraStack_.Count > 0)
        {
            paraFormat_ = paraStack_[paraStack_.Count - 1];
            paraStack_.RemoveAt(paraStack_.Count - 1);
        }
        end();
        endDisplay();
    }

    private void startDisplay(DisplayNIC nic)
    {
        // Accumulate space before (take maximum)
        int spaceBefore = twips(nic.spaceBefore.nominal.length);
        if (spaceBefore > accumSpace_)
            accumSpace_ = spaceBefore;
        DisplayInfo info = new DisplayInfo();
        info.spaceAfter = twips(nic.spaceAfter.nominal.length);
        info.keepWithNext = nic.keepWithNext;
        displayStack_.Add(info);
    }

    private void endDisplay()
    {
        if (displayStack_.Count > 0)
        {
            DisplayInfo info = displayStack_[displayStack_.Count - 1];
            // Accumulate space after (take maximum)
            if (info.spaceAfter > accumSpace_)
                accumSpace_ = info.spaceAfter;
            displayStack_.RemoveAt(displayStack_.Count - 1);
        }
    }

    private void newPar(bool allowSpaceBefore = true)
    {
        os("\\pard");
        // Space before from accumulated space
        if (accumSpace_ != 0)
        {
            os("\\sb");
            os(accumSpace_);
            accumSpace_ = 0;
        }
        // Heading level/style
        if (paraFormat_.headingLevel != 0)
        {
            os("\\s");
            os(paraFormat_.headingLevel);
        }
        // Line spacing (negative = exact, positive = at least)
        if (paraFormat_.lineSpacing != 0)
        {
            os("\\sl");
            os(paraFormat_.lineSpacingAtLeast ? paraFormat_.lineSpacing : -paraFormat_.lineSpacing);
        }
        // Output paragraph formatting
        if (paraFormat_.leftIndent != 0)
        {
            os("\\li");
            os(paraFormat_.leftIndent);
        }
        if (paraFormat_.rightIndent != 0)
        {
            os("\\ri");
            os(paraFormat_.rightIndent);
        }
        if (paraFormat_.firstLineIndent != 0)
        {
            os("\\fi");
            os(paraFormat_.firstLineIndent);
        }
        switch (paraFormat_.quadding)
        {
            case 'l': os("\\ql"); break;
            case 'c': os("\\qc"); break;
            case 'r': os("\\qr"); break;
            case 'j': os("\\qj"); break;
        }
        os(" ");
    }

    public override void startDisplayGroup(DisplayGroupNIC nic)
    {
        startDisplay(nic);
        start();
    }

    public override void endDisplayGroup()
    {
        end();
        endDisplay();
    }

    public override void startSimplePageSequence(FOTBuilder?[] headerFooter)
    {
        // Fill array with this builder for default behavior
        for (int i = 0; i < nHF; i++)
            headerFooter[i] = this;
        // Output section break if not first
        if (hadSection_)
            os("\\sect\n");
        hadSection_ = true;

        // Output page setup
        os("\\paperw");
        os((int)pageFormat_.pageWidth);
        os("\\paperh");
        os((int)pageFormat_.pageHeight);
        os("\\margl");
        os((int)pageFormat_.leftMargin);
        os("\\margr");
        os((int)pageFormat_.rightMargin);
        os("\\margt");
        os((int)pageFormat_.topMargin);
        os("\\margb");
        os((int)pageFormat_.bottomMargin);
        os("\n");
        start();
    }

    public override void endSimplePageSequence()
    {
        end();
    }

    public override void pageNumber()
    {
        os("{\\field{\\*\\fldinst PAGE}{\\fldrslt ?}}");
    }

    // Inherited characteristic setters
    public override void setFontSize(long size)
    {
        specFormat_.fontSize = halfPoints(size);
    }

    public override void setFontFamilyName(StringC name)
    {
        StringBuilder sb = new StringBuilder();
        for (nuint i = 0; i < name.size(); i++)
            sb.Append((char)name[i]);
        string fontName = sb.ToString();

        if (!fontFamilyNameTable_.TryGetValue(fontName, out int fontNum))
        {
            fontNum = nextRtfFontNumber_++;
            fontFamilyNameTable_[fontName] = fontNum;
        }
        specFormat_.fontFamily = fontNum;
    }

    public override void setFontWeight(Symbol weight)
    {
        specFormat_.isBold = weight switch
        {
            Symbol.symbolBold => true,
            Symbol.symbolSemiBold => true,
            Symbol.symbolExtraBold => true,
            Symbol.symbolUltraBold => true,
            _ => false
        };
    }

    public override void setFontPosture(Symbol posture)
    {
        specFormat_.isItalic = posture == Symbol.symbolItalic || posture == Symbol.symbolOblique;
    }

    public override void setColor(DeviceRGBColor color)
    {
        int colorIndex = findOrAddColor(color);
        specFormat_.color = colorIndex;
    }

    public override void setBackgroundColor(DeviceRGBColor color)
    {
        int colorIndex = findOrAddColor(color);
        specFormat_.charBackgroundColor = colorIndex;
    }

    private int findOrAddColor(DeviceRGBColor color)
    {
        for (int i = 0; i < colorTable_.Count; i++)
        {
            if (colorTable_[i].red == color.red &&
                colorTable_[i].green == color.green &&
                colorTable_[i].blue == color.blue)
                return i + 1; // RTF colors are 1-indexed
        }
        colorTable_.Add(color);
        return colorTable_.Count;
    }

    public override void setQuadding(Symbol quadding)
    {
        paraFormat_.quadding = quadding switch
        {
            Symbol.symbolStart => 'l',
            Symbol.symbolEnd => 'r',
            Symbol.symbolCenter => 'c',
            Symbol.symbolJustify => 'j',
            _ => 'l'
        };
    }

    public override void setStartIndent(LengthSpec indent)
    {
        paraFormat_.leftIndent = twips(indent.length);
    }

    public override void setEndIndent(LengthSpec indent)
    {
        paraFormat_.rightIndent = twips(indent.length);
    }

    public override void setFirstLineStartIndent(LengthSpec indent)
    {
        paraFormat_.firstLineIndent = twips(indent.length);
    }

    public override void setLineSpacing(LengthSpec spacing)
    {
        paraFormat_.lineSpacing = twips(spacing.length);
    }

    public override void setMinLeading(OptLengthSpec leading)
    {
        paraFormat_.lineSpacingAtLeast = leading.hasLength;
    }

    public override void setHyphenate(bool hyphenate)
    {
        specFormat_.hyphenate = hyphenate;
        if (!hyphenate)
            hyphenateSuppressed_ = true;
    }

    // Note: heading-level characteristic not yet implemented in style engine
    public void setHeadingLevel(long level)
    {
        paraFormat_.headingLevel = (level >= 1 && level <= 9) ? (int)level : 0;
    }

    public override void setPageWidth(long width)
    {
        pageFormat_.pageWidth = twips(width);
    }

    public override void setPageHeight(long height)
    {
        pageFormat_.pageHeight = twips(height);
    }

    public override void setLeftMargin(long margin)
    {
        pageFormat_.leftMargin = twips(margin);
    }

    public override void setRightMargin(long margin)
    {
        pageFormat_.rightMargin = twips(margin);
    }

    public override void setTopMargin(long margin)
    {
        pageFormat_.topMargin = twips(margin);
    }

    public override void setBottomMargin(long margin)
    {
        pageFormat_.bottomMargin = twips(margin);
    }

    public override void setHeaderMargin(long margin)
    {
        pageFormat_.headerMargin = twips(margin);
    }

    public override void setFooterMargin(long margin)
    {
        pageFormat_.footerMargin = twips(margin);
    }

    public override void setWidowCount(long count)
    {
        if (count > 0)
            paraFormat_.widowOrphanControl |= ParaFormat.widowControl;
        else
            paraFormat_.widowOrphanControl &= ~ParaFormat.widowControl;
    }

    public override void setOrphanCount(long count)
    {
        if (count > 0)
            paraFormat_.widowOrphanControl |= ParaFormat.orphanControl;
        else
            paraFormat_.widowOrphanControl &= ~ParaFormat.orphanControl;
    }

    // Table support
    public override void startTable(TableNIC nic)
    {
        start();
    }

    public override void endTable()
    {
        end();
    }

    public override void startTableRow()
    {
        os("\\trowd");
    }

    public override void endTableRow()
    {
        os("\\row\n");
    }

    public override void startTableCell(TableCellNIC nic)
    {
        start();
    }

    public override void endTableCell()
    {
        os("\\cell");
        end();
    }

    // Box support
    public override void startBox(BoxNIC nic)
    {
        os("{");
        start();
    }

    public override void endBox()
    {
        end();
        os("}");
    }

    // Rule
    public override void rule(RuleNIC nic)
    {
        int thickness = twips(specFormat_.lineThickness);
        os("\\par\\pard{\\*\\brdrtl\\brdrs\\brdrw");
        os(thickness);
        os("}\\par");
        atomic();
    }

    // External graphic
    public override void externalGraphic(ExternalGraphicNIC nic)
    {
        // RTF picture support would go here
        atomic();
    }

    // Link support
    public override void startLink(Address addr)
    {
        start();
        os("{\\field{\\*\\fldinst HYPERLINK \"");
        // Output URL if available
        os("\"}{\\fldrslt ");
    }

    public override void endLink()
    {
        os("}}");
        end();
    }

    // Score (underline/strikethrough)
    public override void startScore(Symbol type)
    {
        start();
        switch (type)
        {
            case Symbol.symbolBefore:
                os("{\\strike ");
                break;
            default:
                os("{\\ul ");
                break;
        }
    }

    public override void endScore()
    {
        os("}");
        end();
    }

    // Node tracking
    public override void startNode(NodePtr node, StringC processingMode)
    {
        // Bookmark support
    }

    public override void endNode()
    {
    }

    // Finalize RTF output
    public void finish()
    {
        os("}\n"); // Close RTF document
        stream_?.flush();
    }
}
