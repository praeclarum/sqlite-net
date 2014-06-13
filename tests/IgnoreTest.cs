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
	}
}
