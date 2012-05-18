using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite
{
    public enum PooledConnectionState
    {
        Idle = 0,
        InUse = 1   
    }
}
