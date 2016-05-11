//
// Copyright (c) 2012 Krueger Systems, Inc.
// Copyright (c) 2013 Øystein Krog (oystein.krog@gmail.com)
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
using System.Reflection;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

namespace SQLite.Net
{
    /// <summary>
    ///     Represents an open connection to a SQLite database.
    /// </summary>
    [PublicAPI]
    public class SQLiteConnection : IDisposable
    {
        internal static readonly IDbHandle NullHandle = default(IDbHandle);

        /// <summary>
        ///     Used to list some code that we want the MonoTouch linker
        ///     to see, but that we never want to actually execute.
        /// </summary>
#pragma warning disable 649
        private static bool _preserveDuringLinkMagic;
#pragma warning restore 649
        private readonly Random _rand = new Random();
        private readonly IDictionary<string, TableMapping> _tableMappings;
		private readonly object _tableMappingsLocks;
        private TimeSpan _busyTimeout;
        private long _elapsedMilliseconds;
        private IDictionary<TableMapping, ActiveInsertCommand> _insertCommandCache;
        private bool _open;
        private IStopwatch _sw;
        private int _transactionDepth;

        static SQLiteConnection()
        {
            if (_preserveDuringLinkMagic)
            {
                // ReSharper disable once UseObjectOrCollectionInitializer
                var ti = new ColumnInfo();
                ti.Name = "magic";
            }
        }

        /// <summary>
        ///     Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
        /// </summary>
        /// <param name="sqlitePlatform"></param>
        /// <param name="databasePath">
        ///     Specifies the path to the database file.
        /// </param>
        /// <param name="storeDateTimeAsTicks">
        ///     Specifies whether to store DateTime properties as ticks (true) or strings (false). You
        ///     absolutely do want to store them as Ticks in all new projects. The option to set false is
        ///     only here for backwards compatibility. There is a *significant* speed advantage, with no
        ///     down sides, when setting storeDateTimeAsTicks = true.
        /// </param>
        /// <param name="serializer">
        ///     Blob serializer to use for storing undefined and complex data structures. If left null
        ///     these types will thrown an exception as usual.
        /// </param>
        /// <param name="tableMappings">
        ///     Exisiting table mapping that the connection can use. If its null, it creates the mappings,
        ///     if and when required. The mappings are also created when an unknown type is used for the first
        ///     time.
        /// </param>
        /// <param name="extraTypeMappings">
        ///     Any extra type mappings that you wish to use for overriding the default for creating
        ///     column definitions for SQLite DDL in the class Orm (snake in Swedish).
        /// </param>
        /// <param name="resolver">
        ///     A contract resovler for resolving interfaces to concreate types during object creation
        /// </param>
        [PublicAPI]
        public SQLiteConnection([JetBrains.Annotations.NotNull] ISQLitePlatform sqlitePlatform, [JetBrains.Annotations.NotNull] string databasePath,
            bool storeDateTimeAsTicks = true, [CanBeNull] IBlobSerializer serializer = null, [CanBeNull] IDictionary<string, TableMapping> tableMappings = null,
            [CanBeNull] IDictionary<Type, string> extraTypeMappings = null, [CanBeNull] IContractResolver resolver = null)
            : this(sqlitePlatform, databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, storeDateTimeAsTicks,
                serializer, tableMappings, extraTypeMappings, resolver)
        {
        }

        /// <summary>
        ///     Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
        /// </summary>
        /// <param name="sqlitePlatform"></param>
        /// <param name="databasePath">
        ///     Specifies the path to the database file.
        /// </param>
        /// <param name="openFlags"></param>
        /// <param name="storeDateTimeAsTicks">
        ///     Specifies whether to store DateTime properties as ticks (true) or strings (false). You
        ///     absolutely do want to store them as Ticks in all new projects. The option to set false is
        ///     only here for backwards compatibility. There is a *significant* speed advantage, with no
        ///     down sides, when setting storeDateTimeAsTicks = true.
        /// </param>
        /// <param name="serializer">
        ///     Blob serializer to use for storing undefined and complex data structures. If left null
        ///     these types will thrown an exception as usual.
        /// </param>
        /// <param name="tableMappings">
        ///     Exisiting table mapping that the connection can use. If its null, it creates the mappings,
        ///     if and when required. The mappings are also created when an unknown type is used for the first
        ///     time.
        /// </param>
        /// <param name="extraTypeMappings">
        ///     Any extra type mappings that you wish to use for overriding the default for creating
        ///     column definitions for SQLite DDL in the class Orm (snake in Swedish).
        /// </param>
        /// <param name="resolver">
        ///     A contract resovler for resolving interfaces to concreate types during object creation
        /// </param>
        [PublicAPI]
        public SQLiteConnection([JetBrains.Annotations.NotNull] ISQLitePlatform sqlitePlatform, string databasePath, SQLiteOpenFlags openFlags,
            bool storeDateTimeAsTicks = true, [CanBeNull] IBlobSerializer serializer = null, [CanBeNull] IDictionary<string, TableMapping> tableMappings = null,
            [CanBeNull] IDictionary<Type, string> extraTypeMappings = null, IContractResolver resolver = null)
        {
            if (sqlitePlatform == null)
            {
                throw new ArgumentNullException("sqlitePlatform");
            }
            ExtraTypeMappings = extraTypeMappings ?? new Dictionary<Type, string>();
            Serializer = serializer;
            Platform = sqlitePlatform;
            Resolver = resolver ?? ContractResolver.Current;

            _tableMappings = tableMappings ?? new Dictionary<string, TableMapping>();
			_tableMappingsLocks = new object();

            if (string.IsNullOrEmpty(databasePath))
            {
                throw new ArgumentException("Must be specified", "databasePath");
            }

            DatabasePath = databasePath;

            IDbHandle handle;
            var databasePathAsBytes = GetNullTerminatedUtf8(DatabasePath);
            var r = Platform.SQLiteApi.Open(databasePathAsBytes, out handle, (int) openFlags, IntPtr.Zero);

            Handle = handle;
            if (r != Result.OK)
            {
                throw SQLiteException.New(r, string.Format("Could not open database file: {0} ({1})", DatabasePath, r));
            }

            if (handle == null)
            {
                throw new NullReferenceException("Database handle is null");
            }

            Handle = handle;

            _open = true;

            StoreDateTimeAsTicks = storeDateTimeAsTicks;

            BusyTimeout = TimeSpan.FromSeconds(0.1);
        }

