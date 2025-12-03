// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Message reporter that formats and outputs messages
// C++ version inherits from both MessageFormatter and Messenger
// C# version inherits from Messenger and includes formatting functionality
public class MessageReporter : Messenger
{
    [Flags]
    public enum Option
    {
        openElements = 0x01,
        openEntities = 0x02,
        messageNumbers = 0x04,
        clauses = 0x08
    }

    protected OutputCharStream? os_;
    protected uint options_;
    protected StringC programName_ = new StringC();

    // MessageReporter(OutputCharStream *);
    public MessageReporter(OutputCharStream? os)
    {
        os_ = os;
        options_ = 0;
    }

    // virtual ~MessageReporter();
    // C# GC handles cleanup

    // void setMessageStream(OutputCharStream *);
    public void setMessageStream(OutputCharStream? os)
    {
        os_ = os;
    }

    // OutputCharStream *releaseMessageStream();
    public OutputCharStream? releaseMessageStream()
    {
        OutputCharStream? tem = os_;
        os_ = null;
        return tem;
    }

    // void addOption(Option);
    public void addOption(Option option)
    {
        options_ |= (uint)option;
    }

    // void setProgramName(const StringC &);
    public void setProgramName(StringC programName)
    {
        programName_.assign(programName.data()!, programName.size());
    }

    // OutputCharStream &os();
    protected OutputCharStream os()
    {
        return os_!;
    }

    // virtual void dispatchMessage(const Message &);
    public override void dispatchMessage(Message message)
    {
        if (os_ == null)
            return;

        Offset off = 0;
        ExternalInfo? externalInfo = locationHeader(message.loc, ref off);
        if (programName_.size() > 0)
        {
            os_.operatorOutput(programName_);
            os_.put((Char)':');
        }
        if (externalInfo != null)
        {
            printLocation(externalInfo, off);
            os_.put((Char)':');
        }
        if ((options_ & (uint)Option.messageNumbers) != 0)
        {
            // Output module domain and message number
            MessageModule? mod = message.type!.module();
            if (mod?.domain != null)
                os_.operatorOutput(mod.domain);
            os_.put((Char)'.');
            os_.operatorOutput((ulong)message.type!.number());
            os_.put((Char)':');
        }
        switch (message.type!.severity())
        {
            case MessageType.Severity.info:
                formatFragment(MessageReporterMessages.infoTag, os_);
                break;
            case MessageType.Severity.warning:
                formatFragment(MessageReporterMessages.warningTag, os_);
                break;
            case MessageType.Severity.quantityError:
                formatFragment(MessageReporterMessages.quantityErrorTag, os_);
                break;
            case MessageType.Severity.idrefError:
                formatFragment(MessageReporterMessages.idrefErrorTag, os_);
                break;
            case MessageType.Severity.error:
                formatFragment(MessageReporterMessages.errorTag, os_);
                break;
        }
        os_.operatorOutput(": ");
        formatMessage(message.type!, message.args, os_);
        os_.put((Char)'\n');

        if ((options_ & (uint)Option.clauses) != 0 && message.type!.clauses() != null)
        {
            if (programName_.size() > 0)
            {
                os_.operatorOutput(programName_);
                os_.put((Char)':');
            }
            if (externalInfo != null)
            {
                printLocation(externalInfo, off);
                os_.operatorOutput(": ");
            }
            formatFragment(MessageReporterMessages.relevantClauses, os_);
            os_.operatorOutput(" ");
            os_.operatorOutput(message.type!.clauses()!);
            os_.put((Char)'\n');
        }

        if (!message.auxLoc.origin().isNull())
        {
            Offset auxOff = 0;
            ExternalInfo? auxExternalInfo = locationHeader(message.auxLoc, ref auxOff);
            if (programName_.size() > 0)
            {
                os_.operatorOutput(programName_);
                os_.put((Char)':');
            }
            if (auxExternalInfo != null)
            {
                printLocation(auxExternalInfo, auxOff);
                os_.operatorOutput(": ");
            }
            formatMessage(message.type!.auxFragment(), message.args, os_);
            os_.put((Char)'\n');
        }

        if ((options_ & (uint)Option.openElements) != 0 && message.openElementInfo.size() > 0)
        {
            if (programName_.size() > 0)
            {
                os_.operatorOutput(programName_);
                os_.put((Char)':');
            }
            if (externalInfo != null)
            {
                printLocation(externalInfo, off);
                os_.operatorOutput(": ");
            }
            formatFragment(MessageReporterMessages.openElements, os_);
            os_.put((Char)':');
            formatOpenElements(message.openElementInfo, os_);
            os_.put((Char)'\n');
        }

        os_.flush();
    }

