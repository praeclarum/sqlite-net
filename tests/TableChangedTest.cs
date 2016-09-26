using System;
using System.Linq;


#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SetUp = Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#else
using NUnit.Framework;
#endif



namespace SQLite.Tests
{    
	[TestFixture]
	public class TableChangedTest
	{
		TestDb db;
		int changeCount = 0;

		[SetUp]
		public void SetUp ()
		{
			db = new TestDb ();
			db.Trace = true;
			db.CreateTable<Product> ();
			db.CreateTable<Order> ();
			db.InsertAll (from i in Enumerable.Range (0, 22)
				select new Product { Name = "Thing" + i, Price = (decimal)Math.Pow (2, i) });

			changeCount = 0;

			db.TableChanged += (sender, e) => {

				if (e.Table.TableName == "Product") {
					changeCount++;
				}
			};
		}

		[Test]
		public void Insert ()
		{
			var query =
				from p in db.Table<Product> ()
				select p;

			Assert.AreEqual (0, changeCount);
			Assert.AreEqual (22, query.Count ());

			db.Insert (new Product { Name = "Hello", Price = 1001 });

			Assert.AreEqual (1, changeCount);
			Assert.AreEqual (23, query.Count ());
		}

		[Test]
		public void InsertAll ()
		{
			var query =
				from p in db.Table<Product> ()
				select p;

			Assert.AreEqual (0, changeCount);
			Assert.AreEqual (22, query.Count ());

			db.InsertAll (from i in Enumerable.Range (0, 22)
				select new Product { Name = "Test" + i, Price = (decimal)Math.Pow (3, i) });

			Assert.AreEqual (22, changeCount);
			Assert.AreEqual (44, query.Count ());
		}

		[Test]
		public void Update ()
		{
			var query =
				from p in db.Table<Product> ()
				select p;

			Assert.AreEqual (0, changeCount);
			Assert.AreEqual (22, query.Count ());

			var pr = query.First ();
			pr.Price = 10000000;
			db.Update (pr);

			Assert.AreEqual (1, changeCount);
			Assert.AreEqual (22, query.Count ());
		}

		[Test]
		public void Delete ()
		{
			var query =
				from p in db.Table<Product> ()
				select p;

			Assert.AreEqual (0, changeCount);
			Assert.AreEqual (22, query.Count ());

			var pr = query.First ();
			pr.Price = 10000000;
			db.Delete (pr);

			Assert.AreEqual (1, changeCount);
			Assert.AreEqual (21, query.Count ());

			db.DeleteAll<Product> ();

			Assert.AreEqual (2, changeCount);
			Assert.AreEqual (0, query.Count ());
		}
	}
}
