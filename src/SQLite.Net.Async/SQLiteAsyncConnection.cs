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

namespace SQLite.Net.Async
{
    public class SQLiteAsyncConnection
    {
        private readonly Func<SQLiteConnectionWithLock> _sqliteConnectionFunc;
        private readonly TaskScheduler _taskScheduler;
        private readonly TaskCreationOptions _taskCreationOptions = TaskCreationOptions.None;

        /// <summary>
        /// Create a new async connection
        /// </summary>
        /// <param name="sqliteConnectionFunc"></param>
        /// <param name="taskScheduler">If null this parameter will be TaskScheduler.Default (evaluated when used in each method, not in ctor)</param>
        /// <param name="taskCreationOptions">Defaults to DenyChildAttach</param>
        public SQLiteAsyncConnection(Func<SQLiteConnectionWithLock> sqliteConnectionFunc, TaskScheduler taskScheduler = null, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None)
        {
            _sqliteConnectionFunc = sqliteConnectionFunc;
            _taskCreationOptions = taskCreationOptions;
            _taskScheduler = taskScheduler;
        }

        protected SQLiteConnectionWithLock GetConnection()
        {
            return _sqliteConnectionFunc();
        }

        public Task<CreateTablesResult> CreateTableAsync<T>(CancellationToken cancellationToken = default (CancellationToken))
            where T : new()
        {
            return CreateTablesAsync(cancellationToken, typeof(T));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2>(CancellationToken cancellationToken = default (CancellationToken))
            where T : new()
            where T2 : new()
        {
            return CreateTablesAsync(cancellationToken, typeof(T), typeof(T2));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3>(CancellationToken cancellationToken = default (CancellationToken))
            where T : new()
            where T2 : new()
            where T3 : new()
        {
            return CreateTablesAsync(cancellationToken, typeof(T), typeof(T2), typeof(T3));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4>(CancellationToken cancellationToken = default (CancellationToken))
            where T : new()
            where T2 : new()
            where T3 : new()
            where T4 : new()
        {
            return CreateTablesAsync(cancellationToken, typeof(T), typeof(T2), typeof(T3), typeof(T4));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4, T5>(CancellationToken cancellationToken = default (CancellationToken))
            where T : new()
            where T2 : new()
            where T3 : new()
            where T4 : new()
            where T5 : new()
        {
            return CreateTablesAsync(cancellationToken, typeof(T), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
        }

        public class CreateTablesResult
        {
            public Dictionary<Type, int> Results { get; private set; }

            internal CreateTablesResult()
            {
                this.Results = new Dictionary<Type, int>();
            }
        }

        public Task<CreateTablesResult> CreateTablesAsync(params Type[] types)
        {
            return CreateTablesAsync(CancellationToken.None, types);
        }

        public Task<CreateTablesResult> CreateTablesAsync(CancellationToken cancellationToken, params Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException("types");
            }
            return Task.Factory.StartNew(() =>
            {
                var result = new CreateTablesResult();
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    foreach (Type type in types)
                    {
                        int aResult = conn.CreateTable(type);
                        result.Results[type] = aResult;
                    }
                }
                return result;
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<int> DropTableAsync<T>(CancellationToken cancellationToken = default (CancellationToken))
            where T : new()
        {
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.DropTable<T>();
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<int> InsertAsync(object item, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Insert(item);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<int> UpdateAsync(object item, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Update(item);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<int> InsertOrReplaceAsync(object item, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.InsertOrReplace(item);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<int> DeleteAsync(object item, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Delete(item);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<int> DeleteAllAsync<T>(CancellationToken cancellationToken = default (CancellationToken))
        {
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.DeleteAll<T>();
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<int> DeleteAsync<T>(object pk, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (pk == null)
            {
                throw new ArgumentNullException("pk");
            }
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Delete<T>(pk);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<T> GetAsync<T>(object pk, CancellationToken cancellationToken = default(CancellationToken))
            where T : new()
        {
            if (pk == null)
            {
                throw new ArgumentNullException("pk");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return conn.Get<T>(pk);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<T> FindAsync<T>(object pk, CancellationToken cancellationToken = default (CancellationToken))
            where T : new()
        {
            if (pk == null)
            {
                throw new ArgumentNullException("pk");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return conn.Find<T>(pk);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<T> GetAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default (CancellationToken))
            where T : new()
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return conn.Get(predicate);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<T> FindAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
            where T : new()
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return conn.Find(predicate);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<int> ExecuteAsync(string query, params object[] args)
        {
            return ExecuteAsync(CancellationToken.None, query, args);
        }

        public Task<int> ExecuteAsync(CancellationToken cancellationToken, string query, params object[] args)
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
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Execute(query, args);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<int> InsertAllAsync(IEnumerable items, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.InsertAll(items);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<int> InsertOrReplaceAllAsync(IEnumerable items, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.InsertOrReplaceAll(items);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [Obsolete(
            "Will cause a deadlock if any call in action ends up in a different thread. Use RunInTransactionAsync(Action<SQLiteConnection>) instead."
            )]
        public Task RunInTransactionAsync(Action<SQLiteAsyncConnection> action, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
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

        public Task RunInTransactionAsync(Action<SQLiteConnection> action, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                SQLiteConnectionWithLock conn = GetConnection();
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

        public AsyncTableQuery<T> Table<T>()
            where T : new()
        {
            //
            // This isn't async as the underlying connection doesn't go out to the database
            // until the query is performed. The Async methods are on the query iteself.
            //
            SQLiteConnectionWithLock conn = GetConnection();
            return new AsyncTableQuery<T>(conn.Table<T>(), _taskScheduler, _taskCreationOptions);
        }

        public Task<T> ExecuteScalarAsync<T>(string sql, params object[] args)
        {
            return ExecuteScalarAsync<T>(CancellationToken.None, sql, args);
        }

        public Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken, string sql, params object[] args)
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
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SQLiteCommand command = conn.CreateCommand(sql, args);
                    return command.ExecuteScalar<T>();
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        public Task<List<T>> QueryAsync<T>(string sql, params object[] args)
            where T : new()
        {
            return QueryAsync<T> (CancellationToken.None, sql, args);
        }

        public Task<List<T>> QueryAsync<T>(CancellationToken cancellationToken, string sql, params object[] args)
            where T : new()
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
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return conn.Query<T>(sql, args);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }
    }
}
