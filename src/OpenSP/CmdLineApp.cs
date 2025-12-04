// Copyright (c) 1996 James Clark, 1999 Matthias Clasen
// See the file COPYING for copying permission.

using System.Runtime.InteropServices;

namespace OpenSP;

// Command line application base class
public abstract class CmdLineApp : MessageReporter
{
    public const string SP_PACKAGE = "OpenSP.NET";
    public const string SP_VERSION = "1.0.0";

    protected string? errorFile_;
    protected CodingSystem? outputCodingSystem_;
    protected Vector<LongOption<Char>> opts_ = new Vector<LongOption<Char>>();
    protected Vector<MessageType1> optDocs_ = new Vector<MessageType1>();
    protected Vector<MessageFragment> optArgs_ = new Vector<MessageFragment>();
    protected Vector<MessageType1> usages_ = new Vector<MessageType1>();
    protected Vector<MessageType1> preInfos_ = new Vector<MessageType1>();
    protected Vector<MessageType1> infos_ = new Vector<MessageType1>();
    protected Boolean internalCharsetIsDocCharset_;
    protected Ptr<CodingSystemKit> codingSystemKit_ = new Ptr<CodingSystemKit>();

    protected enum Action
    {
        normalAction,
        usageAction
    }
    protected Action action_;

    private CodingSystem? codingSystem_;

    // CmdLineApp(const char *requiredInternalCode = 0);
    public CmdLineApp(string? requiredInternalCode = null)
        : base(null)
    {
        errorFile_ = null;
        outputCodingSystem_ = null;
        internalCharsetIsDocCharset_ = true;
        codingSystem_ = null;
        action_ = Action.normalAction;

        initCodingSystem(requiredInternalCode);
        setMessageStream(makeStdErr());

        if (internalCharsetIsDocCharset_)
            registerOption('b', "bctf",
                           CmdLineAppMessages.name, CmdLineAppMessages.bHelp);
        else
            registerOption('b', "encoding",
                           CmdLineAppMessages.name, CmdLineAppMessages.eHelp);
        registerOption('f', "error-file",
                       CmdLineAppMessages.file, CmdLineAppMessages.fHelp);
        registerOption('v', "version", CmdLineAppMessages.vHelp);
        registerOption('h', "help", CmdLineAppMessages.hHelp);
        registerInfo(CmdLineAppMessages.usageStart, true);
    }

    // int run(int argc, AppChar **argv);
    public int run(string[] args)
    {
        try
        {
            int ret = init(args);
            if (ret != 0)
                return ret;
            int firstArg;
            ret = processOptions(args, out firstArg);
            if (ret != 0)
                return ret;
            // We do this here, so that the -b option works even if it is present after
            // the -h option.
            if (action_ == Action.usageAction)
            {
                usage();
                return 0;
            }
            string[] remainingArgs = new string[args.Length - firstArg];
            Array.Copy(args, firstArg, remainingArgs, 0, remainingArgs.Length);
            ret = processArguments(remainingArgs);
            return ret;
        }
        catch (OutOfMemoryException)
        {
            Console.Error.WriteLine("SP library: out of memory");
            return 1;
        }
    }

