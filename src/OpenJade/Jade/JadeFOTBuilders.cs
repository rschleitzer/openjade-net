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

// TeX FOT Builder - produces TeX/LaTeX output
public class TeXFOTBuilder : FOTBuilder
{
    private OutputByteStream? stream_;
    private Messenger? messenger_;

    public TeXFOTBuilder(OutputByteStream stream, Messenger messenger)
    {
        stream_ = stream;
        messenger_ = messenger;
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

    // Additional TeX-specific implementations would go here
}

// HTML FOT Builder - produces HTML output
public class HtmlFOTBuilder : FOTBuilder
{
    private StringC basename_;
    private CmdLineApp? app_;

    public HtmlFOTBuilder(StringC basename, CmdLineApp app)
    {
        basename_ = basename;
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

    // Additional HTML-specific implementations would go here
}

// RTF FOT Builder - produces Rich Text Format output
public class RtfFOTBuilder : FOTBuilder
{
    private OutputByteStream? stream_;
    private System.Collections.Generic.List<StringC>? options_;
    private Ptr<ExtendEntityManager>? entityManager_;
    private CharsetInfo? charsetInfo_;
    private Messenger? messenger_;

    public RtfFOTBuilder(
        OutputByteStream stream,
        System.Collections.Generic.List<StringC> options,
        Ptr<ExtendEntityManager> entityManager,
        CharsetInfo charsetInfo,
        Messenger messenger)
    {
        stream_ = stream;
        options_ = options;
        entityManager_ = entityManager;
        charsetInfo_ = charsetInfo;
        messenger_ = messenger;
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

    // Additional RTF-specific implementations would go here
}

// SGML FOT Builder - produces SGML output
public class SgmlFOTBuilder : FOTBuilder
{
    private OutputCharStream? stream_;

    public SgmlFOTBuilder(OutputCharStream stream)
    {
        stream_ = stream;
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

    // Additional SGML-specific implementations would go here
}

// Transform FOT Builder - produces transformed SGML/XML output
public class TransformFOTBuilder : FOTBuilder
{
    private CmdLineApp? app_;
    private bool xml_;
    private System.Collections.Generic.List<StringC>? options_;

    public TransformFOTBuilder(CmdLineApp app, bool xml, System.Collections.Generic.List<StringC> options)
    {
        app_ = app;
        xml_ = xml;
        options_ = options;
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

    // Additional Transform-specific implementations would go here
}

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
