// Copyright (c) 1996, 1997 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// SaveFOTBuilder for deferred FOT output
public class SaveFOTBuilder : FOTBuilder
{
    private NodePtr node_;
    private StringC modeName_;
    private System.Collections.Generic.List<Action<FOTBuilder>> operations_ = new();

    public SaveFOTBuilder()
    {
        node_ = new NodePtr();
        modeName_ = new StringC();
    }

    public SaveFOTBuilder(NodePtr node, StringC modeName)
    {
        node_ = node;
        modeName_ = modeName;
    }

    public override SaveFOTBuilder? asSaveFOTBuilder() { return this; }

    public void emit(FOTBuilder target)
    {
        // Only wrap with startNode/endNode if node_ is set (like C++)
        bool hasNode = node_;  // Uses implicit bool conversion
        if (hasNode)
            target.startNode(node_, modeName_);
        foreach (var op in operations_)
            op(target);
        if (hasNode)
            target.endNode();
    }

    // Capture FOT operations for later replay
    public override void characters(Char[] data, nuint len)
    {
        Char[] copy = new Char[len];
        Array.Copy(data, copy, (int)len);
        operations_.Add(fotb => fotb.characters(copy, (nuint)copy.Length));
    }

    public override void startSequence()
    {
        operations_.Add(fotb => fotb.startSequence());
    }

    public override void endSequence()
    {
        operations_.Add(fotb => fotb.endSequence());
    }

    public override void startParagraph(ParagraphNIC nic)
    {
        var copy = nic;
        operations_.Add(fotb => fotb.startParagraph(copy));
    }

    public override void endParagraph()
    {
        operations_.Add(fotb => fotb.endParagraph());
    }

    public override void startDisplayGroup(DisplayGroupNIC nic)
    {
        var copy = nic;
        operations_.Add(fotb => fotb.startDisplayGroup(copy));
    }

    public override void endDisplayGroup()
    {
        operations_.Add(fotb => fotb.endDisplayGroup());
    }

    public override void pageNumber()
    {
        operations_.Add(fotb => fotb.pageNumber());
    }

    // Font and styling characteristics
    public override void setFontSize(long size)
    {
        operations_.Add(fotb => fotb.setFontSize(size));
    }

    public override void setFontFamilyName(StringC name)
    {
        var copy = new StringC(name);
        operations_.Add(fotb => fotb.setFontFamilyName(copy));
    }

    public override void setFontWeight(Symbol weight)
    {
        operations_.Add(fotb => fotb.setFontWeight(weight));
    }

    public override void setFontPosture(Symbol posture)
    {
        operations_.Add(fotb => fotb.setFontPosture(posture));
    }
}

public class FOTBuilder
{
    public enum Symbol
    {
        symbolFalse,
        symbolTrue,
        symbolNotApplicable,
        symbolUltraCondensed,
        symbolExtraCondensed,
        symbolCondensed,
        symbolSemiCondensed,
        symbolUltraLight,
        symbolExtraLight,
        symbolLight,
        symbolSemiLight,
        symbolMedium,
        symbolSemiExpanded,
        symbolExpanded,
        symbolExtraExpanded,
        symbolUltraExpanded,
        symbolSemiBold,
        symbolBold,
        symbolExtraBold,
        symbolUltraBold,
        symbolUpright,
        symbolOblique,
        symbolBackSlantedOblique,
        symbolItalic,
        symbolBackSlantedItalic,
        symbolStart,
        symbolEnd,
        symbolCenter,
        symbolJustify,
        symbolSpreadInside,
        symbolSpreadOutside,
        symbolPageInside,
        symbolPageOutside,
        symbolWrap,
        symbolAsis,
        symbolAsisWrap,
        symbolAsisTruncate,
        symbolNone,
        symbolBefore,
        symbolThrough,
        symbolAfter,
        symbolTopToBottom,
        symbolLeftToRight,
        symbolBottomToTop,
        symbolRightToLeft,
        symbolInside,
        symbolOutside,
        symbolHorizontal,
        symbolVertical,
        symbolEscapement,
        symbolLineProgression,
        symbolMath,
        symbolOrdinary,
        symbolOperator,
        symbolBinary,
        symbolRelation,
        symbolOpening,
        symbolClosing,
        symbolPunctuation,
        symbolInner,
        symbolSpace,
        symbolPage,
        symbolPageRegion,
        symbolColumnSet,
        symbolColumn,
        symbolMax,
        symbolMaxUniform,
        symbolMiter,
        symbolRound,
        symbolBevel,
        symbolButt,
        symbolSquare,
        symbolLoose,
        symbolNormal,
        symbolKern,
        symbolTight,
        symbolTouch,
        symbolPreserve,
        symbolCollapse,
        symbolIgnore,
        symbolRelative,
        symbolDisplay,
        symbolInline,
        symbolBorder,
        symbolBackground,
        symbolBoth,
        symbolBase,
        symbolFont,
        symbolTop,
        symbolBottom,
        symbolSpread,
        symbolSolid,
        symbolOutline,
        symbolWith,
        symbolAgainst,
        symbolForce,
        symbolIndependent,
        symbolPile,
        symbolSupOut,
        symbolSubOut,
        symbolLeadEdge,
        symbolTrailEdge,
        symbolExplicit,
        symbolRowMajor,
        symbolColumnMajor
    }