    // virtual int processOptions(int argc, AppChar **argv, int &nextArg);
    public virtual int processOptions(string[] args, out int nextArg)
    {
        nextArg = 0;

        // Convert string[] to Char[][] for Options class
        // Note: In C#, args[0] is the first argument, not the program name.
        // Options class expects argv[0] to be program name (starts at ind_=1).
        // We prepend a dummy program name entry.
        Char[][] argv = new Char[args.Length + 1][];
        argv[0] = new Char[] { (Char)'n', (Char)'s', (Char)'g', (Char)'m', (Char)'l', (Char)'s' }; // dummy program name
        for (int i = 0; i < args.Length; i++)
        {
            argv[i + 1] = new Char[args[i].Length];
            for (int j = 0; j < args[i].Length; j++)
                argv[i + 1][j] = args[i][j];
        }

        Options<Char> options = new Options<Char>(args.Length + 1, argv, opts_);
        Char opt;
        while (options.get(out opt))
        {
            switch (opt)
            {
                case '-':
                case '?':
                case '=':
                case ':':
                    {
                        StringC ostr = new StringC();
                        if (options.opt() == 0)
                        {
                            // Long option error
                            int idx = options.ind() - 1;
                            if (idx >= 0 && idx < args.Length)
                            {
                                string arg = args[idx];
                                for (int i = 2; i < arg.Length && i < 81; i++)
                                {
                                    if (arg[i] == '=' || arg[i] == '\0')
                                        break;
                                    ostr.operatorPlusAssign((Char)arg[i]);
                                }
                            }
                        }
                        else
                        {
                            ostr.operatorPlusAssign(options.opt());
                        }
                        message((opt == '-') ? CmdLineAppMessages.ambiguousOptionError
                               : ((opt == '=') ? CmdLineAppMessages.erroneousOptionArgError
                               : ((opt == ':') ? CmdLineAppMessages.missingOptionArgError
                               : CmdLineAppMessages.invalidOptionError)),
                              new StringMessageArg(ostr));
                        message(CmdLineAppMessages.tryHelpOptionForInfo);
                        return 1;
                    }
                default:
                    {
                        Char[]? argArray = options.arg();
                        string? argStr = null;
                        if (argArray != null)
                        {
                            char[] chars = new char[argArray.Length];
                            for (int i = 0; i < argArray.Length; i++)
                                chars[i] = (char)argArray[i];
                            argStr = new string(chars);
                        }
                        processOption((char)opt, argStr);
                        break;
                    }
            }
        }
        // Subtract 1 to account for dummy program name we added at argv[0]
        nextArg = options.ind() - 1;
        if (errorFile_ != null)
        {
            FileOutputByteStream file = new FileOutputByteStream();
            if (!file.open(errorFile_))
            {
                message(CmdLineAppMessages.openFileError,
                    new StringMessageArg(convertInput(errorFile_)),
                    new ErrnoMessageArg(0)); // errno not easily available in C#
                return 1;
            }
            setMessageStream(new EncodeOutputCharStream(file, codingSystem()!));
        }
        if (outputCodingSystem_ == null)
            outputCodingSystem_ = codingSystem();
        return 0;
    }

    // virtual void processOption(AppChar opt, const AppChar *arg);
    public virtual void processOption(char opt, string? arg)
    {
        switch (opt)
        {
            case 'b':
                outputCodingSystem_ = lookupCodingSystem(arg);
                if (outputCodingSystem_ == null)
                    message(internalCharsetIsDocCharset_
                          ? CmdLineAppMessages.unknownBctf
                          : CmdLineAppMessages.unknownEncoding,
                          new StringMessageArg(convertInput(arg ?? "")));
                break;
            case 'f':
                errorFile_ = arg;
                break;
            case 'v':
                // print the version number
                message(CmdLineAppMessages.versionInfo,
                    new StringMessageArg(codingSystem()!.convertIn(SP_PACKAGE)),
                    new StringMessageArg(codingSystem()!.convertIn(SP_VERSION)));
                break;
            case 'h':
                action_ = Action.usageAction;
                break;
            default:
                throw new InvalidOperationException("CANNOT_HAPPEN");
        }
    }

    // virtual int processArguments(int argc, AppChar **files) = 0;
    public abstract int processArguments(string[] files);

    // static const MessageType2 &openFileErrorMessage();
    public static MessageType2 openFileErrorMessage()
    {
        return CmdLineAppMessages.openFileError;
    }

    // static const MessageType2 &closeFileErrorMessage();
    public static MessageType2 closeFileErrorMessage()
    {
        return CmdLineAppMessages.closeFileError;
    }

