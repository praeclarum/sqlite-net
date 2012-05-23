using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite.Tests
{
    internal static class TestHelper
    {
        internal static string GetTempDatabasePath()
        {
            return Path.Combine(SQLiteConnectionSpecification.MetroStyleDataPath, Guid.NewGuid().ToString() + ".db");
        }
    }
}
