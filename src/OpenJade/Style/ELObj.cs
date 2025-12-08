// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using GroveChar = System.UInt32;
using Boolean = System.Boolean;

// Forward declarations as interfaces for circular dependencies
public interface IInterpreter { }
public interface IEvalContext { }

public class Unit : Named
{
    private long exact_ = 0;
    private double inexact_ = 0.0;
    private bool isDefined_ = false;
    private bool isExact_ = false;
    private uint defPart_ = 0;
    private Location defLoc_ = new Location();

    public Unit() : base(new StringC()) { }
    public Unit(StringC name) : base(name) { }

    public void setValue(long val)
    {
        exact_ = val;
        isDefined_ = true;
        isExact_ = true;
    }

    public void setValue(double val)
    {
        inexact_ = val;
        isDefined_ = true;
        isExact_ = false;
    }

    public bool defined(ref uint part, ref Location loc)
    {
        if (!isDefined_)
            return false;
        part = defPart_;
        loc = defLoc_;
        return true;
    }

    public ELObj? resolveQuantity(bool force, Interpreter interp, long val, int valExp)
    {
        if (!isDefined_)
            return null;
        if (isExact_)
        {
            // Scale val by exact_
            double x = val;
            while (valExp > 0) { x *= 10.0; valExp--; }
            while (valExp < 0) { x /= 10.0; valExp++; }
            return new LengthObj((long)(x * exact_));
        }
        else
        {
            double x = val;
            while (valExp > 0) { x *= 10.0; valExp--; }
            while (valExp < 0) { x /= 10.0; valExp++; }
            return new LengthObj((long)(x * inexact_));
        }
    }

    public ELObj? resolveQuantity(bool force, Interpreter interp, double val, int unitExp)
    {
        if (!isDefined_)
            return null;
        double factor = isExact_ ? exact_ : inexact_;
        while (unitExp > 0) { factor *= factor; unitExp--; }
        return new QuantityObj(val * factor, unitExp);
    }
}

// Base class for all expression language objects
public class ELObj : Collector.Object
{
    public ELObj() { }

    // Make object permanent (will not be collected)
    public new void makePermanent()
    {
        base.makePermanent();
    }

    public virtual bool isNil() { return false; }
    public virtual bool isList() { return false; }
    public virtual bool isTrue() { return true; }
    public virtual PairObj? asPair() { return null; }
    public virtual SymbolObj? asSymbol() { return null; }
    public virtual KeywordObj? asKeyword() { return null; }
    public virtual FunctionObj? asFunction() { return null; }
    public virtual SosofoObj? asSosofo() { return null; }
    public virtual AppendSosofoObj? asAppendSosofo() { return null; }
    public virtual ColorObj? asColor() { return null; }
    public virtual ColorSpaceObj? asColorSpace() { return null; }
    public virtual StyleObj? asStyle() { return null; }
    public virtual AddressObj? asAddress() { return null; }
    public virtual DisplaySpaceObj? asDisplaySpace() { return null; }
    public virtual InlineSpaceObj? asInlineSpace() { return null; }
    public virtual GlyphSubstTableObj? asGlyphSubstTable() { return null; }
    public virtual NodeListObj? asNodeList() { return null; }
    public virtual NamedNodeListObj? asNamedNodeList() { return null; }
    public virtual StringObj? convertToString() { return null; }
    public virtual BoxObj? asBox() { return null; }
    public virtual VectorObj? asVector() { return null; }
    public virtual LanguageObj? asLanguage() { return null; }
    public virtual LengthObj? asLength() { return null; }
    public virtual LengthSpecObj? asLengthSpec() { return null; }
    public virtual bool charValue(out Char c) { c = 0; return false; }
    public virtual bool stringData(out Char[]? data, out nuint size) { data = null; size = 0; return false; }
    public virtual void print(Interpreter interp, OutputCharStream os)
    {
        os.put("#<unknown object>");
    }
    public virtual void print(Interpreter interp, OutputCharStream os, uint radix)
    {
        print(interp, os);
    }
    public virtual bool exactIntegerValue(out long value) { value = 0; return false; }
    public virtual bool realValue(out double value) { value = 0; return false; }
    public virtual bool inexactRealValue(out double value) { value = 0; return false; }
    public virtual bool lengthValue(out long value) { value = 0; return false; }
    public virtual FOTBuilder.GlyphId? glyphId() { return null; }
    public virtual LengthSpec? lengthSpec() { return null; }

    public enum QuantityType
    {
        noQuantity,
        longQuantity,
        doubleQuantity
    }

    public virtual QuantityType quantityValue(out long lval, out double dval, out int dim)
    {
        lval = 0; dval = 0; dim = 0;
        return QuantityType.noQuantity;
    }

    public virtual ELObj? resolveQuantities(bool force, Interpreter interp, Location loc)
    {
        return this;
    }

    public virtual bool optSingletonNodeList(IEvalContext ctx, Interpreter interp, ref NodePtr ptr)
    {
        return false;
    }

    public static bool equal(ELObj a, ELObj b)
    {
        if (ReferenceEquals(a, b))
            return true;
        return a.isEqual(b);
    }

    public static bool eqv(ELObj a, ELObj b)
    {
        if (ReferenceEquals(a, b))
            return true;
        return a.isEquiv(b);
    }

    protected virtual bool isEqual(ELObj other) { return false; }
    protected virtual bool isEquiv(ELObj other) { return isEqual(other); }
}

public class ErrorObj : ELObj
{
    internal ErrorObj() { }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        os.put("#<error>");
    }
}

public class UnspecifiedObj : ELObj
{
    internal UnspecifiedObj() { }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        os.put("#v");
    }
}

public class NilObj : ELObj
{
    internal NilObj() { }

    public override bool isNil() { return true; }
    public override bool isList() { return true; }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        os.put("()");
    }
}

