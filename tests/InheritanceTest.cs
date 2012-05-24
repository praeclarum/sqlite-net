using System;
using System.Linq;

#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SetUp = Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#else
using NUnit.Framework;
#endif


namespace SQLite.Tests
{
	[TestFixture]
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
		
		
		[Test]
		public void InheritanceWorks ()
		{
			var db = new TestDb ();
			
			var mapping = db.GetMapping<Derived> ();
			
			Assert.AreEqual (3, mapping.Columns.Length);
			Assert.AreEqual ("Id", mapping.PK.Name);
		}
	}
}
