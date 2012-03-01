using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SQLite.Tests
{
	[TestFixture]
	public class MultiplePKs
	{
		private class TestDb<TObj> : SQLiteConnection
		{
			public TestDb(String path) :base(path)
			{
				CreateTable<TObj>();
			}
		}
		
		/// <summary>Testing multiple keys and partial classes</summary>
		[PrimaryKeyNames("Id0", "Id1")]
		private partial class TestObj1
		{
			public int Id0 { get; set; }
			public string Text { get; set; }
			public override string ToString() { return string.Format("[TestObj: Id0={0}, Id1={1}, Text={2}]", Id0, Id1, Text); }
		}
		private partial class TestObj1 
		{
			public int Id1 { get; set; }
		}
		
		[Test]
		public void MultiplePK_Test1()
		{
			var db = new TestDb<TestObj1>(Path.GetTempFileName()) {Trace = false};
			
			// insert some objects with multiple primary keys
			const int I = 10, J = 5;
			TestObj1[] objs = new TestObj1[I*J];
			for (int j = 0; j != J; ++j)
			for (int i = 0; i != I; ++i)
				objs[j*I + i] = new TestObj1{Id0 = i, Id1 = j, Text = string.Format("I am ({0},{1})",i,j)};
			
			Assert.AreEqual(I*J, db.InsertAll(objs));
			
			// get an object using multiple keys
			var obj = db.Get<TestObj1>(5,3);
			Assert.AreEqual(5, obj.Id0);
			Assert.AreEqual(3, obj.Id1);
			Assert.AreEqual("I am (5,3)", obj.Text);
			
			// check that insert constraints work for multiple key objects
			try { db.Insert(obj); }
			catch (SQLiteException ex) { Assert.AreEqual(SQLite3.Result.Constraint, ex.Result); }
			
			// update objects with multiple primary keys
			obj.Text = "I've been changed";
			db.Update(obj);
			Assert.AreEqual("I've been changed", db.Get<TestObj1>(5,3).Text);
			
			// update a specific field of an object
			db.Update<TestObj1>("Text", "I've been changed also", 8, 2);
			Assert.AreEqual("I've been changed also", db.Get<TestObj1>(8,2).Text);
			
			// update a specific field for all objects in a table
			db.UpdateAll<TestObj1>("Text", "All changed");
			foreach (var o in (from o in db.Table<TestObj1>() select o))
				Assert.AreEqual("All changed", o.Text);
			
			// count objects in the table
			var numCount = db.Table<TestObj1>().Count();
			Assert.AreEqual(numCount, objs.Length);
			
			// delete an object using multiple keys
			obj = db.Get<TestObj1>(8,2);
			db.Delete(obj);
			Assert.Throws(typeof(InvalidOperationException), ()=> db.Get<TestObj1>(8,2));
			
			// delete a set of objects
			db.CreateCommand("delete from TestObj1 where Id1=2").ExecuteNonQuery();
			Assert.AreEqual(objs.Length - 10, db.Table<TestObj1>().Count());
			foreach (var o in (from o in db.Table<TestObj1>() select o))
				Assert.AreNotEqual(2, o.Id1);
			
			// check the method for reading the primary keys from an object
			obj = db.Get<TestObj1>(2,3);
			var pks = db.GetPrimaryKeys(obj);
			Assert.AreEqual(obj.Id0, (int)pks[0]);
			Assert.AreEqual(obj.Id1, (int)pks[1]);
			
			// check querying for an object that isn't in the table
			Assert.IsNull(db.GetOrDefault<TestObj1>(11,-1));
			
			// check querying with an incorrect number of primary keys
			try { db.GetOrDefault<TestObj1>(1); }
			catch (SQLiteException ex) { Assert.AreEqual(SQLite3.Result.Error, ex.Result); }
			try { db.GetOrDefault<TestObj1>(1,2,3); }
			catch (SQLiteException ex) { Assert.AreEqual(SQLite3.Result.Error, ex.Result); }
		}
		
		/// <summary>Testing multiple keys given by property attribute and class inheritance</summary>
		private class TestObj2Base
		{
			// testing key order not dependent on property order
			[PrimaryKey(MultiKeyOrder = 2)] public int Id2 { get; set; }
			[PrimaryKey(MultiKeyOrder = 0)] public int Id0 { get; set; }
		}
		private class TestObj2 :TestObj2Base
		{
			[PrimaryKey(MultiKeyOrder = 1)] public int Id1 { get; set; }
			public string Text { get; set; }
			public override string ToString() { return string.Format("[TestObj: Id0={0}, Id1={1}, Id2={2}, Text={3}]", Id0, Id1, Id2, Text); }
		}
		
		[Test]
		public void MultiplePK_Test2()
		{
			var db = new TestDb<TestObj2>(Path.GetTempFileName()) {Trace = false};
			
			// insert some objects
			const int I = 3, J = 4, K = 5;
			for (int k = 0; k != K; ++k)
			for (int j = 0; j != J; ++j)
			for (int i = 0; i != I; ++i)
				db.Insert(new TestObj2{Id0 = i, Id1 = j, Id2 = k, Text = string.Format("I am ({0},{1},{2})",i,j,k)});
			Assert.AreEqual(I*J*K, db.Table<TestObj2>().Count());
			
			// query for objects using Get<>()/GetOrDefault()
			var obj = db.Get<TestObj2>(1,2,3);
			Assert.AreEqual(1, obj.Id0);
			Assert.AreEqual(2, obj.Id1);
			Assert.AreEqual(3, obj.Id2);
			Assert.AreEqual("I am (1,2,3)", obj.Text);

			Assert.IsNull(db.GetOrDefault<TestObj2>(1,1,10));
			Assert.Throws(typeof(SQLiteException), ()=> db.Get<TestObj2>(1,2));
			Assert.Throws(typeof(SQLiteException), ()=> db.GetOrDefault<TestObj2>(1,2));
			
			var pks = db.GetPrimaryKeys(obj);
			var obj2 = db.Get<TestObj2>(pks);
			Assert.AreEqual(1, pks[0]);
			Assert.AreEqual(2, pks[1]);
			Assert.AreEqual(3, pks[2]);
			Assert.AreEqual(obj.Id0, obj2.Id0);
			Assert.AreEqual(obj.Id1, obj2.Id1);
			Assert.AreEqual(obj.Id2, obj2.Id2);
			Assert.AreEqual(obj.Text, obj2.Text);
			
			// check insert constraints
			try { db.Insert(obj2); }
			catch (SQLiteException ex) { Assert.AreEqual(SQLite3.Result.Constraint, ex.Result); }
			
			// updates
			obj.Text = "I've been changed";
			Assert.AreEqual(1, db.Update(obj));
			Assert.AreEqual("I've been changed", db.Get<TestObj2>(obj.Id0, obj.Id1, obj.Id2).Text);
			
			// single property update
			Assert.AreEqual(1, db.Update<TestObj2>("Text", "Changed again", obj.Id0, obj.Id1, obj.Id2));
 			Assert.AreEqual("Changed again", db.Get<TestObj2>(obj.Id0, obj.Id1, obj.Id2).Text);
			
			// delete
			Assert.AreEqual(1, db.Delete(obj));
			Assert.Throws(typeof(InvalidOperationException), ()=> db.Get<TestObj2>(obj.Id0, obj.Id1, obj.Id2));
		}
	}
}