public class TrueObj : ELObj
{
    internal TrueObj() { }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        os.put("#t");
    }
}

public class FalseObj : ELObj
{
    internal FalseObj() { }

    public override bool isTrue() { return false; }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        os.put("#f");
    }
}

public class SymbolObj : ELObj
{
    private StringObj? name_;
    private FOTBuilder.Symbol cValue_;

    internal SymbolObj(StringObj? name)
    {
        name_ = name;
    }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        if (name_ != null)
            os.write(name_.data(), name_.size());
    }

    public override SymbolObj? asSymbol() { return this; }

    public override StringObj? convertToString() { return name_; }

    public StringObj? name() { return name_; }

    public override void traceSubObjects(Collector c)
    {
        if (name_ != null)
            c.trace(name_);
    }

    public FOTBuilder.Symbol cValue() { return cValue_; }
    public void setCValue(FOTBuilder.Symbol value) { cValue_ = value; }

    public static StringC key(SymbolObj sym)
    {
        return sym.name_!;
    }
}

public class Identifier : Named
{
    private StringC name_;
    private ELObj? value_;
    private Expression? expression_;  // Uncompiled expression for lazy evaluation
    private InsnPtr insn_;            // Compiled instruction
    private uint defPart_;
    private Location defLoc_;
    private bool isDefined_;
    private bool evaluated_;
    private bool beingComputed_;      // Loop detection
    private ConstPtr<InheritedC>? inheritedC_;
    private FlowObj? flowObj_;
    private SyntacticKey synKey_;

    public enum SyntacticKey
    {
        notKey,
        keyUse,
        keyLabel,
        keyContentMap,
        // Top-level forms
        define,
        defineUnit,
        element,
        orElement,
        defaultEntity,
        root,
        id,
        mode,
        match,
        declareInitialValue,
        declareCharacteristic,
        declareFlowObjectClass,
        declareDefaultLanguage,
        defineLanguage,
        // Expression keywords
        quote,
        quasiquote,
        unquote,
        unquoteSplicing,
        ifKey,
        cond,
        caseKey,
        and,
        or,
        let,
        letStar,
        letrec,
        lambda,
        begin,
        set,
        thereExists,
        forAll,
        selectEach,
        unionForEach,
        make,
        style,
        withMode,
        elseKey,
        arrowKey,
        external,
        // Display NIC keywords
        keyPositionPreference,
        keyIsKeepWithPrevious,
        keyIsKeepWithNext,
        keyKeep,
        keyBreakBefore,
        keyBreakAfter,
        keyIsMayViolateKeepBefore,
        keyIsMayViolateKeepAfter,
        keySpaceBefore,
        keySpaceAfter,
        keyCoalesceId,
        // Character and rule keywords
        keyCh,
        keyChar,
        keyGlyphId,
        keyIsBreakBeforePriority,
        keyIsBreakAfterPriority,
        keyBreakBeforePriority,
        keyBreakAfterPriority,
        keyOrientation,
        keyLength,
        // Display and box keywords
        keyIsDisplay,
        keyScale,
        keyMaxWidth,
        keyMaxHeight,
        keyEntitySystemId,
        keyNotationSystemId,
        keyPositionPointX,
        keyPositionPointY,
        keyEscapementDirection,
        // Character flow object keywords
        keyIsSpace,
        keyIsRecordEnd,
        keyIsInputTab,
        keyIsInputWhitespace,
        keyIsPunct,
        keyIsDropAfterLineBreak,
        keyIsDropUnlessBeforeLineBreak,
        keyScript,
        keyMathClass,
        keyMathFontPosture,
        keyStretchFactor,
        // Link keywords
        keyDestination,
        // Multi-mode keywords
        keyMultiModes,
        // Score keywords
        keyType,
        // Page sequence keywords
        keyLeftHeader,
        keyCenterHeader,
        keyRightHeader,
        keyLeftFooter,
        keyCenterFooter,
        keyRightFooter,
        // Table keywords
        keyBeforeRowBorder,
        keyAfterRowBorder,
        keyBeforeColumnBorder,
        keyAfterColumnBorder,
        keyTableWidth,
        keyColumnNumber,
        keyRowNumber,
        keyNColumnsSpanned,
        keyNRowsSpanned,
        keyWidth,
        keyIsStartsRow,
        keyIsEndsRow,
        // Grid keywords
        keyGridNColumns,
        keyGridNRows,
        // Radical keyword
        keyRadical,
        // Data keyword
        keyData,
    }

    public Identifier(StringC name) : base(name)
    {
        name_ = name;
        value_ = null;
        expression_ = null;
        insn_ = new InsnPtr();
        defPart_ = 0;
        defLoc_ = new Location();
        isDefined_ = false;
        evaluated_ = false;
        beingComputed_ = false;
        inheritedC_ = null;
        flowObj_ = null;
        synKey_ = SyntacticKey.notKey;
    }

    public new StringC name() { return name_; }

    public bool defined(out uint part, out Location loc)
    {
        part = defPart_;
        loc = defLoc_;
        return isDefined_;
    }

    public void setDefinition(uint part, Location loc, ELObj? value)
    {
        defPart_ = part;
        defLoc_ = loc;
        value_ = value;
        isDefined_ = true;
        evaluated_ = (value != null);
    }

    public void setExpression(Expression? expr, uint part, Location loc)
    {
        defPart_ = part;
        defLoc_ = loc;
        expression_ = expr;
        isDefined_ = true;
        evaluated_ = false;
    }

    public Expression? expression() { return expression_; }

