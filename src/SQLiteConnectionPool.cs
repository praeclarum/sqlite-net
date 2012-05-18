using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLite
{
    public class SQLiteConnectionPool
    {
        private Dictionary<string, PoolEntry> Entries { get; set; }
        private object _entriesLock = new object();

		private static SQLiteConnectionPool _current = new SQLiteConnectionPool();
				
		private SQLiteConnectionPool()
		{
            this.Entries = new Dictionary<string, PoolEntry>();
		}
						
		/// <summary>
		/// Gets the singleton instance of the connection tool.
		/// </summary>
		public static SQLiteConnectionPool Current
		{
			get
			{
				if(_current == null)
					throw new ObjectDisposedException("SQLiteConnectionPool");
				return _current;
			}
		}

        internal SQLiteConnectionWithLock GetConnection(SQLiteConnectionSpecification spec)
        {
            lock(_entriesLock)
            {
                string key = spec.Key;
                if (!(this.Entries.ContainsKey(key)))
                    this.Entries[key] = new PoolEntry(spec);

                // return an unused connection...
                PoolEntry entry = this.Entries[key];
                return entry.GetUnusedConnection();
            }
        }

        /// <summary>
        /// Resets the pool - only for use in unit tests.
        /// </summary>
        public void Reset()
        {
            _current = new SQLiteConnectionPool();
        }

        internal void ConnectionFinished(SQLiteConnection conn)
        {
            lock(_entriesLock)
            {
                // signal that we've finished...
                bool  found= false;
                foreach(PoolEntry entry in this.Entries.Values)
                {
                    if(entry.ConnectionFinished(conn))
                    {
                        found= true;
                        break;
                    }
                }

                // if we're not found, close directly...
                if(!(found))
                    conn.CloseInternal ();
            }
        }

        /// <summary>
        /// Call this method when the application is suspended.
        /// </summary>
        /// <remarks>Behaviour here is to close any open connections that are idle. We can't wait around in this
        /// call (we only have five seconds total), so we can't block for running queries to finish. What happens
        /// is that the pool is reset. Any connections that call Dispose after this point will not be found in the pool
        /// and regular cleanup operations will happen.</remarks>
        public void ApplicationSuspended()
        {
            lock(_entriesLock)
            {
                // find...
                foreach (PoolEntry entry in this.Entries.Values)
                    entry.ApplicationSuspended();

                // reset the pool...
                this.Entries = new Dictionary<string, PoolEntry>();
            }
        }
    }
}
