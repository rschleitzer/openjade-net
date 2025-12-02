// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// TODO: Port from MessageBuilder.h

namespace OpenSP;

public interface MessageBuilder
{
    void appendChars(Char[]? data, nuint size);
    void appendNumber(ulong n);
    void appendOrdinal(ulong n);
    void appendOther(OtherMessageArg arg);
    void appendFragment(MessageFragment fragment);
}
