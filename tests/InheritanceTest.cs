using System;
using System.Linq;

#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
#if NETFX_CORE
    [TestClass]
#else
    [TestFixture]
#endif
    public class InheritanceTest
	{
		class Base
		{
			[PrimaryKey]
			public int Id { get; set; }
			
			public string BaseProp { get; set; }
		}
		
		class Derived : Base
		{
			public string DerivedProp { get; set; }
		}

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        public void InheritanceWorks()
		{
			var db = new TestDb ();
			
			var mapping = db.GetMapping<Derived> ();
			
			Assert.AreEqual (3, mapping.Columns.Length);
			Assert.AreEqual ("Id", mapping.PK.Name);
		}
	}
}
