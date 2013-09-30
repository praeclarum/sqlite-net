using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class DeleteTest
    {
        private class TestTable
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public int Datum { get; set; }
        }

        private const int Count = 100;

        private SQLiteConnection CreateDb()
        {
            var db = new TestDb();
            db.CreateTable<TestTable>();
            IEnumerable<TestTable> items = from i in Enumerable.Range(0, Count)
                select new TestTable {Datum = 1000 + i};
            db.InsertAll(items);
            Assert.AreEqual(Count, db.Table<TestTable>().Count());
            return db;
        }

        [Test]
        public void DeleteAll()
        {
            SQLiteConnection db = CreateDb();

            int r = db.DeleteAll<TestTable>();

            Assert.AreEqual(Count, r);
            Assert.AreEqual(0, db.Table<TestTable>().Count());
        }

        [Test]
        public void DeleteEntityOne()
        {
            SQLiteConnection db = CreateDb();

            int r = db.Delete(db.Get<TestTable>(1));

            Assert.AreEqual(1, r);
            Assert.AreEqual(Count - 1, db.Table<TestTable>().Count());
        }

        [Test]
        public void DeletePKNone()
        {
            SQLiteConnection db = CreateDb();

            int r = db.Delete<TestTable>(348597);

            Assert.AreEqual(0, r);
            Assert.AreEqual(Count, db.Table<TestTable>().Count());
        }

        [Test]
        public void DeletePKOne()
        {
            SQLiteConnection db = CreateDb();

            int r = db.Delete<TestTable>(1);

            Assert.AreEqual(1, r);
            Assert.AreEqual(Count - 1, db.Table<TestTable>().Count());
        }
    }
}