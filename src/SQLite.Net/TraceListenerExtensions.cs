using System.Globalization;

namespace SQLite.Net
{
    public static class TraceListenerExtensions
    {
        public static void WriteLine(this ITraceListener @this, string message)
        {
            if (@this == null)
            {
                return;
            }

            @this.Receive(message);
        }

        public static void WriteLine(this ITraceListener @this, string format, object arg1)
        {
            if (@this == null)
            {
                return;
            }

            @this.Receive(string.Format(CultureInfo.InvariantCulture, format, arg1));
        }

        public static void WriteLine(this ITraceListener @this, string format, object arg1, object arg2)
        {
            if (@this == null)
            {
                return;
            }

            @this.Receive(string.Format(CultureInfo.InvariantCulture, format, arg1, arg2));
        }

        public static void WriteLine(this ITraceListener @this, string format, params object[] args)
        {
            if (@this == null)
            {
                return;
            }

            @this.Receive(string.Format(CultureInfo.InvariantCulture, format, args));
        }
    }
}