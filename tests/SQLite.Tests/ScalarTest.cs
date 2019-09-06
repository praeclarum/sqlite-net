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
	public class ScalarTest
	{
		class TestTable
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }
			public int Two { get; set; }
		}

		const int Count = 100;

		SQLiteConnection CreateDb ()
		{
			var db = new TestDb ();
			db.CreateTable<TestTable> ();
			var items = from i in Enumerable.Range (0, Count)
				select new TestTable { Two = 2 };
			db.InsertAll (items);
			Assert.AreEqual (Count, db.Table<TestTable> ().Count ());
			return db;
		}


		[Test]
		public void Int32 ()
		{
			var db = CreateDb ();
			
			var r = db.ExecuteScalar<int> ("SELECT SUM(Two) FROM TestTable");

			Assert.AreEqual (Count * 2, r);
		}

		[Test]
		public void SelectSingleRowValue ()
		{
			var db = CreateDb ();

			var r = db.ExecuteScalar<int> ("SELECT Two FROM TestTable WHERE Id = 1 LIMIT 1");

			Assert.AreEqual (2, r);
		}

		[Test]
		public void SelectNullableSingleRowValue ()
		{
			var db = CreateDb ();

			var r = db.ExecuteScalar<int?> ("SELECT Two FROM TestTable WHERE Id = 1 LIMIT 1");

			Assert.AreEqual (true, r.HasValue);
			Assert.AreEqual (2, r);
		}

		[Test]
		public void SelectNoRowValue ()
		{
			var db = CreateDb ();

			var r = db.ExecuteScalar<int?> ("SELECT Two FROM TestTable WHERE Id = 999");

			Assert.AreEqual (false, r.HasValue);
		}

		[Test]
		public void SelectNullRowValue ()
		{
			var db = CreateDb ();

			var r = db.ExecuteScalar<int?> ("SELECT null AS Unknown FROM TestTable WHERE Id = 1 LIMIT 1");

			Assert.AreEqual (false, r.HasValue);
		}
	}
}