    public ELObj? computeValue(bool force, Interpreter interp)
    {
        if (!isDefined_)
            return null;
        if (value_ != null)
            return value_;

        // Need to compute from expression
        if (expression_ == null)
            return null;

        // Check for recursive definition loop
        if (beingComputed_)
        {
            if (force)
            {
                interp.setNextLocation(defLoc_);
                interp.message(InterpreterMessages.identifierLoop, name_.ToString());
                value_ = interp.makeError();
            }
            return value_;
        }

        beingComputed_ = true;
        try
        {
            // Compile expression if not already compiled
            if (insn_.isNull())
            {
                insn_ = Expression.optimizeCompile(expression_, interp, new Environment(), 0, new InsnPtr());
            }

            // Evaluate if forced or expression can be evaluated at compile time
            if (force || expression_.canEval(false))
            {
                VM vm = new VM(interp);
                ELObj? v = vm.eval(insn_.pointer());
                if (v != null)
                    interp.makePermanent(v);
                value_ = v;
            }
        }
        finally
        {
            beingComputed_ = false;
        }

        return value_;
    }

    public bool evaluated() { return evaluated_; }

    public bool syntacticKey(out SyntacticKey key)
    {
        key = synKey_;
        return synKey_ != SyntacticKey.notKey;
    }

    public void setSyntacticKey(SyntacticKey key) { synKey_ = key; }

    public ConstPtr<InheritedC>? inheritedC() { return inheritedC_; }
    public void setInheritedC(ConstPtr<InheritedC>? ic) { inheritedC_ = ic; }

    public FlowObj? flowObj() { return flowObj_; }
    public void setFlowObj(FlowObj? fo) { flowObj_ = fo; }
}

public class KeywordObj : ELObj
{
    private Identifier? ident_;

    internal KeywordObj(Identifier? ident)
    {
        ident_ = ident;
    }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        if (ident_ != null)
        {
            os.write(ident_.name().data(), ident_.name().size());
            os.put(":");
        }
    }

    public override KeywordObj? asKeyword() { return this; }

    public Identifier? identifier() { return ident_; }

    public StringC name() { return ident_?.name() ?? new StringC(); }

    protected override bool isEqual(ELObj other)
    {
        if (other is KeywordObj kw)
            return ident_ == kw.ident_;
        return false;
    }
}

public class PairObj : ELObj
{
    private ELObj? car_;
    private ELObj? cdr_;

    public PairObj(ELObj? car, ELObj? cdr)
    {
        car_ = car;
        cdr_ = cdr;
    }

    public ELObj? car() { return car_; }
    public ELObj? cdr() { return cdr_; }
    public void setCar(ELObj? obj) { car_ = obj; }
    public void setCdr(ELObj? obj) { cdr_ = obj; }

    public override PairObj? asPair() { return this; }

    public override bool isList()
    {
        ELObj? p = cdr_;
        while (p != null && !p.isNil())
        {
            PairObj? pair = p.asPair();
            if (pair == null)
                return false;
            p = pair.cdr();
        }
        return true;
    }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        os.put("(");
        if (car_ != null)
            car_.print(interp, os);
        ELObj? p = cdr_;
        for (;;)
        {
            if (p == null || p.isNil())
            {
                os.put(")");
                return;
            }
            PairObj? pair = p.asPair();
            if (pair == null)
            {
                os.put(" . ");
                p.print(interp, os);
                os.put(")");
                return;
            }
            os.put(" ");
            if (pair.car() != null)
                pair.car()!.print(interp, os);
            p = pair.cdr();
        }
    }

    public override void traceSubObjects(Collector c)
    {
        if (car_ != null)
            c.trace(car_);
        if (cdr_ != null)
            c.trace(cdr_);
    }

    public override ELObj? resolveQuantities(bool force, Interpreter interp, Location loc)
    {
        bool fail = false;
        PairObj pair = this;
        for (;;)
        {
            ELObj? tem = pair.car_?.resolveQuantities(force, interp, loc);
            if (tem == null)
                fail = true;
            else
            {
                if (pair.permanent())
                    interp.makePermanent(tem);
                pair.car_ = tem;
            }
            PairObj? nextPair = pair.cdr_?.asPair();
            if (nextPair == null)
                break;
            pair = nextPair;
        }
        ELObj? tem2 = pair.cdr_?.resolveQuantities(force, interp, loc);
        if (tem2 == null)
            fail = true;
        else
        {
            if (pair.permanent())
                interp.makePermanent(tem2);
            pair.cdr_ = tem2;
        }
        if (fail)
            return null;
        else
            return this;
    }

    protected override bool isEqual(ELObj other)
    {
        PairObj? p = other.asPair();
        // FIXME need non-recursive implementation
        return p != null && equal(p.car()!, car()!) && equal(p.cdr()!, cdr()!);
    }

    protected override bool isEquiv(ELObj other)
    {
        PairObj? p = other.asPair();
        // FIXME need non-recursive implementation
        return p != null && eqv(p.car()!, car()!) && eqv(p.cdr()!, cdr()!);
    }
}

public class VectorObj : ELObj
{
    private System.Collections.Generic.List<ELObj?> elements_ = new System.Collections.Generic.List<ELObj?>();

    public VectorObj() { }

    public VectorObj(System.Collections.Generic.List<ELObj?> v)
    {
        elements_ = new System.Collections.Generic.List<ELObj?>(v);
    }

    public VectorObj(int size, ELObj? fill)
    {
        for (int i = 0; i < size; i++)
            elements_.Add(fill);
    }

    public override void traceSubObjects(Collector c)
    {
        foreach (var e in elements_)
            if (e != null)
                c.trace(e);
    }

    public override VectorObj? asVector() { return this; }

    protected override bool isEqual(ELObj other)
    {
        VectorObj? v = other.asVector();
        if (v == null)
            return false;
        if (size() != v.size())
            return false;
        for (int i = 0; i < (int)size(); i++)
            if (!equal(this[i]!, v[i]!))
                return false;
        return true;
    }

