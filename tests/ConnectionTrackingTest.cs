
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SetUp = Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#else
using NUnit.Framework;
#endif


namespace SQLite.Tests
{
	[TestFixture]
	public class ConnectionTrackingTest
	{
		public class Product
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }
			public string Name { get; set; }
			public decimal Price { get; set; }

			public TestDb Connection { get; private set; }

			public OrderLine[] GetOrderLines ()
			{
				return Connection.Table<OrderLine> ().Where (o => o.ProductId == Id).ToArray ();
			}
		}
		public class OrderLine
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }
			public int ProductId { get; set; }
			public int Quantity { get; set; }
			public decimal UnitPrice { get; set; }

			public TestDb Connection { get; private set; }
		}

		public class TestDb : SQLiteConnection
		{
			public TestDb () : base(TestPath.GetTempFileName ())
			{
				CreateTable<Product> ();
				CreateTable<OrderLine> ();
				Trace = true;
			}
		}

		[Test]
		public void CreateThem ()
		{
			var db = new TestDb ();
			
			var foo = new Product { Name = "Foo", Price = 10.0m };
			var bar = new Product { Name = "Bar", Price = 0.10m };
			db.Insert (foo);
			db.Insert (bar);
			db.Insert (new OrderLine { ProductId = foo.Id, Quantity = 6, UnitPrice = 10.01m });
			db.Insert (new OrderLine { ProductId = foo.Id, Quantity = 3, UnitPrice = 0.02m });
			db.Insert (new OrderLine { ProductId = bar.Id, Quantity = 9, UnitPrice = 100.01m });
			
			var lines = foo.GetOrderLines ();
			
			Assert.AreEqual (lines.Length, 2, "Has 2 order lines");
			Assert.AreEqual (foo.Connection, db, "foo.Connection was set");
			Assert.AreEqual (lines[0].Connection, db, "lines[0].Connection was set");
		}
	}
}
