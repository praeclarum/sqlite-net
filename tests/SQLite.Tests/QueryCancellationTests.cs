using System.Linq;
using System.Text;
using SQLite;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
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
	public class QueryCancellationTests
	{
		private readonly SQLiteConnection _db = new SQLiteConnection (Path.GetTempFileName (), true);
		private readonly (int Value, double Walue)[] _records = new[]
		{
			(42, 0.5),
		};

		public QueryCancellationTests ()
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
		public void CanCancelScalarQuery ()
		{
			var cancellationSource = new CancellationTokenSource ();
			cancellationSource.Cancel ();

			Assert.Throws<OperationCanceledException> (() => {
				var r = _db.QueryScalarsWithCancellation<int> ("select Value from G", cancellationSource.Token);
			});
		}

		[Test]
		public void CanCancelQuery ()
		{
			var cancellationSource = new CancellationTokenSource ();
			cancellationSource.Cancel ();

			Assert.Throws<OperationCanceledException> (() => {
				var r = _db.QueryScalarsWithCancellation<GenericObject> ("select * from G", cancellationSource.Token);
			});
		}
	}
}
