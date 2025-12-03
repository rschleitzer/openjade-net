// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class SgmlParser
{
    public class Params
    {
        public enum EntityType
        {
            document,
            subdoc,
            dtd
        }

        public EntityType entityType;      // defaults to document
        public StringC sysid = new StringC();  // must be specified
        public Ptr<InputSourceOrigin> origin = new Ptr<InputSourceOrigin>();
        public Ptr<EntityManager> entityManager = new Ptr<EntityManager>();
        public SgmlParser? parent;
        public ConstPtr<Sd> sd = new ConstPtr<Sd>();
        public ConstPtr<Syntax> prologSyntax = new ConstPtr<Syntax>();
        public ConstPtr<Syntax> instanceSyntax = new ConstPtr<Syntax>();
        public uint subdocLevel;
        public ParserOptions? options;
        public PackedBoolean subdocInheritActiveLinkTypes;
        // referenced subdocs count against SUBDOC limit in SGML declaration
        public PackedBoolean subdocReferenced;
        public StringC doctypeName = new StringC();

        // Params();
        public Params()
        {
            entityType = EntityType.document;
            parent = null;
            options = null;
            subdocInheritActiveLinkTypes = false;
            subdocReferenced = false;
            subdocLevel = 0;
        }
    }

    internal Parser? parser_;

    // SgmlParser();
    public SgmlParser()
    {
        parser_ = null;
    }

    // SgmlParser(const Params &params);
    public SgmlParser(Params @params)
    {
        parser_ = new Parser(@params);
    }

    // void init(const Params &params);
    public void init(Params @params)
    {
        // delete parser_ - handled by GC
        parser_ = new Parser(@params);
    }

    // ~SgmlParser();
    // C# GC handles cleanup

    // Event *nextEvent();
    public Event? nextEvent()
    {
        return parser_?.nextEvent();
    }

    // void parseAll(EventHandler &, const volatile sig_atomic_t *cancelPtr);
    public void parseAll(EventHandler handler, int cancelPtr = 0)
    {
        parser_?.parseAll(handler, cancelPtr);
    }

    // ConstPtr<Sd> sd() const;
    public ConstPtr<Sd> sd()
    {
        return parser_?.sdPointer() ?? new ConstPtr<Sd>();
    }

    // ConstPtr<Syntax> instanceSyntax() const;
    public ConstPtr<Syntax> instanceSyntax()
    {
        return parser_?.instanceSyntaxPointer() ?? new ConstPtr<Syntax>();
    }

    // ConstPtr<Syntax> prologSyntax() const;
    public ConstPtr<Syntax> prologSyntax()
    {
        return parser_?.prologSyntaxPointer() ?? new ConstPtr<Syntax>();
    }

    // EntityManager &entityManager() const;
    public EntityManager entityManager()
    {
        return parser_!.entityManager();
    }

    // const EntityCatalog &entityCatalog() const;
    public EntityCatalog entityCatalog()
    {
        return parser_!.entityCatalog();
    }

    // const ParserOptions &options() const;
    public ParserOptions options()
    {
        return parser_!.options();
    }

    // Ptr<Dtd> baseDtd();
    public Ptr<Dtd> baseDtd()
    {
        return parser_?.baseDtd() ?? new Ptr<Dtd>();
    }

    // void activateLinkType(const StringC &);
    public void activateLinkType(StringC name)
    {
        parser_?.activateLinkType(name);
    }

    // void allLinkTypesActivated();
    public void allLinkTypesActivated()
    {
        parser_?.allLinkTypesActivated();
    }

    // void swap(SgmlParser &);
    public void swap(SgmlParser s)
    {
        Parser? tem = parser_;
        parser_ = s.parser_;
        s.parser_ = tem;
    }
}