    // virtual const ExternalInfo *locationHeader(const Location &, Offset &off);
    protected virtual ExternalInfo? locationHeader(Location loc, ref Offset off)
    {
        return locationHeader(loc.origin().pointer(), loc.index(), ref off);
    }

    // virtual const ExternalInfo *locationHeader(const Origin *, Index, Offset &off);
    protected virtual ExternalInfo? locationHeader(Origin? origin, Index index, ref Offset off)
    {
        if ((options_ & (uint)Option.openEntities) == 0)
        {
            while (origin != null)
            {
                ExternalInfo? externalInfo = origin.externalInfo();
                if (externalInfo != null)
                {
                    off = origin.startOffset(index);
                    return externalInfo;
                }
                Location loc = origin.parent();
                if (loc.origin().isNull())
                {
                    Origin? newOrigin;
                    Index newIndex;
                    if (!origin.defLocation(origin.startOffset(index), out newOrigin, out newIndex))
                        break;
                    origin = newOrigin;
                    index = newIndex;
                }
                else
                {
                    EntityOrigin? entityOrigin = origin.asEntityOrigin();
                    if (entityOrigin != null)
                        index = loc.index() + origin.refLength();
                    else
                        index += loc.index();
                    origin = loc.origin().pointer();
                }
            }
        }
        else
        {
            Boolean doneHeader = false;
            while (origin != null)
            {
                if (origin.entityName() != null || origin.parent().origin().isNull())
                {
                    if (!doneHeader)
                    {
                        Offset parentOff = 0;
                        Location parentLoc = origin.parent();
                        ExternalInfo? parentInfo = locationHeader(parentLoc.origin().pointer(),
                                                                   parentLoc.index() + origin.refLength(),
                                                                   ref parentOff);
                        if (parentInfo != null)
                        {
                            StringC text = new StringC();
                            if (getMessageText(origin.entityName() != null
                                ? MessageReporterMessages.inNamedEntity
                                : MessageReporterMessages.inUnnamedEntity, text))
                            {
                                for (nuint i = 0; i < text.size(); i++)
                                {
                                    if (text[i] == '%')
                                    {
                                        if (i + 1 < text.size())
                                        {
                                            i++;
                                            if (text[i] == '1')
                                                os_!.operatorOutput(origin.entityName()!);
                                            else if (text[i] == '2')
                                                printLocation(parentInfo, parentOff);
                                            else if (text[i] >= '3' && text[i] <= '9')
                                            {
                                                // skip unused placeholders %3-%9
                                            }
                                            else
                                                os_!.put(text[i]);
                                        }
                                    }
                                    else
                                        os_!.put(text[i]);
                                }
                                os_!.put((Char)'\n');
                            }
                        }
                        doneHeader = true;
                    }
                    off = origin.startOffset(index);
                    ExternalInfo? externalInfo = origin.externalInfo();
                    if (externalInfo != null)
                        return externalInfo;
                    Origin? newOrigin;
                    Index newIndex;
                    if (!origin.defLocation(off, out newOrigin, out newIndex))
                        break;
                    origin = newOrigin;
                    index = newIndex;
                }
                else
                {
                    Location loc = origin.parent();
                    EntityOrigin? entityOrigin = origin.asEntityOrigin();
                    if (entityOrigin != null)
                        index = loc.index() + origin.refLength();
                    else
                        index += loc.index();
                    origin = loc.origin().pointer();
                }
            }
        }
        return null;
    }

