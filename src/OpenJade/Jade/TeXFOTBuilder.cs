// TeXFOTBuilder.cs: a Generic TeX backend for Jade
// Written by David Megginson <dmeggins@microstar.com>
// With changes from Sebastian Rahtz <s.rahtz@elsevier.co.uk>
// Ported to C# as part of OpenJade-NET

// Table Support: Kathleen Marszalek <kmarszal@watarts.uwaterloo.ca>

namespace OpenJade.Jade;

using OpenSP;
using OpenJade.Style;
using OpenJade.Grove;
using System.Text;
using Char = System.UInt32;
using Boolean = System.Boolean;

// TeX FOT Builder - produces TeX/LaTeX output from DSSSL flow objects
public class TeXFOTBuilder : FOTBuilder
{
    private OutputByteStream os_;
    private Messenger? mgr_;
#pragma warning disable CS0414 // Field assigned but never used (port stub)
    private bool preserveSdata_;
#pragma warning restore CS0414
    private int inMath_;
    private System.Collections.Generic.List<Format> formatStack_;
    private Format nextFormat_;

    // Format structure for inherited characteristics
    public class Format
    {
        public long fontSize = 10000; // 10pt default
        public StringC fontFamilyName = new StringC();
        public Symbol fontWeight = Symbol.symbolMedium;
        public Symbol fontPosture = Symbol.symbolUpright;
        public LengthSpec startIndent = new LengthSpec();
        public LengthSpec endIndent = new LengthSpec();
        public LengthSpec firstLineStartIndent = new LengthSpec();
        public LengthSpec lastLineEndIndent = new LengthSpec();
        public LengthSpec lineSpacing = new LengthSpec();
        public Symbol lines = Symbol.symbolWrap;
        public Symbol quadding = Symbol.symbolStart;
        public Symbol displayAlignment = Symbol.symbolStart;
        public DeviceRGBColor color;
        public bool hasBackgroundColor = false;
        public DeviceRGBColor backgroundColor;
        public bool borderPresent = false;
        public long lineThickness = 1000; // 1pt
        public bool inhibitLineBreaks = false;
        public bool hyphenate = true;
        public bool kern = true;
        public bool ligature = true;
        public bool scoreSpaces = true;
        public Letter2 language;
        public Letter2 country;

        public Format()
        {
            color = new DeviceRGBColor { red = 0, green = 0, blue = 0 };
            backgroundColor = new DeviceRGBColor { red = 255, green = 255, blue = 255 };
            language = new Letter2((ushort)0);
            country = new Letter2((ushort)0);
        }

        public Format(Format other)
        {
            fontSize = other.fontSize;
            fontFamilyName = new StringC(other.fontFamilyName);
            fontWeight = other.fontWeight;
            fontPosture = other.fontPosture;
            startIndent = other.startIndent;
            endIndent = other.endIndent;
            firstLineStartIndent = other.firstLineStartIndent;
            lastLineEndIndent = other.lastLineEndIndent;
            lineSpacing = other.lineSpacing;
            lines = other.lines;
            quadding = other.quadding;
            displayAlignment = other.displayAlignment;
            color = other.color;
            hasBackgroundColor = other.hasBackgroundColor;
            backgroundColor = other.backgroundColor;
            borderPresent = other.borderPresent;
            lineThickness = other.lineThickness;
            inhibitLineBreaks = other.inhibitLineBreaks;
            hyphenate = other.hyphenate;
            kern = other.kern;
            ligature = other.ligature;
            scoreSpaces = other.scoreSpaces;
            language = other.language;
            country = other.country;
        }
    }

    public TeXFOTBuilder(OutputByteStream os, Messenger? mgr)
    {
        os_ = os;
        mgr_ = mgr;
        preserveSdata_ = false;
        inMath_ = 0;
        formatStack_ = new System.Collections.Generic.List<Format>();
        nextFormat_ = new Format();
        formatStack_.Add(new Format());
    }

    private Format curFormat()
    {
        return formatStack_.Count > 0 ? formatStack_[formatStack_.Count - 1] : nextFormat_;
    }

    private void pushFormat()
    {
        formatStack_.Add(new Format(nextFormat_));
    }

    private void popFormat()
    {
        if (formatStack_.Count > 1)
        {
            formatStack_.RemoveAt(formatStack_.Count - 1);
            nextFormat_ = new Format(formatStack_[formatStack_.Count - 1]);
        }
    }

    // Output helpers
    private void output(string s)
    {
        foreach (char c in s)
            os_.sputc((sbyte)c);
    }

    private void outputChar(Char c)
    {
        if (c < 128)
            os_.sputc((sbyte)c);
        else
        {
            // Output as TeX Unicode command
            output("\\char");
            output(c.ToString());
            output(" ");
        }
    }

    private void outputLength(long units)
    {
        // Convert from 1/1000 pt to pt
        double pts = units / 1000.0;
        output(pts.ToString("F3"));
        output("pt");
    }

    public override void start()
    {
        pushFormat();
    }

    public override void end()
    {
        popFormat();
    }

    public override void characters(Char[] data, nuint size)
    {
        for (nuint i = 0; i < size; i++)
        {
            Char c = data[i];
            switch (c)
            {
                case '\\':
                    output("\\Slash{}");
                    break;
                case '{':
                    output("\\{");
                    break;
                case '}':
                    output("\\}");
                    break;
                case '$':
                    output("\\$");
                    break;
                case '&':
                    output("\\&");
                    break;
                case '#':
                    output("\\#");
                    break;
                case '%':
                    output("\\%");
                    break;
                case '^':
                    output("\\^{}");
                    break;
                case '_':
                    output("\\_");
                    break;
                case '~':
                    output("\\~{}");
                    break;
                case '<':
                    output("$<$");
                    break;
                case '>':
                    output("$>$");
                    break;
                case '|':
                    output("$|$");
                    break;
                default:
                    if (c < 128)
                        os_.sputc((sbyte)c);
                    else if (c < 256)
                    {
                        // Latin-1 supplement - use appropriate TeX encoding
                        output("\\char");
                        output(c.ToString());
                        output(" ");
                    }
                    else
                    {
                        // Unicode character
                        output("\\unichar{");
                        output(c.ToString());
                        output("}");
                    }
                    break;
            }
        }
    }

