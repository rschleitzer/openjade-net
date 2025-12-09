// Copyright (c) 1996, 1997 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Jade;

using OpenSP;
using OpenJade.Style;
using OpenJade.Grove;
using System.Text;
using Char = System.UInt32;
using Boolean = System.Boolean;

// SGML/XML FOT Builder - produces XML representation of the flow object tree
public class SgmlFOTBuilder : FOTBuilder
{
    private OutputCharStream os_;
    private StringBuilder ics_; // inherited characteristics string buffer
    private uint nodeLevel_;
    private System.Collections.Generic.List<NodePtr> pendingElements_;
    private System.Collections.Generic.List<uint> pendingElementLevels_;
    private uint nPendingElementsNonEmpty_;
    private bool suppressAnchors_;

    // Header/footer buffering (from C++ hfs_ and hf_)
    private StrOutputCharStream hfs_; // header/footer stream buffer
    private OutputCharStream curOs_; // current output stream
    private StringC[] hf_; // array of header/footer content by flags

    // SaveFOTBuilder stack for serial flow object processing (from C++ SerialFOTBuilder)
    private System.Collections.Generic.Stack<SaveFOTBuilder> save_ = new();

    // Multi-mode tracking
    private System.Collections.Generic.List<bool> multiModeHasPrincipalMode_ = new();

    private const char RE = '\r';
    private const char quot = '"';
    private const string trueString = "true";
    private const string falseString = "false";

    // Header/footer flag constants from C++ FOTBuilder.h
    private const int firstHF = 1;    // 01
    private const int otherHF = 0;
    private const int frontHF = 2;    // 02
    private const int backHF = 0;
    private const int headerHF = 4;   // 04
    private const int footerHF = 0;
    private const int leftHF_ = 0;     // Local constants for HF output
    private const int centerHF_ = 8;   // 010
    private const int rightHF_ = 16;   // 020

    public SgmlFOTBuilder(OutputCharStream outputStream)
    {
        os_ = outputStream;
        ics_ = new StringBuilder();
        nodeLevel_ = 0;
        pendingElements_ = new System.Collections.Generic.List<NodePtr>();
        pendingElementLevels_ = new System.Collections.Generic.List<uint>();
        nPendingElementsNonEmpty_ = 0;
        suppressAnchors_ = false;
        hfs_ = new StrOutputCharStream();
        curOs_ = outputStream;  // Initially point to main output stream
        hf_ = new StringC[nHF];
        for (int i = 0; i < nHF; i++)
            hf_[i] = new StringC();

        os().put((Char)'<').put((Char)'?').write("xml version=\"1.0\"");
        os().put((Char)'?').put((Char)'>').put((Char)RE);
        os().put((Char)'<').write("fot").put((Char)'>').put((Char)RE);
    }

    // Returns current output stream (either main os_ or header/footer buffer hfs_)
    private OutputCharStream os() { return curOs_; }

    public void close()
    {
        os().put((Char)'<').put((Char)'/').write("fot").put((Char)'>').put((Char)RE);
        os().flush();
    }

    public override void characters(Char[] data, nuint size)
    {
        if (size == 0)
            return;
        flushPendingElements();
        os().put((Char)'<').write("text").put((Char)'>');
        writeEscapedData(data, size);
        os().put((Char)'<').put((Char)'/').write("text").put((Char)'>').put((Char)RE);
    }

