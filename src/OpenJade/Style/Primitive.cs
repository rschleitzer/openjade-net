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

// Current-node primitive
public class CurrentNodePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, false);
    public CurrentNodePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (ctx.currentNode == null)
            return noCurrentNodeError(interp, loc);
        return new NodePtrNodeListObj(ctx.currentNode);
    }

    private static ELObj? noCurrentNodeError(Interpreter interp, Location loc)
    {
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.noCurrentNode);
        return interp.makeError();
    }
}

// Node-list? primitive
public class IsNodeListPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsNodeListPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0]?.asNodeList() != null ? interp.makeTrue() : interp.makeFalse();
    }
}

// Node-list-empty? primitive
public class IsNodeListEmptyPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsNodeListEmptyPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodeListObj? nl = args[0]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);
        if (nl.nodeListFirst(ctx, interp) != null)
            return interp.makeFalse();
        return interp.makeTrue();
    }
}

// Node-list-first primitive
public class NodeListFirstPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public NodeListFirstPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodeListObj? nl = args[0]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);
        NodePtr? nd = nl.nodeListFirst(ctx, interp);
        return new NodePtrNodeListObj(nd);
    }
}

// Node-list-rest primitive
public class NodeListRestPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public NodeListRestPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodeListObj? nl = args[0]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);
        return nl.nodeListRest(ctx, interp);
    }
}

// Node-list primitive
public class NodeListPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public NodeListPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (nArgs == 0)
            return interp.makeEmptyNodeList();
        int i = nArgs - 1;
        NodeListObj? nl = args[i]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, i, args[i]);
        for (i--; i >= 0; i--)
        {
            NodeListObj? tem = args[i]?.asNodeList();
            if (tem == null)
                return argError(interp, loc, InterpreterMessages.notANodeList, i, args[i]);
            nl = new PairNodeListObj(tem, nl);
        }
        return nl;
    }
}

// Empty-node-list primitive
public class EmptyNodeListPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, false);
    public EmptyNodeListPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return interp.makeEmptyNodeList();
    }
}

// Children primitive
public class ChildrenPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public ChildrenPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (!args[0]!.optSingletonNodeList(ctx, interp, ref node))
        {
            NodeListObj? nl = args[0]?.asNodeList();
            if (nl != null)
                return new MapNodeListObj(this, nl, new MapNodeListObj.Context(ctx, loc));
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);
        }
        if (node == null)
            return args[0];
        NodeListPtr? nlp = null;
        if (node.children(ref nlp) != AccessResult.accessOK)
            return interp.makeEmptyNodeList();
        return new NodeListPtrNodeListObj(nlp!);
    }
}

// Parent primitive
public class ParentPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 1, false);
    public ParentPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (nArgs > 0)
        {
            if (!args[0]!.optSingletonNodeList(ctx, interp, ref node))
                return argError(interp, loc, InterpreterMessages.notAnOptSingletonNode, 0, args[0]);
            if (node == null)
                return args[0];
        }
        else
        {
            node = ctx.currentNode;
            if (node == null)
                return noCurrentNodeError(interp, loc);
        }
        NodePtr parent = new NodePtr();
        if (node.getParent(ref parent) != AccessResult.accessOK)
            return interp.makeEmptyNodeList();
        return new NodePtrNodeListObj(parent);
    }

    private static ELObj? noCurrentNodeError(Interpreter interp, Location loc)
    {
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.noCurrentNode);
        return interp.makeError();
    }
}

// Gi primitive (get generic identifier / element name)
public class GiPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 1, false);
    public GiPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (nArgs > 0)
        {
            if (!args[0]!.optSingletonNodeList(ctx, interp, ref node))
                return argError(interp, loc, InterpreterMessages.notAnOptSingletonNode, 0, args[0]);
        }
        else
        {
            if (ctx.currentNode == null)
                return noCurrentNodeError(interp, loc);
            node = ctx.currentNode;
        }
        GroveString str = new GroveString();
        if (node != null && node.getGi(str) == AccessResult.accessOK)
            return interp.makeString(str.data(), str.size());
        return interp.makeFalse();
    }

    private static ELObj? noCurrentNodeError(Interpreter interp, Location loc)
    {
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.noCurrentNode);
        return interp.makeError();
    }
}

