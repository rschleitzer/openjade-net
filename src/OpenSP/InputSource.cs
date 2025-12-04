// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public abstract class InputSource : Link
{
    public const int eE = -1; // end of entity signal

    private Char[]? cur_;
    private nuint curIndex_;
    private Char[]? start_;
    private nuint startIndex_;
    private Char[]? end_;
    private nuint endIndex_;
    private Location startLocation_ = new Location();
    private Ptr<InputSourceOrigin> origin_ = new Ptr<InputSourceOrigin>();
    private Boolean accessError_;
    private Boolean scanSuppress_;
    private Boolean scanSuppressSingle_;
    private Index scanSuppressIndex_;
    private Boolean multicode_;
    private XcharMap<byte> markupScanTable_ = new XcharMap<byte>();

    // InputSource(InputSourceOrigin *origin, const Char *start, const Char *end);
    protected InputSource(InputSourceOrigin? origin, Char[]? start, nuint startIdx, Char[]? end, nuint endIdx)
    {
        origin_ = new Ptr<InputSourceOrigin>(origin);
        start_ = start;
        startIndex_ = startIdx;
        end_ = end;
        endIndex_ = endIdx;
        cur_ = start;
        curIndex_ = startIdx;
        accessError_ = false;
        startLocation_ = new Location(origin, 0);
        multicode_ = false;
        scanSuppress_ = false;
    }

    // virtual ~InputSource();
    // C# handles cleanup via GC

    // void reset(const Char *start, const Char *end);
    protected void reset(Char[]? start, nuint startIdx, Char[]? end, nuint endIdx)
    {
        origin_ = new Ptr<InputSourceOrigin>(origin_.pointer()?.copy());
        start_ = start;
        startIndex_ = startIdx;
        end_ = end;
        endIndex_ = endIdx;
        cur_ = start;
        curIndex_ = startIdx;
        startLocation_ = new Location(origin_.pointer(), 0);
        multicode_ = false;
        scanSuppress_ = false;
        markupScanTable_.clear();
    }

    // Xchar get(Messenger &);
    public Xchar get(Messenger mgr)
    {
        advanceStart(cur_, curIndex_);
        if (cur_ != null && curIndex_ < endIndex_)
            return (Xchar)cur_[curIndex_++];
        return fill(mgr);
    }

    // virtual void pushCharRef(Char ch, const NamedCharRef &) = 0;
    public abstract void pushCharRef(Char ch, NamedCharRef charRef);

    // const Location &currentLocation() const;
    public Location currentLocation()
    {
        return startLocation_;
    }

    // const Char *currentTokenStart() const;
    public Char[]? currentTokenStart()
    {
        return start_;
    }

    // Returns the index within currentTokenStart() where the token begins
    // In C++, currentTokenStart() returns a pointer already at the right offset;
    // in C# we need to use array + index separately
    public nuint currentTokenStartIndex()
    {
        return startIndex_;
    }

    // size_t currentTokenLength() const;
    public nuint currentTokenLength()
    {
        return curIndex_ - startIndex_;
    }

    // const Char *currentTokenEnd() const;
    public Char[]? currentTokenEnd()
    {
        return cur_;
    }

    // Index nextIndex() const;
    public Index nextIndex()
    {
        return startLocation_.index() + (Index)(curIndex_ - startIndex_);
    }

    // void discardInitial();
    public void discardInitial()
    {
        advanceStart(cur_, curIndex_ - 1);
    }

    // void startToken();
    public void startToken()
    {
        advanceStart(cur_, curIndex_);
    }

    // void startTokenNoMulticode();
    public void startTokenNoMulticode()
    {
        startLocation_.operatorPlusAssign((Index)(curIndex_ - startIndex_));
        start_ = cur_;
        startIndex_ = curIndex_;
    }

    // void endToken(size_t length);
    public void endToken(nuint length)
    {
        curIndex_ = startIndex_ + length;
    }

    // Xchar tokenChar(Messenger &);
    public Xchar tokenChar(Messenger mgr)
    {
        if (cur_ != null && curIndex_ < endIndex_)
            return (Xchar)cur_[curIndex_++];
        return fill(mgr);
    }

    // Xchar tokenCharInBuffer(Messenger &);
    public Xchar tokenCharInBuffer(Messenger mgr)
    {
        if (cur_ != null && curIndex_ < endIndex_)
            return (Xchar)cur_[curIndex_++];
        return eE;
    }

    // void ungetToken();
    public void ungetToken()
    {
        curIndex_ = startIndex_;
    }

    // void setMarkupScanTable(const XcharMap<unsigned char> &);
    public void setMarkupScanTable(XcharMap<byte> table)
    {
        markupScanTable_ = table;
        multicode_ = true;
    }

    // Boolean scanSuppress() const;
    public Boolean scanSuppress()
    {
        return scanSuppress_ && (!scanSuppressSingle_
                                 || startLocation_.index() == scanSuppressIndex_);
    }

    // void extendToBufferEnd();
    public void extendToBufferEnd()
    {
        curIndex_ = endIndex_;
    }

    // virtual void willNotRewind();
    public virtual void willNotRewind()
    {
    }

    // virtual Boolean rewind(Messenger &) = 0;
    public abstract Boolean rewind(Messenger mgr);

    // Boolean accessError() const;
    public Boolean accessError()
    {
        return accessError_;
    }

    // virtual void setDocCharset(const CharsetInfo &docCharset, const CharsetInfo &emCharset);
    public virtual void setDocCharset(CharsetInfo docCharset, CharsetInfo emCharset)
    {
    }

    // virtual void willNotSetDocCharset();
    public virtual void willNotSetDocCharset()
    {
    }

    // virtual InternalInputSource *asInternalInputSource();
    public virtual InternalInputSource? asInternalInputSource()
    {
        return null;
    }

    // InputSourceOrigin *inputSourceOrigin();
    protected InputSourceOrigin? inputSourceOrigin()
    {
        return origin_.pointer();
    }

    // void noteCharRef(Index replacementIndex, const NamedCharRef &);
    protected void noteCharRef(Index replacementIndex, NamedCharRef charRef)
    {
        origin_.pointer()?.noteCharRef(replacementIndex, charRef);
    }

    // const Char *cur();
    protected nuint curIdx()
    {
        return curIndex_;
    }

    // Advance curIndex_ by 1 (for ExternalInputSource record processing)
    protected void advanceCur()
    {
        curIndex_++;
    }

    // Initialize buffer pointers without adjusting indices (for ExternalInputSource)
    protected void initializeBuffer(Char[] buf)
    {
        cur_ = buf;
        start_ = buf;
        end_ = buf;
        // Don't change curIndex_, startIndex_, or endIndex_ - they're already 0
    }

    // Update buffer pointer after reallocation (preserves indices)
    protected void updateBufferPointer(Char[] buf)
    {
        cur_ = buf;
        start_ = buf;
        end_ = buf;
        // Indices stay the same since data was copied to same positions
    }

    // const Char *start();
    protected nuint startIdx()
    {
        return startIndex_;
    }

    // const Char *end();
    protected nuint endIdx()
    {
        return endIndex_;
    }

    // Index startIndex();
    protected Index getStartIndex()
    {
        return startLocation_.index();
    }

    // void changeBuffer(const Char *newBase, const Char *oldBase);
    // In C++, this adjusts pointers by (newBase - oldBase).
    // In C#, we just update the array references - indices stay the same because
    // the data positions don't change when we resize with Array.Copy.
    protected void changeBuffer(Char[]? newBase, Char[]? oldBase)
    {
        // Only update the array references - indices remain the same
        // because the data was copied to the same positions in the new buffer
        cur_ = newBase;
        start_ = newBase;
        end_ = newBase;
        // NOTE: Don't modify curIndex_, startIndex_, or endIndex_ here!
        // The data positions are the same, so the indices should stay the same.
    }

    // void advanceEnd(const Char *newEnd);
    protected void advanceEnd(nuint newEnd)
    {
        endIndex_ = newEnd;
    }

    // void moveLeft();
    protected void moveLeft()
    {
        startIndex_--;
        curIndex_--;
    }

    // void moveStart(const Char *newStart);
    protected void moveStart(Char[]? newStart, nuint newStartIdx)
    {
        nuint curOffset = curIndex_ - startIndex_;
        nuint endOffset = endIndex_ - startIndex_;
        cur_ = newStart;
        end_ = newStart;
        start_ = newStart;
        startIndex_ = newStartIdx;
        curIndex_ = newStartIdx + curOffset;
        endIndex_ = newStartIdx + endOffset;
    }

    // Char nextChar();
    protected Char nextChar()
    {
        return cur_![curIndex_++];
    }

    // void setAccessError();
    protected void setAccessError()
    {
        accessError_ = true;
    }

    // virtual Xchar fill(Messenger &) = 0;
    protected abstract Xchar fill(Messenger mgr);

    // void advanceStart(const Char *to);
    private void advanceStart(Char[]? to, nuint toIdx)
    {
        if (multicode_)
            advanceStartMulticode(to, toIdx);
        else
        {
            startLocation_.operatorPlusAssign((Index)(toIdx - startIndex_));
            start_ = to;
            startIndex_ = toIdx;
        }
    }

    // void advanceStartMulticode(const Char *to);
    private void advanceStartMulticode(Char[]? to, nuint toIdx)
    {
        while (startIndex_ < toIdx)
        {
            switch ((MarkupScan.Type)markupScanTable_[(Xchar)start_![startIndex_]])
            {
                case MarkupScan.Type.normal:
                    break;
                case MarkupScan.Type.@in:
                    scanSuppress_ = false;
                    break;
                case MarkupScan.Type.@out:
                    if (!scanSuppress())
                    {
                        scanSuppress_ = true;
                        scanSuppressSingle_ = false;
                    }
                    break;
                case MarkupScan.Type.suppress:
                    if (!scanSuppress())
                    {
                        scanSuppress_ = true;
                        scanSuppressSingle_ = true;
                        scanSuppressIndex_ = startLocation_.index() + 1;
                    }
                    break;
            }
            startIndex_++;
            startLocation_.operatorPlusAssign(1);
        }
        start_ = to;
    }
}

