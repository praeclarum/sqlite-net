using System;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.WinRT
{
    public class SQLiteApiWinRT : ISQLiteApi
    {
        public Result Open(byte[] filename, out IDbHandle db, int flags, IntPtr zvfs)
        {
            throw new NotImplementedException();
        }

        public Result EnableLoadExtension(IDbHandle db, int onoff)
        {
            throw new NotImplementedException();
        }

        public Result Close(IDbHandle db)
        {
            throw new NotImplementedException();
        }

        public Result BusyTimeout(IDbHandle db, int milliseconds)
        {
            throw new NotImplementedException();
        }

        public int Changes(IDbHandle db)
        {
            throw new NotImplementedException();
        }

        public IDbStatement Prepare2(IDbHandle db, string query)
        {
            throw new NotImplementedException();
        }

        public Result Step(IDbStatement stmt)
        {
            throw new NotImplementedException();
        }

        public Result Reset(IDbStatement stmt)
        {
            throw new NotImplementedException();
        }

        public Result Finalize(IDbStatement stmt)
        {
            throw new NotImplementedException();
        }

        public long LastInsertRowid(IDbHandle db)
        {
            throw new NotImplementedException();
        }

        public string Errmsg16(IDbHandle db)
        {
            throw new NotImplementedException();
        }

        public int BindParameterIndex(IDbStatement stmt, string name)
        {
            throw new NotImplementedException();
        }

        public int BindNull(IDbStatement stmt, int index)
        {
            throw new NotImplementedException();
        }

        public int BindInt(IDbStatement stmt, int index, int val)
        {
            throw new NotImplementedException();
        }

        public int BindInt64(IDbStatement stmt, int index, long val)
        {
            throw new NotImplementedException();
        }

        public int BindDouble(IDbStatement stmt, int index, double val)
        {
            throw new NotImplementedException();
        }

        public int BindText16(IDbStatement stmt, int index, string val, int n, IntPtr free)
        {
            throw new NotImplementedException();
        }

        public int BindBlob(IDbStatement stmt, int index, byte[] val, int n, IntPtr free)
        {
            throw new NotImplementedException();
        }

        public int ColumnCount(IDbStatement stmt)
        {
            throw new NotImplementedException();
        }

        public string ColumnName16(IDbStatement stmt, int index)
        {
            throw new NotImplementedException();
        }

        public ColType ColumnType(IDbStatement stmt, int index)
        {
            throw new NotImplementedException();
        }

        public int ColumnInt(IDbStatement stmt, int index)
        {
            throw new NotImplementedException();
        }

        public long ColumnInt64(IDbStatement stmt, int index)
        {
            throw new NotImplementedException();
        }

        public double ColumnDouble(IDbStatement stmt, int index)
        {
            throw new NotImplementedException();
        }

        public string ColumnText16(IDbStatement stmt, int index)
        {
            throw new NotImplementedException();
        }

        public byte[] ColumnBlob(IDbStatement stmt, int index)
        {
            throw new NotImplementedException();
        }

        public int ColumnBytes(IDbStatement stmt, int index)
        {
            throw new NotImplementedException();
        }

        public byte[] ColumnByteArray(IDbStatement stmt, int index)
        {
            throw new NotImplementedException();
        }
    }
}