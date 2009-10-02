using System;
using System.Collections.Generic;
using System.Linq;
using Path = System.IO.Path;

using SQLite;

namespace Stocks
{
	class Valuation
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }
		[Indexed]
		public int StockId { get; set; }
		public DateTime Time { get; set; }
		public decimal Price { get; set; }
		
		public override string ToString ()
		{
			return string.Format("{0:MMM dd yy}    {1:C}", Time, Price);
		}

	}

	class Stock
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

	class Database : SQLiteConnection
	{
		public Database (string path) : base(path)
		{
		}
		public IEnumerable<Valuation> QueryValuations (Stock stock)
		{
			return Query<Valuation> ("select * from Valuation where StockId = ?", stock.Id);
		}
		public Valuation QueryLatestValuation (Stock stock)
		{
			return Query<Valuation> ("select * from Valuation where StockId = ? order by Time desc limit 1", stock.Id).FirstOrDefault ();
		}
		public Stock QueryStock (string stockSymbol)
		{
			return Query<Stock> ("select * from Stock where Symbol = ?", stockSymbol).FirstOrDefault ();
		}
		public IEnumerable<Stock> QueryAllStocks ()
		{
			return Query<Stock> ("select * from Stock order by Symbol");
		}
	}


	class YahooScraper
	{
		public IEnumerable<Valuation> GetValuations (Stock stock, DateTime start, DateTime end)
		{
			var t = "http://ichart.finance.yahoo.com/table.csv?s={0}&d={1}&e={2}&f={3}&g=d&a={4}&b={5}&c={6}&ignore=.csv";
			var url = string.Format (t, stock.Symbol, end.Month - 1, end.Day, end.Year, start.Month - 1, start.Day, start.Year);
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
							Price = decimal.Parse (parts[priceCol]),
							Time = DateTime.Parse (parts[dateCol])
						};
					}
				}
			}
		}
	}
}
