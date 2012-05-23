using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Shapes;
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

		[MaxLength (64)]
		public string Email { get; set; }
	}

	/// <summary>
	/// Defines tests that exercise async behaviour.
	/// </summary>
#if NETFX_CORE
	[TestClass]
#else
	[TestFixture]
#endif
	public class AsyncTests
	{
		private const string DatabaseName = "async.db";

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

		internal static SQLiteAsyncConnection GetConnection ()
		{
			string path = null;
			return GetConnection (ref path);
		}

		internal static SQLiteAsyncConnection GetConnection (ref string path)
		{
#if NETFX_CORE
			return new SQLiteAsyncConnection(SQLiteConnectionSpecification.CreateForAsyncMetroStyle(DatabaseName, ref path));
#else
			path = System.IO.Path.GetTempFileName ();
			return new SQLiteAsyncConnection (path);
#endif
		}

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
		public void TestGetAsync ()
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
		public void TestRunInTransactionAsync ()
		{
			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			conn.CreateTableAsync<Customer> ().Wait ();

			// run...
			Customer customer = new Customer ();
			conn.RunInTransactionAsync ((c) => {
				// insert...
				customer.FirstName = "foo";
				customer.LastName = "bar";
				customer.Email = Guid.NewGuid ().ToString ();
				conn.InsertAsync (customer).Wait ();

				// delete it again...
				conn.ExecuteAsync ("delete from customer where id=?", customer.Id).Wait ();

			}).Wait ();

			// check...
			using (SQLiteConnection check = new SQLiteConnection (path)) {
				// load it back and check - should be deleted...
				var loaded = check.Table<Customer> ().Where (v => v.Id == customer.Id).ToList ();
				Assert.AreEqual (0, loaded.Count);
			}
		}

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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

#if NETFX_CORE
		[TestMethod]
#else
		[Test]
#endif
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
	}
}