    public const int nSymbols = (int)Symbol.symbolColumnMajor + 1;

    // Extension class for FOT builder extensions
    public class Extension
    {
        // Extension data
    }

    public struct GlyphId
    {
        public string? publicId;
        public uint suffix;

        public GlyphId() { publicId = null; suffix = 0; }
        public GlyphId(string? s, uint n = 0) { publicId = s; suffix = n; }

        public bool Equals(GlyphId other)
        {
            return publicId == other.publicId && suffix == other.suffix;
        }
    }

    public class GlyphSubstTable : IResource
    {
        private int refCount_ = 0;
        public uint uniqueId;
        public System.Collections.Generic.List<GlyphId> pairs = new System.Collections.Generic.List<GlyphId>();

        public int count() { return refCount_; }
        public void @ref() { refCount_++; }
        public int unref() { return --refCount_; }

        public GlyphId subst(GlyphId id)
        {
            for (int i = 0; i < pairs.Count; i += 2)
                if (id.Equals(pairs[i]))
                    return pairs[i + 1];
            return id;
        }
    }

    public struct LengthSpec
    {
        public long length;
        public double displaySizeFactor;

        public LengthSpec() { length = 0; displaySizeFactor = 0.0; }
        public LengthSpec(long len) { length = len; displaySizeFactor = 0.0; }

        public static implicit operator bool(LengthSpec s) => s.length != 0 || s.displaySizeFactor != 0.0;
    }

    public struct TableLengthSpec
    {
        public long length;
        public double displaySizeFactor;
        public double tableUnitFactor;

        public TableLengthSpec() { length = 0; displaySizeFactor = 0.0; tableUnitFactor = 0.0; }
    }

    public struct OptLengthSpec
    {
        public bool hasLength;
        public LengthSpec length;

        public OptLengthSpec() { hasLength = false; length = new LengthSpec(); }
    }

    public struct DisplaySpace
    {
        public LengthSpec nominal;
        public LengthSpec min;
        public LengthSpec max;
        public long priority;
        public bool conditional;
        public bool force;

        public DisplaySpace()
        {
            nominal = new LengthSpec();
            min = new LengthSpec();
            max = new LengthSpec();
            priority = 0;
            conditional = true;
            force = false;
        }
    }

    public struct InlineSpace
    {
        public LengthSpec nominal;
        public LengthSpec min;
        public LengthSpec max;

        public InlineSpace()
        {
            nominal = new LengthSpec();
            min = new LengthSpec();
            max = new LengthSpec();
        }
    }

    public struct OptInlineSpace
    {
        public bool hasSpace;
        public InlineSpace space;

        public OptInlineSpace() { hasSpace = false; space = new InlineSpace(); }
    }

    public class DisplayNIC
    {
        public DisplaySpace spaceBefore = new DisplaySpace();
        public DisplaySpace spaceAfter = new DisplaySpace();
        public Symbol positionPreference = Symbol.symbolFalse;
        public Symbol keep = Symbol.symbolFalse;
        public Symbol breakBefore = Symbol.symbolFalse;
        public Symbol breakAfter = Symbol.symbolFalse;
        public bool keepWithPrevious = false;
        public bool keepWithNext = false;
        public bool mayViolateKeepBefore = false;
        public bool mayViolateKeepAfter = false;
    }

    public class InlineNIC
    {
        public long breakBeforePriority = 0;
        public long breakAfterPriority = 0;
    }

    public class DisplayGroupNIC : DisplayNIC
    {
        public bool hasCoalesceId = false;
        public StringC coalesceId = new StringC();
    }

