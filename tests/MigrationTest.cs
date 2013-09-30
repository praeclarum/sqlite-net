using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
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
				Assert.That (cols.Count, Is.EqualTo (1));
				Assert.That (cols[0].Name, Is.EqualTo ("Id"));
			}
		}
	}
}
