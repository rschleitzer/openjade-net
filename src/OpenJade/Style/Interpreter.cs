// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// The DSSSL Interpreter
public class Interpreter : Pattern.MatchContext, IInterpreter, IMessenger
{
    // Singleton objects
    private ErrorObj errorObj_ = new ErrorObj();
    private UnspecifiedObj unspecifiedObj_ = new UnspecifiedObj();
    private NilObj nilObj_ = new NilObj();
    private TrueObj trueObj_ = new TrueObj();
    private FalseObj falseObj_ = new FalseObj();
    private int unitsPerInch_ = 72000; // Default: 1000 units per point

    // Symbol and identifier tables
    private Dictionary<string, SymbolObj> symbolTable_ = new();
    private Dictionary<string, Identifier> identTable_ = new();

    public Interpreter()
    {
    }

    public int unitsPerInch() { return unitsPerInch_; }

    public void makePermanent(ELObj obj)
    {
        obj.makePermanent();
    }

    public ErrorObj errorObj() { return errorObj_; }
    public UnspecifiedObj unspecifiedObj() { return unspecifiedObj_; }
    public NilObj nilObj() { return nilObj_; }
    public TrueObj trueObj() { return trueObj_; }
    public FalseObj falseObj() { return falseObj_; }

    public ELObj makeFalse() { return falseObj_; }
    public ELObj makeTrue() { return trueObj_; }
    public ELObj makeNil() { return nilObj_; }
    public ELObj makeError() { return errorObj_; }

    public IntegerObj makeInteger(long n)
    {
        return new IntegerObj(n);
    }

    public RealObj makeReal(double n)
    {
        return new RealObj(n);
    }

    public CharObj makeChar(Char c)
    {
        return new CharObj(c);
    }

    public StringObj makeString(StringC str)
    {
        return new StringObj(str);
    }

    public PairObj makePair(ELObj? car, ELObj? cdr)
    {
        return new PairObj(car, cdr);
    }

    public SymbolObj makeSymbol(StringC str)
    {
        string key = str.ToString();
        if (symbolTable_.TryGetValue(key, out SymbolObj? sym))
            return sym;
        StringObj strObj = new StringObj(str);
        makePermanent(strObj);
        sym = new SymbolObj(strObj);
        makePermanent(sym);
        symbolTable_[key] = sym;
        return sym;
    }

    public Identifier lookup(StringC str)
    {
        string key = str.ToString();
        if (identTable_.TryGetValue(key, out Identifier? ident))
            return ident;
        ident = new Identifier(str);
        identTable_[key] = ident;
        return ident;
    }

    public KeywordObj makeKeyword(StringC str)
    {
        return new KeywordObj(lookup(str));
    }

    public VectorObj makeVector(System.Collections.Generic.List<ELObj?> elems)
    {
        return new VectorObj(elems);
    }

    public LengthObj makeLength(long units)
    {
        return new LengthObj(units);
    }

    public QuantityObj makeQuantity(double val, int dim)
    {
        return new QuantityObj(val, dim);
    }

    public void message(MessageType.Severity severity, Location loc, string text)
    {
        string typeStr = severity switch
        {
            MessageType.Severity.warning => "warning",
            MessageType.Severity.error => "error",
            MessageType.Severity.info => "info",
            _ => "message"
        };
        Console.Error.WriteLine($"{typeStr}: {text}");
    }

    // Processing mode support
    private ProcessingMode? initialProcessingMode_;
    private StyleObj? initialStyle_;

    public ProcessingMode? initialProcessingMode() { return initialProcessingMode_; }
    public StyleObj? initialStyle() { return initialStyle_; }

    public void setInitialProcessingMode(ProcessingMode? mode) { initialProcessingMode_ = mode; }
    public void setInitialStyle(StyleObj? style) { initialStyle_ = style; }

    public bool isError(ELObj? obj)
    {
        return obj is ErrorObj;
    }

    public void setNodeLocation(NodePtr node)
    {
        // Set the current location for error reporting
    }

    // IMessenger implementation
    public void dispatchMessage(Message msg)
    {
        message(msg.type.severity(), new Location(), msg.type.ToString());
    }

    public void initMessage(Message msg)
    {
        // Initialize message with context
    }

    public void setNextLocation(Location loc)
    {
        // Set the next location for error messages
    }

    // Debug mode
    private bool debugMode_ = false;
    public bool debugMode() { return debugMode_; }
    public void setDebugMode(bool debug) { debugMode_ = debug; }

    // Create a StringC from string
    public StringC makeStringC(string s)
    {
        return new StringC(s);
    }

    // Get string by index for interned strings
    public StringObj? storeString(StringC s)
    {
        return makeString(s);
    }

    // Make empty node list
    public NodeListObj makeEmptyNodeList()
    {
        return new EmptyNodeListObj();
    }

    // Make unspecified value
    public ELObj makeUnspecified()
    {
        return unspecifiedObj_;
    }

    // Make string from char array
    public StringObj makeString(Char[] data, nuint size)
    {
        return new StringObj(data, size);
    }

