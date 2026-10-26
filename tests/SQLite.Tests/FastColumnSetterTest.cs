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
	public class FastColumnSetterTest
	{
		public interface IEntity
		{
			int Id { get; set; }
			string Name { get; set; }
		}

		public class BaseEntity : IEntity
		{
			[PrimaryKey]
			public int Id { get; set; }

			public string Name { get; set; }
		}

		public class VirtualEntity : BaseEntity
		{
			public virtual int? Count { get; set; }

			public virtual DateTime Date { get; set; }
		}

		static VirtualEntity CreateEntity ()
		{
			return new VirtualEntity {
				Id = 1,
				Name = "Hello",
				Count = 42,
				Date = new DateTime (2026, 10, 22, 11, 28, 39),
			};
		}

		static void AssertEntity (VirtualEntity expected, VirtualEntity actual)
		{
			Assert.AreEqual (expected.Id, actual.Id);
			Assert.AreEqual (expected.Name, actual.Name);
			Assert.AreEqual (expected.Count, actual.Count);
			Assert.AreEqual (expected.Date, actual.Date);
		}

		[Test]
		public void SetterDelegatesAreReusedAcrossQueries ()
		{
			var db = new TestDb ();
			db.CreateTable<VirtualEntity> ();
			var expected = CreateEntity ();
			db.Insert (expected);

			var map = db.GetMapping<VirtualEntity> ();

			AssertEntity (expected, db.Query<VirtualEntity> ("select * from VirtualEntity").Single ());
			var delegates = map.Columns.Select (c => c.SetPropertyDelegate).ToArray ();
			Assert.That (delegates, Has.None.Null);

			AssertEntity (expected, db.Query<VirtualEntity> ("select * from VirtualEntity").Single ());
			AssertEntity (expected, (VirtualEntity)db.Query (map, "select * from VirtualEntity").Single ());

			for (var i = 0; i < map.Columns.Length; i++) {
				Assert.AreSame (delegates[i], map.Columns[i].SetPropertyDelegate, map.Columns[i].Name);
			}
		}

		[Test]
		public void SetterDelegatesAreSharedAcrossConnectionsWithDifferentSettings ()
		{
			var expected = CreateEntity ();

			var ticksDb = new TestDb (storeDateTimeAsTicks: true);
			ticksDb.CreateTable<VirtualEntity> ();
			ticksDb.Insert (expected);

			var textDb = new TestDb (storeDateTimeAsTicks: false);
			textDb.CreateTable<VirtualEntity> ();
			textDb.Insert (expected);

			AssertEntity (expected, ticksDb.Query<VirtualEntity> ("select * from VirtualEntity").Single ());
			AssertEntity (expected, textDb.Query<VirtualEntity> ("select * from VirtualEntity").Single ());
			AssertEntity (expected, ticksDb.Query<VirtualEntity> ("select * from VirtualEntity").Single ());

			Assert.AreSame (ticksDb.GetMapping<VirtualEntity> (), textDb.GetMapping<VirtualEntity> ());
		}
	}
}
