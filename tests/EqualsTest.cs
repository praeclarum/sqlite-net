using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    internal class EqualsTest
    {
        public abstract class TestObjBase<T>
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public T Data { get; set; }

            public DateTime Date { get; set; }
        }

        public class TestObjString : TestObjBase<string>
        {
        }

        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(new SQLitePlatformTest(), path)
            {
                CreateTable<TestObjString>();
            }
        }

        [Test]
        public void CanCompareAnyField()
        {
            int n = 20;
            IEnumerable<TestObjString> cq = from i in Enumerable.Range(1, n)
                select new TestObjString
                {
                    Data = Convert.ToString(i),
                    Date = new DateTime(2013, 1, i)
                };

            var db = new TestDb(TestPath.CreateTemporaryDatabase());
            db.InsertAll(cq);

            TableQuery<TestObjString> results = db.Table<TestObjString>().Where(o => o.Data.Equals("10"));
            Assert.AreEqual(results.Count(), 1);
            Assert.AreEqual(results.FirstOrDefault().Data, "10");

            results = db.Table<TestObjString>().Where(o => o.Id.Equals(10));
            Assert.AreEqual(results.Count(), 1);
            Assert.AreEqual(results.FirstOrDefault().Data, "10");

            var date = new DateTime(2013, 1, 10);
            results = db.Table<TestObjString>().Where(o => o.Date.Equals(date));
            Assert.AreEqual(results.Count(), 1);
            Assert.AreEqual(results.FirstOrDefault().Data, "10");
        }
    }
}