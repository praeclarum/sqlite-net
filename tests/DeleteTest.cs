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
		}

		const int Count = 100;

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
	}
}

