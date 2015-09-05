using System;
using NUnit.Framework;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class CollateTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public string CollateDefault { get; set; }

            [Collation("BINARY")]
            public string CollateBinary { get; set; }

            [Collation("RTRIM")]
            public string CollateRTrim { get; set; }

            [Collation("NOCASE")]
            public string CollateNoCase { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}]", Id);
            }
        }

        public class TestDb : SQLiteConnection
        {
            public TestDb(ISQLitePlatform sqlitePlatform, String path)
                : base(sqlitePlatform, path)
            {
                TraceListener = DebugTraceListener.Instance;
                CreateTable<TestObj>();
            }
        }

        [Test]
        public void Collate()
        {
            var obj = new TestObj
            {
                CollateDefault = "Alpha ",
                CollateBinary = "Alpha ",
                CollateRTrim = "Alpha ",
                CollateNoCase = "Alpha ",
            };

            var db = new TestDb(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());

            db.Insert(obj);

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateDefault == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateBinary == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateRTrim == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateRTrim == "ALPHA " select o).Count());
            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateRTrim == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateRTrim == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateNoCase == "Alpha " select o).Count());
            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateNoCase == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateNoCase == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateNoCase == "ALPHA" select o).Count());
        }

        /// <summary>
        /// A possible better way of specifying the type of the collation
        /// </summary>
        private enum CollationType
        {
            BINARY,
            RTRIM,
            NOCASE
        }

        /// <summary>
        /// A subtype of the collation attribute that allows the use of <see cref="CollationType"/>
        /// </summary>
        private class EasierCollationAttribute : CollationAttribute
        {
            public EasierCollationAttribute(CollationType type) : base(type.ToString())
            {
                
            }
        }

        public class TestObjWithSubtypedAttributes
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public string CollateDefault { get; set; }

            [EasierCollation(CollationType.BINARY)]
            public string CollateBinary { get; set; }

            [EasierCollation(CollationType.RTRIM)]
            public string CollateRTrim { get; set; }

            [EasierCollation(CollationType.NOCASE)]
            public string CollateNoCase { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}]", Id);
            }
        }

        public class TestDbSubtype : SQLiteConnection
        {
            public TestDbSubtype(ISQLitePlatform sqlitePlatform, String path)
                : base(sqlitePlatform, path)
            {
                TraceListener = DebugTraceListener.Instance;
                CreateTable<TestObjWithSubtypedAttributes>();
            }
        }

        /// <summary>
        /// The same test as before but with a subtyped collation attribute.
        /// </summary>
        [Test]
        public void CollateAttributeSubtype()
        {
            var obj = new TestObjWithSubtypedAttributes
            {
                CollateDefault = "Alpha ",
                CollateBinary = "Alpha ",
                CollateRTrim = "Alpha ",
                CollateNoCase = "Alpha ",
            };

            var db = new TestDbSubtype(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());

            db.Insert(obj);

            var testTable = db.Table<TestObjWithSubtypedAttributes>();
            Assert.AreEqual(1, (from o in testTable where o.CollateDefault == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in testTable where o.CollateDefault == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in testTable where o.CollateDefault == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in testTable where o.CollateDefault == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in testTable where o.CollateBinary == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in testTable where o.CollateBinary == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in testTable where o.CollateBinary == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in testTable where o.CollateBinary == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in testTable where o.CollateRTrim == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in testTable where o.CollateRTrim == "ALPHA " select o).Count());
            Assert.AreEqual(1, (from o in testTable where o.CollateRTrim == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in testTable where o.CollateRTrim == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in testTable where o.CollateNoCase == "Alpha " select o).Count());
            Assert.AreEqual(1, (from o in testTable where o.CollateNoCase == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in testTable where o.CollateNoCase == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in testTable where o.CollateNoCase == "ALPHA" select o).Count());
        }
    }
}