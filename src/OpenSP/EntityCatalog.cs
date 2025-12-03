// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class EntityCatalog : Resource
{
    // Nested abstract class for syntax information
    public abstract class Syntax
    {
        public abstract Boolean namecaseGeneral();
        public abstract Boolean namecaseEntity();
        public abstract SubstTable upperSubstTable();
        public abstract StringC peroDelim();
    }

    // Adapter that wraps OpenSP.Syntax to implement EntityCatalog.Syntax
    public class SyntaxAdapter : Syntax
    {
        private readonly OpenSP.Syntax syntax_;

        public SyntaxAdapter(OpenSP.Syntax syntax)
        {
            syntax_ = syntax;
        }

        public override Boolean namecaseGeneral() => syntax_.namecaseGeneral();
        public override Boolean namecaseEntity() => syntax_.namecaseEntity();
        public override SubstTable upperSubstTable() => syntax_.upperSubstTable()!;
        public override StringC peroDelim() => syntax_.peroDelim();
    }

    // virtual ~EntityCatalog();
    // C# handles cleanup via GC

    // virtual Boolean sgmlDecl(const CharsetInfo &, Messenger &, const StringC &, StringC &) const;
    public virtual Boolean sgmlDecl(CharsetInfo charset, Messenger mgr,
                                    StringC systemId, StringC result)
    {
        return false;
    }

    // virtual Boolean lookup(const EntityDecl &, const Syntax &, const CharsetInfo &, Messenger &, StringC &) const;
    public virtual Boolean lookup(EntityDecl decl, Syntax syntax,
                                  CharsetInfo charset, Messenger mgr, StringC str)
    {
        StringC? p = decl.systemIdPointer();
        if (p == null)
            return false;
        str.operatorAssign(p);
        return true;
    }

    // virtual Boolean lookupPublic(const StringC &, const CharsetInfo &, Messenger &, StringC &) const;
    public virtual Boolean lookupPublic(StringC publicId, CharsetInfo charset,
                                        Messenger mgr, StringC result)
    {
        return false;
    }

    // virtual Boolean lookupChar(const StringC &, const CharsetInfo &, Messenger &, UnivChar &) const;
    public virtual Boolean lookupChar(StringC name, CharsetInfo charset,
                                      Messenger mgr, out UnivChar result)
    {
        result = 0;
        return false;
    }
}
