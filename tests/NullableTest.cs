using System;
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

using System.IO;

namespace SQLite.Tests
{
	[TestFixture]
	public class NullableTest
	{
		public class NullableIntClass
		{
			[PrimaryKey, AutoIncrement]
			public int ID { get; set; }

			public Nullable<int> NullableInt { get; set; }

			public override bool Equals(object obj)
			{
				NullableIntClass other = (NullableIntClass)obj;
				return this.ID == other.ID && this.NullableInt == other.NullableInt;
			}
			
			public override int GetHashCode ()
			{
				return ID.GetHashCode () + NullableInt.GetHashCode ();
			}
		}

		[Test]
		[Description("Create a table with a nullable int column then insert and select against it")]
		public void NullableInt()
		{
			SQLiteConnection db = new SQLiteConnection(TestPath.GetTempFileName());
			db.CreateTable<NullableIntClass>();

			NullableIntClass withNull = new NullableIntClass() { NullableInt = null };
			NullableIntClass with0 = new NullableIntClass() { NullableInt = 0 };
			NullableIntClass with1 = new NullableIntClass() { NullableInt = 1 };
			NullableIntClass withMinus1 = new NullableIntClass() { NullableInt = -1 };

			db.Insert(withNull);
			db.Insert(with0);
			db.Insert(with1);
			db.Insert(withMinus1);

			NullableIntClass[] results = db.Table<NullableIntClass>().OrderBy(x => x.ID).ToArray();
			
			Assert.AreEqual(4, results.Length);

			Assert.AreEqual(withNull, results[0]);
			Assert.AreEqual(with0, results[1]);
			Assert.AreEqual(with1, results[2]);
			Assert.AreEqual(withMinus1, results[3]);
		}


		public class NullableFloatClass
		{
			[PrimaryKey, AutoIncrement]
			public int ID { get; set; }

			public Nullable<float> NullableFloat { get; set; }

			public override bool Equals(object obj)
			{
				NullableFloatClass other = (NullableFloatClass)obj;
				return this.ID == other.ID && this.NullableFloat == other.NullableFloat;
			}
			
			public override int GetHashCode ()
			{
				return ID.GetHashCode () + NullableFloat.GetHashCode ();
			}
		}

		[Test]
		[Description("Create a table with a nullable int column then insert and select against it")]
		public void NullableFloat()
		{
			SQLiteConnection db = new SQLiteConnection(TestPath.GetTempFileName());
			db.CreateTable<NullableFloatClass>();

			NullableFloatClass withNull = new NullableFloatClass() { NullableFloat = null };
			NullableFloatClass with0 = new NullableFloatClass() { NullableFloat = 0 };
			NullableFloatClass with1 = new NullableFloatClass() { NullableFloat = 1 };
			NullableFloatClass withMinus1 = new NullableFloatClass() { NullableFloat = -1 };

			db.Insert(withNull);
			db.Insert(with0);
			db.Insert(with1);
			db.Insert(withMinus1);

			NullableFloatClass[] results = db.Table<NullableFloatClass>().OrderBy(x => x.ID).ToArray();

			Assert.AreEqual(4, results.Length);

			Assert.AreEqual(withNull, results[0]);
			Assert.AreEqual(with0, results[1]);
			Assert.AreEqual(with1, results[2]);
			Assert.AreEqual(withMinus1, results[3]);
		}



		public class StringClass
		{
			[PrimaryKey, AutoIncrement]
			public int ID { get; set; }

			//Strings are allowed to be null by default
			public string StringData { get; set; }

			public override bool Equals(object obj)
			{
				StringClass other = (StringClass)obj;
				return this.ID == other.ID && this.StringData == other.StringData;
			}
			
			public override int GetHashCode ()
			{
				return ID.GetHashCode () + StringData.GetHashCode ();
			}
		}

