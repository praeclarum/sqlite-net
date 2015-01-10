using JetBrains.Annotations;

namespace SQLite.Net
{
    public interface ITraceListener
    {
        [PublicAPI]
        void Receive(string message);
    }
}