using System.Linq;
using System.Text;
using SQLite;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

using System.IO;

namespace SQLite.Tests
{
	[TestFixture]
	public class MigrationTest
	{
		[Table ("Test")]
		class LowerId {
			public int Id { get; set; }
		}

		[Table ("Test")]
		class UpperId {
			public int ID { get; set; }
		}

		[Test]
		public void UpperAndLowerColumnNames ()
		{
			using (var db = new TestDb (true) { Trace = true } ) {
				db.CreateTable<LowerId> ();
				db.CreateTable<UpperId> ();

				var cols = db.GetTableInfo ("Test").ToList ();
				Assert.AreEqual (1, cols.Count);
				Assert.AreEqual ("Id", cols[0].Name);
			}
		}

		[Table ("TestAdd")]
		class TestAddBefore
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }

			public string Name { get; set; }
		}

		[Table ("TestAdd")]
		class TestAddAfter
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }

			public string Name { get; set; }

			public int IntValue { get; set; }
			public string StringValue { get; set; }
		}

		[Test]
		public void AddColumns ()
		{
			//
			// Init the DB
			//
			var path = "";
			using (var db = new TestDb (true) { Trace = true }) {
				path = db.DatabasePath;

				db.CreateTable<TestAddBefore> ();

				var cols = db.GetTableInfo ("TestAdd");
				Assert.AreEqual (2, cols.Count);

				var o = new TestAddBefore {
					Name = "Foo",
				};

				db.Insert (o);

				var oo = db.Table<TestAddBefore> ().First ();

				Assert.AreEqual ("Foo", oo.Name);
			}

			//
			// Migrate and use it
			//
			using (var db = new SQLiteConnection (path, true) { Trace = true }) {

				db.CreateTable<TestAddAfter> ();

				var cols = db.GetTableInfo ("TestAdd");
				Assert.AreEqual (4, cols.Count);

				var oo = db.Table<TestAddAfter> ().First ();

				Assert.AreEqual ("Foo", oo.Name);
				Assert.AreEqual (0, oo.IntValue);
				Assert.AreEqual (null, oo.StringValue);

				var o = new TestAddAfter {
					Name = "Bar",
					IntValue = 42,
					StringValue = "Hello",
				};
				db.Insert (o);

				var ooo = db.Get<TestAddAfter> (o.Id);
				Assert.AreEqual ("Bar", ooo.Name);
				Assert.AreEqual (42, ooo.IntValue);
				Assert.AreEqual ("Hello", ooo.StringValue);
			}
		}
	}
}
