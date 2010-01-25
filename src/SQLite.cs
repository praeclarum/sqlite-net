//
// Copyright (c) 2009-2010 Krueger Systems, Inc.
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
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace SQLite
{
	public class SQLiteException : System.Exception
	{
		public SQLite3.Result Result { get; private set; }
		protected SQLiteException (SQLite3.Result r, string message) : base(message)
		{
			Result = r;
		}
		public static SQLiteException New (SQLite3.Result r, string message)
		{
			return new SQLiteException (r, message);
		}
	}

	public class SQLiteConnection : IDisposable
	{
		private bool _open;
		Dictionary<string, TableMapping> _mappings = null;
		Dictionary<string, TableMapping> _tables = null;

		public IntPtr Handle { get; private set; }
		public string DatabasePath { get; private set; }
		public bool Trace { get; set; }

		public SQLiteConnection (string databasePath)
		{
			DatabasePath = databasePath;
			IntPtr handle;
			var r = SQLite3.Open (DatabasePath, out handle);
			Handle = handle;
			if (r != SQLite3.Result.OK) {
				throw SQLiteException.New (r, "Could not open database file: " + DatabasePath);
			}
			_open = true;
		}
		
		public SQLiteCommand CreateCommand (string cmdText, params object[] ps)
		{
			if (!_open) {
				throw SQLiteException.New (SQLite3.Result.Error, "Cannot create commands from unopened database");
			} else {
				var cmd = new SQLiteCommand (this);
				cmd.CommandText = cmdText;
				foreach (var o in ps) {
					cmd.Bind (o);
				}
				return cmd;
			}
		}
		
		
		public IEnumerable<TableMapping> Tables {
			get {
				if (_tables == null) {
					return Enumerable.Empty<TableMapping>();
				}
				else {
					return _tables.Values;
				}
			}
		}
		public int CreateTable<T> ()
		{
			var ty = typeof(T);
			
			if (_tables == null) {
				_tables = new Dictionary<string, TableMapping>();
			}
			TableMapping map;
			if (!_tables.TryGetValue(ty.FullName, out map)) {
				map = GetMapping(ty);
				_tables.Add(ty.FullName, map);
			}
			var query = "create table if not exists \"" + map.TableName + "\"(\n";
			
			var decls = map.Columns.Select (p => Orm.SqlDecl (p));
			var decl = string.Join (",\n", decls.ToArray ());
			query += decl;
			query += ")";
			
			var count = Execute (query);
			
			foreach (var p in map.Columns.Where (x => x.IsIndexed)) {
				var indexName = map.TableName + "_" + p.Name;
				var q = string.Format ("create index if not exists \"{0}\" on \"{1}\"(\"{2}\")", indexName, map.TableName, p.Name);
				count += Execute (q);
			}
			
			return count;
		}
		
		public TableMapping GetMapping(Type type) {
			if (_mappings == null) {
				_mappings = new Dictionary<string, TableMapping>();
			}
			TableMapping map;
			if (!_mappings.TryGetValue(type.FullName, out map)) {
				map = new TableMapping(type);
				_mappings[type.FullName] = map;
			}
			return map;
		}
		
		System.Diagnostics.Stopwatch _sw;
		long _elapsedMilliseconds = 0;

		public int Execute (string query, params object[] ps)
		{
			var cmd = CreateCommand (query, ps);
			if (Trace) {
				Console.WriteLine ("Executing: " + cmd);
				if (_sw == null) {
					_sw = new System.Diagnostics.Stopwatch();
				}
				_sw.Reset();
				_sw.Start();
			}
			int r = cmd.ExecuteNonQuery ();
			if (Trace) {
				_sw.Stop();
				_elapsedMilliseconds += _sw.ElapsedMilliseconds;
				Console.WriteLine ("Finished in {0} ms ({1:0.0} s total)", _sw.ElapsedMilliseconds, _elapsedMilliseconds/1000.0);
			}
			return r;
		}

		public IEnumerable<T> Query<T> (string query, params object[] ps) where T : new()
		{
			var cmd = CreateCommand (query, ps);
			return cmd.ExecuteQuery<T> ();
		}
		public IEnumerable<object> Query (TableMapping map, string query, params object[] ps)
		{
			var cmd = CreateCommand (query, ps);
			return cmd.ExecuteQuery (map);
		}

		/// <summary>
		/// Inserts all specified objects.
		/// </summary>
		/// <param name="objects">
		/// A <see cref="IEnumerable<T>"/> of objects to insert.
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/> of the number of rows added to the table.
		/// </returns>
		public int InsertAll<T> (IEnumerable<T> objects)
		{
			var map = GetMapping(typeof(T));
			var c = 0;
			foreach (var r in objects) {
				c += Insert (r, map);
			}
			return c;
		}

		public int Insert<T> (T obj)
		{
			return Insert (obj, GetMapping(obj.GetType()));
		}
		public int Insert<T> (T obj, TableMapping map)
		{
			var vals =	from c in map.InsertColumns
						select c.GetValue (obj);
			
			var count = Execute (map.InsertSql, vals.ToArray ());
			
			var id = SQLite3.LastInsertRowid(Handle);
			map.SetAutoIncPK(obj, id);
			
			return count;
		}
		
		public void Delete<T>(T obj)
		{
			var map = GetMapping(obj.GetType ());
			var pk = map.PK;
			if (pk == null) {
				throw new NotSupportedException ("Cannot delete " + map.TableName + ": it has no PK");
			}			
			var q = string.Format("delete from \"{0}\" where \"{1}\" = ?",
			                      map.TableName,
			                      pk.Name);
			Execute(q, pk.GetValue(obj));
		}
		
		public T Get<T> (object pk) where T : new()
		{
			var map = GetMapping(typeof(T));
			string query = string.Format ("select * from \"{0}\" where \"{1}\" = ?", map.TableName, map.PK.Name);
			return Query<T> (query, pk).First ();
		}

		public int Update (object obj)
		{
			if (obj == null) { return 0; }
			
			var map = GetMapping(obj.GetType ());
			
			var pk = map.PK;
			
			if (pk == null) {
				throw new NotSupportedException ("Cannot update " + map.TableName + ": it has no PK");
			}
			var cols = from p in map.Columns
				where p != pk
				select p;
			var vals = from c in cols
				select c.GetValue (obj);
			var ps = new List<object> (vals);
			ps.Add(pk.GetValue(obj)); 
			var q = string.Format("update \"{0}\" set {1} where {2} = ? ",
			    map.TableName,
			    string.Join(",", (from c in cols select "\"" + c.Name + "\" = ? ").ToArray()),
			    pk.Name); 
			return Execute (q, ps.ToArray ());
		}

		public void Dispose ()
		{
			if (_open) {
				SQLite3.Close (Handle);
				Handle = IntPtr.Zero;
				_open = false;
			}
		}
	}

	public class PrimaryKeyAttribute : Attribute
	{
	}
	public class AutoIncrementAttribute : Attribute
	{
	}
	public class IndexedAttribute : Attribute
	{
	}
	public class MaxLengthAttribute : Attribute
	{
		public int Value { get; private set; }
		public MaxLengthAttribute (int length)
		{
			Value = length;
		}
	}
	
	public class TableMapping {
		public Type MappedType { get; private set; }
		public Column[] Columns { get; private set; }
		
		public TableMapping(Type type) {
			MappedType = type;
			TableName = MappedType.Name;
			var props = MappedType.GetProperties();
			Columns = new Column[props.Length];
			for (int i = 0; i < props.Length; i++) {
				Columns[i] = new PropColumn(props[i]);
			}
			foreach (var c in Columns) {
				if (c.IsAutoInc && c.IsPK) {
					_autoPk = c;
				}
				if (c.IsPK) {
					PK = c;
				}
			}
		}
		public string TableName { get; private set; }
		public Column PK { get; private set; }
		Column _autoPk = null;
		public void SetAutoIncPK(object obj, long id) {
			if (_autoPk != null) {
				_autoPk.SetValue(obj, Convert.ChangeType(id, _autoPk.ColumnType));
			}
		}
		string _insertSql = null;
		Column[] _insertColumns = null;
		public Column[] InsertColumns {
			get {
				if (_insertColumns == null) {
					_insertColumns = Columns.Where(c => !c.IsAutoInc).ToArray();
				}
				return _insertColumns;
			}
		}
		public Column FindColumn(string name) {
			var exact = Columns.Where(c => c.Name == name).FirstOrDefault();
			return exact;
		}
		public string InsertSql {
			get {
				if (_insertSql == null) {
					var cols = InsertColumns;
					_insertSql = string.Format ("insert into \"{0}\"({1}) values ({2})", TableName, string.Join (",", (from c in cols
						select "\"" + c.Name + "\"").ToArray ()), string.Join (",", (from c in cols
						select "?").ToArray ()));
				}
				return _insertSql;
			}
		}
		public abstract class Column {
			public string Name { get; protected set; }
			public Type ColumnType { get; protected set; }
			public bool IsAutoInc { get; protected set; }
			public bool IsPK { get; protected set; }
			public bool IsIndexed { get; protected set; }
			public bool IsNullable { get; protected set; }
			public int MaxStringLength { get; protected set; }
			public abstract void SetValue(object obj, object val);
			public abstract object GetValue(object obj);
		}
		public class PropColumn : Column {
			PropertyInfo _prop;
			public PropColumn(PropertyInfo prop) {
				_prop = prop;
				Name = prop.Name;
				ColumnType = prop.PropertyType;
				IsAutoInc = Orm.IsAutoInc(prop);
				IsPK = Orm.IsPK(prop);
				IsIndexed = Orm.IsIndexed(prop);
				IsNullable = !IsPK;
				MaxStringLength = Orm.MaxStringLength(prop);
			}
			public override void SetValue(object obj, object val) {
				_prop.SetValue(obj, val, null);
			}
			public override object GetValue(object obj) {
				return _prop.GetValue(obj, null);
			}
		}
	}


	public static class Orm
	{
		public const int DefaultMaxStringLength = 140;

		public static string SqlDecl (TableMapping.Column p)
		{
			string decl = "\"" + p.Name + "\" " + SqlType (p) + " ";
			
			if (p.IsPK) {
				decl += "primary key ";
			}
			if (p.IsAutoInc) {
				decl += "autoincrement ";
			}
			if (!p.IsNullable) {
				decl += "not null ";
			}
			
			return decl;
		}

		public static string SqlType (TableMapping.Column p)
		{
			var clrType = p.ColumnType;
			if (clrType == typeof(Boolean) || clrType == typeof(Byte) || clrType == typeof(UInt16) || clrType == typeof(SByte) || clrType == typeof(Int16) || clrType == typeof(Int32)) {
				return "integer";
			} else if (clrType == typeof(UInt32) || clrType == typeof(Int64)) {
				return "bigint";
			} else if (clrType == typeof(Single) || clrType == typeof(Double) || clrType == typeof(Decimal)) {
				return "float";
			} else if (clrType == typeof(String)) {
				int len = p.MaxStringLength;
				return "varchar(" + len + ")";
			} else if (clrType == typeof(DateTime)) {
				return "datetime";
			} else {
				throw new NotSupportedException ("Don't know about " + clrType);
			}
		}

		public static bool IsPK (MemberInfo p)
		{
			var attrs = p.GetCustomAttributes (typeof(PrimaryKeyAttribute), true);
			return attrs.Length > 0;
		}

		public static bool IsAutoInc (MemberInfo p)
		{
			var attrs = p.GetCustomAttributes (typeof(AutoIncrementAttribute), true);
			return attrs.Length > 0;
		}

		public static bool IsIndexed (MemberInfo p)
		{
			var attrs = p.GetCustomAttributes (typeof(IndexedAttribute), true);
			return attrs.Length > 0;
		}

		public static int MaxStringLength (PropertyInfo p)
		{
			var attrs = p.GetCustomAttributes (typeof(MaxLengthAttribute), true);
			if (attrs.Length > 0) {
				return ((MaxLengthAttribute)attrs[0]).Value;
			} else {
				return DefaultMaxStringLength;
			}
		}
		
	}

	public class SQLiteCommand
	{
		SQLiteConnection _conn;
		private List<Binding> _bindings;

		public string CommandText { get; set; }

		internal SQLiteCommand (SQLiteConnection conn)
		{
			_conn = conn;
			_bindings = new List<Binding> ();
			CommandText = "";
		}

		public int ExecuteNonQuery ()
		{
			var stmt = Prepare ();
			
			var r = SQLite3.Step (stmt);
			if (r == SQLite3.Result.Error) {
				string msg = SQLite3.Errmsg (_conn.Handle);
				SQLite3.Finalize (stmt);
				throw SQLiteException.New (r, msg);
			} else if (r == SQLite3.Result.Done) {
				int rowsAffected = SQLite3.Changes (_conn.Handle);
				SQLite3.Finalize (stmt);
				return rowsAffected;
			} else {
				SQLite3.Finalize (stmt);
				throw SQLiteException.New (r, "Unknown error");
			}
		}
		
		public IEnumerable<T> ExecuteQuery<T> () where T : new()		
		{
			return ExecuteQuery(_conn.GetMapping(typeof(T))).Cast<T>();
		}

		public IEnumerable<object> ExecuteQuery (TableMapping map)
		{
			if (_conn.Trace) {
				Console.WriteLine ("Executing Query: " + this);
			}
			
			var stmt = Prepare ();
			
			var cols = new TableMapping.Column[SQLite3.ColumnCount (stmt)];
			for (int i = 0; i < cols.Length; i++) {
				var name = Marshal.PtrToStringAuto(SQLite3.ColumnName (stmt, i));
				cols[i] = map.FindColumn(name);
			}
			
			while (SQLite3.Step (stmt) == SQLite3.Result.Row) {
				var obj = Activator.CreateInstance(map.MappedType);
				for (int i = 0; i < cols.Length; i++) {
					if (cols[i] == null)
						continue;
					var val = ReadCol (stmt, i, cols[i].ColumnType);
					cols[i].SetValue (obj, val);
				}
				yield return obj;
			}
			
			SQLite3.Finalize (stmt);
		}
		
		public T ExecuteScalar<T>()
		{
			T val = default(T);

			var stmt = Prepare();
			if (SQLite3.Step(stmt) == SQLite3.Result.Row) {
				val = (T)ReadCol(stmt, 0, typeof(T));
			}
			SQLite3.Finalize(stmt);

			return val;
		}

		public void Bind (string name, object val)
		{
			_bindings.Add (new Binding {
				Name = name,
				Value = val
			});
		}
		public void Bind (object val)
		{
			Bind (null, val);
		}

		public override string ToString ()
		{
			return CommandText;
		}

		IntPtr Prepare ()
		{
			var stmt = SQLite3.Prepare (_conn.Handle, CommandText);
			BindAll (stmt);
			return stmt;
		}

		void BindAll (IntPtr stmt)
		{
			int nextIdx = 1;
			foreach (var b in _bindings) {
				if (b.Name != null) {
					b.Index = SQLite3.BindParameterIndex (stmt, b.Name);
				} else {
					b.Index = nextIdx++;
				}
			}
			foreach (var b in _bindings) {
				if (b.Value == null) {
					SQLite3.BindNull (stmt, b.Index);
				} else {
					if (b.Value is Byte || b.Value is UInt16 || b.Value is SByte || b.Value is Int16 || b.Value is Int32) {
						SQLite3.BindInt (stmt, b.Index, Convert.ToInt32 (b.Value));
					} else if (b.Value is Boolean) {
						SQLite3.BindInt(stmt, b.Index, Convert.ToBoolean(b.Value) ? 1 : 0);
					} else if (b.Value is UInt32 || b.Value is Int64) {
						SQLite3.BindInt64 (stmt, b.Index, Convert.ToInt64 (b.Value));
					} else if (b.Value is Single || b.Value is Double || b.Value is Decimal) {
						SQLite3.BindDouble (stmt, b.Index, Convert.ToDouble (b.Value));
					} else if (b.Value is String) {
						SQLite3.BindText (stmt, b.Index, b.Value.ToString (), -1, new IntPtr (-1));
					} else if (b.Value is DateTime) {
						SQLite3.BindText (stmt, b.Index, ((DateTime)b.Value).ToString ("yyyy-MM-dd HH:mm:ss"), -1, new IntPtr (-1));
					}
				}
			}
		}

		class Binding
		{
			public string Name { get; set; }
			public object Value { get; set; }
			public int Index { get; set; }
		}

		object ReadCol (IntPtr stmt, int index, Type clrType)
		{
			var type = SQLite3.ColumnType (stmt, index);
			if (type == SQLite3.ColType.Null) {
				return null;
			} else {
				if (clrType == typeof(Byte) || clrType == typeof(UInt16) || clrType == typeof(SByte) || clrType == typeof(Int16) || clrType == typeof(Int32)) {
					return Convert.ChangeType (SQLite3.ColumnInt (stmt, index), clrType);
				} else if (clrType == typeof(Boolean)) {
					return ((Byte)Convert.ChangeType (SQLite3.ColumnInt (stmt, index), typeof(Byte)) == 1);
				} else if (clrType == typeof(UInt32) || clrType == typeof(Int64)) {
					return Convert.ChangeType (SQLite3.ColumnInt64 (stmt, index), clrType);
				} else if (clrType == typeof(Single) || clrType == typeof(Double) || clrType == typeof(Decimal)) {
					return Convert.ChangeType (SQLite3.ColumnDouble (stmt, index), clrType);
				} else if (clrType == typeof(String)) {
					var text = Marshal.PtrToStringAuto(SQLite3.ColumnText (stmt, index));
					return text;
				} else if (clrType == typeof(DateTime)) {
					var text = Marshal.PtrToStringAuto(SQLite3.ColumnText (stmt, index));
					return Convert.ChangeType (text, clrType);
				} else {
					throw new NotSupportedException ("Don't know how to read " + clrType);
				}
			}
		}

	}

	public static class SQLite3
	{
		public enum Result : int
		{
			OK = 0,
			Error = 1,
			Row = 100,
			Done = 101
		}
		
		public enum ConfigOption : int {
			SingleThread = 1,
			MultiThread = 2,
			Serialized = 3,
		}		

		[DllImport("sqlite3", EntryPoint = "sqlite3_open")]
		public static extern Result Open (string filename, out IntPtr db);
		[DllImport("sqlite3", EntryPoint = "sqlite3_close")]
		public static extern Result Close (IntPtr db);
		[DllImport("sqlite3", EntryPoint = "sqlite3_config")]
		public static extern Result Config (ConfigOption option);

		[DllImport("sqlite3", EntryPoint = "sqlite3_changes")]
		public static extern int Changes (IntPtr db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_prepare_v2")]
		public static extern Result Prepare (IntPtr db, string sql, int numBytes, out IntPtr stmt, IntPtr pzTail);
		public static IntPtr Prepare (IntPtr db, string query)
		{
			IntPtr stmt;
			var r = Prepare (db, query, query.Length, out stmt, IntPtr.Zero);
			if (r != Result.OK) {
				throw SQLiteException.New (r, Errmsg (db));
			}
			return stmt;
		}

		[DllImport("sqlite3", EntryPoint = "sqlite3_step")]
		public static extern Result Step (IntPtr stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_finalize")]
		public static extern Result Finalize (IntPtr stmt);
		
		[DllImport("sqlite3", EntryPoint = "sqlite3_last_insert_rowid")]
		public static extern long LastInsertRowid (IntPtr db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_errmsg")]
		public static extern string Errmsg (IntPtr db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_parameter_index")]
		public static extern int BindParameterIndex (IntPtr stmt, string name);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_null")]
		public static extern int BindNull (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_int")]
		public static extern int BindInt (IntPtr stmt, int index, int val);
		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_int64")]
		public static extern int BindInt64 (IntPtr stmt, int index, long val);
		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_double")]
		public static extern int BindDouble (IntPtr stmt, int index, double val);
		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_text")]
		public static extern int BindText (IntPtr stmt, int index, string val, int n, IntPtr free);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_count")]
		public static extern int ColumnCount (IntPtr stmt);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_name")]
		public static extern IntPtr ColumnName (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_type")]
		public static extern ColType ColumnType (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_int")]
		public static extern int ColumnInt (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_int64")]
		public static extern long ColumnInt64 (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_double")]
		public static extern double ColumnDouble (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_text")]
		public static extern IntPtr ColumnText (IntPtr stmt, int index);

		public enum ColType : int
		{
			Integer = 1,
			Float = 2,
			Text = 3,
			Blob = 4,
			Null = 5
		}
	}
	
}
