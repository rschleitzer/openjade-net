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

    // Node property name to ComponentName.Id mapping
    private Dictionary<string, ComponentName.Id> nodePropertyTable_ = new();

    // Extension table for backend-specific flow objects
    private FOTBuilder.ExtensionTableEntry[]? extensionTable_;

    // External procedure table (public ID -> FunctionObj)
    private Dictionary<string, FunctionObj> externalProcTable_ = new();

    // Counter for inherited characteristic indices
    private uint nInheritedC_ = 0;

    // Grove manager for loading external files
    private GroveManager? groveManager_;

    public Interpreter() : this(null, null) { }

    public Interpreter(GroveManager? groveManager, FOTBuilder.ExtensionTableEntry[]? extensionTable)
    {
        groveManager_ = groveManager;
        extensionTable_ = extensionTable;

        // Create the default/initial processing mode
        initialProcessingMode_ = new ProcessingMode(new StringC()); // Empty name = initial mode

        // Register syntactic keywords
        installSyntacticKey("define", Identifier.SyntacticKey.define);
        installSyntacticKey("define-unit", Identifier.SyntacticKey.defineUnit);
        installSyntacticKey("element", Identifier.SyntacticKey.element);
        installSyntacticKey("or-element", Identifier.SyntacticKey.orElement);
        installSyntacticKey("default", Identifier.SyntacticKey.defaultEntity);
        installSyntacticKey("root", Identifier.SyntacticKey.root);
        installSyntacticKey("id", Identifier.SyntacticKey.id);
        installSyntacticKey("mode", Identifier.SyntacticKey.mode);
        installSyntacticKey("declare-initial-value", Identifier.SyntacticKey.declareInitialValue);
        installSyntacticKey("declare-characteristic", Identifier.SyntacticKey.declareCharacteristic);
        installSyntacticKey("declare-flow-object-class", Identifier.SyntacticKey.declareFlowObjectClass);
        installSyntacticKey("declare-default-language", Identifier.SyntacticKey.declareDefaultLanguage);
        installSyntacticKey("define-language", Identifier.SyntacticKey.defineLanguage);
        installSyntacticKey("quote", Identifier.SyntacticKey.quote);
        installSyntacticKey("quasiquote", Identifier.SyntacticKey.quasiquote);
        installSyntacticKey("unquote", Identifier.SyntacticKey.unquote);
        installSyntacticKey("unquote-splicing", Identifier.SyntacticKey.unquoteSplicing);
        installSyntacticKey("if", Identifier.SyntacticKey.ifKey);
        installSyntacticKey("cond", Identifier.SyntacticKey.cond);
        installSyntacticKey("case", Identifier.SyntacticKey.caseKey);
        installSyntacticKey("and", Identifier.SyntacticKey.and);
        installSyntacticKey("or", Identifier.SyntacticKey.or);
        installSyntacticKey("let", Identifier.SyntacticKey.let);
        installSyntacticKey("let*", Identifier.SyntacticKey.letStar);
        installSyntacticKey("letrec", Identifier.SyntacticKey.letrec);
        installSyntacticKey("lambda", Identifier.SyntacticKey.lambda);
        installSyntacticKey("make", Identifier.SyntacticKey.make);
        installSyntacticKey("style", Identifier.SyntacticKey.style);
        installSyntacticKey("with-mode", Identifier.SyntacticKey.withMode);
        installSyntacticKey("use", Identifier.SyntacticKey.keyUse);
        installSyntacticKey("else", Identifier.SyntacticKey.elseKey);
        installSyntacticKey("=>", Identifier.SyntacticKey.arrowKey);
        installSyntacticKey("begin", Identifier.SyntacticKey.begin);
        installSyntacticKey("set!", Identifier.SyntacticKey.set);
        installSyntacticKey("there-exists?", Identifier.SyntacticKey.thereExists);
        installSyntacticKey("for-all?", Identifier.SyntacticKey.forAll);
        installSyntacticKey("select-each", Identifier.SyntacticKey.selectEach);
        installSyntacticKey("union-for-each", Identifier.SyntacticKey.unionForEach);
        installSyntacticKey("destination", Identifier.SyntacticKey.keyDestination);

        // Header/footer syntactic keys for simple-page-sequence
        installSyntacticKey("left-header", Identifier.SyntacticKey.keyLeftHeader);
        installSyntacticKey("center-header", Identifier.SyntacticKey.keyCenterHeader);
        installSyntacticKey("right-header", Identifier.SyntacticKey.keyRightHeader);
        installSyntacticKey("left-footer", Identifier.SyntacticKey.keyLeftFooter);
        installSyntacticKey("center-footer", Identifier.SyntacticKey.keyCenterFooter);
        installSyntacticKey("right-footer", Identifier.SyntacticKey.keyRightFooter);

        // Display NIC syntactic keys
        installSyntacticKey("position-preference", Identifier.SyntacticKey.keyPositionPreference);
        installSyntacticKey("keep", Identifier.SyntacticKey.keyKeep);
        installSyntacticKey("keep-with-previous?", Identifier.SyntacticKey.keyIsKeepWithPrevious);
        installSyntacticKey("keep-with-next?", Identifier.SyntacticKey.keyIsKeepWithNext);
        installSyntacticKey("break-before", Identifier.SyntacticKey.keyBreakBefore);
        installSyntacticKey("break-after", Identifier.SyntacticKey.keyBreakAfter);
        installSyntacticKey("may-violate-keep-before?", Identifier.SyntacticKey.keyIsMayViolateKeepBefore);
        installSyntacticKey("may-violate-keep-after?", Identifier.SyntacticKey.keyIsMayViolateKeepAfter);
        installSyntacticKey("space-before", Identifier.SyntacticKey.keySpaceBefore);
        installSyntacticKey("space-after", Identifier.SyntacticKey.keySpaceAfter);
        installSyntacticKey("break-before-priority", Identifier.SyntacticKey.keyBreakBeforePriority);
        installSyntacticKey("break-after-priority", Identifier.SyntacticKey.keyBreakAfterPriority);
        installSyntacticKey("coalesce-id", Identifier.SyntacticKey.keyCoalesceId);
        // External graphic and box characteristics
        installSyntacticKey("display?", Identifier.SyntacticKey.keyIsDisplay);
        installSyntacticKey("scale", Identifier.SyntacticKey.keyScale);
        installSyntacticKey("max-width", Identifier.SyntacticKey.keyMaxWidth);
        installSyntacticKey("max-height", Identifier.SyntacticKey.keyMaxHeight);
        installSyntacticKey("entity-system-id", Identifier.SyntacticKey.keyEntitySystemId);
        installSyntacticKey("notation-system-id", Identifier.SyntacticKey.keyNotationSystemId);
        installSyntacticKey("position-point-x", Identifier.SyntacticKey.keyPositionPointX);
        installSyntacticKey("position-point-y", Identifier.SyntacticKey.keyPositionPointY);
        installSyntacticKey("escapement-direction", Identifier.SyntacticKey.keyEscapementDirection);
        // Character flow object characteristics
        installSyntacticKey("char", Identifier.SyntacticKey.keyChar);
        installSyntacticKey("glyph-id", Identifier.SyntacticKey.keyGlyphId);
        installSyntacticKey("space?", Identifier.SyntacticKey.keyIsSpace);
        installSyntacticKey("record-end?", Identifier.SyntacticKey.keyIsRecordEnd);
        installSyntacticKey("input-tab?", Identifier.SyntacticKey.keyIsInputTab);
        installSyntacticKey("input-whitespace?", Identifier.SyntacticKey.keyIsInputWhitespace);
        installSyntacticKey("punct?", Identifier.SyntacticKey.keyIsPunct);
        installSyntacticKey("drop-after-line-break?", Identifier.SyntacticKey.keyIsDropAfterLineBreak);
        installSyntacticKey("drop-unless-before-line-break?", Identifier.SyntacticKey.keyIsDropUnlessBeforeLineBreak);
        installSyntacticKey("math-class", Identifier.SyntacticKey.keyMathClass);
        installSyntacticKey("math-font-posture", Identifier.SyntacticKey.keyMathFontPosture);
        installSyntacticKey("script", Identifier.SyntacticKey.keyScript);
        installSyntacticKey("stretch-factor", Identifier.SyntacticKey.keyStretchFactor);
        // Rule characteristics
        installSyntacticKey("orientation", Identifier.SyntacticKey.keyOrientation);
        installSyntacticKey("length", Identifier.SyntacticKey.keyLength);
        // Score characteristics
        installSyntacticKey("type", Identifier.SyntacticKey.keyType);
        // Table characteristics
        installSyntacticKey("before-row-border", Identifier.SyntacticKey.keyBeforeRowBorder);
        installSyntacticKey("after-row-border", Identifier.SyntacticKey.keyAfterRowBorder);
        installSyntacticKey("before-column-border", Identifier.SyntacticKey.keyBeforeColumnBorder);
        installSyntacticKey("after-column-border", Identifier.SyntacticKey.keyAfterColumnBorder);
        installSyntacticKey("column-number", Identifier.SyntacticKey.keyColumnNumber);
        installSyntacticKey("row-number", Identifier.SyntacticKey.keyRowNumber);
        installSyntacticKey("n-columns-spanned", Identifier.SyntacticKey.keyNColumnsSpanned);
        installSyntacticKey("n-rows-spanned", Identifier.SyntacticKey.keyNRowsSpanned);
        installSyntacticKey("width", Identifier.SyntacticKey.keyWidth);
        installSyntacticKey("starts-row?", Identifier.SyntacticKey.keyIsStartsRow);
        installSyntacticKey("ends-row?", Identifier.SyntacticKey.keyIsEndsRow);
        installSyntacticKey("table-width", Identifier.SyntacticKey.keyTableWidth);
        // Multi-mode characteristics
        installSyntacticKey("multi-modes", Identifier.SyntacticKey.keyMultiModes);
        installSyntacticKey("data", Identifier.SyntacticKey.keyData);

        // Install built-in units
        installUnits();

        // Install flow objects
        installFlowObjs();

        // Install node property name mappings
        installNodeProperties();

        // Install standard inherited characteristics
        installInheritedCs();

        // Install primitive procedures
        installPrimitives();

        // Install character name tables (non-strict mode)
        installCharNames();
        installSdata();

        // Install builtins from builtins.dsl
        installBuiltins();
    }

    private void installPrimitive(string name, PrimitiveObj prim)
    {
        Identifier ident = lookup(makeStringC(name));
        prim.setIdentifier(ident);
        ident.setDefinition(0, new Location(), prim);
        makePermanent(prim);
    }

    private void installFunction(string name, FunctionObj func)
    {
        Identifier ident = lookup(makeStringC(name));
        ident.setDefinition(0, new Location(), func);
        makePermanent(func);
    }

    private void installPrimitives()
    {
        // Core list primitives
        installPrimitive("cons", new ConsPrimitiveObj());
        installPrimitive("list", new ListPrimitiveObj());
        installPrimitive("null?", new IsNullPrimitiveObj());
        installPrimitive("list?", new IsListPrimitiveObj());
        installPrimitive("pair?", new IsPairPrimitiveObj());
        installPrimitive("car", new CarPrimitiveObj());
        installPrimitive("cdr", new CdrPrimitiveObj());
        installPrimitive("append", new AppendPrimitiveObj());
        installPrimitive("reverse", new ReversePrimitiveObj());
        installPrimitive("length", new LengthPrimitiveObj());
        installPrimitive("list-ref", new ListRefPrimitiveObj());
        installPrimitive("equal?", new IsEqualPrimitiveObj());
        installPrimitive("eqv?", new IsEqvPrimitiveObj());
        installPrimitive("memq", new MemqPrimitiveObj());
        installPrimitive("member", new MemberPrimitiveObj());
        installPrimitive("memv", new MemvPrimitiveObj());
        installPrimitive("assq", new AssqPrimitiveObj());

        // String primitives
        installPrimitive("string", new StringFromCharsPrimitiveObj());
        installPrimitive("substring", new SubstringPrimitiveObj());
        installPrimitive("string-append", new StringAppendPrimitiveObj());
        installPrimitive("string-length", new StringLengthPrimitiveObj());
        installPrimitive("string-ref", new StringRefPrimitiveObj());
        installPrimitive("string->list", new StringToListPrimitiveObj());
        installPrimitive("list->string", new ListToStringPrimitiveObj());
        installPrimitive("symbol->string", new SymbolToStringPrimitiveObj());
        installPrimitive("string=?", new StringEqualPrimitiveObj());
        installPrimitive("string<?", new StringLessPrimitiveObj());
        installPrimitive("string<=?", new StringLessEqualPrimitiveObj());
        installPrimitive("number->string", new NumberToStringPrimitiveObj());
        installPrimitive("string->number", new StringToNumberPrimitiveObj());
        installPrimitive("format-number", new FormatNumberPrimitiveObj());
        installPrimitive("format-number-list", new FormatNumberListPrimitiveObj());

        // Arithmetic primitives
        installPrimitive("+", new PlusPrimitiveObj());
        installPrimitive("-", new MinusPrimitiveObj());
        installPrimitive("*", new MultiplyPrimitiveObj());
        installPrimitive("/", new DividePrimitiveObj());
        installPrimitive("=", new EqualPrimitiveObj());
        installPrimitive("<", new LessPrimitiveObj());
        installPrimitive(">", new GreaterPrimitiveObj());
        installPrimitive("<=", new LessEqualPrimitiveObj());
        installPrimitive(">=", new GreaterEqualPrimitiveObj());
        installPrimitive("not", new NotPrimitiveObj());
        installPrimitive("floor", new FloorPrimitiveObj());
        installPrimitive("ceiling", new CeilingPrimitiveObj());
        installPrimitive("round", new RoundPrimitiveObj());
        installPrimitive("abs", new AbsPrimitiveObj());
        installPrimitive("sqrt", new SqrtPrimitiveObj());
        installPrimitive("odd?", new IsOddPrimitiveObj());
        installPrimitive("even?", new IsEvenPrimitiveObj());
        installPrimitive("zero?", new IsZeroPrimitiveObj());
        installPrimitive("positive?", new IsPositivePrimitiveObj());
        installPrimitive("negative?", new IsNegativePrimitiveObj());

        // Sosofo primitives
        installPrimitive("empty-sosofo", new EmptySosofoPrimitiveObj());
        installPrimitive("sosofo-append", new SosofoAppendPrimitiveObj());
        installPrimitive("process-children", new ProcessChildrenPrimitiveObj());
        installPrimitive("process-children-trim", new ProcessChildrenTrimPrimitiveObj());
        installPrimitive("page-number-sosofo", new PageNumberSosofoPrimitiveObj());
        installPrimitive("current-node-page-number-sosofo", new CurrentNodePageNumberSosofoPrimitiveObj());
        installPrimitive("literal", new LiteralPrimitiveObj());
        installPrimitive("next-match", new NextMatchPrimitiveObj());
        installPrimitive("merge-style", new MergeStylePrimitiveObj());
        installPrimitive("process-node-list", new ProcessNodeListPrimitiveObj());

        // Node primitives
        installPrimitive("current-node", new CurrentNodePrimitiveObj());
        installPrimitive("node-list-first", new NodeListFirstPrimitiveObj());
        installPrimitive("node-list-rest", new NodeListRestPrimitiveObj());
        installPrimitive("node-list-empty?", new IsNodeListEmptyPrimitiveObj());
        installPrimitive("node-list", new NodeListPrimitiveObj());
        installPrimitive("empty-node-list", new EmptyNodeListPrimitiveObj());
        installPrimitive("children", new ChildrenPrimitiveObj());
        installPrimitive("parent", new ParentPrimitiveObj());
        installPrimitive("descendants", new DescendantsPrimitiveObj());
        installPrimitive("ancestor", new AncestorPrimitiveObj());
        installPrimitive("select-elements", new SelectElementsPrimitiveObj());
        installPrimitive("node-list-length", new NodeListLengthPrimitiveObj());
        installPrimitive("node-list-ref", new NodeListRefPrimitiveObj());
        installPrimitive("node-list-reverse", new NodeListReversePrimitiveObj());
        installPrimitive("node-list=?", new NodeListEqualPrimitiveObj());
        installPrimitive("node-list->list", new NodeListToListPrimitiveObj());
        installPrimitive("node-list-map", new NodeListMapPrimitiveObj());
        installPrimitive("node-property", new NodePropertyPrimitiveObj());
        installPrimitive("follow", new FollowPrimitiveObj());
        installPrimitive("preced", new PrecedPrimitiveObj());
        installPrimitive("data", new DataPrimitiveObj());
        installPrimitive("attributes", new AttributesPrimitiveObj());
        installPrimitive("first-sibling?", new IsFirstSiblingPrimitiveObj());
        installPrimitive("last-sibling?", new IsLastSiblingPrimitiveObj());
        installPrimitive("absolute-first-sibling?", new IsAbsoluteFirstSiblingPrimitiveObj());
        installPrimitive("absolute-last-sibling?", new IsAbsoluteLastSiblingPrimitiveObj());

        // Note: node-list-some?, node-list-every?, node-list-filter, node-list-union-map
        // are defined in builtins.dsl (not as primitives), used by the special query syntax:
        // there-exists?, for-all?, select-each, union-for-each

        // Element access
        installPrimitive("gi", new GiPrimitiveObj());
        installPrimitive("general-name-normalize", new GeneralNameNormalizePrimitiveObj());
        installPrimitive("have-ancestor?", new HaveAncestorPrimitiveObj());
        installPrimitive("attribute-string", new AttributeStringPrimitiveObj());
        installPrimitive("inherited-attribute-string", new InheritedAttributeStringPrimitiveObj());
        installPrimitive("id", new IdPrimitiveObj());
        installPrimitive("element-with-id", new ElementWithIdPrimitiveObj());
        installPrimitive("child-number", new ChildNumberPrimitiveObj());
        installPrimitive("ancestor-child-number", new AncestorChildNumberPrimitiveObj());
        installPrimitive("hierarchical-number-recursive", new HierarchicalNumberRecursivePrimitiveObj());
        installPrimitive("element-number", new ElementNumberPrimitiveObj());
        installPrimitive("all-element-number", new AllElementNumberPrimitiveObj());
        installPrimitive("external-procedure", new ExternalProcedurePrimitiveObj());

        // Register external procedures by public ID (ISO/IEC 10179:1996//Procedure::)
        installExternalPrimitive("all-element-number", new AllElementNumberPrimitiveObj());

        // Register James Clark extension procedures (UNREGISTERED::James Clark//Procedure::)
        const string jcPrefix = "UNREGISTERED::James Clark//Procedure::";
        installXPrimitive(jcPrefix, "if-first-page", new IfFirstPagePrimitiveObj());
        installXPrimitive(jcPrefix, "if-front-page", new IfFrontPagePrimitiveObj());
        installXPrimitive(jcPrefix, "debug", new DebugPrimitiveObj());
        installXPrimitive(jcPrefix, "all-element-number", new AllElementNumberPrimitiveObj());
        installXPrimitive(jcPrefix, "read-entity", new ReadEntityPrimitiveObj());

        // Type predicates
        installPrimitive("string?", new IsStringPrimitiveObj());
        installPrimitive("number?", new IsNumberPrimitiveObj());
        installPrimitive("integer?", new IsIntegerPrimitiveObj());
        installPrimitive("real?", new IsRealPrimitiveObj());
        installPrimitive("procedure?", new IsProcedurePrimitiveObj());
        installPrimitive("boolean?", new IsBooleanPrimitiveObj());
        installPrimitive("symbol?", new IsSymbolPrimitiveObj());
        installPrimitive("keyword?", new IsKeywordPrimitiveObj());
        installPrimitive("char?", new IsCharPrimitiveObj());
        installPrimitive("char-upcase", new CharUpcasePrimitiveObj());
        installPrimitive("char-downcase", new CharDowncasePrimitiveObj());
        installPrimitive("char->integer", new CharToIntegerPrimitiveObj());
        installPrimitive("integer->char", new IntegerToCharPrimitiveObj());
        installPrimitive("char<?", new IsCharLessPrimitiveObj());
        installPrimitive("char<=?", new IsCharLessOrEqualPrimitiveObj());
        installPrimitive("char=?", new IsCharEqualPrimitiveObj());
        installPrimitive("node-list?", new IsNodeListPrimitiveObj());
        installPrimitive("sosofo?", new IsSosofoPrimitiveObj());
        installPrimitive("style?", new IsStylePrimitiveObj());
        installPrimitive("vector?", new IsVectorPrimitiveObj());
        installPrimitive("quantity?", new IsQuantityPrimitiveObj());
        installPrimitive("color?", new IsColorPrimitiveObj());
        installPrimitive("color-space?", new IsColorSpacePrimitiveObj());
        installPrimitive("address?", new IsAddressPrimitiveObj());

        // Vector primitives
        installPrimitive("vector", new VectorPrimitiveObj());
        installPrimitive("vector->list", new VectorToListPrimitiveObj());
        installPrimitive("list->vector", new ListToVectorPrimitiveObj());

        // Color primitives
        installPrimitive("color-space", new ColorSpacePrimitiveObj());
        installPrimitive("color", new ColorPrimitiveObj());

        // Address primitives
        installPrimitive("current-node-address", new CurrentNodeAddressPrimitiveObj());
        installPrimitive("idref-address", new IdrefAddressPrimitiveObj());
        installPrimitive("entity-address", new EntityAddressPrimitiveObj());
        installPrimitive("node-list-address", new NodeListAddressPrimitiveObj());

        // Special function primitives (extend FunctionObj instead of PrimitiveObj)
        installFunction("apply", new ApplyPrimitiveObj());
        installFunction("call-with-current-continuation", new CallWithCurrentContinuationPrimitiveObj());
    }

    private void installBuiltins()
    {
        if (groveManager_ == null)
            return;

        // Load builtins.dsl
        StringC sysid = makeStringC("builtins.dsl");
        groveManager_.mapSysid(ref sysid);
        if (groveManager_.readEntity(sysid, out StringC src))
        {
            InputSource inputSource = new InternalInputSource(src, InputSourceOrigin.make());
            SchemeParser scm = new SchemeParser(this, inputSource);
            scm.parse();
        }
    }

    private void installFlowObjs()
    {
        // Core flow objects
        installFlowObj("sequence", new SequenceFlowObj());
        installFlowObj("display-group", new DisplayGroupFlowObj());
        installFlowObj("paragraph", new ParagraphFlowObj());
        installFlowObj("paragraph-break", new ParagraphBreakFlowObj());
        installFlowObj("line-field", new LineFieldFlowObj());
        installFlowObj("score", new ScoreFlowObj());
        installFlowObj("external-graphic", new ExternalGraphicFlowObj());
        installFlowObj("rule", new RuleFlowObj());
        installFlowObj("leader", new LeaderFlowObj());
        installFlowObj("character", new CharacterFlowObj());
        installFlowObj("box", new BoxFlowObj());
        installFlowObj("alignment-point", new AlignmentPointFlowObj());
        installFlowObj("sideline", new SidelineFlowObj());

        // Simple page
        installFlowObj("simple-page-sequence", new SimplePageSequenceFlowObj());

        // Tables
        installFlowObj("table", new TableFlowObj());
        installFlowObj("table-part", new TablePartFlowObj());
        installFlowObj("table-column", new TableColumnFlowObj());
        installFlowObj("table-row", new TableRowFlowObj());
        installFlowObj("table-cell", new TableCellFlowObj());
        installFlowObj("table-border", new TableBorderFlowObj());

        // Online
        installFlowObj("link", new LinkFlowObj());
        installFlowObj("scroll", new ScrollFlowObj());
        installFlowObj("marginalia", new MarginaliaFlowObj());
        installFlowObj("multi-mode", new MultiModeFlowObj());

        // Math
        installFlowObj("math-sequence", new MathSequenceFlowObj());
        installFlowObj("fraction", new FractionFlowObj());
        installFlowObj("unmath", new UnmathFlowObj());
        installFlowObj("superscript", new SuperscriptFlowObj());
        installFlowObj("subscript", new SubscriptFlowObj());
        installFlowObj("script", new ScriptFlowObj());
        installFlowObj("mark", new MarkFlowObj());
        installFlowObj("fence", new FenceFlowObj());
        installFlowObj("radical", new RadicalFlowObj());
        installFlowObj("math-operator", new MathOperatorFlowObj());
        installFlowObj("grid", new GridFlowObj());
        installFlowObj("grid-cell", new GridCellFlowObj());
    }

    private void installNodeProperties()
    {
        // Install all node property names from ComponentName
        // Both rcsName (kebab-case) and sdqlName (lowercase) forms
        for (int i = 0; i < ComponentName.nIds; i++)
        {
            ComponentName.Id id = (ComponentName.Id)i;
            string? rcsName = ComponentName.rcsName(id);
            string? sdqlName = ComponentName.sdqlName(id);
            if (rcsName != null)
                nodePropertyTable_[rcsName] = id;
            if (sdqlName != null && sdqlName != rcsName)
                nodePropertyTable_[sdqlName] = id;
        }
    }

    private void installInheritedCs()
    {
        // Install standard DSSSL inherited characteristics
        // Names WITHOUT trailing colon - parseMake strips the colon from keywords
        // Default font-size: 10pt = 10*1000 units (1000 units per point)
        installInheritedC("font-size", new FontSizeC(null, nInheritedC_++, (unitsPerInch_ * 10) / 72));
        installInheritedC("font-family-name", new FontFamilyNameC(null, nInheritedC_++, makeStringC("iso-serif")));
        installInheritedC("font-weight", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setFontWeight(sym), FOTBuilder.Symbol.symbolMedium));
        installInheritedC("font-posture", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setFontPosture(sym), FOTBuilder.Symbol.symbolUpright));
        // Line spacing: 12pt default
        installInheritedC("line-spacing", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setLineSpacing(ls), (unitsPerInch_ * 12) / 72));
        // Margins
        installInheritedC("left-margin", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setLeftMargin(size), 0));
        installInheritedC("right-margin", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setRightMargin(size), 0));
        installInheritedC("top-margin", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setTopMargin(size), 0));
        installInheritedC("bottom-margin", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setBottomMargin(size), 0));
        // Indents
        installInheritedC("start-indent", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setStartIndent(ls), 0));
        installInheritedC("end-indent", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setEndIndent(ls), 0));
        installInheritedC("first-line-start-indent", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setFirstLineStartIndent(ls), 0));
        installInheritedC("last-line-end-indent", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setLastLineEndIndent(ls), 0));
        // Quadding
        installInheritedC("quadding", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setQuadding(sym), FOTBuilder.Symbol.symbolStart));
        installInheritedC("display-alignment", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setDisplayAlignment(sym), FOTBuilder.Symbol.symbolStart));
        installInheritedC("field-align", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setFieldAlign(sym), FOTBuilder.Symbol.symbolStart));
        installInheritedC("lines", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setLines(sym), FOTBuilder.Symbol.symbolWrap));
        installInheritedC("field-width", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setFieldWidth(ls), 0));
        // Page dimensions
        installInheritedC("page-width", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setPageWidth(size), unitsPerInch_ * 17 / 2)); // 8.5 inches
        installInheritedC("page-height", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setPageHeight(size), unitsPerInch_ * 11));   // 11 inches
        installInheritedC("header-margin", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setHeaderMargin(size), 0));
        installInheritedC("footer-margin", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setFooterMargin(size), 0));
        // Color - default black
        installInheritedC("color", new ColorC(null, nInheritedC_++, new DeviceRGBColorObj(new double[] { 0, 0, 0 }), this));
        // Background color - default #f (no background)
        installInheritedC("background-color", new BackgroundColorC(null, nInheritedC_++, null, this));
        // Border
        installInheritedC("border-present?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setBorderPresent(b), true));
        borderTrueStyle_ = makeBorderStyle(true, nInheritedC_ - 1);
        borderFalseStyle_ = makeBorderStyle(false, nInheritedC_ - 1);
        installInheritedC("line-thickness", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setLineThickness(size), unitsPerInch_ / 72));
        // Cell margins
        installInheritedC("cell-before-row-margin", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setCellBeforeRowMargin(size), 0));
        installInheritedC("cell-after-row-margin", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setCellAfterRowMargin(size), 0));
        installInheritedC("cell-before-column-margin", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setCellBeforeColumnMargin(size), 0));
        installInheritedC("cell-after-column-margin", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setCellAfterColumnMargin(size), 0));
        // Table/cell border characteristics (IgnoredC - just store value, don't set FOTBuilder)
        var tableBorderIC = new BorderC(null, nInheritedC_++, makeFalse(), this);
        installInheritedC("table-border", tableBorderIC);
        tableBorderC_ = new ConstPtr<InheritedC>(tableBorderIC);
        var cellBeforeRowBorderIC = new BorderC(null, nInheritedC_++, makeFalse(), this);
        installInheritedC("cell-before-row-border", cellBeforeRowBorderIC);
        cellBeforeRowBorderC_ = new ConstPtr<InheritedC>(cellBeforeRowBorderIC);
        var cellAfterRowBorderIC = new BorderC(null, nInheritedC_++, makeFalse(), this);
        installInheritedC("cell-after-row-border", cellAfterRowBorderIC);
        cellAfterRowBorderC_ = new ConstPtr<InheritedC>(cellAfterRowBorderIC);
        var cellBeforeColumnBorderIC = new BorderC(null, nInheritedC_++, makeFalse(), this);
        installInheritedC("cell-before-column-border", cellBeforeColumnBorderIC);
        cellBeforeColumnBorderC_ = new ConstPtr<InheritedC>(cellBeforeColumnBorderIC);
        var cellAfterColumnBorderIC = new BorderC(null, nInheritedC_++, makeFalse(), this);
        installInheritedC("cell-after-column-border", cellAfterColumnBorderIC);
        cellAfterColumnBorderC_ = new ConstPtr<InheritedC>(cellAfterColumnBorderIC);
        // Length specs
        installInheritedC("line-sep", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setLineSep(size), unitsPerInch_ / 72));
        installInheritedC("box-size-before", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setBoxSizeBefore(size), 8 * unitsPerInch_ / 72));
        installInheritedC("box-size-after", new GenericLengthInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long size) => fotb.setBoxSizeAfter(size), 4 * unitsPerInch_ / 72));
        installInheritedC("position-point-shift", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setPositionPointShift(ls), 0));
        installInheritedC("start-margin", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setStartMargin(ls), 0));
        installInheritedC("end-margin", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setEndMargin(ls), 0));
        installInheritedC("sideline-sep", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setSidelineSep(ls), 4 * unitsPerInch_ / 72));
        installInheritedC("asis-wrap-indent", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setAsisWrapIndent(ls), 0));
        installInheritedC("line-number-sep", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setLineNumberSep(ls), 0));
        installInheritedC("last-line-justify-limit", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setLastLineJustifyLimit(ls), 0));
        installInheritedC("justify-glyph-space-max-add", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setJustifyGlyphSpaceMaxAdd(ls), 0));
        installInheritedC("justify-glyph-space-max-remove", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setJustifyGlyphSpaceMaxRemove(ls), 0));
        installInheritedC("table-corner-radius", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setTableCornerRadius(ls), 3 * unitsPerInch_ / 72));
        installInheritedC("box-corner-radius", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setBoxCornerRadius(ls), 3 * unitsPerInch_ / 72));
        installInheritedC("marginalia-sep", new GenericLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.LengthSpec ls) => fotb.setMarginaliaSep(ls), 0));
        // Boolean characteristics
        installInheritedC("inhibit-line-breaks?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setInhibitLineBreaks(b), false));
        installInheritedC("hyphenate?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setHyphenate(b), false));
        installInheritedC("kern?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setKern(b), false));
        installInheritedC("ligature?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setLigature(b), false));
        installInheritedC("score-spaces?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setScoreSpaces(b), false));
        installInheritedC("float-out-sidelines?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setFloatOutSidelines(b), false));
        installInheritedC("float-out-marginalia?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setFloatOutMarginalia(b), false));
        installInheritedC("float-out-line-numbers?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setFloatOutLineNumbers(b), false));
        installInheritedC("cell-background?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setCellBackground(b), false));
        installInheritedC("span-weak?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setSpanWeak(b), false));
        installInheritedC("ignore-record-end?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setIgnoreRecordEnd(b), false));
        installInheritedC("numbered-lines?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setNumberedLines(b), true));
        installInheritedC("hanging-punct?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setHangingPunct(b), false));
        installInheritedC("box-open-end?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setBoxOpenEnd(b), false));
        installInheritedC("truncate-leader?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setTruncateLeader(b), false));
        installInheritedC("align-leader?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setAlignLeader(b), true));
        installInheritedC("table-part-omit-middle-header?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setTablePartOmitMiddleHeader(b), false));
        installInheritedC("table-part-omit-middle-footer?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setTablePartOmitMiddleFooter(b), false));
        installInheritedC("border-omit-at-break?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setBorderOmitAtBreak(b), false));
        installInheritedC("principal-mode-simultaneous?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setPrincipalModeSimultaneous(b), false));
        installInheritedC("marginalia-keep-with-previous?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setMarginaliaKeepWithPrevious(b), false));
        installInheritedC("grid-equidistant-rows?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setGridEquidistantRows(b), false));
        installInheritedC("grid-equidistant-columns?", new GenericBoolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, bool b) => fotb.setGridEquidistantColumns(b), false));
        // Symbol characteristics
        installInheritedC("line-join", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setLineJoin(sym), FOTBuilder.Symbol.symbolMiter));
        installInheritedC("line-cap", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setLineCap(sym), FOTBuilder.Symbol.symbolButt));
        installInheritedC("line-number-side", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setLineNumberSide(sym), FOTBuilder.Symbol.symbolStart));
        installInheritedC("kern-mode", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setKernMode(sym), FOTBuilder.Symbol.symbolNormal));
        installInheritedC("input-whitespace-treatment", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setInputWhitespaceTreatment(sym), FOTBuilder.Symbol.symbolPreserve));
        installInheritedC("filling-direction", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setFillingDirection(sym), FOTBuilder.Symbol.symbolTopToBottom));
        installInheritedC("writing-mode", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setWritingMode(sym), FOTBuilder.Symbol.symbolLeftToRight));
        installInheritedC("last-line-quadding", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setLastLineQuadding(sym), FOTBuilder.Symbol.symbolRelative));
        installInheritedC("math-display-mode", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setMathDisplayMode(sym), FOTBuilder.Symbol.symbolDisplay));
        installInheritedC("script-pre-align", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setScriptPreAlign(sym), FOTBuilder.Symbol.symbolIndependent));
        installInheritedC("script-post-align", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setScriptPostAlign(sym), FOTBuilder.Symbol.symbolIndependent));
        installInheritedC("script-mid-sup-align", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setScriptMidSupAlign(sym), FOTBuilder.Symbol.symbolCenter));
        installInheritedC("script-mid-sub-align", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setScriptMidSubAlign(sym), FOTBuilder.Symbol.symbolCenter));
        installInheritedC("numerator-align", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setNumeratorAlign(sym), FOTBuilder.Symbol.symbolCenter));
        installInheritedC("denominator-align", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setDenominatorAlign(sym), FOTBuilder.Symbol.symbolCenter));
        installInheritedC("grid-position-cell-type", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setGridPositionCellType(sym), FOTBuilder.Symbol.symbolRowMajor));
        installInheritedC("grid-column-alignment", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setGridColumnAlignment(sym), FOTBuilder.Symbol.symbolCenter));
        installInheritedC("grid-row-alignment", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setGridRowAlignment(sym), FOTBuilder.Symbol.symbolCenter));
        installInheritedC("box-type", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setBoxType(sym), FOTBuilder.Symbol.symbolBorder));
        installInheritedC("glyph-alignment-mode", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setGlyphAlignmentMode(sym), FOTBuilder.Symbol.symbolFont));
        installInheritedC("box-border-alignment", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setBoxBorderAlignment(sym), FOTBuilder.Symbol.symbolOutside));
        installInheritedC("cell-row-alignment", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setCellRowAlignment(sym), FOTBuilder.Symbol.symbolStart));
        installInheritedC("border-alignment", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setBorderAlignment(sym), FOTBuilder.Symbol.symbolCenter));
        installInheritedC("sideline-side", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setSidelineSide(sym), FOTBuilder.Symbol.symbolStart));
        installInheritedC("hyphenation-keep", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setHyphenationKeep(sym), FOTBuilder.Symbol.symbolFalse));
        installInheritedC("font-structure", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setFontStructure(sym), FOTBuilder.Symbol.symbolSolid));
        installInheritedC("font-proportionate-width", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setFontProportionateWidth(sym), FOTBuilder.Symbol.symbolMedium));
        installInheritedC("cell-crossed", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setCellCrossed(sym), FOTBuilder.Symbol.symbolFalse));
        installInheritedC("marginalia-side", new GenericSymbolInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.Symbol sym) => fotb.setMarginaliaSide(sym), FOTBuilder.Symbol.symbolStart));
        // Integer characteristics
        installInheritedC("layer", new GenericIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setLayer(n), 0));
        installInheritedC("background-layer", new GenericIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setBackgroundLayer(n), -1));
        installInheritedC("border-priority", new GenericIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setBorderPriority(n), 0));
        installInheritedC("line-repeat", new GenericIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setLineRepeat(n), 1));
        installInheritedC("span", new GenericIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setSpan(n), 1));
        installInheritedC("min-leader-repeat", new GenericIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setMinLeaderRepeat(n), 1));
        installInheritedC("hyphenation-remain-char-count", new GenericIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setHyphenationRemainCharCount(n), 2));
        installInheritedC("hyphenation-push-char-count", new GenericIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setHyphenationPushCharCount(n), 2));
        installInheritedC("widow-count", new GenericIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setWidowCount(n), 2));
        installInheritedC("orphan-count", new GenericIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setOrphanCount(n), 2));
        // #f or strictly positive integer
        installInheritedC("expand-tabs?", new GenericMaybeIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setExpandTabs(n), 8));
        installInheritedC("hyphenation-ladder-count", new GenericMaybeIntegerInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, long n) => fotb.setHyphenationLadderCount(n), 0));
        // Public ID characteristics
        installInheritedC("background-tile", new GenericPublicIdInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, string? pubid) => fotb.setBackgroundTile(pubid), null));
        installInheritedC("line-breaking-method", new GenericPublicIdInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, string? pubid) => fotb.setLineBreakingMethod(pubid), null));
        installInheritedC("line-composition-method", new GenericPublicIdInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, string? pubid) => fotb.setLineCompositionMethod(pubid), null));
        installInheritedC("implicit-bidi-method", new GenericPublicIdInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, string? pubid) => fotb.setImplicitBidiMethod(pubid), null));
        installInheritedC("glyph-subst-method", new GenericPublicIdInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, string? pubid) => fotb.setGlyphSubstMethod(pubid), null));
        installInheritedC("glyph-reorder-method", new GenericPublicIdInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, string? pubid) => fotb.setGlyphReorderMethod(pubid), null));
        installInheritedC("hyphenation-method", new GenericPublicIdInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, string? pubid) => fotb.setHyphenationMethod(pubid), null));
        installInheritedC("table-auto-width-method", new GenericPublicIdInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, string? pubid) => fotb.setTableAutoWidthMethod(pubid), null));
        installInheritedC("font-name", new GenericPublicIdInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, string? pubid) => fotb.setFontName(pubid), null));
        // Language and country (2 letter symbol or #f)
        installInheritedC("language", new GenericLetter2InheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, ushort code) => fotb.setLanguage(new FOTBuilder.Letter2(code)), 0));
        installInheritedC("country", new GenericLetter2InheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, ushort code) => fotb.setCountry(new FOTBuilder.Letter2(code)), 0));
        // Optional length specs
        installInheritedC("min-pre-line-spacing", new GenericOptLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.OptLengthSpec ols) => fotb.setMinPreLineSpacing(ols)));
        installInheritedC("min-post-line-spacing", new GenericOptLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.OptLengthSpec ols) => fotb.setMinPostLineSpacing(ols)));
        installInheritedC("min-leading", new GenericOptLengthSpecInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.OptLengthSpec ols) => fotb.setMinLeading(ols)));
        // Inline spaces
        installInheritedC("escapement-space-before", new GenericInlineSpaceInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.InlineSpace sp) => fotb.setEscapementSpaceBefore(sp)));
        installInheritedC("escapement-space-after", new GenericInlineSpaceInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.InlineSpace sp) => fotb.setEscapementSpaceAfter(sp)));
        // Optional inline space
        installInheritedC("inline-space-space", new GenericOptInlineSpaceInheritedC(null, nInheritedC_++,
            (FOTBuilder fotb, FOTBuilder.OptInlineSpace sp) => fotb.setInlineSpaceSpace(sp)));
    }

    private void installInheritedC(string name, InheritedC ic)
    {
        StringC sname = makeStringC(name);
        Identifier ident = lookup(sname);
        ic.setIdentifier(ident);
        ident.setInheritedC(new ConstPtr<InheritedC>(ic));
        installInheritedCProc(ident);
    }

    public bool lookupNodeProperty(StringC str, out ComponentName.Id id)
    {
        string name = str.ToString() ?? "";
        if (nodePropertyTable_.TryGetValue(name, out id))
            return true;
        // Try lowercase version
        string lower = name.ToLowerInvariant();
        if (nodePropertyTable_.TryGetValue(lower, out id))
            return true;
        id = ComponentName.Id.noId;
        return false;
    }

    private void installFlowObj(string name, FlowObj flowObj)
    {
        Identifier ident = lookup(makeStringC(name));
        ident.setFlowObj(flowObj);
        makePermanent(flowObj);
    }

    private void installSyntacticKey(string name, Identifier.SyntacticKey key)
    {
        Identifier ident = lookup(makeStringC(name));
        ident.setSyntacticKey(key);
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

    // Convert a number token string (may include unit suffix) to an ELObj
    public ELObj? convertNumber(StringC str, int radix = 10)
    {
        if (str.size() == 0)
            return null;

        nuint i = 0;
        // Handle radix prefix (#d, #x, #o, #b)
        if (str[0] == '#')
        {
            if (str.size() < 2)
                return null;
            switch (str[1])
            {
                case 'd': radix = 10; break;
                case 'x': radix = 16; break;
                case 'o': radix = 8; break;
                case 'b': radix = 2; break;
                default: return null;
            }
            i = 2;
        }
        if (i >= str.size())
            return null;

        bool negative = false;
        if (str[i] == '-')
        {
            negative = true;
            i++;
        }
        else if (str[i] == '+')
        {
            i++;
        }

        bool hadDecimalPoint = false;
        bool hadDigit = false;
        long n = 0;
        int exp = 0;

        // Parse digits
        for (; i < str.size(); i++)
        {
            Char c = str[i];
            int weight = getDigitWeight(c, radix);
            if (weight >= 0)
            {
                hadDigit = true;
                if (negative)
                    n = n * radix - weight;
                else
                    n = n * radix + weight;
                if (hadDecimalPoint)
                    exp--;
            }
            else if (c == '.' && radix == 10)
            {
                if (hadDecimalPoint)
                    return null;
                hadDecimalPoint = true;
            }
            else
                break;
        }

        if (!hadDigit || (radix != 10 && i < str.size()))
            return null;

        // Handle exponent
        if (i + 1 < str.size() && (str[i] == 'e' || str[i] == 'E'))
        {
            Char next = str[i + 1];
            if (next >= '0' && next <= '9' || next == '+' || next == '-')
            {
                hadDecimalPoint = true;
                i++;
                int e = 0;
                bool negExp = false;
                if (str[i] == '-') { negExp = true; i++; }
                else if (str[i] == '+') { i++; }
                for (; i < str.size() && str[i] >= '0' && str[i] <= '9'; i++)
                    e = e * 10 + (int)(str[i] - '0');
                exp += negExp ? -e : e;
            }
        }

        // Handle unit suffix
        if (i < str.size())
        {
            StringC unitName = new StringC();
            for (; i < str.size(); i++)
            {
                Char c = str[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    unitName.operatorPlusAssign(c);
                else
                    return null;  // Invalid character in unit
            }
            Unit? unit = lookupUnit(unitName);
            if (unit == null)
                return null;  // Unknown unit
            // Return unresolved length - will be resolved later when quantities are resolved
            return new UnresolvedLengthObj(n, exp, unit);
        }

        if (hadDecimalPoint)
        {
            double val = n * Math.Pow(10, exp);
            return makeReal(val);
        }
        return makeInteger(n);
    }

    private int getDigitWeight(Char c, int radix)
    {
        int weight;
        if (c >= '0' && c <= '9')
            weight = (int)(c - '0');
        else if (c >= 'a' && c <= 'f')
            weight = (int)(c - 'a') + 10;
        else if (c >= 'A' && c <= 'F')
            weight = (int)(c - 'A') + 10;
        else
            return -1;
        return weight < radix ? weight : -1;
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
        // Create a copy of the StringC since the original may be reused
        ident = new Identifier(new StringC(str));
        identTable_[key] = ident;
        return ident;
    }

    public FunctionObj? lookupExternalProc(StringC pubid)
    {
        string key = pubid.ToString();
        if (externalProcTable_.TryGetValue(key, out FunctionObj? func))
            return func;
        return null;
    }

    // Install a standard external procedure (ISO/IEC 10179:1996//Procedure::name)
    public void installExternalPrimitive(string name, PrimitiveObj value)
    {
        string pubid = "ISO/IEC 10179:1996//Procedure::" + name;
        externalProcTable_[pubid] = value;
    }

    // Install an extension external procedure with custom prefix
    public void installXPrimitive(string prefix, string name, PrimitiveObj value)
    {
        string pubid = prefix + name;
        externalProcTable_[pubid] = value;
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
    private bool debugMode_ = true;  // TEMP: enable for debugging
    public bool debugMode() { return debugMode_; }
    public void setDebugMode(bool debug) { debugMode_ = debug; }

    // Grove manager accessor
    public GroveManager? groveManager() { return groveManager_; }

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

    // C++ implementation: upstream/openjade/style/Interpreter.cxx:627
    // Converts a character name to its Unicode code point
    public bool convertCharName(StringC str, out Char c)
    {
        c = 0;
        string key = str.ToString();
        if (namedCharTable_.TryGetValue(key, out CharPart cp))
        {
            c = (Char)cp.c;
            return true;
        }
        return convertUnicodeCharName(str, out c);
    }

    // C++ implementation: upstream/openjade/style/Interpreter.cxx:635
    // Parses Unicode character names like U-2014
    private bool convertUnicodeCharName(StringC str, out Char c)
    {
        c = 0;
        if (str.size() != 6 || str[0] != 'U' || str[1] != '-')
            return false;
        Char value = 0;
        for (int i = 2; i < 6; i++)
        {
            value <<= 4;
            Char ch = str[(nuint)i];
            if (ch >= '0' && ch <= '9')
                value |= (Char)(ch - '0');
            else if (ch >= 'A' && ch <= 'F')
                value |= (Char)(ch - 'A' + 10);
            else if (ch >= 'a' && ch <= 'f')
                value |= (Char)(ch - 'a' + 10);
            else
                return false;
        }
        c = value;
        return true;
    }

    // C++ implementation: upstream/openjade/style/Interpreter.cxx:428
    // Install standard character names (from charNames.h)
    private void installCharNames()
    {
        // Character names from upstream/openjade/style/charNames.h
        var charNames = new (Char code, string name)[]
        {
            (0x000a, "line-feed"),
            (0x000d, "carriage-return"),
            (0x0020, "space"),
            (0x0021, "exclamation-mark"),
            (0x0022, "quotation-mark"),
            (0x0023, "number-sign"),
            (0x0024, "dollar-sign"),
            (0x0025, "percent-sign"),
            (0x0026, "ampersand"),
            (0x0027, "apostrophe"),
            (0x0028, "left-parenthesis"),
            (0x0029, "right-parenthesis"),
            (0x002a, "asterisk"),
            (0x002b, "plus-sign"),
            (0x002c, "comma"),
            (0x002d, "hyphen-minus"),
            (0x002e, "full-stop"),
            (0x002f, "solidus"),
            (0x0030, "digit-zero"),
            (0x0031, "digit-one"),
            (0x0032, "digit-two"),
            (0x0033, "digit-three"),
            (0x0034, "digit-four"),
            (0x0035, "digit-five"),
            (0x0036, "digit-six"),
            (0x0037, "digit-seven"),
            (0x0038, "digit-eight"),
            (0x0039, "digit-nine"),
            (0x003a, "colon"),
            (0x003b, "semicolon"),
            (0x003c, "less-than-sign"),
            (0x003d, "equals-sign"),
            (0x003e, "greater-than-sign"),
            (0x003f, "question-mark"),
            (0x0040, "commercial-at"),
            (0x005b, "left-square-bracket"),
            (0x005c, "reverse-solidus"),
            (0x005d, "right-square-bracket"),
            (0x005e, "circumflex-accent"),
            (0x005f, "low-line"),
            (0x0060, "grave-accent"),
            (0x007b, "left-curly-bracket"),
            (0x007c, "vertical-line"),
            (0x007d, "right-curly-bracket"),
            (0x007e, "tilde"),
            (0x00a0, "no-break-space"),
            (0x00a9, "copyright-sign"),
            (0x00ab, "left-pointing-double-angle-quotation-mark"),
            (0x00ad, "soft-hyphen"),
            (0x00ae, "registered-sign"),
            (0x00bb, "right-pointing-double-angle-quotation-mark"),
            (0x00d7, "multiplication-sign"),
            (0x00f7, "division-sign"),
            // Important punctuation
            (0x2013, "en-dash"),
            (0x2014, "em-dash"),
            (0x2015, "horizontal-bar"),
            (0x2018, "left-single-quotation-mark"),
            (0x2019, "right-single-quotation-mark"),
            (0x201c, "left-double-quotation-mark"),
            (0x201d, "right-double-quotation-mark"),
            (0x2020, "dagger"),
            (0x2021, "double-dagger"),
            (0x2022, "bullet"),
            (0x2026, "horizontal-ellipsis"),
            (0x2032, "prime"),
            (0x2033, "double-prime"),
            (0x2039, "single-left-pointing-angle-quotation-mark"),
            (0x203a, "single-right-pointing-angle-quotation-mark"),
            (0x2122, "trade-mark-sign"),
            // Mathematical operators
            (0x2212, "minus-sign"),
            (0x221e, "infinity"),
            (0x2260, "not-equal-to"),
            (0x2264, "less-than-or-equal-to"),
            (0x2265, "greater-than-or-equal-to"),
            // Arrows
            (0x2190, "leftwards-arrow"),
            (0x2191, "upwards-arrow"),
            (0x2192, "rightwards-arrow"),
            (0x2193, "downwards-arrow"),
            (0x2194, "left-right-arrow"),
            // Box drawing (commonly used)
            (0x25a0, "black-square"),
            (0x25a1, "white-square"),
            (0x25cf, "black-circle"),
            (0x25cb, "white-circle"),
            // Dingbats - check marks and ballot boxes
            (0x2713, "check-mark"),
            (0x2714, "heavy-check-mark"),
            (0x2717, "ballot-x"),
            (0x2718, "heavy-ballot-x"),
            (0x2610, "ballot-box"),
            (0x2611, "ballot-box-with-check"),
            (0x2612, "ballot-box-with-x"),
            // Dingbats - negative circled sans-serif digits
            (0x278a, "dingbat-negative-circled-sans-serif-digit-one"),
            (0x278b, "dingbat-negative-circled-sans-serif-digit-two"),
            (0x278c, "dingbat-negative-circled-sans-serif-digit-three"),
            (0x278d, "dingbat-negative-circled-sans-serif-digit-four"),
            (0x278e, "dingbat-negative-circled-sans-serif-digit-five"),
            (0x278f, "dingbat-negative-circled-sans-serif-digit-six"),
            (0x2790, "dingbat-negative-circled-sans-serif-digit-seven"),
            (0x2791, "dingbat-negative-circled-sans-serif-digit-eight"),
            (0x2792, "dingbat-negative-circled-sans-serif-digit-nine"),
        };

        foreach (var (code, name) in charNames)
        {
            namedCharTable_[name] = new CharPart { c = (int)code, defPart = uint.MaxValue };
        }
    }

    // C++ implementation: upstream/openjade/style/Interpreter.cxx:445
    // Install SDATA entity mappings - uses the existing sdataEntityNameTable_ declared later
    private void installSdata()
    {
        // SDATA entity mappings from upstream/openjade/style/sdata.h
        var sdataEntities = new (Char code, string name)[]
        {
            (0x2014, "mdash"),  // em dash
            (0x2013, "ndash"),  // en dash
            (0x2018, "lsquo"),  // left single quote
            (0x2019, "rsquo"),  // right single quote
            (0x201c, "ldquo"),  // left double quote
            (0x201d, "rdquo"),  // right double quote
            (0x2026, "hellip"), // horizontal ellipsis
            (0x00a0, "nbsp"),   // non-breaking space
            (0x00a9, "copy"),   // copyright
            (0x00ae, "reg"),    // registered
            (0x2122, "trade"),  // trademark
            (0x0027, "apos"),   // apostrophe
            (0x0022, "quot"),   // quotation mark
            (0x0026, "amp"),    // ampersand
            (0x003c, "lt"),     // less than
            (0x003e, "gt"),     // greater than
        };

        foreach (var (code, name) in sdataEntities)
        {
            sdataEntityNameTable_[name] = code;
        }
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

    // Make string from char array with offset
    public StringObj makeString(Char[]? data, nuint offset, nuint size)
    {
        if (data == null || size == 0)
            return new StringObj(Array.Empty<Char>(), 0);
        if (offset == 0)
            return new StringObj(data, size);
        // Copy the slice to a new array
        Char[] slice = new Char[size];
        Array.Copy(data, (int)offset, slice, 0, (int)size);
        return new StringObj(slice, size);
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
        // Also accept integers (representing internal units)
        long n;
        if (obj.exactIntegerValue(out n))
        {
            result = n;
            return true;
        }
        // Accept LengthSpec objects too (use their length value)
        LengthSpecObj? ls = obj.asLengthSpec();
        if (ls != null)
        {
            result = ls.lengthSpecFOT().length;
            return true;
        }
        // Accept QuantityObj with dimension 1 (lengths)
        long lval;
        double dval;
        int dim;
        if (obj.quantityValue(out lval, out dval, out dim) != ELObj.QuantityType.noQuantity && dim == 1)
        {
            result = (long)dval;
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
        // Also accept integers as lengths (with 0 implicit dimension)
        long intVal;
        if (obj.exactIntegerValue(out intVal))
        {
            result = new FOTBuilder.LengthSpec(intVal);
            return true;
        }
        // Accept QuantityObj with dimension 1 (lengths)
        long lval;
        double dval;
        int dim;
        if (obj.quantityValue(out lval, out dval, out dim) != ELObj.QuantityType.noQuantity && dim == 1)
        {
            // Round to nearest integer like C++ does: d < 0.0 ? long(d - .5) : long(d + .5)
            result = new FOTBuilder.LengthSpec((long)(dval < 0.0 ? dval - 0.5 : dval + 0.5));
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
        // Get the symbol name from the StringObj
        StringObj? nameObj = sym.name();
        if (nameObj == null)
        {
            invalidCharacteristicValue(ident, loc);
            return false;
        }
        // Convert StringObj to string - use data() and size() to get actual content
        StringC nameC = new StringC(nameObj.data(), nameObj.size());
        string name = nameC.ToString().ToLower().Replace("-", "");
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
        // Use convertToString to handle both StringObj and SymbolObj
        StringObj? strObj = obj.convertToString();
        if (strObj != null)
        {
            Char[]? data;
            nuint size;
            if (strObj.stringData(out data, out size) && data != null)
            {
                // Valid 2-letter uppercase code (e.g., "EN", "DE")
                if (size == 2
                    && data[0] >= 'A' && data[0] <= 'Z'
                    && data[1] >= 'A' && data[1] <= 'Z')
                {
                    result = (ushort)((data[0] << 8) | data[1]);
                    return true;
                }
                // Empty string means no language specified
                if (size == 0)
                {
                    result = 0;
                    return true;
                }
            }
        }
        else if (obj == makeFalse())
        {
            result = 0;
            return true;
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

    public ProcessingMode lookupProcessingMode(StringC name)
    {
        string key = name.ToString();
        if (processingModeTable_.TryGetValue(key, out ProcessingMode? mode))
            return mode;
        // Create the mode with initialProcessingMode_ as fallback (like C++)
        mode = new ProcessingMode(name, initialProcessingMode_);
        processingModeTable_[key] = mode;
        return mode;
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
    // Values are in internal units where unitsPerInch_ (72000) units = 1 inch
    private Dictionary<string, Unit> unitTable_ = new();

    // Initialize built-in units - called from constructor
    private void installUnits()
    {
        // Built-in units with their values in internal units
        // Based on unitsPerInch_ = 72000 (1000 units per point at 72 points/inch)
        (string name, int numer, int denom)[] units = {
            ("m", 5000, 127),     // meters: 72000 * 5000 / 127
            ("cm", 50, 127),      // centimeters
            ("mm", 5, 127),       // millimeters
            ("in", 1, 1),         // inches: 72000 units
            ("pt", 1, 72),        // points: 1000 units
            ("pica", 1, 6),       // picas: 12000 units (12 points)
            ("pc", 1, 6),         // picas (alternate)
            ("pi", 1, 6),         // picas (alternate)
        };

        foreach (var (name, numer, denom) in units)
        {
            Unit unit = lookupUnit(new StringC(name))!;
            long n = unitsPerInch_ * numer;
            if (n % denom == 0)
                unit.setValue(n / denom);
            else
                unit.setValue((double)n / denom);
        }
    }

    // Look up or create a unit by name
    public Unit? lookupUnit(StringC name)
    {
        string key = name.ToString().ToLowerInvariant();
        if (unitTable_.TryGetValue(key, out Unit? unit))
            return unit;
        // Create new unit - MUST copy the name since caller may modify the StringC
        unit = new Unit(new StringC(name));
        unitTable_[key] = unit;
        return unit;
    }

    public void installUnit(StringC name, long value)
    {
        Unit? unit = lookupUnit(name);
        if (unit != null)
            unit.setValue(value);
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

    // Override base class method to use GroveString parameters
    public override bool sdataMap(GroveString name, GroveString text, out uint c)
    {
        // Convert GroveString to string for lookup
        string nameStr = name.ToString();
        string textStr = text.ToString();

        if (sdataEntityNameTable_.TryGetValue(nameStr, out Char mapped))
        {
            c = mapped;
            return true;
        }
        if (sdataEntityTextTable_.TryGetValue(textStr, out mapped))
        {
            c = mapped;
            return true;
        }

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
    // Accepts #f (false) as empty string for optional string characteristics
    public bool convertStringC(ELObj obj, Identifier? ident, Location loc, out StringC result)
    {
        // Accept #f as empty string (common for optional characteristics like notation-system-id)
        if (obj == makeFalse())
        {
            result = new StringC();
            return true;
        }
        Char[]? s;
        nuint n;
        if (obj.stringData(out s, out n))
        {
            // s can be null for empty strings, which is valid
            result = (s != null) ? new StringC(s, n) : new StringC();
            return true;
        }
        // Try to convert to string (handles SymbolObj that can be converted)
        StringObj? strObj = obj.convertToString();
        if (strObj != null && strObj.stringData(out s, out n))
        {
            result = (s != null) ? new StringC(s, n) : new StringC();
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

    private StyleObj? makeBorderStyle(bool b, uint index)
    {
        var forceSpecs = new System.Collections.Generic.List<ConstPtr<InheritedC>>();
        var specs = new System.Collections.Generic.List<ConstPtr<InheritedC>>();
        specs.Add(new ConstPtr<InheritedC>(
            new GenericBoolInheritedC(lookup(makeStringC("border-present?")),
                index, (FOTBuilder fotb, bool val) => fotb.setBorderPresent(val), b)));
        var style = new VarStyleObj(new ConstPtr<StyleSpec>(new StyleSpec(forceSpecs, specs)), null, null, new NodePtr());
        makePermanent(style);
        return style;
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

    public void installExtensionFlowObjectClass(Identifier ident, StringC pubid, Location loc)
    {
        FlowObj? tem = null;
        if (debugMode_)
            Console.Error.WriteLine($"installExtensionFlowObjectClass: {ident.name()}, pubid: {pubid}");
        if (extensionTable_ != null)
        {
            foreach (var ep in extensionTable_)
            {
                if (ep.pubid == null)
                    break;
                if (pubid.ToString() == ep.pubid)
                {
                    if (ep.flowObj != null)
                    {
                        var cFlowObj = ep.flowObj.asCompoundExtensionFlowObj();
                        if (cFlowObj != null)
                        {
                            if (debugMode_)
                                Console.Error.WriteLine($"  -> Found compound extension flow object");
                            tem = new CompoundExtensionFlowObj(cFlowObj);
                        }
                        else
                        {
                            if (debugMode_)
                                Console.Error.WriteLine($"  -> Found extension flow object");
                            tem = new ExtensionFlowObj(ep.flowObj);
                        }
                    }
                    break;
                }
            }
        }
        if (tem == null)
        {
            if (pubid.ToString() == "UNREGISTERED::James Clark//Flow Object Class::formatting-instruction")
            {
                if (debugMode_)
                    Console.Error.WriteLine($"  -> Creating FormattingInstructionFlowObj");
                tem = new FormattingInstructionFlowObj();
            }
            else
            {
                if (debugMode_)
                    Console.Error.WriteLine($"  -> Creating UnknownFlowObj");
                tem = new UnknownFlowObj();
            }
        }
        makePermanent(tem);
        ident.setFlowObj(tem);
    }

    public void installExtensionInheritedC(Identifier ident, StringC pubid, Location loc)
    {
        // If the identifier already has a proper InheritedC (from installInheritedCs),
        // don't overwrite it with an IgnoredC
        var existing = ident.inheritedC();
        if (existing != null && !existing.isNull() && !(existing.pointer() is IgnoredC))
        {
            // Already has a proper characteristic class, just ensure the proc is installed
            installInheritedCProc(ident);
            return;
        }

        // For extension characteristics not in our built-in set, create an IgnoredC
        var ic = new ConstPtr<InheritedC>(new IgnoredC(ident, nInheritedC_++, makeFalse(), this));

        // Register the inherited characteristic on the identifier
        ident.setInheritedC(ic);

        // Install the inherited-<name> procedure
        installInheritedCProc(ident);
    }

    private void installInheritedCProc(Identifier ident)
    {
        // Create the "inherited-<name>" procedure for accessing this characteristic
        var ic = ident.inheritedC();
        if (ic == null || ic.isNull())
            return;

        StringC procName = new StringC();
        procName.assign("inherited-");
        procName.operatorPlusAssign(ident.name());

        Identifier procIdent = lookup(procName);
        if (procIdent.expression() == null)
        {
            procIdent.setExpression(new ConstantExpression(new InheritedCPrimitiveObj(ic), loc: new Location()), 0, new Location());
        }
    }

    public uint currentPartIndex() { return 0; }  // Part index for multi-part stylesheets
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
    unknownCharName,
    invalidCharNumber,
    invalidNumber,
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
    // Node property messages
    notAStringOrSymbol,
    notASingletonNode,
    noNodePropertyValue,
    // Unit definition messages
    badUnitDefinition,
    unitLoop,
    duplicateUnitDefinition,
    invalidUnitName,
}
