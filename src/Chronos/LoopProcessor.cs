using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos
{
    public abstract class LoopProcessor
    {
        public abstract TimeSpan TimeBetweenLoops { get; }
        protected Task T;
        protected CancellationTokenSource Ts;
        protected CancellationToken CancellationToken;

        protected abstract Action<string> DebugLogAction { get; }
        protected abstract Action<string, Exception> FatalLogAction { get; }

        protected LoopProcessor()
        {
            Ts = new CancellationTokenSource();
            CancellationToken = Ts.Token;

            T = new Task(() =>
            {
                for (;;)
                {
                    if (DebugLogAction != null) { DebugLogAction("Starting process loop"); }
                    var startTime = Stopwatch.StartNew();
                    try
                    {
                        ProcessLoop();
                    }
                    catch (Exception x)
                    {
                        if (FatalLogAction != null)
                        {
                            FatalLogAction("Fatal Error in Loop Processor", x);
                        }
                        Environment.Exit(-1);
                    }

                    if (DebugLogAction != null)
                    {
                        DebugLogAction(string.Format("Finished process loop in {0}, waiting {1}",
                            startTime.Elapsed.ToString("g"), TimeBetweenLoops.ToString("g")));
                    }

                    Thread.Sleep(TimeBetweenLoops);
                    if (CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

            }); 
        }

        public virtual void Start() { T.Start();}
        public virtual void Stop() {Ts.Cancel();}
        public abstract void ProcessLoop();
    }
}