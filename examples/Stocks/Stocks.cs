using System;
using System.Collections.Generic;
using System.Linq;
using Path = System.IO.Path;

using SQLite;
using System.Globalization;

namespace Stocks
{
	public class Stock
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }
		[MaxLength(8)]
		public string Symbol { get; set; }

		public override string ToString ()
		{
			return Symbol;
		}
	}

	public class Valuation
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }
		[Indexed]
		public int StockId { get; set; }
		public DateTime Time { get; set; }
		public decimal Price { get; set; }

		public override string ToString ()
		{
			return string.Format ("{0:MMM dd yy}    {1:C}", Time, Price);
		}
	}

	public class Database : SQLiteConnection
	{
		public Database (string path) : base(path)
		{
			CreateTable<Stock> ();
			CreateTable<Valuation> ();
		}
		public IEnumerable<Valuation> QueryValuations (Stock stock)
		{
			return Table<Valuation> ().Where(x => x.StockId == stock.Id);
		}
		public Valuation QueryLatestValuation (Stock stock)
		{
			return Table<Valuation> ().Where(x => x.StockId == stock.Id).OrderByDescending(x => x.Time).Take(1).FirstOrDefault();
		}
		public Stock QueryStock (string stockSymbol)
		{
			return	(from s in Table<Stock> ()
					where s.Symbol == stockSymbol
					select s).FirstOrDefault ();
		}
		public IEnumerable<Stock> QueryAllStocks ()
		{
			return	from s in Table<Stock> ()
					orderby s.Symbol
					select s;
		}

		public void UpdateStock (string stockSymbol)
		{
			//
			// Ensure that there is a valid Stock in the DB
			//
			var stock = QueryStock (stockSymbol);
			if (stock == null) {
				stock = new Stock { Symbol = stockSymbol };
				Insert (stock);
			}
			
			//
			// When was it last valued?
			//
			var latest = QueryLatestValuation (stock);
			var latestDate = latest != null ? latest.Time : new DateTime (1950, 1, 1);
			
			//
			// Get the latest valuations
			//
			try {
				var newVals = new YahooScraper ().GetValuations (stock, latestDate + TimeSpan.FromHours (23), DateTime.Now);
				InsertAll (newVals);
			} catch (System.Net.WebException ex) {
				Console.WriteLine (ex);
			}
		}
	}

	public class YahooScraper
	{
		public IEnumerable<Valuation> GetValuations (Stock stock, DateTime start, DateTime end)
		{
			var t = "http://ichart.finance.yahoo.com/table.csv?s={0}&d={1}&e={2}&f={3}&g=d&a={4}&b={5}&c={6}&ignore=.csv";
			var url = string.Format (t, stock.Symbol, end.Month - 1, end.Day, end.Year, start.Month - 1, start.Day, start.Year);
			Console.WriteLine ("GET {0}", url);
			var req = System.Net.WebRequest.Create (url);
			using (var resp = new System.IO.StreamReader (req.GetResponse ().GetResponseStream ())) {
				var first = true;
				var dateCol = 0;
				var priceCol = 6;
				for (var line = resp.ReadLine (); line != null; line = resp.ReadLine ()) {
					var parts = line.Split (',');
					if (first) {
						dateCol = Array.IndexOf (parts, "Date");
						priceCol = Array.IndexOf (parts, "Adj Close");
						first = false;
					} else {
						yield return new Valuation {
							StockId = stock.Id,
							Price = decimal.Parse (parts[priceCol], CultureInfo.InvariantCulture),
							Time = DateTime.Parse (parts[dateCol])
						};
					}
				}
			}
		}
	}
}
