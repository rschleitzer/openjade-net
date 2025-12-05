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
    public override bool isCharacter() { return false; }
    public override bool isRule() { return false; }
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

    // Check if identifier is a display-related NIC
    public static bool isDisplayNIC(Identifier? ident)
    {
        if (ident == null)
            return false;
        Identifier.SyntacticKey key;
        if (ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyPositionPreference:
                case Identifier.SyntacticKey.keyIsKeepWithPrevious:
                case Identifier.SyntacticKey.keyIsKeepWithNext:
                case Identifier.SyntacticKey.keyKeep:
                case Identifier.SyntacticKey.keyBreakBefore:
                case Identifier.SyntacticKey.keyBreakAfter:
                case Identifier.SyntacticKey.keyIsMayViolateKeepBefore:
                case Identifier.SyntacticKey.keyIsMayViolateKeepAfter:
                case Identifier.SyntacticKey.keySpaceBefore:
                case Identifier.SyntacticKey.keySpaceAfter:
                    return true;
                default:
                    break;
            }
        }
        return false;
    }

    // Set display-related NIC
    public static bool setDisplayNIC<T>(ref T nic, Identifier? ident,
                                     ELObj? obj, Location loc, Interpreter interp) where T : FOTBuilder.DisplayNIC
    {
        if (ident == null || obj == null)
            return false;

        Identifier.SyntacticKey key;
        if (ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyPositionPreference:
                    interp.convertEnumC(obj, ident, loc, out nic.positionPreference);
                    return true;
                case Identifier.SyntacticKey.keyIsKeepWithPrevious:
                    interp.convertBooleanC(obj, ident, loc, out nic.keepWithPrevious);
                    return true;
                case Identifier.SyntacticKey.keyIsKeepWithNext:
                    interp.convertBooleanC(obj, ident, loc, out nic.keepWithNext);
                    return true;
                case Identifier.SyntacticKey.keyKeep:
                    interp.convertEnumC(obj, ident, loc, out nic.keep);
                    return true;
                case Identifier.SyntacticKey.keyBreakBefore:
                    interp.convertEnumC(obj, ident, loc, out nic.breakBefore);
                    return true;
                case Identifier.SyntacticKey.keyBreakAfter:
                    interp.convertEnumC(obj, ident, loc, out nic.breakAfter);
                    return true;
                case Identifier.SyntacticKey.keyIsMayViolateKeepBefore:
                    interp.convertBooleanC(obj, ident, loc, out nic.mayViolateKeepBefore);
                    return true;
                case Identifier.SyntacticKey.keyIsMayViolateKeepAfter:
                    interp.convertBooleanC(obj, ident, loc, out nic.mayViolateKeepAfter);
                    return true;
                case Identifier.SyntacticKey.keySpaceBefore:
                case Identifier.SyntacticKey.keySpaceAfter:
                    {
                        ref FOTBuilder.DisplaySpace ds = ref (key == Identifier.SyntacticKey.keySpaceBefore
                            ? ref nic.spaceBefore
                            : ref nic.spaceAfter);
                        DisplaySpaceObj? dso = obj.asDisplaySpace();
                        if (dso != null)
                            ds = dso.displaySpace();
                        else if (interp.convertLengthSpecC(obj, ident, loc, ref ds.nominal))
                        {
                            ds.max = ds.nominal;
                            ds.min = ds.nominal;
                        }
                    }
                    return true;
                default:
                    break;
            }
        }
        return false;
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
        c.nic_ = nic_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startParagraph(nic_);
        base.processInner(context);
        fotb.endParagraph();
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        return isDisplayNIC(ident);
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? value, Location loc, Interpreter interp)
    {
        setDisplayNIC(ref nic_, ident, value, loc, interp);
    }
}

// Paragraph break flow object
public class ParagraphBreakFlowObj : FlowObj
{
    private FOTBuilder.ParagraphNIC nic_ = new FOTBuilder.ParagraphNIC();

