using SQLite.Net.Interop;

namespace SQLite.Net.Platform.WindowsPhone8
{
    public class SQLitePlatformWP8 : ISQLitePlatform
    {
        public SQLitePlatformWP8()
        {
            var api = new SQLiteApiWP8();

//            api.SetDirectory(/*temp directory type*/2, Windows.Storage.ApplicationData.Current.TemporaryFolder.Path);

            SQLiteApi = api;
            VolatileService = new VolatileServiceWP8();
            ReflectionService = new ReflectionServiceWP8();
            StopwatchFactory = new StopwatchFactoryWP8();
        }

        public ISQLiteApi SQLiteApi { get; private set; }
        public IStopwatchFactory StopwatchFactory { get; private set; }
        public IReflectionService ReflectionService { get; private set; }
        public IVolatileService VolatileService { get; private set; }
    }
}