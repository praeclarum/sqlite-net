
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
    public class InsertTest
    {
        private TestDb _db;

        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
            public String Text { get; set; }

            public override string ToString ()
            {
            	return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }

        }

        public class TestObj2
        {
            [PrimaryKey]
            public int Id { get; set; }
            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }

        }

		public class TestObjWithOne2Many
		{
			[PrimaryKey]
			[AutoIncrement]
			public int Id {get; set;}

			[One2Many(typeof(TestDependentObj1))]
			public List<TestDependentObj1> ObjList {get; set;}

			[One2One(typeof(TestDependentObj1))]
			public TestDependentObj1 Obj {get; set;}

			public string Text {get;set;}
		}

		public class TestObjWithOne2One
		{
			[PrimaryKey]
			[AutoIncrement]
			public int Id {get; set;}


			[One2One(typeof(TestDependentObj2))]
			public TestDependentObj2 Obj1 {get; set;}

			[One2One(typeof(TestDependentObj2))]
			[Lazy]
			public TestDependentObj3 Obj2 {get; set;}

			public string Text {get;set;}
		}

		public class TestDependentObj1
		{
			[PrimaryKey]
			[AutoIncrement]
			public int Id {get; set;}
			public string Text {get; set;}

			[References(typeof(TestObjWithOne2Many))]
			[ForeignKey]
			public int OwnerId {get; set;}
		}

		public class TestDependentObj2
		{
			[PrimaryKey]
			[AutoIncrement]
			public int Id {get; set;}
			public string Text {get; set;}

			[References(typeof(TestObjWithOne2One))]
			[ForeignKey]
			public int OwnerId {get; set;}
		}

		public class TestDependentObj3
		{
			[PrimaryKey]
			[AutoIncrement]
			public int Id {get; set;}
			public string Text {get; set;}

			[References(typeof(TestObjWithOne2One))]
			[ForeignKey]
			public int OwnerId {get; set;}
		}

        public class OneColumnObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
        }

		public class UniqueObj
		{
			[PrimaryKey]
			public int Id { get; set; }
		}

        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(path)
            {
				CreateTable<TestObj>();
                CreateTable<TestObj2>();
                CreateTable<OneColumnObj>();
				CreateTable<UniqueObj>();
				CreateTable<TestObjWithOne2Many>();
				CreateTable<TestObjWithOne2One>();
				CreateTable<TestDependentObj1>();
				CreateTable<TestDependentObj2>();
				CreateTable<TestDependentObj3>();
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
        public void InsertALot()
        {
			int n = 10000;
			var q =	from i in Enumerable.Range(1, n)
					select new TestObj() {
				Text = "I am"
			};
			var objs = q.ToArray();
			_db.Trace = false;
			
			var sw = new Stopwatch();
			sw.Start();
			
			var numIn = _db.InsertAll(objs);
			
			sw.Stop();
			
			Assert.AreEqual(numIn, n, "Num inserted must = num objects");
			
			var inObjs = _db.CreateCommand("select * from TestObj").ExecuteQuery<TestObj>().ToArray();
			
			for (var i = 0; i < inObjs.Length; i++) {
				Assert.AreEqual(i+1, objs[i].Id);
				Assert.AreEqual(i+1, inObjs[i].Id);
				Assert.AreEqual("I am", inObjs[i].Text);
			}
			
			var numCount = _db.CreateCommand("select count(*) from TestObj").ExecuteScalar<int>();
			
			Assert.AreEqual(numCount, n, "Num counted must = num objects");
        }

        [Test]
        public void InsertTwoTimes()
        {
            var obj1 = new TestObj() { Text = "GLaDOS loves testing!" };
            var obj2 = new TestObj() { Text = "Keep testing, just keep testing" };


            var numIn1 = _db.Insert(obj1);
            var numIn2 = _db.Insert(obj2);
            Assert.AreEqual(1, numIn1);
            Assert.AreEqual(1, numIn2);

            var result = _db.Query<TestObj>("select * from TestObj").ToList();
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(obj1.Text, result[0].Text);
            Assert.AreEqual(obj2.Text, result[1].Text);
        }

        [Test]
        public void InsertIntoTwoTables()
        {
            var obj1 = new TestObj() { Text = "GLaDOS loves testing!" };
            var obj2 = new TestObj2() { Text = "Keep testing, just keep testing" };

            var numIn1 = _db.Insert(obj1);
            Assert.AreEqual(1, numIn1);
            var numIn2 = _db.Insert(obj2);
            Assert.AreEqual(1, numIn2);

            var result1 = _db.Query<TestObj>("select * from TestObj").ToList();
            Assert.AreEqual(numIn1, result1.Count);
            Assert.AreEqual(obj1.Text, result1.First().Text);

            var result2 = _db.Query<TestObj>("select * from TestObj2").ToList();
            Assert.AreEqual(numIn2, result2.Count);
        }

        [Test]
        public void InsertWithExtra()
        {
            var obj1 = new TestObj2() { Id=1, Text = "GLaDOS loves testing!" };
            var obj2 = new TestObj2() { Id=1, Text = "Keep testing, just keep testing" };
            var obj3 = new TestObj2() { Id=1, Text = "Done testing" };

            _db.Insert(obj1);
            
            
            try {
                _db.Insert(obj2);
                Assert.Fail("Expected unique constraint violation");
            }
            catch (SQLiteException) {
            }
            _db.Insert(obj2, "OR REPLACE");


            try {
                _db.Insert(obj3);
                Assert.Fail("Expected unique constraint violation");
            }
            catch (SQLiteException) {
            }
            _db.Insert(obj3, "OR IGNORE");

            var result = _db.Query<TestObj>("select * from TestObj2").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(obj2.Text, result.First().Text);
        }

        [Test]
        public void InsertIntoOneColumnAutoIncrementTable()
        {
            var obj = new OneColumnObj();
            _db.Insert(obj);

            var result = _db.Get<OneColumnObj>(1);
            Assert.AreEqual(1, result.Id);
        }

		[Test]
		public void InsertAllSuccessOutsideTransaction()
		{
			var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();

			_db.InsertAll(testObjects);

			Assert.AreEqual(testObjects.Count, _db.Table<UniqueObj>().Count());
		}

		[Test]
		public void InsertAllFailureOutsideTransaction()
		{
			var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();
			testObjects[testObjects.Count - 1].Id = 1; // causes the insert to fail because of duplicate key

			ExceptionAssert.Throws<SQLiteException>(() => _db.InsertAll(testObjects));

			Assert.AreEqual(0, _db.Table<UniqueObj>().Count());
		}

		[Test]
		public void InsertAllSuccessInsideTransaction()
		{
			var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();

			_db.RunInTransaction(() => {
				_db.InsertAll(testObjects);
			});

			Assert.AreEqual(testObjects.Count, _db.Table<UniqueObj>().Count());
		}

		[Test]
		public void InsertAllFailureInsideTransaction()
		{
			var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();
			testObjects[testObjects.Count - 1].Id = 1; // causes the insert to fail because of duplicate key

			ExceptionAssert.Throws<SQLiteException>(() => _db.RunInTransaction(() => {
				_db.InsertAll(testObjects);
			}));

			Assert.AreEqual(0, _db.Table<UniqueObj>().Count());
		}

		[Test]
		public void InsertOrReplace ()
		{
			_db.Trace = true;
			_db.InsertAll (from i in Enumerable.Range(1, 20) select new TestObj { Text = "#" + i });

			Assert.AreEqual (20, _db.Table<TestObj> ().Count ());

			var t = new TestObj { Id = 5, Text = "Foo", };
			_db.InsertOrReplace (t);

			var r = (from x in _db.Table<TestObj> () orderby x.Id select x).ToList ();
			Assert.AreEqual (20, r.Count);
			Assert.AreEqual ("Foo", r[4].Text);
		}

		[Test]
		public void InsertObjWithOne2Many()
		{
			var ownerObj = new TestObjWithOne2Many {Id = 1};
			var testObjects = Enumerable.Range(1,10)
				.Select(i => new TestDependentObj1 {Id = i,OwnerId = ownerObj.Id, Text = "Test"+i}).ToList();
			ownerObj.ObjList = testObjects;

			_db.Insert(ownerObj);
			var resultObj = _db.Table<TestObjWithOne2Many>().First();

			Assert.AreNotEqual(null,resultObj);
			Assert.AreEqual(10,resultObj.ObjList.Count);
			Assert.AreEqual("Test1",resultObj.ObjList.First(x => x.Id == 1).Text);			
		}

		[Test]
		public void InsertOrReplaceWithOne2Many()
		{
			var ownerObj = new TestObjWithOne2Many {Id = 1, Text = "Test1"};
			var testObjects = Enumerable.Range(1,10)
				.Select(i => new TestDependentObj1 {Id = i,OwnerId = ownerObj.Id, Text = "Test"+i}).ToList();
			ownerObj.ObjList = testObjects;

			_db.InsertOrReplace(ownerObj);
			var tmpObject = _db.Table<TestObjWithOne2Many>().First();
			foreach(var o in ownerObj.ObjList)
			{
				o.Text += o.Id;
			}
			tmpObject.Text = "Test2";

			_db.InsertOrReplace(tmpObject);
			var resultObjs = _db.Table<TestObjWithOne2Many>().ToList();

			Assert.AreEqual(1, resultObjs.Count);
			Assert.AreEqual(tmpObject.ObjList[0].Text, resultObjs[0].ObjList[0].Text);
			Assert.AreNotEqual(ownerObj.ObjList[0].Text, resultObjs[0].ObjList[0].Text);
		}

		[Test]
		public void InsertAllWithOne2Many()
		{
			var ownerObjs = Enumerable.Range(1,3)
				.Select(i => new TestObjWithOne2Many {Id = i, 
													  ObjList = Enumerable.Range(1,5)
														.Select(x => new TestDependentObj1{Id = (i*10) + x,
																						  OwnerId = i,
																						  Text = "Test" + ((int)(i*10) + x)})
																							.ToList()}).ToList();

			_db.InsertAll(ownerObjs);

			var resultObjs = _db.Table<TestObjWithOne2Many>().ToList();
			var testObj1 = resultObjs.First(x => x.Id == 1);
			var testObj2 = resultObjs.First(x => x.Id == 2);

			Assert.AreEqual(3,resultObjs.Count);
			Assert.AreEqual("Test11", testObj1.ObjList[0].Text);
			Assert.AreEqual("Test12", testObj1.ObjList[1].Text);
			Assert.AreEqual("Test21", testObj2.ObjList[0].Text);
			Assert.AreEqual("Test22", testObj2.ObjList[1].Text);
		}

		[Test]
		public void InsertObjWithOwnerIdAutogetting()
		{
			var ownerObj = new TestObjWithOne2Many ();
			var testObjects = Enumerable.Range(1,10)
				.Select(i => new TestDependentObj1 {Text = "Test"+i}).ToList();
			ownerObj.ObjList = testObjects;

			_db.Insert(ownerObj);
			var resultObj = _db.Table<TestObjWithOne2Many>().First();

			Assert.AreNotEqual(null,resultObj);
			Assert.AreEqual(10,resultObj.ObjList.Count);
			Assert.AreEqual("Test1",resultObj.ObjList.First(x => x.Id == 1).Text);			
		}

		[Test]
		public void InsertOrReplaceWithOwnerIdAutogetting()
		{
			var ownerObj = new TestObjWithOne2Many {Text = "Test1"};
			var testObjects = Enumerable.Range(1,10)
				.Select(i => new TestDependentObj1 {Text = "Test"+i}).ToList();
			ownerObj.ObjList = testObjects;

			_db.InsertOrReplace(ownerObj);
			var tmpObject = _db.Table<TestObjWithOne2Many>().First();
			foreach(var o in tmpObject.ObjList)
			{
				o.Text += o.Id;
			}
			tmpObject.Text = "Test2";

			_db.InsertOrReplace(tmpObject);
			var resultObjs = _db.Table<TestObjWithOne2Many>().ToList();

			Assert.AreEqual(1, resultObjs.Count);
			Assert.AreEqual(tmpObject.ObjList[0].Text, resultObjs[0].ObjList[0].Text);
			Assert.AreNotEqual(ownerObj.ObjList[0].Text, resultObjs[0].ObjList[0].Text);
		}

		[Test]
		public void InsertAllWithOwnerIdAutogetting()
		{
			var ownerObjs = Enumerable.Range(1,3)
				.Select(i => new TestObjWithOne2Many {ObjList = Enumerable.Range(1,5)
														.Select(x => new TestDependentObj1{Text = "Test" + ((int)(i*10) + x)})
																							.ToList()}).ToList();

			_db.InsertAll(ownerObjs);

			var resultObjs = _db.Table<TestObjWithOne2Many>().ToList();
			var testObj1 = resultObjs.First(x => x.Id == 1);
			var testObj2 = resultObjs.First(x => x.Id == 2);

			Assert.AreEqual(3,resultObjs.Count);
			Assert.AreEqual("Test11", testObj1.ObjList[0].Text);
			Assert.AreEqual("Test12", testObj1.ObjList[1].Text);
			Assert.AreEqual("Test21", testObj2.ObjList[0].Text);
			Assert.AreEqual("Test22", testObj2.ObjList[1].Text);
		}

		[Test]
		public void ForeignKeyConstraintWhileInsertTest()
		{
			var ownerObj = new TestObjWithOne2Many {Id = 1, Text = "Test1"};
			var testObjects = Enumerable.Range(1,10)
				.Select(i => new TestDependentObj1 {Id = i,OwnerId = ownerObj.Id, Text = "Test"+i}).ToList();
			testObjects.Add(new TestDependentObj1{Id = 11,OwnerId = 99});
			ownerObj.ObjList = testObjects;

			string exception = string.Empty;
			try{
				_db.Insert(ownerObj);
			}
			catch(SQLiteException ex){
				exception = ex.Message;
			}

			Assert.AreNotEqual(string.Empty,exception);
			Assert.AreEqual("Constraint", exception);
		}

		
		[Test]
		public void InsertOrReplaceWithOne2One()
		{
			var ownerObj = new TestObjWithOne2One {Id = 1, Text = "Test1"};

			ownerObj.Obj1 = new TestDependentObj2{Text = "DependentObj1", OwnerId = 1};

			_db.InsertOrReplace(ownerObj);
			var tmpObject = _db.Table<TestObjWithOne2One>().First();


			tmpObject.Text = "Test2";

			_db.InsertOrReplace(tmpObject);
			var resultObjs = _db.Table<TestObjWithOne2One>().ToList();

			Assert.AreEqual(1, resultObjs.Count);
			Assert.AreEqual("Test2",resultObjs[0].Text);
		}

		[Test]
		public void LazyLoadTest()
		{
			var ownerObj = new TestObjWithOne2One {Id = 1, Text = "Test1"};

			ownerObj.Obj1 = new TestDependentObj2{Text = "Obj1", OwnerId = 1};
			ownerObj.Obj2 = new TestDependentObj3{Text = "Obj2", OwnerId = 1};

			_db.InsertOrReplace(ownerObj);

			var resultObj = _db.Table<TestObjWithOne2One>().First(x => x.Id == 1);
			var resultDependentObj = _db.Table<TestDependentObj3>().First(x => x.OwnerId == 1);

			Assert.AreEqual("Obj1",resultObj.Obj1.Text);
			Assert.AreEqual(null,resultObj.Obj2);
			Assert.AreEqual("Obj2",resultDependentObj.Text);
		}
    }
}
