// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Boolean inherited characteristic
public class BoolInheritedC : InheritedC
{
    protected bool value_;

    public BoolInheritedC(Identifier? ident, uint index, bool value)
        : base(ident, index)
    {
        value_ = value;
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        if (value_)
            return vm.interp.makeTrue();
        else
            return vm.interp.makeFalse();
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        return null;
    }
}

// Integer inherited characteristic
public class IntegerInheritedC : InheritedC
{
    protected long n_;

    public IntegerInheritedC(Identifier? ident, uint index, long n)
        : base(ident, index)
    {
        n_ = n;
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        return vm.interp.makeInteger(n_);
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        return null;
    }
}

// Length inherited characteristic
public class LengthInheritedC : InheritedC
{
    protected long size_;

    public LengthInheritedC(Identifier? ident, uint index, long size)
        : base(ident, index)
    {
        size_ = size;
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        return new LengthObj(size_);
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        return null;
    }
}

// Symbol inherited characteristic
public class SymbolInheritedC : InheritedC
{
    protected FOTBuilder.Symbol sym_;

    public SymbolInheritedC(Identifier? ident, uint index, FOTBuilder.Symbol sym)
        : base(ident, index)
    {
        sym_ = sym;
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        return vm.interp.cValueSymbol(sym_);
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        return null;
    }
}

// PublicId inherited characteristic
public class PublicIdInheritedC : InheritedC
{
    protected string? pubid_;

    public PublicIdInheritedC(Identifier? ident, uint index, string? pubid = null)
        : base(ident, index)
    {
        pubid_ = pubid;
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        if (pubid_ != null)
            return new StringObj(vm.interp.makeStringC(pubid_));
        else
            return vm.interp.makeFalse();
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        return null;
    }
}

// Letter2 (two-letter code) inherited characteristic
public class Letter2InheritedC : InheritedC
{
    protected ushort code_;

    public Letter2InheritedC(Identifier? ident, uint index, ushort code = 0)
        : base(ident, index)
    {
        code_ = code;
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        if (code_ != 0)
        {
            StringC buf = new StringC();
            buf.operatorPlusAssign((Char)((code_ >> 8) & 0xff));
            buf.operatorPlusAssign((Char)(code_ & 0xff));
            return vm.interp.makeSymbol(buf);
        }
        else
            return vm.interp.makeFalse();
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        return null;
    }
}

// LengthSpec inherited characteristic
public class LengthSpecInheritedC : InheritedC
{
    protected FOTBuilder.LengthSpec value_;

    public LengthSpecInheritedC(Identifier? ident, uint index, long n = 0)
        : base(ident, index)
    {
        value_ = new FOTBuilder.LengthSpec(n);
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        return vm.interp.makeLengthSpec(value_);
    }

    protected bool setValue(ELObj obj, Location loc, Interpreter interp)
    {
        return interp.convertLengthSpecC(obj, identifier(), loc, ref value_);
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        return null;
    }
}

// Optional LengthSpec inherited characteristic
public class OptLengthSpecInheritedC : InheritedC
{
    protected FOTBuilder.OptLengthSpec value_;

    public OptLengthSpecInheritedC(Identifier? ident, uint index)
        : base(ident, index)
    {
        value_ = new FOTBuilder.OptLengthSpec();
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        if (!value_.hasLength)
            return vm.interp.makeFalse();
        else
            return vm.interp.makeLengthSpec(value_.length);
    }

    protected bool setValue(ELObj obj, Location loc, Interpreter interp)
    {
        return interp.convertOptLengthSpecC(obj, identifier(), loc, ref value_);
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        return null;
    }
}

// String inherited characteristic
public class StringInheritedC : InheritedC
{
    protected StringC str_;

    public StringInheritedC(Identifier? ident, uint index, Char[] s, nuint n)
        : base(ident, index)
    {
        str_ = new StringC(s, n);
    }

    public StringInheritedC(Identifier? ident, uint index, StringC s)
        : base(ident, index)
    {
        str_ = new StringC(s);
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        return new StringObj(str_);
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        return null;
    }
}

// Generic boolean inherited characteristic with FOTBuilder setter
public class GenericBoolInheritedC : BoolInheritedC
{
    public delegate void Setter(FOTBuilder fotb, bool value);
    private Setter setter_;

    public GenericBoolInheritedC(Identifier? ident, uint index, Setter setter, bool value = false)
        : base(ident, index, value)
    {
        setter_ = setter;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        setter_(fotb, value_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        bool b;
        if (interp.convertBooleanC(obj, identifier(), loc, out b))
            return new ConstPtr<InheritedC>(new GenericBoolInheritedC(identifier(), index(), setter_, b));
        return null;
    }
}

// Generic length inherited characteristic with FOTBuilder setter
public class GenericLengthInheritedC : LengthInheritedC
{
    public delegate void Setter(FOTBuilder fotb, long size);
    private Setter setter_;

