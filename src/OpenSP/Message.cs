// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class MessageFragment
{
    private ushort number_;
    private MessageModule? module_;
    protected byte spare_;
    private string? text_;

    public MessageFragment()
    {
    }

    public MessageFragment(MessageModule? module, uint number, string? text = null)
    {
        module_ = module;
        number_ = (ushort)number;
        text_ = text;
    }

    public MessageModule? module() => module_;
    public uint number() => number_;
    public string? text() => text_;
}

public class MessageType : MessageFragment
{
    public enum Severity
    {
        info,
        warning,
        quantityError,
        idrefError,
        error
    }

    private string? clauses_;
    private string? auxText_;

    public MessageType(
        Severity severity = Severity.info,
        MessageModule? module = null,
        uint number = unchecked((uint)-1),
        string? text = null,
        string? clauses = null,
        string? auxText = null)
        : base(module ?? MessageModules.libModule, number, text)
    {
        spare_ = (byte)severity;
        clauses_ = clauses;
        auxText_ = auxText;
    }

    public Severity severity() => (Severity)spare_;

    public MessageFragment auxFragment()
    {
        return new MessageFragment(module(), number() + 1, auxText_);
    }

    public Boolean isError()
    {
        return severity() != Severity.info && severity() != Severity.warning;
    }

    public string? clauses() => clauses_;
}

public class MessageType0 : MessageType
{
    public MessageType0(
        Severity severity = Severity.info,
        MessageModule? module = null,
        uint number = unchecked((uint)-1),
        string? text = null,
        string? clauses = null)
        : base(severity, module ?? MessageModules.libModule, number, text, clauses)
    {
    }
}

public class MessageType1 : MessageType
{
    public MessageType1(
        Severity severity = Severity.info,
        MessageModule? module = null,
        uint number = unchecked((uint)-1),
        string? text = null,
        string? clauses = null)
        : base(severity, module ?? MessageModules.libModule, number, text, clauses)
    {
    }
}

public class MessageType2 : MessageType
{
    public MessageType2(
        Severity severity = Severity.info,
        MessageModule? module = null,
        uint number = unchecked((uint)-1),
        string? text = null,
        string? clauses = null)
        : base(severity, module ?? MessageModules.libModule, number, text, clauses)
    {
    }
}

public class MessageType3 : MessageType
{
    public MessageType3(
        Severity severity = Severity.info,
        MessageModule? module = null,
        uint number = unchecked((uint)-1),
        string? text = null,
        string? clauses = null)
        : base(severity, module ?? MessageModules.libModule, number, text, clauses)
    {
    }
}

public class MessageType4 : MessageType
{
    public MessageType4(
        Severity severity = Severity.info,
        MessageModule? module = null,
        uint number = unchecked((uint)-1),
        string? text = null,
        string? clauses = null)
        : base(severity, module ?? MessageModules.libModule, number, text, clauses)
    {
    }
}

public class MessageType5 : MessageType
{
    public MessageType5(
        Severity severity = Severity.info,
        MessageModule? module = null,
        uint number = unchecked((uint)-1),
        string? text = null,
        string? clauses = null)
        : base(severity, module ?? MessageModules.libModule, number, text, clauses)
    {
    }
}

public class MessageType6 : MessageType
{
    public MessageType6(
        Severity severity = Severity.info,
        MessageModule? module = null,
        uint number = unchecked((uint)-1),
        string? text = null,
        string? clauses = null)
        : base(severity, module ?? MessageModules.libModule, number, text, clauses)
    {
    }
}

public class MessageType0L : MessageType
{
    public MessageType0L(
        Severity severity = Severity.info,
        MessageModule? module = null,
        uint number = unchecked((uint)-1),
        string? text = null,
        string? clauses = null,
        string? auxText = null)
        : base(severity, module ?? MessageModules.libModule, number, text, clauses, auxText)
    {
    }
}