		private IColumnInformationProvider _columnInformationProvider;
		[CanBeNull, PublicAPI]
		public IColumnInformationProvider ColumnInformationProvider 
		{
			get { return _columnInformationProvider; }
			set
			{
				_columnInformationProvider = value;
				Orm.ColumnInformationProvider = _columnInformationProvider ?? new DefaultColumnInformationProvider ();
			}
		}

        [CanBeNull, PublicAPI]
        public IBlobSerializer Serializer { get; private set; }

        [CanBeNull, PublicAPI]
        public IDbHandle Handle { get; private set; }

        [JetBrains.Annotations.NotNull, PublicAPI]
        public string DatabasePath { get; private set; }

        [PublicAPI]
        public bool TimeExecution { get; set; }

        [CanBeNull]
        [PublicAPI]
        public ITraceListener TraceListener { get; set; }

        [PublicAPI]
        public bool StoreDateTimeAsTicks { get; private set; }

        [JetBrains.Annotations.NotNull, PublicAPI]
        public IDictionary<Type, string> ExtraTypeMappings { get; private set; }

        [JetBrains.Annotations.NotNull, PublicAPI]
        public IContractResolver Resolver { get; private set; }

        /// <summary>
        ///     Sets a busy handler to sleep the specified amount of time when a table is locked.
        ///     The handler will sleep multiple times until a total time of <see cref="BusyTimeout" /> has accumulated.
        /// </summary>
        [PublicAPI]
        public TimeSpan BusyTimeout
        {
            get { return _busyTimeout; }
            set
            {
                _busyTimeout = value;
                if (Handle != NullHandle)
                {
                    Platform.SQLiteApi.BusyTimeout(Handle, (int) _busyTimeout.TotalMilliseconds);
                }
            }
        }

        /// <summary>
        ///     Returns the mappings from types to tables that the connection
        ///     currently understands.
        /// </summary>
        [PublicAPI]
        [JetBrains.Annotations.NotNull]
        public IEnumerable<TableMapping> TableMappings
        {
            get
			{
				lock (_tableMappingsLocks)
				{
					return _tableMappings.Values.ToList();
				}
			}
        }

        /// <summary>
        ///     Whether <see cref="BeginTransaction" /> has been called and the database is waiting for a <see cref="Commit" />.
        /// </summary>
        [PublicAPI]
        public bool IsInTransaction
        {
            get { return _transactionDepth > 0; }
        }

        [JetBrains.Annotations.NotNull, PublicAPI]
        public ISQLitePlatform Platform { get; private set; }

        [PublicAPI]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [PublicAPI]
        public void EnableLoadExtension(int onoff)
        {
            var r = Platform.SQLiteApi.EnableLoadExtension(Handle, onoff);
            if (r != Result.OK)
            {
                var msg = Platform.SQLiteApi.Errmsg16(Handle);
                throw SQLiteException.New(r, msg);
            }
        }

