using SQLite.Net.Interop;

namespace SQLite.Net.Platform.OSX
{
    public class SQLitePlatformOSX : ISQLitePlatform
    {
        public SQLitePlatformOSX()
        {
            SQLiteApi = new SQLiteApiOSX();
            StopwatchFactory = new StopwatchFactoryOSX();
            ReflectionService = new ReflectionServiceOSX();
            VolatileService = new VolatileServiceOSX();
        }

        public ISQLiteApi SQLiteApi { get; private set; }
        public IStopwatchFactory StopwatchFactory { get; private set; }
        public IReflectionService ReflectionService { get; private set; }
        public IVolatileService VolatileService { get; private set; }
    }
}
