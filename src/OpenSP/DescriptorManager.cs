// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class DescriptorUser
{
    private DescriptorManager? manager_;

    // DescriptorUser(DescriptorManager *);
    public DescriptorUser(DescriptorManager? manager)
    {
        manager_ = manager;
        if (manager_ != null)
            manager_.addUser(this);
    }

    // virtual ~DescriptorUser();
    ~DescriptorUser()
    {
        if (manager_ != null)
            manager_.removeUser(this);
    }

    // virtual Boolean suspend();
    public virtual Boolean suspend()
    {
        return false;
    }

    // void managerDeleted();
    public void managerDeleted()
    {
        manager_ = null;
    }

    // void acquireD();
    public void acquireD()
    {
        if (manager_ != null)
            manager_.acquireD();
    }

    // void releaseD();
    public void releaseD()
    {
        if (manager_ != null)
            manager_.releaseD();
    }

    // DescriptorManager *manager() const;
    public DescriptorManager? manager()
    {
        return manager_;
    }
}

public class DescriptorManager
{
    private int usedD_;
    private int maxD_;
    private List<DescriptorUser> users_ = new List<DescriptorUser>();

    // DescriptorManager(int maxD);
    public DescriptorManager(int maxD)
    {
        maxD_ = maxD;
        usedD_ = 0;
    }

    // ~DescriptorManager();
    ~DescriptorManager()
    {
        for (ListIter<DescriptorUser> iter = new ListIter<DescriptorUser>(users_);
             iter.done() == 0;
             iter.next())
        {
            iter.cur()?.managerDeleted();
        }
    }

    // void acquireD();
    public void acquireD()
    {
        if (usedD_ >= maxD_)
        {
            for (ListIter<DescriptorUser> iter = new ListIter<DescriptorUser>(users_);
                 iter.done() == 0;
                 iter.next())
            {
                if (iter.cur()?.suspend() == true)
                    break;
            }
        }
        usedD_++;
    }

    // void releaseD();
    public void releaseD()
    {
        usedD_--;
    }

    // void addUser(DescriptorUser *p);
    public void addUser(DescriptorUser p)
    {
        users_.insert(p);
    }

    // void removeUser(DescriptorUser *p);
    public void removeUser(DescriptorUser p)
    {
        users_.remove(p);
    }
}
