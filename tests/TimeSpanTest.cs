using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SQLite.Net.Async;
using SQLite.Net.Attributes;

#if __WIN32__
using SQLitePlatformTest = SQLite.Net.Platform.Win32.SQLitePlatformWin32;
#elif WINDOWS_PHONE
using SQLitePlatformTest = SQLite.Net.Platform.WindowsPhone8.SQLitePlatformWP8;
#elif __WINRT__
using SQLitePlatformTest = SQLite.Net.Platform.WinRT.SQLitePlatformWinRT;
#elif __IOS__
using SQLitePlatformTest = SQLite.Net.Platform.XamarinIOS.SQLitePlatformIOS;
#elif __ANDROID__
using SQLitePlatformTest = SQLite.Net.Platform.XamarinAndroid.SQLitePlatformAndroid;
#elif __OSX__
using SQLitePlatformTest = SQLite.Net.Platform.OSX.SQLitePlatformOSX;
#else
using SQLitePlatformTest = SQLite.Net.Platform.Generic.SQLitePlatformGeneric;
#endif


namespace SQLite.Net.Tests
{
    [TestFixture]
    public class TimeSpanTest
    {
        private class TestDb
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public TimeSpan Time { get; set; }
        }


        private async Task TestAsyncDateTime(SQLiteAsyncConnection db)
        {
            await db.CreateTableAsync<TestDb>();

            var val1 = new TestDb
            {
                Time = new TimeSpan(1000),
            };
            await db.InsertAsync(val1);
            TestDb val2 = await db.GetAsync<TestDb>(val1.Id);
            Assert.AreEqual(val1.Time, val2.Time);
        }

        [Test]
        public async Task TestTimeSpan()
        {
            var sqLiteConnectionString = new SQLiteConnectionString(TestPath.CreateTemporaryDatabase(), true);
            var db = new SQLiteAsyncConnection(() => new SQLiteConnectionWithLock(new SQLitePlatformTest(), sqLiteConnectionString));
            await TestAsyncDateTime(db);
        }
    }
}