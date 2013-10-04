using SQLite.Net.Interop;

namespace SQLite.Net.Platform.XamarinAndroid
{
    public class SQLitePlatformAndroid : ISQLitePlatform
    {
        public SQLitePlatformAndroid()
        {
            SQLiteApi = new SQLiteApiAndroid();
            StopwatchFactory = new StopwatchFactoryAndroid();
            ReflectionService = new ReflectionServiceAndroid();
            VolatileService = new VolatileServiceAndroid();
        }

        public ISQLiteApi SQLiteApi { get; private set; }
        public IStopwatchFactory StopwatchFactory { get; private set; }
        public IReflectionService ReflectionService { get; private set; }
        public IVolatileService VolatileService { get; private set; }
    }
}