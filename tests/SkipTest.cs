using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using SQLite;

#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
#if NETFX_CORE
    [TestClass]
#else
    [TestFixture]
#endif
    public class SkipTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
			public int Order { get; set; }

            public override string ToString ()
            {
            	return string.Format("[TestObj: Id={0}, Order={1}]", Id, Order);
            }

        }
        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(path)
            {
				CreateTable<TestObj>();
            }
        }

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        public void Skip()
        {
			var n = 100;
			
			var cq =	from i in Enumerable.Range(1, n)
					select new TestObj() {
				Order = i
			};
			var objs = cq.ToArray();
			var db = new TestDb(TestHelper.GetTempDatabaseName());
						
			var numIn = db.InsertAll(objs);			
			Assert.AreEqual(numIn, n, "Num inserted must = num objects");
			
			var q = from o in db.Table<TestObj>()
					orderby o.Order
					select o;
			
			var qs1 = q.Skip(1);			
			var s1 = qs1.ToList();
			Assert.AreEqual(n - 1, s1.Count);
			Assert.AreEqual(2, s1[0].Order);
			
			var qs5 = q.Skip(5);			
			var s5 = qs5.ToList();
			Assert.AreEqual(n - 5, s5.Count);
			Assert.AreEqual(6, s5[0].Order);
        }
    }
}
