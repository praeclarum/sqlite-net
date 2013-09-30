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

#if WINDOWS_PHONE && !USE_WP8_NATIVE_SQLITE
#define USE_CSHARP_SQLITE
#endif

using SQLite.Net.Interop;
#if USE_CSHARP_SQLITE
using Sqlite3 = Community.CsharpSqlite.Sqlite3;
#elif USE_WP8_NATIVE_SQLITE
using Sqlite3 = Sqlite.Sqlite3;
#else
using SQLite.Net.Interop;
#endif

using System;
using System.Runtime.InteropServices;

namespace SQLite.Net.Interop
{
    public static class SQLite3
    {
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
            Row = 100,
            Done = 101
        }

        public enum ConfigOption : int
        {
            SingleThread = 1,
            MultiThread = 2,
            Serialized = 3
        }

#if !USE_CSHARP_SQLITE && !USE_WP8_NATIVE_SQLITE
        internal struct Sqlite3DatabaseInternal : IDbHandle
        {
            public IntPtr Handle { get; set; }

            public Sqlite3DatabaseInternal(IntPtr handle) : this()
            {
                Handle = handle;
            }
        }

        internal struct Sqlite3StatementInternal : IDbStatement
        {
            public IntPtr Handle { get; set; }

            public Sqlite3StatementInternal(IntPtr handle)
                : this()
            {
                Handle = handle;
            }
        }
#elif USE_WP8_NATIVE_SQLITE        
        internal struct Sqlite3DatabaseInternal : IDbHandle
        {
            public Sqlite.Database Handle { get; set; }

            public Sqlite3DatabaseInternal(Sqlite.Database handle)
                : this()
            {
                Handle = handle;
            }
        }

        internal struct Sqlite3StatementInternal : IDbStatement
        {
            public Sqlite.Statement Handle { get; set; }

            public Sqlite3StatementInternal(Sqlite.Statement handle)
                : this()
            {
                Handle = handle;
            }
        }
#elif USE_CSHARP_SQLITE
        internal struct Sqlite3DatabaseInternal : IDbHandle
        {
            public Sqlite3.sqlite3 Handle { get; set; }

            public Sqlite3DatabaseInternal(Sqlite3.sqlite3 handle)
                : this()
            {
                Handle = handle;
            }
        }

        internal struct Sqlite3StatementInternal : IDbStatement
        {
            public Sqlite3.Vdbe Handle { get; set; }

            public Sqlite3StatementInternal(Sqlite3.Vdbe handle)
                : this()
            {
                Handle = handle;
            }
        }
#endif

#if !USE_CSHARP_SQLITE && !USE_WP8_NATIVE_SQLITE
        public static Result Open(string filename, out IDbHandle db)
        {
            IntPtr dbPtr;
            var r = Sqlite3Native.Open(filename, out dbPtr);
            db = new Sqlite3DatabaseInternal(dbPtr);
            return r;
        }

        public static Result Open(string filename, out IDbHandle db, int flags, IntPtr zvfs)
        {
            IntPtr dbPtr;
            var r = Sqlite3Native.Open(filename, out dbPtr, flags, zvfs);
            db = new Sqlite3DatabaseInternal(dbPtr);
            return r;
        }

        public static Result Open(byte[] filename, out IDbHandle db, int flags, IntPtr zvfs)
        {
            IntPtr dbPtr;
            var r = Sqlite3Native.Open(filename, out dbPtr, flags, zvfs);
            db = new Sqlite3DatabaseInternal(dbPtr);
            return r;
        }

        public static Result Open16(string filename, out IDbHandle db)
        {
            IntPtr dbPtr;
            var r = Sqlite3Native.Open(filename, out dbPtr);
            db = new Sqlite3DatabaseInternal(dbPtr);
            return r;
        }

