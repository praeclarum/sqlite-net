using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;

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
    public class SkipTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public int Order { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Order={1}]", Id, Order);
            }
        }

        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(new SQLitePlatformTest(), path)
            {
                CreateTable<TestObj>();
            }
        }

        [Test]
        public void Skip()
        {
            int n = 100;

            IEnumerable<TestObj> cq = from i in Enumerable.Range(1, n)
                select new TestObj
                {
                    Order = i
                };
            TestObj[] objs = cq.ToArray();
            var db = new TestDb(TestPath.GetTempFileName());

            int numIn = db.InsertAll(objs);
            Assert.AreEqual(numIn, n, "Num inserted must = num objects");

            TableQuery<TestObj> q = from o in db.Table<TestObj>()
                orderby o.Order
                select o;

            TableQuery<TestObj> qs1 = q.Skip(1);
            List<TestObj> s1 = qs1.ToList();
            Assert.AreEqual(n - 1, s1.Count);
            Assert.AreEqual(2, s1[0].Order);

            TableQuery<TestObj> qs5 = q.Skip(5);
            List<TestObj> s5 = qs5.ToList();
            Assert.AreEqual(n - 5, s5.Count);
            Assert.AreEqual(6, s5[0].Order);
        }
    }
}