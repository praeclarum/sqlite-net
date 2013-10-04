using SQLite.Net.Interop;

namespace SQLite.Net.Platform.WindowsPhone71
{
    public class VolatileServiceWP71 : IVolatileService
    {
        public void Write(ref int transactionDepth, int depth)
        {
            // :/
            // TODO: this is not good!
            transactionDepth = depth;
        }
    }
}