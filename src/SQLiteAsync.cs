//
// Copyright (c) 2012-2024 Krueger Systems, Inc.
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
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1591 // XML Doc Comments

namespace SQLite
{
	public interface ISQLiteAsyncConnection
	{
		string DatabasePath { get; }
		int LibVersionNumber { get; }
		string DateTimeStringFormat { get; }
		bool StoreDateTimeAsTicks { get; }
		bool StoreTimeSpanAsTicks { get; }
		bool Trace { get; set; }
		Action<string> Tracer { get; set; }
		bool TimeExecution { get; set; }
		IEnumerable<TableMapping> TableMappings { get; }

		Task BackupAsync (string destinationDatabasePath, string databaseName = "main");
		Task CloseAsync ();
		Task<int> CreateIndexAsync (string tableName, string columnName, bool unique = false);
		Task<int> CreateIndexAsync (string indexName, string tableName, string columnName, bool unique = false);
		Task<int> CreateIndexAsync (string tableName, string[] columnNames, bool unique = false);
		Task<int> CreateIndexAsync (string indexName, string tableName, string[] columnNames, bool unique = false);
		Task<int> CreateIndexAsync<T> (Expression<Func<T, object>> property, bool unique = false);
		Task<CreateTableResult> CreateTableAsync<T> (CreateFlags createFlags = CreateFlags.None) where T : new();
		Task<CreateTableResult> CreateTableAsync (Type ty, CreateFlags createFlags = CreateFlags.None);
		Task<CreateTablesResult> CreateTablesAsync<T, T2> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new();
		Task<CreateTablesResult> CreateTablesAsync<T, T2, T3> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new();
		Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new()
			where T4 : new();
		Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4, T5> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new()
			where T4 : new()
			where T5 : new();
		Task<CreateTablesResult> CreateTablesAsync (CreateFlags createFlags = CreateFlags.None, params Type[] types);
		Task<IEnumerable<T>> DeferredQueryAsync<T> (string query, params object[] args) where T : new();
		Task<IEnumerable<object>> DeferredQueryAsync (TableMapping map, string query, params object[] args);
		Task<int> DeleteAllAsync<T> ();
		Task<int> DeleteAllAsync (TableMapping map);
		Task<int> DeleteAsync (object objectToDelete);
		Task<int> DeleteAsync<T> (object primaryKey);
		Task<int> DeleteAsync (object primaryKey, TableMapping map);
		Task<int> DropTableAsync<T> () where T : new();
		Task<int> DropTableAsync (TableMapping map);
		Task EnableLoadExtensionAsync (bool enabled);
		Task EnableWriteAheadLoggingAsync ();
		Task<int> ExecuteAsync (string query, params object[] args);
		Task<T> ExecuteScalarAsync<T> (string query, params object[] args);
		Task<T> FindAsync<T> (object pk) where T : new();
		Task<object> FindAsync (object pk, TableMapping map);
		Task<T> FindAsync<T> (Expression<Func<T, bool>> predicate) where T : new();
		Task<T> FindWithQueryAsync<T> (string query, params object[] args) where T : new();
		Task<object> FindWithQueryAsync (TableMapping map, string query, params object[] args);
		Task<T> GetAsync<T> (object pk) where T : new();
		Task<object> GetAsync (object pk, TableMapping map);
		Task<T> GetAsync<T> (Expression<Func<T, bool>> predicate) where T : new();
		TimeSpan GetBusyTimeout ();
		SQLiteConnectionWithLock GetConnection ();
		Task<TableMapping> GetMappingAsync (Type type, CreateFlags createFlags = CreateFlags.None);
		Task<TableMapping> GetMappingAsync<T> (CreateFlags createFlags = CreateFlags.None) where T : new();
		Task<List<SQLiteConnection.ColumnInfo>> GetTableInfoAsync (string tableName);
		Task<int> InsertAllAsync (IEnumerable objects, bool runInTransaction = true);
		Task<int> InsertAllAsync (IEnumerable objects, string extra, bool runInTransaction = true);
		Task<int> InsertAllAsync (IEnumerable objects, Type objType, bool runInTransaction = true);
		Task<int> InsertAsync (object obj);
		Task<int> InsertAsync (object obj, Type objType);
		Task<int> InsertAsync (object obj, string extra);
		Task<int> InsertAsync (object obj, string extra, Type objType);
		Task<int> InsertOrReplaceAsync (object obj);
		Task<int> InsertOrReplaceAsync (object obj, Type objType);
		Task<List<T>> QueryAsync<T> (string query, params object[] args) where T : new();
		Task<List<object>> QueryAsync (TableMapping map, string query, params object[] args);
		Task<List<T>> QueryScalarsAsync<T> (string query, params object[] args);
		Task ReKeyAsync (string key);
		Task ReKeyAsync (byte[] key);
		Task RunInTransactionAsync (Action<SQLiteConnection> action);
		Task SetBusyTimeoutAsync (TimeSpan value);
		AsyncTableQuery<T> Table<T> () where T : new();
		Task<int> UpdateAllAsync (IEnumerable objects, bool runInTransaction = true);
		Task<int> UpdateAsync (object obj);
		Task<int> UpdateAsync (object obj, Type objType);
	}

	/// <summary>
	/// A pooled asynchronous connection to a SQLite database.
	/// </summary>
	public partial class SQLiteAsyncConnection : ISQLiteAsyncConnection
	{
		readonly SQLiteConnectionString _connectionString;

		/// <summary>
		/// Constructs a new SQLiteAsyncConnection and opens a pooled SQLite database specified by databasePath.
		/// </summary>
		/// <param name="databasePath">
		/// Specifies the path to the database file.
		/// </param>
		/// <param name="storeDateTimeAsTicks">
		/// Specifies whether to store DateTime properties as ticks (true) or strings (false). You
		/// absolutely do want to store them as Ticks in all new projects. The value of false is
		/// only here for backwards compatibility. There is a *significant* speed advantage, with no
		/// down sides, when setting storeDateTimeAsTicks = true.
		/// If you use DateTimeOffset properties, it will be always stored as ticks regardingless
		/// the storeDateTimeAsTicks parameter.
		/// </param>
		public SQLiteAsyncConnection (string databasePath, bool storeDateTimeAsTicks = true)
			: this (new SQLiteConnectionString (databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex, storeDateTimeAsTicks))
		{
		}

