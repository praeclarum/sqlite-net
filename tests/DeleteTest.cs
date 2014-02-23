using System;
using System.Linq;
using System.Collections.Generic;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
	[TestFixture]
	public class DeleteTest
	{
		class TestTable
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }
			public int Datum { get; set; }
		}

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
			[AutoIncrement]
			public int Id {get; set;}

			[References(typeof(TestObjWithOne2Many))]
			[OnDeleteCascade]
			public int OwnerId {get; set;}
		}

		const int Count = 100;
		const int parentCount = 3;
		const int childCount = 5;

		SQLiteConnection CreateDb ()
		{
			var db = new TestDb ();
			db.CreateTable<TestTable> ();
			var items = from i in Enumerable.Range (0, Count)
				select new TestTable { Datum = 1000+i };
			db.InsertAll (items);
			Assert.AreEqual (Count, db.Table<TestTable> ().Count ());
			return db;
		}

		SQLiteConnection CreateDbWithOne2Many ()
		{
			var db = new TestDb ()
				.SetForeignKeysPermissions(true);
			db.CreateTable<TestObjWithOne2Many> ();
			db.CreateTable<TestDependentObj> ();
			var items = Enumerable.Range(1,parentCount)
							.Select(i => new TestObjWithOne2Many {Id = i, 
																  ObjList = Enumerable.Range(1,childCount)
																	.Select(x => new TestDependentObj{OwnerId = i})
																										.ToList()}).ToList();
			db.InsertAll (items);
			Assert.AreEqual (parentCount, db.Table<TestObjWithOne2Many> ().Count ());
			Assert.AreEqual (parentCount * childCount , db.Table<TestDependentObj> ().Count ());
			return db;
		}

		[Test]
		public void DeleteEntityOne ()
		{
			var db = CreateDb ();

			var r = db.Delete (db.Get<TestTable> (1));

			Assert.AreEqual (1, r);
			Assert.AreEqual (Count - 1, db.Table<TestTable> ().Count ());
		}

		[Test]
		public void DeletePKOne ()
		{
			var db = CreateDb ();

			var r = db.Delete<TestTable> (1);

			Assert.AreEqual (1, r);
			Assert.AreEqual (Count - 1, db.Table<TestTable> ().Count ());
		}

		[Test]
		public void DeletePKNone ()
		{
			var db = CreateDb ();

			var r = db.Delete<TestTable> (348597);

			Assert.AreEqual (0, r);
			Assert.AreEqual (Count, db.Table<TestTable> ().Count ());
		}

		[Test]
		public void DeleteAll ()
		{
			var db = CreateDb ();

			var r = db.DeleteAll<TestTable> ();

			Assert.AreEqual (Count, r);
			Assert.AreEqual (0, db.Table<TestTable> ().Count ());
		}

		[Test]
		public void DeleteEntityOneWithOnDeleteCascade()
		{
			var db = CreateDbWithOne2Many();

			var obj = db.Get<TestObjWithOne2Many>(1);
			var r = db.Delete(obj);
			var childObjects = db.Table<TestDependentObj>().Where(x => x.OwnerId == 1).ToList();

			Assert.AreEqual(1, r);
			Assert.AreEqual(parentCount - 1, db.Table<TestObjWithOne2Many>().Count());
			Assert.AreEqual(0,childObjects.Count());
		}

		[Test]
		public void DeleteAllWithOnDeleteCascade()
		{
			var db = CreateDbWithOne2Many();

			var r = db.DeleteAll<TestObjWithOne2Many>();

			var childObjects = db.Table<TestDependentObj>().ToList();

			Assert.AreEqual(parentCount, r);
			Assert.AreEqual(0, db.Table<TestObjWithOne2Many>().Count());
			Assert.AreEqual(0,childObjects.Count());
		}

		[Test]
		public void DeleteWhere()
		{
			var db = CreateDb ();

			int deleted = db.Table<TestTable>().DeleteWhere(table => table.Datum < 1050);
			Assert.AreEqual(deleted, 50);

			int remaining = db.Table<TestTable>().Count();
			Assert.AreEqual(remaining, 50);
		}
	}
}

