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
	public class DbCommandTest
	{
		[Test]
		public void QueryCommand()
		{
			var db = new SQLiteConnection (Path.GetTempFileName(), true);
			db.CreateTable<Product>();
			var b = new Product();
			db.Insert(b);

			var test = db.CreateCommand("select * from Product")
				.ExecuteDeferredQuery<Product>(new TableMapping(typeof(Product))).ToList();


			Assert.AreEqual (test.Count, 1);
		}

		#region Issue #1048

		[Test]
		public void QueryCommandCastToObject()
		{
			var db = new SQLiteConnection (Path.GetTempFileName(), true);
			db.CreateTable<Product>();
			var b = new Product();
			db.Insert(b);

			var test = db.CreateCommand("select * from Product")
				.ExecuteDeferredQuery<object>(new TableMapping(typeof(Product))).ToList();


			Assert.AreEqual (test.Count, 1);
		}

		#endregion
	}
}
