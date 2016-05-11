using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class DefaulAttributeTest
    {
        private class WithDefaultValue
        {
			public const string CustomAttributeDefaultValue = "12345";
            public const int IntVal = 666;
            public static decimal DecimalVal = 666.666m;
            public static string StringVal = "Working String";
            public static DateTime DateTimegVal = new DateTime(2014, 2, 13);

            public WithDefaultValue()
            {
                TestInt = IntVal;
                TestDateTime = DateTimegVal;
                TestDecimal = DecimalVal;
                TestString = StringVal;
            }
            
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }


            [Default]
            public int TestInt { get; set; }

            [Default]
            public decimal TestDecimal { get; set; }

            [Default]
            public DateTime TestDateTime { get; set; }

            [Default]
            public string TestString { get; set; }


            [Default(value: IntVal, usePropertyValue: false)]
            public int DefaultValueInAttributeTestInt { get; set; }

            public class Default666Attribute : DefaultAttribute
            {
                public Default666Attribute() :base(usePropertyValue:false, value:IntVal)
                {
                    
                }
            }

            [Default666]
            public int TestIntWithSubtypeOfDefault { get; set; }

        }

		private class TestDefaultValueAttribute : Attribute
		{
			public string DefaultValue { get; private set; }

			public TestDefaultValueAttribute(string defaultValue)
			{
				DefaultValue = defaultValue;
			}
		}

		public class TestColumnInformationProvider : IColumnInformationProvider
		{
			public string GetColumnName(PropertyInfo p)
			{
				return p.Name;
			}

			public bool IsIgnored(PropertyInfo p)
			{
				return false;
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
				var defaultValueAttributes = p.GetCustomAttributes<TestDefaultValueAttribute> ();
				if (!defaultValueAttributes.Any())
				{
					return null;
				}

				return defaultValueAttributes.First().DefaultValue;
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

		public class TestObjIntWithDefaultValue : TestObjBase<int>
		{
			[TestDefaultValue("12345")]
			public string SomeValue { get; set; }
		}

		public class TestDbWithCustomAttributes : SQLiteConnection
		{
			public TestDbWithCustomAttributes(String path)
				: base(new SQLitePlatformTest(), path)
			{
				ColumnInformationProvider = new TestColumnInformationProvider();
				CreateTable<TestObjIntWithDefaultValue>();
			}
		}

        [Test]
        public void TestColumnValues()
        {
            using (TestDb db = new TestDb())
            {
                db.CreateTable<WithDefaultValue>();
               

                string failed = string.Empty;
                foreach (var col in db.GetMapping<WithDefaultValue>().Columns)
                {
                    if (col.PropertyName == "TestInt" && !col.DefaultValue.Equals(WithDefaultValue.IntVal))
                        failed += " , TestInt does not equal " + WithDefaultValue.IntVal;


                    if (col.PropertyName == "TestDecimal" && !col.DefaultValue.Equals(WithDefaultValue.DecimalVal))
                        failed += "TestDecimal does not equal " + WithDefaultValue.DecimalVal;

                    if (col.PropertyName == "TestDateTime" && !col.DefaultValue.Equals(WithDefaultValue.DateTimegVal))
                        failed += "TestDateTime does not equal " + WithDefaultValue.DateTimegVal;

                    if (col.PropertyName == "TestString" && !col.DefaultValue.Equals(WithDefaultValue.StringVal))
                        failed += "TestString does not equal " + WithDefaultValue.StringVal;

                    if (col.PropertyName == "DefaultValueInAttributeTestInt" && !col.DefaultValue.Equals(WithDefaultValue.IntVal))
                        failed += " , DefaultValueInAttributeTestInt does not equal " + WithDefaultValue.IntVal;

                    if (col.PropertyName == "TestIntWithSubtypeOfDefault" && !col.DefaultValue.Equals(WithDefaultValue.IntVal))
                        failed += " , TestIntWithSubtypeOfDefault does not equal " + WithDefaultValue.IntVal;

                }

                Assert.True(string.IsNullOrWhiteSpace(failed), failed);

            }
        }

		[Test]
		public void TestCustomDefaultColumnValues()
		{
			using (var db = new TestDbWithCustomAttributes(TestPath.CreateTemporaryDatabase()))
			{
				string failed = string.Empty;
				foreach (var col in db.GetMapping<TestObjIntWithDefaultValue>().Columns)
				{
					if (col.PropertyName == "SomeValue" && !col.DefaultValue.Equals(WithDefaultValue.CustomAttributeDefaultValue))
						failed += " , SomeValue does not equal " + WithDefaultValue.CustomAttributeDefaultValue;
				}

				Assert.True(string.IsNullOrWhiteSpace(failed), failed);
				db.ColumnInformationProvider = null;
			}
		}
    }
}