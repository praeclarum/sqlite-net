using System;
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
			
			VerifyCreations(db);
		}

	    [Test]
        public void CreateAsPassedInTypes ()
        {
            var db = new TestDb();

            db.CreateTable(typeof(Product));
            db.CreateTable(typeof(Order));
            db.CreateTable(typeof(OrderLine));
            db.CreateTable(typeof(OrderHistory));

            VerifyCreations(db);
        }

		[Test]
		public void CreateTwice ()
		{
			var db = new TestDb ();
			
			db.CreateTable<Product> ();
			db.CreateTable<OrderLine> ();
			db.CreateTable<Order> ();
			db.CreateTable<OrderLine> ();
			db.CreateTable<OrderHistory> ();
			
			VerifyCreations(db);
		}
        
        private static void VerifyCreations(TestDb db)
        {
            var orderLine = db.GetMapping(typeof(OrderLine));
            Assert.AreEqual(6, orderLine.Columns.Length);

            var l = new OrderLine()
            {
                Status = OrderLineStatus.Shipped
            };
            db.Insert(l);
            var lo = db.Table<OrderLine>().First(x => x.Status == OrderLineStatus.Shipped);
            Assert.AreEqual(lo.Id, l.Id);
        }

    }
}
