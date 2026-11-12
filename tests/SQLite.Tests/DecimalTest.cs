using System;
using System.Linq;
using System.Globalization;
using SQLite;

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
	public class DecimalTest
	{
		class DecimalSample
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }
			public decimal Amount { get; set; }
			public decimal? NullableAmount { get; set; }
		}

		TestDb _db;

		[SetUp]
		public void SetUp ()
		{
			_db = new TestDb ();
			_db.CreateTable<DecimalSample> ();
		}

		[TestCase ("0")]
		[TestCase ("0.1")]
		[TestCase ("0.7")]
		[TestCase ("1.23")]
		[TestCase ("2.675")]
		[TestCase ("19.99")]
		[TestCase ("1234.56")]
		[TestCase ("-1234.56")]
		[TestCase ("0.00001")]
		[TestCase ("123456789.12345")]
		[TestCase ("999999999999.99")]
		public void DecimalIsReadAsStored (string text)
		{
			var stored = decimal.Parse (text, CultureInfo.InvariantCulture);
			_db.Insert (new DecimalSample { Amount = stored, NullableAmount = stored });

			var read = _db.Table<DecimalSample> ().Single ();

			Assert.AreEqual (stored, read.Amount);
			Assert.AreEqual (stored, read.NullableAmount);
			Assert.AreEqual (stored, _db.ExecuteScalar<decimal> ("select Amount from DecimalSample"));
			Assert.AreEqual (stored.ToString (CultureInfo.InvariantCulture), read.Amount.ToString (CultureInfo.InvariantCulture));
		}

		[Test]
		public void NullDecimalIsReadAsNull ()
		{
			_db.Insert (new DecimalSample { Amount = 1m });

			Assert.IsNull (_db.Table<DecimalSample> ().Single ().NullableAmount);
		}

		[Test]
		public void SumOfDecimalsHasNoFloatingPointNoise ()
		{
			_db.Insert (new DecimalSample { Amount = 19.99m });
			_db.Insert (new DecimalSample { Amount = 1234.56m });

			Assert.AreEqual (1254.55m, _db.ExecuteScalar<decimal> ("select sum(Amount) from DecimalSample"));
		}
	}
}
