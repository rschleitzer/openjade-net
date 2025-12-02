// Copyright (c) 1996 James Clark, 1999 Matthias Clasen
// See the file COPYING for copying permission.

// This is a mildly C++ified version of getopt().
// (extended to include getopt_long() functionality.)
// It never prints any message.

namespace OpenSP;

public class LongOption<T> where T : struct
{
    public T[]? name;
    public T key;
    public T value;
    public bool hasArgument;

    public LongOption()
    {
        name = null;
        key = default;
        value = default;
        hasArgument = false;
    }
}

public class Options<T> where T : struct
{
    private T[][]? argv_;
    private int argc_;
    private int ind_;
    private T opt_;
    private T[]? arg_;
    private int sp_;
    private Vector<LongOption<T>> opts_;
    private int optInd_;

    // Options(int argc, T *const *, const Vector<LongOption<T> > &);
    public Options(int argc, T[][] argv, Vector<LongOption<T>> opts)
    {
        argv_ = argv;
        argc_ = argc;
        ind_ = 1;
        sp_ = 1;
        opts_ = opts;
        optInd_ = -1;
        opt_ = default;
        arg_ = null;
    }

    // bool get(T &);
    public bool get(out T c)
    {
        c = default;
        arg_ = null;
        optInd_ = -1;

        if (argv_ == null)
            return false;

        // Skip past non-option arguments that have been permuted to the end
        while (ind_ < argc_ && argv_[ind_] == null)
            ind_++;

        if (ind_ >= argc_)
            return false;

        T[] currentArg = argv_[ind_];

        // Check for end of options
        if (currentArg.Length == 0)
        {
            ind_++;
            return false;
        }

        // Check if it looks like an option (starts with '-')
        T minus = (T)(object)(uint)'-';
        if (!currentArg[0].Equals(minus))
            return false;

        // Handle "--" end of options marker
        if (currentArg.Length == 2 && currentArg[1].Equals(minus))
        {
            ind_++;
            return false;
        }

        // Handle long options (starting with "--")
        if (currentArg.Length > 2 && currentArg[1].Equals(minus))
        {
            // Extract the long option name (starting after "--")
            nuint optStart = 2;
            nuint optLen = (nuint)currentArg.Length - optStart;

            // Find '=' if present
            T equals = (T)(object)(uint)'=';
            nuint equalsPos = optLen;
            for (nuint i = 0; i < optLen; i++)
            {
                if (currentArg[optStart + i].Equals(equals))
                {
                    equalsPos = i;
                    break;
                }
            }

            T[] optName = new T[equalsPos];
            for (nuint i = 0; i < equalsPos; i++)
                optName[i] = currentArg[optStart + i];

            if (searchLong(optName))
            {
                ind_++;
                if (equalsPos < optLen)
                {
                    // Argument follows '='
                    nuint argLen = optLen - equalsPos - 1;
                    arg_ = new T[argLen];
                    for (nuint i = 0; i < argLen; i++)
                        arg_[i] = currentArg[optStart + equalsPos + 1 + i];
                }
                else if (opts_[(nuint)optInd_].hasArgument && ind_ < argc_)
                {
                    arg_ = argv_[ind_++];
                }
                c = opts_[(nuint)optInd_].value;
                opt_ = opts_[(nuint)optInd_].key;
                return true;
            }
            // Unknown long option
            opt_ = currentArg[optStart];
            ind_++;
            return false;
        }

        // Handle short options
        if (sp_ >= currentArg.Length)
        {
            ind_++;
            sp_ = 1;
            return get(out c);
        }

        T optChar = currentArg[sp_];
        if (search(optChar))
        {
            sp_++;
            if (opts_[(nuint)optInd_].hasArgument)
            {
                if (sp_ < currentArg.Length)
                {
                    // Argument follows option char directly
                    nuint argLen = (nuint)(currentArg.Length - sp_);
                    arg_ = new T[argLen];
                    for (nuint i = 0; i < argLen; i++)
                        arg_[i] = currentArg[sp_ + (int)i];
                    ind_++;
                    sp_ = 1;
                }
                else if (++ind_ < argc_)
                {
                    arg_ = argv_[ind_++];
                    sp_ = 1;
                }
                else
                {
                    sp_ = 1;
                }
            }
            else if (sp_ >= currentArg.Length)
            {
                ind_++;
                sp_ = 1;
            }
            c = opts_[(nuint)optInd_].value;
            opt_ = opts_[(nuint)optInd_].key;
            return true;
        }

        // Unknown short option
        opt_ = optChar;
        sp_++;
        if (sp_ >= currentArg.Length)
        {
            ind_++;
            sp_ = 1;
        }
        return false;
    }

    // T *arg() const { return arg_; }
    public T[]? arg()
    {
        return arg_;
    }

    // T opt() const { return opt_; }
    public T opt()
    {
        return opt_;
    }

    // int ind() const { return ind_; }
    public int ind()
    {
        return ind_;
    }

    // int longIndex() const { return optInd_; }
    public int longIndex()
    {
        return optInd_;
    }

    // bool search(T);
    private bool search(T c)
    {
        for (nuint i = 0; i < opts_.size(); i++)
        {
            if (opts_[i].key.Equals(c))
            {
                optInd_ = (int)i;
                return true;
            }
        }
        return false;
    }

    // bool searchLong(const T *);
    private bool searchLong(T[] name)
    {
        for (nuint i = 0; i < opts_.size(); i++)
        {
            T[]? optName = opts_[i].name;
            if (optName != null && arraysEqual(optName, name))
            {
                optInd_ = (int)i;
                return true;
            }
        }
        return false;
    }

    private static bool arraysEqual(T[] a, T[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
            if (!a[i].Equals(b[i]))
                return false;
        return true;
    }
}