    public override FlowObj copy(Interpreter interp)
    {
        ParagraphBreakFlowObj c = new ParagraphBreakFlowObj();
        c.setStyle(style());
        c.nic_ = nic_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        context.currentFOTBuilder().paragraphBreak(nic_);
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        return isDisplayNIC(ident);
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? value, Location loc, Interpreter interp)
    {
        setDisplayNIC(ref nic_, ident, value, loc, interp);
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
        c.nic_ = nic_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startDisplayGroup(nic_);
        base.processInner(context);
        fotb.endDisplayGroup();
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key) && key == Identifier.SyntacticKey.keyCoalesceId)
            return true;
        return isDisplayNIC(ident);
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? value, Location loc, Interpreter interp)
    {
        if (setDisplayNIC(ref nic_, ident, value, loc, interp))
            return;
        Char[]? s;
        nuint n;
        if (value == null || !value.stringData(out s, out n) || s == null)
        {
            interp.invalidCharacteristicValue(ident, loc);
            return;
        }
        nic_.hasCoalesceId = true;
        nic_.coalesceId = new StringC(s, n);
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

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key) && key == Identifier.SyntacticKey.keyType)
            return true;
        return false;
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (obj == null)
            return;
        Char c;
        if (obj.charValue(out c))
        {
            scoreType_ = ScoreType.TypeChar;
            charType_ = c;
            return;
        }
        long n;
        double d;
        int dim;
        switch (obj.quantityValue(out n, out d, out dim))
        {
            case ELObj.QuantityType.longQuantity:
                if (dim == 1)
                {
                    scoreType_ = ScoreType.TypeLength;
                    lengthType_ = new FOTBuilder.LengthSpec(n);
                    return;
                }
                break;
            case ELObj.QuantityType.doubleQuantity:
                if (dim == 1)
                {
                    scoreType_ = ScoreType.TypeLength;
                    lengthType_ = new FOTBuilder.LengthSpec((long)d);
                    return;
                }
                break;
            default:
                break;
        }
        FOTBuilder.Symbol sym;
        if (interp.convertEnumC(obj, ident, loc, out sym))
        {
            scoreType_ = ScoreType.TypeSymbol;
            symbolType_ = sym;
        }
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
        c.nic_ = nic_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startBox(nic_);
        base.processInner(context);
        fotb.endBox();
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyIsDisplay:
                case Identifier.SyntacticKey.keyBreakBeforePriority:
                case Identifier.SyntacticKey.keyBreakAfterPriority:
                    return true;
                default:
                    break;
            }
        }
        return isDisplayNIC(ident);
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (setDisplayNIC(ref nic_, ident, obj, loc, interp))
            return;
        if (ident == null || obj == null)
            return;
        Identifier.SyntacticKey key;
        if (ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyIsDisplay:
                    interp.convertBooleanC(obj, ident, loc, out nic_.isDisplay);
                    return;
                case Identifier.SyntacticKey.keyBreakBeforePriority:
                    interp.convertIntegerC(obj, ident, loc, out nic_.breakBeforePriority);
                    return;
                case Identifier.SyntacticKey.keyBreakAfterPriority:
                    interp.convertIntegerC(obj, ident, loc, out nic_.breakAfterPriority);
                    return;
                default:
                    break;
            }
        }
    }
}

