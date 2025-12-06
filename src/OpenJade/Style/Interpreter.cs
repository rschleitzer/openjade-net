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
    private bool dsssl2_ = false;
    private bool strictMode_ = false;

    // Symbol and identifier tables
    private Dictionary<string, SymbolObj> symbolTable_ = new();
    private Dictionary<string, Identifier> identTable_ = new();

    public Interpreter()
    {
        // Create the default/initial processing mode
        initialProcessingMode_ = new ProcessingMode(new StringC()); // Empty name = initial mode
    }

    public int unitsPerInch() { return unitsPerInch_; }
    public void setUnitsPerInch(int u) { unitsPerInch_ = u; }
    public bool dsssl2() { return dsssl2_; }
    public void setDsssl2(bool d) { dsssl2_ = d; }
    public bool strictMode() { return strictMode_; }
    public void setStrictMode(bool s) { strictMode_ = s; }

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

    // Named character table for standard characters
    private struct CharPart
    {
        public int c;
        public uint defPart;
    }
    private Dictionary<string, CharPart> namedCharTable_ = new();
    private uint dPartIndex_ = 0;

    public void addStandardChar(StringC name, StringC num)
    {
        if (!int.TryParse(num.ToString(), out int n))
        {
            message(InterpreterMessages.invalidCharNumber, num.ToString());
            return;
        }

        string key = name.ToString();
        if (namedCharTable_.TryGetValue(key, out CharPart def))
        {
            if (dPartIndex_ < def.defPart)
                namedCharTable_[key] = new CharPart { c = n, defPart = dPartIndex_ };
            else if (def.defPart == dPartIndex_ && def.c != n)
                message(InterpreterMessages.duplicateCharName, key);
        }
        else
        {
            namedCharTable_[key] = new CharPart { c = n, defPart = dPartIndex_ };
        }
    }

    public void addNameChar(StringC name)
    {
        string key = name.ToString();
        if (!namedCharTable_.TryGetValue(key, out CharPart cp))
        {
            message(InterpreterMessages.badCharName, key);
            return;
        }
        // FIXME: set lexical category for character
    }

    public void addSeparatorChar(StringC name)
    {
        string key = name.ToString();
        if (!namedCharTable_.TryGetValue(key, out CharPart cp))
        {
            message(InterpreterMessages.badCharName, key);
            return;
        }
        // FIXME: set lexical category for character
    }

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

    public ProcessingMode defineProcessingMode(StringC name, Location loc)
    {
        string key = name.ToString();
        if (processingModeTable_.TryGetValue(key, out ProcessingMode? mode))
            return mode;
        mode = new ProcessingMode(name, loc);
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

    // SDATA entity mapping
    private Dictionary<string, Char> sdataEntityNameTable_ = new Dictionary<string, Char>();
    private Dictionary<string, Char> sdataEntityTextTable_ = new Dictionary<string, Char>();

    public void addSdataEntity(StringC ename, StringC etext, StringC charName)
    {
        // Look up the character name - for now, simple ASCII lookup
        // A full implementation would use a named character table
        if (charName.size() == 0)
            return;

        // Try to interpret the char name as a simple character
        Char c = charName[0];

        // Add to entity tables if name or text is non-empty
        if (ename.size() > 0)
        {
            sdataEntityNameTable_[ename.ToString()] = c;
        }
        if (etext.size() > 0)
        {
            sdataEntityTextTable_[etext.ToString()] = c;
        }
    }

    public bool sdataMap(StringC name, StringC text, out Char c)
    {
        string nameStr = name.ToString();
        string textStr = text.ToString();

        if (sdataEntityNameTable_.TryGetValue(nameStr, out c))
            return true;
        if (sdataEntityTextTable_.TryGetValue(textStr, out c))
            return true;

        c = 0;
        return false;
    }

    // Address support
    private AddressObj? addressNone_;

    public AddressObj makeAddressNone()
    {
        if (addressNone_ == null)
        {
            FOTBuilder.Address addr = new FOTBuilder.Address();
            addr.type = FOTBuilder.Address.Type.none;
            addressNone_ = new AddressObj(addr);
            makePermanent(addressNone_);
        }
        return addressNone_;
    }

    // String conversion
    public bool convertStringC(ELObj obj, Identifier? ident, Location loc, out StringC result)
    {
        Char[]? s;
        nuint n;
        if (obj.stringData(out s, out n) && s != null)
        {
            result = new StringC(s, n);
            return true;
        }
        result = new StringC();
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    // Character conversion
    public bool convertCharC(ELObj obj, Identifier? ident, Location loc, out Char result)
    {
        result = 0;
        if (obj.charValue(out result))
            return true;
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    // Real number conversion
    public bool convertRealC(ELObj obj, Identifier? ident, Location loc, out double result)
    {
        result = 0;
        if (obj.realValue(out result))
            return true;
        invalidCharacteristicValue(ident, loc);
        return false;
    }

    // Public ID storage
    public string? storePublicId(Char[] data, nuint size, Location loc)
    {
        if (data == null)
            return null;
        return new string(Array.ConvertAll(data, c => (char)c), 0, (int)size);
    }

    // Border styles
    private StyleObj? borderTrueStyle_;
    private StyleObj? borderFalseStyle_;

    public StyleObj? borderTrueStyle()
    {
        return borderTrueStyle_;
    }

    public StyleObj? borderFalseStyle()
    {
        return borderFalseStyle_;
    }

    public void setBorderTrueStyle(StyleObj? style)
    {
        borderTrueStyle_ = style;
    }

    public void setBorderFalseStyle(StyleObj? style)
    {
        borderFalseStyle_ = style;
    }

    // Port names for multi-port flow objects
    public enum PortName
    {
        portNumerator,
        portDenominator,
        portPreSup,
        portPreSub,
        portPostSup,
        portPostSub,
        portMidSup,
        portMidSub,
        portOverMark,
        portUnderMark,
        portOpen,
        portClose,
        portDegree,
        portOperator,
        portLowerLimit,
        portUpperLimit,
        portHeader,
        portFooter
    }

    public const int nPortNames = (int)PortName.portFooter + 1;

    private SymbolObj?[] portNames_ = new SymbolObj?[nPortNames];

    public SymbolObj portName(PortName i)
    {
        if (portNames_[(int)i] == null)
        {
            string name = i switch
            {
                PortName.portNumerator => "numerator",
                PortName.portDenominator => "denominator",
                PortName.portPreSup => "pre-sup",
                PortName.portPreSub => "pre-sub",
                PortName.portPostSup => "post-sup",
                PortName.portPostSub => "post-sub",
                PortName.portMidSup => "mid-sup",
                PortName.portMidSub => "mid-sub",
                PortName.portOverMark => "over-mark",
                PortName.portUnderMark => "under-mark",
                PortName.portOpen => "open",
                PortName.portClose => "close",
                PortName.portDegree => "degree",
                PortName.portOperator => "operator",
                PortName.portLowerLimit => "lower-limit",
                PortName.portUpperLimit => "upper-limit",
                PortName.portHeader => "header",
                PortName.portFooter => "footer",
                _ => i.ToString()
            };
            portNames_[(int)i] = makeSymbol(makeStringC(name));
            makePermanent(portNames_[(int)i]!);
        }
        return portNames_[(int)i]!;
    }

    // Fraction bar characteristic
    private ConstPtr<InheritedC>? fractionBarC_;

    public ConstPtr<InheritedC>? fractionBarC()
    {
        return fractionBarC_;
    }

    public void setFractionBarC(ConstPtr<InheritedC> ic)
    {
        fractionBarC_ = ic;
    }

    // Table border characteristics
    private ConstPtr<InheritedC>? tableBorderC_;
    private ConstPtr<InheritedC>? cellBeforeRowBorderC_;
    private ConstPtr<InheritedC>? cellAfterRowBorderC_;
    private ConstPtr<InheritedC>? cellBeforeColumnBorderC_;
    private ConstPtr<InheritedC>? cellAfterColumnBorderC_;

    public ConstPtr<InheritedC>? tableBorderC() { return tableBorderC_; }
    public ConstPtr<InheritedC>? cellBeforeRowBorderC() { return cellBeforeRowBorderC_; }
    public ConstPtr<InheritedC>? cellAfterRowBorderC() { return cellAfterRowBorderC_; }
    public ConstPtr<InheritedC>? cellBeforeColumnBorderC() { return cellBeforeColumnBorderC_; }
    public ConstPtr<InheritedC>? cellAfterColumnBorderC() { return cellAfterColumnBorderC_; }

    public void setTableBorderC(ConstPtr<InheritedC> ic) { tableBorderC_ = ic; }
    public void setCellBeforeRowBorderC(ConstPtr<InheritedC> ic) { cellBeforeRowBorderC_ = ic; }
    public void setCellAfterRowBorderC(ConstPtr<InheritedC> ic) { cellAfterRowBorderC_ = ic; }
    public void setCellBeforeColumnBorderC(ConstPtr<InheritedC> ic) { cellBeforeColumnBorderC_ = ic; }
    public void setCellAfterColumnBorderC(ConstPtr<InheritedC> ic) { cellAfterColumnBorderC_ = ic; }

    // Enum conversion with specific allowed values
    public bool convertEnumC(FOTBuilder.Symbol[] vals, int nVals, ELObj obj, Identifier? ident, Location loc, out FOTBuilder.Symbol result)
    {
        result = FOTBuilder.Symbol.symbolFalse;
        if (obj == makeFalse())
        {
            // Check if symbolFalse is in the allowed values
            for (int i = 0; i < nVals; i++)
            {
                if (vals[i] == FOTBuilder.Symbol.symbolFalse)
                {
                    result = FOTBuilder.Symbol.symbolFalse;
                    return true;
                }
            }
            invalidCharacteristicValue(ident, loc);
            return false;
        }
        if (obj == makeTrue())
        {
            // Check if symbolTrue is in the allowed values
            for (int i = 0; i < nVals; i++)
            {
                if (vals[i] == FOTBuilder.Symbol.symbolTrue)
                {
                    result = FOTBuilder.Symbol.symbolTrue;
                    return true;
                }
            }
            invalidCharacteristicValue(ident, loc);
            return false;
        }
        SymbolObj? sym = obj.asSymbol();
        if (sym == null)
        {
            invalidCharacteristicValue(ident, loc);
            return false;
        }
        // Map symbol name to FOTBuilder.Symbol
        string name = sym.name().ToString().ToLower().Replace("-", "");
        if (Enum.TryParse<FOTBuilder.Symbol>("symbol" + name, true, out FOTBuilder.Symbol parsed))
        {
            for (int i = 0; i < nVals; i++)
            {
                if (vals[i] == parsed)
                {
                    result = parsed;
                    return true;
                }
            }
        }
        invalidCharacteristicValue(ident, loc);
        return false;
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
    invalidUnquoteSplicing,
    badQuasiquote,
    // Table messages
    tableRowOutsideTable,
    tableCellOutsideTable,
    invalidCharacteristicValue,
    // DSSSL spec messages
    missingPart,
    useLoop,
    specNotArc,
    noParts,
    // Color messages
    colorArgCount,
    notAColorSpace,
    unknownColorSpaceFamily,
    colorSpaceArgError,
    colorSpaceNoArgs,
    // ProcessContext messages
    processNodeLoop,
    badConnection,
    contentMapBadPort,
    badContentMap,
    // Identifier messages
    identifierLoop,
    // Character definition messages
    duplicateCharName,
    badCharName,
}