    protected override bool isEquiv(ELObj other)
    {
        return false;
    }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        os.put("#(");
        for (int i = 0; i < elements_.Count; i++)
        {
            if (i > 0)
                os.put(" ");
            ELObj? tem = elements_[i];
            if (tem == null)
                os.put("#<cycle>");
            else
            {
                elements_[i] = null;
                tem.print(interp, os);
                elements_[i] = tem;
            }
        }
        os.put(")");
    }

    public override ELObj? resolveQuantities(bool force, Interpreter interp, Location loc)
    {
        bool fail = false;
        for (int i = 0; i < elements_.Count; i++)
        {
            ELObj? tem = elements_[i]?.resolveQuantities(force, interp, loc);
            if (tem != null)
            {
                if (permanent())
                    interp.makePermanent(tem);
                elements_[i] = tem;
            }
            else
                fail = true;
        }
        if (fail)
            return null;
        return this;
    }

    public nuint size() { return (nuint)elements_.Count; }
    public ELObj? this[int index] { get => elements_[index]; set => elements_[index] = value; }
}

public class CharObj : ELObj
{
    private Char ch_;

    internal CharObj(Char ch)
    {
        ch_ = ch;
    }

    public Char ch() { return ch_; }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        os.put("#\\");
        os.put(ch_); // FIXME
    }

    public void display(Interpreter interp, OutputCharStream os)
    {
        os.put(ch_); // FIXME
    }

    protected override bool isEqual(ELObj other)
    {
        if (other is CharObj co)
            return ch_ == co.ch_;
        return false;
    }

    public override bool charValue(out Char c)
    {
        c = ch_;
        return true;
    }

    public override bool stringData(out Char[]? data, out nuint size)
    {
        // A character can be treated as a single-character string
        data = new Char[] { ch_ };
        size = 1;
        return true;
    }

    public override StringObj? convertToString()
    {
        StringObj s = new StringObj();
        s.append(new Char[] { ch_ }, 1);
        return s;
    }
}

public class StringObj : ELObj
{
    private StringC str_ = new StringC();

    public StringObj() { }

    public StringObj(StringC str)
    {
        // Make a copy since the input StringC may be reused by the parser
        str_ = new StringC(str);
    }

    public StringObj(Char[] data, nuint size)
    {
        str_ = new StringC(data, size);
    }

    public Char[] data() { return str_.data(); }
    public nuint size() { return str_.size(); }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        // FIXME
        os.put("\"");
        Char[] s = str_.data();
        for (nuint i = 0; i < str_.size(); i++)
        {
            switch (s[i])
            {
                case '\\':
                case '"':
                    os.put("\\");
                    os.put(s[i]);
                    break;
                default:
                    os.put(s[i]);
                    break;
            }
        }
        os.put("\"");
    }

    public override StringObj? convertToString() { return this; }

    public override bool stringData(out Char[]? data, out nuint size)
    {
        data = str_.data();
        size = str_.size();
        return true;
    }

    protected override bool isEqual(ELObj other)
    {
        Char[]? s;
        nuint n;
        if (!other.stringData(out s, out n))
            return false;
        if (n != str_.size())
            return false;
        if (n == 0)
            return true;
        Char[] myData = str_.data();
        for (nuint i = 0; i < n; i++)
            if (s![i] != myData[i])
                return false;
        return true;
    }

    // Append string data
    public void append(Char[] data, nuint size)
    {
        str_.append(data, size);
    }

    // Append string data with offset
    public void append(Char[] data, nuint offset, nuint size)
    {
        str_.append(data, offset, size);
    }

    // Allow implicit conversion to StringC
    public static implicit operator StringC(StringObj obj) => obj.str_;
}

public class IntegerObj : ELObj
{
    private long n_;

    internal IntegerObj() { n_ = 0; }
    internal IntegerObj(long n) { n_ = n; }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        print(interp, os, 10);
    }

    public override void print(Interpreter interp, OutputCharStream os, uint radix)
    {
        if (radix == 10)
        {
            if (n_ < 0)
            {
                os.put("-");
                os.put(((ulong)(-n_)).ToString());
            }
            else
                os.put(((ulong)n_).ToString());
            return;
        }

        if (n_ == 0)
        {
            os.put("0");
            return;
        }

        ulong n;
        if (n_ < 0)
        {
            os.put("-");
            n = (ulong)(-n_);
        }
        else
            n = (ulong)n_;

        char[] buf = new char[64];
        int i = 0;
        const string digits = "0123456789abcdef";

        while (n != 0)
        {
            buf[i++] = digits[(int)(n % radix)];
            n /= radix;
        }

        while (i > 0)
            os.put((Char)buf[--i]);
    }

    public override bool exactIntegerValue(out long value)
    {
        value = n_;
        return true;
    }

    public override bool realValue(out double value)
    {
        value = n_;
        return true;
    }

    public override QuantityType quantityValue(out long lval, out double dval, out int dim)
    {
        lval = n_;
        dval = 0;
        dim = 0;
        return QuantityType.longQuantity;
    }

    protected override bool isEqual(ELObj other)
    {
        if (other is IntegerObj io)
            return n_ == io.n_;
        return false;
    }
}

public class RealObj : ELObj
{
    private double n_;

    public RealObj(double n) { n_ = n; }

    public override bool realValue(out double value)
    {
        value = n_;
        return true;
    }

    public override bool inexactRealValue(out double value)
    {
        value = n_;
        return true;
    }

    public override QuantityType quantityValue(out long lval, out double dval, out int dim)
    {
        lval = 0;
        dval = n_;
        dim = 0;
        return QuantityType.doubleQuantity;
    }

    protected override bool isEqual(ELObj other)
    {
        double n;
        return other.inexactRealValue(out n) && n == n_;
    }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        os.put(n_.ToString("G"));
    }
}

public class LengthObj : ELObj
{
    private long n_;

    public LengthObj(long units) { n_ = units; }

