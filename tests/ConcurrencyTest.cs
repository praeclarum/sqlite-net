using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TearDown = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestCleanupAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
    [TestFixture]
    public class ConcurrencyTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}]", Id);
            }
        }

        public class DbReader
        {
            private CancellationToken cancellationToken;

            public DbReader(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public Task Run()
            {
                var t = Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            //
                            // NOTE: Change this to readwrite and then it does work ???
                            // No more IOERROR
                            // 
                        
                            using (var dbConnection = new DbConnection(SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadOnly))
                            {
                                var records = dbConnection.Table<TestObj>().ToList();
                                System.Diagnostics.Debug.WriteLine($"{Environment.CurrentManagedThreadId} Read records: {records.Count}");
                            }

                            // No await so we stay on the same thread
                            Task.Delay(10).GetAwaiter().GetResult();
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });

                return t;
            }

        }

        public class DbWriter
        {
            private CancellationToken cancellationToken;

            public DbWriter(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public Task Run()
            {
                var t = Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            using (var dbConnection = new DbConnection(SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite))
                            {
                                System.Diagnostics.Debug.WriteLine($"{Environment.CurrentManagedThreadId} Start insert");

                                for (var i = 0; i < 50; i++)
                                {
                                    var newRecord = new TestObj()
                                    {
                                    };

                                    dbConnection.Insert(newRecord);
                                }

                                System.Diagnostics.Debug.WriteLine($"{Environment.CurrentManagedThreadId} Inserted records");
                            }

                            // No await so we stay on the same thread
                            Task.Delay(1).GetAwaiter().GetResult();
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });

                return t;
            }

        }

        public class DbConnection : SQLiteConnection
        {
            private static string DbPath = GetTempFileName();

            private static string GetTempFileName()
            {
#if NETFX_CORE
                var name = Guid.NewGuid() + ".sqlite";
                return Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, name);
#else
                return Path.GetTempFileName();
#endif
            }


            public DbConnection(SQLiteOpenFlags openflags) : base(DbPath, openflags)
            {
                this.BusyTimeout = TimeSpan.FromSeconds(5);
            }

            public void CreateTables()
            {
                CreateTable<TestObj>();
            }
        }

        [SetUp]
        public void Setup()
        {
            using (var dbConenction = new DbConnection(SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create))
            {
                dbConenction.CreateTables();
            }
        }


        [Test]
        public void TestLoad()
        {
            try
            {
                //var result = SQLitePCL.raw.sqlite3_threadsafe();
                //Assert.AreEqual(2, result);
                // Yes it's threadsafe on iOS

                var tokenSource = new CancellationTokenSource();
                var tasks = new List<Task>();
                tasks.Add(new DbReader(tokenSource.Token).Run());
                tasks.Add(new DbWriter(tokenSource.Token).Run());

                // Wait 5sec
                tokenSource.CancelAfter(5000);

                Task.WhenAll(tasks).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }


    }
}

