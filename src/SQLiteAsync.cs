using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SQLite
{
	public class SQLiteAsyncConnection
	{
		SQLiteConnectionSpecification _spec;

		public SQLiteAsyncConnection (string connectionString)
		{
			_spec = new SQLiteConnectionSpecification (connectionString);
		}

		SQLiteConnectionWithLock GetConcreteConnection ()
		{
			return SQLiteConnectionPool.Current.GetConnection (_spec);
		}

		public Task<CreateTablesResult> CreateTableAsync<T> ()
			where T : new ()
		{
			return CreateTablesAsync (typeof (T));
		}

		public Task<CreateTablesResult> CreateTablesAsync<T, T2> ()
			where T : new ()
			where T2 : new ()
		{
			return CreateTablesAsync (typeof (T), typeof (T2));
		}

		public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3> ()
			where T : new ()
			where T2 : new ()
			where T3 : new ()
		{
			return CreateTablesAsync (typeof (T), typeof (T2), typeof (T3));
		}

		public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4> ()
			where T : new ()
			where T2 : new ()
			where T3 : new ()
			where T4 : new ()
		{
			return CreateTablesAsync (typeof (T), typeof (T2), typeof (T3), typeof (T4));
		}

		public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4, T5> ()
			where T : new ()
			where T2 : new ()
			where T3 : new ()
			where T4 : new ()
			where T5 : new ()
		{
			return CreateTablesAsync (typeof (T), typeof (T2), typeof (T3), typeof (T4), typeof (T5));
		}

		public Task<CreateTablesResult> CreateTablesAsync (params Type[] types)
		{
			return Task.Factory.StartNew (() => {
				CreateTablesResult result = new CreateTablesResult ();
				using (var conn = GetConcreteConnection ()) {
					using (conn.Lock ()) {
						foreach (Type type in types) {
							int aResult = conn.CreateTable (type);
							result.Results[type] = aResult;
						}
					}
				}
				return result;
			});
		}

		public Task<int> DropTableAsync<T> ()
			where T : new ()
		{
			return Task.Factory.StartNew (() => {
				using (var conn = GetConcreteConnection ()) {
					using (conn.Lock ()) {
						return conn.DropTable<T> ();
					}
				}
			});
		}

		public Task<int> InsertAsync (object item)
		{
			return Task.Factory.StartNew (() => {
				using (var conn = GetConcreteConnection ()) {
					using (conn.Lock ())
						return conn.Insert (item);
				}

			});
		}

		public Task<int> UpdateAsync (object item)
		{
			return Task.Factory.StartNew (() => {
				using (var conn = GetConcreteConnection ()) {
					using (conn.Lock ())
						return conn.Update (item);
				}

			});
		}

		public Task<int> DeleteAsync (object item)
		{
			return Task.Factory.StartNew (() => {
				using (var conn = GetConcreteConnection ()) {
					using (conn.Lock ())
						return conn.Delete (item);
				}
			});
		}

		public Task<T> GetAsync<T> (object pk)
			where T : new ()
		{
			return Task.Factory.StartNew (() => {
				using (var conn = GetConcreteConnection ()) {
					using (conn.Lock ())
						return conn.Get<T> (pk);
				}
			});
		}

		public Task<T> GetSafeAsync<T> (object pk)
			where T : new ()
		{
			// TODO: Replace with Find
			return Task<T>.Factory.StartNew (() => {
				// go...
				using (var conn = GetConcreteConnection ()) {
					using (conn.Lock ()) {
						// create a command...
						var map = conn.GetMapping<T> ();
						string query = string.Format ("select * from \"{0}\" where \"{1}\" = ?", map.TableName, map.PK.Name);

						SQLiteCommand command = conn.CreateCommand (query, pk);
						List<T> results = command.ExecuteQuery<T> ();
						if (results.Count > 0)
							return results[0];
						else
							return default (T);
					}
				}
			});
		}

		public Task<int> ExecuteAsync (string query, params object[] args)
		{
			return Task<int>.Factory.StartNew (() => {
				using (var conn = GetConcreteConnection ()) {
					using (conn.Lock ())
						return conn.Execute (query, args);
				}

			});
		}

		public Task<int> InsertAllAsync (IEnumerable items)
		{
			return Task.Factory.StartNew (() => {
				using (var conn = GetConcreteConnection ()) {
					using (conn.Lock ()) {
						return conn.InsertAll (items);
					}
				}
			});
		}

		public Task RunInTransactionAsync (Action<SQLiteAsyncConnection> action)
		{
			return Task.Factory.StartNew (() => {
				using (var conn = this.GetConcreteConnection ()) {
					using (conn.Lock ()) {
						conn.BeginTransaction ();
						try {
							action (this);
							conn.Commit ();
						}
						catch (Exception ex) {
							conn.Rollback ();
							throw;
						}
					}
				}
			});
		}

		public AsyncTableQuery<T> Table<T> ()
			where T : new ()
		{
			// This isn't async as the underlying connection doesn't go out to the database
			// until the query is performed.
			using (var conn = GetConcreteConnection ()) {
				return new AsyncTableQuery<T> (conn.Table<T> ());
			}
		}

		public Task<T> ExecuteScalarAsync<T> (string sql, params object[] args)
		{
			// run...
			return Task<T>.Factory.StartNew (() => {
				// get...
				using (var conn = GetConcreteConnection ()) {
					using (conn.Lock ()) {
						SQLiteCommand command = conn.CreateCommand (sql, args);
						return command.ExecuteScalar<T> ();
					}
				}
			});
		}

		public Task<List<T>> QueryAsync<T> (string sql, params object[] args)
			where T : new ()
		{
			// run...
			return Task<List<T>>.Factory.StartNew (() => {
				// get...
				using (var conn = GetConcreteConnection ()) {
					using (conn.Lock ())
						return conn.Query<T> (sql, args);
				}
			});
		}
	}

	public class AsyncTableQuery<T>
		where T : new ()
	{
		TableQuery<T> _innerQuery;

		public AsyncTableQuery (TableQuery<T> innerQuery)
		{
			_innerQuery = innerQuery;
		}

		public AsyncTableQuery<T> Where (Expression<Func<T, bool>> predExpr)
		{
			return new AsyncTableQuery<T> (_innerQuery.Where (predExpr));
		}

		public AsyncTableQuery<T> Skip (int n)
		{
			return new AsyncTableQuery<T> (_innerQuery.Skip (n));
		}

		public AsyncTableQuery<T> Take (int n)
		{
			return new AsyncTableQuery<T> (_innerQuery.Take (n));
		}

		public AsyncTableQuery<T> OrderBy<U> (Expression<Func<T, U>> orderExpr)
		{
			return new AsyncTableQuery<T> (_innerQuery.OrderBy<U> (orderExpr));
		}

		public AsyncTableQuery<T> OrderByDescending<U> (Expression<Func<T, U>> orderExpr)
		{
			return new AsyncTableQuery<T> (_innerQuery.OrderByDescending<U> (orderExpr));
		}

		public Task<List<T>> ToListAsync ()
		{
			return Task.Factory.StartNew (() => {
				// TODO: Lock the connection
				// load the items from the underlying store...
				return _innerQuery.ToList ();
			});
		}

		public Task<int> CountAsync ()
		{
			return Task.Factory.StartNew (() => {
				// TODO: Lock the connection
				return _innerQuery.Count ();
			});
		}

		public Task<T> ElementAtAsync (int index)
		{
			return Task.Factory.StartNew (() => {
				// TODO: Lock the connection
				return _innerQuery.ElementAt (index);
			});
		}
	}

	public class CreateTablesResult
	{
		public Dictionary<Type, int> Results { get; private set; }

		internal CreateTablesResult ()
		{
			this.Results = new Dictionary<Type, int> ();
		}
	}

	class SQLiteConnectionPool
	{
		class PoolEntry
		{
			public SQLiteConnectionSpecification Specification { get; private set; }
			public SQLiteConnectionWithLock Connection { get; private set; }

			internal PoolEntry (SQLiteConnectionSpecification spec)
			{
				Specification = spec;
				Connection = new SQLiteConnectionWithLock (spec);
			}

			public void OnApplicationSuspended ()
			{
				Connection.Dispose ();
				Connection = null;
			}
		}

		readonly Dictionary<string, PoolEntry> _entries = new Dictionary<string, PoolEntry> ();
		readonly object _entriesLock = new object ();

		static readonly SQLiteConnectionPool _current = new SQLiteConnectionPool ();

		/// <summary>
		/// Gets the singleton instance of the connection tool.
		/// </summary>
		public static SQLiteConnectionPool Current
		{
			get
			{
				return _current;
			}
		}

		public SQLiteConnectionWithLock GetConnection (SQLiteConnectionSpecification spec)
		{
			lock (_entriesLock) {
				PoolEntry entry;
				string key = spec.Key;

				if (!_entries.TryGetValue (key, out entry)) {
					entry = new PoolEntry (spec);
					_entries[key] = entry;
				}

				return entry.Connection;
			}
		}

		/// <summary>
		/// Closes all connections managed by this pool.
		/// </summary>
		public void Reset ()
		{
			lock (_entriesLock) {
				foreach (PoolEntry entry in _entries.Values) {
					entry.OnApplicationSuspended ();
				}
				_entries.Clear ();
			}
		}

		/// <summary>
		/// Call this method when the application is suspended.
		/// </summary>
		/// <remarks>Behaviour here is to close any open connections.</remarks>
		public void ApplicationSuspended ()
		{
			Reset ();
		}
	}

	/// <summary>
	/// Defines a class that points to a database.
	/// </summary>
	class SQLiteConnectionSpecification
	{
		public string Key { get; private set; }
		public string DatabasePath { get; private set; }

#if NETFX_CORE
		internal static string MetroStyleDataPath = null;
#endif

		public SQLiteConnectionSpecification (string connectionString)
		{
			Key = connectionString;

#if NETFX_CORE
			DatabasePath = System.IO.Path.Combine (MetroStyleDataPath, connectionString);
#else
			DatabasePath = connectionString;
#endif
		}

#if NETFX_CORE
		static SQLiteConnectionSpecification ()
		{
			MetroStyleDataPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
		}
#endif
	}

	class SQLiteConnectionWithLock : SQLiteConnection
	{
		readonly object _lockPoint = new object ();

		public SQLiteConnectionWithLock (SQLiteConnectionSpecification spec)
			: base (spec.DatabasePath)
		{
		}

		public IDisposable Lock ()
		{
			return new LockWrapper (this);
		}

		private class LockWrapper : IDisposable
		{
			object _lockPoint;

			public LockWrapper (object lockPoint)
			{
				_lockPoint = lockPoint;
				Monitor.Enter (_lockPoint);
			}

			public void Dispose ()
			{
				Monitor.Exit (_lockPoint);
			}
		}
	}
}

