// Copyright (c) 2026 Ralf Schleitzer
// See the file copying.txt for copying permission.

// Not in the original. OpenJade parses define-page-model and define-column-set-model and skips them, and has
// no page-sequence or column-set-sequence flow object class. This file adds a part of the full page model of
// ISO/IEC 10179:1996 (12.6.4, 12.6.5): the features page and multi-column, restricted, and not nested-column-set.
//
// - a page model has the size of the page and exactly one region, filled top to bottom, with its origin and size
//   (the origin at the lower left, y going up, 12.3.1) and the principal port as its flow;
// - a column-set model has exactly one column subset, its columns each with an x origin and a width, the
//   principal port flowed into the body-text zone, and balance?;
// - page-sequence takes initial-page-models and repeat-page-models, column-set-sequence column-set-model and the
//   display characteristics.
//
// Everything else of the page model is an error where it is written; a characteristic outside the subset is
// allowed with its initial value only. Nothing is passed over, or a stylesheet would be set otherwise than it asks.

namespace OpenJade.Style;

using OpenSP;
using Char = System.UInt32;

// A page model as the backend gets it: the page and its one region, lengths in units
public sealed class PageModel
{
    public long width;
    public long height;
    public long regionXOrigin;
    public long regionYOrigin;
    public long regionWidth;
    public long regionHeight;
}

// A column-set model as the backend gets it: the columns of its one subset, from the left edge of the column set
public sealed class ColumnSetModel
{
    public long[] columnXOrigin = System.Array.Empty<long>();
    public long[] columnWidth = System.Array.Empty<long>();
    public bool balance;
    public bool hasWidth;
    public long width;
}

public class PageModelObj : ELObj
{
    private readonly PageModel model_;
    public PageModelObj(PageModel model) { model_ = model; }
    public PageModel pageModel() { return model_; }
}

public class ColumnSetModelObj : ELObj
{
    private readonly ColumnSetModel model_;
    public ColumnSetModelObj(ColumnSetModel model) { model_ = model; }
    public ColumnSetModel columnSetModel() { return model_; }
}

// The parts of a model on the way to it: a region, a column subset, a column
internal class ModelPartObj : ELObj
{
    public readonly string kind;
    public readonly System.Collections.Generic.Dictionary<string, ELObj> clauses;
    public readonly System.Collections.Generic.List<ModelPartObj> parts;
    public ModelPartObj(string kind, System.Collections.Generic.Dictionary<string, ELObj> clauses,
                        System.Collections.Generic.List<ModelPartObj> parts)
    {
        this.kind = kind;
        this.clauses = clauses;
        this.parts = parts;
    }
}

// Builds a model or a part of it from the clauses the parser read: a symbol for each clause name, then its value
internal class ModelBuilderPrimitiveObj : PrimitiveObj
{
    private static readonly Signature signature_ = new Signature(0, 0, true);
    private readonly string kind_;

