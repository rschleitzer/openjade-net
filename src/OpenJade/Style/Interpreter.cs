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
}
