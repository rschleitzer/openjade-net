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
    public virtual bool setImplicitChar(ELObj? obj, Location loc, Interpreter interp) { return false; }
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
        else
            context.processChildren(context.vm().interp.initialProcessingMode());
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
        FOTBuilder?[] hf_fotb = new FOTBuilder?[FOTBuilder.nHF];
        fotb.startSimplePageSequence(hf_fotb);

        // Map part index (0-5) to HF flags for hf_fotb slot calculation
        int[] partToHF = {
            (int)FOTBuilder.HF.leftHF,                                    // 0 = leftFooter
            (int)FOTBuilder.HF.leftHF | (int)FOTBuilder.HF.headerHF,     // 1 = leftHeader
            (int)FOTBuilder.HF.centerHF,                                  // 2 = centerFooter
            (int)FOTBuilder.HF.centerHF | (int)FOTBuilder.HF.headerHF,   // 3 = centerHeader
            (int)FOTBuilder.HF.rightHF,                                   // 4 = rightFooter
            (int)FOTBuilder.HF.rightHF | (int)FOTBuilder.HF.headerHF     // 5 = rightHeader
        };

        // Process header/footer sosofos for each page type
        for (int i = 0; i < (1 << nPageTypeBits); i++)  // 4 page types
        {
            context.setPageType((uint)i);
            for (int j = 0; j < nHeaderFooterParts; j++)  // 6 parts
            {
                if (headerFooter_[j] != null)
                {
                    int hfSlot = i | partToHF[j];
                    context.pushPrincipalPort(hf_fotb[hfSlot]);
                    headerFooter_[j]!.process(context);
                    context.popPrincipalPort();
                }
            }
        }

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
        // Part indices (0-5) matching processInner's partToHF mapping
        const int partLeftFooter = 0;
        const int partLeftHeader = 1;
        const int partCenterFooter = 2;
        const int partCenterHeader = 3;
        const int partRightFooter = 4;
        const int partRightHeader = 5;

        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyLeftHeader:
                    headerFooter_[partLeftHeader] = sosofo;
                    return;
                case Identifier.SyntacticKey.keyCenterHeader:
                    headerFooter_[partCenterHeader] = sosofo;
                    return;
                case Identifier.SyntacticKey.keyRightHeader:
                    headerFooter_[partRightHeader] = sosofo;
                    return;
                case Identifier.SyntacticKey.keyLeftFooter:
                    headerFooter_[partLeftFooter] = sosofo;
                    return;
                case Identifier.SyntacticKey.keyCenterFooter:
                    headerFooter_[partCenterFooter] = sosofo;
                    return;
                case Identifier.SyntacticKey.keyRightFooter:
                    headerFooter_[partRightFooter] = sosofo;
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
        c.nic_ = nic_;
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

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyCh:
                case Identifier.SyntacticKey.keyChar:
                case Identifier.SyntacticKey.keyGlyphId:
                case Identifier.SyntacticKey.keyIsSpace:
                case Identifier.SyntacticKey.keyIsRecordEnd:
                case Identifier.SyntacticKey.keyIsInputTab:
                case Identifier.SyntacticKey.keyIsInputWhitespace:
                case Identifier.SyntacticKey.keyIsPunct:
                case Identifier.SyntacticKey.keyIsDropAfterLineBreak:
                case Identifier.SyntacticKey.keyIsDropUnlessBeforeLineBreak:
                case Identifier.SyntacticKey.keyScript:
                case Identifier.SyntacticKey.keyMathClass:
                case Identifier.SyntacticKey.keyMathFontPosture:
                case Identifier.SyntacticKey.keyStretchFactor:
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
                case Identifier.SyntacticKey.keyCh:
                case Identifier.SyntacticKey.keyChar:
                    {
                        Char c;
                        if (interp.convertCharC(obj, ident, loc, out c))
                            ch_ = c;
                    }
                    return;
                case Identifier.SyntacticKey.keyGlyphId:
                    {
                        string? s;
                        if (interp.convertPublicIdC(obj, ident, loc, out s))
                            nic_.glyphId = new FOTBuilder.GlyphId(s);
                    }
                    return;
                case Identifier.SyntacticKey.keyIsSpace:
                    interp.convertBooleanC(obj, ident, loc, out nic_.isSpace);
                    return;
                case Identifier.SyntacticKey.keyIsRecordEnd:
                    interp.convertBooleanC(obj, ident, loc, out nic_.isRecordEnd);
                    return;
                case Identifier.SyntacticKey.keyIsInputTab:
                    interp.convertBooleanC(obj, ident, loc, out nic_.isInputTab);
                    return;
                case Identifier.SyntacticKey.keyIsInputWhitespace:
                    interp.convertBooleanC(obj, ident, loc, out nic_.isInputWhitespace);
                    return;
                case Identifier.SyntacticKey.keyIsPunct:
                    interp.convertBooleanC(obj, ident, loc, out nic_.isPunct);
                    return;
                case Identifier.SyntacticKey.keyIsDropAfterLineBreak:
                    interp.convertBooleanC(obj, ident, loc, out nic_.isDropAfterLineBreak);
                    return;
                case Identifier.SyntacticKey.keyIsDropUnlessBeforeLineBreak:
                    interp.convertBooleanC(obj, ident, loc, out nic_.isDropUnlessBeforeLineBreak);
                    return;
                case Identifier.SyntacticKey.keyScript:
                    {
                        string? s;
                        if (interp.convertPublicIdC(obj, ident, loc, out s))
                            nic_.script = s;
                    }
                    return;
                case Identifier.SyntacticKey.keyMathClass:
                    interp.convertEnumC(obj, ident, loc, out nic_.mathClass);
                    return;
                case Identifier.SyntacticKey.keyMathFontPosture:
                    interp.convertEnumC(obj, ident, loc, out nic_.mathFontPosture);
                    return;
                case Identifier.SyntacticKey.keyStretchFactor:
                    interp.convertRealC(obj, ident, loc, out nic_.stretchFactor);
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
    private StyleObj? beforeRowBorder_;
    private StyleObj? afterRowBorder_;
    private StyleObj? beforeColumnBorder_;
    private StyleObj? afterColumnBorder_;

    public override FlowObj copy(Interpreter interp)
    {
        TableFlowObj c = new TableFlowObj();
        c.setStyle(style());
        c.setContent(content());
        c.nic_ = nic_;
        c.beforeRowBorder_ = beforeRowBorder_;
        c.afterRowBorder_ = afterRowBorder_;
        c.beforeColumnBorder_ = beforeColumnBorder_;
        c.afterColumnBorder_ = afterColumnBorder_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        context.startTable();
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startTable(nic_);
        base.processInner(context);
        if (context.inTableRow())
            context.endTableRow();
        context.endTable();
        fotb.endTable();
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyBeforeRowBorder:
                case Identifier.SyntacticKey.keyAfterRowBorder:
                case Identifier.SyntacticKey.keyBeforeColumnBorder:
                case Identifier.SyntacticKey.keyAfterColumnBorder:
                case Identifier.SyntacticKey.keyTableWidth:
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
        if (!ident.syntacticKey(out key))
            return;
        if (key == Identifier.SyntacticKey.keyTableWidth)
        {
            if (obj == interp.makeFalse())
                nic_.widthType = FOTBuilder.TableNIC.WidthType.widthMinimum;
            else if (interp.convertLengthSpecC(obj, ident, loc, ref nic_.width))
                nic_.widthType = FOTBuilder.TableNIC.WidthType.widthExplicit;
            return;
        }
        StyleObj? style = null;
        SosofoObj? sosofo = obj.asSosofo();
        if (sosofo == null || !sosofo.tableBorderStyle(out style))
        {
            bool b;
            if (!interp.convertBooleanC(obj, ident, loc, out b))
                return;
            style = b ? interp.borderTrueStyle() : interp.borderFalseStyle();
        }
        switch (key)
        {
            case Identifier.SyntacticKey.keyBeforeRowBorder:
                beforeRowBorder_ = style;
                break;
            case Identifier.SyntacticKey.keyAfterRowBorder:
                afterRowBorder_ = style;
                break;
            case Identifier.SyntacticKey.keyBeforeColumnBorder:
                beforeColumnBorder_ = style;
                break;
            case Identifier.SyntacticKey.keyAfterColumnBorder:
                afterColumnBorder_ = style;
                break;
        }
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
        context.startTablePart();
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startTablePart(nic_, out FOTBuilder? header, out FOTBuilder? footer);
        var fotbs = new System.Collections.Generic.List<FOTBuilder?> { header, footer };
        var labels = new System.Collections.Generic.List<SymbolObj>
        {
            context.vm().interp!.portName(Interpreter.PortName.portHeader),
            context.vm().interp!.portName(Interpreter.PortName.portFooter)
        };
        context.pushPorts(true, labels, fotbs);
        base.processInner(context);
        context.popPorts();
        if (context.inTableRow())
            context.endTableRow();
        context.endTablePart();
        fotb.endTablePart();
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (ident == null || obj == null)
            return;
        setDisplayNIC(ref nic_, ident, obj, loc, interp);
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        if (!isDisplayNIC(ident))
            return false;
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            if (key == Identifier.SyntacticKey.keyPositionPreference)
                return false;
        }
        return true;
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

    // Table row doesn't push its own style - the style is pushed via startTableRow
    public override void pushStyle(ProcessContext context, ref uint flags)
    {
        // Do nothing - style is handled specially for table rows
    }

    public override void popStyle(ProcessContext context, uint flags)
    {
        // Do nothing - style is handled specially for table rows
    }

    public override void processInner(ProcessContext context)
    {
        if (!context.inTable())
        {
            context.vm().interp?.message(InterpreterMessages.tableRowOutsideTable);
            base.processInner(context);
            return;
        }
        if (context.inTableRow())
            context.endTableRow();
        context.startTableRow(style());
        base.processInner(context);
        if (context.inTableRow())
            context.endTableRow();
    }
}