    public GenericLengthInheritedC(Identifier? ident, uint index, Setter setter, long size = 0)
        : base(ident, index, size)
    {
        setter_ = setter;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        setter_(fotb, size_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        long n;
        if (interp.convertLengthC(obj, identifier(), loc, out n))
            return new ConstPtr<InheritedC>(new GenericLengthInheritedC(identifier(), index(), setter_, n));
        return null;
    }
}

// Generic integer inherited characteristic with FOTBuilder setter
public class GenericIntegerInheritedC : IntegerInheritedC
{
    public delegate void Setter(FOTBuilder fotb, long n);
    protected Setter setter_;

    public GenericIntegerInheritedC(Identifier? ident, uint index, Setter setter, long n)
        : base(ident, index, n)
    {
        setter_ = setter;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        setter_(fotb, n_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        long n;
        if (interp.convertIntegerC(obj, identifier(), loc, out n))
            return new ConstPtr<InheritedC>(new GenericIntegerInheritedC(identifier(), index(), setter_, n));
        return null;
    }
}

// Generic maybe integer inherited characteristic (#f or positive integer)
public class GenericMaybeIntegerInheritedC : GenericIntegerInheritedC
{
    public GenericMaybeIntegerInheritedC(Identifier? ident, uint index, Setter setter, long n)
        : base(ident, index, setter, n)
    {
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        if (n_ == 0)
            return vm.interp.makeFalse();
        else
            return base.value(vm, style, dependencies);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        long n;
        if (interp.convertOptPositiveIntegerC(obj, identifier(), loc, out n))
            return new ConstPtr<InheritedC>(new GenericMaybeIntegerInheritedC(identifier(), index(), setter_, n));
        return null;
    }
}

// Generic symbol inherited characteristic with FOTBuilder setter
public class GenericSymbolInheritedC : SymbolInheritedC
{
    public delegate void Setter(FOTBuilder fotb, FOTBuilder.Symbol sym);
    private Setter setter_;

    public GenericSymbolInheritedC(Identifier? ident, uint index, Setter setter,
                                    FOTBuilder.Symbol sym = FOTBuilder.Symbol.symbolFalse)
        : base(ident, index, sym)
    {
        setter_ = setter;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        setter_(fotb, sym_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        FOTBuilder.Symbol sym;
        if (interp.convertEnumC(obj, identifier(), loc, out sym))
            return new ConstPtr<InheritedC>(new GenericSymbolInheritedC(identifier(), index(), setter_, sym));
        return null;
    }
}

// Generic LengthSpec inherited characteristic with FOTBuilder setter
public class GenericLengthSpecInheritedC : LengthSpecInheritedC
{
    public delegate void Setter(FOTBuilder fotb, FOTBuilder.LengthSpec value);
    private Setter setter_;

    public GenericLengthSpecInheritedC(Identifier? ident, uint index, Setter setter, long n = 0)
        : base(ident, index, n)
    {
        setter_ = setter;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        setter_(fotb, value_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        var copy = new GenericLengthSpecInheritedC(identifier(), index(), setter_);
        if (!copy.setValue(obj, loc, interp))
            return null;
        return new ConstPtr<InheritedC>(copy);
    }
}

// Generic optional LengthSpec inherited characteristic with FOTBuilder setter
public class GenericOptLengthSpecInheritedC : OptLengthSpecInheritedC
{
    public delegate void Setter(FOTBuilder fotb, FOTBuilder.OptLengthSpec value);
    private Setter setter_;

    public GenericOptLengthSpecInheritedC(Identifier? ident, uint index, Setter setter)
        : base(ident, index)
    {
        setter_ = setter;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        setter_(fotb, value_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        var copy = new GenericOptLengthSpecInheritedC(identifier(), index(), setter_);
        if (!copy.setValue(obj, loc, interp))
            return null;
        return new ConstPtr<InheritedC>(copy);
    }
}

// Generic PublicId inherited characteristic with FOTBuilder setter
public class GenericPublicIdInheritedC : PublicIdInheritedC
{
    public delegate void Setter(FOTBuilder fotb, string? pubid);
    private Setter setter_;

