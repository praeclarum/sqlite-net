using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

using System.Diagnostics;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif


namespace SQLite.Tests
{    
    [TestFixture]
    public class BooleanTest
    {
        public class VO
        {
            [AutoIncrement, PrimaryKey]
            public int ID { get; set; }
            public bool Flag { get; set; }
            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("VO:: ID:{0} Flag:{1} Text:{2}", ID, Flag, Text);
            }
        }
        public class DbAcs : SQLiteConnection
        {
            public DbAcs(String path)
                : base(path)
            {
            }

            public void buildTable()
            {
                CreateTable<VO>();
            }

            public int CountWithFlag(Boolean flag)
            {
                var cmd = CreateCommand("SELECT COUNT(*) FROM VO Where Flag = ?", flag);
                return cmd.ExecuteScalar<int>();                
            }
        }
        
        [Test]
        public void TestBoolean()
        {
            var tmpFile = TestPath.GetTempFileName();
            var db = new DbAcs(tmpFile);         
            db.buildTable();
            for (int i = 0; i < 10; i++)
                db.Insert(new VO() { Flag = (i % 3 == 0), Text = String.Format("VO{0}", i) });                
            
            // count vo which flag is true            
            Assert.AreEqual(4, db.CountWithFlag(true));
            Assert.AreEqual(6, db.CountWithFlag(false));

            Debug.WriteLine("VO with true flag:");
            foreach (var vo in db.Query<VO>("SELECT * FROM VO Where Flag = ?", true))
				Debug.WriteLine (vo.ToString ());

			Debug.WriteLine ("VO with false flag:");
            foreach (var vo in db.Query<VO>("SELECT * FROM VO Where Flag = ?", false))
				Debug.WriteLine (vo.ToString ());
        }
    }
}