// Id primitive (get element ID)
public class IdPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 1, false);
    public IdPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (nArgs > 0)
        {
            if (!args[0]!.optSingletonNodeList(ctx, interp, ref node))
                return argError(interp, loc, InterpreterMessages.notAnOptSingletonNode, 0, args[0]);
        }
        else
        {
            if (ctx.currentNode == null)
                return noCurrentNodeError(interp, loc);
            node = ctx.currentNode;
        }
        GroveString str = new GroveString();
        if (node != null && node.getId(str) == AccessResult.accessOK)
            return interp.makeString(str.data(), str.size());
        return interp.makeFalse();
    }

    private static ELObj? noCurrentNodeError(Interpreter interp, Location loc)
    {
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.noCurrentNode);
        return interp.makeError();
    }
}

// Process-children primitive
public class ProcessChildrenPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, false);
    public ProcessChildrenPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (ctx.processingMode == null)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.noCurrentProcessingMode);
            return interp.makeError();
        }
        return new ProcessChildrenSosofoObj(ctx.processingMode);
    }
}

// Process-children-trim primitive
public class ProcessChildrenTrimPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, false);
    public ProcessChildrenTrimPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (ctx.processingMode == null)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.noCurrentProcessingMode);
            return interp.makeError();
        }
        return new ProcessChildrenTrimSosofoObj(ctx.processingMode);
    }
}

// Sosofo-append primitive
public class SosofoAppendPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public SosofoAppendPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (nArgs == 0)
            return new EmptySosofoObj();
        if (nArgs == 1)
        {
            SosofoObj? sosofo = args[0]?.asSosofo();
            if (sosofo == null)
                return argError(interp, loc, InterpreterMessages.notASosofo, 0, args[0]);
            return sosofo;
        }
        AppendSosofoObj? obj;
        int i = 0;
        if (args[i]?.asAppendSosofo() != null)
            obj = args[i++]!.asAppendSosofo();
        else
            obj = new AppendSosofoObj();
        for (; i < nArgs; i++)
        {
            SosofoObj? sosofo = args[i]?.asSosofo();
            if (sosofo == null)
                return argError(interp, loc, InterpreterMessages.notASosofo, i, args[i]);
            obj!.append(sosofo);
        }
        return obj;
    }
}

// Literal primitive
public class LiteralPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public LiteralPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (nArgs == 0)
            return new EmptySosofoObj();
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        if (nArgs == 1)
            return new LiteralSosofoObj(args[0]!);
        StringObj strObj = new StringObj(s!, n);
        for (int i = 1; i < nArgs; i++)
        {
            if (!args[i]!.stringData(out s, out n))
                return argError(interp, loc, InterpreterMessages.notAString, i, args[i]);
            strObj.append(s!, n);
        }
        return new LiteralSosofoObj(strObj);
    }
}

// Next-match primitive
public class NextMatchPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 1, false);
    public NextMatchPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (ctx.processingMode == null)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.noCurrentProcessingMode);
            return interp.makeError();
        }
        StyleObj? style = null;
        if (nArgs > 0)
        {
            style = args[0]?.asStyle();
            if (style == null)
                return argError(interp, loc, InterpreterMessages.notAStyle, 0, args[0]);
        }
        return new NextMatchSosofoObj(style);
    }
}

// Merge-style primitive
public class MergeStylePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public MergeStylePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        MergeStyleObj merged = new MergeStyleObj();
        for (int i = 0; i < nArgs; i++)
        {
            StyleObj? style = args[i]?.asStyle();
            if (style == null)
                return argError(interp, loc, InterpreterMessages.notAStyle, i, args[i]);
            merged.append(style);
        }
        return merged;
    }
}

