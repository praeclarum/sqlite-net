using System.Diagnostics;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.Generic
{
    public class StopwatchFactoryGeneric : IStopwatchFactory
    {
        public IStopwatch Create()
        {
            return new StopwatchGeneric();
        }

        private class StopwatchGeneric : IStopwatch
        {
            private readonly Stopwatch _stopWatch;

            public StopwatchGeneric()
            {
                _stopWatch = new Stopwatch();
            }

            public void Stop()
            {
                _stopWatch.Stop();
            }

            public void Reset()
            {
                _stopWatch.Reset();
            }

            public void Start()
            {
                _stopWatch.Start();
            }

            public long ElapsedMilliseconds
            {
                get { return _stopWatch.ElapsedMilliseconds; }
            }
        }
    }
}