public class MessageType1L : MessageType
{
    public MessageType1L(
        Severity severity = Severity.info,
        MessageModule? module = null,
        uint number = unchecked((uint)-1),
        string? text = null,
        string? clauses = null,
        string? auxText = null)
        : base(severity, module ?? MessageModules.libModule, number, text, clauses, auxText)
    {
    }
}

public class OpenElementInfo
{
    public PackedBoolean included;
    public StringC gi = new StringC();
    public StringC matchType = new StringC();
    public uint matchIndex;

    public OpenElementInfo()
    {
        included = false;
        matchIndex = 0;
    }

    public OpenElementInfo(OpenElementInfo x)
    {
        included = x.included;
        matchIndex = x.matchIndex;
        gi = new StringC(x.gi);
        matchType = new StringC(x.matchType);
    }
}

public class Message
{
    public MessageType? type;
    public Location loc = new Location();
    public Location auxLoc = new Location();
    public Vector<CopyOwner<MessageArg>> args = new Vector<CopyOwner<MessageArg>>();
    public Vector<OpenElementInfo> openElementInfo = new Vector<OpenElementInfo>();

    public Message()
    {
    }

    public Message(int nArgs)
    {
        args = new Vector<CopyOwner<MessageArg>>((nuint)nArgs);
    }

    // Copy constructor
    public Message(Message from)
    {
        type = from.type;
        loc = new Location(from.loc);
        auxLoc = new Location(from.auxLoc);
        // Deep copy args
        args = new Vector<CopyOwner<MessageArg>>(from.args.size());
        for (nuint i = 0; i < from.args.size(); i++)
        {
            MessageArg? arg = from.args[i].pointer();
            args[i] = new CopyOwner<MessageArg>(arg?.copy());
        }
        // Deep copy openElementInfo
        openElementInfo = new Vector<OpenElementInfo>(from.openElementInfo.size());
        for (nuint i = 0; i < from.openElementInfo.size(); i++)
            openElementInfo[i] = new OpenElementInfo(from.openElementInfo[i]);
    }

    public void swap(Message to)
    {
        MessageType? tem = type;
        type = to.type;
        to.type = tem;
        to.loc.swap(loc);
        to.auxLoc.swap(auxLoc);
        args.swap(to.args);
        openElementInfo.swap(to.openElementInfo);
    }

    public Boolean isError()
    {
        return type?.isError() ?? false;
    }
}

public abstract class Messenger
{
    private Boolean haveNextLocation_;
    private Location nextLocation_ = new Location();

    public Messenger()
    {
        haveNextLocation_ = false;
    }

    public void message(MessageType0 type)
    {
        Message msg = new Message(0);
        doInitMessage(msg);
        msg.type = type;
        dispatchMessage(msg);
    }

    public void message(MessageType1 type, MessageArg arg0)
    {
        Message msg = new Message(1);
        doInitMessage(msg);
        msg.args[0] = new CopyOwner<MessageArg>(arg0.copy());
        msg.type = type;
        dispatchMessage(msg);
    }

    public void message(MessageType2 type, MessageArg arg0, MessageArg arg1)
    {
        Message msg = new Message(2);
        doInitMessage(msg);
        msg.args[0] = new CopyOwner<MessageArg>(arg0.copy());
        msg.args[1] = new CopyOwner<MessageArg>(arg1.copy());
        msg.type = type;
        dispatchMessage(msg);
    }

    public void message(MessageType3 type, MessageArg arg0, MessageArg arg1, MessageArg arg2)
    {
        Message msg = new Message(3);
        doInitMessage(msg);
        msg.args[0] = new CopyOwner<MessageArg>(arg0.copy());
        msg.args[1] = new CopyOwner<MessageArg>(arg1.copy());
        msg.args[2] = new CopyOwner<MessageArg>(arg2.copy());
        msg.type = type;
        dispatchMessage(msg);
    }

