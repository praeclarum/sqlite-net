using System;
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

			Assert.AreEqual ("Id", mapping.Columns[0].Name);
			Assert.AreEqual ("AGoodColumnName", mapping.Columns[1].Name);
		}

		class OverrideNamesBase
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }

			public virtual string Name { get; set; }
			public virtual string Value { get; set; }
		}

		class OverrideNamesClass : OverrideNamesBase
		{
			[Column ("n")]
			public override string Name { get; set; }
			[Column ("v")]
			public override string Value { get; set; }
		}

		[Test]
		public void OverrideNames ()
		{
			var db = new TestDb ();
			db.CreateTable<OverrideNamesClass> ();

			var cols = db.GetTableInfo ("OverrideNamesClass");
			Assert.AreEqual (3, cols.Count);
			Assert.IsTrue (cols.Exists (x => x.Name == "n"));
			Assert.IsTrue (cols.Exists (x => x.Name == "v"));

			var o = new OverrideNamesClass {
				Name = "Foo",
				Value = "Bar",
			};

			db.Insert (o);

			var oo = db.Table<OverrideNamesClass> ().First ();

			Assert.AreEqual ("Foo", oo.Name);
			Assert.AreEqual ("Bar", oo.Value);
		}

		#region Issue #86

		[Table ("foo")]
		public class Foo
		{
			[Column ("baz")]
			public int Bar { get; set; }
		}

		[Test]
		public void Issue86 ()
		{
			var db = new TestDb ();
			db.CreateTable<Foo> ();

			db.Insert (new Foo { Bar = 42 });
			db.Insert (new Foo { Bar = 69 });

			var found42 = db.Table<Foo> ().Where (f => f.Bar == 42).FirstOrDefault ();
			Assert.IsNotNull (found42);

			var ordered = new List<Foo> (db.Table<Foo> ().OrderByDescending (f => f.Bar));
			Assert.AreEqual (2, ordered.Count);
			Assert.AreEqual (69, ordered[0].Bar);
			Assert.AreEqual (42, ordered[1].Bar);
		}

		#endregion

		#region Issue #572

		public class OnlyKeyModel
		{
			[PrimaryKey]
			public string MyModelId { get; set; }
		}

		[Test]
		public void OnlyKey ()
		{
			var db = new TestDb ();
			db.CreateTable<OnlyKeyModel> ();

			db.InsertOrReplace (new OnlyKeyModel { MyModelId = "Foo" });
			var foo = db.Get<OnlyKeyModel> ("Foo");
			Assert.AreEqual (foo.MyModelId, "Foo");

			db.Insert (new OnlyKeyModel { MyModelId = "Bar" });
			var bar = db.Get<OnlyKeyModel> ("Bar");
			Assert.AreEqual (bar.MyModelId, "Bar");

			db.Update (new OnlyKeyModel { MyModelId = "Foo" });
			var foo2 = db.Get<OnlyKeyModel> ("Foo");
			Assert.AreEqual (foo2.MyModelId, "Foo");
		}

		#endregion
	}
}

