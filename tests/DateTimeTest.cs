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
            public DateTime ModifiedTime { get; set; }
        }


        private async Task TestAsyncDateTime(SQLiteAsyncConnection db, bool storeDateTimeAsTicks)
        {
            await db.CreateTableAsync<TestObj>();

            TestObj o, o2;

            //
            // Ticks
            //
            o = new TestObj
            {
                ModifiedTime = new DateTime(2012, 1, 14, 3, 2, 1, DateTimeKind.Utc),
            };
            await db.InsertAsync(o);
            o2 = await db.GetAsync<TestObj>(o.Id);
            Assert.AreEqual(o.ModifiedTime, o2.ModifiedTime.ToUniversalTime());

            var expectedTimeZone = storeDateTimeAsTicks ? DateTimeKind.Utc : DateTimeKind.Local;
            Assert.AreEqual(o2.ModifiedTime.Kind, expectedTimeZone);
        }

        private void TestDateTime(TestDb db)
        {
            db.CreateTable<TestObj>();

            TestObj o, o2;

            //
            // Ticks
            //
            o = new TestObj
            {
                ModifiedTime = new DateTime(2012, 1, 14, 3, 2, 1, DateTimeKind.Utc),
            };
            db.Insert(o);
            o2 = db.Get<TestObj>(o.Id);
            Assert.AreEqual(o.ModifiedTime, o2.ModifiedTime.ToUniversalTime());

            var expectedTimeZone = db.StoreDateTimeAsTicks ? DateTimeKind.Utc : DateTimeKind.Local;
            Assert.AreEqual(o2.ModifiedTime.Kind, expectedTimeZone);
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