    public long value() { return n_; }

    public override LengthObj? asLength() { return this; }

    public override bool lengthValue(out long value)
    {
        value = n_;
        return true;
    }

    public override QuantityType quantityValue(out long lval, out double dval, out int dim)
    {
        lval = n_;
        dval = 0;
        dim = 1;
        return QuantityType.longQuantity;
    }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        double pt = n_ * 72.0 / interp.unitsPerInch();
        os.put(pt.ToString("G") + "pt");
    }

    protected override bool isEqual(ELObj other)
    {
        long n;
        double d;
        int dim;
        switch (other.quantityValue(out n, out d, out dim))
        {
            case QuantityType.noQuantity:
                break;
            case QuantityType.doubleQuantity:
                return dim == 1 && d == n_;
            case QuantityType.longQuantity:
                return dim == 1 && n == n_;
        }
        return false;
    }
}

public class QuantityObj : ELObj
{
    private double val_;
    private int dim_;

    public QuantityObj(double val, int dim)
    {
        val_ = val;
        dim_ = dim;
    }

    public override QuantityType quantityValue(out long lval, out double dval, out int dim)
    {
        lval = 0;
        dval = val_;
        dim = dim_;
        return QuantityType.doubleQuantity;
    }

    public override void print(Interpreter interp, OutputCharStream os)
    {
        double val = val_ * Math.Pow(72.0 / interp.unitsPerInch(), (double)dim_);
        os.put(val.ToString("G") + "pt" + dim_.ToString());
    }

    protected override bool isEqual(ELObj other)
    {
        long n;
        double d;
        int dim;
        switch (other.quantityValue(out n, out d, out dim))
        {
            case QuantityType.noQuantity:
                break;
            case QuantityType.doubleQuantity:
                return dim == dim_ && d == val_;
            case QuantityType.longQuantity:
                return dim == dim_ && n == val_;
        }
        return false;
    }

    public override bool realValue(out double value)
    {
        value = val_;
        return dim_ == 0;
    }

    public override bool inexactRealValue(out double value)
    {
        value = val_;
        return dim_ == 0;
    }
}

public class LengthSpec
{
    public enum Unknown { displaySize = 1, tableUnit }

    private const int nVals = 3;
    private double[] val_ = new double[nVals];

    public LengthSpec()
    {
        for (int i = 0; i < nVals; i++)
            val_[i] = 0;
    }

    public LengthSpec(double d)
    {
        val_[0] = d;
        for (int i = 1; i < nVals; i++)
            val_[i] = 0;
    }

    public LengthSpec(Unknown u, double d)
    {
        for (int i = 0; i < nVals; i++)
            val_[i] = 0;
        val_[(int)u] = d;
    }

    public double[] val() { return val_; }

    public void add(LengthSpec other)
    {
        for (int i = 0; i < nVals; i++)
            val_[i] += other.val_[i];
    }

    public void add(double d) { val_[0] += d; }

    public void subtract(LengthSpec other)
    {
        for (int i = 0; i < nVals; i++)
            val_[i] -= other.val_[i];
    }

    public void subtract(double d) { val_[0] -= d; }

    public void multiply(double d)
    {
        for (int i = 0; i < nVals; i++)
            val_[i] *= d;
    }

    public void divide(double d)
    {
        for (int i = 0; i < nVals; i++)
            val_[i] /= d;
    }

    public bool convert(out FOTBuilder.LengthSpec spec)
    {
        // FIXME do some checking
        spec = new FOTBuilder.LengthSpec();
        spec.length = (long)(val_[0] < 0.0 ? val_[0] - 0.5 : val_[0] + 0.5);
        spec.displaySizeFactor = val_[1];
        return true;
    }

    public bool convert(out FOTBuilder.TableLengthSpec spec)
    {
        // FIXME do some checking
        spec = new FOTBuilder.TableLengthSpec();
        spec.length = (long)(val_[0] < 0.0 ? val_[0] - 0.5 : val_[0] + 0.5);
        spec.displaySizeFactor = val_[1];
        spec.tableUnitFactor = val_[2];
        return true;
    }
}

public class LengthSpecObj : ELObj
{
    private LengthSpec lengthSpec_;
    private FOTBuilder.LengthSpec? fotLengthSpec_;

    public LengthSpecObj(LengthSpec spec)
    {
        lengthSpec_ = spec;
        fotLengthSpec_ = null;
    }

    public LengthSpecObj(FOTBuilder.LengthSpec spec)
    {
        // Convert FOTBuilder.LengthSpec to ELObj.LengthSpec
        lengthSpec_ = new LengthSpec(spec.length);
        fotLengthSpec_ = spec;
    }

    public override LengthSpec? lengthSpec() { return lengthSpec_; }

    public override LengthSpecObj? asLengthSpec() { return this; }

    public FOTBuilder.LengthSpec lengthSpecFOT()
    {
        if (fotLengthSpec_.HasValue)
            return fotLengthSpec_.Value;
        // Convert ELObj.LengthSpec to FOTBuilder.LengthSpec
        return new FOTBuilder.LengthSpec((long)lengthSpec_.val()[0]);
    }
}

public class DisplaySpaceObj : ELObj
{
    private FOTBuilder.DisplaySpace displaySpace_;

    public DisplaySpaceObj(FOTBuilder.DisplaySpace space)
    {
        displaySpace_ = space;
    }

    public FOTBuilder.DisplaySpace displaySpace() { return displaySpace_; }
    public override DisplaySpaceObj? asDisplaySpace() { return this; }
}

public class InlineSpaceObj : ELObj
{
    private FOTBuilder.InlineSpace inlineSpace_;

    public InlineSpaceObj(FOTBuilder.InlineSpace space)
    {
        inlineSpace_ = space;
    }

