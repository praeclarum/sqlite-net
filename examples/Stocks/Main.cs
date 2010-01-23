using System;
using System.Collections.Generic;
using System.Linq;
using Path = System.IO.Path;

using SQLite;

namespace Stocks.CommandLine
{
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
			_db.UpdateStock(stockSymbol);
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
}
