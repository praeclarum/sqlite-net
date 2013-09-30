using System.Collections.Generic;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class MappingTest
    {
        [Table("AGoodTableName")]
        private class AFunnyTableName
        {
            [PrimaryKey]
            public int Id { get; set; }

            [Column("AGoodColumnName")]
            public string AFunnyColumnName { get; set; }
        }


        [Table("foo")]
        public class Foo
        {
            [Column("baz")]
            public int Bar { get; set; }
        }

        [Test]
        public void HasGoodNames()
        {
            var db = new TestDb();

            db.CreateTable<AFunnyTableName>();

            TableMapping mapping = db.GetMapping<AFunnyTableName>();

            Assert.AreEqual("AGoodTableName", mapping.TableName);

            Assert.AreEqual("Id", mapping.Columns[0].Name);
            Assert.AreEqual("AGoodColumnName", mapping.Columns[1].Name);
        }

        [Test]
        public void Issue86()
        {
            var db = new TestDb();
            db.CreateTable<Foo>();

            db.Insert(new Foo {Bar = 42});
            db.Insert(new Foo {Bar = 69});

            Foo found42 = db.Table<Foo>().Where(f => f.Bar == 42).FirstOrDefault();
            Assert.IsNotNull(found42);

            var ordered = new List<Foo>(db.Table<Foo>().OrderByDescending(f => f.Bar));
            Assert.AreEqual(2, ordered.Count);
            Assert.AreEqual(69, ordered[0].Bar);
            Assert.AreEqual(42, ordered[1].Bar);
        }
    }
}