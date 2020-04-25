using Database.SQLite.Modeling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.SQLite;
using System.Diagnostics;

namespace Database.SQLite
{
	public partial class QueryTests
	{
		[TestMethod]
		public void TestForeignKeyDefinition()
		{
			try
			{
				Database.CreateTable<ForeignKeyTest>();
			}
			catch (SQLiteException e) when (e.Message.ToLower().Contains("no such table"))
			{
				return;
			}

			Assert.Fail("The simulated SQL error did not throw an exception.");
		}

		[TestMethod]
		public void TestForeignKeys()
		{
			Database.CreateTable<TestReference>();
			Database.CreateTable<ForeignKeyTest>();

			Database.Insert<TestReference>(new[] {
				new TestReference(),
				new TestReference(),
				new TestReference(),
				new TestReference(),
			});

			Database.Insert<ForeignKeyTest>(new[]
			{
				new ForeignKeyTest() { ReferenceID = 1 },
				new ForeignKeyTest() { ReferenceID = 2 },
				new ForeignKeyTest() { ReferenceID = 3 },
				new ForeignKeyTest() { ReferenceID = 4 },
			});

			try
			{
				Database.Insert<ForeignKeyTest>(new[]
					{
				new ForeignKeyTest() { ReferenceID = -1 }
			});
			}
			catch (SQLiteException e) when (e.Message.ToLower().Contains("constraint failed"))
			{
				return;
			}

			Assert.Fail("The simulated constraint violation did not throw an exception.");
		}
	}

	internal class TestReference
	{
		[Primary]
		public int? ID { get; set; }
	}

	internal class ForeignKeyTest
	{
		[Unique]
		[ForeignKey(typeof(TestReference))]
		public int ReferenceID { get; set; }
	}
}
