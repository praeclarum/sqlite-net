
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
            var obj1 = new EncryptedObj() { Text = "Sensitive Data" };
            _db.Insert(obj1);

            var updObj = _db.Table<EncryptedObj>().First();
            updObj.Text = "Updated Sensitive Data";
            _db.Update(updObj);

            var actual = _db.Table<EncryptedObj>().First();
            Assert.AreEqual("Updated Sensitive Data", actual.Text);

        }
    }
}
