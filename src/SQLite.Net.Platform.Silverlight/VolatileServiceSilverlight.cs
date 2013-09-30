using SQLite.Net.Interop;

namespace SQLite.Net.Silverlight
{
    public class VolatileServiceSilverlight : IVolatileService
    {
        public void Write(ref int transactionDepth, int depth)
        {
            // :/
            // TODO: this is not good!
            transactionDepth = depth;
        }
    }
}