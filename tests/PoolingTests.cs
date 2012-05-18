using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
    // @mbrit - 2012-05-14 - NOTE - the lack of async use in this class is because the VS11 test runner falsely
    // reports any failing test as succeeding if marked as async. Should be fixed in the "June 2012" drop...

    /// <summary>
    /// Defines tests related to connection lifetime and pooling.
    /// </summary>
#if NETFX_CORE
    [TestClass]
#else
    [TestFixture]
#endif
    public class PoolingTests
    {
        private const string DatabaseName = "pooling.db";

        private Customer CreateCustomer()
        {
            Customer customer = new Customer() 
            {
                FirstName = "foo",
                LastName = "bar",
                Email = Guid.NewGuid().ToString()
            };
            return customer;
        }

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        public void TestApplicationSuspended()
        {
            // create a connection and do something...
            SQLiteAsyncConnection conn = new SQLiteAsyncConnection(SQLiteConnectionSpecification.CreateForAsync(DatabaseName));
            conn.CreateTableAsync<Customer>().Wait();
            conn.InsertAsync(this.CreateCustomer()).Wait();

            // ok...
            SQLiteConnectionPool.Current.ApplicationSuspended();

            // do something again - should all spring into life...
            conn = new SQLiteAsyncConnection(SQLiteConnectionSpecification.CreateForAsync(DatabaseName));
            conn.CreateTableAsync<Customer>().Wait();
            conn.InsertAsync(this.CreateCustomer()).Wait();
        }

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        public void TestMultithreading()
        {
            // connect...
            SQLiteAsyncConnection conn = new SQLiteAsyncConnection(SQLiteConnectionSpecification.CreateForAsync(DatabaseName));
            conn.CreateTableAsync<Customer>().Wait();
            conn.ExecuteAsync("delete from customer").Wait();

            // create a whole bunch of tasks that all hammer the same connection...
            List<Customer> customers = new List<Customer>();
            List<Task> tasks = new List<Task>();
            for (int index = 0; index < 250; index++)
            {
                Customer customer = this.CreateCustomer();

                // save...
                customers.Add(customer);
                tasks.Add(conn.InsertAsync(customer));
            }

            // wait...
            Task.WaitAll(tasks.ToArray());

            // load...
            using (SQLiteConnection check = new SQLiteConnection(DatabaseName))
            {
                var loadeds = check.Table<Customer>().ToList();
                Assert.AreEqual(customers.Count, loadeds.Count);

                // walk...
                foreach (Customer customer in customers)
                {
                    Customer loaded = loadeds.Where(v => v.Id == customer.Id).First();
                    Assert.AreEqual(customer.Email, loaded.Email);
                }
            }
        }
    }
}
