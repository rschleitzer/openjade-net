// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Recognizer : Resource
{
    private Boolean multicode_;
    private Owner<Trie> trie_ = new Owner<Trie>();
    private XcharMap<EquivCode> map_;
    private Vector<Token> suppressTokens_ = new Vector<Token>();

    // Recognizer(Trie *, const XcharMap<EquivCode> &);
    public Recognizer(Trie trie, XcharMap<EquivCode> map)
    {
        trie_ = new Owner<Trie>(trie);
        map_ = map;
        multicode_ = false;
    }

    // Recognizer(Trie *, const XcharMap<EquivCode> &, Vector<Token> &);
    public Recognizer(Trie trie, XcharMap<EquivCode> map, Vector<Token> suppressTokens)
    {
        trie_ = new Owner<Trie>(trie);
        map_ = map;
        multicode_ = true;
        suppressTokens.swap(suppressTokens_);
    }

    // ~Recognizer();
    // C# GC handles cleanup

    // Token recognize(InputSource *, Messenger &) const;
    public Token recognize(InputSource input, Messenger mgr)
    {
        if (multicode_)
        {
            input.startToken();
            if (input.scanSuppress())
                return suppressTokens_[map_[input.tokenChar(mgr)]];
        }
        else
            input.startTokenNoMulticode();

        Trie? pos = trie_.pointer();
        do
        {
            pos = pos!.next((int)map_[input.tokenChar(mgr)]);
        } while (pos!.hasNext());

        if (pos.blank() == null)
        {
            input.endToken((nuint)pos.tokenLength());
            return pos.token();
        }

        BlankTrie b = pos.blank()!;
        Trie newPos = b;
        nuint maxBlanks = b.maxBlanksToScan();
        nuint nBlanks;
        for (nBlanks = 0; nBlanks < maxBlanks; nBlanks++)
        {
            EquivCode code = map_[input.tokenChar(mgr)];
            if (!b.codeIsBlank(code))
            {
                if (newPos.hasNext())
                    newPos = newPos.next((int)code)!;
                break;
            }
        }
        while (newPos.hasNext())
            newPos = newPos.next((int)map_[input.tokenChar(mgr)])!;

        if (newPos.token() != 0)
        {
            input.endToken((nuint)(newPos.tokenLength() + b.additionalLength()) + nBlanks);
            return newPos.token();
        }
        else
        {
            input.endToken((nuint)pos.tokenLength() + (pos.includeBlanks() ? nBlanks : 0));
            return pos.token();
        }
    }
}