// Table column flow object
public class TableColumnFlowObj : FlowObj
{
    private FOTBuilder.TableColumnNIC nic_ = new FOTBuilder.TableColumnNIC();
    private bool hasColumnNumber_;

    public override FlowObj copy(Interpreter interp)
    {
        TableColumnFlowObj c = new TableColumnFlowObj();
        c.setStyle(style());
        c.nic_ = nic_;
        c.hasColumnNumber_ = hasColumnNumber_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        if (hasColumnNumber_)
        {
            context.currentFOTBuilder().tableColumn(nic_);
            context.addTableColumn(nic_.columnIndex, nic_.nColumnsSpanned, style());
        }
        else
        {
            FOTBuilder.TableColumnNIC nic = nic_;
            nic.columnIndex = context.currentTableColumn();
            context.currentFOTBuilder().tableColumn(nic);
            context.addTableColumn(nic.columnIndex, nic_.nColumnsSpanned, style());
        }
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyColumnNumber:
                case Identifier.SyntacticKey.keyNColumnsSpanned:
                case Identifier.SyntacticKey.keyWidth:
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
                case Identifier.SyntacticKey.keyColumnNumber:
                case Identifier.SyntacticKey.keyNColumnsSpanned:
                    {
                        long n;
                        if (!interp.convertIntegerC(obj, ident, loc, out n))
                            return;
                        if (n <= 0)
                        {
                            interp.invalidCharacteristicValue(ident, loc);
                            return;
                        }
                        if (key == Identifier.SyntacticKey.keyColumnNumber)
                        {
                            nic_.columnIndex = (uint)(n - 1);
                            hasColumnNumber_ = true;
                        }
                        else
                            nic_.nColumnsSpanned = (uint)n;
                    }
                    return;
                case Identifier.SyntacticKey.keyWidth:
                    {
                        FOTBuilder.LengthSpec len = new FOTBuilder.LengthSpec();
                        if (interp.convertLengthSpecC(obj, ident, loc, ref len))
                        {
                            nic_.width = new FOTBuilder.TableLengthSpec { length = len.length };
                            nic_.hasWidth = true;
                        }
                    }
                    return;
                default:
                    break;
            }
        }
    }
}

