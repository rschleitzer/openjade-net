// Copyright (c) 1996, 1997, 1998 James Clark, David Megginson, Kathleen Marszalek
// See the file copying.txt for copying permission.

namespace OpenJade.Jade;

using OpenSP;
using OpenJade.Style;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Factory class for creating FOT builders
public static class FOTBuilderFactory
{
    public static FOTBuilder makeTeXFOTBuilder(OutputByteStream stream, Messenger messenger, out FOTBuilder.ExtensionTableEntry[]? ext)
    {
        ext = null;
        return new TeXFOTBuilder(stream, messenger);
    }

    public static FOTBuilder makeHtmlFOTBuilder(StringC basename, CmdLineApp app, out FOTBuilder.ExtensionTableEntry[]? ext)
    {
        ext = null;
        return new HtmlFOTBuilder(basename, app);
    }

    public static FOTBuilder makeRtfFOTBuilder(
        OutputByteStream stream,
        System.Collections.Generic.List<StringC> options,
        Ptr<ExtendEntityManager> entityManager,
        CharsetInfo charsetInfo,
        Messenger messenger,
        out FOTBuilder.ExtensionTableEntry[]? ext)
    {
        ext = null;
        return new RtfFOTBuilder(stream, options, entityManager, charsetInfo, messenger);
    }

    public static FOTBuilder makeSgmlFOTBuilder(OutputCharStream stream)
    {
        return new SgmlFOTBuilder(stream);
    }

    public static FOTBuilder makeTransformFOTBuilder(
        CmdLineApp app,
        bool xml,
        System.Collections.Generic.List<StringC> options,
        out FOTBuilder.ExtensionTableEntry[]? extensions)
    {
        extensions = TransformFOTBuilder.GetExtensions();
        return new TransformFOTBuilder(app, xml, options);
    }

    public static FOTBuilder makeMifFOTBuilder(
        StringC fileLoc,
        Ptr<ExtendEntityManager> entityManager,
        CharsetInfo charsetInfo,
        CmdLineApp app,
        out FOTBuilder.ExtensionTableEntry[]? ext)
    {
        ext = null;
        return new MifFOTBuilder(fileLoc, entityManager, charsetInfo, app);
    }
}

// TeXFOTBuilder is implemented in TeXFOTBuilder.cs
// HtmlFOTBuilder is implemented in HtmlFOTBuilder.cs
// RtfFOTBuilder is implemented in RtfFOTBuilder.cs
// SgmlFOTBuilder is implemented in SgmlFOTBuilder.cs
// TransformFOTBuilder is implemented in TransformFOTBuilder.cs
// MifFOTBuilder is implemented in MifFOTBuilder.cs
