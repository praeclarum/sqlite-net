using SQLite.Net.Interop;

namespace SQLite.Net.Platform.Win32
{
    public class SQLitePlatformWin32 : ISQLitePlatform
    {
        public SQLitePlatformWin32(string nativeInteropSearchPath = null)
        {
            SQLiteApi = new SQLiteApiWin32(nativeInteropSearchPath);
            StopwatchFactory = new StopwatchFactoryWin32();
            ReflectionService = new ReflectionServiceWin32();
            VolatileService = new VolatileServiceWin32();
        }

        public ISQLiteApi SQLiteApi { get; private set; }
        public IStopwatchFactory StopwatchFactory { get; private set; }
        public IReflectionService ReflectionService { get; private set; }
        public IVolatileService VolatileService { get; private set; }
    }
}