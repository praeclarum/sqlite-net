using System;
using System.Diagnostics;
using NUnit.Framework;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class BooleanTest
    {
        public class VO
        {
            [AutoIncrement, PrimaryKey]
            public int ID { get; set; }

            public bool Flag { get; set; }
            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("VO:: ID:{0} Flag:{1} Text:{2}", ID, Flag, Text);
            }
        }

        public class DbAcs : SQLiteConnection
        {
            public DbAcs(ISQLitePlatform sqlitePlatform, String path)
                : base(sqlitePlatform, path)
            {
                TraceListener = DebugTraceListener.Instance;
            }

            public void buildTable()
            {
                CreateTable<VO>();
            }

            public int CountWithFlag(Boolean flag)
            {
                SQLiteCommand cmd = CreateCommand("SELECT COUNT(*) FROM VO Where Flag = ?", flag);
                return cmd.ExecuteScalar<int>();
            }
        }

        [Test]
        public void TestBoolean()
        {
            var sqlite3Platform = new SQLitePlatformTest();
            string tmpFile = TestPath.CreateTemporaryDatabase();
            var db = new DbAcs(sqlite3Platform, tmpFile);
            db.buildTable();
            for (int i = 0; i < 10; i++)
            {
                db.Insert(new VO
                {
                    Flag = (i%3 == 0),
                    Text = String.Format("VO{0}", i)
                });
            }

            // count vo which flag is true            
            Assert.AreEqual(4, db.CountWithFlag(true));
            Assert.AreEqual(6, db.CountWithFlag(false));

            Debug.WriteLine("VO with true flag:");
            foreach (VO vo in db.Query<VO>("SELECT * FROM VO Where Flag = ?", true))
            {
                Debug.WriteLine(vo.ToString());
            }

            Debug.WriteLine("VO with false flag:");
            foreach (VO vo in db.Query<VO>("SELECT * FROM VO Where Flag = ?", false))
            {
                Debug.WriteLine(vo.ToString());
            }
        }
    }
}