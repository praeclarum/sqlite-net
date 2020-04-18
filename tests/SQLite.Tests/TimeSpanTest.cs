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
			TestTimeSpan (db);
		}

		[Test]
		public void AsStrings ()
		{
			var db = new TestDb (TimeSpanAsTicks (false));
			TestTimeSpan (db);
		}

		[Test]
		public void AsyncAsTicks ()
		{
			var db = new SQLiteAsyncConnection (TimeSpanAsTicks (true));
			TestAsyncTimeSpan (db);
		}

		[Test]
		public void AsyncAsStrings ()
		{
			var db = new SQLiteAsyncConnection (TimeSpanAsTicks (false));
			TestAsyncTimeSpan (db);
		}

		SQLiteConnectionString TimeSpanAsTicks (bool asTicks = true) => new SQLiteConnectionString (TestPath.GetTempFileName (), SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite, true, storeTimeSpanAsTicks: asTicks);

		void TestAsyncTimeSpan (SQLiteAsyncConnection db)
		{
			db.CreateTableAsync<TestObj> ().Wait ();

			TestObj o, o2;

			o = new TestObj {
				Duration = new TimeSpan (42, 12, 33, 20, 501),
			};
			db.InsertAsync (o).Wait ();
			o2 = db.GetAsync<TestObj> (o.Id).Result;
			Assert.AreEqual (o.Duration, o2.Duration);
		}

		void TestTimeSpan (TestDb db)
		{
			db.CreateTable<TestObj> ();

			TestObj o, o2;

			o = new TestObj {
				Duration = new TimeSpan (42, 12, 33, 20, 501),
			};
			db.Insert (o);
			o2 = db.Get<TestObj> (o.Id);
			Assert.AreEqual (o.Duration, o2.Duration);
		}
	}
}
