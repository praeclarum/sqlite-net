
using System;

namespace SQLitePCL
{
    internal static class Batteries_V2
    {
	    public static void Init()
	    {
#if PROVIDER_sqlite3
		    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
#elif PROVIDER_e_sqlite3
		    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
#else
#error batteries_v2.cs built with nothing specified
#endif
	    }
    }
}

