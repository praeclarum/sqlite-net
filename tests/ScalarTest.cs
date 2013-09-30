using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class ScalarTest
    {
        private class TestTable
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public int Two { get; set; }
        }

        private const int Count = 100;

        private SQLiteConnection CreateDb()
        {
            var db = new TestDb();
            db.CreateTable<TestTable>();
            IEnumerable<TestTable> items = from i in Enumerable.Range(0, Count)
                select new TestTable {Two = 2};
            db.InsertAll(items);
            Assert.AreEqual(Count, db.Table<TestTable>().Count());
            return db;
        }


        [Test]
        public void Int32()
        {
            SQLiteConnection db = CreateDb();

            var r = db.ExecuteScalar<int>("SELECT SUM(Two) FROM TestTable");

            Assert.AreEqual(Count*2, r);
        }
    }
}