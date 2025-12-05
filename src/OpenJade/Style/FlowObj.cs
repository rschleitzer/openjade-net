// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Base flow object class
public class FlowObj : SosofoObj
{
    private StyleObj? style_;

    public FlowObj() { style_ = null; }

    public virtual FlowObj copy(Interpreter interp)
    {
        FlowObj c = new FlowObj();
        c.style_ = style_;
        return c;
    }

    public StyleObj? style() { return style_; }
    public void setStyle(StyleObj? style) { style_ = style; }

    public virtual bool hasNonInheritedC(Identifier? ident) { return false; }
    public virtual bool hasPseudoNonInheritedC(Identifier? ident) { return false; }
    public virtual void setNonInheritedC(Identifier? ident, ELObj? value, Location loc, Interpreter interp) { }
    public virtual bool isCharacter() { return false; }
    public virtual bool isRule() { return false; }
    public virtual CompoundFlowObj? asCompoundFlowObj() { return null; }

    public virtual void pushStyle(ProcessContext context, ref uint flags)
    {
        if (style_ != null)
        {
            context.currentStyleStack().push(style_, context.vm(), context.currentFOTBuilder());
            flags |= 1;
        }
    }

    public virtual void popStyle(ProcessContext context, uint flags)
    {
        if ((flags & 1) != 0)
            context.currentStyleStack().pop();
    }

    public override void process(ProcessContext context)
    {
        context.startFlowObj();
        uint flags = 0;
        pushStyle(context, ref flags);
        processInner(context);
        popStyle(context, flags);
        context.endFlowObj();
    }

    public virtual void processInner(ProcessContext context)
    {
        // To be overridden by concrete flow objects
    }
}

// Compound flow object - can contain other flow objects
public class CompoundFlowObj : FlowObj
{
    private SosofoObj? content_;

    public CompoundFlowObj() { content_ = null; }

    public override CompoundFlowObj? asCompoundFlowObj() { return this; }

    public void setContent(SosofoObj? content) { content_ = content; }
    public SosofoObj? content() { return content_; }

    public override FlowObj copy(Interpreter interp)
    {
        CompoundFlowObj c = new CompoundFlowObj();
        c.setStyle(style());
        c.content_ = content_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        if (content_ != null)
            content_.process(context);
    }
}

// Sequence flow object
public class SequenceFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        SequenceFlowObj c = new SequenceFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startSequence();
        base.processInner(context);
        fotb.endSequence();
    }
}

// Paragraph flow object
public class ParagraphFlowObj : CompoundFlowObj
{
    private FOTBuilder.ParagraphNIC nic_ = new FOTBuilder.ParagraphNIC();

    public override FlowObj copy(Interpreter interp)
    {
        ParagraphFlowObj c = new ParagraphFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startParagraph(nic_);
        base.processInner(context);
        fotb.endParagraph();
    }
}

// Display group flow object
public class DisplayGroupFlowObj : CompoundFlowObj
{
    private FOTBuilder.DisplayGroupNIC nic_ = new FOTBuilder.DisplayGroupNIC();

    public override FlowObj copy(Interpreter interp)
    {
        DisplayGroupFlowObj c = new DisplayGroupFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startDisplayGroup(nic_);
        base.processInner(context);
        fotb.endDisplayGroup();
    }
}

// Inline sequence flow object
public class InlineSequenceFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        InlineSequenceFlowObj c = new InlineSequenceFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Score flow object
public class ScoreFlowObj : CompoundFlowObj
{
    public enum ScoreType { TypeNone, TypeSymbol, TypeLength, TypeChar }
    private ScoreType scoreType_ = ScoreType.TypeNone;
    private FOTBuilder.Symbol symbolType_ = FOTBuilder.Symbol.symbolFalse;
    private FOTBuilder.LengthSpec lengthType_ = new FOTBuilder.LengthSpec();
    private Char charType_ = 0;

    public override FlowObj copy(Interpreter interp)
    {
        ScoreFlowObj c = new ScoreFlowObj();
        c.setStyle(style());
        c.setContent(content());
        c.scoreType_ = scoreType_;
        c.symbolType_ = symbolType_;
        c.lengthType_ = lengthType_;
        c.charType_ = charType_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        switch (scoreType_)
        {
            case ScoreType.TypeSymbol:
                fotb.startScore(symbolType_);
                break;
            case ScoreType.TypeLength:
                fotb.startScore(lengthType_);
                break;
            case ScoreType.TypeChar:
                fotb.startScore(charType_);
                break;
            default:
                fotb.startSequence();
                base.processInner(context);
                fotb.endSequence();
                return;
        }
        base.processInner(context);
        fotb.endScore();
    }
}

// Box flow object
public class BoxFlowObj : CompoundFlowObj
{
    private FOTBuilder.BoxNIC nic_ = new FOTBuilder.BoxNIC();

    public override FlowObj copy(Interpreter interp)
    {
        BoxFlowObj c = new BoxFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startBox(nic_);
        base.processInner(context);
        fotb.endBox();
    }
}

