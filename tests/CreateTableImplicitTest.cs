using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class CreateTableImplicitTest
    {
        private class NoAttributes
        {
            public int Id { get; set; }
            public string AColumn { get; set; }
            public int IndexedId { get; set; }
        }

        private class PkAttribute
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
                var na = new NoAttributes {Id = i, AColumn = i.ToString(), IndexedId = 0};
                db.Insert(na);
            }
            var item = db.Get<NoAttributes>(2);
            Assert.IsNotNull(item);
            Assert.AreEqual(2, item.Id);
        }


        [Test]
        public void ImplicitAutoInc()
        {
            var db = new TestDb();

            db.CreateTable<PkAttribute>(CreateFlags.AutoIncPK);

            TableMapping mapping = db.GetMapping<PkAttribute>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsTrue(mapping.PK.IsAutoInc);
        }

        [Test]
        public void ImplicitAutoIncAsPassedInTypes()
        {
            var db = new TestDb();

            db.CreateTable(typeof (PkAttribute), CreateFlags.AutoIncPK);

            TableMapping mapping = db.GetMapping<PkAttribute>();

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

            TableMapping mapping = db.GetMapping<NoAttributes>();
            TableMapping.Column column = mapping.Columns[2];
            Assert.AreEqual("IndexedId", column.Name);
            Assert.IsTrue(column.Indices.Any());
        }

        [Test]
        public void ImplicitPK()
        {
            var db = new TestDb();

            db.CreateTable<NoAttributes>(CreateFlags.ImplicitPK);

            TableMapping mapping = db.GetMapping<NoAttributes>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsFalse(mapping.PK.IsAutoInc);

            CheckPK(db);
        }

        [Test]
        public void ImplicitPKAutoInc()
        {
            var db = new TestDb();

            db.CreateTable(typeof (NoAttributes), CreateFlags.ImplicitPK | CreateFlags.AutoIncPK);

            TableMapping mapping = db.GetMapping<NoAttributes>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsTrue(mapping.PK.IsAutoInc);
        }

        [Test]
        public void ImplicitPKAutoIncAsPassedInTypes()
        {
            var db = new TestDb();

            db.CreateTable(typeof (NoAttributes), CreateFlags.ImplicitPK | CreateFlags.AutoIncPK);

            TableMapping mapping = db.GetMapping<NoAttributes>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsTrue(mapping.PK.IsAutoInc);
        }

        [Test]
        public void ImplicitPkAsPassedInTypes()
        {
            var db = new TestDb();

            db.CreateTable(typeof (NoAttributes), CreateFlags.ImplicitPK);

            TableMapping mapping = db.GetMapping<NoAttributes>();

            Assert.IsNotNull(mapping.PK);
            Assert.AreEqual("Id", mapping.PK.Name);
            Assert.IsTrue(mapping.PK.IsPK);
            Assert.IsFalse(mapping.PK.IsAutoInc);
        }

        [Test]
        public void WithoutImplicitMapping()
        {
            var db = new TestDb();

            db.CreateTable<NoAttributes>();

            TableMapping mapping = db.GetMapping<NoAttributes>();

            Assert.IsNull(mapping.PK);

            TableMapping.Column column = mapping.Columns[2];
            Assert.AreEqual("IndexedId", column.Name);
            Assert.IsFalse(column.Indices.Any());

            Assert.Throws(typeof (AssertionException), () => CheckPK(db));
        }
    }
}