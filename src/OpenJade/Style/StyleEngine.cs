// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Boolean = System.Boolean;

// The DSSSL style engine
public class StyleEngine : IDisposable
{
    private Interpreter? interpreter_;
    private StringC cmdline_;

    public StyleEngine(Messenger mgr, GroveManager groveManager,
                       int unitsPerInch, bool debugMode, bool dsssl2,
                       bool strictMode, FOTBuilder.ExtensionTableEntry[]? extensionTable = null)
    {
        interpreter_ = new Interpreter(groveManager, extensionTable);
        interpreter_.setUnitsPerInch(unitsPerInch);
        interpreter_.setDebugMode(debugMode);
        interpreter_.setDsssl2(dsssl2);
        interpreter_.setStrictMode(strictMode);
        cmdline_ = new StringC();
    }

    public void defineVariable(StringC str)
    {
        // Interpret "name=value" as a string variable setting
        if (str.size() > 0 && str[0] == '(')
        {
            // Already a DSSSL expression - pass through
            cmdline_.append(str);
        }
        else
        {
            // Find the '=' separator
            nuint i;
            for (i = 0; i < str.size() && str[i] != '='; i++)
                ;

            if (i == 0 || i >= str.size())
            {
                // No '=' found or empty name - define as #t
                cmdline_.append(interpreter_.makeStringC("(define "));
                cmdline_.append(str);
                cmdline_.append(interpreter_.makeStringC(" #t)"));
            }
            else
            {
                // name=value format - define as string
                cmdline_.append(interpreter_.makeStringC("(define "));
                // Append the name part (before '=')
                cmdline_.append(str.data(), 0, i);
                cmdline_.append(interpreter_.makeStringC(" \""));
                // Append the value part (after '=')
                if (str.size() > i + 1)
                    cmdline_.append(str.data(), i + 1, str.size() - i - 1);
                cmdline_.append(interpreter_.makeStringC("\")"));
            }
        }
    }

    public void parseSpec(SgmlParser specParser, CharsetInfo charset,
                          StringC id, Messenger mgr)
    {
        // Create handler for DSSSL specification parsing
        DssslSpecEventHandler specHandler = new DssslSpecEventHandler(mgr);

        // Load and resolve specification parts
        System.Collections.Generic.List<DssslSpecEventHandler.Part> parts =
            new System.Collections.Generic.List<DssslSpecEventHandler.Part>();
        specHandler.load(specParser, charset, id, parts);

        // Parse each part's body elements
        for (int partIndex = parts.Count - 1; partIndex >= 0; partIndex--)
        {
            DssslSpecEventHandler.Part part = parts[partIndex];

            // Process declarations first
            for (IListIter<DssslSpecEventHandler.DeclarationElement> diter = part.diter();
                 diter.done() == 0; diter.next())
            {
                DssslSpecEventHandler.DeclarationElement decl = diter.cur()!;
                processDeclaration(decl, charset);
            }

            // Process body elements
            int bodyCount = 0;
            for (IListIter<DssslSpecEventHandler.BodyElement> iter = part.iter();
                 iter.done() == 0; iter.next())
            {
                bodyCount++;
                DssslSpecEventHandler.BodyElement body = iter.cur()!;
                body.makeInputSource(specHandler, out InputSource? inputSource);
                if (inputSource != null)
                {
                    SchemeParser parser = new SchemeParser(interpreter_!, inputSource);
                    parser.parse();
                }
            }
        }

        // Compile all processing modes
        interpreter_!.compile();
    }

    private void processDeclaration(DssslSpecEventHandler.DeclarationElement decl, CharsetInfo charset)
    {
        // Process DSSSL declarations like features, char-repertoire, etc.
        switch (decl.type())
        {
            case DssslSpecEventHandler.DeclarationType.features:
                // Parse feature declaration
                break;
            case DssslSpecEventHandler.DeclarationType.mapSdataEntity:
                // Register SDATA entity mapping
                break;
            case DssslSpecEventHandler.DeclarationType.charRepertoire:
                // Set character repertoire
                break;
            case DssslSpecEventHandler.DeclarationType.sgmlGrovePlan:
                // Set grove plan
                break;
            default:
                // Other declarations
                break;
        }
    }

