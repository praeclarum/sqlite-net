using System;
using System.Linq;
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
	public class SourceGeneratorTest
	{
		public class TestSetter
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }

			public string Data { get; set; }

			public DateTime Date { get; set; }
		}

		public class TestDb : SQLiteConnection
		{
			public TestDb (String path)
				: base (path)
			{
				CreateTable<TestSetter> ();
			}
		}

		[Test]
		public void SqliteInitializer_AndReadData()
		{
			//SQLiteInitializer.Init();

			var n = 20;
			var cq = from i in Enumerable.Range (1, n)
					 select new TestSetter {
						 Data = Convert.ToString (i),
						 Date = new DateTime (2013, 1, i)
					 };

			var db = new TestDb (TestPath.GetTempFileName ());
			db.InsertAll (cq);

			var results = db.Table<TestSetter> ().Where (o => o.Data.Equals ("10"));
			Assert.AreEqual (results.Count (), 1);
			Assert.AreEqual (results.FirstOrDefault ().Data, "10");
		}

		[Test]
		public void SetFastColumnSetters_AndReadData_IsCalled()
		{
			//SQLiteInitializer.Init ();

			int callCount = 0;

			var n = 20;
			var cq = from i in Enumerable.Range (1, n)
				select new TestSetter {
					Data = Convert.ToString (i),
					Date = new DateTime (2013, 1, i)
				};

			var db = new TestDb (TestPath.GetTempFileName ());
			db.InsertAll (cq);

			var results = db.Table<TestSetter> ().Where (o => o.Data.Equals ("10"));
			Assert.AreEqual (results.Count (), 1);
			Assert.AreEqual (results.FirstOrDefault ().Data, "10");

			Assert.IsTrue(callCount > 0);
		}
	}
}
