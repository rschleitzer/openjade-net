using OpenSP;
// Copyright (c) 1994, 1995 James Clark
// See the file COPYING for copying permission.

namespace Nsgmls;

public class PrologMessageEventHandler : MessageEventHandler
{
    // PrologMessageEventHandler(class Messenger *messenger);
    public PrologMessageEventHandler(Messenger messenger)
        : base(messenger)
    {
    }

    // void endProlog(EndPrologEvent *);
    public override void endProlog(EndPrologEvent? ev)
    {
        cancel();
    }
}

public class XRastEventHandler : RastEventHandler
{
    private Messenger messenger_;
    // file_ must come before os_ so it gets inited first
    private FileOutputByteStream file_ = new FileOutputByteStream();
    private EncodeOutputCharStream os_;
    private string filename_;
    private StringC filenameStr_;
    private CmdLineApp app_;

    // XRastEventHandler(SgmlParser *, const NsgmlsApp::AppChar *filename, const StringC &filenameStr,
    //                   const OutputCodingSystem *, CmdLineApp *, class ::Messenger *messenger);
    public XRastEventHandler(SgmlParser parser,
                             string filename,
                             StringC filenameStr,
                             CodingSystem? codingSystem,
                             CmdLineApp app,
                             Messenger messenger)
        : base(parser, messenger)
    {
        messenger_ = messenger;
        filename_ = filename;
        filenameStr_ = filenameStr;
        app_ = app;

        // errno = 0;
        if (!file_.open(filename))
        {
            messenger.message(CmdLineApp.openFileErrorMessage(),
                              new StringMessageArg(filenameStr),
                              new ErrnoMessageArg(0)); // errno not easily available in C#
            Environment.Exit(1);
        }
        os_ = new EncodeOutputCharStream(file_, codingSystem!);
        setOutputStream(os_);
    }

    // ~XRastEventHandler();
    ~XRastEventHandler()
    {
        end();
    }

    // void truncateOutput();
    public override void truncateOutput()
    {
        os_.flush();
        // errno = 0;
        if (!file_.close())
            messenger_.message(CmdLineApp.closeFileErrorMessage(),
                               new StringMessageArg(filenameStr_),
                               new ErrnoMessageArg(0));
        // errno = 0;
        if (!file_.open(filename_))
        {
            messenger_.message(CmdLineApp.openFileErrorMessage(),
                               new StringMessageArg(filenameStr_),
                               new ErrnoMessageArg(0));
            Environment.Exit(1);
        }
    }

    // void message(MessageEvent *);
    public override void message(MessageEvent? ev)
    {
        if (ev != null)
        {
            messenger_.dispatchMessage(ev.message());
            base.message(ev);
        }
    }

    // void allLinkTypesActivated();
    public void allLinkTypesActivated()
    {
        // Empty in original
    }
}

public class NsgmlsApp : ParserApp
{
    public struct OptionFlags
    {
        public string name;
        public uint flag;
    }

    public static readonly OptionFlags[] outputOptions = new OptionFlags[]
    {
        new OptionFlags { name = "all", flag = SgmlsEventHandler.outputAll },
        new OptionFlags { name = "line", flag = SgmlsEventHandler.outputLine },
        new OptionFlags { name = "entity", flag = SgmlsEventHandler.outputEntity },
        new OptionFlags { name = "id", flag = SgmlsEventHandler.outputId },
        new OptionFlags { name = "included", flag = SgmlsEventHandler.outputIncluded },
        new OptionFlags { name = "notation-sysid", flag = SgmlsEventHandler.outputNotationSysid },
        new OptionFlags { name = "nonsgml", flag = SgmlsEventHandler.outputNonSgml },
        new OptionFlags { name = "empty", flag = SgmlsEventHandler.outputEmpty },
        new OptionFlags { name = "data-attribute", flag = SgmlsEventHandler.outputDataAtt },
        new OptionFlags { name = "comment", flag = SgmlsEventHandler.outputComment },
        new OptionFlags { name = "omitted", flag = SgmlsEventHandler.outputTagOmission | SgmlsEventHandler.outputAttributeOmission },
        new OptionFlags { name = "tagomit", flag = SgmlsEventHandler.outputTagOmission },
        new OptionFlags { name = "attromit", flag = SgmlsEventHandler.outputAttributeOmission },
        new OptionFlags { name = "version", flag = SgmlsEventHandler.outputParserInformation },
        new OptionFlags { name = "all", flag = 0 }, // sentinel
    };

    private Boolean suppressOutput_;
    private Boolean prologOnly_;
    private uint outputFlags_;
    private String<char> rastFile_ = new String<char>();
    private string? rastOption_;
    private Boolean batchMode_;

