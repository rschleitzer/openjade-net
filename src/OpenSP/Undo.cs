// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public abstract class Undo : Link
{
    public Undo()
    {
    }

    public abstract void undo(ParserState parser);
}

public class UndoTransition : Undo
{
    private MatchState state_;

    public UndoTransition(MatchState state)
    {
        state_ = state;
    }

    public override void undo(ParserState parser)
    {
        parser.currentElement().setMatchState(state_);
    }
}

public class UndoStartTag : Undo
{
    public UndoStartTag()
    {
    }

    public override void undo(ParserState parser)
    {
        parser.popElement();
    }
}

public class UndoEndTag : Undo
{
    private Owner<OpenElement> element_ = new Owner<OpenElement>();

    public UndoEndTag(OpenElement? e)
    {
        element_ = new Owner<OpenElement>(e);
    }

    public override void undo(ParserState parser)
    {
        parser.pushElement(element_.extract());
    }
}
