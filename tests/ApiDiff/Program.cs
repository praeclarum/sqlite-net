using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using SQLite;
using ListDiff;

namespace ApiDiff
{
	class Api
	{
		public string Name;
		public string Declaration;
		public string Index;

		static readonly Regex task1Re = new Regex (@"System\.Threading\.Tasks\.Task`1\[([^\]]*)\]");
		static readonly Regex taskRe = new Regex (@"System\.Threading\.Tasks\.Task(\s*)");

		public Api (MemberInfo member, string nameSuffix)
		{
			Name = member.Name;
			Declaration = member.ToString ();
			Index = Declaration.Replace ("AsyncTableQuery`1", "TableQuery`1");

			if (nameSuffix.Length > 0 && Name.EndsWith (nameSuffix)) {
				var indexName = Name.Substring (0, Name.IndexOf (nameSuffix));
				Index = taskRe
					.Replace (task1Re.Replace (Index.Replace (Name, indexName), "$1"), "Void$1")
					.Replace ("System.Int32", "Int32");
				Name = indexName;
			}
		}
	}

	class Apis
	{
		public List<Api> All;
		readonly string nameSuffix;
		readonly Type type;

		public static readonly HashSet<string> connectionIgnores = new HashSet<string> {
			"RunInTransaction",
			"RunInTransactionAsync",
			"BeginTransaction",
			"SaveTransactionPoint",
			"Commit",
			"Rollback",
			"RollbackTo",
			"IsInTransaction",
			"Release",
			"EndTransaction",

			"BusyTimeout",
			"GetBusyTimeout",
			"SetBusyTimeoutAsync",

			"GetConnection",
			"Handle",

			"Dispose",

			"Table",
			"CreateCommand",
			"TableChanged",
		};

		public static readonly HashSet<string> queryIgnores = new HashSet<string> {
			".ctor",
			"Clone",
			"Connection",
			"Deferred",
			"Table",
			"GetEnumerator",
		};

		public Apis (Type type, HashSet<string> ignores, string nameSuffix = "")
		{
			this.type = type;
			this.nameSuffix = nameSuffix;
			All = type.GetMembers (BindingFlags.Public|BindingFlags.Instance)
			          .Where (x => !ignores.Contains(x.Name))
			          .Where (x => x.MemberType != MemberTypes.NestedType)
			          .Where (x => !x.Name.StartsWith("get_") && !x.Name.StartsWith ("set_") && !x.Name.StartsWith ("remove_") && !x.Name.StartsWith ("add_"))
			          .Select (x => new Api(x, nameSuffix))
			          .OrderBy (x => x.Index)
			          .OrderBy (x => x.Name)
			          .ToList ();
		}

		public int DumpComparison (Apis other)
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine ("## " + type.FullName);
			Console.WriteLine ();

			var diff = new ListDiff<Api, Api> (All, other.All, (x, y) => x.Index == y.Index);

			var n = 0;

			foreach (var a in diff.Actions) {
				switch (a.ActionType) {
					case ListDiffActionType.Add:
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine ($"- [ ] *add* `{a.DestinationItem.Index.Replace('`', '_')}`");
						n++;
						break;
					case ListDiffActionType.Remove:
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine ($"- [ ] *remove* `{a.SourceItem.Index.Replace('`', '_')}`");
						n++;
						break;
					case ListDiffActionType.Update:
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine ($"- [x] `{a.SourceItem.Index.Replace('`', '_')}`");
						break;
				}
			}
			Console.ResetColor ();
			Console.WriteLine ();
			Console.WriteLine ($"**{n}** differences");
			Console.WriteLine ();

			return n;
		}
	}

	class MainClass
	{
		public static int Main (string[] args)
		{
			var synchronousConnection = new Apis (typeof (SQLiteConnection), Apis.connectionIgnores);
			var asynchronousConnection = new Apis (typeof (SQLiteAsyncConnection), Apis.connectionIgnores, "Async");
			var n = asynchronousConnection.DumpComparison (synchronousConnection);

			var synchronousQuery = new Apis (typeof (TableQuery<>), Apis.queryIgnores);
			var asynchronousQuery = new Apis (typeof (AsyncTableQuery<>), Apis.queryIgnores, "Async");
			n += asynchronousQuery.DumpComparison (synchronousQuery);

			return n > 0 ? 1 : 0;
		}
	}
}
