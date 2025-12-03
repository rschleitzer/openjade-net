// Copyright (c) 1994 James Clark, 1999 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

public interface MessageArg : ICopyable<MessageArg>
{
    void append(MessageBuilder builder);
}

public class StringMessageArg : MessageArg
{
    private StringC s_ = new StringC();

    public StringMessageArg(StringC s)
    {
        s_ = new StringC(s);
    }

    public MessageArg copy()
    {
        return new StringMessageArg(s_);
    }

    public void append(MessageBuilder builder)
    {
        builder.appendChars(s_.data(), s_.size());
    }
}

public class NumberMessageArg : MessageArg
{
    private ulong n_;

    public NumberMessageArg(ulong n)
    {
        n_ = n;
    }

    public MessageArg copy()
    {
        return new NumberMessageArg(n_);
    }

    public void append(MessageBuilder builder)
    {
        builder.appendNumber(n_);
    }
}

public class OrdinalMessageArg : MessageArg
{
    private ulong n_;

    public OrdinalMessageArg(ulong n)
    {
        n_ = n;
    }

    public MessageArg copy()
    {
        return new OrdinalMessageArg(n_);
    }

    public void append(MessageBuilder builder)
    {
        builder.appendOrdinal(n_);
    }
}

public abstract class OtherMessageArg : MessageArg
{
    // RTTI_CLASS - C# has built-in RTTI

    protected OtherMessageArg()
    {
    }

    // Interface method from ICopyable<MessageArg> - subclasses must implement
    public abstract MessageArg copy();

    public void append(MessageBuilder builder)
    {
        builder.appendOther(this);
    }
}

public class StringVectorMessageArg : MessageArg
{
    private Vector<StringC> v_ = new Vector<StringC>();

    public StringVectorMessageArg(Vector<StringC> v)
    {
        v_ = new Vector<StringC>(v);
    }

    public MessageArg copy()
    {
        return new StringVectorMessageArg(v_);
    }

    public void append(MessageBuilder builder)
    {
        for (nuint i = 0; i < v_.size(); i++)
        {
            if (i > 0)
                builder.appendFragment(ParserMessages.listSep);
            builder.appendChars(v_[i].data(), v_[i].size());
        }
    }
}

public class LocationMessageArg : OtherMessageArg
{
    private Location loc_ = new Location();

    public LocationMessageArg(Location loc)
    {
        loc_ = new Location(loc);
    }

    public override MessageArg copy()
    {
        return new LocationMessageArg(loc_);
    }

    public Location location()
    {
        return loc_;
    }
}

// CharsetMessageArg - displays a set of character codes as a formatted list
public class CharsetMessageArg : MessageArg
{
    private ISet<WideChar> set_;

    public CharsetMessageArg(ISet<WideChar> set)
    {
        set_ = new ISet<WideChar>(set);
    }

    public MessageArg copy()
    {
        return new CharsetMessageArg(set_);
    }

    public void append(MessageBuilder builder)
    {
        ISetIter<WideChar> iter = new ISetIter<WideChar>(set_);
        WideChar min, max;
        Boolean first = true;
        while (iter.next(out min, out max) != 0)
        {
            if (first)
                first = false;
            else
                builder.appendFragment(ParserMessages.listSep);
            builder.appendNumber(min);
            if (max != min)
            {
                builder.appendFragment(max == min + 1
                    ? ParserMessages.listSep
                    : ParserMessages.rangeSep);
                builder.appendNumber(max);
            }
        }
    }
}
