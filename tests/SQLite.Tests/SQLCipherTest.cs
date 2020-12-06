using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Net.Sockets;

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

		[SetUp]
		public void Setup ()
		{
			// open an in memory connection and reset SQLCipher default pragma settings
			using (var c = new SQLiteConnection (":memory:", true)) {
				c.Execute ("PRAGMA cipher_default_use_hmac = ON;");
			}
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

		[Test]
		public void SetPreKeyAction ()
		{
			var path = TestPath.GetTempFileName ();
			var key = "SecretKey";

			using (var db = new SQLiteConnection (new SQLiteConnectionString (path, true, key,
				preKeyAction: conn => conn.Execute ("PRAGMA page_size = 8192;")))) {
				db.CreateTable<TestTable> ();
				db.Insert (new TestTable { Value = "Secret Value" });
				Assert.AreEqual ("8192", db.ExecuteScalar<string> ("PRAGMA page_size;"));
			}
		}

		[Test]
		public void SetPostKeyAction ()
		{
			var path = TestPath.GetTempFileName ();
			var key = "SecretKey";

			using (var db = new SQLiteConnection (new SQLiteConnectionString (path, true, key,
				postKeyAction: conn => conn.Execute ("PRAGMA page_size = 512;")))) {
				db.CreateTable<TestTable> ();
				db.Insert (new TestTable { Value = "Secret Value" });
				Assert.AreEqual ("512", db.ExecuteScalar<string> ("PRAGMA page_size;"));
			}
		}

		[Test]
		public void CheckJournalModeForNonKeyed ()
		{
			using (var db = new TestDb ()) {
				db.CreateTable<TestTable> ();
				Assert.AreEqual ("wal", db.ExecuteScalar<string> ("PRAGMA journal_mode;"));
			}
		}

		class TestData
		{
			[PrimaryKey, AutoIncrement]
			public int Id { get; set; }

			public string Stuff { get; set; } = "";
		}

		[Test]
		public void CanOpenV3 ()
		{
			// Issue #655
			// SQLCipher switched defaults from v3 to v4
			// Cannot load v3 without some action:
			// 1. Migrating the DB
			// 2. Compatibility mode

			var resName = "SQLite.Tests.EncryptedV3.sqlite";

			// Encrypted DBs with v1.6 cannot be opened in v1.7
			var tempPath = System.IO.Path.GetTempFileName ();
			var res = GetType ().Assembly.GetManifestResourceNames ();
			using (var ins = GetType ().Assembly.GetManifestResourceStream (resName))
			using (var outs = new System.IO.FileStream (tempPath, FileMode.Create, FileAccess.Write)) {
				ins.CopyTo (outs);
			}

			//var path = "/Users/fak/Projects/Repro655/MakeDB/Version1.6.sqlite";
			var path = tempPath;

			var key = "FRANK";
			var cstring = new SQLiteConnectionString (
				path,
				storeDateTimeAsTicks: true,
				key: key,
				//preKeyAction: c => {
				//	c.Execute ("PRAGMA cipher_compatibility = 3");
				//}
				postKeyAction: c => {
					//var res = c.ExecuteScalar<int> ("PRAGMA cipher_migrate");
					c.Execute ("PRAGMA cipher_compatibility = 3");
				}
				);
			Console.WriteLine ("Copied to " + path);

			using (var db = new SQLiteConnection (cstring)) {

				//Console.ReadLine ();

				db.CreateTable<TestData> ();

				var results = db.Table<TestData> ().ToList ();

				Assert.AreEqual ("Hello Chatroom!", results[0].Stuff);
			}

			//
			// Assert the database has not changed
			//
			var md5 = MD5.Create ();
			var origMD5 = new byte[0];
			using (var ins = GetType ().Assembly.GetManifestResourceStream (resName)) {
				origMD5 = md5.ComputeHash (ins);
			}
			var newMD5 = md5.ComputeHash (File.ReadAllBytes (tempPath));
			Assert.True (origMD5.SequenceEqual (newMD5));
		}
	}
}
