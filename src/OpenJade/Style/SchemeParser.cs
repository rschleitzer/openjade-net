// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;
using Xchar = System.Int32;  // Char + EOF (-1)

// Token flags for parser
[Flags]
public enum TokenAllow
{
    EndOfEntity = 0x01,
    False = 0x02,
    Keyword = 0x04,
    OpenParen = 0x08,
    CloseParen = 0x10,
    Identifier = 0x20,
    Period = 0x40,
    OtherExpr = 0x80,  // number, character, glyph-id, quote, backquote
    ExpressionKey = 0x100,
    KeyDefine = 0x200,
    KeyElse = 0x400,
    KeyArrow = 0x800,  // =>
    String = 0x1000,
    HashOptional = 0x2000,
    HashKey = 0x4000,
    HashRest = 0x8000,
    Unquote = 0x10000,
    UnquoteSplicing = 0x20000,
    QuasiquoteKey = 0x40000,
    Vector = 0x80000,
    HashContents = 0x100000,
    Expr = False | Keyword | OpenParen | Identifier | String | HashOptional | HashKey | HashRest | OtherExpr
}

// Parser tokens
public enum Token
{
    EndOfEntity,
    True,
    False,
    String,
    Identifier,
    Keyword,
    Char,
    Number,
    GlyphId,
    OpenParen,
    CloseParen,
    Period,
    Vector,
    Quote,
    Quasiquote,
    Unquote,
    UnquoteSplicing,
    HashRest,
    HashOptional,
    HashKey,
    HashContents,
    Void
}

// DSSSL Scheme Parser
public class SchemeParser : Messenger
{
    private const Char defaultChar = 0xfffd;
    private const Xchar noChar = -2;  // No pushed-back character

    private Interpreter interp_;
    private InputSource? in_;
    private StringC currentToken_;
    private ProcessingMode? defMode_;
    private string? afiiPublicId_;
    private bool dsssl2_;
    private LangObj? lang_;
    private Xchar pushedBack_ = noChar;  // One-character pushback buffer

    public SchemeParser(Interpreter interp, InputSource input)
    {
        interp_ = interp;
        in_ = input;
        currentToken_ = new StringC();
        defMode_ = interp.initialProcessingMode();
        dsssl2_ = false;  // TODO: interp.dsssl2()
        lang_ = null;
        afiiPublicId_ = "ISO/IEC 10036/RA//Glyphs";
        pushedBack_ = noChar;
    }

    // Character reading helpers
    private Xchar getChar()
    {
        if (pushedBack_ != noChar)
        {
            Xchar c = pushedBack_;
            pushedBack_ = noChar;
            return c;
        }
        if (in_ == null)
            return InputSource.eE;
        return in_.get(this);
    }

    private void ungetChar(Xchar c)
    {
        pushedBack_ = c;
    }

    public void parse()
    {
        // Main parsing entry point
        for (;;)
        {
            Token tok;
            if (!getToken(TokenAllow.OpenParen | TokenAllow.EndOfEntity, out tok))
                break;
            if (tok == Token.EndOfEntity)
                break;
            if (!parseTopLevel())
                break;
        }
    }

    private bool parseTopLevel()
    {
        Token tok;
        if (!getToken(TokenAllow.Identifier | TokenAllow.KeyDefine, out tok))
            return false;
        if (tok == Token.Identifier)
        {
            Identifier ident = lookup(currentToken_);
            Identifier.SyntacticKey key;
            if (ident.syntacticKey(out key))
            {
                switch (key)
                {
                    case Identifier.SyntacticKey.define:
                        return doDefine();
                    case Identifier.SyntacticKey.defineUnit:
                        return doDefineUnit();
                    case Identifier.SyntacticKey.element:
                        return doElement();
                    case Identifier.SyntacticKey.orElement:
                        return doOrElement();
                    case Identifier.SyntacticKey.defaultEntity:
                        return doDefault();
                    case Identifier.SyntacticKey.root:
                        return doRoot();
                    case Identifier.SyntacticKey.id:
                        return doId();
                    case Identifier.SyntacticKey.mode:
                        return doMode();
                    case Identifier.SyntacticKey.declareInitialValue:
                        return doDeclareInitialValue();
                    case Identifier.SyntacticKey.declareCharacteristic:
                        return doDeclareCharacteristic();
                    default:
                        break;
                }
            }
            message(InterpreterMessages.unknownTopLevelForm);
            return skipForm();
        }
        message(InterpreterMessages.badTopLevelForm);
        return skipForm();
    }

