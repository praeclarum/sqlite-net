using System;
using System.Linq;
using NUnit.Framework;

namespace SQLite.Tests
{
	[TestFixture]
	public class UnicodeTest
	{
		[Test]
		public void Insert ()
		{
			var db = new TestDb ();
			
			db.CreateTable<Product> ();
			
			string testString = "\u2329\u221E\u232A";
			
			db.Insert (new Product {
				Name = testString,
			});
			
			var p = db.Get<Product> (1);
			
			Assert.AreEqual (testString, p.Name);
		}
		
		[Test]
		public void Query ()
		{
			var db = new TestDb ();
			
			db.CreateTable<Product> ();
			
			string testString = "\u2329\u221E\u232A";
			
			db.Insert (new Product {
				Name = testString,
			});
			
			var ps = (from p in db.Table<Product> () where p.Name == testString select p).ToList ();
			
			Assert.AreEqual (1, ps.Count);
			Assert.AreEqual (testString, ps [0].Name);
		}
	}
}