// String-append primitive
public class StringAppendPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public StringAppendPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (nArgs == 0)
            return interp.makeString(Array.Empty<Char>(), 0);
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        if (nArgs == 1)
            return args[0];
        StringObj result = new StringObj(s!, n);
        for (int i = 1; i < nArgs; i++)
        {
            if (!args[i]!.stringData(out s, out n))
                return argError(interp, loc, InterpreterMessages.notAString, i, args[i]);
            result.append(s!, n);
        }
        return result;
    }
}

// String-length primitive
public class StringLengthPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public StringLengthPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        return interp.makeInteger((long)n);
    }
}

// String-ref primitive
public class StringRefPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public StringRefPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        long k = 0;
        if (!args[1]!.exactIntegerValue(out k))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
        if (k < 0 || (nuint)k >= n)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        return interp.makeChar(s![(int)k]);
    }
}

// Substring primitive
public class SubstringPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(3, 0, false);
    public SubstringPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        long start = 0;
        if (!args[1]!.exactIntegerValue(out start))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
        long end = 0;
        if (!args[2]!.exactIntegerValue(out end))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 2, args[2]);
        if (start < 0 || end < start || (nuint)end > n)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        Char[] result = new Char[end - start];
        Array.Copy(s!, (int)start, result, 0, (int)(end - start));
        return interp.makeString(result, (nuint)(end - start));
    }
}

// String=? primitive
public class IsStringEqualPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public IsStringEqualPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s1 = null;
        nuint n1 = 0;
        if (!args[0]!.stringData(out s1, out n1))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        Char[]? s2 = null;
        nuint n2 = 0;
        if (!args[1]!.stringData(out s2, out n2))
            return argError(interp, loc, InterpreterMessages.notAString, 1, args[1]);
        if (n1 != n2)
            return interp.makeFalse();
        for (nuint i = 0; i < n1; i++)
        {
            if (s1![(int)i] != s2![(int)i])
                return interp.makeFalse();
        }
        return interp.makeTrue();
    }
}

// List-tail primitive
public class ListTailPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public ListTailPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long k = 0;
        if (!args[1]!.exactIntegerValue(out k))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
        if (k < 0)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        ELObj? lst = args[0];
        for (long i = 0; i < k; i++)
        {
            PairObj? pair = lst?.asPair();
            if (pair == null)
                return argError(interp, loc, InterpreterMessages.notAList, 0, args[0]);
            lst = pair.cdr();
        }
        return lst;
    }
}

// List-ref primitive
public class ListRefPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public ListRefPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long k = 0;
        if (!args[1]!.exactIntegerValue(out k))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
        if (k < 0)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        ELObj? lst = args[0];
        for (long i = 0; i < k; i++)
        {
            PairObj? pair = lst?.asPair();
            if (pair == null)
                return argError(interp, loc, InterpreterMessages.notAList, 0, args[0]);
            lst = pair.cdr();
        }
        PairObj? p = lst?.asPair();
        if (p == null)
            return argError(interp, loc, InterpreterMessages.notAList, 0, args[0]);
        return p.car();
    }
}

// Member primitive
public class MemberPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public MemberPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        ELObj? lst = args[1];
        while (true)
        {
            if (lst?.isNil() == true)
                return interp.makeFalse();
            PairObj? pair = lst?.asPair();
            if (pair == null)
                return argError(interp, loc, InterpreterMessages.notAList, 1, args[1]);
            if (ELObj.equal(args[0], pair.car()))
                return lst;
            lst = pair.cdr();
        }
    }
}

// Assoc primitive
public class AssocPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public AssocPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        ELObj? lst = args[1];
        while (true)
        {
            if (lst?.isNil() == true)
                return interp.makeFalse();
            PairObj? pair = lst?.asPair();
            if (pair == null)
                return argError(interp, loc, InterpreterMessages.notAList, 1, args[1]);
            PairObj? assocPair = pair.car()?.asPair();
            if (assocPair == null)
                return argError(interp, loc, InterpreterMessages.notAPair, 1, args[1]);
            if (ELObj.equal(args[0], assocPair.car()))
                return assocPair;
            lst = pair.cdr();
        }
    }
}

