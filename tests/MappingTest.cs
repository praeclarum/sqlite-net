using System;

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

		#region Issue #86

		[Table("foo")]
		public class Foo
		{
		    [Column("baz")]
		    public int Bar { get; set; }
		}

		[Test]
		public void Issue86 ()
		{
			var db = new TestDb ();
			db.CreateTable<Foo> ();

			db.Insert (new Foo { Bar = 42 } );
			db.Insert (new Foo { Bar = 69 } );

			var found42 = db.Table<Foo> ().Where (f => f.Bar == 42).FirstOrDefault();
			Assert.IsNotNull (found42);
		}

		#endregion
	}
}