    public class ExternalGraphicNIC : DisplayNIC
    {
        public bool isDisplay = true;
        public Symbol scaleType = Symbol.symbolFalse;
        public double[] scale = new double[2];
        public StringC entitySystemId = new StringC();
        public StringC notationSystemId = new StringC();
        public bool hasMaxWidth = false;
        public LengthSpec maxWidth = new LengthSpec();
        public bool hasMaxHeight = false;
        public LengthSpec maxHeight = new LengthSpec();
        public Symbol escapementDirection = Symbol.symbolFalse;
        public LengthSpec positionPointX = new LengthSpec();
        public LengthSpec positionPointY = new LengthSpec();
        public long breakBeforePriority = 0;
        public long breakAfterPriority = 0;
    }

    public class BoxNIC : DisplayNIC
    {
        public bool isDisplay = true;
        public long breakBeforePriority = 0;
        public long breakAfterPriority = 0;
    }

    public class RuleNIC : DisplayNIC
    {
        public Symbol orientation = Symbol.symbolHorizontal;
        public bool hasLength = false;
        public LengthSpec length = new LengthSpec();
        public long breakBeforePriority = 0;
        public long breakAfterPriority = 0;
    }

    public class LeaderNIC : InlineNIC
    {
        public bool hasLength = false;
        public LengthSpec length = new LengthSpec();
    }

    public class ParagraphNIC : DisplayNIC { }

    public class CharacterNIC
    {
        public const int cIsDropAfterLineBreak = 1;
        public const int cIsDropUnlessBeforeLineBreak = 2;
        public const int cIsPunct = 4;
        public const int cIsInputWhitespace = 8;
        public const int cIsInputTab = 16;
        public const int cIsRecordEnd = 32;
        public const int cIsSpace = 64;
        public const int cChar = 128;
        public const int cGlyphId = 256;
        public const int cScript = 512;
        public const int cMathClass = 1024;
        public const int cMathFontPosture = 2048;
        public const int cBreakBeforePriority = 4096;
        public const int cBreakAfterPriority = 8192;

        public bool valid = false;
        public uint specifiedC = 0;
        public Char ch;
        public GlyphId glyphId;
        public long breakBeforePriority = 0;
        public long breakAfterPriority = 0;
        public Symbol mathClass = Symbol.symbolOrdinary;
        public Symbol mathFontPosture = Symbol.symbolFalse;
        public string? script = null;
        public bool isDropAfterLineBreak = false;
        public bool isDropUnlessBeforeLineBreak = false;
        public bool isPunct = false;
        public bool isInputWhitespace = false;
        public bool isInputTab = false;
        public bool isRecordEnd = false;
        public bool isSpace = false;
        public double stretchFactor = 1.0;
    }

    public class LineFieldNIC : InlineNIC { }

    public class TableNIC : DisplayNIC
    {
        public enum WidthType { widthFull, widthMinimum, widthExplicit }

        public WidthType widthType = WidthType.widthFull;
        public LengthSpec width = new LengthSpec();
    }

    public class TablePartNIC : DisplayNIC
    {
        public bool isExplicit;
    }

    public class TableColumnNIC
    {
        public uint columnIndex = 0;
        public uint nColumnsSpanned = 1;
        public bool hasWidth = false;
        public TableLengthSpec width = new TableLengthSpec();
    }

    public class TableCellNIC
    {
        public bool missing = false;
        public uint columnIndex = 0;
        public uint nColumnsSpanned = 1;
        public uint nRowsSpanned = 1;
    }

    public struct DeviceRGBColor
    {
        public byte red;
        public byte green;
        public byte blue;
    }

    public class MultiMode
    {
        public bool hasDesc = false;
        public StringC name = new StringC();
        public StringC desc = new StringC();
    }

    public class Address
    {
        public enum Type
        {
            none,
            resolvedNode,
            idref,
            entity,
            sgmlDocument,
            hytimeLinkend,
            tei,
            html
        }

        public Type type = Type.none;
        public NodePtr node = new NodePtr();
        public StringC[] @params = new StringC[3] { new StringC(), new StringC(), new StringC() };
    }

    public class GridNIC
    {
        public uint nColumns = 1;
        public uint nRows = 1;
    }

    public class GridCellNIC
    {
        public uint columnNumber = 1;
        public uint rowNumber = 1;
    }

    public enum HF
    {
        firstHF = 1,    // 01 octal
        otherHF = 0,
        frontHF = 2,    // 02 octal
        backHF = 0,
        headerHF = 4,   // 04 octal
        footerHF = 0,
        leftHF = 0,     // 0 octal
        centerHF = 8,   // 010 octal
        rightHF = 16    // 020 octal
    }