        [JetBrains.Annotations.NotNull]
        private static byte[] GetNullTerminatedUtf8(string s)
        {
            var utf8Length = Encoding.UTF8.GetByteCount(s);
            var bytes = new byte[utf8Length + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
            return bytes;
        }

        /// <summary>
        ///     Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <param name="type">
        ///     The type whose mapping to the database is returned.
        /// </param>
        /// <param name="createFlags">
        ///     Optional flags allowing implicit PK and indexes based on naming conventions
        /// </param>
        /// <returns>
        ///     The mapping represents the schema of the columns of the database and contains
        ///     methods to set and get properties of objects.
        /// </returns>
        [PublicAPI]
        public TableMapping GetMapping(Type type, CreateFlags createFlags = CreateFlags.None)
        {
			lock (_tableMappingsLocks)
			{
				TableMapping map;
				return _tableMappings.TryGetValue(type.FullName, out map) ? map : CreateAndSetMapping(type, createFlags, _tableMappings);
			}
        }

        private TableMapping CreateAndSetMapping(Type type, CreateFlags createFlags, IDictionary<string, TableMapping> mapTable)
        {
            var props = Platform.ReflectionService.GetPublicInstanceProperties(type);
			var map = new TableMapping(type, props, createFlags, _columnInformationProvider);
            mapTable[type.FullName] = map;
        	return map;
        }

        /// <summary>
        ///     Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <returns>
        ///     The mapping represents the schema of the columns of the database and contains
        ///     methods to set and get properties of objects.
        /// </returns>
        [PublicAPI]
        public TableMapping GetMapping<T>()
        {
            return GetMapping(typeof (T));
        }

        /// <summary>
        ///     Executes a "drop table" on the database.  This is non-recoverable.
        /// </summary>
        [PublicAPI]
        public int DropTable<T>()
        {
            return DropTable(typeof (T));
        }

        /// <summary>
        ///     Executes a "drop table" on the database.  This is non-recoverable.
        /// </summary>
        [PublicAPI]
        public int DropTable(Type t)
        {
            var map = GetMapping(t);

            var query = string.Format("drop table if exists \"{0}\"", map.TableName);

            return Execute(query);
        }

        /// <summary>
        ///     Executes a "create table if not exists" on the database. It also
        ///     creates any specified indexes on the columns of the table. It uses
        ///     a schema automatically generated from the specified type. You can
        ///     later access this schema by calling GetMapping.
        /// </summary>
        /// <returns>
        ///     The number of entries added to the database schema.
        /// </returns>
        [PublicAPI]
        public int CreateTable<T>(CreateFlags createFlags = CreateFlags.None)
        {
            return CreateTable(typeof (T), createFlags);
        }

        /// <summary>
        ///     Executes a "create table if not exists" on the database. It also
        ///     creates any specified indexes on the columns of the table. It uses
        ///     a schema automatically generated from the specified type. You can
        ///     later access this schema by calling GetMapping.
        /// </summary>
        /// <param name="ty">Type to reflect to a database table.</param>
        /// <param name="createFlags">Optional flags allowing implicit PK and indexes based on naming conventions.</param>
        /// <returns>
        ///     The number of entries added to the database schema.
        /// </returns>
        [PublicAPI]
        public int CreateTable(Type ty, CreateFlags createFlags = CreateFlags.None)
        {
            var map = GetMapping(ty, createFlags);

            var query = "create table if not exists \"" + map.TableName + "\"(\n";

            var mapColumns = map.Columns;

            if (!mapColumns.Any())
            {
                throw new Exception("Table has no (public) columns");
            }

            var decls = mapColumns.Select(p => Orm.SqlDecl(p, StoreDateTimeAsTicks, Serializer, ExtraTypeMappings));
            var decl = string.Join(",\n", decls.ToArray());
            query += decl;
            query += ")";

            var count = Execute(query);

            if (count == 0)
            {
                //Possible bug: This always seems to return 0?
                // Table already exists, migrate it
                MigrateTable(map);
            }

            var indexes = new Dictionary<string, IndexInfo>();
            foreach (var c in mapColumns)
            {
                foreach (var i in c.Indices)
                {
                    var iname = i.Name ?? map.TableName + "_" + c.Name;
                    IndexInfo iinfo;
                    if (!indexes.TryGetValue(iname, out iinfo))
                    {
                        iinfo = new IndexInfo
                        {
                            IndexName = iname,
                            TableName = map.TableName,
                            Unique = i.Unique,
                            Columns = new List<IndexedColumn>()
                        };
                        indexes.Add(iname, iinfo);
                    }

                    if (i.Unique != iinfo.Unique)
                    {
                        throw new Exception(
                            "All the columns in an index must have the same value for their Unique property");
                    }

                    iinfo.Columns.Add(new IndexedColumn
                    {
                        Order = i.Order,
                        ColumnName = c.Name
                    });
                }
            }

            foreach (var indexName in indexes.Keys)
            {
                var index = indexes[indexName];
                var columns = index.Columns.OrderBy(i => i.Order).Select(i => i.ColumnName).ToArray();
                count += CreateIndex(indexName, index.TableName, columns, index.Unique);
            }

            return count;
        }

        /// <summary>
        ///     Creates an index for the specified table and columns.
        /// </summary>
        /// <param name="indexName">Name of the index to create</param>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnNames">An array of column names to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        [PublicAPI]
        public int CreateIndex(string indexName, string tableName, string[] columnNames, bool unique = false)
        {
            const string sqlFormat = "create {2} index if not exists \"{3}\" on \"{0}\"(\"{1}\")";
            var sql = string.Format(sqlFormat, tableName, string.Join("\", \"", columnNames), unique ? "unique" : "", indexName);
            return Execute(sql);
        }

        /// <summary>
        ///     Creates an index for the specified table and column.
        /// </summary>
        /// <param name="indexName">Name of the index to create</param>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        [PublicAPI]
        public int CreateIndex(string indexName, string tableName, string columnName, bool unique = false)
        {
            return CreateIndex(indexName, tableName, new[] {columnName}, unique);
        }

        /// <summary>
        ///     Creates an index for the specified table and column.
        /// </summary>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        [PublicAPI]
        public int CreateIndex(string tableName, string columnName, bool unique = false)
        {
            return CreateIndex(tableName + "_" + columnName, tableName, columnName, unique);
        }

        /// <summary>
        ///     Creates an index for the specified table and columns.
        /// </summary>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnNames">An array of column names to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        [PublicAPI]
        public int CreateIndex(string tableName, string[] columnNames, bool unique = false)
        {
            return CreateIndex(tableName + "_" + string.Join("_", columnNames), tableName, columnNames, unique);
        }

        /// <summary>
        ///     Creates an index for the specified object property.
        ///     e.g. CreateIndex{Client}(c => c.Name);
        /// </summary>
        /// <typeparam name="T">Type to reflect to a database table.</typeparam>
        /// <param name="property">Property to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        [PublicAPI]
        public void CreateIndex<T>(Expression<Func<T, object>> property, bool unique = false)
        {
            MemberExpression mx;
            if (property.Body.NodeType == ExpressionType.Convert)
            {
                mx = ((UnaryExpression) property.Body).Operand as MemberExpression;
            }
            else
            {
                mx = (property.Body as MemberExpression);
            }
            if (mx == null)
            {
                throw new ArgumentException("Expression is not supported", "property");
            }
            var propertyInfo = mx.Member as PropertyInfo;
            if (propertyInfo == null)
            {
                throw new ArgumentException("The lambda expression 'property' should point to a valid Property");
            }

            var propName = propertyInfo.Name;

            var map = GetMapping<T>();
            var colName = map.FindColumnWithPropertyName(propName).Name;

            CreateIndex(map.TableName, colName, unique);
        }

        [PublicAPI]
        public List<ColumnInfo> GetTableInfo(string tableName)
        {
            var query = "pragma table_info(\"" + tableName + "\")";
            return Query<ColumnInfo>(query);
        }

		[PublicAPI]
		public void MigrateTable<T>()
		{
			MigrateTable(typeof(T));
		}

		[PublicAPI]
		public void MigrateTable(Type t)
		{
			var map = GetMapping(t);
			MigrateTable(map);
		}

        private void MigrateTable(TableMapping map)
        {
            var existingCols = GetTableInfo(map.TableName);

            var toBeAdded = new List<TableMapping.Column>();

            foreach (var p in map.Columns)
            {
                var found = false;
                foreach (var c in existingCols)
                {
                    found = (string.Compare(p.Name, c.Name, StringComparison.OrdinalIgnoreCase) == 0);
                    if (found)
                    {
                        break;
                    }
                }
                if (!found)
                {
                    toBeAdded.Add(p);
                }
            }

            foreach (var p in toBeAdded)
            {
                var addCol = "alter table \"" + map.TableName + "\" add column " +
                             Orm.SqlDecl(p, StoreDateTimeAsTicks, Serializer, ExtraTypeMappings);
                Execute(addCol);
            }
        }

        /// <summary>
        ///     Creates a new SQLiteCommand. Can be overridden to provide a sub-class.
        /// </summary>
        /// <seealso cref="SQLiteCommand.OnInstanceCreated" />
        [PublicAPI]
        protected virtual SQLiteCommand NewCommand()
        {
            return new SQLiteCommand(Platform, this);
        }

        /// <summary>
        ///     Creates a new SQLiteCommand given the command text with arguments. Place a '?'
        ///     in the command text for each of the arguments.
        /// </summary>
        /// <param name="cmdText">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the command text.
        /// </param>
        /// <returns>
        ///     A <see cref="SQLiteCommand" />
        /// </returns>
        [PublicAPI]
        public SQLiteCommand CreateCommand(string cmdText, params object[] args)
        {
            if (!_open)
            {
                throw SQLiteException.New(Result.Error, "Cannot create commands from unopened database");
            }

            var cmd = NewCommand();
            cmd.CommandText = cmdText;
            foreach (var o in args)
            {
                cmd.Bind(o);
            }
            return cmd;
        }

        /// <summary>
        ///     Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///     in the command text for each of the arguments and then executes that command.
        ///     Use this method instead of Query when you don't expect rows back. Such cases include
        ///     INSERTs, UPDATEs, and DELETEs.
        ///     You can set the Trace or TimeExecution properties of the connection
        ///     to profile execution.
        /// </summary>
        /// <param name="query">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///     The number of rows modified in the database as a result of this execution.
        /// </returns>
        [PublicAPI]
        public int Execute(string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);

            if (TimeExecution)
            {
                if (_sw == null)
                {
                    _sw = Platform.StopwatchFactory.Create();
                }
                _sw.Reset();
                _sw.Start();
            }

            var r = cmd.ExecuteNonQuery();

            if (TimeExecution)
            {
                _sw.Stop();
                _elapsedMilliseconds += _sw.ElapsedMilliseconds;

                TraceListener.WriteLine("Finished in {0} ms ({1:0.0} s total)", _sw.ElapsedMilliseconds, _elapsedMilliseconds/1000.0);
            }

            return r;
        }