    public ModelBuilderPrimitiveObj(string kind) : base(signature_) { kind_ = kind; }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ec, Interpreter interp, Location loc)
    {
        var clauses = new System.Collections.Generic.Dictionary<string, ELObj>();
        var parts = new System.Collections.Generic.List<ModelPartObj>();
        for (int i = 0; i + 1 < nArgs; i += 2)
        {
            string name = symbolName(args[i]!.asSymbol()!);
            ELObj value = args[i + 1]!;
            if (value is ModelPartObj part)
                parts.Add(part);
            else
                clauses[name] = value;
        }
        switch (kind_)
        {
            case "page-model": return makePageModel(clauses, parts, interp, loc);
            case "column-set-model": return makeColumnSetModel(clauses, parts, interp, loc);
            default: return new ModelPartObj(kind_, clauses, parts);
        }
    }

    internal static string symbolName(SymbolObj sym)
    {
        Char[]? s;
        nuint n;
        if (sym.name() == null || !sym.name()!.stringData(out s, out n) || s == null)
            return "";
        return new StringC(s, n).ToString();
    }

    private static ELObj error(Interpreter interp, Location loc, string what)
    {
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.invalidPageModel, what);
        return interp.makeError();
    }

    private static bool length(System.Collections.Generic.Dictionary<string, ELObj> clauses, string name, out long n)
    {
        n = 0;
        if (!clauses.TryGetValue(name, out var obj))
            return false;
        LengthObj? l = obj.asLength();
        if (l != null)
        {
            n = l.value();
            return true;
        }
        long ln;
        double d;
        int dim;
        switch (obj.quantityValue(out ln, out d, out dim))
        {
            case ELObj.QuantityType.longQuantity:
                if (dim != 1)
                    return false;
                n = ln;
                return true;
            case ELObj.QuantityType.doubleQuantity:
                if (dim != 1)
                    return false;
                n = (long)System.Math.Round(d);
                return true;
            default:
                return false;
        }
    }

    // A required length: false with a message if it is missing or not a length
    private static bool required(System.Collections.Generic.Dictionary<string, ELObj> clauses, string name, string of,
                                 out long n, Interpreter interp, Location loc)
    {
        if (length(clauses, name, out n))
            return true;
        error(interp, loc, (clauses.ContainsKey(name) ? "not a length: " : "missing: ") + name + " of " + of);
        return false;
    }

    // filling-direction, where given, is top-to-bottom
    private static bool topToBottom(System.Collections.Generic.Dictionary<string, ELObj> clauses, string of,
                                    Interpreter interp, Location loc)
    {
        if (!clauses.TryGetValue("filling-direction", out var obj))
            return true;
        SymbolObj? sym = obj.asSymbol();
        if (sym != null && symbolName(sym) == "top-to-bottom")
            return true;
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.notSupportedInPageModel, "filling-direction of " + of + " other than top-to-bottom");
        return false;
    }

    private static ELObj makePageModel(System.Collections.Generic.Dictionary<string, ELObj> clauses,
                                       System.Collections.Generic.List<ModelPartObj> parts, Interpreter interp, Location loc)
    {
        var m = new PageModel();
        if (!required(clauses, "width", "page-model", out m.width, interp, loc)
            || !required(clauses, "height", "page-model", out m.height, interp, loc)
            || !topToBottom(clauses, "page-model", interp, loc))
            return interp.makeError();
        if (parts.Count != 1)
        {
            if (parts.Count == 0)
                return error(interp, loc, "a page-model needs a region");
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.notSupportedInPageModel, "more than one region");
            return interp.makeError();
        }
        var region = parts[0].clauses;
        if (!required(region, "x-origin", "region", out m.regionXOrigin, interp, loc)
            || !required(region, "y-origin", "region", out m.regionYOrigin, interp, loc)
            || !required(region, "width", "region", out m.regionWidth, interp, loc)
            || !required(region, "height", "region", out m.regionHeight, interp, loc)
            || !topToBottom(region, "region", interp, loc))
            return interp.makeError();
        return new PageModelObj(m);
    }

    private static ELObj makeColumnSetModel(System.Collections.Generic.Dictionary<string, ELObj> clauses,
                                            System.Collections.Generic.List<ModelPartObj> parts, Interpreter interp, Location loc)
    {
        var m = new ColumnSetModel();
        if (!topToBottom(clauses, "column-set-model", interp, loc))
            return interp.makeError();
        if (clauses.TryGetValue("fill-out", out var fillOut) && fillOut.isTrue())
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.notSupportedInPageModel, "fill-out #t");
            return interp.makeError();
        }
        if (clauses.ContainsKey("width"))
        {
            if (!required(clauses, "width", "column-set-model", out m.width, interp, loc))
                return interp.makeError();
            m.hasWidth = true;
        }
        if (parts.Count != 1)
        {
            if (parts.Count == 0)
                return error(interp, loc, "a column-set-model needs a column-subset");
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.notSupportedInPageModel, "more than one column-subset");
            return interp.makeError();
        }
        var subset = parts[0];
        if (subset.clauses.TryGetValue("balance?", out var balance))
            m.balance = balance.isTrue();
        long deviation;
        if (subset.clauses.ContainsKey("length-deviation")
            && (!length(subset.clauses, "length-deviation", out deviation) || deviation != 0))
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.notSupportedInPageModel, "length-deviation other than 0pt");
            return interp.makeError();
        }
        if (subset.parts.Count == 0)
            return error(interp, loc, "a column-subset needs a column");
        m.columnXOrigin = new long[subset.parts.Count];
        m.columnWidth = new long[subset.parts.Count];
        for (int i = 0; i < subset.parts.Count; i++)
        {
            var column = subset.parts[i].clauses;
            if (!required(column, "x-origin", "column", out m.columnXOrigin[i], interp, loc)
                || !required(column, "width", "column", out m.columnWidth[i], interp, loc))
                return interp.makeError();
        }
        return new ColumnSetModelObj(m);
    }
}

