//
// Copyright (c) 2009 Krueger Systems, Inc.
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
		private IntPtr _db;
		private bool _open;

		public string Database { get; set; }
		public bool Trace { get; set; }

		public SQLiteConnection (string database)
		{
			Database = database;
			var r = SQLite3.Open (Database, out _db);
			if (r != SQLite3.Result.OK) {
				throw SQLiteException.New (r, "Could not open database file: " + Database);
			}
			_open = true;
		}

		public SQLiteCommand CreateCommand (string cmdText, params object[] ps)
		{
			if (!_open) {
				throw SQLiteException.New (SQLite3.Result.Error, "Cannot create commands from unopened database");
			} else {
				var cmd = new SQLiteCommand (_db);
				cmd.CommandText = cmdText;
				foreach (var o in ps) {
					cmd.Bind (o);
				}
				return cmd;
			}
		}

		public int CreateTable<T> ()
		{
			var ty = typeof(T);
			var query = "create table if not exists '" + ty.Name + "'(\n";
			
			var decls = Orm.GetColumns (ty).Select (p => Orm.SqlDecl (p));
			var decl = string.Join (",\n", decls.ToArray ());
			query += decl;
			query += ")";
			
			var count = Execute (query);
			
			foreach (var p in Orm.GetColumns (ty).Where (x => Orm.IsIndexed (x))) {
				var indexName = ty.Name + "_" + p.Name;
				var q = string.Format ("create index if not exists '{0}' on '{1}'('{2}')", indexName, ty.Name, p.Name);
				count += Execute (q);
			}
			
			return count;
		}

		public int Execute (string query, params object[] ps)
		{
			var cmd = CreateCommand (query, ps);
			if (Trace) {
				Console.Error.WriteLine ("Executing: " + cmd);
			}
			return cmd.ExecuteNonQuery ();
		}

		public IEnumerable<T> Query<T> (string query, params object[] ps) where T : new()
		{
			var cmd = CreateCommand (query, ps);
			return cmd.ExecuteQuery<T> ();
		}

		public int InsertAll<T> (IEnumerable<T> rows)
		{
			var c = 0;
			foreach (var r in rows) {
				Insert (r);
				c++;
			}
			return c;
		}

		public T Insert<T> (T obj)
		{
			var type = obj.GetType ();
			var cols = Orm.GetColumns (type).Where(c => !Orm.IsAutoInc(c));
			var q = string.Format ("insert into '{0}'({1}) values ({2})", type.Name, string.Join (",", (from c in cols
				select "'" + c.Name + "'").ToArray ()), string.Join (",", (from c in cols
				select "?").ToArray ()));
			var vals = from c in cols
				select c.GetValue (obj, null);
			
			Execute (q, vals.ToArray ());
			
			var id = SQLite3.LastInsertRowid(_db);
			Orm.SetAutoIncPK(obj, id);
			return obj;
		}
		
		public void Delete<T>(T obj)
		{
			var type = obj.GetType ();
			var pk = Orm.GetColumns (type).Where(c => Orm.IsPK(c)).FirstOrDefault();
			if (pk == null) {
				throw new NotSupportedException ("Cannot delete " + type.Name + ": it has no PK");
			}			
			var q = string.Format("delete from '{0}' where '{1}' = ?",
			                      type.Name,
			                      pk.Name);
			Execute(q, pk.GetValue(obj, null));
		}

		public T Get<T> (object pk) where T : new()
		{
			string query = string.Format ("select * from '{0}' where '{1}'=?", typeof(T).Name, Orm.GetPK (typeof(T)).Name);
			return Query<T> (query, pk).First ();
		}

		public int Update (object obj)
		{
			if (obj == null)
				return 0;
			return Update (obj.GetType ().Name, obj);
		}

		public int Update (string name, object obj)
		{
			var type = obj.GetType ();
			var props = Orm.GetColumns (type);
			var pk = Orm.GetPK (type);
			if (pk == null) {
				throw new NotSupportedException ("Cannot update " + name + ": it has no PK");
			}
			var cols = from p in props
				where p != pk
				select p;
			var vals = from c in cols
				select c.GetValue (obj, null);
			var ps = new List<object> (vals);
			ps.Add(pk.GetValue(obj, null)); 
			var q = string.Format("update '{0}' set {1} where {2} = ? ",
			    type.Name,
			    string.Join(",", (from c in cols select "'" + c.Name + "' = ? ").ToArray()),
			    pk.Name); 
			return Execute (q, ps.ToArray ());
		}

		public void Dispose ()
		{
			if (_open) {
				SQLite3.Close (_db);
				_db = IntPtr.Zero;
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

	public static class Orm
	{
		public const int DefaultMaxStringLength = 140;

		public static string SqlDecl (PropertyInfo p)
		{
			string decl = "'" + p.Name + "' " + SqlType (p) + " ";
			
			if (IsPK (p)) {
				decl += "primary key ";
			}
			if (IsAutoInc (p)) {
				decl += "autoincrement ";
			}
			if (!IsNullable (p)) {
				decl += "not null ";
			}
			
			return decl;
		}

		public static string SqlType (PropertyInfo p)
		{
			var clrType = p.PropertyType;
			if (clrType == typeof(Byte) || clrType == typeof(UInt16) || clrType == typeof(SByte) || clrType == typeof(Int16) || clrType == typeof(Int32)) {
				return "integer";
			} else if (clrType == typeof(UInt32) || clrType == typeof(Int64)) {
				return "bigint";
			} else if (clrType == typeof(Single) || clrType == typeof(Double) || clrType == typeof(Decimal)) {
				return "float";
			} else if (clrType == typeof(String)) {
				int len = MaxStringLength (p);
				return "varchar(" + len + ")";
			} else if (clrType == typeof(DateTime)) {
				return "datetime";
			} else {
				throw new NotSupportedException ("Don't know about " + clrType);
			}
		}

		public static bool IsPK (PropertyInfo p)
		{
			var attrs = p.GetCustomAttributes (typeof(PrimaryKeyAttribute), true);
			return attrs.Length > 0;
		}

		public static bool IsAutoInc (PropertyInfo p)
		{
			var attrs = p.GetCustomAttributes (typeof(AutoIncrementAttribute), true);
			return attrs.Length > 0;
		}

		public static bool IsIndexed (PropertyInfo p)
		{
			var attrs = p.GetCustomAttributes (typeof(IndexedAttribute), true);
			return attrs.Length > 0;
		}

		public static bool IsNullable (PropertyInfo p)
		{
			return !IsPK (p);
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

		public static System.Reflection.PropertyInfo GetPK (Type t)
		{
			var props = GetColumns (t);
			foreach (var p in props) {
				if (IsPK (p))
					return p;
			}
			return null;
		}

		public static IEnumerable<PropertyInfo> GetColumns (Type t)
		{
			var raw = t.GetProperties (BindingFlags.Public | BindingFlags.Instance);
			return from p in raw
				where p.CanWrite
				select p;
		}
		
		public static void SetAutoIncPK(object obj, long id) {
			var pk = GetPK(obj.GetType());
			if (pk != null && IsAutoInc(pk)) {
				pk.SetValue(obj, Convert.ChangeType(id, pk.PropertyType), null);
			}
		}
	}

	public class SQLiteCommand
	{
		private IntPtr _db;
		private List<Binding> _bindings;

		public string CommandText { get; set; }

		internal SQLiteCommand (IntPtr db)
		{
			_db = db;
			_bindings = new List<Binding> ();
			CommandText = "";
		}

		public int ExecuteNonQuery ()
		{
			var stmt = Prepare ();
			
			var r = SQLite3.Step (stmt);
			if (r == SQLite3.Result.Error) {
				string msg = SQLite3.Errmsg (_db);
				SQLite3.Finalize (stmt);
				throw SQLiteException.New (r, msg);
			} else if (r == SQLite3.Result.Done) {
				int rowsAffected = SQLite3.Changes (_db);
				SQLite3.Finalize (stmt);
				return rowsAffected;
			} else {
				SQLite3.Finalize (stmt);
				throw SQLiteException.New (r, "Unknown error");
			}
		}

		public IEnumerable<T> ExecuteQuery<T> () where T : new()
		{
			var stmt = Prepare ();
			
			var props = Orm.GetColumns (typeof(T));
			var cols = new System.Reflection.PropertyInfo[SQLite3.ColumnCount (stmt)];
			for (int i = 0; i < cols.Length; i++) {
				var name = Marshal.PtrToStringAuto(SQLite3.ColumnName (stmt, i));
				cols[i] = MatchColProp (name, props);
			}
			
			while (SQLite3.Step (stmt) == SQLite3.Result.Row) {
				var obj = new T ();
				for (int i = 0; i < cols.Length; i++) {
					if (cols[i] == null)
						continue;
					var val = ReadCol (stmt, i, cols[i].PropertyType);
					cols[i].SetValue (obj, val, null);
				}
				yield return obj;
			}
			
			SQLite3.Finalize (stmt);
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
			var stmt = SQLite3.Prepare (_db, CommandText);
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


		static System.Reflection.PropertyInfo MatchColProp (string colName, IEnumerable<PropertyInfo> props)
		{
			foreach (var p in props) {
				if (p.Name == colName) {
					return p;
				}
			}
			return null;
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

		[DllImport("sqlite3", EntryPoint = "sqlite3_open")]
		public static extern Result Open (string filename, out IntPtr db);
		[DllImport("sqlite3", EntryPoint = "sqlite3_close")]
		public static extern Result Close (IntPtr db);

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
