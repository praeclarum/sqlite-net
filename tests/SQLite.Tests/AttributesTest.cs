using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

using System.Diagnostics;

namespace SQLite.Tests
{
	[TestFixture]
	public class AttributesTest
	{
		public AttributesTest ()
		{
		}

		[Test]
		public void TestCtors ()
		{
			Assert.DoesNotThrow (() => new CollationAttribute ("NOCASE"));
			Assert.DoesNotThrow (() => new ColumnAttribute ("Bar"));
			Assert.DoesNotThrow (() => new IgnoreAttribute ());
			Assert.DoesNotThrow (() => new IndexedAttribute ());
			Assert.DoesNotThrow (() => new NotNullAttribute ());
			Assert.DoesNotThrow (() => new PreserveAttribute ());
			Assert.DoesNotThrow (() => new PrimaryKeyAttribute ());
			Assert.DoesNotThrow (() => new StoreAsTextAttribute ());
			Assert.DoesNotThrow (() => new TableAttribute ("Foo"));
			Assert.DoesNotThrow (() => new UniqueAttribute ());
		}
	}
}
