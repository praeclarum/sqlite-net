using System;
using System.Linq;
using Newtonsoft;
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
	public class OuterTestSetter
	{
		[AutoIncrement, PrimaryKey]
		public int Id { get; set; }

		public string Data { get; set; }

		public DateTime Date { get; set; }
	}

	public class OuterTestDb : SQLiteConnection
	{
		public OuterTestDb (String path)
			: base (path)
		{
			CreateTable<OuterTestSetter> ();
		}
	}

	[TestFixture]
	public class SourceGeneratorTest
	{
		public class InnerTestSetter
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }

			public string Data { get; set; }

			public DateTime Date { get; set; }
		}

		// Shouldn't generate Setter because it is not accessible
		private class PrivateInnerTestSetter
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }

			public string Data { get; set; }

			public DateTime Date { get; set; }
		}

		public class InnerTestDb : SQLiteConnection
		{
			public InnerTestDb (String path)
				: base (path)
			{
				CreateTable<InnerTestSetter> ();
			}
		}

		[Test]
		public void SqliteInitializer_PrivateInnerTestSetter ()
		{
			SQLiteInitializer.Init ();

			if (!SQLite.FastColumnSetter.customSetter.TryGetValue((typeof(PrivateInnerTestSetter), nameof(PrivateInnerTestSetter.Id)), out var setter))
			{
				Assert.IsTrue(true, "Should not be registered");
			}
			else
			{
				Assert.Fail("Should not be registered");
			}
		}

		[Test]
		public void SqliteInitializer_InnerTestSetter ()
		{
			SQLiteInitializer.Init ();

			if (SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (InnerTestSetter), nameof (InnerTestSetter.Id)), out var setter)) {
				Assert.IsTrue (true, "Should be registered");
			}
			else {
				Assert.Fail ("Should be registered");
			}
		}

		[Test]
		public void SqliteInitializer_OuterTestSetter ()
		{
			SQLiteInitializer.Init ();

			if (SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (OuterTestSetter), nameof (OuterTestSetter.Id)), out var setter)) {
				Assert.IsTrue (true, "Should be registered");
			}
			else {
				Assert.Fail ("Should be registered");
			}
		}

		[Test]
		public void SqliteInitializer_Inner_AndReadData()
		{
			SQLiteInitializer.Init();

			var n = 20;
			var cq = from i in Enumerable.Range (1, n)
					 select new InnerTestSetter {
						 Data = Convert.ToString (i),
						 Date = new DateTime (2013, 1, i)
					 };

			var db = new InnerTestDb (TestPath.GetTempFileName ());
			db.InsertAll (cq);

			var results = db.Table<InnerTestSetter> ().Where (o => o.Data.Equals ("10"));
			Assert.AreEqual (results.Count (), 1);
			Assert.AreEqual (results.FirstOrDefault ().Data, "10");
		}

		[Test]
		public void SetFastColumnSetters_Inner_AndReadData_IsCalled()
		{
			SQLiteInitializer.Init ();

			int callCount = 0;

			var n = 20;
			var cq = from i in Enumerable.Range (1, n)
				select new InnerTestSetter {
					Data = Convert.ToString (i),
					Date = new DateTime (2013, 1, i)
				};

			var db = new InnerTestDb (TestPath.GetTempFileName ());
			db.InsertAll (cq);

			var results = db.Table<InnerTestSetter> ().Where (o => o.Data.Equals ("10"));
			Assert.AreEqual (results.Count (), 1);
			Assert.AreEqual (results.FirstOrDefault ().Data, "10");

			Assert.IsTrue(callCount > 0);
		}

		[Test]
		public void SqliteInitializer_Outer_AndReadData ()
		{
			SQLiteInitializer.Init ();

			var n = 20;
			var cq = from i in Enumerable.Range (1, n)
				select new OuterTestSetter() {
					Data = Convert.ToString (i),
					Date = new DateTime (2013, 1, i)
				};

			var db = new OuterTestDb(TestPath.GetTempFileName ());
			db.InsertAll (cq);

			var results = db.Table<InnerTestSetter> ().Where (o => o.Data.Equals ("10"));
			Assert.AreEqual (results.Count (), 1);
			Assert.AreEqual (results.FirstOrDefault ().Data, "10");
		}

		[Test]
		public void SetFastColumnSetters_Outer_AndReadData_IsCalled ()
		{
			SQLiteInitializer.Init ();

			int callCount = 0;

			var n = 20;
			var cq = from i in Enumerable.Range (1, n)
				select new OuterTestSetter {
					Data = Convert.ToString (i),
					Date = new DateTime (2013, 1, i)
				};

			var db = new OuterTestDb (TestPath.GetTempFileName ());
			db.InsertAll (cq);

			var results = db.Table<InnerTestSetter> ().Where (o => o.Data.Equals ("10"));
			Assert.AreEqual (results.Count (), 1);
			Assert.AreEqual (results.FirstOrDefault ().Data, "10");

			Assert.IsTrue (callCount > 0);
		}
	}
}
