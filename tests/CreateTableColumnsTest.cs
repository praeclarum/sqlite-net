using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
    public class CreateTableColumnsTest
    {

        class NoAttributes
        {
            public int Id { get; set; }
            public string AColumn { get; set; }
            public int IndexedId { get; set; }
        }

        private void CheckPK(TestDb db)
        {
            for (int i = 1; i <= 10; i++)
            {
                var na = new NoAttributes { Id = i, AColumn = i.ToString(), IndexedId = 0 };
                db.Insert(na);
            }
            var item = db.Get<NoAttributes>(2);
            Assert.IsNotNull(item);
            Assert.AreEqual(2, item.Id);
        }

        [Test]
        public void ExplicitPK()
        {
            var db = new TestDb();

            db.CreateTable<NoAttributes>(CreateFlags.None, c => c.AddColumn(o => o.Id, isPK: true));

            var mapping = db.GetMapping<NoAttributes>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);

            CheckPK(db);
        }

        [Test]
        public void ExplicitIgnore()
        {
            var db = new TestDb();

            db.CreateTable<NoAttributes>(CreateFlags.None, c => c.AddColumn(o => o.AColumn, ignore: true));

            var mapping = db.GetMapping<NoAttributes>();
            var col = mapping.FindColumn("AColumn");
            Assert.IsNull(col);
        }

        [Test]
        public void ExplicitColumnName()
        {
            var db = new TestDb();

            db.CreateTable<NoAttributes>(CreateFlags.None, c => c.AddColumn(o => o.AColumn, columnName: "ThisColumn"));

            var mapping = db.GetMapping<NoAttributes>();
            var col = mapping.FindColumn("ThisColumn");
            Assert.IsNotNull(col);
            Assert.AreEqual("AColumn", col.PropertyName);
        }

        [Test]
        public void ExplicitMaxStringLength()
        {
            var db = new TestDb();

            db.CreateTable<NoAttributes>(CreateFlags.None, c => c.AddColumn(o => o.AColumn, maxStringLength: 4000));

            var mapping = db.GetMapping<NoAttributes>();
            var col = mapping.FindColumn("AColumn");
            Assert.IsNotNull(col);
            Assert.AreEqual(4000, col.MaxStringLength);
        }

        [Test]
        public void ExplicitIsNullable_True()
        {
            var db = new TestDb();

            db.CreateTable<NoAttributes>(CreateFlags.None, c => c.AddColumn(o => o.AColumn, isNullable: true));

            var mapping = db.GetMapping<NoAttributes>();
            var col = mapping.FindColumn("AColumn");
            Assert.IsNotNull(col);
            Assert.IsTrue(col.IsNullable);
        }

        [Test]
        public void ExplicitIsNullable_False()
        {
            var db = new TestDb();

            db.CreateTable<NoAttributes>(CreateFlags.None, c => c.AddColumn(o => o.AColumn, isNullable: false));

            var mapping = db.GetMapping<NoAttributes>();
            var col = mapping.FindColumn("AColumn");
            Assert.IsNotNull(col);
            Assert.IsFalse(col.IsNullable);
        }


        //[Test]
        //public void ImplicitIndex()
        //{
        //    var db = new TestDb();

        //    db.CreateTable<NoAttributes>(CreateFlags.ImplicitIndex);

        //    var mapping = db.GetMapping<NoAttributes>();
        //    var column = mapping.Columns[2];
        //    Assert.AreEqual("IndexedId", column.Name);
        //    Assert.IsTrue(column.Indices.Any());
        //    Assert.Fail("Not yet")
        //}

        [Test]
        public void ExplicitAutoInc()
        {
            var db = new TestDb();

            db.CreateTable<NoAttributes>(CreateFlags.None, c => c.AddColumn(o => o.Id, isPK: true, isAutoInc: true));
            var mapping = db.GetMapping<NoAttributes>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsTrue(mapping.PK.IsAutoInc);
        }
    }
}

