// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class EventsWanted
{
    private PackedBoolean instanceMarkup_;
    private PackedBoolean commentDecls_;
    private PackedBoolean markedSections_;
    private PackedBoolean prologMarkup_;

    // EventsWanted()
    // : instanceMarkup_(0), commentDecls_(0), markedSections_(0), prologMarkup_(0)
    public EventsWanted()
    {
        instanceMarkup_ = false;
        commentDecls_ = false;
        markedSections_ = false;
        prologMarkup_ = false;
    }

    // Boolean wantInstanceMarkup() const
    public Boolean wantInstanceMarkup()
    {
        return instanceMarkup_;
    }

    // void addInstanceMarkup()
    public void addInstanceMarkup()
    {
        instanceMarkup_ = true;
        commentDecls_ = true;
        markedSections_ = true;
    }

    // Boolean wantCommentDecls() const // in instance
    public Boolean wantCommentDecls()
    {
        return commentDecls_;
    }

    // void addCommentDecls()
    public void addCommentDecls()
    {
        commentDecls_ = true;
    }

    // Boolean wantMarkedSections() const // in instance
    public Boolean wantMarkedSections()
    {
        return markedSections_;
    }

    // void addMarkedSections()
    public void addMarkedSections()
    {
        markedSections_ = true;
    }

    // Boolean wantPrologMarkup() const
    public Boolean wantPrologMarkup()
    {
        return prologMarkup_;
    }

    // void addPrologMarkup()
    public void addPrologMarkup()
    {
        prologMarkup_ = true;
    }
}
