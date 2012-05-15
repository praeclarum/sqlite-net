using System;
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;

namespace SQLite.Tests
{
	[TestFixture]
	public class StringQueryTest
	{
		TestDb db;
		
		[TestFixtureSetUp]
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
		
		[Test]
		public void StartsWith ()
		{
			var fs = db.Table<Product> ().Where (x => x.Name.StartsWith ("F")).ToList ();
			Assert.AreEqual (2, fs.Count);
			
			var bs = db.Table<Product> ().Where (x => x.Name.StartsWith ("B")).ToList ();
			Assert.AreEqual (1, bs.Count);
		}
		
		[Test]
		public void EndsWith ()
		{
			var fs = db.Table<Product> ().Where (x => x.Name.EndsWith ("ar")).ToList ();
			Assert.AreEqual (2, fs.Count);
			
			var bs = db.Table<Product> ().Where (x => x.Name.EndsWith ("o")).ToList ();
			Assert.AreEqual (1, bs.Count);
		}
	}
}
