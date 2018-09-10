using System;
using System.Collections.Generic;
using System.Linq;

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
	public class SQLCipherTest
	{
		class TestTable
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }

			public string Value { get; set; }
		}

		[Test]
		public void SetStringKey ()
		{
			string path;

			var key = "SecretPassword";

			using (var db = new TestDb (key: key)) {
				path = db.DatabasePath;

				db.CreateTable<TestTable> ();
				db.Insert (new TestTable { Value = "Hello" });
			}

			using (var db = new TestDb (path, key: key)) {
				path = db.DatabasePath;

				var r = db.Table<TestTable> ().First ();

				Assert.AreEqual ("Hello", r.Value);
			}
		}

		[Test]
		public void SetBytesKey ()
		{
			string path;

			var rand = new Random ();
			var key = new byte[32];
			rand.NextBytes (key);

			using (var db = new TestDb (key: key)) {
				path = db.DatabasePath;

				db.CreateTable<TestTable> ();
				db.Insert (new TestTable { Value = "Hello" });
			}

			using (var db = new TestDb (path, key: key)) {
				path = db.DatabasePath;

				var r = db.Table<TestTable> ().First ();

				Assert.AreEqual ("Hello", r.Value);
			}
		}

		[Test]
		public void SetEmptyStringKey ()
		{
			using (var db = new TestDb (key: "")) {
			}
		}

		[Test]
		public void SetBadTypeKey ()
		{
			try {
				using (var db = new TestDb (key: 42)) {
				}
				Assert.Fail ("Should have thrown");
			}
			catch (ArgumentException) {
			}
		}

		[Test]
		public void SetBadBytesKey ()
		{
			try {
				using (var db = new TestDb (key: new byte[] { 1, 2, 3, 4 })) {
				}
				Assert.Fail ("Should have thrown");
			}
			catch (ArgumentException) {
			}
		}
	}
}