    public void message(MessageType4 type, MessageArg arg0, MessageArg arg1, MessageArg arg2, MessageArg arg3)
    {
        Message msg = new Message(4);
        doInitMessage(msg);
        msg.args[0] = new CopyOwner<MessageArg>(arg0.copy());
        msg.args[1] = new CopyOwner<MessageArg>(arg1.copy());
        msg.args[2] = new CopyOwner<MessageArg>(arg2.copy());
        msg.args[3] = new CopyOwner<MessageArg>(arg3.copy());
        msg.type = type;
        dispatchMessage(msg);
    }

    public void message(MessageType5 type, MessageArg arg0, MessageArg arg1, MessageArg arg2, MessageArg arg3, MessageArg arg4)
    {
        Message msg = new Message(5);
        doInitMessage(msg);
        msg.args[0] = new CopyOwner<MessageArg>(arg0.copy());
        msg.args[1] = new CopyOwner<MessageArg>(arg1.copy());
        msg.args[2] = new CopyOwner<MessageArg>(arg2.copy());
        msg.args[3] = new CopyOwner<MessageArg>(arg3.copy());
        msg.args[4] = new CopyOwner<MessageArg>(arg4.copy());
        msg.type = type;
        dispatchMessage(msg);
    }

    public void message(MessageType6 type, MessageArg arg0, MessageArg arg1, MessageArg arg2, MessageArg arg3, MessageArg arg4, MessageArg arg5)
    {
        Message msg = new Message(6);
        doInitMessage(msg);
        msg.args[0] = new CopyOwner<MessageArg>(arg0.copy());
        msg.args[1] = new CopyOwner<MessageArg>(arg1.copy());
        msg.args[2] = new CopyOwner<MessageArg>(arg2.copy());
        msg.args[3] = new CopyOwner<MessageArg>(arg3.copy());
        msg.args[4] = new CopyOwner<MessageArg>(arg4.copy());
        msg.args[5] = new CopyOwner<MessageArg>(arg5.copy());
        msg.type = type;
        dispatchMessage(msg);
    }

    public void message(MessageType0L type, Location loc)
    {
        Message msg = new Message(0);
        doInitMessage(msg);
        msg.type = type;
        msg.auxLoc = new Location(loc);
        dispatchMessage(msg);
    }

    public void message(MessageType1L type, MessageArg arg0, Location loc)
    {
        Message msg = new Message(1);
        doInitMessage(msg);
        msg.args[0] = new CopyOwner<MessageArg>(arg0.copy());
        msg.type = type;
        msg.auxLoc = new Location(loc);
        dispatchMessage(msg);
    }

    public void setNextLocation(Location loc)
    {
        haveNextLocation_ = true;
        nextLocation_ = new Location(loc);
    }

    public virtual void initMessage(Message msg)
    {
    }

    public abstract void dispatchMessage(Message msg);

    public virtual void dispatchMessageConst(Message msg)
    {
        dispatchMessage(msg);
    }

    private void doInitMessage(Message msg)
    {
        initMessage(msg);
        if (haveNextLocation_)
        {
            msg.loc = new Location(nextLocation_);
            haveNextLocation_ = false;
        }
    }
}

public class ForwardingMessenger : Messenger
{
    private Messenger to_;

    public ForwardingMessenger(Messenger to)
    {
        to_ = to;
    }

    public override void dispatchMessage(Message msg)
    {
        to_.dispatchMessage(msg);
    }

    public override void dispatchMessageConst(Message msg)
    {
        to_.dispatchMessageConst(msg);
    }

    public override void initMessage(Message msg)
    {
        to_.initMessage(msg);
    }
}

public class ParentLocationMessenger : ForwardingMessenger
{
    public ParentLocationMessenger(Messenger mgr) : base(mgr)
    {
    }

    public override void initMessage(Message msg)
    {
        base.initMessage(msg);
        if (!msg.loc.origin().isNull())
            msg.loc = msg.loc.origin().pointer()!.parent();
    }
}

public class NullMessenger : Messenger
{
    public NullMessenger()
    {
    }

    public override void dispatchMessage(Message msg)
    {
        // Do nothing
    }
}
