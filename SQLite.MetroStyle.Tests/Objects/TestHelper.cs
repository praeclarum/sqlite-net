using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite.Tests
{
    internal static class TestHelper
    {
        internal static string GetTempDatabaseName()
        {
            return Guid.NewGuid().ToString() + ".db";
        }
    }
}