// Simple page sequence flow object
public class SimplePageSequenceFlowObj : CompoundFlowObj
{
    public const int nHeaderFooterParts = 6;
    private SosofoObj?[] headerFooter_ = new SosofoObj?[nHeaderFooterParts];

    public SimplePageSequenceFlowObj()
    {
        hasSubObjects_ = (char)1;
    }

    public override FlowObj copy(Interpreter interp)
    {
        SimplePageSequenceFlowObj c = new SimplePageSequenceFlowObj();
        c.setStyle(style());
        c.setContent(content());
        Array.Copy(headerFooter_, c.headerFooter_, nHeaderFooterParts);
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startSimplePageSequence(null);
        fotb.endSimplePageSequenceHeaderFooter();
        base.processInner(context);
        fotb.endSimplePageSequence();
    }

    public override void traceSubObjects(Collector c)
    {
        for (int i = 0; i < nHeaderFooterParts; i++)
            if (headerFooter_[i] != null)
                c.trace(headerFooter_[i]);
    }
}

// Scroll flow object
public class ScrollFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        ScrollFlowObj c = new ScrollFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startScroll();
        base.processInner(context);
        fotb.endScroll();
    }
}

// Link flow object
public class LinkFlowObj : CompoundFlowObj
{
    private AddressObj? addressObj_;

    public override FlowObj copy(Interpreter interp)
    {
        LinkFlowObj c = new LinkFlowObj();
        c.setStyle(style());
        c.setContent(content());
        c.addressObj_ = addressObj_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        if (addressObj_ == null)
        {
            FOTBuilder.Address addr = new FOTBuilder.Address();
            addr.type = FOTBuilder.Address.Type.none;
            fotb.startLink(addr);
        }
        else
            fotb.startLink(addressObj_.address());
        base.processInner(context);
        fotb.endLink();
    }
}

// Character flow object
public class CharacterFlowObj : FlowObj
{
    private Char ch_;
    private FOTBuilder.CharacterNIC nic_ = new FOTBuilder.CharacterNIC();

    public CharacterFlowObj() { ch_ = 0; }
    public CharacterFlowObj(Char c) { ch_ = c; }

    public override bool isCharacter() { return true; }

    public override FlowObj copy(Interpreter interp)
    {
        CharacterFlowObj c = new CharacterFlowObj();
        c.setStyle(style());
        c.ch_ = ch_;
        return c;
    }

    public Char ch() { return ch_; }
    public void setCh(Char c) { ch_ = c; }

    public override void processInner(ProcessContext context)
    {
        nic_.ch = ch_;
        nic_.valid = true;
        context.currentFOTBuilder().character(nic_);
    }
}

// External graphic flow object
public class ExternalGraphicFlowObj : FlowObj
{
    private FOTBuilder.ExternalGraphicNIC nic_ = new FOTBuilder.ExternalGraphicNIC();

    public override FlowObj copy(Interpreter interp)
    {
        ExternalGraphicFlowObj c = new ExternalGraphicFlowObj();
        c.setStyle(style());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        context.currentFOTBuilder().externalGraphic(nic_);
    }
}

// Rule flow object
public class RuleFlowObj : FlowObj
{
    private FOTBuilder.RuleNIC nic_ = new FOTBuilder.RuleNIC();

    public override FlowObj copy(Interpreter interp)
    {
        RuleFlowObj c = new RuleFlowObj();
        c.setStyle(style());
        return c;
    }

    public override bool isRule() { return true; }

    public override void processInner(ProcessContext context)
    {
        context.currentFOTBuilder().rule(nic_);
    }
}

// Leader flow object
public class LeaderFlowObj : FlowObj
{
    private FOTBuilder.LeaderNIC nic_ = new FOTBuilder.LeaderNIC();

    public override FlowObj copy(Interpreter interp)
    {
        LeaderFlowObj c = new LeaderFlowObj();
        c.setStyle(style());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startLeader(nic_);
        fotb.endLeader();
    }
}

// Line field flow object
public class LineFieldFlowObj : CompoundFlowObj
{
    private FOTBuilder.LineFieldNIC nic_ = new FOTBuilder.LineFieldNIC();

    public override FlowObj copy(Interpreter interp)
    {
        LineFieldFlowObj c = new LineFieldFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startLineField(nic_);
        base.processInner(context);
        fotb.endLineField();
    }
}

// Sideline flow object
public class SidelineFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        SidelineFlowObj c = new SidelineFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startSideline();
        base.processInner(context);
        fotb.endSideline();
    }
}

// Anchor flow object
public class AnchorFlowObj : FlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        AnchorFlowObj c = new AnchorFlowObj();
        c.setStyle(style());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        // Anchor generates an alignment point
        context.currentFOTBuilder().alignmentPoint();
    }
}

// Alignment point flow object
public class AlignmentPointFlowObj : FlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        AlignmentPointFlowObj c = new AlignmentPointFlowObj();
        c.setStyle(style());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        context.currentFOTBuilder().alignmentPoint();
    }
}

