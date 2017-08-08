//
// Copyright (c) 2009-2017 Krueger Systems, Inc.
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
#if WINDOWS_PHONE && !USE_WP8_NATIVE_SQLITE
#define USE_CSHARP_SQLITE
#endif

using System;
using System.Collections;
using System.Diagnostics;
#if !USE_SQLITEPCL_RAW
using System.Runtime.InteropServices;
#endif
using System.Collections.Generic;
#if NO_CONCURRENT
using ConcurrentStringDictionary = System.Collections.Generic.Dictionary<string, object>;
using ConcurrentCommandDictionary = System.Collections.Generic.Dictionary<string, SQLite.SQLiteCommand>;
using ConcurrentTableDictionary = System.Collections.Generic.Dictionary<System.Type, SQLite.TableMapping>;
using SQLite.Extensions;
#else
using ConcurrentStringDictionary = System.Collections.Concurrent.ConcurrentDictionary<string, object>;
using ConcurrentCommandDictionary = System.Collections.Concurrent.ConcurrentDictionary<string, SQLite.SQLiteCommand>;
using ConcurrentTableDictionary = System.Collections.Concurrent.ConcurrentDictionary<System.Type, SQLite.TableMapping>;
#endif
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

#if USE_CSHARP_SQLITE
using Sqlite3 = Community.CsharpSqlite.Sqlite3;
using Sqlite3DatabaseHandle = Community.CsharpSqlite.Sqlite3.sqlite3;
using Sqlite3Statement = Community.CsharpSqlite.Sqlite3.Vdbe;
#elif USE_WP8_NATIVE_SQLITE
using Sqlite3 = Sqlite.Sqlite3;
using Sqlite3DatabaseHandle = Sqlite.Database;
using Sqlite3Statement = Sqlite.Statement;
#elif USE_SQLITEPCL_RAW
using Sqlite3DatabaseHandle = SQLitePCL.sqlite3;
using Sqlite3Statement = SQLitePCL.sqlite3_stmt;
using Sqlite3 = SQLitePCL.raw;
#else
using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3Statement = System.IntPtr;
#endif

#pragma warning disable 1591 // XML Doc Comments
// ReSharper disable All

namespace SQLite
{
	public class SQLiteException : Exception 
	{
		public SQLite3.Result Result { get; }
		public string SQLQuery { get; }

		protected SQLiteException(SQLite3.Result r, string message, string sql = null) : base(message)
		{
			Result = r;
			SQLQuery = sql;
		}

		public static SQLiteException New(SQLite3.Result r, string message = null, string sql = null)
		{
			if (message == null) {
				var x = (int) r;
				message = r.ToString();

				if (x > (int) SQLite3.Result.Done)
					message = ((SQLite3.ExtendedResult) r).ToString();
			}

			return new SQLiteException(r, message, sql);
		}

		public override string ToString()
		{
			if (String.IsNullOrWhiteSpace(SQLQuery) == false)
				return $"--------------------------\n{SQLQuery}\n--------------------------\n{base.ToString()}";

			return base.ToString();
		}
	}

	public class NotNullConstraintViolationException : SQLiteException
	{
		public IEnumerable<TableMapping.Column> Columns { get; protected set; }

		protected NotNullConstraintViolationException (SQLite3.Result r, string message)
			: this (r, message, null, null)
		{

		}

		protected NotNullConstraintViolationException (SQLite3.Result r, string message, TableMapping mapping, object obj)
			: base (r, message)
		{
			if (mapping != null && obj != null) {
				this.Columns = from c in mapping.Columns
							   where c.IsNullable == false && c.GetValue (obj) == null
							   select c;
			}
		}

		public static NotNullConstraintViolationException New (SQLite3.Result r, string message)
		{
			return new NotNullConstraintViolationException (r, message);
		}

		public static NotNullConstraintViolationException New (SQLite3.Result r, string message, TableMapping mapping, object obj)
		{
			return new NotNullConstraintViolationException (r, message, mapping, obj);
		}

		public static NotNullConstraintViolationException New (SQLiteException exception, TableMapping mapping, object obj)
		{
			return new NotNullConstraintViolationException (exception.Result, exception.Message, mapping, obj);
		}
	}

	[Flags]
	public enum SQLiteOpenFlags {
		ReadOnly = 0x1,
		ReadWrite = 0x2,
		Create = 0x4,
		OpenUri = 0x40,
		OpenMemory = 0x80,
		NoMutex = 0x8000,
		FullMutex = 0x10000,
		SharedCache = 0x20000,
		PrivateCache = 0x40000,
		Wal = 0x80000,
		ProtectionComplete = 0x00100000,
		ProtectionCompleteUnlessOpen = 0x00200000,
		ProtectionCompleteUntilFirstUserAuthentication = 0x00300000,
		ProtectionNone = 0x00400000
	}

	[Flags]
	public enum CreateFlags
	{
		/// <summary>
		/// Use the default creation options
		/// </summary>
		None = 0x000,
		/// <summary>
		/// Create a primary key index for a property called 'Id' (case-insensitive).
		/// This avoids the need for the [PrimaryKey] attribute.
		/// </summary>
		ImplicitPK = 0x001,
		/// <summary>
		/// Create indices for properties ending in 'Id' (case-insensitive).
		/// </summary>
		ImplicitIndex = 0x002,
		/// <summary>
		/// Create a primary key for a property called 'Id' and
		/// create an indices for properties ending in 'Id' (case-insensitive).
		/// </summary>
		AllImplicit = 0x003,
		/// <summary>
		/// Force the primary key property to be auto incrementing.
		/// This avoids the need for the [AutoIncrement] attribute.
		/// The primary key property on the class should have type int or long.
		/// </summary>
		AutoIncPK = 0x004,
		/// <summary>
		/// Create virtual table using FTS3
		/// </summary>
		FullTextSearch3 = 0x100,
		/// <summary>
		/// Create virtual table using FTS4
		/// </summary>
		FullTextSearch4 = 0x200
	}

	/// <summary>
	/// An open connection to a SQLite database.
	/// </summary>
	public partial class SQLiteConnection : IDisposable
	{
		private bool _open;
		private TimeSpan _busyTimeout;
		private ConcurrentCommandDictionary _cachedPreparedCommands;

		private int _transactionDepth = 0;
		private Random _rand = new Random ();

		public Sqlite3DatabaseHandle Handle { get; private set; }
		public static readonly Sqlite3DatabaseHandle NullHandle = default(Sqlite3DatabaseHandle);

		/// <summary>
		/// Gets the database path used by this connection.
		/// </summary>
		public string DatabasePath { get; private set; }

        /// <summary>
        /// Gets the SQLite library version number. 3007014 would be v3.7.14
        /// </summary>
        [Obsolete("Use LibraryVersion instead.")]
        public int LibVersionNumber => LibraryVersion.ToInt32();

	    /// <summary>
	    /// Whether Trace lines should be written that show the execution time of queries.
	    /// </summary>
	    [Obsolete("Use TraceTime instead.")]
	    public bool TimeExecution
	    {
	        get { return TraceTime; }
	        set { TraceTime = value; }
	    }

		/// <summary>
		/// Whether to writer queries to <see cref="Tracer"/> during execution.
		/// </summary>
		/// <value>The tracer.</value>
		public bool Trace { get; set; }

	    /// <summary>
	    /// Whether Trace lines should be written that show the execution time of queries.
	    /// </summary>
        public bool TraceTime { get; set; } = true;

