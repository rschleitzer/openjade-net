// Copyright (c) 2001 Epremis Corp.
// See the file COPYING for copying permission.

namespace OpenSP;

public class MessageModule
{
    // In C++, this is const char* domain for message domain identification
    // Used for internationalization/localization of messages
    public string? domain;

    public MessageModule()
    {
        domain = null;
    }

    public MessageModule(string? messageDomain)
    {
        domain = messageDomain;
    }
}

public static class MessageModules
{
    public static readonly MessageModule libModule = new MessageModule("OpenSP");
    public static readonly MessageModule appModule = new MessageModule("app");
}
