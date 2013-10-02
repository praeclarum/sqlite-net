using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SQLite.Net.Async;
using SQLite.Net.Attributes;
using SQLite.Net.Platform.Win32;

namespace SQLite.Net.Tests
{
    // @mbrit - 2012-05-14 - NOTE - the lack of async use in this class is because the VS11 test runner falsely
    // reports any failing test as succeeding if marked as async. Should be fixed in the "June 2012" drop...

    public class Customer
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        [MaxLength(64)]
        public string FirstName { get; set; }

        [MaxLength(64)]
        public string LastName { get; set; }

        [MaxLength(64)]
        public string Email { get; set; }
    }

    /// <summary>
    ///     Defines tests that exercise async behaviour.
    /// </summary>
    [TestFixture]
    public class AsyncTests
    {
        [SetUp]
        public void SetUp()
        {
            if (_sqliteConnectionPool != null)
            {
                _sqliteConnectionPool.Reset();
            }
            _path = Path.Combine(Path.GetTempPath(), DatabaseName);
            // delete old db file
            File.Delete(_path);

            _connectionParameters = new SQLiteConnectionString(_path, false);
            _sqliteConnectionPool = new SQLiteConnectionPool(_sqlite3Platform);
        }

        private const string DatabaseName = "async.db";

        private SQLiteAsyncConnection GetConnection()
        {
            string path = null;
            return GetConnection(ref path);
        }

        private string _path;
        private SQLiteConnectionString _connectionParameters;
        private SQLitePlatformWin32 _sqlite3Platform;
        private SQLiteConnectionPool _sqliteConnectionPool;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _sqlite3Platform = new SQLitePlatformWin32();
            _sqliteConnectionPool = new SQLiteConnectionPool(_sqlite3Platform);
        }

        private SQLiteAsyncConnection GetConnection(ref string path)
        {
            path = _path;
            return new SQLiteAsyncConnection(()=>_sqliteConnectionPool.GetConnection(_connectionParameters));
        }

        private Customer CreateCustomer()
        {
            var customer = new Customer
            {
                FirstName = "foo",
                LastName = "bar",
                Email = Guid.NewGuid().ToString()
            };
            return customer;
        }

        [Test]
        public void FindAsyncWithExpression()
        {
            // create...
            var customer = new Customer();
            customer.FirstName = "foo";
            customer.LastName = "bar";
            customer.Email = Guid.NewGuid().ToString();

            // connect and insert...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            conn.CreateTableAsync<Customer>().Wait();
            conn.InsertAsync(customer).Wait();

            // check...
            Assert.AreNotEqual(0, customer.Id);

            // get it back...
            Task<Customer> task = conn.FindAsync<Customer>(x => x.Id == customer.Id);
            task.Wait();
            Customer loaded = task.Result;

            // check...
            Assert.AreEqual(customer.Id, loaded.Id);
        }

        [Test]
        public void FindAsyncWithExpressionNull()
        {
            // connect and insert...
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // get it back...
            Task<Customer> task = conn.FindAsync<Customer>(x => x.Id == 1);
            task.Wait();
            Customer loaded = task.Result;

            // check...
            Assert.IsNull(loaded);
        }

        [Test]
        public void GetAsync()
        {
            // create...
            var customer = new Customer();
            customer.FirstName = "foo";
            customer.LastName = "bar";
            customer.Email = Guid.NewGuid().ToString();

            // connect and insert...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            conn.CreateTableAsync<Customer>().Wait();
            conn.InsertAsync(customer).Wait();

            // check...
            Assert.AreNotEqual(0, customer.Id);

            // get it back...
            Task<Customer> task = conn.GetAsync<Customer>(customer.Id);
            task.Wait();
            Customer loaded = task.Result;

            // check...
            Assert.AreEqual(customer.Id, loaded.Id);
        }

        [Test]
        public void StressAsync()
        {
            string path = null;
            SQLiteAsyncConnection globalConn = GetConnection(ref path);

            globalConn.CreateTableAsync<Customer>().Wait();

            int threadCount = 0;
            var doneEvent = new AutoResetEvent(false);
            int n = 500;
            var errors = new List<string>();
            for (int i = 0; i < n; i++)
            {
                Task.Factory.StartNew(delegate
                {
                    try
                    {
                        SQLiteAsyncConnection conn = GetConnection();
                        var obj = new Customer
                        {
                            FirstName = i.ToString(),
                        };
                        conn.InsertAsync(obj).Wait();
                        if (obj.Id == 0)
                        {
                            lock (errors)
                            {
                                errors.Add("Bad Id");
                            }
                        }
                        Customer obj2 =
                            (from c in conn.Table<Customer>() where c.Id == obj.Id select c).ToListAsync()
                                .Result.FirstOrDefault();
                        if (obj2 == null)
                        {
                            lock (errors)
                            {
                                errors.Add("Failed query");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (errors)
                        {
                            errors.Add(ex.Message);
                        }
                    }
                    threadCount++;
                    if (threadCount == n)
                    {
                        doneEvent.Set();
                    }
                });
            }
            doneEvent.WaitOne();

            int count = globalConn.Table<Customer>().CountAsync().Result;

            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(n, count);
        }

        [Test]
        public void TestAsyncGetWithExpression()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
            {
                Customer customer = CreateCustomer();
                customer.FirstName = index.ToString();
                conn.InsertAsync(customer).Wait();
            }

            // get...
            Task<Customer> result = conn.GetAsync<Customer>(x => x.FirstName == "7");
            result.Wait();
            Customer loaded = result.Result;
            // check...
            Assert.AreEqual("7", loaded.FirstName);
        }

        [Test]
        public void TestAsyncTableElementAtAsync()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
            {
                Customer customer = CreateCustomer();
                customer.FirstName = index.ToString();
                conn.InsertAsync(customer).Wait();
            }

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.FirstName);
            Task<Customer> task = query.ElementAtAsync(7);
            task.Wait();
            Customer loaded = task.Result;

            // check...
            Assert.AreEqual("7", loaded.FirstName);
        }

        [Test]
        public void TestAsyncTableOrderBy()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
                conn.InsertAsync(CreateCustomer()).Wait();

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.Email);
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Assert.AreEqual(-1, string.Compare(items[0].Email, items[9].Email));
        }

        [Test]
        public void TestAsyncTableOrderByDescending()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
                conn.InsertAsync(CreateCustomer()).Wait();

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderByDescending(v => v.Email);
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Assert.AreEqual(1, string.Compare(items[0].Email, items[9].Email));
        }

        [Test]
        public void TestAsyncTableQueryCountAsync()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
                conn.InsertAsync(CreateCustomer()).Wait();

            // load...
            AsyncTableQuery<Customer> query = conn.Table<Customer>();
            Task<int> task = query.CountAsync();
            task.Wait();

            // check...
            Assert.AreEqual(10, task.Result);
        }

        [Test]
        public void TestAsyncTableQuerySkip()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
            {
                Customer customer = CreateCustomer();
                customer.FirstName = index.ToString();
                conn.InsertAsync(customer).Wait();
            }

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.FirstName).Skip(5);
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Assert.AreEqual(5, items.Count);
            Assert.AreEqual("5", items[0].FirstName);
        }

        [Test]
        public void TestAsyncTableQueryTake()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create...
            for (int index = 0; index < 10; index++)
            {
                Customer customer = CreateCustomer();
                customer.FirstName = index.ToString();
                conn.InsertAsync(customer).Wait();
            }

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.FirstName).Take(1);
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("0", items[0].FirstName);
        }

        [Test]
        public void TestAsyncTableQueryToFirstAsyncFound()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // create...
            Customer customer = CreateCustomer();
            conn.InsertAsync(customer).Wait();

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().Where(v => v.Id == customer.Id);
            Task<Customer> task = query.FirstAsync();
            task.Wait();
            Customer loaded = task.Result;

            // check...
            Assert.AreEqual(customer.Email, loaded.Email);
        }

        [Test]
        public void TestAsyncTableQueryToFirstAsyncMissing()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // create...
            Customer customer = CreateCustomer();
            conn.InsertAsync(customer).Wait();

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().Where(v => v.Id == -1);
            Task<Customer> task = query.FirstAsync();
            ExceptionAssert.Throws<AggregateException>(() => task.Wait());
        }

        [Test]
        public void TestAsyncTableQueryToFirstOrDefaultAsyncFound()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // create...
            Customer customer = CreateCustomer();
            conn.InsertAsync(customer).Wait();

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().Where(v => v.Id == customer.Id);
            Task<Customer> task = query.FirstOrDefaultAsync();
            task.Wait();
            Customer loaded = task.Result;

            // check...
            Assert.AreEqual(customer.Email, loaded.Email);
        }

        [Test]
        public void TestAsyncTableQueryToFirstOrDefaultAsyncMissing()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // create...
            Customer customer = CreateCustomer();
            conn.InsertAsync(customer).Wait();

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().Where(v => v.Id == -1);
            Task<Customer> task = query.FirstOrDefaultAsync();
            task.Wait();
            Customer loaded = task.Result;

            // check...
            Assert.IsNull(loaded);
        }

        [Test]
        public void TestAsyncTableQueryToListAsync()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // create...
            Customer customer = CreateCustomer();
            conn.InsertAsync(customer).Wait();

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>();
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Customer loaded = items.Where(v => v.Id == customer.Id).First();
            Assert.AreEqual(customer.Email, loaded.Email);
        }

        [Test]
        public void TestAsyncTableQueryWhereOperation()
        {
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // create...
            Customer customer = CreateCustomer();
            conn.InsertAsync(customer).Wait();

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>();
            Task<List<Customer>> task = query.ToListAsync();
            task.Wait();
            List<Customer> items = task.Result;

            // check...
            Customer loaded = items.Where(v => v.Id == customer.Id).First();
            Assert.AreEqual(customer.Email, loaded.Email);
        }

        [Test]
        public void TestCreateTableAsync()
        {
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);

            // drop the customer table...
            conn.ExecuteAsync("drop table if exists Customer").Wait();

            // run...
            conn.CreateTableAsync<Customer>().Wait();

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // run it - if it's missing we'll get a failure...
                check.Execute("select * from Customer");
            }
        }

        [Test]
        public void TestDeleteAsync()
        {
            // create...
            Customer customer = CreateCustomer();

            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            conn.CreateTableAsync<Customer>().Wait();

            // run...
            conn.InsertAsync(customer).Wait();

            // delete it...
            conn.DeleteAsync(customer).Wait();

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // load it back - should be null...
                List<Customer> loaded = check.Table<Customer>().Where(v => v.Id == customer.Id).ToList();
                Assert.AreEqual(0, loaded.Count);
            }
        }

        [Test]
        public void TestDropTableAsync()
        {
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            conn.CreateTableAsync<Customer>().Wait();

            // drop it...
            conn.DropTableAsync<Customer>().Wait();

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // load it back and check - should be missing
                SQLiteCommand command =
                    check.CreateCommand("select name from sqlite_master where type='table' and name='customer'");
                Assert.IsNull(command.ExecuteScalar<string>());
            }
        }

        [Test]
        public void TestExecuteAsync()
        {
            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            conn.CreateTableAsync<Customer>().Wait();

            // do a manual insert...
            string email = Guid.NewGuid().ToString();
            conn.ExecuteAsync("insert into customer (firstname, lastname, email) values (?, ?, ?)",
                "foo", "bar", email).Wait();

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // load it back - should be null...
                TableQuery<Customer> result = check.Table<Customer>().Where(v => v.Email == email);
                Assert.IsNotNull(result);
            }
        }

        [Test]
        public void TestExecuteScalar()
        {
            // connect...
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // check...
            Task<object> task =
                conn.ExecuteScalarAsync<object>("select name from sqlite_master where type='table' and name='customer'");
            task.Wait();
            object name = task.Result;
            Assert.AreNotEqual("Customer", name);
        }

        [Test]
        public void TestFindAsyncItemMissing()
        {
            // connect and insert...
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // now get one that doesn't exist...
            Task<Customer> task = conn.FindAsync<Customer>(-1);
            task.Wait();

            // check...
            Assert.IsNull(task.Result);
        }

        [Test]
        public void TestFindAsyncItemPresent()
        {
            // create...
            Customer customer = CreateCustomer();

            // connect and insert...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            conn.CreateTableAsync<Customer>().Wait();
            conn.InsertAsync(customer).Wait();

            // check...
            Assert.AreNotEqual(0, customer.Id);

            // get it back...
            Task<Customer> task = conn.FindAsync<Customer>(customer.Id);
            task.Wait();
            Customer loaded = task.Result;

            // check...
            Assert.AreEqual(customer.Id, loaded.Id);
        }

        [Test]
        public void TestInsertAllAsync()
        {
            // create a bunch of customers...
            var customers = new List<Customer>();
            for (int index = 0; index < 100; index++)
            {
                var customer = new Customer();
                customer.FirstName = "foo";
                customer.LastName = "bar";
                customer.Email = Guid.NewGuid().ToString();
                customers.Add(customer);
            }

            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            conn.CreateTableAsync<Customer>().Wait();

            // insert them all...
            conn.InsertAllAsync(customers).Wait();

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                for (int index = 0; index < customers.Count; index++)
                {
                    // load it back and check...
                    var loaded = check.Get<Customer>(customers[index].Id);
                    Assert.AreEqual(loaded.Email, customers[index].Email);
                }
            }
        }

        [Test]
        public void TestInsertAsync()
        {
            // create...
            Customer customer = CreateCustomer();

            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            conn.CreateTableAsync<Customer>().Wait();

            // run...
            conn.InsertAsync(customer).Wait();

            // check that we got an id...
            Assert.AreNotEqual(0, customer.Id);

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // load it back...
                var loaded = check.Get<Customer>(customer.Id);
                Assert.AreEqual(loaded.Id, customer.Id);
            }
        }

        [Test]
        public void TestQueryAsync()
        {
            // connect...
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();

            // insert some...
            var customers = new List<Customer>();
            for (int index = 0; index < 5; index++)
            {
                Customer customer = CreateCustomer();

                // insert...
                conn.InsertAsync(customer).Wait();

                // add...
                customers.Add(customer);
            }

            // return the third one...
            Task<List<Customer>> task = conn.QueryAsync<Customer>("select * from customer where id=?", customers[2].Id);
            task.Wait();
            List<Customer> loaded = task.Result;

            // check...
            Assert.AreEqual(1, loaded.Count);
            Assert.AreEqual(customers[2].Email, loaded[0].Email);
        }

        [Test]
        public void TestRunInTransactionAsync()
        {
            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            conn.CreateTableAsync<Customer>().Wait();
            bool transactionCompleted = false;

            // run...
            var customer = new Customer();
            conn.RunInTransactionAsync(c =>
            {
                // insert...
                customer.FirstName = "foo";
                customer.LastName = "bar";
                customer.Email = Guid.NewGuid().ToString();
                c.Insert(customer);

                // delete it again...
                c.Execute("delete from customer where id=?", customer.Id);

                // set completion flag
                transactionCompleted = true;
            }).Wait(10000);

            // check...
            Assert.IsTrue(transactionCompleted);
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // load it back and check - should be deleted...
                List<Customer> loaded = check.Table<Customer>().Where(v => v.Id == customer.Id).ToList();
                Assert.AreEqual(0, loaded.Count);
            }
        }

        [Test]
        public void TestTableAsync()
        {
            // connect...
            SQLiteAsyncConnection conn = GetConnection();
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // insert some...
            var customers = new List<Customer>();
            for (int index = 0; index < 5; index++)
            {
                var customer = new Customer();
                customer.FirstName = "foo";
                customer.LastName = "bar";
                customer.Email = Guid.NewGuid().ToString();

                // insert...
                conn.InsertAsync(customer).Wait();

                // add...
                customers.Add(customer);
            }

            // run the table operation...
            AsyncTableQuery<Customer> query = conn.Table<Customer>();
            List<Customer> loaded = query.ToListAsync().Result;

            // check that we got them all back...
            Assert.AreEqual(5, loaded.Count);
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[0].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[1].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[2].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[3].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[4].Id));
        }

        [Test]
        public void TestUpdateAsync()
        {
            // create...
            Customer customer = CreateCustomer();

            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            conn.CreateTableAsync<Customer>().Wait();

            // run...
            conn.InsertAsync(customer).Wait();

            // change it...
            string newEmail = Guid.NewGuid().ToString();
            customer.Email = newEmail;

            // save it...
            conn.UpdateAsync(customer).Wait();

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // load it back - should be changed...
                var loaded = check.Get<Customer>(customer.Id);
                Assert.AreEqual(newEmail, loaded.Email);
            }
        }
    }
}