
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

using NUnit.Framework;

namespace SQLite.Tests
{    
    [TestFixture]
    public class InsertTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
            public String Text { get; set; }

            public override string ToString ()
            {
            	return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }

        }
        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(path)
            {
				CreateTable<TestObj>();
            }

        }
		
        [Test]
        public void InsertALot()
        {
			int n = 500;
			var q =	from i in Enumerable.Range(1, n)
					select new TestObj() {
				Text = "I am"
			};
			var objs = q.ToArray();
			var db = new TestDb(Path.GetTempFileName());
			db.Trace = true;
			
			var numIn = db.InsertAll(objs);
			
			Assert.AreEqual(numIn, n, "Num inserted must = num objects");
			
			var inObjs = db.CreateCommand("select * from TestObj").ExecuteQuery<TestObj>().ToArray();
			
			for (var i = 0; i < inObjs.Length; i++) {
				Assert.AreEqual(i+1, objs[i].Id);
				Assert.AreEqual(i+1, inObjs[i].Id);
				Assert.AreEqual("I am", inObjs[i].Text);
			}
			
			var numCount = db.CreateCommand("select count(*) from TestObj").ExecuteScalar<int>();
			
			Assert.AreEqual(numCount, n, "Num counted must = num objects");
        }
    }
}
