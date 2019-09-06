using System;
using System.Linq;
using System.Collections.Generic;

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
		
		[Test]
		public void FunctionParameter ()
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
		
		[Test]
		public void WhereGreaterThan ()
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
		
		[Test]
        public void GetWithExpression ()
		{
			var db = CreateDb();
			
			db.Insert (new Product {
				Name = "A",
				Price = 20,
			});
			
			db.Insert (new Product {
				Name = "B",
				Price = 10,
			});

            db.Insert(new Product
            {
                Name = "C",
                Price = 5,
            });
			
			Assert.AreEqual (3, db.Table<Product> ().Count ());
			
			var r = db.Get<Product>(x => x.Price == 10);
            Assert.IsNotNull(r);
			Assert.AreEqual ("B", r.Name);
		}
		
		[Test]
        public void FindWithExpression ()
		{
			var db = CreateDb();
			
			var r = db.Find<Product> (x => x.Price == 10);
            Assert.IsNull (r);
		}

		[Test]
		public void OrderByCast ()
		{
			var db = CreateDb();

			db.Insert (new Product {
				Name = "A",
				TotalSales = 1,
			});
			db.Insert (new Product {
				Name = "B",
				TotalSales = 100,
			});

			var nocast = (from p in db.Table<Product> () orderby p.TotalSales descending select p).ToList ();
			Assert.AreEqual (2, nocast.Count);
			Assert.AreEqual ("B", nocast [0].Name);

			var cast = (from p in db.Table<Product> () orderby (int)p.TotalSales descending select p).ToList ();
			Assert.AreEqual (2, cast.Count);
			Assert.AreEqual ("B", cast [0].Name);			
		}

		public class Issue96_A
		{
			[ AutoIncrement, PrimaryKey]
			public int ID { get; set; }
			public string AddressLine { get; set; }
			
			[Indexed]
			public int? ClassB { get; set; }
			[Indexed]
			public int? ClassC { get; set; }
		}
		
		public class Issue96_B
		{
			[ AutoIncrement, PrimaryKey]
			public int ID { get; set; }
			public string CustomerName { get; set; }
		}
		
		public class Issue96_C
		{
			[ AutoIncrement, PrimaryKey]
			public int ID { get; set; }
			public string SupplierName { get; set; }
		}

		[Test]
		public void Issue96_NullabelIntsInQueries ()
		{
			var db = CreateDb();
			db.CreateTable<Issue96_A> ();

			var id = 42;

			db.Insert (new Issue96_A {
				ClassB = id,
			});
			db.Insert (new Issue96_A {
				ClassB = null,
			});
			db.Insert (new Issue96_A {
				ClassB = null,
			});
			db.Insert (new Issue96_A {
				ClassB = null,
			});


			Assert.AreEqual (1, db.Table<Issue96_A>().Where(p => p.ClassB == id).Count ());
			Assert.AreEqual (3, db.Table<Issue96_A>().Where(p => p.ClassB == null).Count ());
		}

        public class Issue303_A
        {
            [PrimaryKey, NotNull]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Issue303_B
        {
            [PrimaryKey, NotNull]
            public int Id { get; set; }
            public bool Flag { get; set; }
        }

        [Test]
        public void Issue303_WhereNot_A()
        {
            using (var db = new TestDb())
            {
                db.CreateTable<Issue303_A>();
                db.Insert(new Issue303_A { Id = 1, Name = "aa" });
                db.Insert(new Issue303_A { Id = 2, Name = null });
                db.Insert(new Issue303_A { Id = 3, Name = "test" });
                db.Insert(new Issue303_A { Id = 4, Name = null });

                var r = (from p in db.Table<Issue303_A>() where !(p.Name == null) select p).ToList();
                Assert.AreEqual(2, r.Count);
                Assert.AreEqual(1, r[0].Id);
                Assert.AreEqual(3, r[1].Id);
            }
        }

        [Test]
        public void Issue303_WhereNot_B()
        {
            using (var db = new TestDb())
            {
                db.CreateTable<Issue303_B>();
                db.Insert(new Issue303_B { Id = 1, Flag = true });
                db.Insert(new Issue303_B { Id = 2, Flag = false });
                db.Insert(new Issue303_B { Id = 3, Flag = true });
                db.Insert(new Issue303_B { Id = 4, Flag = false });

                var r = (from p in db.Table<Issue303_B>() where !p.Flag select p).ToList();
                Assert.AreEqual(2, r.Count);
                Assert.AreEqual(2, r[0].Id);
                Assert.AreEqual(4, r[1].Id);
            }
        }

		[Test]
		public void QuerySelectAverage ()
		{
			var db = CreateDb ();

			db.Insert (new Product {
				Name = "A",
				Price = 20,
				TotalSales = 100,
			});

			db.Insert (new Product {
				Name = "B",
				Price = 10,
				TotalSales = 100,
			});

			db.Insert (new Product {
				Name = "C",
				Price = 1000,
				TotalSales = 1,
			});

			var r = db.Table<Product> ().Where (x => x.TotalSales > 50).Select (s => s.Price).Average ();

			Assert.AreEqual (15m, r);
		}

		interface IEntity
		{
			int Id { get; set; }
			string Value { get; set; }
		}

		class Entity : IEntity
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }
			public string Value { get; set; }
		}

		static T GetEntity<T> (TestDb db, int id) where T : IEntity, new ()
		{
			return db.Table<T> ().FirstOrDefault (x => x.Id == id);
		}

		[Test]
		public void CastedParameters ()
		{
			var db = CreateDb ();
			db.CreateTable<Entity> ();

			db.Insert (new Entity {
				Value = "Foo",
			});

			var r = GetEntity<Entity> (db, 1);

			Assert.AreEqual ("Foo", r.Value);
		}

		[Test]
		public void Issue460_ReplaceWith2Args ()
		{
			var db = CreateDb ();
			db.Trace = true;
			//db.Tracer = Console.WriteLine;

			db.Insert (new Product {
				Name = "I am not B X B",
			});
			db.Insert (new Product {
				Name = "I am B O B",
			});

			var cl = (from c in db.Table<Product> ()
			          where c.Name.Replace (" ", "").Contains ("BOB")
					  select c).FirstOrDefault ();

			Assert.AreEqual (2, cl.Id);
			Assert.AreEqual ("I am B O B", cl.Name);
		}
	}
}