    // NsgmlsApp();
    public NsgmlsApp()
        : base()
    {
        suppressOutput_ = false;
        batchMode_ = false;
        prologOnly_ = false;
        outputFlags_ = 0;
        rastOption_ = null;

        registerOption('B', "batch-mode", NsgmlsMessages.BHelp);
        registerOption('o', "option", NsgmlsMessages.option, NsgmlsMessages.oHelp);
        registerOption('p', "only-prolog", NsgmlsMessages.pHelp);
        registerOption('s', "no-output", NsgmlsMessages.sHelp);
        registerOption('t', "rast-file", NsgmlsMessages.file, NsgmlsMessages.tHelp);
        // FIXME treat these as aliases
        registerOption('d', null, NsgmlsMessages.dHelp);
        registerOption('l', null, NsgmlsMessages.lHelp);
        // registerOption('m', "catalog", NsgmlsMessages.sysid, NsgmlsMessages.mHelp);
        registerOption('m', null, NsgmlsMessages.sysid, NsgmlsMessages.mHelp);
        registerOption('r', null, NsgmlsMessages.rHelp);
        registerOption('u', null, NsgmlsMessages.uHelp);
        registerInfo(NsgmlsMessages.info1);
        registerInfo(NsgmlsMessages.info2);
        registerInfo(NsgmlsMessages.info3);
        registerInfo(NsgmlsMessages.info4);
        registerInfo(NsgmlsMessages.info5);
        registerInfo(NsgmlsMessages.info6);
        registerInfo(NsgmlsMessages.info7);
        registerInfo(NsgmlsMessages.info8);
    }

    // void processOption(AppChar opt, const AppChar *arg);
    public override void processOption(char opt, string? arg)
    {
        switch (opt)
        {
            case 'B':
                batchMode_ = true;
                break;
            case 'd':
                // warn about duplicate entity declarations
                options_.warnDuplicateEntity = true;
                break;
            case 'l':
                // output L commands
                outputFlags_ |= SgmlsEventHandler.outputLine;
                break;
            case 'm':
                processOption('c', arg);
                break;
            case 'o':
                {
                    Boolean found = false;
                    for (int i = 0; outputOptions[i].flag != 0; i++)
                        if (arg == outputOptions[i].name)
                        {
                            outputFlags_ |= outputOptions[i].flag;
                            found = true;
                            break;
                        }
                    if (!found)
                        message(NsgmlsMessages.unknownOutputOption,
                                new StringMessageArg(convertInput(arg ?? "")));
                }
                break;
            case 'p':
                prologOnly_ = true;
                break;
            case 'r':
                // warn about defaulted entity reference
                options_.warnDefaultEntityReference = true;
                break;
            case 's':
                suppressOutput_ = true;
                break;
            case 't':
                rastOption_ = arg;
                break;
            case 'u':
                // warn about undefined elements
                options_.warnUndefinedElement = true;
                break;
            default:
                base.processOption(opt, arg);
                break;
        }
        if ((outputFlags_ & SgmlsEventHandler.outputComment) != 0)
        {
            options_.eventsWanted.addCommentDecls();
            options_.eventsWanted.addPrologMarkup();
        }
        if ((outputFlags_ & SgmlsEventHandler.outputTagOmission) != 0)
            options_.eventsWanted.addInstanceMarkup();
    }

    // int processArguments(int argc, AppChar **argv);
    public override int processArguments(string[] argv)
    {
        if (batchMode_)
        {
            int ret = 0;
            for (int i = 0; i < argv.Length; i++)
            {
                if (rastOption_ != null)
                {
                    rastFile_.assign(rastOption_);
                    rastFile_.append(argv[i]);
                    rastFile_.operatorPlusAssign('\0');
                }
                int tem = base.processArguments(new string[] { argv[i] });
                if (tem > ret)
                    ret = tem;
            }
            return ret;
        }
        else
            return base.processArguments(argv);
    }

    // void allLinkTypesActivated();
    public override void allLinkTypesActivated()
    {
        if (rastOption_ == null)
            base.allLinkTypesActivated();
    }

    // ErrorCountEventHandler *makeEventHandler();
    public override ErrorCountEventHandler? makeEventHandler()
    {
        if (prologOnly_)
            return new PrologMessageEventHandler(this);
        else if (rastOption_ != null)
        {
            string s = batchMode_ ? new string(rastFile_.data()) : rastOption_;
            return new XRastEventHandler(parser_, s, convertInput(s),
                                         outputCodingSystem_, this, this);
        }
        else if (suppressOutput_)
            return new MessageEventHandler(this, parser_);
        else
            return new SgmlsEventHandler(parser_,
                                         makeStdOut(),
                                         this,
                                         outputFlags_);
    }
}
