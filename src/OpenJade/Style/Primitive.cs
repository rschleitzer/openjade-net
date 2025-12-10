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

// Length primitive - works on lists and strings
public class LengthPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public LengthPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        // Check if it's a string first
        StringObj? str = args[0] as StringObj;
        if (str != null)
        {
            // Return the string length (number of characters)
            return interp.makeInteger((long)str.size());
        }

        // Otherwise, treat as a list
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

// String=? primitive - string equality comparison
public class StringEqualPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public StringEqualPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s1 = null, s2 = null;
        nuint n1 = 0, n2 = 0;
        if (!args[0]!.stringData(out s1, out n1))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        if (!args[1]!.stringData(out s2, out n2))
            return argError(interp, loc, InterpreterMessages.notAString, 1, args[1]);
        if (n1 != n2)
            return interp.makeFalse();
        for (nuint i = 0; i < n1; i++)
        {
            if (s1![i] != s2![i])
                return interp.makeFalse();
        }
        return interp.makeTrue();
    }
}

// String<? primitive - string less-than comparison
public class StringLessPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public StringLessPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s1 = null, s2 = null;
        nuint n1 = 0, n2 = 0;
        if (!args[0]!.stringData(out s1, out n1))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        if (!args[1]!.stringData(out s2, out n2))
            return argError(interp, loc, InterpreterMessages.notAString, 1, args[1]);
        nuint minLen = n1 < n2 ? n1 : n2;
        for (nuint i = 0; i < minLen; i++)
        {
            if (s1![i] < s2![i])
                return interp.makeTrue();
            if (s1![i] > s2![i])
                return interp.makeFalse();
        }
        return n1 < n2 ? interp.makeTrue() : interp.makeFalse();
    }
}

// String<=? primitive - string less-than-or-equal comparison
public class StringLessEqualPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public StringLessEqualPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s1 = null, s2 = null;
        nuint n1 = 0, n2 = 0;
        if (!args[0]!.stringData(out s1, out n1))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        if (!args[1]!.stringData(out s2, out n2))
            return argError(interp, loc, InterpreterMessages.notAString, 1, args[1]);
        nuint minLen = n1 < n2 ? n1 : n2;
        for (nuint i = 0; i < minLen; i++)
        {
            if (s1![i] < s2![i])
                return interp.makeTrue();
            if (s1![i] > s2![i])
                return interp.makeFalse();
        }
        return n1 <= n2 ? interp.makeTrue() : interp.makeFalse();
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
        if (node == null || !node)
            return args[0];
        NodeListPtr nlp = new NodeListPtr();
        var result = node.children(ref nlp);
        if (result != AccessResult.accessOK)
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
            if (node == null || !node)
                return args[0];
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }
        NodePtr parent = new NodePtr();
        if (node.getParent(ref parent) != AccessResult.accessOK)
            return interp.makeEmptyNodeList();
        return new NodePtrNodeListObj(parent);
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
        if (node != null && node.getGi(ref str) == AccessResult.accessOK)
            return interp.makeString(str.data(), str.offset(), str.size());
        return interp.makeFalse();
    }
}

// General-name-normalize primitive - normalizes a general name (element/attribute name)
// according to the SGML declaration (e.g., case folding)
public class GeneralNameNormalizePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    public GeneralNameNormalizePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        // Get the string to normalize
        Char[]? s;
        nuint n;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);

        // Get the node for context (to get grove root and elements list)
        NodePtr? node = null;
        if (nArgs > 1)
        {
            if (!args[1]!.optSingletonNodeList(ctx, interp, ref node) || node == null || !node)
                return argError(interp, loc, InterpreterMessages.notASingletonNode, 1, args[1]);
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }

        // Copy the string to a mutable array
        Char[] result = new Char[n];
        for (nuint i = 0; i < n; i++)
            result[i] = s![i];

        // Get grove root and elements named node list for normalization
        NodePtr root = new NodePtr();
        if (node.node!.getGroveRoot(ref root) == AccessResult.accessOK)
        {
            NamedNodeListPtr elements = new NamedNodeListPtr();
            if (root.node!.getElements(ref elements) == AccessResult.accessOK && elements.list != null)
            {
                // Normalize using the elements list's normalize method
                nuint newSize = elements.list.normalize(result, n);
                return interp.makeString(result, 0, newSize);
            }
        }

        // If we can't get the grove/elements, just return the original string
        return interp.makeString(result, 0, n);
    }
}

// have-ancestor? primitive - checks if node has an ancestor with given GI
public class HaveAncestorPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    public HaveAncestorPrimitiveObj() : base(sig) { }

    // Helper: convert ELObj to normalized general name
    private bool convertGeneralName(ELObj? obj, NodePtr node, out Char[] result, out nuint size)
    {
        result = Array.Empty<Char>();
        size = 0;

        Char[]? s;
        nuint n;
        if (obj == null || !obj.stringData(out s, out n))
            return false;

        // Copy to result array
        result = new Char[n];
        for (nuint i = 0; i < n; i++)
            result[i] = s![i];

        // Normalize using grove elements
        NodePtr root = new NodePtr();
        if (node.node!.getGroveRoot(ref root) == AccessResult.accessOK)
        {
            NamedNodeListPtr elements = new NamedNodeListPtr();
            if (root.node!.getElements(ref elements) == AccessResult.accessOK && elements.list != null)
            {
                size = elements.list.normalize(result, n);
                return true;
            }
        }

        size = n;
        return true;
    }

    // Helper: recursive ancestor matching for list of GIs
    private bool matchAncestors(ELObj? obj, NodePtr node, out ELObj? unmatched)
    {
        unmatched = obj;

        NodePtr parent = new NodePtr();
        if (node.getParent(ref parent) != AccessResult.accessOK)
        {
            // No more ancestors, unmatched stays as is
            return true;
        }

        // Recurse first (to check from root down)
        if (!matchAncestors(obj, parent, out unmatched))
            return false;

        if (unmatched != null && !unmatched.isNil())
        {
            PairObj? pair = unmatched.asPair();
            if (pair == null)
                return false;

            Char[] gi;
            nuint giSize;
            if (!convertGeneralName(pair.car(), node, out gi, out giSize))
                return false;

            GroveString tem = new GroveString();
            if (parent.getGi(ref tem) == AccessResult.accessOK)
            {
                // Compare GIs
                bool match = (giSize == tem.size());
                if (match)
                {
                    for (nuint i = 0; i < giSize && match; i++)
                        match = (gi[i] == tem.data()![tem.offset() + i]);
                }
                if (match)
                    unmatched = pair.cdr();
            }
        }

        return true;
    }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (nArgs > 1)
        {
            if (!args[1]!.optSingletonNodeList(ctx, interp, ref node) || node == null || !node)
                return argError(interp, loc, InterpreterMessages.notASingletonNode, 1, args[1]);
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }

        // First try as a single GI string
        Char[] gi;
        nuint giSize;
        if (convertGeneralName(args[0], node, out gi, out giSize))
        {
            // Check ancestors one by one
            NodePtr parent = new NodePtr(node);
            while (parent.getParent(ref parent) == AccessResult.accessOK)
            {
                GroveString tem = new GroveString();
                if (parent.getGi(ref tem) == AccessResult.accessOK)
                {
                    // Compare GIs
                    if (giSize == tem.size())
                    {
                        bool match = true;
                        for (nuint i = 0; i < giSize && match; i++)
                            match = (gi[i] == tem.data()![tem.offset() + i]);
                        if (match)
                            return interp.makeTrue();
                    }
                }
            }
            return interp.makeFalse();
        }

        // If not a string, try as a list of GIs
        ELObj? unmatched;
        if (!matchAncestors(args[0], node, out unmatched))
            return argError(interp, loc, InterpreterMessages.notAList, 0, args[0]);

        if (unmatched != null && unmatched.isNil())
            return interp.makeTrue();
        else
            return interp.makeFalse();
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
        if (node != null && node.getId(ref str) == AccessResult.accessOK)
            return interp.makeString(str.data(), str.offset(), str.size());
        return interp.makeFalse();
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

