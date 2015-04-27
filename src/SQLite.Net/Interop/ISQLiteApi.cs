//
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

using System;
using JetBrains.Annotations;

namespace SQLite.Net.Interop
{
    [PublicAPI]
    public interface ISQLiteApi
    {
        Result Open(byte[] filename, out IDbHandle db, int flags, IntPtr zvfs);
        //        Result Open16(string filename, out IDbHandle db);

        ExtendedResult ExtendedErrCode(IDbHandle db);
        int LibVersionNumber();
        string SourceID();
        Result EnableLoadExtension(IDbHandle db, int onoff);
        Result Close(IDbHandle db);
        Result Initialize();
        Result Shutdown();
        Result Config(ConfigOption option);
        //        int SetDirectory(uint directoryType, string directoryPath);

        Result BusyTimeout(IDbHandle db, int milliseconds);
        int Changes(IDbHandle db);
        //        Result Prepare2(IDbHandle db, string sql, int numBytes, out IDbStatement stmt, IntPtr pzTail);

        IDbStatement Prepare2(IDbHandle db, string query);
        Result Step(IDbStatement stmt);
        Result Reset(IDbStatement stmt);
        Result Finalize(IDbStatement stmt);
        long LastInsertRowid(IDbHandle db);
        string Errmsg16(IDbHandle db);
        //        string GetErrmsg(IDbHandle db);

        int BindParameterIndex(IDbStatement stmt, string name);
        int BindNull(IDbStatement stmt, int index);
        int BindInt(IDbStatement stmt, int index, int val);
        int BindInt64(IDbStatement stmt, int index, long val);
        int BindDouble(IDbStatement stmt, int index, double val);
        int BindText16(IDbStatement stmt, int index, string val, int n, IntPtr free);
        int BindBlob(IDbStatement stmt, int index, byte[] val, int n, IntPtr free);
        int ColumnCount(IDbStatement stmt);
        //        string ColumnName(IDbStatement stmt, int index);

        string ColumnName16(IDbStatement stmt, int index);
        ColType ColumnType(IDbStatement stmt, int index);
        int ColumnInt(IDbStatement stmt, int index);
        long ColumnInt64(IDbStatement stmt, int index);
        double ColumnDouble(IDbStatement stmt, int index);
        //        string ColumnText(IDbStatement stmt, int index);

        string ColumnText16(IDbStatement stmt, int index);
        byte[] ColumnBlob(IDbStatement stmt, int index);
        int ColumnBytes(IDbStatement stmt, int index);
        //        string ColumnText(IDbStatement stmt, int index);

        byte[] ColumnByteArray(IDbStatement stmt, int index);
    }
}