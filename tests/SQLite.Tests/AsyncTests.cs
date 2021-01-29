using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{



	// @mbrit - 2012-05-14 - NOTE - the lack of async use in this class is because the VS11 test runner falsely
	// reports any failing test as succeeding if marked as async. Should be fixed in the "June 2012" drop...

	public class Customer
	{
		[AutoIncrement, PrimaryKey]
		public int Id { get; set; }

		[MaxLength (64)]
		public string FirstName { get; set; }

		[MaxLength (64)]
		public string LastName { get; set; }

		[MaxLength (64), Indexed]
		public string Email { get; set; }

		[MaxLength (64), Indexed]
		public string Address { get; set; }

		[MaxLength (64), Indexed]
		public string Country { get; set; }
	}

	/// <summary>
	/// Defines tests that exercise async behaviour.
	/// </summary>
	[TestFixture]
	public class AsyncTests
	{
        // platform independent deletefile function
		private static void DeleteFile(string filePath)
		{
#if NETFX_CORE
			try {
				var f = Windows.Storage.StorageFile.GetFileFromPathAsync (filePath).AsTask ().Result;
				f.DeleteAsync ().AsTask ().Wait ();
			}
			catch (Exception) {
			}
#else
			System.IO.File.Delete(filePath);
#endif
		}


		// introduced to allow tests run each one with its own private database file, so they can be run in total isolation and in parallel
		// (by test Nunit runners that allow it)
		public class TestEnvironment : IDisposable
		{
			public string DatabaseFilePath { get; }
			public SQLiteAsyncConnection Connection { get; }
			public string ConnectionString { get; }
			public TestEnvironment([System.Runtime.CompilerServices.CallerMemberName] string TestMethodName = "unknown")
			{
				// each test gets its own database file named after the test itself.. and we include also a random number in the name
				// so it is possible to run concurrently the same test on multiple threads (some task runners allow this "stress testing" execution mode
				string DatabaseName = nameof(AsyncTests) + '_' + TestMethodName+ '_' + Guid.NewGuid() + ".db";
#if NETFX_CORE
				DatabaseFilePath = Path.Combine (Windows.Storage.ApplicationData.Current.LocalFolder.Path, DatabaseName);

#else
				ConnectionString = Path.Combine(Path.GetTempPath(), DatabaseName);
				DatabaseFilePath = ConnectionString;
#endif
				DeleteFile(DatabaseFilePath);

				Connection = new SQLiteAsyncConnection(ConnectionString);
			}
			
			#region IDisposable Support
			private bool alreadyDisposed = false; // To detect redundant calls

			protected virtual void Dispose()
			{
				if (alreadyDisposed)
					return;
				alreadyDisposed = true;

				this.Connection?.CloseAsync().Wait();
				DeleteFile(DatabaseFilePath);
			}

			~TestEnvironment()
			{
				Dispose();
			}

			// This code added to correctly implement the disposable pattern.
			void IDisposable.Dispose()
			{
				Dispose();
				GC.SuppressFinalize(this);
			}
			#endregion

		}

		[Test]
		public async Task EnableWalAsync ()
		{
			var path = Path.GetTempFileName ();
			var connection = new SQLiteAsyncConnection (path);
			try
			{
				await connection.EnableWriteAheadLoggingAsync().ConfigureAwait(false);
			}
			finally
			{
				connection.CloseAsync().Wait();
				DeleteFile(path);
			}
		}

		[Test, NUnit.Framework.Ignore ("Mac sqlite does not have this function")]
		public async Task EnableLoadExtensionAsync ()
		{
			using (var env = new TestEnvironment ()) {
				var connection = env.Connection;

				await connection.EnableLoadExtensionAsync (true);
			}
		}

		[Test]
		public async Task InsertAsyncWithType ()
		{
			using (var env = new TestEnvironment ()) {
				var connection = env.Connection;
				await connection.CreateTableAsync<Customer> ();

				var customer = new Customer {
					FirstName = "Joe"
				};
				await connection.InsertAsync (customer, typeof(Customer));
				var c = await connection.QueryAsync<Customer> ("select * from Customer");
				Assert.AreEqual ("Joe", c[0].FirstName);
			}
		}

		[Test]
		public async Task InsertAsyncWithExtra ()
		{
			using (var env = new TestEnvironment ()) {
				var connection = env.Connection;
				await connection.CreateTableAsync<Customer> ();

				var customer = new Customer {
					FirstName = "Joe"
				};
				await connection.InsertAsync (customer, "or replace");
				var c = await connection.QueryAsync<Customer> ("select * from Customer");
				Assert.AreEqual ("Joe", c[0].FirstName);
			}
		}

		[Test]
		public async Task InsertAsyncWithTypeAndExtra ()
		{
			using (var env = new TestEnvironment ()) {
				var connection = env.Connection;
				await connection.CreateTableAsync<Customer> ();

				var customer = new Customer {
					FirstName = "Joe"
				};
				await connection.InsertAsync (customer, "or replace", typeof(Customer));
				var c = await connection.QueryAsync<Customer> ("select * from Customer");
				Assert.AreEqual ("Joe", c[0].FirstName);
			}
		}

		[Test]
		public async Task InsertOrReplaceAsync ()
		{
			using (var env = new TestEnvironment ()) {
				var connection = env.Connection;
				await connection.CreateTableAsync<Customer> ();

				var customer = new Customer {
					FirstName = "Joe"
				};
				await connection.InsertOrReplaceAsync (customer);
				var c = await connection.QueryAsync<Customer> ("select * from Customer");
				Assert.AreEqual ("Joe", c[0].FirstName);
			}
		}

		[Test]
		public async Task InsertOrReplaceAsyncWithType ()
		{
			using (var env = new TestEnvironment ()) {
				var connection = env.Connection;
				await connection.CreateTableAsync<Customer> ();

				var customer = new Customer {
					FirstName = "Joe"
				};
				await connection.InsertOrReplaceAsync (customer, typeof(Customer));
				var c = await connection.QueryAsync<Customer> ("select * from Customer");
				Assert.AreEqual ("Joe", c[0].FirstName);
			}
		}

		[Test]
		public async Task QueryAsync ()
		{
			using (var env = new TestEnvironment())
			{
				var connection = env.Connection;
				await connection.CreateTableAsync<Customer>().ConfigureAwait(false);
				var customer = new Customer
				{
					FirstName = "Joe"
				};
				await connection.InsertAsync(customer);
				await connection.QueryAsync<Customer>("select * from Customer");
			}
		}

		[Test]
		public async Task MemoryQueryAsync ()
		{
			var connection = new SQLiteAsyncConnection (":memory:", false);
			try
			{
				await connection.CreateTableAsync<Customer>().ConfigureAwait(false);

				var customer = new Customer
				{
					FirstName = "Joe"
				};

				await connection.InsertAsync(customer);

				await connection.QueryAsync<Customer>("select * from Customer");
			}
			finally
			{
				connection.CloseAsync().Wait();
			}
		}

		[Test]
		public async Task BusyTime ()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				await conn.CreateTableAsync<Customer>();

				var defaultBusyTime = conn.GetBusyTimeout();
				Assert.True(defaultBusyTime > TimeSpan.FromMilliseconds(999));

				await conn.SetBusyTimeoutAsync(TimeSpan.FromSeconds(10));
				var newBusyTime = conn.GetBusyTimeout();
				Assert.True(newBusyTime > TimeSpan.FromMilliseconds(9999));
			}
		}

		[Test]
		public async Task StressAsync ()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				await conn.CreateTableAsync<Customer>();

				await conn.SetBusyTimeoutAsync(TimeSpan.FromSeconds(1));

				var n = 500;
				var errors = new List<string>();
				var tasks = new List<Task>();
				for (var i = 0; i < n; i++)
				{
					var ii = i;

					tasks.Add(Task.Run(async () =>
					{
						try
						{
							var obj = new Customer
							{
								FirstName = ii.ToString(),
							};
							await conn.InsertAsync(obj).ConfigureAwait(false);
							if (obj.Id == 0)
							{
								lock (errors)
								{
									errors.Add("Bad Id");
								}
							}
							var query = await (from c in conn.Table<Customer>() where c.Id == obj.Id select c).ToListAsync().ConfigureAwait(false);
							var obj2 = query.FirstOrDefault();
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
								errors.Add($"[{ii}] {ex}");
							}
						}
					}));
				}

				await Task.WhenAll(tasks.ToArray());

				var count = await conn.Table<Customer>().CountAsync();

				foreach (var e in errors)
				{
					Console.WriteLine("ERROR " + e);
				}

				Assert.AreEqual(0, errors.Count, string.Join(", ", errors));
				Assert.AreEqual(n, count);
			}
		}

		[Test]
		public void TestCreateTableAsync ()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				// drop the customer table...
				conn.ExecuteAsync("drop table if exists Customer").Wait();
				// run...
				conn.CreateTableAsync<Customer>().Wait();
				// check...
				using (SQLiteConnection check = new SQLiteConnection(env.ConnectionString))
				{
					// run it - if it's missing we'll get a failure...
					check.Execute("select * from Customer");
				}
			}
		}


		[Test]
		public async Task DropTableAsync ()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				await conn.CreateTableAsync<Customer>();

				// drop it...
				await conn.DropTableAsync<Customer>();

				// check...
				using (var check = new SQLiteConnection(env.ConnectionString))
				{
					// load it back and check - should be missing
					var command = check.CreateCommand("select name from sqlite_master where type='table' and name='customer'");
					Assert.IsNull(command.ExecuteScalar<string>());
				}
			}
		}

		[Test]
		public async Task DropTableAsyncNonGeneric ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;
				await conn.CreateTableAsync<Customer> ();

				// drop it...
				await conn.DropTableAsync (await conn.GetMappingAsync(typeof(Customer)));

				// check...
				using (var check = new SQLiteConnection (env.ConnectionString)) {
					// load it back and check - should be missing
					var command = check.CreateCommand ("select name from sqlite_master where type='table' and name='customer'");
					Assert.IsNull (command.ExecuteScalar<string> ());
				}
			}
		}

		private Customer CreateCustomer (string address = null, string country = null, string firstName = "")
		{
			Customer customer = new Customer () {
				FirstName = string.IsNullOrEmpty(firstName) ? "foo" : firstName,
				LastName = "bar",
				Email = Guid.NewGuid ().ToString (),
				Address = address,
				Country = country
			};
			return customer;
		}

		[Test]
		public void TestInsertAsync()
		{
			// create...
			Customer customer = this.CreateCustomer();

			// connect...
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// run...
				conn.InsertAsync(customer).Wait();

				// check that we got an id...
				Assert.AreNotEqual(0, customer.Id);

				// check...
				using (SQLiteConnection check = new SQLiteConnection(env.ConnectionString))
				{
					// load it back...
					Customer loaded = check.Get<Customer>(customer.Id);
					Assert.AreEqual(loaded.Id, customer.Id);
				}
			}
		}

		[Test]
		public void UpdateAsync ()
		{
			// create...
			Customer customer = CreateCustomer ();

			// connect...
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// run...
				conn.InsertAsync(customer).Wait();

				// change it...
				string newEmail = Guid.NewGuid().ToString();
				customer.Email = newEmail;

				// save it...
				conn.UpdateAsync(customer).Wait();

				// check...
				using (SQLiteConnection check = new SQLiteConnection(env.ConnectionString))
				{
					// load it back - should be changed...
					Customer loaded = check.Get<Customer>(customer.Id);
					Assert.AreEqual(newEmail, loaded.Email);
				}
			}
		}

		[Test]
		public async Task UpdateAsyncWithType ()
		{
			// create...
			Customer customer = CreateCustomer ();

			// connect...
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;
				await conn.CreateTableAsync<Customer> ();

				// run...
				await conn.InsertAsync (customer);

				// change it...
				string newEmail = Guid.NewGuid ().ToString ();
				customer.Email = newEmail;

				// save it...
				await conn.UpdateAsync (customer, typeof (Customer));

				// check...
				var loaded = await conn.GetAsync<Customer> (customer.Id);
				Assert.AreEqual (newEmail, loaded.Email);
			}
		}

		[Test]
		public async Task UpdateAllAsync ()
		{
			// create...
			var customer1 = CreateCustomer (firstName: "Frank");
			var customer2 = CreateCustomer (country: "Mexico");
			var customers = new[] { customer1, customer2 };

			// connect...
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;
				await conn.CreateTableAsync<Customer> ();

				// run...
				await conn.InsertAllAsync (customers);

				// change it...
				string newEmail1 = Guid.NewGuid ().ToString ();
				string newEmail2 = Guid.NewGuid ().ToString ();
				customer1.Email = newEmail1;
				customer2.Email = newEmail2;

				// save it...
				await conn.UpdateAllAsync (customers);

				// check...
				var loaded = await conn.Table<Customer> ().ToListAsync();
				Assert.AreEqual (1, loaded.Count(x => x.Email == newEmail1));
				Assert.AreEqual (1, loaded.Count(x => x.Email == newEmail2));
			}
		}

		[Test]
		public void TestDeleteAsync ()
		{
			// create...
			Customer customer = CreateCustomer ();

			// connect...
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// run...
				conn.InsertAsync(customer).Wait();

				// delete it...
				conn.DeleteAsync(customer).Wait();

				// check...
				using (SQLiteConnection check = new SQLiteConnection(env.ConnectionString))
				{
					// load it back - should be null...
					var loaded = check.Table<Customer>().Where(v => v.Id == customer.Id).ToList();
					Assert.AreEqual(0, loaded.Count);
				}
			}
		}

		[Test]
		public void GetAsync ()
		{
			// create...
			Customer customer = new Customer ();
			customer.FirstName = "foo";
			customer.LastName = "bar";
			customer.Email = Guid.NewGuid ().ToString ();

			// connect and insert...
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				conn.InsertAsync(customer).Wait();

				// check...
				Assert.AreNotEqual(0, customer.Id);

				// get it back...
				var task = conn.GetAsync<Customer>(customer.Id);
				task.Wait();
				Customer loaded = task.Result;

				// check...
				Assert.AreEqual(customer.Id, loaded.Id);
			}
		}

		[Test]
		public void FindAsyncWithExpression()
		{
			// create...
			Customer customer = new Customer();
			customer.FirstName = "foo";
			customer.LastName = "bar";
			customer.Email = Guid.NewGuid().ToString();

			// connect and insert...
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				conn.InsertAsync(customer).Wait();

				// check...
				Assert.AreNotEqual(0, customer.Id);

				// get it back...
				var task = conn.FindAsync<Customer>(x => x.Id == customer.Id);
				task.Wait();
				Customer loaded = task.Result;

				// check...
				Assert.AreEqual(customer.Id, loaded.Id);
			}
		}

		[Test]
		public void FindAsyncWithExpressionNull()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// get it back...
				var task = conn.FindAsync<Customer>(x => x.Id == 1);
				task.Wait();
				var loaded = task.Result;

				// check...
				Assert.IsNull(loaded);
			}
		}

		[Test]
		public void TestFindAsyncItemPresent()
		{
			// create...
			Customer customer = CreateCustomer();

			// connect and insert...
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				conn.InsertAsync(customer).Wait();

				// check...
				Assert.AreNotEqual(0, customer.Id);

				// get it back...
				var task = conn.FindAsync<Customer>(customer.Id);
				task.Wait();
				Customer loaded = task.Result;

				// check...
				Assert.AreEqual(customer.Id, loaded.Id);
			}
		}

		[Test]
		public void TestFindAsyncItemMissing()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// now get one that doesn't exist...
				var task = conn.FindAsync<Customer>(-1);
				task.Wait();

				// check...
				Assert.IsNull(task.Result);
			}
		}

		[Test]
		public void TestQueryAsync()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// insert some...
				List<Customer> customers = new List<Customer>();
				for (int index = 0; index < 5; index++) {
					Customer customer = CreateCustomer();

					// insert...
					conn.InsertAsync(customer).Wait();

					// add...
					customers.Add(customer);
				}

				// return the third one...
				var task = conn.QueryAsync<Customer>("select * from customer where id=?", customers[2].Id);
				task.Wait();
				var loaded = task.Result;

				// check...
				Assert.AreEqual(1, loaded.Count);
				Assert.AreEqual(customers[2].Email, loaded[0].Email);
			}
		}
		[Test]
		public void TestSingleQueryAsync()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// insert some...
				List<Customer> customers = new List<Customer>();
				for (int index = 0; index < 5; index++) {
					Customer customer = CreateCustomer();

					// insert...
					conn.InsertAsync(customer).Wait();

					// add...
					customers.Add(customer);
				}

				// return the third one...
				var task = conn.QueryScalarsAsync<string>("select Email from customer where id=?", customers[2].Id);
				task.Wait();
				var loaded = task.Result;

				// check...
				Assert.AreEqual(1, loaded.Count);
				Assert.AreEqual(customers[2].Email, loaded[0]);

				// return the third one...
				var inttest = conn.QueryScalarsAsync<int>("select Id from customer where id=?", customers[2].Id);
				task.Wait();
				var intloaded = inttest.Result;

				// check...
				Assert.AreEqual(1, loaded.Count);
				Assert.AreEqual(customers[2].Id, intloaded[0]);

				// return string list
				var listtask = conn.QueryScalarsAsync<string>("select Email from customer order by Id");
				listtask.Wait();
				var listloaded = listtask.Result;

				// check...
				Assert.AreEqual(5, listloaded.Count);
				Assert.AreEqual(customers[2].Email, listloaded[2]);

				// select columns
				var columnstask = conn.QueryScalarsAsync<string>("select FirstName, LastName from customer");
				Assert.AreEqual(5, columnstask.Result.Count);
			}
		}
		[Test]
		public void TestTableAsync()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				conn.ExecuteAsync("delete from customer").Wait();

				// insert some...
				List<Customer> customers = new List<Customer>();
				for (int index = 0; index < 5; index++) {
					Customer customer = new Customer();
					customer.FirstName = "foo";
					customer.LastName = "bar";
					customer.Email = Guid.NewGuid().ToString();

					// insert...
					conn.InsertAsync(customer).Wait();

					// add...
					customers.Add(customer);
				}

				// run the table operation...
				var query = conn.Table<Customer>();
				var loaded = query.ToListAsync().Result;

				// check that we got them all back...
				Assert.AreEqual(5, loaded.Count);
				Assert.IsNotNull(loaded.Where(v => v.Id == customers[0].Id));
				Assert.IsNotNull(loaded.Where(v => v.Id == customers[1].Id));
				Assert.IsNotNull(loaded.Where(v => v.Id == customers[2].Id));
				Assert.IsNotNull(loaded.Where(v => v.Id == customers[3].Id));
				Assert.IsNotNull(loaded.Where(v => v.Id == customers[4].Id));
			}
		}

		[Test]
		public void TestExecuteAsync()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// do a manual insert...
				string email = Guid.NewGuid().ToString();
				conn.ExecuteAsync("insert into customer (firstname, lastname, email) values (?, ?, ?)",
					"foo", "bar", email).Wait();

				// check...
				using (SQLiteConnection check = new SQLiteConnection(env.ConnectionString)) {
					// load it back - should be null...
					var result = check.Table<Customer>().Where(v => v.Email == email);
					Assert.IsNotNull(result);
				}
			}
		}
		[Test]
		public void TestInsertAllAsync()
		{
			// create a bunch of customers...
			List<Customer> customers = new List<Customer>();
			for (int index = 0; index < 100; index++) {
				Customer customer = new Customer();
				customer.FirstName = "foo";
				customer.LastName = "bar";
				customer.Email = Guid.NewGuid().ToString();
				customers.Add(customer);
			}

			// connect...
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// insert them all...
				conn.InsertAllAsync(customers).Wait();

				// check...
				using (SQLiteConnection check = new SQLiteConnection(env.ConnectionString)) {
					for (int index = 0; index < customers.Count; index++) {
						// load it back and check...
						Customer loaded = check.Get<Customer>(customers[index].Id);
						Assert.AreEqual(loaded.Email, customers[index].Email);
					}
				}
			}
		}

		[Test]
		public void TestRunInTransactionAsync()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				bool transactionCompleted = false;

				// run...
				Customer customer = new Customer();
				conn.RunInTransactionAsync((c) => {
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
				using (SQLiteConnection check = new SQLiteConnection(env.ConnectionString)) {
					// load it back and check - should be deleted...
					var loaded = check.Table<Customer>().Where(v => v.Id == customer.Id).ToList();
					Assert.AreEqual(0, loaded.Count);
				}
			}
		}

		[Test]
		public void TestExecuteScalar()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// check...
				var task = conn.ExecuteScalarAsync<object>("select name from sqlite_master where type='table' and name='customer'");
				task.Wait();
				object name = task.Result;
				Assert.AreNotEqual("Customer", name);
				//delete 
				conn.DeleteAllAsync<Customer>().Wait();
				// check...
				var nodatatask = conn.ExecuteScalarAsync<int>("select Max(Id) from customer where FirstName='hfiueyf8374fhi'");
				task.Wait();
				Assert.AreEqual(0, nodatatask.Result);
			}
		}

		[Test]
		public void TestAsyncTableQueryToListAsync()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// create...
				Customer customer = this.CreateCustomer();
				conn.InsertAsync(customer).Wait();

				// query...
				var query = conn.Table<Customer>();
				var task = query.ToListAsync();
				task.Wait();
				var items = task.Result;

				// check...
				var loaded = items.Where(v => v.Id == customer.Id).First();
				Assert.AreEqual(customer.Email, loaded.Email);
			}
		}

		[Test]
		public void TestAsyncTableQueryToFirstAsyncFound()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// create...
				Customer customer = this.CreateCustomer();
				conn.InsertAsync(customer).Wait();

				// query...
				var query = conn.Table<Customer>().Where(v => v.Id == customer.Id);
				var task = query.FirstAsync();
				task.Wait();
				var loaded = task.Result;

				// check...
				Assert.AreEqual(customer.Email, loaded.Email);
			}
		}

		[Test]
		public void TestAsyncTableQueryToFirstAsyncMissing()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// create...
				Customer customer = this.CreateCustomer();
				conn.InsertAsync(customer).Wait();

				// query...
				var query = conn.Table<Customer>().Where(v => v.Id == -1);
				var task = query.FirstAsync();
				ExceptionAssert.Throws<AggregateException>(() => task.Wait());
			}
		}

		[Test]
		public void TestAsyncTableQueryToFirstOrDefaultAsyncFound()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// create...
				Customer customer = this.CreateCustomer();
				conn.InsertAsync(customer).Wait();

				// query...
				var query = conn.Table<Customer>().Where(v => v.Id == customer.Id);
				var task = query.FirstOrDefaultAsync();
				task.Wait();
				var loaded = task.Result;

				// check...
				Assert.AreEqual(customer.Email, loaded.Email);
			}
		}

		[Test]
		public void TestAsyncTableQueryToFirstOrDefaultAsyncMissing()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// create...
				Customer customer = this.CreateCustomer();
				conn.InsertAsync(customer).Wait();

				// query...
				var query = conn.Table<Customer>().Where(v => v.Id == -1);
				var task = query.FirstOrDefaultAsync();
				task.Wait();
				var loaded = task.Result;

				// check...
				Assert.IsNull(loaded);
			}
		}

		[Test]
		public void TestAsyncTableQueryWhereOperation()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();

				// create...
				Customer customer1 = this.CreateCustomer(string.Empty, "country");
				conn.InsertAsync(customer1).Wait();
				Customer customer2 = this.CreateCustomer("address");
				conn.InsertAsync(customer2).Wait();

				// query...
				var query = conn.Table<Customer>();

				// check...
				var loaded = query.Where(v => v.Id == customer1.Id).ToListAsync().Result.First();
				Assert.AreEqual(customer1.Email, loaded.Email);

				// check...
				var emptyaddress = query.Where(v => string.IsNullOrEmpty(v.Address)).ToListAsync().Result.First();
				Assert.True(string.IsNullOrEmpty(emptyaddress.Address));
				Assert.AreEqual(customer1.Email, emptyaddress.Email);

				// check...
				var nullcountry = query.Where(v => string.IsNullOrEmpty(v.Country)).ToListAsync().Result.First();
				Assert.True(string.IsNullOrEmpty(nullcountry.Country));
				Assert.AreEqual(customer2.Email, nullcountry.Email);

				// check...
				var isnotnullorempty = query.Where(v => !string.IsNullOrEmpty(v.Country)).ToListAsync().Result.First();
				Assert.True(!string.IsNullOrEmpty(isnotnullorempty.Country));
				Assert.AreEqual(customer1.Email, isnotnullorempty.Email);
			}
		}

		[Test]
		public void TestAsyncTableQueryCountAsync()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				conn.ExecuteAsync("delete from customer").Wait();

				// create...
				for (int index = 0; index < 10; index++)
					conn.InsertAsync(this.CreateCustomer()).Wait();

				// load...
				var query = conn.Table<Customer>();
				var task = query.CountAsync();
				task.Wait();

				// check...
				Assert.AreEqual(10, task.Result);
			}
		}

		[Test]
		public void TestAsyncTableOrderBy()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				conn.ExecuteAsync("delete from customer").Wait();

				// create...
				for (int index = 0; index < 10; index++)
					conn.InsertAsync(this.CreateCustomer()).Wait();

				// query...
				var query = conn.Table<Customer>().OrderBy(v => v.Email);
				var task = query.ToListAsync();
				task.Wait();
				var items = task.Result;

				// check...
				Assert.AreEqual(-1, string.Compare(items[0].Email, items[9].Email));
			}
		}

		[Test]
		public void TestAsyncTableOrderByDescending()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				conn.ExecuteAsync("delete from customer").Wait();

				// create...
				for (int index = 0; index < 10; index++)
					conn.InsertAsync(this.CreateCustomer()).Wait();

				// query...
				var query = conn.Table<Customer>().OrderByDescending(v => v.Email);
				var task = query.ToListAsync();
				task.Wait();
				var items = task.Result;

				// check...
				Assert.AreEqual(1, string.Compare(items[0].Email, items[9].Email));
			}
		}

		[Test]
		public void TestAsyncTableQueryTake()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				conn.ExecuteAsync("delete from customer").Wait();

				// create...
				for (int index = 0; index < 10; index++) {
					var customer = this.CreateCustomer();
					customer.FirstName = index.ToString();
					conn.InsertAsync(customer).Wait();
				}

				// query...
				var query = conn.Table<Customer>().OrderBy(v => v.FirstName).Take(1);
				var task = query.ToListAsync();
				task.Wait();
				var items = task.Result;

				// check...
				Assert.AreEqual(1, items.Count);
				Assert.AreEqual("0", items[0].FirstName);
			}
		}

		[Test]
		public void TestAsyncTableQuerySkip()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				conn.ExecuteAsync("delete from customer").Wait();

				// create...
				for (int index = 0; index < 10; index++) {
					var customer = this.CreateCustomer();
					customer.FirstName = index.ToString();
					conn.InsertAsync(customer).Wait();
				}

				// query...
				var query = conn.Table<Customer>().OrderBy(v => v.FirstName).Skip(5);
				var task = query.ToListAsync();
				task.Wait();
				var items = task.Result;

				// check...
				Assert.AreEqual(5, items.Count);
				Assert.AreEqual("5", items[0].FirstName);
			}
		}

		[Test]
		public void TestAsyncTableElementAtAsync()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				conn.ExecuteAsync("delete from customer").Wait();

				// create...
				for (int index = 0; index < 10; index++) {
					var customer = this.CreateCustomer();
					customer.FirstName = index.ToString();
					conn.InsertAsync(customer).Wait();
				}

				// query...
				var query = conn.Table<Customer>().OrderBy(v => v.FirstName);
				var task = query.ElementAtAsync(7);
				task.Wait();
				var loaded = task.Result;

				// check...
				Assert.AreEqual("7", loaded.FirstName);
			}
		}


		[Test]
		public void TestAsyncGetWithExpression()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;
				conn.CreateTableAsync<Customer>().Wait();
				conn.ExecuteAsync("delete from customer").Wait();

				// create...
				for (int index = 0; index < 10; index++)
				{
					var customer = this.CreateCustomer();
					customer.FirstName = index.ToString();
					conn.InsertAsync(customer).Wait();
				}

				// get...
				var result = conn.GetAsync<Customer>(x => x.FirstName == "7");
				result.Wait();
				var loaded = result.Result;
				// check...
				Assert.AreEqual("7", loaded.FirstName);
			}
		}

		[Test]
		public void CreateTable()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;

				var trace = new List<string>();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = conn.CreateTableAsync<Customer>().Result;

				Assert.AreEqual(CreateTableResult.Created, r0);

				var r1 = conn.CreateTableAsync<Customer>().Result;

				Assert.AreEqual(CreateTableResult.Migrated, r1);

				var r2 = conn.CreateTableAsync<Customer>().Result;

				Assert.AreEqual(CreateTableResult.Migrated, r2);

				Assert.AreEqual(4 * 3 + 1, trace.Count);
			}
		}

		[Test]
		public void CreateTableNonGeneric ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;

				var trace = new List<string> ();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = conn.CreateTableAsync (typeof(Customer)).Result;

				Assert.AreEqual (CreateTableResult.Created, r0);

				var r1 = conn.CreateTableAsync (typeof (Customer)).Result;

				Assert.AreEqual (CreateTableResult.Migrated, r1);

				var r2 = conn.CreateTableAsync (typeof (Customer)).Result;

				Assert.AreEqual (CreateTableResult.Migrated, r2);

				Assert.AreEqual (4 * 3 + 1, trace.Count);
			}
		}

		[Test]
		public void CreateTablesNonGeneric ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;

				var trace = new List<string> ();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = conn.CreateTablesAsync(CreateFlags.None, typeof(Customer), typeof(Order)).Result;

				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (Customer)]);
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (Order)]);
			}
		}

		[Test]
		public void CreateTables2 ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;

				var trace = new List<string> ();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = conn.CreateTablesAsync<Customer, Order> ().Result;

				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof(Customer)]);
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof(Order)]);
			}
		}

		[Test]
		public void CreateTables3 ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;

				var trace = new List<string> ();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = conn.CreateTablesAsync<Customer, Order, OrderHistory> ().Result;
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (Customer)]);
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (Order)]);
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (OrderHistory)]);
			}
		}

		[Test]
		public void CreateTables4 ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;

				var trace = new List<string> ();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = conn.CreateTablesAsync<Customer, Order, OrderHistory, Product> ().Result;
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (Customer)]);
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (Order)]);
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (OrderHistory)]);
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (Product)]);
			}
		}

		[Test]
		public void CreateTables5 ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;

				var trace = new List<string> ();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = conn.CreateTablesAsync<Customer, Order, OrderHistory, Product, OrderLine> ().Result;
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (Customer)]);
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (Order)]);
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (OrderHistory)]);
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (Product)]);
				Assert.AreEqual (CreateTableResult.Created, r0.Results[typeof (OrderLine)]);
			}
		}

		[Test]
		public async Task CreateIndexAsync ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;

				var trace = new List<string> ();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = await conn.CreateTableAsync<Customer> ();
				Assert.AreEqual (CreateTableResult.Created, r0);

				var ri = await conn.CreateIndexAsync<Customer>(x => x.FirstName);
				Assert.AreEqual (0, ri);
			}
		}

		[Test]
		public async Task CreateIndexAsyncNonGeneric ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;

				var trace = new List<string> ();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = await conn.CreateTableAsync<Customer> ();
				Assert.AreEqual (CreateTableResult.Created, r0);

				var ri = await conn.CreateIndexAsync (nameof (Customer), nameof (Customer.FirstName));
				Assert.AreEqual (0, ri);
			}
		}

		[Test]
		public async Task CreateIndexAsyncWithName ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;

				var trace = new List<string> ();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = await conn.CreateTableAsync<Customer> ();
				Assert.AreEqual (CreateTableResult.Created, r0);

				var ri = await conn.CreateIndexAsync ("Foofoo", nameof (Customer), nameof (Customer.FirstName));
				Assert.AreEqual (0, ri);
			}
		}

		[Test]
		public async Task CreateIndexAsyncWithManyColumns ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;

				var trace = new List<string> ();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = await conn.CreateTableAsync<Customer> ();
				Assert.AreEqual (CreateTableResult.Created, r0);

				var ri = await conn.CreateIndexAsync (nameof (Customer),
					new[] { nameof (Customer.FirstName), nameof (Customer.LastName) });
				Assert.AreEqual (0, ri);
			}
		}

		[Test]
		public async Task CreateIndexAsyncWithManyColumnsWithName ()
		{
			using (var env = new TestEnvironment ()) {
				var conn = env.Connection;

				var trace = new List<string> ();
				conn.Tracer = trace.Add;
				conn.Trace = true;

				var r0 = await conn.CreateTableAsync<Customer> ();
				Assert.AreEqual (CreateTableResult.Created, r0);

				var ri = await conn.CreateIndexAsync ("Foofoo", nameof (Customer),
					new[] { nameof (Customer.FirstName), nameof (Customer.LastName) });
				Assert.AreEqual (0, ri);
			}
		}

		[Test]
		public void CloseAsync()
		{
			using (var env = new TestEnvironment())
			{
				var conn = env.Connection;

				var r0 = conn.CreateTableAsync<Customer>().Result;

				Assert.AreEqual(CreateTableResult.Created, r0);

				conn.CloseAsync().Wait();
			}
		}

		[Test]
		public async Task Issue881()
		{
			using (var env = new TestEnvironment())
			{
				var connection = env.Connection;

				var t1 = Task.Run(async () =>
				await connection.RunInTransactionAsync(db => Thread.Sleep(TimeSpan.FromSeconds(0.2))));

				var t2 = Task.Run(async () =>
			   {
				   Thread.Sleep(TimeSpan.FromSeconds(0.1));
				   await connection.RunInTransactionAsync(db => Thread.Sleep(TimeSpan.FromSeconds(0.1)));
			   }
				);

				await Task.WhenAll(t1, t2);
			}
		}

	}
}