// Table cell flow object
public class TableCellFlowObj : CompoundFlowObj
{
    private FOTBuilder.TableCellNIC nic_ = new FOTBuilder.TableCellNIC();
    private bool hasColumnNumber_;
    private bool startsRow_;
    private bool endsRow_;

    public TableCellFlowObj(bool missing = false)
    {
        if (missing)
            nic_.missing = true;
    }

    public override FlowObj copy(Interpreter interp)
    {
        TableCellFlowObj c = new TableCellFlowObj();
        c.setStyle(style());
        c.setContent(content());
        c.nic_ = nic_;
        c.hasColumnNumber_ = hasColumnNumber_;
        c.startsRow_ = startsRow_;
        c.endsRow_ = endsRow_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        if (!context.inTable())
        {
            base.processInner(context);
            return;
        }
        FOTBuilder fotb = context.currentFOTBuilder();
        if (!hasColumnNumber_)
        {
            FOTBuilder.TableCellNIC nic = nic_;
            nic.columnIndex = context.currentTableColumn();
            fotb.startTableCell(nic);
            if (!nic_.missing)
                context.noteTableCell(nic.columnIndex, nic.nColumnsSpanned, nic.nRowsSpanned);
        }
        else
        {
            fotb.startTableCell(nic_);
            if (!nic_.missing)
                context.noteTableCell(nic_.columnIndex, nic_.nColumnsSpanned, nic_.nRowsSpanned);
        }
        base.processInner(context);
        fotb.endTableCell();
        if (endsRow_)
            context.endTableRow();
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyNRowsSpanned:
                    return true;
                default:
                    break;
            }
        }
        return false;
    }

    public override bool hasPseudoNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyColumnNumber:
                case Identifier.SyntacticKey.keyNColumnsSpanned:
                case Identifier.SyntacticKey.keyIsStartsRow:
                case Identifier.SyntacticKey.keyIsEndsRow:
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
                case Identifier.SyntacticKey.keyIsStartsRow:
                    interp.convertBooleanC(obj, ident, loc, out startsRow_);
                    return;
                case Identifier.SyntacticKey.keyIsEndsRow:
                    interp.convertBooleanC(obj, ident, loc, out endsRow_);
                    return;
                case Identifier.SyntacticKey.keyColumnNumber:
                case Identifier.SyntacticKey.keyNColumnsSpanned:
                case Identifier.SyntacticKey.keyNRowsSpanned:
                    {
                        long n;
                        if (!interp.convertIntegerC(obj, ident, loc, out n))
                            return;
                        if (n <= 0)
                        {
                            interp.invalidCharacteristicValue(ident, loc);
                            return;
                        }
                        if (key == Identifier.SyntacticKey.keyColumnNumber)
                        {
                            nic_.columnIndex = (uint)(n - 1);
                            hasColumnNumber_ = true;
                        }
                        else if (key == Identifier.SyntacticKey.keyNColumnsSpanned)
                            nic_.nColumnsSpanned = (uint)n;
                        else
                            nic_.nRowsSpanned = (uint)n;
                    }
                    return;
                default:
                    break;
            }
        }
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

    public override void process(ProcessContext context)
    {
        // Table borders don't process themselves
    }

    public override void processInner(ProcessContext context)
    {
        // Table borders don't process themselves
    }

    public override bool tableBorderStyle(out StyleObj? style)
    {
        style = this.style();
        return true;
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
        var fotbs = new System.Collections.Generic.List<FOTBuilder?> { num, denom };

        // Get fraction bar style from inherited characteristic
        var dep = new System.Collections.Generic.List<nuint>();
        StyleObj? fractionBarStyle = null;
        ConstPtr<InheritedC>? fractionBarC = context.vm().interp?.fractionBarC();
        if (fractionBarC != null && !fractionBarC.isNull())
        {
            ELObj? obj = context.currentStyleStack().actual(fractionBarC, context.vm().interp!, dep);
            SosofoObj? sosofo = obj?.asSosofo();
            if (sosofo != null)
                sosofo.ruleStyle(context, out fractionBarStyle);
        }
        if (fractionBarStyle != null)
            context.currentStyleStack().push(fractionBarStyle, context.vm(), fotb);
        fotb.fractionBar();
        if (fractionBarStyle != null)
            context.currentStyleStack().pop();

        var labels = new System.Collections.Generic.List<SymbolObj>
        {
            context.vm().interp!.portName(Interpreter.PortName.portNumerator),
            context.vm().interp!.portName(Interpreter.PortName.portDenominator)
        };
        context.pushPorts(false, labels, fotbs);
        // Fraction flow object doesn't have principal port
        base.processInner(context);
        context.popPorts();
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

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startScript(out FOTBuilder? preSup, out FOTBuilder? preSub,
                         out FOTBuilder? postSup, out FOTBuilder? postSub,
                         out FOTBuilder? midSup, out FOTBuilder? midSub);
        var fotbs = new System.Collections.Generic.List<FOTBuilder?>
            { preSup, preSub, postSup, postSub, midSup, midSub };
        var labels = new System.Collections.Generic.List<SymbolObj>
        {
            context.vm().interp!.portName(Interpreter.PortName.portPreSup),
            context.vm().interp!.portName(Interpreter.PortName.portPreSub),
            context.vm().interp!.portName(Interpreter.PortName.portPostSup),
            context.vm().interp!.portName(Interpreter.PortName.portPostSub),
            context.vm().interp!.portName(Interpreter.PortName.portMidSup),
            context.vm().interp!.portName(Interpreter.PortName.portMidSub)
        };
        context.pushPorts(true, labels, fotbs);
        base.processInner(context);
        context.popPorts();
        fotb.endScript();
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

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startMark(out FOTBuilder? overMark, out FOTBuilder? underMark);
        var fotbs = new System.Collections.Generic.List<FOTBuilder?> { overMark, underMark };
        var labels = new System.Collections.Generic.List<SymbolObj>
        {
            context.vm().interp!.portName(Interpreter.PortName.portOverMark),
            context.vm().interp!.portName(Interpreter.PortName.portUnderMark)
        };
        context.pushPorts(true, labels, fotbs);
        base.processInner(context);
        context.popPorts();
        fotb.endMark();
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

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startFence(out FOTBuilder? open, out FOTBuilder? close);
        var fotbs = new System.Collections.Generic.List<FOTBuilder?> { open, close };
        var labels = new System.Collections.Generic.List<SymbolObj>
        {
            context.vm().interp!.portName(Interpreter.PortName.portOpen),
            context.vm().interp!.portName(Interpreter.PortName.portClose)
        };
        context.pushPorts(true, labels, fotbs);
        base.processInner(context);
        context.popPorts();
        fotb.endFence();
    }
}