		/// <summary>
		/// Constructs a new SQLiteAsyncConnection and opens a pooled SQLite database specified by databasePath.
		/// </summary>
		/// <param name="databasePath">
		/// Specifies the path to the database file.
		/// </param>
		/// <param name="openFlags">
		/// Flags controlling how the connection should be opened.
		/// Async connections should have the FullMutex flag set to provide best performance.
		/// </param>
		/// <param name="storeDateTimeAsTicks">
		/// Specifies whether to store DateTime properties as ticks (true) or strings (false). You
		/// absolutely do want to store them as Ticks in all new projects. The value of false is
		/// only here for backwards compatibility. There is a *significant* speed advantage, with no
		/// down sides, when setting storeDateTimeAsTicks = true.
		/// If you use DateTimeOffset properties, it will be always stored as ticks regardingless
		/// the storeDateTimeAsTicks parameter.
		/// </param>
		public SQLiteAsyncConnection (string databasePath, SQLiteOpenFlags openFlags, bool storeDateTimeAsTicks = true)
			: this (new SQLiteConnectionString (databasePath, openFlags, storeDateTimeAsTicks))
		{
		}

		/// <summary>
		/// Constructs a new SQLiteAsyncConnection and opens a pooled SQLite database
		/// using the given connection string.
		/// </summary>
		/// <param name="connectionString">
		/// Details on how to find and open the database.
		/// </param>
		public SQLiteAsyncConnection (SQLiteConnectionString connectionString)
		{
			_connectionString = connectionString;
		}

		/// <summary>
		/// Gets the database path used by this connection.
		/// </summary>
		public string DatabasePath => GetConnection ().DatabasePath;

		/// <summary>
		/// Gets the SQLite library version number. 3007014 would be v3.7.14
		/// </summary>
		public int LibVersionNumber => GetConnection ().LibVersionNumber;

		/// <summary>
		/// The format to use when storing DateTime properties as strings. Ignored if StoreDateTimeAsTicks is true.
		/// </summary>
		/// <value>The date time string format.</value>
		public string DateTimeStringFormat => GetConnection ().DateTimeStringFormat;

		/// <summary>
		/// The amount of time to wait for a table to become unlocked.
		/// </summary>
		public TimeSpan GetBusyTimeout ()
		{
			return GetConnection ().BusyTimeout;
		}

		/// <summary>
		/// Sets the amount of time to wait for a table to become unlocked.
		/// </summary>
		public Task SetBusyTimeoutAsync (TimeSpan value)
		{
			return ReadAsync<object> (conn => {
				conn.BusyTimeout = value;
				return null;
			});
		}

		/// <summary>
		/// Enables the write ahead logging. WAL is significantly faster in most scenarios
		/// by providing better concurrency and better disk IO performance than the normal
		/// journal mode. You only need to call this function once in the lifetime of the database.
		/// </summary>
		public Task EnableWriteAheadLoggingAsync ()
		{
			return WriteAsync<object> (conn => {
				conn.EnableWriteAheadLogging ();
				return null;
			});
		}

		/// <summary>
		/// Whether to store DateTime properties as ticks (true) or strings (false).
		/// </summary>
		public bool StoreDateTimeAsTicks => GetConnection ().StoreDateTimeAsTicks;

		/// <summary>
		/// Whether to store TimeSpan properties as ticks (true) or strings (false).
		/// </summary>
		public bool StoreTimeSpanAsTicks => GetConnection ().StoreTimeSpanAsTicks;

		/// <summary>
		/// Whether to writer queries to <see cref="Tracer"/> during execution.
		/// </summary>
		/// <value>The tracer.</value>
		public bool Trace {
			get { return GetConnection ().Trace; }
			set { GetConnection ().Trace = value; }
		}

		/// <summary>
		/// The delegate responsible for writing trace lines.
		/// </summary>
		/// <value>The tracer.</value>
		public Action<string> Tracer {
			get { return GetConnection ().Tracer; }
			set { GetConnection ().Tracer = value; }
		}

		/// <summary>
		/// Whether Trace lines should be written that show the execution time of queries.
		/// </summary>
		public bool TimeExecution {
			get { return GetConnection ().TimeExecution; }
			set { GetConnection ().TimeExecution = value; }
		}

		/// <summary>
		/// Returns the mappings from types to tables that the connection
		/// currently understands.
		/// </summary>
		public IEnumerable<TableMapping> TableMappings => GetConnection ().TableMappings;

		/// <summary>
		/// Closes all connections to all async databases.
		/// You should *never* need to do this.
		/// This is a blocking operation that will return when all connections
		/// have been closed.
		/// </summary>
		public static void ResetPool ()
		{
			SQLiteConnectionPool.Shared.Reset ();
		}

		/// <summary>
		/// Gets the pooled lockable connection used by this async connection.
		/// You should never need to use this. This is provided only to add additional
		/// functionality to SQLite-net. If you use this connection, you must use
		/// the Lock method on it while using it.
		/// </summary>
		public SQLiteConnectionWithLock GetConnection ()
		{
			return SQLiteConnectionPool.Shared.GetConnection (_connectionString);
		}

		SQLiteConnectionWithLock GetConnectionAndTransactionLock (out object transactionLock)
		{
			return SQLiteConnectionPool.Shared.GetConnectionAndTransactionLock (_connectionString, out transactionLock);
		}

