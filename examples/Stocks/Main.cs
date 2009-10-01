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

	class Program
	{
		public static void Main (string[] args)
		{
			new Program ().Run ();
		}

		Database _db;

		void Initialize ()
		{
			var dbPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "Stocks.db");
			_db = new Database (dbPath);
			_db.CreateTable<Stock> ();
			_db.CreateTable<Valuation> ();
		}

		void DisplayStock (string stockSymbol)
		{
			var stock = _db.QueryStock (stockSymbol);
			
			if (stock == null) {
				Console.WriteLine ("I don't know about {0}", stockSymbol);
				Console.WriteLine ("Run \"up {0}\" to update the stock", stockSymbol);
			} else {
				
				//
				// Display the last 1 week
				//				
				foreach (var v in _db.QueryValuations (stock)) {
					Console.WriteLine ("  {0}", v);
				}
				
			}
		}

		void UpdateStock (string stockSymbol)
		{
			//
			// Ensure that there is a valid Stock in the DB
			//
			var stock = _db.QueryStock (stockSymbol);
			if (stock == null) {
				stock = new Stock { Symbol = stockSymbol };
				_db.Insert (stock);
			}
			
			//
			// When was it last valued?
			//
			var latest = _db.QueryLatestValuation (stock);
			var latestDate = latest != null ? latest.Time : new DateTime (1950, 1, 1);
			
			//
			// Get the latest valuations
			//
			var newVals = new YahooScraper ().GetValuations (stock, latestDate + TimeSpan.FromHours (23), DateTime.Now);
			foreach (var v in newVals) {
				Console.Write(".");
				_db.Insert (v);
			}
			Console.WriteLine();
		}

		void ListStocks ()
		{
			foreach (var stock in _db.QueryAllStocks ()) {
				Console.WriteLine (stock);
			}
		}

		void DisplayBanner ()
		{
			Console.WriteLine ("Stocks - a demo of sqlite-net");
			Console.WriteLine ("Using " + _db.Database);
			Console.WriteLine ();
		}

		void DisplayHelp (string cmd)
		{
			Action<string, string> display = (c, h) => { Console.WriteLine ("{0} {1}", c, h); };
			var cmds = new SortedDictionary<string, string> {
				{
					"ls",
					"\t List all known stocks"
				},
				{
					"exit",
					"\t Exit stocks"
				},
				{
					"up stock",
					"Updates stock"
				},
				{
					"help",
					"\t Displays help"
				},
				{
					"stock",
					"\t Displays latest valuations for stock"
				}
			};
			if (cmds.ContainsKey (cmd)) {
				display (cmd, cmds[cmd]);
			} else {
				foreach (var ch in cmds) {
					display (ch.Key, ch.Value);
				}
			}
		}

		void Run ()
		{
			var WS = new char[] {
				' ',
				'\t',
				'\r',
				'\n'
			};
			
			Initialize ();
			
			DisplayBanner ();
			DisplayHelp ("");
			
			for (;;) {
				Console.Write ("$ ");
				var cmdline = Console.ReadLine ();
				
				var args = cmdline.Split (WS, StringSplitOptions.RemoveEmptyEntries);
				if (args.Length < 1)
					continue;
				var cmd = args[0].ToLowerInvariant ();
				
				if (cmd == "?" || cmd == "help") {
					DisplayHelp ("");
				} else if (cmd == "exit") {
					break;
				} else if (cmd == "ls") {
					ListStocks ();
				} else if (cmd == "up") {
					if (args.Length == 2) {
						UpdateStock (args[1].ToUpperInvariant ());
					} else {
						DisplayHelp ("up stock");
					}
				} else {
					DisplayStock (cmd.ToUpperInvariant ());
				}
			}
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
