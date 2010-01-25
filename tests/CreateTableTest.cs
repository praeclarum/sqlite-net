
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

using NUnit.Framework;

namespace SQLite.Tests
{
	[TestFixture]
	public class CreateTableTest
	{
		public class Product
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }
			public string Name { get; set; }
			public decimal Price { get; set; }
		}
		public class Order
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }
			public DateTime PlacedTime { get; set; }
		}
		public class OrderHistory {
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }
			public int OrderId { get; set; }
			public DateTime Time { get; set; }
			public string Comment { get; set; }
		}
		public class OrderLine
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }
			public int OrderId { get; set; }
			public int ProductId { get; set; }
			public int Quantity { get; set; }
			public decimal UnitPrice { get; set; }
			public OrderLineStatus Status { get; set; }
		}
		public enum OrderLineStatus {
			Placed = 1,
			Shipped = 100
		}

		public class TestDb : SQLiteConnection
		{
			public TestDb () : base(Path.GetTempFileName ())
			{
				Trace = true;
			}
		}

		[Test]
		public void CreateThem ()
		{
			var db = new TestDb ();
			
			db.CreateTable<Product> ();
			db.CreateTable<Order> ();
			db.CreateTable<OrderLine> ();
			db.CreateTable<OrderHistory> ();
			
			var orderLine = db.GetMapping(typeof(OrderLine));
			Assert.AreEqual(6, orderLine.Columns.Length, "Order history has 3 columns");
			
			var l = new OrderLine() {
				Status = OrderLineStatus.Shipped
			};
			db.Insert(l);
			var lo = db.Table<OrderLine>().Where(x => x.Status == CreateTableTest.OrderLineStatus.Shipped).FirstOrDefault();
			Assert.AreEqual(lo.Id, l.Id);
		}
	}
}