// Page-number-sosofo primitive - returns a sosofo that outputs the current page number
public class PageNumberSosofoPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, false);
    public PageNumberSosofoPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return new PageNumberSosofoObj();
    }
}

// Current-node-page-number-sosofo primitive - returns a sosofo that outputs the page number for the current node
public class CurrentNodePageNumberSosofoPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, false);
    public CurrentNodePageNumberSosofoPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return new CurrentNodePageNumberSosofoObj(ctx.currentNode);
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

// String primitive (creates string from characters)
public class StringFromCharsPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);  // rest arg
    public StringFromCharsPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[] result = new Char[nArgs];
        for (int i = 0; i < nArgs; i++)
        {
            Char c;
            if (!args[i]!.charValue(out c))
                return argError(interp, loc, InterpreterMessages.notAChar, i, args[i]);
            result[i] = c;
        }
        return interp.makeString(result, (nuint)nArgs);
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

// Member primitive - uses equal? for comparison
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

// Memv primitive - uses eqv? for comparison
public class MemvPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public MemvPrimitiveObj() : base(sig) { }

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
            if (ELObj.eqv(args[0], pair.car()))
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

    // Made public so FormatNumberListPrimitiveObj can use it
    public static void formatNumberPublic(long n, Char[] s, nuint len, StringC result)
    {
        formatNumber(n, s, len, result);
    }
}

// Format-number-list primitive
public class FormatNumberListPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(3, 0, false);
    public FormatNumberListPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        ELObj? numbers = args[0];
        ELObj? formats = args[1];
        ELObj? seps = args[2];
        StringObj result = new StringObj();
        bool first = true;

        while (numbers != null && !numbers.isNil())
        {
            Char[]? s = null;
            nuint len = 0;

            // Add separator (except before first number)
            if (!first)
            {
                if (!seps!.stringData(out s, out len))
                {
                    PairObj? tem = seps.asPair();
                    if (tem == null)
                        return argError(interp, loc, InterpreterMessages.notAList, 2, args[2]);
                    if (!tem.car()!.stringData(out s, out len))
                        return argError(interp, loc, InterpreterMessages.notAString, 2, tem.car());
                    seps = tem.cdr();
                }
                if (s != null)
                    result.append(s, len);
            }
            first = false;

            // Get the number
            PairObj? numPair = numbers.asPair();
            if (numPair == null)
                return argError(interp, loc, InterpreterMessages.notAList, 0, args[0]);
            long k;
            if (!numPair.car()!.exactIntegerValue(out k))
                return argError(interp, loc, InterpreterMessages.notAnExactInteger, 0, numPair.car());
            numbers = numPair.cdr();

            // Get the format
            if (!formats!.stringData(out s, out len))
            {
                PairObj? fmtPair = formats.asPair();
                if (fmtPair == null)
                    return argError(interp, loc, InterpreterMessages.notAList, 1, args[1]);
                if (!fmtPair.car()!.stringData(out s, out len))
                    return argError(interp, loc, InterpreterMessages.notAString, 1, fmtPair.car());
                formats = fmtPair.cdr();
            }

            // Format the number
            StringC formatted = new StringC();
            FormatNumberPrimitiveObj.formatNumberPublic(k, s!, len, formatted);
            result.append(formatted.data()!, formatted.size());
        }
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

// <= primitive
public class LessEqualPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public LessEqualPrimitiveObj() : base(sig) { }

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
            if (!(v1 <= v2))
                return interp.makeFalse();
        }
        return interp.makeTrue();
    }
}

// >= primitive
public class GreaterEqualPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, true);
    public GreaterEqualPrimitiveObj() : base(sig) { }

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
            if (!(v1 >= v2))
                return interp.makeFalse();
        }
        return interp.makeTrue();
    }
}

// Descendants primitive
public class DescendantsPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public DescendantsPrimitiveObj() : base(sig) { }

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
        if (node == null || !node)
            return args[0];
        return new DescendantsNodeListObj(node);
    }
}

// DescendantsNodeListObj - traverses all descendants of a node
public class DescendantsNodeListObj : NodeListObj
{
    private NodePtr start_;
    private uint depth_;

    public DescendantsNodeListObj(NodePtr start, uint depth = 0)
    {
        start_ = start;
        depth_ = depth;
    }

    public override NodePtr? nodeListFirst(EvalContext ctx, Interpreter interp)
    {
        NodePtr node = new NodePtr(start_);
        uint depth = depth_;
        advance(ref node, ref depth);
        if (depth > 0)
            return node;
        return null;
    }

    public override NodeListObj? nodeListRest(EvalContext ctx, Interpreter interp)
    {
        NodePtr node = new NodePtr(start_);
        uint depth = depth_;
        advance(ref node, ref depth);
        if (depth > 0)
            return new DescendantsNodeListObj(node, depth);
        return interp.makeEmptyNodeList() as NodeListObj;
    }

    private static void advance(ref NodePtr node, ref uint depth)
    {
        if (node.assignFirstChild() == AccessResult.accessOK)
        {
            depth++;
            return;
        }
        while (depth > 0)
        {
            if (node.assignNextChunkSibling() == AccessResult.accessOK)
                return;
            if (node.assignParent() != AccessResult.accessOK)
                break;
            depth--;
        }
        depth = 0;
    }
}

// Follow primitive (following siblings)
public class FollowPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public FollowPrimitiveObj() : base(sig) { }

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
        if (node == null || !node)
            return args[0];
        NodePtr next = new NodePtr();
        if (node.nextChunkSibling(ref next) != AccessResult.accessOK)
            return interp.makeEmptyNodeList();
        return new SiblingNodeListObj(next, new NodePtr());
    }
}

// Preced primitive (preceding siblings)
public class PrecedPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public PrecedPrimitiveObj() : base(sig) { }

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
        if (node == null || !node)
            return args[0];
        // Get parent's first child, iterate until we reach this node
        NodePtr parent = new NodePtr();
        if (node.getParent(ref parent) != AccessResult.accessOK)
            return interp.makeEmptyNodeList();
        NodePtr first = new NodePtr();
        if (parent.firstChild(ref first) != AccessResult.accessOK)
            return interp.makeEmptyNodeList();
        if (first.Equals(node))
            return interp.makeEmptyNodeList();
        return new SiblingNodeListObj(first, node);
    }
}

// SiblingNodeListObj - node list of siblings between first and end (exclusive)
public class SiblingNodeListObj : NodeListObj
{
    private NodePtr first_;
    private NodePtr end_;

    public SiblingNodeListObj(NodePtr first, NodePtr end)
    {
        first_ = first;
        end_ = end;
    }

    public override NodePtr? nodeListFirst(EvalContext ctx, Interpreter interp)
    {
        if (end_ != null && !end_.isNull() && first_.Equals(end_))
            return null;
        return first_;
    }

    public override NodeListObj? nodeListRest(EvalContext ctx, Interpreter interp)
    {
        if (end_ != null && !end_.isNull() && first_.Equals(end_))
            return interp.makeEmptyNodeList() as NodeListObj;
        NodePtr next = new NodePtr();
        if (first_.nextChunkSibling(ref next) != AccessResult.accessOK)
            return interp.makeEmptyNodeList() as NodeListObj;
        return new SiblingNodeListObj(next, end_);
    }
}

// Data primitive - returns character data of a node list
// C++ implementation: upstream/openjade/style/primitive.cxx:4093
public class DataPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public DataPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        // C++ uses asNodeList and iterates over all nodes, NOT optSingletonNodeList
        NodeListObj? nl = args[0]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);

        StringObj result = new StringObj();
        for (;;)
        {
            NodePtr? nd = nl.nodeListFirst(ctx, interp);
            if (nd == null || !nd)
                break;
            bool chunk = false;
            nl = nl.nodeListChunkRest(ctx, interp, ref chunk);
            nodeData(nd, interp, chunk, result);
        }
        return result;
    }

    private static void nodeData(NodePtr node, Interpreter interp, bool chunk, StringObj result)
    {
        GroveString tem = new GroveString();
        if (node.charChunk(interp, ref tem) == AccessResult.accessOK)
        {
            if (chunk)
            {
                // Use the entire chunk
                if (tem.data() != null)
                    result.append(tem.data()!, tem.offset(), tem.size());
            }
            else
            {
                // Collect data recursively
                collectData(node, interp, result);
            }
        }
        else
        {
            collectData(node, interp, result);
        }
    }

    private static void collectData(NodePtr node, Grove.SdataMapper mapper, StringObj result)
    {
        GroveString chunk = new GroveString();
        if (node.charChunk(mapper, ref chunk) == AccessResult.accessOK)
        {
            if (chunk.data() != null)
                result.append(chunk.data()!, chunk.offset(), chunk.size());
            return;
        }
        NodePtr child = new NodePtr();
        if (node.firstChild(ref child) == AccessResult.accessOK)
        {
            do
            {
                collectData(child, mapper, result);
            } while (child.assignNextChunkSibling() == AccessResult.accessOK);
        }
    }
}

