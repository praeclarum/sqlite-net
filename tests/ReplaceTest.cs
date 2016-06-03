using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class ReplaceTest
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

        public void ReplaceInWhere()
        {
            string testElement = "Element";
            string alternateElement = "Alternate";
            string replacedElement = "ReplacedElement";

            int n = 20;
            IEnumerable<TestObj> cq = from i in Enumerable.Range(1, n)
                                      select new TestObj
                                      {
                                          Name = (i % 2 == 0) ? testElement : alternateElement
                                      };

            var db = new TestDb(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());

            db.InsertAll(cq);

            db.TraceListener = DebugTraceListener.Instance;


            List<TestObj> result = (from o in db.Table<TestObj>() where o.Name.Replace(testElement, replacedElement) == replacedElement select o).ToList();
            Assert.AreEqual(10, result.Count);

        }

    }
}