// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class TokenMessageArg : MessageArg
{
    private Token token_;
    private Mode mode_;
    private ConstPtr<Syntax> syntax_;
    private ConstPtr<Sd> sd_;

    // TokenMessageArg(Token token, Mode mode, const ConstPtr<Syntax> &syntax, const ConstPtr<Sd> &sd);
    public TokenMessageArg(Token token, Mode mode, ConstPtr<Syntax> syntax, ConstPtr<Sd> sd)
    {
        token_ = token;
        mode_ = mode;
        syntax_ = syntax;
        sd_ = sd;
    }

    // MessageArg *copy() const;
    public MessageArg copy()
    {
        return new TokenMessageArg(token_, mode_, syntax_, sd_);
    }

    // void append(MessageBuilder &) const;
    public void append(MessageBuilder builder)
    {
        // TODO: Implement token description based on mode and syntax
        builder.appendFragment(ParserMessages.name);
    }
}