        [PublicAPI]
        public T ExecuteScalar<T>(string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);

            if (TimeExecution)
            {
                if (_sw == null)
                {
                    _sw = Platform.StopwatchFactory.Create();
                }
                _sw.Reset();
                _sw.Start();
            }

            var r = cmd.ExecuteScalar<T>();

            if (TimeExecution)
            {
                _sw.Stop();
                _elapsedMilliseconds += _sw.ElapsedMilliseconds;

                TraceListener.WriteLine("Finished in {0} ms ({1:0.0} s total)", _sw.ElapsedMilliseconds, _elapsedMilliseconds/1000.0);
            }

            return r;
        }

        /// <summary>
        ///     Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///     in the command text for each of the arguments and then executes that command.
        ///     It returns each row of the result using the mapping automatically generated for
        ///     the given type.
        /// </summary>
        /// <param name="query">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///     An enumerable with one result for each row returned by the query.
        /// </returns>
        [PublicAPI]
        public List<T> Query<T>(string query, params object[] args) where T : class
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteQuery<T>();
        }

        /// <summary>
        ///     Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///     in the command text for each of the arguments and then executes that command.
        ///     It returns each row of the result using the mapping automatically generated for
        ///     the given type.
        /// </summary>
        /// <param name="query">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///     An enumerable with one result for each row returned by the query.
        ///     The enumerator will call sqlite3_step on each call to MoveNext, so the database
        ///     connection must remain open for the lifetime of the enumerator.
        /// </returns>
        [PublicAPI]
        public IEnumerable<T> DeferredQuery<T>(string query, params object[] args) where T : class
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteDeferredQuery<T>();
        }

        /// <summary>
        ///     Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///     in the command text for each of the arguments and then executes that command.
        ///     It returns each row of the result using the specified mapping. This function is
        ///     only used by libraries in order to query the database via introspection. It is
        ///     normally not used.
        /// </summary>
        /// <param name="map">
        ///     A <see cref="TableMapping" /> to use to convert the resulting rows
        ///     into objects.
        /// </param>
        /// <param name="query">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///     An enumerable with one result for each row returned by the query.
        /// </returns>
        [PublicAPI]
        public List<object> Query(TableMapping map, string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteQuery<object>(map);
        }

        /// <summary>
        ///     Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///     in the command text for each of the arguments and then executes that command.
        ///     It returns each row of the result using the specified mapping. This function is
        ///     only used by libraries in order to query the database via introspection. It is
        ///     normally not used.
        /// </summary>
        /// <param name="map">
        ///     A <see cref="TableMapping" /> to use to convert the resulting rows
        ///     into objects.
        /// </param>
        /// <param name="query">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///     An enumerable with one result for each row returned by the query.
        ///     The enumerator will call sqlite3_step on each call to MoveNext, so the database
        ///     connection must remain open for the lifetime of the enumerator.
        /// </returns>
        [PublicAPI]
        public IEnumerable<object> DeferredQuery(TableMapping map, string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteDeferredQuery<object>(map);
        }

        /// <summary>
        ///     Returns a queryable interface to the table represented by the given type.
        /// </summary>
        /// <returns>
        ///     A queryable object that is able to translate Where, OrderBy, and Take
        ///     queries into native SQL.
        /// </returns>
        [PublicAPI]
        public TableQuery<T> Table<T>() where T : class
        {
            return new TableQuery<T>(Platform, this);
        }

        /// <summary>
        ///     Attempts to retrieve an object with the given primary key from the table
        ///     associated with the specified type. Use of this method requires that
        ///     the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        ///     The primary key.
        /// </param>
        /// <returns>
        ///     The object with the given primary key. Throws a not found exception
        ///     if the object is not found.
        /// </returns>
        [PublicAPI]
        public T Get<T>(object pk) where T : class
        {
            var map = GetMapping(typeof (T));
            return Query<T>(map.GetByPrimaryKeySql, pk).First();
        }

        /// <summary>
        ///     Attempts to retrieve the first object that matches the predicate from the table
        ///     associated with the specified type.
        /// </summary>
        /// <param name="predicate">
        ///     A predicate for which object to find.
        /// </param>
        /// <returns>
        ///     The object that matches the given predicate. Throws a not found exception
        ///     if the object is not found.
        /// </returns>
        [PublicAPI]
        public T Get<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return Table<T>().Where(predicate).First();
        }

        /// <summary>
        ///     Attempts to retrieve an object with the given primary key from the table
        ///     associated with the specified type. Use of this method requires that
        ///     the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        ///     The primary key.
        /// </param>
        /// <returns>
        ///     The object with the given primary key or null
        ///     if the object is not found.
        /// </returns>
        [PublicAPI]
        public T Find<T>(object pk) where T : class
        {
            var map = GetMapping(typeof (T));
            return Query<T>(map.GetByPrimaryKeySql, pk).FirstOrDefault();
        }

        /// <summary>
        ///     Attempts to retrieve the first object that matches the query from the table
        ///     associated with the specified type.
        /// </summary>
        /// <param name="query">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///     The object that matches the given predicate or null
        ///     if the object is not found.
        /// </returns>
        [PublicAPI]
        public T FindWithQuery<T>(string query, params object[] args) where T : class
        {
            return Query<T>(query, args).FirstOrDefault();
        }

        /// <summary>
        ///     Attempts to retrieve an object with the given primary key from the table
        ///     associated with the specified type. Use of this method requires that
        ///     the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        ///     The primary key.
        /// </param>
        /// <param name="map">
        ///     The TableMapping used to identify the object type.
        /// </param>
        /// <returns>
        ///     The object with the given primary key or null
        ///     if the object is not found.
        /// </returns>
        [PublicAPI]
        public object Find(object pk, TableMapping map)
        {
            return Query(map, map.GetByPrimaryKeySql, pk).FirstOrDefault();
        }

        /// <summary>
        ///     Attempts to retrieve the first object that matches the predicate from the table
        ///     associated with the specified type.
        /// </summary>
        /// <param name="predicate">
        ///     A predicate for which object to find.
        /// </param>
        /// <returns>
        ///     The object that matches the given predicate or null
        ///     if the object is not found.
        /// </returns>
        [PublicAPI]
        public T Find<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return Table<T>().Where(predicate).FirstOrDefault();
        }

        /// <summary>
        ///     Begins a new transaction. Call <see cref="Commit" /> to end the transaction.
        /// </summary>
        /// <example cref="System.InvalidOperationException">Throws if a transaction has already begun.</example>
        [PublicAPI]
        public void BeginTransaction()
        {
            // The BEGIN command only works if the transaction stack is empty, 
            //    or in other words if there are no pending transactions. 
            // If the transaction stack is not empty when the BEGIN command is invoked, 
            //    then the command fails with an error.
            // Rather than crash with an error, we will just ignore calls to BeginTransaction
            //    that would result in an error.
            if (Interlocked.CompareExchange(ref _transactionDepth, 1, 0) == 0)
            {
                try
                {
                    Execute("begin transaction");
                }
                catch (Exception ex)
                {
                    var sqlExp = ex as SQLiteException;
                    if (sqlExp != null)
                    {
                        // It is recommended that applications respond to the errors listed below 
                        //    by explicitly issuing a ROLLBACK command.
                        // TODO: This rollback failsafe should be localized to all throw sites.
                        switch (sqlExp.Result)
                        {
                            case Result.IOError:
                            case Result.Full:
                            case Result.Busy:
                            case Result.NoMem:
                            case Result.Interrupt:
                                RollbackTo(null, true);
                                break;
                        }
                    }
                    else
                    {
                        // Call decrement and not VolatileWrite in case we've already 
                        //    created a transaction point in SaveTransactionPoint since the catch.
                        Interlocked.Decrement(ref _transactionDepth);
                    }

                    throw;
                }
            }
            else
            {
                // Calling BeginTransaction on an already open transaction is invalid
                throw new InvalidOperationException("Cannot begin a transaction while already in a transaction.");
            }
        }

        /// <summary>
        ///     Creates a savepoint in the database at the current point in the transaction timeline.
        ///     Begins a new transaction if one is not in progress.
        ///     Call <see cref="RollbackTo" /> to undo transactions since the returned savepoint.
        ///     Call <see cref="Release" /> to commit transactions after the savepoint returned here.
        ///     Call <see cref="Commit" /> to end the transaction, committing all changes.
        /// </summary>
        /// <returns>A string naming the savepoint.</returns>
        [PublicAPI]
        public string SaveTransactionPoint()
        {
            var depth = Interlocked.Increment(ref _transactionDepth) - 1;
            var retVal = "S" + _rand.Next(short.MaxValue) + "D" + depth;

            try
            {
                Execute("savepoint " + retVal);
            }
            catch (Exception ex)
            {
                var sqlExp = ex as SQLiteException;
                if (sqlExp != null)
                {
                    // It is recommended that applications respond to the errors listed below 
                    //    by explicitly issuing a ROLLBACK command.
                    // TODO: This rollback failsafe should be localized to all throw sites.
                    switch (sqlExp.Result)
                    {
                        case Result.IOError:
                        case Result.Full:
                        case Result.Busy:
                        case Result.NoMem:
                        case Result.Interrupt:
                            RollbackTo(null, true);
                            break;
                    }
                }
                else
                {
                    Interlocked.Decrement(ref _transactionDepth);
                }

                throw;
            }

            return retVal;
        }

        /// <summary>
        ///     Rolls back the transaction that was begun by <see cref="BeginTransaction" /> or <see cref="SaveTransactionPoint" />
        ///     .
        /// </summary>
        [PublicAPI]
        public void Rollback()
        {
            RollbackTo(null, false);
        }

        /// <summary>
        ///     Rolls back the savepoint created by <see cref="BeginTransaction" /> or SaveTransactionPoint.
        /// </summary>
        /// <param name="savepoint">
        ///     The name of the savepoint to roll back to, as returned by <see cref="SaveTransactionPoint" />.
        ///     If savepoint is null or empty, this method is equivalent to a call to <see cref="Rollback" />
        /// </param>
        [PublicAPI]
        public void RollbackTo(string savepoint)
        {
            RollbackTo(savepoint, false);
        }

        /// <summary>
        ///     Rolls back the transaction that was begun by <see cref="BeginTransaction" />.
        /// </summary>
        /// <param name="savepoint">the savepoint name/key</param>
        /// <param name="noThrow">true to avoid throwing exceptions, false otherwise</param>
        private void RollbackTo(string savepoint, bool noThrow)
        {
            // Rolling back without a TO clause rolls backs all transactions 
            //    and leaves the transaction stack empty.   
            try
            {
                if (string.IsNullOrEmpty(savepoint))
                {
                    if (Interlocked.Exchange(ref _transactionDepth, 0) > 0)
                    {
                        Execute("rollback");
                    }
                }
                else
                {
                    DoSavePointExecute(savepoint, "rollback to ");
                }
            }
            catch (SQLiteException)
            {
                if (!noThrow)
                {
                    throw;
                }
            }
            // No need to rollback if there are no transactions open.
        }

        /// <summary>
        ///     Releases a savepoint returned from <see cref="SaveTransactionPoint" />.  Releasing a savepoint
        ///     makes changes since that savepoint permanent if the savepoint began the transaction,
        ///     or otherwise the changes are permanent pending a call to <see cref="Commit" />.
        ///     The RELEASE command is like a COMMIT for a SAVEPOINT.
        /// </summary>
        /// <param name="savepoint">
        ///     The name of the savepoint to release.  The string should be the result of a call to
        ///     <see cref="SaveTransactionPoint" />
        /// </param>
        [PublicAPI]
        public void Release(string savepoint)
        {
            DoSavePointExecute(savepoint, "release ");
        }

        private void DoSavePointExecute(string savePoint, string cmd)
        {
            // Validate the savepoint
            var firstLen = savePoint.IndexOf('D');
            if (firstLen >= 2 && savePoint.Length > firstLen + 1)
            {
                int depth;
                if (int.TryParse(savePoint.Substring(firstLen + 1), out depth))
                {
                    // TODO: Mild race here, but inescapable without locking almost everywhere.
                    if (0 <= depth && depth < _transactionDepth)
                    {
                        Platform.VolatileService.Write(ref _transactionDepth, depth);
                        Execute(cmd + savePoint);
                        return;
                    }
                }
            }

            throw new ArgumentException(
                "savePoint is not valid, and should be the result of a call to SaveTransactionPoint.", "savePoint");
        }

        /// <summary>
        ///     Commits the transaction that was begun by <see cref="BeginTransaction" />.
        /// </summary>
        [PublicAPI]
        public void Commit()
        {
            if (Interlocked.Exchange(ref _transactionDepth, 0) != 0)
            {
                Execute("commit");
            }
            // Do nothing on a commit with no open transaction
        }

        /// <summary>
        ///     Executes
        ///     <paramref name="action" />
        ///     within a (possibly nested) transaction by wrapping it in a SAVEPOINT. If an
        ///     exception occurs the whole transaction is rolled back, not just the current savepoint. The exception
        ///     is rethrown.
        /// </summary>
        /// <param name="action">
        ///     The <see cref="Action" /> to perform within a transaction.
        ///     <paramref name="action" />
        ///     can contain any number
        ///     of operations on the connection but should never call <see cref="BeginTransaction" /> or
        ///     <see cref="Commit" />.
        /// </param>
        [PublicAPI]
        public void RunInTransaction(Action action)
        {
            try
            {
                var savePoint = SaveTransactionPoint();
                action();
                Release(savePoint);
            }
            catch (Exception)
            {
                Rollback();
                throw;
            }
        }

        /// <summary>
        ///     Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        ///     An <see cref="IEnumerable" /> of the objects to insert.
        /// </param>
        /// <param name="runInTransaction">A boolean indicating if the inserts should be wrapped in a transaction.</param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        [PublicAPI]
        public int InsertAll(IEnumerable objects, bool runInTransaction = true)
        {
            var c = 0;
            if (runInTransaction)
            {
                RunInTransaction(() =>
                {
                    foreach (var r in objects)
                    {
                        c += Insert(r);
                    }
                });
            }
            else
            {
                foreach (var r in objects)
                {
                    c += Insert(r);
                }
            }
            return c;
        }

        /// <summary>
        ///     Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        ///     An <see cref="IEnumerable" /> of the objects to insert.
        /// </param>
        /// <param name="extra">
        ///     Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <param name="runInTransaction">A boolean indicating if the inserts should be wrapped in a transaction.</param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        [PublicAPI]
        public int InsertAll(IEnumerable objects, string extra, bool runInTransaction = true)
        {
            var c = 0;
            if (runInTransaction)
            {
                RunInTransaction(() =>
                {
                    foreach (var r in objects)
                    {
                        c += Insert(r, extra);
                    }
                });
            }
            else
            {
                foreach (var r in objects)
                {
                    c += Insert(r, extra);
                }
            }
            return c;
        }

        /// <summary>
        ///     Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        ///     An <see cref="IEnumerable" /> of the objects to insert.
        /// </param>
        /// <param name="objType">
        ///     The type of object to insert.
        /// </param>
        /// <param name="runInTransaction">A boolean indicating if the inserts should be wrapped in a transaction.</param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        [PublicAPI]
        public int InsertAll(IEnumerable objects, Type objType, bool runInTransaction = true)
        {
            var c = 0;
            if (runInTransaction)
            {
                RunInTransaction(() =>
                {
                    foreach (var r in objects)
                    {
                        c += Insert(r, objType);
                    }
                });
            }
            else
            {
                foreach (var r in objects)
                {
                    c += Insert(r, objType);
                }
            }
            return c;
        }

        [PublicAPI]
        public int InsertOrIgnoreAll (IEnumerable objects)
        {
            return InsertAll (objects, "OR IGNORE");
        }

        [PublicAPI]
        public int InsertOrIgnore (object obj)
        {
            if (obj == null) {
                return 0;
            }
            return Insert (obj, "OR IGNORE", obj.GetType ());
        }

        [PublicAPI]
        public int InsertOrIgnore (object obj, Type objType)
        {
            return Insert (obj, "OR IGNORE", objType);
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        [PublicAPI]
        public int Insert(object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            return Insert(obj, "", obj.GetType());
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        ///     If a UNIQUE constraint violation occurs with
        ///     some pre-existing object, this function deletes
        ///     the old object.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows modified.
        /// </returns>
        [PublicAPI]
        public int InsertOrReplace(object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            return Insert(obj, "OR REPLACE", obj.GetType());
        }

        /// <summary>
        ///     Inserts all specified objects.
        ///     For each insertion, if a UNIQUE
        ///     constraint violation occurs with
        ///     some pre-existing object, this function
        ///     deletes the old object.
        /// </summary>
        /// <param name="objects">
        ///     An <see cref="IEnumerable" /> of the objects to insert or replace.
        /// </param>
        /// <returns>
        ///     The total number of rows modified.
        /// </returns>
        [PublicAPI]
        public int InsertOrReplaceAll(IEnumerable objects)
        {
            var c = 0;
            RunInTransaction(() =>
            {
                foreach (var r in objects)
                {
                    c += InsertOrReplace(r);
                }
            });
            return c;
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <param name="objType">
        ///     The type of object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        [PublicAPI]
        public int Insert(object obj, Type objType)
        {
            return Insert(obj, "", objType);
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        ///     If a UNIQUE constraint violation occurs with
        ///     some pre-existing object, this function deletes
        ///     the old object.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <param name="objType">
        ///     The type of object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows modified.
        /// </returns>
        [PublicAPI]
        public int InsertOrReplace(object obj, Type objType)
        {
            return Insert(obj, "OR REPLACE", objType);
        }

        /// <summary>
        ///     Inserts all specified objects.
        ///     For each insertion, if a UNIQUE
        ///     constraint violation occurs with
        ///     some pre-existing object, this function
        ///     deletes the old object.
        /// </summary>
        /// <param name="objects">
        ///     An <see cref="IEnumerable" /> of the objects to insert or replace.
        /// </param>
        /// <param name="objType">
        ///     The type of objects to insert or replace.
        /// </param>
        /// <returns>
        ///     The total number of rows modified.
        /// </returns>
        [PublicAPI]
        public int InsertOrReplaceAll(IEnumerable objects, Type objType)
        {
            var c = 0;
            RunInTransaction(() =>
            {
                foreach (var r in objects)
                {
                    c += InsertOrReplace(r, objType);
                }
            });
            return c;
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <param name="extra">
        ///     Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        [PublicAPI]
        public int Insert(object obj, string extra)
        {
            if (obj == null)
            {
                return 0;
            }
            return Insert(obj, extra, obj.GetType());
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <param name="extra">
        ///     Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <param name="objType">
        ///     The type of object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        [PublicAPI]
        public int Insert(object obj, string extra, Type objType)
        {
            if (obj == null || objType == null)
            {
                return 0;
            }

            var map = GetMapping(objType);

            if (map.PK != null && map.PK.IsAutoGuid)
            {
                var prop = objType.GetRuntimeProperty(map.PK.PropertyName);
                if (prop != null)
                {
                    if (prop.GetValue(obj, null).Equals(Guid.Empty))
                    {
                        prop.SetValue(obj, Guid.NewGuid(), null);
                    }
                }
            }

            var replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;

            var cols = replacing ? map.Columns : map.InsertColumns;
            var vals = new object[cols.Length];
            for (var i = 0; i < vals.Length; i++)
            {
                vals[i] = cols[i].GetValue(obj);
            }

            var insertCmd = GetInsertCommand(map, extra);
            int count;
            try
            {
                count = insertCmd.ExecuteNonQuery(vals);
            }
            catch (SQLiteException ex)
            {
                if (Platform.SQLiteApi.ExtendedErrCode(Handle) == ExtendedResult.ConstraintNotNull)
                {
                    throw NotNullConstraintViolationException.New(ex.Result, ex.Message, map, obj);
                }
                throw;
            }

            if (map.HasAutoIncPK)
            {
                var id = Platform.SQLiteApi.LastInsertRowid(Handle);
                map.SetAutoIncPK(obj, id);
            }

            return count;
        }

        private PreparedSqlLiteInsertCommand GetInsertCommand(TableMapping map, string extra)
        {
            ActiveInsertCommand cmd;
            if (_insertCommandCache == null)
            {
                _insertCommandCache = new Dictionary<TableMapping, ActiveInsertCommand>();
            }

            if (!_insertCommandCache.TryGetValue(map, out cmd))
            {
                cmd = new ActiveInsertCommand(map);
                _insertCommandCache[map] = cmd;
            }

            return cmd.GetCommand(this, extra);
        }

        /// <summary>
        ///     Updates all of the columns of a table using the specified object
        ///     except for its primary key.
        ///     The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        ///     The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        ///     The number of rows updated.
        /// </returns>
        [PublicAPI]
        public int Update(object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            return Update(obj, obj.GetType());
        }

        /// <summary>
        ///     Updates all of the columns of a table using the specified object
        ///     except for its primary key.
        ///     The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        ///     The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <param name="objType">
        ///     The type of object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows updated.
        /// </returns>
        [PublicAPI]
        public int Update(object obj, Type objType)
        {
            int rowsAffected;
            if (obj == null || objType == null)
            {
                return 0;
            }

            var map = GetMapping(objType);

            var pk = map.PK;

            if (pk == null)
            {
                throw new NotSupportedException("Cannot update " + map.TableName + ": it has no PK");
            }

            var cols = from p in map.Columns
                where p != pk
                select p;
            var vals = from c in cols
                select c.GetValue(obj);
            var ps = new List<object>(vals)
            {
                pk.GetValue(obj)
            };
            var q = string.Format("update \"{0}\" set {1} where {2} = ? ", map.TableName,
                string.Join(",", (from c in cols
                    select "\"" + c.Name + "\" = ? ").ToArray()), pk.Name);
            try
            {
                rowsAffected = Execute(q, ps.ToArray());
            }
            catch (SQLiteException ex)
            {
                if (ex.Result == Result.Constraint && Platform.SQLiteApi.ExtendedErrCode(Handle) == ExtendedResult.ConstraintNotNull)
                {
                    throw NotNullConstraintViolationException.New(ex, map, obj);
                }

                throw;
            }

            return rowsAffected;
        }

        /// <summary>
        ///     Updates all specified objects.
        /// </summary>
        /// <param name="objects">
        ///     An <see cref="IEnumerable" /> of the objects to insert.
        /// </param>
        /// <param name="runInTransaction">A boolean indicating if the inserts should be wrapped in a transaction.</param>
        /// <returns>
        ///     The number of rows modified.
        /// </returns>
        [PublicAPI]
        public int UpdateAll(IEnumerable objects, bool runInTransaction = true)
        {
            var c = 0;
            if (runInTransaction)
            {
                RunInTransaction(() =>
                {
                    foreach (var r in objects)
                    {
                        c += Update(r);
                    }
                });
            }
            else
            {
                foreach (var r in objects)
                {
                    c += Update(r);
                }
            }
            return c;
        }

        /// <summary>
        ///     Deletes the given object from the database using its primary key.
        /// </summary>
        /// <param name="objectToDelete">
        ///     The object to delete. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        ///     The number of rows deleted.
        /// </returns>
        [PublicAPI]
        public int Delete(object objectToDelete)
        {
            var map = GetMapping(objectToDelete.GetType());
            var pk = map.PK;
            if (pk == null)
            {
                throw new NotSupportedException("Cannot delete " + map.TableName + ": it has no PK");
            }
            var q = string.Format("delete from \"{0}\" where \"{1}\" = ?", map.TableName, pk.Name);
            return Execute(q, pk.GetValue(objectToDelete));
        }

        /// <summary>
        ///     Deletes the object with the specified primary key.
        /// </summary>
        /// <param name="primaryKey">
        ///     The primary key of the object to delete.
        /// </param>
        /// <returns>
        ///     The number of objects deleted.
        /// </returns>
        /// <typeparam name='T'>
        ///     The type of object.
        /// </typeparam>
        [PublicAPI]
        public int Delete<T>(object primaryKey)
        {
            var map = GetMapping(typeof (T));
            var pk = map.PK;
            if (pk == null)
            {
                throw new NotSupportedException("Cannot delete " + map.TableName + ": it has no PK");
            }
            var q = string.Format("delete from \"{0}\" where \"{1}\" = ?", map.TableName, pk.Name);
            return Execute(q, primaryKey);
        }

        /// <summary>
        ///     Deletes all the objects from the specified table.
        ///     WARNING WARNING: Let me repeat. It deletes ALL the objects from the
        ///     specified table. Do you really want to do that?
        /// </summary>
        /// <returns>
        ///     The number of objects deleted.
        /// </returns>
        /// <typeparam name='T'>
        ///     The type of objects to delete.
        /// </typeparam>
        [PublicAPI]
        public int DeleteAll<T>()
        {
            return DeleteAll(typeof (T));
        }

        /// <summary>
        ///     Deletes all the objects from the specified table.
        ///     WARNING WARNING: Let me repeat. It deletes ALL the objects from the
        ///     specified table. Do you really want to do that?
        /// </summary>
        /// <returns>
        ///     The number of objects deleted.
        /// </returns>
        [PublicAPI]
        public int DeleteAll(Type t)
        {
            var map = GetMapping(t);
            var query = string.Format("delete from \"{0}\"", map.TableName);
            return Execute(query);
        }

        #region Backup

        public string CreateDatabaseBackup(ISQLitePlatform platform)
        {
            ISQLiteApiExt sqliteApi = platform.SQLiteApi as ISQLiteApiExt;

            if (sqliteApi == null)
            {
                return null;
            }

            string destDBPath = this.DatabasePath + "." + DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");

            IDbHandle destDB;
            byte[] databasePathAsBytes = GetNullTerminatedUtf8(destDBPath);
            Result r = sqliteApi.Open(databasePathAsBytes, out destDB,
                (int) (SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite), IntPtr.Zero);

            if (r != Result.OK)
            {
                throw SQLiteException.New(r, String.Format("Could not open backup database file: {0} ({1})", destDBPath, r));
            }

            /* Open the backup object used to accomplish the transfer */
            IDbBackupHandle bHandle = sqliteApi.BackupInit(destDB, "main", this.Handle, "main");

            if (bHandle == null)
            {
                // Close the database connection 
                sqliteApi.Close(destDB);

                throw SQLiteException.New(r, String.Format("Could not initiate backup process: {0}", destDBPath));
            }

            /* Each iteration of this loop copies 5 database pages from database
            ** pDb to the backup database. If the return value of backup_step()
            ** indicates that there are still further pages to copy, sleep for
            ** 250 ms before repeating. */
            do
            {
                r = sqliteApi.BackupStep(bHandle, 5);

                if (r == Result.OK || r == Result.Busy || r == Result.Locked)
                {
                    sqliteApi.Sleep(250);
                }
            } while (r == Result.OK || r == Result.Busy || r == Result.Locked);

            /* Release resources allocated by backup_init(). */
            r = sqliteApi.BackupFinish(bHandle);

            if (r != Result.OK)
            {
                // Close the database connection 
                sqliteApi.Close(destDB);

                throw SQLiteException.New(r, String.Format("Could not finish backup process: {0} ({1})", destDBPath, r));
            }

            // Close the database connection 
            sqliteApi.Close(destDB);

            return destDBPath;
        }

        #endregion

        ~SQLiteConnection()
        {
            Dispose(false);
        }

        [PublicAPI]
        protected virtual void Dispose(bool disposing)
        {
            Close();
        }

        [PublicAPI]
        public void Close()
        {
            if (_open && Handle != NullHandle)
            {
                try
                {
                    if (_insertCommandCache != null)
                    {
                        foreach (var sqlInsertCommand in _insertCommandCache.Values)
                        {
                            sqlInsertCommand.Dispose();
                        }
                    }
                    var r = Platform.SQLiteApi.Close(Handle);
                    if (r != Result.OK)
                    {
                        var msg = Platform.SQLiteApi.Errmsg16(Handle);
                        throw SQLiteException.New(r, msg);
                    }
                }
                finally
                {
                    Handle = NullHandle;
                    _open = false;
                }
            }
        }

        public class ColumnInfo
        {
            //			public int cid { get; set; }

            [PublicAPI]
            [Column("name")]
            public string Name { get; set; }

            //			[Column ("type")]
            //			public string ColumnType { get; set; }

            [PublicAPI]
            public int notnull { get; set; }

            //			public string dflt_value { get; set; }

            //			public int pk { get; set; }

            [PublicAPI]
            public override string ToString()
            {
                return Name;
            }
        }

        private struct IndexInfo
        {
            public List<IndexedColumn> Columns;
            public string IndexName;
            public string TableName;
            public bool Unique;
        }

        private struct IndexedColumn
        {
            public string ColumnName;
            public int Order;
        }
    }
}