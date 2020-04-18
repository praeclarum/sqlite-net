using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
    public class EnumCacheTests
    {
        [StoreAsText]
        public enum TestEnumStoreAsText
        {
            Value1,

            Value2,

            Value3
        }

        public enum TestEnumStoreAsInt
        {
            Value1,

            Value2,

            Value3
        }

        public enum TestByteEnumStoreAsInt : byte
        {
            Value1,

            Value2,

            Value3
        }

		public enum TestEnumWithRepeats
		{
			Value1 = 1,

			Value2 = 2,

			Value2Again = 2,

			Value3 = 3,
		}

		[StoreAsText]
		public enum TestEnumWithRepeatsAsText
		{
			Value1 = 1,

			Value2 = 2,

			Value2Again = 2,

			Value3 = 3,
		}

		public class TestClassThusNotEnum
        {

        }

        [Test]
        public void ShouldReturnTrueForEnumStoreAsText()
        {
            var info = EnumCache.GetInfo<TestEnumStoreAsText>();

            Assert.IsTrue(info.IsEnum);
            Assert.IsTrue(info.StoreAsText);
            Assert.IsNotNull(info.EnumValues);

            var values = Enum.GetValues(typeof(TestEnumStoreAsText)).Cast<object>().ToList();

            for (int i = 0; i < values.Count; i++)
            {
                Assert.AreEqual(values[i].ToString(), info.EnumValues[i]);
            }
        }

        [Test]
        public void ShouldReturnTrueForEnumStoreAsInt()
        {
            var info = EnumCache.GetInfo<TestEnumStoreAsInt>();

            Assert.IsTrue(info.IsEnum);
            Assert.IsFalse(info.StoreAsText);
            Assert.IsNull(info.EnumValues);
        }

        [Test]
        public void ShouldReturnTrueForByteEnumStoreAsInt()
        {
            var info = EnumCache.GetInfo<TestByteEnumStoreAsInt>();

            Assert.IsTrue(info.IsEnum);
            Assert.IsFalse(info.StoreAsText);
        }

        [Test]
        public void ShouldReturnFalseForClass()
        {
            var info = EnumCache.GetInfo<TestClassThusNotEnum>();

            Assert.IsFalse(info.IsEnum);
            Assert.IsFalse(info.StoreAsText);
            Assert.IsNull(info.EnumValues);
        }

		[Test]
		public void Issue598_EnumsWithRepeatedValues ()
		{
			var info = EnumCache.GetInfo<TestEnumWithRepeats> ();

			Assert.IsTrue (info.IsEnum);
			Assert.IsFalse (info.StoreAsText);
			Assert.IsNull (info.EnumValues);
		}

		[Test]
		public void Issue598_EnumsWithRepeatedValuesAsText ()
		{
			var info = EnumCache.GetInfo<TestEnumWithRepeatsAsText> ();

			Assert.IsTrue (info.IsEnum);
			Assert.IsTrue (info.StoreAsText);
			Assert.IsNotNull (info.EnumValues);
		}
	}
}
