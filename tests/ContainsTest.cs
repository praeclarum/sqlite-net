using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

using System.Diagnostics;

namespace SQLite.Tests
{    
    [TestFixture]
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
		
        [Test]
        public void ContainsConstantData()
        {
			int n = 20;
			var cq =from i in Enumerable.Range(1, n)
					select new TestObj() {
				Name = i.ToString()
			};
			
			var db = new TestDb(TestPath.GetTempFileName());
			
			db.InsertAll(cq);
			
			db.Trace = true;
			
			var tensq = new string[] { "0", "10", "20" };			
			var tens = (from o in db.Table<TestObj>() where tensq.Contains(o.Name) select o).ToList();
			Assert.AreEqual(2, tens.Count);
			
			var moreq = new string[] { "0", "x", "99", "10", "20", "234324" };			
			var more = (from o in db.Table<TestObj>() where moreq.Contains(o.Name) select o).ToList();
			Assert.AreEqual(2, more.Count);
        }
		
		[Test]
        public void ContainsQueriedData()
        {
			int n = 20;
			var cq =from i in Enumerable.Range(1, n)
					select new TestObj() {
				Name = i.ToString()
			};
			
			var db = new TestDb(TestPath.GetTempFileName());
			
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
