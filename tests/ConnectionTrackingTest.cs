using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

namespace SQLite.Net.Tests
{
    [NUnit.Framework.Ignore("This test class/file was not included in the original project and is broken")]
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

            public OrderLine[] GetOrderLines()
            {
                return Connection.Table<OrderLine>().Where(o => o.ProductId == Id).ToArray();
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
            public TestDb(ISQLitePlatform sqlitePlatform)
                : base(sqlitePlatform, TestPath.CreateTemporaryDatabase())
            {
                CreateTable<Product>();
                CreateTable<OrderLine>();
                TraceListener = DebugTraceListener.Instance;
            }
        }

        [Test]
        public void CreateThem()
        {
            var db = new TestDb(new SQLitePlatformTest());

            var foo = new Product
            {
                Name = "Foo",
                Price = 10.0m
            };
            var bar = new Product
            {
                Name = "Bar",
                Price = 0.10m
            };
            db.Insert(foo);
            db.Insert(bar);
            db.Insert(new OrderLine
            {
                ProductId = foo.Id,
                Quantity = 6,
                UnitPrice = 10.01m
            });
            db.Insert(new OrderLine
            {
                ProductId = foo.Id,
                Quantity = 3,
                UnitPrice = 0.02m
            });
            db.Insert(new OrderLine
            {
                ProductId = bar.Id,
                Quantity = 9,
                UnitPrice = 100.01m
            });

            OrderLine[] lines = foo.GetOrderLines();

            Assert.AreEqual(lines.Length, 2, "Has 2 order lines");
            Assert.AreEqual(foo.Connection, db, "foo.Connection was set");
            Assert.AreEqual(lines[0].Connection, db, "lines[0].Connection was set");
        }
    }
}