		/// <summary>
		/// Closes any pooled connections used by the database.
		/// </summary>
		public Task CloseAsync ()
		{
			return Task.Factory.StartNew (() => {
				SQLiteConnectionPool.Shared.CloseConnection (_connectionString);
			}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		Task<T> ReadAsync<T> (Func<SQLiteConnectionWithLock, T> read)
		{
			return Task.Factory.StartNew (() => {
				var conn = GetConnection ();
				using (conn.Lock ()) {
					return read (conn);
				}
			}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		Task<T> WriteAsync<T> (Func<SQLiteConnectionWithLock, T> write)
		{
			return Task.Factory.StartNew (() => {
				var conn = GetConnection ();
				using (conn.Lock ()) {
					return write (conn);
				}
			}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		Task<T> TransactAsync<T> (Func<SQLiteConnectionWithLock, T> transact)
		{
			return Task.Factory.StartNew (() => {
				var conn = GetConnectionAndTransactionLock (out var transactionLock);
				lock (transactionLock) {
					using (conn.Lock ()) {
						return transact (conn);
					}
				}
			}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		/// <summary>
		/// Enable or disable extension loading.
		/// </summary>
		public Task EnableLoadExtensionAsync (bool enabled)
		{
			return WriteAsync<object> (conn => {
				conn.EnableLoadExtension (enabled);
				return null;
			});
		}

		/// <summary>
		/// Executes a "create table if not exists" on the database. It also
		/// creates any specified indexes on the columns of the table. It uses
		/// a schema automatically generated from the specified type. You can
		/// later access this schema by calling GetMapping.
		/// </summary>
		/// <returns>
		/// Whether the table was created or migrated.
		/// </returns>
		public Task<CreateTableResult> CreateTableAsync<T> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
		{
			return WriteAsync (conn => conn.CreateTable<T> (createFlags));
		}

		/// <summary>
		/// Executes a "create table if not exists" on the database. It also
		/// creates any specified indexes on the columns of the table. It uses
		/// a schema automatically generated from the specified type. You can
		/// later access this schema by calling GetMapping.
		/// </summary>
		/// <param name="ty">Type to reflect to a database table.</param>
		/// <param name="createFlags">Optional flags allowing implicit PK and indexes based on naming conventions.</param>  
		/// <returns>
		/// Whether the table was created or migrated.
		/// </returns>
		public Task<CreateTableResult> CreateTableAsync (Type ty, CreateFlags createFlags = CreateFlags.None)
		{
			return WriteAsync (conn => conn.CreateTable (ty, createFlags));
		}

		/// <summary>
		/// Executes a "create table if not exists" on the database for each type. It also
		/// creates any specified indexes on the columns of the table. It uses
		/// a schema automatically generated from the specified type. You can
		/// later access this schema by calling GetMapping.
		/// </summary>
		/// <returns>
		/// Whether the table was created or migrated for each type.
		/// </returns>
		public Task<CreateTablesResult> CreateTablesAsync<T, T2> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
		{
			return CreateTablesAsync (createFlags, typeof (T), typeof (T2));
		}

		/// <summary>
		/// Executes a "create table if not exists" on the database for each type. It also
		/// creates any specified indexes on the columns of the table. It uses
		/// a schema automatically generated from the specified type. You can
		/// later access this schema by calling GetMapping.
		/// </summary>
		/// <returns>
		/// Whether the table was created or migrated for each type.
		/// </returns>
		public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new()
		{
			return CreateTablesAsync (createFlags, typeof (T), typeof (T2), typeof (T3));
		}

		/// <summary>
		/// Executes a "create table if not exists" on the database for each type. It also
		/// creates any specified indexes on the columns of the table. It uses
		/// a schema automatically generated from the specified type. You can
		/// later access this schema by calling GetMapping.
		/// </summary>
		/// <returns>
		/// Whether the table was created or migrated for each type.
		/// </returns>
		public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new()
			where T4 : new()
		{
			return CreateTablesAsync (createFlags, typeof (T), typeof (T2), typeof (T3), typeof (T4));
		}

		/// <summary>
		/// Executes a "create table if not exists" on the database for each type. It also
		/// creates any specified indexes on the columns of the table. It uses
		/// a schema automatically generated from the specified type. You can
		/// later access this schema by calling GetMapping.
		/// </summary>
		/// <returns>
		/// Whether the table was created or migrated for each type.
		/// </returns>
		public Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4, T5> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new()
			where T4 : new()
			where T5 : new()
		{
			return CreateTablesAsync (createFlags, typeof (T), typeof (T2), typeof (T3), typeof (T4), typeof (T5));
		}

		/// <summary>
		/// Executes a "create table if not exists" on the database for each type. It also
		/// creates any specified indexes on the columns of the table. It uses
		/// a schema automatically generated from the specified type. You can
		/// later access this schema by calling GetMapping.
		/// </summary>
		/// <returns>
		/// Whether the table was created or migrated for each type.
		/// </returns>
		public Task<CreateTablesResult> CreateTablesAsync (CreateFlags createFlags = CreateFlags.None, params Type[] types)
		{
			return WriteAsync (conn => conn.CreateTables (createFlags, types));
		}

		/// <summary>
		/// Executes a "drop table" on the database.  This is non-recoverable.
		/// </summary>
		public Task<int> DropTableAsync<T> ()
			where T : new()
		{
			return WriteAsync (conn => conn.DropTable<T> ());
		}

		/// <summary>
		/// Executes a "drop table" on the database.  This is non-recoverable.
		/// </summary>
		/// <param name="map">
		/// The TableMapping used to identify the table.
		/// </param>
		public Task<int> DropTableAsync (TableMapping map)
		{
			return WriteAsync (conn => conn.DropTable (map));
		}

		/// <summary>
		/// Creates an index for the specified table and column.
		/// </summary>
		/// <param name="tableName">Name of the database table</param>
		/// <param name="columnName">Name of the column to index</param>
		/// <param name="unique">Whether the index should be unique</param>
		/// <returns>Zero on success.</returns>
		public Task<int> CreateIndexAsync (string tableName, string columnName, bool unique = false)
		{
			return WriteAsync (conn => conn.CreateIndex (tableName, columnName, unique));
		}

		/// <summary>
		/// Creates an index for the specified table and column.
		/// </summary>
		/// <param name="indexName">Name of the index to create</param>
		/// <param name="tableName">Name of the database table</param>
		/// <param name="columnName">Name of the column to index</param>
		/// <param name="unique">Whether the index should be unique</param>
		/// <returns>Zero on success.</returns>
		public Task<int> CreateIndexAsync (string indexName, string tableName, string columnName, bool unique = false)
		{
			return WriteAsync (conn => conn.CreateIndex (indexName, tableName, columnName, unique));
		}

		/// <summary>
		/// Creates an index for the specified table and columns.
		/// </summary>
		/// <param name="tableName">Name of the database table</param>
		/// <param name="columnNames">An array of column names to index</param>
		/// <param name="unique">Whether the index should be unique</param>
		/// <returns>Zero on success.</returns>
		public Task<int> CreateIndexAsync (string tableName, string[] columnNames, bool unique = false)
		{
			return WriteAsync (conn => conn.CreateIndex (tableName, columnNames, unique));
		}

		/// <summary>
		/// Creates an index for the specified table and columns.
		/// </summary>
		/// <param name="indexName">Name of the index to create</param>
		/// <param name="tableName">Name of the database table</param>
		/// <param name="columnNames">An array of column names to index</param>
		/// <param name="unique">Whether the index should be unique</param>
		/// <returns>Zero on success.</returns>
		public Task<int> CreateIndexAsync (string indexName, string tableName, string[] columnNames, bool unique = false)
		{
			return WriteAsync (conn => conn.CreateIndex (indexName, tableName, columnNames, unique));
		}

		/// <summary>
		/// Creates an index for the specified object property.
		/// e.g. CreateIndex&lt;Client&gt;(c => c.Name);
		/// </summary>
		/// <typeparam name="T">Type to reflect to a database table.</typeparam>
		/// <param name="property">Property to index</param>
		/// <param name="unique">Whether the index should be unique</param>
		/// <returns>Zero on success.</returns>
		public Task<int> CreateIndexAsync<T> (Expression<Func<T, object>> property, bool unique = false)
		{
			return WriteAsync (conn => conn.CreateIndex (property, unique));
		}

		/// <summary>
		/// Inserts the given object and (and updates its
		/// auto incremented primary key if it has one).
		/// </summary>
		/// <param name="obj">
		/// The object to insert.
		/// </param>
		/// <returns>
		/// The number of rows added to the table.
		/// </returns>
		public Task<int> InsertAsync (object obj)
		{
			return WriteAsync (conn => conn.Insert (obj));
		}

		/// <summary>
		/// Inserts the given object (and updates its
		/// auto incremented primary key if it has one).
		/// The return value is the number of rows added to the table.
		/// </summary>
		/// <param name="obj">
		/// The object to insert.
		/// </param>
		/// <param name="objType">
		/// The type of object to insert.
		/// </param>
		/// <returns>
		/// The number of rows added to the table.
		/// </returns>
		public Task<int> InsertAsync (object obj, Type objType)
		{
			return WriteAsync (conn => conn.Insert (obj, objType));
		}

		/// <summary>
		/// Inserts the given object (and updates its
		/// auto incremented primary key if it has one).
		/// The return value is the number of rows added to the table.
		/// </summary>
		/// <param name="obj">
		/// The object to insert.
		/// </param>
		/// <param name="extra">
		/// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
		/// </param>
		/// <returns>
		/// The number of rows added to the table.
		/// </returns>
		public Task<int> InsertAsync (object obj, string extra)
		{
			return WriteAsync (conn => conn.Insert (obj, extra));
		}

		/// <summary>
		/// Inserts the given object (and updates its
		/// auto incremented primary key if it has one).
		/// The return value is the number of rows added to the table.
		/// </summary>
		/// <param name="obj">
		/// The object to insert.
		/// </param>
		/// <param name="extra">
		/// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
		/// </param>
		/// <param name="objType">
		/// The type of object to insert.
		/// </param>
		/// <returns>
		/// The number of rows added to the table.
		/// </returns>
		public Task<int> InsertAsync (object obj, string extra, Type objType)
		{
			return WriteAsync (conn => conn.Insert (obj, extra, objType));
		}

		/// <summary>
		/// Inserts the given object (and updates its
		/// auto incremented primary key if it has one).
		/// The return value is the number of rows added to the table.
		/// If a UNIQUE constraint violation occurs with
		/// some pre-existing object, this function deletes
		/// the old object.
		/// </summary>
		/// <param name="obj">
		/// The object to insert.
		/// </param>
		/// <returns>
		/// The number of rows modified.
		/// </returns>
		public Task<int> InsertOrReplaceAsync (object obj)
		{
			return WriteAsync (conn => conn.InsertOrReplace (obj));
		}

		/// <summary>
		/// Inserts the given object (and updates its
		/// auto incremented primary key if it has one).
		/// The return value is the number of rows added to the table.
		/// If a UNIQUE constraint violation occurs with
		/// some pre-existing object, this function deletes
		/// the old object.
		/// </summary>
		/// <param name="obj">
		/// The object to insert.
		/// </param>
		/// <param name="objType">
		/// The type of object to insert.
		/// </param>
		/// <returns>
		/// The number of rows modified.
		/// </returns>
		public Task<int> InsertOrReplaceAsync (object obj, Type objType)
		{
			return WriteAsync (conn => conn.InsertOrReplace (obj, objType));
		}

		/// <summary>
		/// Updates all of the columns of a table using the specified object
		/// except for its primary key.
		/// The object is required to have a primary key.
		/// </summary>
		/// <param name="obj">
		/// The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
		/// </param>
		/// <returns>
		/// The number of rows updated.
		/// </returns>
		public Task<int> UpdateAsync (object obj)
		{
			return WriteAsync (conn => conn.Update (obj));
		}

		/// <summary>
		/// Updates all of the columns of a table using the specified object
		/// except for its primary key.
		/// The object is required to have a primary key.
		/// </summary>
		/// <param name="obj">
		/// The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
		/// </param>
		/// <param name="objType">
		/// The type of object to insert.
		/// </param>
		/// <returns>
		/// The number of rows updated.
		/// </returns>
		public Task<int> UpdateAsync (object obj, Type objType)
		{
			return WriteAsync (conn => conn.Update (obj, objType));
		}

		/// <summary>
		/// Updates all specified objects.
		/// </summary>
		/// <param name="objects">
		/// An <see cref="IEnumerable"/> of the objects to insert.
		/// </param>
		/// <param name="runInTransaction">
		/// A boolean indicating if the inserts should be wrapped in a transaction
		/// </param>
		/// <returns>
		/// The number of rows modified.
		/// </returns>
		public Task<int> UpdateAllAsync (IEnumerable objects, bool runInTransaction = true)
		{
			return WriteAsync (conn => conn.UpdateAll (objects, runInTransaction));
		}

		/// <summary>
		/// Deletes the given object from the database using its primary key.
		/// </summary>
		/// <param name="objectToDelete">
		/// The object to delete. It must have a primary key designated using the PrimaryKeyAttribute.
		/// </param>
		/// <returns>
		/// The number of rows deleted.
		/// </returns>
		public Task<int> DeleteAsync (object objectToDelete)
		{
			return WriteAsync (conn => conn.Delete (objectToDelete));
		}

		/// <summary>
		/// Deletes the object with the specified primary key.
		/// </summary>
		/// <param name="primaryKey">
		/// The primary key of the object to delete.
		/// </param>
		/// <returns>
		/// The number of objects deleted.
		/// </returns>
		/// <typeparam name='T'>
		/// The type of object.
		/// </typeparam>
		public Task<int> DeleteAsync<T> (object primaryKey)
		{
			return WriteAsync (conn => conn.Delete<T> (primaryKey));
		}

		/// <summary>
		/// Deletes the object with the specified primary key.
		/// </summary>
		/// <param name="primaryKey">
		/// The primary key of the object to delete.
		/// </param>
		/// <param name="map">
		/// The TableMapping used to identify the table.
		/// </param>
		/// <returns>
		/// The number of objects deleted.
		/// </returns>
		public Task<int> DeleteAsync (object primaryKey, TableMapping map)
		{
			return WriteAsync (conn => conn.Delete (primaryKey, map));
		}

		/// <summary>
		/// Deletes all the objects from the specified table.
		/// WARNING WARNING: Let me repeat. It deletes ALL the objects from the
		/// specified table. Do you really want to do that?
		/// </summary>
		/// <returns>
		/// The number of objects deleted.
		/// </returns>
		/// <typeparam name='T'>
		/// The type of objects to delete.
		/// </typeparam>
		public Task<int> DeleteAllAsync<T> ()
		{
			return WriteAsync (conn => conn.DeleteAll<T> ());
		}

		/// <summary>
		/// Deletes all the objects from the specified table.
		/// WARNING WARNING: Let me repeat. It deletes ALL the objects from the
		/// specified table. Do you really want to do that?
		/// </summary>
		/// <param name="map">
		/// The TableMapping used to identify the table.
		/// </param>
		/// <returns>
		/// The number of objects deleted.
		/// </returns>
		public Task<int> DeleteAllAsync (TableMapping map)
		{
			return WriteAsync (conn => conn.DeleteAll (map));
		}

		/// <summary>
		/// Backup the entire database to the specified path.
		/// </summary>
		/// <param name="destinationDatabasePath">Path to backup file.</param>
		/// <param name="databaseName">The name of the database to backup (usually "main").</param>
		public Task BackupAsync (string destinationDatabasePath, string databaseName = "main")
		{
			return WriteAsync (conn => {
				conn.Backup (destinationDatabasePath, databaseName);
				return 0;
			});
		}

		/// <summary>
		/// Attempts to retrieve an object with the given primary key from the table
		/// associated with the specified type. Use of this method requires that
		/// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
		/// </summary>
		/// <param name="pk">
		/// The primary key.
		/// </param>
		/// <returns>
		/// The object with the given primary key. Throws a not found exception
		/// if the object is not found.
		/// </returns>
		public Task<T> GetAsync<T> (object pk)
			where T : new()
		{
			return ReadAsync (conn => conn.Get<T> (pk));
		}

		/// <summary>
		/// Attempts to retrieve an object with the given primary key from the table
		/// associated with the specified type. Use of this method requires that
		/// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
		/// </summary>
		/// <param name="pk">
		/// The primary key.
		/// </param>
		/// <param name="map">
		/// The TableMapping used to identify the table.
		/// </param>
		/// <returns>
		/// The object with the given primary key. Throws a not found exception
		/// if the object is not found.
		/// </returns>
		public Task<object> GetAsync (object pk, TableMapping map)
		{
			return ReadAsync (conn => conn.Get (pk, map));
		}

		/// <summary>
		/// Attempts to retrieve the first object that matches the predicate from the table
		/// associated with the specified type. 
		/// </summary>
		/// <param name="predicate">
		/// A predicate for which object to find.
		/// </param>
		/// <returns>
		/// The object that matches the given predicate. Throws a not found exception
		/// if the object is not found.
		/// </returns>
		public Task<T> GetAsync<T> (Expression<Func<T, bool>> predicate)
			where T : new()
		{
			return ReadAsync (conn => conn.Get<T> (predicate));
		}

		/// <summary>
		/// Attempts to retrieve an object with the given primary key from the table
		/// associated with the specified type. Use of this method requires that
		/// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
		/// </summary>
		/// <param name="pk">
		/// The primary key.
		/// </param>
		/// <returns>
		/// The object with the given primary key or null
		/// if the object is not found.
		/// </returns>
		public Task<T> FindAsync<T> (object pk)
			where T : new()
		{
			return ReadAsync (conn => conn.Find<T> (pk));
		}

		/// <summary>
		/// Attempts to retrieve an object with the given primary key from the table
		/// associated with the specified type. Use of this method requires that
		/// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
		/// </summary>
		/// <param name="pk">
		/// The primary key.
		/// </param>
		/// <param name="map">
		/// The TableMapping used to identify the table.
		/// </param>
		/// <returns>
		/// The object with the given primary key or null
		/// if the object is not found.
		/// </returns>
		public Task<object> FindAsync (object pk, TableMapping map)
		{
			return ReadAsync (conn => conn.Find (pk, map));
		}

		/// <summary>
		/// Attempts to retrieve the first object that matches the predicate from the table
		/// associated with the specified type. 
		/// </summary>
		/// <param name="predicate">
		/// A predicate for which object to find.
		/// </param>
		/// <returns>
		/// The object that matches the given predicate or null
		/// if the object is not found.
		/// </returns>
		public Task<T> FindAsync<T> (Expression<Func<T, bool>> predicate)
			where T : new()
		{
			return ReadAsync (conn => conn.Find<T> (predicate));
		}

		/// <summary>
		/// Attempts to retrieve the first object that matches the query from the table
		/// associated with the specified type. 
		/// </summary>
		/// <param name="query">
		/// The fully escaped SQL.
		/// </param>
		/// <param name="args">
		/// Arguments to substitute for the occurences of '?' in the query.
		/// </param>
		/// <returns>
		/// The object that matches the given predicate or null
		/// if the object is not found.
		/// </returns>
		public Task<T> FindWithQueryAsync<T> (string query, params object[] args)
			where T : new()
		{
			return ReadAsync (conn => conn.FindWithQuery<T> (query, args));
		}

		/// <summary>
		/// Attempts to retrieve the first object that matches the query from the table
		/// associated with the specified type. 
		/// </summary>
		/// <param name="map">
		/// The TableMapping used to identify the table.
		/// </param>
		/// <param name="query">
		/// The fully escaped SQL.
		/// </param>
		/// <param name="args">
		/// Arguments to substitute for the occurences of '?' in the query.
		/// </param>
		/// <returns>
		/// The object that matches the given predicate or null
		/// if the object is not found.
		/// </returns>
		public Task<object> FindWithQueryAsync (TableMapping map, string query, params object[] args)
		{
			return ReadAsync (conn => conn.FindWithQuery (map, query, args));
		}

		/// <summary>
		/// Retrieves the mapping that is automatically generated for the given type.
		/// </summary>
		/// <param name="type">
		/// The type whose mapping to the database is returned.
		/// </param>         
		/// <param name="createFlags">
		/// Optional flags allowing implicit PK and indexes based on naming conventions
		/// </param>     
		/// <returns>
		/// The mapping represents the schema of the columns of the database and contains 
		/// methods to set and get properties of objects.
		/// </returns>
		public Task<TableMapping> GetMappingAsync (Type type, CreateFlags createFlags = CreateFlags.None)
		{
			return ReadAsync (conn => conn.GetMapping (type, createFlags));
		}

		/// <summary>
		/// Retrieves the mapping that is automatically generated for the given type.
		/// </summary>
		/// <param name="createFlags">
		/// Optional flags allowing implicit PK and indexes based on naming conventions
		/// </param>     
		/// <returns>
		/// The mapping represents the schema of the columns of the database and contains 
		/// methods to set and get properties of objects.
		/// </returns>
		public Task<TableMapping> GetMappingAsync<T> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
		{
			return ReadAsync (conn => conn.GetMapping<T> (createFlags));
		}

		/// <summary>
		/// Query the built-in sqlite table_info table for a specific tables columns.
		/// </summary>
		/// <returns>The columns contains in the table.</returns>
		/// <param name="tableName">Table name.</param>
		public Task<List<SQLiteConnection.ColumnInfo>> GetTableInfoAsync (string tableName)
		{
			return ReadAsync (conn => conn.GetTableInfo (tableName));
		}

		/// <summary>
		/// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
		/// in the command text for each of the arguments and then executes that command.
		/// Use this method instead of Query when you don't expect rows back. Such cases include
		/// INSERTs, UPDATEs, and DELETEs.
		/// You can set the Trace or TimeExecution properties of the connection
		/// to profile execution.
		/// </summary>
		/// <param name="query">
		/// The fully escaped SQL.
		/// </param>
		/// <param name="args">
		/// Arguments to substitute for the occurences of '?' in the query.
		/// </param>
		/// <returns>
		/// The number of rows modified in the database as a result of this execution.
		/// </returns>
		public Task<int> ExecuteAsync (string query, params object[] args)
		{
			return WriteAsync (conn => conn.Execute (query, args));
		}

		/// <summary>
		/// Inserts all specified objects.
		/// </summary>
		/// <param name="objects">
		/// An <see cref="IEnumerable"/> of the objects to insert.
		/// <param name="runInTransaction"/>
		/// A boolean indicating if the inserts should be wrapped in a transaction.
		/// </param>
		/// <returns>
		/// The number of rows added to the table.
		/// </returns>
		public Task<int> InsertAllAsync (IEnumerable objects, bool runInTransaction = true)
		{
			return WriteAsync (conn => conn.InsertAll (objects, runInTransaction));
		}

		/// <summary>
		/// Inserts all specified objects.
		/// </summary>
		/// <param name="objects">
		/// An <see cref="IEnumerable"/> of the objects to insert.
		/// </param>
		/// <param name="extra">
		/// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
		/// </param>
		/// <param name="runInTransaction">
		/// A boolean indicating if the inserts should be wrapped in a transaction.
		/// </param>
		/// <returns>
		/// The number of rows added to the table.
		/// </returns>
		public Task<int> InsertAllAsync (IEnumerable objects, string extra, bool runInTransaction = true)
		{
			return WriteAsync (conn => conn.InsertAll (objects, extra, runInTransaction));
		}

		/// <summary>
		/// Inserts all specified objects.
		/// </summary>
		/// <param name="objects">
		/// An <see cref="IEnumerable"/> of the objects to insert.
		/// </param>
		/// <param name="objType">
		/// The type of object to insert.
		/// </param>
		/// <param name="runInTransaction">
		/// A boolean indicating if the inserts should be wrapped in a transaction.
		/// </param>
		/// <returns>
		/// The number of rows added to the table.
		/// </returns>
		public Task<int> InsertAllAsync (IEnumerable objects, Type objType, bool runInTransaction = true)
		{
			return WriteAsync (conn => conn.InsertAll (objects, objType, runInTransaction));
		}

		/// <summary>
		/// Executes <paramref name="action"/> within a (possibly nested) transaction by wrapping it in a SAVEPOINT. If an
		/// exception occurs the whole transaction is rolled back, not just the current savepoint. The exception
		/// is rethrown.
		/// </summary>
		/// <param name="action">
		/// The <see cref="Action"/> to perform within a transaction. <paramref name="action"/> can contain any number
		/// of operations on the connection but should never call <see cref="SQLiteConnection.Commit"/> or
		/// <see cref="SQLiteConnection.Commit"/>.
		/// </param>
		public Task RunInTransactionAsync (Action<SQLiteConnection> action)
		{
			return TransactAsync<object> (conn => {
				conn.BeginTransaction ();
				try {
					action (conn);
					conn.Commit ();
					return null;
				}
				catch (Exception) {
					conn.Rollback ();
					throw;
				}
			});
		}

		/// <summary>
		/// Returns a queryable interface to the table represented by the given type.
		/// </summary>
		/// <returns>
		/// A queryable object that is able to translate Where, OrderBy, and Take
		/// queries into native SQL.
		/// </returns>
		public AsyncTableQuery<T> Table<T> ()
			where T : new()
		{
			//
			// This isn't async as the underlying connection doesn't go out to the database
			// until the query is performed. The Async methods are on the query iteself.
			//
			var conn = GetConnection ();
			return new AsyncTableQuery<T> (conn.Table<T> ());
		}

		/// <summary>
		/// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
		/// in the command text for each of the arguments and then executes that command.
		/// Use this method when return primitive values.
		/// You can set the Trace or TimeExecution properties of the connection
		/// to profile execution.
		/// </summary>
		/// <param name="query">
		/// The fully escaped SQL.
		/// </param>
		/// <param name="args">
		/// Arguments to substitute for the occurences of '?' in the query.
		/// </param>
		/// <returns>
		/// The number of rows modified in the database as a result of this execution.
		/// </returns>
		public Task<T> ExecuteScalarAsync<T> (string query, params object[] args)
		{
			return WriteAsync (conn => {
				var command = conn.CreateCommand (query, args);
				return command.ExecuteScalar<T> ();
			});
		}

		/// <summary>
		/// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
		/// in the command text for each of the arguments and then executes that command.
		/// It returns each row of the result using the mapping automatically generated for
		/// the given type.
		/// </summary>
		/// <param name="query">
		/// The fully escaped SQL.
		/// </param>
		/// <param name="args">
		/// Arguments to substitute for the occurences of '?' in the query.
		/// </param>
		/// <returns>
		/// A list with one result for each row returned by the query.
		/// </returns>
		public Task<List<T>> QueryAsync<T> (string query, params object[] args)
			where T : new()
		{
			return ReadAsync (conn => conn.Query<T> (query, args));
		}

		/// <summary>
		/// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
		/// in the command text for each of the arguments and then executes that command.
		/// It returns the first column of each row of the result.
		/// </summary>
		/// <param name="query">
		/// The fully escaped SQL.
		/// </param>
		/// <param name="args">
		/// Arguments to substitute for the occurences of '?' in the query.
		/// </param>
		/// <returns>
		/// A list with one result for the first column of each row returned by the query.
		/// </returns>
		public Task<List<T>> QueryScalarsAsync<T> (string query, params object[] args)
		{
			return ReadAsync (conn => conn.QueryScalars<T> (query, args));
		}

		/// <summary>
		/// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
		/// in the command text for each of the arguments and then executes that command.
		/// It returns each row of the result using the specified mapping. This function is
		/// only used by libraries in order to query the database via introspection. It is
		/// normally not used.
		/// </summary>
		/// <param name="map">
		/// A <see cref="TableMapping"/> to use to convert the resulting rows
		/// into objects.
		/// </param>
		/// <param name="query">
		/// The fully escaped SQL.
		/// </param>
		/// <param name="args">
		/// Arguments to substitute for the occurences of '?' in the query.
		/// </param>
		/// <returns>
		/// An enumerable with one result for each row returned by the query.
		/// </returns>
		public Task<List<object>> QueryAsync (TableMapping map, string query, params object[] args)
		{
			return ReadAsync (conn => conn.Query (map, query, args));
		}

		/// <summary>
		/// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
		/// in the command text for each of the arguments and then executes that command.
		/// It returns each row of the result using the mapping automatically generated for
		/// the given type.
		/// </summary>
		/// <param name="query">
		/// The fully escaped SQL.
		/// </param>
		/// <param name="args">
		/// Arguments to substitute for the occurences of '?' in the query.
		/// </param>
		/// <returns>
		/// An enumerable with one result for each row returned by the query.
		/// The enumerator will call sqlite3_step on each call to MoveNext, so the database
		/// connection must remain open for the lifetime of the enumerator.
		/// </returns>
		public Task<IEnumerable<T>> DeferredQueryAsync<T> (string query, params object[] args)
			where T : new()
		{
			return ReadAsync (conn => (IEnumerable<T>)conn.DeferredQuery<T> (query, args).ToList ());
		}

		/// <summary>
		/// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
		/// in the command text for each of the arguments and then executes that command.
		/// It returns each row of the result using the specified mapping. This function is
		/// only used by libraries in order to query the database via introspection. It is
		/// normally not used.
		/// </summary>
		/// <param name="map">
		/// A <see cref="TableMapping"/> to use to convert the resulting rows
		/// into objects.
		/// </param>
		/// <param name="query">
		/// The fully escaped SQL.
		/// </param>
		/// <param name="args">
		/// Arguments to substitute for the occurences of '?' in the query.
		/// </param>
		/// <returns>
		/// An enumerable with one result for each row returned by the query.
		/// The enumerator will call sqlite3_step on each call to MoveNext, so the database
		/// connection must remain open for the lifetime of the enumerator.
		/// </returns>
		public Task<IEnumerable<object>> DeferredQueryAsync (TableMapping map, string query, params object[] args)
		{
			return ReadAsync (conn => (IEnumerable<object>)conn.DeferredQuery (map, query, args).ToList ());
		}

		/// <summary>
		/// Change the encryption key for a SQLCipher database with "pragma rekey = ...".
		/// </summary>
		/// <param name="key">Encryption key plain text that is converted to the real encryption key using PBKDF2 key derivation</param>
		public Task ReKeyAsync (string key)
		{
			return WriteAsync<object> (conn => {
				conn.ReKey (key);
				return null;
			});
		}

		/// <summary>
		/// Change the encryption key for a SQLCipher database.
		/// </summary>
		/// <param name="key">256-bit (32 byte) or 384-bit (48 bytes) encryption key data</param>
		public Task ReKeyAsync (byte[] key)
		{
			return WriteAsync<object> (conn => {
				conn.ReKey (key);
				return null;
			});			
		}
	}

	/// <summary>
	/// Query to an asynchronous database connection.
	/// </summary>
	public class AsyncTableQuery<T>
		where T : new()
	{
		TableQuery<T> _innerQuery;

		/// <summary>
		/// Creates a new async query that uses given the synchronous query.
		/// </summary>
		public AsyncTableQuery (TableQuery<T> innerQuery)
		{
			_innerQuery = innerQuery;
		}

		Task<U> ReadAsync<U> (Func<SQLiteConnectionWithLock, U> read)
		{
			return Task.Factory.StartNew (() => {
				var conn = (SQLiteConnectionWithLock)_innerQuery.Connection;
				using (conn.Lock ()) {
					return read (conn);
				}
			}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		Task<U> WriteAsync<U> (Func<SQLiteConnectionWithLock, U> write)
		{
			return Task.Factory.StartNew (() => {
				var conn = (SQLiteConnectionWithLock)_innerQuery.Connection;
				using (conn.Lock ()) {
					return write (conn);
				}
			}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		/// <summary>
		/// Filters the query based on a predicate.
		/// </summary>
		public AsyncTableQuery<T> Where (Expression<Func<T, bool>> predExpr)
		{
			return new AsyncTableQuery<T> (_innerQuery.Where (predExpr));
		}

		/// <summary>
		/// Skips a given number of elements from the query and then yields the remainder.
		/// </summary>
		public AsyncTableQuery<T> Skip (int n)
		{
			return new AsyncTableQuery<T> (_innerQuery.Skip (n));
		}

		/// <summary>
		/// Yields a given number of elements from the query and then skips the remainder.
		/// </summary>
		public AsyncTableQuery<T> Take (int n)
		{
			return new AsyncTableQuery<T> (_innerQuery.Take (n));
		}

		/// <summary>
		/// Order the query results according to a key.
		/// </summary>
		public AsyncTableQuery<T> OrderBy<U> (Expression<Func<T, U>> orderExpr)
		{
			return new AsyncTableQuery<T> (_innerQuery.OrderBy<U> (orderExpr));
		}

		/// <summary>
		/// Order the query results according to a key.
		/// </summary>
		public AsyncTableQuery<T> OrderByDescending<U> (Expression<Func<T, U>> orderExpr)
		{
			return new AsyncTableQuery<T> (_innerQuery.OrderByDescending<U> (orderExpr));
		}

		/// <summary>
		/// Order the query results according to a key.
		/// </summary>
		public AsyncTableQuery<T> ThenBy<U> (Expression<Func<T, U>> orderExpr)
		{
			return new AsyncTableQuery<T> (_innerQuery.ThenBy<U> (orderExpr));
		}

		/// <summary>
		/// Order the query results according to a key.
		/// </summary>
		public AsyncTableQuery<T> ThenByDescending<U> (Expression<Func<T, U>> orderExpr)
		{
			return new AsyncTableQuery<T> (_innerQuery.ThenByDescending<U> (orderExpr));
		}

		/// <summary>
		/// Queries the database and returns the results as a List.
		/// </summary>
		public Task<List<T>> ToListAsync ()
		{
			return ReadAsync (conn => _innerQuery.ToList ());
		}

		/// <summary>
		/// Queries the database and returns the results as an array.
		/// </summary>
		public Task<T[]> ToArrayAsync ()
		{
			return ReadAsync (conn => _innerQuery.ToArray ());
		}

		/// <summary>
		/// Execute SELECT COUNT(*) on the query
		/// </summary>
		public Task<int> CountAsync ()
		{
			return ReadAsync (conn => _innerQuery.Count ());
		}

		/// <summary>
		/// Execute SELECT COUNT(*) on the query with an additional WHERE clause.
		/// </summary>
		public Task<int> CountAsync (Expression<Func<T, bool>> predExpr)
		{
			return ReadAsync (conn => _innerQuery.Count (predExpr));
		}

		/// <summary>
		/// Returns the element at a given index
		/// </summary>
		public Task<T> ElementAtAsync (int index)
		{
			return ReadAsync (conn => _innerQuery.ElementAt (index));
		}

		/// <summary>
		/// Returns the first element of this query.
		/// </summary>
		public Task<T> FirstAsync ()
		{
			return ReadAsync (conn => _innerQuery.First ());
		}

		/// <summary>
		/// Returns the first element of this query, or null if no element is found.
		/// </summary>
		public Task<T> FirstOrDefaultAsync ()
		{
			return ReadAsync (conn => _innerQuery.FirstOrDefault ());
		}

		/// <summary>
		/// Returns the first element of this query that matches the predicate.
		/// </summary>
		public Task<T> FirstAsync (Expression<Func<T, bool>> predExpr)
		{
			return ReadAsync (conn => _innerQuery.First (predExpr));
		}

		/// <summary>
		/// Returns the first element of this query that matches the predicate.
		/// </summary>
		public Task<T> FirstOrDefaultAsync (Expression<Func<T, bool>> predExpr)
		{
			return ReadAsync (conn => _innerQuery.FirstOrDefault (predExpr));
		}

		/// <summary>
		/// Delete all the rows that match this query and the given predicate.
		/// </summary>
		public Task<int> DeleteAsync (Expression<Func<T, bool>> predExpr)
		{
			return WriteAsync (conn => _innerQuery.Delete (predExpr));
		}

		/// <summary>
		/// Delete all the rows that match this query.
		/// </summary>
		public Task<int> DeleteAsync ()
		{
			return WriteAsync (conn => _innerQuery.Delete ());
		}
	}

	class SQLiteConnectionPool
	{
		class Entry
		{
			public SQLiteConnectionWithLock Connection { get; private set; }

			public SQLiteConnectionString ConnectionString { get; }

			public object TransactionLock { get; } = new object ();

			public Entry (SQLiteConnectionString connectionString)
			{
				ConnectionString = connectionString;
				Connection = new SQLiteConnectionWithLock (ConnectionString);

				// If the database is FullMutex, then we don't need to bother locking
				if (ConnectionString.OpenFlags.HasFlag (SQLiteOpenFlags.FullMutex)) {
					Connection.SkipLock = true;
				}
			}

			public void Close ()
			{
				var wc = Connection;
				Connection = null;
				if (wc != null) {
					wc.Close ();
				}
			}
		}

		readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry> ();
		readonly object _entriesLock = new object ();

		static readonly SQLiteConnectionPool _shared = new SQLiteConnectionPool ();

		/// <summary>
		/// Gets the singleton instance of the connection tool.
		/// </summary>
		public static SQLiteConnectionPool Shared {
			get {
				return _shared;
			}
		}

		public SQLiteConnectionWithLock GetConnection (SQLiteConnectionString connectionString)
		{
			return GetConnectionAndTransactionLock (connectionString, out var _);
		}

		public SQLiteConnectionWithLock GetConnectionAndTransactionLock (SQLiteConnectionString connectionString, out object transactionLock)
		{
			var key = connectionString.UniqueKey;
			Entry entry;
			lock (_entriesLock) {
				if (!_entries.TryGetValue (key, out entry)) {
					// The opens the database while we're locked
					// This is to ensure another thread doesn't get an unopened database
					entry = new Entry (connectionString);
					_entries[key] = entry;
				}
				transactionLock = entry.TransactionLock;
				return entry.Connection;
			}
		}

		public void CloseConnection (SQLiteConnectionString connectionString)
		{
			var key = connectionString.UniqueKey;
			Entry entry;
			lock (_entriesLock) {
				if (_entries.TryGetValue (key, out entry)) {
					_entries.Remove (key);
				}
			}
			entry?.Close ();
		}

		/// <summary>
		/// Closes all connections managed by this pool.
		/// </summary>
		public void Reset ()
		{
			List<Entry> entries;
			lock (_entriesLock) {
				entries = new List<Entry> (_entries.Values);
				_entries.Clear ();
			}

			foreach (var e in entries) {
				e.Close ();
			}
		}
	}

	/// <summary>
	/// This is a normal connection except it contains a Lock method that
	/// can be used to serialize access to the database
	/// in lieu of using the sqlite's FullMutex support.
	/// </summary>
	public class SQLiteConnectionWithLock : SQLiteConnection
	{
		readonly object _lockPoint = new object ();

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SQLite.SQLiteConnectionWithLock"/> class.
		/// </summary>
		/// <param name="connectionString">Connection string containing the DatabasePath.</param>
		public SQLiteConnectionWithLock (SQLiteConnectionString connectionString)
			: base (connectionString)
		{
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:SQLite.SQLiteConnectionWithLock"/> skip lock.
		/// </summary>
		/// <value><c>true</c> if skip lock; otherwise, <c>false</c>.</value>
		public bool SkipLock { get; set; }

		/// <summary>
		/// Lock the database to serialize access to it. To unlock it, call Dispose
		/// on the returned object.
		/// </summary>
		/// <returns>The lock.</returns>
		public IDisposable Lock ()
		{
			return SkipLock ? (IDisposable)new FakeLockWrapper() : new LockWrapper (_lockPoint);
		}

		class LockWrapper : IDisposable
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
		class FakeLockWrapper : IDisposable
		{
			public void Dispose ()
			{
			}
		}
	}
}

