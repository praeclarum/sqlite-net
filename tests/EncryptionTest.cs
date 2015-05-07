﻿
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TearDown = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestCleanupAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

using System.Diagnostics;

namespace SQLite.Tests
{    
    [TestFixture]
    public class EncryptionTest
    {

        private TestDb _db;

        public class EncryptedObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
            [Encrypt]
            public String Text { get; set; }

        }
        public class InvalidObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
            [Encrypt]
            public int NotAString { get; set; }

        }

        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(path)
            {

                CreateTable<EncryptedObj>();
            }
        }

        [SetUp]
        public void Setup()
        {
            _db = new TestDb(TestPath.GetTempFileName());
        }
        [TearDown]
        public void TearDown()
        {
            if (_db != null) _db.Close();
        }

        [Test]
        public void EncryptionInsert()
        {
            SQLite.Encryption.Provider = new TestEncryptionProvider();

            var obj1 = new EncryptedObj() { Text = "Sensitive Data" };
            _db.Insert(obj1);

            var result = _db.Query<EncryptedObj>("select * from EncryptedObj").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(obj1.Text, result[0].Text);

            var result1 = _db.Get<EncryptedObj>(result[0].Id);
            Assert.AreEqual(obj1.Text, result1.Text);

            var result2 = _db.Table<EncryptedObj>().First();
            Assert.AreEqual(obj1.Text, result2.Text);


        }

        [Test]
        public void EncryptionUpdate()
        {
            SQLite.Encryption.Provider = new TestEncryptionProvider();

            var obj1 = new EncryptedObj() { Text = "Sensitive Data" };
            _db.Insert(obj1);

            var updObj = _db.Table<EncryptedObj>().First();
            updObj.Text = "Updated Sensitive Data";
            _db.Update(updObj);

            var actual = _db.Table<EncryptedObj>().First();
            Assert.AreEqual("Updated Sensitive Data", actual.Text);

        }

        [Test]
        public void EncryptionInvalidType()
        {
            Assert.ThrowsException<Exception>(() => _db.CreateTable<InvalidObj>());
        }

        [Test]
        public void EncryptMissingProvider()
        {
            SQLite.Encryption.Provider = null;

            Assert.ThrowsException<Exception>(() => 
            {
                var obj1 = new EncryptedObj() { Text = "Sensitive Data" };
                _db.Insert(obj1);
            });
        }

        [Test]
        public void EncryptWinRTCrypto()
        {
            SQLiteWinRTCryptoProvider cryptoProvider = new SQLiteWinRTCryptoProvider("NEVER_STORE_YOUR_KEY_IN_CODE");
            
            // Test just the provider
            string expected = "Sensitive Data Which Needs Protected";
            string actual = cryptoProvider.EncryptString(expected);
            
            Assert.AreNotEqual(expected, actual);

            actual = cryptoProvider.DecryptString(actual);
            Assert.AreEqual(expected, actual);

            // Now with SQLite
            SQLite.Encryption.Provider = cryptoProvider;
            var obj1 = new EncryptedObj() { Text = expected };
            _db.Insert(obj1);

            var actualClear = _db.Table<EncryptedObj>().First();
            Assert.AreEqual(expected, actualClear.Text);

            // Now test that it is encrypted
            SQLite.Encryption.Provider = new PassthroughEncryptionProvider();
            actualClear = _db.Table<EncryptedObj>().First();
            Assert.AreNotEqual(expected, actualClear.Text);

        }
    }

    public class TestEncryptionProvider : IEncryptionProvider
    {

        public string EncryptString(string value)
        {
            return "E" + value;
        }

        public string DecryptString(string value)
        {
            return value.Substring(1);
        }
    }

    public class PassthroughEncryptionProvider : IEncryptionProvider
    {

        public string EncryptString(string value)
        {
            return value;
        }

        public string DecryptString(string value)
        {
            return value;
        }
    }

}