    public class SimplePageSequenceHeaderFooter
    {
        public FOTBuilder?[] hf = new FOTBuilder?[24];
    }

    public class PageSequenceHeaderFooter : SimplePageSequenceHeaderFooter { }

    // Virtual methods
    public virtual SaveFOTBuilder? asSaveFOTBuilder() { return null; }

    public virtual void start() { }
    public virtual void end() { }
    public virtual void atomic() { start(); end(); }
    public virtual void startNode(NodePtr node, StringC processingMode) { }
    public virtual void endNode() { }

    public virtual void characters(Char[] data, nuint size) { }
    public virtual void charactersFromNode(NodePtr nd, Char[] data, nuint size) { characters(data, size); }
    public virtual void charactersFromNode(NodePtr nd, Char[] data, nuint start, nuint length)
    {
        if (start == 0)
            characters(data, length);
        else
        {
            Char[] subset = new Char[length];
            Array.Copy(data, (int)start, subset, 0, (int)length);
            characters(subset, length);
        }
    }
    public virtual void character(CharacterNIC nic) { atomic(); }
    public virtual void paragraphBreak(ParagraphNIC nic) { atomic(); }
    public virtual void externalGraphic(ExternalGraphicNIC nic) { atomic(); }
    public virtual void rule(RuleNIC nic) { atomic(); }
    public virtual void alignmentPoint() { atomic(); }
    public virtual void formattingInstruction(StringC data) { atomic(); }
    public virtual void pageNumber() { atomic(); }
    public virtual void currentNodePageNumber(NodePtr node) { atomic(); }

    public virtual void startSequence() { start(); }
    public virtual void endSequence() { end(); }
    public virtual void startLineField(LineFieldNIC nic) { start(); }
    public virtual void endLineField() { end(); }
    public virtual void startParagraph(ParagraphNIC nic) { start(); }
    public virtual void endParagraph() { end(); }
    public virtual void startDisplayGroup(DisplayGroupNIC nic) { start(); }
    public virtual void endDisplayGroup() { end(); }
    public virtual void startScroll() { start(); }
    public virtual void endScroll() { end(); }
    public virtual void startLink(Address addr) { start(); }
    public virtual void endLink() { end(); }
    public virtual void startMarginalia() { start(); }
    public virtual void endMarginalia() { end(); }
    public virtual void startMultiMode(MultiMode? principalPort, System.Collections.Generic.List<MultiMode> namedPorts, out System.Collections.Generic.List<FOTBuilder> builders)
    {
        builders = new System.Collections.Generic.List<FOTBuilder>();
        start();
    }
    public virtual void endMultiMode() { end(); }
    public virtual void startScore(Char c) { start(); }
    public virtual void startScore(LengthSpec length) { start(); }
    public virtual void startScore(Symbol sym) { start(); }
    public virtual void endScore() { end(); }
    public virtual void startLeader(LeaderNIC nic) { start(); }
    public virtual void endLeader() { end(); }
    public virtual void startSideline() { start(); }
    public virtual void endSideline() { end(); }
    public virtual void startBox(BoxNIC nic) { start(); }
    public virtual void endBox() { end(); }

    // Tables
    public virtual void startTable(TableNIC nic) { start(); }
    public virtual void endTable() { end(); }
    public virtual void tableBeforeRowBorder() { }
    public virtual void tableAfterRowBorder() { }
    public virtual void tableBeforeColumnBorder() { }
    public virtual void tableAfterColumnBorder() { }
    public virtual void startTablePart(TablePartNIC nic, out FOTBuilder? header, out FOTBuilder? footer)
    {
        header = null;
        footer = null;
        start();
    }
    public virtual void endTablePart() { end(); }
    public virtual void tableColumn(TableColumnNIC nic) { }
    public virtual void startTableRow() { start(); }
    public virtual void endTableRow() { end(); }
    public virtual void startTableCell(TableCellNIC nic) { start(); }
    public virtual void endTableCell() { end(); }
    public virtual void tableCellBeforeRowBorder() { }
    public virtual void tableCellAfterRowBorder() { }
    public virtual void tableCellBeforeColumnBorder() { }
    public virtual void tableCellAfterColumnBorder() { }