    // void usage();
    public void usage()
    {
        Owner<OutputCharStream> stdOut = new Owner<OutputCharStream>(ConsoleOutput.makeOutputCharStream(1));
        if (stdOut.pointer() == null)
            stdOut = new Owner<OutputCharStream>(new EncodeOutputCharStream(
                new FileOutputByteStream(Console.OpenStandardOutput()), codingSystem()!));

        Vector<CopyOwner<MessageArg>> args = new Vector<CopyOwner<MessageArg>>(1);
        StringMessageArg arg = new StringMessageArg(convertInput("program"));
        args.push_back(new CopyOwner<MessageArg>(arg.copy()));

        if (usages_.size() == 0)
            usages_.push_back(CmdLineAppMessages.defaultUsage);

        for (nuint i = 0; i < usages_.size(); i++)
        {
            StrOutputCharStream ostr = new StrOutputCharStream();
            StringC tem = new StringC();
            formatMessage(usages_[i], args, ostr, true);
            ostr.extractString(tem);

            Vector<CopyOwner<MessageArg>> args2 = new Vector<CopyOwner<MessageArg>>(1);
            StringMessageArg arg2 = new StringMessageArg(tem);
            args2.push_back(new CopyOwner<MessageArg>(arg2.copy()));
            formatMessage(i != 0 ? CmdLineAppMessages.usageCont
                          : CmdLineAppMessages.usage,
                          args2, stdOut.pointer()!, true);
            stdOut.pointer()!.put('\n');
        }

        for (nuint i = 0; i < preInfos_.size(); i++)
        {
            formatMessage(preInfos_[i], args, stdOut.pointer()!, true);
            stdOut.pointer()!.put('\n');
        }

        // Display options
        Vector<StringC> leftSide = new Vector<StringC>();
        nuint leftSize = 0;
        for (nuint i = 0; i < opts_.size(); i++)
        {
            leftSide.resize(leftSide.size() + 1);
            StringC s = leftSide[leftSide.size() - 1];
            s.operatorPlusAssign((Char)' ');
            s.operatorPlusAssign((Char)' ');
            if (opts_[i].key != 0)
            {
                s.operatorPlusAssign((Char)'-');
                s.operatorPlusAssign(opts_[i].key);
                if (opts_[i].name != null)
                {
                    s.operatorPlusAssign((Char)',');
                    s.operatorPlusAssign((Char)' ');
                }
                else if (opts_[i].hasArgument)
                {
                    s.operatorPlusAssign((Char)' ');
                }
            }
            if (opts_[i].name != null)
            {
                s.operatorPlusAssign((Char)'-');
                s.operatorPlusAssign((Char)'-');
                for (nuint j = 0; j < (nuint)opts_[i].name!.Length; j++)
                    s.operatorPlusAssign(opts_[i].name![(int)j]);
                if (opts_[i].hasArgument)
                {
                    s.operatorPlusAssign((Char)'=');
                }
            }
            if (opts_[i].hasArgument)
            {
                StringC tem = new StringC();
                getMessageText(optArgs_[i], tem);
                for (nuint j = 0; j < tem.size(); j++)
                    s.operatorPlusAssign(tem[j]);
            }
            leftSide[leftSide.size() - 1] = s;
            if (s.size() > leftSize)
                leftSize = s.size();
        }
        leftSize += 2;

        for (nuint i = 0; i < opts_.size(); i++)
        {
            StringC left = leftSide[i];
            while (left.size() <= leftSize)
                left.operatorPlusAssign((Char)' ');
            leftSide[i] = left;

            StrOutputCharStream ostr = new StrOutputCharStream();
            Vector<CopyOwner<MessageArg>> args2 = new Vector<CopyOwner<MessageArg>>(1);
            StringC t = new StringC();
            getMessageText(optArgs_[i], t);
            StringMessageArg argT = new StringMessageArg(t);
            args2.push_back(new CopyOwner<MessageArg>(argT.copy()));
            formatMessage(optDocs_[i], args2, ostr, true);
            StringC tem = new StringC();
            ostr.extractString(tem);
            stdOut.pointer()!.operatorOutput(leftSide[i]);
            stdOut.pointer()!.operatorOutput(tem);
            stdOut.pointer()!.put((Char)'\n');
        }

        for (nuint i = 0; i < infos_.size(); i++)
        {
            formatMessage(infos_[i], args, stdOut.pointer()!, true);
            stdOut.pointer()!.put('\n');
        }
    }

