using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class ByteArrayTest
    {
        [SetUp]
        public void SetUp()
        {
            _sqlite3Platform = new SQLitePlatformTest();
        }

        private SQLitePlatformTest _sqlite3Platform;

        public class ByteArrayClass
        {
            [PrimaryKey, AutoIncrement]
            public int ID { get; set; }

            public byte[] bytes { get; set; }

            public void AssertEquals(ByteArrayClass other)
            {
                Assert.AreEqual(other.ID, ID);
                if (other.bytes == null || bytes == null)
                {
                    Assert.IsNull(other.bytes);
                    Assert.IsNull(bytes);
                }
                else
                {
                    Assert.AreEqual(other.bytes.Length, bytes.Length);
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        Assert.AreEqual(other.bytes[i], bytes[i]);
                    }
                }
            }
        }

        [Test]
        [Description("Create objects with various byte arrays and check they can be stored and retrieved correctly")]
        public void ByteArrays()
        {
            //Byte Arrays for comparisson
            ByteArrayClass[] byteArrays =
            {
                new ByteArrayClass
                {
                    bytes = new byte[] {1, 2, 3, 4, 250, 252, 253, 254, 255}
                }, //Range check
                new ByteArrayClass
                {
                    bytes = new byte[] {0}
                }, //null bytes need to be handled correctly
                new ByteArrayClass
                {
                    bytes = new byte[] {0, 0}
                },
                new ByteArrayClass
                {
                    bytes = new byte[] {0, 1, 0}
                },
                new ByteArrayClass
                {
                    bytes = new byte[] {1, 0, 1}
                },
                new ByteArrayClass
                {
                    bytes = new byte[] {}
                }, //Empty byte array should stay empty (and not become null)
                new ByteArrayClass
                {
                    bytes = null
                } //Null should be supported
            };

            var database = new SQLiteConnection(_sqlite3Platform, TestPath.CreateTemporaryDatabase());
            database.CreateTable<ByteArrayClass>();

            //Insert all of the ByteArrayClass
            foreach (ByteArrayClass b in byteArrays)
            {
                database.Insert(b);
            }

            //Get them back out
            ByteArrayClass[] fetchedByteArrays = database.Table<ByteArrayClass>().OrderBy(x => x.ID).ToArray();

            Assert.AreEqual(fetchedByteArrays.Length, byteArrays.Length);
            //Check they are the same
            for (int i = 0; i < byteArrays.Length; i++)
            {
                byteArrays[i].AssertEquals(fetchedByteArrays[i]);
            }
        }

        [Test]
        [Description("Uses a byte array to find a record")]
        public void ByteArrayWhere()
        {
            //Byte Arrays for comparisson
            ByteArrayClass[] byteArrays = new ByteArrayClass[] {
                new ByteArrayClass() { bytes = new byte[] { 1, 2, 3, 4, 250, 252, 253, 254, 255 } }, //Range check
                new ByteArrayClass() { bytes = new byte[] { 0 } }, //null bytes need to be handled correctly
                new ByteArrayClass() { bytes = new byte[] { 0, 0 } },
                new ByteArrayClass() { bytes = new byte[] { 0, 1, 0 } },
                new ByteArrayClass() { bytes = new byte[] { 1, 0, 1 } },
                new ByteArrayClass() { bytes = new byte[] { } }, //Empty byte array should stay empty (and not become null)
                new ByteArrayClass() { bytes = null } //Null should be supported
            };

            var database = new SQLiteConnection(_sqlite3Platform, TestPath.CreateTemporaryDatabase());
            database.CreateTable<ByteArrayClass>();

            byte[] criterion = new byte[] { 1, 0, 1 };

            //Insert all of the ByteArrayClass
            int id = 0;
            foreach (ByteArrayClass b in byteArrays)
            {
                database.Insert(b);
                if (b.bytes != null && criterion.SequenceEqual<byte>(b.bytes))
                    id = b.ID;
            }
            Assert.AreNotEqual(0, id, "An ID wasn't set");

            //Get it back out
            ByteArrayClass fetchedByteArray = database.Table<ByteArrayClass>().Where(x => x.bytes == criterion).First();
            Assert.IsNotNull(fetchedByteArray);
            //Check they are the same
            Assert.AreEqual(id, fetchedByteArray.ID);
        }

        [Test]
        [Description("Uses a null byte array to find a record")]
        public void ByteArrayWhereNull()
        {
            //Byte Arrays for comparisson
            ByteArrayClass[] byteArrays = new ByteArrayClass[] {
                new ByteArrayClass() { bytes = new byte[] { 1, 2, 3, 4, 250, 252, 253, 254, 255 } }, //Range check
                new ByteArrayClass() { bytes = new byte[] { 0 } }, //null bytes need to be handled correctly
                new ByteArrayClass() { bytes = new byte[] { 0, 0 } },
                new ByteArrayClass() { bytes = new byte[] { 0, 1, 0 } },
                new ByteArrayClass() { bytes = new byte[] { 1, 0, 1 } },
                new ByteArrayClass() { bytes = new byte[] { } }, //Empty byte array should stay empty (and not become null)
                new ByteArrayClass() { bytes = null } //Null should be supported
            };

            var database = new SQLiteConnection(_sqlite3Platform, TestPath.CreateTemporaryDatabase());
            database.CreateTable<ByteArrayClass>();

            byte[] criterion = null;

            //Insert all of the ByteArrayClass
            int id = 0;
            foreach (ByteArrayClass b in byteArrays)
            {
                database.Insert(b);
                if (b.bytes == null)
                    id = b.ID;
            }
            Assert.AreNotEqual(0, id, "An ID wasn't set");

            //Get it back out
            ByteArrayClass fetchedByteArray = database.Table<ByteArrayClass>().Where(x => x.bytes == criterion).First();

            Assert.IsNotNull(fetchedByteArray);
            //Check they are the same
            Assert.AreEqual(id, fetchedByteArray.ID);
        }

        [Test]
        [Description("Create A large byte array and check it can be stored and retrieved correctly")]
        public void LargeByteArray()
        {
            const int byteArraySize = 1024*1024;
            var bytes = new byte[byteArraySize];
            for (int i = 0; i < byteArraySize; i++)
            {
                bytes[i] = (byte) (i%256);
            }

            var byteArray = new ByteArrayClass
            {
                bytes = bytes
            };

            var database = new SQLiteConnection(_sqlite3Platform, TestPath.CreateTemporaryDatabase());
            database.CreateTable<ByteArrayClass>();

            //Insert the ByteArrayClass
            database.Insert(byteArray);

            //Get it back out
            ByteArrayClass[] fetchedByteArrays = database.Table<ByteArrayClass>().ToArray();

            Assert.AreEqual(fetchedByteArrays.Length, 1);

            //Check they are the same
            byteArray.AssertEquals(fetchedByteArrays[0]);
        }
    }
}