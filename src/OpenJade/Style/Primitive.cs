// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// DSSSL primitive functions

// Cons primitive
public class ConsPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public ConsPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return interp.makePair(args[0], args[1]);
    }
}

// List primitive
public class ListPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public ListPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (nArgs == 0)
            return interp.makeNil();
        PairObj head = interp.makePair(args[0], null);
        PairObj tail = head;
        for (int i = 1; i < nArgs; i++)
        {
            PairObj newTail = interp.makePair(args[i], null);
            tail.setCdr(newTail);
            tail = newTail;
        }
        tail.setCdr(interp.makeNil());
        return head;
    }
}

// Null? primitive
public class IsNullPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsNullPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0]?.isNil() == true ? interp.makeTrue() : interp.makeFalse();
    }
}

// List? primitive
public class IsListPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsListPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0]?.isList() == true ? interp.makeTrue() : interp.makeFalse();
    }
}

// Pair? primitive
public class IsPairPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsPairPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0]?.asPair() != null ? interp.makeTrue() : interp.makeFalse();
    }
}

// Equal? primitive
public class IsEqualPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public IsEqualPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (args[0] == null && args[1] == null)
            return interp.makeTrue();
        if (args[0] == null || args[1] == null)
            return interp.makeFalse();
        return ELObj.equal(args[0], args[1]) ? interp.makeTrue() : interp.makeFalse();
    }
}

// Car primitive
public class CarPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public CarPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        PairObj? pair = args[0]?.asPair();
        if (pair == null)
            return argError(interp, loc, 0, args[0]);
        return pair.car();
    }
}

// Cdr primitive
public class CdrPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public CdrPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        PairObj? pair = args[0]?.asPair();
        if (pair == null)
            return argError(interp, loc, 0, args[0]);
        return pair.cdr();
    }
}

// Length primitive
public class LengthPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public LengthPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long len = 0;
        ELObj? list = args[0];
        while (list != null && !list.isNil())
        {
            PairObj? pair = list.asPair();
            if (pair == null)
                return argError(interp, loc, 0, args[0]);
            len++;
            list = pair.cdr();
        }
        return interp.makeInteger(len);
    }
}

// Append primitive
public class AppendPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public AppendPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (nArgs == 0)
            return interp.makeNil();
        if (nArgs == 1)
            return args[0];

        PairObj? head = null;
        PairObj? tail = null;
        for (int i = 0; i < nArgs - 1; i++)
        {
            ELObj? list = args[i];
            while (list != null && !list.isNil())
            {
                PairObj? pair = list.asPair();
                if (pair == null)
                    return argError(interp, loc, i, args[i]);
                PairObj newPair = interp.makePair(pair.car(), null);
                if (head == null)
                {
                    head = newPair;
                    tail = newPair;
                }
                else
                {
                    tail!.setCdr(newPair);
                    tail = newPair;
                }
                list = pair.cdr();
            }
        }
        if (tail != null)
            tail.setCdr(args[nArgs - 1]);
        else
            return args[nArgs - 1];
        return head;
    }
}

// Reverse primitive
public class ReversePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public ReversePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        ELObj? result = interp.makeNil();
        ELObj? list = args[0];
        while (list != null && !list.isNil())
        {
            PairObj? pair = list.asPair();
            if (pair == null)
                return argError(interp, loc, 0, args[0]);
            result = interp.makePair(pair.car(), result);
            list = pair.cdr();
        }
        return result;
    }
}

// Not primitive
public class NotPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public NotPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0]?.isTrue() != true ? interp.makeTrue() : interp.makeFalse();
    }
}

// Symbol? primitive
public class IsSymbolPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsSymbolPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0]?.asSymbol() != null ? interp.makeTrue() : interp.makeFalse();
    }
}

// Keyword? primitive
public class IsKeywordPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsKeywordPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0]?.asKeyword() != null ? interp.makeTrue() : interp.makeFalse();
    }
}

// Boolean? primitive
public class IsBooleanPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsBooleanPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return (args[0] is TrueObj || args[0] is FalseObj) ? interp.makeTrue() : interp.makeFalse();
    }
}

// Procedure? primitive
public class IsProcedurePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsProcedurePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0]?.asFunction() != null ? interp.makeTrue() : interp.makeFalse();
    }
}

// String? primitive
public class IsStringPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsStringPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0] is StringObj ? interp.makeTrue() : interp.makeFalse();
    }
}

// Integer? primitive
public class IsIntegerPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsIntegerPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0] is IntegerObj ? interp.makeTrue() : interp.makeFalse();
    }
}

// Real? primitive
public class IsRealPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsRealPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return (args[0] is IntegerObj || args[0] is RealObj) ? interp.makeTrue() : interp.makeFalse();
    }
}

