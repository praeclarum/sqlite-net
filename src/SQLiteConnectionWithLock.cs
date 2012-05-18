using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLite
{
    internal class SQLiteConnectionWithLock : SQLiteConnection
    {
        private object LockPoint { get; set; }

        internal SQLiteConnectionWithLock(string databasePath, object lockPoint)
            : base(databasePath)
        {
            this.Initialize(lockPoint);
        }

        internal SQLiteConnectionWithLock(string databasePath, SQLiteOpenFlags flags, object lockPoint)
            : base(databasePath, flags)
        {
            this.Initialize(lockPoint);
        }

        private void Initialize(object lockPoint)
        {
            this.LockPoint = lockPoint;
        }

        internal IDisposable Lock()
        {
            return new LockWrapper(this);
        }

        private class LockWrapper : IDisposable
        {
            private SQLiteConnectionWithLock Owner { get; set; }

            internal LockWrapper(SQLiteConnectionWithLock owner)
            {
                this.Owner = owner;

                // lock it...
                Monitor.Enter(this.Owner.LockPoint);
            }

            public void Dispose()
            {
                // unlock it...
 	            Monitor.Exit(this.Owner.LockPoint);
            }
        }
    }
}
