using System.Linq;
using System.Text;
using SQLite;
using System.Threading.Tasks;
using System.IO;

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
	public class QueryTest
	{
		class GenericObject
		{
			public int Value { get; set; }
		}

		[Test]
		public void QueryGenericObject ()
		{
			var path = Path.GetTempFileName ();
			var db = new SQLiteConnection (path, true);

			db.Execute ("create table G(Value integer not null)");
			db.Execute ("insert into G(Value) values (?)", 42);
			var r = db.Query<GenericObject> ("select * from G");

			Assert.AreEqual (1, r.Count);
			Assert.AreEqual (42, r[0].Value);
		}

		[Test]
		public void QueryGenericObjectAsDictionary ()
		{
			var path = Path.GetTempFileName ();
			var db = new SQLiteConnection (path, true);

			db.Execute ("create table G(Value integer not null)");
			db.Execute ("insert into G(Value) values (?)", 42);
			db.Execute ("insert into G(Value) values (?)", 43);
			db.Execute ("insert into G(Value) values (?)", 44);
			var r = db.Query ("select * from G where value > ?", 42);

			Assert.AreEqual (2, r.Count);
			Assert.AreEqual (43, r[0]["Value"]);
			Assert.AreEqual (44, r[1]["Value"]);
		}
	}
}
