// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// TODO: Port from MessageModule.h

namespace OpenSP;

public class MessageModule
{
}

public static class MessageModules
{
    public static readonly MessageModule libModule = new MessageModule();
    public static readonly MessageModule appModule = new MessageModule();
}
