using System;
using NUnit.Framework;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

#if __WIN32__
using SQLitePlatformTest = SQLite.Net.Platform.Win32.SQLitePlatformWin32;
#elif WINDOWS_PHONE
using SQLitePlatformTest = SQLite.Net.Platform.WindowsPhone8.SQLitePlatformWP8;
#elif __WINRT__
using SQLitePlatformTest = SQLite.Net.Platform.WinRT.SQLitePlatformWinRT;
#elif __IOS__
using SQLitePlatformTest = SQLite.Net.Platform.XamarinIOS.SQLitePlatformIOS;
#elif __ANDROID__
using SQLitePlatformTest = SQLite.Net.Platform.XamarinAndroid.SQLitePlatformAndroid;
#else
using SQLitePlatformTest = SQLite.Net.Platform.Generic.SQLitePlatformGeneric;
#endif

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class CollateTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public string CollateDefault { get; set; }

            [Collation("BINARY")]
            public string CollateBinary { get; set; }

            [Collation("RTRIM")]
            public string CollateRTrim { get; set; }

            [Collation("NOCASE")]
            public string CollateNoCase { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}]", Id);
            }
        }

        public class TestDb : SQLiteConnection
        {
            public TestDb(ISQLitePlatform sqlitePlatform, String path)
                : base(sqlitePlatform, path)
            {
                TraceListener = DebugTraceListener.Instance;
                CreateTable<TestObj>();
            }
        }

        [Test]
        public void Collate()
        {
            var obj = new TestObj
            {
                CollateDefault = "Alpha ",
                CollateBinary = "Alpha ",
                CollateRTrim = "Alpha ",
                CollateNoCase = "Alpha ",
            };

            var db = new TestDb(new SQLitePlatformTest(), TestPath.GetTempFileName());

            db.Insert(obj);

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateDefault == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateBinary == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateRTrim == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateRTrim == "ALPHA " select o).Count());
            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateRTrim == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateRTrim == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateNoCase == "Alpha " select o).Count());
            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateNoCase == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateNoCase == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateNoCase == "ALPHA" select o).Count());
        }
    }
}