    public void process(NodePtr node, FOTBuilder fotb)
    {
        var context = new ProcessContextImpl(interpreter_!, fotb);
        context.process(node);
    }

    public void Dispose()
    {
        interpreter_ = null;
    }
}

// Grove manager interface
public abstract class GroveManager
{
    public abstract bool load(StringC sysid, System.Collections.Generic.List<StringC> active,
                              NodePtr parent, ref NodePtr rootNode,
                              System.Collections.Generic.List<StringC> architecture);

    public abstract bool readEntity(StringC name, out StringC content);

    public abstract void mapSysid(ref StringC sysid);
}

// ProcessContext base class
public abstract class ProcessContext
{
    public abstract FOTBuilder currentFOTBuilder();
    public abstract StyleStack currentStyleStack();
    public abstract VM vm();
    public abstract void processNode(NodePtr node, ProcessingMode? mode, bool chunk = true);
    public abstract void processChildren(ProcessingMode? mode);
    public abstract void processChildrenTrim(ProcessingMode? mode);
    public abstract void nextMatch(StyleObj? style);
    public abstract void startFlowObj();
    public abstract void endFlowObj();
    public abstract void startConnection(SymbolObj? label, Location loc);
    public abstract void endConnection();

    // Table methods
    public abstract void startTable();
    public abstract void endTable();
    public abstract bool inTable();
    public abstract bool inTableRow();
    public abstract void startTableRow(StyleObj? style);
    public abstract void endTableRow();
    public abstract uint currentTableColumn();
    public abstract void addTableColumn(uint columnIndex, uint span, StyleObj? style);
    public abstract void noteTableCell(uint columnIndex, uint columnSpan, uint rowSpan);
    public abstract StyleObj? tableColumnStyle(uint columnIndex, uint span);
    public abstract StyleObj? tableRowStyle();
    public abstract void startTablePart();
    public abstract void endTablePart();

    // Port management
    public abstract void pushPorts(bool hasPrincipalPort, System.Collections.Generic.List<SymbolObj> labels, System.Collections.Generic.List<FOTBuilder?> fotbs);
    public abstract void popPorts();
    public abstract void pushPrincipalPort(FOTBuilder? principalPort);
    public abstract void popPrincipalPort();

    // Page type for simple page sequences
    public abstract void setPageType(uint n);

    // Character output
    public virtual void characters(Char[] data, nuint start, nuint len)
    {
        if (start == 0)
            currentFOTBuilder().characters(data, len);
        else
        {
            // Create a copy starting from offset
            Char[] sub = new Char[len];
            Array.Copy(data, (int)start, sub, 0, (int)len);
            currentFOTBuilder().characters(sub, len);
        }
    }
}

// Full ProcessContext implementation
public class ProcessContextImpl : ProcessContext
{
    private FOTBuilder ignoreFotb_;
    private System.Collections.Generic.List<Connection> connectionStack_;
    private System.Collections.Generic.List<Connectable> connectableStack_;
    private uint connectableStackLevel_;
    private System.Collections.Generic.List<Table> tableStack_;
    private System.Collections.Generic.List<System.Collections.Generic.Queue<SaveFOTBuilder>> principalPortSaveQueues_;
    private VM vm_;
    private ProcessingMode.Specificity matchSpecificity_;
    private uint flowObjLevel_;
    private bool havePageType_;
    private uint pageType_;
    private System.Collections.Generic.List<NodeStackEntry> nodeStack_;

