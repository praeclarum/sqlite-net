using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

#if __WIN32__
using SQLitePlatformTest = SQLite.Net.Platform.Win32.SQLitePlatformWin32;
#elif WINDOWS_PHONE
using SQLitePlatformTest = SQLite.Net.Platform.WindowsPhone8.SQLitePlatformWP8;
#elif __WINRT__
using SQLitePlatformTest = SQLite.Net.Platform.WinRT.SQLitePlatformWinRT;
#elif __IOS__
using SQLitePlatformTest = SQLite.Net.Platform.XamarinIOS.SQLitePlatformIOS;
#elif __ANDROID__
using SQLitePlatformTest = SQLite.Net.Platform.XamarinAndroid.SQLitePlatformAndroid;
#else
using SQLitePlatformTest = SQLite.Net.Platform.Generic.SQLitePlatformGeneric;
#endif

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class DefaulAttributeTest
    {
        private class WithDefaultValue
        {

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
    }
}