using System;
using System.Linq;

using NUnit.Framework;
using SQLite.Net;
using SQLite.Net.Attributes;
using SQLite.Net.Platform.Win32;

namespace SQLite.Net.Tests
{
    [TestFixture]
    class EqualsTest
    {
        public abstract class TestObjBase<T>
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public T Data { get; set; }

            public DateTime Date { get; set; }
        }

        public class TestObjString : TestObjBase<string> { }

        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(new SQLitePlatformWin32(), path)
            {
                CreateTable<TestObjString>();
            }
        }

        [Test]
        public void CanCompareAnyField()
        {
            var n = 20;
            var cq =from i in Enumerable.Range(1, n)
					select new TestObjString {
				Data = Convert.ToString(i),
                Date = new DateTime(2013, 1, i)
			};

            var db = new TestDb(TestPath.GetTempFileName());
            db.InsertAll(cq);

            var results = db.Table<TestObjString>().Where(o => o.Data.Equals("10"));
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
