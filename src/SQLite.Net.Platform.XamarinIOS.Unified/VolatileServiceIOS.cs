using System.Threading;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.XamarinIOS
{
    public class VolatileServiceIOS : IVolatileService
    {
        public void Write(ref int transactionDepth, int depth)
        {
            Volatile.Write(ref transactionDepth, depth);
        }
    }
}