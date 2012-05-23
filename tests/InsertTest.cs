
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

        public class OneColumnObj
        {
            [AutoIncrement, PrimaryKey]
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
            }
        }

#if NETFX_CORE
        [TestInitialize]
#else
        [SetUp]
#endif
        public void Setup()
        {
            _db = new TestDb(TestHelper.GetTempDatabasePath());
        }

#if NETFX_CORE
        [TestCleanup]
#else
        [TearDown]
#endif
        public void TearDown()
        {
            if (_db != null) _db.Dispose();
        }

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
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

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
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

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
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

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
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

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        public void InsertIntoOneColumnAutoIncrementTable()
        {
            var obj = new OneColumnObj();
            _db.Insert(obj);

            var result = _db.Get<OneColumnObj>(1);
            Assert.AreEqual(1, result.Id);
        }
    }
}