public class IsPageModelPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsPageModelPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0] is PageModelObj ? interp.makeTrue() : interp.makeFalse();
    }
}

public class IsColumnSetModelPrimitiveObj : PrimitiveObj
{
    private static readonly Signature sig = new Signature(1, 0, false);
    public IsColumnSetModelPrimitiveObj() : base(sig) { }

    public override ELObj? primitiveCall(int nArgs, ELObj?[] args, EvalContext ctx, Interpreter interp, Location loc)
    {
        return args[0] is ColumnSetModelObj ? interp.makeTrue() : interp.makeFalse();
    }
}

// initial-page-models: and repeat-page-models: (inherited, 12.6.4): a list of page models
public class PageModelsC : InheritedC
{
    public delegate void Setter(FOTBuilder fotb, PageModel[] models);
    private readonly Setter setter_;
    private readonly ELObj list_;
    private readonly PageModel[] models_;

    public PageModelsC(Identifier? ident, uint index, Setter setter, ELObj list, PageModel[] models)
        : base(ident, index)
    {
        setter_ = setter;
        list_ = list;
        models_ = models;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        setter_(fotb, models_);
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        return list_;
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        var models = new System.Collections.Generic.List<PageModel>();
        ELObj p = obj;
        for (;;)
        {
            if (p.isNil())
                break;
            PairObj? pair = p.asPair();
            if (pair == null || pair.car() is not PageModelObj m)
            {
                interp.invalidCharacteristicValue(identifier(), loc);
                return null;
            }
            models.Add(m.pageModel());
            p = pair.cdr()!;
        }
        return new ConstPtr<InheritedC>(new PageModelsC(identifier(), index(), setter_, obj, models.ToArray()));
    }
}

// column-set-model: (inherited, 12.6.5): a column-set model, or #f
public class ColumnSetModelC : InheritedC
{
    private readonly ELObj? obj_;

    public ColumnSetModelC(Identifier? ident, uint index, ELObj? obj) : base(ident, index) { obj_ = obj; }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
        fotb.setColumnSetModel((obj_ as ColumnSetModelObj)?.columnSetModel());
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        return obj_ ?? vm.interp.makeFalse();
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        if (obj is ColumnSetModelObj || obj == interp.makeFalse())
            return new ConstPtr<InheritedC>(new ColumnSetModelC(identifier(), index(), obj));
        interp.invalidCharacteristicValue(identifier(), loc);
        return null;
    }
}

// A characteristic of the page model outside the subset: only its initial value may be specified
public class InitialOnlyC : InheritedC
{
    private readonly ELObj initial_;
    private readonly bool anyAllowed_;

    // anyAllowed: the standard gives no initial value, so no value may be specified
    public InitialOnlyC(Identifier? ident, uint index, ELObj initial, bool anyAllowed = true)
        : base(ident, index)
    {
        initial_ = initial;
        anyAllowed_ = anyAllowed;
    }

    public override void set(VM vm, VarStyleObj? style, FOTBuilder fotb,
                             ref ELObj? cacheValue, System.Collections.Generic.List<nuint> dependencies)
    {
    }

    public override ELObj? value(VM vm, VarStyleObj? style, System.Collections.Generic.List<nuint> dependencies)
    {
        return initial_;
    }

    public override ConstPtr<InheritedC>? make(ELObj obj, Location loc, Interpreter interp)
    {
        if (anyAllowed_ && ELObj.equal(obj, initial_))
            return new ConstPtr<InheritedC>(new InitialOnlyC(identifier(), index(), initial_, anyAllowed_));
        interp.setNextLocation(loc);
        interp.message(InterpreterMessages.notSupportedInPageModel,
                       (identifier()?.name().ToString() ?? "characteristic") + " other than its initial value");
        return null;
    }
}

