// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Trie
{
    private Trie[]? next_;
    private int nCodes_;
    private ushort token_;
    private byte tokenLength_;
    private byte priority_;
    private CopyOwner<BlankTrie> blank_ = new CopyOwner<BlankTrie>();

    // Trie() : next_(0), nCodes_(0) { }
    public Trie()
    {
        next_ = null;
        nCodes_ = 0;
        token_ = 0;
        tokenLength_ = 0;
        priority_ = 0;
    }

    // Trie(const Trie &);
    public Trie(Trie other)
    {
        nCodes_ = other.nCodes_;
        token_ = other.token_;
        tokenLength_ = other.tokenLength_;
        priority_ = other.priority_;
        if (other.blank_.pointer() != null)
            blank_ = new CopyOwner<BlankTrie>(other.blank_.pointer()!.copy());
        if (other.next_ != null)
        {
            next_ = new Trie[nCodes_];
            for (int i = 0; i < nCodes_; i++)
                next_[i] = new Trie(other.next_[i]);
        }
        else
        {
            next_ = null;
        }
    }

    // ~Trie();
    // C# GC handles cleanup

    // Trie &operator=(const Trie &);
    public void assign(Trie other)
    {
        if (this == other)
            return;
        next_ = null;
        nCodes_ = other.nCodes_;
        token_ = other.token_;
        tokenLength_ = other.tokenLength_;
        priority_ = other.priority_;
        if (other.blank_.pointer() != null)
            blank_ = new CopyOwner<BlankTrie>(other.blank_.pointer()!.copy());
        else
            blank_ = new CopyOwner<BlankTrie>();
        if (other.next_ != null)
        {
            next_ = new Trie[nCodes_];
            for (int i = 0; i < nCodes_; i++)
                next_[i] = new Trie(other.next_[i]);
        }
    }

    // const Trie *next(int i) const { return &next_[i]; }
    public Trie? next(int i)
    {
        return next_ != null ? next_[i] : null;
    }

    // Boolean hasNext() const { return next_ != 0; }
    public Boolean hasNext()
    {
        return next_ != null;
    }

    // Token token() const { return token_; }
    public Token token()
    {
        return token_;
    }

    // int tokenLength() const { return tokenLength_; }
    public int tokenLength()
    {
        return tokenLength_;
    }

    // const BlankTrie *blank() const;
    public BlankTrie? blank()
    {
        return blank_.pointer();
    }

    // Boolean includeBlanks() const { return Priority::isBlank(priority_); }
    public Boolean includeBlanks()
    {
        return Priority.isBlank(priority_);
    }

    // For TrieBuilder access
    internal Trie[]? getNext() => next_;
    internal void setNext(Trie[]? value) => next_ = value;
    internal int getNCodes() => nCodes_;
    internal void setNCodes(int value) => nCodes_ = value;
    internal void setToken(ushort value) => token_ = value;
    internal void setTokenLength(byte value) => tokenLength_ = value;
    internal void setPriority(byte value) => priority_ = value;
    internal CopyOwner<BlankTrie> getBlank() => blank_;
    internal void setBlank(CopyOwner<BlankTrie> value) => blank_ = value;
}

public class BlankTrie : Trie, ICopyable<BlankTrie>
{
    private byte additionalLength_;
    private nuint maxBlanksToScan_;
    private Vector<PackedBoolean> codeIsBlank_ = new Vector<PackedBoolean>();

    // BlankTrie() { }
    public BlankTrie() : base()
    {
        additionalLength_ = 0;
        maxBlanksToScan_ = 0;
    }

    // Copy constructor
    public BlankTrie(BlankTrie other) : base(other)
    {
        additionalLength_ = other.additionalLength_;
        maxBlanksToScan_ = other.maxBlanksToScan_;
        codeIsBlank_ = new Vector<PackedBoolean>(other.codeIsBlank_);
    }

    // Boolean codeIsBlank(EquivCode c) const { return codeIsBlank_[c]; }
    public Boolean codeIsBlank(EquivCode c)
    {
        return codeIsBlank_[(nuint)c];
    }

    // size_t maxBlanksToScan() const { return maxBlanksToScan_; }
    public nuint maxBlanksToScan()
    {
        return maxBlanksToScan_;
    }

    // int additionalLength() const { return additionalLength_; }
    public int additionalLength()
    {
        return additionalLength_;
    }

    // BlankTrie *copy() const { return new BlankTrie(*this); }
    public BlankTrie copy()
    {
        return new BlankTrie(this);
    }

    // For TrieBuilder access
    internal void setAdditionalLength(byte value) => additionalLength_ = value;
    internal void setMaxBlanksToScan(nuint value) => maxBlanksToScan_ = value;
    internal Vector<PackedBoolean> getCodeIsBlank() => codeIsBlank_;
}
