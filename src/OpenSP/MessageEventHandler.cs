// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Message event handler that dispatches messages and counts errors
// If parser is non-null then subdocs will be parsed automatically
public class MessageEventHandler : ErrorCountEventHandler
{
    private Messenger messenger_;
    private SgmlParser? parser_;

    // MessageEventHandler(Messenger *messenger, const SgmlParser *parser = 0);
    public MessageEventHandler(Messenger messenger, SgmlParser? parser = null)
        : base()
    {
        messenger_ = messenger;
        parser_ = parser;
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

    // void subdocEntity(SubdocEntityEvent *);
    public override void subdocEntity(SubdocEntityEvent? ev)
    {
        if (ev == null) return;
        SubdocEntity? entity = ev.entity();
        if (entity != null && parser_ != null)
        {
            SgmlParser.Params @params = new SgmlParser.Params();
            @params.subdocReferenced = true;
            @params.subdocInheritActiveLinkTypes = true;
            @params.origin = new Ptr<InputSourceOrigin>(ev.entityOrigin().pointer());
            @params.parent = parser_;
            @params.sysid = entity.externalId().effectiveSystemId();
            @params.entityType = SgmlParser.Params.EntityType.subdoc;
            SgmlParser parser = new SgmlParser(@params);
            SgmlParser? oldParser = parser_;
            parser_ = parser;
            parser.parseAll(this);
            parser_ = oldParser;
        }
    }

    // Messenger *messenger() const;
    public Messenger messenger()
    {
        return messenger_;
    }
}
