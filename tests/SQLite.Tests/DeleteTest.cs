using System;
using System.Linq;

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
			public string Test { get; set;}
		}

		const int Count = 100;

		SQLiteConnection CreateDb ()
		{
			var db = new TestDb ();
			db.CreateTable<TestTable> ();
			var items = from i in Enumerable.Range (0, Count)
			            select new TestTable { Datum = 1000+i, Test = "Hello World" };
			db.InsertAll (items);
			Assert.AreEqual (Count, db.Table<TestTable> ().Count ());
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
		public void DeleteWithPredicate()
		{
			var db = CreateDb();

			var r = db.Table<TestTable>().Delete (p => p.Test == "Hello World");

			Assert.AreEqual (Count, r);
			Assert.AreEqual (0, db.Table<TestTable> ().Count ());
		}

		[Test]
		public void DeleteWithPredicateHalf()
		{
			var db = CreateDb();
			db.Insert(new TestTable() { Datum = 1, Test = "Hello World 2" });

			var r = db.Table<TestTable>().Delete (p => p.Test == "Hello World");

			Assert.AreEqual (Count, r);
			Assert.AreEqual (1, db.Table<TestTable> ().Count ());
		}

		[Test]
		public void DeleteWithWherePredicate ()
		{
			var db = CreateDb ();

			var r = db.Table<TestTable> ().Where (p => p.Test == "Hello World").Delete ();

			Assert.AreEqual (Count, r);
			Assert.AreEqual (0, db.Table<TestTable> ().Count ());
		}

		[Test]
		public void DeleteWithoutPredicate ()
		{
			var db = CreateDb ();

			try {
				var r = db.Table<TestTable> ().Delete ();
				Assert.Fail ();
			}
			catch (InvalidOperationException) {
			}
		}

		[Test]
		public void DeleteWithTake ()
		{
			var db = CreateDb ();

			try {
				var r = db.Table<TestTable> ().Where (p => p.Test == "Hello World").Take (2).Delete ();
				Assert.Fail ();
			}
			catch (InvalidOperationException) {
			}
		}

		[Test]
		public void DeleteWithSkip ()
		{
			var db = CreateDb ();

			try {
				var r = db.Table<TestTable> ().Where (p => p.Test == "Hello World").Skip (2).Delete ();
				Assert.Fail ();
			}
			catch (InvalidOperationException) {
			}
		}
	}
}