		[Test]
		public void NullableString()
		{
			SQLiteConnection db = new SQLiteConnection(TestPath.GetTempFileName());
			db.CreateTable<StringClass>();

			StringClass withNull = new StringClass() { StringData = null };
			StringClass withEmpty = new StringClass() { StringData = "" };
			StringClass withData = new StringClass() { StringData = "data" };

			db.Insert(withNull);
			db.Insert(withEmpty);
			db.Insert(withData);

			StringClass[] results = db.Table<StringClass>().OrderBy(x => x.ID).ToArray();

			Assert.AreEqual(3, results.Length);

			Assert.AreEqual(withNull, results[0]);
			Assert.AreEqual(withEmpty, results[1]);
			Assert.AreEqual(withData, results[2]);
		}

		[Test]
		public void WhereNotNull()
		{
			SQLiteConnection db = new SQLiteConnection(TestPath.GetTempFileName());
			db.CreateTable<NullableIntClass>();

			NullableIntClass withNull = new NullableIntClass() { NullableInt = null };
			NullableIntClass with0 = new NullableIntClass() { NullableInt = 0 };
			NullableIntClass with1 = new NullableIntClass() { NullableInt = 1 };
			NullableIntClass withMinus1 = new NullableIntClass() { NullableInt = -1 };

			db.Insert(withNull);
			db.Insert(with0);
			db.Insert(with1);
			db.Insert(withMinus1);

			NullableIntClass[] results = db.Table<NullableIntClass>().Where(x => x.NullableInt != null).OrderBy(x => x.ID).ToArray();

			Assert.AreEqual(3, results.Length);

			Assert.AreEqual(with0, results[0]);
			Assert.AreEqual(with1, results[1]);
			Assert.AreEqual(withMinus1, results[2]);
		}

		[Test]
		public void WhereNull()
		{
			SQLiteConnection db = new SQLiteConnection(TestPath.GetTempFileName());
			db.CreateTable<NullableIntClass>();

			NullableIntClass withNull = new NullableIntClass() { NullableInt = null };
			NullableIntClass with0 = new NullableIntClass() { NullableInt = 0 };
			NullableIntClass with1 = new NullableIntClass() { NullableInt = 1 };
			NullableIntClass withMinus1 = new NullableIntClass() { NullableInt = -1 };

			db.Insert(withNull);
			db.Insert(with0);
			db.Insert(with1);
			db.Insert(withMinus1);

			NullableIntClass[] results = db.Table<NullableIntClass>().Where(x => x.NullableInt == null).OrderBy(x => x.ID).ToArray();

			Assert.AreEqual(1, results.Length);
			Assert.AreEqual(withNull, results[0]);
		}

		[Test]
		public void StringWhereNull()
		{
			SQLiteConnection db = new SQLiteConnection(TestPath.GetTempFileName());
			db.CreateTable<StringClass>();

			StringClass withNull = new StringClass() { StringData = null };
			StringClass withEmpty = new StringClass() { StringData = "" };
			StringClass withData = new StringClass() { StringData = "data" };

			db.Insert(withNull);
			db.Insert(withEmpty);
			db.Insert(withData);

			StringClass[] results = db.Table<StringClass>().Where(x => x.StringData == null).OrderBy(x => x.ID).ToArray();
			Assert.AreEqual(1, results.Length);
			Assert.AreEqual(withNull, results[0]);
		}

		[Test]
		public void StringWhereNotNull()
		{
			SQLiteConnection db = new SQLiteConnection(TestPath.GetTempFileName());
			db.CreateTable<StringClass>();

			StringClass withNull = new StringClass() { StringData = null };
			StringClass withEmpty = new StringClass() { StringData = "" };
			StringClass withData = new StringClass() { StringData = "data" };

			db.Insert(withNull);
			db.Insert(withEmpty);
			db.Insert(withData);

			StringClass[] results = db.Table<StringClass>().Where(x => x.StringData != null).OrderBy(x => x.ID).ToArray();
			Assert.AreEqual(2, results.Length);
			Assert.AreEqual(withEmpty, results[0]);
			Assert.AreEqual(withData, results[1]);
		}
	}
}