    // Math
    public virtual void startMathSequence() { start(); }
    public virtual void endMathSequence() { end(); }
    public virtual void startFraction(out FOTBuilder? numerator, out FOTBuilder? denominator)
    {
        numerator = null;
        denominator = null;
        start();
    }
    public virtual void fractionBar() { }
    public virtual void endFraction() { end(); }
    public virtual void startUnmath() { start(); }
    public virtual void endUnmath() { end(); }
    public virtual void startSuperscript() { start(); }
    public virtual void endSuperscript() { end(); }
    public virtual void startSubscript() { start(); }
    public virtual void endSubscript() { end(); }
    public virtual void startScript(out FOTBuilder? preSup, out FOTBuilder? preSub, out FOTBuilder? postSup, out FOTBuilder? postSub, out FOTBuilder? midSup, out FOTBuilder? midSub)
    {
        preSup = null; preSub = null; postSup = null; postSub = null; midSup = null; midSub = null;
        start();
    }
    public virtual void endScript() { end(); }
    public virtual void startMark(out FOTBuilder? overMark, out FOTBuilder? underMark)
    {
        overMark = null;
        underMark = null;
        start();
    }
    public virtual void endMark() { end(); }
    public virtual void startFence(out FOTBuilder? open, out FOTBuilder? close)
    {
        open = null;
        close = null;
        start();
    }
    public virtual void endFence() { end(); }
    public virtual void startRadical(out FOTBuilder? degree)
    {
        degree = null;
        start();
    }
    public virtual void radicalRadical(CharacterNIC nic) { }
    public virtual void radicalRadicalDefaulted() { }
    public virtual void endRadical() { end(); }
    public virtual void startMathOperator(out FOTBuilder? oper, out FOTBuilder? lowerLimit, out FOTBuilder? upperLimit)
    {
        oper = null;
        lowerLimit = null;
        upperLimit = null;
        start();
    }
    public virtual void endMathOperator() { end(); }

    // Grid
    public virtual void startGrid(GridNIC nic) { start(); }
    public virtual void endGrid() { end(); }
    public virtual void startGridCell(GridCellNIC nic) { start(); }
    public virtual void endGridCell() { end(); }

    // Simple page
    public const int nHF = 24;  // 4 page types × 6 header/footer parts
    public virtual void startSimplePageSequence(FOTBuilder?[] headerFooter)
    {
        // Default: all header/footer content goes to this builder
        for (int i = 0; i < nHF; i++)
            headerFooter[i] = this;
        start();
    }
    public virtual void endSimplePageSequenceHeaderFooter() { }
    public virtual void endSimplePageSequence() { end(); }

    // Simple page sequence serial form (for output backends)
    public virtual void startSimplePageSequenceSerial() { start(); }
    public virtual void endSimplePageSequenceSerial() { end(); }
    public virtual void startSimplePageSequenceHeaderFooter(uint flags) { start(); }
    public virtual void endSimplePageSequenceHeaderFooter(uint flags) { end(); }
    public virtual void endAllSimplePageSequenceHeaderFooter() { }

    // Multi-mode serial form
    public virtual void startMultiModeSerial(MultiMode? principalMode) { start(); }
    public virtual void endMultiModeSerial() { end(); }
    public virtual void startMultiModeMode(MultiMode mode) { start(); }
    public virtual void endMultiModeMode() { end(); }

    // Math flow objects serial form
    public virtual void startFractionSerial() { start(); }
    public virtual void endFractionSerial() { end(); }
    public virtual void startFractionNumerator() { start(); }
    public virtual void endFractionNumerator() { end(); }
    public virtual void startFractionDenominator() { start(); }
    public virtual void endFractionDenominator() { end(); }

    public virtual void startScriptSerial() { start(); }
    public virtual void endScriptSerial() { end(); }
    public virtual void startScriptPreSup() { start(); }
    public virtual void endScriptPreSup() { end(); }
    public virtual void startScriptPreSub() { start(); }
    public virtual void endScriptPreSub() { end(); }
    public virtual void startScriptPostSup() { start(); }
    public virtual void endScriptPostSup() { end(); }
    public virtual void startScriptPostSub() { start(); }
    public virtual void endScriptPostSub() { end(); }
    public virtual void startScriptMidSup() { start(); }
    public virtual void endScriptMidSup() { end(); }
    public virtual void startScriptMidSub() { start(); }
    public virtual void endScriptMidSub() { end(); }

    public virtual void startMarkSerial() { start(); }
    public virtual void endMarkSerial() { end(); }
    public virtual void startMarkOver() { start(); }
    public virtual void endMarkOver() { end(); }
    public virtual void startMarkUnder() { start(); }
    public virtual void endMarkUnder() { end(); }

    public virtual void startFenceSerial() { start(); }
    public virtual void endFenceSerial() { end(); }
    public virtual void startFenceOpen() { start(); }
    public virtual void endFenceOpen() { end(); }
    public virtual void startFenceClose() { start(); }
    public virtual void endFenceClose() { end(); }