    public ProcessContextImpl(Interpreter interp, FOTBuilder fotb)
    {
        ignoreFotb_ = new IgnoreFOTBuilder();
        connectionStack_ = new System.Collections.Generic.List<Connection>();
        connectableStack_ = new System.Collections.Generic.List<Connectable>();
        connectableStackLevel_ = 0;
        tableStack_ = new System.Collections.Generic.List<Table>();
        principalPortSaveQueues_ = new System.Collections.Generic.List<System.Collections.Generic.Queue<SaveFOTBuilder>>();
        vm_ = new VM(interp);
        matchSpecificity_ = new ProcessingMode.Specificity();
        flowObjLevel_ = 0;
        havePageType_ = false;
        pageType_ = 0;
        nodeStack_ = new System.Collections.Generic.List<NodeStackEntry>();

        // Initialize connection stack with FOT builder
        connectionStack_.Add(new Connection(fotb));
    }

    public override FOTBuilder currentFOTBuilder()
    {
        return connectionStack_[connectionStack_.Count - 1].fotb!;
    }

    public override StyleStack currentStyleStack()
    {
        return connectionStack_[connectionStack_.Count - 1].styleStack;
    }

    public void process(NodePtr node)
    {
        Interpreter interp = vm_.interp;

        // Get the document element from the document node
        NodePtr docElement = new NodePtr();
        if (node.getDocumentElement(ref docElement) != AccessResult.accessOK)
        {
            // Fallback: try to use node directly if it's already an element
            docElement = node;
        }

        StyleObj? style = interp.initialStyle();
        if (style != null)
        {
            currentStyleStack().push(style, vm(), currentFOTBuilder());
            currentFOTBuilder().startSequence();
        }
        processNode(docElement, interp.initialProcessingMode());
        if (style != null)
        {
            currentFOTBuilder().endSequence();
            currentStyleStack().pop();
        }
    }

    public override void processNode(NodePtr node, ProcessingMode? mode, bool chunk = true)
    {
        if (mode == null)
            return;

        // Check if this is a text/data node - if so, output characters directly
        GroveString str = new GroveString();
        if (node.charChunk(vm_.interp, ref str) == AccessResult.accessOK)
        {
            currentFOTBuilder().charactersFromNode(node, str.data()!, chunk ? str.size() : 1);
            return;
        }

        // Save current context
        using var cns = new CurrentNodeSetter(node, mode, vm_);
        var saveSpecificity = matchSpecificity_;
        matchSpecificity_ = new ProcessingMode.Specificity();

        currentFOTBuilder().startNode(node, mode.name());

        // Find matching rule and execute it
        var context = new Pattern.MatchContext();
        var rule = mode.findMatch(node, context, vm_.interp, ref matchSpecificity_);

        if (rule != null)
        {
            // Get the action's instruction and sosofo
            InsnPtr? insn;
            SosofoObj? sosofo;
            rule.action().get(out insn, out sosofo);

            if (sosofo != null)
            {
                // Use pre-compiled sosofo directly
                sosofo.process(this);
            }
            else if (insn != null)
            {
                // Evaluate the instruction to get a sosofo
                ELObj? result = vm_.eval(insn.pointer(), null, null);
                SosofoObj? resultSosofo = result?.asSosofo();
                if (resultSosofo != null)
                {
                    resultSosofo.process(this);
                }
            }
        }
        else
        {
            // No matching rule - process children by default
            processChildren(mode);
        }

        currentFOTBuilder().endNode();
        matchSpecificity_ = saveSpecificity;
    }

    public void processNodeSafe(NodePtr nodePtr, ProcessingMode? processingMode, bool chunk = true)
    {
        ulong elementIndex = 0;
        if (nodePtr.elementIndex(ref elementIndex) == AccessResult.accessOK)
        {
            uint groveIndex = nodePtr.groveIndex();
            for (int i = 0; i < nodeStack_.Count; i++)
            {
                NodeStackEntry nse = nodeStack_[i];
                if (nse.elementIndex == elementIndex &&
                    nse.groveIndex == groveIndex &&
                    nse.processingMode == processingMode)
                {
                    // Loop detected - would report error
                    vm_.interp.setNodeLocation(nodePtr);
                    return;
                }
            }
            nodeStack_.Add(new NodeStackEntry
            {
                elementIndex = elementIndex,
                groveIndex = groveIndex,
                processingMode = processingMode
            });
            processNode(nodePtr, processingMode, chunk);
            nodeStack_.RemoveAt(nodeStack_.Count - 1);
        }
        else
            processNode(nodePtr, processingMode, chunk);
    }

