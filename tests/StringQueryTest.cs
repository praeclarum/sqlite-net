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
		public void StringEquals ()
		{
			// C#: x => x.Name == "Foo"
			var fs = db.Table<Product>().Where(x => x.Name == "Foo").ToList();
			Assert.AreEqual(1, fs.Count);

#if !NO_VB
			// VB: Function(x) x.Name = "Foo"
			var param = System.Linq.Expressions.Expression.Parameter(typeof(Product), "x");
			var expr = System.Linq.Expressions.Expression.Lambda<Func<Product, bool>>(
				System.Linq.Expressions.Expression.Equal(
					System.Linq.Expressions.Expression.Call(
						typeof(Microsoft.VisualBasic.CompilerServices.Operators).GetMethod("CompareString"),
						System.Linq.Expressions.Expression.MakeMemberAccess(param, typeof(Product).GetMember("Name").First()),
						System.Linq.Expressions.Expression.Constant("Foo"),
						System.Linq.Expressions.Expression.Constant(false)), // Option Compare Binary (false) or Text (true)
					System.Linq.Expressions.Expression.Constant(0)),
				param);
			var bs = db.Table<Product>().Where(expr).ToList();
			Assert.AreEqual(1, bs.Count);
#endif
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
