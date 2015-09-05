using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class SkipTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public int Order { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Order={1}]", Id, Order);
            }
        }

        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(new SQLitePlatformTest(), path)
            {
                CreateTable<TestObj>();
            }
        }

        [Test]
        public void Skip()
        {
            int n = 100;

            IEnumerable<TestObj> cq = from i in Enumerable.Range(1, n)
                                      select new TestObj
                                      {
                                          Order = i
                                      };
            TestObj[] objs = cq.ToArray();
            var db = new TestDb(TestPath.CreateTemporaryDatabase());

            int numIn = db.InsertAll(objs);
            Assert.AreEqual(numIn, n, "Num inserted must = num objects");

            TableQuery<TestObj> q = from o in db.Table<TestObj>()
                                    orderby o.Order
                                    select o;

            TableQuery<TestObj> qs1 = q.Skip(1);
            List<TestObj> s1 = qs1.ToList();
            Assert.AreEqual(n - 1, s1.Count);
            Assert.AreEqual(2, s1[0].Order);

            TableQuery<TestObj> qs5 = q.Skip(5);
            List<TestObj> s5 = qs5.ToList();
            Assert.AreEqual(n - 5, s5.Count);
            Assert.AreEqual(6, s5[0].Order);
        }


        [Test]
        public void MultipleSkipsWillSkipTheSumOfTheSkips()
        {
            int n = 100;

            IEnumerable<TestObj> cq = from i in Enumerable.Range(1, n)
                                      select new TestObj
                                      {
                                          Order = i
                                      };
            TestObj[] objs = cq.ToArray();
            var db = new TestDb(TestPath.CreateTemporaryDatabase());

            int numIn = db.InsertAll(objs);
            Assert.AreEqual(numIn, n, "Num inserted must = num objects");

            TableQuery<TestObj> q = from o in db.Table<TestObj>()
                                    orderby o.Order
                                    select o;

            TableQuery<TestObj> qs1 = q.Skip(1).Skip(5);
            List<TestObj> s1 = qs1.ToList();
            Assert.AreEqual(n - 6, s1.Count, "Should have skipped 5 + 1 = 6 objects.");
            Assert.AreEqual(7, s1[0].Order);
        }

        [Test]
        public void MultipleTakesWillTakeTheMinOfTheTakes()
        {
            var db = GetTestDBWith100Elements();

            TableQuery<TestObj> q = from o in db.Table<TestObj>()
                                    orderby o.Order
                                    select o;

            TableQuery<TestObj> qs1 = q.Take(1).Take(5);
            List<TestObj> s1 = qs1.ToList();
            Assert.AreEqual(1, s1.Count, "Should have taken exactly one object.");
            Assert.AreEqual(1, s1[0].Order);
        }

        private static TestDb GetTestDBWith100Elements()
        {
            int n = 100;

            IEnumerable<TestObj> cq = from i in Enumerable.Range(1, n)
                                      select new TestObj
                                      {
                                          Order = i
                                      };
            TestObj[] objs = cq.ToArray();
            var db = new TestDb(TestPath.CreateTemporaryDatabase());

            int numIn = db.InsertAll(objs);
            Assert.AreEqual(numIn, n, "Num inserted must = num objects");
            return db;
        }

        [Test]
        public void SkipAndWhereWorkTogether()
        {
            var testDB = GetTestDBWith100Elements();
            IEnumerable<TestObj> last91Elements = testDB.Table<TestObj>().OrderBy(o => o.Order).Where(o => o.Order != 5).Skip(10);
            Assert.That(last91Elements.Count(), Is.EqualTo(89), "Should miss out element number 5 and 10 more.");

            try
            {
                IEnumerable<TestObj> last90Elements = testDB.Table<TestObj>().OrderBy(o => o.Order).Skip(10).Where(o => o.Order != 5);
                Assert.That(last90Elements.Count(), Is.EqualTo(90), "Should have skipped just the first 10 elements as element number 5 was in the first 10.");
            }
            catch (NotSupportedException)
            {
                //Not supported exception is better than the wrong answer.
            }
           
        }
    }
}