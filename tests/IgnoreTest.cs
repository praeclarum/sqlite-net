using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class IgnoredTest
    {
        public class DummyClass
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Foo { get; set; }
            public string Bar { get; set; }

            [Attributes.Ignore]
            public List<string> Ignored { get; set; }
        }

        [Test]
        public void NullableFloat()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            // if the Ignored property is not ignore this will cause an exception
            db.CreateTable<DummyClass>();
        }
    }
}