// Number? primitive
public class IsNumberPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsNumberPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return (args[0] is IntegerObj || args[0] is RealObj || args[0] is LengthObj || args[0] is QuantityObj)
            ? interp.makeTrue() : interp.makeFalse();
    }
}

// Char? primitive
public class IsCharPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsCharPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0] is CharObj ? interp.makeTrue() : interp.makeFalse();
    }
}

// + primitive
public class PlusPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public PlusPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (nArgs == 0)
            return interp.makeInteger(0);

        long intSum = 0;
        double doubleSum = 0;
        bool isDouble = false;
        int dim = 0;

        for (int i = 0; i < nArgs; i++)
        {
            long lval = 0;
            double dval = 0;
            int d = 0;
            var q = args[i]?.quantityValue(out lval, out dval, out d) ?? ELObj.QuantityType.noQuantity;
            if (q == ELObj.QuantityType.noQuantity)
                return argError(interp, loc, i, args[i]);
            if (i == 0)
                dim = d;
            else if (d != dim)
            {
                interp.setNextLocation(loc);
                return interp.makeError();
            }
            if (q == ELObj.QuantityType.doubleQuantity)
            {
                if (!isDouble)
                {
                    isDouble = true;
                    doubleSum = intSum;
                }
                doubleSum += dval;
            }
            else
            {
                if (isDouble)
                    doubleSum += lval;
                else
                    intSum += lval;
            }
        }

        if (dim == 0)
            return isDouble ? (ELObj)interp.makeReal(doubleSum) : interp.makeInteger(intSum);
        else if (dim == 1)
            return isDouble ? (ELObj)interp.makeQuantity(doubleSum, dim) : interp.makeLength(intSum);
        else
            return isDouble ? (ELObj)interp.makeQuantity(doubleSum, dim) : interp.makeQuantity(intSum, dim);
    }
}

// - primitive
public class MinusPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, true);
    public MinusPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long lval = 0;
        double dval = 0;
        int dim = 0;
        var q = args[0]?.quantityValue(out lval, out dval, out dim) ?? ELObj.QuantityType.noQuantity;

        if (nArgs == 1)
        {
            // Unary negation
            if (q == ELObj.QuantityType.noQuantity)
                return argError(interp, loc, 0, args[0]);
            if (q == ELObj.QuantityType.doubleQuantity)
                return dim == 0 ? (ELObj)interp.makeReal(-dval) : interp.makeQuantity(-dval, dim);
            else
                return dim == 0 ? (ELObj)interp.makeInteger(-lval) :
                    (dim == 1 ? (ELObj)interp.makeLength(-lval) : interp.makeQuantity(-lval, dim));
        }

        // Binary subtraction
        if (q == ELObj.QuantityType.noQuantity)
            return argError(interp, loc, 0, args[0]);

        long intResult = lval;
        double doubleResult = dval;
        int resultDim = dim;
        bool isDouble = (q == ELObj.QuantityType.doubleQuantity);

        for (int i = 1; i < nArgs; i++)
        {
            lval = 0; dval = 0; dim = 0;
            q = args[i]?.quantityValue(out lval, out dval, out dim) ?? ELObj.QuantityType.noQuantity;
            if (q == ELObj.QuantityType.noQuantity)
                return argError(interp, loc, i, args[i]);
            if (dim != resultDim)
            {
                interp.setNextLocation(loc);
                return interp.makeError();
            }
            if (q == ELObj.QuantityType.doubleQuantity)
            {
                if (!isDouble)
                {
                    isDouble = true;
                    doubleResult = intResult;
                }
                doubleResult -= dval;
            }
            else
            {
                if (isDouble)
                    doubleResult -= lval;
                else
                    intResult -= lval;
            }
        }

        if (resultDim == 0)
            return isDouble ? (ELObj)interp.makeReal(doubleResult) : interp.makeInteger(intResult);
        else if (resultDim == 1)
            return isDouble ? (ELObj)interp.makeQuantity(doubleResult, resultDim) : interp.makeLength(intResult);
        else
            return isDouble ? (ELObj)interp.makeQuantity(doubleResult, resultDim) : interp.makeQuantity(intResult, resultDim);
    }
}

