using System.Threading;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.Win32
{
    public class VolatileServiceWin32 : IVolatileService
    {
        public void Write(ref int transactionDepth, int depth)
        {
            Thread.VolatileWrite(ref transactionDepth, depth);
        }
    }
}