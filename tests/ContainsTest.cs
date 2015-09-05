using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class ContainsTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Name={1}]", Id, Name);
            }
        }

        public class TestDb : SQLiteConnection
        {
            public TestDb(ISQLitePlatform sqlitePlatform, string path)
                : base(sqlitePlatform, path)
            {
                CreateTable<TestObj>();
            }
        }

        [Test]
        public void ContainsConstantData()
        {
            int n = 20;
            IEnumerable<TestObj> cq = from i in Enumerable.Range(1, n)
                select new TestObj
                {
                    Name = i.ToString()
                };

            var db = new TestDb(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());

            db.InsertAll(cq);

            db.TraceListener = DebugTraceListener.Instance;

            var tensq = new[] {"0", "10", "20"};
            List<TestObj> tens = (from o in db.Table<TestObj>() where tensq.Contains(o.Name) select o).ToList();
            Assert.AreEqual(2, tens.Count);

            var moreq = new[] {"0", "x", "99", "10", "20", "234324"};
            List<TestObj> more = (from o in db.Table<TestObj>() where moreq.Contains(o.Name) select o).ToList();
            Assert.AreEqual(2, more.Count);
        }

        [Test]
        public void ContainsQueriedData()
        {
            int n = 20;
            IEnumerable<TestObj> cq = from i in Enumerable.Range(1, n)
                select new TestObj
                {
                    Name = i.ToString()
                };

            var db = new TestDb(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());

            db.InsertAll(cq);

            db.TraceListener = DebugTraceListener.Instance;

            var tensq = new[] {"0", "10", "20"};
            List<TestObj> tens = (from o in db.Table<TestObj>() where tensq.Contains(o.Name) select o).ToList();
            Assert.AreEqual(2, tens.Count);

            var moreq = new[] {"0", "x", "99", "10", "20", "234324"};
            List<TestObj> more = (from o in db.Table<TestObj>() where moreq.Contains(o.Name) select o).ToList();
            Assert.AreEqual(2, more.Count);

            // https://github.com/praeclarum/SQLite.Net/issues/28
            List<string> moreq2 = moreq.ToList();
            List<TestObj> more2 = (from o in db.Table<TestObj>() where moreq2.Contains(o.Name) select o).ToList();
            Assert.AreEqual(2, more2.Count);
        }
    }
}