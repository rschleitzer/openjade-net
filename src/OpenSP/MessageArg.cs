// Copyright (c) 1994 James Clark, 1999 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

public interface MessageArg : ICopyable<MessageArg>
{
    void append(MessageBuilder builder);

    // appendToStringC - appends the argument value to a StringC
    // Default implementation uses a StringCMessageBuilder
    void appendToStringC(StringC s);
}

// Helper class to collect MessageArg output into a StringC
internal class StringCMessageBuilder : MessageBuilder
{
    private StringC result_;

    public StringCMessageBuilder(StringC result)
    {
        result_ = result;
    }

    public void appendNumber(ulong n)
    {
        string s = n.ToString();
        foreach (char c in s)
            result_.operatorPlusAssign((Char)c);
    }

    public void appendOrdinal(ulong n)
    {
        appendNumber(n);
        // Add ordinal suffix
        string suffix = "th";
        if (n % 100 < 10 || n % 100 > 20)
        {
            switch (n % 10)
            {
                case 1: suffix = "st"; break;
                case 2: suffix = "nd"; break;
                case 3: suffix = "rd"; break;
            }
        }
        foreach (char c in suffix)
            result_.operatorPlusAssign((Char)c);
    }

    public void appendChars(Char[]? data, nuint size)
    {
        if (data != null)
        {
            for (nuint i = 0; i < size; i++)
                result_.operatorPlusAssign(data[i]);
        }
    }

    public void appendOther(OtherMessageArg arg)
    {
        // For other args, just append a placeholder
        result_.operatorPlusAssign((Char)'?');
    }

    public void appendFragment(MessageFragment frag)
    {
        string? text = frag.text();
        if (text != null)
        {
            foreach (char c in text)
                result_.operatorPlusAssign((Char)c);
        }
    }
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

    public void appendToStringC(StringC result)
    {
        StringCMessageBuilder builder = new StringCMessageBuilder(result);
        append(builder);
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

    public void appendToStringC(StringC result)
    {
        StringCMessageBuilder builder = new StringCMessageBuilder(result);
        append(builder);
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

    public void appendToStringC(StringC result)
    {
        StringCMessageBuilder builder = new StringCMessageBuilder(result);
        append(builder);
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

    public void appendToStringC(StringC result)
    {
        StringCMessageBuilder builder = new StringCMessageBuilder(result);
        append(builder);
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

    public void appendToStringC(StringC result)
    {
        StringCMessageBuilder builder = new StringCMessageBuilder(result);
        append(builder);
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

    public void appendToStringC(StringC result)
    {
        StringCMessageBuilder builder = new StringCMessageBuilder(result);
        append(builder);
    }
}
