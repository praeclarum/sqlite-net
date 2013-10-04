using System.Diagnostics;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.XamarinAndroid
{
    public class StopwatchFactoryAndroid : IStopwatchFactory
    {
        public IStopwatch Create()
        {
            return new StopwatchAndroid();
        }

        private class StopwatchAndroid : IStopwatch
        {
            private readonly Stopwatch _stopWatch;

            public StopwatchAndroid()
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