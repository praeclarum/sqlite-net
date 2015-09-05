using System;
using SQLite.Net.Async;
using SQLite.Net.Attributes;
using NUnit.Framework;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class DateTimeOffsetTest
    {
        class TestObj
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Name { get; set; }
            public DateTimeOffset ModifiedTime { get; set; }
        }

        [Test]
        public void AsTicks ()
        {
            var db = new TestDb ();
            TestDateTimeOffset (db);
        }

        [Test]
        public void AsyncAsTicks ()
        {
            var sqLiteConnectionString = new SQLiteConnectionString(TestPath.CreateTemporaryDatabase(), false);
            var db = new SQLiteAsyncConnection(() => new SQLiteConnectionWithLock(new SQLitePlatformTest(), sqLiteConnectionString));
            TestAsyncDateTimeOffset (db);
        }

        void TestAsyncDateTimeOffset (SQLiteAsyncConnection db)
        {
            db.CreateTableAsync<TestObj> ().Wait ();

            TestObj o, o2;

            //
            // Ticks
            //
            o = new TestObj {
                ModifiedTime = new DateTimeOffset (2012, 1, 14, 3, 2, 1, TimeSpan.Zero),
            };
            db.InsertAsync (o).Wait ();
            o2 = db.GetAsync<TestObj> (o.Id).Result;
            Assert.AreEqual (o.ModifiedTime, o2.ModifiedTime);
        }

        void TestDateTimeOffset (TestDb db)
        {
            db.CreateTable<TestObj> ();

            TestObj o, o2;

            //
            // Ticks
            //
            o = new TestObj {
                ModifiedTime = new DateTimeOffset (2012, 1, 14, 3, 2, 1, TimeSpan.Zero),
            };
            db.Insert (o);
            o2 = db.Get<TestObj> (o.Id);
            Assert.AreEqual (o.ModifiedTime, o2.ModifiedTime);
        }

    }
}

