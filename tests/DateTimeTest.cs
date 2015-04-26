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
#else
using SQLitePlatformTest = SQLite.Net.Platform.Generic.SQLitePlatformGeneric;
#endif


namespace SQLite.Net.Tests
{
    [TestFixture]
    public class DateTimeTest
    {
        private class TestObj
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Name { get; set; }
            public DateTime Time1 { get; set; }
            public DateTime Time2 { get; set; }
        }


        private async Task TestAsyncDateTime(SQLiteAsyncConnection db, bool storeDateTimeAsTicks)
        {
            await db.CreateTableAsync<TestObj>();

            var org = new TestObj
            {
                Time1 = DateTime.UtcNow,
                Time2 = DateTime.Now,
            };
            await db.InsertAsync(org);
            var fromDb = await db.GetAsync<TestObj>(org.Id);
            Assert.AreEqual(fromDb.Time1.ToUniversalTime(), org.Time1.ToUniversalTime());
            Assert.AreEqual(fromDb.Time2.ToUniversalTime(), org.Time2.ToUniversalTime());

            Assert.AreEqual(fromDb.Time1.ToLocalTime(), org.Time1.ToLocalTime());
            Assert.AreEqual(fromDb.Time2.ToLocalTime(), org.Time2.ToLocalTime());
        }

        private void TestDateTime(TestDb db)
        {
            db.CreateTable<TestObj>();

            //
            // Ticks
            //
            var org = new TestObj
            {
                Time1 = DateTime.UtcNow,
                Time2 = DateTime.Now,
            };
            db.Insert(org);
            var fromDb = db.Get<TestObj>(org.Id);
            Assert.AreEqual(fromDb.Time1.ToUniversalTime(), org.Time1.ToUniversalTime());
            Assert.AreEqual(fromDb.Time2.ToUniversalTime(), org.Time2.ToUniversalTime());

            Assert.AreEqual(fromDb.Time1.ToLocalTime(), org.Time1.ToLocalTime());
            Assert.AreEqual(fromDb.Time2.ToLocalTime(), org.Time2.ToLocalTime());
        }

        [Test]
        public void AsStrings()
        {
            var db = new TestDb(storeDateTimeAsTicks: false);
            TestDateTime(db);
        }

        [Test]
        public void AsTicks()
        {
            var db = new TestDb(true);
            TestDateTime(db);
        }

        [Test]
        public async Task AsyncAsString()
        {
            var sqLiteConnectionPool = new SQLiteConnectionPool(new SQLitePlatformTest());
            var sqLiteConnectionString = new SQLiteConnectionString(TestPath.GetTempFileName(), false);
            var db = new SQLiteAsyncConnection(() => sqLiteConnectionPool.GetConnection(sqLiteConnectionString));
            await TestAsyncDateTime(db, sqLiteConnectionString.StoreDateTimeAsTicks);
        }

        [Test]
        public async Task AsyncAsTicks()
        {
            var sqLiteConnectionPool = new SQLiteConnectionPool(new SQLitePlatformTest());
            var sqLiteConnectionString = new SQLiteConnectionString(TestPath.GetTempFileName(), true);
            var db = new SQLiteAsyncConnection(() => sqLiteConnectionPool.GetConnection(sqLiteConnectionString));
            await TestAsyncDateTime(db, sqLiteConnectionString.StoreDateTimeAsTicks);
        }
    }
}