        public static Result EnableLoadExtension(IDbHandle db, int onoff)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
            return Sqlite3Native.EnableLoadExtension(internalDbHandle.Handle, onoff);
        }

        public static Result Close(IDbHandle db)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
            return Sqlite3Native.Close(internalDbHandle.Handle);
        }

        public static Result Config(ConfigOption option)
        {
            return Sqlite3Native.Config(option);
        }

        public static int SetDirectory(uint directoryType, string directoryPath)
        {
            return Sqlite3Native.SetDirectory(directoryType, directoryPath);
        }

        public static Result BusyTimeout(IDbHandle db, int milliseconds)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
            return Sqlite3Native.BusyTimeout(internalDbHandle.Handle, milliseconds);
        }

        public static int Changes(IDbHandle db)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
            return Sqlite3Native.Changes(internalDbHandle.Handle);
        }

        public static Result Prepare2(IDbHandle db, string sql, int numBytes, out IDbStatement stmt, IntPtr pzTail)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal) db;
            IntPtr stmtPtr;
            var r = Sqlite3Native.Prepare2(internalDbHandle.Handle, sql, numBytes, out stmtPtr, pzTail);
            stmt = new Sqlite3StatementInternal(stmtPtr);
            return r;
        }

        public static IDbStatement Prepare2(IDbHandle db, string query)
        {
            IDbStatement stmt;
            var r = Prepare2 (db, query, query.Length, out stmt, IntPtr.Zero);
            if (r != Result.OK) {
                throw SQLiteException.New (r, GetErrmsg (db));
            }
            return stmt;
        }

        public static Result Step(IDbStatement stmt)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.Step(internalStmt.Handle);
        }

        public static Result Reset(IDbStatement stmt)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.Reset(internalStmt.Handle);
        }

        public static Result Finalize(IDbStatement stmt)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.Finalize(internalStmt.Handle);
        }

        public static long LastInsertRowid(IDbHandle db)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
            return Sqlite3Native.LastInsertRowid(internalDbHandle.Handle);
        }

        public static IntPtr Errmsg(IDbHandle db)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
            return Sqlite3Native.Errmsg(internalDbHandle.Handle);
        }

        public static string GetErrmsg(IDbHandle db)
        {
            return Marshal.PtrToStringUni (Errmsg (db));
        }

        public static int BindParameterIndex(IDbStatement stmt, string name)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.BindParameterIndex(internalStmt.Handle, name);
        }

        public static int BindNull(IDbStatement stmt, int index)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.BindNull(internalStmt.Handle,index);
        }

        public static int BindInt(IDbStatement stmt, int index, int val)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.BindInt(internalStmt.Handle, index, val);
        }

        public static int BindInt64(IDbStatement stmt, int index, long val)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.BindInt64(internalStmt.Handle, index, val);
        }

        public static int BindDouble(IDbStatement stmt, int index, double val)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.BindDouble(internalStmt.Handle, index, val);
        }

        public static int BindText(IDbStatement stmt, int index, string val, int n, IntPtr free)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.BindText(internalStmt.Handle, index, val, n, free);
        }

        public static int BindBlob(IDbStatement stmt, int index, byte[] val, int n, IntPtr free)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.BindBlob(internalStmt.Handle, index, val, n, free);
        }

        public static int ColumnCount(IDbStatement stmt)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.ColumnCount(internalStmt.Handle);
        }

        public static IntPtr ColumnName(IDbStatement stmt, int index)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.ColumnName(internalStmt.Handle, index);
        }

        static IntPtr ColumnName16Internal(IDbStatement stmt, int index)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.ColumnName16Internal(internalStmt.Handle, index);
        }

        public static string ColumnName16(IDbStatement stmt, int index)
        {
            return Marshal.PtrToStringUni(ColumnName16Internal(stmt, index));
        }

        public static ColType ColumnType(IDbStatement stmt, int index)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.ColumnType(internalStmt.Handle, index);
        }

        public static int ColumnInt(IDbStatement stmt, int index)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.ColumnInt(internalStmt.Handle, index);
        }

        public static long ColumnInt64(IDbStatement stmt, int index)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.ColumnInt64(internalStmt.Handle, index);
        }

        public static double ColumnDouble(IDbStatement stmt, int index)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.ColumnDouble(internalStmt.Handle, index);
        }

        public static IntPtr ColumnText(IDbStatement stmt, int index)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.ColumnText(internalStmt.Handle, index);
        }

        public static IntPtr ColumnText16(IDbStatement stmt, int index)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.ColumnText16(internalStmt.Handle, index);
        }

        public static IntPtr ColumnBlob(IDbStatement stmt, int index)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.ColumnBlob(internalStmt.Handle, index);
        }

        public static int ColumnBytes(IDbStatement stmt, int index)
        {
            var internalStmt = (Sqlite3StatementInternal)stmt;
            return Sqlite3Native.ColumnBytes(internalStmt.Handle, index);
        }

        public static string ColumnString(IDbStatement stmt, int index)
        {
            return Marshal.PtrToStringUni (SQLite3.ColumnText16 (stmt, index));
        }

        public static byte[] ColumnByteArray(IDbStatement stmt, int index)
        {
            int length = ColumnBytes (stmt, index);
            var result = new byte[length];
            if (length > 0)
                Marshal.Copy (ColumnBlob (stmt, index), result, 0, length);
            return result;
        }
