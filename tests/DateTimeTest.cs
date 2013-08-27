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
	public class DateTimeTest
	{
		class TestObj
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }

			public string Name { get; set; }
			public DateTime ModifiedTime { get; set; }
		}


		[Test]
		public void AsTicks ()
		{
			var db = new TestDb (storeDateTimeAsTicks: true);
			TestDateTime (db);
		}

		[Test]
		public void AsStrings ()
		{
			var db = new TestDb (storeDateTimeAsTicks: false);			
			TestDateTime (db);
		}

		[Test]
		public void AsyncAsTicks ()
		{
			var db = new SQLiteAsyncConnection (TestPath.GetTempFileName (), true);
			TestAsyncDateTime (db);
		}

		[Test]
		public void AsyncAsString ()
		{
			var db = new SQLiteAsyncConnection (TestPath.GetTempFileName (), false);
			TestAsyncDateTime (db);
		}

		void TestAsyncDateTime (SQLiteAsyncConnection db)
		{
			db.CreateTableAsync<TestObj> ().Wait ();

			TestObj o, o2;

			//
			// Ticks
			//
			o = new TestObj {
				ModifiedTime = new DateTime (2012, 1, 14, 3, 2, 1),
			};
			db.InsertAsync (o).Wait ();
			o2 = db.GetAsync<TestObj> (o.Id).Result;
			Assert.AreEqual (o.ModifiedTime, o2.ModifiedTime);
		}

		void TestDateTime (TestDb db)
		{
			db.CreateTable<TestObj> ();

			TestObj o, o2;

			//
			// Ticks
			//
			o = new TestObj {
				ModifiedTime = new DateTime (2012, 1, 14, 3, 2, 1),
			};
			db.Insert (o);
			o2 = db.Get<TestObj> (o.Id);
			Assert.AreEqual (o.ModifiedTime, o2.ModifiedTime);
		}
	}
}

