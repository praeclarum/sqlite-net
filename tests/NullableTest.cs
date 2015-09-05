using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class NullableTest
    {
        public class NullableIntClass
        {
            [PrimaryKey, AutoIncrement]
            public int ID { get; set; }

            public int? NullableInt { get; set; }

            public override bool Equals(object obj)
            {
                var other = (NullableIntClass) obj;
                return ID == other.ID && NullableInt == other.NullableInt;
            }

            public override int GetHashCode()
            {
                return ID.GetHashCode() + NullableInt.GetHashCode();
            }
        }


        public class NullableFloatClass
        {
            [PrimaryKey, AutoIncrement]
            public int ID { get; set; }

            public float? NullableFloat { get; set; }

            public override bool Equals(object obj)
            {
                var other = (NullableFloatClass) obj;
                return ID == other.ID && NullableFloat == other.NullableFloat;
            }

            public override int GetHashCode()
            {
                return ID.GetHashCode() + NullableFloat.GetHashCode();
            }
        }


        public class StringClass
        {
            [PrimaryKey, AutoIncrement]
            public int ID { get; set; }

            //Strings are allowed to be null by default
            public string StringData { get; set; }

            public override bool Equals(object obj)
            {
                var other = (StringClass) obj;
                return ID == other.ID && StringData == other.StringData;
            }

            public override int GetHashCode()
            {
                return ID.GetHashCode() + StringData.GetHashCode();
            }
        }

        [Test]
        public void NullableScalarInt()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            db.CreateTable<NullableIntClass>();

            var withNull = new NullableIntClass
            {
                NullableInt = null
            };
            var with0 = new NullableIntClass
            {
                NullableInt = 0
            };
            var with1 = new NullableIntClass
            {
                NullableInt = 1
            };
            var withMinus1 = new NullableIntClass
            {
                NullableInt = -1
            };

            db.Insert(withNull);
            db.Insert(with0);
            db.Insert(with1);
            db.Insert(withMinus1);

            var actualShouldBeNull   = db.ExecuteScalar<int?>("select NullableInt from NullableIntClass order by ID limit 1");
            var actualShouldBe0      = db.ExecuteScalar<int?>("select NullableInt from NullableIntClass order by ID limit 1 offset 1");
            var actualShouldBe1      = db.ExecuteScalar<int?>("select NullableInt from NullableIntClass order by ID limit 1 offset 2");
            var actualShouldBeMinus1 = db.ExecuteScalar<int?>("select NullableInt from NullableIntClass order by ID limit 1 offset 3");

            Assert.AreEqual(null, actualShouldBeNull);
            Assert.AreEqual(0, actualShouldBe0);
            Assert.AreEqual(1, actualShouldBe1);
            Assert.AreEqual(-1, actualShouldBeMinus1);
        }

        [Test]
        public void NullableSumTest()
        {
            SQLiteConnection db = new TestDb();
            db.CreateTable<NullableIntClass>();

            var r = db.ExecuteScalar<int>("SELECT SUM(NullableInt) FROM NullableIntClass WHERE 1 = 0");

            Assert.AreEqual(0, r);
        }

        [Test]
        [Description("Create a table with a nullable int column then insert and select against it")]
        public void NullableFloat()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            db.CreateTable<NullableFloatClass>();

            var withNull = new NullableFloatClass
            {
                NullableFloat = null
            };
            var with0 = new NullableFloatClass
            {
                NullableFloat = 0
            };
            var with1 = new NullableFloatClass
            {
                NullableFloat = 1
            };
            var withMinus1 = new NullableFloatClass
            {
                NullableFloat = -1
            };

            db.Insert(withNull);
            db.Insert(with0);
            db.Insert(with1);
            db.Insert(withMinus1);

            NullableFloatClass[] results = db.Table<NullableFloatClass>().OrderBy(x => x.ID).ToArray();

            Assert.AreEqual(4, results.Length);

            Assert.AreEqual(withNull, results[0]);
            Assert.AreEqual(with0, results[1]);
            Assert.AreEqual(with1, results[2]);
            Assert.AreEqual(withMinus1, results[3]);
        }

        [Test]
        [Description("Create a table with a nullable int column then insert and select against it")]
        public void NullableInt()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            db.CreateTable<NullableIntClass>();

            var withNull = new NullableIntClass
            {
                NullableInt = null
            };
            var with0 = new NullableIntClass
            {
                NullableInt = 0
            };
            var with1 = new NullableIntClass
            {
                NullableInt = 1
            };
            var withMinus1 = new NullableIntClass
            {
                NullableInt = -1
            };

            db.Insert(withNull);
            db.Insert(with0);
            db.Insert(with1);
            db.Insert(withMinus1);

            NullableIntClass[] results = db.Table<NullableIntClass>().OrderBy(x => x.ID).ToArray();

            Assert.AreEqual(4, results.Length);

            Assert.AreEqual(withNull, results[0]);
            Assert.AreEqual(with0, results[1]);
            Assert.AreEqual(with1, results[2]);
            Assert.AreEqual(withMinus1, results[3]);
        }

        [Test]
        public void NullableString()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            db.CreateTable<StringClass>();

            var withNull = new StringClass
            {
                StringData = null
            };
            var withEmpty = new StringClass
            {
                StringData = ""
            };
            var withData = new StringClass
            {
                StringData = "data"
            };

            db.Insert(withNull);
            db.Insert(withEmpty);
            db.Insert(withData);

            StringClass[] results = db.Table<StringClass>().OrderBy(x => x.ID).ToArray();

            Assert.AreEqual(3, results.Length);

            Assert.AreEqual(withNull, results[0]);
            Assert.AreEqual(withEmpty, results[1]);
            Assert.AreEqual(withData, results[2]);
        }

        [Test]
        public void StringWhereNotNull()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            db.CreateTable<StringClass>();

            var withNull = new StringClass
            {
                StringData = null
            };
            var withEmpty = new StringClass
            {
                StringData = ""
            };
            var withData = new StringClass
            {
                StringData = "data"
            };

            db.Insert(withNull);
            db.Insert(withEmpty);
            db.Insert(withData);

            StringClass[] results =
                db.Table<StringClass>().Where(x => x.StringData != null).OrderBy(x => x.ID).ToArray();
            Assert.AreEqual(2, results.Length);
            Assert.AreEqual(withEmpty, results[0]);
            Assert.AreEqual(withData, results[1]);
        }

        [Test]
        public void StringWhereNull()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            db.CreateTable<StringClass>();

            var withNull = new StringClass
            {
                StringData = null
            };
            var withEmpty = new StringClass
            {
                StringData = ""
            };
            var withData = new StringClass
            {
                StringData = "data"
            };

            db.Insert(withNull);
            db.Insert(withEmpty);
            db.Insert(withData);

            StringClass[] results =
                db.Table<StringClass>().Where(x => x.StringData == null).OrderBy(x => x.ID).ToArray();
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(withNull, results[0]);
        }

        [Test]
        public void WhereNotNull()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            db.CreateTable<NullableIntClass>();

            var withNull = new NullableIntClass
            {
                NullableInt = null
            };
            var with0 = new NullableIntClass
            {
                NullableInt = 0
            };
            var with1 = new NullableIntClass
            {
                NullableInt = 1
            };
            var withMinus1 = new NullableIntClass
            {
                NullableInt = -1
            };

            db.Insert(withNull);
            db.Insert(with0);
            db.Insert(with1);
            db.Insert(withMinus1);

            NullableIntClass[] results =
                db.Table<NullableIntClass>().Where(x => x.NullableInt != null).OrderBy(x => x.ID).ToArray();

            Assert.AreEqual(3, results.Length);

            Assert.AreEqual(with0, results[0]);
            Assert.AreEqual(with1, results[1]);
            Assert.AreEqual(withMinus1, results[2]);
        }

        [Test]
        public void WhereNull()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            db.CreateTable<NullableIntClass>();

            var withNull = new NullableIntClass
            {
                NullableInt = null
            };
            var with0 = new NullableIntClass
            {
                NullableInt = 0
            };
            var with1 = new NullableIntClass
            {
                NullableInt = 1
            };
            var withMinus1 = new NullableIntClass
            {
                NullableInt = -1
            };

            db.Insert(withNull);
            db.Insert(with0);
            db.Insert(with1);
            db.Insert(withMinus1);

            NullableIntClass[] results =
                db.Table<NullableIntClass>().Where(x => x.NullableInt == null).OrderBy(x => x.ID).ToArray();

            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(withNull, results[0]);
        }
    }
}