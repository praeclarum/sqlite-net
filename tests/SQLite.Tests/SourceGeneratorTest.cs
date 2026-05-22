using System;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Newtonsoft;
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

#pragma warning disable CS0618 // Disable obsolete Warnings
#pragma warning disable CS0612 // Disable obsolete Warnings

namespace SQLite.Tests
{
	[AttributeUsage (AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	class DerivedIgnoreAttribute : IgnoreAttribute
	{
	}

	public class OuterTestSetter
	{
		[AutoIncrement, PrimaryKey]
		public int Id { get; set; }

		public string Data { get; set; }

		public DateTime Date { get; set; }

		public string NotWritable { get; }

		[Ignore]
		public string Ignore { get; set; }

		[DerivedIgnore]
		public string DerivedIgnore { get; set; }

		[Column("A")]
		public string Z { get; set; }

		private string Private { get; set; }
		public static string StaticProperty {get; set; }

		[Obsolete]
		public string Obsolete { get; set; }

		public string PrivateSet { get; private set; }

		public string Init { get; init; }
	}

	public class OuterTestDb : SQLiteConnection
	{
		public OuterTestDb (String path)
			: base (path)
		{
			CreateTable<OuterTestSetter> ();
		}
	}

	[TestFixture]
	public class SourceGeneratorTest
	{
		[Table("Test")]
		public class StringTest : BaseTest<string>
		{
		}

		public class IntTest : BaseTest<int>
		{
		}


		public partial class PartialTest
		{
			[PrimaryKey]
			public string Id { get; set; }
		}

		public partial class PartialTest
		{
			[Column ("Test")]
			public string Test { get; set; }
		}

		public class BaseTest<T>
		{
			[PrimaryKey]
			public T Id { get; set; }
		}

		public class InnerTestSetter
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }

			public string Data { get; set; }

			public DateTime Date { get; set; }
		}

        public class AllBasicTypesSetter
        {
            [PrimaryKey] 
            public int Id { get; set; }

            public string String { get; set; }

            public byte Byte { get; set; }

            public short Short { get; set; }

            public int Int { get; set; }

            public long Long { get; set; }

            public float Float { get; set; }

            public double Double { get; set; }

            public decimal Decimal { get; set; }

            public TimeSpan TimeSpam { get; set; }

            public DateTime DateTime { get; set; }

            public Guid Guid { get; set; }
        }


        public class AllBasicTypesSetterNullable
        {
	        [PrimaryKey]
	        public int Id { get; set; }

	        public string String { get; set; }

	        public byte? Byte { get; set; }

	        public short? Short { get; set; }

	        public int? Int { get; set; }

	        public long? Long { get; set; }

	        public float? Float { get; set; }

	        public double? Double { get; set; }

	        public decimal? Decimal { get; set; }

	        public TimeSpan? TimeSpam { get; set; }

	        public DateTime? DateTime { get; set; }

	        public Guid? Guid { get; set; }
        }

// Shouldn't generate Setter because it is not accessible
		private class PrivateInnerTestSetter
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }

			public string Data { get; set; }