    public override void startParagraph(ParagraphNIC nic)
    {
        output("\n\\FOTParagraph{");
        pushFormat();
        outputDisplayNIC(nic);
        output("}{");
    }

    public override void endParagraph()
    {
        output("}\n");
        popFormat();
    }

    public override void startSequence()
    {
        pushFormat();
    }

    public override void endSequence()
    {
        popFormat();
    }

    public override void startDisplayGroup(DisplayGroupNIC nic)
    {
        output("\\FOTDisplayGroup{");
        pushFormat();
        outputDisplayNIC(nic);
        output("}{");
    }

    public override void endDisplayGroup()
    {
        output("}\n");
        popFormat();
    }

    public override void startScroll()
    {
        output("\\FOTScroll{");
        pushFormat();
    }

    public override void endScroll()
    {
        output("}\n");
        popFormat();
    }

    public override void startBox(BoxNIC nic)
    {
        output("\\FOTBox{");
        pushFormat();
        if (nic.isDisplay)
            outputDisplayNIC(nic);
        output("}{");
    }

    public override void endBox()
    {
        output("}\n");
        popFormat();
    }

    public override void startTable(TableNIC nic)
    {
        output("\n\\begin{FOTTable}");
        pushFormat();
        switch (nic.widthType)
        {
            case TableNIC.WidthType.widthExplicit:
                output("[width=");
                outputLengthSpec(nic.width);
                output("]");
                break;
            case TableNIC.WidthType.widthMinimum:
                output("[minimum-width]");
                break;
        }
        output("\n");
    }

    public override void endTable()
    {
        output("\\end{FOTTable}\n");
        popFormat();
    }

    public override void startTableRow()
    {
        output("\\FOTTableRow{");
        pushFormat();
    }

    public override void endTableRow()
    {
        output("}\n");
        popFormat();
    }

    public override void startTableCell(TableCellNIC nic)
    {
        output("\\FOTTableCell");
        if (!nic.missing)
        {
            output("[column=");
            output((nic.columnIndex + 1).ToString());
            if (nic.nColumnsSpanned != 1)
            {
                output(",colspan=");
                output(nic.nColumnsSpanned.ToString());
            }
            if (nic.nRowsSpanned != 1)
            {
                output(",rowspan=");
                output(nic.nRowsSpanned.ToString());
            }
            output("]");
        }
        output("{");
        pushFormat();
    }

    public override void endTableCell()
    {
        output("}");
        popFormat();
    }

    public override void tableColumn(TableColumnNIC nic)
    {
        output("\\FOTTableColumn[column=");
        output((nic.columnIndex + 1).ToString());
        if (nic.nColumnsSpanned != 1)
        {
            output(",colspan=");
            output(nic.nColumnsSpanned.ToString());
        }
        if (nic.hasWidth)
        {
            output(",width=");
            outputTableLengthSpec(nic.width);
        }
        output("]\n");
    }

    public override void externalGraphic(ExternalGraphicNIC nic)
    {
        output("\\FOTExternalGraphic{");
        outputStringC(nic.entitySystemId);
        output("}{");
        outputStringC(nic.notationSystemId);
        output("}\n");
    }

    public override void rule(RuleNIC nic)
    {
        output("\\FOTRule");
        switch (nic.orientation)
        {
            case Symbol.symbolHorizontal:
                output("[horizontal]");
                break;
            case Symbol.symbolVertical:
                output("[vertical]");
                break;
        }
        if (nic.hasLength)
        {
            output("{");
            outputLengthSpec(nic.length);
            output("}");
        }
        output("\n");
    }

    public override void alignmentPoint()
    {
        output("\\FOTAlignmentPoint{}");
    }

    public override void character(CharacterNIC nic)
    {
        if ((nic.specifiedC & (1 << CharacterNIC.cChar)) != 0)
        {
            Char[] data = new Char[] { nic.ch };
            characters(data, 1);
        }
    }

    public override void paragraphBreak(ParagraphNIC nic)
    {
        output("\\FOTParagraphBreak{}\n");
    }

    public override void startLink(Address addr)
    {
        output("\\FOTLink{");
        pushFormat();
    }

    public override void endLink()
    {
        output("}");
        popFormat();
    }

    public override void startMarginalia()
    {
        output("\\FOTMarginalia{");
        pushFormat();
    }

    public override void endMarginalia()
    {
        output("}");
        popFormat();
    }

    public override void startLeader(LeaderNIC nic)
    {
        output("\\FOTLeader{");
        pushFormat();
    }

    public override void endLeader()
    {
        output("}");
        popFormat();
    }

    public override void startSideline()
    {
        output("\\FOTSideline{");
        pushFormat();
    }

    public override void endSideline()
    {
        output("}");
        popFormat();
    }

    public override void startLineField(LineFieldNIC nic)
    {
        output("\\FOTLineField{");
        pushFormat();
    }

    public override void endLineField()
    {
        output("}");
        popFormat();
    }

    public override void startScore(Char c)
    {
        output("\\FOTScore[char=");
        outputChar(c);
        output("]{");
        pushFormat();
    }