// Page number flow object
public class PageNumberFlowObj : FlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        PageNumberFlowObj c = new PageNumberFlowObj();
        c.setStyle(style());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        context.currentFOTBuilder().pageNumber();
    }
}

// Marginalia flow object
public class MarginaliaFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        MarginaliaFlowObj c = new MarginaliaFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startMarginalia();
        base.processInner(context);
        fotb.endMarginalia();
    }
}

// Aligned column flow object
public class AlignedColumnFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        AlignedColumnFlowObj c = new AlignedColumnFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Table flow object
public class TableFlowObj : CompoundFlowObj
{
    private FOTBuilder.TableNIC nic_ = new FOTBuilder.TableNIC();

    public override FlowObj copy(Interpreter interp)
    {
        TableFlowObj c = new TableFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startTable(nic_);
        base.processInner(context);
        fotb.endTable();
    }
}

// Table part flow object
public class TablePartFlowObj : CompoundFlowObj
{
    private FOTBuilder.TablePartNIC nic_ = new FOTBuilder.TablePartNIC();

    public override FlowObj copy(Interpreter interp)
    {
        TablePartFlowObj c = new TablePartFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startTablePart(nic_, out FOTBuilder? header, out FOTBuilder? footer);
        base.processInner(context);
        fotb.endTablePart();
    }
}

// Table row flow object
public class TableRowFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        TableRowFlowObj c = new TableRowFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startTableRow();
        base.processInner(context);
        fotb.endTableRow();
    }
}

// Table column flow object
public class TableColumnFlowObj : FlowObj
{
    private FOTBuilder.TableColumnNIC nic_ = new FOTBuilder.TableColumnNIC();

    public override FlowObj copy(Interpreter interp)
    {
        TableColumnFlowObj c = new TableColumnFlowObj();
        c.setStyle(style());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        context.currentFOTBuilder().tableColumn(nic_);
    }
}

// Table cell flow object
public class TableCellFlowObj : CompoundFlowObj
{
    private FOTBuilder.TableCellNIC nic_ = new FOTBuilder.TableCellNIC();

    public override FlowObj copy(Interpreter interp)
    {
        TableCellFlowObj c = new TableCellFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startTableCell(nic_);
        base.processInner(context);
        fotb.endTableCell();
    }
}

// Table border flow object
public class TableBorderFlowObj : FlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        TableBorderFlowObj c = new TableBorderFlowObj();
        c.setStyle(style());
        return c;
    }
}

// Math sequence flow object
public class MathSequenceFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        MathSequenceFlowObj c = new MathSequenceFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startMathSequence();
        base.processInner(context);
        fotb.endMathSequence();
    }
}

// Fraction flow object
public class FractionFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        FractionFlowObj c = new FractionFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startFraction(out FOTBuilder? num, out FOTBuilder? denom);
        base.processInner(context);
        fotb.endFraction();
    }
}

// Unmath flow object
public class UnmathFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        UnmathFlowObj c = new UnmathFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startUnmath();
        base.processInner(context);
        fotb.endUnmath();
    }
}

// Superscript flow object
public class SuperscriptFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        SuperscriptFlowObj c = new SuperscriptFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startSuperscript();
        base.processInner(context);
        fotb.endSuperscript();
    }
}

// Subscript flow object
public class SubscriptFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        SubscriptFlowObj c = new SubscriptFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startSubscript();
        base.processInner(context);
        fotb.endSubscript();
    }
}

// Script flow object
public class ScriptFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        ScriptFlowObj c = new ScriptFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Mark flow object
public class MarkFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        MarkFlowObj c = new MarkFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Fence flow object
public class FenceFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        FenceFlowObj c = new FenceFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Radical flow object
public class RadicalFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        RadicalFlowObj c = new RadicalFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Math operator flow object
public class MathOperatorFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        MathOperatorFlowObj c = new MathOperatorFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Grid flow object
public class GridFlowObj : CompoundFlowObj
{
    private FOTBuilder.GridNIC nic_ = new FOTBuilder.GridNIC();

    public override FlowObj copy(Interpreter interp)
    {
        GridFlowObj c = new GridFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startGrid(nic_);
        base.processInner(context);
        fotb.endGrid();
    }
}

// Grid cell flow object
public class GridCellFlowObj : CompoundFlowObj
{
    private FOTBuilder.GridCellNIC nic_ = new FOTBuilder.GridCellNIC();

    public override FlowObj copy(Interpreter interp)
    {
        GridCellFlowObj c = new GridCellFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startGridCell(nic_);
        base.processInner(context);
        fotb.endGridCell();
    }
}

// Glyph annotation flow object
public class GlyphAnnotationFlowObj : FlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        GlyphAnnotationFlowObj c = new GlyphAnnotationFlowObj();
        c.setStyle(style());
        return c;
    }
}

// Multi mode flow object
public class MultiModeFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        MultiModeFlowObj c = new MultiModeFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Empty sosofo object
public class EmptySosofoObj : SosofoObj
{
    public override void process(ProcessContext context)
    {
        // Empty - does nothing
    }
}
