// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.
// Ported to C# as part of OpenJade-NET

namespace Jade;

public static class Program
{
    public static int Main(string[] args)
    {
        // Run on a thread with larger stack size (8 MB) to handle deep recursion
        // in DSSSL processing (e.g., nested map operations).
        int result = 0;
        var thread = new System.Threading.Thread(() =>
        {
            JadeApp app = new JadeApp();
            result = app.run(args);
        }, 8 * 1024 * 1024);  // 8 MB stack
        thread.Start();
        thread.Join();
        return result;
    }
}
