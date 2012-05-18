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
    public class ContainsTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
			
			public string Name { get; set; }
			
            public override string ToString ()
            {
            	return string.Format("[TestObj: Id={0}, Name={1}]", Id, Name);
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
        public void ContainsConstantData()
        {
			int n = 20;
			var cq =from i in Enumerable.Range(1, n)
					select new TestObj() {
				Name = i.ToString()
			};
			
			var db = new TestDb(TestHelper.GetTempDatabaseName());
			
			db.InsertAll(cq);
			
			db.Trace = true;
			
			var tensq = new string[] { "0", "10", "20" };			
			var tens = (from o in db.Table<TestObj>() where tensq.Contains(o.Name) select o).ToList();
			Assert.AreEqual(2, tens.Count);
			
			var moreq = new string[] { "0", "x", "99", "10", "20", "234324" };			
			var more = (from o in db.Table<TestObj>() where moreq.Contains(o.Name) select o).ToList();
			Assert.AreEqual(2, more.Count);
        }

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        public void ContainsQueriedData()
        {
			int n = 20;
			var cq =from i in Enumerable.Range(1, n)
					select new TestObj() {
				Name = i.ToString()
			};
			
			var db = new TestDb(TestHelper.GetTempDatabaseName());
			
			db.InsertAll(cq);
			
			db.Trace = true;
			
			var tensq = new string[] { "0", "10", "20" };			
			var tens = (from o in db.Table<TestObj>() where tensq.Contains(o.Name) select o).ToList();
			Assert.AreEqual(2, tens.Count);
			
			var moreq = new string[] { "0", "x", "99", "10", "20", "234324" };			
			var more = (from o in db.Table<TestObj>() where moreq.Contains(o.Name) select o).ToList();
			Assert.AreEqual(2, more.Count);
			
			// https://github.com/praeclarum/sqlite-net/issues/28
			var moreq2 = moreq.ToList ();
			var more2 = (from o in db.Table<TestObj>() where moreq2.Contains(o.Name) select o).ToList();
			Assert.AreEqual(2, more2.Count);			
        }
    }
}
