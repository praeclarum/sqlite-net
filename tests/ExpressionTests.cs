using System.Collections.Generic;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class ExpressionTests
    {
        [Table("AGoodTableName")]
        private class TestTable
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }
        }


        [Test]
        public void ToLower()
        {
            var db = new TestDb();

            db.CreateTable<TestTable>();
            var testTable = new TestTable()
            {
                Name = "TEST"
            };
            db.Insert(testTable);

            var x = db.Table<TestTable>().Where(t => t.Name.ToLower() == "test");

            Assert.AreEqual(1, x.Count());
        }

        [Test]
        public void ToUpper()
        {
            var db = new TestDb();

            db.CreateTable<TestTable>();
            var testTable = new TestTable()
            {
                Name = "test"
            };
            db.Insert(testTable);

            var x = db.Table<TestTable>().Where(t => t.Name.ToUpper() == "TEST");

            Assert.AreEqual(1, x.Count());
        }
    }
}