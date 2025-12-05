// Copyright (c) 1998, 1999 Matthias Clasen
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;
using Char = System.UInt32;
using Boolean = System.Boolean;

// Level sort configuration for collation
public struct LevelSort
{
    public bool forward;
    public bool backward;
    public bool position;
}

// Build data for language definition
internal class LangBuildData
{
    public Dictionary<string, StringC> order = new();
    public Char currentpos = 0;
    public Dictionary<string, StringC> ce = new();    // multi-collating elements
    public Dictionary<string, Char> syms = new();     // collating symbols

    public LangBuildData() { }
}

// Runtime data for language object
internal class LangData
{
    public const int MaxLevels = 20;
    public const Char charMax = 0xFFFFFFFF;

    public LevelSort[] level = new LevelSort[MaxLevels];
    public Char levels = 0;
    public Dictionary<string, StringC> weights = new();
    public Dictionary<string, Char> collate = new();
    public Dictionary<Char, Char> toupper = new();
    public Dictionary<Char, Char> tolower = new();

    public LangData() { }
}

// Language object - implements DSSSL language definition
public class LangObj : LanguageObj
{
    private LangBuildData? buildData_;
    private LangData? data_;

    public LangObj()
    {
        buildData_ = new LangBuildData();
        data_ = new LangData();
    }

    public override LanguageObj? asLanguage() { return this; }

    public uint levels() { return data_?.levels ?? 0; }

    public override Char toUpper(Char c)
    {
        if (data_ != null && data_.toupper.TryGetValue(c, out Char uc))
            return uc != LangData.charMax ? uc : c;
        return c;
    }

    public override Char toLower(Char c)
    {
        if (data_ != null && data_.tolower.TryGetValue(c, out Char lc))
            return lc != LangData.charMax ? lc : c;
        return c;
    }

    public override bool areEquivalent(StringC r, StringC s, Char k)
    {
        return compare(r, s, k) == 0;
    }

    public override bool isLess(StringC r, StringC s)
    {
        return compare(r, s, levels()) < 0;
    }

    public override bool isLessOrEqual(StringC r, StringC s)
    {
        return compare(r, s, levels()) <= 0;
    }

    // Add multi-collating element
    public void addMultiCollatingElement(StringC sym, StringC str)
    {
        if (buildData_ != null)
            buildData_.ce[sym.ToString()] = str;
    }

    // Add collating symbol
    public void addCollatingSymbol(StringC sym)
    {
        if (buildData_ != null)
            buildData_.syms[sym.ToString()] = LangData.charMax;
    }

    // Add level configuration
    public void addLevel(LevelSort sort)
    {
        if (data_ != null && data_.levels < LangData.MaxLevels)
            data_.level[data_.levels++] = sort;
    }

    // Add default position
    public void addDefaultPos()
    {
        addCollatingPos(new StringC());
    }

    // Add collating position
    public bool addCollatingPos(StringC sym)
    {
        if (buildData_ == null)
            return false;

        string symKey = sym.ToString();
        if (!buildData_.ce.ContainsKey(symKey) && !buildData_.syms.ContainsKey(symKey))
        {
            if (sym.size() <= 1)
                buildData_.ce[symKey] = sym;
            else
                return false;
        }

        string posKey = ((char)buildData_.currentpos).ToString();
        buildData_.order[posKey] = sym;
        buildData_.currentpos++;
        return true;
    }

    // Add level weight
    public bool addLevelWeight(Char l, StringC w)
    {
        if (buildData_ == null)
            return false;

        string wKey = w.ToString();
        if (!buildData_.ce.ContainsKey(wKey) && !buildData_.syms.ContainsKey(wKey))
        {
            if (w.size() <= 1)
                buildData_.ce[wKey] = w;
            else
                return false;
        }

        // Build key: (currentpos-1, level, index)
        Char index = 0;
        string key;
        do
        {
            key = $"{(char)(buildData_.currentpos - 1)}{(char)l}{(char)index}";
            index++;
        } while (buildData_.order.ContainsKey(key));

        buildData_.order[key] = w;
        return true;
    }

    // Add toupper mapping
    public void addToupper(Char lc, Char uc)
    {
        if (data_ != null)
            data_.toupper[lc] = uc;
    }

    // Add tolower mapping
    public void addTolower(Char uc, Char lc)
    {
        if (data_ != null)
            data_.tolower[uc] = lc;
    }

    // Compile the language definition
    public bool compile()
    {
        if (buildData_ == null || data_ == null)
            return false;

        string emptyKey = "";

        data_.collate[emptyKey] = buildData_.currentpos;

        // First phase: build collate and syms maps
        for (Char pos = 0; pos < buildData_.currentpos; pos++)
        {
            string posKey = ((char)pos).ToString();
            if (!buildData_.order.TryGetValue(posKey, out StringC? match))
                return false;

            string matchKey = match.ToString();
            if (!buildData_.ce.TryGetValue(matchKey, out StringC? match2))
                buildData_.syms[matchKey] = pos;
            else
                data_.collate[match2.ToString()] = pos;
        }

        // Second phase: build weights map
        for (Char pos = 0; pos < buildData_.currentpos; pos++)
        {
            for (Char level = 0; level < levels(); level++)
            {
                string key = $"{(char)pos}{(char)level}";
                StringC val = new StringC();

                for (Char idx = 0; ; idx++)
                {
                    string dataKey = $"{(char)pos}{(char)level}{(char)idx}";
                    if (!buildData_.order.TryGetValue(dataKey, out StringC? match))
                        break;

                    string matchKey = match.ToString();
                    Char col;

                    if (!buildData_.ce.TryGetValue(matchKey, out StringC? match2))
                    {
                        if (!buildData_.syms.TryGetValue(matchKey, out col))
                            return false;
                    }
                    else
                    {
                        if (!data_.collate.TryGetValue(match2.ToString(), out col))
                            return false;
                    }
                    val.append(new Char[] { col }, 1);
                }

                data_.weights[key] = val;
            }
        }

        // Free build data
        buildData_ = null;
        return true;
    }

