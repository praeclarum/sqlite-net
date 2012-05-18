using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLite
{
    internal class PooledConnection
    {
        internal long PoolId { get; private set; }
        internal SQLiteConnectionWithLock Connection { get; private set; }
        internal PooledConnectionState State { get; private set; }
        internal DateTime ExpiresUtc { get; private set; }

        private static long _nextPoolId = 0;

        private const int ExpirationMinutes = 120;

        internal PooledConnection(SQLiteConnectionWithLock conn)
        {
            this.PoolId = Interlocked.Increment(ref _nextPoolId);
            this.Connection = conn;
            this.State = PooledConnectionState.InUse;
            this.UpdateExpiration();
        }

        internal void SetIdle()
        {
            if(this.State == PooledConnectionState.Idle || this.State == PooledConnectionState.InUse)
                this.State = PooledConnectionState.Idle;
            else
                throw new NotSupportedException(string.Format("Cannot handle '{0}'.", this.State));
        }

        internal void SetAsInUse()
        {
            if (this.State == PooledConnectionState.Idle)
            {
                this.State = PooledConnectionState.InUse;
                this.UpdateExpiration();
            }
            else
                throw new NotSupportedException(string.Format("Cannot handle '{0}'.", this.State));
        }

        private void UpdateExpiration()
        {
            this.ExpiresUtc = DateTime.UtcNow.AddMinutes(ExpirationMinutes);
        }
    }
}
