using System.Linq;
using System.Text;
using SQLite;
using System.Threading.Tasks;

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
	public class BackupTest
	{
		[Test]
		public async Task BackupOneTable ()
		{
			var pathSrc = Path.GetTempFileName ();
			var pathDest = Path.GetTempFileName ();

			var db = new SQLiteAsyncConnection (pathSrc);
			await db.CreateTableAsync<OrderLine> ().ConfigureAwait (false);
			await db.InsertAsync (new OrderLine { });
			var lines = await db.Table<OrderLine> ().ToListAsync ();
			Assert.AreEqual (1, lines.Count);

			await db.BackupAsync (pathDest);

			var destLen = new FileInfo (pathDest).Length;
			Assert.True (destLen >= 4096);
		}
	}
}