// * primitive
public class MultiplyPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public MultiplyPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (nArgs == 0)
            return interp.makeInteger(1);

        long intResult = 1;
        double doubleResult = 1;
        bool isDouble = false;
        int totalDim = 0;

        for (int i = 0; i < nArgs; i++)
        {
            long lval = 0;
            double dval = 0;
            int dim = 0;
            var q = args[i]?.quantityValue(out lval, out dval, out dim) ?? ELObj.QuantityType.noQuantity;
            if (q == ELObj.QuantityType.noQuantity)
                return argError(interp, loc, i, args[i]);
            totalDim += dim;
            if (q == ELObj.QuantityType.doubleQuantity)
            {
                if (!isDouble)
                {
                    isDouble = true;
                    doubleResult = intResult;
                }
                doubleResult *= dval;
            }
            else
            {
                if (isDouble)
                    doubleResult *= lval;
                else
                    intResult *= lval;
            }
        }

        if (totalDim == 0)
            return isDouble ? (ELObj)interp.makeReal(doubleResult) : interp.makeInteger(intResult);
        else
            return isDouble ? (ELObj)interp.makeQuantity(doubleResult, totalDim) : interp.makeQuantity(intResult, totalDim);
    }
}

// / primitive
public class DividePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, true);
    public DividePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long lval = 0;
        double dval = 0;
        int dim = 0;
        var q0 = args[0]?.quantityValue(out lval, out dval, out dim) ?? ELObj.QuantityType.noQuantity;
        if (q0 == ELObj.QuantityType.noQuantity)
            return argError(interp, loc, 0, args[0]);

        double result = (q0 == ELObj.QuantityType.doubleQuantity) ? dval : lval;
        int resultDim = dim;

        if (nArgs == 1)
        {
            // Reciprocal
            if (result == 0)
            {
                interp.setNextLocation(loc);
                return interp.makeError();
            }
            return interp.makeQuantity(1.0 / result, -resultDim);
        }

        for (int i = 1; i < nArgs; i++)
        {
            lval = 0; dval = 0; dim = 0;
            var q = args[i]?.quantityValue(out lval, out dval, out dim) ?? ELObj.QuantityType.noQuantity;
            if (q == ELObj.QuantityType.noQuantity)
                return argError(interp, loc, i, args[i]);
            double divisor = (q == ELObj.QuantityType.doubleQuantity) ? dval : lval;
            if (divisor == 0)
            {
                interp.setNextLocation(loc);
                return interp.makeError();
            }
            result /= divisor;
            resultDim -= dim;
        }

        if (resultDim == 0)
            return interp.makeReal(result);
        else
            return interp.makeQuantity(result, resultDim);
    }
}

// < primitive
public class LessPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public LessPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (nArgs < 2)
            return interp.makeTrue();

        for (int i = 0; i < nArgs - 1; i++)
        {
            long lval1 = 0, lval2 = 0;
            double dval1 = 0, dval2 = 0;
            int dim1 = 0, dim2 = 0;
            var q1 = args[i]?.quantityValue(out lval1, out dval1, out dim1) ?? ELObj.QuantityType.noQuantity;
            var q2 = args[i + 1]?.quantityValue(out lval2, out dval2, out dim2) ?? ELObj.QuantityType.noQuantity;
            if (q1 == ELObj.QuantityType.noQuantity)
                return argError(interp, loc, i, args[i]);
            if (q2 == ELObj.QuantityType.noQuantity)
                return argError(interp, loc, i + 1, args[i + 1]);
            if (dim1 != dim2)
            {
                interp.setNextLocation(loc);
                return interp.makeError();
            }
            double v1 = (q1 == ELObj.QuantityType.doubleQuantity) ? dval1 : lval1;
            double v2 = (q2 == ELObj.QuantityType.doubleQuantity) ? dval2 : lval2;
            if (!(v1 < v2))
                return interp.makeFalse();
        }
        return interp.makeTrue();
    }
}

// > primitive
public class GreaterPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public GreaterPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (nArgs < 2)
            return interp.makeTrue();

        for (int i = 0; i < nArgs - 1; i++)
        {
            long lval1 = 0, lval2 = 0;
            double dval1 = 0, dval2 = 0;
            int dim1 = 0, dim2 = 0;
            var q1 = args[i]?.quantityValue(out lval1, out dval1, out dim1) ?? ELObj.QuantityType.noQuantity;
            var q2 = args[i + 1]?.quantityValue(out lval2, out dval2, out dim2) ?? ELObj.QuantityType.noQuantity;
            if (q1 == ELObj.QuantityType.noQuantity)
                return argError(interp, loc, i, args[i]);
            if (q2 == ELObj.QuantityType.noQuantity)
                return argError(interp, loc, i + 1, args[i + 1]);
            if (dim1 != dim2)
            {
                interp.setNextLocation(loc);
                return interp.makeError();
            }
            double v1 = (q1 == ELObj.QuantityType.doubleQuantity) ? dval1 : lval1;
            double v2 = (q2 == ELObj.QuantityType.doubleQuantity) ? dval2 : lval2;
            if (!(v1 > v2))
                return interp.makeFalse();
        }
        return interp.makeTrue();
    }
}