// Attributes primitive - returns attributes node list
public class AttributesPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public AttributesPrimitiveObj() : base(sig) { }

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
        if (node == null || !node)
            return args[0];
        NamedNodeListPtr atts = new NamedNodeListPtr();
        if (node.getAttributes(ref atts) != AccessResult.accessOK)
            return interp.makeEmptyNodeList();
        return new NamedNodeListPtrNodeListObj(atts);
    }
}

// AttributeString primitive
public class AttributeStringPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    private static bool debugAttributeString = false; // Set to true for debugging
    public AttributeStringPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        NodePtr? node = null;
        if (nArgs > 1)
        {
            if (!args[1]!.optSingletonNodeList(ctx, interp, ref node))
            {
                if (debugAttributeString)
                {
                    string attNameStr = new string(Array.ConvertAll(s!, c => (char)c), 0, (int)n);
                    if (attNameStr == "entity" || attNameStr == "link" || attNameStr == "type")
                        Console.Error.WriteLine($"AttributeString: '{attNameStr}' - optSingletonNodeList returned false for args[1] type {args[1]?.GetType().Name}");
                }
                return argError(interp, loc, InterpreterMessages.notAnOptSingletonNode, 1, args[1]);
            }
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }
        if (node == null || !node)
        {
            if (debugAttributeString)
            {
                string attNameStr = new string(Array.ConvertAll(s!, c => (char)c), 0, (int)n);
                if (attNameStr == "entity")
                    Console.Error.WriteLine($"AttributeString: 'entity' - node is null/empty");
            }
            return interp.makeFalse();
        }
        StringC attName = new StringC(s!, n);
        StringC value = new StringC();
        if (nodeAttributeString(node, attName, interp, value))
        {
            if (debugAttributeString)
            {
                string attNameStr = attName.ToString() ?? "";
                GroveString nodeGi = new GroveString();
                string giStr = "<no-gi>";
                if (node.getGi(ref nodeGi) == AccessResult.accessOK && nodeGi.data() != null)
                    giStr = new string(Array.ConvertAll(nodeGi.data()!, c => (char)c), (int)nodeGi.offset(), (int)nodeGi.size());
                if (attNameStr == "type" || (attNameStr == "link" && giStr.ToLower() == "return") || (attNameStr == "entity" && giStr.ToLower() == "class"))
                {
                    string valueStr = value.ToString() ?? "";
                    Console.Error.WriteLine($"AttributeString: '{attNameStr}' on <{giStr}> = '{valueStr}'");
                }
            }
            return interp.makeString(value);
        }
        if (debugAttributeString)
        {
            string attNameStr = attName.ToString() ?? "";
            GroveString nodeGi = new GroveString();
            string giStr = "<no-gi>";
            if (node.getGi(ref nodeGi) == AccessResult.accessOK && nodeGi.data() != null)
                giStr = new string(Array.ConvertAll(nodeGi.data()!, c => (char)c), (int)nodeGi.offset(), (int)nodeGi.size());
            if (attNameStr == "type" || (attNameStr == "link" && giStr.ToLower() == "return") || (attNameStr == "entity" && giStr.ToLower() == "class"))
            {
                Console.Error.WriteLine($"AttributeString: '{attNameStr}' on <{giStr}> = NOT FOUND");
            }
        }
        return interp.makeFalse();
    }

    internal static bool nodeAttributeString(NodePtr node, StringC attName, Grove.SdataMapper mapper, StringC value)
    {
        NamedNodeListPtr atts = new NamedNodeListPtr();
        if (node.getAttributes(ref atts) != AccessResult.accessOK)
            return false;
        NodePtr att = new NodePtr();
        if (atts.namedNode(new GroveString(attName.data(), attName.size()), ref att) != AccessResult.accessOK)
            return false;
        bool implied = false;
        if (att.getImplied(out implied) == AccessResult.accessOK && implied)
            return false;
        // Try tokens first
        GroveString tokens = new GroveString();
        if (att.tokens(ref tokens) == AccessResult.accessOK)
        {
            if (tokens.data() != null)
                value.assign(tokens.data()!, tokens.offset(), tokens.size());
            return true;
        }
        // Collect character content
        NodePtr child = new NodePtr();
        if (att.firstChild(ref child) == AccessResult.accessOK)
        {
            do
            {
                GroveString chunk = new GroveString();
                if (child.charChunk(mapper, ref chunk) == AccessResult.accessOK && chunk.data() != null)
                    value.append(chunk.data()!, chunk.offset(), chunk.size());
            } while (child.assignNextChunkSibling() == AccessResult.accessOK);
        }
        return true;
    }
}

// InheritedAttributeString primitive
public class InheritedAttributeStringPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    public InheritedAttributeStringPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        NodePtr? node = null;
        if (nArgs > 1)
        {
            if (!args[1]!.optSingletonNodeList(ctx, interp, ref node))
                return argError(interp, loc, InterpreterMessages.notAnOptSingletonNode, 1, args[1]);
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }
        if (node == null || !node)
            return interp.makeFalse();
        StringC attName = new StringC(s!, n);
        StringC value = new StringC();
        // Walk up ancestors looking for attribute
        NodePtr cur = new NodePtr(node);
        do
        {
            if (AttributeStringPrimitiveObj.nodeAttributeString(cur, attName, interp, value))
                return interp.makeString(value);
        } while (cur.assignParent() == AccessResult.accessOK);
        return interp.makeFalse();
    }
}

// Ancestor primitive
public class AncestorPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    public AncestorPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        NodePtr? node = null;
        if (nArgs > 1)
        {
            if (!args[1]!.optSingletonNodeList(ctx, interp, ref node) || node == null)
                return argError(interp, loc, InterpreterMessages.notAnOptSingletonNode, 1, args[1]);
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }
        StringC gi = new StringC(s!, n);
        // Normalize the GI using grove's element name normalization
        NodePtr cur = new NodePtr(node);
        while (cur.assignParent() == AccessResult.accessOK)
        {
            GroveString nodeGi = new GroveString();
            if (cur.getGi(ref nodeGi) == AccessResult.accessOK)
            {
                if (nodeGi.size() == gi.size())
                {
                    bool match = true;
                    for (nuint i = 0; i < gi.size() && match; i++)
                    {
                        if (nodeGi.data()![i] != gi.data()[i])
                            match = false;
                    }
                    if (match)
                        return new NodePtrNodeListObj(cur);
                }
            }
        }
        return interp.makeEmptyNodeList();
    }
}

// SelectElements primitive
public class SelectElementsPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public SelectElementsPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodeListObj? nl = args[0]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);

        // args[1] can be a string (GI) or a list of strings (GIs)
        System.Collections.Generic.List<StringC> gis = new System.Collections.Generic.List<StringC>();
        Char[]? s = null;
        nuint n = 0;
        if (args[1]!.stringData(out s, out n))
        {
            gis.Add(new StringC(s!, n));
        }
        else
        {
            ELObj? lst = args[1];
            while (lst != null && !lst.isNil())
            {
                PairObj? pair = lst.asPair();
                if (pair == null)
                    return argError(interp, loc, InterpreterMessages.notAList, 1, args[1]);
                if (!pair.car()!.stringData(out s, out n))
                    return argError(interp, loc, InterpreterMessages.notAString, 1, args[1]);
                gis.Add(new StringC(s!, n));
                lst = pair.cdr();
            }
        }
        return new SelectElementsNodeListObj(nl, gis);
    }
}