// Quotient primitive
public class QuotientPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public QuotientPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long n1 = 0;
        if (!args[0]!.exactIntegerValue(out n1))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 0, args[0]);
        long n2 = 0;
        if (!args[1]!.exactIntegerValue(out n2))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
        if (n2 == 0)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.divideByZero);
            return interp.makeError();
        }
        return interp.makeInteger(n1 / n2);
    }
}

// Remainder primitive
public class RemainderPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public RemainderPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long n1 = 0;
        if (!args[0]!.exactIntegerValue(out n1))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 0, args[0]);
        long n2 = 0;
        if (!args[1]!.exactIntegerValue(out n2))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
        if (n2 == 0)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.divideByZero);
            return interp.makeError();
        }
        return interp.makeInteger(n1 % n2);
    }
}

// Modulo primitive
public class ModuloPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public ModuloPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long n1 = 0;
        if (!args[0]!.exactIntegerValue(out n1))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 0, args[0]);
        long n2 = 0;
        if (!args[1]!.exactIntegerValue(out n2))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
        if (n2 == 0)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.divideByZero);
            return interp.makeError();
        }
        long rem = n1 % n2;
        if (rem != 0 && (n1 < 0) != (n2 < 0))
            rem += n2;
        return interp.makeInteger(rem);
    }
}

// Min primitive
public class MinPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, true);
    public MinPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double minVal = 0;
        bool isExact = false;
        long minExact = 0;
        if (args[0]!.exactIntegerValue(out minExact))
        {
            isExact = true;
            minVal = minExact;
        }
        else if (!args[0]!.realValue(out minVal))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        for (int i = 1; i < nArgs; i++)
        {
            long n = 0;
            double d = 0;
            if (args[i]!.exactIntegerValue(out n))
            {
                if (isExact)
                {
                    if (n < minExact)
                        minExact = n;
                }
                else if (n < minVal)
                    minVal = n;
            }
            else if (args[i]!.realValue(out d))
            {
                isExact = false;
                if (d < minVal)
                    minVal = d;
            }
            else
                return argError(interp, loc, InterpreterMessages.notANumber, i, args[i]);
        }
        if (isExact)
            return interp.makeInteger(minExact);
        return interp.makeReal(minVal);
    }
}

// Max primitive
public class MaxPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, true);
    public MaxPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double maxVal = 0;
        bool isExact = false;
        long maxExact = 0;
        if (args[0]!.exactIntegerValue(out maxExact))
        {
            isExact = true;
            maxVal = maxExact;
        }
        else if (!args[0]!.realValue(out maxVal))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        for (int i = 1; i < nArgs; i++)
        {
            long n = 0;
            double d = 0;
            if (args[i]!.exactIntegerValue(out n))
            {
                if (isExact)
                {
                    if (n > maxExact)
                        maxExact = n;
                }
                else if (n > maxVal)
                    maxVal = n;
            }
            else if (args[i]!.realValue(out d))
            {
                isExact = false;
                if (d > maxVal)
                    maxVal = d;
            }
            else
                return argError(interp, loc, InterpreterMessages.notANumber, i, args[i]);
        }
        if (isExact)
            return interp.makeInteger(maxExact);
        return interp.makeReal(maxVal);
    }
}

// Truncate primitive
public class TruncatePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public TruncatePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double d = 0;
        if (!args[0]!.realValue(out d))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        return interp.makeInteger((long)Math.Truncate(d));
    }
}

// Exp primitive
public class ExpPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public ExpPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double d = 0;
        if (!args[0]!.realValue(out d))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        return interp.makeReal(Math.Exp(d));
    }
}

// Log primitive
public class LogPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public LogPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double d = 0;
        if (!args[0]!.realValue(out d))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        if (d <= 0)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        return interp.makeReal(Math.Log(d));
    }
}

// Sin primitive
public class SinPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public SinPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double d = 0;
        if (!args[0]!.realValue(out d))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        return interp.makeReal(Math.Sin(d));
    }
}

// Cos primitive
public class CosPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public CosPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double d = 0;
        if (!args[0]!.realValue(out d))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        return interp.makeReal(Math.Cos(d));
    }
}

