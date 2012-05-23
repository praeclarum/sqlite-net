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
    public class StringQueryTest
	{
		TestDb db;

#if NETFX_CORE
        [TestInitialize]
#else
        [TestFixtureSetUp]
#endif
        public void SetUp ()
		{
			db = new TestDb ();
			db.CreateTable<Product> ();
			
			var prods = new[] {
				new Product { Name = "Foo" },
				new Product { Name = "Bar" },
				new Product { Name = "Foobar" },
			};
			
			db.InsertAll (prods);
		}

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        public void StartsWith()
		{
			var fs = db.Table<Product> ().Where (x => x.Name.StartsWith ("F")).ToList ();
			Assert.AreEqual (2, fs.Count);
			
			var bs = db.Table<Product> ().Where (x => x.Name.StartsWith ("B")).ToList ();
			Assert.AreEqual (1, bs.Count);
		}

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        public void EndsWith()
		{
			var fs = db.Table<Product> ().Where (x => x.Name.EndsWith ("ar")).ToList ();
			Assert.AreEqual (2, fs.Count);
			
			var bs = db.Table<Product> ().Where (x => x.Name.EndsWith ("o")).ToList ();
			Assert.AreEqual (1, bs.Count);
		}

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        public void Contains()
		{
			var fs = db.Table<Product> ().Where (x => x.Name.Contains ("o")).ToList ();
			Assert.AreEqual (2, fs.Count);
			
			var bs = db.Table<Product> ().Where (x => x.Name.Contains ("a")).ToList ();
			Assert.AreEqual (2, bs.Count);
		}
	}
}
