using System.Diagnostics;

namespace SQLite.Net
{
    public sealed class DebugTraceListener : ITraceListener
    {
        public static DebugTraceListener Instance = new DebugTraceListener();

        private DebugTraceListener()
        {
        }

        public void Receive(string message)
        {
            Debug.WriteLine(message);
        }
    }
}