using System.Threading;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.WindowsPhone8
{
    public class VolatileServiceWP8 : IVolatileService
    {
        public void Write(ref int transactionDepth, int depth)
        {
            Volatile.Write(ref transactionDepth, depth);
        }
    }
}