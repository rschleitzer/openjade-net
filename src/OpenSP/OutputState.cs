// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class OutputStateLevel : Link
{
    public sbyte state;         // should be OutputState.State
    public ulong reSerial;
    public Location reLocation = new Location();

    public OutputStateLevel()
    {
        state = (sbyte)OutputState.State.afterStartTag;
    }

    public Boolean hasPendingRe()
    {
        return (int)state >= (int)OutputState.State.pendingAfterRsOrRe;
    }
}

public class OutputState
{
    public enum State
    {
        afterStartTag,
        afterRsOrRe,
        afterData,
        pendingAfterRsOrRe,
        pendingAfterMarkup
    }

    private IList<OutputStateLevel> stack_ = new IList<OutputStateLevel>();
    private Char re_;
    private ulong nextSerial_;

    public OutputState()
    {
        init();
    }

    public void init()
    {
        nextSerial_ = 0;
        stack_.clear();
        stack_.insert(new OutputStateLevel());
    }

    private OutputStateLevel top()
    {
        return stack_.head()!;
    }

    public void handleRe(EventHandler handler, Allocator alloc,
                         EventsWanted eventsWanted, Char re,
                         Location location)
    {
        re_ = re;
        if (eventsWanted.wantInstanceMarkup())
            handler.reOrigin(new ReOriginEvent(re_, new Location(location), nextSerial_));
        switch ((State)top().state)
        {
            case State.afterStartTag:
                // it's the first RE in the element
                if (eventsWanted.wantInstanceMarkup())
                    handler.ignoredRe(new IgnoredReEvent(re_, new Location(location), nextSerial_++));
                top().state = (sbyte)State.afterRsOrRe;
                break;
            case State.afterRsOrRe:
            case State.afterData:
                top().state = (sbyte)State.pendingAfterRsOrRe;
                top().reLocation = new Location(location);
                top().reSerial = nextSerial_++;
                break;
            case State.pendingAfterRsOrRe:
                // We now know that the pending RE won't be ignored as the last RE.
                handler.data(new ReEvent(new Char[] { re_ }, new Location(top().reLocation), top().reSerial));
                top().state = (sbyte)State.pendingAfterRsOrRe;
                top().reLocation = new Location(location);
                top().reSerial = nextSerial_++;
                break;
            case State.pendingAfterMarkup:
                // We've had only markup since the last RS or RE, so this
                // RE is ignored.  Note that it's this RE that's ignored, not
                // the pending one.
                if (eventsWanted.wantInstanceMarkup())
                    handler.ignoredRe(new IgnoredReEvent(re_, new Location(location), nextSerial_++));
                top().state = (sbyte)State.pendingAfterRsOrRe;
                break;
        }
    }

    public void noteRs(EventHandler handler, Allocator alloc, EventsWanted eventsWanted)
    {
        if (top().hasPendingRe())
            top().state = (sbyte)State.pendingAfterRsOrRe;
        else
            top().state = (sbyte)State.afterRsOrRe;
    }

    public void noteMarkup(EventHandler handler, Allocator alloc, EventsWanted eventsWanted)
    {
        switch ((State)top().state)
        {
            case State.afterRsOrRe:
                top().state = (sbyte)State.afterStartTag;
                break;
            case State.pendingAfterRsOrRe:
                top().state = (sbyte)State.pendingAfterMarkup;
                break;
            default:
                break;  // avoid warning
        }
    }

    public void noteData(EventHandler handler, Allocator alloc, EventsWanted eventsWanted)
    {
        if (top().hasPendingRe())
            handler.data(new ReEvent(new Char[] { re_ }, new Location(top().reLocation), top().reSerial));
        top().state = (sbyte)State.afterData;
    }

    public void noteStartElement(Boolean included, EventHandler handler,
                                 Allocator alloc, EventsWanted eventsWanted)
    {
        if (included)
            stack_.insert(new OutputStateLevel());
        else
        {
            if (top().hasPendingRe())
                handler.data(new ReEvent(new Char[] { re_ }, new Location(top().reLocation), top().reSerial));
            top().state = (sbyte)State.afterStartTag;
        }
    }

    public void noteEndElement(Boolean included, EventHandler handler,
                               Allocator alloc, EventsWanted eventsWanted)
    {
        if (eventsWanted.wantInstanceMarkup() && top().hasPendingRe())
            handler.ignoredRe(new IgnoredReEvent(re_, new Location(top().reLocation), top().reSerial));
        if (included)
        {
            OutputStateLevel? removed = stack_.get();
            // delete removed - handled by GC
            noteMarkup(handler, alloc, eventsWanted);
        }
        else
            top().state = (sbyte)State.afterData;
    }
}