    public override void startScore(LengthSpec length)
    {
        output("\\FOTScore[length=");
        outputLengthSpec(length);
        output("]{");
        pushFormat();
    }

    public override void startScore(Symbol sym)
    {
        output("\\FOTScore[");
        outputSymbol(sym);
        output("]{");
        pushFormat();
    }

    public override void endScore()
    {
        output("}");
        popFormat();
    }

    // Math flow objects
    public override void startMathSequence()
    {
        output("$");
        inMath_++;
        pushFormat();
    }

    public override void endMathSequence()
    {
        output("$");
        inMath_--;
        popFormat();
    }

    public override void startSuperscript()
    {
        output("^{");
        pushFormat();
    }

    public override void endSuperscript()
    {
        output("}");
        popFormat();
    }

    public override void startSubscript()
    {
        output("_{");
        pushFormat();
    }

    public override void endSubscript()
    {
        output("}");
        popFormat();
    }

    public override void startUnmath()
    {
        output("\\mbox{");
        pushFormat();
    }

    public override void endUnmath()
    {
        output("}");
        popFormat();
    }

    // Grid flow objects
    public override void startGrid(GridNIC nic)
    {
        output("\\begin{FOTGrid}");
        if (nic.nColumns != 0 || nic.nRows != 0)
        {
            output("[");
            if (nic.nColumns != 0)
            {
                output("columns=");
                output(nic.nColumns.ToString());
            }
            if (nic.nRows != 0)
            {
                if (nic.nColumns != 0)
                    output(",");
                output("rows=");
                output(nic.nRows.ToString());
            }
            output("]");
        }
        output("\n");
        pushFormat();
    }

    public override void endGrid()
    {
        output("\\end{FOTGrid}\n");
        popFormat();
    }

    public override void startGridCell(GridCellNIC nic)
    {
        output("\\FOTGridCell");
        if (nic.columnNumber != 1 || nic.rowNumber != 1)
        {
            output("[");
            if (nic.columnNumber != 1)
            {
                output("column=");
                output(nic.columnNumber.ToString());
            }
            if (nic.rowNumber != 1)
            {
                if (nic.columnNumber != 1)
                    output(",");
                output("row=");
                output(nic.rowNumber.ToString());
            }
            output("]");
        }
        output("{");
        pushFormat();
    }

    public override void endGridCell()
    {
        output("}");
        popFormat();
    }

    // Inherited characteristic setters
    public override void setFontSize(long size)
    {
        nextFormat_.fontSize = size;
        output("\\FOTFontSize{");
        outputLength(size);
        output("}");
    }

    public override void setFontFamilyName(StringC name)
    {
        nextFormat_.fontFamilyName = new StringC(name);
        output("\\FOTFontFamily{");
        outputStringC(name);
        output("}");
    }

    public override void setFontWeight(Symbol weight)
    {
        nextFormat_.fontWeight = weight;
        switch (weight)
        {
            case Symbol.symbolBold:
                output("\\bfseries{}");
                break;
            case Symbol.symbolMedium:
                output("\\mdseries{}");
                break;
            case Symbol.symbolLight:
                output("\\ltseries{}");
                break;
        }
    }

    public override void setFontPosture(Symbol posture)
    {
        nextFormat_.fontPosture = posture;
        switch (posture)
        {
            case Symbol.symbolItalic:
                output("\\itshape{}");
                break;
            case Symbol.symbolOblique:
                output("\\slshape{}");
                break;
            case Symbol.symbolUpright:
                output("\\upshape{}");
                break;
        }
    }

    public override void setStartIndent(LengthSpec indent)
    {
        nextFormat_.startIndent = indent;
        output("\\FOTStartIndent{");
        outputLengthSpec(indent);
        output("}");
    }

    public override void setEndIndent(LengthSpec indent)
    {
        nextFormat_.endIndent = indent;
        output("\\FOTEndIndent{");
        outputLengthSpec(indent);
        output("}");
    }

    public override void setFirstLineStartIndent(LengthSpec indent)
    {
        nextFormat_.firstLineStartIndent = indent;
        output("\\FOTFirstLineStartIndent{");
        outputLengthSpec(indent);
        output("}");
    }

    public override void setLastLineEndIndent(LengthSpec indent)
    {
        nextFormat_.lastLineEndIndent = indent;
        output("\\FOTLastLineEndIndent{");
        outputLengthSpec(indent);
        output("}");
    }

    public override void setLineSpacing(LengthSpec spacing)
    {
        nextFormat_.lineSpacing = spacing;
        output("\\FOTLineSpacing{");
        outputLengthSpec(spacing);
        output("}");
    }

    public override void setLines(Symbol lines)
    {
        nextFormat_.lines = lines;
        output("\\FOTLines{");
        outputSymbol(lines);
        output("}");
    }

    public override void setQuadding(Symbol quadding)
    {
        nextFormat_.quadding = quadding;
        switch (quadding)
        {
            case Symbol.symbolStart:
                output("\\raggedright{}");
                break;
            case Symbol.symbolEnd:
                output("\\raggedleft{}");
                break;
            case Symbol.symbolCenter:
                output("\\centering{}");
                break;
            case Symbol.symbolJustify:
                // Default TeX behavior
                break;
        }
    }

    public override void setDisplayAlignment(Symbol alignment)
    {
        nextFormat_.displayAlignment = alignment;
        output("\\FOTDisplayAlignment{");
        outputSymbol(alignment);
        output("}");
    }

    public override void setColor(DeviceRGBColor color)
    {
        nextFormat_.color = color;
        output("\\color[rgb]{");
        output((color.red / 255.0).ToString("F3"));
        output(",");
        output((color.green / 255.0).ToString("F3"));
        output(",");
        output((color.blue / 255.0).ToString("F3"));
        output("}");
    }