    // Message methods for InterpreterMessages
    public void message(InterpreterMessages msg)
    {
        message(MessageType.Severity.error, new Location(), msg.ToString());
    }

    public void message(InterpreterMessages msg, string arg)
    {
        message(MessageType.Severity.error, new Location(), $"{msg}: {arg}");
    }

    public void message(InterpreterMessages msg, int argIndex, ELObj? obj)
    {
        string objStr = obj?.ToString() ?? "null";
        message(MessageType.Severity.error, new Location(), $"{msg}: argument {argIndex}: {objStr}");
    }

    // Conversion methods for inherited characteristics
    public LengthSpecObj? makeLengthSpec(FOTBuilder.LengthSpec ls)
    {
        return new LengthSpecObj(ls);
    }

    public SymbolObj? cValueSymbol(FOTBuilder.Symbol sym)
    {
        // Map FOTBuilder.Symbol to SymbolObj
        return makeSymbol(makeStringC(sym.ToString().Replace("symbol", "")));
    }

    public bool convertBooleanC(ELObj obj, Identifier? ident, Location loc, out bool result)
    {
        result = false;
        if (obj == makeTrue())
        {
            result = true;
            return true;
        }
        if (obj == makeFalse())
        {
            result = false;
            return true;
        }
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    public bool convertLengthC(ELObj obj, Identifier? ident, Location loc, out long result)
    {
        result = 0;
        LengthObj? l = obj.asLength();
        if (l != null)
        {
            result = l.value();
            return true;
        }
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    public bool convertIntegerC(ELObj obj, Identifier? ident, Location loc, out long result)
    {
        result = 0;
        long n;
        if (obj.exactIntegerValue(out n))
        {
            result = n;
            return true;
        }
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    public bool convertOptPositiveIntegerC(ELObj obj, Identifier? ident, Location loc, out long result)
    {
        result = 0;
        if (obj == makeFalse())
            return true;
        long n;
        if (obj.exactIntegerValue(out n) && n > 0)
        {
            result = n;
            return true;
        }
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    public bool convertLengthSpecC(ELObj obj, Identifier? ident, Location loc, ref FOTBuilder.LengthSpec result)
    {
        LengthObj? l = obj.asLength();
        if (l != null)
        {
            result = new FOTBuilder.LengthSpec(l.value());
            return true;
        }
        LengthSpecObj? ls = obj.asLengthSpec();
        if (ls != null)
        {
            result = ls.lengthSpecFOT();
            return true;
        }
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    public bool convertOptLengthSpecC(ELObj obj, Identifier? ident, Location loc, ref FOTBuilder.OptLengthSpec result)
    {
        if (obj == makeFalse())
        {
            result.hasLength = false;
            return true;
        }
        if (convertLengthSpecC(obj, ident, loc, ref result.length))
        {
            result.hasLength = true;
            return true;
        }
        return false;
    }

    public bool convertEnumC(ELObj obj, Identifier? ident, Location loc, out FOTBuilder.Symbol result)
    {
        result = FOTBuilder.Symbol.symbolFalse;
        SymbolObj? sym = obj.asSymbol();
        if (sym == null)
        {
            invalidCharacteristicValue(ident, loc);
            return false;
        }
        // Map symbol name to FOTBuilder.Symbol
        string name = sym.name().ToString().ToLower().Replace("-", "");
        if (Enum.TryParse<FOTBuilder.Symbol>("symbol" + name, true, out result))
            return true;
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    public bool convertPublicIdC(ELObj obj, Identifier? ident, Location loc, out string? result)
    {
        result = null;
        if (obj == makeFalse())
            return true;
        Char[]? s;
        nuint n;
        if (obj.stringData(out s, out n) && s != null)
        {
            result = new string(Array.ConvertAll(s, c => (char)c), 0, (int)n);
            return true;
        }
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    public bool convertLetter2C(ELObj obj, Identifier? ident, Location loc, out ushort result)
    {
        result = 0;
        if (obj == makeFalse())
            return true;
        SymbolObj? sym = obj.asSymbol();
        if (sym != null)
        {
            StringC name = sym.name();
            if (name.size() == 2)
            {
                result = (ushort)((name[0] << 8) | name[1]);
                return true;
            }
        }
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    public bool convertColorC(ELObj obj, Identifier? ident, Location loc, out ColorObj? result)
    {
        result = obj.asColor();
        if (result != null)
            return true;
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    public bool convertOptColorC(ELObj obj, Identifier? ident, Location loc, out ColorObj? result)
    {
        result = null;
        if (obj == makeFalse())
            return true;
        return convertColorC(obj, ident, loc, out result);
    }

    public void invalidCharacteristicValue(Identifier? ident, Location loc)
    {
        string name = ident?.name().ToString() ?? "unknown";
        message(MessageType.Severity.error, loc, $"Invalid value for characteristic {name}");
    }

    public void makeReadOnly(ELObj? obj)
    {
        // Mark object as read-only (GC semantics - in C# we don't need to do anything special)
    }

    // Processing mode table
    private Dictionary<string, ProcessingMode> processingModeTable_ = new();

    public ProcessingMode? lookupProcessingMode(StringC name)
    {
        string key = name.ToString();
        if (processingModeTable_.TryGetValue(key, out ProcessingMode? mode))
            return mode;
        return null;
    }

    public ProcessingMode getProcessingMode(StringC name)
    {
        string key = name.ToString();
        if (processingModeTable_.TryGetValue(key, out ProcessingMode? mode))
            return mode;
        mode = new ProcessingMode(name);
        processingModeTable_[key] = mode;
        return mode;
    }

    // Initial values storage
    private System.Collections.Generic.List<Identifier> initialValueNames_ = new();
    private System.Collections.Generic.List<Expression?> initialValueValues_ = new();
    private int currentPartFirstInitialValue_ = 0;

    public void installInitialValue(Identifier ident, Expression expr)
    {
        for (int i = 0; i < initialValueNames_.Count; i++)
        {
            if (ident == initialValueNames_[i])
            {
                if (i >= currentPartFirstInitialValue_)
                {
                    setNextLocation(expr.location());
                    message(InterpreterMessages.duplicateInitialValue, ident.name().ToString());
                }
                return;
            }
        }
        initialValueValues_.Add(expr);
        initialValueNames_.Add(ident);
    }

    // Compile all definitions
    public void compile()
    {
        compileInitialValues();
        initialProcessingMode_?.compile(this);
        foreach (var mode in processingModeTable_.Values)
            mode.compile(this);
    }

    public void compileInitialValues()
    {
        var ics = new System.Collections.Generic.List<ConstPtr<InheritedC>>();
        for (int i = 0; i < initialValueNames_.Count; i++)
        {
            Identifier ident = initialValueNames_[i];
            Expression? expr = initialValueValues_[i];
            if (expr == null)
                continue;

            ConstPtr<InheritedC>? icPtr = ident.inheritedC();
            if (icPtr == null || icPtr.isNull())
                continue;

            InheritedC ic = icPtr.pointer()!;

            expr.optimize(this, new Environment(), ref expr);
            ELObj? val = expr.constantValue();
            if (val != null)
            {
                ConstPtr<InheritedC>? tem = ic.make(val, expr.location(), this);
                if (tem != null && !tem.isNull())
                    ics.Add(tem);
            }
            else
            {
                ics.Add(new ConstPtr<InheritedC>(new VarInheritedC(
                    icPtr,
                    expr.compile(this, new Environment(), 0, new InsnPtr()),
                    expr.location())));
            }
        }
        if (ics.Count > 0)
        {
            var forceIcs = new System.Collections.Generic.List<ConstPtr<InheritedC>>();
            initialStyle_ = new VarStyleObj(new ConstPtr<StyleSpec>(new StyleSpec(forceIcs, ics)), null, null, new NodePtr());
            makePermanent(initialStyle_);
        }
    }

    // Character property lookup
    public ELObj charProperty(StringC propertyName, Char c, Location loc, int argi)
    {
        // Default implementation - returns false for most properties
        string prop = propertyName.ToString();
        if (prop == "input-whitespace?")
        {
            // Standard ASCII whitespace
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\f')
                return trueObj_;
            return falseObj_;
        }
        return falseObj_;
    }

    // Unit conversion support
    private Dictionary<string, long> unitTable_ = new()
    {
        { "pt", 1000 },          // 1000 units per point (based on 72000 units/inch = 72 points/inch * 1000)
        { "pc", 12000 },         // 12 points = 1 pica
        { "in", 72000 },         // 72 points = 1 inch, so 72000 units/inch
        { "cm", 28346 },         // 72000 / 2.54 ≈ 28346 units/cm
        { "mm", 2835 },          // 72000 / 25.4 ≈ 2835 units/mm
        { "px", 1000 },          // Assume 1px = 1pt for screen (CSS standard 96dpi would be 750)
        { "em", 0 },             // Relative unit - context dependent
        { "ex", 0 },             // Relative unit - context dependent
    };

    public long lookupUnit(StringC name)
    {
        string key = name.ToString().ToLowerInvariant();
        if (unitTable_.TryGetValue(key, out long value))
            return value;
        return 0;
    }

    public void installUnit(StringC name, long value)
    {
        unitTable_[name.ToString().ToLowerInvariant()] = value;
    }
}

// Interpreter error messages
public enum InterpreterMessages
{
    noCurrentNode,
    noCurrentProcessingMode,
    notANodeList,
    notAnOptSingletonNode,
    notASosofo,
    notAStyle,
    notAString,
    notAnExactInteger,
    notANumber,
    notAChar,
    notAList,
    notAPair,
    notAVector,
    notAKeyword,
    outOfRange,
    divideByZero,
    errorProc,
    notInCharacteristicValue,
    duplicateInitialValue,
    caseFail,
    // Parser messages
    unknownTopLevelForm,
    badTopLevelForm,
    badDeclaration,
    badExpression,
    unexpectedToken,
    unterminatedString,
    invalidCharName,
    invalidCharNumber,
}
