// Copyright (c) 1997 James Clark
// See the file copying.txt for copying permission.
// Ported to C# as part of OpenJade-NET

namespace OpenJade.Jade;

using OpenSP;
using OpenJade.Style;
using OpenJade.Grove;
using System.Text;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Transform FOT Builder - produces transformed SGML/XML output
public class TransformFOTBuilder : FOTBuilder
{
    private CmdLineApp? app_;
    private OutputCharStream? os_;
    private bool xml_;
    private System.Collections.Generic.List<StringC>? options_;
    private System.Collections.Generic.List<StringC> openElements_;
    private ReState state_;
    private bool preserveSdata_;
    private System.Collections.Generic.List<bool> preserveSdataStack_;

    public enum ReState
    {
        stateMiddle,
        stateStartOfElement,
        statePendingRe
    }

    // Document type NIC for doctype declaration
    public class DocumentTypeNIC
    {
        public StringC name = new StringC();
        public StringC publicId = new StringC();
        public StringC systemId = new StringC();
    }

    // Element NIC for element construction
    public class ElementNIC
    {
        public StringC gi = new StringC();
        public System.Collections.Generic.List<StringC> attributes = new System.Collections.Generic.List<StringC>();
    }

    // Constructor
    public TransformFOTBuilder(CmdLineApp app, bool xml, System.Collections.Generic.List<StringC> options)
    {
        app_ = app;
        xml_ = xml;
        options_ = options;
        openElements_ = new System.Collections.Generic.List<StringC>();
        state_ = ReState.stateMiddle;
        preserveSdata_ = false;
        preserveSdataStack_ = new System.Collections.Generic.List<bool>();

        // Create output stream
        os_ = app_.makeStdOut();

        // Output XML declaration if in XML mode
        if (xml_)
            output("<?xml version=\"1.0\"?>\n");
    }

    private void output(string s)
    {
        if (os_ == null) return;
        foreach (char c in s)
            os_.put((Char)c);
    }

    private void output(StringC s)
    {
        if (os_ == null) return;
        for (nuint i = 0; i < s.size(); i++)
            os_.put(s[i]);
    }

    private void flushPendingRe()
    {
        if (state_ == ReState.statePendingRe)
        {
            output("\r");
            state_ = ReState.stateMiddle;
        }
    }

    private void flushPendingReCharRef()
    {
        if (state_ == ReState.statePendingRe)
        {
            output("&#13;");
            state_ = ReState.stateMiddle;
        }
    }

    // Output attributes
    private void outputAttributes(System.Collections.Generic.List<StringC> atts)
    {
        for (int i = 0; i + 1 < atts.Count; i += 2)
        {
            output(" ");
            output(atts[i]);
            output("=\"");
            outputAttributeValue(atts[i + 1]);
            output("\"");
        }
    }

    private void outputAttributeValue(StringC value)
    {
        for (nuint i = 0; i < value.size(); i++)
        {
            Char c = value[i];
            switch (c)
            {
                case '<': output("&lt;"); break;
                case '>': output("&gt;"); break;
                case '&': output("&amp;"); break;
                case '"': output("&quot;"); break;
                default:
                    if (os_ != null)
                        os_.put(c);
                    break;
            }
        }
    }

    public override void start()
    {
        preserveSdataStack_.Add(preserveSdata_);
    }

    public override void end()
    {
        if (preserveSdataStack_.Count > 0)
        {
            preserveSdata_ = preserveSdataStack_[preserveSdataStack_.Count - 1];
            preserveSdataStack_.RemoveAt(preserveSdataStack_.Count - 1);
        }
    }