    public void parseStandardChars()
    {
        for (;;)
        {
            Token tok;
            if (!getToken(TokenAllow.Identifier | TokenAllow.EndOfEntity, out tok)
                || tok == Token.EndOfEntity)
                break;

            StringC name = new StringC(currentToken_);

            if (!getToken(TokenAllow.OtherExpr, out tok) || tok != Token.Number)
            {
                message(InterpreterMessages.badDeclaration);
                break;
            }

            // TODO: validate name and add standard char
            // interp_.addStandardChar(name, currentToken_);
        }
    }

    public void parseNameChars()
    {
        for (;;)
        {
            Token tok;
            if (!getToken(TokenAllow.Identifier | TokenAllow.EndOfEntity, out tok)
                || tok == Token.EndOfEntity)
                break;
            // interp_.addNameChar(currentToken_);
        }
    }

    public void parseSeparatorChars()
    {
        for (;;)
        {
            Token tok;
            if (!getToken(TokenAllow.Identifier | TokenAllow.EndOfEntity, out tok)
                || tok == Token.EndOfEntity)
                break;
            // interp_.addSeparatorChar(currentToken_);
        }
    }

    public void parseMapSdataEntity(StringC name, StringC text)
    {
        // TODO: implement SDATA entity mapping
    }

    public bool parseExpression(out Expression? expr)
    {
        expr = null;
        Identifier.SyntacticKey key;
        Token tok;
        return parseExpression(TokenAllow.Expr, out expr, out key, out tok);
    }

    private bool parseExpression(TokenAllow allowed, out Expression? expr,
                                 out Identifier.SyntacticKey key, out Token tok)
    {
        expr = null;
        key = Identifier.SyntacticKey.notKey;
        tok = Token.EndOfEntity;

        if (!getToken(allowed, out tok))
            return false;

        switch (tok)
        {
            case Token.Identifier:
                {
                    Identifier ident = lookup(currentToken_);
                    if (ident.syntacticKey(out key))
                    {
                        // Check for special forms
                    }
                    expr = new VariableExpression(ident, in_?.currentLocation() ?? new Location());
                    return true;
                }
            case Token.Number:
                {
                    // Parse number
                    long val = 0;
                    for (nuint i = 0; i < currentToken_.size(); i++)
                    {
                        Char c = currentToken_[i];
                        if (c >= '0' && c <= '9')
                            val = val * 10 + (c - '0');
                    }
                    expr = new ConstantExpression(interp_.makeInteger(val), in_?.currentLocation() ?? new Location());
                    return true;
                }
            case Token.String:
                {
                    expr = new ConstantExpression(interp_.makeString(currentToken_), in_?.currentLocation() ?? new Location());
                    return true;
                }
            case Token.True:
                {
                    expr = new ConstantExpression(interp_.makeTrue(), in_?.currentLocation() ?? new Location());
                    return true;
                }
            case Token.False:
                {
                    expr = new ConstantExpression(interp_.makeFalse(), in_?.currentLocation() ?? new Location());
                    return true;
                }
            case Token.OpenParen:
                return parseCall(out expr);
            case Token.Quote:
                return parseQuote(out expr);
            case Token.Quasiquote:
                return parseQuasiquote(out expr);
            case Token.Char:
                {
                    if (currentToken_.size() > 0)
                        expr = new ConstantExpression(interp_.makeChar(currentToken_[0]), in_?.currentLocation() ?? new Location());
                    else
                        expr = new ConstantExpression(interp_.makeChar(0), in_?.currentLocation() ?? new Location());
                    return true;
                }
            default:
                return false;
        }
    }