			public DateTime Date { get; set; }
		}

        public class AllBasicTypesTestDb : SQLiteConnection
        {
            public AllBasicTypesTestDb (String path)
                : base (path)
            {
                CreateTable<AllBasicTypesSetter> ();
            }
        }

        public class AllBasicTypesNullableTestDb : SQLiteConnection
        {
	        public AllBasicTypesNullableTestDb (String path)
		        : base (path)
	        {
		        CreateTable<AllBasicTypesSetterNullable> ();
	        }
        }

		public class InnerTestDb : SQLiteConnection
		{
			public InnerTestDb (String path)
				: base (path)
			{
				CreateTable<InnerTestSetter> ();
			}
		}

		[Test]
		public void SqliteInitializer_PrivateInnerTestSetter ()
		{
			if (!SQLite.FastColumnSetter.customSetter.TryGetValue((typeof(PrivateInnerTestSetter), nameof(PrivateInnerTestSetter.Id)), out var setter))
			{
				Assert.IsTrue(true, "Should not be registered");
			}
			else
			{
				Assert.Fail("Should not be registered");
			}
		}

		[Test]
		public void SqliteInitializer_StringTestSetter ()
		{
			if (SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (StringTest), nameof (StringTest.Id)), out var setter)) {
				Assert.IsTrue (true, "Should be registered");
			}
			else {
				Assert.Fail ("Should be registered");
			}
		}

		[Test]
		public void SqliteInitializer_PartialTestSetter ()
		{
			if (SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (PartialTest), nameof (PartialTest.Id)), out var setter)) {
				Assert.IsTrue (true, "Should be registered");
			}
			else {
				Assert.Fail ("Should be registered");
			}
		}

		[Test]
		public void SqliteInitializer_PartialTestSetter_Test()
		{
			if (SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (PartialTest), nameof (PartialTest.Test)), out var setter)) {
				Assert.IsTrue (true, "Should be registered");
			}
			else {
				Assert.Fail ("Should be registered");
			}
		}

		[Test]
		public void SqliteInitializer_IntTestSetter ()
		{
			if (SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (IntTest), nameof (IntTest.Id)), out var setter)) {
				Assert.IsTrue (true, "Should be registered");
			}
			else {
				Assert.Fail ("Should be registered");
			}
		}

		[Test]
		public void SqliteInitializer_InnerTestSetter ()
		{
			if (SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (InnerTestSetter), nameof (InnerTestSetter.Id)), out var setter)) {
				Assert.IsTrue (true, "Should be registered");
			}
			else {
				Assert.Fail ("Should be registered");
			}
		}


		[Test]
		public void SqliteInitializer_OuterTestSetter_ZRenamedA()
		{
			if (SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (OuterTestSetter), "A"), out var setter)) {
				Assert.IsTrue (true, "Should be registered");
			}
			else {
				Assert.Fail ("Should be registered");
			}
		}

		[Test]
		public void SqliteInitializer_OuterTestSetter_Obsolete_Property()
		{
			if (SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (OuterTestSetter), nameof(OuterTestSetter.Obsolete)), out var setter)) {
				Assert.IsTrue (true, "Should be registered");
			}
			else {
				Assert.Fail ("Should be registered");
			}
		}

		[Test]
		public void SqliteInitializer_OuterTestSetter_NotWritable_NotRegistered()
		{
			if (!SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (OuterTestSetter), nameof (OuterTestSetter.NotWritable)), out var setter)) {
				Assert.IsTrue (true, "Should not be registered (not writable)");
			}
			else {
				Assert.Fail ("Should not be registered (not writable)");
			}
		}

		[Test]
		public void SqliteInitializer_OuterTestSetter_Ignore_NotRegistered ()
		{
			if (!SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (OuterTestSetter), nameof (OuterTestSetter.Ignore)), out var setter)) {
				Assert.IsTrue (true, "Should not be registered (Ignore)");
			}
			else {
				Assert.Fail ("Should not be registered (Ignore)");
			}
		}

		[Test]
		public void SqliteInitializer_OuterTestSetter_DeriveIgnore_NotRegistered ()
		{
			if (!SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (OuterTestSetter), nameof (OuterTestSetter.DerivedIgnore)), out var setter)) {
				Assert.IsTrue (true, "Should not be registered (Ignore)");
			}
			else {
				Assert.Fail ("Should not be registered (Ignore)");
			}
		}

		[Test]
		public void SqliteInitializer_OuterTestSetter_Private_NotRegistered ()
		{
			if (!SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (OuterTestSetter), "Private"), out var setter)) {
				Assert.IsTrue (true, "Private properties Should not be registered");
			}
			else {
				Assert.Fail ("Private properties Should not be registered");
			}
		}

		[Test]
		public void SqliteInitializer_OuterTestSetter_StaticProperty_NotRegistered ()
		{
			if (!SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (OuterTestSetter), nameof(OuterTestSetter.StaticProperty)), out var setter)) {
				Assert.IsTrue (true, "Static properties Should not be registered");
			}
			else {
				Assert.Fail ("Static properties Should not be registered");
			}
		}

		[Test]
		public void SqliteInitializer_OuterTestSetter_PrivateSet_NotRegistered ()
		{
			if (!SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (OuterTestSetter), nameof (OuterTestSetter.PrivateSet)), out var setter)) {
				Assert.IsTrue (true, "Private Set properties Should not be registered");
			}
			else {
				Assert.Fail ("Private Set properties Should not be registered");
			}
		}

		[Test]
		public void SqliteInitializer_OuterTestSetter_Init_NotRegistered ()
		{
			if (!SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (OuterTestSetter), nameof (OuterTestSetter.Init)), out var setter)) {
				Assert.IsTrue (true, "Init properties Should not be registered");
			}
			else {
				Assert.Fail ("Init properties Should not be registered");
			}
		}

		[Test]
		public void SqliteInitializer_OuterTestSetter ()
		{
			if (SQLite.FastColumnSetter.customSetter.TryGetValue ((typeof (OuterTestSetter), nameof (OuterTestSetter.Id)), out var setter)) {
				Assert.IsTrue(true, "Should not be registered");
			}
			else {
				Assert.Fail ("Should not be registered");
			}
		}

		[Test]
		public void SqliteInitializer_Inner_AndReadData()
		{
            var mapperCount = FastColumnSetter.customSetter.Count;

			var n = 20;
			var cq = from i in Enumerable.Range (1, n)
					 select new InnerTestSetter {
						 Data = Convert.ToString (i),
						 Date = new DateTime (2013, 1, i)
					 };

			var db = new InnerTestDb (TestPath.GetTempFileName ());
			db.InsertAll (cq);

			var results = db.Table<InnerTestSetter> ().Where (o => o.Data.Equals ("10"));
			Assert.AreEqual (results.Count (), 1);
			Assert.AreEqual (results.FirstOrDefault ().Data, "10");
            Assert.AreEqual (mapperCount, FastColumnSetter.customSetter.Count);
		}

		[Test]
		public void SetFastColumnSetters_Inner_AndReadData_IsCalled()
		{
			
            var mapperCount = FastColumnSetter.customSetter.Count;

			var n = 20;
			var cq = from i in Enumerable.Range (1, n)
				select new InnerTestSetter {
					Data = Convert.ToString (i),
					Date = new DateTime (2013, 1, i)
				};

			var db = new InnerTestDb (TestPath.GetTempFileName ());
			db.InsertAll (cq);

			var results = db.Table<InnerTestSetter> ().Where (o => o.Data.Equals ("10"));
			Assert.AreEqual (results.Count (), 1);
			Assert.AreEqual (results.FirstOrDefault ().Data, "10");
            Assert.AreEqual (mapperCount, FastColumnSetter.customSetter.Count);
		}

		[Test]
		public void SqliteInitializer_Outer_AndReadData ()
		{
			
            var mapperCount = FastColumnSetter.customSetter.Count;

			var n = 20;
			var cq = from i in Enumerable.Range (1, n)
				select new OuterTestSetter() {
					Data = Convert.ToString (i),
					Date = new DateTime (2013, 1, i)
				};

			var db = new OuterTestDb(TestPath.GetTempFileName ());
			db.InsertAll (cq);

			var results = db.Table<OuterTestSetter> ().Where (o => o.Data.Equals ("10"));
			Assert.AreEqual (results.Count (), 1);
			Assert.AreEqual (results.FirstOrDefault ().Data, "10");
            Assert.AreEqual (mapperCount, FastColumnSetter.customSetter.Count);
		}

		[Test]
		public void SqliteInitializer_Outer_AndReadData_ZRenamedA()
		{
			
            var mapperCount = FastColumnSetter.customSetter.Count;

			var n = 20;
			var cq = from i in Enumerable.Range (1, n)
				select new OuterTestSetter () {
					Data = Convert.ToString (i),
					Date = new DateTime (2013, 1, i),
					Z = Convert.ToString(i),
				};

			var db = new OuterTestDb (TestPath.GetTempFileName ());
			db.InsertAll (cq);

			var results = db.Table<OuterTestSetter> ().Where (o => o.Z.Equals ("10"));
			Assert.AreEqual (results.Count (), 1);
			Assert.AreEqual (results.FirstOrDefault ().Z, "10");
			Assert.AreEqual(mapperCount, FastColumnSetter.customSetter.Count);
		}

		[Test]
		public void SetFastColumnSetters_Outer_AndReadData_IsCalled ()
		{
			
            var mapperCount = FastColumnSetter.customSetter.Count;

			var n = 20;
			var cq = from i in Enumerable.Range (1, n)
				select new OuterTestSetter {
					Data = Convert.ToString (i),
					Date = new DateTime (2013, 1, i)
				};

			var db = new OuterTestDb (TestPath.GetTempFileName ());
			db.InsertAll (cq);

			var results = db.Table<OuterTestSetter> ().Where (o => o.Data.Equals ("10"));
			Assert.AreEqual (results.Count (), 1);
			Assert.AreEqual (results.FirstOrDefault ().Data, "10");
            Assert.AreEqual (mapperCount, FastColumnSetter.customSetter.Count);
		}

        [Test]
        public void SetFastColumnSetters_AllBasicTypes_Works ()
        {
            
            var mapperCount = FastColumnSetter.customSetter.Count;

            var n = 20;
            var cq = from i in Enumerable.Range (1, n)
                select new AllBasicTypesSetter() {
                    Id = i,
					String = Convert.ToString(i),
					Byte = (byte)i,
					Short = (short)i,
					Int = i,
					Long = i,
					Float = i,
					Double = i,
					Decimal = i,
					DateTime = new DateTime(2000, 1, i),
					TimeSpam = new TimeSpan(i, 0, 0),
					Guid = new Guid (i, 0, 0, new byte[8]),
                };

            var db = new AllBasicTypesTestDb(TestPath.GetTempFileName ());
            db.InsertAll (cq);

            var results = db.Table<AllBasicTypesSetter> ().Where (o => o.Id.Equals (10));
            Assert.AreEqual (results.Count (), 1);
            var data = results.FirstOrDefault ();
            Assert.AreEqual (data.String, "10");
            Assert.AreEqual (data.Byte, (byte)10);
            Assert.AreEqual (data.Short, (short)10);
            Assert.AreEqual (data.Int, (int)10);
            Assert.AreEqual (data.Long, (long)10);
            Assert.AreEqual (data.Float, (float)10);
            Assert.AreEqual (data.Double, (double)10);
            Assert.AreEqual (data.Decimal, (decimal)10);
            Assert.AreEqual (data.TimeSpam, new TimeSpan(10, 0, 0));
            Assert.AreEqual (data.DateTime, new DateTime(2000, 1, 10));
            Assert.AreEqual (data.Guid, new Guid (10, 0, 0, new byte[8]));

			Assert.AreEqual (mapperCount, FastColumnSetter.customSetter.Count);
        }

        [Test]
        public void SetFastColumnSetters_AllBasicTypesNullable_Works ()
        {

	        var mapperCount = FastColumnSetter.customSetter.Count;

	        var n = 20;
	        var cq = from i in Enumerable.Range (1, n)
		        select new AllBasicTypesSetterNullable() {
			        Id = i,
			        String = null,
			        Byte = null,
			        Short = null,
			        Int = null,
			        Long = null,
			        Float = null,
			        Double = null,
			        Decimal = null,
			        DateTime = null,
			        TimeSpam = null,
			        Guid = null,
		        };

	        var db = new AllBasicTypesNullableTestDb (TestPath.GetTempFileName ());
	        db.InsertAll (cq);

	        var results = db.Table<AllBasicTypesSetterNullable> ().Where (o => o.Id.Equals (10));
	        Assert.AreEqual (results.Count (), 1);
	        var data = results.FirstOrDefault ();
	        Assert.AreEqual (data.String, null);
	        Assert.AreEqual (data.Byte, null);
	        Assert.AreEqual (data.Short, null);
	        Assert.AreEqual (data.Int, null);
	        Assert.AreEqual (data.Long, null);
	        Assert.AreEqual (data.Float, null);
	        Assert.AreEqual (data.Double, null);
	        Assert.AreEqual (data.Decimal, null);
	        Assert.AreEqual (data.TimeSpam, null);
	        Assert.AreEqual (data.DateTime, null);
	        Assert.AreEqual (data.Guid, null);

	        Assert.AreEqual (mapperCount, FastColumnSetter.customSetter.Count);
        }
	}
}
