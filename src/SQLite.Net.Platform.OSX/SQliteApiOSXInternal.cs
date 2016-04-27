using System;
using System.IO;
using System.Runtime.InteropServices;
using SQLite.Net.Interop;
using System.Reflection;

namespace SQLite.Net.Platform.OSX
{
    internal static class SQLiteApiOSXInternal
    {
        static SQLiteApiOSXInternal()
        {
		}

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_open([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_open([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db,
            int flags,
            IntPtr zvfs);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_open16", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_open16([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr db);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_enable_load_extension",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_enable_load_extension(IntPtr db, int onoff);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_close(IntPtr db);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_initialize", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_initialize();

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_shutdown", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_shutdown();

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_config", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_config(ConfigOption option);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_win32_set_directory",
            CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int sqlite3_win32_set_directory(uint directoryType, string directoryPath);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_busy_timeout",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_busy_timeout(IntPtr db, int milliseconds);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_changes", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_changes(IntPtr db);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_prepare16_v2", CallingConvention = CallingConvention.Cdecl)
        ]
        public static extern Result sqlite3_prepare16_v2(IntPtr db, [MarshalAs(UnmanagedType.LPWStr)] string sql,
            int numBytes,
            out IntPtr stmt, IntPtr pzTail);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_step(IntPtr stmt);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_reset", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_reset(IntPtr stmt);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_finalize(IntPtr stmt);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_last_insert_rowid",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern long sqlite3_last_insert_rowid(IntPtr db);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_errmsg16", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_errmsg16(IntPtr db);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_bind_parameter_index",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_parameter_index(IntPtr stmt, [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_bind_null", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_null(IntPtr stmt, int index);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_int(IntPtr stmt, int index, int val);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_bind_int64", CallingConvention = CallingConvention.Cdecl)
        ]
        public static extern int sqlite3_bind_int64(IntPtr stmt, int index, long val);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_bind_double", CallingConvention = CallingConvention.Cdecl
            )]
        public static extern int sqlite3_bind_double(IntPtr stmt, int index, double val);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_bind_text16", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode)]
        public static extern int sqlite3_bind_text16(IntPtr stmt, int index,
            [MarshalAs(UnmanagedType.LPWStr)] string val,
            int n,
            IntPtr free);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_bind_blob", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_blob(IntPtr stmt, int index, byte[] val, int n, IntPtr free);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_count",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_column_count(IntPtr stmt);

        //        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_name", CallingConvention = CallingConvention.Cdecl)]
        //        private extern IntPtr ColumnNameInternal(IntPtr stmt, int index);

        //        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_name", CallingConvention = CallingConvention.Cdecl)]
        //        public string ColumnName(IntPtr stmt, int index)
        //        {
        //            return ColumnNameInternal(stmt, index);
        //        }

        public static string ColumnName16(IntPtr stmt, int index)
        {
            return Marshal.PtrToStringUni(sqlite3_column_name16(stmt, index));
        }

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_type", CallingConvention = CallingConvention.Cdecl
            )]
        public static extern ColType sqlite3_column_type(IntPtr stmt, int index);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)
        ]
        public static extern int sqlite3_column_int(IntPtr stmt, int index);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_int64",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern long sqlite3_column_int64(IntPtr stmt, int index);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_double",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern double sqlite3_column_double(IntPtr stmt, int index);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_text16",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_column_text16(IntPtr stmt, int index);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_blob", CallingConvention = CallingConvention.Cdecl
            )]
        public static extern byte[] ColumnBlob(IntPtr stmt, int index);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_blob", CallingConvention = CallingConvention.Cdecl
            )]
        public static extern IntPtr sqlite3_column_blob(IntPtr stmt, int index);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_bytes",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_column_bytes(IntPtr stmt, int index);

        public static byte[] ColumnByteArray(IntPtr stmt, int index)
        {
            int length = sqlite3_column_bytes(stmt, index);
            var result = new byte[length];
            if (length > 0)
            {
                Marshal.Copy(sqlite3_column_blob(stmt, index), result, 0, length);
            }
            return result;
        }

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_open_v2(byte[] filename, out IntPtr db, int flags, IntPtr zvfs);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_column_name16",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sqlite3_column_name16(IntPtr stmt, int index);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_extended_errcode", CallingConvention = CallingConvention.Cdecl)]
        public static extern ExtendedResult sqlite3_extended_errcode(IntPtr db);

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_libversion_number", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_libversion_number();

        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_sourceid", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr sqlite3_sourceid();

        #region Backup
        
        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_backup_init", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_backup_init(IntPtr destDB,
                                                        [MarshalAs(UnmanagedType.LPStr)] string  destName, 
                                                        IntPtr srcDB,
                                                        [MarshalAs(UnmanagedType.LPStr)] string srcName);
        
        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_backup_step", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_backup_step(IntPtr backup, int pageCount);
        
        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_backup_finish", CallingConvention = CallingConvention.Cdecl)]
        public static extern Result sqlite3_backup_finish(IntPtr backup);
        
        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_backup_remaining", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_backup_remaining(IntPtr backup);
        
        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_backup_pagecount", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_backup_pagecount(IntPtr backup);
        
        [DllImport("libsqlite3_for_net", EntryPoint = "sqlite3_sleep", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_sleep(int millis);
        
        #endregion
    }
}
