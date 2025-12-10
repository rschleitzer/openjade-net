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
    private int inSimplePageSequence_;
    private System.Collections.Generic.List<(uint groveIndex, GroveString id)> pendingBookmarks_;
    private BreakType doBreak_;
    private bool keep_;
    private bool hadParInKeep_;

    // Header/footer line spacing constants (in twips)
    // Use a line-spacing of 12pt for the header and footer
    // and assume 2.5pt of it occur after the baseline
    private const int hfPreSpace = 190;
    private const int hfPostSpace = 50;

    // Header/footer storage
    private string[] hfPart_;
    private StringBuilder hfBuilder_;
    private bool inHeaderFooter_;
    private OutputFormat saveOutputFormat_;

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
        public uint lang = 1024; // DEFAULT_LANG (neutral)
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
        public bool hyphenate = false;
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
        public string pageNumberFormat = "dec";
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
        inSimplePageSequence_ = 0;
        pendingBookmarks_ = new System.Collections.Generic.List<(uint, GroveString)>();
        doBreak_ = BreakType.breakNone;
        keep_ = false;
        hadParInKeep_ = false;
        hfPart_ = new string[nHF];
        for (int i = 0; i < nHF; i++)
            hfPart_[i] = "";
        hfBuilder_ = new StringBuilder();
        inHeaderFooter_ = false;
        saveOutputFormat_ = new OutputFormat();
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
        if (inHeaderFooter_)
        {
            hfBuilder_.Append(s);
            return;
        }
        if (stream_ == null) return;
        foreach (char c in s)
            stream_.sputc((sbyte)c);
    }

    private void os(int n)
    {
        os(n.ToString());
    }

    private void os(char c)
    {
        os(c.ToString());
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
        flushPendingBookmarks();
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
        // Language output
        if (specFormat_.language != outputFormat_.language ||
            specFormat_.country != outputFormat_.country)
        {
            outputFormat_.language = specFormat_.language;
            outputFormat_.country = specFormat_.country;
            uint lang = convertLanguage(specFormat_.language, specFormat_.country);
            if (lang != outputFormat_.lang)
            {
                outputFormat_.lang = lang;
                os("\\lang");
                os((int)lang);
                changed = true;
            }
        }
        if (specFormat_.fontFamily != outputFormat_.fontFamily)
        {
            os("\\f");
            os(specFormat_.fontFamily);
            outputFormat_.fontFamily = specFormat_.fontFamily;
            changed = true;
        }
        // Track hyphenation suppression for paragraph end
        if (!specFormat_.hyphenate)
            hyphenateSuppressed_ = true;
        if (changed)
            os(" ");
    }

    public override void startParagraph(ParagraphNIC nic)
    {
        startDisplay(nic);
        start();
        paraStack_.Add(new ParaFormat(paraFormat_));
        paraFormat_ = new ParaFormat(specFormat_.para);
        newPar();
    }

    public override void endParagraph()
    {
        outputParEnd();
        os("\n");
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
        // Handle keep-with-previous
        if (nic.keepWithPrevious)
            keepWithNext_ = true;
        // Handle break-before
        switch (nic.breakBefore)
        {
            case Symbol.symbolPage:
            case Symbol.symbolPageRegion:
            case Symbol.symbolColumnSet:
                doBreak_ = BreakType.breakPage;
                break;
            case Symbol.symbolColumn:
                doBreak_ = BreakType.breakColumn;
                break;
        }
        DisplayInfo info = new DisplayInfo();
        info.spaceAfter = twips(nic.spaceAfter.nominal.length);
        info.keepWithNext = nic.keepWithNext;
        // Handle break-after
        switch (nic.breakAfter)
        {
            case Symbol.symbolPage:
            case Symbol.symbolPageRegion:
            case Symbol.symbolColumnSet:
                info.breakAfter = BreakType.breakPage;
                break;
            case Symbol.symbolColumn:
                info.breakAfter = BreakType.breakColumn;
                break;
            default:
                info.breakAfter = BreakType.breakNone;
                break;
        }
        // Handle keep
        info.saveKeep = keep_;
        switch (nic.keep)
        {
            case Symbol.symbolTrue:
            case Symbol.symbolPage:
            case Symbol.symbolColumnSet:
            case Symbol.symbolColumn:
                if (!keep_)
                {
                    hadParInKeep_ = false;
                    keep_ = true;
                }
                break;
        }
        displayStack_.Add(info);
    }

    private void endDisplay()
    {
        if (displayStack_.Count > 0)
        {
            DisplayInfo info = displayStack_[displayStack_.Count - 1];
            // Restore break type
            doBreak_ = info.breakAfter;
            // Restore keep state
            keep_ = info.saveKeep;
            // Accumulate space after (take maximum)
            if (info.spaceAfter > accumSpace_)
                accumSpace_ = info.spaceAfter;
            // Note: keepWithNext is now handled in outputParEnd, not here
            displayStack_.RemoveAt(displayStack_.Count - 1);
        }
    }

    private void newPar(bool allowSpaceBefore = true)
    {
        // Output page/column break BEFORE \pard (from previous display's breakAfter)
        switch (doBreak_)
        {
            case BreakType.breakPage:
                os("\\page");
                doBreak_ = BreakType.breakNone;
                break;
            case BreakType.breakColumn:
                os("\\column");
                doBreak_ = BreakType.breakNone;
                break;
        }
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
        // Only output quadding if not default (left)
        if (paraFormat_.quadding != 'l')
        {
            os("\\q");
            os(paraFormat_.quadding.ToString());
        }
        os(" ");
    }

    private void outputParEnd()
    {
        // Handle keep region - set keepWithNext_ for paragraphs after the first in a kept region
        if (keep_)
        {
            if (hadParInKeep_ || continuePar_)
                keepWithNext_ = true;
            hadParInKeep_ = true;
        }
        // Check if current display has keepWithNext - apply it to THIS paragraph
        if (displayStack_.Count > 0 && displayStack_[displayStack_.Count - 1].keepWithNext)
            keepWithNext_ = true;
        // Output keep-with-next (only if no break pending)
        if (doBreak_ == BreakType.breakNone && keepWithNext_)
        {
            os("\\keepn");
        }
        keepWithNext_ = false;
        // Output hyphenation suppression
        if (hyphenateSuppressed_)
        {
            os("\\hyphpar0");
            hyphenateSuppressed_ = false;
        }
        os("\\par");
        // Note: page/column break is output in newPar() to match C++ order
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
        inSimplePageSequence_++;
        start();
        // Output section break if not first
        if (hadSection_)
            os("\\sect");
        else
            hadSection_ = true;
        // Ensure minimum margins for header/footer
        if (pageFormat_.headerMargin < hfPreSpace)
            pageFormat_.headerMargin = hfPreSpace;
        if (pageFormat_.footerMargin < hfPostSpace)
            pageFormat_.footerMargin = hfPostSpace;
        // Word 97 seems to get very confused by top or bottom margins less than this
        const int minVMargin = 12 * 20;  // 240 twips
        if (pageFormat_.topMargin < minVMargin)
            pageFormat_.topMargin = minVMargin;
        if (pageFormat_.bottomMargin < minVMargin)
            pageFormat_.bottomMargin = minVMargin;
        // Output section formatting
        os("\\sectd\\plain");
        if (pageFormat_.pageWidth > pageFormat_.pageHeight)
            os("\\lndscpsxn");
        os("\\pgwsxn");
        os((int)pageFormat_.pageWidth);
        os("\\pghsxn");
        os((int)pageFormat_.pageHeight);
        os("\\marglsxn");
        os((int)pageFormat_.leftMargin);
        os("\\margrsxn");
        os((int)pageFormat_.rightMargin);
        os("\\margtsxn");
        os((int)pageFormat_.topMargin);
        os("\\margbsxn");
        os((int)pageFormat_.bottomMargin);
        os("\\headery");
        os(0);
        os("\\footery");
        os(0);
        os("\\pgn");
        // Output page number format (value set by setPageNumberFormat)
        os(pageFormat_.pageNumberFormat);
        if (pageFormat_.pageNumberRestart)
            os("\\pgnrestart");
        // Calculate display size for column support
        displaySize_ = pageFormat_.pageWidth - pageFormat_.leftMargin - pageFormat_.rightMargin;
        outputFormat_ = new OutputFormat();
        accumSpace_ = 0;
    }

    public override void endSimplePageSequence()
    {
        inSimplePageSequence_--;
        end();
    }

    public override void startSimplePageSequenceHeaderFooter(uint flags)
    {
        inlineState_ = InlineState.inlineMiddle;
        saveOutputFormat_ = new OutputFormat(outputFormat_);
        outputFormat_ = new OutputFormat();
        inHeaderFooter_ = true;
        hfBuilder_.Clear();
    }

    public override void endSimplePageSequenceHeaderFooter(uint flags)
    {
        outputFormat_ = saveOutputFormat_;
        hfPart_[flags] = hfBuilder_.ToString();
        inHeaderFooter_ = false;
    }

    public override void endAllSimplePageSequenceHeaderFooter()
    {
        // Check if we have different first-page headers/footers
        bool titlePage = false;
        for (int i = 0; i < nHF; i += nHF / 6)
        {
            if (hfPart_[i | (int)HF.frontHF | (int)HF.firstHF] != hfPart_[i | (int)HF.frontHF | (int)HF.otherHF] ||
                hfPart_[i | (int)HF.backHF | (int)HF.firstHF] != hfPart_[i | (int)HF.backHF | (int)HF.otherHF])
            {
                titlePage = true;
                break;
            }
        }
        if (titlePage)
        {
            os("\\titlepg");
            outputHeaderFooter("f", (int)HF.frontHF | (int)HF.firstHF);
        }
        outputHeaderFooter("l", (int)HF.backHF | (int)HF.otherHF);
        outputHeaderFooter("r", (int)HF.frontHF | (int)HF.otherHF);
        // Clear header/footer storage
        for (int i = 0; i < nHF; i++)
            hfPart_[i] = "";
        inlineState_ = InlineState.inlineFirst;
        continuePar_ = false;
    }

    private void outputHeaderFooter(string suffix, int flags)
    {
        // Calculate tab stops for centering and right alignment
        long tabCenter = (pageFormat_.pageWidth - pageFormat_.leftMargin - pageFormat_.rightMargin) / 2;
        long tabRight = pageFormat_.pageWidth - pageFormat_.leftMargin - pageFormat_.rightMargin;

        // Output header
        os("{\\header");
        os(suffix);
        os("\\pard\\sl");
        os(-(hfPreSpace + hfPostSpace));
        os("\\sb");
        os((int)(pageFormat_.headerMargin - hfPreSpace));
        os("\\sa");
        os((int)(pageFormat_.topMargin - hfPostSpace - pageFormat_.headerMargin));
        os("\\plain\\tqc\\tx");
        os((int)tabCenter);
        os("\\tqr\\tx");
        os((int)tabRight);
        os(" {");
        os(hfPart_[flags | (int)HF.headerHF | (int)HF.leftHF]);
        os("}\\tab {");
        os(hfPart_[flags | (int)HF.headerHF | (int)HF.centerHF]);
        os("}\\tab {");
        os(hfPart_[flags | (int)HF.headerHF | (int)HF.rightHF]);
        os("}\\par}");

        // Output footer
        os("{\\footer");
        os(suffix);
        os("\\pard\\sl");
        os(-(hfPreSpace + hfPostSpace));
        os("\\sb");
        os((int)(pageFormat_.bottomMargin - hfPreSpace - pageFormat_.footerMargin));
        os("\\sa");
        os((int)(pageFormat_.footerMargin - hfPostSpace));
        os("\\plain\\tqc\\tx");
        os((int)tabCenter);
        os("\\tqr\\tx");
        os((int)tabRight);
        os(" {");
        os(hfPart_[flags | (int)HF.footerHF | (int)HF.leftHF]);
        os("}\\tab {");
        os(hfPart_[flags | (int)HF.footerHF | (int)HF.centerHF]);
        os("}\\tab {");
        os(hfPart_[flags | (int)HF.footerHF | (int)HF.rightHF]);
        os("}\\par}");
    }

    public override void pageNumber()
    {
        syncCharFormat();
        os("\\chpgn ");
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

    // Convert ISO language/country codes to RTF language code
    private static uint convertLanguage(uint language, uint country)
    {
        const uint DEFAULT_LANG = 1024; // neutral
        if (language == 0)
            return DEFAULT_LANG;

        // Helper to create 2-letter code
        static uint L2(char c1, char c2) => (uint)((c1 << 8) | c2);

        return language switch
        {
            var l when l == L2('E', 'N') => country switch
            {
                var c when c == L2('U', 'S') => 0x409,  // US English
                var c when c == L2('G', 'B') => 0x809,  // British English
                var c when c == L2('A', 'U') => 0xc09,  // Australian English
                var c when c == L2('C', 'A') => 0x1009, // Canadian English
                _ => 0x409 // Default to US English
            },
            var l when l == L2('D', 'E') => country switch
            {
                var c when c == L2('D', 'E') => 0x407,  // German
                var c when c == L2('C', 'H') => 0x807,  // Swiss German
                var c when c == L2('A', 'T') => 0xc07,  // Austrian German
                _ => 0x407
            },
            var l when l == L2('F', 'R') => country switch
            {
                var c when c == L2('F', 'R') => 0x40c,  // French
                var c when c == L2('B', 'E') => 0x80c,  // Belgian French
                var c when c == L2('C', 'A') => 0xc0c,  // Canadian French
                var c when c == L2('C', 'H') => 0x100c, // Swiss French
                _ => 0x40c
            },
            var l when l == L2('E', 'S') => 0x40a,  // Spanish
            var l when l == L2('I', 'T') => 0x410,  // Italian
            var l when l == L2('P', 'T') => 0x416,  // Portuguese (Brazil)
            var l when l == L2('N', 'L') => 0x413,  // Dutch
            var l when l == L2('D', 'A') => 0x406,  // Danish
            var l when l == L2('S', 'V') => 0x41d,  // Swedish
            var l when l == L2('N', 'O') => 0x414,  // Norwegian
            var l when l == L2('F', 'I') => 0x40b,  // Finnish
            var l when l == L2('P', 'L') => 0x415,  // Polish
            var l when l == L2('C', 'S') => 0x405,  // Czech
            var l when l == L2('H', 'U') => 0x40e,  // Hungarian
            var l when l == L2('R', 'U') => 0x419,  // Russian
            var l when l == L2('E', 'L') => 0x408,  // Greek
            var l when l == L2('J', 'A') => 0x411,  // Japanese
            var l when l == L2('Z', 'H') => 0x804,  // Chinese (simplified)
            var l when l == L2('K', 'O') => 0x412,  // Korean
            _ => DEFAULT_LANG
        };
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
        specFormat_.para.quadding = quadding switch
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
        specFormat_.para.leftIndent = twips(indent.length);
    }

    public override void setEndIndent(LengthSpec indent)
    {
        specFormat_.para.rightIndent = twips(indent.length);
    }

    public override void setFirstLineStartIndent(LengthSpec indent)
    {
        specFormat_.para.firstLineIndent = twips(indent.length);
    }

    public override void setLineSpacing(LengthSpec spacing)
    {
        specFormat_.para.lineSpacing = twips(spacing.length);
    }

    public override void setMinLeading(OptLengthSpec leading)
    {
        specFormat_.para.lineSpacingAtLeast = leading.hasLength;
    }

    public override void setHyphenate(bool hyphenate)
    {
        specFormat_.hyphenate = hyphenate;
    }

    public override void setHeadingLevel(long level)
    {
        specFormat_.para.headingLevel = (level >= 1 && level <= 9) ? (int)level : 0;
    }

    public override void setLanguage(Letter2 lang)
    {
        specFormat_.language = lang.value;
    }

    public override void setCountry(Letter2 country)
    {
        specFormat_.country = country.value;
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

    public void setPageNumberRestart(bool b)
    {
        if (inSimplePageSequence_ == 0)
            pageFormat_.pageNumberRestart = b;
    }

    public override void setPageNumberFormat(StringC str)
    {
        if (inSimplePageSequence_ > 0)
            return;
        pageFormat_.pageNumberFormat = "dec";
        if (str.size() == 1)
        {
            switch (str[0])
            {
                case 'A':
                    pageFormat_.pageNumberFormat = "ucltr";
                    break;
                case 'a':
                    pageFormat_.pageNumberFormat = "lcltr";
                    break;
                case 'I':
                    pageFormat_.pageNumberFormat = "ucrm";
                    break;
                case 'i':
                    pageFormat_.pageNumberFormat = "lcrm";
                    break;
            }
        }
    }

    public override void setWidowCount(long count)
    {
        if (count > 0)
            specFormat_.para.widowOrphanControl |= ParaFormat.widowControl;
        else
            specFormat_.para.widowOrphanControl &= ~ParaFormat.widowControl;
    }

    public override void setOrphanCount(long count)
    {
        if (count > 0)
            specFormat_.para.widowOrphanControl |= ParaFormat.orphanControl;
        else
            specFormat_.para.widowOrphanControl &= ~ParaFormat.orphanControl;
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
        switch (addr.type)
        {
            case Address.Type.resolvedNode:
                {
                    // Link to a node - use bookmark reference
                    GroveString id = new GroveString();
                    if (addr.node.getId(ref id) == AccessResult.accessOK && id.size() > 0)
                    {
                        os("{\\field{\\*\\fldinst   HYPERLINK  \\\\l ");
                        outputBookmarkName(addr.node.groveIndex(), id);
                        os("}{\\fldrslt ");
                    }
                    else
                    {
                        // Fall back to empty hyperlink
                        os("{\\field{\\*\\fldinst HYPERLINK \"\"}{\\fldrslt ");
                    }
                }
                break;
            default:
                // External link or unknown - use empty hyperlink for now
                os("{\\field{\\*\\fldinst HYPERLINK \"\"}{\\fldrslt ");
                break;
        }
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
        // Bookmark support - store bookmark if node has an ID (output later in flushPendingBookmarks)
        if (processingMode.size() == 0 && node != null)
        {
            GroveString id = new GroveString();
            if (node.getId(ref id) == AccessResult.accessOK && id.size() > 0)
            {
                // Store a copy of the id since it may be reused
                uint[] copyData = new uint[id.size()];
                for (nuint i = 0; i < id.size(); i++)
                    copyData[i] = id[i];
                GroveString idCopy = new GroveString(copyData, id.size());
                pendingBookmarks_.Add((node.groveIndex(), idCopy));
            }
        }
    }

    public override void endNode()
    {
        // If no content was output for this node, remove its pending bookmark
        // (simplified version - full C++ implementation tracks node levels)
    }

    public override void currentNodePageNumber(NodePtr node)
    {
        inlinePrepare();
        syncCharFormat();
        GroveString id = new GroveString();
        if (node.getId(ref id) == AccessResult.accessOK)
        {
            os("{\\field\\flddirty{\\*\\fldinst PAGEREF ");
            outputBookmarkName(node.groveIndex(), id);
            os("}{\\fldrslt 000}}");
        }
        else
        {
            ulong n = 0;
            if (node.elementIndex(ref n) == AccessResult.accessOK)
            {
                os("{\\field\\flddirty{\\*\\fldinst PAGEREF ");
                outputBookmarkName(node.groveIndex(), n);
                os("}{\\fldrslt 000}}");
            }
        }
    }

    private void flushPendingBookmarks()
    {
        foreach (var (groveIndex, id) in pendingBookmarks_)
        {
            os("{\\*\\bkmkstart ");
            outputBookmarkName(groveIndex, id);
            os("}{\\*\\bkmkend ");
            outputBookmarkName(groveIndex, id);
            os("}");
        }
        pendingBookmarks_.Clear();
    }

    private void inlinePrepare()
    {
        // Simplified version - just make sure we're ready for inline content
        // Don't output paragraph formatting if already in inline mode
        if (inlineState_ == InlineState.inlineMiddle ||
            inlineState_ == InlineState.inlineField)
            return;
        inlineState_ = InlineState.inlineMiddle;
    }

    private void outputBookmarkName(uint groveIndex, GroveString id)
    {
        os("ID");
        if (groveIndex > 1)
            os((int)(groveIndex - 1));
        os("_");
        // Convert id characters: alphanumeric and underscore stays, others become _XX_
        for (nuint i = 0; i < id.size(); i++)
        {
            uint c = id[i];
            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')
                os(((char)c).ToString());
            else
            {
                os("_");
                os((int)c);
                os("_");
            }
        }
    }

    private void outputBookmarkName(uint groveIndex, ulong elementIndex)
    {
        // For element index based bookmarks (when no id is available)
        os("_");
        if (groveIndex > 0)
        {
            os((int)groveIndex);
            os("_");
        }
        os(elementIndex.ToString());
    }

    // Finalize RTF output
    public void finish()
    {
        os("}\n"); // Close RTF document
        stream_?.flush();
    }
}