    public override void characters(Char[] data, nuint size)
    {
        if (state_ == ReState.stateStartOfElement)
            state_ = ReState.stateMiddle;
        else
            flushPendingRe();

        for (nuint i = 0; i < size; i++)
        {
            Char c = data[i];
            switch (c)
            {
                case '<': output("&lt;"); break;
                case '>': output("&gt;"); break;
                case '&': output("&amp;"); break;
                case '\r':
                    if (state_ == ReState.stateMiddle)
                        state_ = ReState.statePendingRe;
                    else
                        flushPendingReCharRef();
                    break;
                default:
                    if (os_ != null)
                        os_.put(c);
                    break;
            }
        }
    }

    public override void charactersFromNode(NodePtr nd, Char[] data, nuint size)
    {
        characters(data, size);
    }

    // Start an element
    public void startElement(ElementNIC nic)
    {
        flushPendingRe();
        output("<");
        output(nic.gi);
        outputAttributes(nic.attributes);
        output(">");
        openElements_.Add(new StringC(nic.gi));
        state_ = ReState.stateStartOfElement;
    }

    // End an element
    public void endElement()
    {
        if (openElements_.Count > 0)
        {
            StringC gi = openElements_[openElements_.Count - 1];
            openElements_.RemoveAt(openElements_.Count - 1);

            if (state_ == ReState.stateStartOfElement)
                state_ = ReState.stateMiddle;
            else
                flushPendingRe();

            output("</");
            output(gi);
            output(">");
            state_ = ReState.statePendingRe;
        }
    }

    // Empty element
    public void emptyElement(ElementNIC nic)
    {
        flushPendingRe();
        output("<");
        output(nic.gi);
        outputAttributes(nic.attributes);
        if (xml_)
            output("/>");
        else
            output(">");
        state_ = ReState.statePendingRe;
    }

    // Processing instruction
    public void processingInstruction(StringC data)
    {
        flushPendingRe();
        output("<?");
        output(data);
        if (xml_)
            output("?>");
        else
            output(">");
        state_ = ReState.statePendingRe;
    }

    // Document type declaration
    public void documentType(DocumentTypeNIC nic)
    {
        flushPendingRe();
        output("<!DOCTYPE ");
        output(nic.name);
        if (nic.publicId.size() > 0)
        {
            output(" PUBLIC \"");
            output(nic.publicId);
            output("\"");
            if (nic.systemId.size() > 0)
            {
                output(" \"");
                output(nic.systemId);
                output("\"");
            }
        }
        else if (nic.systemId.size() > 0)
        {
            output(" SYSTEM \"");
            output(nic.systemId);
            output("\"");
        }
        output(">\n");
    }

    // Entity reference
    public void entityRef(StringC name)
    {
        flushPendingRe();
        output("&");
        output(name);
        output(";");
    }

    // Start external entity
    public void startEntity(StringC systemId)
    {
        // Would open a new output file here for multi-file output
    }

    // End external entity
    public void endEntity()
    {
        // Would close the external entity file
    }

    public override void formattingInstruction(StringC s)
    {
        flushPendingRe();
        output(s);
    }

    // Extension flow object handling
    public override void extension(ExtensionFlowObj fo, NodePtr currentNode)
    {
        // Handle transform extension flow objects
    }

    public override void startExtension(ExtensionFlowObj fo, NodePtr currentNode, System.Collections.Generic.List<FOTBuilder?> fotbs)
    {
        // Handle compound transform extension flow objects
    }

    public override void endExtension(ExtensionFlowObj fo)
    {
    }

    // Setters
    public void setPreserveSdata(bool flag)
    {
        preserveSdata_ = flag;
    }

    // Additional flow object support
    public override void startParagraph(ParagraphNIC nic)
    {
        start();
    }

    public override void endParagraph()
    {
        output("\n");
        end();
    }

    public override void startDisplayGroup(DisplayGroupNIC nic)
    {
        start();
    }

    public override void endDisplayGroup()
    {
        end();
    }

    public override void startScroll()
    {
        start();
    }

    public override void endScroll()
    {
        end();
    }

    public override void startSequence()
    {
        start();
    }

    public override void endSequence()
    {
        end();
    }
}
