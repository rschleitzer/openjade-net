// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// The DSSSL Interpreter
public class Interpreter : IInterpreter
{
    // Singleton objects
    private ErrorObj errorObj_ = new ErrorObj();
    private UnspecifiedObj unspecifiedObj_ = new UnspecifiedObj();
    private NilObj nilObj_ = new NilObj();
    private TrueObj trueObj_ = new TrueObj();
    private FalseObj falseObj_ = new FalseObj();
    private int unitsPerInch_ = 72000; // Default: 1000 units per point

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
        throw new NotImplementedException();
    }

    public RealObj makeReal(double n)
    {
        return new RealObj(n);
    }

    public CharObj makeChar(Char c)
    {
        throw new NotImplementedException();
    }

    public StringObj makeString(StringC str)
    {
        return new StringObj(str);
    }

    public PairObj makePair(ELObj? car, ELObj? cdr)
    {
        return new PairObj(car, cdr);
    }

    public SymbolObj makeSymbol(StringC name)
    {
        throw new NotImplementedException();
    }

    public KeywordObj makeKeyword(Identifier? ident)
    {
        throw new NotImplementedException();
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

    public void message(MessageType type, Location loc, string text)
    {
        throw new NotImplementedException();
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
}