public class InternalInputSource : InputSource
{
    private Char[]? buf_;
    private StringC? contents_;

    // InternalInputSource(const StringC &str, InputSourceOrigin *origin);
    public InternalInputSource(StringC str, InputSourceOrigin? origin)
        : base(origin, str.data(), 0, str.data(), (nuint)str.size())
    {
        buf_ = null;
        contents_ = str;
    }

    // ~InternalInputSource();
    // C# GC handles cleanup

    // Xchar fill(Messenger &);
    protected override Xchar fill(Messenger mgr)
    {
        return eE;
    }

    // void pushCharRef(Char ch, const NamedCharRef &);
    public override void pushCharRef(Char ch, NamedCharRef charRef)
    {
        System.Diagnostics.Debug.Assert(curIdx() == startIdx());
        noteCharRef(getStartIndex() + (Index)(curIdx() - startIdx()), charRef);
        if (buf_ == null)
        {
            nuint len = endIdx() - startIdx();
            buf_ = new Char[len + 1];
            // Copy contents starting at index 1
            Char[]? curBuf = currentTokenStart();
            if (curBuf != null)
            {
                for (nuint i = 0; i < len; i++)
                    buf_[i + 1] = curBuf[startIdx() + i];
            }
            // Change buffer: new base starts at index 1
            changeBuffer(buf_, currentTokenStart());
        }
        moveLeft();
        // Set the character at current position
        buf_![curIdx()] = ch;
    }

    // Boolean rewind(Messenger &);
    public override Boolean rewind(Messenger mgr)
    {
        if (contents_ != null)
        {
            reset(contents_.data(), 0, contents_.data(), (nuint)contents_.size());
        }
        if (buf_ != null)
        {
            buf_ = null;
        }
        return true;
    }

    // const StringC *contents();
    public StringC? contents()
    {
        return contents_;
    }

    // InternalInputSource *asInternalInputSource();
    public override InternalInputSource? asInternalInputSource()
    {
        return this;
    }
}