// SelectElementsNodeListObj - filters node list by element name(s)
public class SelectElementsNodeListObj : NodeListObj
{
    private NodeListObj nodeList_;
    private System.Collections.Generic.List<StringC> gis_;

    public SelectElementsNodeListObj(NodeListObj nl, System.Collections.Generic.List<StringC> gis)
    {
        nodeList_ = nl;
        gis_ = gis;
    }

    // C++ implementation: upstream/openjade/style/primitive.cxx:5625
    // IMPORTANT: nodeListFirst must be idempotent - calling it multiple times must return
    // the same result. We use a LOCAL variable to iterate, never modifying nodeList_.
    // This is critical because sosofos containing node lists may be processed multiple times
    // (e.g., for different page types in simple-page-sequence headers).
    public override NodePtr? nodeListFirst(EvalContext ctx, Interpreter interp)
    {
        NodeListObj nl = nodeList_;  // Start from original, never modify nodeList_
        for (;;)
        {
            NodePtr? node = nl.nodeListFirst(ctx, interp);
            if (node == null || !node)
                return null;
            if (matchesGi(node))
                return node;
            // Advance LOCAL nl to skip non-matching node
            bool chunk = false;
            nl = nl.nodeListChunkRest(ctx, interp, ref chunk);
        }
    }

    // C++ implementation: upstream/openjade/style/primitive.cxx:5641
    public override NodeListObj? nodeListRest(EvalContext ctx, Interpreter interp)
    {
        // Use local variable to iterate, don't modify nodeList_
        NodeListObj nl = nodeList_;
        // First, advance to the first matching node
        for (;;)
        {
            NodePtr? node = nl.nodeListFirst(ctx, interp);
            if (node == null || !node)
                break;
            if (matchesGi(node))
                break;
            // Advance LOCAL nl to skip non-matching node
            bool chunk = false;
            nl = nl.nodeListChunkRest(ctx, interp, ref chunk);
        }
        // Now get the rest after the current (matching) node
        bool restChunk = false;
        NodeListObj rest = nl.nodeListChunkRest(ctx, interp, ref restChunk);
        return new SelectElementsNodeListObj(rest, gis_);
    }

    private bool matchesGi(NodePtr node)
    {
        GroveString nodeGi = new GroveString();
        if (node.getGi(ref nodeGi) != AccessResult.accessOK)
            return false;
        foreach (StringC gi in gis_)
        {
            if (nodeGi.size() == gi.size())
            {
                bool match = true;
                for (nuint i = 0; i < gi.size() && match; i++)
                {
                    // Case-insensitive comparison (SGML GIs are case-insensitive)
                    // Use indexer which properly handles offset
                    Char c1 = nodeGi[i];
                    Char c2 = gi.data()[i];
                    // Convert both to uppercase for comparison
                    if (c1 >= 'a' && c1 <= 'z') c1 -= 32;
                    if (c2 >= 'a' && c2 <= 'z') c2 -= 32;
                    if (c1 != c2)
                        match = false;
                }
                if (match)
                    return true;
            }
        }
        return false;
    }

}

// NodeListLength primitive
public class NodeListLengthPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public NodeListLengthPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodeListObj? nl = args[0]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);
        long count = 0;
        while (nl != null && nl.nodeListFirst(ctx, interp) != null)
        {
            count++;
            nl = nl.nodeListRest(ctx, interp);
        }
        return interp.makeInteger(count);
    }
}

// NodeListRef primitive
public class NodeListRefPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public NodeListRefPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodeListObj? nl = args[0]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);
        long k = 0;
        if (!args[1]!.exactIntegerValue(out k))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 1, args[1]);
        if (k < 0)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        for (long i = 0; i < k && nl != null; i++)
        {
            var first = nl.nodeListFirst(ctx, interp);
            if (first == null)
            {
                interp.setNextLocation(loc);
                interp.message(InterpreterMessages.outOfRange);
                return interp.makeError();
            }
            nl = nl.nodeListRest(ctx, interp);
        }
        NodePtr? node = nl?.nodeListFirst(ctx, interp);
        if (node == null || !node)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.outOfRange);
            return interp.makeError();
        }
        return new NodePtrNodeListObj(node);
    }
}

// NodeListReverse primitive
public class NodeListReversePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public NodeListReversePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodeListObj? nl = args[0]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);
        // Collect all nodes
        var nodes = new System.Collections.Generic.List<NodePtr>();
        while (nl != null)
        {
            NodePtr? node = nl.nodeListFirst(ctx, interp);
            if (node == null || !node)
                break;
            nodes.Add(node);
            nl = nl.nodeListRest(ctx, interp);
        }
        // Build reversed list
        NodeListObj? result = interp.makeEmptyNodeList() as NodeListObj;
        for (int i = 0; i < nodes.Count; i++)
        {
            result = new PairNodeListObj(new NodePtrNodeListObj(nodes[i]), result!);
        }
        return result;
    }
}

// ProcessNodeList primitive
public class ProcessNodeListPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public ProcessNodeListPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodeListObj? nl = args[0]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);
        if (ctx.processingMode == null)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.noCurrentProcessingMode);
            return interp.makeError();
        }
        return new ProcessNodeListSosofoObj(nl, ctx.processingMode);
    }
}

// ElementWithId primitive
public class ElementWithIdPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    private static bool debugElementWithId = false; // Set to true for debugging
    public ElementWithIdPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        NodePtr? node = null;
        if (nArgs > 1)
        {
            if (!args[1]!.optSingletonNodeList(ctx, interp, ref node))
                return argError(interp, loc, InterpreterMessages.notAnOptSingletonNode, 1, args[1]);
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }
        if (node == null || !node)
            return interp.makeEmptyNodeList();
        // Get grove root
        NodePtr root = new NodePtr();
        if (node.getGroveRoot(ref root) != AccessResult.accessOK)
            return interp.makeEmptyNodeList();
        // Get element by ID
        StringC id = new StringC(s!, n);
        NodePtr elem = new NodePtr();
        if (root.getElementWithId(new GroveString(id.data(), id.size()), ref elem) != AccessResult.accessOK)
        {
            if (debugElementWithId)
                Console.Error.WriteLine($"ElementWithId: NOT FOUND id='{id}'");
            return interp.makeEmptyNodeList();
        }
        if (debugElementWithId)
            Console.Error.WriteLine($"ElementWithId: FOUND id='{id}'");
        return new NodePtrNodeListObj(elem);
    }
}

// First-sibling? primitive
public class IsFirstSiblingPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 1, false);
    public IsFirstSiblingPrimitiveObj() : base(sig) { }

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
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }
        if (node == null || !node)
            return interp.makeFalse();
        // Check if there's a previous sibling with same GI
        GroveString myGi = new GroveString();
        if (node.getGi(ref myGi) != AccessResult.accessOK)
            return interp.makeFalse();
        NodePtr prev = new NodePtr(node);
        while (prev.assignPreviousSibling() == AccessResult.accessOK)
        {
            GroveString prevGi = new GroveString();
            if (prev.getGi(ref prevGi) == AccessResult.accessOK)
            {
                if (myGi.size() == prevGi.size())
                {
                    bool same = true;
                    for (nuint i = 0; i < myGi.size() && same; i++)
                    {
                        if (myGi.data()![i] != prevGi.data()![i])
                            same = false;
                    }
                    if (same)
                        return interp.makeFalse();
                }
            }
        }
        return interp.makeTrue();
    }
}

// Last-sibling? primitive
public class IsLastSiblingPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 1, false);
    public IsLastSiblingPrimitiveObj() : base(sig) { }

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
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }
        if (node == null || !node)
            return interp.makeFalse();
        // Check if there's a next sibling with same GI
        GroveString myGi = new GroveString();
        if (node.getGi(ref myGi) != AccessResult.accessOK)
            return interp.makeFalse();
        NodePtr next = new NodePtr(node);
        while (next.assignNextChunkSibling() == AccessResult.accessOK)
        {
            GroveString nextGi = new GroveString();
            if (next.getGi(ref nextGi) == AccessResult.accessOK)
            {
                if (myGi.size() == nextGi.size())
                {
                    bool same = true;
                    for (nuint i = 0; i < myGi.size() && same; i++)
                    {
                        if (myGi.data()![i] != nextGi.data()![i])
                            same = false;
                    }
                    if (same)
                        return interp.makeFalse();
                }
            }
        }
        return interp.makeTrue();
    }
}

