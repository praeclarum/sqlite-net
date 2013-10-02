using System;
using NUnit.Framework;
using SQLite.Net.Async;
using SQLite.Net.Attributes;
using SQLite.Net.Platform.Win32;

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


        private void TestAsyncDateTime(SQLiteAsyncConnection db)
        {
            db.CreateTableAsync<TestObj>().Wait();

            TestObj o, o2;

            //
            // Ticks
            //
            o = new TestObj
            {
                ModifiedTime = new DateTime(2012, 1, 14, 3, 2, 1),
            };
            db.InsertAsync(o).Wait();
            o2 = db.GetAsync<TestObj>(o.Id).Result;
            Assert.AreEqual(o.ModifiedTime, o2.ModifiedTime);
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
                ModifiedTime = new DateTime(2012, 1, 14, 3, 2, 1),
            };
            db.Insert(o);
            o2 = db.Get<TestObj>(o.Id);
            Assert.AreEqual(o.ModifiedTime, o2.ModifiedTime);
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
        public void AsyncAsString()
        {
            var sqLiteConnectionPool = new SQLiteConnectionPool(new SQLitePlatformWin32());
            var sqLiteConnectionString = new SQLiteConnectionString(TestPath.GetTempFileName(), false);
            var db = new SQLiteAsyncConnection(() => sqLiteConnectionPool.GetConnection(sqLiteConnectionString));
            TestAsyncDateTime(db);
        }

        [Test]
        public void AsyncAsTicks()
        {
            var sqLiteConnectionPool = new SQLiteConnectionPool(new SQLitePlatformWin32());
            var sqLiteConnectionString = new SQLiteConnectionString(TestPath.GetTempFileName(), true);
            var db = new SQLiteAsyncConnection(() => sqLiteConnectionPool.GetConnection(sqLiteConnectionString));
            TestAsyncDateTime(db);
        }
    }
}