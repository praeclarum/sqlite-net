using System;
using System.IO;
using System.Collections.Generic;

#if NETFX_CORE
class DescriptionAttribute : Attribute
{
	public DescriptionAttribute (string desc)
	{
	}
}
#endif

namespace SQLite.Tests
{
	public class Product
	{
		[AutoIncrement, PrimaryKey]
		public int Id { get; set; }
		public string Name { get; set; }
		public decimal Price { get; set; }

		public uint TotalSales { get; set; }
	}
	public class ProductWithFK
	{
		[AutoIncrement, PrimaryKey]
		public int Id { get; set; }
		public string Name { get; set; }
		public decimal Price { get; set; }
		public uint TotalSales { get; set; }
		[References(typeof(Order1WithO2M))]
		[ForeignKey]
		public int Owner1Id {get; set;}

		[References(typeof(Order2WithO2M))]
		[ForeignKey]
		public int Owner2Id {get; set;}
	}

	public class Order
	{
		[AutoIncrement, PrimaryKey]
		public int Id { get; set; }
		public DateTime PlacedTime { get; set; }
	}
	public class Order1WithO2M
	{
		[AutoIncrement, PrimaryKey]
		public int Id { get; set; }
		public DateTime PlacedTime { get; set; }

		[One2Many(typeof(ProductWithFK))]
		public List<ProductWithFK> Products {get; set;}
	}
	public class Order2WithO2M
	{
		[AutoIncrement, PrimaryKey]
		public int Id { get; set; }
		public DateTime PlacedTime { get; set; }

		[One2Many(typeof(ProductWithFK))]
		public List<ProductWithFK> Products {get; set;}
	}
	public class OrderHistory {
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
	public enum OrderLineStatus {
		Placed = 1,
		Shipped = 100
	}

	public class TestDb : SQLiteConnection
	{
		public TestDb (bool storeDateTimeAsTicks = false) : base (TestPath.GetTempFileName (), storeDateTimeAsTicks)
		{
			Trace = true;
		}
	}

	public class TestPath
	{
		public static string GetTempFileName ()
		{
#if NETFX_CORE
			var name = Guid.NewGuid () + ".sqlite";
			return Path.Combine (Windows.Storage.ApplicationData.Current.LocalFolder.Path, name);
#else
			return Path.GetTempFileName ();
#endif
		}
	}
}