    public FOTBuilder.InlineSpace inlineSpace() { return inlineSpace_; }
    public override InlineSpaceObj? asInlineSpace() { return this; }
}

public class UnresolvedQuantityObj : ELObj
{
    private double val_;
    private Unit? unit_;
    private int unitExp_;

    public UnresolvedQuantityObj(double val, Unit? unit, int unitExp)
    {
        val_ = val;
        unit_ = unit;
        unitExp_ = unitExp;
    }

    public override ELObj? resolveQuantities(bool force, Interpreter interp, Location loc)
    {
        if (unit_ == null)
            return interp.makeError();
        uint part = 0;
        Location defLoc = new Location();
        if (!unit_.defined(ref part, ref defLoc))
        {
            interp.message(MessageType.Severity.error, loc, $"undefined quantity: {unit_.name()}");
            return interp.makeError();
        }
        return unit_.resolveQuantity(force, interp, val_, unitExp_);
    }
}

public class UnresolvedLengthObj : ELObj
{
    private long val_;
    private Unit? unit_;
    private int valExp_;

    public UnresolvedLengthObj(long val, int valExp, Unit? unit)
    {
        val_ = val;
        unit_ = unit;
        valExp_ = valExp;
    }

    public override ELObj? resolveQuantities(bool force, Interpreter interp, Location loc)
    {
        if (unit_ == null)
            return interp.makeError();
        uint part = 0;
        Location defLoc = new Location();
        if (!unit_.defined(ref part, ref defLoc))
        {
            interp.message(MessageType.Severity.error, loc, $"undefined quantity: {unit_.name()}");
            return interp.makeError();
        }
        return unit_.resolveQuantity(force, interp, val_, valExp_);
    }
}

public class GlyphIdObj : ELObj
{
    private FOTBuilder.GlyphId glyphId_;

    public GlyphIdObj(FOTBuilder.GlyphId id)
    {
        glyphId_ = id;
    }

    public override FOTBuilder.GlyphId? glyphId() { return glyphId_; }

    protected override bool isEqual(ELObj other)
    {
        if (other is GlyphIdObj gio)
            return glyphId_.Equals(gio.glyphId_);
        return false;
    }
}

public class GlyphSubstTableObj : ELObj
{
    private ConstPtr<FOTBuilder.GlyphSubstTable> table_;

    public GlyphSubstTableObj(ConstPtr<FOTBuilder.GlyphSubstTable> table)
    {
        table_ = table;
    }

    public override GlyphSubstTableObj? asGlyphSubstTable() { return this; }
    public ConstPtr<FOTBuilder.GlyphSubstTable> glyphSubstTable() { return table_; }
}

public class AddressObj : ELObj
{
    private FOTBuilder.Address address_;

    public AddressObj(FOTBuilder.Address addr)
    {
        address_ = addr;
    }

    public override AddressObj? asAddress() { return this; }
    public FOTBuilder.Address address() { return address_; }
}

// NOTE: BoxObj and FunctionObj are defined in FunctionObj.cs

public abstract class SosofoObj : ELObj
{
    public override SosofoObj? asSosofo() { return this; }
    public abstract void process(ProcessContext context);
    public virtual bool isRule() { return false; }
    public virtual bool isCharacter() { return false; }
    public virtual bool tableBorderStyle(out StyleObj? style) { style = null; return false; }
    public virtual bool ruleStyle(ProcessContext context, out StyleObj? style) { style = null; return false; }
    public virtual bool characterStyle(ProcessContext context, out StyleObj? style, FOTBuilder.CharacterNIC nic) { style = null; return false; }
}

public class AppendSosofoObj : SosofoObj
{
    private System.Collections.Generic.List<SosofoObj> v_ = new();

    public override AppendSosofoObj? asAppendSosofo() { return this; }

    public void append(SosofoObj sosofo)
    {
        v_.Add(sosofo);
    }

    public override void process(ProcessContext context)
    {
        foreach (var sosofo in v_)
            sosofo.process(context);
    }
}

public class ColorObj : ELObj
{
    public override ColorObj? asColor() { return this; }
    public virtual void set(FOTBuilder fotb) { }
    public virtual void setBackground(FOTBuilder fotb) { }
}

public class ColorSpaceObj : ELObj
{
    public override ColorSpaceObj? asColorSpace() { return this; }
}

public class StyleObj : ELObj
{
    public override StyleObj? asStyle() { return this; }
    public virtual void appendIter(StyleObjIter iter) { }
}

public class NodeListObj : ELObj
{
    public override NodeListObj? asNodeList() { return this; }
    public virtual NodePtr? nodeListFirst(EvalContext ctx, Interpreter interp) { return null; }
    public virtual NodeListObj nodeListRest(EvalContext ctx, Interpreter interp) { return new EmptyNodeListObj(); }
    // C++ implementation: upstream/openjade/style/ELObj.cxx:998
    public virtual NodeListObj nodeListChunkRest(EvalContext ctx, Interpreter interp, ref bool chunk)
    {
        chunk = false;  // C++ sets chunk = 0
        return nodeListRest(ctx, interp);
    }
    public virtual NodeListObj nodeListNoOrder(Interpreter interp) { return this; }

    // C++ implementation: upstream/openjade/style/ELObj.cxx:1004
    // Returns true if this is a singleton (0 or 1 element) node list
    public override bool optSingletonNodeList(IEvalContext ictx, Interpreter interp, ref NodePtr ptr)
    {
        EvalContext ctx = (EvalContext)ictx;
        NodeListObj rest = nodeListRest(ctx, interp);
        if (rest.nodeListFirst(ctx, interp) != null)
            return false;  // More than one element
        ptr = nodeListFirst(ctx, interp) ?? new NodePtr();
        return true;
    }
}

// Empty node list
public class EmptyNodeListObj : NodeListObj
{
    public override NodePtr? nodeListFirst(EvalContext ctx, Interpreter interp) { return null; }
    public override NodeListObj nodeListRest(EvalContext ctx, Interpreter interp) { return this; }
}

