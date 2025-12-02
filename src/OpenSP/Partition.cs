// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class Partition
{
    private EquivCode maxCode_;
    private Vector<String<EquivCode>> setCodes_ = new Vector<String<EquivCode>>();
    private XcharMap<EquivCode> map_;

    // Partition(const ISet<Char> &chars, const ISet<Char> **sets, int nSets, const SubstTable &subst);
    public Partition(ISet<Char> chars, ISet<Char>[] sets, int nSets, SubstTable subst)
    {
        map_ = new XcharMap<EquivCode>(0); // eE gets code 0

        IList<EquivClass> classes = new IList<EquivClass>();
        classes.insert(new EquivClass());
        classes.head()!.set.addRange(0, Constant.charMax);

        {
            ISetIter<Char> iter = new ISetIter<Char>(chars);
            Char min, max;
            while (iter.next(out min, out max) != 0)
            {
                do
                {
                    refineByChar(classes, subst[min]);
                } while (min++ != max);
            }
        }

        int i;
        for (i = 0; i < nSets; i++)
            refineBySet(classes, sets[i], (uint)(1 << i));

        maxCode_ = 0;

        setCodes_.resize((nuint)nSets);
        for (nuint j = 0; j < (nuint)nSets; j++)
            setCodes_[j] = new String<EquivCode>();

        for (IListIter<EquivClass> listIter = new IListIter<EquivClass>(classes);
             listIter.done() == 0;
             listIter.next())
        {
            ++maxCode_;
            System.Diagnostics.Debug.Assert(maxCode_ != 0);
            EquivClass p = listIter.cur()!;
            for (i = 0; i < nSets; i++)
                if (((1 << i) & p.inSets) != 0)
                    setCodes_[(nuint)i].operatorPlusAssign(maxCode_);
            ISetIter<Char> setIter = new ISetIter<Char>(p.set);
            Char min, max;
            while (setIter.next(out min, out max) != 0)
                map_.setRange(min, max, maxCode_);
        }

        {
            ISetIter<Char> iter = new ISetIter<Char>(chars);
            Char min, max;
            while (iter.next(out min, out max) != 0)
            {
                do
                {
                    StringC str = subst.inverse(min);
                    EquivCode code = map_[(Xchar)min];
                    for (nuint k = 0; k < str.size(); k++)
                        map_.setChar(str[k], code);
                } while (min++ != max);
            }
        }
    }

    // EquivCode maxCode() const;
    public EquivCode maxCode()
    {
        return maxCode_;
    }

    // EquivCode charCode(Char c) const;
    public EquivCode charCode(Char c)
    {
        return map_[(Xchar)c];
    }

    // EquivCode eECode() const;
    public EquivCode eECode()
    {
        return 0;
    }

    // const String<EquivCode> &setCodes(int i) const;
    public String<EquivCode> setCodes(int i)
    {
        return setCodes_[(nuint)i];
    }

    // const XcharMap<EquivCode> &map() const;
    public XcharMap<EquivCode> map()
    {
        return map_;
    }

    // static void refineByChar(IList<EquivClass> *, Char);
    private static void refineByChar(IList<EquivClass> classes, Char c)
    {
        // Avoid modifying *classes, while there's an active iter on it.
        EquivClass? found = null;
        {
            for (IListIter<EquivClass> iter = new IListIter<EquivClass>(classes); iter.done() == 0; iter.next())
            {
                if (iter.cur()!.set.contains(c))
                {
                    found = iter.cur();
                    break;
                }
            }
        }
        if (found != null && !found.set.isSingleton())
        {
            found.set.remove(c);
            classes.insert(new EquivClass(found.inSets));
            classes.head()!.set.add(c);
        }
    }

    // static void addUpTo(ISet<Char> *to, Char limit, const ISet<Char> &from)
    private static void addUpTo(ISet<Char> to, Char limit, ISet<Char> from)
    {
        ISetIter<Char> iter = new ISetIter<Char>(from);
        Char min, max;
        while (iter.next(out min, out max) != 0 && min < limit)
            to.addRange(min, max >= limit ? limit - 1 : max);
    }

    private enum RefineResult { allIn, allOut, someInSomeOut }

    // static RefineResult refine(const ISet<Char> &set, const ISet<Char> &refiner, ISet<Char> *inp, ISet<Char> *outp)
    private static RefineResult refine(ISet<Char> set, ISet<Char> refiner, ISet<Char> inp, ISet<Char> outp)
    {
        Char setMin, setMax, refMin, refMax;
        ISetIter<Char> refIter = new ISetIter<Char>(refiner);
        ISetIter<Char> setIter = new ISetIter<Char>(set);
        Boolean oneIn = false;
        Boolean oneOut = false;

        if (refIter.next(out refMin, out refMax) == 0)
            return RefineResult.allOut;
        while (setIter.next(out setMin, out setMax) != 0)
        {
            while (setMin <= setMax)
            {
                while (refMax < setMin && refIter.next(out refMin, out refMax) != 0)
                    ;
                if (refMax < setMin || setMin < refMin)
                {
                    if (!oneOut)
                    {
                        if (oneIn)
                            addUpTo(inp, setMin, set);
                        oneOut = true;
                    }
                    if (refMax < setMin || refMin > setMax)
                    {
                        if (oneIn)
                            outp.addRange(setMin, setMax);
                        break;
                    }
                    else
                    {
                        if (oneIn)
                            outp.addRange(setMin, refMin - 1);
                        setMin = refMin;
                    }
                }
                else
                {
                    if (!oneIn)
                    {
                        if (oneOut)
                            addUpTo(outp, setMin, set);
                        oneIn = true;
                    }
                    if (setMax <= refMax)
                    {
                        if (oneOut)
                            inp.addRange(setMin, setMax);
                        break;
                    }
                    else
                    {
                        // refMax < setMax
                        if (oneOut)
                            inp.addRange(setMin, refMax);
                        // avoid wrapping round
                        if (refMax == Constant.charMax)
                            break;
                        setMin = refMax + 1;
                    }
                }
            }
        }
        if (oneIn)
            return oneOut ? RefineResult.someInSomeOut : RefineResult.allIn;
        else
            return RefineResult.allOut;
    }

    // static void refineBySet(IList<EquivClass> *classes, const ISet<Char> &set, unsigned setFlag)
    private static void refineBySet(IList<EquivClass> classes, ISet<Char> set, uint setFlag)
    {
        Owner<EquivClass> inClass = new Owner<EquivClass>(new EquivClass());
        Owner<EquivClass> outClass = new Owner<EquivClass>(new EquivClass());
        IList<EquivClass> newClasses = new IList<EquivClass>();
        for (;;)
        {
            EquivClass? p = classes.head();
            if (p == null)
                break;
            if (outClass.pointer() == null)
                outClass = new Owner<EquivClass>(new EquivClass());
            switch (refine(p.set, set, inClass.pointer()!.set, outClass.pointer()!.set))
            {
                case RefineResult.someInSomeOut:
                    inClass.pointer()!.inSets = p.inSets | setFlag;
                    newClasses.insert(inClass.extract()!);
                    outClass.pointer()!.inSets = p.inSets;
                    newClasses.insert(outClass.extract()!);
                    inClass = new Owner<EquivClass>(classes.get());
                    inClass.pointer()!.set.clear();
                    inClass.pointer()!.inSets = 0;
                    break;
                case RefineResult.allIn:
                    p.inSets |= setFlag;
                    newClasses.insert(classes.get()!);
                    break;
                case RefineResult.allOut:
                    newClasses.insert(classes.get()!);
                    break;
            }
        }
        classes.swap(newClasses);
    }
}
