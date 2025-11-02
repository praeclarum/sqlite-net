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
	public class DateOnlyTest
	{
		const string DefaultSQLiteDateString = "yyyy'-'MM'-'dd";

		class TestObj
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }

			public string Name { get; set; }
			public DateOnly ModifiedDate { get; set; }
		}

		[Test]
		public void AsStrings ()
		{
			var date = new DateOnly (2012, 1, 14);
			var db = new TestDb (storeDateTimeAsTicks: false);
			TestDateOnly (db, date, date.ToString (DefaultSQLiteDateString));
		}

		[TestCase ("o")]
		[TestCase ("MMM'-'dd'-'yyyy")]
		public void AsCustomStrings (string format)
		{
			var dateTime = new DateOnly(2012, 1, 14);
			var db = new TestDb (CustomDateString (format));
			TestDateOnly (db, dateTime, dateTime.ToString (format, System.Globalization.CultureInfo.InvariantCulture));
		}

		[Test]
		public void AsyncAsString ()
		{
			var date = new DateOnly(2012, 1, 14);
			var db = new SQLiteAsyncConnection (TestPath.GetTempFileName (), false);
			TestAsyncDateTime (db, date, date.ToString (DefaultSQLiteDateString));
		}

		[TestCase ("o")]
		[TestCase ("MMM'-'dd'-'yyyy")]
		public void AsyncAsCustomStrings (string format)
		{
			var dateTime = new DateOnly (2012, 1, 14);
			var db = new SQLiteAsyncConnection (CustomDateString (format));
			TestAsyncDateTime (db, dateTime, dateTime.ToString (format,System.Globalization.CultureInfo.InvariantCulture));
		}

		SQLiteConnectionString CustomDateString (string dateTimeFormat) => new SQLiteConnectionString (TestPath.GetTempFileName (), SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite, false, dateStringFormat: dateTimeFormat);

		void TestAsyncDateTime (SQLiteAsyncConnection db, DateOnly dateTime, string expected)
		{
			db.CreateTableAsync<TestObj> ().Wait ();

			TestObj o, o2;

			o = new TestObj {
				ModifiedDate = dateTime,
			};
			db.InsertAsync (o).Wait ();
			o2 = db.GetAsync<TestObj> (o.Id).Result;
			Assert.AreEqual (o.ModifiedDate, o2.ModifiedDate);

			var stored = db.ExecuteScalarAsync<string> ("SELECT ModifiedDate FROM TestObj;").Result;
			Assert.AreEqual (expected, stored);
		}

		void TestDateOnly (TestDb db, DateOnly date, string expected)
		{
			db.CreateTable<TestObj> ();

			TestObj o, o2;

			o = new TestObj {
				ModifiedDate = date,
			};
			db.Insert (o);
			o2 = db.Get<TestObj> (o.Id);
			Assert.AreEqual (o.ModifiedDate, o2.ModifiedDate);

			var stored = db.ExecuteScalar<string> ("SELECT ModifiedDate FROM TestObj;");
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

