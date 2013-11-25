// -----------------------------------------------------------------------
// <copyright file="AttributeTests.cs" company="UBS AG">
// Copyright (c) 2013.
// </copyright>
// -----------------------------------------------------------------------
namespace SQLite.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class AttributeTests
    {
        class Entity
        {
            [PrimaryKey]
            public int Id { get; set; }
        }

        class Person : Entity
        {
            public string Name { get; set; }

            [One2One(typeof(Address))]
            public Address Address { get; set; }
        }

        class Address : Entity
        {
            [ForeignKey]
            [References(typeof(Person))]
            public int PersonId { get; set; } 
        }

        [Test]
        public void IfAttributeExistsOnObjectReturnsTrue()
        {
            var entity = new Entity();

            var exists = PrimaryKeyAttribute.IsDefined(entity);

            Assert.IsTrue(exists);
        }

        [Test]
        public void IfAttributeDoesNotExistsOnObjectReturnsFalse()
        {
            var entity = new Entity();

            var exists = ReferencesAttribute.IsDefined(entity);

            Assert.IsFalse(exists);
        }

        [Test]
        public void CanRetrieveTheValueOfAPropertyLookingItUpByAttribute()
        {
            var entity = new Entity { Id = 101 };

            int value = (int)BaseAttribute.GetValueOfProperty<PrimaryKeyAttribute>(entity);

            Assert.That(value, Is.EqualTo(101));
        }
    }
}