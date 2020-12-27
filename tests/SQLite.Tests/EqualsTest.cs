using System;
using System.Linq;
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
                : base(path)
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