    public override void setBackgroundColor()
    {
        nextFormat_.hasBackgroundColor = false;
    }

    public override void setBackgroundColor(DeviceRGBColor color)
    {
        nextFormat_.hasBackgroundColor = true;
        nextFormat_.backgroundColor = color;
        output("\\FOTBackgroundColor{");
        output((color.red / 255.0).ToString("F3"));
        output(",");
        output((color.green / 255.0).ToString("F3"));
        output(",");
        output((color.blue / 255.0).ToString("F3"));
        output("}");
    }

    public override void setBorderPresent(bool present)
    {
        nextFormat_.borderPresent = present;
    }

    public override void setLineThickness(long thickness)
    {
        nextFormat_.lineThickness = thickness;
        output("\\FOTLineThickness{");
        outputLength(thickness);
        output("}");
    }

    public override void setInhibitLineBreaks(bool inhibit)
    {
        nextFormat_.inhibitLineBreaks = inhibit;
        if (inhibit)
            output("\\FOTInhibitLineBreaks{}");
    }

    public override void setHyphenate(bool hyphenate)
    {
        nextFormat_.hyphenate = hyphenate;
        if (!hyphenate)
            output("\\hyphenpenalty=10000{}");
    }

    public override void setKern(bool kern)
    {
        nextFormat_.kern = kern;
    }

    public override void setLigature(bool ligature)
    {
        nextFormat_.ligature = ligature;
    }

    public override void setScoreSpaces(bool score)
    {
        nextFormat_.scoreSpaces = score;
    }

    public override void setLanguage(Letter2 lang)
    {
        nextFormat_.language = lang;
        if (lang.value != 0)
        {
            output("\\selectlanguage{");
            output(((char)((lang.value >> 8) & 0xff)).ToString());
            output(((char)(lang.value & 0xff)).ToString());
            output("}");
        }
    }

    public override void setCountry(Letter2 country)
    {
        nextFormat_.country = country;
    }

    // Helper methods
    private void outputDisplayNIC(DisplayNIC nic)
    {
        if (nic.keepWithPrevious)
            output("keep-with-previous,");
        if (nic.keepWithNext)
            output("keep-with-next,");
        if (nic.positionPreference != Symbol.symbolFalse)
        {
            output("position-preference=");
            outputSymbol(nic.positionPreference);
            output(",");
        }
        if (nic.keep != Symbol.symbolFalse)
        {
            output("keep=");
            outputSymbol(nic.keep);
            output(",");
        }
        if (nic.breakBefore != Symbol.symbolFalse)
        {
            output("break-before=");
            outputSymbol(nic.breakBefore);
            output(",");
        }
        if (nic.breakAfter != Symbol.symbolFalse)
        {
            output("break-after=");
            outputSymbol(nic.breakAfter);
            output(",");
        }
    }

    private void outputLengthSpec(LengthSpec ls)
    {
        if (ls.displaySizeFactor != 0.0)
        {
            if (ls.length != 0)
            {
                outputLength(ls.length);
                if (ls.displaySizeFactor >= 0.0)
                    output("+");
            }
            output((ls.displaySizeFactor * 100.0).ToString("F2"));
            output("%");
        }
        else
            outputLength(ls.length);
    }

    private void outputTableLengthSpec(TableLengthSpec ls)
    {
        bool needSign = false;
        if (ls.length != 0)
        {
            outputLength(ls.length);
            needSign = true;
        }
        if (ls.displaySizeFactor != 0.0)
        {
            if (needSign && ls.displaySizeFactor >= 0.0)
                output("+");
            output((ls.displaySizeFactor * 100.0).ToString("F2"));
            output("%");
            needSign = true;
        }
        if (ls.tableUnitFactor != 0.0)
        {
            if (needSign && ls.tableUnitFactor >= 0.0)
                output("+");
            output(ls.tableUnitFactor.ToString("F2"));
            output("*");
        }
        if (!needSign && ls.tableUnitFactor == 0.0)
            output("0pt");
    }

    private void outputSymbol(Symbol sym)
    {
        string? s = symbolName(sym);
        if (s != null)
            output(s);
        else if (sym == Symbol.symbolFalse)
            output("false");
        else if (sym == Symbol.symbolTrue)
            output("true");
    }

    private void outputStringC(StringC str)
    {
        for (nuint i = 0; i < str.size(); i++)
        {
            Char c = str[i];
            if (c < 128)
                os_.sputc((sbyte)c);
            else
            {
                output("\\char");
                output(c.ToString());
                output(" ");
            }
        }
    }

    private static new string? symbolName(Symbol sym)
    {
        return sym switch
        {
            Symbol.symbolStart => "start",
            Symbol.symbolEnd => "end",
            Symbol.symbolCenter => "center",
            Symbol.symbolJustify => "justify",
            Symbol.symbolWrap => "wrap",
            Symbol.symbolAsis => "asis",
            Symbol.symbolAsisWrap => "asis-wrap",
            Symbol.symbolAsisTruncate => "asis-truncate",
            Symbol.symbolNone => "none",
            Symbol.symbolBefore => "before",
            Symbol.symbolThrough => "through",
            Symbol.symbolAfter => "after",
            Symbol.symbolHorizontal => "horizontal",
            Symbol.symbolVertical => "vertical",
            Symbol.symbolMiter => "miter",
            Symbol.symbolRound => "round",
            Symbol.symbolBevel => "bevel",
            Symbol.symbolButt => "butt",
            Symbol.symbolSquare => "square",
            Symbol.symbolBold => "bold",
            Symbol.symbolMedium => "medium",
            Symbol.symbolLight => "light",
            Symbol.symbolUpright => "upright",
            Symbol.symbolItalic => "italic",
            Symbol.symbolOblique => "oblique",
            Symbol.symbolPreserve => "preserve",
            Symbol.symbolCollapse => "collapse",
            Symbol.symbolIgnore => "ignore",
            Symbol.symbolPage => "page",
            Symbol.symbolColumn => "column",
            Symbol.symbolSolid => "solid",
            Symbol.symbolOutline => "outline",
            Symbol.symbolTop => "top",
            Symbol.symbolBottom => "bottom",
            _ => null
        };
    }