// Absolute-first-sibling? primitive
// Returns #t if there are no element siblings before this node
// C++ implementation: upstream/openjade/style/primitive.cxx:3119
public class IsAbsoluteFirstSiblingPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 1, false);
    public IsAbsoluteFirstSiblingPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (nArgs > 0)
        {
            if (!args[0]!.optSingletonNodeList(ctx, interp, ref node) || node == null || !node)
                return argError(interp, loc, InterpreterMessages.notASingletonNode, 0, args[0]);
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }

        // Get first sibling
        NodePtr p = new NodePtr();
        if (node.node!.firstSibling(ref p) != AccessResult.accessOK)
            return interp.makeFalse();

        // Iterate through siblings until we reach the current node
        // Use Equals to compare node identity (not object reference)
        while (!p.node!.Equals(node.node!))
        {
            GroveString tem = new GroveString();
            // If any sibling before us has a GI (is an element), return false
            if (p.getGi(ref tem) == AccessResult.accessOK)
                return interp.makeFalse();
            if (p.assignNextChunkSibling() != AccessResult.accessOK)
                break; // Should not happen
        }
        return interp.makeTrue();
    }
}

// Absolute-last-sibling? primitive
// Returns #t if there are no element siblings after this node
// C++ implementation: upstream/openjade/style/primitive.cxx:3159
public class IsAbsoluteLastSiblingPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 1, false);
    public IsAbsoluteLastSiblingPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (nArgs > 0)
        {
            if (!args[0]!.optSingletonNodeList(ctx, interp, ref node) || node == null || !node)
                return argError(interp, loc, InterpreterMessages.notASingletonNode, 0, args[0]);
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }
        // Iterate through siblings after the current node
        NodePtr p = new NodePtr(node);
        while (p.assignNextChunkSibling() == AccessResult.accessOK)
        {
            GroveString tem = new GroveString();
            // If any sibling after us has a GI (is an element), return false
            if (p.getGi(ref tem) == AccessResult.accessOK)
                return interp.makeFalse();
        }
        return interp.makeTrue();
    }
}

// ChildNumber primitive
public class ChildNumberPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 1, false);
    public ChildNumberPrimitiveObj() : base(sig) { }

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
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }
        if (node == null || !node)
            return interp.makeFalse();
        // Count preceding siblings with same GI
        GroveString myGi = new GroveString();
        if (node.getGi(ref myGi) != AccessResult.accessOK)
            return interp.makeFalse();
        long count = 1;
        NodePtr prev = new NodePtr(node);
        while (prev.assignPreviousSibling() == AccessResult.accessOK)
        {
            GroveString prevGi = new GroveString();
            if (prev.getGi(ref prevGi) == AccessResult.accessOK)
            {
                if (myGi.size() == prevGi.size())
                {
                    bool same = true;
                    for (nuint i = 0; i < myGi.size() && same; i++)
                    {
                        if (myGi.data()![i] != prevGi.data()![i])
                            same = false;
                    }
                    if (same)
                        count++;
                }
            }
        }
        return interp.makeInteger(count);
    }
}

// ElementNumber primitive - counts elements with same GI before current node in document order
// C++ implementation: upstream/openjade/style/NumberCache.cxx:94
public class ElementNumberPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 1, false);
    public ElementNumberPrimitiveObj() : base(sig) { }

    // Helper to compare GroveStrings accounting for offset
    private static bool groveStringEquals(GroveString a, GroveString b)
    {
        if (a.size() != b.size())
            return false;
        if (a.data() == null || b.data() == null)
            return a.data() == b.data();
        for (nuint i = 0; i < a.size(); i++)
        {
            if (a.data()![a.offset() + i] != b.data()![b.offset() + i])
                return false;
        }
        return true;
    }

    // Document-order traversal that doesn't rely on chunk linkage.
    // Traverses the element tree: first children, then siblings, then up to parent.
    private static bool advanceDocumentOrder(ref NodePtr ptr)
    {
        // Try first child
        NodePtr child = new NodePtr();
        if (ptr.assignFirstChild() == AccessResult.accessOK)
            return true;
        // No child, try next sibling
        while (true)
        {
            NodePtr next = new NodePtr();
            if (ptr.node!.nextSibling(ref next) == AccessResult.accessOK)
            {
                ptr.assign(next.node);
                return true;
            }
            // No sibling, go up to parent
            NodePtr parent = new NodePtr();
            if (ptr.getParent(ref parent) != AccessResult.accessOK)
                return false; // Reached root
            ptr.assign(parent.node);
        }
    }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (nArgs > 0)
        {
            if (!args[0]!.optSingletonNodeList(ctx, interp, ref node) || node == null || !node)
                return argError(interp, loc, InterpreterMessages.notASingletonNode, 0, args[0]);
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }
        // Get the GI of the node
        GroveString gi = new GroveString();
        if (node.getGi(ref gi) != AccessResult.accessOK)
            return interp.makeFalse();

        // Get the document element to start traversal
        NodePtr start = new NodePtr();
        if (node.getGroveRoot(ref start) != AccessResult.accessOK)
            return interp.makeFalse();
        if (start.getDocumentElement(ref start) != AccessResult.accessOK)
            return interp.makeFalse();
        // Traverse document in order, counting elements with same GI
        ulong count = 0;

        while (start)
        {
            GroveString temGi = new GroveString();
            if (start.getGi(ref temGi) == AccessResult.accessOK)
            {
                if (groveStringEquals(temGi, gi))
                    count++;
            }
            // Check if we've reached the target node
            if (start.node!.Equals(node.node!))
                break;
            // Advance to next element in document order (element tree traversal)
            if (!advanceDocumentOrder(ref start))
                break;
        }
        return interp.makeInteger((long)count);
    }
}

// AllElementNumber primitive - returns the index of the element in document order
public class AllElementNumberPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 1, false);
    public AllElementNumberPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (nArgs > 0)
        {
            if (!args[0]!.optSingletonNodeList(ctx, interp, ref node) || node == null || !node)
                return argError(interp, loc, InterpreterMessages.notASingletonNode, 0, args[0]);
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }
        // Use elementIndex to get the 0-based index and return 1-based
        ulong index = 0;
        if (node.elementIndex(ref index) == AccessResult.accessOK)
            return interp.makeInteger((long)(index + 1));
        return interp.makeFalse();
    }
}

// IsColor? primitive
public class IsColorPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsColorPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (args[0]?.asColor() != null)
            return interp.makeTrue();
        return interp.makeFalse();
    }
}

// IsColorSpace? primitive
public class IsColorSpacePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsColorSpacePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (args[0]?.asColorSpace() != null)
            return interp.makeTrue();
        return interp.makeFalse();
    }
}

// Device RGB color space
public class DeviceRGBColorSpaceObj : ColorSpaceObj
{
    public ELObj? makeColor(int nArgs, ELObj?[] args, Interpreter interp, Location loc)
    {
        if (nArgs != 3)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.colorArgCount);
            return interp.makeError();
        }
        double[] components = new double[3];
        for (int i = 0; i < 3; i++)
        {
            if (!args[i]!.realValue(out components[i]))
            {
                interp.setNextLocation(loc);
                interp.message(InterpreterMessages.notANumber);
                return interp.makeError();
            }
        }
        return new DeviceRGBColorObj(components);
    }
}

// Device RGB color
public class DeviceRGBColorObj : ColorObj
{
    private double r_, g_, b_;

    public DeviceRGBColorObj(double[] rgb)
    {
        r_ = rgb[0];
        g_ = rgb[1];
        b_ = rgb[2];
    }

    public override void set(FOTBuilder fotb)
    {
        FOTBuilder.DeviceRGBColor c = new FOTBuilder.DeviceRGBColor();
        c.red = (byte)(r_ * 255);
        c.green = (byte)(g_ * 255);
        c.blue = (byte)(b_ * 255);
        fotb.setColor(c);
    }