    // const CodingSystem *codingSystem();
    public CodingSystem? codingSystem()
    {
        return codingSystem_;
    }

    // const CodingSystem *outputCodingSystem();
    public CodingSystem? outputCodingSystem()
    {
        return outputCodingSystem_;
    }

    // ConstPtr<InputCodingSystemKit> inputCodingSystemKit();
    public ConstPtr<InputCodingSystemKit> inputCodingSystemKit()
    {
        CodingSystemKit? kit = codingSystemKit_.pointer();
        if (kit != null)
            return new ConstPtr<InputCodingSystemKit>(kit);
        return new ConstPtr<InputCodingSystemKit>();
    }

    // const CharsetInfo &systemCharset();
    public CharsetInfo systemCharset()
    {
        return codingSystemKit_.pointer()!.systemCharset();
    }

    // StringC convertInput(const AppChar *s);
    public StringC convertInput(string s)
    {
        StringC str = new StringC();
        foreach (char c in s)
        {
            if (c == '\n')
                str.operatorPlusAssign((Char)'\r');
            else
                str.operatorPlusAssign((Char)c);
        }
        return str;
    }

    // OutputCharStream *makeStdOut();
    public OutputCharStream makeStdOut()
    {
        OutputCharStream? os = ConsoleOutput.makeOutputCharStream(1);
        if (os != null)
            return os;
        return new EncodeOutputCharStream(
            new FileOutputByteStream(Console.OpenStandardOutput()),
            outputCodingSystem_!);
    }

    // OutputCharStream *makeStdErr();
    public OutputCharStream makeStdErr()
    {
        OutputCharStream? os = ConsoleOutput.makeOutputCharStream(2);
        if (os != null)
            return os;
        return new EncodeOutputCharStream(
            new FileOutputByteStream(Console.OpenStandardError()),
            codingSystem()!);
    }

    // virtual void registerOption(AppChar c, const AppChar *name, const MessageType1 &doc);
    protected virtual void registerOption(char c, string? name, MessageType1 doc)
    {
        registerOption(c, name, CmdLineAppMessages.noArg, doc);
    }

    // virtual void registerOption(AppChar c, const AppChar *name, const MessageFragment &arg, const MessageType1 &doc);
    protected virtual void registerOption(char c, string? name, MessageFragment arg, MessageType1 doc)
    {
        LongOption<Char> opt = new LongOption<Char>();
        opt.value = (Char)c;
        opt.key = char.IsLetterOrDigit(c) ? (Char)c : (Char)0;
        if (name != null)
        {
            opt.name = new Char[name.Length];
            for (int i = 0; i < name.Length; i++)
                opt.name[i] = (Char)name[i];
        }
        opt.hasArgument = (arg.module() != CmdLineAppMessages.noArg.module()
                          || arg.number() != CmdLineAppMessages.noArg.number());

        // Check for existing option with same value
        for (nuint i = 0; i < opts_.size(); i++)
        {
            if (opts_[i].value == (Char)c)
            {
                // Move to end
                for (nuint j = i; j + 1 < opts_.size(); j++)
                {
                    opts_[j] = opts_[j + 1];
                    optArgs_[j] = optArgs_[j + 1];
                    optDocs_[j] = optDocs_[j + 1];
                }
                opts_[opts_.size() - 1] = opt;
                optArgs_[optArgs_.size() - 1] = arg;
                optDocs_[optDocs_.size() - 1] = doc;
                return;
            }
        }
        opts_.push_back(opt);
        optArgs_.push_back(arg);
        optDocs_.push_back(doc);
    }

    // virtual void changeOptionRegistration(AppChar oldc, AppChar newc);
    protected virtual void changeOptionRegistration(char oldc, char newc)
    {
        for (nuint i = 0; i < opts_.size(); i++)
        {
            if (opts_[i].value == (Char)oldc)
            {
                LongOption<Char> opt = opts_[i];
                opt.value = (Char)newc;
                opt.key = char.IsLetterOrDigit(newc) ? (Char)newc : (Char)0;
                opts_[i] = opt;
                return;
            }
        }
    }

