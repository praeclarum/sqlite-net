using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SQLite.Net.Async;
using SQLite.Net.Attributes;
using SQLite.Net.Platform.Win32;

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
            var sqLiteConnectionPool = new SQLiteConnectionPool(new SQLitePlatformWin32());
            var sqLiteConnectionString = new SQLiteConnectionString(TestPath.GetTempFileName(), true);
            var db = new SQLiteAsyncConnection(() => sqLiteConnectionPool.GetConnection(sqLiteConnectionString));
            await TestAsyncDateTime(db);
        }
    }
}