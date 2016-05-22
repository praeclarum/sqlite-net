using System;
using SQLite.Net.Interop;
using System.Runtime.InteropServices;
using System.Text;
using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3Statement = System.IntPtr;

namespace SQLite.Net.Platform.WinRT
{
    public class SQLiteApiWinRT : ISQLiteApiExt
    {
        private readonly bool _useWinSqlite;

        /// <summary>
        /// Creates a SQLite API object for use from WinRT.
        /// </summary>
        /// <param name="tempFolderPath">Optional: Temporary folder path. Defaults to <see cref="Windows.Storage.ApplicationData.Current.TemporaryFolder.Path"/></param>
        /// <param name="useWinSqlite">Optional: Whether to use WinSQLite instead of SQLite. WinSQLite is built-in to Windows 10.0.10586.0 and above. Using it can reduce app size and potentially increase SQLite load time.</param>
        public SQLiteApiWinRT(string tempFolderPath = null, bool useWinSqlite = false)
        {
            _useWinSqlite = useWinSqlite;

            if (_useWinSqlite)
            {
                WinSQLite3.SetDirectory(/*temp directory type*/2, tempFolderPath ?? Windows.Storage.ApplicationData.Current.TemporaryFolder.Path);
            }
            else
            {
                SQLite3.SetDirectory(/*temp directory type*/2, tempFolderPath ?? Windows.Storage.ApplicationData.Current.TemporaryFolder.Path);
            }
        }

