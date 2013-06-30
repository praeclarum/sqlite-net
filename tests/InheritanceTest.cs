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
	[TestFixture]
	public class InheritanceTest
	{
		class Base
		{
			[PrimaryKey]
			public int Id { get; set; }
			
			public string BaseProp { get; set; }
		}
		
		class Derived : Base
		{
			public string DerivedProp { get; set; }
		}
		
		
		[Test]
		public void InheritanceWorks ()
		{
			var db = new TestDb ();
			
			var mapping = db.GetMapping<Derived> ();
			
			Assert.AreEqual (3, mapping.Columns.Length);
			Assert.AreEqual ("Id", mapping.PK.Name);
		}
	}

    [TestFixture]
    public class InheritanceTestWithOne2OneRelationship
    {
        class EntityWithId
        {
            [PrimaryKey]
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
        }

        public class InheritanceTestWithOne2OneRelationshipDb : SQLiteConnection
        {
            public InheritanceTestWithOne2OneRelationshipDb(String path) : base(path)
            {
                CreateTable<Person>();
                CreateTable<Name>();
            }
        }

        private InheritanceTestWithOne2OneRelationshipDb database;
        
        [SetUp]
        public void Setup()
        {
            this.database = new InheritanceTestWithOne2OneRelationshipDb(TestPath.GetTempFileName());
            this.database.SetForeignKeysPermissions(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (this.database != null) this.database.Close();
        }

        [Test]
        public void SavingParentSavesChildEntity()
        {
            const string firstName = "Clark";
            const string lastName = "Kent";
            
            var person = new Person
                {
                    Age = 18,
                    Name = new Name { FirstName = firstName, LastName = lastName }
                };

            database.Insert(person);

            var nameSaved = this.database.Table<Name>().SingleOrDefault(x => x.PersonId == person.Id);
            Assert.IsNotNull(nameSaved);
            Assert.That(nameSaved.FirstName, Is.EqualTo(firstName));
            Assert.That(nameSaved.LastName, Is.EqualTo(lastName));
        }

        [Test]
        public void LoadingParentEntityLoadsChildEntity()
        {
            const string firstName = "Martha";
            const string lastName = "Stewart";

            var person = new Person
            {
                Age = 18,
                Name = new Name { FirstName = firstName, LastName = lastName }
            };

            database.Insert(person);

            var loadedParent = this.database.Table<Person>().SingleOrDefault(x => x.Id == person.Id);
            
            Assert.That(loadedParent.Name.FirstName, Is.EqualTo(firstName));
            Assert.That(loadedParent.Name.LastName, Is.EqualTo(lastName));
        }
    }
}
