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

		public static ISQLiteAsyncConnection CreateAsyncSqliteInMemoryDb ()
		{
			//return new SQLiteConnection (":memory:"); this simpler version would make all tests run using the very same memory database, so test would not run in parallel
			// this more complex way of creating a memory database allows to give a separate name to different memory databases
			return new SQLiteAsyncConnection ($"file:memdb{Guid.NewGuid ().ToString ("N")}?mode=memory&cache=private");
		}


		/// <summary>
		/// this is a "create view" command that creates a view that will return 100 millions of records
		/// by using a recursive "with". I use it for having some slow queries whose execution I can cancel
		/// </summary>
		public const string BIGVIEW_DEFCMD =
			@"create view BIGVIEW as
						WITH RECURSIVE qry(n)
						AS (
							SELECT 1 as n
							UNION ALL
							SELECT n + 1 as n FROM qry WHERE n < 100000000
						)
						SELECT n as fld1, n as fld2 FROM qry";

		// this entity maps bigview
		[Table ("BIGVIEW")]
		public class MyRecType
		{
			public long fld1 { get; set; }
			public long fld2 { get; set; }
		}

		#region SQLiteConnection

		[Test]
		public async Task CancelableExecute_Test ()
		{
			using (var conn = CreateSqliteInMemoryDb ()) {
				conn.Execute (BIGVIEW_DEFCMD);
				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;

				var task = Task.Run (() => {
					conn.Execute (tok, "create table bigtable as select * from bigview");
				});

				Exception e = null;
				try {
					await Task.Delay (300); 
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
		public async Task CancelableCount_Test ()
		{
			using (var conn = CreateSqliteInMemoryDb ()) {
				conn.Execute (BIGVIEW_DEFCMD);
				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;

				var task = Task.Run (() => {
					var cnt = conn.Table<MyRecType> ().CancelToken (tok).Count (); 
				});

				Exception e = null;
				try {
					await Task.Delay (300); 
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
		public async Task CancelableQueryQueryScalars_Test()
		{
			using (var conn = CreateSqliteInMemoryDb()) {
				conn.Execute (BIGVIEW_DEFCMD);
				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;
				
				var task = Task.Run (() => {
   				   var longArray = conn.QueryScalars<long> (tok, "select fld1 from BIGVIEW where fld1 = 1000000");
				});

				Exception e = null;
				try {
					await Task.Delay (300); 
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
				conn.Execute (BIGVIEW_DEFCMD);

				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;
				var task = Task.Run (() => {
					var recs = conn.Query<MyRecType> (tok, "select * from BIGVIEW where fld1 = 1000000");
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


		[Test]
		public async Task CancelableTable_Test ()
		{
			using (var conn = CreateSqliteInMemoryDb()) {
				// 
				conn.Execute (BIGVIEW_DEFCMD);

				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;

				var task = Task.Run(() => {
				
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
		#endregion

		#region SQLiteAyncConnection

		[Test]
		public async Task CancelableExecuteAsync_Test ()
		{
			var conn = CreateAsyncSqliteInMemoryDb ();
			try { 
				await conn.ExecuteAsync (BIGVIEW_DEFCMD);
				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;

				var task = Task.Run (async () => {
					await conn.ExecuteAsync (tok, "create table bigtable as select * from bigview");
				});

				Exception e = null;
				try {
					await Task.Delay (300); 
					CancTokSource.Cancel ();
					await task;
				}
				catch (Exception ex) {
					e = ex;
				}
				Assert.That (e, Is.InstanceOf<OperationCanceledException> ());
			}
			finally {
				await conn.CloseAsync ();
			}
		}

		[Test]
		public async Task CancelableAsyncQueryQueryScalars_Test ()
		{
			var conn = CreateAsyncSqliteInMemoryDb ();
			try {
				await conn.ExecuteAsync (BIGVIEW_DEFCMD);
				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;

				var task = Task.Run (async () => {
					var longArray = await conn.QueryScalarsAsync<long> (tok, "select fld1 from BIGVIEW where fld1 = 1000000");
				});

				Exception e = null;
				try {
					await Task.Delay (300); 
					CancTokSource.Cancel ();
					await task;
				}
				catch (Exception ex) {
					e = ex;
				}
				Assert.That (e, Is.InstanceOf<OperationCanceledException> ());
			}
			finally {
				await conn.CloseAsync ();
			}
		}

		[Test]
		public async Task CancelableAsyncQuery_Test ()
		{
			var conn = CreateAsyncSqliteInMemoryDb ();
			try
			{
				await conn.ExecuteAsync (BIGVIEW_DEFCMD);

				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;
				var task = Task.Run (async () => {
					var recs = await conn.QueryAsync<MyRecType> (tok, "select * from BIGVIEW where fld1 = 1000000");
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
			finally {
				await conn.CloseAsync ();
			}
		}


		[Test]
		public async Task CancelableAsyncTable_Test ()
		{
			var conn = CreateAsyncSqliteInMemoryDb ();
			try {
				await conn.ExecuteAsync(BIGVIEW_DEFCMD);

				var CancTokSource = new CancellationTokenSource ();
				var tok = CancTokSource.Token;

				var task = Task.Run (async () => {
					
					var result =
					  await conn.Table<MyRecType> ()
					  .Where (x => x.fld1 != x.fld2)
					  .CancelToken (tok) 
					  .ToListAsync ();
				}).ConfigureAwait (false);

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
			finally {
				await conn.CloseAsync ();
			}
		}

		#endregion
	}
}