    // Additional atomic flow objects
    public override void pageNumber()
    {
        output("\\FOTPageNumber{}");
    }

    public override void formattingInstruction(StringC instr)
    {
        // Pass through raw formatting instruction to TeX
        for (nuint i = 0; i < instr.size(); i++)
            os_.sputc((sbyte)instr[i]);
    }

    public override void currentNodePageNumber(NodePtr node)
    {
        output("\\FOTCurrentNodePageNumber{");
        // Output node identifier if available
        output("}");
    }

    // Table border methods
    public override void tableBeforeRowBorder()
    {
        output("\\FOTTableBeforeRowBorder{}");
    }

    public override void tableAfterRowBorder()
    {
        output("\\FOTTableAfterRowBorder{}");
    }

    public override void tableBeforeColumnBorder()
    {
        output("\\FOTTableBeforeColumnBorder{}");
    }

    public override void tableAfterColumnBorder()
    {
        output("\\FOTTableAfterColumnBorder{}");
    }

    public override void tableCellBeforeRowBorder()
    {
        output("\\FOTTableCellBeforeRowBorder{}");
    }

    public override void tableCellAfterRowBorder()
    {
        output("\\FOTTableCellAfterRowBorder{}");
    }

    public override void tableCellBeforeColumnBorder()
    {
        output("\\FOTTableCellBeforeColumnBorder{}");
    }

    public override void tableCellAfterColumnBorder()
    {
        output("\\FOTTableCellAfterColumnBorder{}");
    }

    // Math flow objects - fraction support
    public override void fractionBar()
    {
        output("\\FOTFractionBar{}");
    }

    public override void startFraction(out FOTBuilder? numerator, out FOTBuilder? denominator)
    {
        output("\\FOTFraction{");
        pushFormat();
        numerator = null;
        denominator = null;
    }

    public override void endFraction()
    {
        output("}");
        popFormat();
    }

    // Radical support
    public override void radicalRadical(CharacterNIC c)
    {
        output("\\FOTRadicalRadical{");
        if ((c.specifiedC & (1 << CharacterNIC.cChar)) != 0)
            outputChar(c.ch);
        output("}");
    }

    public override void radicalRadicalDefaulted()
    {
        output("\\FOTRadicalRadicalDefaulted{}");
    }

    public override void startRadical(out FOTBuilder? degree)
    {
        output("\\FOTRadical{");
        pushFormat();
        degree = null;
    }

    public override void endRadical()
    {
        output("}");
        popFormat();
    }

    // Script serial flow objects
    public override void startScript(out FOTBuilder? preSup, out FOTBuilder? preSub, out FOTBuilder? postSup, out FOTBuilder? postSub, out FOTBuilder? midSup, out FOTBuilder? midSub)
    {
        output("\\FOTScript{");
        pushFormat();
        preSup = null; preSub = null; postSup = null; postSub = null; midSup = null; midSub = null;
    }

    public override void endScript()
    {
        output("}");
        popFormat();
    }

    // Mark flow objects
    public override void startMark(out FOTBuilder? overMark, out FOTBuilder? underMark)
    {
        output("\\FOTMark{");
        pushFormat();
        overMark = null;
        underMark = null;
    }

    public override void endMark()
    {
        output("}");
        popFormat();
    }

    // Fence flow objects
    public override void startFence(out FOTBuilder? open, out FOTBuilder? close)
    {
        output("\\FOTFence{");
        pushFormat();
        open = null;
        close = null;
    }

    public override void endFence()
    {
        output("}");
        popFormat();
    }

    // Math operator flow objects
    public override void startMathOperator(out FOTBuilder? oper, out FOTBuilder? lowerLimit, out FOTBuilder? upperLimit)
    {
        output("\\FOTMathOperator{");
        pushFormat();
        oper = null;
        lowerLimit = null;
        upperLimit = null;
    }

    public override void endMathOperator()
    {
        output("}");
        popFormat();
    }

    // Node tracking
    public override void startNode(NodePtr node, StringC processingMode)
    {
        output("\\FOTNode{");
        pushFormat();
    }

    public override void endNode()
    {
        output("}");
        popFormat();
    }

    // Simple page sequence
    public override void startSimplePageSequence(FOTBuilder? headerFooter)
    {
        output("\\begin{FOTSimplePageSequence}\n");
        pushFormat();
    }

    public override void endSimplePageSequenceHeaderFooter()
    {
        // End of header/footer region
    }

    public override void endSimplePageSequence()
    {
        output("\\end{FOTSimplePageSequence}\n");
        popFormat();
    }

    // Table part with header/footer
    public override void startTablePart(TablePartNIC nic, out FOTBuilder? header, out FOTBuilder? footer)
    {
        output("\\FOTTablePart{");
        pushFormat();
        header = null;
        footer = null;
    }

    public override void endTablePart()
    {
        output("}");
        popFormat();
    }

    // Multi-mode support
    public override void startMultiMode(MultiMode? principalPort, System.Collections.Generic.List<MultiMode> namedPorts, out System.Collections.Generic.List<FOTBuilder> builders)
    {
        output("\\FOTMultiMode{");
        pushFormat();
        builders = new System.Collections.Generic.List<FOTBuilder>();
    }

