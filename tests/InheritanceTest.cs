using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class InheritanceTest
    {
        private class Base
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string BaseProp { get; set; }
        }

        private class Derived : Base
        {
            public string DerivedProp { get; set; }
        }


        [Test]
        public void InheritanceWorks()
        {
            var db = new TestDb();

            TableMapping mapping = db.GetMapping<Derived>();

            Assert.AreEqual(3, mapping.Columns.Length);
            Assert.AreEqual("Id", mapping.PK.Name);
        }
    }
}