    private bool parseCall(out Expression? expr)
    {
        expr = null;
        Token tok;
        Identifier.SyntacticKey key;
        if (!getToken(TokenAllow.Identifier | TokenAllow.OpenParen | TokenAllow.CloseParen, out tok))
            return false;

        if (tok == Token.CloseParen)
        {
            // Empty list ()
            expr = new ConstantExpression(interp_.makeNil(), in_?.currentLocation() ?? new Location());
            return true;
        }

        if (tok == Token.Identifier)
        {
            Identifier ident = lookup(currentToken_);
            if (ident.syntacticKey(out key))
            {
                switch (key)
                {
                    case Identifier.SyntacticKey.quote:
                        return parseQuote(out expr);
                    case Identifier.SyntacticKey.ifKey:
                        return parseIf(out expr);
                    case Identifier.SyntacticKey.cond:
                        return parseCond(out expr);
                    case Identifier.SyntacticKey.caseKey:
                        return parseCase(out expr);
                    case Identifier.SyntacticKey.and:
                        return parseAnd(out expr);
                    case Identifier.SyntacticKey.or:
                        return parseOr(out expr);
                    case Identifier.SyntacticKey.let:
                        return parseLet(out expr);
                    case Identifier.SyntacticKey.letStar:
                        return parseLetStar(out expr);
                    case Identifier.SyntacticKey.letrec:
                        return parseLetrec(out expr);
                    case Identifier.SyntacticKey.lambda:
                        return parseLambda(out expr);
                    case Identifier.SyntacticKey.begin:
                        return parseBegin(out expr);
                    case Identifier.SyntacticKey.set:
                        return parseSet(out expr);
                    case Identifier.SyntacticKey.make:
                        return parseMake(out expr);
                    case Identifier.SyntacticKey.style:
                        return parseStyle(out expr);
                    case Identifier.SyntacticKey.withMode:
                        return parseWithMode(out expr);
                    default:
                        break;
                }
            }
            // Regular function call
            return parseRegularCall(ident, out expr);
        }

        // Expression in function position
        Expression? funcExpr;
        Identifier.SyntacticKey funcKey;
        if (!parseExpression(TokenAllow.Expr, out funcExpr, out funcKey, out tok))
            return false;
        return parseCallArgs(funcExpr, out expr);
    }

    private bool parseRegularCall(Identifier func, out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        System.Collections.Generic.List<Expression> args = new System.Collections.Generic.List<Expression>();
        Token tok;

        for (;;)
        {
            Identifier.SyntacticKey key;
            Expression? arg;
            if (!parseExpression(TokenAllow.Expr | TokenAllow.CloseParen, out arg, out key, out tok))
                return false;
            if (tok == Token.CloseParen)
                break;
            if (arg != null)
                args.Add(arg);
        }

        expr = new CallExpression(new VariableExpression(func, loc), args, loc);
        return true;
    }

    private bool parseCallArgs(Expression? func, out Expression? expr)
    {
        expr = null;
        if (func == null)
            return false;

        Location loc = in_?.currentLocation() ?? new Location();
        System.Collections.Generic.List<Expression> args = new System.Collections.Generic.List<Expression>();
        Token tok;

        for (;;)
        {
            Identifier.SyntacticKey key;
            Expression? arg;
            if (!parseExpression(TokenAllow.Expr | TokenAllow.CloseParen, out arg, out key, out tok))
                return false;
            if (tok == Token.CloseParen)
                break;
            if (arg != null)
                args.Add(arg);
        }

        expr = new CallExpression(func, args, loc);
        return true;
    }

    private bool parseQuote(out Expression? expr)
    {
        expr = null;
        ELObj? datum;
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;
        if (!parseDatum(TokenAllow.Expr, out datum, out loc, out tok))
            return false;
        if (!expectCloseParen())
            return false;
        expr = new ConstantExpression(datum, loc);
        return true;
    }

    private bool parseIf(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        Expression? test, consequent, alternate = null;
        Identifier.SyntacticKey key;
        Token tok;

        if (!parseExpression(TokenAllow.Expr, out test, out key, out tok))
            return false;
        if (!parseExpression(TokenAllow.Expr, out consequent, out key, out tok))
            return false;
        parseExpression(TokenAllow.Expr | TokenAllow.CloseParen, out alternate, out key, out tok);
        if (tok != Token.CloseParen)
        {
            if (!expectCloseParen())
                return false;
        }

        expr = new IfExpression(test!, consequent!, alternate, loc);
        return true;
    }

    private bool parseCond(out Expression? expr, bool opt = false)
    {
        expr = null;
        // TODO: implement cond parsing
        return skipForm();
    }

    private bool parseCase(out Expression? expr)
    {
        expr = null;
        // TODO: implement case parsing
        return skipForm();
    }

    private bool parseAnd(out Expression? expr, bool opt = false)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        System.Collections.Generic.List<Expression> exprs = new System.Collections.Generic.List<Expression>();
        Token tok;

        for (;;)
        {
            Identifier.SyntacticKey key;
            Expression? e;
            if (!parseExpression(TokenAllow.Expr | TokenAllow.CloseParen, out e, out key, out tok))
                return false;
            if (tok == Token.CloseParen)
                break;
            if (e != null)
                exprs.Add(e);
        }

