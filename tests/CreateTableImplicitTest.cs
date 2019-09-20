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
    public class CreateTableImplicitTest
    {

        class NoAttributes
        {
            public int Id { get; set; }
            public string AColumn { get; set; }
            public int IndexedId { get; set; }
        }

		class NoAttributesNoOptions
		{
			public int Id { get; set; }
			public string AColumn { get; set; }
			public int IndexedId { get; set; }
		}

		class PkAttribute
        {
            [PrimaryKey]
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
        public void WithoutImplicitMapping ()
        {
            var db = new TestDb ();

            db.CreateTable<NoAttributesNoOptions>();

            var mapping = db.GetMapping<NoAttributesNoOptions> ();

            Assert.IsNull (mapping.PK, "Should not be a key");

            var column = mapping.Columns[2];
            Assert.AreEqual("IndexedId", column.Name);
            Assert.IsFalse(column.Indices.Any());
        }

        [Test]
        public void ImplicitPK()
        {
            var db = new TestDb();

            db.CreateTable<NoAttributes>(CreateFlags.ImplicitPK);

            var mapping = db.GetMapping<NoAttributes>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsFalse(mapping.PK.IsAutoInc);

            CheckPK(db);
        }


        [Test]
        public void ImplicitAutoInc()
        {
            var db = new TestDb();

            db.CreateTable<PkAttribute>(CreateFlags.AutoIncPK);

            var mapping = db.GetMapping<PkAttribute>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsTrue(mapping.PK.IsAutoInc);
        }

        [Test]
        public void ImplicitIndex()
        {
            var db = new TestDb();

            db.CreateTable<NoAttributes>(CreateFlags.ImplicitIndex);

            var mapping = db.GetMapping<NoAttributes>();
            var column = mapping.Columns[2];
            Assert.AreEqual("IndexedId", column.Name);
            Assert.IsTrue(column.Indices.Any());
        }

        [Test]
        public void ImplicitPKAutoInc()
        {
            var db = new TestDb();

            db.CreateTable(typeof(NoAttributes), CreateFlags.ImplicitPK | CreateFlags.AutoIncPK);

            var mapping = db.GetMapping<NoAttributes>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsTrue(mapping.PK.IsAutoInc);
        }

        [Test]
        public void ImplicitAutoIncAsPassedInTypes()
        {
            var db = new TestDb();

            db.CreateTable(typeof(PkAttribute), CreateFlags.AutoIncPK);

            var mapping = db.GetMapping<PkAttribute>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsTrue(mapping.PK.IsAutoInc);
        }

        [Test]
        public void ImplicitPkAsPassedInTypes()
        {
            var db = new TestDb();

            db.CreateTable(typeof(NoAttributes), CreateFlags.ImplicitPK);

            var mapping = db.GetMapping<NoAttributes>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsFalse(mapping.PK.IsAutoInc);
        }

        [Test]
        public void ImplicitPKAutoIncAsPassedInTypes()
        {
            var db = new TestDb();

            db.CreateTable(typeof(NoAttributes), CreateFlags.ImplicitPK | CreateFlags.AutoIncPK);

            var mapping = db.GetMapping<NoAttributes>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsTrue(mapping.PK.IsAutoInc);
        }
    }
}

