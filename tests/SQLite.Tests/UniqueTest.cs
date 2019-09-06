using System;
using System.Collections.Generic;
using System.Linq;

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
	public class UniqueIndexTest
	{
		public class TheOne {
			[PrimaryKey, AutoIncrement]
			public int ID { get; set; }

			[Unique (Name = "UX_Uno")]
			public int Uno { get; set;}

			[Unique (Name = "UX_Dos")]
			public int Dos { get; set;}
			[Unique (Name = "UX_Dos")]
			public int Tres { get; set;}

			[Indexed (Name = "UX_Uno_bool", Unique = true)]
			public int Cuatro { get; set;}

			[Indexed (Name = "UX_Dos_bool", Unique = true)]
			public int Cinco { get; set;}
			[Indexed (Name = "UX_Dos_bool", Unique = true)]
			public int Seis { get; set;}
		}

		public class IndexColumns {
			public int seqno { get; set;} 
			public int cid { get; set;} 
			public string name { get; set; } 
		}

		public class IndexInfo {
			public int seq { get; set;} 
			public string name { get; set;} 
			public bool unique { get; set;}
		}

		[Test]
		public void CreateUniqueIndexes ()
		{
			using (var db = new TestDb ()) {
				db.CreateTable<TheOne> ();
				var indexes = db.Query<IndexInfo> ("PRAGMA INDEX_LIST (\"TheOne\")");
				Assert.AreEqual (4, indexes.Count, "# of indexes");
				CheckIndex (db, indexes, "UX_Uno", true, "Uno");
				CheckIndex (db, indexes, "UX_Dos", true, "Dos", "Tres");
				CheckIndex (db, indexes, "UX_Uno_bool", true, "Cuatro");
				CheckIndex (db, indexes, "UX_Dos_bool", true, "Cinco", "Seis");
			}
		}

		static void CheckIndex (TestDb db, List<IndexInfo> indexes, string iname, bool unique, params string [] columns)
		{
			if (columns == null)
				throw new Exception ("Don't!");
			var idx = indexes.SingleOrDefault (i => i.name == iname);
			Assert.IsNotNull (idx, String.Format ("Index {0} not found", iname));
			Assert.AreEqual (idx.unique, unique, String.Format ("Index {0} unique expected {1} but got {2}", iname, unique, idx.unique));
			var idx_columns = db.Query<IndexColumns> (String.Format ("PRAGMA INDEX_INFO (\"{0}\")", iname));
			Assert.AreEqual (columns.Length, idx_columns.Count, String.Format ("# of columns: expected {0}, got {1}", columns.Length, idx_columns.Count));
			foreach (var col in columns) {
				Assert.IsNotNull (idx_columns.SingleOrDefault (c => c.name == col), String.Format ("Column {0} not in index {1}", col, idx.name));
			}
		}
	}
}
