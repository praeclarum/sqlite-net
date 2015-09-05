using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    internal class ArithmeticTest
    {
        public abstract class TestObjBase<T>
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public T Data { get; set; }

        }

        public class TestObjString : TestObjBase<string>
        {
        }

        public class TestObjInt : TestObjBase<int>
        {
        }

        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(new SQLitePlatformTest(), path)
            {
                CreateTable<TestObjString>();
                CreateTable<TestObjInt>();
            }
        }

        [Test]
        public void CanHaveAddInWhereClause()
        {
            int n = 20;
            IEnumerable<TestObjInt> cq = from i in Enumerable.Range(1, n)
                                         select new TestObjInt()
                                         {
                                             Data = i,
                                         };

            var db = new TestDb(TestPath.CreateTemporaryDatabase());
            db.InsertAll(cq);

            TableQuery<TestObjInt> results = db.Table<TestObjInt>().Where(o => o.Data + 10 >= n);
            Assert.AreEqual(results.Count(), 11);
            Assert.AreEqual(results.OrderBy(o => o.Data).FirstOrDefault().Data, 10);
        }

        [Test]
        public void CanHaveSubtractInWhereClause()
        {
            int n = 20;
            IEnumerable<TestObjInt> cq = from i in Enumerable.Range(1, n)
                                         select new TestObjInt()
                                         {
                                             Data = i,
                                         };

            var db = new TestDb(TestPath.CreateTemporaryDatabase());
            db.InsertAll(cq);

            TableQuery<TestObjInt> results = db.Table<TestObjInt>().Where(o => o.Data - 10 >= 0);
            Assert.AreEqual(results.Count(), 11);
            Assert.AreEqual(results.OrderBy(o => o.Data).FirstOrDefault().Data, 10);
        }

        [Test]
        public void AddForStringsMeansConcatenate()
        {
            int n = 20;
            IEnumerable<TestObjString> cq = from i in Enumerable.Range(1, n)
                                            select new TestObjString()
                                            {
                                                Data = i.ToString(),
                                            };

            var db = new TestDb(TestPath.CreateTemporaryDatabase());
            db.InsertAll(cq);

            TableQuery<TestObjString> results = db.Table<TestObjString>().Where(o => o.Data + "1" == "11");

            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("1", results.OrderBy(o => o.Data).FirstOrDefault().Data);
        }
    }
}