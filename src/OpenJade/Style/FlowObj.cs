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
    public virtual CompoundFlowObj? asCompoundFlowObj() { return null; }

    public override void process(ProcessContext context)
    {
        // Default: process as atomic flow object
        processInner(context);
    }

    protected virtual void processInner(ProcessContext context)
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

    public override void process(ProcessContext context)
    {
        // Process compound flow object
        processInner(context);
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
}

// Paragraph flow object
public class ParagraphFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        ParagraphFlowObj c = new ParagraphFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Display group flow object
public class DisplayGroupFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        DisplayGroupFlowObj c = new DisplayGroupFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
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
    public override FlowObj copy(Interpreter interp)
    {
        ScoreFlowObj c = new ScoreFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Box flow object
public class BoxFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        BoxFlowObj c = new BoxFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Simple page sequence flow object
public class SimplePageSequenceFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        SimplePageSequenceFlowObj c = new SimplePageSequenceFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
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
}

// Link flow object
public class LinkFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        LinkFlowObj c = new LinkFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Character flow object
public class CharacterFlowObj : FlowObj
{
    private Char ch_;

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
}

// External graphic flow object
public class ExternalGraphicFlowObj : FlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        ExternalGraphicFlowObj c = new ExternalGraphicFlowObj();
        c.setStyle(style());
        return c;
    }
}

// Rule flow object
public class RuleFlowObj : FlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        RuleFlowObj c = new RuleFlowObj();
        c.setStyle(style());
        return c;
    }
}

// Leader flow object
public class LeaderFlowObj : FlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        LeaderFlowObj c = new LeaderFlowObj();
        c.setStyle(style());
        return c;
    }
}

// Line field flow object
public class LineFieldFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        LineFieldFlowObj c = new LineFieldFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
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
    public override FlowObj copy(Interpreter interp)
    {
        TableFlowObj c = new TableFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Table part flow object
public class TablePartFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        TablePartFlowObj c = new TablePartFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
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
}

// Table column flow object
public class TableColumnFlowObj : FlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        TableColumnFlowObj c = new TableColumnFlowObj();
        c.setStyle(style());
        return c;
    }
}

// Table cell flow object
public class TableCellFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        TableCellFlowObj c = new TableCellFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
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
    public override FlowObj copy(Interpreter interp)
    {
        GridFlowObj c = new GridFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }
}

// Grid cell flow object
public class GridCellFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        GridCellFlowObj c = new GridCellFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
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