    public override void setBackground(FOTBuilder fotb)
    {
        FOTBuilder.DeviceRGBColor c = new FOTBuilder.DeviceRGBColor();
        c.red = (byte)(r_ * 255);
        c.green = (byte)(g_ * 255);
        c.blue = (byte)(b_ * 255);
        fotb.setBackgroundColor(c);
    }
}

// Device Gray color space
public class DeviceGrayColorSpaceObj : ColorSpaceObj
{
    public ELObj? makeColor(int nArgs, ELObj?[] args, Interpreter interp, Location loc)
    {
        if (nArgs != 1)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.colorArgCount);
            return interp.makeError();
        }
        double gray = 0;
        if (!args[0]!.realValue(out gray))
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.notANumber);
            return interp.makeError();
        }
        return new DeviceGrayColorObj(gray);
    }
}

// Device Gray color
public class DeviceGrayColorObj : ColorObj
{
    private double gray_;

    public DeviceGrayColorObj(double gray)
    {
        gray_ = gray;
    }

    public override void set(FOTBuilder fotb)
    {
        byte g = (byte)(gray_ * 255);
        FOTBuilder.DeviceRGBColor c = new FOTBuilder.DeviceRGBColor();
        c.red = g;
        c.green = g;
        c.blue = g;
        fotb.setColor(c);
    }

    public override void setBackground(FOTBuilder fotb)
    {
        byte g = (byte)(gray_ * 255);
        FOTBuilder.DeviceRGBColor c = new FOTBuilder.DeviceRGBColor();
        c.red = g;
        c.green = g;
        c.blue = g;
        fotb.setBackgroundColor(c);
    }
}

// Device CMYK color space
public class DeviceCMYKColorSpaceObj : ColorSpaceObj
{
    public ELObj? makeColor(int nArgs, ELObj?[] args, Interpreter interp, Location loc)
    {
        if (nArgs != 4)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.colorArgCount);
            return interp.makeError();
        }
        double[] components = new double[4];
        for (int i = 0; i < 4; i++)
        {
            if (!args[i]!.realValue(out components[i]))
            {
                interp.setNextLocation(loc);
                interp.message(InterpreterMessages.notANumber);
                return interp.makeError();
            }
        }
        return new DeviceCMYKColorObj(components);
    }
}

// Device CMYK color
public class DeviceCMYKColorObj : ColorObj
{
    private double c_, m_, y_, k_;

    public DeviceCMYKColorObj(double[] cmyk)
    {
        c_ = cmyk[0];
        m_ = cmyk[1];
        y_ = cmyk[2];
        k_ = cmyk[3];
    }

    public override void set(FOTBuilder fotb)
    {
        // Convert CMYK to RGB for FOTBuilder
        FOTBuilder.DeviceRGBColor c = new FOTBuilder.DeviceRGBColor();
        c.red = (byte)((1 - c_) * (1 - k_) * 255);
        c.green = (byte)((1 - m_) * (1 - k_) * 255);
        c.blue = (byte)((1 - y_) * (1 - k_) * 255);
        fotb.setColor(c);
    }

    public override void setBackground(FOTBuilder fotb)
    {
        FOTBuilder.DeviceRGBColor c = new FOTBuilder.DeviceRGBColor();
        c.red = (byte)((1 - c_) * (1 - k_) * 255);
        c.green = (byte)((1 - m_) * (1 - k_) * 255);
        c.blue = (byte)((1 - y_) * (1 - k_) * 255);
        fotb.setBackgroundColor(c);
    }
}

// ColorSpace primitive
public class ColorSpacePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, true);
    public ColorSpacePrimitiveObj() : base(sig) { }

    private const string DevicePrefix = "ISO/IEC 10179:1996//Color-Space Family::Device ";

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        StringC str = new StringC(s!, n);
        string strStr = str.ToString();

        if (strStr.StartsWith(DevicePrefix))
        {
            string type = strStr.Substring(DevicePrefix.Length);
            switch (type)
            {
                case "RGB":
                    return new DeviceRGBColorSpaceObj();
                case "Gray":
                    return new DeviceGrayColorSpaceObj();
                case "CMYK":
                    return new DeviceCMYKColorSpaceObj();
                default:
                    interp.setNextLocation(loc);
                    interp.message(InterpreterMessages.unknownColorSpaceFamily, strStr);
                    return interp.makeError();
            }
        }
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.unknownColorSpaceFamily, strStr);
        return interp.makeError();
    }
}

// Color primitive
public class ColorPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, true);
    public ColorPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        ColorSpaceObj? cs = args[0]?.asColorSpace();
        if (cs == null)
            return argError(interp, loc, InterpreterMessages.notAColorSpace, 0, args[0]);
        // Call makeColor on the color space with remaining arguments
        ELObj?[] colorArgs = new ELObj?[nArgs - 1];
        for (int i = 1; i < nArgs; i++)
            colorArgs[i - 1] = args[i];
        // Type-specific makeColor call
        if (cs is DeviceRGBColorSpaceObj rgb)
            return rgb.makeColor(nArgs - 1, colorArgs, interp, loc);
        if (cs is DeviceGrayColorSpaceObj gray)
            return gray.makeColor(nArgs - 1, colorArgs, interp, loc);
        if (cs is DeviceCMYKColorSpaceObj cmyk)
            return cmyk.makeColor(nArgs - 1, colorArgs, interp, loc);
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.unknownColorSpaceFamily, "unknown");
        return interp.makeError();
    }
}

// IsAddress? primitive
public class IsAddressPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsAddressPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (args[0]?.asAddress() != null)
            return interp.makeTrue();
        return interp.makeFalse();
    }
}

// CurrentNodeAddress primitive
public class CurrentNodeAddressPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(0, 0, false);
    public CurrentNodeAddressPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        if (ctx.currentNode == null)
            return noCurrentNodeError(interp, loc);
        FOTBuilder.Address addr = new FOTBuilder.Address();
        addr.type = FOTBuilder.Address.Type.resolvedNode;
        addr.node = ctx.currentNode;
        return new AddressObj(addr);
    }
}

// IdrefAddress primitive
public class IdrefAddressPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IdrefAddressPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        if (ctx.currentNode == null)
            return noCurrentNodeError(interp, loc);
        FOTBuilder.Address addr = new FOTBuilder.Address();
        addr.type = FOTBuilder.Address.Type.idref;
        addr.node = ctx.currentNode;
        addr.@params[0] = new StringC(s!, n);
        return new AddressObj(addr);
    }
}

// EntityAddress primitive
public class EntityAddressPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public EntityAddressPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        if (ctx.currentNode == null)
            return noCurrentNodeError(interp, loc);
        FOTBuilder.Address addr = new FOTBuilder.Address();
        addr.type = FOTBuilder.Address.Type.entity;
        addr.node = ctx.currentNode;
        addr.@params[0] = new StringC(s!, n);
        return new AddressObj(addr);
    }
}

// NodeListAddress primitive
public class NodeListAddressPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public NodeListAddressPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (!args[0]!.optSingletonNodeList(ctx, interp, ref node) || node == null)
            return argError(interp, loc, InterpreterMessages.notAnOptSingletonNode, 0, args[0]);
        FOTBuilder.Address addr = new FOTBuilder.Address();
        addr.type = FOTBuilder.Address.Type.resolvedNode;
        addr.node = node;
        return new AddressObj(addr);
    }
}

// IsQuantity? primitive
public class IsQuantityPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsQuantityPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long lval = 0;
        double dval = 0;
        int dim = 0;
        if (args[0]!.quantityValue(out lval, out dval, out dim) != ELObj.QuantityType.noQuantity)
            return interp.makeTrue();
        return interp.makeFalse();
    }
}

// IsOdd? primitive
public class IsOddPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsOddPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long n = 0;
        if (!args[0]!.exactIntegerValue(out n))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 0, args[0]);
        if ((n % 2) != 0)
            return interp.makeTrue();
        return interp.makeFalse();
    }
}

