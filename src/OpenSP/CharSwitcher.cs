// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// CharSwitcher handles character switching in SGML declarations
public class CharSwitcher
{
    private Vector<PackedBoolean> switchUsed_ = new Vector<PackedBoolean>();
    private Vector<WideChar> switches_ = new Vector<WideChar>();

    // CharSwitcher();
    public CharSwitcher()
    {
    }

    // void addSwitch(WideChar from, WideChar to);
    public void addSwitch(WideChar from, WideChar to)
    {
        switches_.push_back(from);
        switches_.push_back(to);
        switchUsed_.push_back(false);
    }

    // SyntaxChar subst(WideChar c);
    public SyntaxChar subst(WideChar c)
    {
        for (nuint i = 0; i < switches_.size(); i += 2)
        {
            if (switches_[(int)i] == c)
            {
                switchUsed_[(int)(i / 2)] = true;
                return switches_[(int)(i + 1)];
            }
        }
        return c;
    }

    // size_t nSwitches() const;
    public nuint nSwitches()
    {
        return switchUsed_.size();
    }

    // Boolean switchUsed(size_t i) const;
    public Boolean switchUsed(nuint i)
    {
        return switchUsed_[(int)i];
    }

    // WideChar switchFrom(size_t i) const;
    public WideChar switchFrom(nuint i)
    {
        return switches_[(int)(i * 2)];
    }

    // WideChar switchTo(size_t i) const;
    public WideChar switchTo(nuint i)
    {
        return switches_[(int)(i * 2 + 1)];
    }
}
