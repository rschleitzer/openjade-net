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
                       bool strictMode, FOTBuilder.Extension? extensionTable = null)
    {
        interpreter_ = new Interpreter();
        cmdline_ = new StringC();
    }

    public void defineVariable(StringC str)
    {
        throw new NotImplementedException();
    }

    public void parseSpec(SgmlParser specParser, CharsetInfo charset,
                          StringC id, Messenger mgr)
    {
        throw new NotImplementedException();
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

// Full ProcessContext implementation
public class ProcessContextImpl
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

    public FOTBuilder currentFOTBuilder()
    {
        return connectionStack_[connectionStack_.Count - 1].fotb!;
    }

    public StyleStack currentStyleStack()
    {
        return connectionStack_[connectionStack_.Count - 1].styleStack;
    }

    public void process(NodePtr node)
    {
        Interpreter interp = vm_.interp;
        StyleObj? style = interp.initialStyle();
        if (style != null)
        {
            currentStyleStack().push(style, vm(), currentFOTBuilder());
            currentFOTBuilder().startSequence();
        }
        processNode(node, interp.initialProcessingMode());
        if (style != null)
        {
            currentFOTBuilder().endSequence();
            currentStyleStack().pop();
        }
    }

    public void processNode(NodePtr node, ProcessingMode? mode, bool chunk = true)
    {
        throw new NotImplementedException();
    }

    public void processNodeSafe(NodePtr node, ProcessingMode? mode, bool chunk = true)
    {
        throw new NotImplementedException();
    }

    public void nextMatch(StyleObj? style)
    {
        throw new NotImplementedException();
    }

    public void processChildren(ProcessingMode? mode)
    {
        NodePtr currentNode = vm_.currentNode;
        if (currentNode.assignFirstChild() == AccessResult.accessOK)
        {
            do
            {
                processNode(currentNode, mode);
            } while (currentNode.assignNextChunkSibling() == AccessResult.accessOK);
        }
        else
        {
            // Try to get document element
            NodePtr docElement = new NodePtr();
            AccessResult result = currentNode.getDocumentElement(ref docElement);
            if (result == AccessResult.accessOK)
                processNode(docElement, mode);
        }
    }

    public void processChildrenTrim(ProcessingMode? mode)
    {
        throw new NotImplementedException();
    }

    public void startFlowObj()
    {
        flowObjLevel_++;
    }

    public void endFlowObj()
    {
        flowObjLevel_--;
    }

    public void startConnection(SymbolObj? label, Location loc)
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

    public void endConnection()
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

    public void pushPorts(bool hasPrincipalPort, System.Collections.Generic.List<SymbolObj?> labels, System.Collections.Generic.List<FOTBuilder?> fotbs)
    {
        Connectable c = new Connectable(labels.Count, currentStyleStack(), flowObjLevel_);
        connectableStack_.Insert(0, c);
        for (int i = 0; i < labels.Count; i++)
        {
            c.ports[i].labels.Add(labels[i]);
            c.ports[i].fotb = fotbs[i];
        }
        connectableStackLevel_++;
        // TODO: deal with !hasPrincipalPort
    }

    public void popPorts()
    {
        connectableStackLevel_--;
        if (connectableStack_.Count > 0)
            connectableStack_.RemoveAt(0);
    }

    public void pushPrincipalPort(FOTBuilder? principalPort)
    {
        connectionStack_.Insert(0, new Connection(principalPort));
    }

    public void popPrincipalPort()
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
    public void startTable()
    {
        tableStack_.Add(new Table());
    }

    public void endTable()
    {
        tableStack_.RemoveAt(tableStack_.Count - 1);
    }

    public void startTablePart()
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

    public void endTablePart()
    {
        coverSpannedRows();
    }

    public void addTableColumn(uint columnIndex, uint span, StyleObj? style)
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

    public uint currentTableColumn()
    {
        return tableStack_[tableStack_.Count - 1].currentColumn;
    }

    public void noteTableCell(uint colIndex, uint colSpan, uint rowSpan)
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

    public StyleObj? tableColumnStyle(uint columnIndex, uint span)
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

    public StyleObj? tableRowStyle()
    {
        return tableStack_[tableStack_.Count - 1].rowStyle;
    }

    public void startTableRow(StyleObj? style)
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

    public bool inTable()
    {
        return tableStack_.Count > 0;
    }

    public bool inTableRow()
    {
        return tableStack_.Count > 0 && tableStack_[tableStack_.Count - 1].inTableRow;
    }

    public void endTableRow()
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

    public void setPageType(uint n)
    {
        havePageType_ = true;
        pageType_ = n;
    }

    public bool getPageType(out uint n)
    {
        n = pageType_;
        return havePageType_;
    }

    public VM vm() { return vm_; }

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