// IsEven? primitive
public class IsEvenPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsEvenPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long n = 0;
        if (!args[0]!.exactIntegerValue(out n))
            return argError(interp, loc, InterpreterMessages.notAnExactInteger, 0, args[0]);
        if ((n % 2) == 0)
            return interp.makeTrue();
        return interp.makeFalse();
    }
}

// IsZero? primitive
public class IsZeroPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsZeroPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long lval = 0;
        double dval = 0;
        int dim = 0;
        var qt = args[0]!.quantityValue(out lval, out dval, out dim);
        if (qt == ELObj.QuantityType.noQuantity)
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        double v = (qt == ELObj.QuantityType.doubleQuantity) ? dval : lval;
        if (v == 0)
            return interp.makeTrue();
        return interp.makeFalse();
    }
}

// IsPositive? primitive
public class IsPositivePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsPositivePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long lval = 0;
        double dval = 0;
        int dim = 0;
        var qt = args[0]!.quantityValue(out lval, out dval, out dim);
        if (qt == ELObj.QuantityType.noQuantity)
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        double v = (qt == ELObj.QuantityType.doubleQuantity) ? dval : lval;
        if (v > 0)
            return interp.makeTrue();
        return interp.makeFalse();
    }
}

// IsNegative? primitive
public class IsNegativePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsNegativePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        long lval = 0;
        double dval = 0;
        int dim = 0;
        var qt = args[0]!.quantityValue(out lval, out dval, out dim);
        if (qt == ELObj.QuantityType.noQuantity)
            return argError(interp, loc, InterpreterMessages.notANumber, 0, args[0]);
        double v = (qt == ELObj.QuantityType.doubleQuantity) ? dval : lval;
        if (v < 0)
            return interp.makeTrue();
        return interp.makeFalse();
    }
}

// StringToList primitive
public class StringToListPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public StringToListPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        Char[]? s = null;
        nuint n = 0;
        if (!args[0]!.stringData(out s, out n))
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        ELObj? result = interp.makeNil();
        for (int i = (int)n - 1; i >= 0; i--)
            result = interp.makePair(interp.makeChar(s![i]), result);
        return result;
    }
}

// ListToString primitive
public class ListToStringPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public ListToStringPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        var chars = new System.Collections.Generic.List<Char>();
        ELObj? lst = args[0];
        while (lst != null && !lst.isNil())
        {
            PairObj? pair = lst.asPair();
            if (pair == null)
                return argError(interp, loc, InterpreterMessages.notAList, 0, args[0]);
            Char c = 0;
            if (!pair.car()!.charValue(out c))
                return argError(interp, loc, InterpreterMessages.notAChar, 0, args[0]);
            chars.Add(c);
            lst = pair.cdr();
        }
        return interp.makeString(chars.ToArray(), (nuint)chars.Count);
    }
}

// VectorToList primitive
public class VectorToListPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public VectorToListPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        VectorObj? vec = args[0]?.asVector();
        if (vec == null)
            return argError(interp, loc, InterpreterMessages.notAVector, 0, args[0]);
        ELObj? result = interp.makeNil();
        for (int i = (int)vec.size() - 1; i >= 0; i--)
            result = interp.makePair(vec[i], result);
        return result;
    }
}

// ListToVector primitive
public class ListToVectorPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public ListToVectorPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        var elements = new System.Collections.Generic.List<ELObj?>();
        ELObj? lst = args[0];
        while (lst != null && !lst.isNil())
        {
            PairObj? pair = lst.asPair();
            if (pair == null)
                return argError(interp, loc, InterpreterMessages.notAList, 0, args[0]);
            elements.Add(pair.car());
            lst = pair.cdr();
        }
        return interp.makeVector(elements);
    }
}

// Memq primitive (compare with eq?)
public class MemqPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public MemqPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        ELObj? key = args[0];
        ELObj? lst = args[1];
        while (lst != null && !lst.isNil())
        {
            PairObj? pair = lst.asPair();
            if (pair == null)
                return argError(interp, loc, InterpreterMessages.notAList, 1, args[1]);
            if (pair.car() == key)
                return lst;
            lst = pair.cdr();
        }
        return interp.makeFalse();
    }
}

// Assq primitive (compare keys with eq?)
public class AssqPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public AssqPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        ELObj? key = args[0];
        ELObj? lst = args[1];
        while (lst != null && !lst.isNil())
        {
            PairObj? pair = lst.asPair();
            if (pair == null)
                return argError(interp, loc, InterpreterMessages.notAList, 1, args[1]);
            PairObj? entry = pair.car()?.asPair();
            if (entry != null && entry.car() == key)
                return entry;
            lst = pair.cdr();
        }
        return interp.makeFalse();
    }
}

// Node-list=? primitive - tests if two node lists contain the same nodes in the same order
// C++ implementation: upstream/openjade/style/primitive.cxx:3882
public class NodeListEqualPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public NodeListEqualPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodeListObj? nl1 = args[0]?.asNodeList();
        if (nl1 == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);

        // Same object - equal
        if (nl1 == args[1])
            return interp.makeTrue();

        NodeListObj? nl2 = args[1]?.asNodeList();
        if (nl2 == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 1, args[1]);

        // Iterate through both lists comparing nodes
        while (true)
        {
            NodePtr? nd1 = nl1.nodeListFirst(ctx, interp);
            NodePtr? nd2 = nl2.nodeListFirst(ctx, interp);

            if (nd1 == null || !nd1)
            {
                // nl1 exhausted - equal if nl2 is also exhausted
                if (nd2 == null || !nd2)
                    return interp.makeTrue();
                else
                    return interp.makeFalse();
            }
            else if (nd2 == null || !nd2)
            {
                // nl2 exhausted but nl1 isn't
                return interp.makeFalse();
            }
            else if (nd1.node == null || nd2.node == null || !nd1.node.Equals(nd2.node))
            {
                // Nodes differ
                return interp.makeFalse();
            }

            // Move to rest
            nl1 = nl1.nodeListRest(ctx, interp);
            nl2 = nl2.nodeListRest(ctx, interp);
        }
    }
}

// Node-list->list primitive - converts a node list to a Scheme list iteratively
public class NodeListToListPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public NodeListToListPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodeListObj? nl = args[0]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 0, args[0]);

        // Build list - collect all nodes first to avoid recursion issues
        var items = new System.Collections.Generic.List<NodePtr>();

        // Iteratively collect all nodes
        while (true)
        {
            NodePtr? nd = nl.nodeListFirst(ctx, interp);
            if (nd == null)
                break;
            items.Add(nd);
            nl = nl.nodeListRest(ctx, interp);
        }

        // Build the list in proper order (reverse)
        ELObj? result = interp.makeNil();
        for (int i = items.Count - 1; i >= 0; i--)
        {
            ELObj singleton = new NodePtrNodeListObj(items[i]);
            result = interp.makePair(singleton, result);
        }

        return result;
    }
}

// Node-list-map primitive - maps a function over a node list, returning a new node list
// C++ implementation: upstream/openjade/style/primitive.cxx
public class NodeListMapPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public NodeListMapPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        FunctionObj? func = args[0]?.asFunction();
        if (func == null)
        {
            interp.setNextLocation(loc);
            return interp.makeError();
        }

        // Check that function can accept 1 argument
        if (func.nRequiredArgs() > 1)
        {
            interp.setNextLocation(loc);
            return interp.makeError();
        }
        if (func.nRequiredArgs() + func.nOptionalArgs() + (func.restArg() ? 1 : 0) == 0)
        {
            interp.setNextLocation(loc);
            return interp.makeError();
        }

        interp.makeReadOnly(func);

        NodeListObj? nl = args[1]?.asNodeList();
        if (nl == null)
            return argError(interp, loc, InterpreterMessages.notANodeList, 1, args[1]);

        return new MapNodeListObj(func, nl, new MapNodeListObj.Context(ctx, loc));
    }
}

