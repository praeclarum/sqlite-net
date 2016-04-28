using Windows.Storage;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.WinRT
{
    public class SQLitePlatformWinRT : ISQLitePlatform
    {
        /// <summary>
        /// Creates a SQLite platform object for use from WinRT.
        /// </summary>
        /// <param name="tempFolderPath">Optional: Temporary folder path. Defaults to <see cref="Windows.Storage.ApplicationData.Current.TemporaryFolder.Path"/></param>
        /// <param name="useWinSqlite">Optional: Whether to use WinSQLite instead of SQLite. WinSQLite is built-in to Windows 10.0.10586.0 and above. Using it can reduce app size and potentially increase SQLite load time.</param>
        public SQLitePlatformWinRT(string tempFolderPath = null, bool useWinSqlite = false)
        {
            SQLiteApi = new SQLiteApiWinRT(tempFolderPath, useWinSqlite);
            VolatileService = new VolatileServiceWinRT();
            StopwatchFactory = new StopwatchFactoryWinRT();
            ReflectionService = new ReflectionServiceWinRT();
        }

        public string DatabaseRootDirectory
        {
            get { return ApplicationData.Current.LocalFolder.Path; }
        }

        public ISQLiteApi SQLiteApi { get; private set; }
        public IStopwatchFactory StopwatchFactory { get; private set; }
        public IReflectionService ReflectionService { get; private set; }
        public IVolatileService VolatileService { get; private set; }
    }
}