    public override void nextMatch(StyleObj? style)
    {
        // Find next matching rule and process it
        var rule = vm_.processingMode?.findMatch(vm_.currentNode, new Pattern.MatchContext(), vm_.interp, ref matchSpecificity_);
        if (rule == null)
        {
            // No more rules - process children
            processChildren(vm_.processingMode);
        }
        else if (!matchSpecificity_.isStyle())
        {
            // Construction rule
            rule.action().get(out InsnPtr? insn, out SosofoObj? sosofoObj);
            if (sosofoObj != null)
                sosofoObj.process(this);
            else if (insn?.get() != null)
            {
                ELObj? result = vm_.eval(insn.get());
                if (result?.asSosofo() != null)
                    result.asSosofo()!.process(this);
            }
        }
        else
        {
            // Style rule
            if (style != null)
                currentStyleStack().push(style, vm_, currentFOTBuilder());
            processChildren(vm_.processingMode);
        }
    }

    public override void processChildren(ProcessingMode? mode)
    {
        // Match C++ exactly: use vm_.currentNode directly and modify it during iteration
        if (vm_.currentNode.assignFirstChild() == AccessResult.accessOK)
        {
            do
            {
                processNode(vm_.currentNode, mode);
            } while (vm_.currentNode.assignNextChunkSibling() == AccessResult.accessOK);
        }
        else if (vm_.currentNode.getDocumentElement(ref vm_.currentNode) == AccessResult.accessOK)
        {
            processNode(vm_.currentNode, mode);
        }
    }

    public override void processChildrenTrim(ProcessingMode? mode)
    {
        NodePtr origNode = vm_.currentNode;
        if (vm_.currentNode.assignFirstChild() == AccessResult.accessOK)
        {
            bool atStart = true;
            do
            {
                NodePtr curNode = vm_.currentNode;
                GroveString str = new GroveString();
                if (curNode.charChunk(vm_.interp, ref str) == AccessResult.accessOK)
                {
                    Char[] data = str.data() ?? Array.Empty<Char>();
                    nuint len = str.size();
                    nuint startIdx = 0;

                    if (atStart)
                    {
                        // Skip leading whitespace
                        for (; startIdx < len; startIdx++)
                        {
                            if (!isWhiteSpace(data[startIdx]))
                                break;
                        }
                        if (startIdx >= len)
                        {
                            // Entire chunk was whitespace
                            continue;
                        }
                        atStart = false;
                    }

                    if (len > 0)
                    {
                        // Check if we need to trim trailing whitespace
                        if (isWhiteSpace(data[len - 1]) && onlyWhiteSpaceFollows(curNode))
                        {
                            nuint endIdx = len;
                            for (; endIdx > startIdx; endIdx--)
                            {
                                if (!isWhiteSpace(data[endIdx - 1]))
                                    break;
                            }
                            if (endIdx > startIdx)
                                currentFOTBuilder().charactersFromNode(curNode, data, startIdx, endIdx - startIdx);
                            return;
                        }
                        currentFOTBuilder().charactersFromNode(curNode, data, startIdx, len - startIdx);
                    }
                }
                else
                {
                    GroveString gi = new GroveString();
                    if (atStart && vm_.currentNode.getGi(ref gi) == AccessResult.accessOK)
                        atStart = false;
                    processNode(vm_.currentNode, mode);
                }
            } while (vm_.currentNode.assignNextChunkSibling() == AccessResult.accessOK);
        }
        else
        {
            NodePtr docElement = new NodePtr();
            if (origNode.getDocumentElement(ref docElement) == AccessResult.accessOK)
                processNode(docElement, mode);
        }
        vm_.currentNode = origNode;
    }

