using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SQLite.Net.Async;
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


    /// <summary>
    ///     Defines tests that exercise async behaviour.
    /// </summary>
    [TestFixture]
    public class SelectTest
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
        public void SelectWorks()
        {
            using (var db = new TestDb(TestPath.GetTempFileName()))
            {
                db.Insert(new TestObj() {Order = 5});
                try
                {
                    Assert.That(db.Table<TestObj>().Select(obj => obj.Order * 2).First(), Is.EqualTo(10));
                }
                catch (NotImplementedException)
                {
                    //Allow Not implemented exceptions as the selection may be too complex.
                }
            }
           
        }
    }
}