    public virtual void startRadicalSerial() { start(); }
    public virtual void endRadicalSerial() { end(); }
    public virtual void startRadicalDegree() { start(); }
    public virtual void endRadicalDegree() { end(); }

    public virtual void startMathOperatorSerial() { start(); }
    public virtual void endMathOperatorSerial() { end(); }
    public virtual void startMathOperatorOperator() { start(); }
    public virtual void endMathOperatorOperator() { end(); }
    public virtual void startMathOperatorLowerLimit() { start(); }
    public virtual void endMathOperatorLowerLimit() { end(); }
    public virtual void startMathOperatorUpperLimit() { start(); }
    public virtual void endMathOperatorUpperLimit() { end(); }

    // Table part serial form
    public virtual void startTablePartSerial(TablePartNIC nic) { start(); }
    public virtual void endTablePartSerial() { end(); }
    public virtual void startTablePartHeader() { start(); }
    public virtual void endTablePartHeader() { end(); }
    public virtual void startTablePartFooter() { start(); }
    public virtual void endTablePartFooter() { end(); }

    // Page sequence
    public virtual void startPageSequence() { start(); }
    public virtual void endPageSequence() { end(); }

    // Column set sequence
    public virtual void startColumnSetSequence() { start(); }
    public virtual void endColumnSetSequence() { end(); }

    // Extension flow objects - default to atomic
    public virtual void extension(StringC extension, StringC publicId) { atomic(); }
    public virtual void startExtension(StringC extension, StringC publicId) { start(); }
    public virtual void endExtension() { end(); }

