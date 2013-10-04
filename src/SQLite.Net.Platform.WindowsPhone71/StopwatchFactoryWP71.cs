using SQLite.Net.Interop;

namespace SQLite.Net.Platform.WindowsPhone71
{
    public class StopwatchFactoryWP71 : IStopwatchFactory
    {
        public IStopwatch Create()
        {
            return new StopwatchDummy();
        }

        private class StopwatchDummy : IStopwatch
        {

            public StopwatchDummy()
            {
            }

            public void Stop()
            {
            }

            public void Reset()
            {
            }

            public void Start()
            {
            }

            public long ElapsedMilliseconds
            {
                get { return 0; }
            }
        }
    }
}