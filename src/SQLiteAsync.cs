using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLite
{
    public class SQLiteAsyncConnection
    {
        public SQLiteConnectionSpecification Specification { get; private set; }

        public SQLiteAsyncConnection(SQLiteConnectionSpecification spec)
        {
            this.Specification = spec;
        }

        public Task<CreateTablesResult> CreateTableAsync<T>()
            where T : new()
        {
            return CreateTablesAsync(typeof(T));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2>()
            where T : new()
            where T2 : new()
        {
            return CreateTablesAsync(typeof(T), typeof(T2));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3>()
            where T : new()
            where T2 : new()
            where T3 : new()
        {
            return CreateTablesAsync(typeof(T), typeof(T2), typeof(T3));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4>()
            where T : new()
            where T2 : new()
            where T3 : new()
            where T4 : new()
        {
            return CreateTablesAsync(typeof(T), typeof(T2), typeof(T3), typeof(T4));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4, T5>()
            where T : new()
            where T2 : new()
            where T3 : new()
            where T4 : new()
            where T5 : new()
        {
            return CreateTablesAsync(typeof(T), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
        }

        public Task<CreateTablesResult> CreateTablesAsync(params Type[] types)
        {
            return Task.Factory.StartNew(() =>
            {
                CreateTablesResult result = new CreateTablesResult();
                using (var conn = GetConcreteConnection())
                {
                    using (conn.Lock())
                    {
                        foreach (Type type in types)
                        {
                            int aResult = conn.CreateTable(type);
                            result.Results[type] = aResult;
                        }
                    }
                }

                // return...
                return result;

            });
        }

        public Task<int> DropTableAsync<T>()
            where T : new()
        {
            return Task.Factory.StartNew(() =>
            {
                using (var conn = GetConcreteConnection())
                {
                    using(conn.Lock())
                        return conn.DropTable<T>();
                }

            });
        }

        internal SQLiteConnectionWithLock GetConcreteConnection()
        {
            return SQLiteConnectionPool.Current.GetConnection(this.Specification);
        }

        public Task<int> InsertAsync(object item)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var conn = GetConcreteConnection())
                {
                    using (conn.Lock())
                        return conn.Insert(item);
                }

            });
        }

        public Task<int> UpdateAsync(object item)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var conn = GetConcreteConnection())
                {
                    using(conn.Lock())
                        return conn.Update(item);
                }

            });
        }

        public Task<int> DeleteAsync(object item)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var conn = GetConcreteConnection())
                {
                    using(conn.Lock())
                        return conn.Delete(item);
                }
            });
        }

        public Task<T> GetAsync<T>(object pk)
            where T : new()
        {
            return Task.Factory.StartNew(() =>
            {
                using (var conn = GetConcreteConnection())
                {
                    using(conn.Lock())
                        return conn.Get<T>(pk);
                }
            });
        }

        public Task<T> GetSafeAsync<T>(object pk)
            where T : new()
        {
            return Task<T>.Factory.StartNew(() =>
            {
                // go...
                using (var conn = GetConcreteConnection())
                {
                    using (conn.Lock())
                    {
                        // create a command...
                        var map = conn.GetMapping<T>();
                        string query = string.Format("select * from \"{0}\" where \"{1}\" = ?", map.TableName, map.PK.Name);

                        SQLiteCommand command = conn.CreateCommand(query, pk);
                        List<T> results = command.ExecuteQuery<T>();
                        if (results.Count > 0)
                            return results[0];
                        else
                            return default(T);
                    }
                }
            });
        }

        public Task<int> ExecuteAsync(string query, params object[] args)
        {
            return Task<int>.Factory.StartNew(() =>
            {
                using (var conn = GetConcreteConnection())
                {
                    using(conn.Lock())
                        return conn.Execute(query, args);
                }

            });
        }

        public Task<int> InsertAllAsync(IEnumerable items)
        {
            return Task<int>.Factory.StartNew(() =>
            {
                using (var conn = GetConcreteConnection())
                {
                    using(conn.Lock())
                        return conn.InsertAll(items);
                }
            });
        }

        public Task<int> RunInTransactionAsync(Action<SQLiteAsyncConnection> action)
        {
            return Task<int>.Factory.StartNew(() =>
            {
                using (var conn = this.GetConcreteConnection())
                {
                    using (conn.Lock())
                    {
                        conn.BeginTransaction();
                        try
                        {
                            action(this);
                            conn.Commit();
                        }
                        catch (Exception ex)
                        {
                            conn.Rollback();
                            throw new InvalidOperationException("The transaction failed.", ex);
                        }
                    }
                }

                // have to return something to get nested task behaviour...
                return 1;

            });
        }

        public AsyncTableQuery<T> Table<T>()
            where T : new()
        {
            // this isn't async as the underlying connection doesn't go out to the database
            // until GetEnumerator is called.
            using (var conn = this.GetConcreteConnection())
                return new AsyncTableQuery<T>(conn.Table<T>());
        }

        public Task<T> ExecuteScalarAsync<T>(string sql, params object[] args)
        {
            // run...
            return Task<T>.Factory.StartNew(() =>
            {
                // get...
                using(var conn = this.GetConcreteConnection())
                {
                    using (conn.Lock())
                    {
                        SQLiteCommand command = conn.CreateCommand(sql, args);
                        return command.ExecuteScalar<T>();
                    }
                }

            });
        }

        public Task<List<T>> QueryAsync<T>(string sql, params object[] args)
            where T : new()
        {
            // run...
            return Task<List<T>>.Factory.StartNew(() =>
            {
                // get...
                using (var conn = this.GetConcreteConnection())
                {
                    using (conn.Lock())
                        return conn.Query<T>(sql, args);
                }

            });
        }

        public Task<IEnumerable<T>> DeferredQueryAsync<T>(string sql, params object[] args)
            where T : new()
        {
            // run...
            return Task<IEnumerable<T>>.Factory.StartNew(() =>
            {
                // get...
                using (var conn = this.GetConcreteConnection())
                {
                    // @mbrit - 2012-05-14 - needs improving. we can't use a deferred query as the connection
                    // goes away. this just simulates the old call with the existing Query call...
                  //  return conn.DeferredQuery<T>(source);
                    using (conn.Lock())
                        return conn.Query<T>(sql, args);
                }

            });
        }
    }

    public class AsyncTableQuery<T> : IEnumerable<T>
        where T : new()
    {
        private TableQuery<T> InnerQuery { get; set; }

        public AsyncTableQuery(TableQuery<T> innerQuery)
        {
            this.InnerQuery = innerQuery;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.InnerQuery.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public AsyncTableQuery<T> Clone()
        {
            return new AsyncTableQuery<T>(this.InnerQuery.Clone());
        }

        public Task<List<T>> ToListAsync()
        {
            return Task<List<T>>.Factory.StartNew(() =>
            {
                // load the items from the underlying store...
                return new List<T>(this);
            });
        }

        public AsyncTableQuery<T> Where(Expression<Func<T, bool>> predExpr)
        {
            var newQuery = this.InnerQuery.Where(predExpr);
            return new AsyncTableQuery<T>(newQuery);
        }

        public Task<int> CountAsync()
        {
            return Task<int>.Factory.StartNew(() =>
            {
                return this.InnerQuery.Count();
            });
        }

        public Task<T> ElementAtAsync(int index)
        {
            return Task<T>.Factory.StartNew(() =>
            {
                return this.InnerQuery.ElementAt(index);

            });
        }

        public AsyncTableQuery<T> Skip(int n)
        {
            var newQuery = this.InnerQuery.Skip(n);
            return new AsyncTableQuery<T>(newQuery);
        }

        public AsyncTableQuery<T> Take(int n)
        {
            var newQuery = this.InnerQuery.Take(n);
            return new AsyncTableQuery<T>(newQuery);
        }

        public AsyncTableQuery<T> OrderBy<U>(Expression<Func<T, U>> orderExpr)
        {
            var newQuery = this.InnerQuery.OrderBy<U>(orderExpr);
            return new AsyncTableQuery<T>(newQuery);
        }

        public AsyncTableQuery<T> OrderByDescending<U>(Expression<Func<T, U>> orderExpr)
        {
            var newQuery = this.InnerQuery.OrderByDescending<U>(orderExpr);
            return new AsyncTableQuery<T>(newQuery);
        }
    }

    public class CreateTablesResult
    {
        public Dictionary<Type, int> Results { get; private set; }

        internal CreateTablesResult()
        {
            this.Results = new Dictionary<Type, int>();
        }
    }

    internal class PooledConnection
    {
        internal long PoolId { get; private set; }
        internal SQLiteConnectionWithLock Connection { get; private set; }
        internal PooledConnectionState State { get; private set; }
        internal DateTime ExpiresUtc { get; private set; }

        private static long _nextPoolId = 0;

        private const int ExpirationMinutes = 120;

        internal PooledConnection(SQLiteConnectionWithLock conn)
        {
            this.PoolId = Interlocked.Increment(ref _nextPoolId);
            this.Connection = conn;
            this.State = PooledConnectionState.InUse;
            this.UpdateExpiration();
        }

        internal void SetIdle()
        {
            if(this.State == PooledConnectionState.Idle || this.State == PooledConnectionState.InUse)
                this.State = PooledConnectionState.Idle;
            else
                throw new NotSupportedException(string.Format("Cannot handle '{0}'.", this.State));
        }

        internal void SetAsInUse()
        {
            if (this.State == PooledConnectionState.Idle)
            {
                this.State = PooledConnectionState.InUse;
                this.UpdateExpiration();
            }
            else
                throw new NotSupportedException(string.Format("Cannot handle '{0}'.", this.State));
        }

        private void UpdateExpiration()
        {
            this.ExpiresUtc = DateTime.UtcNow.AddMinutes(ExpirationMinutes);
        }
    }

    public enum PooledConnectionState
    {
        Idle = 0,
        InUse = 1   
    }

    internal class PoolEntry : IPoolEntry
    {
        private SQLiteConnectionSpecification Specification { get; set; }
        private List<PooledConnection> Connections { get; set; }
        private object _connectionsLock = new object();

        private static object _globalLock = new object();

        internal PoolEntry(SQLiteConnectionSpecification spec)
        {
            this.Specification = spec;
            this.Connections = new List<PooledConnection>();
        }

        internal SQLiteConnectionWithLock GetUnusedConnection()
        {
            lock(_connectionsLock)
            {
                // find...
                PooledConnection idle = this.Connections.Where(v => v.State == PooledConnectionState.Idle).FirstOrDefault();
                if (idle != null)
                {
                    idle.SetAsInUse();
                    return idle.Connection;
                }

                // create one...
                var conn = this.Specification.GetConcreteConnectionWithLock(_globalLock);
                PooledConnection pooled = new PooledConnection(conn);
                pooled.Connection.Enlist(this, pooled.PoolId);
                this.Connections.Add(pooled);
                return pooled.Connection;
            }
        }

        internal bool ConnectionFinished(SQLiteConnection conn)
        {
            lock(_connectionsLock)
            {
                // find it...
                PooledConnection pooled = this.Connections.Where(v => v.PoolId == conn.PoolId).FirstOrDefault();
                if (pooled != null)
                {
                    // set it to idle...
                    pooled.SetIdle();

                    // signal that we found it...
                    return true;
                }
                else
                {
                    // signal that we didn't find...
                    return false;
                }
            }
        }

        internal void ApplicationSuspended()
        {
            lock(_connectionsLock)
            {
                // walk...
                List<PooledConnection> toRemove = new List<PooledConnection>();
                foreach (PooledConnection conn in this.Connections)
                {
                    if (conn.State == PooledConnectionState.Idle)
                    {
                        conn.Connection.CloseInternal();
                        toRemove.Add(conn);
                    }
                }

                // remove...
                foreach (PooledConnection conn in toRemove)
                    this.Connections.Remove(conn);
            }
        }

        IPoolOwner IPoolEntry.Owner
        {
            get
            {
                return SQLiteConnectionPool.Current;
            }
        }
    }

    public class SQLiteConnectionPool : IPoolOwner
    {
        private Dictionary<string, PoolEntry> Entries { get; set; }
        private object _entriesLock = new object();

		private static SQLiteConnectionPool _current = new SQLiteConnectionPool();
				
		private SQLiteConnectionPool()
		{
            this.Entries = new Dictionary<string, PoolEntry>();
		}
						
		/// <summary>
		/// Gets the singleton instance of the connection tool.
		/// </summary>
		public static SQLiteConnectionPool Current
		{
			get
			{
				if(_current == null)
					throw new ObjectDisposedException("SQLiteConnectionPool");
				return _current;
			}
		}

        internal SQLiteConnectionWithLock GetConnection(SQLiteConnectionSpecification spec)
        {
            lock(_entriesLock)
            {
                string key = spec.Key;
                if (!(this.Entries.ContainsKey(key)))
                    this.Entries[key] = new PoolEntry(spec);

                // return an unused connection...
                PoolEntry entry = this.Entries[key];
                return entry.GetUnusedConnection();
            }
        }

        /// <summary>
        /// Resets the pool - only for use in unit tests.
        /// </summary>
        public void Reset()
        {
            _current = new SQLiteConnectionPool();
        }

        public void ConnectionFinished(SQLiteConnection conn)
        {
            lock(_entriesLock)
            {
                // signal that we've finished...
                bool  found= false;
                foreach(PoolEntry entry in this.Entries.Values)
                {
                    if(entry.ConnectionFinished(conn))
                    {
                        found= true;
                        break;
                    }
                }

                // if we're not found, close directly...
                if(!(found))
                    conn.CloseInternal ();
            }
        }

        /// <summary>
        /// Call this method when the application is suspended.
        /// </summary>
        /// <remarks>Behaviour here is to close any open connections that are idle. We can't wait around in this
        /// call (we only have five seconds total), so we can't block for running queries to finish. What happens
        /// is that the pool is reset. Any connections that call Dispose after this point will not be found in the pool
        /// and regular cleanup operations will happen.</remarks>
        public void ApplicationSuspended()
        {
            lock(_entriesLock)
            {
                // find...
                foreach (PoolEntry entry in this.Entries.Values)
                    entry.ApplicationSuspended();

                // reset the pool...
                this.Entries = new Dictionary<string, PoolEntry>();
            }
        }
    }

    /// <summary>
    /// Defines a class that points to a database.
    /// </summary>
    public class SQLiteConnectionSpecification
    {
        private string DatabasePath { get; set; }
        private SQLiteOpenFlags Flags { get; set; }
        private bool HasFlags { get; set; }
        internal string Key { get; set; }

#if NETFX_CORE
        internal static string MetroStyleDataPath = null;
#endif

        public SQLiteConnectionSpecification(string databasePath)
        {
            this.Initialize(databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite, false);
        }

        public SQLiteConnectionSpecification(string databasePath, SQLiteOpenFlags flags)
        {
            this.Initialize(databasePath, flags, true);
        }

        static SQLiteConnectionSpecification()
        {
#if NETFX_CORE
            MetroStyleDataPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#endif
        }

#if NETFX_CORE
        /// <summary>
        /// Method for creataing specifications for use with Metro-style apps. (Puts the file in the correct location.)
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public static SQLiteConnectionSpecification CreateForAsyncMetroStyle(string databaseName)
        {
            string path = null;
            return CreateForAsyncMetroStyle(databaseName, ref path);
        }

        /// <summary>
        /// Method for creataing specifications for use with Metro-style apps. (Puts the file in the correct location.)
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public static SQLiteConnectionSpecification CreateForAsyncMetroStyle(string databaseName, ref string path)
        {
            path = Path.Combine(MetroStyleDataPath, databaseName);
            return new SQLiteConnectionSpecification(path, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.FullMutex);
        }
#else
        /// <summary>
        /// Method for creating specifications for use with asynchronous calls. This must be used with the async methods.
        /// </summary>
        /// <param name="databasePath"></param>
        /// <returns></returns>
        /// <remarks>Not supported in Metro-style as it will fail on the folder create.</remarks>
        public static SQLiteConnectionSpecification CreateForAsync(string databasePath)
        {
            return new SQLiteConnectionSpecification(databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | 
                SQLiteOpenFlags.FullMutex);
        }

#endif

        private void Initialize(string databasePath, SQLiteOpenFlags flags, bool hasFlags)
        {
            this.DatabasePath = databasePath;
            this.Flags = flags;
            this.HasFlags = hasFlags;

            // configure the key...
            this.Key = string.Format("{0}|{1}|{2}", this.Flags, this.HasFlags, this.DatabasePath);
        }

        internal SQLiteConnectionWithLock GetConcreteConnectionWithLock(object lockPoint)
        {
            if (this.HasFlags)
                return new SQLiteConnectionWithLock(this.DatabasePath, this.Flags, lockPoint);
            else
                return new SQLiteConnectionWithLock(this.DatabasePath, lockPoint);
        }
    }

    internal class SQLiteConnectionWithLock : SQLiteConnection
    {
        private object LockPoint { get; set; }

        internal SQLiteConnectionWithLock(string databasePath, object lockPoint)
            : base(databasePath)
        {
            this.Initialize(lockPoint);
        }

        internal SQLiteConnectionWithLock(string databasePath, SQLiteOpenFlags flags, object lockPoint)
            : base(databasePath, flags)
        {
            this.Initialize(lockPoint);
        }

        private void Initialize(object lockPoint)
        {
            this.LockPoint = lockPoint;
        }

        internal IDisposable Lock()
        {
            return new LockWrapper(this);
        }

        private class LockWrapper : IDisposable
        {
            private SQLiteConnectionWithLock Owner { get; set; }

            internal LockWrapper(SQLiteConnectionWithLock owner)
            {
                this.Owner = owner;

                // lock it...
                Monitor.Enter(this.Owner.LockPoint);
            }

            public void Dispose()
            {
                // unlock it...
 	            Monitor.Exit(this.Owner.LockPoint);
            }
        }
    }
}