    // Inherited characteristics setting methods - all empty by default
    public virtual void setFontSize(long size) { }
    public virtual void setFontFamilyName(StringC name) { }
    public virtual void setFontWeight(Symbol weight) { }
    public virtual void setFontPosture(Symbol posture) { }
    public virtual void setStartIndent(LengthSpec indent) { }
    public virtual void setEndIndent(LengthSpec indent) { }
    public virtual void setFirstLineStartIndent(LengthSpec indent) { }
    public virtual void setLastLineEndIndent(LengthSpec indent) { }
    public virtual void setLineSpacing(LengthSpec spacing) { }
    public virtual void setFieldWidth(LengthSpec width) { }
    public virtual void setMarginaliaSep(LengthSpec sep) { }
    public virtual void setLines(Symbol lines) { }
    public virtual void setQuadding(Symbol quadding) { }
    public virtual void setDisplayAlignment(Symbol alignment) { }
    public virtual void setFieldAlign(Symbol align) { }
    public virtual void setColor(DeviceRGBColor color) { }
    public virtual void setBackgroundColor() { } // background-color: #f
    public virtual void setBackgroundColor(DeviceRGBColor color) { }
    public virtual void setBorderPresent(bool present) { }
    public virtual void setLineThickness(long thickness) { }
    public virtual void setCellBeforeRowMargin(LengthSpec margin) { }
    public virtual void setCellAfterRowMargin(LengthSpec margin) { }
    public virtual void setCellBeforeColumnMargin(LengthSpec margin) { }
    public virtual void setCellAfterColumnMargin(LengthSpec margin) { }
    public virtual void setLineSep(LengthSpec sep) { }
    public virtual void setBoxSizeBefore(LengthSpec size) { }
    public virtual void setBoxSizeAfter(LengthSpec size) { }
    // Length (long) overloads for above
    public virtual void setCellBeforeRowMargin(long margin) { }
    public virtual void setCellAfterRowMargin(long margin) { }
    public virtual void setCellBeforeColumnMargin(long margin) { }
    public virtual void setCellAfterColumnMargin(long margin) { }
    public virtual void setLineSep(long sep) { }
    public virtual void setBoxSizeBefore(long size) { }
    public virtual void setBoxSizeAfter(long size) { }
    public virtual void setPositionPointShift(LengthSpec shift) { }
    public virtual void setStartMargin(LengthSpec margin) { }
    public virtual void setEndMargin(LengthSpec margin) { }
    public virtual void setSidelineSep(LengthSpec sep) { }
    public virtual void setAsisWrapIndent(LengthSpec indent) { }
    public virtual void setLineNumberSep(LengthSpec sep) { }
    public virtual void setLastLineJustifyLimit(LengthSpec limit) { }
    public virtual void setJustifyGlyphSpaceMaxAdd(LengthSpec add) { }
    public virtual void setJustifyGlyphSpaceMaxRemove(LengthSpec remove) { }
    public virtual void setTableCornerRadius(LengthSpec radius) { }
    public virtual void setBoxCornerRadius(LengthSpec radius) { }
    public virtual void setInhibitLineBreaks(bool inhibit) { }
    public virtual void setHyphenate(bool hyphenate) { }
    public virtual void setKern(bool kern) { }
    public virtual void setLigature(bool ligature) { }
    public virtual void setScoreSpaces(bool score) { }
    public virtual void setFloatOutMarginalia(bool @float) { }
    public virtual void setFloatOutSidelines(bool @float) { }
    public virtual void setFloatOutLineNumbers(bool @float) { }
    public virtual void setCellBackground(bool background) { }
    public virtual void setSpanWeak(bool weak) { }
    public virtual void setIgnoreRecordEnd(bool ignore) { }
    public virtual void setNumberedLines(bool numbered) { }
    public virtual void setHangingPunct(bool hanging) { }
    public virtual void setBoxOpenEnd(bool open) { }
    public virtual void setTruncateLeader(bool truncate) { }
    public virtual void setAlignLeader(bool align) { }
    public virtual void setTablePartOmitMiddleHeader(bool omit) { }
    public virtual void setTablePartOmitMiddleFooter(bool omit) { }
    public virtual void setBorderOmitAtBreak(bool omit) { }
    public virtual void setPrincipalModeSimultaneous(bool simultaneous) { }
    public virtual void setMarginaliaKeepWithPrevious(bool keep) { }
    public virtual void setGridEquidistantRows(bool equidistant) { }
    public virtual void setGridEquidistantColumns(bool equidistant) { }
    public virtual void setLineJoin(Symbol join) { }
    public virtual void setLineCap(Symbol cap) { }
    public virtual void setLineMiterLimit(double limit) { }
    public virtual void setLineDash(System.Collections.Generic.List<LengthSpec> dashes, LengthSpec offset) { }
    public virtual void setLineDash(System.Collections.Generic.List<long> dashes, long offset) { }
    public virtual void setLineRepeat(uint repeat) { }
    public virtual void setLineRepeat(long repeat) { }
    public virtual void setGlyphAlignmentMode(Symbol mode) { }
    public virtual void setInputWhitespaceTreatment(Symbol treatment) { }
    public virtual void setFillingDirection(Symbol direction) { }
    public virtual void setWritingMode(Symbol mode) { }
    public virtual void setLastLineQuadding(Symbol quadding) { }
    public virtual void setMathDisplayMode(Symbol mode) { }
    public virtual void setScriptPreAlign(Symbol align) { }
    public virtual void setScriptPostAlign(Symbol align) { }
    public virtual void setScriptMidSupAlign(Symbol align) { }
    public virtual void setScriptMidSubAlign(Symbol align) { }
    public virtual void setNumeratorAlign(Symbol align) { }
    public virtual void setDenominatorAlign(Symbol align) { }
    public virtual void setGridPositionCellType(Symbol type) { }
    public virtual void setGridColumnAlignment(Symbol alignment) { }
    public virtual void setGridRowAlignment(Symbol alignment) { }
    public virtual void setBoxType(Symbol type) { }
    public virtual void setGlyphSubstTable(ConstPtr<GlyphSubstTable> table) { }
    public virtual void setGlyphReorderMethod(Symbol method) { }
    public virtual void setGlyphReorderMethod(string? pubid) { }
    public virtual void setHyphenationKeep(Symbol keep) { }
    public virtual void setFontStructure(Symbol structure) { }
    public virtual void setFontProportionateWidth(Symbol width) { }
    public virtual void setCjkStyle(Symbol style) { }
    public virtual void setBorderAlignment(Symbol alignment) { }
    public virtual void setSidelineSide(Symbol side) { }
    public virtual void setHyphenationLadderCount(Symbol count) { }
    public virtual void setHyphenationLadderCount(long count) { }
    public virtual void setBackgroundLayer(long layer) { }
    public virtual void setBorderPriority(long priority) { }
    public virtual void setLineRepeatCount(long count) { }
    public virtual void setSpan(long span) { }
    public virtual void setMinLeaderRepeat(long repeat) { }
    public virtual void setHyphenationRemainCharCount(long count) { }
    public virtual void setHyphenationPushCharCount(long count) { }
    public virtual void setWidowCount(long count) { }
    public virtual void setOrphanCount(long count) { }
    public virtual void setExpandTabs(long tabs) { }
    public virtual void setHyphenationChar(Char c) { }
    public virtual void setLanguage(Letter2 lang) { }
    public virtual void setCountry(Letter2 country) { }
    public virtual void setEscapementSpaceBefore(InlineSpace space) { }
    public virtual void setEscapementSpaceAfter(InlineSpace space) { }
    public virtual void setInlineSpaceSpace(OptInlineSpace space) { }
    public virtual void setGlyphSubstTableX(System.Collections.Generic.List<ConstPtr<GlyphSubstTable>> tables) { }

