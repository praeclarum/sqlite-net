using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
	[TestFixture]
	public class UnicodeTest
	{
		[Table("\u7523\u54C1")]
		public class UnicodeProduct
		{
			[AutoIncrement, PrimaryKey, Column("\u6A19\u8B58")]
			public int Id { get; set; }

			[Column("\u540D")]
			public string Name { get; set; }

			[Column("\u5024")]
			public decimal Price { get; set; }

			[Column("\u53CE\u76CA")]
			public uint TotalSales { get; set; }
		}

		[Test]
		public void Insert()
		{
			var db = new TestDb();

			db.CreateTable<UnicodeProduct>();

			string testString = "\u2329\u221E\u232A";

			db.Insert(new UnicodeProduct
			{
				Name = testString,
			});

			var p = db.Get<UnicodeProduct>(1);

			Assert.AreEqual(testString, p.Name);
		}

		[Test]
		public void Query()
		{
			var db = new TestDb();

			db.CreateTable<UnicodeProduct>();

			string testString = "\u2329\u221E\u232A";

			db.Insert(new UnicodeProduct
			{
				Name = testString,
			});

			var ps = (from p in db.Table<UnicodeProduct>() where p.Name == testString select p).ToList();

			Assert.AreEqual(1, ps.Count);
			Assert.AreEqual(testString, ps[0].Name);
		}
	}
}