    private bool isWhiteSpace(Char c)
    {
        // Check if character is whitespace per DSSSL semantics
        return c == ' ' || c == '\t' || c == '\n' || c == '\r';
    }

    private bool onlyWhiteSpaceFollows(NodePtr node)
    {
        NodePtr tem = new NodePtr();
        if (node.nextChunkSibling(ref tem) == AccessResult.accessOK)
        {
            do
            {
                GroveString str = new GroveString();
                if (tem.charChunk(vm_.interp, ref str) == AccessResult.accessOK)
                {
                    Char[] data = str.data() ?? Array.Empty<Char>();
                    for (nuint i = 0; i < str.size(); i++)
                    {
                        if (!isWhiteSpace(data[i]))
                            return false;
                    }
                }
                else
                {
                    GroveString gi = new GroveString();
                    if (tem.getGi(ref gi) == AccessResult.accessOK)
                        return false;
                }
            } while (tem.assignNextChunkSibling() == AccessResult.accessOK);
        }
        return true;
    }

    public override void startFlowObj()
    {
        flowObjLevel_++;
    }

    public override void endFlowObj()
    {
        flowObjLevel_--;
    }

    public override void startConnection(SymbolObj? label, Location loc)
    {
        uint connLevel = connectableStackLevel_;
        for (int idx = 0; idx < connectableStack_.Count; idx++)
        {
            Connectable conn = connectableStack_[idx];
            for (int i = 0; i < conn.ports.Count; i++)
            {
                Port port = conn.ports[i];
                for (int j = 0; j < port.labels.Count; j++)
                {
                    if (port.labels[j] == label)
                    {
                        restoreConnection(connLevel, (nuint)i);
                        return;
                    }
                }
            }
            for (int i = 0; i < conn.principalPortLabels.Count; i++)
            {
                if (conn.principalPortLabels[i] == label)
                {
                    restoreConnection(connLevel, nuint.MaxValue);
                    return;
                }
            }
            connLevel--;
        }
        // Bad connection - would report error
        if (connectionStack_.Count > 0)
            connectionStack_[0].nBadFollow++;
    }

    public override void endConnection()
    {
        if (inTableRow() && tableStack_.Count > 0 && tableStack_[tableStack_.Count - 1].rowConnectableLevel == connectableStackLevel_)
            endTableRow();
        if (connectionStack_.Count > 0 && connectionStack_[0].nBadFollow > 0)
            connectionStack_[0].nBadFollow--;
        else if (connectionStack_.Count > 0)
        {
            currentFOTBuilder().endNode();
            Port? port = connectionStack_[0].port;
            if (port != null && --(port.connected) == 0)
            {
                while (port.saveQueue.Count > 0)
                {
                    SaveFOTBuilder saved = port.saveQueue.Dequeue();
                    saved.emit(port.fotb!);
                }
            }
            connectionStack_.RemoveAt(0);
        }
    }

    public override void pushPorts(bool hasPrincipalPort, System.Collections.Generic.List<SymbolObj> labels, System.Collections.Generic.List<FOTBuilder?> fotbs)
    {
        Connectable c = new Connectable(labels.Count, currentStyleStack(), flowObjLevel_);
        connectableStack_.Insert(0, c);
        for (int i = 0; i < labels.Count; i++)
        {
            c.ports[i].labels.Add(labels[i]);
            c.ports[i].fotb = fotbs[i];
        }
        connectableStackLevel_++;
        // FIXME: deal with !hasPrincipalPort (matching upstream ProcessContext.cxx:382)
    }

    public override void popPorts()
    {
        connectableStackLevel_--;
        if (connectableStack_.Count > 0)
            connectableStack_.RemoveAt(0);
    }

    public override void pushPrincipalPort(FOTBuilder? principalPort)
    {
        connectionStack_.Insert(0, new Connection(principalPort));
    }

    public override void popPrincipalPort()
    {
        if (connectionStack_.Count > 0)
            connectionStack_.RemoveAt(0);
    }

