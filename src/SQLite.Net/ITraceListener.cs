namespace SQLite.Net
{
    public interface ITraceListener
    {
        void Receive(string message);
    }
}