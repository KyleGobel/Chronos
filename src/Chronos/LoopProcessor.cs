using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Chronos
{
    public abstract class LoopProcessor
    {
        public abstract TimeSpan TimeBetweenLoops { get; }
        protected Task T;
        protected CancellationTokenSource Ts;
        protected CancellationToken CancellationToken;

        private static readonly ILogger Log = Serilog.Log.ForContext<LoopProcessor>();

        protected LoopProcessor()
        {
            Ts = new CancellationTokenSource();
            CancellationToken = Ts.Token;

            T = new Task(() =>
            {
                for (;;)
                {
                    Log.Debug("Starting process loop");
                    var startTime = Stopwatch.StartNew();
                    try
                    {
                        ProcessLoop();
                    }
                    catch (Exception x)
                    {
                        Log.Fatal(x, "Fatal Error in Loop Processor");
                        Environment.Exit(-1);
                    }

                    Log.Debug("Finished process loop in {Time}, waiting {SleepTime}", startTime.Elapsed, TimeBetweenLoops);

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