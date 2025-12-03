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
        // FIXME translate function characters in delimiters into
        // &#NAME; form.
        if (token_ >= Tokens.tokenFirstShortref)
        {
            builder.appendFragment(ParserMessages.shortrefDelim);
            return;
        }
        if (token_ == Tokens.tokenEe)
        {
            builder.appendFragment(ParserMessages.entityEnd);
            return;
        }
        ModeInfo iter = new ModeInfo(mode_, sd_.pointer()!);
        TokenInfo info = new TokenInfo();
        MessageFragment? fragment = null;
        while (iter.nextToken(info))
        {
            if (info.token == token_)
            {
                switch (info.type)
                {
                    case TokenInfo.Type.delimType:
                    case TokenInfo.Type.delimDelimType:
                    case TokenInfo.Type.delimSetType:
                        {
                            StringC delim = syntax_.pointer()!.delimGeneral((int)info.delim1);
                            builder.appendFragment(ParserMessages.delimStart);
                            builder.appendChars(delim.data(), delim.size());
                            // #if 0
                            // fragment = ParserMessages.delimEnd;
                            // #endif
                        }
                        break;
                    case TokenInfo.Type.setType:
                        switch (info.set)
                        {
                            case Syntax.Set.digit:
                                fragment = ParserMessages.digit;
                                break;
                            case Syntax.Set.nameStart:
                                fragment = ParserMessages.nameStartCharacter;
                                break;
                            case Syntax.Set.sepchar:
                                fragment = ParserMessages.sepchar;
                                break;
                            case Syntax.Set.s:
                                fragment = ParserMessages.separator;
                                break;
                            case Syntax.Set.nmchar:
                                fragment = ParserMessages.nameCharacter;
                                break;
                            case Syntax.Set.sgmlChar:
                                fragment = ParserMessages.dataCharacter;
                                break;
                            case Syntax.Set.minimumData:
                                fragment = ParserMessages.minimumDataCharacter;
                                break;
                            case Syntax.Set.significant:
                                fragment = ParserMessages.significantCharacter;
                                break;
                            default:
                                break; // CANNOT_HAPPEN()
                        }
                        break;
                    case TokenInfo.Type.functionType:
                        switch (info.function)
                        {
                            case Syntax.StandardFunction.fRE:
                                fragment = ParserMessages.recordEnd;
                                break;
                            case Syntax.StandardFunction.fRS:
                                fragment = ParserMessages.recordStart;
                                break;
                            case Syntax.StandardFunction.fSPACE:
                                fragment = ParserMessages.space;
                                break;
                        }
                        break;
                }
                break;
            }
        }
        if (fragment != null)
            builder.appendFragment(fragment);
    }
}
