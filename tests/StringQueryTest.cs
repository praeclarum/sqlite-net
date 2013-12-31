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
	public class StringQueryTest
	{
		TestDb db;
		
		[SetUp]
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
		
		[Test]
		public void Contains ()
		{
			var fs = db.Table<Product> ().Where (x => x.Name.Contains ("o")).ToList ();
			Assert.AreEqual (2, fs.Count);
			
			var bs = db.Table<Product> ().Where (x => x.Name.Contains ("a")).ToList ();
			Assert.AreEqual (2, bs.Count);
		}
	}
}