    // virtual void registerUsage(const MessageType1 &u);
    protected virtual void registerUsage(MessageType1 u)
    {
        usages_.push_back(u);
    }

    // virtual void registerInfo(const MessageType1 &i, bool pre = 0);
    protected virtual void registerInfo(MessageType1 i, bool pre = false)
    {
        if (pre)
            preInfos_.push_back(i);
        else
            infos_.push_back(i);
    }

    // Backward compatibility - will not display argName
    protected virtual void registerOption(char c, string? argName = null)
    {
        if (argName != null)
            registerOption(c, null, CmdLineAppMessages.someArg, CmdLineAppMessages.undocOption);
        else
            registerOption(c, null, CmdLineAppMessages.undocOption);
    }

    // virtual int init(int argc, AppChar **argv);
    protected virtual int init(string[] argv)
    {
        if (argv.Length > 0)
            setProgramName(convertInput(argv[0]));
        return 0;
    }

    // void resetCodingSystemKit();
    protected void resetCodingSystemKit()
    {
        CodingSystemKit? kit = codingSystemKit_.pointer();
        if (kit != null)
            codingSystemKit_ = new Ptr<CodingSystemKit>(kit.copy());
    }

    // static Boolean stringMatches(const AppChar *s, const char *key);
    protected static Boolean stringMatches(string s, string key)
    {
        if (s.Length != key.Length)
            return false;
        for (int i = 0; i < key.Length; i++)
        {
            char sc = s[i];
            char kc = key[i];
            if (char.ToLower(sc) != char.ToLower(kc))
                return false;
        }
        return true;
    }

    // Boolean getMessageText(const MessageFragment &frag, StringC &text);
    // Override base class method - uses fragment's text directly
    public override Boolean getMessageText(MessageFragment frag, StringC text)
    {
        // Call base class implementation which gets text from fragment
        return base.getMessageText(frag, text);
    }

    // void initCodingSystem(const char *requiredInternalCode);
    private void initCodingSystem(string? requiredInternalCode)
    {
        string? name = requiredInternalCode;
        if (name == null)
        {
            string? internalCode = Environment.GetEnvironmentVariable("SP_SYSTEM_CHARSET");
            if (!string.IsNullOrEmpty(internalCode))
                name = internalCode;
        }
        if (requiredInternalCode != null)
            internalCharsetIsDocCharset_ = false;
        else
        {
            string? useInternal = Environment.GetEnvironmentVariable("SP_CHARSET_FIXED");
            if (useInternal != null
                && (stringMatches(useInternal, "YES")
                    || stringMatches(useInternal, "1")))
                internalCharsetIsDocCharset_ = false;
        }
        codingSystemKit_ = new Ptr<CodingSystemKit>(CodingSystemKit.make(name));

        string? codingName = Environment.GetEnvironmentVariable(
            internalCharsetIsDocCharset_ ? "SP_BCTF" : "SP_ENCODING");
        if (codingName != null)
            codingSystem_ = lookupCodingSystem(codingName);

        // Default encoding for non-Unix platforms
        if (codingSystem_ == null && !internalCharsetIsDocCharset_)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                codingSystem_ = lookupCodingSystem("WINDOWS");
            else
                codingSystem_ = lookupCodingSystem("IS8859-1");
        }

        if (codingSystem_ == null)
            codingSystem_ = codingSystemKit_.pointer()!.identityCodingSystem();
    }

    // const CodingSystem *lookupCodingSystem(const AppChar *codingName);
    private CodingSystem? lookupCodingSystem(string? codingName)
    {
        if (codingName == null || codingName.Length == 0 || codingName.Length >= 50)
            return null;
        return codingSystemKit_.pointer()!.makeCodingSystem(codingName, internalCharsetIsDocCharset_);
    }
}
