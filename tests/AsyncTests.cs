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
        private SQLitePlatformTest _sqlite3Platform;
        private SQLiteConnectionPool _sqliteConnectionPool;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _sqlite3Platform = new SQLitePlatformTest();
            _sqliteConnectionPool = new SQLiteConnectionPool(_sqlite3Platform);
        }

        private SQLiteAsyncConnection GetConnection(ref string path)
        {
            path = _path;
            return new SQLiteAsyncConnection(() => _sqliteConnectionPool.GetConnection(_connectionParameters));
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
        public async Task FindAsyncWithExpression()
        {
            // create...
            var customer = new Customer();
            customer.FirstName = "foo";
            customer.LastName = "bar";
            customer.Email = Guid.NewGuid().ToString();

            // connect and insert...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            await conn.CreateTableAsync<Customer>();
            await conn.InsertAsync(customer);

            // check...
            Assert.AreNotEqual(0, customer.Id);

            // get it back...
            var loaded = await conn.FindAsync<Customer>(x => x.Id == customer.Id);

            // check...
            Assert.AreEqual(customer.Id, loaded.Id);
        }

        [Test]
        public async Task FindAsyncWithExpressionNull()
        {
            // connect and insert...
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();

            // get it back...
            var loaded = await conn.FindAsync<Customer>(x => x.Id == 1);

            // check...
            Assert.IsNull(loaded);
        }

        [Test]
        public async Task GetAsync()
        {
            // create...
            var customer = new Customer();
            customer.FirstName = "foo";
            customer.LastName = "bar";
            customer.Email = Guid.NewGuid().ToString();

            // connect and insert...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            await conn.CreateTableAsync<Customer>();
            await conn.InsertAsync(customer);

            // check...
            Assert.AreNotEqual(0, customer.Id);

            // get it back...
            var loaded = await conn.GetAsync<Customer>(customer.Id);

            // check...
            Assert.AreEqual(customer.Id, loaded.Id);
        }

        [Test]
        public async Task StressAsync()
        {
            string path = null;
            SQLiteAsyncConnection globalConn = GetConnection(ref path);

            await globalConn.CreateTableAsync<Customer>();

            int n = 500;
            var errors = new List<string>();
            var tasks = new List<Task>();
            for (int i = 0; i < n; i++)
            {
                tasks.Add(Task.Factory.StartNew(async delegate
                {
                    try
                    {
                        SQLiteAsyncConnection conn = GetConnection();
                        var obj = new Customer
                        {
                            FirstName = i.ToString(),
                        };
                        await conn.InsertAsync(obj);
                        if (obj.Id == 0)
                        {
                            lock (errors)
                            {
                                errors.Add("Bad Id");
                            }
                        }
                        var obj3 = await (from c in conn.Table<Customer>() where c.Id == obj.Id select c).ToListAsync();
                        Customer obj2 = obj3.FirstOrDefault();
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
                }));
            }

            await Task.WhenAll(tasks);
            int count = await globalConn.Table<Customer>().CountAsync();

            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(n, count);
        }

        [Test]
        public async Task TestAsyncGetWithExpression()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();
            await conn.ExecuteAsync("delete from customer");

            // create...
            for (int index = 0; index < 10; index++)
            {
                Customer customer = CreateCustomer();
                customer.FirstName = index.ToString();
                await conn.InsertAsync(customer);
            }

            // get...
            var loaded = await conn.GetAsync<Customer>(x => x.FirstName == "7");
            // check...
            Assert.AreEqual("7", loaded.FirstName);
        }

        [Test]
        public async Task TestAsyncTableElementAtAsync()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();
            await conn.ExecuteAsync("delete from customer");

            // create...
            for (int index = 0; index < 10; index++)
            {
                Customer customer = CreateCustomer();
                customer.FirstName = index.ToString();
                await conn.InsertAsync(customer);
            }

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.FirstName);
            var loaded = await query.ElementAtAsync(7);

            // check...
            Assert.AreEqual("7", loaded.FirstName);
        }

        [Test]
        public async Task TestAsyncTableOrderBy()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();
            await conn.ExecuteAsync("delete from customer");

            // create...
            for (int index = 0; index < 10; index++)
            {
                await conn.InsertAsync(CreateCustomer());
            }

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.Email);
            var items = await query.ToListAsync();

            // check...
            Assert.AreEqual(-1, string.Compare(items[0].Email, items[9].Email));
        }

        [Test]
        public async Task TestAsyncTableOrderByDescending()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();
            await conn.ExecuteAsync("delete from customer");

            // create...
            for (int index = 0; index < 10; index++)
            {
                await conn.InsertAsync(CreateCustomer());
            }

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderByDescending(v => v.Email);
            var items = await query.ToListAsync();

            // check...
            Assert.AreEqual(1, string.Compare(items[0].Email, items[9].Email));
        }

        [Test]
        public async Task TestAsyncTableThenBy()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();
            await conn.ExecuteAsync("delete from customer");

            // create...
            for (int index = 0; index < 10; index++)
            {
                await conn.InsertAsync(CreateCustomer());
            }
            var preceedingFirstNameCustomer = CreateCustomer();
            preceedingFirstNameCustomer.FirstName = "a" + preceedingFirstNameCustomer.FirstName;
            await conn.InsertAsync(preceedingFirstNameCustomer);

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.FirstName).ThenBy(v => v.Email);
            var items = await query.ToListAsync();

            // check...
            var list = (await conn.Table<Customer>().ToListAsync()).OrderBy(v => v.FirstName).ThenBy(v => v.Email).ToList();
            for (var i = 0; i < list.Count; i++)
                Assert.AreEqual(list[i].Email, items[i].Email);
        }

        [Test]
        public async Task TestAsyncTableThenByDescending()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();
            await conn.ExecuteAsync("delete from customer");

            // create...
            for (int index = 0; index < 10; index++)
            {
                await conn.InsertAsync(CreateCustomer());
            }
            var preceedingFirstNameCustomer = CreateCustomer();
            preceedingFirstNameCustomer.FirstName = "a" + preceedingFirstNameCustomer.FirstName;
            await conn.InsertAsync(preceedingFirstNameCustomer);

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.FirstName).ThenByDescending(v => v.Email);
            var items = await query.ToListAsync();


            // check...
            var list = (await conn.Table<Customer>().ToListAsync()).OrderBy(v => v.FirstName).ThenByDescending(v => v.Email).ToList();
            for (var i = 0; i < list.Count; i++)
                Assert.AreEqual(list[i].Email, items[i].Email);
        }

        [Test]
        public async Task TestAsyncTableQueryCountAsync()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();
            await conn.ExecuteAsync("delete from customer");

            // create...
            for (int index = 0; index < 10; index++)
            {
                await conn.InsertAsync(CreateCustomer());
            }

            // load...
            AsyncTableQuery<Customer> query = conn.Table<Customer>();
            var result = await query.CountAsync();

            // check...
            Assert.AreEqual(10, result);
        }

        [Test]
        public async Task TestAsyncTableQuerySkip()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();
            await conn.ExecuteAsync("delete from customer");

            // create...
            for (int index = 0; index < 10; index++)
            {
                Customer customer = CreateCustomer();
                customer.FirstName = index.ToString();
                await conn.InsertAsync(customer);
            }

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.FirstName).Skip(5);
            var items = await query.ToListAsync();

            // check...
            Assert.AreEqual(5, items.Count);
            Assert.AreEqual("5", items[0].FirstName);
        }

        [Test]
        public async Task TestAsyncTableQueryTake()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();
            await conn.ExecuteAsync("delete from customer");

            // create...
            for (int index = 0; index < 10; index++)
            {
                Customer customer = CreateCustomer();
                customer.FirstName = index.ToString();
                await conn.InsertAsync(customer);
            }

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().OrderBy(v => v.FirstName).Take(1);
            var items = await query.ToListAsync();

            // check...
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("0", items[0].FirstName);
        }

        [Test]
        public async Task TestAsyncTableQueryToFirstAsyncFound()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();

            // create...
            Customer customer = CreateCustomer();
            await conn.InsertAsync(customer);

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().Where(v => v.Id == customer.Id);
            var loaded = await query.FirstAsync();
            
            // check...
            Assert.AreEqual(customer.Email, loaded.Email);
        }

        [Test]
        public async Task TestAsyncTableQueryToFirstAsyncMissing()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();

            // create...
            Customer customer = CreateCustomer();
            await conn.InsertAsync(customer);

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().Where(v => v.Id == -1);
            Assert.That(async () => await query.FirstAsync(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task TestAsyncTableQueryToFirstOrDefaultAsyncFound()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();

            // create...
            Customer customer = CreateCustomer();
            await conn.InsertAsync(customer);

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().Where(v => v.Id == customer.Id);
            Customer loaded = await query.FirstOrDefaultAsync();

            // check...
            Assert.AreEqual(customer.Email, loaded.Email);
        }

        [Test]
        public async Task TestAsyncTableQueryToFirstOrDefaultAsyncMissing()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();

            // create...
            Customer customer = CreateCustomer();
            await conn.InsertAsync(customer);

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>().Where(v => v.Id == -1);
            Customer loaded = await query.FirstOrDefaultAsync();

            // check...
            Assert.IsNull(loaded);
        }

        [Test]
        public async Task TestAsyncTableQueryToListAsync()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();

            // create...
            Customer customer = CreateCustomer();
            await conn.InsertAsync(customer);

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>();
            List<Customer> items = await query.ToListAsync();

            // check...
            Customer loaded = items.Where(v => v.Id == customer.Id).First();
            Assert.AreEqual(customer.Email, loaded.Email);
        }

        [Test]
        public async Task TestAsyncTableQueryWhereOperation()
        {
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();

            // create...
            Customer customer = CreateCustomer();
            await conn.InsertAsync(customer);

            // query...
            AsyncTableQuery<Customer> query = conn.Table<Customer>();
            var items = await query.ToListAsync();

            // check...
            Customer loaded = items.Where(v => v.Id == customer.Id).First();
            Assert.AreEqual(customer.Email, loaded.Email);
        }

        [Test]
        public async Task TestCreateTableAsync()
        {
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);

            // drop the customer table...
            await conn.ExecuteAsync("drop table if exists Customer");

            // run...
            await conn.CreateTableAsync<Customer>();

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // run it - if it's missing we'll get a failure...
                check.Execute("select * from Customer");
            }
        }

        [Test]
        public async Task TestDeleteAsync()
        {
            // create...
            Customer customer = CreateCustomer();

            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            await conn.CreateTableAsync<Customer>();

            // run...
            await conn.InsertAsync(customer);

            // delete it...
            await conn.DeleteAsync(customer);

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // load it back - should be null...
                List<Customer> loaded = check.Table<Customer>().Where(v => v.Id == customer.Id).ToList();
                Assert.AreEqual(0, loaded.Count);
            }
        }

        [Test]
        public async Task TestDropTableAsync()
        {
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            await conn.CreateTableAsync<Customer>();

            // drop it...
            await conn.DropTableAsync<Customer>();

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
        public async Task TestExecuteAsync()
        {
            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            await conn.CreateTableAsync<Customer>();

            // do a manual insert...
            string email = Guid.NewGuid().ToString();
            await conn.ExecuteAsync("insert into customer (firstname, lastname, email) values (?, ?, ?)",
                "foo", "bar", email);

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // load it back - should be null...
                TableQuery<Customer> result = check.Table<Customer>().Where(v => v.Email == email);
                Assert.IsNotNull(result);
            }
        }

        [Test]
        public async Task TestExecuteScalar()
        {
            // connect...
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();

            // check...
            object name = await conn.ExecuteScalarAsync<object>("select name from sqlite_master where type='table' and name='customer'");
            Assert.AreNotEqual("Customer", name);
        }

        [Test]
        public async Task TestFindAsyncItemMissing()
        {
            // connect and insert...
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();

            // now get one that doesn't exist...
            Task<Customer> task = conn.FindAsync<Customer>(-1);

            // check...
            Assert.IsNull(await task);
        }

        [Test]
        public async Task TestFindAsyncItemPresent()
        {
            // create...
            Customer customer = CreateCustomer();

            // connect and insert...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            await conn.CreateTableAsync<Customer>();
            await conn.InsertAsync(customer);

            // check...
            Assert.AreNotEqual(0, customer.Id);

            // get it back...
            Task<Customer> task = conn.FindAsync<Customer>(customer.Id);
            Customer loaded = await task;

            // check...
            Assert.AreEqual(customer.Id, loaded.Id);
        }

        [Test]
        public async Task TestInsertAllAsync()
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
            await conn.CreateTableAsync<Customer>();

            // insert them all...
            await conn.InsertAllAsync(customers);

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
        public async Task TestInsertAsync()
        {
            // create...
            Customer customer = CreateCustomer();

            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            await conn.CreateTableAsync<Customer>();

            // run...
            await conn.InsertAsync(customer);

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
        public async Task TestInsertOrReplaceAllAsync()
        {
            // create a bunch of customers...
            var customers = new List<Customer>();
            for (int index = 0; index < 100; index++)
            {
                var customer = new Customer();
                customer.Id = index;
                customer.FirstName = "foo";
                customer.LastName = "bar";
                customer.Email = Guid.NewGuid().ToString();
                customers.Add(customer);
            }

            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            await conn.CreateTableAsync<Customer>();

            // insert them all...
            await conn.InsertOrReplaceAllAsync(customers);

            // change the existing ones...
            foreach (var customer in customers)
            {
                customer.FirstName = "baz";
                customer.LastName = "biz";
            }

            // ... and add a few more
            for (int index = 100; index < 200; index++)
            {
                var customer = new Customer();
                customer.Id = index;
                customer.FirstName = "foo";
                customer.LastName = "bar";
                customer.Email = Guid.NewGuid().ToString();
                customers.Add(customer);
            }

            // insert them all, replacing the already existing ones
            await conn.InsertOrReplaceAllAsync(customers);

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                for (int index = 0; index < customers.Count; index++)
                {
                    // load it back and check...
                    var loaded = check.Get<Customer>(customers[index].Id);
                    Assert.AreEqual(loaded.FirstName, customers[index].FirstName);
                    Assert.AreEqual(loaded.LastName, customers[index].LastName);
                    Assert.AreEqual(loaded.Email, customers[index].Email);
                }
            }

        }

        [Test]
        public async Task TestInsertOrReplaceAsync()
        {
            // create...
            var customer = new Customer();
            customer.Id = 42;
            customer.FirstName = "foo";
            customer.LastName = "bar";
            customer.Email = Guid.NewGuid().ToString();

            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            await conn.CreateTableAsync<Customer>();

            // run...
            await conn.InsertOrReplaceAsync(customer);

            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // load it back...
                var loaded = check.Get<Customer>(customer.Id);
                Assert.AreEqual(loaded.Id, customer.Id);
                Assert.AreEqual(loaded.FirstName, customer.FirstName);
                Assert.AreEqual(loaded.LastName, customer.LastName);
                Assert.AreEqual(loaded.Email, customer.Email);
            }

            // change ...
            customer.FirstName = "baz";
            customer.LastName = "biz";
            customer.Email = Guid.NewGuid().ToString();

            // replace...
            await conn.InsertOrReplaceAsync(customer);

            // check again...
            // check...
            using (var check = new SQLiteConnection(_sqlite3Platform, path))
            {
                // load it back...
                var loaded = check.Get<Customer>(customer.Id);
                Assert.AreEqual(loaded.Id, customer.Id);
                Assert.AreEqual(loaded.FirstName, customer.FirstName);
                Assert.AreEqual(loaded.LastName, customer.LastName);
                Assert.AreEqual(loaded.Email, customer.Email);
            }
        }

        [Test]
        public async Task TestQueryAsync()
        {
            // connect...
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();

            // insert some...
            var customers = new List<Customer>();
            for (int index = 0; index < 5; index++)
            {
                Customer customer = CreateCustomer();

                // insert...
                await conn.InsertAsync(customer);

                // add...
                customers.Add(customer);
            }

            // return the third one...
            List<Customer> loaded = await conn.QueryAsync<Customer>("select * from customer where id=?", customers[2].Id);

            // check...
            Assert.AreEqual(1, loaded.Count);
            Assert.AreEqual(customers[2].Email, loaded[0].Email);
        }

        [Test]
        public async Task TestRunInTransactionAsync()
        {
            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            await conn.CreateTableAsync<Customer>();
            bool transactionCompleted = false;

            // run...
            var customer = new Customer();
            await conn.RunInTransactionAsync(c =>
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
            });

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
        public async Task TestTableAsync()
        {
            // connect...
            SQLiteAsyncConnection conn = GetConnection();
            await conn.CreateTableAsync<Customer>();
            await conn.ExecuteAsync("delete from customer");

            // insert some...
            var customers = new List<Customer>();
            for (int index = 0; index < 5; index++)
            {
                var customer = new Customer();
                customer.FirstName = "foo";
                customer.LastName = "bar";
                customer.Email = Guid.NewGuid().ToString();

                // insert...
                await conn.InsertAsync(customer);

                // add...
                customers.Add(customer);
            }

            // run the table operation...
            AsyncTableQuery<Customer> query = conn.Table<Customer>();
            List<Customer> loaded = await query.ToListAsync();

            // check that we got them all back...
            Assert.AreEqual(5, loaded.Count);
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[0].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[1].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[2].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[3].Id));
            Assert.IsNotNull(loaded.Where(v => v.Id == customers[4].Id));
        }

        [Test]
        public async Task TestUpdateAsync()
        {
            // create...
            Customer customer = CreateCustomer();

            // connect...
            string path = null;
            SQLiteAsyncConnection conn = GetConnection(ref path);
            await conn.CreateTableAsync<Customer>();

            // run...
            await conn.InsertAsync(customer);

            // change it...
            string newEmail = Guid.NewGuid().ToString();
            customer.Email = newEmail;

            // save it...
            await conn.UpdateAsync(customer);

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