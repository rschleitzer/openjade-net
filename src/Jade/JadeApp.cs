// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.
// Ported to C# as part of OpenJade-NET

namespace Jade;

using OpenSP;
using OpenJade.Style;
using OpenJade.Jade;
using Char = System.UInt32;

// Jade Application - DSSSL to various output format processor
public class JadeApp : DssslApp
{
    public enum OutputType
    {
        fotType,
        rtfType,
        htmlType,
        texType,
        mifType,
        sgmlType,
        xmlType
    }

    private static readonly string[] outputTypeNames = {
        "fot",
        "rtf",
        "html",
        "tex",
        "mif",
        "sgml",
        "xml"
    };

    private OutputType outputType_ = OutputType.fotType;
    private string outputFilename_ = "";
    private System.Collections.Generic.List<StringC> outputOptions_;
    private FileOutputByteStream? outputFile_;

    public JadeApp() : base(72000) // 72000 units per inch
    {
        outputOptions_ = new System.Collections.Generic.List<StringC>();
        registerOption('t', "(fot|rtf|html|tex|mif|sgml|xml)");
        registerOption('o', "output_file");
    }

    public override void processOption(char opt, string? arg)
    {
        if (arg == null) arg = "";
        switch (opt)
        {
            case 't':
                {
                    int dashPos = arg.IndexOf('-');
                    string typeName = dashPos >= 0 ? arg.Substring(0, dashPos) : arg;

                    bool found = false;
                    for (int i = 0; i < outputTypeNames.Length; i++)
                    {
                        if (typeName == outputTypeNames[i])
                        {
                            outputType_ = (OutputType)i;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Console.Error.WriteLine("Error: Unknown output type: " + arg);
                    }

                    // Parse sub-options
                    if (dashPos >= 0)
                    {
                        string subOpts = arg.Substring(dashPos);
                        System.Text.StringBuilder currentOpt = new System.Text.StringBuilder();
                        foreach (char c in subOpts)
                        {
                            if (c == '-')
                            {
                                if (currentOpt.Length > 0)
                                {
                                    StringC sc = new StringC();
                                    sc.assign(currentOpt.ToString());
                                    outputOptions_.Add(sc);
                                    currentOpt.Clear();
                                }
                            }
                            else
                            {
                                currentOpt.Append(c);
                            }
                        }
                        if (currentOpt.Length > 0)
                        {
                            StringC sc = new StringC();
                            sc.assign(currentOpt.ToString());
                            outputOptions_.Add(sc);
                        }
                    }
                }
                break;
            case 'o':
                if (arg.Length == 0)
                    Console.Error.WriteLine("Error: Empty output filename");
                else
                    outputFilename_ = arg;
                break;
            default:
                base.processOption(opt, arg);
                break;
        }
    }

    public override FOTBuilder? makeFOTBuilder(out FOTBuilder.ExtensionTableEntry[]? ext)
    {
        ext = null;

        // Determine output filename
        if (outputFilename_.Length == 0)
        {
            if (defaultOutputBasename_.size() > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (nuint i = 0; i < defaultOutputBasename_.size(); i++)
                    sb.Append((char)defaultOutputBasename_[i]);
                outputFilename_ = sb.ToString();
            }
            else
            {
                outputFilename_ = "jade-out";
            }
            outputFilename_ += "." + outputTypeNames[(int)outputType_];
        }

        // Open output file for appropriate types
        switch (outputType_)
        {
            case OutputType.htmlType:
            case OutputType.sgmlType:
            case OutputType.xmlType:
                // These don't need a pre-opened file
                break;
            default:
                outputFile_ = new FileOutputByteStream();
                if (!outputFile_.open(outputFilename_))
                {
                    Console.Error.WriteLine("Error: Cannot open output file: " + outputFilename_);
                    return null;
                }
                break;
        }

        // Create appropriate FOT builder
        switch (outputType_)
        {
            case OutputType.rtfType:
                unitsPerInch_ = 20 * 72; // twips
                return FOTBuilderFactory.makeRtfFOTBuilder(
                    outputFile_!,
                    outputOptions_,
                    entityManager(),
                    systemCharset(),
                    this,
                    out ext);

            case OutputType.texType:
                return FOTBuilderFactory.makeTeXFOTBuilder(
                    outputFile_!,
                    this,
                    out ext);

            case OutputType.htmlType:
                {
                    StringC basename = new StringC();
                    basename.assign(outputFilename_);
                    return FOTBuilderFactory.makeHtmlFOTBuilder(
                        basename,
                        this,
                        out ext);
                }

            case OutputType.mifType:
                {
                    StringC fileLoc = new StringC();
                    fileLoc.assign(outputFilename_);
                    return FOTBuilderFactory.makeMifFOTBuilder(
                        fileLoc,
                        entityManager(),
                        systemCharset(),
                        this,
                        out ext);
                }

            case OutputType.fotType:
                {
                    var encodeStream = new EncodeOutputCharStream(outputFile_!, outputCodingSystem_!);
                    var recordStream = new RecordOutputCharStream(encodeStream);
                    return FOTBuilderFactory.makeSgmlFOTBuilder(recordStream);
                }

            case OutputType.sgmlType:
            case OutputType.xmlType:
                return FOTBuilderFactory.makeTransformFOTBuilder(
                    this,
                    outputType_ == OutputType.xmlType,
                    outputOptions_,
                    out ext);

            default:
                return null;
        }
    }

    public string programName()
    {
        return "jade";
    }

    public void printUsage()
    {
        Console.WriteLine("Usage: jade [options] DSSSL-spec document");
        Console.WriteLine("Options:");
        Console.WriteLine("  -t type    Output type (fot, rtf, html, tex, mif, sgml, xml)");
        Console.WriteLine("  -o file    Output file");
        Console.WriteLine("  -d spec    DSSSL specification");
        Console.WriteLine("  -V var     Define variable");
        Console.WriteLine("  -c catalog Use SGML catalog");
    }
}
