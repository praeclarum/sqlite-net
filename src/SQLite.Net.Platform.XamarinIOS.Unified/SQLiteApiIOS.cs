using System;
using System.Runtime.InteropServices;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.XamarinIOS
{
    public class SQLiteApiIOS : ISQLiteApiExt
    {
        public Result Open(byte[] filename, out IDbHandle db, int flags, IntPtr zvfs)
        {
            IntPtr dbPtr;
            Result r = SQLiteApiIOSInternal.sqlite3_open_v2(filename, out dbPtr, flags, zvfs);
            db = new DbHandle(dbPtr);
            return r;
        }

        public ExtendedResult ExtendedErrCode(IDbHandle db)
        {
            var internalDbHandle = (DbHandle) db;
            return SQLiteApiIOSInternal.sqlite3_extended_errcode(internalDbHandle.DbPtr);
        }

        public int LibVersionNumber()
        {
            return SQLiteApiIOSInternal.sqlite3_libversion_number();
        }
        
        public string SourceID()
        {
            return Marshal.PtrToStringAnsi(SQLiteApiIOSInternal.sqlite3_sourceid());
        }                     

        public Result EnableLoadExtension(IDbHandle db, int onoff)
        {
            var internalDbHandle = (DbHandle) db;
            return SQLiteApiIOSInternal.sqlite3_enable_load_extension(internalDbHandle.DbPtr, onoff);
        }

        public Result Close(IDbHandle db)
        {
            var internalDbHandle = (DbHandle) db;
            return SQLiteApiIOSInternal.sqlite3_close(internalDbHandle.DbPtr);
        }

        public Result Initialize()
        {
            return SQLiteApiIOSInternal.sqlite3_initialize();
        }
        public Result Shutdown()
        {
            return SQLiteApiIOSInternal.sqlite3_shutdown();
        }

        public Result Config(ConfigOption option)
        {
            return SQLiteApiIOSInternal.sqlite3_config(option);
        }

        public Result BusyTimeout(IDbHandle db, int milliseconds)
        {
            var internalDbHandle = (DbHandle) db;
            return SQLiteApiIOSInternal.sqlite3_busy_timeout(internalDbHandle.DbPtr, milliseconds);
        }

        public int Changes(IDbHandle db)
        {
            var internalDbHandle = (DbHandle) db;
            return SQLiteApiIOSInternal.sqlite3_changes(internalDbHandle.DbPtr);
        }

        public IDbStatement Prepare2(IDbHandle db, string query)
        {
            var internalDbHandle = (DbHandle) db;
            IntPtr stmt;
            Result r = SQLiteApiIOSInternal.sqlite3_prepare16_v2(internalDbHandle.DbPtr, query, -1, out stmt, IntPtr.Zero);
            if (r != Result.OK)
            {
                throw SQLiteException.New(r, Errmsg16(internalDbHandle));
            }
            return new DbStatement(stmt);
        }

        public Result Step(IDbStatement stmt)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_step(internalStmt.StmtPtr);
        }

        public Result Reset(IDbStatement stmt)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_reset(internalStmt.StmtPtr);
        }

        public Result Finalize(IDbStatement stmt)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_finalize(internalStmt.StmtPtr);
        }

        public long LastInsertRowid(IDbHandle db)
        {
            var internalDbHandle = (DbHandle) db;
            return SQLiteApiIOSInternal.sqlite3_last_insert_rowid(internalDbHandle.DbPtr);
        }

        public string Errmsg16(IDbHandle db)
        {
            var internalDbHandle = (DbHandle) db;
            return Marshal.PtrToStringUni(SQLiteApiIOSInternal.sqlite3_errmsg16(internalDbHandle.DbPtr));
        }

        public int BindParameterIndex(IDbStatement stmt, string name)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_bind_parameter_index(internalStmt.StmtPtr, name);
        }

        public int BindNull(IDbStatement stmt, int index)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_bind_null(internalStmt.StmtPtr, index);
        }

        public int BindInt(IDbStatement stmt, int index, int val)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_bind_int(internalStmt.StmtPtr, index, val);
        }

        public int BindInt64(IDbStatement stmt, int index, long val)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_bind_int64(internalStmt.StmtPtr, index, val);
        }

        public int BindDouble(IDbStatement stmt, int index, double val)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_bind_double(internalStmt.StmtPtr, index, val);
        }

        public int BindText16(IDbStatement stmt, int index, string val, int n, IntPtr free)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_bind_text16(internalStmt.StmtPtr, index, val, n, free);
        }

        public int BindBlob(IDbStatement stmt, int index, byte[] val, int n, IntPtr free)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_bind_blob(internalStmt.StmtPtr, index, val, n, free);
        }

        public int ColumnCount(IDbStatement stmt)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_column_count(internalStmt.StmtPtr);
        }

        public string ColumnName16(IDbStatement stmt, int index)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.ColumnName16(internalStmt.StmtPtr, index);
        }

        public ColType ColumnType(IDbStatement stmt, int index)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_column_type(internalStmt.StmtPtr, index);
        }

        public int ColumnInt(IDbStatement stmt, int index)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_column_int(internalStmt.StmtPtr, index);
        }

        public long ColumnInt64(IDbStatement stmt, int index)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_column_int64(internalStmt.StmtPtr, index);
        }

        public double ColumnDouble(IDbStatement stmt, int index)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_column_double(internalStmt.StmtPtr, index);
        }

        public string ColumnText16(IDbStatement stmt, int index)
        {
            var internalStmt = (DbStatement) stmt;
            return Marshal.PtrToStringUni(SQLiteApiIOSInternal.sqlite3_column_text16(internalStmt.StmtPtr, index));
        }

        public byte[] ColumnBlob(IDbStatement stmt, int index)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.ColumnBlob(internalStmt.StmtPtr, index);
        }

        public int ColumnBytes(IDbStatement stmt, int index)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.sqlite3_column_bytes(internalStmt.StmtPtr, index);
        }

        public byte[] ColumnByteArray(IDbStatement stmt, int index)
        {
            var internalStmt = (DbStatement) stmt;
            return SQLiteApiIOSInternal.ColumnByteArray(internalStmt.StmtPtr, index);
        }

        #region Backup
        
        public IDbBackupHandle BackupInit(IDbHandle destHandle, string destName, IDbHandle srcHandle, string srcName) {
        	var internalDestDb = (DbHandle)destHandle;
        	var internalSrcDb = (DbHandle)srcHandle;
        
        	IntPtr p = SQLiteApiIOSInternal.sqlite3_backup_init(internalDestDb.DbPtr, 
        	                                                    destName, 
        	                                                    internalSrcDb.DbPtr, 
        	                                                    srcName);
        
        	if(p == IntPtr.Zero) {
        		return null;
        	} else {
        		return new DbBackupHandle(p);
        	}
        }
        
        public Result BackupStep(IDbBackupHandle handle, int pageCount) {
        	var internalBackup = (DbBackupHandle)handle;
        	return SQLiteApiIOSInternal.sqlite3_backup_step(internalBackup.DbBackupPtr, pageCount);
        }
        
        public Result BackupFinish(IDbBackupHandle handle) {
        	var internalBackup = (DbBackupHandle)handle;
        	return SQLiteApiIOSInternal.sqlite3_backup_finish(internalBackup.DbBackupPtr);
        }
        
        public int BackupRemaining(IDbBackupHandle handle) {
        	var internalBackup = (DbBackupHandle)handle;
        	return SQLiteApiIOSInternal.sqlite3_backup_remaining(internalBackup.DbBackupPtr);
        }
        
        public int BackupPagecount(IDbBackupHandle handle) {
        	var internalBackup = (DbBackupHandle)handle;
        	return SQLiteApiIOSInternal.sqlite3_backup_pagecount(internalBackup.DbBackupPtr);
        }
        
        public int Sleep(int millis) {
        	return SQLiteApiIOSInternal.sqlite3_sleep(millis);
        }
        
        private struct DbBackupHandle : IDbBackupHandle {
        	public DbBackupHandle(IntPtr dbBackupPtr) : this() {
        		DbBackupPtr = dbBackupPtr;
        	}
        
        	internal IntPtr DbBackupPtr { get; set; }
        
        	public bool Equals(IDbBackupHandle other) {
        		return other is DbBackupHandle && DbBackupPtr == ((DbBackupHandle)other).DbBackupPtr;
        	}
        }
        
        #endregion

        private struct DbHandle : IDbHandle
        {
            public DbHandle(IntPtr dbPtr)
                : this()
            {
                DbPtr = dbPtr;
            }

            internal IntPtr DbPtr { get; set; }

            public bool Equals(IDbHandle other)
            {
                return other is DbHandle && DbPtr == ((DbHandle) other).DbPtr;
            }
        }

        private struct DbStatement : IDbStatement
        {
            public DbStatement(IntPtr stmtPtr)
                : this()
            {
                StmtPtr = stmtPtr;
            }

            internal IntPtr StmtPtr { get; set; }

            public bool Equals(IDbStatement other)
            {
                return other is DbStatement && StmtPtr == ((DbStatement) other).StmtPtr;
            }
        }
    }
}