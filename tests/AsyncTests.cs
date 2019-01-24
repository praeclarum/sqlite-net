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
	}

	/// <summary>
	/// Defines tests that exercise async behaviour.
	/// </summary>
	[TestFixture]
	public class AsyncTests
	{
		private const string DatabaseName = "async.db";

		[Test]
		public async Task EnableWalAsync ()
		{
			var path = Path.GetTempFileName ();
			var connection = new SQLiteAsyncConnection (path);

			await connection.EnableWriteAheadLoggingAsync ().ConfigureAwait (false);
		}

		[Test]
		public async Task QueryAsync ()
		{
			var connection = GetConnection ();
			await connection.CreateTableAsync<Customer> ().ConfigureAwait (false);

			var customer = new Customer {
				FirstName = "Joe"
			};

			await connection.InsertAsync (customer);

			await connection.QueryAsync<Customer> ("select * from Customer");
		}

		[Test]
		public async Task MemoryQueryAsync ()
		{
			var connection = new SQLiteAsyncConnection (":memory:", false);
			await connection.CreateTableAsync<Customer> ().ConfigureAwait (false);

			var customer = new Customer {
				FirstName = "Joe"
			};

			await connection.InsertAsync (customer);

			await connection.QueryAsync<Customer> ("select * from Customer");
		}

		[Test]
		public void StressAsync ()
		{
			string path = null;
			var globalConn = GetConnection (ref path);
			
			globalConn.CreateTableAsync<Customer> ().Wait ();
			
			var threadCount = 0;
			var doneEvent = new AutoResetEvent (false);
			var n = 500;
			var errors = new List<string> ();
			for (var i = 0; i < n; i++) {
				var ii = i;

#if NETFX_CORE
                Task.Run (
#else
				var th = new Thread ((ThreadStart)
#endif
                delegate {
					try {
						var conn = GetConnection ();
						var obj = new Customer {
							FirstName = ii.ToString (),
						};
						conn.InsertAsync (obj).Wait ();
						if (obj.Id == 0) {
							lock (errors) {
								errors.Add ("Bad Id");
							}
						}
						var obj2 = (from c in conn.Table<Customer> () where c.Id == obj.Id select c).ToListAsync ().Result.FirstOrDefault();
						if (obj2 == null) {
							lock (errors) {
								errors.Add ("Failed query");
							}
						}
					}
					catch (Exception ex) {
						lock (errors) {
							errors.Add (ex.Message);
						}
					}
					threadCount++;
					if (threadCount == n) {
						doneEvent.Set();
					}
				});

#if !NETFX_CORE
				th.Start ();
#endif
			}
			doneEvent.WaitOne ();
			
			var count = globalConn.Table<Customer> ().CountAsync ().Result;

			foreach (var e in errors) {
				Console.WriteLine ("ERROR " + e);
			}
			
			Assert.AreEqual (0, errors.Count);
			Assert.AreEqual (n, count);			
		}

		[Test]
		public void TestCreateTableAsync ()
		{
			string path = null;
			var conn = GetConnection (ref path);

			// drop the customer table...
			conn.ExecuteAsync ("drop table if exists Customer").Wait ();

			// run...
			conn.CreateTableAsync<Customer> ().Wait ();

			// check...
			using (SQLiteConnection check = new SQLiteConnection (path)) {
				// run it - if it's missing we'll get a failure...
				check.Execute ("select * from Customer");
			}
		}

		SQLiteAsyncConnection GetConnection ()
		{
			string path = null;
			return GetConnection (ref path);
		}
		
		string _path;
		string _connectionString;
		
		[SetUp]
		public void SetUp()
		{
			SQLite.SQLiteConnectionPool.Shared.Reset ();
#if NETFX_CORE
			_connectionString = DatabaseName;
			_path = Path.Combine (Windows.Storage.ApplicationData.Current.LocalFolder.Path, DatabaseName);
			try {
				var f = Windows.Storage.StorageFile.GetFileFromPathAsync (_path).AsTask ().Result;
				f.DeleteAsync ().AsTask ().Wait ();
			}
			catch (Exception) {
			}
#else
			_connectionString = Path.Combine (Path.GetTempPath (), DatabaseName);
			_path = _connectionString;
			System.IO.File.Delete (_path);
#endif
		}
		
		SQLiteAsyncConnection GetConnection (ref string path)
		{
			path = _path;
			return new SQLiteAsyncConnection (_connectionString);
		}

		[Test]
		public void TestDropTableAsync ()
		{
			string path = null;
			var conn = GetConnection (ref path);
			conn.CreateTableAsync<Customer> ().Wait ();

			// drop it...
			conn.DropTableAsync<Customer> ().Wait ();

			// check...
			using (SQLiteConnection check = new SQLiteConnection (path)) {
				// load it back and check - should be missing
				var command = check.CreateCommand ("select name from sqlite_master where type='table' and name='customer'");
				Assert.IsNull (command.ExecuteScalar<string> ());
			}
		}

		private Customer CreateCustomer ()
		{
			Customer customer = new Customer () {
				FirstName = "foo",
				LastName = "bar",
				Email = Guid.NewGuid ().ToString ()
			};
			return customer;
		}

		[Test]
		public void TestInsertAsync ()
		{
			// create...
			Customer customer = this.CreateCustomer ();

			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			conn.CreateTableAsync<Customer> ().Wait ();

			// run...
			conn.InsertAsync (customer).Wait ();

			// check that we got an id...
			Assert.AreNotEqual (0, customer.Id);

			// check...
			using (SQLiteConnection check = new SQLiteConnection (path)) {
				// load it back...
				Customer loaded = check.Get<Customer> (customer.Id);
				Assert.AreEqual (loaded.Id, customer.Id);
			}
		}

		[Test]
		public void TestUpdateAsync ()
		{
			// create...
			Customer customer = CreateCustomer ();

			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			conn.CreateTableAsync<Customer> ().Wait ();

			// run...
			conn.InsertAsync (customer).Wait ();

			// change it...
			string newEmail = Guid.NewGuid ().ToString ();
			customer.Email = newEmail;

			// save it...
			conn.UpdateAsync (customer).Wait ();

			// check...
			using (SQLiteConnection check = new SQLiteConnection (path)) {
				// load it back - should be changed...
				Customer loaded = check.Get<Customer> (customer.Id);
				Assert.AreEqual (newEmail, loaded.Email);
			}
		}

		[Test]
		public void TestDeleteAsync ()
		{
			// create...
			Customer customer = CreateCustomer ();

			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			conn.CreateTableAsync<Customer> ().Wait ();

			// run...
			conn.InsertAsync (customer).Wait ();

			// delete it...
			conn.DeleteAsync (customer).Wait ();

			// check...
			using (SQLiteConnection check = new SQLiteConnection (path)) {
				// load it back - should be null...
				var loaded = check.Table<Customer> ().Where (v => v.Id == customer.Id).ToList ();
				Assert.AreEqual (0, loaded.Count);
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
			string path = null;
			var conn = GetConnection (ref path);
			conn.CreateTableAsync<Customer> ().Wait ();
			conn.InsertAsync (customer).Wait ();

			// check...
			Assert.AreNotEqual (0, customer.Id);

			// get it back...
			var task = conn.GetAsync<Customer> (customer.Id);
			task.Wait ();
			Customer loaded = task.Result;

			// check...
			Assert.AreEqual (customer.Id, loaded.Id);
		}
		
		[Test]
		public void FindAsyncWithExpression ()
		{
			// create...
			Customer customer = new Customer ();
			customer.FirstName = "foo";
			customer.LastName = "bar";
			customer.Email = Guid.NewGuid ().ToString ();

			// connect and insert...
			string path = null;
			var conn = GetConnection (ref path);
			conn.CreateTableAsync<Customer> ().Wait ();
			conn.InsertAsync (customer).Wait ();

			// check...
			Assert.AreNotEqual (0, customer.Id);

			// get it back...
			var task = conn.FindAsync<Customer> (x => x.Id == customer.Id);
			task.Wait ();
			Customer loaded = task.Result;

			// check...
			Assert.AreEqual (customer.Id, loaded.Id);
		}
		
		[Test]
		public void FindAsyncWithExpressionNull ()
		{
			// connect and insert...
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();

			// get it back...
			var task = conn.FindAsync<Customer> (x => x.Id == 1);
			task.Wait ();
			var loaded = task.Result;

			// check...
			Assert.IsNull (loaded);
		}

		[Test]
		public void TestFindAsyncItemPresent ()
		{
			// create...
			Customer customer = CreateCustomer ();

			// connect and insert...
			string path = null;
			var conn = GetConnection (ref path);
			conn.CreateTableAsync<Customer> ().Wait ();
			conn.InsertAsync (customer).Wait ();

			// check...
			Assert.AreNotEqual (0, customer.Id);

			// get it back...
			var task = conn.FindAsync<Customer> (customer.Id);
			task.Wait ();
			Customer loaded = task.Result;

			// check...
			Assert.AreEqual (customer.Id, loaded.Id);
		}

		[Test]
		public void TestFindAsyncItemMissing ()
		{
			// connect and insert...
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();

			// now get one that doesn't exist...
			var task = conn.FindAsync<Customer> (-1);
			task.Wait ();

			// check...
			Assert.IsNull (task.Result);
		}

		[Test]
		public void TestQueryAsync ()
		{
			// connect...
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();

			// insert some...
			List<Customer> customers = new List<Customer> ();
			for (int index = 0; index < 5; index++) {
				Customer customer = CreateCustomer ();

				// insert...
				conn.InsertAsync (customer).Wait ();

				// add...
				customers.Add (customer);
			}

			// return the third one...
			var task = conn.QueryAsync<Customer> ("select * from customer where id=?", customers[2].Id);
			task.Wait ();
			var loaded = task.Result;

			// check...
			Assert.AreEqual (1, loaded.Count);
			Assert.AreEqual (customers[2].Email, loaded[0].Email);
		}

		[Test]
		public void TestTableAsync ()
		{
			// connect...
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();
			conn.ExecuteAsync ("delete from customer").Wait ();

			// insert some...
			List<Customer> customers = new List<Customer> ();
			for (int index = 0; index < 5; index++) {
				Customer customer = new Customer ();
				customer.FirstName = "foo";
				customer.LastName = "bar";
				customer.Email = Guid.NewGuid ().ToString ();

				// insert...
				conn.InsertAsync (customer).Wait ();

				// add...
				customers.Add (customer);
			}

			// run the table operation...
			var query = conn.Table<Customer> ();
			var loaded = query.ToListAsync ().Result;

			// check that we got them all back...
			Assert.AreEqual (5, loaded.Count);
			Assert.IsNotNull (loaded.Where (v => v.Id == customers[0].Id));
			Assert.IsNotNull (loaded.Where (v => v.Id == customers[1].Id));
			Assert.IsNotNull (loaded.Where (v => v.Id == customers[2].Id));
			Assert.IsNotNull (loaded.Where (v => v.Id == customers[3].Id));
			Assert.IsNotNull (loaded.Where (v => v.Id == customers[4].Id));
		}

		[Test]
		public void TestExecuteAsync ()
		{
			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			conn.CreateTableAsync<Customer> ().Wait ();

			// do a manual insert...
			string email = Guid.NewGuid ().ToString ();
			conn.ExecuteAsync ("insert into customer (firstname, lastname, email) values (?, ?, ?)",
				"foo", "bar", email).Wait ();

			// check...
			using (SQLiteConnection check = new SQLiteConnection (path)) {
				// load it back - should be null...
				var result = check.Table<Customer> ().Where (v => v.Email == email);
				Assert.IsNotNull (result);
			}
		}

		[Test]
		public void TestInsertAllAsync ()
		{
			// create a bunch of customers...
			List<Customer> customers = new List<Customer> ();
			for (int index = 0; index < 100; index++) {
				Customer customer = new Customer ();
				customer.FirstName = "foo";
				customer.LastName = "bar";
				customer.Email = Guid.NewGuid ().ToString ();
				customers.Add (customer);
			}

			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			conn.CreateTableAsync<Customer> ().Wait ();

			// insert them all...
			conn.InsertAllAsync (customers).Wait ();

			// check...
			using (SQLiteConnection check = new SQLiteConnection (path)) {
				for (int index = 0; index < customers.Count; index++) {
					// load it back and check...
					Customer loaded = check.Get<Customer> (customers[index].Id);
					Assert.AreEqual (loaded.Email, customers[index].Email);
				}
			}
		}

		[Test]
		public void TestRunInTransactionAsync ()
		{
			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			conn.CreateTableAsync<Customer> ().Wait ();
			bool transactionCompleted = false;

			// run...
			Customer customer = new Customer ();
			conn.RunInTransactionAsync ((c) => {
				// insert...
				customer.FirstName = "foo";
				customer.LastName = "bar";
				customer.Email = Guid.NewGuid ().ToString ();
				c.Insert (customer);

				// delete it again...
				c.Execute ("delete from customer where id=?", customer.Id);

				// set completion flag
				transactionCompleted = true;
			}).Wait (10000);

			// check...
			Assert.IsTrue(transactionCompleted);
			using (SQLiteConnection check = new SQLiteConnection (path)) {
				// load it back and check - should be deleted...
				var loaded = check.Table<Customer> ().Where (v => v.Id == customer.Id).ToList ();
				Assert.AreEqual (0, loaded.Count);
			}
		}

		[Test]
		public void TestExecuteScalar ()
		{
			// connect...
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();

			// check...
			var task = conn.ExecuteScalarAsync<object> ("select name from sqlite_master where type='table' and name='customer'");
			task.Wait ();
			object name = task.Result;
			Assert.AreNotEqual ("Customer", name);
		}

		[Test]
		public void TestAsyncTableQueryToListAsync ()
		{
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();

			// create...
			Customer customer = this.CreateCustomer ();
			conn.InsertAsync (customer).Wait ();

			// query...
			var query = conn.Table<Customer> ();
			var task = query.ToListAsync ();
			task.Wait ();
			var items = task.Result;

			// check...
			var loaded = items.Where (v => v.Id == customer.Id).First ();
			Assert.AreEqual (customer.Email, loaded.Email);
		}

		[Test]
		public void TestAsyncTableQueryToFirstAsyncFound ()
		{
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer>().Wait();

			// create...
			Customer customer = this.CreateCustomer();
			conn.InsertAsync(customer).Wait();

			// query...
			var query = conn.Table<Customer> ().Where(v => v.Id == customer.Id);
			var task = query.FirstAsync ();
			task.Wait();
			var loaded = task.Result;

			// check...
			Assert.AreEqual(customer.Email, loaded.Email);
		}

		[Test]
		public void TestAsyncTableQueryToFirstAsyncMissing ()
		{
			var conn = GetConnection();
			conn.CreateTableAsync<Customer>().Wait();

			// create...
			Customer customer = this.CreateCustomer();
			conn.InsertAsync(customer).Wait();

			// query...
			var query = conn.Table<Customer>().Where(v => v.Id == -1);
			var task = query.FirstAsync();
			ExceptionAssert.Throws<AggregateException>(() => task.Wait());
		}

		[Test]
		public void TestAsyncTableQueryToFirstOrDefaultAsyncFound ()
		{
			var conn = GetConnection();
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

		[Test]
		public void TestAsyncTableQueryToFirstOrDefaultAsyncMissing ()
		{
			var conn = GetConnection();
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

		[Test]
		public void TestAsyncTableQueryWhereOperation ()
		{
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();

			// create...
			Customer customer = this.CreateCustomer ();
			conn.InsertAsync (customer).Wait ();

			// query...
			var query = conn.Table<Customer> ();
			var task = query.ToListAsync ();
			task.Wait ();
			var items = task.Result;

			// check...
			var loaded = items.Where (v => v.Id == customer.Id).First ();
			Assert.AreEqual (customer.Email, loaded.Email);
		}

		[Test]
		public void TestAsyncTableQueryCountAsync ()
		{
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();
			conn.ExecuteAsync ("delete from customer").Wait ();

			// create...
			for (int index = 0; index < 10; index++)
				conn.InsertAsync (this.CreateCustomer ()).Wait ();

			// load...
			var query = conn.Table<Customer> ();
			var task = query.CountAsync ();
			task.Wait ();

			// check...
			Assert.AreEqual (10, task.Result);
		}

		[Test]
		public void TestAsyncTableOrderBy ()
		{
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();
			conn.ExecuteAsync ("delete from customer").Wait ();

			// create...
			for (int index = 0; index < 10; index++)
				conn.InsertAsync (this.CreateCustomer ()).Wait ();

			// query...
			var query = conn.Table<Customer> ().OrderBy (v => v.Email);
			var task = query.ToListAsync ();
			task.Wait ();
			var items = task.Result;

			// check...
			Assert.AreEqual (-1, string.Compare (items[0].Email, items[9].Email));
		}

		[Test]
		public void TestAsyncTableOrderByDescending ()
		{
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();
			conn.ExecuteAsync ("delete from customer").Wait ();

			// create...
			for (int index = 0; index < 10; index++)
				conn.InsertAsync (this.CreateCustomer ()).Wait ();

			// query...
			var query = conn.Table<Customer> ().OrderByDescending (v => v.Email);
			var task = query.ToListAsync ();
			task.Wait ();
			var items = task.Result;

			// check...
			Assert.AreEqual (1, string.Compare (items[0].Email, items[9].Email));
		}

		[Test]
		public void TestAsyncTableQueryTake ()
		{
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();
			conn.ExecuteAsync ("delete from customer").Wait ();

			// create...
			for (int index = 0; index < 10; index++) {
				var customer = this.CreateCustomer ();
				customer.FirstName = index.ToString ();
				conn.InsertAsync (customer).Wait ();
			}

			// query...
			var query = conn.Table<Customer> ().OrderBy (v => v.FirstName).Take (1);
			var task = query.ToListAsync ();
			task.Wait ();
			var items = task.Result;

			// check...
			Assert.AreEqual (1, items.Count);
			Assert.AreEqual ("0", items[0].FirstName);
		}

		[Test]
		public void TestAsyncTableQuerySkip ()
		{
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();
			conn.ExecuteAsync ("delete from customer").Wait ();

			// create...
			for (int index = 0; index < 10; index++) {
				var customer = this.CreateCustomer ();
				customer.FirstName = index.ToString ();
				conn.InsertAsync (customer).Wait ();
			}

			// query...
			var query = conn.Table<Customer> ().OrderBy (v => v.FirstName).Skip (5);
			var task = query.ToListAsync ();
			task.Wait ();
			var items = task.Result;

			// check...
			Assert.AreEqual (5, items.Count);
			Assert.AreEqual ("5", items[0].FirstName);
		}

		[Test]
		public void TestAsyncTableElementAtAsync ()
		{
			var conn = GetConnection ();
			conn.CreateTableAsync<Customer> ().Wait ();
			conn.ExecuteAsync ("delete from customer").Wait ();

			// create...
			for (int index = 0; index < 10; index++) {
				var customer = this.CreateCustomer ();
				customer.FirstName = index.ToString ();
				conn.InsertAsync (customer).Wait ();
			}

			// query...
			var query = conn.Table<Customer> ().OrderBy (v => v.FirstName);
			var task = query.ElementAtAsync (7);
			task.Wait ();
			var loaded = task.Result;

			// check...
			Assert.AreEqual ("7", loaded.FirstName);
		}


        [Test]
        public void TestAsyncGetWithExpression()
        {
            var conn = GetConnection();
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

		[Test]
		public void CreateTable ()
		{
			var conn = GetConnection ();

			var trace = new List<string> ();
			conn.Tracer = trace.Add;
			conn.Trace = true;

			var r0 = conn.CreateTableAsync<Customer> ().Result;

			Assert.AreEqual (CreateTableResult.Created, r0);

			var r1 = conn.CreateTableAsync<Customer> ().Result;

			Assert.AreEqual (CreateTableResult.Migrated, r1);

			var r2 = conn.CreateTableAsync<Customer> ().Result;

			Assert.AreEqual (CreateTableResult.Migrated, r1);

			Assert.AreEqual (7, trace.Count);
		}

		[Test]
		public void CloseAsync ()
		{
			var conn = GetConnection ();

			var r0 = conn.CreateTableAsync<Customer> ().Result;

			Assert.AreEqual (CreateTableResult.Created, r0);

			conn.CloseAsync ().Wait ();
		}


	}
}
