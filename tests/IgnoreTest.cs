using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLite.Net.Attributes;
using System;
using System.Reflection;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class IgnoredTest
    {
        public class DummyClass
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Foo { get; set; }
            public string Bar { get; set; }

            [Attributes.Ignore]
            public List<string> Ignored { get; set; }
        }

		private class TestIgnoreAttribute : Attribute
		{
		}

		public class TestColumnInformationProvider : IColumnInformationProvider
		{
			public string GetColumnName(PropertyInfo p)
			{
				return p.Name;
			}

			public bool IsIgnored(PropertyInfo p)
			{
				return p.IsDefined(typeof (TestIgnoreAttribute), true);
			}

			public IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
			{
				return p.GetCustomAttributes<IndexedAttribute>();
			}

			public bool IsPK(MemberInfo m)
			{
				return m.GetCustomAttributes<PrimaryKeyAttribute>().Any();
			}
			public string Collation(MemberInfo m)
			{
				return string.Empty;
			}
			public bool IsAutoInc(MemberInfo m)
			{
				return false;
			}
			public int? MaxStringLength(PropertyInfo p)
			{
				return null;
			}
			public object GetDefaultValue(PropertyInfo p)
			{
				return null;
			}
			public bool IsMarkedNotNull(MemberInfo p)
			{
				return false;
			}
		}

		public abstract class TestObjBase<T>
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }

			public T Data { get; set; }

		}

		public class TestObjIntWithIgnore : TestObjBase<int>
		{
			[TestIgnore]
			public List<string> Ignored { get; set; }
		}

        [Test]
        public void NullableFloat()
        {
            var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
            // if the Ignored property is not ignore this will cause an exception
            db.CreateTable<DummyClass>();
        }

		[Test]
		public void CustomIgnoreAttributeTest()
		{
			var db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase());
			db.ColumnInformationProvider = new TestColumnInformationProvider();
			// if the Ignored property is not ignore this will cause an exception
			db.CreateTable<TestObjIntWithIgnore>();
			db.ColumnInformationProvider = null;
		}
    }
}