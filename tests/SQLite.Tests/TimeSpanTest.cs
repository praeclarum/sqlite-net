using System;
using System.Threading.Tasks;

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
	public class TimeSpanTest
	{
		class TestObj
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }

			public string Name { get; set; }
			public TimeSpan Duration { get; set; }
		}

		[Test]
		public void AsTicks ()
		{
			var db = new TestDb (TimeSpanAsTicks (true));
			var span = new TimeSpan (42, 12, 33, 20, 501);
			TestTimeSpan (db, span, span.Ticks.ToString ());
		}

		[Test]
		public void AsStrings ()
		{
			var db = new TestDb (TimeSpanAsTicks (false));
			var span = new TimeSpan (42, 12, 33, 20, 501);
			TestTimeSpan (db, span, span.ToString ());
		}

		[Test]
		public void AsyncAsTicks ()
		{
			var db = new SQLiteAsyncConnection (TimeSpanAsTicks (true));
			var span = new TimeSpan (42, 12, 33, 20, 501);
			TestAsyncTimeSpan (db, span, span.Ticks.ToString ());
		}

		[Test]
		public void AsyncAsStrings ()
		{
			var db = new SQLiteAsyncConnection (TimeSpanAsTicks (false));
			var span = new TimeSpan (42, 12, 33, 20, 501);
			TestAsyncTimeSpan (db, span, span.ToString ());
		}

		SQLiteConnectionString TimeSpanAsTicks (bool asTicks = true) => new SQLiteConnectionString (TestPath.GetTempFileName (), SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite, true, storeTimeSpanAsTicks: asTicks);

		void TestAsyncTimeSpan (SQLiteAsyncConnection db, TimeSpan duration, string expected)
		{
			db.CreateTableAsync<TestObj> ().Wait ();

			TestObj o, o2;

			o = new TestObj {
				Duration = duration,
			};
			db.InsertAsync (o).Wait ();
			o2 = db.GetAsync<TestObj> (o.Id).Result;
			Assert.AreEqual (o.Duration, o2.Duration);

			var stored = db.ExecuteScalarAsync<string> ("SELECT Duration FROM TestObj;").Result;
			Assert.AreEqual (expected, stored);
		}

		void TestTimeSpan (TestDb db, TimeSpan duration, string expected)
		{
			db.CreateTable<TestObj> ();

			TestObj o, o2;

			o = new TestObj {
				Duration = duration,
			};
			db.Insert (o);
			o2 = db.Get<TestObj> (o.Id);
			Assert.AreEqual (o.Duration, o2.Duration);

			var stored = db.ExecuteScalar<string> ("SELECT Duration FROM TestObj;");
			Assert.AreEqual (expected, stored);
		}
	}
}