// Simple page sequence flow object
public class SimplePageSequenceFlowObj : CompoundFlowObj
{
    public const int nHeaderFooterParts = 6;
    private const int nPageTypeBits = 2;
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

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyLeftHeader:
                case Identifier.SyntacticKey.keyCenterHeader:
                case Identifier.SyntacticKey.keyRightHeader:
                case Identifier.SyntacticKey.keyLeftFooter:
                case Identifier.SyntacticKey.keyCenterFooter:
                case Identifier.SyntacticKey.keyRightFooter:
                    return true;
                default:
                    break;
            }
        }
        return false;
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (obj == null)
            return;
        SosofoObj? sosofo = obj.asSosofo();
        if (sosofo == null)
        {
            interp.invalidCharacteristicValue(ident, loc);
            return;
        }
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyLeftHeader:
                    headerFooter_[((int)FOTBuilder.HF.leftHF | (int)FOTBuilder.HF.headerHF) >> nPageTypeBits] = sosofo;
                    return;
                case Identifier.SyntacticKey.keyCenterHeader:
                    headerFooter_[((int)FOTBuilder.HF.centerHF | (int)FOTBuilder.HF.headerHF) >> nPageTypeBits] = sosofo;
                    return;
                case Identifier.SyntacticKey.keyRightHeader:
                    headerFooter_[((int)FOTBuilder.HF.rightHF | (int)FOTBuilder.HF.headerHF) >> nPageTypeBits] = sosofo;
                    return;
                case Identifier.SyntacticKey.keyLeftFooter:
                    headerFooter_[((int)FOTBuilder.HF.leftHF | (int)FOTBuilder.HF.footerHF) >> nPageTypeBits] = sosofo;
                    return;
                case Identifier.SyntacticKey.keyCenterFooter:
                    headerFooter_[((int)FOTBuilder.HF.centerHF | (int)FOTBuilder.HF.footerHF) >> nPageTypeBits] = sosofo;
                    return;
                case Identifier.SyntacticKey.keyRightFooter:
                    headerFooter_[((int)FOTBuilder.HF.rightHF | (int)FOTBuilder.HF.footerHF) >> nPageTypeBits] = sosofo;
                    return;
                default:
                    break;
            }
        }
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

    public override void traceSubObjects(Collector c)
    {
        base.traceSubObjects(c);
        if (addressObj_ != null)
            c.trace(addressObj_);
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key) && key == Identifier.SyntacticKey.keyDestination)
            return true;
        return false;
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (obj == null)
            return;
        AddressObj? address = obj.asAddress();
        if (address == null)
        {
            if (obj != interp.makeFalse())
            {
                interp.invalidCharacteristicValue(ident, loc);
                return;
            }
            address = interp.makeAddressNone();
        }
        addressObj_ = address;
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
        c.nic_ = nic_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        context.currentFOTBuilder().externalGraphic(nic_);
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyIsDisplay:
                case Identifier.SyntacticKey.keyScale:
                case Identifier.SyntacticKey.keyMaxWidth:
                case Identifier.SyntacticKey.keyMaxHeight:
                case Identifier.SyntacticKey.keyEntitySystemId:
                case Identifier.SyntacticKey.keyNotationSystemId:
                case Identifier.SyntacticKey.keyPositionPointX:
                case Identifier.SyntacticKey.keyPositionPointY:
                case Identifier.SyntacticKey.keyEscapementDirection:
                case Identifier.SyntacticKey.keyBreakBeforePriority:
                case Identifier.SyntacticKey.keyBreakAfterPriority:
                    return true;
                default:
                    break;
            }
        }
        return isDisplayNIC(ident);
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (setDisplayNIC(ref nic_, ident, obj, loc, interp))
            return;
        if (ident == null || obj == null)
            return;
        Identifier.SyntacticKey key;
        if (ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyIsDisplay:
                    interp.convertBooleanC(obj, ident, loc, out nic_.isDisplay);
                    return;
                case Identifier.SyntacticKey.keyScale:
                    {
                        double d;
                        if (obj.realValue(out d))
                        {
                            nic_.scaleType = FOTBuilder.Symbol.symbolFalse;
                            nic_.scale[0] = d;
                            nic_.scale[1] = d;
                        }
                        else if (obj.asSymbol() != null)
                        {
                            interp.convertEnumC(obj, ident, loc, out nic_.scaleType);
                        }
                        else
                        {
                            PairObj? pair = obj.asPair();
                            if (pair != null && pair.car()!.realValue(out nic_.scale[0]))
                            {
                                pair = pair.cdr()?.asPair();
                                if (pair != null && pair.car()!.realValue(out nic_.scale[1]) && pair.cdr()!.isNil())
                                    nic_.scaleType = FOTBuilder.Symbol.symbolFalse;
                                else
                                    interp.invalidCharacteristicValue(ident, loc);
                            }
                            else
                                interp.invalidCharacteristicValue(ident, loc);
                        }
                    }
                    return;
                case Identifier.SyntacticKey.keyMaxWidth:
                    if (interp.convertLengthSpecC(obj, ident, loc, ref nic_.maxWidth))
                        nic_.hasMaxWidth = true;
                    return;
                case Identifier.SyntacticKey.keyMaxHeight:
                    if (interp.convertLengthSpecC(obj, ident, loc, ref nic_.maxHeight))
                        nic_.hasMaxHeight = true;
                    return;
                case Identifier.SyntacticKey.keyEntitySystemId:
                    interp.convertStringC(obj, ident, loc, out nic_.entitySystemId);
                    return;
                case Identifier.SyntacticKey.keyNotationSystemId:
                    interp.convertStringC(obj, ident, loc, out nic_.notationSystemId);
                    return;
                case Identifier.SyntacticKey.keyPositionPointX:
                    interp.convertLengthSpecC(obj, ident, loc, ref nic_.positionPointX);
                    return;
                case Identifier.SyntacticKey.keyPositionPointY:
                    interp.convertLengthSpecC(obj, ident, loc, ref nic_.positionPointY);
                    return;
                case Identifier.SyntacticKey.keyEscapementDirection:
                    interp.convertEnumC(obj, ident, loc, out nic_.escapementDirection);
                    return;
                case Identifier.SyntacticKey.keyBreakBeforePriority:
                    interp.convertIntegerC(obj, ident, loc, out nic_.breakBeforePriority);
                    return;
                case Identifier.SyntacticKey.keyBreakAfterPriority:
                    interp.convertIntegerC(obj, ident, loc, out nic_.breakAfterPriority);
                    return;
                default:
                    break;
            }
        }
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
        c.nic_ = nic_;
        return c;
    }

    public override bool isRule() { return true; }

    public override void processInner(ProcessContext context)
    {
        context.currentFOTBuilder().rule(nic_);
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyOrientation:
                case Identifier.SyntacticKey.keyLength:
                case Identifier.SyntacticKey.keyBreakBeforePriority:
                case Identifier.SyntacticKey.keyBreakAfterPriority:
                    return true;
                default:
                    break;
            }
        }
        return isDisplayNIC(ident);
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (setDisplayNIC(ref nic_, ident, obj, loc, interp))
            return;
        if (ident == null || obj == null)
            return;
        Identifier.SyntacticKey key;
        if (ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyOrientation:
                    interp.convertEnumC(obj, ident, loc, out nic_.orientation);
                    return;
                case Identifier.SyntacticKey.keyLength:
                    if (interp.convertLengthSpecC(obj, ident, loc, ref nic_.length))
                        nic_.hasLength = true;
                    return;
                case Identifier.SyntacticKey.keyBreakBeforePriority:
                    interp.convertIntegerC(obj, ident, loc, out nic_.breakBeforePriority);
                    return;
                case Identifier.SyntacticKey.keyBreakAfterPriority:
                    interp.convertIntegerC(obj, ident, loc, out nic_.breakAfterPriority);
                    return;
                default:
                    break;
            }
        }
    }

    public new bool ruleStyle(ProcessContext context, out StyleObj? style)
    {
        style = base.style();
        return true;
    }
}

