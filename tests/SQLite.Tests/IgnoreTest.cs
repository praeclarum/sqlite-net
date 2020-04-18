using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TearDown = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestCleanupAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{    
	[TestFixture]
	public class IgnoreTest
	{
		public class TestObj
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }

			public string Text { get; set; }

			[SQLite.Ignore]
			public Dictionary<int, string> Edibles
			{ 
				get { return this._edibles; }
				set { this._edibles = value; }
			} protected Dictionary<int, string> _edibles = new Dictionary<int, string>();

			[SQLite.Ignore]
			public string IgnoredText { get; set; }

			public override string ToString ()
			{
				return string.Format("[TestObj: Id={0}]", Id);
			}
		}

		[Test]
		public void MappingIgnoreColumn ()
		{
			var db = new TestDb ();
			var m = db.GetMapping<TestObj> ();

			Assert.AreEqual (2, m.Columns.Length);
		}

		[Test]
		public void CreateTableSucceeds ()
		{
			var db = new TestDb ();
			db.CreateTable<TestObj> ();
		}

		[Test]
		public void InsertSucceeds ()
		{
			var db = new TestDb ();
			db.CreateTable<TestObj> ();

			var o = new TestObj {
				Text = "Hello",
				IgnoredText = "World",
			};

			db.Insert (o);

			Assert.AreEqual (1, o.Id);
		}

		[Test]
		public void GetDoesntHaveIgnores ()
		{
			var db = new TestDb ();
			db.CreateTable<TestObj> ();

			var o = new TestObj {
				Text = "Hello",
				IgnoredText = "World",
			};

			db.Insert (o);

			var oo = db.Get<TestObj> (o.Id);

			Assert.AreEqual ("Hello", oo.Text);
			Assert.AreEqual (null, oo.IgnoredText);
		}

		public class BaseClass
		{
			[Ignore]
			public string ToIgnore {
				get;
				set;
			}
		}

		public class TableClass : BaseClass
		{
			public string Name { get; set; }
		}

		[Test]
		public void BaseIgnores ()
		{
			var db = new TestDb ();
			db.CreateTable<TableClass> ();

			var o = new TableClass {
				ToIgnore = "Hello",
				Name = "World",
			};

			db.Insert (o);

			var oo = db.Table<TableClass> ().First ();

			Assert.AreEqual (null, oo.ToIgnore);
			Assert.AreEqual ("World", oo.Name);
		}

		public class RedefinedBaseClass
		{
			public string Name { get; set; }
			public List<string> Values { get; set; }
		}

		public class RedefinedClass : RedefinedBaseClass
		{
			[Ignore]
			public new List<string> Values { get; set; }
			public string Value { get; set; }
		}

		[Test]
		public void RedefinedIgnores ()
		{
			var db = new TestDb ();
			db.CreateTable<RedefinedClass> ();

			var o = new RedefinedClass {
				Name = "Foo",
				Value = "Bar",
				Values = new List<string> { "hello", "world" },
			};

			db.Insert (o);

			var oo = db.Table<RedefinedClass> ().First ();

			Assert.AreEqual ("Foo", oo.Name);
			Assert.AreEqual ("Bar", oo.Value);
			Assert.AreEqual (null, oo.Values);
		}

		[AttributeUsage (AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
		class DerivedIgnoreAttribute : IgnoreAttribute
		{
		}

		class DerivedIgnoreClass
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }

			public string NotIgnored { get; set; }

			[DerivedIgnore]
			public string Ignored { get; set; }
		}

		[Test]
		public void DerivedIgnore ()
		{
			var db = new TestDb ();
			db.CreateTable<DerivedIgnoreClass> ();

			var o = new DerivedIgnoreClass {
				Ignored = "Hello",
				NotIgnored = "World",
			};

			db.Insert (o);

			var oo = db.Table<DerivedIgnoreClass> ().First ();

			Assert.AreEqual (null, oo.Ignored);
			Assert.AreEqual ("World", oo.NotIgnored);
		}
	}
}
