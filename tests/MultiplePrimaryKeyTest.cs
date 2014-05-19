using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
	[Table("MultiplePrimaryKeysTable")]
	public class MultiplePrimaryKeys
	{
		[PrimaryKey, Indexed]
		public int Id { get; set; }

		[PrimaryKey, NotNull, Indexed]
		public string Id2 { get; set; }

		[NotNull]
		public string Value1 { get; set; }

		public bool Value2 { get; set; }
	}

	[TestFixture]
	public class MultiplePrimaryKeyTest
	{
		[Test]
		public void CreateTableWithMultiplePrimaryKeys()
		{
			var db = new TestDb();
			db.CreateTable<MultiplePrimaryKeys>();

			MultiplePrimaryKeys[] insertRows = new MultiplePrimaryKeys[]
			{
				new MultiplePrimaryKeys() { Id = 1, Id2 = "Foo", Value1 = "One", Value2 = true },
				new MultiplePrimaryKeys() { Id = 2, Id2 = "Bar", Value1 = "Two", Value2 = true },
				new MultiplePrimaryKeys() { Id = 3, Id2 = "Baz", Value1 = "Three", Value2 = true },
			};

			db.InsertAll(insertRows);

			var queryRows = (from r in db.Table<MultiplePrimaryKeys>() select r).ToArray();
			Assert.AreEqual(insertRows.Length, queryRows.Length);

			for (int i = 0; i < insertRows.Length; i ++)
			{
				Assert.AreEqual(insertRows[i].Id, queryRows[i].Id);
				Assert.AreEqual(insertRows[i].Id2, queryRows[i].Id2);
			}

			//
			// Test Updates Async
			//

			TestAsyncDb asyncDb = new TestAsyncDb(db.DatabasePath);
			var query = asyncDb.Table<MultiplePrimaryKeys>();

			Task<List<MultiplePrimaryKeys>> rowsTask = (from row in query where row.Value2 select row).ToListAsync();
			rowsTask.Wait();

			List<MultiplePrimaryKeys> rows = rowsTask.Result;
			Assert.AreEqual(rows.Count, 3);
			Assert.AreEqual(rows[0].Value1, "One");
			Assert.AreEqual(rows[1].Value1, "Two");
			Assert.AreEqual(rows[2].Value1, "Three");

			rows[0].Value1 = "Three";
			rows[0].Value2 = false;
			rows[1].Value1 = "Four";
			rows[1].Value2 = false;
			rows[2].Value1 = "Five";
			rows[2].Value2 = false;

			Task insertTask = asyncDb.UpdateAllAsync(rows);
			insertTask.Wait();
		}
	}
}
