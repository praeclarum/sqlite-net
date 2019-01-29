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
	public class CreateTableFluentTest
	{
		class NoPropObject
		{
		}

		[Test, ExpectedException]
		public void CreateTypeWithNoProps ()
		{
			var db = new TestDb ();

			var mapping = TableMapping.Build<NoPropObject> ().ToMapping ();

			db.CreateTable (mapping);
		}

		class DbSchema
		{
			public TableMapping Products { get; }
			public TableMapping Orders { get; }
			public TableMapping OrderLines { get; }
			public TableMapping OrderHistory { get; }
			
			public DbSchema ()
			{
				Products = TableMapping.Build<ProductPoco> ()
					.TableName("Product")
					.PrimaryKey (x => x.Id, autoIncrement: true)
					.ToMapping ();

				Orders = TableMapping.Build<OrderPoco> ()
					.TableName("Order")
					.PrimaryKey (x => x.Id, autoIncrement: true)
					.ToMapping ();

				OrderLines = TableMapping.Build<OrderLinePoco> ()
					.TableName("OrderLine")
					.PrimaryKey (x => x.Id, autoIncrement: true)
					.Index ("IX_OrderProduct", x => x.OrderId, x => x.ProductId)
					.ToMapping ();

				OrderHistory = TableMapping.Build<OrderHistoryPoco> ()
					.TableName("OrderHistory")
					.PrimaryKey (x => x.Id, autoIncrement: true)
					.ToMapping ();
			}

			public TableMapping[] Tables => new[] {Products, Orders, OrderLines, OrderHistory};
		}

		[Test]
		public void CreateThem ()
		{
			var db = new TestDb ();
			var schema = new DbSchema ();

			db.CreateTables (CreateFlags.None, schema.Tables);

			VerifyCreations (db);
		}

		[Test]
		public void CreateTwice ()
		{
			var db = new TestDb ();
			
			var product = TableMapping.Build<ProductPoco> ()
				.TableName("Product")
				.PrimaryKey (x => x.Id, autoIncrement: true)
				.ToMapping ();
			
			var order = TableMapping.Build<OrderPoco>()
				.TableName("Order")
				.PrimaryKey (x => x.Id, autoIncrement: true)
				.ToMapping ();

			var orderLine = TableMapping.Build<OrderLinePoco> ()
				.TableName("OrderLine")
				.PrimaryKey (x => x.Id, autoIncrement: true)
				.Index ("IX_OrderProduct", x => x.OrderId, x => x.ProductId)
				.ToMapping ();
			
			var orderHistory = TableMapping.Build<OrderHistoryPoco>()
				.TableName("OrderHistory")
				.PrimaryKey (x => x.Id, autoIncrement: true)
				.ToMapping ();

			db.CreateTable (product);
			db.CreateTable (order);
			db.CreateTable (orderLine);
			db.CreateTable (orderHistory);
			
			VerifyCreations(db);
		}
        
        private static void VerifyCreations(TestDb db)
        {
            var orderLine = db.GetMapping(typeof(OrderLinePoco));
            Assert.AreEqual(6, orderLine.Columns.Length);

            var l = new OrderLine()
            {
                Status = OrderLineStatus.Shipped
            };
            db.Insert(l);
            var lo = db.Table<OrderLinePoco>().First(x => x.Status == OrderLineStatus.Shipped);
            Assert.AreEqual(lo.Id, l.Id);
        }

		class Issue115_MyObject
		{
			public string UniqueId { get; set; }
			public byte OtherValue { get; set; }
		}

		[Test]
		public void Issue115_MissingPrimaryKey ()
		{
			using (var conn = new TestDb ()) {
				var mapping = TableMapping.Build<Issue115_MyObject> ()
					.PrimaryKey (x => x.UniqueId)
					.ToMapping ();
				conn.CreateTable (mapping);
				conn.InsertAll (from i in Enumerable.Range (0, 10)
					select new Issue115_MyObject {
						UniqueId = i.ToString (),
						OtherValue = (byte)(i * 10),
					});

				var query = conn.Table<Issue115_MyObject> (mapping);
				foreach (var itm in query) {
					itm.OtherValue++;
					Assert.AreEqual (1, conn.Update (itm, typeof(Issue115_MyObject)));
				}
			}
		}

		class WantsNoRowId
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		class SqliteMaster
		{
			public string Type { get; set; }
			public string Name { get; set; }
			public string TableName { get; set; }
			public int RootPage { get; set; }
			public string Sql { get; set; }
		}

		[Test]
		public void WithoutRowId ()
		{
			using (var conn = new TestDb ()) {
				var master = TableMapping.Build<SqliteMaster> ()
					.TableName("sqlite_master")
					.ColumnName (x => x.Type, "type")
					.ColumnName (x => x.Name, "name")
					.ColumnName (x => x.TableName, "tbl_name")
					.ColumnName (x => x.RootPage, "rootpage")
					.ColumnName (x => x.Sql, "sql")
					.ToMapping ();

				var wantsNoRowId = TableMapping.Build<WantsNoRowId> ()
					.PrimaryKey (x => x.Id)
					.WithoutRowId ()
					.ToMapping ();

				var orderLine = TableMapping.Build<OrderLinePoco> ()
					.TableName("OrderLine")
					.PrimaryKey (x => x.Id, autoIncrement: true)
					.Index ("IX_OrderProduct", x => x.OrderId, x => x.ProductId)
					.ToMapping ();

				conn.CreateTable (orderLine);
				var info = conn.Table<SqliteMaster> (master).Where (m => m.TableName == "OrderLine").First ();
				Assert.That (!info.Sql.Contains ("without rowid"));

				conn.CreateTable (wantsNoRowId);
				info = conn.Table<SqliteMaster> (master).Where (m => m.TableName == "WantsNoRowId").First ();
				Assert.That (info.Sql.Contains ("without rowid"));
			}
		}
    }
	
	public class ProductPoco
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public decimal Price { get; set; }

		public uint TotalSales { get; set; }
	}
	public class OrderPoco
	{
		public int Id { get; set; }
		public DateTime PlacedTime { get; set; }
	}
	public class OrderHistoryPoco {
		public int Id { get; set; }
		public int OrderId { get; set; }
		public DateTime Time { get; set; }
		public string Comment { get; set; }
	}
	public class OrderLinePoco
	{
		public int Id { get; set; }
		public int OrderId { get; set; }
		public int ProductId { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public OrderLineStatus Status { get; set; }
	}
}