// Tan primitive
public class TanPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public TanPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double d = 0;
        if (!args[0]!.realValue(out d))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        return interp.makeReal(Math.Tan(d));
    }
}

// Asin primitive
public class AsinPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public AsinPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double d = 0;
        if (!args[0]!.realValue(out d))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        if (d < -1 || d > 1)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        return interp.makeReal(Math.Asin(d));
    }
}

// Acos primitive
public class AcosPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public AcosPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double d = 0;
        if (!args[0]!.realValue(out d))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        if (d < -1 || d > 1)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        return interp.makeReal(Math.Acos(d));
    }
}

// Atan primitive
public class AtanPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    public AtanPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double y = 0;
        if (!args[0]!.realValue(out y))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        if (nArgs == 1)
            return interp.makeReal(Math.Atan(y));
        double x = 0;
        if (!args[1]!.realValue(out x))
            return argError(interp, loc, InterpreterMessages.notANumber, 1, args[1]);
        return interp.makeReal(Math.Atan2(y, x));
    }
}

// Expt primitive (power)
public class ExptPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public ExptPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        double b = 0;
        if (!args[0]!.realValue(out b))
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        double e = 0;
        if (!args[1]!.realValue(out e))
            return argError(interp, loc, InterpreterMessages.notANumber, 1, args[1]);
        return interp.makeReal(Math.Pow(b, e));
    }
}

// Number->string primitive
public class NumberToStringPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    public NumberToStringPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long radix = 10;
        if (nArgs > 1)
        {
            if (!args[1]!.exactIntegerValue(out radix))
                return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
            if (radix < 2 || radix > 36)
            {
                interp.setNextLocation(loc);
                interp.message(InterpreterMessages.outOfRange);
                return interp.makeError();
            }
        }
        long n = 0;
        if (args[0]!.exactIntegerValue(out n))
        {
            string s = Convert.ToString(n, (int)radix);
            return interp.makeString(new StringC(s));
        }
        double d = 0;
        if (args[0]!.realValue(out d))
        {
            string s = d.ToString();
            return interp.makeString(new StringC(s));
        }
        return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
    }
}

// String->number primitive
public class StringToNumberPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    public StringToNumberPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        long radix = 10;
        if (nArgs > 1)
        {
            if (!args[1]!.exactIntegerValue(out radix))
                return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
            if (radix < 2 || radix > 36)
            {
                interp.setNextLocation(loc);
                interp.message(InterpreterMessages.outOfRange);
                return interp.makeError();
            }
        }
        string str = new StringC(s!, n).ToString();
        try
        {
            if (radix == 10)
            {
                if (str.Contains('.') || str.Contains('e') || str.Contains('E'))
                {
                    double d = double.Parse(str);
                    return interp.makeReal(d);
                }
            }
            long val = Convert.ToInt64(str, (int)radix);
            return interp.makeInteger(val);
        }
        catch
        {
            return interp.makeFalse();
        }
    }
}

// Char->integer primitive
public class CharToIntegerPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public CharToIntegerPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char c = 0;
        if (!args[0]!.charValue(out c))
            return argError(interp, loc, InterpreterMessages.notAChar, 0, args[0]);
        return interp.makeInteger(c);
    }
}

// Integer->char primitive
public class IntegerToCharPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IntegerToCharPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long n = 0;
        if (!args[0]!.exactIntegerValue(out n))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 0, args[0]);
        if (n < 0 || n > 0x10FFFF)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        return interp.makeChar((Char)n);
    }
}

// Char<? primitive
public class IsCharLessPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public IsCharLessPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char c1 = 0;
        if (!args[0]!.charValue(out c1))
            return argError(interp, loc, InterpreterMessages.notAChar, 0, args[0]);
        Char c2 = 0;
        if (!args[1]!.charValue(out c2))
            return argError(interp, loc, InterpreterMessages.notAChar, 1, args[1]);
        return c1 < c2 ? interp.makeTrue() : interp.makeFalse();
    }
}

