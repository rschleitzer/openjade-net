// Copyright (c) 1994, 1995, 1997 James Clark
// See the file COPYING for copying permission.

using System;
using System.Runtime.InteropServices;

namespace OpenSP;

public abstract class MessageFormatter
{
    // MessageFormatter();
    public MessageFormatter()
    {
    }

    // virtual ~MessageFormatter();
    // C# GC handles cleanup

    // virtual void formatMessage(const MessageFragment &,
    //                            const Vector<CopyOwner<MessageArg>> &args,
    //                            OutputCharStream &, bool noquote = 0);
    public virtual void formatMessage(MessageFragment frag,
                                      Vector<CopyOwner<MessageArg>> args,
                                      OutputCharStream os,
                                      bool noquote = false)
    {
        StringC text = new StringC();
        if (!getMessageText(frag, text))
        {
            formatFragment(MessageFormatterMessages.invalidMessage, os);
            return;
        }
        Builder builder = new Builder(this, os, noquote || (text.size() == 2));
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
                        args[argIndex].pointer()!.append(builder);
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

    // virtual void formatOpenElements(const Vector<OpenElementInfo> &openElementInfo,
    //                                 OutputCharStream &os);
    public virtual void formatOpenElements(Vector<OpenElementInfo> openElementInfo,
                                           OutputCharStream os)
    {
        nuint nOpenElements = openElementInfo.size();
        for (nuint i = 0; ; i++)
        {
            if (i > 0
                && (i == nOpenElements || openElementInfo[i].included))
            {
                // describe last match in previous open element
                OpenElementInfo prevInfo = openElementInfo[i - 1];
                if (prevInfo.matchType.size() != 0)
                {
                    os.operatorOutput(" (").operatorOutput(prevInfo.matchType);
                    if (prevInfo.matchIndex != 0)
                        os.operatorOutput("[").operatorOutput(prevInfo.matchIndex).operatorOutput("]");
                    os.operatorOutput(")");
                }
            }
            if (i == nOpenElements)
                break;
            OpenElementInfo e = openElementInfo[i];
            os.operatorOutput(" ").operatorOutput(e.gi);
            if (i > 0 && !e.included)
            {
                ulong n = openElementInfo[i - 1].matchIndex;
                if (n != 0)
                    os.operatorOutput("[").operatorOutput(n).operatorOutput("]");
            }
        }
    }

    // virtual Boolean getMessageText(const MessageFragment &, StringC &) = 0;
    public abstract Boolean getMessageText(MessageFragment frag, StringC text);

    // virtual Boolean formatFragment(const MessageFragment &, OutputCharStream &);
    public virtual Boolean formatFragment(MessageFragment frag, OutputCharStream os)
    {
        StringC text = new StringC();
        if (!getMessageText(frag, text))
            return false;
        os.operatorOutput(text);
        return true;
    }

    // Nested Builder class
    protected class Builder : MessageBuilder
    {
        private OutputCharStream os_;
        private MessageFormatter formatter_;
        private bool argIsCompleteMessage_;

        // Builder(MessageFormatter *formatter, OutputCharStream &os, bool b)
        public Builder(MessageFormatter formatter, OutputCharStream os, bool argIsCompleteMessage)
        {
            os_ = os;
            formatter_ = formatter;
            argIsCompleteMessage_ = argIsCompleteMessage;
        }

        // virtual ~Builder();
        // C# GC handles cleanup

        // void appendNumber(unsigned long);
        public void appendNumber(ulong n)
        {
            os_.operatorOutput(n);
        }

        // void appendOrdinal(unsigned long);
        public void appendOrdinal(ulong n)
        {
            os_.operatorOutput(n);
            switch (n % 10)
            {
                case 1:
                    appendFragment(MessageFormatterMessages.ordinal1);
                    break;
                case 2:
                    appendFragment(MessageFormatterMessages.ordinal2);
                    break;
                case 3:
                    appendFragment(MessageFormatterMessages.ordinal3);
                    break;
                default:
                    appendFragment(MessageFormatterMessages.ordinaln);
                    break;
            }
        }

        // void appendChars(const Char *, size_t);
        public void appendChars(Char[]? p, nuint n)
        {
            if (p == null) return;
            if (argIsCompleteMessage_)
                os_.write(p, n);
            else
            {
                os_.put((Char)'"');
                os_.write(p, n);
                os_.put((Char)'"');
            }
        }

        // void appendOther(const OtherMessageArg *);
        public void appendOther(OtherMessageArg? p)
        {
            if (p is ErrnoMessageArg ea)
            {
                // Get system error message
                string errorMessage = GetErrorMessage(ea.errnum());
                os_.operatorOutput(errorMessage);
                return;
            }

            if (p is SearchResultMessageArg sr)
            {
                for (nuint i = 0; i < sr.nTried(); i++)
                {
                    if (i > 0)
                        os_.operatorOutput(", ");
                    StringC f = sr.filename(i);
                    Char[] data = new Char[f.size()];
                    for (nuint j = 0; j < f.size(); j++)
                        data[j] = f[j];
                    appendChars(data, f.size());
                    int err = sr.errnum(i);
                    // ENOENT is typically 2 on Unix systems
                    if (err != 2) // Skip "file not found" messages
                    {
                        os_.operatorOutput(" (");
                        os_.operatorOutput(GetErrorMessage(err));
                        os_.operatorOutput(")");
                    }
                }
                return;
            }
            appendFragment(MessageFormatterMessages.invalidArgumentType);
        }

        // void appendFragment(const MessageFragment &);
        public void appendFragment(MessageFragment frag)
        {
            formatter_.formatFragment(frag, os_);
        }

        // Helper to get error message from errno
        private static string GetErrorMessage(int errno)
        {
            // Use platform-specific error message retrieval
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return new System.ComponentModel.Win32Exception(errno).Message;
                }
                else
                {
                    // On Unix, use Marshal.GetLastPInvokeErrorMessage or similar
                    // For now, return a generic message
                    return $"Error {errno}";
                }
            }
            catch
            {
                return $"Error {errno}";
            }
        }
    }
}
