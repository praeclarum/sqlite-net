using System.Linq;
using NUnit.Framework;
using SQLite.Net;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
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
	}
}

