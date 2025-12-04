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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public void endConnection()
    {
        throw new NotImplementedException();
    }

    public void pushPorts(bool hasPrincipalPort, System.Collections.Generic.List<SymbolObj?> ports, System.Collections.Generic.List<FOTBuilder?> fotbs)
    {
        throw new NotImplementedException();
    }

    public void popPorts()
    {
        throw new NotImplementedException();
    }

    public void pushPrincipalPort(FOTBuilder? principalPort)
    {
        throw new NotImplementedException();
    }

    public void popPrincipalPort()
    {
        throw new NotImplementedException();
    }

    public void startMapContent(ELObj? obj, Location loc)
    {
        throw new NotImplementedException();
    }

    public void endMapContent()
    {
        throw new NotImplementedException();
    }

    public void startDiscardLabeled(SymbolObj? sym)
    {
        throw new NotImplementedException();
    }

    public void endDiscardLabeled()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public void endTablePart()
    {
        throw new NotImplementedException();
    }

    public void addTableColumn(uint columnIndex, uint span, StyleObj? style)
    {
        throw new NotImplementedException();
    }

    public uint currentTableColumn()
    {
        return tableStack_[tableStack_.Count - 1].currentColumn;
    }

    public void noteTableCell(uint colIndex, uint colSpan, uint rowSpan)
    {
        throw new NotImplementedException();
    }

    public StyleObj? tableColumnStyle(uint columnIndex, uint span)
    {
        throw new NotImplementedException();
    }

    public StyleObj? tableRowStyle()
    {
        return tableStack_[tableStack_.Count - 1].rowStyle;
    }

    public void startTableRow(StyleObj? style)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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

    private void badContentMap(ref bool flag, Location loc)
    {
        throw new NotImplementedException();
    }

    private void coverSpannedRows()
    {
        throw new NotImplementedException();
    }

    private void restoreConnection(uint connectableLevel, nuint portIndex)
    {
        throw new NotImplementedException();
    }
}

// Ignore FOT Builder (discards output)
public class IgnoreFOTBuilder : FOTBuilder { }