// Char<=? primitive
public class IsCharLessOrEqualPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public IsCharLessOrEqualPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char c1 = 0;
        if (!args[0]!.charValue(out c1))
            return argError(interp, loc, InterpreterMessages.notAChar, 0, args[0]);
        Char c2 = 0;
        if (!args[1]!.charValue(out c2))
            return argError(interp, loc, InterpreterMessages.notAChar, 1, args[1]);
        return c1 <= c2 ? interp.makeTrue() : interp.makeFalse();
    }
}

// Char=? primitive
public class IsCharEqualPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public IsCharEqualPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char c1 = 0;
        if (!args[0]!.charValue(out c1))
            return argError(interp, loc, InterpreterMessages.notAChar, 0, args[0]);
        Char c2 = 0;
        if (!args[1]!.charValue(out c2))
            return argError(interp, loc, InterpreterMessages.notAChar, 1, args[1]);
        return c1 == c2 ? interp.makeTrue() : interp.makeFalse();
    }
}

// Char-upcase primitive
public class CharUpcasePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public CharUpcasePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char c = 0;
        if (!args[0]!.charValue(out c))
            return argError(interp, loc, InterpreterMessages.notAChar, 0, args[0]);
        return interp.makeChar((Char)System.Char.ToUpper((char)c));
    }
}

// Char-downcase primitive
public class CharDowncasePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public CharDowncasePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char c = 0;
        if (!args[0]!.charValue(out c))
            return argError(interp, loc, InterpreterMessages.notAChar, 0, args[0]);
        return interp.makeChar((Char)System.Char.ToLower((char)c));
    }
}

// Make-vector primitive
public class MakeVectorPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    public MakeVectorPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long k = 0;
        if (!args[0]!.exactIntegerValue(out k))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 0, args[0]);
        if (k < 0)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        ELObj? fill = nArgs > 1 ? args[1] : interp.makeFalse();
        return new VectorObj((int)k, fill);
    }
}

// Vector-ref primitive
public class VectorRefPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public VectorRefPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        VectorObj? vec = args[0] as VectorObj;
        if (vec == null)
            return argError(interp, loc, InterpreterMessages.notAVector, 0, args[0]);
        long k = 0;
        if (!args[1]!.exactIntegerValue(out k))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
        if (k < 0 || k >= (long)vec.size())
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        return vec[(int)k];
    }
}

// Vector-set! primitive
public class VectorSetPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(3, 0, false);
    public VectorSetPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        VectorObj? vec = args[0] as VectorObj;
        if (vec == null)
            return argError(interp, loc, InterpreterMessages.notAVector, 0, args[0]);
        long k = 0;
        if (!args[1]!.exactIntegerValue(out k))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
        if (k < 0 || k >= (long)vec.size())
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        vec[(int)k] = args[2];
        return interp.makeUnspecified();
    }
}

// Vector-length primitive
public class VectorLengthPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public VectorLengthPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        VectorObj? vec = args[0] as VectorObj;
        if (vec == null)
            return argError(interp, loc, InterpreterMessages.notAVector, 0, args[0]);
        return interp.makeInteger((long)vec.size());
    }
}

// Error primitive
public class ErrorPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public ErrorPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.errorProc, new StringC(s!, n).ToString());
        return interp.makeError();
    }
}

// Debug primitive
public class DebugPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public DebugPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        // Debug output implementation
        return args[0];
    }
}

// Keyword->string primitive
public class KeywordToStringPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public KeywordToStringPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        KeywordObj? kw = args[0] as KeywordObj;
        if (kw == null)
            return argError(interp, loc, InterpreterMessages.notAKeyword, 0, args[0]);
        return interp.makeString(kw.name());
    }
}

// String->keyword primitive
public class StringToKeywordPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public StringToKeywordPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        return interp.makeKeyword(new StringC(s!, n));
    }
}

// String->symbol primitive
public class StringToSymbolPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public StringToSymbolPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        return interp.makeSymbol(new StringC(s!, n));
    }
}

