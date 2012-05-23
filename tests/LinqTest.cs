using System;
using System.Linq;
using System.Collections.Generic;

#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
#if NETFX_CORE
    [TestClass]
#else
    [TestFixture]
#endif
    public class LinqTest
	{
		TestDb CreateDb ()
		{
			var db = new TestDb ();
			db.CreateTable<Product> ();
			db.CreateTable<Order> ();
			db.CreateTable<OrderLine> ();
			db.CreateTable<OrderHistory> ();
			return db;
		}

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        public void FunctionParameter()
		{
			var db = CreateDb ();
			
			db.Insert (new Product {
				Name = "A",
				Price = 20,
			});
			
			db.Insert (new Product {
				Name = "B",
				Price = 10,
			});
			
			Func<decimal, List<Product>> GetProductsWithPriceAtLeast = delegate(decimal val) {
				return (from p in db.Table<Product> () where p.Price > val select p).ToList ();
			}; 
			
			var r = GetProductsWithPriceAtLeast (15);
			Assert.AreEqual (1, r.Count);
			Assert.AreEqual ("A", r [0].Name);
		}

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        public void WhereGreaterThan()
		{
			var db = CreateDb ();
			
			db.Insert (new Product {
				Name = "A",
				Price = 20,
			});
			
			db.Insert (new Product {
				Name = "B",
				Price = 10,
			});
			
			Assert.AreEqual (2, db.Table<Product> ().Count ());
			
			var r = (from p in db.Table<Product> () where p.Price > 15 select p).ToList ();
			Assert.AreEqual (1, r.Count);
			Assert.AreEqual ("A", r [0].Name);
		}
	}
}
