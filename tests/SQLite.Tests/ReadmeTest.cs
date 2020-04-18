using System;
using System.Linq;
using System.Text;
using SQLite;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

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
	public class ReadmeTest
	{
		public class Stock
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }
			public string Symbol { get; set; }
		}

		public class Valuation
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }
			[Indexed]
			public int StockId { get; set; }
			public DateTime Time { get; set; }
			public decimal Price { get; set; }
		}

		public static void AddStock (SQLiteConnection db, string symbol)
		{
			var stock = new Stock () {
				Symbol = symbol
			};
			db.Insert (stock); // Returns the number of rows added to the table
			Console.WriteLine ("{0} == {1}", stock.Symbol, stock.Id);
		}

		public static IEnumerable<Valuation> QueryValuations (SQLiteConnection db, Stock stock)
		{
			return db.Query<Valuation> ("select * from Valuation where StockId = ?", stock.Id);
		}

		public class Val
		{
			public decimal Money { get; set; }
			public DateTime Date { get; set; }
		}

		public static IEnumerable<Val> QueryVals (SQLiteConnection db, Stock stock)
		{
			return db.Query<Val> ("select \"Price\" as \"Money\", \"Time\" as \"Date\" from Valuation where StockId = ?", stock.Id);
		}

		[Test]
		public void Synchronous ()
		{
			var databasePath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "MyData.db");
			File.Delete (databasePath);

			var db = new SQLiteConnection (databasePath);
			db.CreateTable<Stock> ();
			db.CreateTable<Valuation> ();

			AddStock (db, "A1");
			AddStock (db, "A2");
			AddStock (db, "A3");
			AddStock (db, "B1");
			AddStock (db, "B2");
			AddStock (db, "B3");

			var query = db.Table<Stock> ().Where (v => v.Symbol.StartsWith ("A"));

			foreach (var stock in query)
				Console.WriteLine ("Stock: " + stock.Symbol);

			Assert.AreEqual (3, query.ToList ().Count);

		}

		[Test]
		public async Task Asynchronous ()
		{
			await Task.Delay (1).ConfigureAwait (false);

			// Get an absolute path to the database file
			var databasePath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "MyData.db");
			File.Delete (databasePath);

			var db = new SQLiteAsyncConnection (databasePath);

			await db.CreateTableAsync<Stock> ();

			Console.WriteLine ("Table created!");

			var stock = new Stock () {
				Symbol = "AAPL"
			};

			await db.InsertAsync (stock);

			Console.WriteLine ("New sti ID: {0}", stock.Id);

			var query = db.Table<Stock> ().Where (s => s.Symbol.StartsWith ("A"));

			var result = await query.ToListAsync ();

			foreach (var s in result)
				Console.WriteLine ("Stock: " + s.Symbol);

			Assert.AreEqual (1, result.Count);

			var count = await db.ExecuteScalarAsync<int> ("select count(*) from Stock");

			Console.WriteLine (string.Format ("Found '{0}' stock items.", count));

			Assert.AreEqual (1, count);
		}

		[Test]
		public void Cipher ()
		{
			var databasePath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "MyData.db");
			File.Delete (databasePath);

			var options = new SQLiteConnectionString (databasePath, true, key: "password");
			var encryptedDb = new SQLiteAsyncConnection (options);

			var options2 = new SQLiteConnectionString (databasePath, true,
				key: "password",
				preKeyAction: db => db.Execute("PRAGMA cipher_default_use_hmac = OFF;"),
				postKeyAction: db => db.Execute ("PRAGMA kdf_iter = 128000;"));
			var encryptedDb2 = new SQLiteAsyncConnection (options2);
		}

		[Test]
		public void Manual()
		{
			var db = new SQLiteConnection (":memory:");

			db.Execute ("create table Stock(Symbol varchar(100) not null)");
			db.Execute ("insert into Stock(Symbol) values (?)", "MSFT");
			var stocks = db.Query<Stock> ("select * from Stock");

			Assert.AreEqual (1, stocks.Count);
			Assert.AreEqual ("MSFT", stocks[0].Symbol);
		}
	}
}
