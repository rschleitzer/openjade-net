// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class TrieBuilder
{
    private int nCodes_;
    private Owner<Trie> root_ = new Owner<Trie>();

    // TrieBuilder(int nCodes);
    public TrieBuilder(int nCodes)
    {
        nCodes_ = nCodes;
        root_ = new Owner<Trie>(new Trie());
        root_.pointer()!.setToken(0);
        root_.pointer()!.setTokenLength(0);
        root_.pointer()!.setPriority(Priority.data);
        root_.pointer()!.setNCodes(nCodes);
    }

    // void recognize(const String<EquivCode> &chars, Token t, Priority::Type pri, TokenVector &ambiguities);
    public void recognize(String<EquivCode> chars, Token t, byte priority, Vector<Token> ambiguities)
    {
        setToken(extendTrie(root_.pointer()!, chars), (int)chars.size(), t, priority, ambiguities);
    }

    // void recognize(const String<EquivCode> &chars, const String<EquivCode> &set, Token t, Priority::Type pri, TokenVector &ambiguities);
    public void recognize(String<EquivCode> chars, String<EquivCode> set, Token t, byte priority, Vector<Token> ambiguities)
    {
        Trie trie = extendTrie(root_.pointer()!, chars);

        for (nuint i = 0; i < set.size(); i++)
            setToken(forceNext(trie, set[i]), (int)chars.size() + 1, t, priority, ambiguities);
    }

    // void recognizeB(const String<EquivCode> &chars, int bSequenceLength, size_t maxBlankSequenceLength,
    //                 const String<EquivCode> &blankCodes, const String<EquivCode> &chars2, Token t, TokenVector &ambiguities);
    public void recognizeB(String<EquivCode> chars, int bSequenceLength, nuint maxBlankSequence,
                           String<EquivCode> blankCodes, String<EquivCode> chars2, Token token, Vector<Token> ambiguities)
    {
        doB(extendTrie(root_.pointer()!, chars),
            (int)chars.size(),
            bSequenceLength,
            maxBlankSequence,
            blankCodes,
            chars2,
            token,
            Priority.blank(bSequenceLength),
            ambiguities);
    }

    // void recognizeEE(EquivCode code, Token t);
    public void recognizeEE(EquivCode code, Token t)
    {
        Trie trie = forceNext(root_.pointer()!, code);
        trie.setTokenLength(0);  // it has length 0 in the buffer
        trie.setToken((ushort)t);
        trie.setPriority(Priority.data);
    }

    // Trie *extractTrie() { return root_.extract(); }
    public Trie? extractTrie()
    {
        return root_.extract();
    }

    private void doB(Trie trie, int tokenLength, int minBLength, nuint maxLength,
                     String<EquivCode> blankCodes, String<EquivCode> chars2,
                     Token token, byte pri, Vector<Token> ambiguities)
    {
        if (minBLength == 0 && !trie.hasNext())
        {
            if (trie.blank() == null)
            {
                BlankTrie b = new BlankTrie();
                trie.setBlank(new CopyOwner<BlankTrie>(b));
                b.setMaxBlanksToScan(maxLength);
                b.setAdditionalLength((byte)tokenLength);
                b.getCodeIsBlank().assign((nuint)nCodes_, false);
                for (nuint i = 0; i < blankCodes.size(); i++)
                    b.getCodeIsBlank()[blankCodes[i]] = true;
                b.setTokenLength(0);
                b.setToken(0);
                b.setPriority(Priority.data);
                b.setNCodes(nCodes_);
            }
            else
            {
                // A B sequence is not allowed to be adjacent to a character
                // that can occur in a blank sequence, so maxLength will be
                // the same at a node, no matter how we got there.
                System.Diagnostics.Debug.Assert(trie.blank()!.maxBlanksToScan() == maxLength);
                System.Diagnostics.Debug.Assert(trie.blank()!.additionalLength() == tokenLength);
            }
            if (chars2.size() == 0)
                setToken(trie, tokenLength, token, pri, ambiguities);
            else
                setToken(extendTrie(trie.blank()!, chars2),
                         (int)chars2.size(),
                         token,
                         pri,
                         ambiguities);
        }
        else
        {
            if (minBLength == 0)
                setToken(extendTrie(trie, chars2), tokenLength + (int)chars2.size(),
                         token, pri, ambiguities);
            for (nuint i = 0; i < blankCodes.size(); i++)
                doB(forceNext(trie, blankCodes[i]),
                    tokenLength + 1,
                    minBLength == 0 ? 0 : minBLength - 1,
                    maxLength - 1,
                    blankCodes,
                    chars2,
                    token,
                    pri,
                    ambiguities);
        }
    }

    // Trie *extendTrie(Trie *, const String<EquivCode> &);
    private Trie extendTrie(Trie trie, String<EquivCode> s)
    {
        for (nuint i = 0; i < s.size(); i++)
            trie = forceNext(trie, s[i]);
        return trie;
    }

    // void setToken(Trie *trie, int tokenLength, Token token, Priority::Type pri, TokenVector &ambiguities);
    private void setToken(Trie trie, int tokenLength, Token token, byte pri, Vector<Token> ambiguities)
    {
        if (tokenLength > trie.tokenLength()
            || (tokenLength == trie.tokenLength() && pri > trie.getPriority()))
        {
            trie.setTokenLength((byte)tokenLength);
            trie.setToken((ushort)token);
            trie.setPriority(pri);
        }
        else if (trie.tokenLength() == tokenLength
                 && trie.getPriority() == pri
                 && trie.token() != token
                 && trie.token() != 0)
        {
            ambiguities.push_back(trie.token());
            ambiguities.push_back(token);
        }
        if (trie.hasNext())
        {
            for (int i = 0; i < nCodes_; i++)
                setToken(trie.next(i)!, tokenLength, token, pri, ambiguities);
        }
    }

    // void copyInto(Trie *, const Trie *, int);
    private void copyInto(Trie into, Trie from, int additionalLength)
    {
        if (from.token() != 0)
        {
            Vector<Token> ambiguities = new Vector<Token>();
            setToken(into, from.tokenLength() + additionalLength, from.token(),
                     from.getPriority(), ambiguities);
            System.Diagnostics.Debug.Assert(ambiguities.size() == 0);
        }
        if (from.hasNext())
            for (int i = 0; i < nCodes_; i++)
                copyInto(forceNext(into, (EquivCode)i), from.next(i)!, additionalLength);
    }

    // Trie *forceNext(Trie *trie, EquivCode c);
    private Trie forceNext(Trie trie, EquivCode c)
    {
        if (!trie.hasNext())
        {
            Trie[] next = new Trie[nCodes_];
            for (int j = 0; j < nCodes_; j++)
                next[j] = new Trie();
            trie.setNext(next);

            if (trie.blank() != null)
            {
                trie.blank()!.setAdditionalLength((byte)(trie.blank()!.additionalLength() + 1));
                trie.blank()!.setMaxBlanksToScan(trie.blank()!.maxBlanksToScan() - 1);
            }
            CopyOwner<BlankTrie> blankOwner = new CopyOwner<BlankTrie>(trie.getBlank().extract());
            BlankTrie? b = blankOwner.pointer();
            for (int i = 0; i < nCodes_; i++)
            {
                Trie p = trie.next(i)!;
                if (b != null && b.codeIsBlank((EquivCode)i))
                    p.setBlank(blankOwner.pointer() != null
                               ? new CopyOwner<BlankTrie>(blankOwner.extract()!)
                               : new CopyOwner<BlankTrie>(new BlankTrie(b)));
                p.setToken((ushort)trie.token());
                p.setTokenLength((byte)trie.tokenLength());
                p.setPriority(trie.getPriority());
                p.setNCodes(nCodes_);
            }
            if (b != null)
                // -1 because 1 was added above
                copyInto(trie, b, b.additionalLength() - 1);
        }
        return trie.next((int)c)!;
    }
}