    public override void endMultiMode()
    {
        output("}");
        popFormat();
    }

    // Extension flow objects
    public override void extension(ExtensionFlowObj fo, NodePtr currentNode)
    {
        output("\\FOTExtension{}");
    }

    public override void startExtension(ExtensionFlowObj fo, NodePtr currentNode, System.Collections.Generic.List<FOTBuilder?> fotbs)
    {
        output("\\FOTExtensionStart{");
        pushFormat();
    }

    public override void endExtension(ExtensionFlowObj fo)
    {
        output("}");
        popFormat();
    }

    // Additional inherited characteristic setters
    public override void setFieldWidth(LengthSpec width)
    {
        output("\\FOTFieldWidth{");
        outputLengthSpec(width);
        output("}");
    }

    public override void setMarginaliaSep(LengthSpec sep)
    {
        output("\\FOTMarginaliaSep{");
        outputLengthSpec(sep);
        output("}");
    }

    public override void setFieldAlign(Symbol align)
    {
        output("\\FOTFieldAlign{");
        outputSymbol(align);
        output("}");
    }

    public override void setCellBeforeRowMargin(LengthSpec margin)
    {
        output("\\FOTCellBeforeRowMargin{");
        outputLengthSpec(margin);
        output("}");
    }

    public override void setCellAfterRowMargin(LengthSpec margin)
    {
        output("\\FOTCellAfterRowMargin{");
        outputLengthSpec(margin);
        output("}");
    }

    public override void setCellBeforeColumnMargin(LengthSpec margin)
    {
        output("\\FOTCellBeforeColumnMargin{");
        outputLengthSpec(margin);
        output("}");
    }

    public override void setCellAfterColumnMargin(LengthSpec margin)
    {
        output("\\FOTCellAfterColumnMargin{");
        outputLengthSpec(margin);
        output("}");
    }

    public override void setLineSep(LengthSpec sep)
    {
        output("\\FOTLineSep{");
        outputLengthSpec(sep);
        output("}");
    }

    public override void setBoxSizeBefore(LengthSpec size)
    {
        output("\\FOTBoxSizeBefore{");
        outputLengthSpec(size);
        output("}");
    }

    public override void setBoxSizeAfter(LengthSpec size)
    {
        output("\\FOTBoxSizeAfter{");
        outputLengthSpec(size);
        output("}");
    }

    public override void setPositionPointShift(LengthSpec shift)
    {
        output("\\FOTPositionPointShift{");
        outputLengthSpec(shift);
        output("}");
    }

    public override void setStartMargin(LengthSpec margin)
    {
        output("\\FOTStartMargin{");
        outputLengthSpec(margin);
        output("}");
    }

    public override void setEndMargin(LengthSpec margin)
    {
        output("\\FOTEndMargin{");
        outputLengthSpec(margin);
        output("}");
    }

    public override void setSidelineSep(LengthSpec sep)
    {
        output("\\FOTSidelineSep{");
        outputLengthSpec(sep);
        output("}");
    }

    public override void setAsisWrapIndent(LengthSpec indent)
    {
        output("\\FOTAsisWrapIndent{");
        outputLengthSpec(indent);
        output("}");
    }

    public override void setLineNumberSep(LengthSpec sep)
    {
        output("\\FOTLineNumberSep{");
        outputLengthSpec(sep);
        output("}");
    }

    public override void setLastLineJustifyLimit(LengthSpec limit)
    {
        output("\\FOTLastLineJustifyLimit{");
        outputLengthSpec(limit);
        output("}");
    }

    public override void setJustifyGlyphSpaceMaxAdd(LengthSpec add)
    {
        output("\\FOTJustifyGlyphSpaceMaxAdd{");
        outputLengthSpec(add);
        output("}");
    }

    public override void setJustifyGlyphSpaceMaxRemove(LengthSpec remove)
    {
        output("\\FOTJustifyGlyphSpaceMaxRemove{");
        outputLengthSpec(remove);
        output("}");
    }

    public override void setTableCornerRadius(LengthSpec radius)
    {
        output("\\FOTTableCornerRadius{");
        outputLengthSpec(radius);
        output("}");
    }

    public override void setBoxCornerRadius(LengthSpec radius)
    {
        output("\\FOTBoxCornerRadius{");
        outputLengthSpec(radius);
        output("}");
    }

    public override void setFloatOutMarginalia(bool flag)
    {
        if (flag) output("\\FOTFloatOutMarginalia{}");
    }

    public override void setFloatOutSidelines(bool flag)
    {
        if (flag) output("\\FOTFloatOutSidelines{}");
    }

    public override void setFloatOutLineNumbers(bool flag)
    {
        if (flag) output("\\FOTFloatOutLineNumbers{}");
    }

    public override void setCellBackground(bool flag)
    {
        if (flag) output("\\FOTCellBackground{}");
    }

    public override void setSpanWeak(bool flag)
    {
        if (flag) output("\\FOTSpanWeak{}");
    }

    public override void setIgnoreRecordEnd(bool flag)
    {
        if (flag) output("\\FOTIgnoreRecordEnd{}");
    }

    public override void setNumberedLines(bool flag)
    {
        if (flag) output("\\FOTNumberedLines{}");
    }

    public override void setHangingPunct(bool flag)
    {
        if (flag) output("\\FOTHangingPunct{}");
    }

    public override void setBoxOpenEnd(bool flag)
    {
        if (flag) output("\\FOTBoxOpenEnd{}");
    }

    public override void setTruncateLeader(bool flag)
    {
        if (flag) output("\\FOTTruncateLeader{}");
    }

    public override void setAlignLeader(bool flag)
    {
        if (flag) output("\\FOTAlignLeader{}");
    }