// The page-sequence flow object class (12.6.4)
public class PageSequenceFlowObj : CompoundFlowObj
{
    public PageSequenceFlowObj()
    {
        hasSubObjects_ = (char)1;
    }

    public override FlowObj copy(Interpreter interp)
    {
        PageSequenceFlowObj c = new PageSequenceFlowObj();
        c.setStyle(style());
        c.setContent(content());
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        fotb.startPageSequence();
        base.processInner(context);
        fotb.endPageSequence();
    }
}

// The column-set-sequence flow object class (12.6.5); a display flow object
public class ColumnSetSequenceFlowObj : CompoundFlowObj
{
    private FOTBuilder.DisplayNIC nic_ = new FOTBuilder.DisplayNIC();
    [System.ThreadStatic] private static int depth_;

    public override FlowObj copy(Interpreter interp)
    {
        ColumnSetSequenceFlowObj c = new ColumnSetSequenceFlowObj();
        c.setStyle(style());
        c.setContent(content());
        c.nic_.CopyFrom(nic_);
        return c;
    }

    public override void processInner(ProcessContext context)
    {
        FOTBuilder fotb = context.currentFOTBuilder();
        if (depth_ > 0)
            context.vm().interp.message(InterpreterMessages.notSupportedInPageModel,
                                        "a column-set-sequence within a column-set-sequence (nested-column-set)");
        depth_++;
        try
        {
            fotb.startColumnSetSequence(nic_);
            base.processInner(context);
            fotb.endColumnSetSequence();
        }
        finally
        {
            depth_--;
        }
    }

    public override bool hasNonInheritedC(Identifier? ident)
    {
        return isDisplayNIC(ident);
    }

    public override void setNonInheritedC(Identifier? ident, ELObj? obj, Location loc, Interpreter interp)
    {
        if (!setDisplayNIC(ref nic_, ident, obj, loc, interp))
            return;
        string? outside = null;
        if (nic_.breakBefore != FOTBuilder.Symbol.symbolFalse && nic_.breakBefore != FOTBuilder.Symbol.symbolPage
            && nic_.breakBefore != FOTBuilder.Symbol.symbolColumn)
            outside = "break-before other than page, column or #f";
        else if (nic_.breakAfter != FOTBuilder.Symbol.symbolFalse && nic_.breakAfter != FOTBuilder.Symbol.symbolPage
                 && nic_.breakAfter != FOTBuilder.Symbol.symbolColumn)
            outside = "break-after other than page, column or #f";
        else if (nic_.keep != FOTBuilder.Symbol.symbolFalse)
            outside = "keep on a column-set-sequence";
        else if (nic_.positionPreference != FOTBuilder.Symbol.symbolFalse)
            outside = "position-preference on a column-set-sequence";
        else if (nic_.mayViolateKeepBefore || nic_.mayViolateKeepAfter)
            outside = "may-violate-keep-before? or may-violate-keep-after? on a column-set-sequence";
        if (outside != null)
        {
            interp.setNextLocation(loc);
            interp.message(InterpreterMessages.notSupportedInPageModel, outside);
        }
    }
}

// define-page-model and define-column-set-model (12.6.4.1 [188]-[203], 12.6.5.1 [204]-[220])
public partial class SchemeParser
{
    private enum ClauseKind { Expr, Part, PageFlow, SubsetFlow }

    // Which clauses each form takes, and what they are; a clause of the standard not listed is not supported
    private static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, ClauseKind>> modelClauses_ = new()
    {
        ["page-model"] = new() { ["width"] = ClauseKind.Expr, ["height"] = ClauseKind.Expr,
                                 ["filling-direction"] = ClauseKind.Expr, ["region"] = ClauseKind.Part },
        ["region"] = new() { ["x-origin"] = ClauseKind.Expr, ["y-origin"] = ClauseKind.Expr, ["width"] = ClauseKind.Expr,
                             ["height"] = ClauseKind.Expr, ["filling-direction"] = ClauseKind.Expr,
                             ["flow"] = ClauseKind.PageFlow },
        ["column-set-model"] = new() { ["column-subset"] = ClauseKind.Part, ["fill-out"] = ClauseKind.Expr,
                                       ["filling-direction"] = ClauseKind.Expr, ["width"] = ClauseKind.Expr },
        ["column-subset"] = new() { ["column"] = ClauseKind.Part, ["flow"] = ClauseKind.SubsetFlow,
                                    ["balance?"] = ClauseKind.Expr, ["length-deviation"] = ClauseKind.Expr },
        ["column"] = new() { ["x-origin"] = ClauseKind.Expr, ["width"] = ClauseKind.Expr },
    };