    public void startMapContent(ELObj? contentMap, Location loc)
    {
        bool badFlag = false;
        if (connectableStack_.Count == 0 || connectableStack_[0].flowObjLevel != flowObjLevel_)
            connectableStack_.Insert(0, new Connectable(0, currentStyleStack(), flowObjLevel_));
        Connectable conn = connectableStack_[0];
        var portNames = new System.Collections.Generic.List<SymbolObj?>();
        for (int i = 0; i < conn.ports.Count; i++)
        {
            portNames.Add(conn.ports[i].labels.Count > 0 ? conn.ports[i].labels[0] : null);
            conn.ports[i].labels.Clear();
        }
        while (contentMap != null && !contentMap.isNil())
        {
            PairObj? tem = contentMap.asPair();
            if (tem == null)
            {
                badContentMap(ref badFlag, loc);
                break;
            }
            ELObj? entry = tem.car();
            contentMap = tem.cdr();
            tem = entry?.asPair();
            if (tem != null)
            {
                SymbolObj? label = tem.car()?.asSymbol();
                if (label != null)
                {
                    tem = tem.cdr()?.asPair();
                    if (tem != null)
                    {
                        SymbolObj? port = tem.car()?.asSymbol();
                        if (port != null)
                        {
                            bool found = false;
                            for (int i = 0; i < portNames.Count; i++)
                            {
                                if (portNames[i] == port)
                                {
                                    conn.ports[i].labels.Add(label);
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                // Bad port - would report error
                            }
                        }
                        else if (tem.car() == vm_.interp.makeFalse())
                            conn.principalPortLabels.Add(label);
                        else
                            badContentMap(ref badFlag, loc);
                        if (tem.cdr() != null && !tem.cdr()!.isNil())
                            badContentMap(ref badFlag, loc);
                    }
                    else
                        badContentMap(ref badFlag, loc);
                }
                else
                    badContentMap(ref badFlag, loc);
            }
            else
                badContentMap(ref badFlag, loc);
        }
    }

    public void endMapContent()
    {
        if (connectableStack_.Count > 0 && connectableStack_[0].ports.Count == 0)
            connectableStack_.RemoveAt(0);
    }

    public void startDiscardLabeled(SymbolObj? label)
    {
        startFlowObj();
        Connectable c = new Connectable(1, currentStyleStack(), flowObjLevel_);
        connectableStack_.Insert(0, c);
        c.ports[0].labels.Add(label);
        c.ports[0].fotb = ignoreFotb_;
    }

    public void endDiscardLabeled()
    {
        if (connectableStack_.Count > 0)
            connectableStack_.RemoveAt(0);
        endFlowObj();
    }

    // Table support
    public override void startTable()
    {
        tableStack_.Add(new Table());
    }

    public override void endTable()
    {
        tableStack_.RemoveAt(tableStack_.Count - 1);
    }

    public override void startTablePart()
    {
        if (tableStack_.Count > 0)
        {
            Table table = tableStack_[tableStack_.Count - 1];
            table.currentColumn = 0;
            table.rowStyle = null;
            table.columnStyles.Clear();
            table.covered.Clear();
            table.nColumns = 0;
        }
    }

    public override void endTablePart()
    {
        coverSpannedRows();
    }

    public override void addTableColumn(uint columnIndex, uint span, StyleObj? style)
    {
        if (tableStack_.Count > 0)
        {
            Table table = tableStack_[tableStack_.Count - 1];
            table.currentColumn = columnIndex + span;
            while (table.columnStyles.Count <= (int)columnIndex)
                table.columnStyles.Add(new System.Collections.Generic.List<StyleObj?>());
            var tem = table.columnStyles[(int)columnIndex];
            if (span > 0)
            {
                while (tem.Count < (int)span)
                    tem.Add(null);
                tem[(int)span - 1] = style;
            }
        }
    }

    public override uint currentTableColumn()
    {
        return tableStack_[tableStack_.Count - 1].currentColumn;
    }

    public override void noteTableCell(uint colIndex, uint colSpan, uint rowSpan)
    {
        if (tableStack_.Count == 0)
            return;
        Table table = tableStack_[tableStack_.Count - 1];
        table.currentColumn = colIndex + colSpan;
        var covered = table.covered;
        while (covered.Count < (int)(colIndex + colSpan))
            covered.Add(0);
        for (uint i = 0; i < colSpan; i++)
            covered[(int)(colIndex + i)] = rowSpan;
        if (colIndex + colSpan > table.nColumns)
            table.nColumns = colIndex + colSpan;
    }

    public override StyleObj? tableColumnStyle(uint columnIndex, uint span)
    {
        if (tableStack_.Count > 0)
        {
            Table table = tableStack_[tableStack_.Count - 1];
            if (columnIndex < table.columnStyles.Count)
            {
                var tem = table.columnStyles[(int)columnIndex];
                if (span > 0 && span <= (uint)tem.Count)
                    return tem[(int)span - 1];
            }
        }
        return null;
    }

    public override StyleObj? tableRowStyle()
    {
        return tableStack_[tableStack_.Count - 1].rowStyle;
    }

    public override void startTableRow(StyleObj? style)
    {
        if (tableStack_.Count > 0)
        {
            Table table = tableStack_[tableStack_.Count - 1];
            table.rowStyle = style;
            table.currentColumn = 0;
            table.inTableRow = true;
            table.rowConnectableLevel = connectionStack_[connectionStack_.Count - 1].connectableLevel;
        }
        currentFOTBuilder().startTableRow();
    }

    public override bool inTable()
    {
        return tableStack_.Count > 0;
    }

    public override bool inTableRow()
    {
        return tableStack_.Count > 0 && tableStack_[tableStack_.Count - 1].inTableRow;
    }

    public override void endTableRow()
    {
        if (tableStack_.Count > 0)
        {
            Table table = tableStack_[tableStack_.Count - 1];
            // Decrement covered counts for each column
            var covered = table.covered;
            for (int i = 0; i < (int)table.nColumns && i < covered.Count; i++)
            {
                if (covered[i] > 0)
                    covered[i] -= 1;
            }
            table.inTableRow = false;
        }
        currentFOTBuilder().endTableRow();
    }

    public void clearPageType()
    {
        havePageType_ = false;
    }

    public override void setPageType(uint n)
    {
        havePageType_ = true;
        pageType_ = n;
    }

    public bool getPageType(out uint n)
    {
        n = pageType_;
        return havePageType_;
    }

    public override VM vm() { return vm_; }

    // Port structure
    private class Port
    {
        public FOTBuilder? fotb;
        public System.Collections.Generic.Queue<SaveFOTBuilder> saveQueue;
        public System.Collections.Generic.List<SymbolObj?> labels;
        public uint connected;

        public Port()
        {
            fotb = null;
            saveQueue = new System.Collections.Generic.Queue<SaveFOTBuilder>();
            labels = new System.Collections.Generic.List<SymbolObj?>();
            connected = 0;
        }
    }

    // Connectable structure
    private class Connectable
    {
        public System.Collections.Generic.List<Port> ports;
        public StyleStack styleStack;
        public uint flowObjLevel;
        public System.Collections.Generic.List<SymbolObj?> principalPortLabels;

        public Connectable(int nPorts, StyleStack stack, uint level)
        {
            ports = new System.Collections.Generic.List<Port>();
            for (int i = 0; i < nPorts; i++)
                ports.Add(new Port());
            styleStack = stack;
            flowObjLevel = level;
            principalPortLabels = new System.Collections.Generic.List<SymbolObj?>();
        }
    }

    // Connection structure
    private class Connection
    {
        public FOTBuilder? fotb;
        public StyleStack styleStack;
        public Port? port;
        public uint connectableLevel;
        public uint nBadFollow;

        public Connection(FOTBuilder? fotb)
        {
            this.fotb = fotb;
            styleStack = new StyleStack();
            port = null;
            connectableLevel = 0;
            nBadFollow = 0;
        }

        public Connection(StyleStack stack, Port? port, uint level)
        {
            fotb = port?.fotb;
            styleStack = stack;
            this.port = port;
            connectableLevel = level;
            nBadFollow = 0;
        }
    }

    // Table structure
    private class Table
    {
        public uint currentColumn;
        public System.Collections.Generic.List<System.Collections.Generic.List<StyleObj?>> columnStyles;
        public System.Collections.Generic.List<uint> covered;
        public uint nColumns;
        public StyleObj? rowStyle;
        public bool inTableRow;
        public uint rowConnectableLevel;

        public Table()
        {
            currentColumn = 0;
            columnStyles = new System.Collections.Generic.List<System.Collections.Generic.List<StyleObj?>>();
            covered = new System.Collections.Generic.List<uint>();
            nColumns = 0;
            rowStyle = null;
            inTableRow = false;
            rowConnectableLevel = 0;
        }
    }

    // Node stack entry
    private class NodeStackEntry
    {
        public ulong elementIndex;
        public uint groveIndex;
        public ProcessingMode? processingMode;
    }

    private void badContentMap(ref bool badFlag, Location loc)
    {
        if (badFlag)
            return;
        badFlag = true;
        // Would report error via vm_.interp.message()
    }

    private void coverSpannedRows()
    {
        // Generate empty cells to cover any remaining vertical spans
        if (tableStack_.Count == 0)
            return;
        Table table = tableStack_[tableStack_.Count - 1];
        // Find max remaining rows to cover
        uint n = 0;
        for (int i = 0; i < table.covered.Count; i++)
            if (table.covered[i] > n)
                n = table.covered[i];
        // Skip generating empty rows for now - would need EmptySosofoObj and TableRowFlowObj
        // The full implementation creates dummy rows to cover spans
    }

    private void restoreConnection(uint connectableLevel, nuint portIndex)
    {
        uint connLevel = connectableStackLevel_;
        int idx = 0;
        while (connLevel != connectableLevel && idx < connectableStack_.Count)
        {
            idx++;
            connLevel--;
        }
        if (idx >= connectableStack_.Count)
            return;
        Connectable conn = connectableStack_[idx];
        if (portIndex != nuint.MaxValue)
        {
            Port port = conn.ports[(int)portIndex];
            Connection c = new Connection(conn.styleStack, port, connLevel);
            if (port.connected > 0)
            {
                port.connected++;
                SaveFOTBuilder save = new SaveFOTBuilder(vm_.currentNode, vm_.processingMode?.name() ?? new StringC());
                c.fotb = save;
                port.saveQueue.Enqueue(save);
            }
            else
            {
                c.fotb = port.fotb;
                port.connected = 1;
            }
            connectionStack_.Insert(0, c);
            currentFOTBuilder().startNode(vm_.currentNode, vm_.processingMode?.name() ?? new StringC());
        }
        else
        {
            Connection c = new Connection(conn.styleStack, null, connLevel);
            if (conn.flowObjLevel == flowObjLevel_)
            {
                c.fotb = currentFOTBuilder();
            }
            else
            {
                SaveFOTBuilder save = new SaveFOTBuilder(vm_.currentNode, vm_.processingMode?.name() ?? new StringC());
                c.fotb = save;
                while (principalPortSaveQueues_.Count <= (int)conn.flowObjLevel)
                    principalPortSaveQueues_.Add(new System.Collections.Generic.Queue<SaveFOTBuilder>());
                principalPortSaveQueues_[(int)conn.flowObjLevel].Enqueue(save);
            }
            connectionStack_.Insert(0, c);
            currentFOTBuilder().startNode(vm_.currentNode, vm_.processingMode?.name() ?? new StringC());
        }
    }
}

// Ignore FOT Builder (discards output)
public class IgnoreFOTBuilder : FOTBuilder { }