    public override void setTablePartOmitMiddleHeader(bool flag)
    {
        if (flag) output("\\FOTTablePartOmitMiddleHeader{}");
    }

    public override void setTablePartOmitMiddleFooter(bool flag)
    {
        if (flag) output("\\FOTTablePartOmitMiddleFooter{}");
    }

    public override void setBorderOmitAtBreak(bool flag)
    {
        if (flag) output("\\FOTBorderOmitAtBreak{}");
    }

    public override void setPrincipalModeSimultaneous(bool flag)
    {
        if (flag) output("\\FOTPrincipalModeSimultaneous{}");
    }

    public override void setMarginaliaKeepWithPrevious(bool flag)
    {
        if (flag) output("\\FOTMarginaliaKeepWithPrevious{}");
    }

    public override void setLineJoin(Symbol join)
    {
        output("\\FOTLineJoin{");
        outputSymbol(join);
        output("}");
    }

    public override void setLineCap(Symbol cap)
    {
        output("\\FOTLineCap{");
        outputSymbol(cap);
        output("}");
    }

    public override void setLineNumberSide(Symbol side)
    {
        output("\\FOTLineNumberSide{");
        outputSymbol(side);
        output("}");
    }

    public override void setKernMode(Symbol mode)
    {
        output("\\FOTKernMode{");
        outputSymbol(mode);
        output("}");
    }

    public override void setInputWhitespaceTreatment(Symbol treatment)
    {
        output("\\FOTInputWhitespaceTreatment{");
        outputSymbol(treatment);
        output("}");
    }

    public override void setFillingDirection(Symbol direction)
    {
        output("\\FOTFillingDirection{");
        outputSymbol(direction);
        output("}");
    }

    public override void setWritingMode(Symbol mode)
    {
        output("\\FOTWritingMode{");
        outputSymbol(mode);
        output("}");
    }

    public override void setLastLineQuadding(Symbol quadding)
    {
        output("\\FOTLastLineQuadding{");
        outputSymbol(quadding);
        output("}");
    }

    public override void setMathDisplayMode(Symbol mode)
    {
        output("\\FOTMathDisplayMode{");
        outputSymbol(mode);
        output("}");
    }

    public override void setBoxType(Symbol type)
    {
        output("\\FOTBoxType{");
        outputSymbol(type);
        output("}");
    }

    public override void setGlyphAlignmentMode(Symbol mode)
    {
        output("\\FOTGlyphAlignmentMode{");
        outputSymbol(mode);
        output("}");
    }

    public override void setBoxBorderAlignment(Symbol align)
    {
        output("\\FOTBoxBorderAlignment{");
        outputSymbol(align);
        output("}");
    }

    public override void setCellRowAlignment(Symbol align)
    {
        output("\\FOTCellRowAlignment{");
        outputSymbol(align);
        output("}");
    }

    public override void setBorderAlignment(Symbol align)
    {
        output("\\FOTBorderAlignment{");
        outputSymbol(align);
        output("}");
    }

    public override void setSidelineSide(Symbol side)
    {
        output("\\FOTSidelineSide{");
        outputSymbol(side);
        output("}");
    }

    public override void setHyphenationKeep(Symbol keep)
    {
        output("\\FOTHyphenationKeep{");
        outputSymbol(keep);
        output("}");
    }

    public override void setFontStructure(Symbol structure)
    {
        output("\\FOTFontStructure{");
        outputSymbol(structure);
        output("}");
    }

    public override void setFontProportionateWidth(Symbol width)
    {
        output("\\FOTFontProportionateWidth{");
        outputSymbol(width);
        output("}");
    }

    public override void setCellCrossed(Symbol crossed)
    {
        output("\\FOTCellCrossed{");
        outputSymbol(crossed);
        output("}");
    }

    public override void setMarginaliaSide(Symbol side)
    {
        output("\\FOTMarginaliaSide{");
        outputSymbol(side);
        output("}");
    }

    public override void setLayer(long n)
    {
        output("\\FOTLayer{");
        output(n.ToString());
        output("}");
    }

    public override void setBackgroundLayer(long n)
    {
        output("\\FOTBackgroundLayer{");
        output(n.ToString());
        output("}");
    }

    public override void setBorderPriority(long n)
    {
        output("\\FOTBorderPriority{");
        output(n.ToString());
        output("}");
    }

    public override void setLineRepeatCount(long n)
    {
        output("\\FOTLineRepeat{");
        output(n.ToString());
        output("}");
    }

    public override void setSpan(long n)
    {
        output("\\FOTSpan{");
        output(n.ToString());
        output("}");
    }

    public override void setMinLeaderRepeat(long n)
    {
        output("\\FOTMinLeaderRepeat{");
        output(n.ToString());
        output("}");
    }

    public override void setHyphenationRemainCharCount(long count)
    {
        output("\\FOTHyphenationRemainCharCount{");
        output(count.ToString());
        output("}");
    }

    public override void setHyphenationPushCharCount(long count)
    {
        output("\\FOTHyphenationPushCharCount{");
        output(count.ToString());
        output("}");
    }

    public override void setWidowCount(long count)
    {
        output("\\widowpenalty=");
        if (count > 0)
            output("10000");
        else
            output("150");
        output("{}");
    }

    public override void setOrphanCount(long count)
    {
        output("\\clubpenalty=");
        if (count > 0)
            output("10000");
        else
            output("150");
        output("{}");
    }

    public override void setExpandTabs(long tabs)
    {
        output("\\FOTExpandTabs{");
        output(tabs.ToString());
        output("}");
    }

