using System.Diagnostics;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.OSX
{
    public class StopwatchFactoryOSX : IStopwatchFactory
    {
        public IStopwatch Create()
        {
            return new StopwatchOSX();
        }

        private class StopwatchOSX : IStopwatch
        {
            private readonly Stopwatch _stopWatch;

            public StopwatchOSX()
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
