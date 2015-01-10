using System.Globalization;
using JetBrains.Annotations;

namespace SQLite.Net
{
    public static class TraceListenerExtensions
    {
        [PublicAPI]
        public static void WriteLine(this ITraceListener @this, string message)
        {
            if (@this == null)
            {
                return;
            }

            @this.Receive(message);
        }

        [PublicAPI]
        public static void WriteLine(this ITraceListener @this, string format, object arg1)
        {
            if (@this == null)
            {
                return;
            }

            @this.Receive(string.Format(CultureInfo.InvariantCulture, format, arg1));
        }

        [PublicAPI]
        public static void WriteLine(this ITraceListener @this, string format, object arg1, object arg2)
        {
            if (@this == null)
            {
                return;
            }

            @this.Receive(string.Format(CultureInfo.InvariantCulture, format, arg1, arg2));
        }

        [PublicAPI]
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