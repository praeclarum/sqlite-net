using System;
using System.Collections.Generic;
using System.Linq;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TearDown = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestCleanupAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
	[TestFixture]
	public class TransactionTest
	{
		private TestDb db;
		private List<TestObj> testObjects;

		public class TestObj
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }

			public override string ToString()
			{
				return string.Format("[TestObj: Id={0}]", Id);
			}
		}

		public class TransactionTestException : Exception
		{
		}

		public class TestDb : SQLiteConnection
		{
			public TestDb(String path) : base(path)
			{
				CreateTable<TestObj>();
			}
		}

		[SetUp]
		public void Setup()
		{
			testObjects = Enumerable.Range(1, 20).Select(i => new TestObj()).ToList();

			db = new TestDb(TestPath.GetTempFileName());
			db.InsertAll(testObjects);
		}

		[TearDown]
		public void TearDown()
		{
			if (db != null) {
				db.Close();
			}
		}

		[Test]
		public void SuccessfulSavepointTransaction()
		{
			db.RunInTransaction(() => {
				db.Delete(testObjects[0]);
				db.Delete(testObjects[1]);
				db.Insert(new TestObj());
			});

			Assert.AreEqual(testObjects.Count - 1, db.Table<TestObj>().Count());
		}

		[Test]
		public void FailSavepointTransaction()
		{
			try {
				db.RunInTransaction(() => {
					db.Delete(testObjects[0]);

					throw new TransactionTestException();
				});
			} catch (TransactionTestException) {
				// ignore
			}

			Assert.AreEqual(testObjects.Count, db.Table<TestObj>().Count());
		}

		[Test]
		public void SuccessfulNestedSavepointTransaction()
		{
			db.RunInTransaction(() => {
				db.Delete(testObjects[0]);

				db.RunInTransaction(() => {
					db.Delete(testObjects[1]);
				});
			});

			Assert.AreEqual(testObjects.Count - 2, db.Table<TestObj>().Count());
		}

		[Test]
		public void FailNestedSavepointTransaction()
		{
			try {
				db.RunInTransaction(() => {
					db.Delete(testObjects[0]);

					db.RunInTransaction(() => {
						db.Delete(testObjects[1]);

						throw new TransactionTestException();
					});
				});
			} catch (TransactionTestException) {
				// ignore
			}

			Assert.AreEqual(testObjects.Count, db.Table<TestObj>().Count());
		}
	}
}

