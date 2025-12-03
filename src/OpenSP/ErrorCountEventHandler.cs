// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Event handler that counts errors and can cancel parsing when limit reached
public class ErrorCountEventHandler : EventHandler
{
    private uint maxErrors_;
    private uint errorCount_;
    private int cancel_;

    // ErrorCountEventHandler(unsigned errorLimit = 0);
    public ErrorCountEventHandler(uint errorLimit = 0)
    {
        errorCount_ = 0;
        maxErrors_ = errorLimit;
        cancel_ = 0;
    }

    // void setErrorLimit(unsigned maxErrors);
    public void setErrorLimit(uint maxErrors)
    {
        maxErrors_ = maxErrors;
    }

    // const sig_atomic_t *cancelPtr() const;
    public int cancelPtr()
    {
        return cancel_;
    }

    // void cancel();
    public void cancel()
    {
        cancel_ = 1;
    }

    // Boolean cancelled() const;
    public Boolean cancelled()
    {
        return cancel_ != 0;
    }

    // unsigned errorCount() const;
    public uint errorCount()
    {
        return errorCount_;
    }

    // void message(MessageEvent *);
    public override void message(MessageEvent ev)
    {
        noteMessage(ev.message());
    }

    // void noteMessage(const Message &);
    public void noteMessage(Message message)
    {
        if (message.isError())
        {
            errorCount_++;
            if (maxErrors_ != 0 && errorCount_ >= maxErrors_)
                cancel_ = 1;
        }
    }
}
