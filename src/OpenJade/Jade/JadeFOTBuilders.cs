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
    public static FOTBuilder makeTeXFOTBuilder(OutputByteStream stream, Messenger messenger, out FOTBuilder.Extension? ext)
    {
        ext = null;
        return new TeXFOTBuilder(stream, messenger);
    }

    public static FOTBuilder makeHtmlFOTBuilder(StringC basename, CmdLineApp app, out FOTBuilder.Extension? ext)
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
        out FOTBuilder.Extension? ext)
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
        out FOTBuilder.Extension? ext)
    {
        ext = null;
        return new TransformFOTBuilder(app, xml, options);
    }

    public static FOTBuilder makeMifFOTBuilder(
        StringC fileLoc,
        Ptr<ExtendEntityManager> entityManager,
        CharsetInfo charsetInfo,
        CmdLineApp app,
        out FOTBuilder.Extension? ext)
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

// MIF FOT Builder - produces FrameMaker MIF output
public class MifFOTBuilder : FOTBuilder
{
    private StringC fileLoc_;
    private Ptr<ExtendEntityManager>? entityManager_;
    private CharsetInfo? charsetInfo_;
    private CmdLineApp? app_;

    public MifFOTBuilder(
        StringC fileLoc,
        Ptr<ExtendEntityManager> entityManager,
        CharsetInfo charsetInfo,
        CmdLineApp app)
    {
        fileLoc_ = fileLoc;
        entityManager_ = entityManager;
        charsetInfo_ = charsetInfo;
        app_ = app;
    }

    public override void characters(Char[] data, nuint size)
    {
        throw new NotImplementedException();
    }

    public override void startParagraph(FOTBuilder.ParagraphNIC nic)
    {
        throw new NotImplementedException();
    }

    public override void endParagraph()
    {
        throw new NotImplementedException();
    }

    // Additional MIF-specific implementations would go here
}
