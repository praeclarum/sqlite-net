using System;
using System.Linq;
#if !__IOS__
using Autofac;
using Autofac.Core;
#endif
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
#if !__IOS__
	    private IContainer _container;
#endif
	    private DefaultContractResolver _contractResolver;

		class NoPropObject
        {
        }

		[SetUp]
	    public void Setup ()
		{
#if !__IOS__
			var cb = new Autofac.ContainerBuilder ();
			cb.RegisterType<Account> ().As<IAccount> ();

			_container = cb.Build ();

			_contractResolver = new DefaultContractResolver {
				CanCreate = type => _container.IsRegistered (type),
				Create = (type, args) => _container.IsRegistered(type) ? _container.Resolve (type) : Activator.CreateInstance(type, args)
			};
#else
			_contractResolver = new DefaultContractResolver();
#endif
		}

		[TearDown]
	    public void TearDown ()
	    {

	    }

        [Test, ExpectedException]
        public void CreateTypeWithNoProps ()
        {
	        var db = new TestDb {
		        Resolver = _contractResolver
	        };

	        db.CreateTable<NoPropObject> ();
        }

        [Test]
        public void CreateThem ()
        {
	        var db = new TestDb {
		        Resolver = _contractResolver
	        };

			db.CreateTable<IAccount> ();
            db.CreateTable<Product> ();
            db.CreateTable<Order> ();
            db.CreateTable<OrderLine> ();
            db.CreateTable<OrderHistory> ();
            
            VerifyCreations(db);
        }

        [Test]
        public void CreateAsPassedInTypes ()
        {
	        var db = new TestDb {
		        Resolver = _contractResolver
	        };

			db.CreateTable(typeof(IAccount));
            db.CreateTable(typeof(Product));
            db.CreateTable(typeof(Order));
            db.CreateTable(typeof(OrderLine));
            db.CreateTable(typeof(OrderHistory));

            VerifyCreations(db);
        }

        [Test]
        public void CreateTwice ()
        {
	        var db = new TestDb {
		        Resolver = _contractResolver
	        };

			db.CreateTable<IAccount> ();
            db.CreateTable<Product> ();
            db.CreateTable<OrderLine> ();
            db.CreateTable<Order> ();
            db.CreateTable<OrderLine> ();
            db.CreateTable<OrderHistory> ();
            
            VerifyCreations(db);
        }
        
        private static void VerifyCreations(TestDb db)
        {
	        var account = db.GetMapping<IAccount> ();
			Assert.AreEqual(3, account.Columns.Length);

			var a = new Account() {
				Name = $"Account Created By Unit Test On {DateTime.Now.ToLongDateString()}",
				CreatedOn = DateTime.Now
			};

	        db.Insert (a);
	        var ao = db.Table<IAccount>().First();
			Assert.AreEqual(ao.Name, a.Name);

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

        class Issue115_MyObject
        {
            [PrimaryKey]
            public string UniqueId { get; set; }
            public byte OtherValue { get; set; }
        }

        [Test]
        public void Issue115_MissingPrimaryKey ()
        {
            using (var conn = new TestDb ()) {

                conn.CreateTable<Issue115_MyObject> ();
                conn.InsertAll (from i in Enumerable.Range (0, 10) select new Issue115_MyObject {
                    UniqueId = i.ToString (),
                    OtherValue = (byte)(i * 10),
                });

                var query = conn.Table<Issue115_MyObject> ();
                foreach (var itm in query) {
                    itm.OtherValue++;
                    Assert.AreEqual (1, conn.Update (itm, typeof(Issue115_MyObject)));
                }
            }
        }

        [Table("WantsNoRowId", WithoutRowId = true)]
        class WantsNoRowId
        {
            [PrimaryKey]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Table("sqlite_master")]
        class SqliteMaster
        {
            [Column ("type")]
            public string Type { get; set; }

            [Column ("name")]
            public string Name { get; set; }

            [Column ("tbl_name")]
            public string TableName { get; set; }

            [Column ("rootpage")]
            public int RootPage { get; set; }

            [Column ("sql")]
            public string Sql { get; set; }
        }

        [Test]
        public void WithoutRowId ()
        {
            using(var conn = new TestDb ())
            {
                conn.CreateTable<OrderLine> ();
                var info = conn.Table<SqliteMaster>().Where(m => m.TableName=="OrderLine").First ();
                Assert.That (!info.Sql.Contains ("without rowid"));
                
                conn.CreateTable<WantsNoRowId> ();
                info = conn.Table<SqliteMaster>().Where(m => m.TableName=="WantsNoRowId").First ();
                Assert.That (info.Sql.Contains ("without rowid"));
            }
        }
    }
}
