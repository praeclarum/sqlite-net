
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TearDown = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestCleanupAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

using System.Diagnostics;

namespace SQLite.Tests
{    
    [TestFixture]
    public class UpdateTest
    {
        private TestDb _db;

		public class TestObjWithOne2Many
		{
			[PrimaryKey]
			public int Id {get; set;}

			[One2Many(typeof(TestDependentObj))]
			public List<TestDependentObj> ObjList {get; set;}
		}

		public class TestDependentObj
		{
			[PrimaryKey]
			public int Id {get; set;}
			public string Text {get; set;}

			[References(typeof(TestObjWithOne2Many))]
			[OnUpdateCascade]
			public int OwnerId {get; set;}
		}

        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(path)
            {
				CreateTable<TestObjWithOne2Many>();
				CreateTable<TestDependentObj>();
            }
        }
		
        [SetUp]
        public void Setup()
        {
            _db = new TestDb(TestPath.GetTempFileName());
			_db.SetForeignKeysPermissions(true);
        }
        [TearDown]
        public void TearDown()
        {
            if (_db != null) _db.Close();
        }

		[Test]
		public void UpdateWithOnUpdateCascade()
		{
			var ownerObjs = Enumerable.Range(1,3)
				.Select(i => new TestObjWithOne2Many {Id = i, 
													  ObjList = Enumerable.Range(1,5)
														.Select(x => new TestDependentObj{Id = (i*10) + x,
																						  OwnerId = i,
																						  Text = "Test" + ((int)(i*10) + x)})
																							.ToList()}).ToList();

			_db.InsertAll(ownerObjs);
			var map = new TableMapping(typeof(TestObjWithOne2Many));
			var q = string.Format("update {0} set Id = 10 where Id = {1}",map.TableName, ownerObjs[0].Id);
			_db.Query(map,q,null);

			var parents = _db.Table<TestObjWithOne2Many>().Where(x => x.Id == 10).ToList();
			var childs = _db.Table<TestDependentObj>().Where(x => x.OwnerId == 10).ToList();

			Assert.AreEqual(1, parents.Count());
			Assert.AreEqual(5, childs.Count());		
		}

    }
}
