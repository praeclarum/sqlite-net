using System.Diagnostics;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.WinRT
{
    public class SQLitePlatformWinRT : ISQLitePlatform
    {
        public SQLitePlatformWinRT()
        {
            SQLiteApi = new SQLiteApiWinRT();
            VolatileService = new VolatileServiceWinRT();
            StopwatchFactory = new StopwatchFactoryWinRT();
            ReflectionService = new ReflectionServiceWinRT();
        }

        public ISQLiteApi SQLiteApi { get; private set; }
        public IStopwatchFactory StopwatchFactory { get; private set; }
        public IReflectionService ReflectionService { get; private set; }
        public IVolatileService VolatileService { get; private set; }

        public string DatabaseRootDirectory
        {
            get
            {
                return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            }
        }
    }
}