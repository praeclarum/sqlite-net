using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
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
            public TestDb() : base(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase())
            {
                TraceListener = DebugTraceListener.Instance;
            }
        }

        [Test]
        public void CreateInsertDrop()
        {
            var db = new TestDb();

            db.CreateTable<Product>();

            db.Insert(new Product
            {
                Name = "Hello",
                Price = 16,
            });

            int n = db.Table<Product>().Count();

            Assert.AreEqual(1, n);

            db.DropTable<Product>();

            ExceptionAssert.Throws<SQLiteException>(() => db.Table<Product>().Count());
        }
    }
}