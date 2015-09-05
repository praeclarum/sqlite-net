using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;
using System.IO;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class BackupTest
    {
        public class BackupTestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }
        }

        public class BackupTestObj2
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }
        }

        public class BackupTestObj3
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
        }


        public class BackupTestDb : SQLiteConnection
        {
            public BackupTestDb(String path) : base(new SQLitePlatformTest(), path)
            {
                CreateTable<BackupTestObj>();
                CreateTable<BackupTestObj2>();
                CreateTable<BackupTestObj3>();
            }
        }

        [Test]
        public void CreateBackup()
        {
            var obj1 = new BackupTestObj
            {
                Text = "GLaDOS loves testing!"
            };
            var obj2 = new BackupTestObj2
            {
                Text = "Keep testing, just keep testing"
            };

            SQLiteConnection srcDb = new BackupTestDb(TestPath.CreateTemporaryDatabase());

            int numIn1 = srcDb.Insert(obj1);
            Assert.AreEqual(1, numIn1);
            int numIn2 = srcDb.Insert(obj2);
            Assert.AreEqual(1, numIn2);

            const int numInserts = 1000;
            int inserts = 0;
            for (int i = 0; i < numInserts; i++)
            {
                inserts += srcDb.Insert(new BackupTestObj3());
            }
            Assert.AreEqual(numInserts, inserts);

            List<BackupTestObj> result1 = srcDb.Query<BackupTestObj>("select * from BackupTestObj").ToList();
            Assert.AreEqual(numIn1, result1.Count);
            Assert.AreEqual(obj1.Text, result1.First().Text);

            List<BackupTestObj> result2 = srcDb.Query<BackupTestObj>("select * from BackupTestObj2").ToList();
            Assert.AreEqual(numIn2, result2.Count);
            Assert.AreEqual(obj2.Text, result2.First().Text);

            string destDbPath = srcDb.CreateDatabaseBackup(new SQLitePlatformTest());
//            Assert.IsTrue(File.Exists(destDbPath));

            SQLiteConnection destDb = new BackupTestDb(destDbPath);
            result1 = destDb.Query<BackupTestObj>("select * from BackupTestObj").ToList();
            Assert.AreEqual(numIn1, result1.Count);
            Assert.AreEqual(obj1.Text, result1.First().Text);

            result2 = destDb.Query<BackupTestObj>("select * from BackupTestObj2").ToList();
            Assert.AreEqual(numIn2, result2.Count);
            Assert.AreEqual(obj2.Text, result2.First().Text);

            int count = destDb.ExecuteScalar<int>("select count(*) from BackupTestObj3");
            Assert.AreEqual(numInserts, count);

            srcDb.Close();
            destDb.Close();
        }
    }
}