    public GenericPublicIdInheritedC(Identifier? ident, uint index, Setter setter, string? pubid = null)
        : base(ident, index, pubid)
    {
        setter_ = setter;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        setter_(fotb, pubid_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        string? pubid;
        if (interp.convertPublicIdC(obj, identifier(), loc, out pubid))
            return new ConstPtr<InheritedC>(new GenericPublicIdInheritedC(identifier(), index(), setter_, pubid));
        return null;
    }
}

// Generic Letter2 inherited characteristic with FOTBuilder setter
public class GenericLetter2InheritedC : Letter2InheritedC
{
    public delegate void Setter(FOTBuilder fotb, ushort code);
    private Setter setter_;

    public GenericLetter2InheritedC(Identifier? ident, uint index, Setter setter, ushort code = 0)
        : base(ident, index, code)
    {
        setter_ = setter;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        setter_(fotb, code_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        ushort code;
        if (interp.convertLetter2C(obj, identifier(), loc, out code))
            return new ConstPtr<InheritedC>(new GenericLetter2InheritedC(identifier(), index(), setter_, code));
        return null;
    }
}

// Generic InlineSpace inherited characteristic with FOTBuilder setter
public class GenericInlineSpaceInheritedC : InheritedC
{
    public delegate void Setter(FOTBuilder fotb, FOTBuilder.InlineSpace value);
    private FOTBuilder.InlineSpace value_;
    private Setter setter_;

    public GenericInlineSpaceInheritedC(Identifier? ident, uint index, Setter setter)
        : base(ident, index)
    {
        value_ = new FOTBuilder.InlineSpace();
        setter_ = setter;
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        return new InlineSpaceObj(value_);
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        setter_(fotb, value_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        var copy = new GenericInlineSpaceInheritedC(identifier(), index(), setter_);
        InlineSpaceObj? iso = obj.asInlineSpace();
        if (iso != null)
        {
            copy.value_ = iso.inlineSpace();
            return new ConstPtr<InheritedC>(copy);
        }
        else if (interp.convertLengthSpecC(obj, identifier(), loc, ref copy.value_.nominal))
        {
            copy.value_.min = copy.value_.nominal;
            copy.value_.max = copy.value_.nominal;
            return new ConstPtr<InheritedC>(copy);
        }
        return null;
    }
}

// Generic optional InlineSpace inherited characteristic with FOTBuilder setter
public class GenericOptInlineSpaceInheritedC : InheritedC
{
    public delegate void Setter(FOTBuilder fotb, FOTBuilder.OptInlineSpace value);
    private FOTBuilder.OptInlineSpace value_;
    private Setter setter_;

    public GenericOptInlineSpaceInheritedC(Identifier? ident, uint index, Setter setter)
        : base(ident, index)
    {
        value_ = new FOTBuilder.OptInlineSpace();
        setter_ = setter;
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        if (!value_.hasSpace)
            return vm.interp.makeFalse();
        else
            return new InlineSpaceObj(value_.space);
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        setter_(fotb, value_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        var copy = new GenericOptInlineSpaceInheritedC(identifier(), index(), setter_);
        InlineSpaceObj? iso = obj.asInlineSpace();
        if (iso != null)
        {
            copy.value_.space = iso.inlineSpace();
            copy.value_.hasSpace = true;
            return new ConstPtr<InheritedC>(copy);
        }
        else
        {
            FOTBuilder.OptLengthSpec res = new FOTBuilder.OptLengthSpec();
            if (interp.convertOptLengthSpecC(obj, identifier(), loc, ref res))
            {
                if (res.hasLength)
                {
                    copy.value_.space.nominal = res.length;
                    copy.value_.space.min = res.length;
                    copy.value_.space.max = res.length;
                    copy.value_.hasSpace = true;
                }
                else
                    copy.value_.hasSpace = false;
                return new ConstPtr<InheritedC>(copy);
            }
        }
        return null;
    }
}

// Font size inherited characteristic
public class FontSizeC : LengthInheritedC
{
    public FontSizeC(Identifier? ident, uint index, long size)
        : base(ident, index, size)
    {
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        fotb.setFontSize(size_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        long n;
        if (interp.convertLengthC(obj, identifier(), loc, out n))
            return new ConstPtr<InheritedC>(new FontSizeC(identifier(), index(), n));
        return null;
    }
}

// Font family name inherited characteristic
public class FontFamilyNameC : StringInheritedC
{
    public FontFamilyNameC(Identifier? ident, uint index, Char[] s, nuint n)
        : base(ident, index, s, n)
    {
    }

    public FontFamilyNameC(Identifier? ident, uint index, StringC s)
        : base(ident, index, s)
    {
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        fotb.setFontFamilyName(str_);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        Char[]? s = null;
        nuint n = 0;
        if (obj.stringData(out s, out n) && s != null)
            return new ConstPtr<InheritedC>(new FontFamilyNameC(identifier(), index(), s, n));
        invalidValue(loc, interp);
        return null;
    }
}

// Color inherited characteristic
public class ColorC : InheritedC
{
    private ColorObj? color_;

