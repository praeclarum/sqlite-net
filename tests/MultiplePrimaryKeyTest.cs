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
	public class MultiplePrimaryKeyTest
	{
		[Table("MultiplePrimaryKeysTable")]
		class MultiplePrimaryKeys
		{
			[PrimaryKey]
			public int Id { get; set; }

			[PrimaryKey]
			public string Id2 { get; set; }
		}

		[Test]
		public void CreateTableWithMultiplePrimaryKeys()
		{
			var db = new TestDb();
			db.CreateTable<MultiplePrimaryKeys>();

			MultiplePrimaryKeys[] insertRows = new MultiplePrimaryKeys[]
			{
				new MultiplePrimaryKeys() { Id = 1, Id2 = "Foo" },
				new MultiplePrimaryKeys() { Id = 2, Id2 = "Bar" },
			};

			db.InsertAll(insertRows);

			var queryRows = (from r in db.Table<MultiplePrimaryKeys>() select r).ToArray();
			Assert.AreEqual(insertRows.Length, queryRows.Length);

			for (int i = 0; i < insertRows.Length; i ++)
			{
				Assert.AreEqual(insertRows[i].Id, queryRows[i].Id);
				Assert.AreEqual(insertRows[i].Id2, queryRows[i].Id2);
			}
		}
	}
}
