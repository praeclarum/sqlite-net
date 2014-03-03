using System;
using System.Collections.Generic;
using System.Linq;

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
    public class PragmaTest
    {
        public class ClassWithOneIndex
        {
            [PrimaryKey]
            [AutoIncrement]
            public int Id { get; set; }
            [Indexed("UX_Name", 1)]
            public string LastName { get; set; }
            [Indexed ("UX_Name", 2)]
            public string FirstName { get; set; }
        }

        public class ClassWithTwoIndices
        {
            [PrimaryKey]
            [AutoIncrement]
            public int Id { get; set; }
            [Indexed("UX_Name", 2)]
            public string FirstName { get; set; }
            [Indexed("UX_Name", 1)]
            public string LastName { get; set; }
            [Indexed("UX_Locale", 1)]
            public string City { get; set; }
            [Indexed ("UX_Locale", 2)]
            public string State { get; set; }
        }

        public class ClassWithNoIndex
        {
            [PrimaryKey]
            [AutoIncrement]
            public int Id { get; set; }
        }

        [Test]
        public void GetSingleIndex()
        {
            using (TestDb db = new TestDb ()) {
                db.CreateTable<ClassWithOneIndex> ();
                var info = Pragma.GetIndexList (db, typeof (ClassWithOneIndex));
                var index = info.FirstOrDefault ();
                var columns = Pragma.GetIndexInfo(db, "UX_Name");
                Assert.AreEqual (1, info.Count ());
                Assert.AreEqual ("UX_Name", index.Name);
                Assert.AreEqual (0, index.Seq);
                Assert.AreEqual (false, index.Unique);
                Assert.AreEqual ("LastName", columns[0].Name);
                Assert.AreEqual ("FirstName", columns[1].Name);
            }
        }

        [Test]
        public void GetMultipleIndices()
        {
            using (TestDb db = new TestDb ()) {
                db.CreateTable<ClassWithTwoIndices> ();
                var info = Pragma.GetIndexList (db, typeof (ClassWithTwoIndices));
                List<Pragma.IndexColumnInfo> columns;

                var firstIndex = info[0];
                Assert.AreEqual ("UX_Locale", firstIndex.Name);
                Assert.AreEqual (0, firstIndex.Seq);
                Assert.AreEqual (false, firstIndex.Unique);
                columns = Pragma.GetIndexInfo (db, firstIndex);
                Assert.AreEqual ("City, State", string.Join (", ", columns.Select (c => c.Name)));

                var secondIndex = info[1];
                Assert.AreEqual ("UX_Name", secondIndex.Name);
                Assert.AreEqual (1, secondIndex.Seq);
                Assert.AreEqual (false, secondIndex.Unique);
                columns = Pragma.GetIndexInfo (db, secondIndex);
                Assert.AreEqual ("LastName, FirstName", string.Join (", ", columns.Select (c => c.Name)));
            }
        }

        [Test]
        public void GetNoIndex()
        {
            using (TestDb db = new TestDb ()) {
                db.CreateTable<ClassWithNoIndex> ();
                var info = Pragma.GetIndexList (db, typeof (ClassWithNoIndex));
                var index = info.FirstOrDefault ();
                Assert.IsNull (index);
            }
        }

        [Test]
        public void GetBadIndexName()
        {
            using (TestDb db = new TestDb ()) {
                db.CreateTable<ClassWithOneIndex> ();
                var columns = Pragma.GetIndexInfo (db, "UX_Nonesuch");
                Assert.AreEqual (0, columns.Count ());
            }
        }

        [Test]
        public void ConvenienceMethodReturnsSameColumns()
        {
            using (TestDb db = new TestDb ()) {
                db.CreateTable<ClassWithOneIndex> ();
                var info = Pragma.GetIndexList (db, typeof (ClassWithOneIndex));
                var index = info.FirstOrDefault ();
                var columns = Pragma.GetIndexInfo (db, "UX_Name");
                var convenienceColumns = index.GetColumns (db);

                Assert.AreEqual (columns[0].Name, convenienceColumns[0].Name);
                Assert.AreEqual (columns[0].SeqNo, convenienceColumns[0].SeqNo);
                Assert.AreEqual (columns[0].TableRank, convenienceColumns[0].TableRank);
            }
        }

        [Test]
        public void GenericMethodReturnsSameIndices()
        {
            using (TestDb db = new TestDb ()) {
                db.CreateTable<ClassWithTwoIndices> ();
                var genericIndex = Pragma.GetIndexList<ClassWithTwoIndices> (db);
                var index = Pragma.GetIndexList (db, typeof (ClassWithTwoIndices));

                Assert.AreEqual (index.Count, genericIndex.Count);

                Assert.AreEqual (index[0].Name, genericIndex[0].Name);
                Assert.AreEqual (index[0].Seq, genericIndex[0].Seq);
                Assert.AreEqual (index[0].Unique, genericIndex[0].Unique);

                Assert.AreEqual (index[1].Name, genericIndex[1].Name);
                Assert.AreEqual (index[1].Seq, genericIndex[1].Seq);
                Assert.AreEqual (index[1].Unique, genericIndex[1].Unique);
            }
        }
    }
}