    public ColorC(Identifier? ident, uint index, ColorObj? color, Interpreter interp)
        : base(ident, index)
    {
        color_ = color;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        if (color_ != null)
            color_.set(fotb);
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        ColorObj? color;
        if (interp.convertColorC(obj, identifier(), loc, out color))
            return new ConstPtr<InheritedC>(new ColorC(identifier(), index(), color, interp));
        return null;
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        if (color_ == null)
            return vm.interp.makeFalse();
        else
            return color_;
    }
}

// Background color inherited characteristic
public class BackgroundColorC : InheritedC
{
    private ColorObj? color_;

    public BackgroundColorC(Identifier? ident, uint index, ColorObj? color, Interpreter interp)
        : base(ident, index)
    {
        color_ = color;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        if (color_ != null)
            color_.setBackground(fotb);
        else
            fotb.setBackgroundColor();
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        ColorObj? color;
        if (interp.convertOptColorC(obj, identifier(), loc, out color))
            return new ConstPtr<InheritedC>(new BackgroundColorC(identifier(), index(), color, interp));
        return null;
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        if (color_ != null)
            return color_;
        else
            return vm.interp.makeFalse();
    }
}

// Ignored inherited characteristic
public class IgnoredC : InheritedC
{
    private ELObj? value_;

    public IgnoredC(Identifier? ident, uint index, ELObj? value, Interpreter interp)
        : base(ident, index)
    {
        value_ = value;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        // Ignored - do nothing
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        return new ConstPtr<InheritedC>(new IgnoredC(identifier(), index(), obj, interp));
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        return value_;
    }
}

// Border inherited characteristic
public class BorderC : IgnoredC
{
    public BorderC(Identifier? ident, uint index, ELObj? value, Interpreter interp)
        : base(ident, index, value, interp)
    {
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        StyleObj? tem;
        SosofoObj? sosofo = obj.asSosofo();
        if (sosofo != null && sosofo.tableBorderStyle(out tem))
            return new ConstPtr<InheritedC>(new BorderC(identifier(), index(), obj, interp));
        bool b;
        if (interp.convertBooleanC(obj, identifier(), loc, out b))
        {
            if (b)
                obj = interp.makeTrue();
            else
                obj = interp.makeFalse();
            return new ConstPtr<InheritedC>(new BorderC(identifier(), index(), obj, interp));
        }
        return null;
    }
}

// Rule inherited characteristic
public class RuleC : IgnoredC
{
    public RuleC(Identifier? ident, uint index, ELObj? value, Interpreter interp)
        : base(ident, index, value, interp)
    {
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        SosofoObj? sosofo = obj.asSosofo();
        if (sosofo != null && sosofo.isRule())
            return new ConstPtr<InheritedC>(new RuleC(identifier(), index(), obj, interp));
        invalidValue(loc, interp);
        return null;
    }
}

// Inherited characteristic primitive object
public class InheritedCPrimitiveObj : PrimitiveObj
{
    private static readonly Signature signature_ = new Signature(0, 0, false);
    private ConstPtr<InheritedC> inheritedC_;

    public InheritedCPrimitiveObj(ConstPtr<InheritedC>? ic)
        : base(signature_)
    {
        inheritedC_ = ic!;
    }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ec,
                                          Interpreter interp, Location loc)
    {
        if (ec.styleStack == null)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.notInCharacteristicValue);
            return interp.makeError();
        }
        var dependencies = new System.Collections.Generic.List<nuint>();
        ELObj? obj = ec.styleStack.inherited(inheritedC_, ec.specLevel, interp, dependencies);
        interp.makeReadOnly(obj);
        return obj;
    }
}

// Actual characteristic primitive object
public class ActualCPrimitiveObj : PrimitiveObj
{
    private static readonly Signature signature_ = new Signature(0, 0, false);
    private ConstPtr<InheritedC> inheritedC_;

    public ActualCPrimitiveObj(ConstPtr<InheritedC>? ic)
        : base(signature_)
    {
        inheritedC_ = ic!;
    }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ec,
                                          Interpreter interp, Location loc)
    {
        if (ec.styleStack == null)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.notInCharacteristicValue);
            return interp.makeError();
        }
        var dependencies = new System.Collections.Generic.List<nuint>();
        ELObj? obj = ec.styleStack.actual(inheritedC_, loc, interp, dependencies);
        interp.makeReadOnly(obj);
        return obj;
    }
}
