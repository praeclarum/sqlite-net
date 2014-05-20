using System;
using System.IO;
using SQLite.Net.Attributes;

#if __WIN32__
using SQLitePlatformTest = SQLite.Net.Platform.Win32.SQLitePlatformWin32;
#elif WINDOWS_PHONE
using SQLitePlatformTest = SQLite.Net.Platform.WindowsPhone8.SQLitePlatformWP8;
#elif __WINRT__
using SQLitePlatformTest = SQLite.Net.Platform.WinRT.SQLitePlatformWinRT;
#elif __IOS__
using SQLitePlatformTest = SQLite.Net.Platform.XamarinIOS.SQLitePlatformIOS;
#elif __ANDROID__
using SQLitePlatformTest = SQLite.Net.Platform.XamarinAndroid.SQLitePlatformAndroid;
#else
using SQLitePlatformTest = SQLite.Net.Platform.Generic.SQLitePlatformGeneric;
#endif


namespace SQLite.Net.Tests
{
    public class Product
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        public string Name { get; set; }
        public decimal Price { get; set; }

        public uint TotalSales { get; set; }
    }

    public class Order
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        public DateTime PlacedTime { get; set; }
    }

    public class OrderHistory
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public DateTime Time { get; set; }
        public string Comment { get; set; }
    }

    public class OrderLine
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        [Indexed("IX_OrderProduct", 1)]
        public int OrderId { get; set; }

        [Indexed("IX_OrderProduct", 2)]
        public int ProductId { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public OrderLineStatus Status { get; set; }
    }

    public enum OrderLineStatus
    {
        Placed = 1,
        Shipped = 100
    }

    public class TestDb : SQLiteConnection
    {
        public TestDb(bool storeDateTimeAsTicks = false)
            : base(new SQLitePlatformTest(), TestPath.GetTempFileName(), storeDateTimeAsTicks)
        {
            TraceListener = DebugTraceListener.Instance;
        }
    }

    public class TestPath
    {
        public static string GetTempFileName()
        {
            return Path.GetTempFileName();
        }
    }
}