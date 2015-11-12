//
// Copyright (c) 2012 Krueger Systems, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace SQLite.Net.Async
{
    public class SQLiteAsyncConnection
    {
        [NotNull] private readonly Func<SQLiteConnectionWithLock> _sqliteConnectionFunc;
        private readonly TaskCreationOptions _taskCreationOptions;
        [CanBeNull] private readonly TaskScheduler _taskScheduler;

        /// <summary>
        ///     Create a new async connection
        /// </summary>
        /// <param name="sqliteConnectionFunc"></param>
        /// <param name="taskScheduler">
        ///     If null this parameter will be TaskScheduler.Default (evaluated when used in each method,
        ///     not in ctor)
        /// </param>
        /// <param name="taskCreationOptions">Defaults to DenyChildAttach</param>
        [PublicAPI]
        public SQLiteAsyncConnection(
            [NotNull] Func<SQLiteConnectionWithLock> sqliteConnectionFunc, [CanBeNull] TaskScheduler taskScheduler = null,
            TaskCreationOptions taskCreationOptions = TaskCreationOptions.None)
        {
            _sqliteConnectionFunc = sqliteConnectionFunc;
            _taskCreationOptions = taskCreationOptions;
            _taskScheduler = taskScheduler;
        }

        [PublicAPI]
        protected SQLiteConnectionWithLock GetConnection()
        {
            return _sqliteConnectionFunc();
        }

        [PublicAPI]
        public Task<CreateTablesResult> CreateTableAsync<T>(CancellationToken cancellationToken = default (CancellationToken))
            where T : class
        {
            return CreateTablesAsync(cancellationToken, typeof (T));
        }

        [PublicAPI]
        public Task<CreateTablesResult> CreateTablesAsync<T, T2>(CancellationToken cancellationToken = default (CancellationToken))
            where T : class
            where T2 : class
        {
            return CreateTablesAsync(cancellationToken, typeof (T), typeof (T2));
        }

        [PublicAPI]
        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3>(CancellationToken cancellationToken = default (CancellationToken))
            where T : class
            where T2 : class
            where T3 : class
        {
            return CreateTablesAsync(cancellationToken, typeof (T), typeof (T2), typeof (T3));
        }

        [PublicAPI]
        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4>(CancellationToken cancellationToken = default (CancellationToken))
            where T : class
            where T2 : class
            where T3 : class
            where T4 : class
        {
            return CreateTablesAsync(cancellationToken, typeof (T), typeof (T2), typeof (T3), typeof (T4));
        }

        [PublicAPI]
        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4, T5>(CancellationToken cancellationToken = default (CancellationToken))
            where T : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
        {
            return CreateTablesAsync(cancellationToken, typeof (T), typeof (T2), typeof (T3), typeof (T4), typeof (T5));
        }

        [PublicAPI]
        public Task<CreateTablesResult> CreateTablesAsync([NotNull] params Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException("types");
            }
            return CreateTablesAsync(CancellationToken.None, types);
        }

        [PublicAPI]
        public Task<CreateTablesResult> CreateTablesAsync(CancellationToken cancellationToken = default(CancellationToken), [NotNull] params Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException("types");
            }
            return Task.Factory.StartNew(() =>
            {
                var result = new CreateTablesResult();
                var conn = GetConnection();
                using (conn.Lock())
                {
                    foreach (var type in types)
                    {
                        var aResult = conn.CreateTable(type);
                        result.Results[type] = aResult;
                    }
                }
                return result;
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> DropTableAsync<T>(CancellationToken cancellationToken = default (CancellationToken))
            where T : class
        {
            return DropTableAsync(typeof (T), cancellationToken);
        }

        [PublicAPI]
        public Task<int> DropTableAsync(Type t, CancellationToken cancellationToken = default (CancellationToken))
        {
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.DropTable(t);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> InsertAsync([NotNull] object item, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Insert(item);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> UpdateAsync([NotNull] object item, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Update(item);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }
        
        [PublicAPI]
        public Task<int> InsertOrIgnoreAsync (object item)
        {
            return Task.Factory.StartNew (() => {
                SQLiteConnectionWithLock conn = GetConnection ();
                using (conn.Lock ()) {
                    return conn.InsertOrIgnore (item);
                }
            });
        }

        [PublicAPI]
        public Task<int> InsertOrIgnoreAllAsync (IEnumerable objects, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (objects == null) {
                throw new ArgumentNullException ("objects");
            }

            return Task.Factory.StartNew (() => {
                SQLiteConnectionWithLock conn = GetConnection ();
                using (conn.Lock ()) {
                    return conn.InsertOrIgnoreAll (objects);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> InsertOrReplaceAsync([NotNull] object item, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.InsertOrReplace(item);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> DeleteAsync([NotNull] object item, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Delete(item);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> DeleteAllAsync<T>(CancellationToken cancellationToken = default (CancellationToken))
        {
            return DeleteAllAsync(typeof(T), cancellationToken);
        }

        [PublicAPI]
        public Task<int> DeleteAllAsync(Type t, CancellationToken cancellationToken = default (CancellationToken))
        {
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.DeleteAll(t);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> DeleteAsync<T>([NotNull] object pk, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (pk == null)
            {
                throw new ArgumentNullException("pk");
            }
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Delete<T>(pk);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<T> GetAsync<T>([NotNull] object pk, CancellationToken cancellationToken = default(CancellationToken))
            where T : class
        {
            if (pk == null)
            {
                throw new ArgumentNullException("pk");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return conn.Get<T>(pk);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<T> FindAsync<T>([NotNull] object pk, CancellationToken cancellationToken = default (CancellationToken))
            where T : class
        {
            if (pk == null)
            {
                throw new ArgumentNullException("pk");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return conn.Find<T>(pk);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<T> GetAsync<T>([NotNull] Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default (CancellationToken))
            where T : class
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return conn.Get(predicate);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<T> FindAsync<T>([NotNull] Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
            where T : class
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return conn.Find(predicate);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> ExecuteAsync([NotNull] string query, [NotNull] params object[] args)
        {
            return ExecuteAsync(CancellationToken.None, query, args);
        }

        [PublicAPI]
        public Task<int> ExecuteAsync(CancellationToken cancellationToken, [NotNull] string query, [NotNull] params object[] args)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Execute(query, args);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> InsertAllAsync([NotNull] IEnumerable items, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.InsertAll(items);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> InsertOrReplaceAllAsync([NotNull] IEnumerable items, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.InsertOrReplaceAll(items);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> UpdateAllAsync([NotNull] IEnumerable items, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.UpdateAll(items);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [Obsolete(
            "Will cause a deadlock if any call in action ends up in a different thread. Use RunInTransactionAsync(Action<SQLiteConnection>) instead."
            )]
        [PublicAPI]
        public Task RunInTransactionAsync([NotNull] Action<SQLiteAsyncConnection> action, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            return Task.Factory.StartNew(() =>
            {
                var conn = GetConnection();
                using (conn.Lock())
                {
                    conn.BeginTransaction();
                    try
                    {
                        action(this);
                        conn.Commit();
                    }
                    catch (Exception)
                    {
                        conn.Rollback();
                        throw;
                    }
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task RunInTransactionAsync([NotNull] Action<SQLiteConnection> action, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    conn.BeginTransaction();
                    try
                    {
                        action(conn);
                        conn.Commit();
                    }
                    catch (Exception)
                    {
                        conn.Rollback();
                        throw;
                    }
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public AsyncTableQuery<T> Table<T>()
            where T : class
        {
            //
            // This isn't async as the underlying connection doesn't go out to the database
            // until the query is performed. The Async methods are on the query iteself.
            //
            var conn = GetConnection();
            return new AsyncTableQuery<T>(conn.Table<T>(), _taskScheduler, _taskCreationOptions);
        }

        [PublicAPI]
        public Task<T> ExecuteScalarAsync<T>([NotNull] string sql, [NotNull] params object[] args)
        {
            return ExecuteScalarAsync<T>(CancellationToken.None, sql, args);
        }

        [PublicAPI]
        public Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken, [NotNull] string sql, [NotNull] params object[] args)
        {
            if (sql == null)
            {
                throw new ArgumentNullException("sql");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var command = conn.CreateCommand(sql, args);
                    return command.ExecuteScalar<T>();
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task ExecuteNonQueryAsync([NotNull] string sql, [NotNull] params object[] args)
        {
            return ExecuteNonQueryAsync(CancellationToken.None, sql, args);
        }

        [PublicAPI]
        public Task ExecuteNonQueryAsync(CancellationToken cancellationToken, [NotNull] string sql, [NotNull] params object[] args)
        {
            if (sql == null)
            {
                throw new ArgumentNullException("sql");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var command = conn.CreateCommand(sql, args);
                    command.ExecuteNonQuery();
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<List<T>> QueryAsync<T>([NotNull] string sql, [NotNull] params object[] args)
            where T : class
        {
            return QueryAsync<T>(CancellationToken.None, sql, args);
        }

        [PublicAPI]
        public Task<List<T>> QueryAsync<T>(CancellationToken cancellationToken, [NotNull] string sql, params object[] args)
            where T : class
        {
            if (sql == null)
            {
                throw new ArgumentNullException("sql");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return conn.Query<T>(sql, args);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<TableMapping> GetMappingAsync<T> ()
        {
            return Task.Factory.StartNew (() => {
                SQLiteConnectionWithLock conn = GetConnection ();
                using (conn.Lock ()) {
                    return conn.GetMapping (typeof(T));
                }
            }, CancellationToken.None, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }
    }
}