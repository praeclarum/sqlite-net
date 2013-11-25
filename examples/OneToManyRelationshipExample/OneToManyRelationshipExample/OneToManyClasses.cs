using System;
using SQLite;
using System.Collections.Generic;

namespace OneToManyRelationshipExample
{
	public class Customer
	{
		public Customer(string name, List<Order> orders)
		{
			this.Name = name;
			this.Orders = orders;
		}
		public Customer ()
		{
			
		}

		[PrimaryKey]
		public int Id {get; set;}

		public string Name {get; set;}

		[One2Many(typeof(Order))]
		public List<Order> Orders {get; set;}

		public override string ToString()
		{
			return string.Format("CustomerId: {0} \t Name: {1}",Id,Name);
		}
	}

	public class Order
	{
		public Order (string orderName, int customerId)
		{
			this.OrderName = orderName;
			this.CustomerId = customerId;
		}

		public Order ()
		{
			
		}

		[PrimaryKey]
		public int Id {get; set;}

		public string OrderName {get; set;}

		[References(typeof(Customer))]
		[OnUpdateCascade]
		[OnDeleteCascade]
		public int CustomerId {get; set;}

		public override string ToString()
		{
			return string.Format("OrderId: {0} \t CustomerId: {1}",Id,CustomerId);
		}
	}
}

