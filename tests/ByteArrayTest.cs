﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
#endif

namespace SQLite.Tests
{
#if NETFX_CORE
    [TestClass]
#else
    [TestFixture]
#endif
    public class ByteArrayTest
	{
		public class ByteArrayClass
		{
			[PrimaryKey, AutoIncrement]
			public int ID { get; set; }

			public byte[] bytes { get; set; }

			public void AssertEquals(ByteArrayClass other)
			{
				Assert.AreEqual(other.ID, ID);
				CollectionAssert.AreEqual(other.bytes, bytes);
			}
		}

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        [Description("Create objects with various byte arrays and check they can be stored and retrieved correctly")]
		public void ByteArrays()
		{
			//Byte Arrays for comparisson
			ByteArrayClass[] byteArrays = new ByteArrayClass[] {
				new ByteArrayClass() { bytes = new byte[] { 1, 2, 3, 4, 250, 252, 253, 254, 255 } }, //Range check
				new ByteArrayClass() { bytes = new byte[] { 0 } }, //null bytes need to be handled correctly
				new ByteArrayClass() { bytes = new byte[] { 0, 0 } },
				new ByteArrayClass() { bytes = new byte[] { 0, 1, 0 } },
				new ByteArrayClass() { bytes = new byte[] { 1, 0, 1 } },
				new ByteArrayClass() { bytes = new byte[] { } }, //Empty byte array should stay empty (and not become null)
				new ByteArrayClass() { bytes = null } //Null should be supported
			};

			SQLiteConnection database = new SQLiteConnection(TestHelper.GetTempDatabasePath());
			database.CreateTable<ByteArrayClass>();

			//Insert all of the ByteArrayClass
			foreach (ByteArrayClass b in byteArrays)
				database.Insert(b);

			//Get them back out
			ByteArrayClass[] fetchedByteArrays = database.Table<ByteArrayClass>().OrderBy(x => x.ID).ToArray();

			Assert.AreEqual(fetchedByteArrays.Length, byteArrays.Length);
			//Check they are the same
			for (int i = 0; i < byteArrays.Length; i++)
			{
				byteArrays[i].AssertEquals(fetchedByteArrays[i]);
			}
		}

#if NETFX_CORE
        [TestMethod]
#else
        [Test]
#endif
        [Description("Create A large byte array and check it can be stored and retrieved correctly")]
		public void LargeByteArray()
		{
			const int byteArraySize = 1024 * 1024;
			byte[] bytes = new byte[byteArraySize];
			for (int i = 0; i < byteArraySize; i++)
				bytes[i] = (byte)(i % 256);

			ByteArrayClass byteArray = new ByteArrayClass() { bytes = bytes };

			SQLiteConnection database = new SQLiteConnection(TestHelper.GetTempDatabasePath());
			database.CreateTable<ByteArrayClass>();

			//Insert the ByteArrayClass
			database.Insert(byteArray);

			//Get it back out
			ByteArrayClass[] fetchedByteArrays = database.Table<ByteArrayClass>().ToArray();

			Assert.AreEqual(fetchedByteArrays.Length, 1);

			//Check they are the same
			byteArray.AssertEquals(fetchedByteArrays[0]);
		}
	}
}