#else

        public static Result Open(string filename, out IDbHandle db)
        {
#if USE_WP8_NATIVE_SQLITE
            Sqlite.Database internalDbHandle;
			var r = (Result)Sqlite3.sqlite3_open(filename, out internalDbHandle);
#else
            Sqlite3.sqlite3 internalDbHandle;
            var r = (Result)Sqlite3.sqlite3_open(filename, out internalDbHandle);
#endif
            db = new Sqlite3DatabaseInternal(internalDbHandle);
            return r;
        }

        public static Result Open(string filename, out IDbHandle db, int flags, IntPtr zVfs)
        {
#if USE_WP8_NATIVE_SQLITE
            Sqlite.Database internalDbHandle;
			var r = (Result)Sqlite3.sqlite3_open_v2(filename, out internalDbHandle, flags, "");
#else
            Sqlite3.sqlite3 internalDbHandle;
            var r = (Result)Sqlite3.sqlite3_open_v2(filename, out internalDbHandle, flags, null);
#endif
            db = new Sqlite3DatabaseInternal(internalDbHandle);
            return r;
		}

		public static Result Close(IDbHandle db)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
            return (Result)Sqlite3.sqlite3_close(internalDbHandle.Handle);
		}

		public static Result BusyTimeout(IDbHandle db, int milliseconds)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
            return (Result)Sqlite3.sqlite3_busy_timeout(internalDbHandle.Handle, milliseconds);
		}

		public static int Changes(IDbHandle db)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
			return Sqlite3.sqlite3_changes(internalDbHandle.Handle);
		}

		public static IDbStatement Prepare2(IDbHandle db, string query)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
#if USE_WP8_NATIVE_SQLITE
			Sqlite.Statement stmt;
			var r = Sqlite3.sqlite3_prepare_v2(db, query, out stmt);
#else
            var stmt = new Sqlite3.Vdbe();
            var r = Sqlite3.sqlite3_prepare_v2(internalDbHandle.Handle, query, -1, ref stmt, 0);
