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
	public class DateTimeTest
	{
		const string DefaultSQLiteDateTimeString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff";

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
			var dateTime = new DateTime (2012, 1, 14, 3, 2, 1, 234);
			var db = new TestDb (storeDateTimeAsTicks: true);
			TestDateTime (db, dateTime, dateTime.Ticks.ToString ());
		}

		[Test]
		public void AsStrings ()
		{
			var dateTime = new DateTime (2012, 1, 14, 3, 2, 1, 234);
			var db = new TestDb (storeDateTimeAsTicks: false);
			TestDateTime (db, dateTime, dateTime.ToString (DefaultSQLiteDateTimeString));
		}

		[TestCase ("o")]
		[TestCase ("MMM'-'dd'-'yyyy' 'HH':'mm':'ss'.'fffffff")]
		public void AsCustomStrings (string format)
		{
			var dateTime = new DateTime (2012, 1, 14, 3, 2, 1, 234);
			var db = new TestDb (CustomDateTimeString (format));
			TestDateTime (db, dateTime, dateTime.ToString (format, System.Globalization.CultureInfo.InvariantCulture));
		}

		[Test]
		public void AsyncAsTicks ()
		{
			var dateTime = new DateTime (2012, 1, 14, 3, 2, 1, 234);
			var db = new SQLiteAsyncConnection (TestPath.GetTempFileName (), true);
			TestAsyncDateTime (db, dateTime, dateTime.Ticks.ToString ());
		}

		[Test]
		public void AsyncAsString ()
		{
			var dateTime = new DateTime (2012, 1, 14, 3, 2, 1, 234);
			var db = new SQLiteAsyncConnection (TestPath.GetTempFileName (), false);
			TestAsyncDateTime (db, dateTime, dateTime.ToString (DefaultSQLiteDateTimeString));
		}

		[TestCase ("o")]
		[TestCase ("MMM'-'dd'-'yyyy' 'HH':'mm':'ss'.'fffffff")]
		public void AsyncAsCustomStrings (string format)
		{
			var dateTime = new DateTime (2012, 1, 14, 3, 2, 1, 234);
			var db = new SQLiteAsyncConnection (CustomDateTimeString (format));
			TestAsyncDateTime (db, dateTime, dateTime.ToString (format,System.Globalization.CultureInfo.InvariantCulture));
		}

		SQLiteConnectionString CustomDateTimeString (string dateTimeFormat) => new SQLiteConnectionString (TestPath.GetTempFileName (), SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite, false, dateTimeStringFormat: dateTimeFormat);

		void TestAsyncDateTime (SQLiteAsyncConnection db, DateTime dateTime, string expected)
		{
			db.CreateTableAsync<TestObj> ().Wait ();

			TestObj o, o2;

			//
			// Ticks
			//
			o = new TestObj {
				ModifiedTime = dateTime,
			};
			db.InsertAsync (o).Wait ();
			o2 = db.GetAsync<TestObj> (o.Id).Result;
			Assert.AreEqual (o.ModifiedTime, o2.ModifiedTime);

			var stored = db.ExecuteScalarAsync<string> ("SELECT ModifiedTime FROM TestObj;").Result;
			Assert.AreEqual (expected, stored);
		}

		void TestDateTime (TestDb db, DateTime dateTime, string expected)
		{
			db.CreateTable<TestObj> ();

			TestObj o, o2;

			//
			// Ticks
			//
			o = new TestObj {
				ModifiedTime = dateTime,
			};
			db.Insert (o);
			o2 = db.Get<TestObj> (o.Id);
			Assert.AreEqual (o.ModifiedTime, o2.ModifiedTime);

			var stored = db.ExecuteScalar<string> ("SELECT ModifiedTime FROM TestObj;");
			Assert.AreEqual (expected, stored);
		}

		class NullableDateObj
		{
			public DateTime? Time { get; set; }
		}

		[Test]
		public async Task LinqNullable ()
		{
			foreach (var option in new[] { true, false }) {
				var db = new SQLiteAsyncConnection (TestPath.GetTempFileName (), option);
				await db.CreateTableAsync<NullableDateObj> ().ConfigureAwait (false);

				var epochTime = new DateTime (1970, 1, 1);

				await db.InsertAsync (new NullableDateObj { Time = epochTime });
				await db.InsertAsync (new NullableDateObj { Time = new DateTime (1980, 7, 23) });
				await db.InsertAsync (new NullableDateObj { Time = null });
				await db.InsertAsync (new NullableDateObj { Time = new DateTime (2019, 1, 23) });

				var res = await db.Table<NullableDateObj> ().Where (x => x.Time == epochTime).ToListAsync ();
				Assert.AreEqual (1, res.Count);

				res = await db.Table<NullableDateObj> ().Where (x => x.Time > epochTime).ToListAsync ();
				Assert.AreEqual (2, res.Count);
			}
		}
	}
}

