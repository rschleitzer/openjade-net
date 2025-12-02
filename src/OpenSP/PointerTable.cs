// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

using System;

namespace OpenSP;

// Interface for hash function
public interface IHashFunction<K>
{
    static abstract ulong hash(K key);
}

// Interface for key extraction function
public interface IKeyFunction<P, K>
{
    static abstract K key(P item);
}

public class PointerTable<P, K, HF, KF>
    where P : class?
    where HF : IHashFunction<K>
    where KF : IKeyFunction<P, K>
    where K : notnull
{
    protected nuint used_;
    protected nuint usedLimit_;
    // Using raw array instead of Vector<P> since P can be nullable
    internal P?[] vec_ = Array.Empty<P?>();
    internal P? null_;

    // PointerTable();
    public PointerTable()
    {
        used_ = 0;
        usedLimit_ = 0;
        null_ = null;
    }

    protected nuint vecSize()
    {
        return (nuint)vec_.Length;
    }

    protected nuint startIndex(K k)
    {
        return (nuint)(HF.hash(k) & ((nuint)vec_.Length - 1));
    }

    protected nuint nextIndex(nuint i)
    {
        return i == 0 ? (nuint)vec_.Length - 1 : i - 1;
    }

    // P insert(P, Boolean replace = 0);
    public P? insert(P p, Boolean replace = false)
    {
        nuint h;
        if (vec_.Length == 0)
        {
            vec_ = new P?[8];
            usedLimit_ = 4;
            h = startIndex(KF.key(p!));
        }
        else
        {
            for (h = startIndex(KF.key(p!)); vec_[h] != null; h = nextIndex(h))
            {
                if (KF.key(vec_[h]!)!.Equals(KF.key(p!)))
                {
                    if (replace)
                    {
                        P? tem = vec_[h];
                        vec_[h] = p;
                        return tem;
                    }
                    else
                        return vec_[h];
                }
            }
            if (used_ >= usedLimit_)
            {
                if ((nuint)vec_.Length > nuint.MaxValue / 2)
                {
                    if (usedLimit_ == (nuint)vec_.Length - 1)
                        throw new InvalidOperationException("PointerTable overflow");
                    else
                        usedLimit_ = (nuint)vec_.Length - 1;
                }
                else
                {
                    // rehash
                    P?[] oldVec = vec_;
                    vec_ = new P?[oldVec.Length * 2];
                    usedLimit_ = (nuint)vec_.Length / 2;
                    for (nuint i = 0; i < (nuint)oldVec.Length; i++)
                    {
                        if (oldVec[i] != null)
                        {
                            nuint j;
                            for (j = startIndex(KF.key(oldVec[i]!));
                                 vec_[j] != null;
                                 j = nextIndex(j))
                                ;
                            vec_[j] = oldVec[i];
                        }
                    }
                    for (h = startIndex(KF.key(p!)); vec_[h] != null; h = nextIndex(h))
                        ;
                }
            }
        }
        used_++;
        vec_[h] = p;
        return null;
    }

    // const P &lookup(const K &) const;
    public P? lookup(K k)
    {
        if (used_ > 0)
        {
            for (nuint i = startIndex(k); vec_[i] != null; i = nextIndex(i))
            {
                if (KF.key(vec_[i]!)!.Equals(k))
                    return vec_[i];
            }
        }
        return null_;
    }

    // P remove(const K &);
    public P? remove(K k)
    {
        if (used_ > 0)
        {
            for (nuint i = startIndex(k); vec_[i] != null; i = nextIndex(i))
            {
                if (KF.key(vec_[i]!)!.Equals(k))
                {
                    P? p = vec_[i];
                    do
                    {
                        vec_[i] = null;
                        nuint j = i;
                        nuint r;
                        do
                        {
                            i = nextIndex(i);
                            if (vec_[i] == null)
                                break;
                            r = startIndex(KF.key(vec_[i]!));
                        } while ((i <= r && r < j) || (r < j && j < i) || (j < i && i <= r));
                        vec_[j] = vec_[i];
                    } while (vec_[i] != null);
                    --used_;
                    return p;
                }
            }
        }
        return null;
    }

    // size_t count() const { return used_; }
    public nuint count()
    {
        return used_;
    }

    // void clear();
    public virtual void clear()
    {
        vec_ = Array.Empty<P?>();
        used_ = 0;
        usedLimit_ = 0;
    }

    // void swap(PointerTable<P, K, HF, KF> &);
    public void swap(PointerTable<P, K, HF, KF> to)
    {
        P?[] temVec = vec_;
        vec_ = to.vec_;
        to.vec_ = temVec;

        nuint tem = to.used_;
        to.used_ = used_;
        used_ = tem;
        tem = to.usedLimit_;
        to.usedLimit_ = usedLimit_;
        usedLimit_ = tem;
    }
}

public class PointerTableIter<P, K, HF, KF>
    where P : class?
    where HF : IHashFunction<K>
    where KF : IKeyFunction<P, K>
    where K : notnull
{
    private PointerTable<P, K, HF, KF> tablePtr_;
    private nuint i_;

    // PointerTableIter(const PointerTable<P, K, HF, KF> &);
    public PointerTableIter(PointerTable<P, K, HF, KF> table)
    {
        tablePtr_ = table;
        i_ = 0;
    }

    // const P &next();
    public P? next()
    {
        for (; i_ < (nuint)tablePtr_.vec_.Length; i_++)
        {
            if (tablePtr_.vec_[i_] != null)
                return tablePtr_.vec_[i_++];
        }
        return tablePtr_.null_;
    }
}