        public int BindBlob(IDbStatement stmt, int index, byte[] val, int n, IntPtr free)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return WinSQLite3.BindBlob(dbStatement.InternalStmt, index, val, n, free);
            }
            else
            {
                return SQLite3.BindBlob(dbStatement.InternalStmt, index, val, n, free);
            }
        }

        public int BindDouble(IDbStatement stmt, int index, double val)
        {
            var dbStatement = (DbStatement)stmt;


            if (_useWinSqlite)
            {
                return WinSQLite3.BindDouble(dbStatement.InternalStmt, index, val);
            }
            else
            {
                return SQLite3.BindDouble(dbStatement.InternalStmt, index, val);
            }
        }

        public int BindInt(IDbStatement stmt, int index, int val)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return WinSQLite3.BindInt(dbStatement.InternalStmt, index, val);
            }
            else
            {
                return SQLite3.BindInt(dbStatement.InternalStmt, index, val);
            }
        }

        public int BindInt64(IDbStatement stmt, int index, long val)
        {
            var dbStatement = (DbStatement)stmt;


            if (_useWinSqlite)
            {
                return WinSQLite3.BindInt64(dbStatement.InternalStmt, index, val);
            }
            else
            {
                return SQLite3.BindInt64(dbStatement.InternalStmt, index, val);
            }
        }

        public int BindNull(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;


            if (_useWinSqlite)
            {
                return WinSQLite3.BindNull(dbStatement.InternalStmt, index);
            }
            else
            {
                return SQLite3.BindNull(dbStatement.InternalStmt, index);
            }
        }

        public int BindParameterIndex(IDbStatement stmt, string name)
        {
            var dbStatement = (DbStatement)stmt;


            if (_useWinSqlite)
            {
                return WinSQLite3.BindParameterIndex(dbStatement.InternalStmt, name);
            }
            else
            {
                return SQLite3.BindParameterIndex(dbStatement.InternalStmt, name);
            }
        }

        public int BindText16(IDbStatement stmt, int index, string val, int n, IntPtr free)
        {
            var dbStatement = (DbStatement)stmt;


            if (_useWinSqlite)
            {
                return WinSQLite3.BindText(dbStatement.InternalStmt, index, val, n, free);
            }
            else
            {
                return SQLite3.BindText(dbStatement.InternalStmt, index, val, n, free);
            }
        }

        public Result BusyTimeout(IDbHandle db, int milliseconds)
        {
            var dbHandle = (DbHandle)db;

            if (_useWinSqlite)
            {
                return (Result)WinSQLite3.BusyTimeout(dbHandle.InternalDbHandle, milliseconds);
            }
            else
            {
                return (Result)SQLite3.BusyTimeout(dbHandle.InternalDbHandle, milliseconds);
            }
        }

        public int Changes(IDbHandle db)
        {
            var dbHandle = (DbHandle)db;

            if (_useWinSqlite)
            {
                return WinSQLite3.Changes(dbHandle.InternalDbHandle);
            }
            else
            {
                return SQLite3.Changes(dbHandle.InternalDbHandle);
            }
        }

        public Result Close(IDbHandle db)
        {
            var dbHandle = (DbHandle)db;

            if (_useWinSqlite)
            {
                return (Result)WinSQLite3.Close(dbHandle.InternalDbHandle);
            }
            else
            {
                return (Result)SQLite3.Close(dbHandle.InternalDbHandle);
            }
        }

        public Result Initialize()
        {
            throw new NotSupportedException();
        }
        public Result Shutdown()
        {
            throw new NotSupportedException();
        }

        public Result Config(ConfigOption option)
        {
            if (_useWinSqlite)
            {
                return (Result)WinSQLite3.Config(option);
            }
            else
            {
                return (Result)SQLite3.Config(option);
            }
        }


        public byte[] ColumnBlob(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;
            int length = ColumnBytes(stmt, index);
            byte[] result = new byte[length];
            if (length > 0)
            {

                if (_useWinSqlite)
                {
                    Marshal.Copy(WinSQLite3.ColumnBlob(dbStatement.InternalStmt, index), result, 0, length);
                }
                else
                {
                    Marshal.Copy(SQLite3.ColumnBlob(dbStatement.InternalStmt, index), result, 0, length);
                }
            }
            return result;
        }

        public byte[] ColumnByteArray(IDbStatement stmt, int index)
        {
            return ColumnBlob(stmt, index);
        }

        public int ColumnBytes(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return WinSQLite3.ColumnBytes(dbStatement.InternalStmt, index);
            }
            else
            {
                return SQLite3.ColumnBytes(dbStatement.InternalStmt, index);
            }
        }

        public int ColumnCount(IDbStatement stmt)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return WinSQLite3.ColumnCount(dbStatement.InternalStmt);
            }
            else
            {
                return SQLite3.ColumnCount(dbStatement.InternalStmt);
            }
        }

        public double ColumnDouble(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return WinSQLite3.ColumnDouble(dbStatement.InternalStmt, index);
            }
            else
            {
                return SQLite3.ColumnDouble(dbStatement.InternalStmt, index);
            }
        }

        public int ColumnInt(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return WinSQLite3.ColumnInt(dbStatement.InternalStmt, index);
            }
            else
            {
                return SQLite3.ColumnInt(dbStatement.InternalStmt, index);
            }
        }

        public long ColumnInt64(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return WinSQLite3.ColumnInt64(dbStatement.InternalStmt, index);
            }
            else
            {
                return SQLite3.ColumnInt64(dbStatement.InternalStmt, index);
            }
        }

        public string ColumnName16(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return WinSQLite3.ColumnName16(dbStatement.InternalStmt, index);
            }
            else
            {
                return SQLite3.ColumnName16(dbStatement.InternalStmt, index);
            }
        }

        public string ColumnText16(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return Marshal.PtrToStringUni(WinSQLite3.ColumnText16(dbStatement.InternalStmt, index));
            }
            else
            {
                return Marshal.PtrToStringUni(SQLite3.ColumnText16(dbStatement.InternalStmt, index));
            }
        }

        public ColType ColumnType(IDbStatement stmt, int index)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return (ColType)WinSQLite3.ColumnType(dbStatement.InternalStmt, index);
            }
            else
            {
                return (ColType)SQLite3.ColumnType(dbStatement.InternalStmt, index);
            }
        }

        public int LibVersionNumber()
        {
            if (_useWinSqlite)
            {
                return WinSQLite3.sqlite3_libversion_number();
            }
            else
            {
                return SQLite3.sqlite3_libversion_number();
            }
        }

        public string SourceID()
        {

            if (_useWinSqlite)
            {
                return Marshal.PtrToStringAnsi(WinSQLite3.sqlite3_sourceid());
            }
            else
            {
                return Marshal.PtrToStringAnsi(SQLite3.sqlite3_sourceid());
            }
        }

        public Result EnableLoadExtension(IDbHandle db, int onoff)
        {
            return (Result)1;
        }

        public string Errmsg16(IDbHandle db)
        {
            var dbHandle = (DbHandle)db;

            if (_useWinSqlite)
            {
                return WinSQLite3.GetErrmsg(dbHandle.InternalDbHandle);
            }
            else
            {
                return SQLite3.GetErrmsg(dbHandle.InternalDbHandle);
            }
        }

        public Result Finalize(IDbStatement stmt)
        {
            var dbStatement = (DbStatement)stmt;
            Sqlite3Statement internalStmt = dbStatement.InternalStmt;

            if (_useWinSqlite)
            {
                return (Result)WinSQLite3.Finalize(internalStmt);
            }
            else
            {
                return (Result)SQLite3.Finalize(internalStmt);
            }
        }

        public long LastInsertRowid(IDbHandle db)
        {
            var dbHandle = (DbHandle)db;

            if (_useWinSqlite)
            {
                return WinSQLite3.LastInsertRowid(dbHandle.InternalDbHandle);
            }
            else
            {
                return SQLite3.LastInsertRowid(dbHandle.InternalDbHandle);
            }
        }

        public Result Open(byte[] filename, out IDbHandle db, int flags, IntPtr zvfs)
        {
            Sqlite3DatabaseHandle internalDbHandle;
            Result ret;

            if (_useWinSqlite)
            {
                ret = (Result)WinSQLite3.Open(filename, out internalDbHandle, flags, zvfs);
            }
            else
            {
                ret = (Result)SQLite3.Open(filename, out internalDbHandle, flags, zvfs);
            }

            db = new DbHandle(internalDbHandle);
            return ret;
        }

        public ExtendedResult ExtendedErrCode(IDbHandle db)
        {
            var dbHandle = (DbHandle)db;

            if (_useWinSqlite)
            {
                return WinSQLite3.sqlite3_extended_errcode(dbHandle.InternalDbHandle);
            }
            else
            {
                return SQLite3.sqlite3_extended_errcode(dbHandle.InternalDbHandle);
            }
        }

        public IDbStatement Prepare2(IDbHandle db, string query)
        {
            var dbHandle = (DbHandle)db;
            var stmt = default(Sqlite3Statement);

            if (_useWinSqlite)
            {
                var r = WinSQLite3.Prepare2(dbHandle.InternalDbHandle, query, query.Length, out stmt, IntPtr.Zero);
                if (r != Result.OK)
                {
                    throw SQLiteException.New(r, WinSQLite3.GetErrmsg(dbHandle.InternalDbHandle));
                }
            }
            else
            {
                var r = SQLite3.Prepare2(dbHandle.InternalDbHandle, query, query.Length, out stmt, IntPtr.Zero);
                if (r != Result.OK)
                {
                    throw SQLiteException.New(r, SQLite3.GetErrmsg(dbHandle.InternalDbHandle));
                }
            }

            return new DbStatement(stmt);
        }

        public Result Reset(IDbStatement stmt)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return (Result)WinSQLite3.Reset(dbStatement.InternalStmt);
            }
            else
            {
                return (Result)SQLite3.Reset(dbStatement.InternalStmt);
            }
        }

        public Result Step(IDbStatement stmt)
        {
            var dbStatement = (DbStatement)stmt;

            if (_useWinSqlite)
            {
                return (Result)WinSQLite3.Step(dbStatement.InternalStmt);
            }
            else
            {
                return (Result)SQLite3.Step(dbStatement.InternalStmt);
            }
        }

        #region Backup

        public IDbBackupHandle BackupInit(IDbHandle destHandle, string destName, IDbHandle srcHandle, string srcName)
        {
            var internalDestDb = (DbHandle)destHandle;
            var internalSrcDb = (DbHandle)srcHandle;
            IntPtr p;

            if (_useWinSqlite)
            {
                p = WinSQLite3.sqlite3_backup_init(internalDestDb.InternalDbHandle,
                                                                  destName,
                                                                  internalSrcDb.InternalDbHandle,
                                                                  srcName);
            }
            else
            {
                p = SQLite3.sqlite3_backup_init(internalDestDb.InternalDbHandle,
                                                                  destName,
                                                                  internalSrcDb.InternalDbHandle,
                                                                  srcName);
            }

            if (p == IntPtr.Zero)
            {
                return null;
            }
            else
            {
                return new DbBackupHandle(p);
            }
        }

        public Result BackupStep(IDbBackupHandle handle, int pageCount)
        {
            var internalBackup = (DbBackupHandle)handle;

            if (_useWinSqlite)
            {
                return WinSQLite3.sqlite3_backup_step(internalBackup.DbBackupPtr, pageCount);
            }
            else
            {
                return SQLite3.sqlite3_backup_step(internalBackup.DbBackupPtr, pageCount);
            }
        }

        public Result BackupFinish(IDbBackupHandle handle)
        {
            var internalBackup = (DbBackupHandle)handle;

            if (_useWinSqlite)
            {
                return WinSQLite3.sqlite3_backup_finish(internalBackup.DbBackupPtr);
            }
            else
            {
                return SQLite3.sqlite3_backup_finish(internalBackup.DbBackupPtr);
            }
        }

        public int BackupRemaining(IDbBackupHandle handle)
        {
            var internalBackup = (DbBackupHandle)handle;

            if (_useWinSqlite)
            {
                return WinSQLite3.sqlite3_backup_remaining(internalBackup.DbBackupPtr);
            }
            else
            {
                return SQLite3.sqlite3_backup_remaining(internalBackup.DbBackupPtr);
            }
        }

        public int BackupPagecount(IDbBackupHandle handle)
        {
            var internalBackup = (DbBackupHandle)handle;

            if (_useWinSqlite)
            {
                return WinSQLite3.sqlite3_backup_pagecount(internalBackup.DbBackupPtr);
            }
            else
            {
                return SQLite3.sqlite3_backup_pagecount(internalBackup.DbBackupPtr);
            }
        }

        public int Sleep(int millis)
        {

            if (_useWinSqlite)
            {
                return WinSQLite3.sqlite3_sleep(millis);
            }
            else
            {
                return SQLite3.sqlite3_sleep(millis);
            }
        }

        private struct DbBackupHandle : IDbBackupHandle
        {
            public DbBackupHandle(IntPtr dbBackupPtr)
                : this()
            {
                DbBackupPtr = dbBackupPtr;
            }

            internal IntPtr DbBackupPtr { get; set; }

            public bool Equals(IDbBackupHandle other)
            {
                return other is DbBackupHandle && DbBackupPtr == ((DbBackupHandle)other).DbBackupPtr;
            }
        }

        #endregion

        private struct DbHandle : IDbHandle
        {
            public DbHandle(Sqlite3DatabaseHandle internalDbHandle)
                : this()
            {
                InternalDbHandle = internalDbHandle;
            }

            public Sqlite3DatabaseHandle InternalDbHandle { get; set; }

            public bool Equals(IDbHandle other)
            {
                return other is DbHandle && InternalDbHandle == ((DbHandle)other).InternalDbHandle;
            }
        }

        private struct DbStatement : IDbStatement
        {
            public DbStatement(Sqlite3Statement internalStmt)
                : this()
            {
                InternalStmt = internalStmt;
            }

            internal Sqlite3Statement InternalStmt { get; set; }

            public bool Equals(IDbStatement other)
            {
                return (other is DbStatement) && ((DbStatement)other).InternalStmt == InternalStmt;
            }
        }
    }

    public static class SQLite3
    {
        [DllImport("sqlite3", EntryPoint = "sqlite3_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Open([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Open([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db, int flags, IntPtr zvfs);

        [DllImport("sqlite3", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Open(byte[] filename, out IntPtr db, int flags, IntPtr zvfs);

        [DllImport("sqlite3", EntryPoint = "sqlite3_open16", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Open16([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Close(IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_config", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Config(ConfigOption option);

        [DllImport("sqlite3", EntryPoint = "sqlite3_win32_set_directory", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int SetDirectory(uint directoryType, string directoryPath);

        [DllImport("sqlite3", EntryPoint = "sqlite3_busy_timeout", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result BusyTimeout(IntPtr db, int milliseconds);

        [DllImport("sqlite3", EntryPoint = "sqlite3_changes", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Changes(IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_prepare16_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Prepare2(IntPtr db, [MarshalAs(UnmanagedType.LPWStr)] string sql, int numBytes, out IntPtr stmt, IntPtr pzTail);

        [DllImport("sqlite3", EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Step(IntPtr stmt);

        [DllImport("sqlite3", EntryPoint = "sqlite3_reset", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Reset(IntPtr stmt);

        [DllImport("sqlite3", EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Finalize(IntPtr stmt);

        [DllImport("sqlite3", EntryPoint = "sqlite3_last_insert_rowid", CallingConvention = CallingConvention.Cdecl)]
        public static extern long LastInsertRowid(IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_errmsg16", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Errmsg(IntPtr db);

        public static string GetErrmsg(IntPtr db)
        {
            return Marshal.PtrToStringUni(Errmsg(db));
        }

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_parameter_index", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindParameterIndex(IntPtr stmt, [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_null", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindNull(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindInt(IntPtr stmt, int index, int val);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_int64", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindInt64(IntPtr stmt, int index, long val);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_double", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindDouble(IntPtr stmt, int index, double val);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_text16", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int BindText(IntPtr stmt, int index, [MarshalAs(UnmanagedType.LPWStr)] string val, int n, IntPtr free);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_blob", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindBlob(IntPtr stmt, int index, byte[] val, int n, IntPtr free);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_count", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ColumnCount(IntPtr stmt);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_name", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ColumnName(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_name16", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ColumnName16Internal(IntPtr stmt, int index);
        public static string ColumnName16(IntPtr stmt, int index)
        {
            return Marshal.PtrToStringUni(ColumnName16Internal(stmt, index));
        }

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern ColType ColumnType(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ColumnInt(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_int64", CallingConvention = CallingConvention.Cdecl)]
        public static extern long ColumnInt64(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_double", CallingConvention = CallingConvention.Cdecl)]
        public static extern double ColumnDouble(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ColumnText(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_text16", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ColumnText16(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_blob", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ColumnBlob(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_bytes", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ColumnBytes(IntPtr stmt, int index);

        public static string ColumnString(IntPtr stmt, int index)
        {
            return Marshal.PtrToStringUni(SQLite3.ColumnText16(stmt, index));
        }

        public static byte[] ColumnByteArray(IntPtr stmt, int index)
        {
            int length = ColumnBytes(stmt, index);
            byte[] result = new byte[length];
            if (length > 0)
                Marshal.Copy(ColumnBlob(stmt, index), result, 0, length);
            return result;
        }

        [DllImport("sqlite3", EntryPoint = "sqlite3_extended_errcode", CallingConvention = CallingConvention.Cdecl)]
        public static extern ExtendedResult sqlite3_extended_errcode(IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_libversion_number", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_libversion_number();

        [DllImport("sqlite3", EntryPoint = "sqlite3_sourceid", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_sourceid();

        #region Backup

        [DllImport("sqlite3", EntryPoint = "sqlite3_backup_init", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_backup_init(IntPtr destDB,
                                                        [MarshalAs(UnmanagedType.LPStr)] string destName,
                                                        IntPtr srcDB,
                                                        [MarshalAs(UnmanagedType.LPStr)] string srcName);

        [DllImport("sqlite3", EntryPoint = "sqlite3_backup_step", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_backup_step(IntPtr backup, int pageCount);

        [DllImport("sqlite3", EntryPoint = "sqlite3_backup_finish", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_backup_finish(IntPtr backup);
        
        [DllImport("sqlite3", EntryPoint = "sqlite3_backup_remaining", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_backup_remaining(IntPtr backup);

        [DllImport("sqlite3", EntryPoint = "sqlite3_backup_pagecount", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_backup_pagecount(IntPtr backup);

        [DllImport("sqlite3", EntryPoint = "sqlite3_sleep", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_sleep(int millis);

        #endregion
    }

    /// <summary>
    /// WinSQLite is built-in to Windows 10.0.10586.0 and above. Using it can reduce app size and potentially increase SQLite load time.
    /// For more information see: <see cref="https://msdn.microsoft.com/en-us/windows/uwp/data-access/sqlite-databases#using-the-sdk-sqlite/">
    /// </summary>
    public static class WinSQLite3
    {
        [DllImport("winsqlite3", EntryPoint = "sqlite3_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Open([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Open([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db, int flags, IntPtr zvfs);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Open(byte[] filename, out IntPtr db, int flags, IntPtr zvfs);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_open16", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Open16([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr db);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Close(IntPtr db);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_config", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Config(ConfigOption option);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_win32_set_directory", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int SetDirectory(uint directoryType, string directoryPath);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_busy_timeout", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result BusyTimeout(IntPtr db, int milliseconds);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_changes", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Changes(IntPtr db);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_prepare16_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Prepare2(IntPtr db, [MarshalAs(UnmanagedType.LPStr)] string sql, int numBytes, out IntPtr stmt, IntPtr pzTail);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Step(IntPtr stmt);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_reset", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Reset(IntPtr stmt);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result Finalize(IntPtr stmt);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_last_insert_rowid", CallingConvention = CallingConvention.Cdecl)]
        public static extern long LastInsertRowid(IntPtr db);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_errmsg16", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Errmsg(IntPtr db);

        public static string GetErrmsg(IntPtr db)
        {
            return Marshal.PtrToStringUni(Errmsg(db));
        }

        [DllImport("winsqlite3", EntryPoint = "sqlite3_bind_parameter_index", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindParameterIndex(IntPtr stmt, [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_bind_null", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindNull(IntPtr stmt, int index);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindInt(IntPtr stmt, int index, int val);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_bind_int64", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindInt64(IntPtr stmt, int index, long val);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_bind_double", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindDouble(IntPtr stmt, int index, double val);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_bind_text16", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int BindText(IntPtr stmt, int index, [MarshalAs(UnmanagedType.LPWStr)] string val, int n, IntPtr free);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_bind_blob", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindBlob(IntPtr stmt, int index, byte[] val, int n, IntPtr free);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_column_count", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ColumnCount(IntPtr stmt);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_column_name", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ColumnName(IntPtr stmt, int index);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_column_name16", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ColumnName16Internal(IntPtr stmt, int index);
        public static string ColumnName16(IntPtr stmt, int index)
        {
            return Marshal.PtrToStringUni(ColumnName16Internal(stmt, index));
        }

        [DllImport("winsqlite3", EntryPoint = "sqlite3_column_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern ColType ColumnType(IntPtr stmt, int index);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ColumnInt(IntPtr stmt, int index);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_column_int64", CallingConvention = CallingConvention.Cdecl)]
        public static extern long ColumnInt64(IntPtr stmt, int index);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_column_double", CallingConvention = CallingConvention.Cdecl)]
        public static extern double ColumnDouble(IntPtr stmt, int index);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ColumnText(IntPtr stmt, int index);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_column_text16", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ColumnText16(IntPtr stmt, int index);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_column_blob", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ColumnBlob(IntPtr stmt, int index);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_column_bytes", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ColumnBytes(IntPtr stmt, int index);

        public static string ColumnString(IntPtr stmt, int index)
        {
            return Marshal.PtrToStringUni(WinSQLite3.ColumnText16(stmt, index));
        }

        public static byte[] ColumnByteArray(IntPtr stmt, int index)
        {
            int length = ColumnBytes(stmt, index);
            byte[] result = new byte[length];
            if (length > 0)
                Marshal.Copy(ColumnBlob(stmt, index), result, 0, length);
            return result;
        }

        [DllImport("winsqlite3", EntryPoint = "sqlite3_extended_errcode", CallingConvention = CallingConvention.Cdecl)]
        public static extern ExtendedResult sqlite3_extended_errcode(IntPtr db);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_libversion_number", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_libversion_number();

        [DllImport("winsqlite3", EntryPoint = "sqlite3_sourceid", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_sourceid();

        #region Backup

        [DllImport("winsqlite3", EntryPoint = "sqlite3_backup_init", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_backup_init(IntPtr destDB,
                                                        [MarshalAs(UnmanagedType.LPStr)] string destName,
                                                        IntPtr srcDB,
                                                        [MarshalAs(UnmanagedType.LPStr)] string srcName);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_backup_step", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_backup_step(IntPtr backup, int pageCount);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_backup_finish", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_backup_finish(IntPtr backup);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_backup_remaining", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_backup_remaining(IntPtr backup);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_backup_pagecount", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_backup_pagecount(IntPtr backup);

        [DllImport("winsqlite3", EntryPoint = "sqlite3_sleep", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_sleep(int millis);

        #endregion
    }
}
