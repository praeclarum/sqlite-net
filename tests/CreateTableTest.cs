using System;
using System.Linq;
using NUnit.Framework;

namespace SQLite.Tests
{
	[TestFixture]
	public class CreateTableTest
	{
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
			var lo = db.Table<OrderLine>().Where(x => x.Status == OrderLineStatus.Shipped).FirstOrDefault();
			Assert.AreEqual(lo.Id, l.Id);
		}
	}
}
