using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SQLite.Tests
{
	// all these test do run some heavy queries in a background task.
	// these queries would take quite some time if they would be allowed to be run to complation,
	// but each test, after having launched the query, it stops it by setting the CancellationToken.
	public class CancelableTest
	{
		public static ISQLiteConnection CreateSqliteInMemoryDb()
		{
			//return new SQLiteConnection (":memory:"); this simpler version would make all tests run using the very same memory database, so test would not run in parallel
			// this more complex way of creating a memory database allows to give a separate name to different memory databases
			return new SQLiteConnection ($"file:memdb{Guid.NewGuid ().ToString ("N")}?mode=memory&cache=private");
		}


		[Test]
		public async Task CancelableQueryQueryScalars_Test()
		{
			using (var conn = CreateSqliteInMemoryDb()) {
				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;
				
				// notice that this query takes ages before returning the first record. 
				// here I am actually testing that the execution is stopped at the "server side"
				// by the sqlite3_interrupt api call, since we never enter in the internal
				// "fetch next row" c# loop
				
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
					await Task.Delay (300); // wait some time to be sure that the query has started
					CancTokSource.Cancel (); 
					await task; 
				}
				catch (Exception ex) {
					e = ex;
				}
				Assert.That (e, Is.InstanceOf<OperationCanceledException> ());
			}
		}

		[Test]
		public async Task CancelableQuery_Test ()
		{
			using (var conn = CreateSqliteInMemoryDb()) {
				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;
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
					await Task.Delay (1000); 
					CancTokSource.Cancel (); 
					await task; 
				}
				catch (Exception ex) {
					e = ex;
				}
				Assert.That (e, Is.InstanceOf<OperationCanceledException> ());
			}
		}


		/// <summary>
		///  this view will return millions of records, by using a recursive "with"
		/// </summary>
		public const string BIGVIEW_DEFCMD=
			@"create view BIGVIEW as
						WITH RECURSIVE qry(n)
						AS (
							SELECT 1 as n
							UNION ALL
							SELECT n + 1 as n FROM qry WHERE n < 100000000
						)
						SELECT n as fld1, n as fld2 FROM qry";
		
		// this entity maps bigview
		[Table("BIGVIEW")]
	    public class MyRecType
		{
			public long fld1 { get; set; }
			public long fld2 { get; set; }
		}

		[Test]
		public async Task CancelableTable_Test ()
		{
			using (var conn = CreateSqliteInMemoryDb()) {
				// 
				conn.Execute (BIGVIEW_DEFCMD);

				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;

				var task = Task.Run(() => {
					// this query would take forever if we couldn't stop it
					var result = 
					  conn.Table<MyRecType>()
					  .Where(x => x.fld1!=x.fld2)
					  .CancelToken (tok)
					  .ToList ();
				}).ConfigureAwait(false);

				Exception e = null;
				try {
					await Task.Delay (10);
					CancTokSource.Cancel ();
					await task;
				}
				catch (Exception ex) {
					e = ex;
				}
			
				Assert.That (e, Is.InstanceOf<OperationCanceledException> ());

			}
		}

	}
}
