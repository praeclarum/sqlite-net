using System;
using Community.CsharpSqlite;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.WindowsPhone8.CSharpSqlite
{
    public class SQLiteApiWP8 : ISQLiteApi
    {
        public Result Open(string filename, out IDbHandle db)
        {
            Sqlite3.sqlite3 internalDbHandle = null;
            var ret = (Result) Sqlite3.sqlite3_open(filename, ref internalDbHandle);
            db = new DbHandle(internalDbHandle);
            return ret;
        }

        public Result Open(byte[] filename, out IDbHandle db, int flags, IntPtr zVfs)
        {
            var dbFileName = System.Text.Encoding.UTF8.GetString(filename, 0, filename.Length);
            Sqlite3.sqlite3 internalDbHandle = null;
            var ret = (Result)Sqlite3.sqlite3_open_v2(dbFileName, ref internalDbHandle, flags, null);
            db = new DbHandle(internalDbHandle);
            return ret;
        }

        public Result EnableLoadExtension(IDbHandle db, int onoff)
        {
            var dbHandle = (DbHandle) db;
            return (Result) Sqlite3.sqlite3_enable_load_extension(dbHandle.InternalDbHandle, onoff);
        }

        public Result Close(IDbHandle db)
        {
            var dbHandle = (DbHandle) db;
            return (Result) Sqlite3.sqlite3_close(dbHandle.InternalDbHandle);
        }

        public Result BusyTimeout(IDbHandle db, int milliseconds)
        {
            var dbHandle = (DbHandle) db;
            return (Result) Sqlite3.sqlite3_busy_timeout(dbHandle.InternalDbHandle, milliseconds);
        }

        public int Changes(IDbHandle db)
        {
            var dbHandle = (DbHandle) db;
            return Sqlite3.sqlite3_changes(dbHandle.InternalDbHandle);
        }


        public IDbStatement Prepare2(IDbHandle db, string query)
        {
            var dbHandle = (DbHandle) db;
            var stmt = new Sqlite3.Vdbe();

            int r = Sqlite3.sqlite3_prepare_v2(dbHandle.InternalDbHandle, query, -1, ref stmt, 0);

            if (r != 0)
            {
                throw SQLiteException.New((Result) r, GetErrmsg(db));
            }
            return new DbStatement(stmt);
        }

        public Result Step(IDbStatement stmt)
        {
            var dbStatement = (DbStatement) stmt;
            return (Result) Sqlite3.sqlite3_step(dbStatement.InternalStmt);
        }

        public Result Reset(IDbStatement stmt)
        {
            var dbStatement = (DbStatement)stmt;
            return (Result)Sqlite3.sqlite3_reset(dbStatement.InternalStmt);
        }

        public Result Finalize(IDbStatement stmt)
        {
            var dbStatement = (DbStatement)stmt;
            var internalStmt = dbStatement.InternalStmt;
            return (Result)Sqlite3.sqlite3_finalize(ref internalStmt);
        }

        public long LastInsertRowid(IDbHandle db)
        {
            var dbHandle = (DbHandle)db;
            return Sqlite3.sqlite3_last_insert_rowid(dbHandle.InternalDbHandle);
        }

        public string Errmsg16(IDbHandle db)
        {
            var dbHandle = (DbHandle)db;
            return Sqlite3.sqlite3_errmsg(dbHandle.InternalDbHandle);
        }

        public string GetErrmsg(IDbHandle db)
        {
            var dbHandle = (DbHandle)db;
            return Sqlite3.sqlite3_errmsg(dbHandle.InternalDbHandle);
        }

        public int BindParameterIndex(IDbStatement stmt, string name)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_bind_parameter_index(dbStatement.InternalStmt, name);
        }

        public int BindNull(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_bind_null(dbStatement.InternalStmt, index);
        }

        public int BindInt(IDbStatement stmt, int index, int val)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_bind_int(dbStatement.InternalStmt, index, val);
        }

        public int BindInt64(IDbStatement stmt, int index, long val)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_bind_int64(dbStatement.InternalStmt, index, val);
        }

        public int BindDouble(IDbStatement stmt, int index, double val)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_bind_double(dbStatement.InternalStmt, index, val);
        }

        public int BindText16(IDbStatement stmt, int index, string val, int n, IntPtr free)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_bind_text(dbStatement.InternalStmt, index, val, n, null);
        }

        public int BindBlob(IDbStatement stmt, int index, byte[] val, int n, IntPtr free)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_bind_blob(dbStatement.InternalStmt, index, val, n, null);
        }

        public int ColumnCount(IDbStatement stmt)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_column_count(dbStatement.InternalStmt);
        }

        public string ColumnName16(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_column_name(dbStatement.InternalStmt, index);
        }

        public ColType ColumnType(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return (ColType)Sqlite3.sqlite3_column_type(dbStatement.InternalStmt, index);
        }

        public int ColumnInt(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_column_int(dbStatement.InternalStmt, index);
        }

        public long ColumnInt64(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_column_int64(dbStatement.InternalStmt, index);
        }

        public double ColumnDouble(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_column_double(dbStatement.InternalStmt, index);
        }

        public int ColumnBytes(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_column_bytes(dbStatement.InternalStmt, index);
        }

        public string ColumnString(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_column_text(dbStatement.InternalStmt, index);
        }

        public byte[] ColumnByteArray(IDbStatement stmt, int index)
        {
            return ColumnBlob(stmt, index);
        }

        public string ColumnName(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_column_name(dbStatement.InternalStmt, index);
        }

        public string ColumnText(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_column_text(dbStatement.InternalStmt, index);
        }

        public string ColumnText16(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_column_text(dbStatement.InternalStmt, index);
        }

        public byte[] ColumnBlob(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            return Sqlite3.sqlite3_column_blob(dbStatement.InternalStmt, index);
        }

        private struct DbHandle : IDbHandle
        {
            public DbHandle(Sqlite3.sqlite3 internalDbHandle) : this()
            {
                InternalDbHandle = internalDbHandle;
            }

            public Sqlite3.sqlite3 InternalDbHandle { get; set; }
            public bool Equals(IDbHandle other)
            {
                return other is DbHandle && InternalDbHandle == ((DbHandle)other).InternalDbHandle;
            }
        }

        private struct DbStatement : IDbStatement
        {
            public DbStatement(Sqlite3.Vdbe internalStmt)
                : this()
            {
                InternalStmt = internalStmt;
            }

            internal Sqlite3.Vdbe InternalStmt { get; set; }

            public bool Equals(IDbStatement other)
            {
                return (other is DbStatement) && ((DbStatement)other).InternalStmt == InternalStmt;
            }
        }
    }

}