// Copyright (c) 1994 James Clark, 2000 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

// SP_MULTI_BYTE is always defined for this port

public class SharedXcharMap<T> : Resource
{
    // Array size: 2 + 0xffff = 65537 (for values from -1 to 0xffff)
    private T[] v_ = new T[2 + 0xffff];

    // SharedXcharMap();
    public SharedXcharMap()
    {
    }

    // SharedXcharMap(T defaultValue);
    public SharedXcharMap(T defaultValue)
    {
        for (int i = 0; i < v_.Length; i++)
            v_[i] = defaultValue;
    }

    // T *ptr() { return v + 1; }
    // Returns pointer to v[1], so index -1 maps to v[0] (for EOF)
    // In C#, we expose the array and handle offset in XcharMap
    public T[] array()
    {
        return v_;
    }

    // Get/set with offset (ptr_ points to v[1] in C++)
    public T getAt(int index)
    {
        return v_[index + 1];
    }

    public void setAt(int index, T value)
    {
        v_[index + 1] = value;
    }
}

public class XcharMap<T>
{
    // ptr_ in C++ points to v[1] of SharedXcharMap
    // In C#, we store offset 1 implicitly
    private SharedXcharMap<T>? sharedMap_;
    // For characters > 0xffff (SP_MULTI_BYTE)
    private CharMapResource<T>? hiMap_;

    // XcharMap();
    public XcharMap()
    {
        sharedMap_ = null;
        hiMap_ = null;
    }

    // XcharMap(T defaultValue);
    public XcharMap(T defaultValue)
    {
        sharedMap_ = new SharedXcharMap<T>(defaultValue);
        hiMap_ = new CharMapResource<T>(defaultValue);
    }

    // T operator[](Xchar c) const;
    public T this[Xchar c]
    {
        get
        {
            if (c > 0xffff)
                return hiMap_![(Char)c];
            return sharedMap_!.getAt((int)c);
        }
    }

    // void setRange(Char min, Char max, T val);
    public void setRange(Char min, Char max, T val)
    {
        if (min <= max)
        {
            if (min <= 0xffff)
            {
                Char m = max <= 0xffff ? max : 0xffff;
                do
                {
                    sharedMap_!.setAt((int)min, val);
                } while (min++ != m);
            }
            if (max >= 0x10000)
                hiMap_!.setRange(min < 0x10000 ? 0x10000 : min, max, val);
        }
    }

    // void setChar(Char c, T val);
    public void setChar(Char c, T val)
    {
        if (c > 0xffff)
        {
            hiMap_!.setChar(c, val);
            return;
        }
        sharedMap_!.setAt((int)c, val);
    }

    // void setEe(T val);
    // Sets the value for EOF (index -1)
    public void setEe(T val)
    {
        sharedMap_!.setAt(-1, val);
    }

    // void clear();
    public void clear()
    {
        sharedMap_ = null;
        hiMap_ = null;
    }
}
