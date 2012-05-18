using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLite
{
    internal class PoolEntry
    {
        private SQLiteConnectionSpecification Specification { get; set; }
        private List<PooledConnection> Connections { get; set; }
        private object _connectionsLock = new object();

        private static object _globalLock = new object();

        internal PoolEntry(SQLiteConnectionSpecification spec)
        {
            this.Specification = spec;
            this.Connections = new List<PooledConnection>();
        }

        internal SQLiteConnectionWithLock GetUnusedConnection()
        {
            lock(_connectionsLock)
            {
                // find...
                PooledConnection idle = this.Connections.Where(v => v.State == PooledConnectionState.Idle).FirstOrDefault();
                if (idle != null)
                {
                    idle.SetAsInUse();
                    return idle.Connection;
                }

                // create one...
                var conn = this.Specification.GetConcreteConnectionWithLock(_globalLock);
                PooledConnection pooled = new PooledConnection(conn);
                pooled.Connection.Enlist(this, pooled.PoolId);
                this.Connections.Add(pooled);
                return pooled.Connection;
            }
        }

        internal bool ConnectionFinished(SQLiteConnection conn)
        {
            lock(_connectionsLock)
            {
                // find it...
                PooledConnection pooled = this.Connections.Where(v => v.PoolId == conn.PoolId).FirstOrDefault();
                if (pooled != null)
                {
                    // set it to idle...
                    pooled.SetIdle();

                    // signal that we found it...
                    return true;
                }
                else
                {
                    // signal that we didn't find...
                    return false;
                }
            }
        }

        internal void ApplicationSuspended()
        {
            lock(_connectionsLock)
            {
                // walk...
                List<PooledConnection> toRemove = new List<PooledConnection>();
                foreach (PooledConnection conn in this.Connections)
                {
                    if (conn.State == PooledConnectionState.Idle)
                    {
                        conn.Connection.CloseInternal();
                        toRemove.Add(conn);
                    }
                }

                // remove...
                foreach (PooledConnection conn in toRemove)
                    this.Connections.Remove(conn);
            }
        }
    }
}
