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

public class OtherMessageArg : MessageArg
{
    // RTTI_CLASS - C# has built-in RTTI

    public OtherMessageArg()
    {
    }

    public virtual MessageArg copy()
    {
        throw new NotImplementedException();
    }

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
