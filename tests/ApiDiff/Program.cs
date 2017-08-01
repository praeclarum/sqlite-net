using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using SQLite;
using Praeclarum;

namespace ApiDiff
{
	class Api
	{
		public string Name;
		public string Declaration;
		public string Index;

		static readonly Regex taskRe = new Regex (@"System\.Threading\.Tasks\.Task`1\[([^\]]*)\]");

		public Api (MemberInfo member, string nameSuffix)
		{
			Name = member.Name;
			Declaration = member.ToString ();
			Index = Declaration;

			if (nameSuffix.Length > 0 && Name.EndsWith (nameSuffix)) {
				var indexName = Name.Substring (0, Name.IndexOf (nameSuffix));
				Index = taskRe.Replace (Index.Replace (Name, indexName), "$1").Replace ("System.Int32", "Int32");
				Name = indexName;
			}
		}
	}

	class Apis
	{
		public List<Api> All;
		readonly string nameSuffix;
		readonly Type type;

		static readonly HashSet<string> ignores = new HashSet<string> {
			"BeginTransaction",
			"Commit",
			"Rollback",
			"RollbackTo",
			"IsInTransaction",
			"EndTransaction",

			"GetConnection",
			"Handle",

			"Close",
			"Dispose",
		};

		public Apis (Type type, string nameSuffix = "")
		{
			this.type = type;
			this.nameSuffix = nameSuffix;
			All = type.GetMembers (BindingFlags.Public|BindingFlags.Instance)
			          .Where (x => !ignores.Contains(x.Name))
			          .Where (x => !x.Name.StartsWith("get_") && !x.Name.StartsWith ("set_") && !x.Name.StartsWith ("remove_") && !x.Name.StartsWith ("add_"))
			          .Select (x => new Api(x, nameSuffix))
			          .OrderBy (x => x.Name)
			          .ToList ();
		}

		public int DumpComparison (Apis other)
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine (type.FullName);

			var diff = new ListDiff<Api, Api> (All, other.All, (x, y) => x.Index == y.Index);

			var n = 0;

			foreach (var a in diff.Actions) {
				switch (a.ActionType) {
					case ListDiffActionType.Add:
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine ($"  + {a.DestinationItem.Index}");
						n++;
						break;
					case ListDiffActionType.Remove:
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine ($"  - {a.SourceItem.Index}");
						n++;
						break;
					case ListDiffActionType.Update:
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine ($"    {a.SourceItem.Index}");
						break;
				}
			}
			Console.ResetColor ();
			Console.WriteLine ($"{n} differences");

			return n;
		}
	}

	class MainClass
	{
		public static int Main (string[] args)
		{
			var synchronous = new Apis (typeof (SQLiteConnection));
			var asynchronous = new Apis (typeof (SQLiteAsyncConnection), "Async");
			var n = asynchronous.DumpComparison (synchronous);
			return n > 0 ? 1 : 0;
		}
	}
}
