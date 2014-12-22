using System.Diagnostics;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.XamarinIOS
{
    public class StopwatchFactoryIOS : IStopwatchFactory
    {
        public IStopwatch Create()
        {
            return new StopwatchIOS();
        }

        private class StopwatchIOS : IStopwatch
        {
            private readonly Stopwatch _stopWatch;

            public StopwatchIOS()
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