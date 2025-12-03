using OpenSP;
// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace Nsgmls;

public static class Program
{
    public static int Main(string[] args)
    {
        NsgmlsApp app = new NsgmlsApp();
        return app.run(args);
    }
}