// Radical flow object
public class RadicalFlowObj : CompoundFlowObj
{
    private SosofoObj? radical_;

    public override FlowObj copy(Interpreter interp)
    {
        RadicalFlowObj c = new RadicalFlowObj();
        c.setStyle(style());
        c.setContent(content());
        c.radical_ = radical_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startRadical(out FOTBuilder? degree);
        StyleObj? style;
        FOTBuilder.CharacterNIC nic = new FOTBuilder.CharacterNIC();
        if (radical_ != null && radical_.characterStyle(context, out style, nic))
        {
            if (style != null)
                context.currentStyleStack().push(style, context.vm(), fotb);
            fotb.radicalRadical(nic);
            if (style != null)
                context.currentStyleStack().pop();
        }
        else
            fotb.radicalRadicalDefaulted();
        var fotbs = new System.Collections.Generic.List<FOTBuilder?> { degree };
        var labels = new System.Collections.Generic.List<SymbolObj>
        {
            context.vm().interp!.portName(Interpreter.PortName.portDegree)
        };
        context.pushPorts(true, labels, fotbs);
        base.processInner(context);
        context.popPorts();
        fotb.endRadical();
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        return ident != null && ident.syntacticKey(out key) && key == Identifier.SyntacticKey.keyRadical;
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (obj == null)
            return;
        radical_ = obj.asSosofo();
        if (radical_ == null || !radical_.isCharacter())
        {
            interp.invalidCharacteristicValue(ident, loc);
        }
    }

