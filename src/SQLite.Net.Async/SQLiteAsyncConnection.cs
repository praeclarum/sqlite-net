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
using System.Threading.Tasks;

namespace SQLite.Net.Async
{
    public class SQLiteAsyncConnection
    {
        private readonly Func<SQLiteConnectionWithLock> _sqliteConnectionFunc;
        private readonly TaskFactory _taskFactory;

        /// <summary>
        /// Create a new async connection, will use TaskScheduler.Default
        /// </summary>
        /// <param name="sqliteConnectionFunc"></param>
        public SQLiteAsyncConnection(Func<SQLiteConnectionWithLock> sqliteConnectionFunc)
        {
            _sqliteConnectionFunc = sqliteConnectionFunc;
            _taskFactory = new TaskFactory(TaskScheduler.Default);
        }

        /// <summary>
        /// Create a new async connection with a specific TaskScheduler 
        /// </summary>
        /// <param name="sqliteConnectionFunc"></param>
        /// <param name="taskScheduler"></param>
        public SQLiteAsyncConnection(Func<SQLiteConnectionWithLock> sqliteConnectionFunc, TaskScheduler taskScheduler)
        {
            _sqliteConnectionFunc = sqliteConnectionFunc;;
            _taskFactory = new TaskFactory(taskScheduler ?? TaskScheduler.Default);
        }

        private SQLiteConnectionWithLock GetConnection()
        {
            return _sqliteConnectionFunc();
        }

        public Task<CreateTablesResult> CreateTableAsync<T>()
            where T : new()
        {
            return CreateTablesAsync(typeof (T));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2>()
            where T : new()
            where T2 : new()
        {
            return CreateTablesAsync(typeof (T), typeof (T2));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3>()
            where T : new()
            where T2 : new()
            where T3 : new()
        {
            return CreateTablesAsync(typeof (T), typeof (T2), typeof (T3));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4>()
            where T : new()
            where T2 : new()
            where T3 : new()
            where T4 : new()
        {
            return CreateTablesAsync(typeof (T), typeof (T2), typeof (T3), typeof (T4));
        }

        public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4, T5>()
            where T : new()
            where T2 : new()
            where T3 : new()
            where T4 : new()
            where T5 : new()
        {
            return CreateTablesAsync(typeof (T), typeof (T2), typeof (T3), typeof (T4), typeof (T5));
        }

        public Task<CreateTablesResult> CreateTablesAsync(params Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException("types");
            }
            return _taskFactory.StartNew(() =>
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
            });
        }

        public Task<int> DropTableAsync<T>()
            where T : new()
        {
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.DropTable<T>();
                }
            });
        }

        public Task<int> InsertAsync(object item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Insert(item);
                }
            });
        }

        public Task<int> UpdateAsync(object item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Update(item);
                }
            });
        }

        public Task<int> DeleteAsync(object item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Delete(item);
                }
            });
        }

        public Task<int> DeleteAllAsync<T>()
        {
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.DeleteAll<T>();
                }
            });
        }

        public Task<int> DeleteAsync<T>(object pk)
        {
            if (pk == null)
            {
                throw new ArgumentNullException("pk");
            }
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Delete<T>(pk);
                }
            });
        }

        public Task<T> GetAsync<T>(object pk)
            where T : new()
        {
            if (pk == null)
            {
                throw new ArgumentNullException("pk");
            }
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Get<T>(pk);
                }
            });
        }

        public Task<T> FindAsync<T>(object pk)
            where T : new()
        {
            if (pk == null)
            {
                throw new ArgumentNullException("pk");
            }
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Find<T>(pk);
                }
            });
        }

        public Task<T> GetAsync<T>(Expression<Func<T, bool>> predicate)
            where T : new()
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Get(predicate);
                }
            });
        }

        public Task<T> FindAsync<T>(Expression<Func<T, bool>> predicate)
            where T : new()
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Find(predicate);
                }
            });
        }

        public Task<int> ExecuteAsync(string query, params object[] args)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            return _taskFactory.StartNew(() =>

            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Execute(query, args);
                }
            });
        }

        public Task<int> InsertAllAsync(IEnumerable items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.InsertAll(items);
                }
            });
        }

        [Obsolete(
            "Will cause a deadlock if any call in action ends up in a different thread. Use RunInTransactionAsync(Action<SQLiteConnection>) instead."
            )]
        public Task RunInTransactionAsync(Action<SQLiteAsyncConnection> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            return _taskFactory.StartNew(() =>
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
            });
        }

        public Task RunInTransactionAsync(Action<SQLiteConnection> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
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
            });
        }

        public AsyncTableQuery<T> Table<T>()
            where T : new()
        {
            //
            // This isn't async as the underlying connection doesn't go out to the database
            // until the query is performed. The Async methods are on the query iteself.
            //
            SQLiteConnectionWithLock conn = GetConnection();
            return new AsyncTableQuery<T>(conn.Table<T>(), _taskFactory);
        }

        public Task<T> ExecuteScalarAsync<T>(string sql, params object[] args)
        {
            if (sql == null)
            {
                throw new ArgumentNullException("sql");
            }
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    SQLiteCommand command = conn.CreateCommand(sql, args);
                    return command.ExecuteScalar<T>();
                }
            });
        }

        public Task<List<T>> QueryAsync<T>(string sql, params object[] args)
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
            return _taskFactory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = GetConnection();
                using (conn.Lock())
                {
                    return conn.Query<T>(sql, args);
                }
            });
        }
    }
}