    // Compare two strings at k levels
    private int compare(StringC rr, StringC ss, Char k)
    {
        StringC rc = asCollatingElts(rr);
        StringC sc = asCollatingElts(ss);

        for (Char l = 0; l < k && l < levels(); l++)
        {
            StringC r = atLevel(rc, l);
            StringC s = atLevel(sc, l);

            nuint i = 0;
            while (i < r.size() || i < s.size())
            {
                if (i == r.size()) return -1;
                if (i == s.size()) return 1;
                if (r[i] < s[i]) return -1;
                if (r[i] > s[i]) return 1;
                i++;
            }
        }
        return 0;
    }

    // Convert string to collating elements
    private StringC asCollatingElts(StringC s)
    {
        StringC res = new StringC();
        if (data_ == null) return res;

        string emptyKey = "";
        Char defaultVal = data_.collate.TryGetValue(emptyKey, out Char def) ? def : LangData.charMax;

        nuint i = 0;
        while (i < s.size())
        {
            Char col = defaultVal;
            StringC key = new StringC();
            nuint j;

            for (j = i; j < s.size(); j++)
            {
                key.append(new Char[] { s[j] }, 1);
                if (!data_.collate.TryGetValue(key.ToString(), out Char c))
                    break;
                col = c;
            }

            if (i == j)
                j++;  // Skip unknown char to avoid infinite loop

            res.append(new Char[] { col }, 1);
            i = j;
        }

        return res;
    }

    // Get weights at specified level
    private StringC atLevel(StringC s, Char l)
    {
        StringC cols = new StringC();
        StringC res = new StringC();

        if (data_ == null || l >= levels())
            return res;

        // Apply backward processing if needed
        if (data_.level[l].backward)
        {
            for (int i = (int)s.size() - 1; i >= 0; i--)
                cols.append(new Char[] { s[(nuint)i] }, 1);
        }
        else
        {
            cols = s;
        }

        // Build result from weights
        for (nuint i = 0; i < cols.size(); i++)
        {
            string key = $"{(char)cols[i]}{(char)l}";
            if (!data_.weights.TryGetValue(key, out StringC? w))
                return res;

            if (data_.level[l].backward)
            {
                for (int j = (int)w.size() - 1; j >= 0; j--)
                {
                    if (data_.level[l].position)
                        res.append(new Char[] { (Char)i }, 1);
                    res.append(new Char[] { w[(nuint)j] }, 1);
                }
            }
            else
            {
                for (nuint j = 0; j < w.size(); j++)
                {
                    if (data_.level[l].position)
                        res.append(new Char[] { (Char)i }, 1);
                    res.append(new Char[] { w[j] }, 1);
                }
            }
        }

        return res;
    }
}

// Reference language object using system locale
public class RefLangObj : LanguageObj
{
    private string? lang_;
    private string? country_;
    private System.Globalization.CultureInfo? culture_;

    public static bool supportedLanguage(StringC lang, StringC country)
    {
        try
        {
            string cultureName = buildCultureName(lang, country);
            var culture = new System.Globalization.CultureInfo(cultureName);
            return culture != null;
        }
        catch
        {
            return false;
        }
    }

    public RefLangObj(StringC lang, StringC country)
    {
        lang_ = lang.ToString();
        country_ = country.ToString();
        string cultureName = buildCultureName(lang, country);
        try
        {
            culture_ = new System.Globalization.CultureInfo(cultureName);
        }
        catch
        {
            culture_ = System.Globalization.CultureInfo.InvariantCulture;
        }
    }

    public override LanguageObj? asLanguage() { return this; }

    public override Char toUpper(Char c)
    {
        if (culture_ != null && c < 0x10000)
        {
            char ch = (char)c;
            return (Char)char.ToUpper(ch, culture_);
        }
        return c;
    }

    public override Char toLower(Char c)
    {
        if (culture_ != null && c < 0x10000)
        {
            char ch = (char)c;
            return (Char)char.ToLower(ch, culture_);
        }
        return c;
    }

    public override bool areEquivalent(StringC r, StringC s, Char l)
    {
        if (culture_ == null)
            return false;
        string rs = r.ToString();
        string ss = s.ToString();
        var options = System.Globalization.CompareOptions.None;
        if (l == 1)
            options = System.Globalization.CompareOptions.IgnoreCase;
        return string.Compare(rs, ss, culture_, options) == 0;
    }

    public override bool isLess(StringC r, StringC s)
    {
        if (culture_ == null)
            return false;
        string rs = r.ToString();
        string ss = s.ToString();
        return string.Compare(rs, ss, culture_, System.Globalization.CompareOptions.None) < 0;
    }

    public override bool isLessOrEqual(StringC r, StringC s)
    {
        if (culture_ == null)
            return false;
        string rs = r.ToString();
        string ss = s.ToString();
        return string.Compare(rs, ss, culture_, System.Globalization.CompareOptions.None) <= 0;
    }

    private static string buildCultureName(StringC lang, StringC country)
    {
        string l = lang.ToString().ToLower();
        string c = country.ToString().ToUpper();
        if (!string.IsNullOrEmpty(c))
            return $"{l}-{c}";
        return l;
    }
}