// = primitive
public class EqualPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public EqualPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (nArgs < 2)
            return interp.makeTrue();

        long lval0 = 0;
        double dval0 = 0;
        int dim0 = 0;
        var q0 = args[0]?.quantityValue(out lval0, out dval0, out dim0) ?? ELObj.QuantityType.noQuantity;
        if (q0 == ELObj.QuantityType.noQuantity)
            return argError(interp, loc, 0, args[0]);
        double v0 = (q0 == ELObj.QuantityType.doubleQuantity) ? dval0 : lval0;

        for (int i = 1; i < nArgs; i++)
        {
            long lval = 0;
            double dval = 0;
            int dim = 0;
            var q = args[i]?.quantityValue(out lval, out dval, out dim) ?? ELObj.QuantityType.noQuantity;
            if (q == ELObj.QuantityType.noQuantity)
                return argError(interp, loc, i, args[i]);
            if (dim != dim0)
            {
                interp.setNextLocation(loc);
                return interp.makeError();
            }
            double v = (q == ELObj.QuantityType.doubleQuantity) ? dval : lval;
            if (v != v0)
                return interp.makeFalse();
        }
        return interp.makeTrue();
    }
}

// Floor primitive
public class FloorPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public FloorPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double dval = 0;
        if (args[0]?.realValue(out dval) != true)
            return argError(interp, loc, 0, args[0]);
        return interp.makeInteger((long)Math.Floor(dval));
    }
}

// Ceiling primitive
public class CeilingPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public CeilingPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double dval = 0;
        if (args[0]?.realValue(out dval) != true)
            return argError(interp, loc, 0, args[0]);
        return interp.makeInteger((long)Math.Ceiling(dval));
    }
}

// Round primitive
public class RoundPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public RoundPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double dval = 0;
        if (args[0]?.realValue(out dval) != true)
            return argError(interp, loc, 0, args[0]);
        return interp.makeInteger((long)Math.Round(dval, MidpointRounding.AwayFromZero));
    }
}

// Abs primitive
public class AbsPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public AbsPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long lval = 0;
        double dval = 0;
        int dim = 0;
        var q = args[0]?.quantityValue(out lval, out dval, out dim) ?? ELObj.QuantityType.noQuantity;
        if (q == ELObj.QuantityType.noQuantity)
            return argError(interp, loc, 0, args[0]);
        if (q == ELObj.QuantityType.doubleQuantity)
            return dim == 0 ? (ELObj)interp.makeReal(Math.Abs(dval)) : interp.makeQuantity(Math.Abs(dval), dim);
        else
            return dim == 0 ? (ELObj)interp.makeInteger(Math.Abs(lval)) :
                (dim == 1 ? (ELObj)interp.makeLength(Math.Abs(lval)) : interp.makeQuantity(Math.Abs(lval), dim));
    }
}

// Sqrt primitive
public class SqrtPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public SqrtPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double dval = 0;
        if (args[0]?.realValue(out dval) != true)
            return argError(interp, loc, 0, args[0]);
        if (dval < 0)
        {
            interp.setNextLocation(loc);
            return interp.makeError();
        }
        return interp.makeReal(Math.Sqrt(dval));
    }
}

// Symbol->string primitive
public class SymbolToStringPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public SymbolToStringPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        SymbolObj? sym = args[0]?.asSymbol();
        if (sym == null)
            return argError(interp, loc, 0, args[0]);
        return sym.name();
    }
}

// Sosofo? primitive
public class IsSosofoPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsSosofoPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0]?.asSosofo() != null ? interp.makeTrue() : interp.makeFalse();
    }
}

// Style? primitive
public class IsStylePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsStylePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0]?.asStyle() != null ? interp.makeTrue() : interp.makeFalse();
    }
}

// Empty-sosofo primitive
public class EmptySosofoPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, false);
    public EmptySosofoPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return new EmptySosofoObj();
    }
}

// Vector? primitive
public class IsVectorPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsVectorPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0]?.asVector() != null ? interp.makeTrue() : interp.makeFalse();
    }
}

// Vector primitive
public class VectorPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public VectorPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        var elems = new System.Collections.Generic.List<ELObj?>(nArgs);
        for (int i = 0; i < nArgs; i++)
            elems.Add(args[i]);
        return interp.makeVector(elems);
    }
}

// Eqv? primitive
public class IsEqvPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public IsEqvPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (args[0] == null && args[1] == null)
            return interp.makeTrue();
        if (args[0] == null || args[1] == null)
            return interp.makeFalse();
        return ELObj.eqv(args[0], args[1]) ? interp.makeTrue() : interp.makeFalse();
    }
}
