using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SQLite.Net.Tests.Generic.IoC
{
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	[TestClass]
	public class IoCUnitTests
	{
		private TinyIoC.TinyIoCContainer _container;

		[TestInitialize]
		public void Initialize()
		{
			_container = new TinyIoC.TinyIoCContainer();

			_container.Register<IProduct, Product>();
			_container.Register<IOrder, Order>();
			_container.Register<IOrderHistory, OrderHistory>();
			_container.Register<IOrderLine, OrderLine>();			
		}

		private static void VerifyCreations(TestDb db)
		{
			var orderLine = db.GetMapping(typeof(OrderLine));
			Assert.AreEqual(6, orderLine.Columns.Length);

			var l = new OrderLine
			{
				Status = OrderLineStatus.Shipped
			};
			db.Insert(l);
			OrderLine lo = db.Table<OrderLine>().First(x => x.Status == OrderLineStatus.Shipped);
			Assert.AreEqual(lo.Id, l.Id);
		}

		[TestMethod]
		public void CreateThemByInterface()
		{
			var db = new TestDb();

			db.CreateTable<IProduct>();
			db.CreateTable<IOrder>();
			db.CreateTable<IOrderLine>();
			db.CreateTable<IOrderHistory>();

			VerifyCreations(db);
		}


		[TestMethod]
		[TestCategory("IoC - TinyIoC")]
		public void BulkInsertAndSelect()
		{
			var db = new TestDb(false, new ContractResolver(t => _container.CanResolve(t), (t, op) => _container.Resolve(t)));

			db.CreateTable<IProduct>();
			db.CreateTable<IOrder>();
			db.CreateTable<IOrderLine>();
			db.CreateTable<IOrderHistory>();

			var rnd = new Random();
			var data = new List<IOrderLine>();
			for (int i = 0; i < 100; i++)
			{
				var l = _container.Resolve<IOrderLine>();

				l.OrderId = rnd.Next(1, 100);
				l.ProductId = rnd.Next(1, 100);
				l.Quantity = rnd.Next(1, 25);
				l.Status = OrderLineStatus.Shipped;
				l.UnitPrice = rnd.Next(0, 100);

				data.Add(l);
			}

			db.InsertAll(data.ToArray());

			var results = db.Table<IOrderLine>();

			Assert.AreEqual(data.Count, results.Count());
			Assert.AreEqual(data.First().UnitPrice, results.First().UnitPrice);
			Assert.AreEqual(data.Last().UnitPrice, results.Last().UnitPrice);
		}
	}
}
