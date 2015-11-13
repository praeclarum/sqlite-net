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
using JetBrains.Annotations;
using SQLite.Net.Interop;
using System.Diagnostics;

namespace SQLite.Net
{
    /// <summary>
    ///     Since the insert never changed, we only need to prepare once.
    /// </summary>
    public class PreparedSqlLiteInsertCommand : IDisposable
    {
        private static readonly IDbStatement NullStatement = default(IDbStatement);

        internal PreparedSqlLiteInsertCommand(SQLiteConnection conn)
        {
            Connection = conn;
        }

        [PublicAPI]
        public bool Initialized { get; set; }

        [PublicAPI]
        public string CommandText { get; set; }

        [PublicAPI]
        protected SQLiteConnection Connection { get; set; }

        [PublicAPI]
        protected IDbStatement Statement { get; set; }

        [PublicAPI]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PreparedSqlLiteInsertCommand()
        {
            Dispose(false);
        }

        static readonly object _locker = new object();

        [PublicAPI]
        public int ExecuteNonQuery(object[] source)
        {
            Connection.TraceListener.WriteLine("Executing: {0}", CommandText);
            if (!Initialized)
            {
                Statement = Prepare();
                Initialized = true;
            }

            var sqlitePlatform = Connection.Platform;
            //bind the values.
            if (source != null)
            {
                for (var i = 0; i < source.Length; i++)
                {
                    SQLiteCommand.BindParameter(sqlitePlatform.SQLiteApi, Statement, i + 1, source[i],
                        Connection.StoreDateTimeAsTicks, Connection.Serializer);
                }
            }

            Result r;
            lock (_locker)
            {
                r = sqlitePlatform.SQLiteApi.Step(Statement);
            }

            if (r == Result.Done)
            {
                var rowsAffected = sqlitePlatform.SQLiteApi.Changes(Connection.Handle);
                sqlitePlatform.SQLiteApi.Reset(Statement);
                return rowsAffected;
            }
            if (r == Result.Error)
            {
                var msg = sqlitePlatform.SQLiteApi.Errmsg16(Connection.Handle);
                sqlitePlatform.SQLiteApi.Reset(Statement);
                throw SQLiteException.New(r, msg);
            }
            if (r == Result.Constraint && sqlitePlatform.SQLiteApi.ExtendedErrCode(Connection.Handle) == ExtendedResult.ConstraintNotNull)
            {
                sqlitePlatform.SQLiteApi.Reset(Statement);
                throw NotNullConstraintViolationException.New(r, sqlitePlatform.SQLiteApi.Errmsg16(Connection.Handle));
            }
            sqlitePlatform.SQLiteApi.Reset(Statement);

            throw SQLiteException.New(r, r.ToString());
        }

        [PublicAPI]
        protected virtual IDbStatement Prepare()
        {
            var stmt = Connection.Platform.SQLiteApi.Prepare2(Connection.Handle, CommandText);
            return stmt;
        }

        private void Dispose(bool disposing)
        {
            if (Statement != NullStatement)
            {
                try
                {
                    Connection.Platform.SQLiteApi.Finalize(Statement);
                }
                finally
                {
                    Statement = NullStatement;
                    Connection = null;
                }
            }
        }
    }
}
