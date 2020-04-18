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
    public class CollateTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
			
			public string CollateDefault { get; set; }
			
			[Collation("BINARY")]
			public string CollateBinary { get; set; }
			
			[Collation("RTRIM")]
			public string CollateRTrim { get; set; }
			
			[Collation("NOCASE")]
			public string CollateNoCase { get; set; }

            public override string ToString ()
            {
            	return string.Format("[TestObj: Id={0}]", Id);
            }
        }
		
        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(path)
            {
				Trace = true;
				CreateTable<TestObj>();
            }
        }
		
        [Test]
        public void Collate()
        {
			var obj = new TestObj() {
				CollateDefault = "Alpha ",
				CollateBinary = "Alpha ",
				CollateRTrim = "Alpha ",
				CollateNoCase = "Alpha ",
			};
			
			var db = new TestDb(TestPath.GetTempFileName());
						
			db.Insert(obj);			
			
			Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateDefault == "Alpha " select o).Count());
			Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "ALPHA " select o).Count());
			Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "Alpha" select o).Count());
			Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "ALPHA" select o).Count());
			
			Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateBinary == "Alpha " select o).Count());
			Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "ALPHA " select o).Count());
			Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "Alpha" select o).Count());
			Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "ALPHA" select o).Count());
			
			Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateRTrim == "Alpha " select o).Count());
			Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateRTrim == "ALPHA " select o).Count());
			Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateRTrim == "Alpha" select o).Count());
			Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateRTrim == "ALPHA" select o).Count());
			
			Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateNoCase == "Alpha " select o).Count());
			Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateNoCase == "ALPHA " select o).Count());
			Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateNoCase == "Alpha" select o).Count());
			Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateNoCase == "ALPHA" select o).Count());
        }
    }
}
