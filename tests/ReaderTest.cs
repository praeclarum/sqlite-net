using System;
using System.Linq;
using System.Collections.Generic;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
    [TestFixture]
    public class ReaderTest
    {
        private class TestTable
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public double Two { get; set; }

            public byte[] Content { get; set; }

            public string Comment { get; set; }

            public int IndexLine { get; set; }

            public bool IsOddIndexLine { get; set; }

            public DateTime DateInsert { get; set; }
        }

        private const int Count = 100;

        private SQLiteConnection CreateDb()
        {
            var db = new TestDb();
            db.CreateTable<TestTable>();
            IEnumerable<TestTable> items = from i in Enumerable.Range(0, Count)
                select new TestTable
                {
                    Two = 2,
                    IndexLine = i,
                    Comment = "Comment " + i,
                    Content = new byte[2] { 1, 2 },
                    IsOddIndexLine = (i % 2) == 0,
                    DateInsert = DateTime.Now
                };
            db.InsertAll(items);
            Assert.AreEqual(Count, db.Table<TestTable>().Count());
            return db;
        }

        [Test]
        public void ExecuteReaderOnTestTable()
        {
            SQLiteConnection db = CreateDb();

            var r = db.ExecuteReader("SELECT * FROM TestTable");

            // All items are 
            Assert.AreEqual(Count, r.Count);

            var firstItem = r[0];

            // We must get 7 fields (Id, ...)
            Assert.AreEqual(7, firstItem.Fields.Count);

            // We will check types :
            Assert.AreEqual(typeof(int), firstItem["Id"].GetType());
            Assert.AreEqual(typeof(double), firstItem["Two"].GetType());
            Assert.AreEqual(typeof(string), firstItem["Comment"].GetType());
            Assert.AreEqual(typeof(byte[]), firstItem["Content"].GetType());
            Assert.AreEqual(typeof(int), firstItem["IndexLine"].GetType());
            // No boolean return by the query, boolean are managed as integer in sqlite
            Assert.AreEqual(typeof(int), firstItem["IsOddIndexLine"].GetType());
            // No DateTime return by the query, datetime are managed as integer in sqlite
            Assert.AreEqual(typeof(int), firstItem["DateInsert"].GetType());

        }
        
        [Test]
        public void ExecuteReaderAsScalar()
        {
            SQLiteConnection db = CreateDb();

            string sumColumnName = "SumResult";

            var r = db.ExecuteReader(string.Format("SELECT Sum(Two) as {0} FROM TestTable", sumColumnName));

            // All items are 
            Assert.AreEqual(1, r.Count);

            var firstItem = r[0];

            // We must get 1 fields (Id, ...)
            Assert.AreEqual(1, firstItem.Fields.Count);

            // The column must match name to SumResult
            Assert.AreEqual(sumColumnName, firstItem.Fields[0]);

            // Item must be a int            
            Assert.AreEqual(typeof(double), firstItem[sumColumnName].GetType());

            // Sum must be equal to 
            Assert.AreEqual(Count*2, firstItem[sumColumnName]);
        }
    }
}