    // Spacing setters
    public virtual void setMinPreLineSpacing(OptLengthSpec spacing) { }
    public virtual void setMinPostLineSpacing(OptLengthSpec spacing) { }
    public virtual void setMinLeading(OptLengthSpec leading) { }

    // Line styling setters
    public virtual void setLineNumberSide(Symbol side) { }
    public virtual void setKernMode(Symbol mode) { }

    // Box/Cell/Border setters
    public virtual void setBoxBorderAlignment(Symbol alignment) { }
    public virtual void setCellRowAlignment(Symbol alignment) { }
    public virtual void setCellCrossed(Symbol crossed) { }
    public virtual void setMarginaliaSide(Symbol side) { }

    // Layer setter
    public virtual void setLayer(long layer) { }

    // PublicId-based setters (methods that use public identifiers)
    public virtual void setBackgroundTile(string? pubid) { }
    public virtual void setLineBreakingMethod(string? pubid) { }
    public virtual void setLineCompositionMethod(string? pubid) { }
    public virtual void setImplicitBidiMethod(string? pubid) { }
    public virtual void setGlyphSubstMethod(string? pubid) { }
    public virtual void setHyphenationMethod(string? pubid) { }
    public virtual void setTableAutoWidthMethod(string? pubid) { }
    public virtual void setFontName(string? name) { }

    // Page and margin setters
    public virtual void setPageWidth(long width) { }
    public virtual void setPageHeight(long height) { }
    public virtual void setLeftMargin(long margin) { }
    public virtual void setRightMargin(long margin) { }
    public virtual void setTopMargin(long margin) { }
    public virtual void setBottomMargin(long margin) { }
    public virtual void setHeaderMargin(long margin) { }
    public virtual void setFooterMargin(long margin) { }

    // Symbol name lookup - converts Symbol enum to DSSSL symbol name
    public static string? symbolName(Symbol sym)
    {
        if (sym == Symbol.symbolFalse || sym == Symbol.symbolTrue)
            return null;
        // Map Symbol enum values to DSSSL hyphenated symbol names
        string name = sym.ToString();
        if (name.StartsWith("symbol"))
            name = name.Substring(6);
        // Convert PascalCase to kebab-case (e.g., "UltraCondensed" -> "ultra-condensed")
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (i > 0 && char.IsUpper(c))
            {
                sb.Append('-');
                sb.Append(char.ToLower(c));
            }
            else
            {
                sb.Append(char.ToLower(c));
            }
        }
        return sb.ToString();
    }

    // Utility for 2-letter ISO codes
    public struct Letter2
    {
        public ushort value;
        public Letter2(char c1, char c2) { value = (ushort)((c1 << 8) | c2); }
        public Letter2(ushort v) { value = v; }
    }

    // Extension flow object interface
    public class ExtensionFlowObj
    {
        public virtual ExtensionFlowObj copy() { return new ExtensionFlowObj(); }
        public virtual CompoundExtensionFlowObj? asCompoundExtensionFlowObj() { return null; }
        public virtual bool hasNIC(StringC name) { return false; }
        public virtual void setNIC(StringC name, IExtensionFlowObjValue value) { }
    }

    // Compound extension flow object interface
    public class CompoundExtensionFlowObj : ExtensionFlowObj
    {
        public override ExtensionFlowObj copy() { return new CompoundExtensionFlowObj(); }
        public override CompoundExtensionFlowObj? asCompoundExtensionFlowObj() { return this; }
        public virtual bool hasPrincipalPort() { return true; }
        public virtual void portNames(System.Collections.Generic.List<StringC> names) { }
    }

    // Extension table entry (for FOT builder extension registration)
    public class ExtensionTableEntry
    {
        public string? pubid;
        public ExtensionFlowObj? flowObj;
    }

    // Extension flow object methods
    public virtual void extension(ExtensionFlowObj fo, NodePtr currentNode) { atomic(); }
    public virtual void startExtension(ExtensionFlowObj fo, NodePtr currentNode, System.Collections.Generic.List<FOTBuilder?> fotbs)
    {
        start();
    }
    public virtual void endExtension(ExtensionFlowObj fo) { end(); }
}

// Note: Collector is defined in Collector.cs
