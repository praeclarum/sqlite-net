﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
	public class GuidAsBlobsTests
	{
		public class TestObj
		{
			[PrimaryKey]
			public Guid Id { get; set; }
			public String Text { get; set; }

			public override string ToString()
			{
				return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
			}

		}

		public class TestDb : SQLiteConnection
		{
			public TestDb(string path)
				: base(new SQLiteConnectionString (path, storeGuidsAsBlobs: true))
			{
				CreateTable<TestObj>();
			}
		}

		[Test]
		public void ShouldPersistAndReadGuid()
		{
			var db = new TestDb(TestPath.GetTempFileName());

			var obj1 = new TestObj() { Id = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"), Text = "First Guid Object" };
			var obj2 = new TestObj() { Id = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"), Text = "Second Guid Object" };

			var numIn1 = db.Insert(obj1);
			var numIn2 = db.Insert(obj2);
			Assert.AreEqual(1, numIn1);
			Assert.AreEqual(1, numIn2);

			var result = db.Query<TestObj>("select * from TestObj").ToList();
			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(obj1.Text, result[0].Text);
			Assert.AreEqual(obj2.Text, result[1].Text);

			Assert.AreEqual(obj1.Id, result[0].Id);
			Assert.AreEqual(obj2.Id, result[1].Id);

			db.Close();
		}
		[Test]
		public void ShouldQueryGuidCorrectly()
		{
			var db = new TestDb(TestPath.GetTempFileName());

			var obj1 = new TestObj() { Id = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"), Text = "First Guid Object" };
			var obj2 = new TestObj() { Id = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"), Text = "Second Guid Object" };

			var numIn1 = db.Insert(obj1);
			var numIn2 = db.Insert(obj2);
			Assert.AreEqual(1, numIn1);
			Assert.AreEqual(1, numIn2);

			var result = db.Query<TestObj>("select * from TestObj where id = ?", obj2.Id).ToList();
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(obj2.Text, result[0].Text);

			Assert.AreEqual(obj2.Id, result[0].Id);

			db.Close();
		}

		[Test]
		public void ShouldQueryGuidCorrectlyUsingLinq()
		{
			var db = new TestDb(TestPath.GetTempFileName());

			var obj1 = new TestObj() { Id = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"), Text = "First Guid Object" };
			var obj2 = new TestObj() { Id = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"), Text = "Second Guid Object" };

			var numIn1 = db.Insert(obj1);
			var numIn2 = db.Insert(obj2);
			Assert.AreEqual(1, numIn1);
			Assert.AreEqual(1, numIn2);

			var result = db.Table<TestObj>().Where(to => to.Id == obj2.Id).ToList();
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(obj2.Text, result[0].Text);

			Assert.AreEqual(obj2.Id, result[0].Id);

			db.Close();
		}
		
		[Test]
		public async Task ShouldQueryGuidCorrectlyAsync()
		{

			var db = new SQLiteAsyncConnection(TestPath.GetTempFileName(), storeGuidsAsBlobs: true);
			await db.CreateTableAsync<TestObj>(CreateFlags.AutoIncPK);

			var obj1 = new TestObj() { Id = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"), Text = "First Guid Object" };
			var obj2 = new TestObj() { Id = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"), Text = "Second Guid Object" };

			var numIn1 = await db.InsertAsync(obj1);
			var numIn2 = await db.InsertAsync(obj2);
			Assert.AreEqual(1, numIn1);
			Assert.AreEqual(1, numIn2);

			var result = (await db.QueryAsync<TestObj>("select * from TestObj where id = ?", obj2.Id)).ToList();
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(obj2.Text, result[0].Text);

			Assert.AreEqual(obj2.Id, result[0].Id);

			await db.CloseAsync();
		}

		[Test]
		public async Task ShouldQueryGuidCorrectlyUsingLinqAsync()
		{

			var db = new SQLiteAsyncConnection(TestPath.GetTempFileName(), storeGuidsAsBlobs: true);
			await db.CreateTableAsync<TestObj>(CreateFlags.AutoIncPK);

			var obj1 = new TestObj() { Id = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"), Text = "First Guid Object" };
			var obj2 = new TestObj() { Id = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"), Text = "Second Guid Object" };

			var numIn1 = await db.InsertAsync(obj1);
			var numIn2 = await db.InsertAsync(obj2);
			Assert.AreEqual(1, numIn1);
			Assert.AreEqual(1, numIn2);

			var result = await db.Table<TestObj>().Where(to => to.Id == obj2.Id).ToListAsync();
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(obj2.Text, result[0].Text);

			Assert.AreEqual(obj2.Id, result[0].Id);

			await db.CloseAsync();
		}


		[Test]
		public void AutoGuid_HasGuid()
		{
			var db = new SQLiteConnection(new SQLiteConnectionString (TestPath.GetTempFileName(), storeGuidsAsBlobs: true));
			db.CreateTable<TestObj>(CreateFlags.AutoIncPK);

			var guid1 = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623");
			var guid2 = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6");

			var obj1 = new TestObj() { Id = guid1, Text = "First Guid Object" };
			var obj2 = new TestObj() { Id = guid2, Text = "Second Guid Object" };

			var numIn1 = db.Insert(obj1);
			var numIn2 = db.Insert(obj2);
			Assert.AreEqual(guid1, obj1.Id);
			Assert.AreEqual(guid2, obj2.Id);

			db.Close();
		}

		[Test]
		public void AutoGuid_EmptyGuid()
		{
			var db = new SQLiteConnection(new SQLiteConnectionString (TestPath.GetTempFileName(), storeGuidsAsBlobs: true));
			db.CreateTable<TestObj>(CreateFlags.AutoIncPK);

			var guid1 = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623");
			var guid2 = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6");

			var obj1 = new TestObj() { Text = "First Guid Object" };
			var obj2 = new TestObj() { Text = "Second Guid Object" };

			Assert.AreEqual(Guid.Empty, obj1.Id);
			Assert.AreEqual(Guid.Empty, obj2.Id);

			var numIn1 = db.Insert(obj1);
			var numIn2 = db.Insert(obj2);
			Assert.AreNotEqual(Guid.Empty, obj1.Id);
			Assert.AreNotEqual(Guid.Empty, obj2.Id);
			Assert.AreNotEqual(obj1.Id, obj2.Id);

			db.Close();
		}
	}
}
