// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class GroupToken
{
    public enum Type
    {
        invalid,
        nameToken,
        name,
        dataTagLiteral,         // data tag (padding) template
        dataTagGroup,
        elementToken,
        modelGroup,
        pcdata,
        dataTagTemplateGroup,
        all,
        @implicit
    }

    public Type type;
    public StringC token = new StringC();       // name nameToken; with substitution
    public Owner<ModelGroup> model = new Owner<ModelGroup>();
    public Owner<ContentToken> contentToken = new Owner<ContentToken>();  // elementToken pcdata dataTagGroup
    public Text text = new Text();
    public Vector<Text> textVector = new Vector<Text>();

    public GroupToken()
    {
    }
}

public class AllowedGroupTokens
{
    private uint flags_;

    public AllowedGroupTokens(GroupToken.Type t1,
                              GroupToken.Type t2 = GroupToken.Type.invalid,
                              GroupToken.Type t3 = GroupToken.Type.invalid,
                              GroupToken.Type t4 = GroupToken.Type.invalid,
                              GroupToken.Type t5 = GroupToken.Type.invalid,
                              GroupToken.Type t6 = GroupToken.Type.invalid)
    {
        flags_ = 0;
        allow(t1);
        allow(t2);
        allow(t3);
        allow(t4);
        allow(t5);
        allow(t6);
    }

    public Boolean groupToken(GroupToken.Type i)
    {
        return ((1u << (int)i) & flags_) != 0;
    }

    // modelGroup, dataTagTemplateGroup
    public GroupToken.Type group()
    {
        if (groupToken(GroupToken.Type.modelGroup))
            return GroupToken.Type.modelGroup;
        else if (groupToken(GroupToken.Type.dataTagTemplateGroup))
            return GroupToken.Type.dataTagTemplateGroup;
        else
            return GroupToken.Type.invalid;
    }

    public GroupToken.Type nameStart()
    {
        if (groupToken(GroupToken.Type.elementToken))
            return GroupToken.Type.elementToken;
        else if (groupToken(GroupToken.Type.nameToken))
            return GroupToken.Type.nameToken;
        else if (groupToken(GroupToken.Type.name))
            return GroupToken.Type.name;
        else
            return GroupToken.Type.invalid;
    }

    private void allow(GroupToken.Type t)
    {
        flags_ |= (1u << (int)t);
    }
}

public struct GroupConnector
{
    public enum Type
    {
        andGC,
        orGC,
        seqGC,
        grpcGC,
        dtgcGC
    }

    public Type type;
}

public class AllowedGroupConnectors
{
    private uint flags_;

    public AllowedGroupConnectors(GroupConnector.Type c1)
    {
        flags_ = 0;
        allow(c1);
    }

    public AllowedGroupConnectors(GroupConnector.Type c1, GroupConnector.Type c2)
    {
        flags_ = 0;
        allow(c1);
        allow(c2);
    }

    public AllowedGroupConnectors(GroupConnector.Type c1, GroupConnector.Type c2,
                                  GroupConnector.Type c3)
    {
        flags_ = 0;
        allow(c1);
        allow(c2);
        allow(c3);
    }

    public AllowedGroupConnectors(GroupConnector.Type c1, GroupConnector.Type c2,
                                  GroupConnector.Type c3, GroupConnector.Type c4)
    {
        flags_ = 0;
        allow(c1);
        allow(c2);
        allow(c3);
        allow(c4);
    }

    public Boolean groupConnector(GroupConnector.Type c)
    {
        return (flags_ & (1u << (int)c)) != 0;
    }

    private void allow(GroupConnector.Type c)
    {
        flags_ |= (1u << (int)c);
    }
}

public class AllowedGroupTokensMessageArg : MessageArg
{
    private AllowedGroupTokens allow_;
    private ConstPtr<Syntax> syntax_;

    public AllowedGroupTokensMessageArg(AllowedGroupTokens allow, ConstPtr<Syntax> syntax)
    {
        allow_ = allow;
        syntax_ = syntax;
    }

    public MessageArg copy()
    {
        return new AllowedGroupTokensMessageArg(allow_, syntax_);
    }

