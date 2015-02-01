using System;
using System.Diagnostics;

namespace Chronos
{
    public class Profiling
    {
        public static void Profile(string description, int iterations, Action func, Action<string> outputAction = null)
        {
            if (outputAction == null)
                outputAction = Console.Write;

            // warm up 
            func();

            var watch = new Stopwatch();

            // clean up
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                func();
            }
            watch.Stop();
            outputAction(description);
            outputAction(string.Format(" Time Elapsed {0} ms{1}", watch.Elapsed.TotalMilliseconds, Environment.NewLine));
        } 
    }
}