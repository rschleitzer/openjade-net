// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Pure abstract class in C++ - implemented as interface in C#
// Used for building formatted message strings from message arguments
public interface MessageBuilder
{
    // virtual void appendNumber(unsigned long) = 0;
    void appendNumber(ulong n);

    // virtual void appendOrdinal(unsigned long) = 0;
    void appendOrdinal(ulong n);

    // virtual void appendChars(const Char *, size_t) = 0;
    void appendChars(Char[]? data, nuint size);

    // virtual void appendOther(const OtherMessageArg *) = 0;
    void appendOther(OtherMessageArg arg);

    // virtual void appendFragment(const MessageFragment &) = 0;
    void appendFragment(MessageFragment fragment);
}
