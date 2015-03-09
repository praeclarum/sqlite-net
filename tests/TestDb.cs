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
    [Table("Product")]
    public interface IProduct
    {
        [AutoIncrement, PrimaryKey]
        int Id { get; set; }

        string Name { get; set; }
        decimal Price { get; set; }
        uint TotalSales { get; set; }
    }

    public class Product : IProduct
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        public string Name { get; set; }
        public decimal Price { get; set; }
        public uint TotalSales { get; set; }
    }

    [Table("Order")]
    public interface IOrder
    {
        [AutoIncrement, PrimaryKey]
        int Id { get; set; }

        DateTime PlacedTime { get; set; }
    }

    public class Order : IOrder
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        public DateTime PlacedTime { get; set; }
    }

    [Table("OrderHistory")]
    public interface IOrderHistory
    {
        [AutoIncrement, PrimaryKey]
        int Id { get; set; }

        int OrderId { get; set; }
        DateTime Time { get; set; }
        string Comment { get; set; }
    }

    public class OrderHistory : IOrderHistory
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public DateTime Time { get; set; }
        public string Comment { get; set; }
    }

    [Table("OrderLine")]
    public interface IOrderLine
    {
        [AutoIncrement, PrimaryKey]
        int Id { get; set; }

        [Indexed("IX_OrderProduct", 1)]
        int OrderId { get; set; }

        [Indexed("IX_OrderProduct", 2)]
        int ProductId { get; set; }

        int Quantity { get; set; }
        decimal UnitPrice { get; set; }
        OrderLineStatus Status { get; set; }
    }

    public class OrderLine : IOrderLine
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
        public TestDb(bool storeDateTimeAsTicks = false, IContractResolver resolver = null)
            : base(
                new SQLitePlatformTest(), TestPath.GetTempFileName(), storeDateTimeAsTicks, null,
                extraTypeMappings: null,
                resolver: resolver)
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