using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
}
