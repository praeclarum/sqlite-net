using System;
using NUnit.Framework;

namespace SQLite.Tests
{
	[TestFixture]
	public class ColumnTypeTest
	{
		enum EEnum { EnumVal1 = 42, EnumVal2 = -12 };
		class Test
		{
			[PrimaryKey]
			public int      Id        { get; set; }
			public Int32    Int32     { get; set; }
			public String   String    { get; set; }
			public Byte     Byte      { get; set; }
			public UInt16   UInt16    { get; set; }
			public SByte    SByte     { get; set; }
			public Int16    Int16     { get; set; }
			public Boolean  Boolean   { get; set; }
			public UInt32   UInt32    { get; set; }
			public Int64    Int64     { get; set; }
			public Single   Single    { get; set; }
			public Double   Double    { get; set; }
			public Decimal  Decimal   { get; set; }
			public EEnum    Enum1     { get; set; }
			public EEnum    Enum2     { get; set; }
			public DateTime Timestamp { get; set; }
			public byte[]   Blob      { get; set; }
			public Guid     GUID      { get; set; }
		}
		
		[Test]
		public void ColumnsSaveLoadCorrectly()
		{
			var db = new TestDb();
			db.CreateTable<Test>();
			
			var test = new Test
			{
				Id = 0,
				Int32 = 0x1337beef,
				String = "A unicode string \u2022 <- bullet point",
				Byte = 0xEA,
				UInt16 = 65535,
				SByte = -128,
				Int16 = -32768,
				Boolean = false,
				UInt32 = 0xdeadbeef,
				Int64 = 0x123456789abcdef,
				Single = 6.283185f,
				Double = 6.283185307179586476925286766559,
				Decimal = (decimal) 6.283185307179586476925286766559,
				Enum1 = EEnum.EnumVal1,
				Enum2 = EEnum.EnumVal2,
				Timestamp = DateTime.Parse("2012-04-05 15:08:24.723"),
				Blob = new byte[]{1,2,3,4,5,6,7,8,9,10},
				GUID = Guid.NewGuid()
			};
			
			db.Insert(test);
			var res = db.Get<Test>(test.Id);
			
			Assert.AreEqual(test.Id        , res.Id        );
			Assert.AreEqual(test.Int32     , res.Int32     );
			Assert.AreEqual(test.String    , res.String    );
			Assert.AreEqual(test.Byte      , res.Byte      );
			Assert.AreEqual(test.UInt16    , res.UInt16    );
			Assert.AreEqual(test.SByte     , res.SByte     );
			Assert.AreEqual(test.Int16     , res.Int16     );
			Assert.AreEqual(test.Boolean   , res.Boolean   );
			Assert.AreEqual(test.UInt32    , res.UInt32    );
			Assert.AreEqual(test.Int64     , res.Int64     );
			Assert.AreEqual(test.Single    , res.Single    );
			Assert.AreEqual(test.Double    , res.Double    );
			Assert.AreEqual(test.Decimal   , res.Decimal   );
			Assert.AreEqual(test.Enum1     , res.Enum1     );
			Assert.AreEqual(test.Enum2     , res.Enum2     );
			Assert.AreEqual(test.Timestamp , res.Timestamp );
			Assert.AreEqual(test.Blob      , res.Blob      );
			Assert.AreEqual(test.GUID      , res.GUID      );
		}
	}
}