    public void append(MessageBuilder builder)
    {
        MessageFragment?[] fragment = new MessageFragment?[4];
        int nFragments = 0;
        if (allow_.groupToken(GroupToken.Type.dataTagLiteral))
            fragment[nFragments++] = ParserMessages.parameterLiteral;
        if (allow_.groupToken(GroupToken.Type.dataTagGroup))
            fragment[nFragments++] = ParserMessages.dataTagGroup;
        switch (allow_.group())
        {
            case GroupToken.Type.modelGroup:
                fragment[nFragments++] = ParserMessages.modelGroup;
                break;
            case GroupToken.Type.dataTagTemplateGroup:
                fragment[nFragments++] = ParserMessages.dataTagTemplateGroup;
                break;
            default:
                break;
        }
        switch (allow_.nameStart())
        {
            case GroupToken.Type.name:
                fragment[nFragments++] = ParserMessages.name;
                break;
            case GroupToken.Type.nameToken:
                fragment[nFragments++] = ParserMessages.nameToken;
                break;
            case GroupToken.Type.elementToken:
                fragment[nFragments++] = ParserMessages.elementToken;
                break;
            default:
                break;
        }
        Boolean first = true;
        for (int i = 0; i < nFragments; i++)
        {
            if (!first)
                builder.appendFragment(ParserMessages.listSep);
            else
                first = false;
            builder.appendFragment(fragment[i]!);
        }
        if (allow_.groupToken(GroupToken.Type.pcdata))
        {
            if (!first)
                builder.appendFragment(ParserMessages.listSep);
            first = false;
            StringC pcdata = new StringC(syntax_.pointer()!.delimGeneral((int)Syntax.DelimGeneral.dRNI));
            pcdata.operatorPlusAssign(syntax_.pointer()!.reservedName(Syntax.ReservedName.rPCDATA));
            builder.appendChars(pcdata.data(), pcdata.size());
        }
        if (allow_.groupToken(GroupToken.Type.all))
        {
            if (!first)
                builder.appendFragment(ParserMessages.listSep);
            first = false;
            StringC all = new StringC(syntax_.pointer()!.delimGeneral((int)Syntax.DelimGeneral.dRNI));
            all.operatorPlusAssign(syntax_.pointer()!.reservedName(Syntax.ReservedName.rALL));
            builder.appendChars(all.data(), all.size());
        }
        if (allow_.groupToken(GroupToken.Type.@implicit))
        {
            if (!first)
                builder.appendFragment(ParserMessages.listSep);
            StringC @implicit = new StringC(syntax_.pointer()!.delimGeneral((int)Syntax.DelimGeneral.dRNI));
            @implicit.operatorPlusAssign(syntax_.pointer()!.reservedName(Syntax.ReservedName.rIMPLICIT));
            builder.appendChars(@implicit.data(), @implicit.size());
        }
    }

    public void appendToStringC(StringC result)
    {
        StringCMessageBuilder builder = new StringCMessageBuilder(result);
        append(builder);
    }
}

public class AllowedGroupConnectorsMessageArg : MessageArg
{
    private AllowedGroupConnectors allow_;
    private ConstPtr<Syntax> syntax_;

    public AllowedGroupConnectorsMessageArg(AllowedGroupConnectors allow, ConstPtr<Syntax> syntax)
    {
        allow_ = allow;
        syntax_ = syntax;
    }

    public MessageArg copy()
    {
        return new AllowedGroupConnectorsMessageArg(allow_, syntax_);
    }

    public void append(MessageBuilder builder)
    {
        GroupConnector.Type[] types = new GroupConnector.Type[]
        {
            GroupConnector.Type.andGC, GroupConnector.Type.orGC, GroupConnector.Type.seqGC,
            GroupConnector.Type.grpcGC, GroupConnector.Type.dtgcGC
        };
        Syntax.DelimGeneral[] delims = new Syntax.DelimGeneral[]
        {
            Syntax.DelimGeneral.dAND, Syntax.DelimGeneral.dOR, Syntax.DelimGeneral.dSEQ,
            Syntax.DelimGeneral.dGRPC, Syntax.DelimGeneral.dDTGC
        };
        Boolean first = true;
        for (nuint i = 0; i < (nuint)types.Length; i++)
        {
            if (allow_.groupConnector(types[i]))
            {
                if (!first)
                    builder.appendFragment(ParserMessages.listSep);
                else
                    first = false;
                StringC delim = syntax_.pointer()!.delimGeneral((int)delims[i]);
                builder.appendFragment(ParserMessages.delimStart);
                builder.appendChars(delim.data(), delim.size());
            }
        }
    }

    public void appendToStringC(StringC result)
    {
        StringCMessageBuilder builder = new StringCMessageBuilder(result);
        append(builder);
    }
}