    // PublicId-based setters
    public override void setBackgroundTile(string? pubid)
    {
        if (pubid != null)
        {
            output("\\FOTBackgroundTile{");
            output(pubid);
            output("}");
        }
    }

    public override void setLineBreakingMethod(string? pubid)
    {
        if (pubid != null)
        {
            output("\\FOTLineBreakingMethod{");
            output(pubid);
            output("}");
        }
    }

    public override void setLineCompositionMethod(string? pubid)
    {
        if (pubid != null)
        {
            output("\\FOTLineCompositionMethod{");
            output(pubid);
            output("}");
        }
    }

    public override void setImplicitBidiMethod(string? pubid)
    {
        if (pubid != null)
        {
            output("\\FOTImplicitBidiMethod{");
            output(pubid);
            output("}");
        }
    }

    public override void setGlyphSubstMethod(string? pubid)
    {
        if (pubid != null)
        {
            output("\\FOTGlyphSubstMethod{");
            output(pubid);
            output("}");
        }
    }

    public override void setHyphenationMethod(string? pubid)
    {
        if (pubid != null)
        {
            output("\\FOTHyphenationMethod{");
            output(pubid);
            output("}");
        }
    }

    public override void setTableAutoWidthMethod(string? pubid)
    {
        if (pubid != null)
        {
            output("\\FOTTableAutoWidthMethod{");
            output(pubid);
            output("}");
        }
    }

    public override void setFontName(string? name)
    {
        if (name != null)
        {
            output("\\FOTFontName{");
            output(name);
            output("}");
        }
    }

    // Page dimension setters
    public override void setPageWidth(long width)
    {
        output("\\FOTPageWidth{");
        outputLength(width);
        output("}");
    }

    public override void setPageHeight(long height)
    {
        output("\\FOTPageHeight{");
        outputLength(height);
        output("}");
    }

    public override void setLeftMargin(long margin)
    {
        output("\\FOTLeftMargin{");
        outputLength(margin);
        output("}");
    }

    public override void setRightMargin(long margin)
    {
        output("\\FOTRightMargin{");
        outputLength(margin);
        output("}");
    }

    public override void setTopMargin(long margin)
    {
        output("\\FOTTopMargin{");
        outputLength(margin);
        output("}");
    }

    public override void setBottomMargin(long margin)
    {
        output("\\FOTBottomMargin{");
        outputLength(margin);
        output("}");
    }

    public override void setHeaderMargin(long margin)
    {
        output("\\FOTHeaderMargin{");
        outputLength(margin);
        output("}");
    }

    public override void setFooterMargin(long margin)
    {
        output("\\FOTFooterMargin{");
        outputLength(margin);
        output("}");
    }

    // Math alignment setters
    public override void setScriptPreAlign(Symbol align)
    {
        output("\\FOTScriptPreAlign{");
        outputSymbol(align);
        output("}");
    }

    public override void setScriptPostAlign(Symbol align)
    {
        output("\\FOTScriptPostAlign{");
        outputSymbol(align);
        output("}");
    }

    public override void setScriptMidSupAlign(Symbol align)
    {
        output("\\FOTScriptMidSupAlign{");
        outputSymbol(align);
        output("}");
    }

    public override void setScriptMidSubAlign(Symbol align)
    {
        output("\\FOTScriptMidSubAlign{");
        outputSymbol(align);
        output("}");
    }

    public override void setNumeratorAlign(Symbol align)
    {
        output("\\FOTNumeratorAlign{");
        outputSymbol(align);
        output("}");
    }

    public override void setDenominatorAlign(Symbol align)
    {
        output("\\FOTDenominatorAlign{");
        outputSymbol(align);
        output("}");
    }

    // Grid alignment setters
    public override void setGridPositionCellType(Symbol type)
    {
        output("\\FOTGridPositionCellType{");
        outputSymbol(type);
        output("}");
    }

    public override void setGridColumnAlignment(Symbol alignment)
    {
        output("\\FOTGridColumnAlignment{");
        outputSymbol(alignment);
        output("}");
    }

    public override void setGridRowAlignment(Symbol alignment)
    {
        output("\\FOTGridRowAlignment{");
        outputSymbol(alignment);
        output("}");
    }

    public override void setGridEquidistantRows(bool flag)
    {
        if (flag) output("\\FOTGridEquidistantRows{}");
    }

    public override void setGridEquidistantColumns(bool flag)
    {
        if (flag) output("\\FOTGridEquidistantColumns{}");
    }

    // Spacing setters
    public override void setMinPreLineSpacing(OptLengthSpec spacing)
    {
        if (spacing.hasLength)
        {
            output("\\FOTMinPreLineSpacing{");
            outputLengthSpec(spacing.length);
            output("}");
        }
    }

    public override void setMinPostLineSpacing(OptLengthSpec spacing)
    {
        if (spacing.hasLength)
        {
            output("\\FOTMinPostLineSpacing{");
            outputLengthSpec(spacing.length);
            output("}");
        }
    }

    public override void setMinLeading(OptLengthSpec leading)
    {
        if (leading.hasLength)
        {
            output("\\FOTMinLeading{");
            outputLengthSpec(leading.length);
            output("}");
        }
    }

    public override void setEscapementSpaceBefore(InlineSpace space)
    {
        output("\\FOTEscapementSpaceBefore{");
        outputInlineSpace(space);
        output("}");
    }

    public override void setEscapementSpaceAfter(InlineSpace space)
    {
        output("\\FOTEscapementSpaceAfter{");
        outputInlineSpace(space);
        output("}");
    }

    private void outputInlineSpace(InlineSpace space)
    {
        outputLengthSpec(space.nominal);
        output(" plus ");
        outputLengthSpec(space.max);
        output(" minus ");
        outputLengthSpec(space.min);
    }
}
