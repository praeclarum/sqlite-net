using System;
using System.Collections.Generic;

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
	public class NestedObjectPropertyMappingTest
	{
		SQLiteConnection db;

		int fooId = 1;
		int fooReferenceId = 3;
		string fooName = "Foo Table";
		string fooDescription = "The main table.";
		string fooReferenceName = "Referenced table.";
		string fooReferenceDescription = "A table referenced by the main table";

		class FooTable
		{
			[PrimaryKey]
			public int Id { get; set; }

			public string FooName { get; set; }

			public string FooDescription { get; set; }

			public int FooTableReferenceId { get; set; }
		}

		class FooTableReference
		{
			[PrimaryKey]
			public int Id { get; set; }

			public string ReferenceName { get; set; }

			public string ReferenceDescription { get; set; }
		}

		class FooDTO
		{
			public int Id { get; set; }

			public string Name { get; set; }

			public string Description { get; set; }

			public FooNestedObject NestedObject { get; set; }
		}

		class FooNestedObject
		{
			public int Id { get; set; }

			public string NestedName { get; set; }

			public string NestedDescription { get; set; }
			
			public FooNestedObject NestedChild { get; set; }
		}

		public NestedObjectPropertyMappingTest()
		{
			SetupDb ();
		}

		void SetupDb()
		{
			db = new TestDb ();

			db.CreateTable<FooTable> ();
			db.CreateTable<FooTableReference> ();

			db.Insert (new FooTable {
				Id = fooId,
				FooName = fooName,
				FooDescription = fooDescription,
				FooTableReferenceId = fooReferenceId
			});
			db.Insert (new FooTableReference {
				Id = fooReferenceId,
				ReferenceName = fooReferenceName,
				ReferenceDescription = fooReferenceDescription,
			});
		}

		[Test]
		public void SelectNestedObject ()
		{
			string query =
				$" select" +
				$" fooTable.{nameof (FooTable.Id)} as '{nameof (FooDTO.Id)}'," +
				$" fooTable.{nameof (FooTable.FooName)} as '{nameof (FooDTO.Name)}'," +
				$" fooTable.{nameof (FooTable.FooDescription)} as '{nameof (FooDTO.Description)}'," +
				$" fooTableReference.{nameof (FooTableReference.Id)} as '{nameof (FooDTO.NestedObject)}.{nameof (FooNestedObject.Id)}'," +
				$" fooTableReference.{nameof (FooTableReference.ReferenceName)} as '{nameof (FooDTO.NestedObject)}.{nameof (FooNestedObject.NestedName)}'," +
				$" fooTableReference.{nameof (FooTableReference.ReferenceDescription)} as '{nameof (FooDTO.NestedObject)}.{nameof (FooNestedObject.NestedDescription)}'" +
				$" from {nameof (FooTable)} as fooTable" +
				$" join {nameof (FooTableReference)} fooTableReference on fooTable.{nameof (FooTable.FooTableReferenceId)} = fooTableReference.{nameof (FooTableReference.Id)}" +
				$" where fooTable.{nameof (FooTable.Id)} = {fooId}";
			var dtoArray = db.Query<FooDTO> (query);
			var dto = dtoArray[0];

			Assert.AreEqual (dto.Id, fooId);
			Assert.AreEqual (dto.Name, fooName);
			Assert.AreEqual (dto.Description, fooDescription);
			Assert.IsNotNull (dto.NestedObject);
			Assert.AreEqual (dto.NestedObject.Id, fooReferenceId);
			Assert.AreEqual (dto.NestedObject.NestedName, fooReferenceName);
			Assert.AreEqual (dto.NestedObject.NestedDescription, fooReferenceDescription);
		}

		[Test]
		public void SelectNestedObjectChildObjectProperty ()
		{
			string query =
				$" select" +
				$" fooTable.{nameof (FooTable.Id)} as '{nameof (FooDTO.Id)}'," +
				$" fooTable.{nameof (FooTable.FooName)} as '{nameof (FooDTO.Name)}'," +
				$" fooTable.{nameof (FooTable.FooDescription)} as '{nameof (FooDTO.Description)}'," +
				$" fooTableReference.{nameof (FooTableReference.Id)} as '{nameof (FooDTO.NestedObject)}.{nameof (FooNestedObject.Id)}'," +
				$" fooTableReference.{nameof (FooTableReference.ReferenceName)} as '{nameof (FooDTO.NestedObject)}.{nameof (FooNestedObject.NestedName)}'," +
				$" fooTableReference.{nameof (FooTableReference.ReferenceDescription)} as '{nameof (FooDTO.NestedObject)}.{nameof (FooNestedObject.NestedChild)}.{nameof (FooNestedObject.NestedDescription)}'" +
				$" from {nameof (FooTable)} as fooTable" +
				$" join {nameof (FooTableReference)} fooTableReference on fooTable.{nameof (FooTable.FooTableReferenceId)} = fooTableReference.{nameof (FooTableReference.Id)}" +
				$" where fooTable.{nameof (FooTable.Id)} = {fooId}";
			var dtoArray = db.Query<FooDTO> (query);
			var dto = dtoArray[0];

			Assert.AreEqual (dto.Id, fooId);
			Assert.AreEqual (dto.Name, fooName);
			Assert.AreEqual (dto.Description, fooDescription);
			Assert.IsNotNull (dto.NestedObject);
			Assert.AreEqual (dto.NestedObject.Id, fooReferenceId);
			Assert.AreEqual (dto.NestedObject.NestedName, fooReferenceName);

			//Does not map to a nested object of a nested object - only goes one level deep.
			Assert.IsNull (dto.NestedObject.NestedChild);
		}

		[Test]
		public void SelectNonExistentNestedObjectProperty ()
		{
			string query =
				$" select" +
				$" fooTable.{nameof (FooTable.Id)} as '{nameof (FooDTO.Id)}'," +
				$" fooTable.{nameof (FooTable.FooName)} as '{nameof (FooDTO.Name)}'," +
				$" fooTable.{nameof (FooTable.FooDescription)} as '{nameof (FooDTO.Description)}'," +
				$" fooTableReference.{nameof (FooTableReference.Id)} as '{nameof (FooDTO.NestedObject)}.{nameof (FooNestedObject.Id)}'," +
				$" fooTableReference.{nameof (FooTableReference.ReferenceName)} as '{nameof (FooDTO.NestedObject)}.{nameof (FooNestedObject.NestedName)}'," +
				$" fooTableReference.{nameof (FooTableReference.ReferenceDescription)} as '{nameof (FooDTO.NestedObject)}.Foo'" +
				$" from {nameof (FooTable)} as fooTable" +
				$" join {nameof (FooTableReference)} fooTableReference on fooTable.{nameof (FooTable.FooTableReferenceId)} = fooTableReference.{nameof (FooTableReference.Id)}" +
				$" where fooTable.{nameof (FooTable.Id)} = {fooId}";
			var dtoArray = db.Query<FooDTO> (query);
			var dto = dtoArray[0];

			Assert.AreEqual (dto.Id, fooId);
			Assert.AreEqual (dto.Name, fooName);
			Assert.AreEqual (dto.Description, fooDescription);
			Assert.IsNotNull (dto.NestedObject);
			Assert.AreEqual (dto.NestedObject.Id, fooReferenceId);
			Assert.AreEqual (dto.NestedObject.NestedName, fooReferenceName);
			Assert.IsNull (dto.NestedObject.NestedDescription);
		}
	}
}

