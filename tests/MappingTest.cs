using System;
using NUnit.Framework;

namespace SQLite.Tests
{
	[TestFixture]
	public class MappingTest
	{
		[Table ("AGoodTableName")]
		class AFunnyTableName
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column ("AGoodColumnName")]
			public string AFunnyColumnName { get; set; }
		}


		[Test]
		public void HasGoodNames ()
		{
			var db = new TestDb ();
			
			db.CreateTable<AFunnyTableName> ();

			var mapping = db.GetMapping<AFunnyTableName> ();

			Assert.AreEqual ("AGoodTableName", mapping.TableName);

			Assert.AreEqual ("Id", mapping.Columns [0].Name);
			Assert.AreEqual ("AGoodColumnName", mapping.Columns [1].Name);
		}
	}
}

