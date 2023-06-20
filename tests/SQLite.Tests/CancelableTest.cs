using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SQLite.Tests
{
	public class CancelableTest
	{
		[Test]
		public async Task CancelableQueryQueryScalarsTest()
		{
			using (var conn = new SQLiteConnection (":memory:") as ISQLiteConnection) {
				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;
				// here I am launching the query in a separate task.
				// this query is extremely slow: Its execution time is way beyond my patience limit
				var task = Task.Run (() => {
					var extremelySlowQuery =
						@"WITH RECURSIVE qry(n) AS (
						  SELECT 1
						  UNION ALL
						  SELECT n + 1 FROM qry WHERE n < 10000000
						)
						SELECT * FROM qry where n = 100000000";

					var result = conn.QueryScalars<long> (tok, extremelySlowQuery);
				});

				Exception e = null;
				try {
					await Task.Delay (1000); // let the query run for a bit
					CancTokSource.Cancel (); // and then we ask the query to stop (this will make a OperationCanceledException to be raised in the context of the task)
					await task; // then we wait the task to be completed 
				}
				catch (Exception ex) {
					e = ex;
				}
				Assert.That (e, Is.InstanceOf<OperationCanceledException> ());
			}
		}

		[Test]
		public async Task CancelableQueryTest ()
		{
			using (var conn = new SQLiteConnection (":memory:")) {
				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;
				// here I am launching the query in a separate task.
				// this query is extremely slow: Its execution time is way beyond my patience limit
				var task = Task.Run (() => {
					var extremelySlowQuery =
						@"WITH RECURSIVE qry(n) AS (
						  SELECT 1 as n
						  UNION ALL
						  SELECT n + 1 as n FROM qry WHERE n < 100000000
						)
						SELECT n as fld1, n as fld2 FROM qry where n = 100000";
					var result = conn.Query<(long fld1,long fld2)>(tok, extremelySlowQuery);
				});

				Exception e = null;
				try {
					await Task.Delay (1000); // let the query run for a bit
					CancTokSource.Cancel (); // and then we ask the query to stop (this will make a OperationCanceledException to be raised in the context of the task)
					await task; // then we wait the task to be completed 
				}
				catch (Exception ex) {
					e = ex;
				}
				Assert.That (e, Is.InstanceOf<OperationCanceledException> ());
			}
		}


	    public class MyRecType
		{
			public long fld1 { get; set; }
			public long fld2 { get; set; }
		}

		[Test]
		public async Task CancelableTableTest ()
		{
			using (var conn = new SQLiteConnection (":memory:")) {
				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;
				// here I am launching the query in a separate task.
				// this query is extremely slow: Its execution time is way beyond my patience limit

				conn.CreateTable<MyRecType> ();

				for (var i= 0; i< 1000000; i++)
					conn.Insert (new MyRecType { fld1 = i, fld2 = i });

				var task = Task.Run (() => {
					var result = conn.Table<MyRecType>().CancelToken (tok).ToList();
				});

				Exception e = null;
				try {
					await Task.Delay (10); // let the query run for a bit
					CancTokSource.Cancel (); // and then we ask the query to stop (this will make a OperationCanceledException to be raised in the context of the task)
					await task; // then we wait the task to be completed 
				}
				catch (Exception ex) {
					e = ex;
				}
				Assert.That (e, Is.InstanceOf<OperationCanceledException> ());
			}
		}

	}
}
