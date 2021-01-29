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
		private readonly SQLiteConnection _db = new SQLiteConnection (Path.GetTempFileName(), true);
		private readonly (int Value, double Walue)[] _records = new[]
		{
			(42, 0.5)
		};

		public QueryTest ()
		{
			_db.Execute ("create table G(Value integer not null, Walue real not null)");

			for (int i = 0; i < _records.Length; i++) {
				_db.Execute ("insert into G(Value, Walue) values (?, ?)",
					_records[i].Value, _records[i].Walue);
			}
		}

		class GenericObject
		{
			public int Value { get; set; }
			public double Walue { get; set; }
		}

		[Test]
		public void QueryGenericObject ()
		{
			var r = _db.Query<GenericObject> ("select * from G");

			Assert.AreEqual (_records.Length, r.Count);
			Assert.AreEqual (_records[0].Value, r[0].Value);
			Assert.AreEqual (_records[0].Walue, r[0].Walue);
		}

		#region Issue #1007

		[Test]
		public void QueryValueTuple()
		{
			var r = _db.Query<(int Value, double Walue)> ("select * from G");

			Assert.AreEqual(_records.Length, r.Count);
			Assert.AreEqual(_records[0].Value, r[0].Value);
			Assert.AreEqual(_records[0].Walue, r[0].Walue);
		}

		#endregion
	}
}
