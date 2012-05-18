using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite
{
    /// <summary>
    /// Defines a class that points to a database.
    /// </summary>
    public class SQLiteConnectionSpecification
    {
        private string DatabasePath { get; set; }
        private SQLiteOpenFlags Flags { get; set; }
        private bool HasFlags { get; set; }
        private bool OverridePath { get; set; }
        internal string Key { get; set; }

        public SQLiteConnectionSpecification(string databasePath, bool overridePath = false)
        {
            this.Initialize(databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite, false, overridePath);
        }

        public SQLiteConnectionSpecification(string databasePath, SQLiteOpenFlags flags, bool overridePath = false)
        {
            this.Initialize(databasePath, flags, true, overridePath);
        }

        /// <summary>
        /// Method for creating specifications for use with asynchronous calls. This must be used with the async methods.
        /// </summary>
        /// <param name="databasePath"></param>
        /// <returns></returns>
        public static SQLiteConnectionSpecification CreateForAsync(string databasePath)
        {
            return new SQLiteConnectionSpecification(databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex,
                false);
        }

        private void Initialize(string databasePath, SQLiteOpenFlags flags, bool hasFlags, bool overridePath)
        {
            this.DatabasePath = databasePath;
            this.Flags = flags;
            this.HasFlags = hasFlags;

            // configure the key...
            this.Key = string.Format("{0}|{1}|{2}|{3}", this.Flags, this.HasFlags, this.OverridePath, this.DatabasePath);
        }

        internal SQLiteConnectionWithLock GetConcreteConnectionWithLock(object lockPoint)
        {
            if (this.HasFlags)
                return new SQLiteConnectionWithLock(this.DatabasePath, this.Flags, lockPoint);
            else
                return new SQLiteConnectionWithLock(this.DatabasePath, lockPoint);
        }
    }
}
