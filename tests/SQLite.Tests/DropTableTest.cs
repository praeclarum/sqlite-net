
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
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
			public TestDb () : base(TestPath.GetTempFileName ())
			{
				Trace = true;
			}
		}
		
		public class TestDbAsync : SQLiteAsyncConnection
		{
			public TestDbAsync () : base(TestPath.GetTempFileName ())
			{
				Trace = true;
			}
		}
		
		[Test]
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
			
			ExceptionAssert.Throws<SQLiteException>(() => db.Table<Product> ().Count ());
		}
		
		[Test]
		public async Task CreateInsertDropAsync ()
		{
			var db = new TestDbAsync ();
			
			await db.CreateTableAsync<Product> ();
			
			await db.InsertAsync (new Product {
				Name = "Hello",
				Price = 16,
			});
			
			var n = await db.Table<Product> ().CountAsync ();
			
			Assert.AreEqual (1, n);
			
			await db.DropTableAsync<Product> ();
			
			try {
				await db.Table<Product> ().CountAsync ();
				Assert.Fail ("Should have thrown");
			} catch (SQLiteException) {
				// Expected
			}
		}
	}
}
