using System;
using SQLite;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using OneToManyRelationshipExample;
using System.Linq;

namespace OneToManyRelationshipExample
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var db = InitDb();

			db.InsertAll(CreateCustomers(5,5),"OR REPLACE");

			var customersFromDb = db.Table<Customer>().ToList();
			var ordersFromDb = db.Table<Order>().ToList();

			PrintInfo(customersFromDb,"customers");
			PrintInfo(ordersFromDb, "orders");

			var map = new TableMapping(typeof(Customer));
			var q = string.Format("update {0} set Id = 999 where Id = 1",map.TableName);
			db.Query(map,q,null);

			customersFromDb = db.Table<Customer>().Where(x => x.Id == 999).ToList();
			ordersFromDb = db.Table<Order>().Where(x => x.CustomerId == 999).ToList();

			PrintInfo(customersFromDb,"customers");
			PrintInfo(ordersFromDb, "orders");

			db.DeleteAll<Customer>();

			customersFromDb = db.Table<Customer>().ToList();
			ordersFromDb = db.Table<Order>().ToList();

			PrintInfo(customersFromDb,"customers");
			PrintInfo(ordersFromDb, "orders");
		}

		private static void PrintInfo (IList objects,string objType)
		{
			if(objects.Count == 0){
				Console.WriteLine (string.Format("\nThere are no {0}!",objType));
			}

			foreach(var o in objects){
					Console.WriteLine (o.ToString());
			}
		}

		private static List<Customer> CreateCustomers (int customersCount, int ordersCount)
		{
			var customers = Enumerable.Range (1, customersCount)
				.Select (x => new Customer {Id = x,Name = "Customer" + x,Orders = Enumerable.Range(1,ordersCount)
					.Select(y => new Order{Id = (x*10)+y, OrderName = string.Format("Order{0}",(x*10)+y),CustomerId = x})
						.ToList()}
			).ToList ();

			return customers;
		}

		private static SQLiteConnection InitDb()
		{
			var dbPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "OneToMany.db");
			var db = new SQLiteConnection (dbPath);
			db.SetForeignKeysPermissions(true);
			db.CreateTable<Customer>();
			db.CreateTable<Order>();

			return db;
		}
	}
}