#endif
			if (r != 0)
			{
				throw SQLiteException.New((Result)r, GetErrmsg(db));
			}
		    return new Sqlite3StatementInternal(stmt);
		}

		public static Result Step(IDbStatement stmt)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
            return (Result)Sqlite3.sqlite3_step(internalStmtHandle.Handle);
		}

		public static Result Reset(IDbStatement stmt)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return (Result)Sqlite3.sqlite3_reset(internalStmtHandle.Handle);
		}

		public static Result Finalize(IDbStatement stmt)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return (Result)Sqlite3.sqlite3_finalize(internalStmtHandle.Handle);
		}

		public static long LastInsertRowid(IDbHandle db)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
			return Sqlite3.sqlite3_last_insert_rowid(internalDbHandle.Handle);
		}

		public static string GetErrmsg(IDbHandle db)
        {
            var internalDbHandle = (Sqlite3DatabaseInternal)db;
			return Sqlite3.sqlite3_errmsg(internalDbHandle.Handle);
		}

		public static int BindParameterIndex(IDbStatement stmt, string name)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_bind_parameter_index(internalStmtHandle.Handle, name);
		}

		public static int BindNull(IDbStatement stmt, int index)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_bind_null(internalStmtHandle.Handle, index);
		}

		public static int BindInt(IDbStatement stmt, int index, int val)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_bind_int(internalStmtHandle.Handle, index, val);
		}

		public static int BindInt64(IDbStatement stmt, int index, long val)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_bind_int64(internalStmtHandle.Handle, index, val);
		}

		public static int BindDouble(IDbStatement stmt, int index, double val)
		{
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_bind_double(internalStmtHandle.Handle, index, val);
		}

		public static int BindText(IDbStatement stmt, int index, string val, int n, IntPtr free)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
#if USE_WP8_NATIVE_SQLITE
			return Sqlite3.sqlite3_bind_text(internalStmtHandle.Handle, index, val, n);
#else
			return Sqlite3.sqlite3_bind_text(internalStmtHandle.Handle, index, val, n, null);
#endif
		}

		public static int BindBlob(IDbStatement stmt, int index, byte[] val, int n, IntPtr free)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
#if USE_WP8_NATIVE_SQLITE
			return Sqlite3.sqlite3_bind_blob(internalStmtHandle.Handle, index, val, n);
#else
			return Sqlite3.sqlite3_bind_blob(internalStmtHandle.Handle, index, val, n, null);
#endif
		}

		public static int ColumnCount(IDbStatement stmt)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_column_count(internalStmtHandle.Handle);
		}

		public static string ColumnName(IDbStatement stmt, int index)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_column_name(internalStmtHandle.Handle, index);
		}

		public static string ColumnName16(IDbStatement stmt, int index)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_column_name(internalStmtHandle.Handle, index);
		}

		public static ColType ColumnType(IDbStatement stmt, int index)
        {
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return (ColType)Sqlite3.sqlite3_column_type(internalStmtHandle.Handle, index);
		}

		public static int ColumnInt(IDbStatement stmt, int index)
		{
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_column_int(internalStmtHandle.Handle, index);
		}

		public static long ColumnInt64(IDbStatement stmt, int index)
		{
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_column_int64(internalStmtHandle.Handle, index);
		}

		public static double ColumnDouble(IDbStatement stmt, int index)
		{
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_column_double(internalStmtHandle.Handle, index);
		}

		public static string ColumnText(IDbStatement stmt, int index)
		{
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_column_text(internalStmtHandle.Handle, index);
		}

		public static string ColumnText16(IDbStatement stmt, int index)
		{
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_column_text(internalStmtHandle.Handle, index);
		}

		public static byte[] ColumnBlob(IDbStatement stmt, int index)
		{
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_column_blob(internalStmtHandle.Handle, index);
		}

		public static int ColumnBytes(IDbStatement stmt, int index)
		{
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_column_bytes(internalStmtHandle.Handle, index);
		}

		public static string ColumnString(IDbStatement stmt, int index)
		{
            var internalStmtHandle = (Sqlite3StatementInternal)stmt;
			return Sqlite3.sqlite3_column_text(internalStmtHandle.Handle, index);
		}

		public static byte[] ColumnByteArray(IDbStatement stmt, int index)
		{
			return ColumnBlob(stmt, index);
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
}