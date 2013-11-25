using System;
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
    public class Get_parent_entity_with_child_having_an_ignore_attribute
    {
        class EntityWithId
        {
            [PrimaryKey, SQLite.AutoIncrement]
            public int Id { get; set; }
        }

        class Person : EntityWithId
        {

            [One2One(typeof(Name))]
            public Name Name { get; set; }

            public int Age { get; set; }
        }

        class Name : EntityWithId
        {
            [References(typeof(Person))]
            [ForeignKey]
            public int PersonId { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            [SQLite.Ignore]
            public string DisplayName 
            { 
                get
                {
                    return string.Format("{0} {1}", this.FirstName, this.LastName);
                }
            }
        }

        public class GetTestWithOne2OneRelationshipDb : SQLiteConnection
        {
            public GetTestWithOne2OneRelationshipDb(String path)
                : base(path)
            {
                CreateTable<Person>();
                CreateTable<Name>();
            }
        }

        private GetTestWithOne2OneRelationshipDb database;

        [SetUp]
        public void Setup()
        {
            this.database = new GetTestWithOne2OneRelationshipDb(TestPath.GetTempFileName());
            this.database.SetForeignKeysPermissions(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (this.database != null) this.database.Close();
        }

        [Test]
        public void should_load_parent_and_child_entity()
        {
            this.database.Insert(
                new Person
                {
                    Age = 30,
                    Name = new Name { FirstName = "Bob", LastName = "Dylan" }
                });

            var entity = this.database.Table<Person>().FirstOrDefault(x => x.Name.LastName.Equals("Dylan"));

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.Name, Is.Not.Null);
            Assert.That(entity.Name.LastName, Is.EqualTo("Dylan"));
        }

    }

    public class Get_parent_entity_where_child_entity_is_initialized_in_parent_constructor_using_load_attribute
    {
        class EntityWithId
        {
            [PrimaryKey, SQLite.AutoIncrement]
            public int Id { get; set; }
        }

        class Person : EntityWithId
        {
            private bool isInitialized;

            public Person()
            {
                this.Name = new Name();
            }

            [One2One(typeof(Name))]
            public Name Name { get; set; }

            public int Age { get; set; }

            [Initialized, Ignore]
            public bool IsInitialized
            {
                get
                {
                   return this.Name.Id > 0;
                }
            }
        }

        class Name : EntityWithId
        {
            [References(typeof(Person))]
            [ForeignKey]
            public int PersonId { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            [SQLite.Ignore]
            public string DisplayName
            {
                get
                {
                    return string.Format("{0} {1}", this.FirstName, this.LastName);
                }
            }
        }

        public class GetTestWithOne2OneRelationshipDb : SQLiteConnection
        {
            public GetTestWithOne2OneRelationshipDb(String path)
                : base(path)
            {
                CreateTable<Person>();
                CreateTable<Name>();
            }
        }

        private GetTestWithOne2OneRelationshipDb database;

        [SetUp]
        public void Setup()
        {
            this.database = new GetTestWithOne2OneRelationshipDb(TestPath.GetTempFileName());
            this.database.SetForeignKeysPermissions(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (this.database != null) this.database.Close();
        }

        [Test]
        public void should_load_parent_and_child_entity()
        {
            this.database.Insert(
                new Person
                {
                    Age = 30,
                    Name = new Name { FirstName = "Bob", LastName = "Dylan" }
                });

            var entity = this.database.Table<Person>().FirstOrDefault(x => x.Name.LastName.Equals("Dylan"));

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.Name, Is.Not.Null);
            Assert.That(entity.Name.LastName, Is.EqualTo("Dylan"));
        }
    }
}
