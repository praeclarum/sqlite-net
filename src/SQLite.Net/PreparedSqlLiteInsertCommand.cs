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
using System.Diagnostics;
using SQLite.Net.Interop;

namespace SQLite.Net
{
    /// <summary>
    ///     Since the insert never changed, we only need to prepare once.
    /// </summary>
    public class PreparedSqlLiteInsertCommand : IDisposable
    {
        internal static readonly IDbStatement NullStatement = default(IDbStatement);
        private readonly ISQLitePlatform _sqlitePlatform;

        internal PreparedSqlLiteInsertCommand(ISQLitePlatform platformImplementation, SQLiteConnection conn)
        {
            _sqlitePlatform = platformImplementation;
            Connection = conn;
        }

        public bool Initialized { get; set; }

        protected SQLiteConnection Connection { get; set; }

        public string CommandText { get; set; }

        protected IDbStatement Statement { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int ExecuteNonQuery(object[] source)
        {
            if (Connection.Trace)
            {
                Debug.WriteLine("Executing: " + CommandText);
            }

            var r = Result.OK;

            if (!Initialized)
            {
                Statement = Prepare();
                Initialized = true;
            }

            //bind the values.
            if (source != null)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    SQLiteCommand.BindParameter(_sqlitePlatform.SQLiteApi, Statement, i + 1, source[i],
                        Connection.StoreDateTimeAsTicks);
                }
            }
            r = _sqlitePlatform.SQLiteApi.Step(Statement);

            if (r == Result.Done)
            {
                int rowsAffected = _sqlitePlatform.SQLiteApi.Changes(Connection.Handle);
                _sqlitePlatform.SQLiteApi.Reset(Statement);
                return rowsAffected;
            }
            if (r == Result.Error)
            {
                string msg = _sqlitePlatform.SQLiteApi.Errmsg16(Connection.Handle);
                _sqlitePlatform.SQLiteApi.Reset(Statement);
                throw SQLiteException.New(r, msg);
            }
            _sqlitePlatform.SQLiteApi.Reset(Statement);
            throw SQLiteException.New(r, r.ToString());
        }

        protected virtual IDbStatement Prepare()
        {
            IDbStatement stmt = _sqlitePlatform.SQLiteApi.Prepare2(Connection.Handle, CommandText);
            return stmt;
        }

        private void Dispose(bool disposing)
        {
            if (Statement != NullStatement)
            {
                try
                {
                    _sqlitePlatform.SQLiteApi.Finalize(Statement);
                }
                finally
                {
                    Statement = NullStatement;
                    Connection = null;
                }
            }
        }

        ~PreparedSqlLiteInsertCommand()
        {
            Dispose(false);
        }
    }
}