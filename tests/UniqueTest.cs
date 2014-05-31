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
	public class UniqueIndexTest
	{
		public class TheOne {
			[PrimaryKey, AutoIncrement]
			public int ID { get; set; }

			[Unique (Name = "UX_Uno")]
			public int Uno { get; set;}

			[Unique (Name = "UX_Dos")]
			public int Dos { get; set;}
			[Unique (Name = "UX_Dos")]
			public int Tres { get; set;}

			[Indexed (Name = "UX_Uno_bool", Unique = true)]
			public int Cuatro { get; set;}

			[Indexed (Name = "UX_Dos_bool", Unique = true)]
			public int Cinco { get; set;}
			[Indexed (Name = "UX_Dos_bool", Unique = true)]
			public int Seis { get; set;}
		}

        public class LosDos
        {
            [PrimaryKey, AutoIncrement]
            public int ID { get; set; }

            [Unique (Name = "UX_Uno")]
            [Unique (Name = "UX_One")]
            public int Uno { get; set; }

            [Unique (Name = "UX_Dos")]
            [Unique (Name = "UX_Two", Order = 2)]
            public int Dos { get; set; }
            [Unique (Name = "UX_Dos")]
            [Unique (Name = "UX_Two", Order = 1)]
            public int Tres { get; set; }
        }

        public class TheThree
        {
            [PrimaryKey, AutoIncrement]
            public int ID { get; set; }

            [Unique (Name = "UX_One")]
            public int One { get; set; }
            [Unique (Name = "UX_One")]
            [Unique (Name = "UX_Two")]
            public int Two { get; set; }
            [Unique (Name = "UX_Two")]
            public int Three { get; set; }

            [Indexed(Name = "IX_Uno")]
            public string NotUnique { get; set; }
        }

        public class ClassWithBadIndex
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            [Indexed (Name = "UX_Bad1", Unique = false)]
            public int Column1 { get; set; }
            [Indexed (Name = "UX_Bad1", Unique = true)]
            public int Column2 { get; set; }
        }

        public class AnotherClassWithBadIndex
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            [Indexed (Name = "UX_Bad1", Unique = false)]
            public int Column1 { get; set; }
            [Unique (Name = "UX_Bad1")]
            public int Column2 { get; set; }
        }

		[Test]
		public void CreateUniqueIndexes ()
		{
			using (var db = new TestDb ()) {
				db.CreateTable<TheOne> ();
                var indexes = Pragma.GetIndexList (db, typeof (TheOne));
				Assert.AreEqual (4, indexes.Count(), "# of indexes");
				CheckIndex (db, indexes, "UX_Uno", true, "Uno");
				CheckIndex (db, indexes, "UX_Dos", true, "Dos", "Tres");
				CheckIndex (db, indexes, "UX_Uno_bool", true, "Cuatro");
				CheckIndex (db, indexes, "UX_Dos_bool", true, "Cinco", "Seis");
			}
		}

        [Test]
        public void CreateMultipleUniqueIndexesWithSharedColumns()
        {
            using (var db = new TestDb ()) {
                db.CreateTable<LosDos> ();
                var indexes = Pragma.GetIndexList (db, typeof (LosDos));
                Assert.AreEqual (4, indexes.Count(), "# of indexes");
                CheckIndex (db, indexes, "UX_Uno", true, "Uno");
                CheckIndex (db, indexes, "UX_One", true, "Uno");
                CheckIndex (db, indexes, "UX_Dos", true, "Dos", "Tres");
                CheckIndex (db, indexes, "UX_Two", true, "Dos", "Tres");
            }
        }

        [Test]
        public void BadIndexThrowsException()
        {
            using (var db = new TestDb ()) {
                bool exceptionCaught = false;
                try {
                    db.CreateTable<ClassWithBadIndex> ();
                }
                catch (Exception) {
                    exceptionCaught = true;
                }

                Assert.IsTrue (exceptionCaught, "Expected an exception to be thrown. No exception was thrown.");
            }
        }

        [Test]
        public void BadIndexWithMixedAttributesThrowsException()
        {
            using (var db = new TestDb ()) {
                bool exceptionCaught = false;
                try {
                    db.CreateTable<AnotherClassWithBadIndex> ();
                }
                catch (Exception) {
                    exceptionCaught = true;
                }

                Assert.IsTrue (exceptionCaught, "Expected an exception to be thrown. No exception was thrown.");
            }
        }

        [Test]
        public void InsertThrowsSpecificException()
        {
            using (var db = new TestDb ()) {
                bool exceptionCaught = false;

                try {
                    TheThree obj;

                    db.CreateTable<TheThree> ();
                    obj = new TheThree () { One = 1, Two = 2 };
                    db.Insert (obj);
                    db.Insert (obj);
                }
                catch (UniqueConstraintViolationException) {
                    exceptionCaught = true;
                }
                catch (SQLiteException ex) {
                    if (SQLite3.LibVersionNumber () < 3007017 && ex.Result == SQLite3.Result.Constraint) {
                        Inconclusive ();
                        return;
                    }
                }
                catch (Exception ex) {
                    Assert.Fail ("Expected an exception of type UniqueConstraintViolationException to be thrown. An exception of type {0} was thrown instead.", ex.GetType ().Name);
                }

                Assert.IsTrue (exceptionCaught, "Expected an exception of type UniqueConstraintViolationException to be thrown. No exception was thrown.");
            }
        }

        [Test]
        public void UpdateThrowsSpecificException()
        {
            using (var db = new TestDb ()) {
                bool exceptionCaught = false;

                try {
                    TheThree obj;

                    db.CreateTable<TheThree> ();
                    obj = new TheThree () { One = 1, Two = 2 };
                    db.Insert (obj);
                    obj.Two = 3;
                    db.Insert (obj);
                    obj.Two = 2;
                    db.Update (obj);
                }
                catch (UniqueConstraintViolationException) {
                    exceptionCaught = true;
                }
                catch (SQLiteException ex) {
                    if (SQLite3.LibVersionNumber () < 3007017 && ex.Result == SQLite3.Result.Constraint) {
                        Inconclusive ();
                        return;
                    }
                }
                catch (Exception ex) {
                    Assert.Fail ("Expected an exception of type UniqueConstraintViolationException to be thrown. An exception of type {0} was thrown instead.", ex.GetType ().Name);
                }

                Assert.IsTrue (exceptionCaught, "Expected an exception of type UniqueConstraintViolationException to be thrown. No exception was thrown.");
            }
        }

        [Test]
        public void InsertQueryThrowsSpecificException()
        {
            using (var db = new TestDb ()) {
                bool exceptionCaught = false;

                try {
                    TheThree obj;

                    db.CreateTable<TheThree> ();
                    obj = new TheThree () { One = 1, Two = 2 };
                    db.Insert (obj);
                    db.Execute ("insert into \"TheThree\" (One, Two) values(?, ?)", obj.One, obj.Two);
                }
                catch (UniqueConstraintViolationException) {
                    exceptionCaught = true;
                }
                catch (SQLiteException ex) {
                    if (SQLite3.LibVersionNumber () < 3007017 && ex.Result == SQLite3.Result.Constraint) {
                        Inconclusive ();
                        return;
                    }
                }
                catch (Exception ex) {
                    Assert.Fail ("Expected an exception of type UniqueConstraintViolationException to be thrown. An exception of type {0} was thrown instead.", ex.GetType ().Name);
                }

                Assert.IsTrue (exceptionCaught, "Expected an exception of type UniqueConstraintViolationException to be thrown. No exception was thrown.");
            }
        }

        [Test]
        public void UpdateQueryThrowsSpecificException()
        {

            using (var db = new TestDb ()) {
                bool exceptionCaught = false;

                try {
                    TheThree obj;

                    db.CreateTable<TheThree> ();
                    obj = new TheThree () { One = 1, Two = 2 };
                    db.Insert (obj);
                    obj.Two = 3;
                    db.Insert (obj);
                    obj.Two = 2;
                    db.Execute ("update \"TheThree\" set One=?, Two=? where ID=?", obj.One, obj.Two, obj.ID);
                }
                catch (UniqueConstraintViolationException) {
                    exceptionCaught = true;
                }
                catch (SQLiteException ex) {
                    if (SQLite3.LibVersionNumber () < 3007017 && ex.Result == SQLite3.Result.Constraint) {
                        Inconclusive ();
                        return;
                    }
                }
                catch (Exception ex) {
                    Assert.Fail ("Expected an exception of type UniqueConstraintViolationException to be thrown. An exception of type {0} was thrown instead.", ex.GetType ().Name);
                }

                Assert.IsTrue (exceptionCaught, "Expected an exception of type UniqueConstraintViolationException to be thrown. No exception was thrown.");
            }
        }

        [Test]
        public void ValidateConstraintsInExceptionHandler()
        {
            using (var db = new TestDb ()) {
                bool exceptionCaught = false;
                TheThree obj = new TheThree ();
                List<SQLiteConnection.ConstraintValidationInfo> validationIssues;

                try {
                    db.CreateTable<TheThree> ();
                    obj.One = 1;
                    obj.Two = 2;
                    obj.Three = 3;
                    db.Insert (obj);
                    obj.ID = 0;
                    db.Insert (obj);
                }
                catch (Exception ex) {
                    if (SQLite3.LibVersionNumber () >= 3007017) {
                        if (ex is UniqueConstraintViolationException) {
                            exceptionCaught = true;
                        }
                    } else if (ex is SQLiteException && ((SQLiteException)ex).Result == SQLite3.Result.Constraint) {
                        exceptionCaught = true;
                    }

                    validationIssues = (List<SQLiteConnection.ConstraintValidationInfo>)db.ValidateUniqueConstraints (obj);
                    Assert.AreEqual (2, validationIssues.Count);
                    var sorted = validationIssues.OrderBy (i => i.ConstraintName).ToArray ();
                    Assert.AreEqual ("UX_One", sorted[0].ConstraintName);
                    Assert.AreEqual ("UX_Two", sorted[1].ConstraintName);
                    Assert.AreEqual ("One, Two", string.Join (", ", sorted[0].Columns.Select (c => c.Name)));
                    Assert.AreEqual ("Two, Three", string.Join (", ", sorted[1].Columns.Select (c => c.Name)));
                }

                Assert.IsTrue (exceptionCaught, "Expected an exception of type UniqueConstraintViolationException or SQLiteException to be thrown. No exception was thrown.");
            }
        }

        [Test]
        public void ValidateConstraints()
        {
            using (var db = new TestDb ()) {
                TheThree obj = new TheThree ();
                List<SQLiteConnection.ConstraintValidationInfo> validationIssues;

                db.CreateTable<TheThree> ();
                obj.One = 1;
                obj.Two = 2;
                obj.Three = 3;
                db.Insert (obj);
                obj.ID = 0;
                obj.Three = 0;

                validationIssues = (List<SQLiteConnection.ConstraintValidationInfo>)db.ValidateUniqueConstraints (obj);
                Assert.AreEqual (1, validationIssues.Count);
                Assert.AreEqual ("UX_One", validationIssues[0].ConstraintName);
                Assert.AreEqual ("One", validationIssues[0].Columns[0].Name);
                Assert.AreEqual ("Two", validationIssues[0].Columns[1].Name);
            }
        }

        [Test]
        public void ValidateWithNoViolations()
        {
            using (var db = new TestDb ()) {
                TheThree obj = new TheThree ();
                List<SQLiteConnection.ConstraintValidationInfo> validationIssues;

                db.CreateTable<TheThree> ();
                obj.One = 1;
                obj.Two = 2;
                obj.Three = 3;
                db.Insert (obj);
                obj.ID = 0;
                obj.One = 2;
                obj.Three = 3;
                obj.Three = 4;
                validationIssues = db.ValidateUniqueConstraints (obj);
                Assert.AreEqual (0, validationIssues.Count);
            }
        }

        [Test]
        public void ValidationIgnoresNonUniqueIndices()
        {
            using (var db = new TestDb ()) {
                TheThree obj = new TheThree ();
                List<SQLiteConnection.ConstraintValidationInfo> validationIssues;

                db.CreateTable<TheThree> ();
                obj.One = 1;
                obj.Two = 2;
                obj.NotUnique = "NotUnique";
                db.Insert (obj);
                obj.ID = 0;
                obj.One = 0;
                validationIssues = db.ValidateUniqueConstraints (obj, obj.GetType());
                Assert.AreEqual (1, validationIssues.Count);
                Assert.AreEqual ("UX_Two", validationIssues[0].ConstraintName);
                Assert.AreEqual ("Two", validationIssues[0].Columns[0].Name);
                Assert.AreEqual ("Three", validationIssues[0].Columns[1].Name);
            }
        }

		static void CheckIndex (TestDb db, IEnumerable<Pragma.IndexInfo>indexes, string iname, bool unique, params string [] columns)
		{
			if (columns == null)
				throw new Exception ("Don't!");
			var idx = indexes.SingleOrDefault (i => i.Name == iname);
			Assert.IsNotNull (idx, String.Format ("Index {0} not found", iname));
			Assert.AreEqual (idx.Unique, unique, String.Format ("Index {0} unique expected {1} but got {2}", iname, unique, idx.Unique));
            var idx_columns = Pragma.GetIndexInfo (db, iname);
			Assert.AreEqual (columns.Length, idx_columns.Count(), String.Format ("# of columns: expected {0}, got {1}", columns.Length, idx_columns.Count()));
			foreach (var col in columns) {
				Assert.IsNotNull (idx_columns.SingleOrDefault (c => c.Name == col), String.Format ("Column {0} not in index {1}", col, idx.Name));
			}
		}

        void Inconclusive()
        {
#if !NETFX_CORE
            Console.WriteLine ("Detailed constraint information is only available in SQLite3 version 3.7.17 and above.");
#endif
        }
	}
}
