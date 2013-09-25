//
// Copyright (c) 2012 Krueger Systems, Inc.
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
using Sqlite3DatabaseHandle = Community.CsharpSqlite.Sqlite3.sqlite3;
using Sqlite3Statement = Community.CsharpSqlite.Sqlite3.Vdbe;
#elif USE_WP8_NATIVE_SQLITE
using Sqlite3 = Sqlite.Sqlite3;
using Sqlite3DatabaseHandle = Sqlite.Database;
using Sqlite3Statement = Sqlite.Statement;
#else
using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3Statement = System.IntPtr;
#endif

using System.Collections.Generic;

namespace SQLite
{
    class SQLiteConnectionPool
    {
        class Entry
        {
            public SQLiteConnectionString ConnectionString { get; private set; }
            public SQLiteConnectionWithLock Connection { get; private set; }

            public Entry (SQLiteConnectionString connectionString)
            {
                ConnectionString = connectionString;
                Connection = new SQLiteConnectionWithLock (connectionString);
            }

            public void OnApplicationSuspended ()
            {
                Connection.Dispose ();
                Connection = null;
            }
        }

        readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry> ();
        readonly object _entriesLock = new object ();

        static readonly SQLiteConnectionPool _shared = new SQLiteConnectionPool ();

        /// <summary>
        /// Gets the singleton instance of the connection tool.
        /// </summary>
        public static SQLiteConnectionPool Shared
        {
            get
            {
                return _shared;
            }
        }

        public SQLiteConnectionWithLock GetConnection (SQLiteConnectionString connectionString)
        {
            lock (_entriesLock) {
                Entry entry;
                string key = connectionString.ConnectionString;

                if (!_entries.TryGetValue (key, out entry)) {
                    entry = new Entry (connectionString);
                    _entries[key] = entry;
                }

                return entry.Connection;
            }
        }

        /// <summary>
        /// Closes all connections managed by this pool.
        /// </summary>
        public void Reset ()
        {
            lock (_entriesLock) {
                foreach (var entry in _entries.Values) {
                    entry.OnApplicationSuspended ();
                }
                _entries.Clear ();
            }
        }

        /// <summary>
        /// Call this method when the application is suspended.
        /// </summary>
        /// <remarks>Behaviour here is to close any open connections.</remarks>
        public void ApplicationSuspended ()
        {
            Reset ();
        }
    }
}