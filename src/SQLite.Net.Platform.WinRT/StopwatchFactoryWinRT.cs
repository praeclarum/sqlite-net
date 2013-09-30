using System.Diagnostics;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.WinRT
{
    public class StopwatchFactoryWinRT : IStopwatchFactory
    {
        public IStopwatch Create()
        {
            return new StopwatchWinRT();
        }

        private class StopwatchWinRT : IStopwatch
        {
            private readonly Stopwatch _stopWatch;

            public StopwatchWinRT()
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