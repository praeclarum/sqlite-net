
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
#if NETFX_CORE
    [TestClass]
#else
    [TestFixture]
#endif
    public class DropTableTest
	{
		public class Product
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }
			public string Name { get; set; }
			public decimal Price { get; set; }
		}
		
		public class TestDb : SQLiteConnection
		{
			public TestDb () : base(TestHelper.GetTempDatabasePath ())
			{
				Trace = true;
			}
		}

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        [ExpectedException(typeof(SQLiteException))]
		public void CreateInsertDrop ()
		{
			var db = new TestDb ();
			
			db.CreateTable<Product> ();
			
			db.Insert (new Product {
				Name = "Hello",
				Price = 16,
			});
			
			var n = db.Table<Product> ().Count ();
			
			Assert.AreEqual (1, n);
			
			db.DropTable<Product> ();
			
			db.Table<Product> ().Count (); // Should throw
		}
	}
}