// Node list from single node pointer
public class NodePtrNodeListObj : NodeListObj
{
    private NodePtr? node_;

    public NodePtrNodeListObj(NodePtr? node)
    {
        node_ = node;
    }

    public override NodePtr? nodeListFirst(EvalContext ctx, Interpreter interp)
    {
        return node_;
    }

    public override NodeListObj nodeListRest(EvalContext ctx, Interpreter interp)
    {
        return new EmptyNodeListObj();
    }

    // C++ implementation: upstream/openjade/style/ELObj.cxx:1087
    public override bool optSingletonNodeList(IEvalContext ctx, Interpreter interp, ref NodePtr ptr)
    {
        ptr = node_ ?? new NodePtr();
        return true;
    }
}

// Node list from grove node list pointer
public class NodeListPtrNodeListObj : NodeListObj
{
    private static bool debugNodeListPtr = false; // Set to true for debugging
    private NodeListPtr nodeList_;
    private NodePtr? current_;
    private bool started_;

    public NodeListPtrNodeListObj(NodeListPtr nodeList)
    {
        nodeList_ = nodeList;
        started_ = false;
    }

    public override NodePtr? nodeListFirst(EvalContext ctx, Interpreter interp)
    {
        if (!started_)
        {
            started_ = true;
            if (nodeList_ != null)
            {
                NodePtr temp = new NodePtr();
                var result = nodeList_.first(ref temp);
                if (debugNodeListPtr)
                    Console.Error.WriteLine($"NodeListPtrNodeListObj.nodeListFirst: nodeList_.first() returned {result}");
                if (result != AccessResult.accessOK)
                    current_ = null;
                else
                {
                    current_ = temp;
                    if (debugNodeListPtr)
                    {
                        GroveString gi = new GroveString();
                        if (current_.getGi(ref gi) == AccessResult.accessOK)
                        {
                            string giStr = "";
                            for (nuint i = 0; i < gi.size(); i++)
                                giStr += (char)gi.data()![i];
                            Console.Error.WriteLine($"NodeListPtrNodeListObj.nodeListFirst: first node has GI '{giStr}'");
                        }
                        else
                            Console.Error.WriteLine($"NodeListPtrNodeListObj.nodeListFirst: first node (cannot get GI)");
                    }
                }
            }
            else if (debugNodeListPtr)
                Console.Error.WriteLine($"NodeListPtrNodeListObj.nodeListFirst: nodeList_ is null");
        }
        return current_;
    }

    public override NodeListObj nodeListRest(EvalContext ctx, Interpreter interp)
    {
        if (nodeList_ == null)
        {
            if (debugNodeListPtr)
                Console.Error.WriteLine("NodeListPtrNodeListObj.nodeListRest: nodeList_ is null, returning empty");
            return new EmptyNodeListObj();
        }
        NodeListPtr rest = new NodeListPtr();
        var result = nodeList_.rest(ref rest);
        if (debugNodeListPtr)
            Console.Error.WriteLine($"NodeListPtrNodeListObj.nodeListRest: nodeList_.rest() returned {result}");
        if (result == AccessResult.accessOK)
            return new NodeListPtrNodeListObj(rest);
        if (debugNodeListPtr)
            Console.Error.WriteLine("NodeListPtrNodeListObj.nodeListRest: returning empty");
        return new EmptyNodeListObj();
    }
}

// Node list from named node list pointer (attributes, elements, etc.)
public class NamedNodeListPtrNodeListObj : NodeListObj
{
    private NamedNodeListPtr namedList_;
    private NodeListPtrNodeListObj? listObj_;

    public NamedNodeListPtrNodeListObj(NamedNodeListPtr namedList)
    {
        namedList_ = namedList;
        listObj_ = null;
    }

    private NodeListPtrNodeListObj getListObj()
    {
        if (listObj_ == null && namedList_ != null)
        {
            NodeListPtr nlp = namedList_.nodeList();
            listObj_ = new NodeListPtrNodeListObj(nlp);
        }
        return listObj_!;
    }

    public override NodePtr? nodeListFirst(EvalContext ctx, Interpreter interp)
    {
        return getListObj().nodeListFirst(ctx, interp);
    }

    public override NodeListObj nodeListRest(EvalContext ctx, Interpreter interp)
    {
        return getListObj().nodeListRest(ctx, interp);
    }
}

// Pair of node lists (concatenation)
public class PairNodeListObj : NodeListObj
{
    private NodeListObj head_;
    private NodeListObj tail_;

    public PairNodeListObj(NodeListObj head, NodeListObj tail)
    {
        head_ = head;
        tail_ = tail;
    }

    public override NodePtr? nodeListFirst(EvalContext ctx, Interpreter interp)
    {
        NodePtr? nd = head_.nodeListFirst(ctx, interp);
        if (nd != null)
            return nd;
        return tail_.nodeListFirst(ctx, interp);
    }

    public override NodeListObj nodeListRest(EvalContext ctx, Interpreter interp)
    {
        NodePtr? nd = head_.nodeListFirst(ctx, interp);
        if (nd != null)
        {
            NodeListObj headRest = head_.nodeListRest(ctx, interp);
            return new PairNodeListObj(headRest, tail_);
        }
        // When head is exhausted, we've already returned tail's first element via nodeListFirst,
        // so we need to return the rest of the tail
        return tail_.nodeListRest(ctx, interp);
    }
}

// Map node list - applies a primitive to each node
public class MapNodeListObj : NodeListObj
{
    private FunctionObj? func_;
    private NodeListObj nodeList_;
    private Context context_;
    private NodeListObj? mapped_;  // Cached result of applying func to current node

    public class Context
    {
        public EvalContext evalContext;
        public Location loc;