// Format-number primitive
public class FormatNumberPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public FormatNumberPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long n;
        if (!args[0]!.exactIntegerValue(out n))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 0, args[0]);

        Char[]? s = null;
        nuint len = 0;
        if (!args[1]!.stringData(out s, out len))
            return argError(interp, loc, InterpreterMessages.notAString, 1, args[1]);

        StringC result = new StringC();
        formatNumber(n, s!, len, result);
        return interp.makeString(result);
    }

    private static void formatNumber(long n, Char[] s, nuint len, StringC result)
    {
        if (len > 0)
        {
            switch ((char)s[len - 1])
            {
                case 'a':
                    result.append(formatNumberLetter(n, "abcdefghijklmnopqrstuvwxyz"));
                    return;
                case 'A':
                    result.append(formatNumberLetter(n, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"));
                    return;
                case 'i':
                    result.append(formatNumberRoman(n, "mdclxvi"));
                    return;
                case 'I':
                    result.append(formatNumberRoman(n, "MDCLXVI"));
                    return;
                case '1':
                    result.append(formatNumberDecimal(n, len));
                    return;
            }
        }
        result.append(formatNumberDecimal(n, 1));
    }

    private static StringC formatNumberDecimal(long n, nuint minWidth)
    {
        string numStr = Math.Abs(n).ToString();
        while ((nuint)numStr.Length < minWidth)
            numStr = "0" + numStr;
        if (n < 0)
            numStr = "-" + numStr;
        StringC result = new StringC();
        result.append(numStr);
        return result;
    }

    private static StringC formatNumberLetter(long n, string letters)
    {
        var chars = new System.Collections.Generic.List<char>();
        int nLetters = letters.Length;
        bool neg = n < 0;
        if (neg) n = -n;
        if (n == 0)
            n = 1;
        while (n > 0)
        {
            n--;
            chars.Insert(0, letters[(int)(n % nLetters)]);
            n /= nLetters;
        }
        if (neg)
            chars.Insert(0, '-');
        StringC result = new StringC();
        result.append(new string(chars.ToArray()));
        return result;
    }

    private static StringC formatNumberRoman(long n, string letters)
    {
        StringC result = new StringC();
        if (n <= 0) return result;
        // letters: m,d,c,l,x,v,i (for lowercase) or M,D,C,L,X,V,I (for uppercase)
        int[] values = { 1000, 500, 100, 50, 10, 5, 1 };
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 7; i += 2)
        {
            int count = (int)(n / values[i]);
            n %= values[i];
            switch (count)
            {
                case 0: break;
                case 1: case 2: case 3:
                    for (int j = 0; j < count; j++)
                        sb.Append(letters[i]);
                    break;
                case 4:
                    sb.Append(letters[i]);
                    sb.Append(letters[i - 1]);
                    break;
                case 5: case 6: case 7: case 8:
                    sb.Append(letters[i - 1]);
                    for (int j = 5; j < count; j++)
                        sb.Append(letters[i]);
                    break;
                case 9:
                    sb.Append(letters[i]);
                    sb.Append(letters[i - 2]);
                    break;
            }
        }
        result.append(sb.ToString());
        return result;
    }
}

// Display-size primitive
public class DisplaySizePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, false);
    public DisplaySizePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        FOTBuilder.LengthSpec spec = new FOTBuilder.LengthSpec();
        spec.displaySizeFactor = 1.0;
        return new LengthSpecObj(spec);
    }
}

// Table-unit primitive
public class TableUnitPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public TableUnitPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long k;
        if (!args[0]!.exactIntegerValue(out k))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 0, args[0]);
        FOTBuilder.TableLengthSpec spec = new FOTBuilder.TableLengthSpec();
        spec.tableUnitFactor = (double)k;
        return new TableLengthSpecObj(spec);
    }
}

// TableLengthSpec object
public class TableLengthSpecObj : ELObj
{
    private FOTBuilder.TableLengthSpec spec_;
    public TableLengthSpecObj(FOTBuilder.TableLengthSpec spec) { spec_ = spec; }
    public FOTBuilder.TableLengthSpec spec() { return spec_; }
}