        /// <summary>
        /// Write a log message when the time exceeds a defined threshold.
        /// </summary>
	    public TimeSpan TraceTimeExceeding { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// The delegate responsible for writing trace lines.
        /// </summary>
        /// <value>The tracer.</value>
        public Action<string> Tracer { get; set; }
		
		/// <summary>
		/// Whether to store DateTime properties as ticks (true) or strings (false).
		/// </summary>
		public bool StoreDateTimeAsTicks { get; private set; }

		/// <summary>
		/// Gets the SQLite library version number. 3007014 would be v3.7.14
		/// </summary>
	    public SQLiteVersion LibraryVersion { get; } = new SQLiteVersion(SQLite3.LibVersionNumber());
		
#if USE_SQLITEPCL_RAW && !NO_SQLITEPCL_RAW_BATTERIES
		static SQLiteConnection ()
		{
			SQLitePCL.Batteries_V2.Init ();
		}
#endif

		/// <summary>
		/// Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
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
		public SQLiteConnection (string databasePath, bool storeDateTimeAsTicks = true)
			: this (databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, storeDateTimeAsTicks)
		{
		}

		/// <summary>
		/// Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
		/// </summary>
		/// <param name="databasePath">
		/// Specifies the path to the database file.
		/// </param>
		/// <param name="openFlags">
		/// Flags controlling how the connection should be opened.
		/// </param>
		/// <param name="storeDateTimeAsTicks">
		/// Specifies whether to store DateTime properties as ticks (true) or strings (false). You
		/// absolutely do want to store them as Ticks in all new projects. The value of false is
		/// only here for backwards compatibility. There is a *significant* speed advantage, with no
		/// down sides, when setting storeDateTimeAsTicks = true.
		/// If you use DateTimeOffset properties, it will be always stored as ticks regardingless
		/// the storeDateTimeAsTicks parameter.
		/// </param>
		/// <exception cref="ArgumentException"></exception>
		public SQLiteConnection (string databasePath, SQLiteOpenFlags openFlags, bool storeDateTimeAsTicks = true)
		{
			if (string.IsNullOrEmpty (databasePath))
				throw new ArgumentException("Must be specified", nameof(databasePath));

			DatabasePath = databasePath;

#if NETFX_CORE
			SQLite3.SetDirectory(/*temp directory type*/2, Windows.Storage.ApplicationData.Current.TemporaryFolder.Path);
#endif

			Sqlite3DatabaseHandle handle;

#if SILVERLIGHT || USE_CSHARP_SQLITE || USE_SQLITEPCL_RAW
			var r = SQLite3.Open (databasePath, out handle, (int)openFlags, IntPtr.Zero);
#else
			// open using the byte[]
			// in the case where the path may include Unicode
			// force open to using UTF-8 using sqlite3_open_v2
			var databasePathAsBytes = GetNullTerminatedUtf8 (DatabasePath);
			var r = SQLite3.Open (databasePathAsBytes, out handle, (int) openFlags, IntPtr.Zero);
#endif

			Handle = handle;
			if (r != SQLite3.Result.OK) {
				throw SQLiteException.New (r, String.Format ("Could not open database file: {0} ({1})", DatabasePath, r));
			}

#if USE_SQLITEPCL_RAW
			// enabled extended result codes
			SQLitePCL.raw.sqlite3_extended_result_codes(handle, 1);
#endif
            _open = true;

			StoreDateTimeAsTicks = storeDateTimeAsTicks;

			BusyTimeout = TimeSpan.FromSeconds (0.1);

			_cachedPreparedCommands = new ConcurrentCommandDictionary();
			Tracer = line => Debug.WriteLine (line);
		}

#if __IOS__
		static SQLiteConnection ()
		{
			if (_preserveDuringLinkMagic) {
				var ti = new ColumnInfo ();
				ti.Name = "magic";
			}
		}

   		/// <summary>
		/// Used to list some code that we want the MonoTouch linker
		/// to see, but that we never want to actually execute.
		/// </summary>
		static bool _preserveDuringLinkMagic;
#endif

		/// <summary>
		/// Enable or disable extension loading.
		/// </summary>
		public void EnableLoadExtension (bool enabled)
		{
			SQLite3.Result r = SQLite3.EnableLoadExtension (Handle, enabled ? 1 : 0);
			if (r != SQLite3.Result.OK) {
				string msg = SQLite3.GetErrorMessage(Handle);
				throw SQLiteException.New (r, msg);
			}
		}

#if !USE_SQLITEPCL_RAW
		static byte[] GetNullTerminatedUtf8 (string s)
		{
			var utf8Length = System.Text.Encoding.UTF8.GetByteCount (s);
			var bytes = new byte [utf8Length + 1];
			utf8Length = System.Text.Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
			return bytes;
		}
#endif

		/// <summary>
		/// Sets a busy handler to sleep the specified amount of time when a table is locked.
		/// The handler will sleep multiple times until a total time of <see cref="BusyTimeout"/> has accumulated.
		/// </summary>
		public TimeSpan BusyTimeout {
			get { return _busyTimeout; }
			set {
				_busyTimeout = value;
				if (Handle != NullHandle) {
					SQLite3.BusyTimeout (Handle, (int)_busyTimeout.TotalMilliseconds);
				}
			}
		}

	    /// <summary>
	    /// Returns the mappings from types to tables that the connection
	    /// currently understands.
	    /// </summary>
	    public IEnumerable<TableMapping> TableMappings => TableMapping.TableMappings;

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
		public TableMapping GetMapping(Type type, CreateFlags createFlags = CreateFlags.None) => TableMapping.GetMapping(type, createFlags);

		/// <summary>
		/// Retrieves the mapping that is automatically generated for the given type.
		/// </summary>
		/// <returns>
		/// The mapping represents the schema of the columns of the database and contains 
		/// methods to set and get properties of objects.
		/// </returns>
		public TableMapping GetMapping<T>() => TableMapping.GetMapping<T>();

		private struct IndexedColumn
		{
			public int Order;
			public string ColumnName;
		}

		private struct IndexInfo
		{
			public string IndexName;
			public string TableName;
			public bool Unique;
			public List<IndexedColumn> Columns;
		}

		/// <summary>
		/// Executes a "drop table" on the database.  This is non-recoverable.
		/// </summary>
		public int DropTable<T> ()
		{
			return DropTable (GetMapping<T>());
		}

		/// <summary>
		/// Executes a "drop table" on the database.  This is non-recoverable.
		/// </summary>
		/// <param name="map">
		/// The TableMapping used to identify the table.
		/// </param>
		public int DropTable (TableMapping map)
		{
			var query = string.Format ("drop table if exists \"{0}\"", map.TableName);
			return Execute (query);
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
		public CreateTableResult CreateTable<T> (CreateFlags createFlags = CreateFlags.None)
		{
			return CreateTable (typeof (T), createFlags);
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
		public CreateTableResult CreateTable (Type ty, CreateFlags createFlags = CreateFlags.None)
		{
			var map = GetMapping(ty, createFlags);

			// Present a nice error if no columns specified
			if (map.Columns.Length == 0) {
				throw new Exception (string.Format ("Cannot create a table with zero columns (does '{0}' have public properties?)", ty.FullName));
			}

			// Check if the table exists
			var result = CreateTableResult.Created;
			var existingCols = GetTableInfo (map.TableName);

			// Create or migrate it
			if (existingCols.Count == 0) {

				// Facilitate virtual tables a.k.a. full-text search.
				bool fts3 = (createFlags & CreateFlags.FullTextSearch3) != 0;
				bool fts4 = (createFlags & CreateFlags.FullTextSearch4) != 0;
				bool fts = fts3 || fts4;
				var @virtual = fts ? "virtual " : string.Empty;
				var @using = fts3 ? "using fts3 " : fts4 ? "using fts4 " : string.Empty;

				// Build query.
				var query = "create " + @virtual + "table if not exists \"" + map.TableName + "\" " + @using + "(\n";
				var decls = map.Columns.Select (p => Orm.SqlDecl (p, StoreDateTimeAsTicks));
				var decl = string.Join (",\n", decls.ToArray ());
				query += decl;
				query += ")";

			    SQLite3.Result r;
			    using (var cmd = NewCommand(query))
			        r = cmd.Execute(null);

				result = r == SQLite3.Result.Done ? CreateTableResult.Created : CreateTableResult.Error;

			}
			else {
				result = CreateTableResult.Migrated;
				MigrateTable (map, existingCols);
			}

			var indexes = new Dictionary<string, IndexInfo> ();
			foreach (var c in map.Columns) {
				foreach (var i in c.Indices) {
					var iname = i.Name ?? map.TableName + "_" + c.Name;
					IndexInfo iinfo;
					if (!indexes.TryGetValue (iname, out iinfo)) {
						iinfo = new IndexInfo {
							IndexName = iname,
							TableName = map.TableName,
							Unique = i.Unique,
							Columns = new List<IndexedColumn> ()
						};
						indexes.Add (iname, iinfo);
					}

					if (i.Unique != iinfo.Unique)
						throw new Exception ("All the columns in an index must have the same value for their Unique property");

					iinfo.Columns.Add (new IndexedColumn {
						Order = i.Order,
						ColumnName = c.Name
					});
				}
			}

		    var success = true;
			foreach (var indexName in indexes.Keys) {
				var index = indexes[indexName];
				var columns = index.Columns.OrderBy(i => i.Order).Select(i => i.ColumnName).ToArray();
			    success = success && CreateIndex(indexName, index.TableName, columns, index.Unique);
			}

			result = success ? result : CreateTableResult.Error;
			return result;
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
		public CreateTablesResult CreateTables<T, T2> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
		{
			return CreateTables (createFlags, typeof (T), typeof (T2));
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
		public CreateTablesResult CreateTables<T, T2, T3> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new()
		{
			return CreateTables (createFlags, typeof (T), typeof (T2), typeof (T3));
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
		public CreateTablesResult CreateTables<T, T2, T3, T4> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new()
			where T4 : new()
		{
			return CreateTables (createFlags, typeof (T), typeof (T2), typeof (T3), typeof (T4));
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
		public CreateTablesResult CreateTables<T, T2, T3, T4, T5> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new()
			where T4 : new()
			where T5 : new()
		{
			return CreateTables (createFlags, typeof (T), typeof (T2), typeof (T3), typeof (T4), typeof (T5));
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
		public CreateTablesResult CreateTables (CreateFlags createFlags = CreateFlags.None, params Type[] types)
		{
			var result = new CreateTablesResult ();
			foreach (Type type in types) {
				var aResult = CreateTable (type, createFlags);
				result.Results[type] = aResult;
			}
			return result;
		}

		/// <summary>
		/// Creates an index for the specified table and columns.
		/// </summary>
		/// <param name="indexName">Name of the index to create</param>
		/// <param name="tableName">Name of the database table</param>
		/// <param name="columnNames">An array of column names to index</param>
		/// <param name="unique">Whether the index should be unique</param>
		public bool CreateIndex(string indexName, string tableName, string[] columnNames, bool unique = false)
		{
			const string sqlFormat = "create {2} index if not exists \"{3}\" on \"{0}\"(\"{1}\")";
			var sql = String.Format (sqlFormat, tableName, string.Join ("\", \"", columnNames), unique ? "unique" : "", indexName);
		    SQLite3.Result r;

		    using (var cmd = NewCommand(sql))
		        r = cmd.Execute(null);

		    return r == SQLite3.Result.Done;
		}

		/// <summary>
		/// Creates an index for the specified table and column.
		/// </summary>
		/// <param name="indexName">Name of the index to create</param>
		/// <param name="tableName">Name of the database table</param>
		/// <param name="columnName">Name of the column to index</param>
		/// <param name="unique">Whether the index should be unique</param>
		public bool CreateIndex(string indexName, string tableName, string columnName, bool unique = false)
		{
			return CreateIndex (indexName, tableName, new string[] { columnName }, unique);
		}

		/// <summary>
		/// Creates an index for the specified table and column.
		/// </summary>
		/// <param name="tableName">Name of the database table</param>
		/// <param name="columnName">Name of the column to index</param>
		/// <param name="unique">Whether the index should be unique</param>
		public bool CreateIndex(string tableName, string columnName, bool unique = false)
		{
			return CreateIndex (tableName + "_" + columnName, tableName, columnName, unique);
		}

		/// <summary>
		/// Creates an index for the specified table and columns.
		/// </summary>
		/// <param name="tableName">Name of the database table</param>
		/// <param name="columnNames">An array of column names to index</param>
		/// <param name="unique">Whether the index should be unique</param>
		public bool CreateIndex(string tableName, string[] columnNames, bool unique = false)
		{
			return CreateIndex (tableName + "_" + string.Join ("_", columnNames), tableName, columnNames, unique);
		}

		/// <summary>
		/// Creates an index for the specified object property.
		/// e.g. CreateIndex&lt;Client&gt;(c => c.Name);
		/// </summary>
		/// <typeparam name="T">Type to reflect to a database table.</typeparam>
		/// <param name="property">Property to index</param>
		/// <param name="unique">Whether the index should be unique</param>
		public bool CreateIndex<T>(Expression<Func<T, object>> property, bool unique = false)
		{
			MemberExpression mx;
			if (property.Body.NodeType == ExpressionType.Convert) {
				mx = ((UnaryExpression)property.Body).Operand as MemberExpression;
			}
			else {
				mx = (property.Body as MemberExpression);
			}
			var propertyInfo = mx.Member as PropertyInfo;
			if (propertyInfo == null) {
				throw new ArgumentException ("The lambda expression 'property' should point to a valid Property");
			}

			var propName = propertyInfo.Name;

			var map = GetMapping<T> ();
			var colName = map.FindColumnWithPropertyName (propName).Name;

			return CreateIndex (map.TableName, colName, unique);
		}

		public class ColumnInfo 
		{
			//public int cid { get; set; }

			[Column("name")]
			public string Name { get; set; }

			[Column("type")]
			public string ColumnType { get; set; }

			[Column("notnull")]
			public int notnull { get; set; }

			//public string dflt_value { get; set; }

			[Column("pk")]
			public int PK { get; set; }

			public override string ToString() => $"{Name} ({ColumnType})";
		}

		/// <summary>
		/// Query the built-in sqlite table_info table for a specific tables columns.
		/// </summary>
		/// <returns>The columns contains in the table.</returns>
		/// <param name="tableName">Table name.</param>
		public List<ColumnInfo> GetTableInfo (string tableName)
		{
			var query = "pragma table_info(\"" + tableName + "\")";
			return Query<ColumnInfo> (query);
		}

		void MigrateTable (TableMapping map, List<ColumnInfo> existingCols)
		{
			var toBeAdded = new List<TableMapping.Column> ();

			foreach (var p in map.Columns) {
				var found = false;
				foreach (var c in existingCols) {
					found = (string.Compare (p.Name, c.Name, StringComparison.OrdinalIgnoreCase) == 0);
					if (found)
						break;
				}
				if (!found) {
					toBeAdded.Add (p);
				}
			}

			foreach (var p in toBeAdded) {
				var addCol = "alter table \"" + map.TableName + "\" add column " + Orm.SqlDecl (p, StoreDateTimeAsTicks);
				Execute (addCol);
			}
		}

		/// <summary>
		/// Creates a new SQLiteCommand. Can be overridden to provide a sub-class.
		/// </summary>
		/// <seealso cref="SQLiteCommand.OnInstanceCreated"/>
		protected virtual SQLiteCommand NewCommand (string cmdText)
		{
			return new SQLiteCommand (this, cmdText);
		}

		/// <summary>
		/// Creates a new SQLiteCommand given the command text with arguments. Place a '?'
		/// in the command text for each of the arguments.
		/// </summary>
		/// <param name="cmdText">
		/// The fully escaped SQL.
		/// </param>
		/// <param name="ps">
		/// Arguments to substitute for the occurences of '?' in the command text.
		/// </param>
		/// <returns>
		/// A <see cref="SQLiteCommand"/>
		/// </returns>
	    public SQLiteCommand CreateCommand(string cmdText, bool fromCache = false)
	    {
	        if (!_open)
	            throw SQLiteException.New(SQLite3.Result.Error, "Cannot create commands from unopened database", sql: cmdText);

	        SQLiteCommand cmd = null;

	        if (fromCache)
	            cmd = GetCachedCommand(cmdText);

	        if (cmd == null)
	            cmd = NewCommand(cmdText);

	        return cmd;
	    }

	    private SQLiteCommand GetCachedCommand(string commandName)
	    {
	        SQLiteCommand cachedCommand;

	        if (!_cachedPreparedCommands.TryGetValue(commandName, out cachedCommand))
	        {
	            var prepCmd = NewCommand(commandName);

	            cachedCommand = prepCmd;
	            if (!_cachedPreparedCommands.TryAdd(commandName, prepCmd))
	            {
	                // concurrent add attempt this add, retreive a fresh copy
	                prepCmd.Dispose();
	                _cachedPreparedCommands.TryGetValue(commandName, out cachedCommand);
	            }
	        }

	        return cachedCommand;
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
	    public int Execute(string query, params object[] args)
	    {
	        using (var cmd = NewCommand(query))
	        {
	            var result = cmd.ExecuteNonQuery(args);
	            return result;
	        }
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
	    public int PreparedExecute(string query, params object[] args)
	    {
	        var cmd = CreateCommand(query, fromCache: true);
	        var result = cmd.ExecuteNonQuery(args);
	        return result;
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
        public T ExecuteScalar<T>(string query, params object[] args)
	    {
	        using (var cmd = NewCommand(query))
	        {
	            var result = cmd.ExecuteScalar<T>(args);
	            return result;
	        }
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
		/// </returns>
		public List<T> Query<T> (string query, params object[] args) where T : new()
		{
		    using (var cmd = CreateCommand(query, fromCache: false))
		    {
		        var result = cmd.ExecuteQuery<T>(args).ToList();
		        return result;
		    }
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
	    public IEnumerable<T> PreparedQuery<T>(string query, params object[] args) where T : new()
	    {
	        var cmd = CreateCommand(query, fromCache: true);
	        var result = cmd.ExecuteQuery<T>(args);
	        return result;
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
        public IEnumerable<T> DeferredQuery<T>(string query, params object[] args) where T : new()
        {
            return PreparedQuery<T>(query, args);
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
		public List<object> Query (TableMapping map, string query, params object[] args)
		{
	        using (var cmd = CreateCommand(query, fromCache: false))
	        {
	            var result = cmd.ExecuteQuery<object>(map, args).ToList();
	            return result;
	        }
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
	    public IEnumerable<object> PreparedQuery(TableMapping map, string query, params object[] args)
	    {
	        var cmd = CreateCommand(query, fromCache: true);
	        var result = cmd.ExecuteQuery<object>(map, args);
	        return result;
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
		public IEnumerable<object> DeferredQuery (TableMapping map, string query, params object[] args)
		{
	        return PreparedQuery(map, query, args);
	    }

		/// <summary>
		/// Returns a queryable interface to the table represented by the given type.
		/// </summary>
		/// <returns>
		/// A queryable object that is able to translate Where, OrderBy, and Take
		/// queries into native SQL.
		/// </returns>
		public TableQuery<T> Table<T> () where T : new()
		{
			return new TableQuery<T> (this);
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
		public T Get<T> (object pk) where T : new()
		{
			var map = GetMapping (typeof (T));
			return Query<T> (map.GetByPrimaryKeySql, pk).First ();
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
		public object Get (object pk, TableMapping map)
		{
			return Query (map, map.GetByPrimaryKeySql, pk).First ();
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
		public T Get<T> (Expression<Func<T, bool>> predicate) where T : new()
		{
			return Table<T> ().Where (predicate).First ();
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
		public T Find<T> (object pk) where T : new()
		{
			var map = GetMapping (typeof (T));
			return Query<T> (map.GetByPrimaryKeySql, pk).FirstOrDefault ();
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
		public object Find (object pk, TableMapping map)
		{
			return Query (map, map.GetByPrimaryKeySql, pk).FirstOrDefault ();
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
		public T Find<T> (Expression<Func<T, bool>> predicate) where T : new()
		{
			return Table<T> ().Where (predicate).FirstOrDefault ();
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
		public T FindWithQuery<T> (string query, params object[] args) where T : new()
		{
			return Query<T> (query, args).FirstOrDefault ();
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
		public object FindWithQuery (TableMapping map, string query, params object[] args)
		{
			return Query (map, query, args).FirstOrDefault ();
		}

		/// <summary>
		/// Whether <see cref="BeginTransaction"/> has been called and the database is waiting for a <see cref="Commit"/>.
		/// </summary>
		public bool IsInTransaction {
			get { return _transactionDepth > 0; }
		}

		/// <summary>
		/// Begins a new transaction. Call <see cref="Commit"/> to end the transaction.
		/// </summary>
		/// <example cref="System.InvalidOperationException">Throws if a transaction has already begun.</example>
		public void BeginTransaction ()
		{
			// The BEGIN command only works if the transaction stack is empty, 
			//    or in other words if there are no pending transactions. 
			// If the transaction stack is not empty when the BEGIN command is invoked, 
			//    then the command fails with an error.
			// Rather than crash with an error, we will just ignore calls to BeginTransaction
			//    that would result in an error.
			if (Interlocked.CompareExchange (ref _transactionDepth, 1, 0) == 0) {
				try {
					Execute ("begin transaction");
				}
				catch (Exception ex) {
					var sqlExp = ex as SQLiteException;
					if (sqlExp != null) {
						// It is recommended that applications respond to the errors listed below 
						//    by explicitly issuing a ROLLBACK command.
						// TODO: This rollback failsafe should be localized to all throw sites.
						switch (sqlExp.Result) {
							case SQLite3.Result.IOError:
							case SQLite3.Result.Full:
							case SQLite3.Result.Busy:
							case SQLite3.Result.NoMem:
							case SQLite3.Result.Interrupt:
								RollbackTo (null, true);
								break;
						}
					}
					else {
						// Call decrement and not VolatileWrite in case we've already 
						//    created a transaction point in SaveTransactionPoint since the catch.
						Interlocked.Decrement (ref _transactionDepth);
					}

					throw;
				}
			}
			else {
				// Calling BeginTransaction on an already open transaction is invalid
				throw new InvalidOperationException ("Cannot begin a transaction while already in a transaction.");
			}
		}

		/// <summary>
		/// Creates a savepoint in the database at the current point in the transaction timeline.
		/// Begins a new transaction if one is not in progress.
		/// 
		/// Call <see cref="RollbackTo(string)"/> to undo transactions since the returned savepoint.
		/// Call <see cref="Release"/> to commit transactions after the savepoint returned here.
		/// Call <see cref="Commit"/> to end the transaction, committing all changes.
		/// </summary>
		/// <returns>A string naming the savepoint.</returns>
		public string SaveTransactionPoint ()
		{
			int depth = Interlocked.Increment (ref _transactionDepth) - 1;
			string retVal = "S" + _rand.Next (short.MaxValue) + "D" + depth;

			try {
				Execute ("savepoint " + retVal);
			}
			catch (Exception ex) {
				var sqlExp = ex as SQLiteException;
				if (sqlExp != null) {
					// It is recommended that applications respond to the errors listed below 
					//    by explicitly issuing a ROLLBACK command.
					// TODO: This rollback failsafe should be localized to all throw sites.
					switch (sqlExp.Result) {
						case SQLite3.Result.IOError:
						case SQLite3.Result.Full:
						case SQLite3.Result.Busy:
						case SQLite3.Result.NoMem:
						case SQLite3.Result.Interrupt:
							RollbackTo (null, true);
							break;
					}
				}
				else {
					Interlocked.Decrement (ref _transactionDepth);
				}

				throw;
			}

			return retVal;
		}

		/// <summary>
		/// Rolls back the transaction that was begun by <see cref="BeginTransaction"/> or <see cref="SaveTransactionPoint"/>.
		/// </summary>
		public void Rollback(bool noThrow = false)
		{
			RollbackTo(null, noThrow);
		}

		/// <summary>
		/// Rolls back the savepoint created by <see cref="BeginTransaction"/> or SaveTransactionPoint.
		/// </summary>
		/// <param name="savepoint">The name of the savepoint to roll back to, as returned by <see cref="SaveTransactionPoint"/>.  If savepoint is null or empty, this method is equivalent to a call to <see cref="Rollback"/></param>
		public void RollbackTo(string savepoint, bool noThrow = false)
		{
			// Rolling back without a TO clause rolls backs all transactions 
			//    and leaves the transaction stack empty.   
			try {
				if (String.IsNullOrEmpty (savepoint)) {
					if (Interlocked.Exchange (ref _transactionDepth, 0) > 0) {
						Execute ("rollback");
					}
				}
				else {
					DoSavePointExecute (savepoint, "rollback to ");
				}
			}
			catch (SQLiteException) {
				if (!noThrow)
					throw;

			}
			// No need to rollback if there are no transactions open.
		}

		/// <summary>
		/// Releases a savepoint returned from <see cref="SaveTransactionPoint"/>.  Releasing a savepoint 
		///    makes changes since that savepoint permanent if the savepoint began the transaction,
		///    or otherwise the changes are permanent pending a call to <see cref="Commit"/>.
		/// 
		/// The RELEASE command is like a COMMIT for a SAVEPOINT.
		/// </summary>
		/// <param name="savepoint">The name of the savepoint to release.  The string should be the result of a call to <see cref="SaveTransactionPoint"/></param>
		public void Release (string savepoint)
		{
			DoSavePointExecute (savepoint, "release ");
		}

		private void DoSavePointExecute(string savepoint, string cmd)
		{
			// Validate the savepoint
			int firstLen = savepoint.IndexOf ('D');
			if (firstLen >= 2 && savepoint.Length > firstLen + 1) {
				int depth;
				if (Int32.TryParse (savepoint.Substring (firstLen + 1), out depth)) {
					// TODO: Mild race here, but inescapable without locking almost everywhere.
					if (0 <= depth && depth < _transactionDepth) {
#if NETFX_CORE || USE_SQLITEPCL_RAW || NETCORE
						Volatile.Write (ref _transactionDepth, depth);
#elif SILVERLIGHT
						_transactionDepth = depth;
#else
                        Thread.VolatileWrite (ref _transactionDepth, depth);
#endif
						Execute (cmd + savepoint);
						return;
					}
				}
			}

			throw new ArgumentException ("savePoint is not valid, and should be the result of a call to SaveTransactionPoint.", "savePoint");
		}

		/// <summary>
		/// Commits the transaction that was begun by <see cref="BeginTransaction"/>.
		/// </summary>
		public void Commit ()
		{
			if (Interlocked.Exchange (ref _transactionDepth, 0) != 0) {
				Execute ("commit");
			}
			// Do nothing on a commit with no open transaction
		}

		/// <summary>
		/// Executes <paramref name="action"/> within a (possibly nested) transaction by wrapping it in a SAVEPOINT. If an
		/// exception occurs the whole transaction is rolled back, not just the current savepoint. The exception
		/// is rethrown.
		/// </summary>
		/// <param name="action">
		/// The <see cref="Action"/> to perform within a transaction. <paramref name="action"/> can contain any number
		/// of operations on the connection but should never call <see cref="BeginTransaction"/> or
		/// <see cref="Commit"/>.
		/// </param>
		public void RunInTransaction (Action action)
		{
			try {
				var savePoint = SaveTransactionPoint ();
				action ();
				Release (savePoint);
			}
			catch (Exception) {
				Rollback ();
				throw;
			}
		}

		/// <summary>
		/// Inserts all specified objects.
		/// </summary>
		/// <param name="objects">
		/// An <see cref="IEnumerable"/> of the objects to insert.
		/// </param>
		/// <param name="runInTransaction">
		/// A boolean indicating if the inserts should be wrapped in a transaction.
		/// </param>
		/// <returns>
		/// The number of rows added to the table.
		/// </returns>
		public int InsertAll (System.Collections.IEnumerable objects, bool runInTransaction = true)
		{
		    return InsertAll(objects, "", null, runInTransaction);
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
        public int InsertAll(System.Collections.IEnumerable objects, string extra, bool runInTransaction = true)
		{
		    return InsertAll(objects, extra, null, runInTransaction);

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
        public int InsertAll(System.Collections.IEnumerable objects, Type objType, bool runInTransaction = true)
		{
		    return InsertAll(objects, "", objType, runInTransaction);
		}

	    private static readonly SQLiteVersion MinInsertAllVersion = new SQLiteVersion(3007011); // 3.7.11

		/// <summary>
		/// Inserts all specified objects.
		/// </summary>
		/// <param name="objects">
		/// An <see cref="IEnumerable"/> of the objects to insert.
		/// </param>
		/// <param name="extra">
		/// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
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
		public int InsertAll(System.Collections.IEnumerable objects, string extra, Type objType, bool runInTransaction = true)
	    {
	        var useFallback = false;
	        var count = 0;

	        if (LibraryVersion < MinInsertAllVersion)
	            useFallback = true;

	        if (!useFallback) {
		        var o = new List<object> ();
	            foreach (var r in objects)
	                if (r != null)
	                    o.Add(r);

	            // if there are no records just return 0
	            if (o.Count == 0)
	                return 0;

	            // if there is only one object it is better to go through the old logic because the insert is already prepared
	            if (!useFallback && o.Count == 1)
	                useFallback = true;

	            if (!useFallback)
	            {
	                var firstRecordType = objType;
	                if (firstRecordType == null)
	                    firstRecordType = o[0].GetType();

	                // all the types need to match
	                foreach (var r in o)
	                {
	                    if (r.GetType() != firstRecordType)
	                    {
	                        useFallback = true;
	                        break;
	                    }
	                }
	            }

	            if (!useFallback)
	            {
	                if (runInTransaction)
	                {
	                    RunInTransaction(() => {
	                        count = InternalInsertAll(o, extra, objType);
	                    });
	                }
	                else
	                {
	                    count = InternalInsertAll(o, extra, objType);
	                }
	            }
	        }

	        if (useFallback)
	        {
	            if (runInTransaction)
	            {
	                RunInTransaction(() => {
	                    foreach (var r in objects)
	                        count += Insert(r, extra, objType);
	                });
	            }
	            else
	            {
	                foreach (var r in objects)
	                    count += Insert(r, extra, objType);
	            }
	        }

	        return count;
	    }

	    private object[] InternalInsertRecordValues(TableMapping map, object obj, Type objType, bool replacing)
	    {
			if (map.PK != null && map.PK.IsAutoGuid)
			{
				// no GetProperty so search our way up the inheritance chain till we find it
				PropertyInfo prop;
				while (objType != null)
				{
					var info = objType.GetTypeInfo();
					prop = info.GetDeclaredProperty(map.PK.PropertyName);
					if (prop != null)
					{
						if (prop.GetValue(obj, null).Equals(Guid.Empty))
						{
							prop.SetValue(obj, Guid.NewGuid(), null);
						}
						break;
					}

					objType = info.BaseType;
				}
			}

	        var cols = replacing ? map.InsertOrReplaceColumns : map.InsertColumns;
	        var vals = new object[cols.Length];

	        for (var i = 0; i < vals.Length; i++)
	        {
	            var col = cols[i];
	            var value = col.GetValue(obj);

	            // primary key, auto increment, with a value of 0 should be considered a new incremented value
	            if (col.IsPK && col.IsAutoInc && col.ColumnType == typeof(long) && Object.Equals(value, 0L))
	                value = null;

	            vals[i] = value;
	        }

	        return vals;
	    }

        private int InternalInsertAll(IList objects, string extra, Type objType)
        {
            if (objects.Count == 0)
                return 0;

            objType = objType ?? objects[0].GetType();

	        int count = 0;
            var map = GetMapping(objType);
	        var replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;
	        var cols = replacing ? map.InsertOrReplaceColumns : map.InsertColumns;

            // todo: look up if this value was changed at runtime by using sqlite3_limit(db,SQLITE_LIMIT_VARIABLE_NUMBER,size)
            const int SQLITE_LIMIT_VARIABLE_NUMBER = 999;

	        var recordValues = new Queue<object[]>();
            foreach(var obj in objects)
                recordValues.Enqueue(InternalInsertRecordValues(map, obj, objType, replacing));

            var line = $"({String.Join(",", cols.Select(c => "?"))}),";
            while (recordValues.Count > 0)
	        {
	            var insertVals = new List<object>();
	            var s = new StringBuilder();
	            s.AppendFormat("insert {2} into \"{0}\"({1}) values ", map.TableName, String.Join(",", cols.Select(c => $"\"{c.Name}\"")), extra);

                while (recordValues.Count > 0 && insertVals.Count + cols.Length < SQLITE_LIMIT_VARIABLE_NUMBER)
	            {
	                insertVals.AddRange(recordValues.Dequeue());
	                s.Append(line);
	            }

	            try
	            {
	                // remove the last comma from the string
	                var insertSql = s.ToString(0, s.Length - 1);
                    count += Execute(insertSql, insertVals.ToArray());
	            }
	            catch (SQLiteException ex)
	            {
	                if (SQLite3.ExtendedErrCode(this.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
	                {
	                    throw NotNullConstraintViolationException.New(ex.Result, ex.Message);
	                }
	                throw;
	            }
	        }

	        if (count > 0)
	            OnTableChanged(map, NotifyTableChangedAction.Insert);

	        return count;
	    }

		/// <summary>
		/// Inserts the given object (and updates its
		/// auto incremented primary key if it has one).
		/// The return value is the number of rows added to the table.
		/// </summary>
		/// <param name="obj">
		/// The object to insert.
		/// </param>
		/// <returns>
		/// The number of rows added to the table.
		/// </returns>
		public int Insert (object obj)
		{
			if (obj == null) {
				return 0;
			}
			return Insert (obj, "", Orm.GetType (obj));
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
		public int InsertOrReplace (object obj)
		{
			if (obj == null) {
				return 0;
			}
			return Insert (obj, "OR REPLACE", Orm.GetType (obj));
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
		public int Insert (object obj, Type objType)
		{
			return Insert (obj, "", objType);
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
		public int InsertOrReplace (object obj, Type objType)
		{
			return Insert (obj, "OR REPLACE", objType);
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
		public int Insert (object obj, string extra)
		{
			if (obj == null) {
				return 0;
			}
			return Insert (obj, extra, Orm.GetType (obj));
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
		public int Insert (object obj, string extra, Type objType)
		{
			if (obj == null) {
				return 0;
			}

		    objType = objType ?? Orm.GetType (obj);
			var map = GetMapping(objType);
			var replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;
			var cols = replacing ? map.InsertOrReplaceColumns : map.InsertColumns;
		    var vals = InternalInsertRecordValues(map, obj, objType, replacing);

			var insertCmd = GetInsertCommand(map, extra);
			int count;

			lock (insertCmd) {
				// We lock here to protect the prepared statement returned via GetInsertCommand.
				// A SQLite prepared statement can be bound for only one operation at a time.
				try {
					count = insertCmd.ExecuteNonQuery (vals);
				}
				catch (SQLiteException ex) {
					if (SQLite3.ExtendedErrCode (this.Handle) == SQLite3.ExtendedResult.ConstraintNotNull) {
						throw NotNullConstraintViolationException.New (ex.Result, ex.Message, map, obj);
					}
					throw;
				}

				if (map.HasAutoIncPK) {
					var id = SQLite3.LastInsertRowid (Handle);
					map.SetAutoIncPK (obj, id);
				}
			}
			if (count > 0)
				OnTableChanged (map, NotifyTableChangedAction.Insert);

			return count;
		}

	    public SQLiteCommand GetInsertCommand(TableMapping map, string extra)
	    {
	        var commandName = $"insert {extra} into \"{map.TableName}\"";

	        SQLiteCommand cachedCommand;

	        if (!_cachedPreparedCommands.TryGetValue(commandName, out cachedCommand)) {
	            var cols = map.InsertColumns;
	            string insertSql;

	            // weird case to handle where there is only one column in the database, and it is an auto incrementing column
	            if (!cols.Any() && map.Columns.Count() == 1 && map.Columns[0].IsAutoInc) {
	                insertSql = $"insert {extra} into \"{map.TableName}\" default values";
	            }
	            else {
	                if (String.Equals(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase))
	                    cols = map.InsertOrReplaceColumns;

	                insertSql = String.Format("insert {3} into \"{0}\"({1}) values ({2})", map.TableName,
	                    String.Join(",", cols.Select(c => $"\"{c.Name}\"")),
	                    String.Join(",", cols.Select(c => "?")), extra);
	            }

	            var prepCmd = new SQLiteCommand(this, insertSql);

	            cachedCommand = prepCmd;
	            if (!_cachedPreparedCommands.TryAdd(commandName, prepCmd)) {
	                // concurrent add attempt this add, retreive a fresh copy
	                prepCmd.Dispose();
	                _cachedPreparedCommands.TryGetValue(commandName, out cachedCommand);
	            }
	        }

	        return cachedCommand;
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
		public int Update (object obj)
		{
			if (obj == null) {
				return 0;
			}
			return Update(obj, "", Orm.GetType (obj));
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
		public int Update (object obj, Type objType)
		{
			if (obj == null) {
				return 0;
			}
			return Update(obj, "", objType);
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
		public int Update(object obj, string extra, Type objType)
		{		
			if (obj == null) {
				return 0;
			}

		    objType = objType ?? Orm.GetType (obj);
			var map = GetMapping(objType);
		    var pk = map.PK;
            var cols = map.UpdateColumns;
            var vals = cols.Select(c => c.GetValue(obj));

		    var ps = new List<object>(vals);
		    ps.Add(pk.GetValue(obj));

		    var updateCmd = GetUpdateCommand(map, extra);
		    int count;

		    lock (updateCmd)
		    {
		        // We lock here to protect the prepared statement returned via GetInsertCommand.
		        // A SQLite prepared statement can be bound for only one operation at a time.
		        try
		        {
		            count = updateCmd.ExecuteNonQuery(ps.ToArray());
		        }
		        catch (SQLiteException ex) {
		            if (ex.Result == SQLite3.Result.Constraint && SQLite3.ExtendedErrCode(this.Handle) == SQLite3.ExtendedResult.ConstraintNotNull) {
		                throw NotNullConstraintViolationException.New(ex, map, obj);
		            }
                    throw;
		        }
		    }

		    if (count > 0)
		        OnTableChanged(map, NotifyTableChangedAction.Update);

			return count;
		}

	    public SQLiteCommand GetUpdateCommand(TableMapping map, string extra)
	    {
	        var commandName = $"update {extra} \"{map.TableName}\"";

	        SQLiteCommand cachedCommand;

	        if (!_cachedPreparedCommands.TryGetValue(commandName, out cachedCommand)) {
	            var pk = map.PK;

	            if (pk == null)
	                throw new NotSupportedException("Cannot update " + map.TableName + ": it has no PK");

	            var cols = map.UpdateColumns;
	            string updateSql;

                updateSql = String.Format("update {3} \"{0}\" set {1} where {2} = ?", map.TableName,
                    String.Join(",", cols.Select(c => $"\"{c.Name}\" = ?")),
                    pk.Name, extra);

	            var prepCmd = new SQLiteCommand(this, updateSql);

	            cachedCommand = prepCmd;
	            if (!_cachedPreparedCommands.TryAdd(commandName, prepCmd))
	            {
	                // concurrent add attempt this add, retreive a fresh copy
	                prepCmd.Dispose();
	                _cachedPreparedCommands.TryGetValue(commandName, out cachedCommand);
	            }
	        }

	        return cachedCommand;
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
		public int UpdateAll (System.Collections.IEnumerable objects, bool runInTransaction = true)
		{
			var c = 0;
			if (runInTransaction) {
				RunInTransaction (() => {
					foreach (var r in objects) {
						c += Update (r);
					}
				});
			}
			else {
				foreach (var r in objects) {
					c += Update (r);
				}
			}
			return c;
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
		public int Delete (object objectToDelete)
		{
			var map = GetMapping (Orm.GetType (objectToDelete));
			var pk = map.PK;
			if (pk == null) {
				throw new NotSupportedException ("Cannot delete " + map.TableName + ": it has no PK");
			}
			var q = string.Format ("delete from \"{0}\" where \"{1}\" = ?", map.TableName, pk.Name);
			var count = PreparedExecute(q, pk.GetValue(objectToDelete));
			if (count > 0)
				OnTableChanged (map, NotifyTableChangedAction.Delete);

			return count;
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
		public int Delete<T> (object primaryKey)
		{
			return Delete (primaryKey, GetMapping (typeof (T)));
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
		public int Delete (object primaryKey, TableMapping map)
		{
			var pk = map.PK;
			if (pk == null) {
				throw new NotSupportedException ("Cannot delete " + map.TableName + ": it has no PK");
			}
			var q = string.Format ("delete from \"{0}\" where \"{1}\" = ?", map.TableName, pk.Name);
			var count = PreparedExecute(q, primaryKey);
			if (count > 0)
				OnTableChanged(map, NotifyTableChangedAction.Delete);

			return count;
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
		public int DeleteAll<T> ()
		{
			var map = GetMapping (typeof (T));
			return DeleteAll (map);
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
		public int DeleteAll (TableMapping map)
		{
			var query = string.Format ("delete from \"{0}\"", map.TableName);
			var count = PreparedExecute(query);
			if (count > 0)
				OnTableChanged (map, NotifyTableChangedAction.Delete);
			return count;
		}

		~SQLiteConnection ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		public void Close ()
		{
			Dispose (true);
		}

	    private static readonly SQLiteVersion MinClose2Version = new SQLiteVersion(3007014); // 3.7.14

		protected virtual void Dispose (bool disposing)
		{
			var useClose2 = LibraryVersion >= MinClose2Version;

			if (_open && Handle != NullHandle) {
				try {
					if (disposing) {
						foreach (var pair in _cachedPreparedCommands)
							pair.Value.Dispose();

						_cachedPreparedCommands.Clear();

						var r = useClose2 ? SQLite3.Close2 (Handle) : SQLite3.Close (Handle);
						if (r != SQLite3.Result.OK)
						{
							string msg = SQLite3.GetErrorMessage(Handle);
							throw SQLiteException.New (r, msg);
						}
					}
					else {
						var r = useClose2 ? SQLite3.Close2 (Handle) : SQLite3.Close (Handle);
					}
				}
				finally {
#if USE_SQLITEPCL_RAW
					Handle.Dispose();
#endif
					Handle = NullHandle;
					_open = false;
				}
			}
		}

		private void OnTableChanged(TableMapping table, NotifyTableChangedAction action)
		{
			var ev = TableChanged;
			if (ev != null)
				ev (this, new NotifyTableChangedEventArgs (table, action));
		}

		public event EventHandler<NotifyTableChangedEventArgs> TableChanged;
	}

	public class NotifyTableChangedEventArgs : EventArgs
	{
		public TableMapping Table { get; private set; }
		public NotifyTableChangedAction Action { get; private set; }

		public NotifyTableChangedEventArgs (TableMapping table, NotifyTableChangedAction action)
		{
			Table = table;
			Action = action;
		}
	}

	public enum NotifyTableChangedAction
	{
		Insert,
		Update,
		Delete,
	}

	/// <summary>
	/// Represents a parsed connection string.
	/// </summary>
	public class SQLiteConnectionString
	{
		public string ConnectionString { get; private set; }
		public string DatabasePath { get; private set; }
		public bool StoreDateTimeAsTicks { get; private set; }

#if NETFX_CORE
		static readonly string MetroStyleDataPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;

        public static readonly string[] InMemoryDbPaths = new[]
        {
            ":memory:",
            "file::memory:"
        };

        public static bool IsInMemoryPath(string databasePath)
        {
            return InMemoryDbPaths.Any(i => i.Equals(databasePath, StringComparison.OrdinalIgnoreCase));
        }

#endif

		public SQLiteConnectionString (string databasePath, bool storeDateTimeAsTicks)
		{
			ConnectionString = databasePath;
			StoreDateTimeAsTicks = storeDateTimeAsTicks;

#if NETFX_CORE
			DatabasePath = IsInMemoryPath(databasePath)
                ? databasePath
                : System.IO.Path.Combine(MetroStyleDataPath, databasePath);

#else
			DatabasePath = databasePath;
#endif
		}
	}
	
	[AttributeUsage(AttributeTargets.Class)]
	public class TableMapperAttribute : Attribute 
	{
		public Type Mapper { get; set; }

		public TableMapperAttribute(Type mapper)
		{
			Mapper = mapper;
		}
	}

	[AttributeUsage (AttributeTargets.Class)]
	public class TableAttribute : Attribute
	{
		public string Name { get; set; }

		public TableAttribute (string name)
		{
			Name = name;
		}
	}

	[AttributeUsage (AttributeTargets.Property)]
	public class ColumnAttribute : Attribute
	{
		public string Name { get; set; }

		public ColumnAttribute (string name)
		{
			Name = name;
		}
	}

	[AttributeUsage (AttributeTargets.Property)]
	public class PrimaryKeyAttribute : Attribute
	{
	}

	[AttributeUsage (AttributeTargets.Property)]
	public class AutoIncrementAttribute : Attribute
	{
	}

	[AttributeUsage (AttributeTargets.Property)]
	public class IndexedAttribute : Attribute
	{
		public string Name { get; set; }
		public int Order { get; set; }
		public virtual bool Unique { get; set; }

		public IndexedAttribute ()
		{
		}

		public IndexedAttribute (string name, int order)
		{
			Name = name;
			Order = order;
		}
	}

	[AttributeUsage (AttributeTargets.Property)]
	public class IgnoreAttribute : Attribute
	{
	}

	[AttributeUsage (AttributeTargets.Property)]
	public class UniqueAttribute : IndexedAttribute
	{
		public override bool Unique {
			get { return true; }
			set { /* throw?  */ }
		}
	}

	[AttributeUsage (AttributeTargets.Property)]
	public class MaxLengthAttribute : Attribute
	{
		public int Value { get; private set; }

		public MaxLengthAttribute (int length)
		{
			Value = length;
		}
	}

	/// <summary>
	/// Select the collating sequence to use on a column.
	/// "BINARY", "NOCASE", and "RTRIM" are supported.
	/// "BINARY" is the default.
	/// </summary>
	[AttributeUsage (AttributeTargets.Property)]
	public class CollationAttribute : Attribute
	{
		public string Value { get; private set; }

		public CollationAttribute (string collation)
		{
			Value = collation;
		}
	}

	[AttributeUsage (AttributeTargets.Property)]
	public class NotNullAttribute : Attribute
	{
	}

	[AttributeUsage (AttributeTargets.Enum)]
	public class StoreAsTextAttribute : Attribute
	{
	}

	public interface ITableMapper
    {
        List<TableMapping.Column> GetColumns(Type t, CreateFlags createFlags = CreateFlags.None);

        object CreateInstance(Type t);

        Type GetSchema(Type t);
    }

    public class TableMapping
    {
        private static readonly ConcurrentTableDictionary _mappings = new ConcurrentTableDictionary();

        public static IEnumerable<TableMapping> TableMappings => _mappings.Values;

        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <returns>
        /// The mapping represents the schema of the columns of the database and contains
        /// methods to set and get properties of objects.
        /// </returns>
        public static TableMapping GetMapping(Type type, CreateFlags createFlags = CreateFlags.None)
        {
            TableMapping map;
            if (!_mappings.TryGetValue(type, out map))
            {
                map = new TableMapping(type, createFlags);
                if (!_mappings.TryAdd(type, map))
                {
                    // concurrent add attempt this add, retreive a fresh copy
                    _mappings.TryGetValue(type, out map);
                }
            }

            return map;
        }

        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <returns>
        /// The mapping represents the schema of the columns of the database and contains
        /// methods to set and get properties of objects.
        /// </returns>
        public static TableMapping GetMapping<T>()
        {
            return GetMapping(typeof(T));
        }

        private readonly ITableMapper _mapper;

        private Type Schema { get; }

        public string TableName { get; }

        public Column[] Columns { get; }

        public Column PK { get; }

		public string GetByPrimaryKeySql { get; private set; }

        private readonly Column _autoPk;
        private Column[] _insertColumns;
        private Column[] _insertOrReplaceColumns;
        private Dictionary<string, Column> _columnNameIndex;
        private Dictionary<string, Column> _columnPropertyNameIndex;
        private Column[] _updateColumns;

        private TableMapping(Type type, CreateFlags createFlags = CreateFlags.None)
        {
	        var typeInfo = type.GetTypeInfo ();
            var mapperAttr = (TableMapperAttribute)typeInfo.GetCustomAttributes(typeof(TableMapperAttribute), true).FirstOrDefault();
            var mapperType = mapperAttr != null ? mapperAttr.Mapper : typeof(DefaultTableMapper);

            _mapper = Activator.CreateInstance(mapperType) as ITableMapper;
            if (_mapper == null)
                throw new Exception($"{mapperType.Name} is not of type {nameof(ITableMapper)}.");

            Schema = _mapper.GetSchema(type);

            var tableAttr =
                typeInfo.CustomAttributes
                    .Where(x => x.AttributeType == typeof(TableAttribute))
                    .Select(x => (TableAttribute)Orm.InflateAttribute(x))
                    .FirstOrDefault();

			TableName = (tableAttr != null && !string.IsNullOrEmpty (tableAttr.Name)) ? tableAttr.Name : Schema.Name;

            Columns = _mapper.GetColumns(Schema, createFlags).ToArray();
            foreach (var c in Columns) {
				if (c.IsAutoInc && c.IsPK) {
					_autoPk = c;
				}
				if (c.IsPK) {
					PK = c;
				}
			}

			HasAutoIncPK = _autoPk != null;

            GetByPrimaryKeySql = PK != null
                ? $"select * from \"{TableName}\" where \"{PK.Name}\" = ?"
                : $"select * from \"{TableName}\" limit 1";
        }

        public object CreateInstance() => _mapper.CreateInstance(Schema);

		public bool HasAutoIncPK { get; private set; }

        public void SetAutoIncPK(object obj, long id)
        {
            _autoPk?.SetValue(obj, Convert.ChangeType(id, _autoPk.ColumnType, null));
        }

        public Column[] UpdateColumns => _updateColumns ?? (_updateColumns = Columns.Where(c => !c.IsPK).ToArray());

        public Column[] InsertColumns => _insertColumns ?? (_insertColumns = Columns.Where(c => !c.IsAutoInc).ToArray());

        public Column[] InsertOrReplaceColumns => _insertOrReplaceColumns ?? (_insertOrReplaceColumns = Columns.ToArray());

        private Dictionary<string, Column> ColumnNameIndex => _columnNameIndex ?? (_columnNameIndex = Columns.ToDictionary(x => x.Name));

        private Dictionary<string, Column> ColumnPropertyNameIndex => _columnPropertyNameIndex ?? (_columnPropertyNameIndex = Columns.ToDictionary(x => x.PropertyName));

        public Column FindColumnWithPropertyName(string propertyName)
        {
            Column col = null;
            ColumnPropertyNameIndex.TryGetValue(propertyName, out col);
            return col;
        }

        public Column FindColumn(string columnName)
        {
            Column col = null;
            ColumnNameIndex.TryGetValue(columnName, out col);
            return col;
        }

        public override string ToString() => TableName;

        private class DefaultTableMapper : ITableMapper
        {
            private static readonly Action<PropertyInfo, object, object> _setValue = (prop, obj, val) => prop.SetValue(obj, val, null);
            private static readonly Func<PropertyInfo, object, object> _getValue = (prop, obj) => prop.GetValue(obj, null);

            public List<TableMapping.Column> GetColumns(Type t, CreateFlags createFlags = CreateFlags.None)
            {
                var props = new List<PropertyInfo>();
                var baseType = t;
                var propNames = new HashSet<string>();
                while (baseType != typeof(object))
                {
                    var ti = baseType.GetTypeInfo();
                    var newProps = (
                        from p in ti.DeclaredProperties
                        where
                        !propNames.Contains(p.Name) &&
                        p.CanRead && p.CanWrite &&
                        (p.GetMethod != null) && (p.SetMethod != null) &&
                        (p.GetMethod.IsPublic && p.SetMethod.IsPublic) &&
                        (!p.GetMethod.IsStatic) && (!p.SetMethod.IsStatic)
                        select p).ToList();
                    foreach (var p in newProps)
                    {
                        propNames.Add(p.Name);
                    }
                    props.AddRange(newProps);
                    baseType = ti.BaseType;
                }

                var cols = new List<Column>();
                foreach (var p in props)
                {
			    	var ignore = p.GetCustomAttributes (typeof(IgnoreAttribute), true).Count() > 0;

                    if (p.CanWrite && !ignore)
                        cols.Add(new Column(p, _setValue, _getValue, createFlags));
                }

                return cols;
            }

            public object CreateInstance(Type t)
            {
                return Activator.CreateInstance(t);
            }

            public Type GetSchema(Type t) => t;
        }

        public class Column
        {
            private readonly PropertyInfo _prop;
            private readonly Action<PropertyInfo, object, object> _setValue;
            private readonly Func<PropertyInfo, object, object> _getValue;

            public string Name { get; }

            public string PropertyName { get; }

            public Type ColumnType { get; }

            public string Collation { get; }

            public bool IsAutoInc { get; }

            public bool IsAutoGuid { get; }

            public bool IsPK { get; }

            public IEnumerable<IndexedAttribute> Indices { get; }

            public bool IsNullable { get; }

			public int? MaxStringLength { get; }

			public bool StoreAsText { get; private set; }
			
            public Column(PropertyInfo prop, Action<PropertyInfo, object, object> setValue, Func<PropertyInfo, object, object> getValue, CreateFlags createFlags)
            {
                var colAttr = (ColumnAttribute)prop.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();

                _prop = prop;
                PropertyName = prop.Name;

                _setValue = setValue;
                _getValue = getValue;

                Name = colAttr == null ? prop.Name : colAttr.Name;
                //If this type is Nullable<T> then Nullable.GetUnderlyingType returns the T, otherwise it returns null, so get the actual type instead
                ColumnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                Collation = Orm.Collation(prop);

                IsPK = Orm.IsPK(prop) ||
                       (((createFlags & CreateFlags.ImplicitPK) == CreateFlags.ImplicitPK) &&
                        string.Compare(prop.Name, Orm.ImplicitPkName, StringComparison.OrdinalIgnoreCase) == 0);

                var isAuto = Orm.IsAutoInc(prop) || (IsPK && ((createFlags & CreateFlags.AutoIncPK) == CreateFlags.AutoIncPK));
                IsAutoGuid = isAuto && ColumnType == typeof(Guid);
                IsAutoInc = isAuto && !IsAutoGuid;

                Indices = Orm.GetIndices(prop);
                if (!Indices.Any()
                    && !IsPK
                    && ((createFlags & CreateFlags.ImplicitIndex) == CreateFlags.ImplicitIndex)
                    && Name.EndsWith(Orm.ImplicitIndexSuffix, StringComparison.OrdinalIgnoreCase)
                )
                {
                    Indices = new[] { new IndexedAttribute() };
                }
                IsNullable = !(IsPK || Orm.IsMarkedNotNull(prop));
                MaxStringLength = Orm.MaxStringLength(prop);
            }

            public void SetValue(object obj, object val) => _setValue(_prop, obj, val);

            public object GetValue(object obj) => _getValue(_prop, obj);

            public override string ToString() => $"{Name} ({ColumnType.Name})";
        }
    }

	class EnumCacheInfo
	{
		public EnumCacheInfo (Type type)
		{
			var typeInfo = type.GetTypeInfo ();

			IsEnum = typeInfo.IsEnum;

			if (IsEnum) {
				StoreAsText = typeInfo.CustomAttributes.Any (x => x.AttributeType == typeof (StoreAsTextAttribute));

				if (StoreAsText) {
					EnumValues = Enum.GetValues (type).Cast<object> ().ToDictionary (Convert.ToInt32, x => x.ToString ());
				}
				else {
					EnumValues = Enum.GetValues (type).Cast<object> ().ToDictionary (Convert.ToInt32, x => Convert.ToInt32 (x).ToString ());
				}
			}
		}

		public bool IsEnum { get; private set; }

		public bool StoreAsText { get; private set; }

		public Dictionary<int, string> EnumValues { get; private set; }
	}

	static class EnumCache
	{
		private static readonly Dictionary<Type, EnumCacheInfo> Cache = new Dictionary<Type, EnumCacheInfo> ();

		public static EnumCacheInfo GetInfo<T> ()
		{
			return GetInfo (typeof (T));
		}

		public static EnumCacheInfo GetInfo (Type type)
		{
			lock (Cache) {
				EnumCacheInfo info = null;
				if (!Cache.TryGetValue (type, out info)) {
					info = new EnumCacheInfo (type);
					Cache[type] = info;
				}

				return info;
			}
		}
	}

	public static class Orm
	{
		public const int DefaultMaxStringLength = 140;
		public const string ImplicitPkName = "Id";
		public const string ImplicitIndexSuffix = "Id";

		public static Type GetType (object obj)
		{
			if (obj == null)
				return typeof (object);
			var rt = obj as IReflectableType;
			if (rt != null)
				return rt.GetTypeInfo ().AsType ();
			return obj.GetType ();
		}

		public static string SqlDecl (TableMapping.Column p, bool storeDateTimeAsTicks)
		{
			string decl = "\"" + p.Name + "\" " + SqlType (p, storeDateTimeAsTicks) + " ";

			if (p.IsPK) {
				decl += "primary key ";
			}
			if (p.IsAutoInc) {
				decl += "autoincrement ";
			}
			if (!p.IsNullable) {
				decl += "not null ";
			}
			if (!string.IsNullOrEmpty (p.Collation)) {
				decl += "collate " + p.Collation + " ";
			}

			return decl;
		}

		public static string SqlType (TableMapping.Column p, bool storeDateTimeAsTicks)
		{
			var clrType = p.ColumnType;
			if (clrType == typeof (Boolean) || clrType == typeof (Byte) || clrType == typeof (UInt16) || clrType == typeof (SByte) || clrType == typeof (Int16) || clrType == typeof (Int32) || clrType == typeof (UInt32) || clrType == typeof (Int64)) {
				return "integer";
			}
			else if (clrType == typeof (Single) || clrType == typeof (Double) || clrType == typeof (Decimal)) {
				return "float";
			}
			else if (clrType == typeof (String)) {
				int? len = p.MaxStringLength;

				if (len.HasValue)
					return "varchar(" + len.Value + ")";

				return "varchar";
			}
			else if (clrType == typeof (TimeSpan)) {
				return "bigint";
			}
			else if (clrType == typeof (DateTime)) {
				return storeDateTimeAsTicks ? "bigint" : "datetime";
			}
			else if (clrType == typeof (DateTimeOffset)) {
				return "bigint";
			}
			else if (clrType.GetTypeInfo ().IsEnum) {
				if (p.StoreAsText)
					return "varchar";
				else
					return "integer";
			}
			else if (clrType == typeof (byte[])) {
				return "blob";
			}
			else if (clrType == typeof (Guid)) {
				return "varchar(36)";
			}
			else {
				throw new NotSupportedException ("Don't know about " + clrType);
			}
		}

		public static bool IsPK (MemberInfo p)
		{
			return p.CustomAttributes.Any (x => x.AttributeType == typeof (PrimaryKeyAttribute));
		}

		public static string Collation (MemberInfo p)
		{
			return
				(p.CustomAttributes
				 .Where (x => typeof (CollationAttribute) == x.AttributeType)
				 .Select (x => {
					 var args = x.ConstructorArguments;
					 return args.Count > 0 ? ((args[0].Value as string) ?? "") : "";
				 })
				 .FirstOrDefault ()) ?? "";
		}

		public static bool IsAutoInc (MemberInfo p)
		{
			return p.CustomAttributes.Any (x => x.AttributeType == typeof (AutoIncrementAttribute));
		}

		public static FieldInfo GetField (TypeInfo t, string name)
		{
			var f = t.GetDeclaredField (name);
			if (f != null)
				return f;
			return GetField (t.BaseType.GetTypeInfo (), name);
		}

		public static PropertyInfo GetProperty (TypeInfo t, string name)
		{
			var f = t.GetDeclaredProperty (name);
			if (f != null)
				return f;
			return GetProperty (t.BaseType.GetTypeInfo (), name);
		}

		public static object InflateAttribute (CustomAttributeData x)
		{
			var atype = x.AttributeType;
			var typeInfo = atype.GetTypeInfo ();
			var args = x.ConstructorArguments.Select (a => a.Value).ToArray ();
			var r = Activator.CreateInstance (x.AttributeType, args);
			foreach (var arg in x.NamedArguments) {
				if (arg.IsField) {
					GetField (typeInfo, arg.MemberName).SetValue (r, arg.TypedValue.Value);
				}
				else {
					GetProperty (typeInfo, arg.MemberName).SetValue (r, arg.TypedValue.Value);
				}
			}
			return r;
		}

		public static IEnumerable<IndexedAttribute> GetIndices (MemberInfo p)
		{
			var indexedInfo = typeof (IndexedAttribute).GetTypeInfo ();
			return
				p.CustomAttributes
				 .Where (x => indexedInfo.IsAssignableFrom (x.AttributeType.GetTypeInfo ()))
				 .Select (x => (IndexedAttribute)InflateAttribute (x));
		}

		public static int? MaxStringLength (PropertyInfo p)
		{
			var attr = p.CustomAttributes.FirstOrDefault (x => x.AttributeType == typeof (MaxLengthAttribute));
			if (attr != null) {
				var attrv = (MaxLengthAttribute)InflateAttribute (attr);
				return attrv.Value;
			}
			return null;
		}

		public static bool IsMarkedNotNull (MemberInfo p)
		{
			return p.CustomAttributes.Any (x => x.AttributeType == typeof (NotNullAttribute));
		}
	}

    /// <summary>
    /// Since the insert or update never changed, we only need to prepare once.
    /// </summary>
    public class SQLiteCommand : IDisposable, IEquatable<SQLiteCommand>
    {
        private readonly static IntPtr NegativePointer = new IntPtr(-1);

        private const string DateTimeExactStoreFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff";


        private bool _disposed;

        public bool Prepared { get; private set; }

        protected SQLiteConnection Connection { get; private set; }

        public string CommandText { get; set; }

        protected Sqlite3Statement Statement { get; set; }
        internal static readonly Sqlite3Statement NullStatement = default(Sqlite3Statement);

		internal SQLiteCommand (SQLiteConnection conn)
		{
            Connection = conn;
        }

        internal SQLiteCommand(SQLiteConnection conn, string commandText)
        {
            Connection = conn;
            CommandText = commandText;
        }

        /// <summary>
        /// Invoked every time an instance is loaded from the database.
        /// </summary>
        /// <param name='obj'>
        /// The newly created object.
        /// </param>
        /// <remarks>
        /// This can be overridden in combination with the <see cref="SQLiteConnection.NewCommand"/>
        /// method to hook into the life-cycle of objects.
        ///
        /// Type safety is not possible because MonoTouch does not support virtual generic methods.
        /// </remarks>
        protected virtual void OnInstanceCreated(object obj)
        {
            // Can be overridden.
        }

	    public IEnumerable<T> ExecuteQuery<T> ()
	    {
		    return ExecuteQuery<T> (Connection.GetMapping<T> (), null);
	    }

	    public IEnumerable<T> ExecuteQuery<T>(object[] source)
        {
            return ExecuteQuery<T>(Connection.GetMapping<T>(), source);
        }

        public IEnumerable<T> ExecuteQuery<T>(TableMapping map, object[] source)
        {
            CheckDisposed();
            Log(nameof(Execute), source);

            var sw = Stopwatch.StartNew();
            try
            {
                var r = SQLite3.Result.OK;

                if (!Prepared)
                {
                    Statement = Prepare();
                    Prepared = true;
                }

                //bind the values.
                if (source != null)
                    for (int i = 0; i < source.Length; i++)
                        BindParameter(Statement, i + 1, source[i], Connection.StoreDateTimeAsTicks);

                var cols = new TableMapping.Column[SQLite3.ColumnCount(Statement)];

                for (int i = 0; i < cols.Length; i++)
                {
                    var name = SQLite3.ColumnName16(Statement, i);
                    cols[i] = map.FindColumn(name);
                }

                while (SQLite3.Step(Statement) == SQLite3.Result.Row)
                {
                    var obj = map.CreateInstance();
                    for (int i = 0; i < cols.Length; i++)
                    {
                        if (cols[i] == null)
                            continue;
                        var colType = SQLite3.ColumnType(Statement, i);
                        var val = ReadCol(Statement, i, colType, cols[i].ColumnType);
                        cols[i].SetValue(obj, val);
                    }
                    OnInstanceCreated(obj);
                    yield return (T) obj;
                }

                if (r == SQLite3.Result.Done || r == SQLite3.Result.OK) { }
                else
                {
                    var msg = SQLite3.GetErrorMessage(Connection.Handle);

                    if (r == SQLite3.Result.Constraint && SQLite3.ExtendedErrCode(Connection.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
                        throw NotNullConstraintViolationException.New(r, msg, sql: CommandText);

                    throw SQLiteException.New(r, msg, sql: CommandText);
                }
            }
            finally
            {
                SQLite3.Reset(Statement);

                sw.Stop();
                Log(sw);
            }
        }

	    public T ExecuteScalar<T> ()
	    {
		    return ExecuteScalar<T> (null);
	    }

		public T ExecuteScalar<T>(object[] source)
        {
            CheckDisposed();
            Log(nameof(ExecuteScalar), source);

            var sw = Stopwatch.StartNew();
            T val = default(T);

            try
            {
                var r = InternalExecute(source);

                if (r == SQLite3.Result.Row)
                {
                    var colType = SQLite3.ColumnType(Statement, 0);
                    val = (T) ReadCol(Statement, 0, colType, typeof(T));
                }
                else if (r == SQLite3.Result.Done || r == SQLite3.Result.OK) { }
            }
            finally
            {
                SQLite3.Reset(Statement);

                sw.Stop();
                Log(sw);
            }

			return val;
		}

	    public int ExecuteNonQuery ()
	    {
		    return ExecuteNonQuery (null);
	    }

	    public int ExecuteNonQuery(object[] source)
        {
            CheckDisposed();
            Log(nameof(ExecuteNonQuery), source);

            var sw = Stopwatch.StartNew();
            try
            {
                var r = InternalExecute(source);

                if (r == SQLite3.Result.Done || r == SQLite3.Result.OK || r == SQLite3.Result.Row)
                {
                    int rowsAffected = SQLite3.Changes(Connection.Handle);
                    return rowsAffected;
                }

                return 0;
            }
            finally
            {
                SQLite3.Reset(Statement);

                sw.Stop();
                Log(sw);
            }
        }

	    public SQLite3.Result Execute ()
	    {
		    return Execute (null);
	    }

		public SQLite3.Result Execute(object[] source)
        {
            CheckDisposed();
            Log(nameof(Execute), source);

            var sw = Stopwatch.StartNew();
            try
            {
                return InternalExecute(source);
            }
            finally
            {
                SQLite3.Reset(Statement);

                sw.Stop();
                Log(sw);
            }
        }

        private SQLite3.Result InternalExecute(object[] source)
        {

            var r = SQLite3.Result.OK;

            if (!Prepared)
            {
                Statement = Prepare();
                Prepared = true;
            }

            //bind the values.
            if (source != null)
                for (int i = 0; i < source.Length; i++)
                    BindParameter(Statement, i + 1, source[i], Connection.StoreDateTimeAsTicks);

            r = SQLite3.Step(Statement);

            if (r == SQLite3.Result.Done || r == SQLite3.Result.OK || r == SQLite3.Result.Row)
            {
                return r;
            }
            else
            {
                var msg = SQLite3.GetErrorMessage(Connection.Handle);

                if (r == SQLite3.Result.Constraint && SQLite3.ExtendedErrCode(Connection.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
                    throw NotNullConstraintViolationException.New(r, msg, sql: CommandText);

                throw SQLiteException.New(r, msg, sql: CommandText);
            }
        }

        private void Log(Stopwatch sw)
        {
            if (Connection.Trace && sw.Elapsed > Connection.TraceTimeExceeding)
            {
                Connection.Tracer($"Database took {sw.ElapsedMilliseconds} ms to execute: {CommandText}");
            }
        }

        private void Log(string method, object[] source)
        {
            if (Connection.Trace)
            {
                var parts = new List<string>();
                parts.Add(CommandText);

                if (source != null)
                {
                    var index = 0;
                    foreach (var b in source)
                        parts.Add($"  {index++}: {b}");
                }

                Connection.Tracer($"{method}: {String.Join(Environment.NewLine, parts)}");
            }
        }

        protected virtual Sqlite3Statement Prepare()
		{
			var stmt = SQLite3.Prepare2(Connection.Handle, CommandText);
			return stmt;
		}

		private object ReadCol (Sqlite3Statement stmt, int index, SQLite3.ColType type, Type clrType)
		{
			if (type == SQLite3.ColType.Null) {
				return null;
			}
			else {
				var clrTypeInfo = clrType.GetTypeInfo ();
				if (clrType == typeof (String)) {
					return SQLite3.ColumnString (stmt, index);
				}
				else if (clrType == typeof (Int32)) {
					return (int)SQLite3.ColumnInt (stmt, index);
				}
				else if (clrType == typeof (Boolean)) {
					return SQLite3.ColumnInt (stmt, index) == 1;
				}
				else if (clrType == typeof (double)) {
					return SQLite3.ColumnDouble (stmt, index);
				}
				else if (clrType == typeof (float)) {
					return (float)SQLite3.ColumnDouble (stmt, index);
				}
				else if (clrType == typeof (TimeSpan)) {
					return new TimeSpan (SQLite3.ColumnInt64 (stmt, index));
				}
				else if (clrType == typeof (DateTime)) {
					if (Connection.StoreDateTimeAsTicks) {
						return new DateTime (SQLite3.ColumnInt64 (stmt, index));
					}
					else {
						var text = SQLite3.ColumnString (stmt, index);
						DateTime resultDate;
						if (!DateTime.TryParseExact (text, DateTimeExactStoreFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDate)) {
							resultDate = DateTime.Parse (text);
						}
						return resultDate;
					}
				}
				else if (clrType == typeof (DateTimeOffset)) {
					return new DateTimeOffset (SQLite3.ColumnInt64 (stmt, index), TimeSpan.Zero);
				}
				else if (clrTypeInfo.IsEnum) {
					if (type == SQLite3.ColType.Text) {
						var value = SQLite3.ColumnString (stmt, index);
						return Enum.Parse (clrType, value.ToString (), true);
					}
					else
						return SQLite3.ColumnInt (stmt, index);
				}
				else if (clrType == typeof (Int64)) {
					return SQLite3.ColumnInt64 (stmt, index);
				}
				else if (clrType == typeof (UInt32)) {
					return (uint)SQLite3.ColumnInt64 (stmt, index);
				}
				else if (clrType == typeof (decimal)) {
					return (decimal)SQLite3.ColumnDouble (stmt, index);
				}
				else if (clrType == typeof (Byte)) {
					return (byte)SQLite3.ColumnInt (stmt, index);
				}
				else if (clrType == typeof (UInt16)) {
					return (ushort)SQLite3.ColumnInt (stmt, index);
				}
				else if (clrType == typeof (Int16)) {
					return (short)SQLite3.ColumnInt (stmt, index);
				}
				else if (clrType == typeof (sbyte)) {
					return (sbyte)SQLite3.ColumnInt (stmt, index);
				}
				else if (clrType == typeof (byte[])) {
					return SQLite3.ColumnByteArray (stmt, index);
				}
				else if (clrType == typeof (Guid)) {
					var text = SQLite3.ColumnString (stmt, index);
					return new Guid (text);
				}
				else {
					throw new NotSupportedException ("Don't know how to read " + clrType);
				}
			}
		}

		private static void BindParameter (Sqlite3Statement stmt, int index, object value, bool storeDateTimeAsTicks)
		{
			if (value == null) {
				SQLite3.BindNull (stmt, index);
			}
			else {
				if (value is Int32) {
					SQLite3.BindInt (stmt, index, (int)value);
				}
				else if (value is String) {
					SQLite3.BindText (stmt, index, (string)value, -1, NegativePointer);
				}
				else if (value is Byte || value is UInt16 || value is SByte || value is Int16) {
					SQLite3.BindInt (stmt, index, Convert.ToInt32 (value));
				}
				else if (value is Boolean) {
					SQLite3.BindInt (stmt, index, (bool)value ? 1 : 0);
				}
				else if (value is UInt32 || value is Int64) {
					SQLite3.BindInt64 (stmt, index, Convert.ToInt64 (value));
				}
				else if (value is Single || value is Double || value is Decimal) {
					SQLite3.BindDouble (stmt, index, Convert.ToDouble (value));
				}
				else if (value is TimeSpan) {
					SQLite3.BindInt64 (stmt, index, ((TimeSpan)value).Ticks);
				}
				else if (value is DateTime) {
					if (storeDateTimeAsTicks) {
						SQLite3.BindInt64 (stmt, index, ((DateTime)value).Ticks);
					}
					else {
						SQLite3.BindText (stmt, index, ((DateTime)value).ToString (DateTimeExactStoreFormat, System.Globalization.CultureInfo.InvariantCulture), -1, NegativePointer);
					}
				}
				else if (value is DateTimeOffset) {
					SQLite3.BindInt64 (stmt, index, ((DateTimeOffset)value).UtcTicks);
				}
				else if (value is byte[]) {
					SQLite3.BindBlob (stmt, index, (byte[])value, ((byte[])value).Length, NegativePointer);
				}
				else if (value is Guid) {
					SQLite3.BindText (stmt, index, ((Guid)value).ToString (), 72, NegativePointer);
				}
				else {
					// Now we could possibly get an enum, retrieve cached info
					var valueType = value.GetType ();
					var enumInfo = EnumCache.GetInfo (valueType);
					if (enumInfo.IsEnum) {
						var enumIntValue = Convert.ToInt32 (value);
						if (enumInfo.StoreAsText)
							SQLite3.BindText (stmt, index, enumInfo.EnumValues[enumIntValue], -1, NegativePointer);
						else
							SQLite3.BindInt (stmt, index, enumIntValue);
					}
					else {
						throw new NotSupportedException ("Cannot store type: " + Orm.GetType (value));
					}
				}
			}
		}


	    public void ResetStatement()
	    {
	        try
	        {
	            SQLite3.Finalize(Statement);
	        }
	        finally
	        {
	            Statement = NullStatement;
	            Prepared = false;
	        }
	    }

        public void Dispose()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

	    private void CheckDisposed()
	    {
	        if (_disposed)
	            throw new ObjectDisposedException(nameof(SQLiteCommand), $"Disposed Query: {CommandText}");
        }

	    private void Dispose(bool disposing)
	    {
	        if (!_disposed && Statement != NullStatement)
	        {
	            _disposed = true;

	            ResetStatement();
	            Connection = null;
	        }
	    }

	    ~SQLiteCommand()
		{
			Dispose(false);
		}

	    public override string ToString() => CommandText;

	    public bool Equals(SQLiteCommand other)
	    {
	        return string.Equals(CommandText, other.CommandText);
	    }

	    public override bool Equals(object obj)
	    {
	        if (ReferenceEquals(null, obj)) return false;
	        if (ReferenceEquals(this, obj)) return true;
	        if (obj.GetType() != this.GetType()) return false;
	        return Equals((SQLiteCommand)obj);
	    }

	    public override int GetHashCode()
	    {
	        return (CommandText != null ? CommandText.GetHashCode() : 0);
	    }

        public static bool operator ==(SQLiteCommand left, SQLiteCommand right)
	    {
	        return Equals(left, right);
	    }

	    public static bool operator !=(SQLiteCommand left, SQLiteCommand right)
	    {
	        return !Equals(left, right);
	    }
    }

	public enum CreateTableResult
	{
		Created,
		Migrated,
		Error
	}

	public class CreateTablesResult
	{
		public Dictionary<Type, CreateTableResult> Results { get; private set; }

		public CreateTablesResult ()
		{
			Results = new Dictionary<Type, CreateTableResult> ();
		}
	}

	public abstract class BaseTableQuery
	{
		protected class Ordering
		{
			public string ColumnName { get; set; }
			public bool Ascending { get; set; }
		}
	}

	public class TableQuery<T> : BaseTableQuery, IEnumerable<T>
	{
		public SQLiteConnection Connection { get; private set; }

		public TableMapping Table { get; private set; }

		Expression _where;
		List<Ordering> _orderBys;
		int? _limit;
		int? _offset;

		BaseTableQuery _joinInner;
		Expression _joinInnerKeySelector;
		BaseTableQuery _joinOuter;
		Expression _joinOuterKeySelector;
		Expression _joinSelector;

		Expression _selector;

		TableQuery (SQLiteConnection conn, TableMapping table)
		{
			Connection = conn;
			Table = table;
		}

		public TableQuery (SQLiteConnection conn)
		{
			Connection = conn;
			Table = Connection.GetMapping (typeof (T));
		}

		public TableQuery<U> Clone<U> ()
		{
			var q = new TableQuery<U> (Connection, Table);
			q._where = _where;
			q._deferred = _deferred;
			if (_orderBys != null) {
				q._orderBys = new List<Ordering> (_orderBys);
			}
			q._limit = _limit;
			q._offset = _offset;
			q._joinInner = _joinInner;
			q._joinInnerKeySelector = _joinInnerKeySelector;
			q._joinOuter = _joinOuter;
			q._joinOuterKeySelector = _joinOuterKeySelector;
			q._joinSelector = _joinSelector;
			q._selector = _selector;
			return q;
		}

		/// <summary>
		/// Filters the query based on a predicate.
		/// </summary>
		public TableQuery<T> Where (Expression<Func<T, bool>> predExpr)
		{
			if (predExpr.NodeType == ExpressionType.Lambda) {
				var lambda = (LambdaExpression)predExpr;
				var pred = lambda.Body;
				var q = Clone<T> ();
				q.AddWhere (pred);
				return q;
			}
			else {
				throw new NotSupportedException ("Must be a predicate");
			}
		}

		/// <summary>
		/// Delete all the rows that match this query.
		/// </summary>
		public int Delete ()
		{
			return Delete (null);
		}

		/// <summary>
		/// Delete all the rows that match this query and the given predicate.
		/// </summary>
		public int Delete (Expression<Func<T, bool>> predExpr)
		{
			if (_limit.HasValue || _offset.HasValue)
				throw new InvalidOperationException ("Cannot delete with limits or offsets");

			if (_where == null && predExpr == null)
				throw new InvalidOperationException ("No condition specified");

			var pred = _where;

			if (predExpr != null && predExpr.NodeType == ExpressionType.Lambda) {
				var lambda = (LambdaExpression)predExpr;
				pred = pred != null ? Expression.AndAlso (pred, lambda.Body) : lambda.Body;
			}

			var args = new List<object> ();
			var cmdText = "delete from \"" + Table.TableName + "\"";
			var w = CompileExpr (pred, args);
			cmdText += " where " + w.CommandText;

		    using (var command = new SQLiteCommand(Connection, cmdText))
		    {
                var result = command.ExecuteNonQuery(args.ToArray());
		        return result;
		    }
		}

		/// <summary>
		/// Yields a given number of elements from the query and then skips the remainder.
		/// </summary>
		public TableQuery<T> Take (int n)
		{
			var q = Clone<T> ();
			q._limit = n;
			return q;
		}

		/// <summary>
		/// Skips a given number of elements from the query and then yields the remainder.
		/// </summary>
		public TableQuery<T> Skip (int n)
		{
			var q = Clone<T> ();
			q._offset = n;
			return q;
		}

		/// <summary>
		/// Returns the element at a given index
		/// </summary>
		public T ElementAt (int index)
		{
			return Skip (index).Take (1).First ();
		}

		bool _deferred;
		public TableQuery<T> Deferred ()
		{
			var q = Clone<T> ();
			q._deferred = true;
			return q;
		}

		/// <summary>
		/// Order the query results according to a key.
		/// </summary>
		public TableQuery<T> OrderBy<U> (Expression<Func<T, U>> orderExpr)
		{
			return AddOrderBy<U> (orderExpr, true);
		}

		/// <summary>
		/// Order the query results according to a key.
		/// </summary>
		public TableQuery<T> OrderByDescending<U> (Expression<Func<T, U>> orderExpr)
		{
			return AddOrderBy<U> (orderExpr, false);
		}

		/// <summary>
		/// Order the query results according to a key.
		/// </summary>
		public TableQuery<T> ThenBy<U> (Expression<Func<T, U>> orderExpr)
		{
			return AddOrderBy<U> (orderExpr, true);
		}

		/// <summary>
		/// Order the query results according to a key.
		/// </summary>
		public TableQuery<T> ThenByDescending<U> (Expression<Func<T, U>> orderExpr)
		{
			return AddOrderBy<U> (orderExpr, false);
		}

		private TableQuery<T> AddOrderBy<U>(Expression<Func<T, U>> orderExpr, bool asc)
		{
			if (orderExpr.NodeType == ExpressionType.Lambda) {
				var lambda = (LambdaExpression)orderExpr;

				MemberExpression mem = null;

				var unary = lambda.Body as UnaryExpression;
				if (unary != null && unary.NodeType == ExpressionType.Convert) {
					mem = unary.Operand as MemberExpression;
				}
				else {
					mem = lambda.Body as MemberExpression;
				}

				if (mem != null && (mem.Expression.NodeType == ExpressionType.Parameter)) {
					var q = Clone<T> ();
					if (q._orderBys == null) {
						q._orderBys = new List<Ordering> ();
					}
					q._orderBys.Add (new Ordering {
						ColumnName = Table.FindColumnWithPropertyName (mem.Member.Name).Name,
						Ascending = asc
					});
					return q;
				}
				else {
					throw new NotSupportedException ("Order By does not support: " + orderExpr);
				}
			}
			else {
				throw new NotSupportedException ("Must be a predicate");
			}
		}

		private void AddWhere (Expression pred)
		{
			if (_where == null) {
				_where = pred;
			}
			else {
				_where = Expression.AndAlso (_where, pred);
			}
		}

		///// <summary>
		///// Performs an inner join of two queries based on matching keys extracted from the elements.
		///// </summary>
		//public TableQuery<TResult> Join<TInner, TKey, TResult> (
		//	TableQuery<TInner> inner,
		//	Expression<Func<T, TKey>> outerKeySelector,
		//	Expression<Func<TInner, TKey>> innerKeySelector,
		//	Expression<Func<T, TInner, TResult>> resultSelector)
		//{
		//	var q = new TableQuery<TResult> (Connection, Connection.GetMapping (typeof (TResult))) {
		//		_joinOuter = this,
		//		_joinOuterKeySelector = outerKeySelector,
		//		_joinInner = inner,
		//		_joinInnerKeySelector = innerKeySelector,
		//		_joinSelector = resultSelector,
		//	};
		//	return q;
		//}

		// Not needed until Joins are supported
		// Keeping this commented out forces the default Linq to objects processor to run
		//public TableQuery<TResult> Select<TResult> (Expression<Func<T, TResult>> selector)
		//{
		//	var q = Clone<TResult> ();
		//	q._selector = selector;
		//	return q;
		//}

		private Tuple<string, object[]> GetCommandText(string selectionList)
		{
			if (_joinInner != null && _joinOuter != null) {
				throw new NotSupportedException ("Joins are not supported.");
			}
			else {
				var cmdText = "select " + selectionList + " from \"" + Table.TableName + "\"";
				var args = new List<object> ();
				if (_where != null) {
					var w = CompileExpr (_where, args);
					cmdText += " where " + w.CommandText;
				}
				if ((_orderBys != null) && (_orderBys.Count > 0)) {
					var t = string.Join (", ", _orderBys.Select (o => "\"" + o.ColumnName + "\"" + (o.Ascending ? "" : " desc")).ToArray ());
					cmdText += " order by " + t;
				}
				if (_limit.HasValue) {
					cmdText += " limit " + _limit.Value;
				}
				if (_offset.HasValue) {
					if (!_limit.HasValue) {
						cmdText += " limit -1 ";
					}
					cmdText += " offset " + _offset.Value;
				}

#if DEBUG_BROAD_QUERY_CHECK
				var queryTooBroadCheck = "select " + selectionList + " from \"" + Table.TableName + "\"";
				if (cmdText == queryTooBroadCheck)
					Debugger.Break();
#endif

			    return new Tuple<string, object[]>(cmdText, args.ToArray());
			}
        }

	    private TResult ExecuteCommand<TResult>(string selectionList, Func<SQLiteCommand, object[], TResult> exec)
	    {
	        var command = GetCommandText(selectionList);
	        using (var cmd = new SQLiteCommand(Connection, command.Item1))
	            return exec(cmd, command.Item2);
	    }

	    private SQLiteCommandEnumerator<T> GetCommandEnumerator(string selectionList, Func<SQLiteCommand, object[], IEnumerable<T>> exec)
	    {
	        var command = GetCommandText(selectionList);
	        var cmd = new SQLiteCommand(Connection, command.Item1);
	        return new SQLiteCommandEnumerator<T>(cmd, exec(cmd, command.Item2).GetEnumerator());
	    }

        private class SQLiteCommandEnumerator<TResult> : IEnumerator<TResult>
	    {
	        private readonly SQLiteCommand _cmd;
	        private readonly IEnumerator<TResult> _e;

	        public SQLiteCommandEnumerator(SQLiteCommand cmd, IEnumerator<TResult> e)
	        {
	            _cmd = cmd;
	            _e = e;
	        }

	        public bool MoveNext() => _e.MoveNext();

	        public void Reset() => _e.Reset();

	        public TResult Current => _e.Current;

	        object IEnumerator.Current => Current;

	        public void Dispose()
	        {
	            _e.Dispose();
                _cmd.Dispose();
	        }
	    }

        private class CompileResult
		{
			public string CommandText { get; set; }

			public object Value { get; set; }
		}

		private CompileResult CompileExpr (Expression expr, List<object> queryArgs)
		{
			if (expr == null) {
				throw new NotSupportedException ("Expression is NULL");
			}
			else if (expr is BinaryExpression) {
				var bin = (BinaryExpression)expr;

				// VB turns 'x=="foo"' into 'CompareString(x,"foo",true/false)==0', so we need to unwrap it
				// http://blogs.msdn.com/b/vbteam/archive/2007/09/18/vb-expression-trees-string-comparisons.aspx
				if (bin.Left.NodeType == ExpressionType.Call) {
					var call = (MethodCallExpression)bin.Left;
					if (call.Method.DeclaringType.FullName == "Microsoft.VisualBasic.CompilerServices.Operators"
						&& call.Method.Name == "CompareString")
						bin = Expression.MakeBinary (bin.NodeType, call.Arguments[0], call.Arguments[1]);
				}


				var leftr = CompileExpr (bin.Left, queryArgs);
				var rightr = CompileExpr (bin.Right, queryArgs);

				//If either side is a parameter and is null, then handle the other side specially (for "is null"/"is not null")
				string text;
				if (leftr.CommandText == "?" && leftr.Value == null)
					text = CompileNullBinaryExpression (bin, rightr);
				else if (rightr.CommandText == "?" && rightr.Value == null)
					text = CompileNullBinaryExpression (bin, leftr);
				else
					text = "(" + leftr.CommandText + " " + GetSqlName (bin) + " " + rightr.CommandText + ")";
				return new CompileResult { CommandText = text };
			}
			else if (expr.NodeType == ExpressionType.Not) {
				var operandExpr = ((UnaryExpression)expr).Operand;
				var opr = CompileExpr (operandExpr, queryArgs);
				object val = opr.Value;
				if (val is bool)
					val = !((bool)val);
				return new CompileResult {
					CommandText = "NOT(" + opr.CommandText + ")",
					Value = val
				};
			}
			else if (expr.NodeType == ExpressionType.Call) {

				var call = (MethodCallExpression)expr;
				var args = new CompileResult[call.Arguments.Count];
				var obj = call.Object != null ? CompileExpr (call.Object, queryArgs) : null;

				for (var i = 0; i < args.Length; i++) {
					args[i] = CompileExpr (call.Arguments[i], queryArgs);
				}

				var sqlCall = "";

				if (call.Method.Name == "Like" && args.Length == 2) {
					sqlCall = "(" + args[0].CommandText + " like " + args[1].CommandText + ")";
				}
				else if (call.Method.Name == "Contains" && args.Length == 2) {
					sqlCall = "(" + args[1].CommandText + " in " + args[0].CommandText + ")";
				}
				else if (call.Method.Name == "Contains" && args.Length == 1) {
					if (call.Object != null && call.Object.Type == typeof (string)) {
						sqlCall = "( instr(" + obj.CommandText + "," + args[0].CommandText + ") >0 )";
					}
					else {
						sqlCall = "(" + args[0].CommandText + " in " + obj.CommandText + ")";
					}
				}
				else if (call.Method.Name == "StartsWith" && args.Length >= 1) {
					var startsWithCmpOp = StringComparison.CurrentCulture;
					if (args.Length == 2) {
						startsWithCmpOp = (StringComparison)args[1].Value;
					}
					switch (startsWithCmpOp) {
						case StringComparison.Ordinal:
						case StringComparison.CurrentCulture:
							sqlCall = "( substr(" + obj.CommandText + ", 1, " + args[0].Value.ToString ().Length + ") =  " + args[0].CommandText + ")";
							break;
						case StringComparison.OrdinalIgnoreCase:
						case StringComparison.CurrentCultureIgnoreCase:
							sqlCall = "(" + obj.CommandText + " like (" + args[0].CommandText + " || '%'))";
							break;
					}

				}
				else if (call.Method.Name == "EndsWith" && args.Length >= 1) {
					var endsWithCmpOp = StringComparison.CurrentCulture;
					if (args.Length == 2) {
						endsWithCmpOp = (StringComparison)args[1].Value;
					}
					switch (endsWithCmpOp) {
						case StringComparison.Ordinal:
						case StringComparison.CurrentCulture:
							sqlCall = "( substr(" + obj.CommandText + ", length(" + obj.CommandText + ") - " + args[0].Value.ToString ().Length + "+1, " + args[0].Value.ToString ().Length + ") =  " + args[0].CommandText + ")";
							break;
						case StringComparison.OrdinalIgnoreCase:
						case StringComparison.CurrentCultureIgnoreCase:
							sqlCall = "(" + obj.CommandText + " like ('%' || " + args[0].CommandText + "))";
							break;
					}
				}
				else if (call.Method.Name == "Equals" && args.Length == 1) {
					sqlCall = "(" + obj.CommandText + " = (" + args[0].CommandText + "))";
				}
				else if (call.Method.Name == "ToLower") {
					sqlCall = "(lower(" + obj.CommandText + "))";
				}
				else if (call.Method.Name == "ToUpper") {
					sqlCall = "(upper(" + obj.CommandText + "))";
				}
				else {
					sqlCall = call.Method.Name.ToLower () + "(" + string.Join (",", args.Select (a => a.CommandText).ToArray ()) + ")";
				}
				return new CompileResult { CommandText = sqlCall };

			}
			else if (expr.NodeType == ExpressionType.Constant) {
				var c = (ConstantExpression)expr;
				queryArgs.Add (c.Value);
				return new CompileResult {
					CommandText = "?",
					Value = c.Value
				};
			}
			else if (expr.NodeType == ExpressionType.Convert) {
				var u = (UnaryExpression)expr;
				var ty = u.Type;
				var valr = CompileExpr (u.Operand, queryArgs);
				return new CompileResult {
					CommandText = valr.CommandText,
					Value = valr.Value != null ? ConvertTo (valr.Value, ty) : null
				};
			}
			else if (expr.NodeType == ExpressionType.MemberAccess) {
				var mem = (MemberExpression)expr;

				var paramExpr = mem.Expression as ParameterExpression;
				if (paramExpr == null) {
					var convert = mem.Expression as UnaryExpression;
					if (convert != null && convert.NodeType == ExpressionType.Convert) {
						paramExpr = convert.Operand as ParameterExpression;
					}
				}

				if (paramExpr != null) {
					//
					// This is a column of our table, output just the column name
					// Need to translate it if that column name is mapped
					//
					var columnName = Table.FindColumnWithPropertyName (mem.Member.Name).Name;
					return new CompileResult { CommandText = "\"" + columnName + "\"" };
				}
				else {
					object obj = null;
					if (mem.Expression != null) {
						var r = CompileExpr (mem.Expression, queryArgs);
						if (r.Value == null) {
							throw new NotSupportedException ("Member access failed to compile expression");
						}
						if (r.CommandText == "?") {
							queryArgs.RemoveAt (queryArgs.Count - 1);
						}
						obj = r.Value;
					}

					//
					// Get the member value
					//
					object val = null;

					if (mem.Member is PropertyInfo) {
						var m = (PropertyInfo)mem.Member;
						val = m.GetValue (obj, null);
					}
					else if (mem.Member is FieldInfo) {
						var m = (FieldInfo)mem.Member;
						val = m.GetValue (obj);
					}
					else {
						throw new NotSupportedException ("MemberExpr: " + mem.Member.GetType ());
					}

					//
					// Work special magic for enumerables
					//
					if (val != null && val is System.Collections.IEnumerable && !(val is string) && !(val is System.Collections.Generic.IEnumerable<byte>)) {
						var sb = new System.Text.StringBuilder ();
						sb.Append ("(");
						var head = "";
						foreach (var a in (System.Collections.IEnumerable)val) {
							queryArgs.Add (a);
							sb.Append (head);
							sb.Append ("?");
							head = ",";
						}
						sb.Append (")");
						return new CompileResult {
							CommandText = sb.ToString (),
							Value = val
						};
					}
					else {
						queryArgs.Add (val);
						return new CompileResult {
							CommandText = "?",
							Value = val
						};
					}
				}
			}
			throw new NotSupportedException ("Cannot compile: " + expr.NodeType.ToString ());
		}

		static object ConvertTo (object obj, Type t)
		{
			Type nut = Nullable.GetUnderlyingType (t);

			if (nut != null) {
				if (obj == null) return null;
				return Convert.ChangeType (obj, nut);
			}
			else {
				return Convert.ChangeType (obj, t);
			}
		}

		/// <summary>
		/// Compiles a BinaryExpression where one of the parameters is null.
		/// </summary>
		/// <param name="expression">The expression to compile</param>
		/// <param name="parameter">The non-null parameter</param>
		private string CompileNullBinaryExpression (BinaryExpression expression, CompileResult parameter)
		{
			if (expression.NodeType == ExpressionType.Equal)
				return "(" + parameter.CommandText + " is ?)";
			else if (expression.NodeType == ExpressionType.NotEqual)
				return "(" + parameter.CommandText + " is not ?)";
			else
				throw new NotSupportedException ("Cannot compile Null-BinaryExpression with type " + expression.NodeType.ToString ());
		}

		string GetSqlName (Expression expr)
		{
			var n = expr.NodeType;
			if (n == ExpressionType.GreaterThan)
				return ">";
			else if (n == ExpressionType.GreaterThanOrEqual) {
				return ">=";
			}
			else if (n == ExpressionType.LessThan) {
				return "<";
			}
			else if (n == ExpressionType.LessThanOrEqual) {
				return "<=";
			}
			else if (n == ExpressionType.And) {
				return "&";
			}
			else if (n == ExpressionType.AndAlso) {
				return "and";
			}
			else if (n == ExpressionType.Or) {
				return "|";
			}
			else if (n == ExpressionType.OrElse) {
				return "or";
			}
			else if (n == ExpressionType.Equal) {
				return "=";
			}
			else if (n == ExpressionType.NotEqual) {
				return "!=";
			} else {
				throw new NotSupportedException ("Cannot get SQL for: " + n);
			}
		}
		
		public bool Any()
		{
            // For example, the below line will generate a SQL query like:
            // select 1 from files where uuid = ? limit 1;
            // for conn.Table<FilesSyncRecord>().Where(x => x.Uuid == uuid).Any();
		    return Take(1).ExecuteCommand("1", (cmd, args) => cmd.ExecuteScalar<int>(args)) > 0;
		}

		public bool Any(Expression<Func<T, bool>> predExpr)
		{
			return Where(predExpr).Any();
		}

		public bool All()
		{
			throw new NotSupportedException();
		}

		public bool All(Expression<Func<T, bool>> predExpr)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Execute SELECT COUNT(*) on the query
		/// </summary>
		public int Count ()
		{
		    return ExecuteCommand("count(*)", (cmd, args) => cmd.ExecuteScalar<int>(args));
		}

		/// <summary>
		/// Execute SELECT COUNT(*) on the query with an additional WHERE clause.
		/// </summary>
		public int Count (Expression<Func<T, bool>> predExpr)
		{
			return Where (predExpr).Count ();
		}

		public IEnumerator<T> GetEnumerator ()
		{
		    if (!_deferred)
		        return GetCommandEnumerator("*", (cmd, args) => cmd.ExecuteQuery<T>(args));

            return ExecuteCommand("*", (cmd, args) => cmd.ExecuteQuery<T>(args).ToList()).GetEnumerator();
        }

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		/// <summary>
		/// Queries the database and returns the results as a List.
		/// </summary>
		public List<T> ToList ()
		{
		    return ExecuteCommand("*", (cmd, args) => cmd.ExecuteQuery<T>(args).ToList());
		}

		/// <summary>
		/// Queries the database and returns the results as an array.
		/// </summary>
		public T[] ToArray ()
		{
		    return ExecuteCommand("*", (cmd, args) => cmd.ExecuteQuery<T>(args).ToArray());
		}

		/// <summary>
		/// Returns the first element of this query.
		/// </summary>
		public T First ()
		{
			var query = Take (1);
			return query.ToList<T> ().First ();
		}

		/// <summary>
		/// Returns the first element of this query, or null if no element is found.
		/// </summary>
		public T FirstOrDefault ()
		{
			var query = Take (1);
			return query.ToList<T> ().FirstOrDefault ();
		}

		/// <summary>
		/// Returns the first element of this query that matches the predicate.
		/// </summary>
		public T First (Expression<Func<T, bool>> predExpr)
		{
			var query = Where(predExpr).Take(1);
			return query.ToList<T>().First();
		}

		/// <summary>
		/// Returns the first element of this query that matches the predicate, or null
		/// if no element is found.
		/// </summary>
		public T FirstOrDefault (Expression<Func<T, bool>> predExpr)
		{
			var query = Where(predExpr).Take(1);
			return query.ToList<T>().FirstOrDefault();
		}
	}

	public static class SQLite3
	{
        // https://www.sqlite.org/rescode.html
        public enum Result : int
		{
			OK = 0,
			Error = 1,
			Internal = 2,
			Perm = 3,
			Abort = 4,
			Busy = 5,
			Locked = 6,
			NoMem = 7,
			ReadOnly = 8,
			Interrupt = 9,
			IOError = 10,
			Corrupt = 11,
			NotFound = 12,
			Full = 13,
			CannotOpen = 14,
			LockErr = 15,
			Empty = 16,
			SchemaChngd = 17,
			TooBig = 18,
			Constraint = 19,
			Mismatch = 20,
			Misuse = 21,
			NotImplementedLFS = 22,
			AccessDenied = 23,
			Format = 24,
			Range = 25,
			NonDBFile = 26,
			Notice = 27,
			Warning = 28,
			Row = 100,
			Done = 101
		}

        // https://www.sqlite.org/rescode.html
        public enum ExtendedResult : int
		{
			IOErrorRead = (Result.IOError | (1 << 8)),
			IOErrorShortRead = (Result.IOError | (2 << 8)),
			IOErrorWrite = (Result.IOError | (3 << 8)),
			IOErrorFsync = (Result.IOError | (4 << 8)),
			IOErrorDirFSync = (Result.IOError | (5 << 8)),
			IOErrorTruncate = (Result.IOError | (6 << 8)),
			IOErrorFStat = (Result.IOError | (7 << 8)),
			IOErrorUnlock = (Result.IOError | (8 << 8)),
			IOErrorRdlock = (Result.IOError | (9 << 8)),
			IOErrorDelete = (Result.IOError | (10 << 8)),
			IOErrorBlocked = (Result.IOError | (11 << 8)),
			IOErrorNoMem = (Result.IOError | (12 << 8)),
			IOErrorAccess = (Result.IOError | (13 << 8)),
			IOErrorCheckReservedLock = (Result.IOError | (14 << 8)),
			IOErrorLock = (Result.IOError | (15 << 8)),
			IOErrorClose = (Result.IOError | (16 << 8)),
			IOErrorDirClose = (Result.IOError | (17 << 8)),
			IOErrorSHMOpen = (Result.IOError | (18 << 8)),
			IOErrorSHMSize = (Result.IOError | (19 << 8)),
			IOErrorSHMLock = (Result.IOError | (20 << 8)),
			IOErrorSHMMap = (Result.IOError | (21 << 8)),
			IOErrorSeek = (Result.IOError | (22 << 8)),
			IOErrorDeleteNoEnt = (Result.IOError | (23 << 8)),
			IOErrorMMap = (Result.IOError | (24 << 8)),
			LockedSharedCache = (Result.Locked | (1 << 8)),
			BusyRecovery = (Result.Busy | (1 << 8)),
			CannottOpenNoTempDir = (Result.CannotOpen | (1 << 8)),
			CannotOpenIsDir = (Result.CannotOpen | (2 << 8)),
			CannotOpenFullPath = (Result.CannotOpen | (3 << 8)),
			CorruptVTab = (Result.Corrupt | (1 << 8)),
			ReadonlyRecovery = (Result.ReadOnly | (1 << 8)),
			ReadonlyCannotLock = (Result.ReadOnly | (2 << 8)),
			ReadonlyRollback = (Result.ReadOnly | (3 << 8)),
			AbortRollback = (Result.Abort | (2 << 8)),
			ConstraintCheck = (Result.Constraint | (1 << 8)),
			ConstraintCommitHook = (Result.Constraint | (2 << 8)),
			ConstraintForeignKey = (Result.Constraint | (3 << 8)),
			ConstraintFunction = (Result.Constraint | (4 << 8)),
			ConstraintNotNull = (Result.Constraint | (5 << 8)),
			ConstraintPrimaryKey = (Result.Constraint | (6 << 8)),
			ConstraintTrigger = (Result.Constraint | (7 << 8)),
			ConstraintUnique = (Result.Constraint | (8 << 8)),
			ConstraintVTab = (Result.Constraint | (9 << 8)),
			NoticeRecoverWAL = (Result.Notice | (1 << 8)),
			NoticeRecoverRollback = (Result.Notice | (2 << 8))
		}


		public enum ConfigOption : int
		{
			SingleThread = 1,
			MultiThread = 2,
			Serialized = 3
		}

		const string LibraryPath = "sqlite3";

#if !USE_CSHARP_SQLITE && !USE_WP8_NATIVE_SQLITE && !USE_SQLITEPCL_RAW
		[DllImport(LibraryPath, EntryPoint = "sqlite3_threadsafe", CallingConvention=CallingConvention.Cdecl)]
		public static extern int Threadsafe ();

		[DllImport(LibraryPath, EntryPoint = "sqlite3_open", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result Open ([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_open_v2", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result Open ([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db, int flags, IntPtr zvfs);
		
		[DllImport(LibraryPath, EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Open(byte[] filename, out IntPtr db, int flags, IntPtr zvfs);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_open16", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Open16([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr db);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_enable_load_extension", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result EnableLoadExtension (IntPtr db, int onoff);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_close", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result Close (IntPtr db);
		
		[DllImport(LibraryPath, EntryPoint = "sqlite3_close_v2", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Close2(IntPtr db);
		
		[DllImport(LibraryPath, EntryPoint = "sqlite3_initialize", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result Initialize();
						
		[DllImport(LibraryPath, EntryPoint = "sqlite3_shutdown", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result Shutdown();
		
		[DllImport(LibraryPath, EntryPoint = "sqlite3_config", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result Config (ConfigOption option);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_win32_set_directory", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Unicode)]
		public static extern int SetDirectory (uint directoryType, string directoryPath);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_busy_timeout", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result BusyTimeout (IntPtr db, int milliseconds);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_changes", CallingConvention=CallingConvention.Cdecl)]
		public static extern int Changes (IntPtr db);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_prepare_v2", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result Prepare2 (IntPtr db, [MarshalAs(UnmanagedType.LPStr)] string sql, int numBytes, out IntPtr stmt, IntPtr pzTail);

#if NETFX_CORE
		[DllImport (LibraryPath, EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Prepare2 (IntPtr db, byte[] queryBytes, int numBytes, out IntPtr stmt, IntPtr pzTail);
#endif

		public static IntPtr Prepare2 (IntPtr db, string query)
		{
			IntPtr stmt;
#if NETFX_CORE
            byte[] queryBytes = System.Text.UTF8Encoding.UTF8.GetBytes (query);
            var r = Prepare2 (db, queryBytes, queryBytes.Length, out stmt, IntPtr.Zero);
#else
            var r = Prepare2 (db, query, System.Text.UTF8Encoding.UTF8.GetByteCount (query), out stmt, IntPtr.Zero);
#endif
			if (r != Result.OK) {
				throw SQLiteException.New (r, GetErrorMessage (db));
			}
			return stmt;
		}

		[DllImport(LibraryPath, EntryPoint = "sqlite3_step", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result Step (IntPtr stmt);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_reset", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result Reset (IntPtr stmt);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_finalize", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result Finalize (IntPtr stmt);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_last_insert_rowid", CallingConvention=CallingConvention.Cdecl)]
		public static extern long LastInsertRowid (IntPtr db);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_errmsg16", CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr ErrorMessage (IntPtr db);

		public static string GetErrorMessage (IntPtr db)
		{
			return Marshal.PtrToStringUni (ErrorMessage (db));
		}

		[DllImport(LibraryPath, EntryPoint = "sqlite3_bind_parameter_index", CallingConvention=CallingConvention.Cdecl)]
		public static extern int BindParameterIndex (IntPtr stmt, [MarshalAs(UnmanagedType.LPStr)] string name);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_bind_null", CallingConvention=CallingConvention.Cdecl)]
		public static extern int BindNull (IntPtr stmt, int index);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_bind_int", CallingConvention=CallingConvention.Cdecl)]
		public static extern int BindInt (IntPtr stmt, int index, int val);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_bind_int64", CallingConvention=CallingConvention.Cdecl)]
		public static extern int BindInt64 (IntPtr stmt, int index, long val);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_bind_double", CallingConvention=CallingConvention.Cdecl)]
		public static extern int BindDouble (IntPtr stmt, int index, double val);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_bind_text16", CallingConvention=CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		public static extern int BindText (IntPtr stmt, int index, [MarshalAs(UnmanagedType.LPWStr)] string val, int n, IntPtr free);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_bind_blob", CallingConvention=CallingConvention.Cdecl)]
		public static extern int BindBlob (IntPtr stmt, int index, byte[] val, int n, IntPtr free);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_column_count", CallingConvention=CallingConvention.Cdecl)]
		public static extern int ColumnCount (IntPtr stmt);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_column_name", CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr ColumnName (IntPtr stmt, int index);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_column_name16", CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr ColumnName16Internal (IntPtr stmt, int index);
		public static string ColumnName16(IntPtr stmt, int index)
		{
			return Marshal.PtrToStringUni(ColumnName16Internal(stmt, index));
		}

		[DllImport(LibraryPath, EntryPoint = "sqlite3_column_type", CallingConvention=CallingConvention.Cdecl)]
		public static extern ColType ColumnType (IntPtr stmt, int index);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_column_int", CallingConvention=CallingConvention.Cdecl)]
		public static extern int ColumnInt (IntPtr stmt, int index);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_column_int64", CallingConvention=CallingConvention.Cdecl)]
		public static extern long ColumnInt64 (IntPtr stmt, int index);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_column_double", CallingConvention=CallingConvention.Cdecl)]
		public static extern double ColumnDouble (IntPtr stmt, int index);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_column_text", CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr ColumnText (IntPtr stmt, int index);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_column_text16", CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr ColumnText16 (IntPtr stmt, int index);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_column_blob", CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr ColumnBlob (IntPtr stmt, int index);

		[DllImport(LibraryPath, EntryPoint = "sqlite3_column_bytes", CallingConvention=CallingConvention.Cdecl)]
		public static extern int ColumnBytes (IntPtr stmt, int index);

		public static string ColumnString (IntPtr stmt, int index)
		{
			return Marshal.PtrToStringUni (SQLite3.ColumnText16 (stmt, index));
		}

		public static byte[] ColumnByteArray (IntPtr stmt, int index)
		{
			int length = ColumnBytes (stmt, index);
			var result = new byte[length];
			if (length > 0)
				Marshal.Copy (ColumnBlob (stmt, index), result, 0, length);
			return result;
		}

		[DllImport (LibraryPath, EntryPoint = "sqlite3_extended_errcode", CallingConvention = CallingConvention.Cdecl)]
		public static extern ExtendedResult ExtendedErrCode (IntPtr db);

		[DllImport (LibraryPath, EntryPoint = "sqlite3_libversion_number", CallingConvention = CallingConvention.Cdecl)]
		public static extern int LibVersionNumber ();
#else
		public static Result Open (string filename, out Sqlite3DatabaseHandle db)
		{
			return (Result)Sqlite3.sqlite3_open (filename, out db);
		}

		public static Result Open (string filename, out Sqlite3DatabaseHandle db, int flags, IntPtr zVfs)
		{
#if USE_WP8_NATIVE_SQLITE
			return (Result)Sqlite3.sqlite3_open_v2(filename, out db, flags, "");
#else
			return (Result)Sqlite3.sqlite3_open_v2 (filename, out db, flags, null);
#endif
		}

		public static Result Close (Sqlite3DatabaseHandle db)
		{
			return (Result)Sqlite3.sqlite3_close (db);
		}

		public static Result Close2 (Sqlite3DatabaseHandle db)
		{
			return (Result)Sqlite3.sqlite3_close_v2 (db);
		}

		public static Result BusyTimeout (Sqlite3DatabaseHandle db, int milliseconds)
		{
			return (Result)Sqlite3.sqlite3_busy_timeout (db, milliseconds);
		}

		public static int Changes (Sqlite3DatabaseHandle db)
		{
			return Sqlite3.sqlite3_changes (db);
		}

		public static Sqlite3Statement Prepare2 (Sqlite3DatabaseHandle db, string query)
		{
			Sqlite3Statement stmt = default (Sqlite3Statement);
#if USE_WP8_NATIVE_SQLITE || USE_SQLITEPCL_RAW
			var r = Sqlite3.sqlite3_prepare_v2 (db, query, out stmt);
#else
			stmt = new Sqlite3Statement();
			var r = Sqlite3.sqlite3_prepare_v2(db, query, -1, ref stmt, 0);
#endif
			if (r != 0)
			{
				throw SQLiteException.New((Result) r, GetErrorMessage(db), sql: query);
			}
			return stmt;
		}

		public static Result Step (Sqlite3Statement stmt)
		{
			return (Result)Sqlite3.sqlite3_step (stmt);
		}

		public static Result Reset (Sqlite3Statement stmt)
		{
			return (Result)Sqlite3.sqlite3_reset (stmt);
		}

		public static Result Finalize (Sqlite3Statement stmt)
		{
			return (Result)Sqlite3.sqlite3_finalize (stmt);
		}

		public static long LastInsertRowid (Sqlite3DatabaseHandle db)
		{
			return Sqlite3.sqlite3_last_insert_rowid (db);
		}

		public static string GetErrorMessage(Sqlite3DatabaseHandle db)
		{
			return Sqlite3.sqlite3_errmsg(db);
		}

	    public static string GetErrorString(SQLite3.Result result)
	    {
	        return Sqlite3.sqlite3_errstr((int)result);
	    }

	    public static string GetErrorString(SQLite3.ExtendedResult result)
	    {
	        return Sqlite3.sqlite3_errstr((int)result);
	    }

		public static int BindParameterIndex (Sqlite3Statement stmt, string name)
		{
			return Sqlite3.sqlite3_bind_parameter_index (stmt, name);
		}

		public static int BindNull (Sqlite3Statement stmt, int index)
		{
			return Sqlite3.sqlite3_bind_null (stmt, index);
		}

		public static int BindInt (Sqlite3Statement stmt, int index, int val)
		{
			return Sqlite3.sqlite3_bind_int (stmt, index, val);
		}

		public static int BindInt64 (Sqlite3Statement stmt, int index, long val)
		{
			return Sqlite3.sqlite3_bind_int64 (stmt, index, val);
		}

		public static int BindDouble (Sqlite3Statement stmt, int index, double val)
		{
			return Sqlite3.sqlite3_bind_double (stmt, index, val);
		}

		public static int BindText (Sqlite3Statement stmt, int index, string val, int n, IntPtr free)
		{
#if USE_WP8_NATIVE_SQLITE
			return Sqlite3.sqlite3_bind_text(stmt, index, val, n);
#elif USE_SQLITEPCL_RAW
			return Sqlite3.sqlite3_bind_text (stmt, index, val);
#else
			return Sqlite3.sqlite3_bind_text(stmt, index, val, n, null);
#endif
		}

		public static int BindBlob (Sqlite3Statement stmt, int index, byte[] val, int n, IntPtr free)
		{
#if USE_WP8_NATIVE_SQLITE
			return Sqlite3.sqlite3_bind_blob(stmt, index, val, n);
#elif USE_SQLITEPCL_RAW
			return Sqlite3.sqlite3_bind_blob (stmt, index, val);
#else
			return Sqlite3.sqlite3_bind_blob(stmt, index, val, n, null);
#endif
		}

		public static int ColumnCount (Sqlite3Statement stmt)
		{
			return Sqlite3.sqlite3_column_count (stmt);
		}

		public static string ColumnName (Sqlite3Statement stmt, int index)
		{
			return Sqlite3.sqlite3_column_name (stmt, index);
		}

		public static string ColumnName16 (Sqlite3Statement stmt, int index)
		{
			return Sqlite3.sqlite3_column_name (stmt, index);
		}

		public static ColType ColumnType (Sqlite3Statement stmt, int index)
		{
			return (ColType)Sqlite3.sqlite3_column_type (stmt, index);
		}

		public static int ColumnInt (Sqlite3Statement stmt, int index)
		{
			return Sqlite3.sqlite3_column_int (stmt, index);
		}

		public static long ColumnInt64 (Sqlite3Statement stmt, int index)
		{
			return Sqlite3.sqlite3_column_int64 (stmt, index);
		}

		public static double ColumnDouble (Sqlite3Statement stmt, int index)
		{
			return Sqlite3.sqlite3_column_double (stmt, index);
		}

		public static string ColumnText (Sqlite3Statement stmt, int index)
		{
			return Sqlite3.sqlite3_column_text (stmt, index);
		}

		public static string ColumnText16 (Sqlite3Statement stmt, int index)
		{
			return Sqlite3.sqlite3_column_text (stmt, index);
		}

		public static byte[] ColumnBlob (Sqlite3Statement stmt, int index)
		{
			return Sqlite3.sqlite3_column_blob (stmt, index);
		}

		public static int ColumnBytes (Sqlite3Statement stmt, int index)
		{
			return Sqlite3.sqlite3_column_bytes (stmt, index);
		}

		public static string ColumnString (Sqlite3Statement stmt, int index)
		{
			return Sqlite3.sqlite3_column_text (stmt, index);
		}

		public static byte[] ColumnByteArray (Sqlite3Statement stmt, int index)
		{
			int length = ColumnBytes (stmt, index);
			if (length > 0) {
				return ColumnBlob (stmt, index);
			}
			return new byte[0];
		}

		public static Result EnableLoadExtension (Sqlite3DatabaseHandle db, int onoff)
		{
			return (Result)Sqlite3.sqlite3_enable_load_extension (db, onoff);
		}

		public static int LibVersionNumber ()
		{
			return Sqlite3.sqlite3_libversion_number ();
		}

		public static ExtendedResult ExtendedErrCode (Sqlite3DatabaseHandle db)
		{
			return (ExtendedResult)Sqlite3.sqlite3_extended_errcode (db);
		}
#endif

		public enum ColType : int
		{
			Integer = 1,
			Float = 2,
			Text = 3,
			Blob = 4,
			Null = 5
		}
	}

    /// <seealso href="https://sqlite.org/c3ref/c_source_id.html"/>
    public struct SQLiteVersion : IEquatable<SQLiteVersion>, IComparable<SQLiteVersion>, IComparable
    {
        /// <summary>
        /// Indicates whether the two SQLiteVersion instances are equal to each other.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator ==(SQLiteVersion x, SQLiteVersion y) => x.Equals(y);

        /// <summary>
        /// Indicates whether the two SQLiteVersion instances are not equal each other.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/> if the two instances are not equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator !=(SQLiteVersion x, SQLiteVersion y) => !(x == y);

        /// <summary>
        /// Indicates if the the first SQLiteVersion is greater than or equal to the second.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteVersion is greater than or equal to the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(SQLiteVersion x, SQLiteVersion y) => x._version >= y._version;

        /// <summary>
        /// Indicates if the the first SQLiteVersion is greater than the second.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteVersion is greater than the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(SQLiteVersion x, SQLiteVersion y) => x._version > y._version;

        /// <summary>
        /// Indicates if the the first SQLiteVersion is less than or equal to the second.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteVersion is less than or equal to the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(SQLiteVersion x, SQLiteVersion y) => x._version <= y._version;

        /// <summary>
        /// Indicates if the the first SQLiteVersion is less than the second.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteVersion is less than the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(SQLiteVersion x, SQLiteVersion y) => x._version < y._version;

        private readonly int _version;

        public SQLiteVersion(int version)
        {
            this._version = version;
        }

        /// <summary>
        /// Gets the major version number.
        /// </summary>
        public int Major => _version / 1000000;

        /// <summary>
        /// Gets the minor version number.
        /// </summary>
        public int Minor => (_version / 1000) % 1000;

        /// <summary>
        /// Gets the release version number.
        /// </summary>
        public int Release => _version % 1000;

        /// <summary>
        /// Converts the version number as an integer with the value (Major*1000000 + Minor*1000 + Release).
        /// </summary>
        /// <returns>The version number as an integer</returns>
        public int ToInt32() => _version;

        public override string ToString() => $"{this.Major}.{this.Minor}.{this.Release}";
        public override int GetHashCode() => _version;
        public bool Equals(SQLiteVersion other) => this._version == other._version;
        public override bool Equals(object other) => other is SQLiteVersion && this == (SQLiteVersion)other;
        public int CompareTo(SQLiteVersion other) => this._version.CompareTo(other._version);
        public int CompareTo(object obj)
        {
            if (obj is SQLiteVersion)
            {
                return this.CompareTo((SQLiteVersion)obj);
            }
            throw new ArgumentException("Can only compare to other SQLiteVersion");
        }
    }
}

#if NO_CONCURRENT
namespace SQLite.Extensions
{
	public static class ListEx
	{
		public static bool TryAdd<TKey, TValue> (this IDictionary<TKey, TValue> dict, TKey key, TValue value)
		{
			try {
				dict.Add (key, value);
				return true;
			}
			catch (ArgumentException) {
				return false;
			}
		}
	}
}
#endif
