using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SQLite.Tests
{
    public static class TestHelper
    {
        internal static string GetTempDatabaseName()
        {
            return Path.GetTempFileName();
        }
    }
}
