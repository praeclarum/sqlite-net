using System.Diagnostics;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.WindowsPhone8
{
    public class StopwatchFactoryWP8 : IStopwatchFactory
    {
        public IStopwatch Create()
        {
            return new StopwatchWP8();
        }

        private class StopwatchWP8 : IStopwatch
        {
            private readonly Stopwatch _stopWatch;

            public StopwatchWP8()
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