        if (exprs.Count == 0)
            expr = new ConstantExpression(interp_.makeTrue(), loc);
        else if (exprs.Count == 1)
            expr = exprs[0];
        else
        {
            // Build right-associative binary tree
            expr = exprs[exprs.Count - 1];
            for (int i = exprs.Count - 2; i >= 0; i--)
                expr = new AndExpression(exprs[i], expr, loc);
        }
        return true;
    }

    private bool parseOr(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        System.Collections.Generic.List<Expression> exprs = new System.Collections.Generic.List<Expression>();
        Token tok;

        for (;;)
        {
            Identifier.SyntacticKey key;
            Expression? e;
            if (!parseExpression(TokenAllow.Expr | TokenAllow.CloseParen, out e, out key, out tok))
                return false;
            if (tok == Token.CloseParen)
                break;
            if (e != null)
                exprs.Add(e);
        }

        if (exprs.Count == 0)
            expr = new ConstantExpression(interp_.makeFalse(), loc);
        else if (exprs.Count == 1)
            expr = exprs[0];
        else
        {
            // Build right-associative binary tree
            expr = exprs[exprs.Count - 1];
            for (int i = exprs.Count - 2; i >= 0; i--)
                expr = new OrExpression(exprs[i], expr, loc);
        }
        return true;
    }

    private bool parseLet(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        System.Collections.Generic.List<Identifier> vars = new System.Collections.Generic.List<Identifier>();
        System.Collections.Generic.List<Expression> inits = new System.Collections.Generic.List<Expression>();
        Expression? body;

        if (!parseBindingsAndBody(vars, inits, out body))
            return false;

        expr = new LetExpression(vars, inits, body!, loc);
        return true;
    }

    private bool parseLetStar(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        System.Collections.Generic.List<Identifier> vars = new System.Collections.Generic.List<Identifier>();
        System.Collections.Generic.List<Expression> inits = new System.Collections.Generic.List<Expression>();
        Expression? body;

        if (!parseBindingsAndBody(vars, inits, out body))
            return false;

        expr = new LetStarExpression(vars, inits, body!, loc);
        return true;
    }

    private bool parseLetrec(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        System.Collections.Generic.List<Identifier> vars = new System.Collections.Generic.List<Identifier>();
        System.Collections.Generic.List<Expression> inits = new System.Collections.Generic.List<Expression>();
        Expression? body;

        if (!parseBindingsAndBody(vars, inits, out body))
            return false;

        expr = new LetrecExpression(vars, inits, body!, loc);
        return true;
    }

    private bool parseBindingsAndBody(System.Collections.Generic.List<Identifier> vars, System.Collections.Generic.List<Expression> inits, out Expression? body)
    {
        body = null;
        Token tok;
        if (!getToken(TokenAllow.OpenParen, out tok))
            return false;

        // Parse bindings
        for (;;)
        {
            if (!getToken(TokenAllow.OpenParen | TokenAllow.CloseParen, out tok))
                return false;
            if (tok == Token.CloseParen)
                break;

            // Parse (var init)
            if (!getToken(TokenAllow.Identifier, out tok))
                return false;
            vars.Add(lookup(currentToken_));

            Identifier.SyntacticKey key;
            Expression? init;
            if (!parseExpression(TokenAllow.Expr, out init, out key, out tok))
                return false;
            inits.Add(init!);

            if (!expectCloseParen())
                return false;
        }

        // Parse body
        return parseBegin(out body);
    }

    private bool parseLambda(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();

        // Parse formals
        Token tok;
        if (!getToken(TokenAllow.OpenParen | TokenAllow.Identifier, out tok))
            return false;

        System.Collections.Generic.List<Identifier> formals = new System.Collections.Generic.List<Identifier>();
        System.Collections.Generic.List<Expression> defaults = new System.Collections.Generic.List<Expression>();
        int nRequired = 0;
        bool hasRest = false;
        int nKey = 0;

        if (tok == Token.Identifier)
        {
            // (lambda x body) - single rest parameter
            formals.Add(lookup(currentToken_));
            hasRest = true;
        }
        else
        {
            // (lambda (args...) body)
            if (!parseFormals(formals, defaults, out nRequired, out hasRest, out nKey))
                return false;
        }

        // Parse body
        Expression? body;
        if (!parseBegin(out body))
            return false;

        int nOptional = formals.Count - nRequired - nKey - (hasRest ? 1 : 0);
        expr = new LambdaExpression(formals, defaults, nOptional, hasRest, nKey, body!, loc);
        return true;
    }

    private bool parseFormals(System.Collections.Generic.List<Identifier> formals, System.Collections.Generic.List<Expression> defaults,
                              out int nRequired, out bool hasRest, out int nKey)
    {
        nRequired = 0;
        hasRest = false;
        nKey = 0;
        Token tok;
        bool seenOptional = false;

        for (;;)
        {
            if (!getToken(TokenAllow.Identifier | TokenAllow.CloseParen | TokenAllow.HashOptional | TokenAllow.HashRest | TokenAllow.HashKey, out tok))
                return false;

            if (tok == Token.CloseParen)
                break;

            if (tok == Token.HashOptional)
            {
                seenOptional = true;
                continue;
            }
            if (tok == Token.HashRest)
            {
                if (!getToken(TokenAllow.Identifier, out tok))
                    return false;
                formals.Add(lookup(currentToken_));
                hasRest = true;
                continue;
            }
            if (tok == Token.HashKey)
            {
                // TODO: handle keyword arguments
                continue;
            }

            if (tok == Token.Identifier)
            {
                formals.Add(lookup(currentToken_));
                defaults.Add(null!);
                if (!seenOptional)
                    nRequired++;
            }
        }

        return true;
    }

    private bool parseBegin(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        System.Collections.Generic.List<Expression> exprs = new System.Collections.Generic.List<Expression>();
        Token tok;

        for (;;)
        {
            Identifier.SyntacticKey key;
            Expression? e;
            if (!parseExpression(TokenAllow.Expr | TokenAllow.CloseParen, out e, out key, out tok))
                return false;
            if (tok == Token.CloseParen)
                break;
            if (e != null)
                exprs.Add(e);
        }

        if (exprs.Count == 0)
            expr = new ConstantExpression(interp_.makeUnspecified(), loc);
        else if (exprs.Count == 1)
            expr = exprs[0];
        else
            expr = new SequenceExpression(exprs, loc);
        return true;
    }

    private bool parseSet(out Expression? expr)
    {
        expr = null;
        // TODO: implement set! parsing
        return skipForm();
    }

    private bool parseMake(out Expression? expr)
    {
        expr = null;
        // TODO: implement make parsing for flow objects
        return skipForm();
    }

    private bool parseStyle(out Expression? expr)
    {
        expr = null;
        // TODO: implement style parsing
        return skipForm();
    }

    private bool parseWithMode(out Expression? expr)
    {
        expr = null;
        // TODO: implement with-mode parsing
        return skipForm();
    }

    private bool parseQuasiquote(out Expression? expr)
    {
        expr = null;
        // TODO: implement quasiquote
        return skipForm();
    }

    private bool parseDatum(TokenAllow allowed, out ELObj? datum, out Location loc, out Token tok)
    {
        datum = null;
        loc = in_?.currentLocation() ?? new Location();
        tok = Token.EndOfEntity;

        if (!getToken(allowed, out tok))
            return false;

        switch (tok)
        {
            case Token.Number:
                {
                    long val = 0;
                    for (nuint i = 0; i < currentToken_.size(); i++)
                    {
                        Char c = currentToken_[i];
                        if (c >= '0' && c <= '9')
                            val = val * 10 + (c - '0');
                    }
                    datum = interp_.makeInteger(val);
                    return true;
                }
            case Token.String:
                datum = interp_.makeString(currentToken_);
                return true;
            case Token.Identifier:
                datum = interp_.makeSymbol(currentToken_);
                return true;
            case Token.True:
                datum = interp_.makeTrue();
                return true;
            case Token.False:
                datum = interp_.makeFalse();
                return true;
            case Token.OpenParen:
                return parseDatumList(out datum, out loc);
            default:
                return false;
        }
    }

    private bool parseDatumList(out ELObj? datum, out Location loc)
    {
        datum = null;
        loc = in_?.currentLocation() ?? new Location();
        System.Collections.Generic.List<ELObj?> items = new System.Collections.Generic.List<ELObj?>();
        Token tok;

        for (;;)
        {
            ELObj? item;
            Location itemLoc;
            if (!parseDatum(TokenAllow.Expr | TokenAllow.CloseParen | TokenAllow.Period, out item, out itemLoc, out tok))
                return false;

            if (tok == Token.CloseParen)
            {
                // Build proper list
                ELObj? result = interp_.makeNil();
                for (int i = items.Count - 1; i >= 0; i--)
                    result = interp_.makePair(items[i], result);
                datum = result;
                return true;
            }

            if (tok == Token.Period)
            {
                // Improper list
                ELObj? cdr;
                Location cdrLoc;
                if (!parseDatum(TokenAllow.Expr, out cdr, out cdrLoc, out tok))
                    return false;
                if (!expectCloseParen())
                    return false;
                ELObj? result = cdr;
                for (int i = items.Count - 1; i >= 0; i--)
                    result = interp_.makePair(items[i], result);
                datum = result;
                return true;
            }

            items.Add(item);
        }
    }

    // Definition parsing
    private bool doDefine()
    {
        Token tok;
        if (!getToken(TokenAllow.Identifier | TokenAllow.OpenParen, out tok))
            return false;

        if (tok == Token.Identifier)
        {
            // (define var expr)
            Identifier ident = lookup(currentToken_);
            Expression? expr;
            Identifier.SyntacticKey key;
            if (!parseExpression(TokenAllow.Expr, out expr, out key, out tok))
                return false;
            if (!expectCloseParen())
                return false;

            // Register definition
            ident.setExpression(expr, 0, in_?.currentLocation() ?? new Location());
            return true;
        }
        else
        {
            // (define (name args...) body)
            if (!getToken(TokenAllow.Identifier, out tok))
                return false;
            Identifier name = lookup(currentToken_);

            System.Collections.Generic.List<Identifier> formals = new System.Collections.Generic.List<Identifier>();
            System.Collections.Generic.List<Expression> defaults = new System.Collections.Generic.List<Expression>();
            int nRequired;
            bool hasRest;
            int nKey;
            if (!parseFormals(formals, defaults, out nRequired, out hasRest, out nKey))
                return false;

            Expression? body;
            if (!parseBegin(out body))
                return false;

            Location loc = in_?.currentLocation() ?? new Location();
            int nOptional = formals.Count - nRequired - nKey - (hasRest ? 1 : 0);
            Expression lambda = new LambdaExpression(formals, defaults, nOptional, hasRest, nKey, body!, loc);
            // Store the lambda expression for later compilation
            name.setExpression(lambda, 0, loc);
            return true;
        }
    }

    private bool doDefineUnit()
    {
        // TODO: implement unit definition
        return skipForm();
    }

    private bool doElement()
    {
        // TODO: implement element rule
        return skipForm();
    }

    private bool doOrElement()
    {
        // TODO: implement or-element rule
        return skipForm();
    }

    private bool doDefault()
    {
        // TODO: implement default rule
        return skipForm();
    }

    private bool doRoot()
    {
        // TODO: implement root rule
        return skipForm();
    }

    private bool doId()
    {
        // TODO: implement id rule
        return skipForm();
    }

    private bool doMode()
    {
        // TODO: implement mode definition
        return skipForm();
    }

    private bool doDeclareInitialValue()
    {
        // TODO: implement declare-initial-value
        return skipForm();
    }

    private bool doDeclareCharacteristic()
    {
        // TODO: implement declare-characteristic
        return skipForm();
    }

    private bool skipForm()
    {
        int depth = 1;
        Token tok;
        while (depth > 0)
        {
            if (!getToken(TokenAllow.OpenParen | TokenAllow.CloseParen | TokenAllow.Expr | TokenAllow.EndOfEntity, out tok))
                return false;
            if (tok == Token.EndOfEntity)
                return false;
            if (tok == Token.OpenParen)
                depth++;
            else if (tok == Token.CloseParen)
                depth--;
        }
        return true;
    }

    private bool expectCloseParen()
    {
        Token tok;
        if (!getToken(TokenAllow.CloseParen, out tok))
            return false;
        return tok == Token.CloseParen;
    }

    // Lexer
    private bool getToken(TokenAllow allowed, out Token tok)
    {
        tok = Token.EndOfEntity;
        if (in_ == null)
            return false;

        skipWhitespaceAndComments();

        Xchar c = getChar();
        if (c < 0)
        {
            tok = Token.EndOfEntity;
            return (allowed & TokenAllow.EndOfEntity) != 0;
        }

        switch (c)
        {
            case '(':
                tok = Token.OpenParen;
                return (allowed & TokenAllow.OpenParen) != 0;
            case ')':
                tok = Token.CloseParen;
                return (allowed & TokenAllow.CloseParen) != 0;
            case '.':
                tok = Token.Period;
                return (allowed & TokenAllow.Period) != 0;
            case '\'':
                tok = Token.Quote;
                return (allowed & TokenAllow.OtherExpr) != 0;
            case '`':
                tok = Token.Quasiquote;
                return (allowed & TokenAllow.QuasiquoteKey) != 0;
            case ',':
                c = getChar();
                if (c == '@')
                {
                    tok = Token.UnquoteSplicing;
                    return (allowed & TokenAllow.UnquoteSplicing) != 0;
                }
                ungetChar(c);
                tok = Token.Unquote;
                return (allowed & TokenAllow.Unquote) != 0;
            case '"':
                return scanString(allowed, out tok);
            case '#':
                return handleHash(allowed, out tok);
            case ';':
                skipComment();
                return getToken(allowed, out tok);
            default:
                if (isDigit((Char)c) || (c == '-' || c == '+'))
                {
                    ungetChar(c);
                    return handleNumber(allowed, out tok);
                }
                if (isInitial((Char)c))
                {
                    ungetChar(c);
                    return handleIdentifier(allowed, out tok);
                }
                return false;
        }
    }

    private bool handleHash(TokenAllow allowed, out Token tok)
    {
        tok = Token.EndOfEntity;
        Xchar c = getChar();
        switch (c)
        {
            case 't':
            case 'T':
                tok = Token.True;
                return (allowed & TokenAllow.OtherExpr) != 0 || (allowed & TokenAllow.False) != 0;
            case 'f':
            case 'F':
                tok = Token.False;
                return (allowed & TokenAllow.False) != 0;
            case '\\':
                return handleCharacter(allowed, out tok);
            case '!':
                return handleHashExclaim(allowed, out tok);
            case '(':
                tok = Token.Vector;
                return (allowed & TokenAllow.Vector) != 0;
            default:
                return false;
        }
    }

    private bool handleCharacter(TokenAllow allowed, out Token tok)
    {
        tok = Token.Char;
        currentToken_.clear();
        Xchar c = getChar();
        if (c < 0)
            return false;
        currentToken_.operatorPlusAssign((Char)c);
        // Check for named character
        if (isLetter((Char)c))
        {
            while (true)
            {
                c = getChar();
                if (c < 0 || !isSubsequent((Char)c))
                {
                    if (c >= 0)
                        ungetChar(c);
                    break;
                }
                currentToken_.operatorPlusAssign((Char)c);
            }
            if (currentToken_.size() > 1)
            {
                // Named character like #\newline
                string name = currentToken_.ToString().ToLower();
                if (name == "space")
                    currentToken_ = new StringC(new Char[] { ' ' }, 1);
                else if (name == "newline")
                    currentToken_ = new StringC(new Char[] { '\n' }, 1);
                else if (name == "tab")
                    currentToken_ = new StringC(new Char[] { '\t' }, 1);
            }
        }
        return (allowed & TokenAllow.OtherExpr) != 0;
    }

    private bool handleHashExclaim(TokenAllow allowed, out Token tok)
    {
        tok = Token.EndOfEntity;
        currentToken_.clear();
        // Read keyword
        while (true)
        {
            Xchar c = getChar();
            if (c < 0 || !isSubsequent((Char)c))
            {
                if (c >= 0)
                    ungetChar(c);
                break;
            }
            currentToken_.operatorPlusAssign((Char)c);
        }
        string kw = currentToken_.ToString().ToLower();
        if (kw == "optional")
        {
            tok = Token.HashOptional;
            return (allowed & TokenAllow.HashOptional) != 0;
        }
        if (kw == "rest")
        {
            tok = Token.HashRest;
            return (allowed & TokenAllow.HashRest) != 0;
        }
        if (kw == "key")
        {
            tok = Token.HashKey;
            return (allowed & TokenAllow.HashKey) != 0;
        }
        if (kw == "contents")
        {
            tok = Token.HashContents;
            return (allowed & TokenAllow.HashContents) != 0;
        }
        return false;
    }

    private bool handleNumber(TokenAllow allowed, out Token tok)
    {
        tok = Token.Number;
        currentToken_.clear();
        bool hasDigit = false;

        Xchar c = getChar();
        if (c == '-' || c == '+')
        {
            currentToken_.operatorPlusAssign((Char)c);
            c = getChar();
        }

        while (c >= 0 && isDigit((Char)c))
        {
            currentToken_.operatorPlusAssign((Char)c);
            hasDigit = true;
            c = getChar();
        }

        if (c == '.')
        {
            currentToken_.operatorPlusAssign((Char)c);
            c = getChar();
            while (c >= 0 && isDigit((Char)c))
            {
                currentToken_.operatorPlusAssign((Char)c);
                hasDigit = true;
                c = getChar();
            }
        }

        if (c >= 0)
            ungetChar(c);

        if (!hasDigit)
        {
            // Must be identifier starting with +/-
            tok = Token.Identifier;
            return handleIdentifier(allowed, out tok);
        }

        return (allowed & TokenAllow.OtherExpr) != 0;
    }

    private bool handleIdentifier(TokenAllow allowed, out Token tok)
    {
        tok = Token.Identifier;
        currentToken_.clear();

        Xchar c = getChar();
        while (c >= 0 && isSubsequent((Char)c))
        {
            currentToken_.operatorPlusAssign((Char)c);
            c = getChar();
        }

        if (c >= 0)
            ungetChar(c);

        // Check for keyword
        if (currentToken_.size() > 0 && currentToken_[currentToken_.size() - 1] == ':')
        {
            tok = Token.Keyword;
            return (allowed & TokenAllow.Keyword) != 0;
        }

        return (allowed & TokenAllow.Identifier) != 0;
    }

    private bool scanString(TokenAllow allowed, out Token tok)
    {
        tok = Token.String;
        currentToken_.clear();

        while (true)
        {
            Xchar c = getChar();
            if (c < 0)
                return false;
            if (c == '"')
                break;
            if (c == '\\')
            {
                c = getChar();
                if (c < 0)
                    return false;
                switch (c)
                {
                    case 'n':
                        currentToken_.operatorPlusAssign((Char)'\n');
                        break;
                    case 't':
                        currentToken_.operatorPlusAssign((Char)'\t');
                        break;
                    case 'r':
                        currentToken_.operatorPlusAssign((Char)'\r');
                        break;
                    case '\\':
                    case '"':
                        currentToken_.operatorPlusAssign((Char)c);
                        break;
                    default:
                        currentToken_.operatorPlusAssign((Char)c);
                        break;
                }
            }
            else
            {
                currentToken_.operatorPlusAssign((Char)c);
            }
        }

        return (allowed & TokenAllow.String) != 0;
    }

    private void skipWhitespaceAndComments()
    {
        while (true)
        {
            Xchar c = getChar();
            if (c < 0)
                return;
            if (c == ';')
            {
                skipComment();
                continue;
            }
            if (!isWhitespace((Char)c))
            {
                ungetChar(c);
                return;
            }
        }
    }

    private void skipComment()
    {
        while (true)
        {
            Xchar c = getChar();
            if (c < 0 || c == '\n')
                return;
        }
    }

    private static bool isWhitespace(Char c)
    {
        return c == ' ' || c == '\t' || c == '\n' || c == '\r';
    }

    private static bool isDigit(Char c)
    {
        return c >= '0' && c <= '9';
    }

    private static bool isLetter(Char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    }

    private static bool isInitial(Char c)
    {
        return isLetter(c) || isSpecialInitial(c);
    }

    private static bool isSpecialInitial(Char c)
    {
        return c == '!' || c == '$' || c == '%' || c == '&' || c == '*' ||
               c == '/' || c == ':' || c == '<' || c == '=' || c == '>' ||
               c == '?' || c == '^' || c == '_' || c == '~' || c == '+' || c == '-';
    }

    private static bool isSubsequent(Char c)
    {
        return isInitial(c) || isDigit(c) || c == '.' || c == '+' || c == '-';
    }

    private Identifier lookup(StringC str)
    {
        return interp_.lookup(str);
    }

    // Messenger implementation
    public override void dispatchMessage(Message msg)
    {
        interp_.dispatchMessage(msg);
    }

    public override void initMessage(Message msg)
    {
        interp_.initMessage(msg);
    }

    private void message(InterpreterMessages msg)
    {
        interp_.message(msg);
    }

    private void message(InterpreterMessages msg, string arg)
    {
        interp_.message(msg, arg);
    }
}

// Placeholder for LangObj (language object for collation)
public class LangObj : LanguageObj
{
}