        public Context(EvalContext ctx, Location l)
        {
            evalContext = ctx;
            loc = l;
        }
    }

    public MapNodeListObj(FunctionObj? func, NodeListObj nodeList, Context context, NodeListObj? mapped = null)
    {
        func_ = func;
        nodeList_ = nodeList;
        context_ = context;
        mapped_ = mapped;
    }

    // C++ MapNodeListObj does NOT override optSingletonNodeList - uses base class implementation
    // See upstream/openjade/style/primitive.cxx - no optSingletonNodeList override exists

    // Matches C++ implementation exactly - see upstream/openjade/style/primitive.cxx:5506
    public override NodePtr? nodeListFirst(EvalContext ctx, Interpreter interp)
    {
        while (true)
        {
            if (mapped_ == null)
            {
                mapNext(ctx, interp);
                if (mapped_ == null)
                    break;
            }
            NodePtr? nd = mapped_.nodeListFirst(ctx, interp);
            if (nd != null)
                return nd;
            mapped_ = null;
        }
        return null;
    }

    // Matches C++ implementation exactly - see upstream/openjade/style/primitive.cxx:5522
    public override NodeListObj nodeListRest(EvalContext ctx, Interpreter interp)
    {
        while (true)
        {
            if (mapped_ == null)
            {
                mapNext(ctx, interp);
                if (mapped_ == null)
                    break;
            }
            NodePtr? nd = mapped_.nodeListFirst(ctx, interp);
            if (nd != null)
            {
                NodeListObj tem = mapped_.nodeListRest(ctx, interp);
                return new MapNodeListObj(func_, nodeList_, context_, tem);
            }
            mapped_ = null;
        }
        return interp.makeEmptyNodeList();
    }

    private void mapNext(EvalContext ctx, Interpreter interp)
    {
        if (func_ == null)
            return;

        NodePtr? nd = nodeList_.nodeListFirst(ctx, interp);
        if (nd == null)
            return;

        // Create VM using stored context and apply function
        VM vm = new VM(context_.evalContext, interp);
        InsnPtr insn = func_.makeCallInsn(1, interp, context_.loc, new InsnPtr());
        ELObj? ret = vm.eval(insn.pointer(), null, new NodePtrNodeListObj(nd));

        if (ret == null || interp.isError(ret))
        {
            func_ = null;
            return;
        }

        mapped_ = ret.asNodeList();
        if (mapped_ == null)
        {
            interp.setNextLocation(context_.loc);
            // Function didn't return a node list - disable further mapping
            func_ = null;
            return;
        }

        nodeList_ = nodeList_.nodeListRest(ctx, interp);
    }
}

public class NamedNodeListObj : ELObj
{
    public override NamedNodeListObj? asNamedNodeList() { return this; }
}

public class LanguageObj : ELObj
{
    public override LanguageObj? asLanguage() { return this; }

    // Virtual methods for language-specific operations
    public virtual Char toUpper(Char c) { return c; }
    public virtual Char toLower(Char c) { return c; }
    public virtual bool areEquivalent(StringC r, StringC s, Char level) { return r.Equals(s); }
    public virtual bool isLess(StringC r, StringC s) { return false; }
    public virtual bool isLessOrEqual(StringC r, StringC s) { return true; }
}

// ELObjPropertyValue - converts grove property values to ELObj objects
public class ELObjPropertyValue : PropertyValue
{
    private Interpreter interp_;
    private bool rcs_;
    public ELObj? obj;

    public ELObjPropertyValue(Interpreter interp, bool rcs)
    {
        interp_ = interp;
        rcs_ = rcs;
        obj = null;
    }

    public override void set(NodePtr ptr)
    {
        obj = new NodePtrNodeListObj(ptr);
    }

    public override void set(NodeListPtr ptr)
    {
        obj = new NodeListPtrNodeListObj(ptr);
    }

    public override void set(NamedNodeListPtr ptr)
    {
        obj = new NamedNodeListPtrNodeListObj(ptr);
    }

    public override void set(bool b)
    {
        obj = b ? interp_.makeTrue() : interp_.makeFalse();
    }

    public override void set(GroveChar c)
    {
        obj = interp_.makeChar(c);
    }

    public override void set(GroveString s)
    {
        obj = new StringObj(s.data(), s.size());
    }

    public override void set(ComponentName.Id id)
    {
        string s = rcs_ ? ComponentName.rcsName(id) : ComponentName.sdqlName(id);
        obj = interp_.makeSymbol(interp_.makeStringC(s));
    }

    public override void set(GroveStringListPtr gsListPtr)
    {
        PairObj head = new PairObj(null, null);
        PairObj tail = head;
        if (gsListPtr.list != null)
        {
            var iter = new ConstGroveStringListIter(gsListPtr.list);
            while (!iter.done())
            {
                var gs = iter.cur();
                StringObj strObj = new StringObj(gs.data(), gs.size());
                PairObj tem = new PairObj(strObj, null);
                tail.setCdr(tem);
                tail = tem;
                iter.next();
            }
        }
        tail.setCdr(interp_.makeNil());
        obj = head.cdr();
    }

    public override void set(ComponentName.Id[] names)
    {
        PairObj head = new PairObj(null, null);
        PairObj tail = head;
        for (int i = 0; i < names.Length && names[i] != ComponentName.Id.noId; i++)
        {
            string s = rcs_ ? ComponentName.rcsName(names[i]) : ComponentName.sdqlName(names[i]);
            SymbolObj sym = interp_.makeSymbol(interp_.makeStringC(s));
            PairObj tem = new PairObj(sym, null);
            tail.setCdr(tem);
            tail = tem;
        }
        tail.setCdr(interp_.makeNil());
        obj = head.cdr();
    }

    public override void set(long l)
    {
        obj = interp_.makeInteger(l);
    }
}