    private bool doDefineModel(string kind)
    {
        Token tok;
        if (!getToken(TokenAllow.Identifier, out tok))
            return false;
        Identifier name = lookup(currentToken_);
        Location loc = in_?.currentLocation() ?? new Location();
        bool ok = true;
        Expression? expr = parseModelBody(kind, loc, ref ok);
        if (expr == null)
            return false;
        // As (define name ...): the model is made when the variable is first used
        if (ok)
            name.setExpression(expr, 0, loc);
        return true;
    }

    // The clauses of a model or a part up to its closing parenthesis, as a call to its builder
    private Expression? parseModelBody(string kind, Location loc, ref bool ok)
    {
        var args = new System.Collections.Generic.List<Expression>();
        var allowed = modelClauses_[kind];
        for (;;)
        {
            Token tok;
            if (!getToken(TokenAllow.OpenParen | TokenAllow.CloseParen, out tok))
                return null;
            if (tok == Token.CloseParen)
                break;
            if (!getToken(TokenAllow.Identifier, out tok))
                return null;
            string clause = currentToken_.ToString();
            Location clauseLoc = in_?.currentLocation() ?? new Location();
            if (!allowed.TryGetValue(clause, out var what))
            {
                interp_.setNextLocation(clauseLoc);
                interp_.message(InterpreterMessages.notSupportedInPageModel, clause + " in " + kind);
                ok = false;
                if (!skipForm())
                    return null;
                continue;
            }
            switch (what)
            {
                case ClauseKind.Expr:
                    {
                        Expression? value;
                        Identifier.SyntacticKey key;
                        if (!parseExpression(TokenAllow.Expr, out value, out key, out tok) || !expectCloseParen())
                            return null;
                        args.Add(new ConstantExpression(interp_.makeSymbol(interp_.makeStringC(clause)), clauseLoc));
                        args.Add(value!);
                        break;
                    }
                case ClauseKind.Part:
                    {
                        Expression? part = parseModelBody(clause, clauseLoc, ref ok);
                        if (part == null)
                            return null;
                        args.Add(new ConstantExpression(interp_.makeSymbol(interp_.makeStringC(clause)), clauseLoc));
                        args.Add(part);
                        break;
                    }
                case ClauseKind.PageFlow:
                case ClauseKind.SubsetFlow:
                    {
                        string? text = flowText();
                        if (text == null)
                            return null;
                        string expected = what == ClauseKind.PageFlow ? "#f" : "((#f body-text))";
                        if (text != expected)
                        {
                            interp_.setNextLocation(clauseLoc);
                            interp_.message(InterpreterMessages.notSupportedInPageModel,
                                            "(flow " + text + ") in " + kind + ", only (flow " + expected + ")");
                            ok = false;
                        }
                        break;
                    }
            }
        }
        var builder = new ModelBuilderPrimitiveObj(kind);
        interp_.makePermanent(builder);
        return new CallExpression(new ConstantExpression(builder, loc), args, loc);
    }

    // The rest of a flow clause up to its closing parenthesis, as text with single spaces
    private string? flowText()
    {
        var sb = new System.Text.StringBuilder();
        int depth = 1;
        for (;;)
        {
            Token tok;
            if (!getToken(TokenAllow.OpenParen | TokenAllow.CloseParen | TokenAllow.Expr | TokenAllow.EndOfEntity, out tok)
                || tok == Token.EndOfEntity)
                return null;
            if (tok == Token.CloseParen && --depth == 0)
                return sb.ToString();
            string piece = tok switch
            {
                Token.OpenParen => "(",
                Token.CloseParen => ")",
                Token.False => "#f",
                Token.True => "#t",
                _ => currentToken_.ToString(),
            };
            if (tok == Token.OpenParen)
                depth++;
            if (sb.Length > 0 && piece != ")" && sb[sb.Length - 1] != '(')
                sb.Append(' ');
            sb.Append(piece);
        }
    }
}
