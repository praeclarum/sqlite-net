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
	public class FastColumnSetterTest
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
		public void SetFastColumnSetters_AndReadData()
		{
			FastColumnSetter.RegisterFastColumnSetter(
				typeof(TestSetter),
				nameof(TestSetter.Id),
				(obj, stmt, index) => { ((TestSetter)obj).Id = SQLite3.ColumnInt(stmt, index); });

			FastColumnSetter.RegisterFastColumnSetter (
				typeof (TestSetter),
				nameof (TestSetter.Data),
				(obj, stmt, index) => { ((TestSetter)obj).Data = SQLite3.ColumnString (stmt, index); });

			FastColumnSetter.RegisterFastColumnSetter (
				typeof (TestSetter),
				nameof (TestSetter.Date),
				(obj, stmt, index) => { ((TestSetter)obj).Date = new DateTime (SQLite3.ColumnInt64 (stmt, index)); });

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
			int callCount = 0;

			FastColumnSetter.RegisterFastColumnSetter (
				typeof (TestSetter),
				nameof (TestSetter.Id),
				(obj, stmt, index) => {
					((TestSetter)obj).Id = SQLite3.ColumnInt (stmt, index);
					callCount++;
				});

			FastColumnSetter.RegisterFastColumnSetter (
				typeof (TestSetter),
				nameof (TestSetter.Data),
				(obj, stmt, index) => {
					((TestSetter)obj).Data = SQLite3.ColumnString (stmt, index);
					callCount++;
				});

			FastColumnSetter.RegisterFastColumnSetter (
				typeof (TestSetter),
				nameof (TestSetter.Date),
				(obj, stmt, index) => {
					((TestSetter)obj).Date = new DateTime (SQLite3.ColumnInt64 (stmt, index));
					callCount++;
				});

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
