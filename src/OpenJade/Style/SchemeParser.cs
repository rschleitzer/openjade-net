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
#pragma warning disable CS0414 // Field assigned but never used (port stubs)
    private string? afiiPublicId_;
    private bool dsssl2_;
    private LangObj? lang_;
#pragma warning restore CS0414
    private Xchar pushedBack_ = noChar;  // One-character pushback buffer

    public SchemeParser(Interpreter interp, InputSource input)
    {
        interp_ = interp;
        in_ = input;
        currentToken_ = new StringC();
        defMode_ = interp.initialProcessingMode();
        dsssl2_ = interp.dsssl2();
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
        // Main parsing entry point - matches original openjade recovery behavior
        bool recovering = false;
        for (;;)
        {
            Token tok;
            // When recovering, accept all tokens to skip past bad forms
            TokenAllow allowed = recovering
                ? (TokenAllow)~0
                : TokenAllow.OpenParen | TokenAllow.EndOfEntity;
            if (!getToken(allowed, out tok))
            {
                recovering = true;
                continue;
            }
            if (tok == Token.EndOfEntity)
                break;
            if (tok != Token.OpenParen)
            {
                // In recovering mode, skip non-openParen tokens
                recovering = true;
                continue;
            }
            // Try to parse the top-level form (which will get the identifier)
            bool success = parseTopLevel();
            if (!success)
            {
                // Parsing failed - skip the rest of this form
                skipForm();
            }
            recovering = !success;
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
                    case Identifier.SyntacticKey.declareFlowObjectClass:
                        return doDeclareFlowObjectClass();
                    case Identifier.SyntacticKey.declareDefaultLanguage:
                        return doDeclareDefaultLanguage();
                    case Identifier.SyntacticKey.defineLanguage:
                        return doDefineLanguage();
                    default:
                        break;
                }
            }
            // Extension forms like make-afii, gen, glyph-subst-table are OpenJade extensions
            // that we don't support - skip them silently
            string formName = currentToken_.ToString();
            if (formName != "make-afii" && formName != "gen" && formName != "glyph-subst-table")
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

            interp_.addStandardChar(name, currentToken_);
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
        // (map-sdata-entity entity-name entity-text char-name)
        Token tok;
        if (!getToken(TokenAllow.Identifier | TokenAllow.EndOfEntity, out tok) || tok == Token.EndOfEntity)
        {
            message(InterpreterMessages.badDeclaration);
            return;
        }

        // currentToken_ now contains the character name
        // A full implementation would look up the character in the named character table
        // and add the sdata entity mapping
        interp_.addSdataEntity(name, text, currentToken_);
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
                    // Parse number with optional unit suffix
                    long val = 0;
                    bool negative = false;
                    bool seenDecimal = false;
                    int decimalPlaces = 0;
                    nuint i = 0;

                    // Check for sign
                    if (i < currentToken_.size() && (currentToken_[i] == '-' || currentToken_[i] == '+'))
                    {
                        negative = currentToken_[i] == '-';
                        i++;
                    }

                    // Parse integer/decimal part
                    for (; i < currentToken_.size(); i++)
                    {
                        Char c = currentToken_[i];
                        if (c >= '0' && c <= '9')
                        {
                            val = val * 10 + (c - '0');
                            if (seenDecimal)
                                decimalPlaces++;
                        }
                        else if (c == '.')
                        {
                            seenDecimal = true;
                        }
                        else
                        {
                            // Not a digit - must be start of unit suffix
                            break;
                        }
                    }

                    if (negative)
                        val = -val;

                    // Check for unit suffix
                    if (i < currentToken_.size())
                    {
                        StringC unitName = new StringC();
                        for (; i < currentToken_.size(); i++)
                            unitName.operatorPlusAssign(currentToken_[i]);

                        long unitValue = interp_.lookupUnit(unitName);
                        if (unitValue != 0)
                        {
                            // Scale by decimal places if any
                            while (decimalPlaces > 0)
                            {
                                unitValue /= 10;
                                decimalPlaces--;
                            }
                            long lengthVal = val * unitValue;
                            expr = new ConstantExpression(new LengthObj(lengthVal), in_?.currentLocation() ?? new Location());
                            return true;
                        }
                        // Unknown unit - fall through to integer
                    }

                    // No unit or unknown unit - return as integer (scaled by decimal places)
                    while (decimalPlaces > 0)
                    {
                        val /= 10;
                        decimalPlaces--;
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
                return parseQuotedDatum(out expr);  // Shorthand 'datum - no close paren
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
            case Token.CloseParen:
                // CloseParen can be allowed to signal end of a list - expr stays null
                return true;
            case Token.Keyword:
                // Keyword token - expr stays null, tok is set
                return true;
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
                    case Identifier.SyntacticKey.thereExists:
                        return parseSpecialQuery(out expr, "node-list-some?");
                    case Identifier.SyntacticKey.forAll:
                        return parseSpecialQuery(out expr, "node-list-every?");
                    case Identifier.SyntacticKey.selectEach:
                        return parseSpecialQuery(out expr, "node-list-filter");
                    case Identifier.SyntacticKey.unionForEach:
                        return parseSpecialQuery(out expr, "node-list-union-map");
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

        // Expression in function position - tok is already OpenParen, meaning we've
        // consumed the '(' so we need to parse the inner call directly
        Expression? funcExpr;
        if (!parseCall(out funcExpr))
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
        // (quote datum) form - expects close paren
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

    private bool parseQuotedDatum(out Expression? expr)
    {
        // 'datum shorthand form - no close paren needed
        expr = null;
        ELObj? datum;
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;
        if (!parseDatum(TokenAllow.Expr, out datum, out loc, out tok))
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
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        // Collect all cond clauses first, then build the nested if structure
        var clauses = new System.Collections.Generic.List<(Expression test, Expression result, bool isElse)>();

        for (;;)
        {
            if (!getToken(TokenAllow.OpenParen | TokenAllow.CloseParen, out tok))
                return false;
            if (tok == Token.CloseParen)
                break;

            // Parse clause (test expr...)
            Identifier.SyntacticKey key;
            Expression? test;
            if (!parseExpression(TokenAllow.Expr | TokenAllow.KeyElse, out test, out key, out tok))
                return false;

            bool isElse = false;
            if (tok == Token.Identifier)
            {
                Identifier ident = lookup(currentToken_);
                if (ident.syntacticKey(out key) && key == Identifier.SyntacticKey.elseKey)
                    isElse = true;
            }

            // Parse the result expression(s)
            System.Collections.Generic.List<Expression> results = new System.Collections.Generic.List<Expression>();
            for (;;)
            {
                Expression? result;
                if (!parseExpression(TokenAllow.Expr | TokenAllow.CloseParen, out result, out key, out tok))
                    return false;
                if (tok == Token.CloseParen)
                    break;
                if (result != null)
                    results.Add(result);
            }

            Expression? consequent;
            if (results.Count == 0)
                consequent = test;  // (cond (test)) returns test if true
            else if (results.Count == 1)
                consequent = results[0];
            else
                consequent = new SequenceExpression(results, loc);

            clauses.Add((test!, consequent!, isElse));

            if (isElse)
            {
                // Skip to close paren after else clause
                if (!expectCloseParen())
                    return false;
                break;
            }
        }

        // Build nested if expressions from last to first
        // (cond (t1 r1) (t2 r2) (else r3)) => (if t1 r1 (if t2 r2 r3))
        Expression? alternate = new CondFailExpression(loc);  // Default if no else clause

        for (int i = clauses.Count - 1; i >= 0; i--)
        {
            var (test, result, isElse) = clauses[i];
            if (isElse)
            {
                alternate = result;
            }
            else
            {
                expr = new IfExpression(test, result, alternate, loc);
                alternate = expr;
            }
        }

        if (expr == null)
            expr = alternate;  // Handle empty cond or only else clause

        return true;
    }

    private bool parseCase(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;
        Identifier.SyntacticKey key;

        // Parse the key expression
        Expression? keyExpr;
        if (!parseExpression(TokenAllow.Expr, out keyExpr, out key, out tok))
        {
            return false;
        }

        var cases = new System.Collections.Generic.List<CaseExpression.Case>();
        Expression? elseExpr = null;

        // Parse clauses
        for (;;)
        {
            if (!getToken(TokenAllow.OpenParen | TokenAllow.CloseParen, out tok))
            {
                return false;
            }
            if (tok == Token.CloseParen)
                break;

            // Parse clause - either ((datums...) expr...) or (else expr...)
            if (!getToken(TokenAllow.OpenParen | TokenAllow.Identifier, out tok))
            {
                return false;
            }

            if (tok == Token.Identifier)
            {
                Identifier ident = lookup(currentToken_);
                if (ident.syntacticKey(out key) && key == Identifier.SyntacticKey.elseKey)
                {
                    // else clause
                    if (!parseBeginUntilClose(out elseExpr))
                        return false;
                    continue;
                }
                // Not else - put it back and treat as datum list error
                return false;
            }

            // Parse datums list
            var datums = new System.Collections.Generic.List<ELObj?>();
            for (;;)
            {
                ELObj? datum;
                Location datumLoc;
                if (!parseDatum(TokenAllow.Expr | TokenAllow.CloseParen, out datum, out datumLoc, out tok))
                {
                    return false;
                }
                if (tok == Token.CloseParen)
                    break;
                datums.Add(datum);
            }

            // Parse result expressions
            Expression? result;
            if (!parseBeginUntilClose(out result))
            {
                return false;
            }

            var caseItem = new CaseExpression.Case
            {
                datums = datums,
                expr = result!
            };
            cases.Add(caseItem);
        }
        expr = new CaseExpression(keyExpr!, cases, elseExpr, loc);
        return true;
    }

    private bool parseBeginUntilClose(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        var exprs = new System.Collections.Generic.List<Expression>();
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
        System.Collections.Generic.List<Identifier?> vars = new System.Collections.Generic.List<Identifier?>();
        System.Collections.Generic.List<Expression> inits = new System.Collections.Generic.List<Expression>();
        Expression? body;

        // Check for named let: (let name ((var init) ...) body)
        Token tok;
        if (!getToken(TokenAllow.OpenParen | TokenAllow.Identifier, out tok))
            return false;

        Identifier? name = null;
        if (tok == Token.Identifier)
        {
            // Named let - save the name and parse bindings
            name = lookup(currentToken_);
            if (!parseBindingsAndBody(vars, inits, out body))
                return false;
        }
        else
        {
            // Regular let - token is already open paren, parse bindings directly
            if (!parseBindingsAndBody1(vars, inits, out body))
                return false;
        }

        if (name != null)
        {
            // Named let: (let loop ((x 1)) body) becomes
            // ((letrec ((loop (lambda (x) body))) loop) 1)
            System.Collections.Generic.List<Identifier?> loopFormal = new System.Collections.Generic.List<Identifier?> { name };
            System.Collections.Generic.List<Expression?> emptyInits = new System.Collections.Generic.List<Expression?>();
            System.Collections.Generic.List<Expression> loopInit = new System.Collections.Generic.List<Expression>
            {
                new LambdaExpression(vars, emptyInits, 0, false, 0, body!, loc)
            };
            Expression loopRef = new VariableExpression(name, loc);
            expr = new LetrecExpression(loopFormal, loopInit, loopRef, loc);
            expr = new CallExpression(expr, inits, loc);
        }
        else
        {
            expr = new LetExpression(vars, inits, body!, loc);
        }
        return true;
    }

    // Parse bindings and body after the open paren has been consumed
    private bool parseBindingsAndBody1(System.Collections.Generic.List<Identifier?> vars, System.Collections.Generic.List<Expression> inits, out Expression? body)
    {
        body = null;
        Token tok;

        // Parse each binding (var init) until we hit close paren
        for (;;)
        {
            if (!getToken(TokenAllow.OpenParen | TokenAllow.CloseParen, out tok))
                return false;
            if (tok == Token.CloseParen)
                break;

            // Parse binding (var init)
            if (!getToken(TokenAllow.Identifier, out tok))
                return false;
            vars.Add(lookup(currentToken_));

            Identifier.SyntacticKey key;
            Expression? init;
            if (!parseExpression(TokenAllow.Expr, out init, out key, out tok))
                return false;
            if (init != null)
                inits.Add(init);
            else
                return false;

            if (!getToken(TokenAllow.CloseParen, out tok))
                return false;
        }

        // Parse body expressions
        System.Collections.Generic.List<Expression> bodyExprs = new System.Collections.Generic.List<Expression>();
        for (;;)
        {
            Identifier.SyntacticKey key;
            Expression? e;
            if (!parseExpression(TokenAllow.Expr | TokenAllow.CloseParen, out e, out key, out tok))
                return false;
            if (tok == Token.CloseParen)
                break;
            if (e != null)
                bodyExprs.Add(e);
        }

        if (bodyExprs.Count == 0)
            body = new ConstantExpression(interp_.makeUnspecified(), new Location());
        else if (bodyExprs.Count == 1)
            body = bodyExprs[0];
        else
            body = new SequenceExpression(bodyExprs, new Location());

        return true;
    }

    private bool parseLetStar(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        System.Collections.Generic.List<Identifier?> vars = new System.Collections.Generic.List<Identifier?>();
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
        System.Collections.Generic.List<Identifier?> vars = new System.Collections.Generic.List<Identifier?>();
        System.Collections.Generic.List<Expression> inits = new System.Collections.Generic.List<Expression>();
        Expression? body;

        if (!parseBindingsAndBody(vars, inits, out body))
            return false;

        expr = new LetrecExpression(vars, inits, body!, loc);
        return true;
    }

    private bool parseBindingsAndBody(System.Collections.Generic.List<Identifier?> vars, System.Collections.Generic.List<Expression> inits, out Expression? body)
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

        System.Collections.Generic.List<Identifier?> formals = new System.Collections.Generic.List<Identifier?>();
        System.Collections.Generic.List<Expression?> defaults = new System.Collections.Generic.List<Expression?>();
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

    // Formal types for argument parsing
    private const int formalRequired = 0;
    private const int formalOptional = 1;
    private const int formalRest = 2;
    private const int formalKey = 3;

    private bool parseFormals(System.Collections.Generic.List<Identifier?> formals, System.Collections.Generic.List<Expression?> inits,
                              out int nRequired, out bool hasRest, out int nKey)
    {
        nRequired = 0;
        hasRest = false;
        nKey = 0;
        Token tok;

        int type = formalRequired;

        TokenAllow allowed = TokenAllow.CloseParen | TokenAllow.Identifier
                           | TokenAllow.HashOptional | TokenAllow.HashRest | TokenAllow.HashKey;

        int[] argCount = new int[4];

        for (;;)
        {
            if (!getToken(allowed, out tok))
                return false;

            switch (tok)
            {
                case Token.HashOptional:
                    allowed |= TokenAllow.OpenParen;
                    allowed &= ~TokenAllow.HashOptional;
                    type = formalOptional;
                    break;
                case Token.HashRest:
                    allowed = TokenAllow.Identifier;
                    type = formalRest;
                    break;
                case Token.HashKey:
                    allowed = TokenAllow.OpenParen | TokenAllow.CloseParen | TokenAllow.Identifier;
                    type = formalKey;
                    break;
                case Token.OpenParen:
                    {
                        // Parse (var init) form for optional/key args with defaults
                        if (!getToken(TokenAllow.Identifier, out tok))
                            return false;
                        argCount[type]++;
                        formals.Add(lookup(currentToken_));
                        // Resize inits to hold optional + key args
                        while (inits.Count < argCount[formalOptional] + argCount[formalKey])
                            inits.Add(null!);
                        // Parse init expression
                        Identifier.SyntacticKey key;
                        Expression? initExpr;
                        if (!parseExpression(TokenAllow.Expr, out initExpr, out key, out tok))
                            return false;
                        inits[inits.Count - 1] = initExpr!;
                        if (!getToken(TokenAllow.CloseParen, out tok))
                            return false;
                    }
                    break;
                case Token.Identifier:
                    {
                        Identifier formal = lookup(currentToken_);
                        formals.Add(formal);
                        argCount[type]++;
                        if (type == formalRest)
                            allowed = TokenAllow.HashKey | TokenAllow.CloseParen;
                        // For optional/key without init, add null placeholder
                        if (type == formalOptional || type == formalKey)
                        {
                            while (inits.Count < argCount[formalOptional] + argCount[formalKey])
                                inits.Add(null!);
                        }
                    }
                    break;
                case Token.CloseParen:
                    goto done;
                default:
                    return false;
            }
        }
    done:
        nRequired = argCount[formalRequired];
        int nOptional = argCount[formalOptional];
        nKey = argCount[formalKey];
        hasRest = argCount[formalRest] > 0;
        // Ensure inits has correct size
        while (inits.Count < nOptional + nKey)
            inits.Add(null!);
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
            {
                return false;
            }
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
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        // Parse variable name
        if (!getToken(TokenAllow.Identifier, out tok))
            return false;
        Identifier var = lookup(currentToken_);

        // Parse value expression
        Identifier.SyntacticKey key;
        Expression? value;
        if (!parseExpression(TokenAllow.Expr, out value, out key, out tok))
            return false;

        if (!expectCloseParen())
            return false;

        expr = new AssignmentExpression(var, value!, loc);
        return true;
    }

    // Parse special query syntax: (there-exists? var node-list body)
    // This transforms to: (node-list-some? (lambda (var) body) node-list)
    private bool parseSpecialQuery(out Expression? expr, string queryFuncName)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        // Parse variable name
        if (!getToken(TokenAllow.Identifier, out tok))
            return false;
        Identifier var = lookup(currentToken_);

        // Look up the query function
        Identifier queryFunc = interp_.lookup(interp_.makeStringC(queryFuncName));
        Expression opExpr = new VariableExpression(queryFunc, loc);

        // Parse node list expression
        Identifier.SyntacticKey key;
        Expression? nodeListExpr;
        if (!parseExpression(TokenAllow.Expr, out nodeListExpr, out key, out tok))
            return false;

        // Parse body expression
        Expression? bodyExpr;
        if (!parseExpression(TokenAllow.Expr, out bodyExpr, out key, out tok))
            return false;

        if (!expectCloseParen())
            return false;

        // Create lambda: (lambda (var) body)
        var formals = new System.Collections.Generic.List<Identifier?> { var };
        var defaults = new System.Collections.Generic.List<Expression?>();
        Expression lambdaExpr = new LambdaExpression(formals, defaults, 0, false, 0, bodyExpr!, loc);

        // Create call: (query-func lambda node-list)
        var args = new System.Collections.Generic.List<Expression> { lambdaExpr, nodeListExpr! };
        expr = new CallExpression(opExpr, args, loc);
        return true;
    }

    private bool parseMake(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        // Parse flow object class name
        if (!getToken(TokenAllow.Identifier, out tok))
            return false;
        Identifier foc = lookup(currentToken_);

        // Parse keywords and content expressions
        var keys = new System.Collections.Generic.List<Identifier?>();
        var exprs = new System.Collections.Generic.List<Expression>();

        for (;;)
        {
            Identifier.SyntacticKey key;
            Expression? e;
            if (!parseExpression(TokenAllow.Expr | TokenAllow.Keyword | TokenAllow.CloseParen, out e, out key, out tok))
                return false;

            if (tok == Token.CloseParen)
                break;

            if (tok == Token.Keyword)
            {
                // Remove trailing colon from keyword name
                StringC keyName = new StringC(currentToken_.data(), currentToken_.size() - 1);
                keys.Add(lookup(keyName));

                // Parse value expression
                if (!parseExpression(TokenAllow.Expr, out e, out key, out tok))
                    return false;
                exprs.Add(e!);
            }
            else if (e != null)
            {
                // Content expression - add only to exprs (not keys)
                // MakeExpression.compile expects content to be at the end of exprs_ beyond keys_.Count
                exprs.Add(e);
            }
        }

        expr = new MakeExpression(foc, keys, exprs, loc);
        return true;
    }

    private bool parseStyle(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        // Parse keyword/value pairs
        var keys = new System.Collections.Generic.List<Identifier?>();
        var exprs = new System.Collections.Generic.List<Expression>();

        for (;;)
        {
            Identifier.SyntacticKey key;
            Expression? e;
            if (!parseExpression(TokenAllow.Keyword | TokenAllow.CloseParen, out e, out key, out tok))
                return false;

            if (tok == Token.CloseParen)
                break;

            if (tok == Token.Keyword)
            {
                // Remove trailing colon from keyword name
                StringC keyName = new StringC(currentToken_.data(), currentToken_.size() - 1);
                keys.Add(lookup(keyName));

                // Parse value expression
                if (!parseExpression(TokenAllow.Expr, out e, out key, out tok))
                    return false;
                exprs.Add(e!);
            }
        }

        expr = new StyleExpression(keys, exprs, loc);
        return true;
    }

    private bool parseWithMode(out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        // Parse mode name
        if (!getToken(TokenAllow.Identifier, out tok))
            return false;
        StringC modeName = new StringC(currentToken_);
        ProcessingMode? mode = interp_.lookupProcessingMode(modeName);

        // Parse body expression
        Identifier.SyntacticKey key;
        Expression? body;
        if (!parseExpression(TokenAllow.Expr, out body, out key, out tok))
            return false;

        if (!expectCloseParen())
            return false;

        expr = new WithModeExpression(mode, body!, loc);
        return true;
    }

    private bool parseQuasiquote(out Expression? expr)
    {
        expr = null;
        return parseQuasiquoteBody(1, out expr);
    }

    private bool parseQuasiquoteBody(int level, out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        if (!getToken(TokenAllow.Expr | TokenAllow.OpenParen | TokenAllow.Unquote | TokenAllow.UnquoteSplicing | TokenAllow.QuasiquoteKey | TokenAllow.Vector, out tok))
            return false;

        if (tok == Token.Unquote)
        {
            if (level == 1)
            {
                // Unquote at level 1 - evaluate the expression
                Identifier.SyntacticKey key;
                if (!parseExpression(TokenAllow.Expr, out expr, out key, out tok))
                    return false;
                return true;
            }
            else
            {
                // Nested unquote - reduce level
                Expression? inner;
                if (!parseQuasiquoteBody(level - 1, out inner))
                    return false;
                var members = new System.Collections.Generic.List<Expression>();
                members.Add(new ConstantExpression(interp_.makeSymbol(interp_.makeStringC("unquote")), loc));
                members.Add(inner!);
                var spliced = new System.Collections.Generic.List<bool> { false, false };
                expr = new QuasiquoteExpression(members, spliced, QuasiquoteExpression.Type.listType, loc);
                return true;
            }
        }

        if (tok == Token.UnquoteSplicing)
        {
            // This should only appear within a list context
            message(InterpreterMessages.invalidUnquoteSplicing);
            return false;
        }

        if (tok == Token.Quasiquote)
        {
            // Nested quasiquote - increase level
            Expression? inner;
            if (!parseQuasiquoteBody(level + 1, out inner))
                return false;
            var members = new System.Collections.Generic.List<Expression>();
            members.Add(new ConstantExpression(interp_.makeSymbol(interp_.makeStringC("quasiquote")), loc));
            members.Add(inner!);
            var spliced = new System.Collections.Generic.List<bool> { false, false };
            expr = new QuasiquoteExpression(members, spliced, QuasiquoteExpression.Type.listType, loc);
            return true;
        }

        if (tok == Token.OpenParen)
        {
            return parseQuasiquoteList(level, QuasiquoteExpression.Type.listType, out expr);
        }

        if (tok == Token.Vector)
        {
            return parseQuasiquoteList(level, QuasiquoteExpression.Type.vectorType, out expr);
        }

        // Self-quoting datum
        switch (tok)
        {
            case Token.Number:
                {
                    ELObj? val = interp_.convertNumber(currentToken_);
                    if (val == null)
                    {
                        message(InterpreterMessages.invalidNumber);
                        return false;
                    }
                    expr = new ConstantExpression(val, loc);
                    return true;
                }
            case Token.String:
                expr = new ConstantExpression(interp_.makeString(currentToken_), loc);
                return true;
            case Token.Identifier:
                expr = new ConstantExpression(interp_.makeSymbol(currentToken_), loc);
                return true;
            case Token.True:
                expr = new ConstantExpression(interp_.makeTrue(), loc);
                return true;
            case Token.False:
                expr = new ConstantExpression(interp_.makeFalse(), loc);
                return true;
            case Token.Char:
                if (currentToken_.size() > 0)
                    expr = new ConstantExpression(interp_.makeChar(currentToken_[0]), loc);
                else
                    expr = new ConstantExpression(interp_.makeChar(0), loc);
                return true;
            default:
                return false;
        }
    }

    private bool parseQuasiquoteList(int level, QuasiquoteExpression.Type type, out Expression? expr)
    {
        expr = null;
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        var members = new System.Collections.Generic.List<Expression>();
        var spliced = new System.Collections.Generic.List<bool>();
        bool improper = false;

        for (;;)
        {
            if (!getToken(TokenAllow.Expr | TokenAllow.OpenParen | TokenAllow.CloseParen | TokenAllow.Period |
                          TokenAllow.Unquote | TokenAllow.UnquoteSplicing | TokenAllow.QuasiquoteKey | TokenAllow.Vector, out tok))
                return false;

            if (tok == Token.CloseParen)
                break;

            if (tok == Token.Period)
            {
                // Improper list
                if (type == QuasiquoteExpression.Type.vectorType)
                {
                    message(InterpreterMessages.badQuasiquote);
                    return false;
                }
                Expression? tail;
                if (!parseQuasiquoteBody(level, out tail))
                    return false;
                members.Add(tail!);
                spliced.Add(false);
                improper = true;
                if (!expectCloseParen())
                    return false;
                break;
            }

            if (tok == Token.UnquoteSplicing)
            {
                if (level == 1)
                {
                    // Spliced unquote
                    Identifier.SyntacticKey key;
                    Expression? e;
                    if (!parseExpression(TokenAllow.Expr, out e, out key, out tok))
                        return false;
                    members.Add(e!);
                    spliced.Add(true);
                }
                else
                {
                    // Nested - reduce level
                    Expression? inner;
                    if (!parseQuasiquoteBody(level - 1, out inner))
                        return false;
                    var subMembers = new System.Collections.Generic.List<Expression>();
                    subMembers.Add(new ConstantExpression(interp_.makeSymbol(interp_.makeStringC("unquote-splicing")), loc));
                    subMembers.Add(inner!);
                    var subSpliced = new System.Collections.Generic.List<bool> { false, false };
                    members.Add(new QuasiquoteExpression(subMembers, subSpliced, QuasiquoteExpression.Type.listType, loc));
                    spliced.Add(false);
                }
                continue;
            }

            if (tok == Token.Unquote)
            {
                if (level == 1)
                {
                    Identifier.SyntacticKey key;
                    Expression? e;
                    if (!parseExpression(TokenAllow.Expr, out e, out key, out tok))
                        return false;
                    members.Add(e!);
                    spliced.Add(false);
                }
                else
                {
                    Expression? inner;
                    if (!parseQuasiquoteBody(level - 1, out inner))
                        return false;
                    var subMembers = new System.Collections.Generic.List<Expression>();
                    subMembers.Add(new ConstantExpression(interp_.makeSymbol(interp_.makeStringC("unquote")), loc));
                    subMembers.Add(inner!);
                    var subSpliced = new System.Collections.Generic.List<bool> { false, false };
                    members.Add(new QuasiquoteExpression(subMembers, subSpliced, QuasiquoteExpression.Type.listType, loc));
                    spliced.Add(false);
                }
                continue;
            }

            if (tok == Token.Quasiquote)
            {
                Expression? inner;
                if (!parseQuasiquoteBody(level + 1, out inner))
                    return false;
                var subMembers = new System.Collections.Generic.List<Expression>();
                subMembers.Add(new ConstantExpression(interp_.makeSymbol(interp_.makeStringC("quasiquote")), loc));
                subMembers.Add(inner!);
                var subSpliced = new System.Collections.Generic.List<bool> { false, false };
                members.Add(new QuasiquoteExpression(subMembers, subSpliced, QuasiquoteExpression.Type.listType, loc));
                spliced.Add(false);
                continue;
            }

            if (tok == Token.OpenParen)
            {
                Expression? subList;
                if (!parseQuasiquoteList(level, QuasiquoteExpression.Type.listType, out subList))
                    return false;
                members.Add(subList!);
                spliced.Add(false);
                continue;
            }

            if (tok == Token.Vector)
            {
                Expression? subVec;
                if (!parseQuasiquoteList(level, QuasiquoteExpression.Type.vectorType, out subVec))
                    return false;
                members.Add(subVec!);
                spliced.Add(false);
                continue;
            }

            // Self-quoting datum
            Expression? elem;
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
                        elem = new ConstantExpression(interp_.makeInteger(val), loc);
                        break;
                    }
                case Token.String:
                    elem = new ConstantExpression(interp_.makeString(currentToken_), loc);
                    break;
                case Token.Identifier:
                    elem = new ConstantExpression(interp_.makeSymbol(currentToken_), loc);
                    break;
                case Token.True:
                    elem = new ConstantExpression(interp_.makeTrue(), loc);
                    break;
                case Token.False:
                    elem = new ConstantExpression(interp_.makeFalse(), loc);
                    break;
                case Token.Char:
                    if (currentToken_.size() > 0)
                        elem = new ConstantExpression(interp_.makeChar(currentToken_[0]), loc);
                    else
                        elem = new ConstantExpression(interp_.makeChar(0), loc);
                    break;
                default:
                    return false;
            }
            members.Add(elem);
            spliced.Add(false);
        }

        if (improper)
            type = QuasiquoteExpression.Type.improperType;

        expr = new QuasiquoteExpression(members, spliced, type, loc);
        return true;
    }

    private bool parseDatum(TokenAllow allowed, out ELObj? datum, out Location loc, out Token tok)
    {
        datum = null;
        loc = in_?.currentLocation() ?? new Location();
        tok = Token.EndOfEntity;

        if (!getToken(allowed, out tok))
        {
            return false;
        }

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
            case Token.Char:
                // Character literal like #\A
                datum = interp_.makeChar(currentToken_.size() > 0 ? currentToken_[0] : 0);
                return true;
            case Token.OpenParen:
                return parseDatumList(out datum, out loc);
            case Token.CloseParen:
                // CloseParen can be allowed to signal end of a list - datum stays null
                return true;
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

            System.Collections.Generic.List<Identifier?> formals = new System.Collections.Generic.List<Identifier?>();
            System.Collections.Generic.List<Expression?> defaults = new System.Collections.Generic.List<Expression?>();
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
            name.setExpression(lambda, 0, loc);
            return true;
        }
    }

    private bool doDefineUnit()
    {
        // (define-unit name value)
        // For now, just skip the form - full implementation requires unit evaluation
        return skipForm();
    }

    private bool doElement()
    {
        // (element gi-spec action)
        // gi-spec can be: name, (name ...), (or name ...), (match name (: name)*)
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        // Parse gi-spec (element name or list of names)
        var names = new System.Collections.Generic.List<StringC>();
        Pattern? pattern = null;

        if (!getToken(TokenAllow.Identifier | TokenAllow.OpenParen, out tok))
            return false;

        if (tok == Token.Identifier)
        {
            // Single element name
            names.Add(new StringC(currentToken_));
        }
        else
        {
            // List of names or match pattern
            if (!getToken(TokenAllow.Identifier, out tok))
                return false;

            Identifier.SyntacticKey key;
            Identifier ident = lookup(currentToken_);
            if (ident.syntacticKey(out key) && key == Identifier.SyntacticKey.match)
            {
                // Match pattern - parse element name and optional ancestors
                pattern = parsePattern();
                if (pattern == null)
                    return false;
            }
            else
            {
                // List of element names
                names.Add(new StringC(currentToken_));
                for (;;)
                {
                    if (!getToken(TokenAllow.Identifier | TokenAllow.CloseParen, out tok))
                        return false;
                    if (tok == Token.CloseParen)
                        break;
                    names.Add(new StringC(currentToken_));
                }
            }
        }

        // Parse action expression
        Identifier.SyntacticKey sk;
        Expression? action;
        if (!parseExpression(TokenAllow.Expr, out action, out sk, out tok))
            return false;

        if (!expectCloseParen())
            return false;

        // Register rule with defMode_
        if (pattern != null)
        {
            defMode_?.addRule(false, pattern, action!, 0, loc, interp_);
        }
        else
        {
            foreach (var name in names)
            {
                Pattern p = new ElementPattern(name);
                defMode_?.addRule(false, p, action!, 0, loc, interp_);
            }
        }

        return true;
    }

    private bool doOrElement()
    {
        // (or-element gi-spec action)
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        // Parse gi-spec
        var names = new System.Collections.Generic.List<StringC>();
        Pattern? pattern = null;

        if (!getToken(TokenAllow.Identifier | TokenAllow.OpenParen, out tok))
            return false;

        if (tok == Token.Identifier)
        {
            names.Add(new StringC(currentToken_));
        }
        else
        {
            if (!getToken(TokenAllow.Identifier, out tok))
                return false;

            Identifier.SyntacticKey key;
            Identifier ident = lookup(currentToken_);
            if (ident.syntacticKey(out key) && key == Identifier.SyntacticKey.match)
            {
                pattern = parsePattern();
                if (pattern == null)
                    return false;
            }
            else
            {
                names.Add(new StringC(currentToken_));
                for (;;)
                {
                    if (!getToken(TokenAllow.Identifier | TokenAllow.CloseParen, out tok))
                        return false;
                    if (tok == Token.CloseParen)
                        break;
                    names.Add(new StringC(currentToken_));
                }
            }
        }

        // Parse action expression
        Identifier.SyntacticKey sk;
        Expression? action;
        if (!parseExpression(TokenAllow.Expr, out action, out sk, out tok))
            return false;

        if (!expectCloseParen())
            return false;

        // Register as "or" rule (combines with existing rules)
        if (pattern != null)
        {
            defMode_?.addRule(true, pattern, action!, 0, loc, interp_);
        }
        else
        {
            foreach (var name in names)
            {
                Pattern p = new ElementPattern(name);
                defMode_?.addRule(true, p, action!, 0, loc, interp_);
            }
        }

        return true;
    }

    private bool doDefault()
    {
        // (default action)
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;
        Identifier.SyntacticKey key;

        // Parse action expression
        Expression? action;
        if (!parseExpression(TokenAllow.Expr, out action, out key, out tok))
            return false;

        if (!expectCloseParen())
            return false;

        // Register default rule
        Pattern defaultPattern = new DefaultPattern();
        defMode_?.addRule(false, defaultPattern, action!, 0, loc, interp_);

        return true;
    }

    private bool doRoot()
    {
        // (root action)
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;
        Identifier.SyntacticKey key;

        // Parse action expression
        Expression? action;
        if (!parseExpression(TokenAllow.Expr, out action, out key, out tok))
        {
            return false;
        }

        if (!expectCloseParen())
        {
            return false;
        }

        // Register root rule
        Pattern rootPattern = new RootPattern();
        defMode_?.addRule(false, rootPattern, action!, 0, loc, interp_);

        return true;
    }

    private bool doId()
    {
        // (id id-spec action)
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        // Parse id-spec (identifier name or list of names)
        var ids = new System.Collections.Generic.List<StringC>();

        if (!getToken(TokenAllow.Identifier | TokenAllow.OpenParen | TokenAllow.String, out tok))
            return false;

        if (tok == Token.Identifier || tok == Token.String)
        {
            ids.Add(new StringC(currentToken_));
        }
        else
        {
            // List of IDs
            for (;;)
            {
                if (!getToken(TokenAllow.Identifier | TokenAllow.String | TokenAllow.CloseParen, out tok))
                    return false;
                if (tok == Token.CloseParen)
                    break;
                ids.Add(new StringC(currentToken_));
            }
        }

        // Parse action expression
        Identifier.SyntacticKey key;
        Expression? action;
        if (!parseExpression(TokenAllow.Expr, out action, out key, out tok))
            return false;

        if (!expectCloseParen())
            return false;

        // Register id rules
        foreach (var id in ids)
        {
            Pattern idPattern = new IdPattern(id);
            defMode_?.addRule(false, idPattern, action!, 0, loc, interp_);
        }

        return true;
    }

    private bool doMode()
    {
        // (mode name body...)
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;

        // Parse mode name
        if (!getToken(TokenAllow.Identifier, out tok))
            return false;
        StringC modeName = new StringC(currentToken_);

        // Get or create the processing mode
        ProcessingMode? mode = interp_.lookupProcessingMode(modeName);
        if (mode == null)
        {
            mode = interp_.defineProcessingMode(modeName, loc);
        }

        // Save current default mode
        ProcessingMode? savedMode = defMode_;
        defMode_ = mode;

        // Parse body - can contain element, default, root, id rules
        for (;;)
        {
            if (!getToken(TokenAllow.OpenParen | TokenAllow.CloseParen, out tok))
            {
                defMode_ = savedMode;
                return false;
            }
            if (tok == Token.CloseParen)
                break;

            if (!getToken(TokenAllow.Identifier, out tok))
            {
                defMode_ = savedMode;
                return false;
            }

            Identifier ident = lookup(currentToken_);
            Identifier.SyntacticKey key;
            if (ident.syntacticKey(out key))
            {
                bool ok = true;
                switch (key)
                {
                    case Identifier.SyntacticKey.element:
                        ok = doElement();
                        break;
                    case Identifier.SyntacticKey.orElement:
                        ok = doOrElement();
                        break;
                    case Identifier.SyntacticKey.defaultEntity:
                        ok = doDefault();
                        break;
                    case Identifier.SyntacticKey.root:
                        ok = doRoot();
                        break;
                    case Identifier.SyntacticKey.id:
                        ok = doId();
                        break;
                    default:
                        message(InterpreterMessages.unknownTopLevelForm);
                        ok = skipForm();
                        break;
                }
                if (!ok)
                {
                    defMode_ = savedMode;
                    return false;
                }
            }
            else
            {
                message(InterpreterMessages.unknownTopLevelForm);
                if (!skipForm())
                {
                    defMode_ = savedMode;
                    return false;
                }
            }
        }

        defMode_ = savedMode;
        return true;
    }

    private Pattern? parsePattern()
    {
        // Parse (match element-name (: ancestor)*)
        Token tok;
        if (!getToken(TokenAllow.Identifier, out tok))
            return null;

        StringC elementName = new StringC(currentToken_);
        var ancestors = new System.Collections.Generic.List<StringC>();
        bool hasDirectParent = false;

        for (;;)
        {
            if (!getToken(TokenAllow.OpenParen | TokenAllow.CloseParen, out tok))
                return null;
            if (tok == Token.CloseParen)
                break;

            // Parse (: ancestor)
            if (!getToken(TokenAllow.Identifier, out tok))
                return null;

            // Check if it's a : token for direct parent
            if (currentToken_.size() == 1 && currentToken_[0] == ':')
                hasDirectParent = true;

            if (!getToken(TokenAllow.Identifier, out tok))
                return null;
            ancestors.Add(new StringC(currentToken_));

            if (!expectCloseParen())
                return null;
        }

        return new MatchPattern(elementName, ancestors, hasDirectParent);
    }

    private bool doDeclareInitialValue()
    {
        // (declare-initial-value name expression)
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;
        if (!getToken(TokenAllow.Identifier, out tok))
            return false;
        Identifier ident = lookup(currentToken_);
        Expression? expr;
        Identifier.SyntacticKey key;
        if (!parseExpression(TokenAllow.Expr, out expr, out key, out tok))
            return false;
        if (!expectCloseParen())
            return false;
        if (expr != null)
            interp_.installInitialValue(ident, expr);
        return true;
    }

    private bool doDeclareCharacteristic()
    {
        // (declare-characteristic name public-id expression)
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;
        if (!getToken(TokenAllow.Identifier, out tok))
            return false;
        Identifier ident = lookup(currentToken_);
        // Get the public-id (string or #f in DSSSL2)
        if (!getToken(TokenAllow.String | TokenAllow.False, out tok))
            return false;
        StringC pubid = new StringC();
        if (tok == Token.String)
            pubid.assign(currentToken_);
        // Parse the initial value expression
        Expression? expr;
        Identifier.SyntacticKey key;
        if (!parseExpression(TokenAllow.Expr, out expr, out key, out tok))
            return false;
        if (!expectCloseParen())
            return false;
        // Install the characteristic and its initial value
        interp_.installExtensionInheritedC(ident, pubid, loc);
        if (expr != null)
            interp_.installInitialValue(ident, expr);
        return true;
    }

    private bool doDeclareDefaultLanguage()
    {
        // (declare-default-language expression)
        // Parses an expression and sets it as the default language
        // For now, just skip the form - language support not yet implemented
        return skipForm();
    }

    private bool doDefineLanguage()
    {
        // (define-language name (toupper ...) (tolower ...))
        // Defines a language object with case conversion mappings
        // For now, just skip the form - language support not yet implemented
        return skipForm();
    }

    private bool doDeclareFlowObjectClass()
    {
        // (declare-flow-object-class name public-id)
        if (interp_.debugMode())
            Console.Error.WriteLine("doDeclareFlowObjectClass: starting");
        Location loc = in_?.currentLocation() ?? new Location();
        Token tok;
        if (!getToken(TokenAllow.Identifier, out tok))
            return false;
        Identifier ident = interp_.lookup(currentToken_);
        if (!getToken(TokenAllow.String, out tok))
            return false;
        if (interp_.debugMode())
            Console.Error.WriteLine($"doDeclareFlowObjectClass: ident={ident.name()}, pubid={currentToken_}");
        interp_.installExtensionFlowObjectClass(ident, currentToken_, loc);
        if (!getToken(TokenAllow.CloseParen, out tok))
            return false;
        return true;
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
        {
            return false;
        }

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

        // Handle exponent (e.g., 1e10, 1.5e-3)
        if (c == 'e' || c == 'E')
        {
            Xchar next = getChar();
            if (next >= 0 && (isDigit((Char)next) || next == '+' || next == '-'))
            {
                currentToken_.operatorPlusAssign((Char)c);
                currentToken_.operatorPlusAssign((Char)next);
                c = getChar();
                while (c >= 0 && isDigit((Char)c))
                {
                    currentToken_.operatorPlusAssign((Char)c);
                    c = getChar();
                }
            }
            else
            {
                if (next >= 0)
                    ungetChar(next);
            }
        }

        // Include unit suffix (e.g., pi, pt, cm, in, mm, pc)
        // Units are letters following the number
        while (c >= 0 && isLetter((Char)c))
        {
            currentToken_.operatorPlusAssign((Char)c);
            c = getChar();
        }

        if (c >= 0)
            ungetChar(c);

        if (!hasDigit)
        {
            // Must be identifier starting with +/- (like the - or + functions)
            // currentToken_ already contains the +/- character, so just return it as identifier
            tok = Token.Identifier;
            return (allowed & TokenAllow.Identifier) != 0;
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
            // Stop at end of entity, LF (0xA), or CR (0xD) - SGML uses CR as RE
            if (c < 0 || c == '\n' || c == '\r')
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

// LangObj is defined in LangObj.cs
