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
    public class NotNullAttributeTest
    {
        private class NotNullNoPK
        {
            [PrimaryKey, AutoIncrement]
            public int? objectId { get; set; }
            [NotNull]
            public int? RequiredIntProp { get; set; }
            public int? OptionalIntProp { get; set; }
            [NotNull]
            public string RequiredStringProp { get; set; }
            public string OptionalStringProp { get; set; }
            [NotNull]
            public string AnotherRequiredStringProp { get; set; }
        }

        private class ClassWithPK
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
        }

        private IEnumerable<SQLiteConnection.ColumnInfo> GetExpectedColumnInfos(IReflectionService reflectionService, Type type)
        {
            var expectedValues = from prop in reflectionService.GetPublicInstanceProperties(type)
                                 select new SQLiteConnection.ColumnInfo
                                 {
                                     Name = prop.Name,
                                     notnull = ((!prop.GetCustomAttributes<NotNullAttribute>(true).Any()) && (!prop.GetCustomAttributes<PrimaryKeyAttribute>(true).Any())) ? 0 : 1
                                 };

            return expectedValues;
        }

        [Test]
        public void PrimaryKeyHasNotNullConstraint()
        {
            using (TestDb db = new TestDb())
            {

                db.CreateTable<ClassWithPK>();
                var cols = db.GetTableInfo("ClassWithPK");

                var joined = from expected in GetExpectedColumnInfos(db.Platform.ReflectionService, typeof(ClassWithPK))
                             join actual in cols on expected.Name equals actual.Name
                             where actual.notnull != expected.notnull
                             select actual.Name;

                Assert.AreNotEqual(0, cols.Count(), "Failed to get table info");
                Assert.IsTrue(joined.Count() == 0, string.Format("not null constraint was not created for the following properties: {0}"
                    , string.Join(", ", joined.ToArray())));
            }
        }

        [Test]
        public void CreateTableWithNotNullConstraints()
        {
            using (var db = new TestDb())
            {
                db.CreateTable<NotNullNoPK>();
                var cols = db.GetTableInfo("NotNullNoPK");

                var joined = from expected in GetExpectedColumnInfos(db.Platform.ReflectionService, typeof(NotNullNoPK))
                             join actual in cols on expected.Name equals actual.Name
                             where actual.notnull != expected.notnull
                             select actual.Name;

                Assert.AreNotEqual(0, cols.Count(), "Failed to get table info");
                Assert.IsTrue(joined.Count() == 0, string.Format("not null constraint was not created for the following properties: {0}"
                    , string.Join(", ", joined.ToArray())));
            }
        }

        [Test]
        public void InsertWithNullsThrowsException()
        {
            using (TestDb db = new TestDb())
            {
                db.CreateTable<NotNullNoPK>();

                try
                {
                    NotNullNoPK obj = new NotNullNoPK();
                    db.Insert(obj);
                }
                catch (NotNullConstraintViolationException)
                {
                    return;
                }
                catch (SQLiteException ex)
                {
                    if (db.Platform.SQLiteApi.LibVersionNumber() < 3007017 && ex.Result == Result.Constraint)
                    {
                        Assert.Inconclusive("Detailed constraint information is only available in SQLite3 version 3.7.17 and above.");
                    }
                }
            }
            Assert.Fail("Expected an exception of type NotNullConstraintViolationException to be thrown. No exception was thrown.");
        }


        [Test]
        public void UpdateWithNullThrowsException()
        {
            using (TestDb db = new TestDb())
            {

                db.CreateTable<NotNullNoPK>();

                try
                {
                    NotNullNoPK obj = new NotNullNoPK()
                    {
                        AnotherRequiredStringProp = "Another required string",
                        RequiredIntProp = 123,
                        RequiredStringProp = "Required string"
                    };
                    db.Insert(obj);
                    obj.RequiredStringProp = null;
                    db.Update(obj);
                }
                catch (NotNullConstraintViolationException)
                {
                    return;
                }
                catch (SQLiteException ex)
                {
                    if (db.Platform.SQLiteApi.LibVersionNumber() < 3007017 && ex.Result == Result.Constraint)
                    {
                        Assert.Inconclusive("Detailed constraint information is only available in SQLite3 version 3.7.17 and above.");
                    }
                }
            }
            Assert.Fail("Expected an exception of type NotNullConstraintViolationException to be thrown. No exception was thrown.");
        }

        [Test]
        public void NotNullConstraintExceptionListsOffendingColumnsOnInsert()
        {
            using (TestDb db = new TestDb())
            {

                db.CreateTable<NotNullNoPK>();

                try
                {
                    NotNullNoPK obj = new NotNullNoPK() { RequiredStringProp = "Some value" };
                    db.Insert(obj);
                }
                catch (NotNullConstraintViolationException ex)
                {
                    string expected = "AnotherRequiredStringProp, RequiredIntProp";
                    string actual = string.Join(", ", ex.Columns.Where(c => !c.IsPK).OrderBy(p => p.PropertyName).Select(c => c.PropertyName));

                    Assert.AreEqual(expected, actual, "NotNullConstraintViolationException did not correctly list the columns that violated the constraint");
                    return;
                }
                catch (SQLiteException ex)
                {
                    if (db.Platform.SQLiteApi.LibVersionNumber() < 3007017 && ex.Result == Result.Constraint)
                    {
                        Assert.Inconclusive("Detailed constraint information is only available in SQLite3 version 3.7.17 and above.");
                    }
                }
                Assert.Fail("Expected an exception of type NotNullConstraintViolationException to be thrown. No exception was thrown.");
            }
        }

        [Test]
        public void NotNullConstraintExceptionListsOffendingColumnsOnUpdate()
        {
            using (TestDb db = new TestDb())
            {
                // Skip this test if the Dll doesn't support the extended SQLITE_CONSTRAINT codes
                if (db.Platform.SQLiteApi.LibVersionNumber() >= 3007017)
                {
                    db.CreateTable<NotNullNoPK>();

                    try
                    {
                        NotNullNoPK obj = new NotNullNoPK()
                        {
                            AnotherRequiredStringProp = "Another required string",
                            RequiredIntProp = 123,
                            RequiredStringProp = "Required string"
                        };
                        db.Insert(obj);
                        obj.RequiredStringProp = null;
                        db.Update(obj);
                    }
                    catch (NotNullConstraintViolationException ex)
                    {
                        string expected = "RequiredStringProp";
                        string actual = string.Join(", ", ex.Columns.Where(c => !c.IsPK).OrderBy(p => p.PropertyName).Select(c => c.PropertyName));

                        Assert.AreEqual(expected, actual, "NotNullConstraintViolationException did not correctly list the columns that violated the constraint");

                        return;
                    }
                    catch (SQLiteException ex)
                    {
                        if (db.Platform.SQLiteApi.LibVersionNumber() < 3007017 && ex.Result == Result.Constraint)
                        {
                            Assert.Inconclusive("Detailed constraint information is only available in SQLite3 version 3.7.17 and above.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail(
                            "Expected an exception of type NotNullConstraintViolationException to be thrown. An exception of type {0} was thrown instead.",
                            ex.GetType().Name);
                    }
                    Assert.Fail("Expected an exception of type NotNullConstraintViolationException to be thrown. No exception was thrown.");
                }
            }
        }

        [Test]
        public void InsertQueryWithNullThrowsException()
        {
            using (TestDb db = new TestDb())
            {
                // Skip this test if the Dll doesn't support the extended SQLITE_CONSTRAINT codes
                if (db.Platform.SQLiteApi.LibVersionNumber() >= 3007017)
                {
                    db.CreateTable<NotNullNoPK>();

                    try
                    {
                        db.Execute("insert into \"NotNullNoPK\" (AnotherRequiredStringProp, RequiredIntProp, RequiredStringProp) values(?, ?, ?)",
                            new object[] {"Another required string", 123, null});
                    }
                    catch (NotNullConstraintViolationException)
                    {
                        return;
                    }
                    catch (SQLiteException ex)
                    {
                        if (db.Platform.SQLiteApi.LibVersionNumber() < 3007017 && ex.Result == Result.Constraint)
                        {
                            Assert.Inconclusive("Detailed constraint information is only available in SQLite3 version 3.7.17 and above.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail(
                            "Expected an exception of type NotNullConstraintViolationException to be thrown. An exception of type {0} was thrown instead.",
                            ex.GetType().Name);
                    }
                }
                Assert.Fail("Expected an exception of type NotNullConstraintViolationException to be thrown. No exception was thrown.");
            }
        }

        [Test]
        public void UpdateQueryWithNullThrowsException()
        {
            // Skip this test if the Dll doesn't support the extended SQLITE_CONSTRAINT codes
            using (TestDb db = new TestDb())
            {

                db.CreateTable<NotNullNoPK>();

                try
                {
                    db.Execute("insert into \"NotNullNoPK\" (AnotherRequiredStringProp, RequiredIntProp, RequiredStringProp) values(?, ?, ?)",
                        new object[] { "Another required string", 123, "Required string" });

                    db.Execute("update \"NotNullNoPK\" set AnotherRequiredStringProp=?, RequiredIntProp=?, RequiredStringProp=? where ObjectId=?",
                        new object[] { "Another required string", 123, null, 1 });
                }
                catch (NotNullConstraintViolationException)
                {
                    return;
                }
                catch (SQLiteException ex)
                {
                    if (db.Platform.SQLiteApi.LibVersionNumber() < 3007017 && ex.Result == Result.Constraint)
                    {
                        Assert.Inconclusive("Detailed constraint information is only available in SQLite3 version 3.7.17 and above.");
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail("Expected an exception of type NotNullConstraintViolationException to be thrown. An exception of type {0} was thrown instead.", ex.GetType().Name);
                }
                Assert.Fail("Expected an exception of type NotNullConstraintViolationException to be thrown. No exception was thrown.");
            }
        }
    }
}