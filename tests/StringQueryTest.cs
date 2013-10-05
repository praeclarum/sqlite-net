using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class StringQueryTest
    {
        [SetUp]
        public void SetUp()
        {
            db = new TestDb();
            db.CreateTable<Product>();

            var prods = new[]
            {
                new Product
                {
                    Name = "Foo"
                },
                new Product
                {
                    Name = "Bar"
                },
                new Product
                {
                    Name = "Foobar"
                }
            };

            db.InsertAll(prods);
        }

        private TestDb db;

        [Test]
        public void Contains()
        {
            List<Product> fs = db.Table<Product>().Where(x => x.Name.Contains("o")).ToList();
            Assert.AreEqual(2, fs.Count);

            List<Product> bs = db.Table<Product>().Where(x => x.Name.Contains("a")).ToList();
            Assert.AreEqual(2, bs.Count);
        }

        [Test]
        public void EndsWith()
        {
            List<Product> fs = db.Table<Product>().Where(x => x.Name.EndsWith("ar")).ToList();
            Assert.AreEqual(2, fs.Count);

            List<Product> bs = db.Table<Product>().Where(x => x.Name.EndsWith("o")).ToList();
            Assert.AreEqual(1, bs.Count);
        }

        [Test]
        public void StartsWith()
        {
            List<Product> fs = db.Table<Product>().Where(x => x.Name.StartsWith("F")).ToList();
            Assert.AreEqual(2, fs.Count);

            List<Product> bs = db.Table<Product>().Where(x => x.Name.StartsWith("B")).ToList();
            Assert.AreEqual(1, bs.Count);
        }
    }
}