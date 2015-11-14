using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PCLStorage;
using SQLite.Net.Async;
using SQLite.Net.Attributes;
using System.Diagnostics;


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

    public class Customer2
    {
        [PrimaryKey]
        public int Id { get; set; }

        [MaxLength(64)]
        public string FirstName { get; set; }

        [MaxLength(64)]
        public string LastName { get; set; }

        [MaxLength(64)]
        public string Email { get; set; }
    }

    [Table("AGoodTableName")]
    public class AFunnyTableName
    {
        [PrimaryKey]
        public int Id { get; set; }

        [Column("AGoodColumnName")]
        public string AFunnyColumnName { get; set; }
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
            var databaseFile = TestPath.CreateTemporaryDatabase();

            _connectionParameters = new SQLiteConnectionString(databaseFile, false);
        }

        private SQLiteConnectionString _connectionParameters;
        private SQLitePlatformTest _sqlite3Platform;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _sqlite3Platform = new SQLitePlatformTest();
        }

        private SQLiteAsyncConnection GetAsyncConnection()
        {
            return new SQLiteAsyncConnection(() => new SQLiteConnectionWithLock(_sqlite3Platform, _connectionParameters));
        }

        private SQLiteConnection GetSyncConnection()
        {
            return new SQLiteConnectionWithLock(_sqlite3Platform, _connectionParameters);
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            const int defaultBusyTimeout = 100;

            SQLiteAsyncConnection globalConn = GetAsyncConnection();

            // see http://stackoverflow.com/questions/12004426/sqlite-returns-sqlite-busy-in-wal-mode
            var journalMode = await globalConn.ExecuteScalarAsync<string>("PRAGMA journal_mode = wal"); // = wal");
            Debug.WriteLine("journal_mode: " + journalMode);
            //var synchronous = await globalConn.ExecuteScalarAsync<string>("PRAGMA synchronous");        // 2 = FULL
            //Debug.WriteLine("synchronous: " + synchronous);
            //var pageSize = await globalConn.ExecuteScalarAsync<string>("PRAGMA page_size");             // 1024 default
            //Debug.WriteLine("page_size: " + pageSize);
            var busyTimeout = await globalConn.ExecuteScalarAsync<string>(
                string.Format("PRAGMA busy_timeout = {0}", defaultBusyTimeout));
            Debug.WriteLine("busy_timeout: " + busyTimeout);

            await globalConn.CreateTableAsync<Customer>();

            int n = 500;
            var errors = new List<string>();
            var tasks = new List<Task>();
            for (int i = 0; i < n; i++)
            {
                int taskId = i;

                tasks.Add(Task.Run(async () =>
                {
                    string taskStep = "";

                    try
                    {
                        taskStep = "CONNECT";
                        SQLiteAsyncConnection conn = GetAsyncConnection();

                        // each connection retains the global journal_mode but somehow resets busy_timeout to 100
                        busyTimeout = await globalConn.ExecuteScalarAsync<string>(
                            string.Format("PRAGMA busy_timeout = {0}", defaultBusyTimeout));
//                        Debug.WriteLine("busy_timeout: " + busyTimeout);

                        var obj = new Customer
                        {
                            FirstName = taskId.ToString(),
                        };

                        taskStep = "INSERT";
                        await conn.InsertAsync(obj);

                        if (obj.Id == 0)
                        {
                            lock (errors)
                            {
                                errors.Add("Bad Id");
                            }
                        }

                        taskStep = "SELECT";
                        var obj3 = await (from c in conn.Table<Customer>() where c.Id == obj.Id select c).ToListAsync();
                        Customer obj2 = obj3.FirstOrDefault();
                        if (obj2 == null)
                        {
                            lock (errors)
                            {
                                errors.Add("Failed query");
                            }
                        }

//                        Debug.WriteLine("task {0} with id {1} and name {2}", taskId, obj.Id, obj.FirstName);
                    }
                    catch (Exception ex)
                    {
                        lock (errors)
                        {
                            errors.Add(string.Format("{0}: {1}", taskStep, ex.Message));
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.AreEqual(n, tasks.Where(t => t.IsCompleted).Count());

            //int j = 0;
            //foreach (var error in errors)
            //{
            //    Debug.WriteLine("{0} {1}", j++, error);
            //}

            Assert.AreEqual(0, errors.Count, "Error in task runs");

            int count = await globalConn.Table<Customer>().CountAsync();
            Assert.AreEqual(n, count, "Not enough items in table");

            // TODO: get out of wal mode - currently fails with 'database is locked'
//            journalMode = await globalConn.ExecuteScalarAsync<string>("PRAGMA journal_mode = delete");
        }

        [Test]
        public async Task TestAsyncGetWithExpression()
        {
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();

            // drop the customer table...
            await conn.ExecuteAsync("drop table if exists Customer");

            // run...
            await conn.CreateTableAsync<Customer>();

            // check...
            using (var check = GetSyncConnection())
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
            await conn.CreateTableAsync<Customer>();

            // run...
            await conn.InsertAsync(customer);

            // delete it...
            await conn.DeleteAsync(customer);

            // check...
            using (var check = GetSyncConnection())
            {
                // load it back - should be null...
                List<Customer> loaded = check.Table<Customer>().Where(v => v.Id == customer.Id).ToList();
                Assert.AreEqual(0, loaded.Count);
            }
        }

        [Test]
        public async Task TestDropTableAsync()
        {
            SQLiteAsyncConnection conn = GetAsyncConnection();
            await conn.CreateTableAsync<Customer>();

            // drop it...
            await conn.DropTableAsync<Customer>();

            // check...
            using (var check = GetSyncConnection())
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
            await conn.CreateTableAsync<Customer>();

            // do a manual insert...
            string email = Guid.NewGuid().ToString();
            await conn.ExecuteAsync("insert into customer (firstname, lastname, email) values (?, ?, ?)",
                "foo", "bar", email);

            // check...
            using (var check = GetSyncConnection())
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
            await conn.CreateTableAsync<Customer>();

            // check...
            object name = await conn.ExecuteScalarAsync<object>("select name from sqlite_master where type='table' and name='customer'");
            Assert.AreNotEqual("Customer", name);
        }

        [Test]
        public async Task TestFindAsyncItemMissing()
        {
            // connect and insert...
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
            await conn.CreateTableAsync<Customer>();

            // insert them all...
            await conn.InsertAllAsync(customers);

            // check...
            using (var check = GetSyncConnection())
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
            await conn.CreateTableAsync<Customer>();

            // run...
            await conn.InsertAsync(customer);

            // check that we got an id...
            Assert.AreNotEqual(0, customer.Id);

            // check...
            using (var check = GetSyncConnection())
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            using (var check = GetSyncConnection())
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
            await conn.CreateTableAsync<Customer>();

            // run...
            await conn.InsertOrReplaceAsync(customer);

            // check...
            using (var check = GetSyncConnection())
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
            using (var check = GetSyncConnection())
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
        public async Task TestInsertOrIgnoreAllAsync ()
        {
            const string originalFirstName = "foo";
            const string originalLastName = "bar";
            
            // create a bunch of customers...
            var customers = new List<Customer2> ();
            for (int index = 0; index < 100; index++) {
                var customer = new Customer2 ();
                customer.Id = index;
                customer.FirstName = originalFirstName;
                customer.LastName = originalLastName;
                customer.Email = Guid.NewGuid ().ToString ();
                customers.Add (customer);
            }

            // connect...
            SQLiteAsyncConnection conn = GetAsyncConnection ();
            await conn.CreateTableAsync<Customer2> ();

            // insert them all...
            await conn.InsertOrIgnoreAllAsync (customers);

            // change the existing ones...
            foreach (var customer in customers) {
                customer.FirstName = "baz";
                customer.LastName = "biz";
            }

            // ... and add a few more
            for (int index = 100; index < 200; index++) {
                var customer = new Customer2 ();
                customer.Id = index;
                customer.FirstName = originalFirstName;
                customer.LastName = originalLastName;
                customer.Email = Guid.NewGuid ().ToString ();
                customers.Add (customer);
            }

            // insert them all, ignoring the already existing ones
            await conn.InsertOrIgnoreAllAsync (customers);

            // check...
            using (var check = GetSyncConnection()) {
                for (int index = 0; index < customers.Count; index++) {
                    // load it back and check...
                    var loaded = check.Get<Customer2> (customers [index].Id);
                    Assert.AreEqual (loaded.FirstName, originalFirstName);
                    Assert.AreEqual (loaded.LastName, originalLastName);
                    Assert.AreEqual (loaded.Email, customers [index].Email);
                }
            }

        }

        [Test]
        public async Task TestInsertOrIgnoreAsync ()
        {
            const string originalFirstName = "foo";
            const string originalLastName = "bar";

            // create...
            var customer = new Customer2 ();
            customer.Id = 42;
            customer.FirstName = originalFirstName;
            customer.LastName = originalLastName;
            customer.Email = Guid.NewGuid ().ToString ();

            // connect...
            SQLiteAsyncConnection conn = GetAsyncConnection ();
            await conn.CreateTableAsync<Customer2> ();

            // run...
            await conn.InsertOrIgnoreAsync (customer);

            // check...
            using (var check = GetSyncConnection()) {
                // load it back...
                var loaded = check.Get<Customer2> (customer.Id);
                Assert.AreEqual (loaded.Id, customer.Id);
                Assert.AreEqual (loaded.FirstName, originalFirstName);
                Assert.AreEqual (loaded.LastName, originalLastName);
                Assert.AreEqual (loaded.Email, customer.Email);
            }

            // change ...
            customer.FirstName = "baz";
            customer.LastName = "biz";

            // insert or ignore...
            await conn.InsertOrIgnoreAsync (customer);

            // check...
            using (var check = GetSyncConnection()) {
                // load it back...
                var loaded = check.Get<Customer2> (customer.Id);
                Assert.AreEqual (loaded.Id, customer.Id);
                Assert.AreEqual (loaded.FirstName, originalFirstName);
                Assert.AreEqual (loaded.LastName, originalLastName);
                Assert.AreEqual (loaded.Email, customer.Email);
            }
        }

        [Test]
        public async Task TestQueryAsync()
        {
            // connect...
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            using (var check = GetSyncConnection())
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
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
            SQLiteAsyncConnection conn = GetAsyncConnection();
            await conn.CreateTableAsync<Customer>();

            // run...
            await conn.InsertAsync(customer);

            // change it...
            string newEmail = Guid.NewGuid().ToString();
            customer.Email = newEmail;

            // save it...
            await conn.UpdateAsync(customer);

            // check...
            using (var check = GetSyncConnection())
            {
                // load it back - should be changed...
                var loaded = check.Get<Customer>(customer.Id);
                Assert.AreEqual(newEmail, loaded.Email);
            }
        }

        [Test]
        public async Task TestGetMappingAsync()
        {
            // connect...
            SQLiteAsyncConnection conn = GetAsyncConnection();
            await conn.CreateTableAsync<AFunnyTableName>();

            // get mapping...
            TableMapping mapping = await conn.GetMappingAsync<AFunnyTableName>();

            // check...
            Assert.AreEqual("AGoodTableName", mapping.TableName);
            Assert.AreEqual("Id", mapping.Columns[0].Name);
            Assert.AreEqual("AGoodColumnName", mapping.Columns[1].Name);
        }
    }
}
