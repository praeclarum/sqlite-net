using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SQLite.Net.Async;
using SQLite.Net.Attributes;

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
            var sqLiteConnectionString = new SQLiteConnectionString(TestPath.CreateTemporaryDatabase(), false);
            var db = new SQLiteAsyncConnection(() => new SQLiteConnectionWithLock(new SQLitePlatformTest(), sqLiteConnectionString));
            await TestAsyncDateTime(db, sqLiteConnectionString.StoreDateTimeAsTicks);
        }

        [Test]
        public async Task AsyncAsTicks()
        {
            var sqLiteConnectionString = new SQLiteConnectionString(TestPath.CreateTemporaryDatabase(), true);
            var db = new SQLiteAsyncConnection(() => new SQLiteConnectionWithLock(new SQLitePlatformTest(), sqLiteConnectionString));
            await TestAsyncDateTime(db, sqLiteConnectionString.StoreDateTimeAsTicks);
        }
    }
}