// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

using System.Threading;

namespace OpenSP;

public class Mutex
{
    private readonly object cs_ = new object();

    public class Lock : IDisposable
    {
        private readonly Mutex? mp_;
        private bool disposed_;

        public Lock(Mutex? mp)
        {
            mp_ = mp;
            if (mp_ != null)
                Monitor.Enter(mp_.cs_);
        }

        public void Dispose()
        {
            if (!disposed_)
            {
                if (mp_ != null)
                    Monitor.Exit(mp_.cs_);
                disposed_ = true;
            }
        }
    }

    public Mutex()
    {
    }
}