    // virtual void printLocation(const ExternalInfo *info, Offset off);
    protected virtual void printLocation(ExternalInfo? externalInfo, Offset off)
    {
        if (externalInfo == null)
        {
            formatFragment(MessageReporterMessages.invalidLocation, os_!);
            return;
        }

        StorageObjectLocation soLoc = new StorageObjectLocation();
        if (!ExtendEntityManager.externalize(externalInfo, off, soLoc))
        {
            formatFragment(MessageReporterMessages.invalidLocation, os_!);
            return;
        }

        if (soLoc.storageObjectSpec?.storageManager != null &&
            soLoc.storageObjectSpec.storageManager.type() != "OSFILE")
        {
            os_!.put((Char)'<');
            os_!.operatorOutput(soLoc.storageObjectSpec.storageManager.type());
            os_!.put((Char)'>');
        }
        os_!.operatorOutput(soLoc.actualStorageId);
        if (soLoc.lineNumber == unchecked((ulong)-1))
        {
            os_!.operatorOutput(": ");
            formatFragment(MessageReporterMessages.offset, os_!);
            os_!.operatorOutput(soLoc.storageObjectOffset);
        }
        else
        {
            os_!.put((Char)':');
            os_!.operatorOutput(soLoc.lineNumber);
            if (soLoc.columnNumber != 0 && soLoc.columnNumber != unchecked((ulong)-1))
            {
                os_!.put((Char)':');
                os_!.operatorOutput(soLoc.columnNumber - 1);
            }
        }
    }

    // virtual Boolean getMessageText(const MessageFragment &, StringC &);
    public virtual Boolean getMessageText(MessageFragment frag, StringC str)
    {
        string? p = frag.text();
        if (p == null)
            return false;
        str.resize(0);
        foreach (char c in p)
            str.operatorPlusAssign((Char)(byte)c);
        return true;
    }

    // Formatting methods (from MessageFormatter functionality)
    public virtual void formatMessage(MessageFragment frag, Vector<CopyOwner<MessageArg>> args, OutputCharStream os, bool noquote = false)
    {
        StringC text = new StringC();
        if (!getMessageText(frag, text))
        {
            formatFragment(MessageFormatterMessages.invalidMessage, os);
            return;
        }
        formatMessageText(text, args, os, noquote);
    }

    public virtual void formatMessage(MessageType type, Vector<CopyOwner<MessageArg>> args, OutputCharStream os, bool noquote = false)
    {
        StringC text = new StringC();
        if (!getMessageText(type, text))
        {
            formatFragment(MessageFormatterMessages.invalidMessage, os);
            return;
        }
        formatMessageText(text, args, os, noquote);
    }

    private void formatMessageText(StringC text, Vector<CopyOwner<MessageArg>> args, OutputCharStream os, bool noquote)
    {
        nuint i = 0;
        while (i < text.size())
        {
            if (text[i] == '%')
            {
                i++;
                if (i >= text.size())
                    break;
                if (text[i] >= '1' && text[i] <= '9')
                {
                    nuint argIndex = text[i] - (Char)'1';
                    if (argIndex < args.size())
                    {
                        // Simple char output for message args
                        MessageArg? arg = args[argIndex].pointer();
                        if (arg != null)
                        {
                            StringC argStr = new StringC();
                            arg.appendToStringC(argStr);
                            os.operatorOutput(argStr);
                        }
                    }
                }
                else
                    os.put(text[i]);
                i++;
            }
            else
            {
                os.put(text[i]);
                i++;
            }
        }
    }

    public virtual Boolean formatFragment(MessageFragment frag, OutputCharStream os)
    {
        StringC text = new StringC();
        if (!getMessageText(frag, text))
            return false;
        os.operatorOutput(text);
        return true;
    }

    public virtual void formatOpenElements(Vector<OpenElementInfo> openElementInfo, OutputCharStream os)
    {
        nuint nOpenElements = openElementInfo.size();
        for (nuint i = 0; ; i++)
        {
            if (i > 0 && (i == nOpenElements || openElementInfo[i].included))
            {
                OpenElementInfo prevInfo = openElementInfo[i - 1];
                if (prevInfo.matchType.size() != 0)
                {
                    os.operatorOutput(" (");
                    os.operatorOutput(prevInfo.matchType);
                    if (prevInfo.matchIndex != 0)
                    {
                        os.operatorOutput("[");
                        os.operatorOutput(prevInfo.matchIndex);
                        os.operatorOutput("]");
                    }
                    os.operatorOutput(")");
                }
            }
            if (i == nOpenElements)
                break;
            OpenElementInfo e = openElementInfo[i];
            os.operatorOutput(" ");
            os.operatorOutput(e.gi);
            if (i > 0 && !e.included)
            {
                ulong n = openElementInfo[i - 1].matchIndex;
                if (n != 0)
                {
                    os.operatorOutput("[");
                    os.operatorOutput(n);
                    os.operatorOutput("]");
                }
            }
        }
    }
}