    public override void traceSubObjects(Collector c)
    {
        c.trace(radical_);
        base.traceSubObjects(c);
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

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startMathOperator(out FOTBuilder? oper, out FOTBuilder? lowerLimit, out FOTBuilder? upperLimit);
        var fotbs = new System.Collections.Generic.List<FOTBuilder?> { oper, lowerLimit, upperLimit };
        var labels = new System.Collections.Generic.List<SymbolObj>
        {
            context.vm().interp!.portName(Interpreter.PortName.portOperator),
            context.vm().interp!.portName(Interpreter.PortName.portLowerLimit),
            context.vm().interp!.portName(Interpreter.PortName.portUpperLimit)
        };
        context.pushPorts(true, labels, fotbs);
        base.processInner(context);
        context.popPorts();
        fotb.endMathOperator();
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
        c.nic_ = nic_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startGrid(nic_);
        base.processInner(context);
        fotb.endGrid();
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyGridNColumns:
                case Identifier.SyntacticKey.keyGridNRows:
                    return true;
            }
        }
        return base.hasNonInheritedC(ident);
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? value, Location loc, Interpreter interp)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            long n;
            switch (key)
            {
                case Identifier.SyntacticKey.keyGridNColumns:
                    if (value != null && value.exactIntegerValue(out n) && n > 0)
                        nic_.nColumns = (uint)n;
                    return;
                case Identifier.SyntacticKey.keyGridNRows:
                    if (value != null && value.exactIntegerValue(out n) && n > 0)
                        nic_.nRows = (uint)n;
                    return;
            }
        }
        base.setNonInheritedC(ident, value, loc, interp);
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
        c.nic_ = nic_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startGridCell(nic_);
        base.processInner(context);
        fotb.endGridCell();
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            switch (key)
            {
                case Identifier.SyntacticKey.keyColumnNumber:
                case Identifier.SyntacticKey.keyRowNumber:
                    return true;
            }
        }
        return base.hasNonInheritedC(ident);
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? value, Location loc, Interpreter interp)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key))
        {
            long n;
            switch (key)
            {
                case Identifier.SyntacticKey.keyColumnNumber:
                    if (value != null && value.exactIntegerValue(out n) && n > 0)
                        nic_.columnNumber = (uint)n;
                    return;
                case Identifier.SyntacticKey.keyRowNumber:
                    if (value != null && value.exactIntegerValue(out n) && n > 0)
                        nic_.rowNumber = (uint)n;
                    return;
            }
        }
        base.setNonInheritedC(ident, value, loc, interp);
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
    public class NIC
    {
        public bool hasPrincipalMode = false;
        public FOTBuilder.MultiMode principalMode = new FOTBuilder.MultiMode();
        public System.Collections.Generic.List<FOTBuilder.MultiMode> namedModes = new();
    }

    private NIC nic_ = new NIC();

    public override FlowObj copy(Interpreter interp)
    {
        MultiModeFlowObj c = new MultiModeFlowObj();
        c.setStyle(style());
        c.setContent(content());
        c.nic_ = nic_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startMultiMode(nic_.hasPrincipalMode ? nic_.principalMode : null,
                            nic_.namedModes,
                            out System.Collections.Generic.List<FOTBuilder> builders);
        var portSyms = new System.Collections.Generic.List<SymbolObj>();
        foreach (var mode in nic_.namedModes)
            portSyms.Add(context.vm().interp!.makeSymbol(mode.name));
        var fotbs = new System.Collections.Generic.List<FOTBuilder?>();
        foreach (var b in builders)
            fotbs.Add(b);
        context.pushPorts(nic_.hasPrincipalMode, portSyms, fotbs);
        base.processInner(context);
        context.popPorts();
        fotb.endMultiMode();
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        return ident != null && ident.syntacticKey(out key) && key == Identifier.SyntacticKey.keyMultiModes;
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (obj == null)
            return;
        while (!obj.isNil())
        {
            PairObj? pair = obj.asPair();
            if (pair == null || !handleMultiModesMember(ident, pair.car(), loc, interp))
            {
                interp.invalidCharacteristicValue(ident, loc);
                return;
            }
            obj = pair.cdr();
        }
    }

    private bool handleMultiModesMember(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (obj == null)
            return false;
        if (obj == interp.makeFalse())
        {
            nic_.hasPrincipalMode = true;
            return true;
        }
        SymbolObj? sym = obj.asSymbol();
        if (sym != null)
        {
            nic_.namedModes.Add(new FOTBuilder.MultiMode { name = sym.name() });
            return true;
        }
        PairObj? pair = obj.asPair();
        if (pair == null)
            return false;
        ELObj spec = pair.car()!;
        pair = pair.cdr()?.asPair();
        if (pair == null || !pair.cdr()!.isNil())
            return false;
        Char[]? s;
        nuint n;
        if (!pair.car()!.stringData(out s, out n) || s == null)
            return false;
        if (spec == interp.makeFalse())
        {
            nic_.hasPrincipalMode = true;
            nic_.principalMode.hasDesc = true;
            nic_.principalMode.desc = new StringC(s, n);
            return true;
        }
        sym = spec.asSymbol();
        if (sym == null)
            return false;
        var mode = new FOTBuilder.MultiMode
        {
            name = sym.name(),
            desc = new StringC(s, n),
            hasDesc = true
        };
        nic_.namedModes.Add(mode);
        return true;
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

// Unknown flow object - for declared-flow-object-class instances
public class UnknownFlowObj : CompoundFlowObj
{
    public override FlowObj copy(Interpreter interp)
    {
        UnknownFlowObj c = new UnknownFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        // Accept any non-inherited characteristic except label and content-map
        Identifier.SyntacticKey syn;
        if (ident != null && ident.syntacticKey(out syn))
        {
            if (syn == Identifier.SyntacticKey.keyLabel || syn == Identifier.SyntacticKey.keyContentMap)
                return false;
        }
        // Don't accept inherited characteristics
        if (ident != null && ident.inheritedC() != null)
            return false;
        return true;
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? value, Location loc, Interpreter interp)
    {
        // Silently ignore all non-inherited characteristics
    }
}

// Formatting instruction flow object
public class FormattingInstructionFlowObj : FlowObj
{
    private StringC data_ = new StringC();

    public override FlowObj copy(Interpreter interp)
    {
        FormattingInstructionFlowObj c = new FormattingInstructionFlowObj();
        c.setStyle(style());
        c.data_ = data_;
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        context.currentFOTBuilder().formattingInstruction(data_);
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        Identifier.SyntacticKey key;
        if (ident != null && ident.syntacticKey(out key) && key == Identifier.SyntacticKey.keyData)
            return true;
        return false;
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (obj != null)
            interp.convertStringC(obj, ident, loc, out data_);
    }
}

// Extension flow object value interface
public interface IExtensionFlowObjValue
{
    bool convertString(out StringC result);
    bool convertStringPairList(out System.Collections.Generic.List<StringC> v);
    bool convertStringList(out System.Collections.Generic.List<StringC> v);
    bool convertBoolean(out bool result);
}

// ELObj wrapper for extension flow object value
public class ELObjExtensionFlowObjValue : IExtensionFlowObjValue
{
    private Identifier? ident_;
    private ELObj? obj_;
    private Location loc_;
    private Interpreter interp_;

    public ELObjExtensionFlowObjValue(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        ident_ = ident;
        obj_ = obj;
        loc_ = loc;
        interp_ = interp;
    }

    public bool convertString(out StringC result)
    {
        return interp_.convertStringC(obj_!, ident_, loc_, out result);
    }

    public bool convertStringPairList(out System.Collections.Generic.List<StringC> v)
    {
        v = new System.Collections.Generic.List<StringC>();
        ELObj? obj = obj_;
        for (; ; )
        {
            if (obj == null || obj.isNil())
                return true;
            PairObj? pair = obj.asPair();
            if (pair == null)
                break;
            obj = pair.cdr();
            PairObj? att = pair.car()?.asPair();
            if (att == null)
                break;
            Char[]? s;
            nuint n;
            if (!att.car()!.stringData(out s, out n) || s == null)
                break;
            v.Add(new StringC(s, n));
            att = att.cdr()?.asPair();
            if (att == null || !att.car()!.stringData(out s, out n) || s == null || !att.cdr()!.isNil())
            {
                v.RemoveAt(v.Count - 1);
                break;
            }
            v.Add(new StringC(s, n));
        }
        interp_.invalidCharacteristicValue(ident_, loc_);
        return false;
    }

    public bool convertStringList(out System.Collections.Generic.List<StringC> v)
    {
        v = new System.Collections.Generic.List<StringC>();
        ELObj? obj = obj_;
        for (; ; )
        {
            if (obj == null || obj.isNil())
                return true;
            PairObj? pair = obj.asPair();
            if (pair == null)
                break;
            Char[]? s;
            nuint n;
            if (!pair.car()!.stringData(out s, out n) || s == null)
                break;
            v.Add(new StringC(s, n));
            obj = pair.cdr();
        }
        interp_.invalidCharacteristicValue(ident_, loc_);
        return false;
    }

    public bool convertBoolean(out bool result)
    {
        return interp_.convertBooleanC(obj_!, ident_, loc_, out result);
    }
}

// Extension flow object (non-compound)
public class ExtensionFlowObj : FlowObj
{
    private FOTBuilder.ExtensionFlowObj? fo_;

    public ExtensionFlowObj(FOTBuilder.ExtensionFlowObj fo)
    {
        fo_ = fo.copy();
    }

    public ExtensionFlowObj(ExtensionFlowObj other)
    {
        setStyle(other.style());
        fo_ = other.fo_?.copy();
    }

    public override FlowObj copy(Interpreter interp)
    {
        return new ExtensionFlowObj(this);
    }

    public override void processInner(ProcessContext context)
    {
        if (fo_ != null)
            context.currentFOTBuilder().extension(fo_, context.vm().currentNode);
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        return fo_ != null && ident != null && fo_.hasNIC(ident.name());
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (fo_ != null && ident != null)
            fo_.setNIC(ident.name(), new ELObjExtensionFlowObjValue(ident, obj, loc, interp));
    }
}

// Compound extension flow object (with ports)
public class CompoundExtensionFlowObj : CompoundFlowObj
{
    private FOTBuilder.CompoundExtensionFlowObj? fo_;

    public CompoundExtensionFlowObj(FOTBuilder.CompoundExtensionFlowObj fo)
    {
        fo_ = fo.copy()?.asCompoundExtensionFlowObj();
    }

    public CompoundExtensionFlowObj(CompoundExtensionFlowObj other)
    {
        setStyle(other.style());
        setContent(other.content());
        fo_ = other.fo_?.copy()?.asCompoundExtensionFlowObj();
    }

    public override FlowObj copy(Interpreter interp)
    {
        return new CompoundExtensionFlowObj(this);
    }

    public override void processInner(ProcessContext context)
    {
        if (fo_ == null)
        {
            base.processInner(context);
            return;
        }
        FOTBuilder fotb = context.currentFOTBuilder();
        var portNames = new System.Collections.Generic.List<StringC>();
        fo_.portNames(portNames);
        var fotbs = new System.Collections.Generic.List<FOTBuilder?>(portNames.Count);
        fotb.startExtension(fo_, context.vm().currentNode, fotbs);
        if (portNames.Count > 0)
        {
            var portSyms = new System.Collections.Generic.List<SymbolObj>();
            foreach (var name in portNames)
                portSyms.Add(context.vm().interp!.makeSymbol(name));
            context.pushPorts(fo_.hasPrincipalPort(), portSyms, fotbs);
            base.processInner(context);
            context.popPorts();
        }
        else
            base.processInner(context);
        fotb.endExtension(fo_);
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        return fo_ != null && ident != null && fo_.hasNIC(ident.name());
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (fo_ != null && ident != null)
            fo_.setNIC(ident.name(), new ELObjExtensionFlowObjValue(ident, obj, loc, interp));
    }
}
