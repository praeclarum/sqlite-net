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
		string DbFilePath;
		SQLiteAsyncConnection connection;
		

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
			{
				try
				{
					File.Delete(DbFilePath);
				}
				catch (System.IO.IOException)
				{
					// file still is locked
				}
			}
		}

		private void WorkerJob()
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


		[Test]
		public void ConcurrentRunInTransactionTest()
		{
			const int WORKERSCOUNT = 20;

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
}