    public override void startParagraph(ParagraphNIC nic)
    {
        os().put((Char)'<').write("paragraph");
        displayNIC(nic);
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    public override void endParagraph()
    {
        endFlow("paragraph");
    }

    public override void startSequence()
    {
        startSimpleFlowObj("sequence");
    }

    public override void endSequence()
    {
        endFlow("sequence");
    }

    public override void startDisplayGroup(DisplayGroupNIC nic)
    {
        os().put((Char)'<').write("display-group");
        if (nic.hasCoalesceId)
        {
            os().write(" coalesce-id=").put((Char)quot);
            writeEscapedData(nic.coalesceId.data()!, nic.coalesceId.size());
            os().put((Char)quot);
        }
        displayNIC(nic);
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    public override void endDisplayGroup()
    {
        endFlow("display-group");
    }

    public override void startScroll()
    {
        startSimpleFlowObj("scroll");
    }

    public override void endScroll()
    {
        endFlow("scroll");
    }

    public override void startBox(BoxNIC nic)
    {
        flushPendingElements();
        os().put((Char)'<').write("box");
        if (nic.isDisplay)
        {
            os().write(" display=").put((Char)quot).write(trueString).put((Char)quot);
            displayNIC(nic);
        }
        else
            inlineNIC(nic.breakBeforePriority, nic.breakAfterPriority);
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    public override void endBox()
    {
        endFlow("box");
    }

    public override void startTable(TableNIC nic)
    {
        flushPendingElements();
        os().put((Char)'<').write("table");
        switch (nic.widthType)
        {
            case TableNIC.WidthType.widthExplicit:
                os().write(" width=").put((Char)quot);
                writeLengthSpec(nic.width);
                os().put((Char)quot);
                break;
            case TableNIC.WidthType.widthMinimum:
                os().write(" minimum-width=").put((Char)quot).write(trueString).put((Char)quot);
                break;
        }
        displayNIC(nic);
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    public override void endTable()
    {
        endFlow("table");
    }

    public override void startTableRow()
    {
        startSimpleFlowObj("table-row");
    }

    public override void endTableRow()
    {
        endFlow("table-row");
    }

    public override void startTableCell(TableCellNIC nic)
    {
        os().put((Char)'<').write("table-cell column-number=").put((Char)quot);
        if (nic.missing)
            os().put((Char)'0');
        else
        {
            os().write((nic.columnIndex + 1).ToString());
            if (nic.nColumnsSpanned != 1)
            {
                os().put((Char)quot).write(" n-columns-spanned=").put((Char)quot);
                os().write(nic.nColumnsSpanned.ToString());
            }
            if (nic.nRowsSpanned != 1)
            {
                os().put((Char)quot).write(" n-rows-spanned=").put((Char)quot);
                os().write(nic.nRowsSpanned.ToString());
            }
        }
        os().put((Char)quot);
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    public override void endTableCell()
    {
        endFlow("table-cell");
    }

    public override void tableColumn(TableColumnNIC nic)
    {
        os().put((Char)'<').write("table-column column-number=").put((Char)quot);
        os().write((nic.columnIndex + 1).ToString()).put((Char)quot);
        if (nic.nColumnsSpanned != 1)
        {
            os().write(" n-columns-spanned=").put((Char)quot);
            os().write(nic.nColumnsSpanned.ToString()).put((Char)quot);
        }
        if (nic.hasWidth)
        {
            os().write(" width=").put((Char)quot);
            writeTableLengthSpec(nic.width);
            os().put((Char)quot);
        }
        outputIcs();
        os().write("/>").put((Char)RE);
    }

    public override void externalGraphic(ExternalGraphicNIC nic)
    {
        flushPendingElements();
        os().put((Char)'<').write("external-graphic entity-system-id=").put((Char)quot);
        writeEscapedData(nic.entitySystemId.data()!, nic.entitySystemId.size());
        os().put((Char)quot).write(" notation-system-id=").put((Char)quot);
        writeEscapedData(nic.notationSystemId.data()!, nic.notationSystemId.size());
        os().put((Char)quot);
        if (nic.scaleType != Symbol.symbolFalse)
        {
            os().write(" scale=").put((Char)quot);
            writeSymbol(nic.scaleType);
            os().put((Char)quot);
        }
        else
        {
            os().write(" scale-x=").put((Char)quot).write(nic.scale[0].ToString()).put((Char)quot);
            os().write(" scale-y=").put((Char)quot).write(nic.scale[1].ToString()).put((Char)quot);
        }
        if (nic.hasMaxWidth)
        {
            os().write(" max-width=").put((Char)quot);
            writeLengthSpec(nic.maxWidth);
            os().put((Char)quot);
        }
        if (nic.hasMaxHeight)
        {
            os().write(" max-height=").put((Char)quot);
            writeLengthSpec(nic.maxHeight);
            os().put((Char)quot);
        }
        if (nic.isDisplay)
        {
            os().write(" display=").put((Char)quot).write(trueString).put((Char)quot);
            displayNIC(nic);
        }
        else
        {
            if (nic.escapementDirection != Symbol.symbolFalse)
            {
                os().write(" escapement-direction=").put((Char)quot);
                writeSymbol(nic.escapementDirection);
                os().put((Char)quot);
            }
            inlineNIC(nic.breakBeforePriority, nic.breakAfterPriority);
        }
        outputIcs();
        os().write("/>").put((Char)RE);
    }

    public override void rule(RuleNIC nic)
    {
        flushPendingElements();
        string? s = symbolName(nic.orientation);
        if (s == null)
            return;
        os().put((Char)'<').write("rule orientation=").put((Char)quot).write(s).put((Char)quot);
        switch (nic.orientation)
        {
            case Symbol.symbolHorizontal:
            case Symbol.symbolVertical:
                displayNIC(nic);
                break;
            default:
                inlineNIC(nic.breakBeforePriority, nic.breakAfterPriority);
                break;
        }
        if (nic.hasLength)
        {
            os().write(" length=").put((Char)quot);
            writeLengthSpec(nic.length);
            os().put((Char)quot);
        }
        outputIcs();
        os().write("/>").put((Char)RE);
    }

    public override void alignmentPoint()
    {
        simpleFlowObj("alignment-point");
    }

    public override void character(CharacterNIC nic)
    {
        flushPendingElements();
        os().put((Char)'<').write("character");
        characterNIC(nic);
        outputIcs();
        os().write("/>").put((Char)RE);
    }

    public override void paragraphBreak(ParagraphNIC nic)
    {
        os().put((Char)'<').write("paragraph-break");
        displayNIC(nic);
        outputIcs();
        os().write("/>").put((Char)RE);
    }

    public override void startLink(Address addr)
    {
        os().put((Char)'<').write("link");
        outputIcs();
        switch (addr.type)
        {
            case Address.Type.resolvedNode:
                os().write(" destination=").put((Char)quot);
                outputElementName(addr.node);
                os().put((Char)quot);
                break;
            case Address.Type.idref:
                os().write(" destination=").put((Char)quot);
                outputElementName(addr.node.groveIndex(), addr.@params[0].data()!, addr.@params[0].size());
                os().put((Char)quot);
                break;
        }
        os().put((Char)'>').put((Char)RE);
    }

    public override void endLink()
    {
        endFlow("link");
    }

    public override void startMarginalia()
    {
        startSimpleFlowObj("marginalia");
    }

    public override void endMarginalia()
    {
        endFlow("marginalia");
    }

    public override void startLeader(LeaderNIC nic)
    {
        flushPendingElements();
        os().put((Char)'<').write("leader");
        if (nic.hasLength)
        {
            os().write(" length=");
            writeLengthSpec(nic.length);
        }
        inlineNIC(nic);
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    public override void endLeader()
    {
        endFlow("leader");
    }

    public override void startSideline()
    {
        startSimpleFlowObj("sideline");
    }

    public override void endSideline()
    {
        endFlow("sideline");
    }

    public override void startLineField(LineFieldNIC nic)
    {
        flushPendingElements();
        startSimpleFlowObj("line-field");
    }

    public override void endLineField()
    {
        endFlow("line-field");
    }

    public override void startScore(Char c)
    {
        os().put((Char)'<').write("score type=\"char\" char=").put((Char)quot);
        os().put(c);
        os().put((Char)quot);
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    public override void startScore(LengthSpec length)
    {
        os().put((Char)'<').write("score type=").put((Char)quot);
        writeLengthSpec(length);
        os().put((Char)quot);
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    public override void startScore(Symbol sym)
    {
        string? s = symbolName(sym);
        if (s != null)
        {
            os().put((Char)'<').write("score type=").put((Char)quot).write(s).put((Char)quot);
            outputIcs();
            os().put((Char)'>').put((Char)RE);
        }
    }

    public override void endScore()
    {
        endFlow("score");
    }

    public override void startMathSequence()
    {
        startSimpleFlowObj("math-sequence");
    }

    public override void endMathSequence()
    {
        endFlow("math-sequence");
    }

    public override void startSuperscript()
    {
        startSimpleFlowObj("superscript");
    }

    public override void endSuperscript()
    {
        endFlow("superscript");
    }

    public override void startSubscript()
    {
        startSimpleFlowObj("subscript");
    }

    public override void endSubscript()
    {
        endFlow("subscript");
    }

    public override void startUnmath()
    {
        startSimpleFlowObj("unmath");
    }

    public override void endUnmath()
    {
        endFlow("unmath");
    }

    public override void startGrid(GridNIC nic)
    {
        os().put((Char)'<').write("grid");
        if (nic.nColumns != 0)
        {
            os().write(" grid-n-columns=").put((Char)quot);
            os().write(nic.nColumns.ToString()).put((Char)quot);
        }
        if (nic.nRows != 0)
        {
            os().write(" grid-n-rows=").put((Char)quot);
            os().write(nic.nRows.ToString()).put((Char)quot);
        }
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    public override void endGrid()
    {
        endFlow("grid");
    }

    public override void startGridCell(GridCellNIC nic)
    {
        os().put((Char)'<').write("grid-cell");
        if (nic.columnNumber != 0)
        {
            os().write(" column-number=").put((Char)quot);
            os().write(nic.columnNumber.ToString()).put((Char)quot);
        }
        if (nic.rowNumber != 0)
        {
            os().write(" row-number=").put((Char)quot);
            os().write(nic.rowNumber.ToString()).put((Char)quot);
        }
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    public override void endGridCell()
    {
        endFlow("grid-cell");
    }

    public override void startNode(NodePtr node, StringC processingMode)
    {
        nodeLevel_++;
        if (processingMode.size() != 0 || !nodeIsElement(node))
            return;
        for (int i = 0; i < pendingElements_.Count; i++)
            if (pendingElements_[i].Equals(node))
                return;
        pendingElements_.Add(node);
        pendingElementLevels_.Add(nodeLevel_);
    }

    public override void endNode()
    {
        if (pendingElements_.Count > 0 && pendingElementLevels_[pendingElementLevels_.Count - 1] == nodeLevel_
            && nPendingElementsNonEmpty_ < (uint)pendingElements_.Count)
        {
            pendingElementLevels_.RemoveAt(pendingElements_.Count - 1);
            pendingElements_.RemoveAt(pendingElements_.Count - 1);
        }
        nodeLevel_--;
    }

    // Inherited characteristics setters
    public override void setFontSize(long size)
    {
        lengthC("font-size", size);
    }

    public override void setFontFamilyName(StringC name)
    {
        ics_.Append(" font-family-name=\"");
        for (nuint i = 0; i < name.size(); i++)
            ics_.Append((char)name[i]);
        ics_.Append('"');
    }

    public override void setFontWeight(Symbol weight)
    {
        symbolC("font-weight", weight);
    }

    public override void setFontPosture(Symbol posture)
    {
        symbolC("font-posture", posture);
    }

    public override void setStartIndent(LengthSpec indent)
    {
        lengthSpecC("start-indent", indent);
    }

    public override void setEndIndent(LengthSpec indent)
    {
        lengthSpecC("end-indent", indent);
    }

    public override void setFirstLineStartIndent(LengthSpec indent)
    {
        lengthSpecC("first-line-start-indent", indent);
    }

    public override void setLastLineEndIndent(LengthSpec indent)
    {
        lengthSpecC("last-line-end-indent", indent);
    }

    public override void setLineSpacing(LengthSpec spacing)
    {
        lengthSpecC("line-spacing", spacing);
    }

    public override void setLines(Symbol lines)
    {
        symbolC("lines", lines);
    }

    public override void setQuadding(Symbol quadding)
    {
        symbolC("quadding", quadding);
    }

    public override void setDisplayAlignment(Symbol alignment)
    {
        symbolC("display-alignment", alignment);
    }

    public override void setColor(DeviceRGBColor color)
    {
        ics_.Append(" color=\"#");
        ics_.Append(color.red.ToString("X2"));
        ics_.Append(color.green.ToString("X2"));
        ics_.Append(color.blue.ToString("X2"));
        ics_.Append('"');
    }

    public override void setBackgroundColor()
    {
        ics_.Append(" background-color=\"false\"");
    }

    public override void setBackgroundColor(DeviceRGBColor color)
    {
        ics_.Append(" background-color=\"#");
        ics_.Append(color.red.ToString("X2"));
        ics_.Append(color.green.ToString("X2"));
        ics_.Append(color.blue.ToString("X2"));
        ics_.Append('"');
    }

    public override void setBorderPresent(bool present)
    {
        boolC("border-present", present);
    }

    public override void setLineThickness(long thickness)
    {
        lengthC("line-thickness", thickness);
    }

    public override void setInhibitLineBreaks(bool inhibit)
    {
        boolC("inhibit-line-breaks", inhibit);
    }

    public override void setHyphenate(bool hyphenate)
    {
        boolC("hyphenate", hyphenate);
    }

    public override void setKern(bool kern)
    {
        boolC("kern", kern);
    }

    public override void setLigature(bool ligature)
    {
        boolC("ligature", ligature);
    }

    public override void setScoreSpaces(bool score)
    {
        boolC("score-spaces", score);
    }

    public override void setLanguage(Letter2 lang)
    {
        ics_.Append(" language=\"");
        if (lang.value != 0)
        {
            ics_.Append((char)((lang.value >> 8) & 0xff));
            ics_.Append((char)(lang.value & 0xff));
        }
        else
            ics_.Append(falseString);
        ics_.Append('"');
    }

    public override void setCountry(Letter2 country)
    {
        ics_.Append(" country=\"");
        if (country.value != 0)
        {
            ics_.Append((char)((country.value >> 8) & 0xff));
            ics_.Append((char)(country.value & 0xff));
        }
        else
            ics_.Append(falseString);
        ics_.Append('"');
    }

    public override void setFieldWidth(LengthSpec ls) { lengthSpecC("field-width", ls); }
    public override void setPositionPointShift(LengthSpec ls) { lengthSpecC("position-point-shift", ls); }
    public override void setStartMargin(LengthSpec ls) { lengthSpecC("start-margin", ls); }
    public override void setEndMargin(LengthSpec ls) { lengthSpecC("end-margin", ls); }
    public override void setSidelineSep(LengthSpec ls) { lengthSpecC("sideline-sep", ls); }
    public override void setAsisWrapIndent(LengthSpec ls) { lengthSpecC("asis-wrap-indent", ls); }
    public override void setLineNumberSep(LengthSpec ls) { lengthSpecC("line-number-sep", ls); }
    public override void setLastLineJustifyLimit(LengthSpec ls) { lengthSpecC("last-line-justify-limit", ls); }
    public override void setJustifyGlyphSpaceMaxAdd(LengthSpec ls) { lengthSpecC("justify-glyph-space-max-add", ls); }
    public override void setJustifyGlyphSpaceMaxRemove(LengthSpec ls) { lengthSpecC("justify-glyph-space-max-remove", ls); }
    public override void setTableCornerRadius(LengthSpec ls) { lengthSpecC("table-corner-radius", ls); }
    public override void setBoxCornerRadius(LengthSpec ls) { lengthSpecC("box-corner-radius", ls); }
    public override void setMarginaliaSep(LengthSpec ls) { lengthSpecC("marginalia-sep", ls); }

    public override void setMinPreLineSpacing(OptLengthSpec ols) { optLengthSpecC("min-pre-line-spacing", ols); }
    public override void setMinPostLineSpacing(OptLengthSpec ols) { optLengthSpecC("min-post-line-spacing", ols); }
    public override void setMinLeading(OptLengthSpec ols) { optLengthSpecC("min-leading", ols); }

    public override void setFieldAlign(Symbol sym) { symbolC("field-align", sym); }
    public override void setLineJoin(Symbol sym) { symbolC("line-join", sym); }
    public override void setLineCap(Symbol sym) { symbolC("line-cap", sym); }
    public override void setLineNumberSide(Symbol sym) { symbolC("line-number-side", sym); }
    public override void setKernMode(Symbol sym) { symbolC("kern-mode", sym); }
    public override void setInputWhitespaceTreatment(Symbol sym) { symbolC("input-whitespace-treatment", sym); }
    public override void setFillingDirection(Symbol sym) { symbolC("filling-direction", sym); }
    public override void setWritingMode(Symbol sym) { symbolC("writing-mode", sym); }
    public override void setLastLineQuadding(Symbol sym) { symbolC("last-line-quadding", sym); }
    public override void setMathDisplayMode(Symbol sym) { symbolC("math-display-mode", sym); }
    public override void setScriptPreAlign(Symbol sym) { symbolC("script-pre-align", sym); }
    public override void setScriptPostAlign(Symbol sym) { symbolC("script-post-align", sym); }
    public override void setScriptMidSupAlign(Symbol sym) { symbolC("script-mid-sup-align", sym); }
    public override void setScriptMidSubAlign(Symbol sym) { symbolC("script-mid-sub-align", sym); }
    public override void setNumeratorAlign(Symbol sym) { symbolC("numerator-align", sym); }
    public override void setDenominatorAlign(Symbol sym) { symbolC("denominator-align", sym); }
    public override void setGridPositionCellType(Symbol sym) { symbolC("grid-position-cell-type", sym); }
    public override void setGridColumnAlignment(Symbol sym) { symbolC("grid-column-alignment", sym); }
    public override void setGridRowAlignment(Symbol sym) { symbolC("grid-row-alignment", sym); }
    public override void setBoxType(Symbol sym) { symbolC("box-type", sym); }
    public override void setGlyphAlignmentMode(Symbol sym) { symbolC("glyph-alignment-mode", sym); }
    public override void setBoxBorderAlignment(Symbol sym) { symbolC("box-border-alignment", sym); }
    public override void setCellRowAlignment(Symbol sym) { symbolC("cell-row-alignment", sym); }
    public override void setBorderAlignment(Symbol sym) { symbolC("border-alignment", sym); }
    public override void setSidelineSide(Symbol sym) { symbolC("sideline-side", sym); }
    public override void setHyphenationKeep(Symbol sym) { symbolC("hyphenation-keep", sym); }
    public override void setFontStructure(Symbol sym) { symbolC("font-structure", sym); }
    public override void setFontProportionateWidth(Symbol sym) { symbolC("font-proportionate-width", sym); }
    public override void setCellCrossed(Symbol sym) { symbolC("cell-crossed", sym); }
    public override void setMarginaliaSide(Symbol sym) { symbolC("marginalia-side", sym); }

    public override void setPageWidth(long units) { lengthC("page-width", units); }
    public override void setPageHeight(long units) { lengthC("page-height", units); }
    public override void setLeftMargin(long units) { lengthC("left-margin", units); }
    public override void setRightMargin(long units) { lengthC("right-margin", units); }
    public override void setTopMargin(long units) { lengthC("top-margin", units); }
    public override void setBottomMargin(long units) { lengthC("bottom-margin", units); }
    public override void setHeaderMargin(long units) { lengthC("header-margin", units); }
    public override void setFooterMargin(long units) { lengthC("footer-margin", units); }
    public override void setCellBeforeRowMargin(long units) { lengthC("cell-before-row-margin", units); }
    public override void setCellAfterRowMargin(long units) { lengthC("cell-after-row-margin", units); }
    public override void setCellBeforeColumnMargin(long units) { lengthC("cell-before-column-margin", units); }
    public override void setCellAfterColumnMargin(long units) { lengthC("cell-after-column-margin", units); }
    public override void setLineSep(long units) { lengthC("line-sep", units); }
    public override void setBoxSizeBefore(long units) { lengthC("box-size-before", units); }
    public override void setBoxSizeAfter(long units) { lengthC("box-size-after", units); }

    public override void setLayer(long n) { integerC("layer", n); }
    public override void setBackgroundLayer(long n) { integerC("background-layer", n); }
    public override void setBorderPriority(long n) { integerC("border-priority", n); }
    public override void setLineRepeat(long n) { integerC("line-repeat", n); }
    public override void setSpan(long n) { integerC("span", n); }
    public override void setMinLeaderRepeat(long n) { integerC("min-leader-repeat", n); }
    public override void setHyphenationRemainCharCount(long n) { integerC("hyphenation-remain-char-count", n); }
    public override void setHyphenationPushCharCount(long n) { integerC("hyphenation-push-char-count", n); }
    public override void setWidowCount(long n) { integerC("widow-count", n); }
    public override void setOrphanCount(long n) { integerC("orphan-count", n); }
    public override void setExpandTabs(long n) { integerC("expand-tabs", n); }
    public override void setHyphenationLadderCount(long n) { integerC("hyphenation-ladder-count", n); }

    public override void setFloatOutMarginalia(bool b) { boolC("float-out-marginalia", b); }
    public override void setFloatOutSidelines(bool b) { boolC("float-out-sidelines", b); }
    public override void setFloatOutLineNumbers(bool b) { boolC("float-out-line-numbers", b); }
    public override void setCellBackground(bool b) { boolC("cell-background", b); }
    public override void setSpanWeak(bool b) { boolC("span-weak", b); }
    public override void setIgnoreRecordEnd(bool b) { boolC("ignore-record-end", b); }
    public override void setNumberedLines(bool b) { boolC("numbered-lines", b); }
    public override void setHangingPunct(bool b) { boolC("hanging-punct", b); }
    public override void setBoxOpenEnd(bool b) { boolC("box-open-end", b); }
    public override void setTruncateLeader(bool b) { boolC("truncate-leader", b); }
    public override void setAlignLeader(bool b) { boolC("align-leader", b); }
    public override void setTablePartOmitMiddleHeader(bool b) { boolC("table-part-omit-middle-header", b); }
    public override void setTablePartOmitMiddleFooter(bool b) { boolC("table-part-omit-middle-footer", b); }
    public override void setBorderOmitAtBreak(bool b) { boolC("border-omit-at-break", b); }
    public override void setPrincipalModeSimultaneous(bool b) { boolC("principal-mode-simultaneous", b); }
    public override void setMarginaliaKeepWithPrevious(bool b) { boolC("marginalia-keep-with-previous", b); }
    public override void setGridEquidistantRows(bool b) { boolC("grid-equidistant-rows", b); }
    public override void setGridEquidistantColumns(bool b) { boolC("grid-equidistant-columns", b); }

    public override void setBackgroundTile(string? pubid) { publicIdC("background-tile", pubid); }
    public override void setLineBreakingMethod(string? pubid) { publicIdC("line-breaking-method", pubid); }
    public override void setLineCompositionMethod(string? pubid) { publicIdC("line-composition-method", pubid); }
    public override void setImplicitBidiMethod(string? pubid) { publicIdC("implicit-bidi-method", pubid); }
    public override void setGlyphSubstMethod(string? pubid) { publicIdC("glyph-subst-method", pubid); }
    public override void setGlyphReorderMethod(string? pubid) { publicIdC("glyph-reorder-method", pubid); }
    public override void setHyphenationMethod(string? pubid) { publicIdC("hyphenation-method", pubid); }
    public override void setTableAutoWidthMethod(string? pubid) { publicIdC("table-auto-width-method", pubid); }
    public override void setFontName(string? pubid) { publicIdC("font-name", pubid); }

    public override void setEscapementSpaceBefore(InlineSpace @is) { inlineSpaceC("escapement-space-before", @is); }
    public override void setEscapementSpaceAfter(InlineSpace @is) { inlineSpaceC("escapement-space-after", @is); }

    // Page sequence methods
    public override void startSimplePageSequence(FOTBuilder?[] headerFooter)
    {
        // Create SaveFOTBuilders for each header/footer slot (like C++ SerialFOTBuilder)
        for (int i = 0; i < nHF; i++)
        {
            var saveFotb = new SaveFOTBuilder();
            save_.Push(saveFotb);
            headerFooter[nHF - 1 - i] = saveFotb;
        }
        startSimplePageSequenceSerial();
    }

    public override void endSimplePageSequence()
    {
        endSimplePageSequenceSerial();
    }

    // SerialFOTBuilder pattern: retrieve buffered HF content and emit it
    public override void endSimplePageSequenceHeaderFooter()
    {
        // Retrieve all 24 SaveFOTBuilders from the stack
        SaveFOTBuilder[] hfBuilders = new SaveFOTBuilder[nHF];
        for (int k = 0; k < nHF; k++)
            hfBuilders[k] = save_.Pop();

        // Emit all 24 parts in the same order as C++ 1.2.1 for compatibility
        for (int i = 0; i < (1 << 2); i++)  // 4 page types
        {
            for (int j = 0; j < 6; j++)  // 6 header/footer parts
            {
                uint k = (uint)(i | (j << 2));
                startSimplePageSequenceHeaderFooter(k);
                hfBuilders[k].emit(this);
                endSimplePageSequenceHeaderFooter(k);
            }
        }
        endAllSimplePageSequenceHeaderFooter();
    }

    public override void startSimplePageSequenceSerial()
    {
        startSimpleFlowObj("simple-page-sequence");
        suppressAnchors_ = true;
        curOs_ = hfs_;  // Redirect output to header/footer stream buffer
    }

    public override void endSimplePageSequenceSerial()
    {
        endFlow("simple-page-sequence");
    }

    public override void startSimplePageSequenceHeaderFooter(uint flags)
    {
        // In C++: does nothing
    }

    public override void endSimplePageSequenceHeaderFooter(uint flags)
    {
        // Extract content from hfs_ to hf_[flags] (like C++: hfs_.extractString(hf_[flags]))
        hfs_.extractString(hf_[flags]);
    }

    public override void endAllSimplePageSequenceHeaderFooter()
    {
        // Restore output to main stream
        curOs_ = os_;
        suppressAnchors_ = false;
        // Output all collected header/footer content as simple-page-sequence.side-hf elements
        for (int i = 0; i < nHF; i += nHF / 6)
        {
            int front;
            if (!hf_[i + (firstHF | frontHF)].Equals(hf_[i + (firstHF | backHF)])
                || !hf_[i + (otherHF | frontHF)].Equals(hf_[i + (otherHF | backHF)]))
                front = frontHF;
            else
                front = 0;
            int first;
            if (!hf_[i + (firstHF | frontHF)].Equals(hf_[i + (otherHF | frontHF)])
                || !hf_[i + (firstHF | backHF)].Equals(hf_[i + (otherHF | backHF)]))
                first = firstHF;
            else
                first = 0;
            for (int j = 0; j <= front; j += (frontHF != 0 ? frontHF : int.MaxValue))
            {
                for (int k = 0; k <= first; k += (firstHF != 0 ? firstHF : int.MaxValue))
                {
                    StringC str = hf_[i + j + k];
                    if (str.size() != 0)
                    {
                        string side;
                        if ((i & centerHF_) != 0)
                            side = "center";
                        else if ((i & rightHF_) != 0)
                            side = "right";
                        else
                            side = "left";
                        string hf = ((i & headerHF) != 0) ? "header" : "footer";
                        os().put((Char)'<').write("simple-page-sequence.").write(side).put((Char)'-').write(hf);
                        if (front != 0)
                        {
                            os().write(" front=").put((Char)quot).write(boolString(j != 0)).put((Char)quot);
                        }
                        if (first != 0)
                        {
                            os().write(" first=").put((Char)quot).write(boolString(k != 0)).put((Char)quot);
                        }
                        os().put((Char)'>').put((Char)RE);
                        // Write the content
                        for (nuint ci = 0; ci < str.size(); ci++)
                            os().put(str.data()![ci]);
                        os().put((Char)'<').put((Char)'/').write("simple-page-sequence.").write(side).put((Char)'-').write(hf).put((Char)'>').put((Char)RE);
                    }
                }
            }
        }
        // Clear hf_ array for next use
        for (int i = 0; i < nHF; i++)
            hf_[i] = new StringC();
    }

    public override void pageNumber()
    {
        os().put((Char)'<').write("page-number/>");
        os().put((Char)RE);
    }

    // Multi-mode methods
    public override void startMultiModeSerial(MultiMode? principalMode)
    {
        startSimpleFlowObj("multi-mode");
        if (principalMode != null)
        {
            os().put((Char)'<').write("multi-mode.mode");
            if (principalMode.hasDesc)
            {
                os().write(" desc=").put((Char)quot);
                writeEscapedData(principalMode.desc.data()!, principalMode.desc.size());
                os().put((Char)quot);
            }
            os().put((Char)'>').put((Char)RE);
        }
        multiModeHasPrincipalMode_.Add(principalMode != null);
    }

    public override void endMultiModeSerial()
    {
        if (multiModeHasPrincipalMode_[multiModeHasPrincipalMode_.Count - 1])
            endFlow("multi-mode.mode");
        multiModeHasPrincipalMode_.RemoveAt(multiModeHasPrincipalMode_.Count - 1);
        endFlow("multi-mode");
    }

    public override void startMultiModeMode(MultiMode mode)
    {
        if (multiModeHasPrincipalMode_[multiModeHasPrincipalMode_.Count - 1])
        {
            endFlow("multi-mode.mode");
            multiModeHasPrincipalMode_[multiModeHasPrincipalMode_.Count - 1] = false;
        }
        os().put((Char)'<').write("multi-mode.mode");
        if (mode.hasDesc)
        {
            os().write(" desc=").put((Char)quot);
            writeEscapedData(mode.desc.data()!, mode.desc.size());
            os().put((Char)quot);
        }
        os().put((Char)'>').put((Char)RE);
    }

    public override void endMultiModeMode()
    {
        endFlow("multi-mode.mode");
    }

    // Math flow objects
    public override void startFractionSerial()
    {
        startSimpleFlowObj("fraction");
    }

    public override void endFractionSerial()
    {
        endFlow("fraction");
    }

    public override void startFractionNumerator()
    {
        startSimpleFlowObj("numerator");
    }

    public override void endFractionNumerator()
    {
        endFlow("numerator");
    }

    public override void startFractionDenominator()
    {
        startSimpleFlowObj("denominator");
    }

    public override void endFractionDenominator()
    {
        endFlow("denominator");
    }

    public override void fractionBar()
    {
        simpleFlowObj("fraction-bar");
    }

    public override void startScriptSerial()
    {
        startSimpleFlowObj("script");
    }

    public override void endScriptSerial()
    {
        endFlow("script");
    }

    public override void startScriptPreSup()
    {
        startSimpleFlowObj("pre-sup");
    }

    public override void endScriptPreSup()
    {
        endFlow("pre-sup");
    }

    public override void startScriptPreSub()
    {
        startSimpleFlowObj("pre-sub");
    }

    public override void endScriptPreSub()
    {
        endFlow("pre-sub");
    }

    public override void startScriptPostSup()
    {
        startSimpleFlowObj("post-sup");
    }

    public override void endScriptPostSup()
    {
        endFlow("post-sup");
    }

    public override void startScriptPostSub()
    {
        startSimpleFlowObj("post-sub");
    }

    public override void endScriptPostSub()
    {
        endFlow("post-sub");
    }

    public override void startScriptMidSup()
    {
        startSimpleFlowObj("mid-sup");
    }

    public override void endScriptMidSup()
    {
        endFlow("mid-sup");
    }

    public override void startScriptMidSub()
    {
        startSimpleFlowObj("mid-sub");
    }

    public override void endScriptMidSub()
    {
        endFlow("mid-sub");
    }

    public override void startMarkSerial()
    {
        startSimpleFlowObj("mark");
    }

    public override void endMarkSerial()
    {
        endFlow("mark");
    }

    public override void startMarkOver()
    {
        startSimpleFlowObj("over-mark");
    }

    public override void endMarkOver()
    {
        endFlow("over-mark");
    }

    public override void startMarkUnder()
    {
        startSimpleFlowObj("under-mark");
    }

    public override void endMarkUnder()
    {
        endFlow("under-mark");
    }

    public override void startFenceSerial()
    {
        startSimpleFlowObj("fence");
    }

    public override void endFenceSerial()
    {
        endFlow("fence");
    }

    public override void startFenceOpen()
    {
        startSimpleFlowObj("open");
    }

    public override void endFenceOpen()
    {
        endFlow("open");
    }

    public override void startFenceClose()
    {
        startSimpleFlowObj("close");
    }

    public override void endFenceClose()
    {
        endFlow("close");
    }

    public override void startRadicalSerial()
    {
        startSimpleFlowObj("radical");
    }

    public override void endRadicalSerial()
    {
        endFlow("radical");
    }

    public override void startRadicalDegree()
    {
        startSimpleFlowObj("degree");
    }

    public override void endRadicalDegree()
    {
        endFlow("degree");
    }

    public override void radicalRadical(CharacterNIC nic)
    {
        os().put((Char)'<').write("radical.radical");
        characterNIC(nic);
        outputIcs();
        os().write("/>").put((Char)RE);
    }

    public override void radicalRadicalDefaulted()
    {
        startPortFlow("radical.principal");
    }

    public override void startMathOperatorSerial()
    {
        startSimpleFlowObj("math-operator");
    }

    public override void endMathOperatorSerial()
    {
        endFlow("math-operator");
    }

    public override void startMathOperatorOperator()
    {
        startSimpleFlowObj("operator");
    }

    public override void endMathOperatorOperator()
    {
        endFlow("operator");
    }

    public override void startMathOperatorLowerLimit()
    {
        startSimpleFlowObj("lower-limit");
    }

    public override void endMathOperatorLowerLimit()
    {
        endFlow("lower-limit");
    }

    public override void startMathOperatorUpperLimit()
    {
        startSimpleFlowObj("upper-limit");
    }

    public override void endMathOperatorUpperLimit()
    {
        endFlow("upper-limit");
    }

    // Table methods
    public override void tableBeforeRowBorder()
    {
        simpleFlowObj("table-before-row-border");
    }

    public override void tableAfterRowBorder()
    {
        simpleFlowObj("table-after-row-border");
    }

    public override void tableBeforeColumnBorder()
    {
        simpleFlowObj("table-before-column-border");
    }

    public override void tableAfterColumnBorder()
    {
        simpleFlowObj("table-after-column-border");
    }

    public override void startTablePartSerial(TablePartNIC nic)
    {
        flushPendingElements();
        os().put((Char)'<').write("table-part");
        if (nic.isExplicit)
            os().write(" explicit=\"true\"");
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    public override void endTablePartSerial()
    {
        endFlow("table-part");
    }

    public override void startTablePartHeader()
    {
        startSimpleFlowObj("table-header");
    }

    public override void endTablePartHeader()
    {
        endFlow("table-header");
    }

    public override void startTablePartFooter()
    {
        startSimpleFlowObj("table-footer");
    }

    public override void endTablePartFooter()
    {
        endFlow("table-footer");
    }

    public override void tableCellBeforeRowBorder()
    {
        simpleFlowObj("cell-before-row-border");
    }

    public override void tableCellAfterRowBorder()
    {
        simpleFlowObj("cell-after-row-border");
    }

    public override void tableCellBeforeColumnBorder()
    {
        simpleFlowObj("cell-before-column-border");
    }

    public override void tableCellAfterColumnBorder()
    {
        simpleFlowObj("cell-after-column-border");
    }

    public override void currentNodePageNumber(NodePtr node)
    {
        if (!nodeIsElement(node))
            return;
        os().put((Char)'<').write("page-number ref=").put((Char)quot);
        outputElementName(node);
        os().put((Char)quot).write("/>").put((Char)RE);
    }

    public override void charactersFromNode(NodePtr nd, Char[] data, nuint size)
    {
        characters(data, size);
    }

    public override void formattingInstruction(StringC s)
    {
        os().put((Char)'<').write("formatting-instruction").put((Char)'>');
        writeEscapedData(s.data()!, s.size());
        os().put((Char)'<').put((Char)'/').write("formatting-instruction").put((Char)'>').put((Char)RE);
    }

    // Private helper methods
    private void optLengthSpecC(string name, OptLengthSpec ols)
    {
        ics_.Append(' ').Append(name).Append("=\"");
        if (ols.hasLength)
        {
            if (ols.length.displaySizeFactor != 0.0)
            {
                if (ols.length.length != 0)
                {
                    long whole = ols.length.length / 1000;
                    long frac = Math.Abs(ols.length.length % 1000);
                    if (frac == 0)
                        ics_.Append(whole).Append("pt");
                    else
                        ics_.Append(whole).Append('.').Append(frac.ToString("D3").TrimEnd('0')).Append("pt");
                    if (ols.length.displaySizeFactor >= 0.0)
                        ics_.Append('+');
                }
                ics_.Append((ols.length.displaySizeFactor * 100.0).ToString("F2")).Append('%');
            }
            else
            {
                long whole = ols.length.length / 1000;
                long frac = Math.Abs(ols.length.length % 1000);
                if (frac == 0)
                    ics_.Append(whole).Append("pt");
                else
                    ics_.Append(whole).Append('.').Append(frac.ToString("D3").TrimEnd('0')).Append("pt");
            }
        }
        else
            ics_.Append(falseString);
        ics_.Append('"');
    }

    private void publicIdC(string name, string? pubid)
    {
        ics_.Append(' ').Append(name).Append("=\"");
        if (pubid != null)
            ics_.Append(pubid);
        else
            ics_.Append(falseString);
        ics_.Append('"');
    }

    private void inlineSpaceC(string name, InlineSpace @is)
    {
        ics_.Append(' ').Append(name).Append("=\"");
        // Write nominal
        writeLengthSpecValue(ics_, @is.nominal);
        if (@is.min.length != @is.nominal.length || @is.max.length != @is.nominal.length)
        {
            ics_.Append(',');
            writeLengthSpecValue(ics_, @is.min);
            ics_.Append(',');
            writeLengthSpecValue(ics_, @is.max);
        }
        ics_.Append('"');
    }

    private void writeLengthSpecValue(StringBuilder sb, LengthSpec ls)
    {
        long whole = ls.length / 1000;
        long frac = Math.Abs(ls.length % 1000);
        if (frac == 0)
            sb.Append(whole).Append("pt");
        else
            sb.Append(whole).Append('.').Append(frac.ToString("D3").TrimEnd('0')).Append("pt");
    }

    // Private helper methods
    private void startSimpleFlowObj(string name)
    {
        os().put((Char)'<').write(name);
        outputIcs();
        os().put((Char)'>').put((Char)RE);
    }

    private void simpleFlowObj(string name)
    {
        os().put((Char)'<').write(name);
        outputIcs();
        os().write("/>").put((Char)RE);
    }

    private void startPortFlow(string name)
    {
        os().put((Char)'<').write(name).put((Char)'>').put((Char)RE);
    }

    private void endFlow(string name)
    {
        os().put((Char)'<').put((Char)'/').write(name).put((Char)'>').put((Char)RE);
    }

    private void outputIcs()
    {
        if (ics_.Length > 0)
        {
            string str = ics_.ToString();
            for (int i = 0; i < str.Length; i++)
                os().put((Char)str[i]);
            ics_.Clear();
        }
    }

    private void displayNIC(DisplayNIC nic)
    {
        if (nic.keepWithPrevious)
            os().write(" keep-with-previous=\"true\"");
        if (nic.keepWithNext)
            os().write(" keep-with-next=\"true\"");
        if (nic.mayViolateKeepBefore)
            os().write(" may-violate-keep-before=\"true\"");
        if (nic.mayViolateKeepAfter)
            os().write(" may-violate-keep-after=\"true\"");
        if (nic.positionPreference != Symbol.symbolFalse)
        {
            os().write(" position-preference=\"");
            writeSymbol(nic.positionPreference);
            os().put((Char)'"');
        }
        if (nic.keep != Symbol.symbolFalse)
        {
            os().write(" keep=\"");
            writeSymbol(nic.keep);
            os().put((Char)'"');
        }
        if (nic.breakBefore != Symbol.symbolFalse)
        {
            os().write(" break-before=\"");
            writeSymbol(nic.breakBefore);
            os().put((Char)'"');
        }
        if (nic.breakAfter != Symbol.symbolFalse)
        {
            os().write(" break-after=\"");
            writeSymbol(nic.breakAfter);
            os().put((Char)'"');
        }
        displaySpaceNIC("space-before", nic.spaceBefore);
        displaySpaceNIC("space-after", nic.spaceAfter);
    }

    private void displaySpaceNIC(string name, DisplaySpace ds)
    {
        if (ds.nominal || ds.min || ds.max)
        {
            os().put((Char)' ').write(name).put((Char)'=').put((Char)'"');
            writeLengthSpec(ds.nominal);
            if (ds.min.length != ds.nominal.length ||
                ds.min.displaySizeFactor != ds.nominal.displaySizeFactor ||
                ds.max.length != ds.nominal.length ||
                ds.max.displaySizeFactor != ds.nominal.displaySizeFactor)
            {
                os().put((Char)',');
                writeLengthSpec(ds.min);
                os().put((Char)',');
                writeLengthSpec(ds.max);
            }
            os().put((Char)'"');
        }
        if (ds.force)
            os().put((Char)' ').write(name).write("-priority=\"force\"");
        else if (ds.priority != 0)
        {
            os().put((Char)' ').write(name).write("-priority=\"");
            os().write(ds.priority.ToString()).put((Char)'"');
        }
        if (!ds.conditional)
            os().put((Char)' ').write(name).write("-conditional=\"false\"");
    }

    private void inlineNIC(InlineNIC nic)
    {
        inlineNIC(nic.breakBeforePriority, nic.breakAfterPriority);
    }

    private void inlineNIC(long breakBeforePriority, long breakAfterPriority)
    {
        if (breakBeforePriority != 0)
        {
            os().write(" break-before-priority=\"");
            os().write(breakBeforePriority.ToString()).put((Char)'"');
        }
        if (breakAfterPriority != 0)
        {
            os().write(" break-after-priority=\"");
            os().write(breakAfterPriority.ToString()).put((Char)'"');
        }
    }

    private void characterNIC(CharacterNIC nic)
    {
        if (nic.specifiedC != 0)
        {
            if ((nic.specifiedC & (1 << CharacterNIC.cChar)) != 0)
                os().write(" char=\"&#").write(nic.ch.ToString()).write(";\"");
            if ((nic.specifiedC & (1 << CharacterNIC.cGlyphId)) != 0)
            {
                os().write(" glyph-id=\"");
                if (nic.glyphId.publicId != null)
                {
                    os().write(nic.glyphId.publicId);
                    if (nic.glyphId.suffix != 0)
                        os().write("::").write(nic.glyphId.suffix.ToString());
                }
                else
                    os().write(falseString);
                os().put((Char)'"');
            }
            if ((nic.specifiedC & (1 << CharacterNIC.cIsDropAfterLineBreak)) != 0)
                os().write(" drop-after-line-break=\"").write(boolString(nic.isDropAfterLineBreak)).put((Char)'"');
            if ((nic.specifiedC & (1 << CharacterNIC.cIsDropUnlessBeforeLineBreak)) != 0)
                os().write(" drop-unless-before-line-break=\"").write(boolString(nic.isDropUnlessBeforeLineBreak)).put((Char)'"');
            if ((nic.specifiedC & (1 << CharacterNIC.cIsPunct)) != 0)
                os().write(" punct=\"").write(boolString(nic.isPunct)).put((Char)'"');
            if ((nic.specifiedC & (1 << CharacterNIC.cIsInputWhitespace)) != 0)
                os().write(" input-whitespace=\"").write(boolString(nic.isInputWhitespace)).put((Char)'"');
            if ((nic.specifiedC & (1 << CharacterNIC.cIsInputTab)) != 0)
                os().write(" input-tab=\"").write(boolString(nic.isInputTab)).put((Char)'"');
            if ((nic.specifiedC & (1 << CharacterNIC.cIsRecordEnd)) != 0)
                os().write(" record-end=\"").write(boolString(nic.isRecordEnd)).put((Char)'"');
            if ((nic.specifiedC & (1 << CharacterNIC.cIsSpace)) != 0)
                os().write(" space=\"").write(boolString(nic.isSpace)).put((Char)'"');
            if ((nic.specifiedC & (1 << CharacterNIC.cMathClass)) != 0)
            {
                os().write(" math-class=\"");
                writeSymbol(nic.mathClass);
                os().put((Char)'"');
            }
            if ((nic.specifiedC & (1 << CharacterNIC.cBreakBeforePriority)) != 0)
                os().write(" break-before-priority=\"").write(nic.breakBeforePriority.ToString()).put((Char)'"');
            if ((nic.specifiedC & (1 << CharacterNIC.cBreakAfterPriority)) != 0)
                os().write(" break-after-priority=\"").write(nic.breakAfterPriority.ToString()).put((Char)'"');
        }
        if (nic.stretchFactor != 1.0)
            os().write(" stretch-factor=\"").write(nic.stretchFactor.ToString()).put((Char)'"');
    }

    private void writeEscapedData(Char[] data, nuint size)
    {
        for (nuint i = 0; i < size; i++)
        {
            Char c = data[i];
            switch (c)
            {
                case '&':
                    os().write("&amp;");
                    break;
                case '<':
                    os().write("&lt;");
                    break;
                case '>':
                    os().write("&gt;");
                    break;
                case '"':
                    os().write("&quot;");
                    break;
                default:
                    if (c < 0x80)
                        os().put(c);
                    else
                        os().write("&#").write(c.ToString()).put((Char)';');
                    break;
            }
        }
    }

    private void writeLengthSpec(LengthSpec ls)
    {
        if (ls.displaySizeFactor != 0.0)
        {
            if (ls.length != 0)
            {
                writeUnits(ls.length);
                if (ls.displaySizeFactor >= 0.0)
                    os().put((Char)'+');
            }
            os().write((ls.displaySizeFactor * 100.0).ToString("F2")).put((Char)'%');
        }
        else
            writeUnits(ls.length);
    }

    private void writeTableLengthSpec(TableLengthSpec ls)
    {
        bool needSign = false;
        if (ls.length != 0)
        {
            writeUnits(ls.length);
            needSign = true;
        }
        if (ls.displaySizeFactor != 0.0)
        {
            if (needSign && ls.displaySizeFactor >= 0.0)
                os().put((Char)'+');
            os().write((ls.displaySizeFactor * 100.0).ToString("F2")).put((Char)'%');
            needSign = true;
        }
        if (ls.tableUnitFactor != 0.0)
        {
            if (needSign && ls.tableUnitFactor >= 0.0)
                os().put((Char)'+');
            os().write(ls.tableUnitFactor.ToString("F2")).put((Char)'*');
        }
        if (!needSign && ls.tableUnitFactor == 0.0)
            os().put((Char)'0').write("pt");
    }

    private void writeUnits(long units)
    {
        // Convert to points with 3 decimal places (units are 1/1000 of a point)
        long whole = units / 1000;
        long frac = Math.Abs(units % 1000);
        if (frac == 0)
            os().write(whole.ToString()).write("pt");
        else
            os().write(whole.ToString()).put((Char)'.').write(frac.ToString("D3").TrimEnd('0')).write("pt");
    }

    private void writeSymbol(Symbol sym)
    {
        string? s = symbolName(sym);
        if (s != null)
            os().write(s);
        else if (sym == Symbol.symbolFalse)
            os().write(falseString);
        else if (sym == Symbol.symbolTrue)
            os().write(trueString);
    }

    private void lengthC(string name, long units)
    {
        ics_.Append(' ').Append(name).Append("=\"");
        long whole = units / 1000;
        long frac = Math.Abs(units % 1000);
        if (frac == 0)
            ics_.Append(whole).Append("pt");
        else
            ics_.Append(whole).Append('.').Append(frac.ToString("D3").TrimEnd('0')).Append("pt");
        ics_.Append('"');
    }

    private void lengthSpecC(string name, LengthSpec ls)
    {
        ics_.Append(' ').Append(name).Append("=\"");
        if (ls.displaySizeFactor != 0.0)
        {
            if (ls.length != 0)
            {
                long whole = ls.length / 1000;
                long frac = Math.Abs(ls.length % 1000);
                if (frac == 0)
                    ics_.Append(whole).Append("pt");
                else
                    ics_.Append(whole).Append('.').Append(frac.ToString("D3").TrimEnd('0')).Append("pt");
                if (ls.displaySizeFactor >= 0.0)
                    ics_.Append('+');
            }
            ics_.Append((ls.displaySizeFactor * 100.0).ToString("F2")).Append('%');
        }
        else
        {
            long whole = ls.length / 1000;
            long frac = Math.Abs(ls.length % 1000);
            if (frac == 0)
                ics_.Append(whole).Append("pt");
            else
                ics_.Append(whole).Append('.').Append(frac.ToString("D3").TrimEnd('0')).Append("pt");
        }
        ics_.Append('"');
    }

    private void symbolC(string name, Symbol sym)
    {
        ics_.Append(' ').Append(name).Append("=\"");
        string? s = symbolName(sym);
        if (s != null)
            ics_.Append(s);
        else if (sym == Symbol.symbolFalse)
            ics_.Append(falseString);
        else if (sym == Symbol.symbolTrue)
            ics_.Append(trueString);
        ics_.Append('"');
    }

    private void boolC(string name, bool b)
    {
        ics_.Append(' ').Append(name).Append("=\"").Append(b ? trueString : falseString).Append('"');
    }

    private void integerC(string name, long n)
    {
        ics_.Append(' ').Append(name).Append("=\"").Append(n).Append('"');
    }

    private static string boolString(bool b)
    {
        return b ? trueString : falseString;
    }

    private static new string? symbolName(Symbol sym)
    {
        return sym switch
        {
            Symbol.symbolFalse => "false",
            Symbol.symbolTrue => "true",
            Symbol.symbolNotApplicable => "not-applicable",
            Symbol.symbolUltraCondensed => "ultra-condensed",
            Symbol.symbolExtraCondensed => "extra-condensed",
            Symbol.symbolCondensed => "condensed",
            Symbol.symbolSemiCondensed => "semi-condensed",
            Symbol.symbolUltraLight => "ultra-light",
            Symbol.symbolExtraLight => "extra-light",
            Symbol.symbolLight => "light",
            Symbol.symbolSemiLight => "semi-light",
            Symbol.symbolMedium => "medium",
            Symbol.symbolSemiExpanded => "semi-expanded",
            Symbol.symbolExpanded => "expanded",
            Symbol.symbolExtraExpanded => "extra-expanded",
            Symbol.symbolUltraExpanded => "ultra-expanded",
            Symbol.symbolSemiBold => "semi-bold",
            Symbol.symbolBold => "bold",
            Symbol.symbolExtraBold => "extra-bold",
            Symbol.symbolUltraBold => "ultra-bold",
            Symbol.symbolUpright => "upright",
            Symbol.symbolOblique => "oblique",
            Symbol.symbolBackSlantedOblique => "back-slanted-oblique",
            Symbol.symbolItalic => "italic",
            Symbol.symbolBackSlantedItalic => "back-slanted-italic",
            Symbol.symbolStart => "start",
            Symbol.symbolEnd => "end",
            Symbol.symbolCenter => "center",
            Symbol.symbolJustify => "justify",
            Symbol.symbolSpreadInside => "spread-inside",
            Symbol.symbolSpreadOutside => "spread-outside",
            Symbol.symbolPageInside => "page-inside",
            Symbol.symbolPageOutside => "page-outside",
            Symbol.symbolWrap => "wrap",
            Symbol.symbolAsis => "asis",
            Symbol.symbolAsisWrap => "asis-wrap",
            Symbol.symbolAsisTruncate => "asis-truncate",
            Symbol.symbolNone => "none",
            Symbol.symbolBefore => "before",
            Symbol.symbolThrough => "through",
            Symbol.symbolAfter => "after",
            Symbol.symbolTopToBottom => "top-to-bottom",
            Symbol.symbolLeftToRight => "left-to-right",
            Symbol.symbolBottomToTop => "bottom-to-top",
            Symbol.symbolRightToLeft => "right-to-left",
            Symbol.symbolInside => "inside",
            Symbol.symbolOutside => "outside",
            Symbol.symbolHorizontal => "horizontal",
            Symbol.symbolVertical => "vertical",
            Symbol.symbolEscapement => "escapement",
            Symbol.symbolLineProgression => "line-progression",
            Symbol.symbolMath => "math",
            Symbol.symbolOrdinary => "ordinary",
            Symbol.symbolOperator => "operator",
            Symbol.symbolBinary => "binary",
            Symbol.symbolRelation => "relation",
            Symbol.symbolOpening => "opening",
            Symbol.symbolClosing => "closing",
            Symbol.symbolPunctuation => "punctuation",
            Symbol.symbolInner => "inner",
            Symbol.symbolSpace => "space",
            Symbol.symbolPage => "page",
            Symbol.symbolPageRegion => "page-region",
            Symbol.symbolColumnSet => "column-set",
            Symbol.symbolColumn => "column",
            Symbol.symbolMax => "max",
            Symbol.symbolMaxUniform => "max-uniform",
            Symbol.symbolMiter => "miter",
            Symbol.symbolRound => "round",
            Symbol.symbolBevel => "bevel",
            Symbol.symbolButt => "butt",
            Symbol.symbolSquare => "square",
            Symbol.symbolLoose => "loose",
            Symbol.symbolNormal => "normal",
            Symbol.symbolKern => "kern",
            Symbol.symbolTight => "tight",
            Symbol.symbolTouch => "touch",
            Symbol.symbolPreserve => "preserve",
            Symbol.symbolCollapse => "collapse",
            Symbol.symbolIgnore => "ignore",
            Symbol.symbolRelative => "relative",
            Symbol.symbolDisplay => "display",
            Symbol.symbolInline => "inline",
            Symbol.symbolBorder => "border",
            Symbol.symbolBackground => "background",
            Symbol.symbolBoth => "both",
            Symbol.symbolBase => "base",
            Symbol.symbolFont => "font",
            Symbol.symbolTop => "top",
            Symbol.symbolBottom => "bottom",
            Symbol.symbolSpread => "spread",
            Symbol.symbolSolid => "solid",
            Symbol.symbolOutline => "outline",
            Symbol.symbolWith => "with",
            Symbol.symbolAgainst => "against",
            Symbol.symbolForce => "force",
            Symbol.symbolIndependent => "independent",
            Symbol.symbolPile => "pile",
            Symbol.symbolSupOut => "sup-out",
            Symbol.symbolSubOut => "sub-out",
            Symbol.symbolLeadEdge => "lead-edge",
            Symbol.symbolTrailEdge => "trail-edge",
            Symbol.symbolExplicit => "explicit",
            Symbol.symbolRowMajor => "row-major",
            Symbol.symbolColumnMajor => "column-major",
            _ => null
        };
    }

    private static bool nodeIsElement(NodePtr node)
    {
        GroveString gi = new GroveString();
        return node.getGi(ref gi) == AccessResult.accessOK;
    }

    private void flushPendingElements()
    {
        if (suppressAnchors_)
            return;
        for (int i = 0; i < pendingElements_.Count; i++)
        {
            NodePtr node = pendingElements_[i];
            os().put((Char)'<').write("a name=\"");
            outputElementName(node);
            os().write("\"/>").put((Char)RE);
        }
        nPendingElementsNonEmpty_ = 0;
        pendingElements_.Clear();
        pendingElementLevels_.Clear();
    }

    private void outputElementName(NodePtr node)
    {
        GroveString id = new GroveString();
        if (node.getId(ref id) == AccessResult.accessOK)
            outputElementName(node.groveIndex(), id.data()!, id.size());
        else
        {
            uint groveIdx = node.groveIndex();
            if (groveIdx != 0)
                os().write(groveIdx.ToString()).put((Char)'.');
            ulong elemIdx = 0;
            if (node.elementIndex(ref elemIdx) == AccessResult.accessOK)
                os().write(elemIdx.ToString());
        }
    }

    private void outputElementName(uint groveIndex, Char[] idData, nuint idSize)
    {
        if (groveIndex != 0)
            os().write(groveIndex.ToString()).put((Char)'.');
        writeEscapedData(idData, idSize);
    }
}
