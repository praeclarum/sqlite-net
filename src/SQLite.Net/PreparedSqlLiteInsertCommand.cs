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

#if USE_CSHARP_SQLITE
using Sqlite3 = Community.CsharpSqlite.Sqlite3;
#elif USE_WP8_NATIVE_SQLITE
using Sqlite3 = Sqlite.Sqlite3;
#else
#endif

using System;
using System.Diagnostics;

namespace SQLite
{
    /// <summary>
    /// Since the insert never changed, we only need to prepare once.
    /// </summary>
    public class PreparedSqlLiteInsertCommand : IDisposable
    {
        public bool Initialized { get; set; }

        protected SQLiteConnection Connection { get; set; }

        public string CommandText { get; set; }

        protected ISqlite3Statement Statement { get; set; }
        internal static readonly ISqlite3Statement NullStatement = default(ISqlite3Statement);

        internal PreparedSqlLiteInsertCommand (SQLiteConnection conn)
        {
            Connection = conn;
        }

        public int ExecuteNonQuery (object[] source)
        {
            if (Connection.Trace) {
                Debug.WriteLine ("Executing: " + CommandText);
            }

            var r = SQLite3.Result.OK;

            if (!Initialized) {
                Statement = Prepare ();
                Initialized = true;
            }

            //bind the values.
            if (source != null) {
                for (int i = 0; i < source.Length; i++) {
                    SQLiteCommand.BindParameter (Statement, i + 1, source [i], Connection.StoreDateTimeAsTicks);
                }
            }
            r = SQLite3.Step (Statement);

            if (r == SQLite3.Result.Done) {
                int rowsAffected = SQLite3.Changes (Connection.Handle);
                SQLite3.Reset (Statement);
                return rowsAffected;
            } else if (r == SQLite3.Result.Error) {
                string msg = SQLite3.GetErrmsg (Connection.Handle);
                SQLite3.Reset (Statement);
                throw SQLiteException.New (r, msg);
            } else {
                SQLite3.Reset (Statement);
                throw SQLiteException.New (r, r.ToString ());
            }
        }

        protected virtual ISqlite3Statement Prepare()
        {
            var stmt = SQLite3.Prepare2 (Connection.Handle, CommandText);
            return stmt;
        }

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        private void Dispose (bool disposing)
        {
            if (Statement != NullStatement) {
                try {
                    SQLite3.Finalize (Statement);
                } finally {
                    Statement = NullStatement;
                    Connection = null;
                }
            }
        }

        ~PreparedSqlLiteInsertCommand ()
        {
            Dispose (false);
        }
    }
}