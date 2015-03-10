using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PCLStorage;
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
#elif __OSX__
using SQLitePlatformTest = SQLite.Net.Platform.OSX.SQLitePlatformOSX;
#else
using SQLitePlatformTest = SQLite.Net.Platform.Generic.SQLitePlatformGeneric;
#endif

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class TransactionTest
    {
        [SetUp]
        public void Setup()
        {
            testObjects = Enumerable.Range(1, 20).Select(i => new TestObj()).ToList();

            db = new TestDb(TestPath.CreateTemporaryDatabase());
            db.InsertAll(testObjects);
        }

        [TearDown]
        public void TearDown()
        {
            if (db != null)
            {
                db.Close();
            }
        }

        private TestDb db;
        private List<TestObj> testObjects;

        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}]", Id);
            }
        }

        public class TransactionTestException : Exception
        {
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
        public void FailNestedSavepointTransaction()
        {
            try
            {
                db.RunInTransaction(() =>
                {
                    db.Delete(testObjects[0]);

                    db.RunInTransaction(() =>
                    {
                        db.Delete(testObjects[1]);

                        throw new TransactionTestException();
                    });
                });
            }
            catch (TransactionTestException)
            {
                // ignore
            }

            Assert.AreEqual(testObjects.Count, db.Table<TestObj>().Count());
        }

        [Test]
        public void FailSavepointTransaction()
        {
            try
            {
                db.RunInTransaction(() =>
                {
                    db.Delete(testObjects[0]);

                    throw new TransactionTestException();
                });
            }
            catch (TransactionTestException)
            {
                // ignore
            }

            Assert.AreEqual(testObjects.Count, db.Table<TestObj>().Count());
        }

        [Test]
        public void SuccessfulNestedSavepointTransaction()
        {
            db.RunInTransaction(() =>
            {
                db.Delete(testObjects[0]);

                db.RunInTransaction(() => { db.Delete(testObjects[1]); });
            });

            Assert.AreEqual(testObjects.Count - 2, db.Table<TestObj>().Count());
        }

        [Test]
        public void SuccessfulSavepointTransaction()
        {
            db.RunInTransaction(() =>
            {
                db.Delete(testObjects[0]);
                db.Delete(testObjects[1]);
                db.Insert(new TestObj());
            });

            Assert.AreEqual(testObjects.Count - 1, db.Table<TestObj>().Count());
        }
    }
}