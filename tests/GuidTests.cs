#if SQLITE_SUPPORT_GUID
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SetUp = Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#else
using NUnit.Framework;
#endif


namespace SQLite.Tests {
    [TestFixture]
    public class GuidTests {
        public class TestObj {
            [PrimaryKey]
            public Guid Id { get; set; }
            public String Text { get; set; }

            public override string ToString() {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }

        }

        public class TestDb : SQLiteConnection {
            public TestDb(String path)
                : base(path) {
                CreateTable<TestObj>();
            }

        }

        [Test]
        public void ShouldPersistAndReadGuid() {
            var db = new TestDb(TestPath.GetTempFileName());

            var obj1 = new TestObj() { Id=new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"), Text = "First Guid Object" };
            var obj2 = new TestObj() {  Id=new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"), Text = "Second Guid Object" };

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
    }
}
#endif