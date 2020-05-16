using Microsoft.VisualBasic.CompilerServices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLite.Tests
{
	/// <summary>
	///  THIS TEST WAS RISING InvalidOperationException ("Cannot begin a transaction while already in a transaction.")
	///  before adopting Nito.AsyncEx locking library (which correctly handlers the await/async paradigm)
	/// </summary>
	[TestFixture]
	public class AsyncConcurrencyTestWithThreads
	{
		private string DbFilePath;
		private SQLiteAsyncConnection connection;

		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			DbFilePath = Path.GetTempFileName();
			connection = new SQLiteAsyncConnection(DbFilePath);
		}

		[OneTimeTearDown]
		public async Task OneTimeTearDown()
		{
			await connection?.CloseAsync();
			if (File.Exists(DbFilePath))
				File.Delete(DbFilePath);
     	}

		public  void WorkerJob()
		{
			Task t = connection.RunInTransactionAsync(
					syncConn =>
					{
					// just pretend we are doing something time consuming using syncConn
					Thread.Sleep(2000);
					}
				);
			t.ConfigureAwait(false);
			t.Wait();
		}


		/// <summary>
		/// in this test we are checking that all tasks, running for multiple parallel threads will obtain 
		/// access in turn to the database connection without ever having the 
		/// "Cannot begin a transaction while already in a transaction." 
		/// exception being raised.
		/// </summary>
		[Test]
		public async Task WithoutBusyTimeout()
		{
			const int WORKERSCOUNT = 5;
			await connection.SetBusyTimeoutAsync(TimeSpan.FromDays(10)); // just to be sure we don't meet the wait timeout before jobs end their transaction

			var workers = new List<Thread>();
			for (int i = 0; i < WORKERSCOUNT; i++)
				workers.Add(new Thread(new ThreadStart(WorkerJob)));
			// launch all background workers
			foreach (var t in workers)
				t.Start();
			// wait them for finish
			foreach (var t in workers)
				t.Join();
		}
	}


	/// <summary>
	///  Very similar to former test in its strucure, but here we are testing that the "BusyTimeout" is taken in consideration
	///  also when trying to acquire access to the SQLiteConnection wrapped inside a SqliteAsyncConnection, not just 
	///  when trying to access the low-level database file
	/// </summary>
	[TestFixture]
	public class BusyTimeoutWithThreads
	{
		private string DbFilePath;
		private SQLiteAsyncConnection connection;

		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			DbFilePath = Path.GetTempFileName();
			connection = new SQLiteAsyncConnection(DbFilePath);
		}

		[OneTimeTearDown]
		public async Task OneTimeTearDown()
		{
			await connection?.CloseAsync();
			if (File.Exists(DbFilePath))
				File.Delete(DbFilePath);
		}

		ManualResetEvent ThreadHasBegunTransaction = new ManualResetEvent(false);
		public void WorkerJob()
		{
			Task t = connection.RunInTransactionAsync(
					syncConn =>
					{
						ThreadHasBegunTransaction.Set();
						// just pretend we are doing something time consuming using syncConn
						Thread.Sleep(2000);
					}
				);
			t.ConfigureAwait(false);
			t.Wait();
		}

		[Test]
		public async Task BusyTimeoutTest()
		{
			
			await connection.SetBusyTimeoutAsync(TimeSpan.FromMilliseconds(40)); // here we will surely meet the timeout
			connection.GetConnection().EnforceBusyTimeout = true;
			var t1 = new Thread(new ThreadStart(WorkerJob));
			t1.Start();
			// wait until the thread has obtained the lock to the session object
			ThreadHasBegunTransaction.WaitOne();

			Assert.ThrowsAsync<SQLite.LockTimeout>(async () =>
		    {
				// let's see what happens while I try to obtain a lock to the session while it is already locked
				await connection.RunInTransactionAsync(
				  syncConn =>
				  {

				  });
		    });
			
			
			t1.Join();
		}
	}


}
