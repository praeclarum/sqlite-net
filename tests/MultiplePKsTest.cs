using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using System.Diagnostics;

namespace SQLite.Tests
{
	[TestFixture]
	public class MultiplePKs
	{
		[PrimaryKeyNames("Id", "SubId")]
		private partial class TestObj
		{
			public int Id { get; set; }
			public String Text { get; set; }
			public override string ToString() { return string.Format("[TestObj: Id={0}, SubId={1}, Text={2}]", Id, SubId, Text); }
		}
		private partial class TestObj 
		{
			public int SubId { get; set; }
		}

		private class TestDb : SQLiteConnection
		{
			public TestDb(String path)
				: base(path)
			{
				CreateTable<TestObj>();
			}
		}
		
		[Test]
		public void MultiplePKOperations()
		{
			var db = new TestDb(Path.GetTempFileName()) {Trace = false};
			
			// insert
			const int N = 10, M = 5;
			TestObj[] objs = new TestObj[N*M];
			for (int j = 0; j != N; ++j)
			for (int i = 0; i != M; ++i)
				objs[j*M + i] = new TestObj{Id = j, SubId = i, Text = "I am ("+j+","+i+")"};

			var sw = new Stopwatch();
			sw.Start();
			
			var numIn = db.InsertAll(objs);
			
			sw.Stop();
			
			Assert.AreEqual(numIn, N*M, "Num inserted must = num objects");
			
			var obj = db.Get<TestObj>(5,3);
			Assert.AreEqual(5, obj.Id);
			Assert.AreEqual(3, obj.SubId);
			Assert.AreEqual("I am (5,3)", obj.Text);

			try {
				db.Insert(obj);
			}
			catch (SQLiteException ex) {
				Assert.AreEqual(SQLite3.Result.Constraint, ex.Result);
			}
			
			// update
			obj.Text = "I've been changed";
			db.Update(obj);
			db.Update<TestObj>("Text", "I've been changed also", 8, 2);
			
			obj = db.Get<TestObj>(5,3);
			Assert.AreEqual("I've been changed", obj.Text);
			
			obj = db.Get<TestObj>(8,2);
			Assert.AreEqual("I've been changed also", obj.Text);

			db.UpdateAll<TestObj>("Text", "All changed");
			var q1 = from o in db.Table<TestObj>() select o;
			foreach (var o in q1)
				Assert.AreEqual("All changed", o.Text);

			var q2 = (from o in db.Table<TestObj>() where o.SubId == 3 select o).ToArray();
			Assert.AreEqual(10, q2.Length);
			for (int i = 0; i != 10; ++i) {
				Assert.AreEqual(i, q2[i].Id);
			}
			
			var numCount = db.CreateCommand("select count(*) from TestObj").ExecuteScalar<int>();
			Assert.AreEqual(numCount, objs.Length, "Num counted must = num objects");
			
			// delete
			obj = db.Get<TestObj>(8,2);
			db.Delete(obj);
			Assert.Throws(typeof(InvalidOperationException), ()=> db.Get<TestObj>(8,2));
			
			db.CreateCommand("delete from TestObj where SubId=2").ExecuteNonQuery();
			numCount = db.CreateCommand("select count(*) from TestObj").ExecuteScalar<int>();
			Assert.AreEqual(numCount, objs.Length - 10);
			foreach (var o in (from o in db.Table<TestObj>() select o))
				Assert.AreNotEqual(2, o.SubId);
		}
	}
}
