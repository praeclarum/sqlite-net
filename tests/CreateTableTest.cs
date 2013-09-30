using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class CreateTableTest
    {
        private static void VerifyCreations(TestDb db)
        {
            TableMapping orderLine = db.GetMapping(typeof (OrderLine));
            Assert.AreEqual(6, orderLine.Columns.Length);

            var l = new OrderLine
            {
                Status = OrderLineStatus.Shipped
            };
            db.Insert(l);
            OrderLine lo = db.Table<OrderLine>().First(x => x.Status == OrderLineStatus.Shipped);
            Assert.AreEqual(lo.Id, l.Id);
        }

        private class Issue115_MyObject
        {
            [PrimaryKey]
            public string UniqueId { get; set; }

            public byte OtherValue { get; set; }
        }

        [Test]
        public void CreateAsPassedInTypes()
        {
            var db = new TestDb();

            db.CreateTable(typeof (Product));
            db.CreateTable(typeof (Order));
            db.CreateTable(typeof (OrderLine));
            db.CreateTable(typeof (OrderHistory));

            VerifyCreations(db);
        }

        [Test]
        public void CreateThem()
        {
            var db = new TestDb();

            db.CreateTable<Product>();
            db.CreateTable<Order>();
            db.CreateTable<OrderLine>();
            db.CreateTable<OrderHistory>();

            VerifyCreations(db);
        }

        [Test]
        public void CreateTwice()
        {
            var db = new TestDb();

            db.CreateTable<Product>();
            db.CreateTable<OrderLine>();
            db.CreateTable<Order>();
            db.CreateTable<OrderLine>();
            db.CreateTable<OrderHistory>();

            VerifyCreations(db);
        }

        [Test]
        public void Issue115_MissingPrimaryKey()
        {
            using (var conn = new TestDb())
            {
                conn.CreateTable<Issue115_MyObject>();
                conn.InsertAll(from i in Enumerable.Range(0, 10)
                    select new Issue115_MyObject
                    {
                        UniqueId = i.ToString(),
                        OtherValue = (byte) (i*10),
                    });

                TableQuery<Issue115_MyObject> query = conn.Table<Issue115_MyObject>();
                foreach (Issue115_MyObject itm in query)
                {
                    itm.OtherValue++;
                    Assert.AreEqual(1, conn.Update(itm, typeof (Issue115_MyObject)));
                }
            }
        }
    }
}