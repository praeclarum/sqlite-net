using System;
using System.IO;
using SQLite.Net.Attributes;
using SQLite.Net.Platform.Win32;

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
            : base(new SQLitePlatformWin32(), TestPath.GetTempFileName(), storeDateTimeAsTicks)
        {
            Trace = true;
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