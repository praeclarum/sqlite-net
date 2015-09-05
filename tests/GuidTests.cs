using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class GuidTests
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
            public TestDb(String path)
                : base(new SQLitePlatformTest(), path)
            {
                CreateTable<TestObj>();
            }
        }

        [Test]
        public void AutoGuid_EmptyGuid()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            db.CreateTable<TestObj>(CreateFlags.AutoIncPK);

            var guid1 = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623");
            var guid2 = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6");

            var obj1 = new TestObj
            {
                Text = "First Guid Object"
            };
            var obj2 = new TestObj
            {
                Text = "Second Guid Object"
            };

            Assert.AreEqual(Guid.Empty, obj1.Id);
            Assert.AreEqual(Guid.Empty, obj2.Id);

            int numIn1 = db.Insert(obj1);
            int numIn2 = db.Insert(obj2);
            Assert.AreNotEqual(Guid.Empty, obj1.Id);
            Assert.AreNotEqual(Guid.Empty, obj2.Id);
            Assert.AreNotEqual(obj1.Id, obj2.Id);

            db.Close();
        }

        [Test]
        public void AutoGuid_HasGuid()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            db.CreateTable<TestObj>(CreateFlags.AutoIncPK);

            var guid1 = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623");
            var guid2 = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6");

            var obj1 = new TestObj
            {
                Id = guid1,
                Text = "First Guid Object"
            };
            var obj2 = new TestObj
            {
                Id = guid2,
                Text = "Second Guid Object"
            };

            int numIn1 = db.Insert(obj1);
            int numIn2 = db.Insert(obj2);
            Assert.AreEqual(guid1, obj1.Id);
            Assert.AreEqual(guid2, obj2.Id);

            db.Close();
        }

        [Test]
        public void ShouldPersistAndReadGuid()
        {
            var db = new TestDb(TestPath.CreateTemporaryDatabase());

            var obj1 = new TestObj
            {
                Id = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"),
                Text = "First Guid Object"
            };
            var obj2 = new TestObj
            {
                Id = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"),
                Text = "Second Guid Object"
            };

            int numIn1 = db.Insert(obj1);
            int numIn2 = db.Insert(obj2);
            Assert.AreEqual(1, numIn1);
            Assert.AreEqual(1, numIn2);

            List<TestObj> result = db.Query<TestObj>("select * from TestObj").ToList();
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(obj1.Text, result[0].Text);
            Assert.AreEqual(obj2.Text, result[1].Text);

            Assert.AreEqual(obj1.Id, result[0].Id);
            Assert.AreEqual(obj2.Id, result[1].Id);

            db.Close();
        }
    }
}