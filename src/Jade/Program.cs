// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.
// Ported to C# as part of OpenJade-NET

namespace Jade;

public static class Program
{
    public static int Main(string[] args)
    {
        JadeApp app = new JadeApp();
        return app.run(args);
    }
}