// NodeProperty primitive - accesses grove node properties
// See upstream/openjade/style/primitive.cxx:4182
public class NodePropertyPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, true);  // 2 required, rest for keywords
    public NodePropertyPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        // First arg is property name (string or symbol)
        StringObj? str = args[0]?.convertToString();
        if (str == null)
            return argError(interp, loc, InterpreterMessages.notAStringOrSymbol, 0, args[0]);
        Char[]? s = null;
        nuint n = 0;
        str.stringData(out s, out n);
        StringC propName = new StringC(s!, n);

        // Second arg is node
        NodePtr? node = null;
        if (!args[1]!.optSingletonNodeList(ctx, interp, ref node) || node == null || !node)
            return argError(interp, loc, InterpreterMessages.notASingletonNode, 1, args[1]);

        // Parse keyword arguments: default:, null:
        ELObj? defaultValue = null;
        ELObj? nullValue = null;
        for (int i = 2; i < nArgs; i += 2)
        {
            if (i + 1 >= nArgs) break;
            KeywordObj? key = args[i]?.asKeyword();
            if (key != null)
            {
                string keyName = key.identifier()?.name().ToString() ?? "";
                if (keyName == "default")
                    defaultValue = args[i + 1];
                else if (keyName == "null")
                    nullValue = args[i + 1];
            }
        }

        // Look up the property name
        ComponentName.Id id;
        if (!interp.lookupNodeProperty(propName, out id) || id == ComponentName.Id.noId)
        {
            if (defaultValue != null)
                return defaultValue;
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.noNodePropertyValue, propName.ToString() ?? "");
            return interp.makeError();
        }

        // Get the property value from the node
        ELObjPropertyValue value = new ELObjPropertyValue(interp, false);
        Grove.SdataMapper mapper = new Grove.SdataMapper();
        AccessResult ret = node.node!.property(id, mapper, value);
        if (ret == AccessResult.accessOK && value.obj != null)
            return value.obj;
        if (ret == AccessResult.accessNull && nullValue != null)
            return nullValue;
        if (defaultValue != null)
            return defaultValue;
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.noNodePropertyValue, propName.ToString() ?? "");
        return interp.makeError();
    }
}

// ExternalProcedure primitive - looks up an external procedure by public ID
public class ExternalProcedurePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public ExternalProcedurePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        StringC? pubid = args[0]?.convertToString();
        if (pubid == null)
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);
        FunctionObj? func = interp.lookupExternalProc(pubid);
        if (func != null)
            return func;
        return interp.makeFalse();
    }
}

// IfFirstPage primitive - returns sosofo that conditionally processes based on first page
public class IfFirstPagePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public IfFirstPagePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        SosofoObj?[] sosofo = new SosofoObj?[2];
        for (int i = 0; i < 2; i++)
        {
            sosofo[i] = args[i]?.asSosofo();
            if (sosofo[i] == null)
                return argError(interp, loc, InterpreterMessages.notASosofo, i, args[i]);
        }
        return new PageTypeSosofoObj((uint)FOTBuilder.HF.firstHF, sosofo[0]!, sosofo[1]!);
    }
}

// IfFrontPage primitive - returns sosofo that conditionally processes based on front page
public class IfFrontPagePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(2, 0, false);
    public IfFrontPagePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        SosofoObj?[] sosofo = new SosofoObj?[2];
        for (int i = 0; i < 2; i++)
        {
            sosofo[i] = args[i]?.asSosofo();
            if (sosofo[i] == null)
                return argError(interp, loc, InterpreterMessages.notASosofo, i, args[i]);
        }
        return new PageTypeSosofoObj((uint)FOTBuilder.HF.frontHF, sosofo[0]!, sosofo[1]!);
    }
}

// ReadEntity primitive - reads an entity by system ID and returns its contents as a string
public class ReadEntityPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public ReadEntityPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        StringC? sysid = args[0]?.convertToString();
        if (sysid == null)
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);

        StringC contents = new StringC();
        if (interp.groveManager() != null && interp.groveManager()!.readEntity(sysid, out contents))
            return new StringObj(contents);
        return interp.makeError();
    }
}

// Helper function to count child number (0-based, elements with same GI before node)
internal static class PrimitiveHelper
{
    public static bool childNumber(NodePtr node, out ulong result)
    {
        result = 0;
        GroveString nodeGi = new GroveString();
        if (node.getGi(ref nodeGi) != AccessResult.accessOK)
            return false;

        NodePtr parent = new NodePtr();
        if (node.getParent(ref parent) != AccessResult.accessOK)
        {
            // Must be document element
            result = 0;
            return true;
        }

        // Count preceding siblings with same GI
        NodePtr sibling = new NodePtr();
        if (parent.firstChild(ref sibling) != AccessResult.accessOK)
            return false;

        ulong count = 0;
        while (sibling)
        {
            if (sibling.node!.Equals(node.node!))
            {
                result = count;
                return true;
            }
            GroveString siblingGi = new GroveString();
            if (sibling.getGi(ref siblingGi) == AccessResult.accessOK)
            {
                if (giEqual(nodeGi, siblingGi))
                    count++;
            }
            if (sibling.assignNextSibling() != AccessResult.accessOK)
                break;
        }
        result = count;
        return true;
    }

    public static bool giEqual(GroveString a, GroveString b)
    {
        if (a.size() != b.size())
            return false;
        for (nuint i = 0; i < a.size(); i++)
        {
            if (a.data()![i] != b.data()![i])
                return false;
        }
        return true;
    }
}

// HierarchicalNumberRecursive primitive
public class HierarchicalNumberRecursivePrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    public HierarchicalNumberRecursivePrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (nArgs > 1)
        {
            if (!args[1]!.optSingletonNodeList(ctx, interp, ref node) || node == null || !node)
                return argError(interp, loc, InterpreterMessages.notASingletonNode, 1, args[1]);
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }

        // Get the GI string to match
        StringC? giStr = args[0]?.convertToString();
        if (giStr == null)
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);

        // Build result list (in reverse order, then we'll return as-is which gives correct order)
        ELObj result = interp.makeNil();

        NodePtr current = new NodePtr(node.node!);
        NodePtr parent = new NodePtr();
        while (current.getParent(ref parent) == AccessResult.accessOK)
        {
            current = parent;
            GroveString nodeGi = new GroveString();
            if (current.getGi(ref nodeGi) == AccessResult.accessOK)
            {
                // Compare GI
                if (giStr.size() == nodeGi.size())
                {
                    bool match = true;
                    for (nuint i = 0; i < giStr.size() && match; i++)
                    {
                        if (giStr[i] != nodeGi.data()![i])
                            match = false;
                    }
                    if (match)
                    {
                        ulong num = 0;
                        PrimitiveHelper.childNumber(current, out num);
                        // Prepend to result list
                        result = new PairObj(interp.makeInteger((long)(num + 1)), result);
                    }
                }
            }
        }
        return result;
    }
}

// AncestorChildNumber primitive
public class AncestorChildNumberPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 1, false);
    public AncestorChildNumberPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        NodePtr? node = null;
        if (nArgs > 1)
        {
            if (!args[1]!.optSingletonNodeList(ctx, interp, ref node) || node == null || !node)
                return argError(interp, loc, InterpreterMessages.notASingletonNode, 1, args[1]);
        }
        else
        {
            node = ctx.currentNode;
            if (node == null || !node)
                return noCurrentNodeError(interp, loc);
        }

        // Get the GI string to match
        StringC? giStr = args[0]?.convertToString();
        if (giStr == null)
            return argError(interp, loc, InterpreterMessages.notAString, 0, args[0]);

        NodePtr current = new NodePtr(node.node!);
        NodePtr parent = new NodePtr();
        while (current.getParent(ref parent) == AccessResult.accessOK)
        {
            current = parent;
            GroveString nodeGi = new GroveString();
            if (current.getGi(ref nodeGi) == AccessResult.accessOK)
            {
                // Compare GI
                if (giStr.size() == nodeGi.size())
                {
                    bool match = true;
                    for (nuint i = 0; i < giStr.size() && match; i++)
                    {
                        if (giStr[i] != nodeGi.data()![i])
                            match = false;
                    }
                    if (match)
                    {
                        ulong num = 0;
                        PrimitiveHelper.childNumber(current, out num);
                        return interp.makeInteger((long)(num + 1));
                    }
                }
            }
        }
        return interp.makeFalse();
    }
}