// Leader flow object
public class LeaderFlowObj : CompoundFlowObj
{
    private FOTBuilder.LeaderNIC nic_ = new FOTBuilder.LeaderNIC();

    public override FlowObj copy(Interpreter interp)
    {
        LeaderFlowObj c = new LeaderFlowObj();
        c.setStyle(style());
        c.setContent(content());
        c.nic_ = nic_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startLeader(nic_);
        base.processInner(context);
        fotb.endLeader();
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyLength:
                case Identifier.SyntacticKey.keyBreakBeforePriority:
                case Identifier.SyntacticKey.keyBreakAfterPriority:
                    return true;
                default:
                    break;
            }
        }
        return false;
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (ident == null || obj == null)
            return;
        Identifier.SyntacticKey key;
        if (ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyLength:
                    if (interp.convertLengthSpecC(obj, ident, loc, ref nic_.length))
                        nic_.hasLength = true;
                    return;
                case Identifier.SyntacticKey.keyBreakBeforePriority:
                    interp.convertIntegerC(obj, ident, loc, out nic_.breakBeforePriority);
                    return;
                case Identifier.SyntacticKey.keyBreakAfterPriority:
                    interp.convertIntegerC(obj, ident, loc, out nic_.breakAfterPriority);
                    return;
                default:
                    break;
            }
        }
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
        c.nic_ = nic_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startLineField(nic_);
        base.processInner(context);
        fotb.endLineField();
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyBreakBeforePriority:
                case Identifier.SyntacticKey.keyBreakAfterPriority:
                    return true;
                default:
                    break;
            }
        }
        return false;
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (ident == null || obj == null)
            return;
        Identifier.SyntacticKey key;
        if (ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyBreakBeforePriority:
                    interp.convertIntegerC(obj, ident, loc, out nic_.breakBeforePriority);
                    return;
                case Identifier.SyntacticKey.keyBreakAfterPriority:
                    interp.convertIntegerC(obj, ident, loc, out nic_.breakAfterPriority);
                    return;
                default:
                    break;
            }
        }
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
