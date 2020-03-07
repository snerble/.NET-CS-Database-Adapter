using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Database.SQLite.Models;

namespace Database.SQLite
{
	/// <summary>
	/// Test class containing test methods that focus on the integrity of all queries.
	/// </summary>
	[TestClass]
	public class QueryTests
	{
		/// <summary>
		/// The path to the database used by the tests in this class.
		/// </summary>
		public const string DatabaseName = "Database.db";
		/// <summary>
		/// Gets <see cref="SQLiteAdapter"/> used by all tests in this class.
		/// </summary>
		public static SQLiteAdapter Database { get; private set; }

		/// <summary>
		/// Gets or sets the <see cref="SQLiteTransaction"/> used for every test.
		/// </summary>
		private SQLiteTransaction TestTransaction { get; set; }

		#region Class Setup
		/// <summary>
		/// Initializes the resources shared by all tests in this class.
		/// </summary>
		[ClassInitialize]
		public static void ClassInit(TestContext _)
		{
			// Remove database file if it exists
			if (File.Exists(DatabaseName))
			{
				File.Delete(DatabaseName);
				Trace.WriteLine("Deleted database at " + Path.GetFullPath(DatabaseName));
			}
			// Open a new connection
			Database = new SQLiteAdapter(DatabaseName);
			Trace.WriteLine($"Opened connection to {Database.Connection.FileName}");
		}
		/// <summary>
		/// Disposes the resources used by all tests in this class.
		/// </summary>
		[ClassCleanup]
		public static void ClassCleanup()
		{
			Database.Dispose();
		}
		#endregion

		#region Test Setup
		[TestInitialize]
		public void TestInit()
		{
			TestTransaction = Database.Connection.BeginTransaction();
		}
		[TestCleanup]
		public void TestCleanup()
		{
			TestTransaction.Dispose();
		}
		#endregion

		[TestMethod]
		public void CreateTable()
		{
			Database.CreateTable<TestModel>();
		}

		[TestMethod]
		public void Insert()
		{
			Database.CreateTable<TestModel>();

			// Populate the database with test data
			var newItems = new TestModel[10];
			for (int i = 0; i < newItems.Length; i++)
			{
				newItems[i] = new TestModel()
				{
					TextField = "InsertTest",
					NumberField = i
				};
			}
			Database.Insert<TestModel>(newItems);
		}

		[TestMethod]
		public void Select()
		{
			Database.CreateTable<TestModel>();

			// Populate the database with test data
			var newItems = new TestModel[10];
			for (int i = 0; i < newItems.Length; i++)
			{
				newItems[i] = new TestModel()
				{
					TextField = i.ToString(),
					NumberField = i
				};
			}
			Database.Insert<TestModel>(newItems);

			// Retrieve the items again
			var items = Database.Select<TestModel>().ToList();

			Assert.AreEqual(newItems.Length, items.Count,
				$"The SELECT query did not return the expected amount of elements.");
		}

		[TestMethod]
		public void Delete()
		{
			Database.CreateTable<TestModel>();

			// Populate the database with test data
			var newItems = new TestModel[10];
			for (int i = 0; i < newItems.Length; i++)
			{
				newItems[i] = new TestModel()
				{
					TextField = "DeleteTest",
					NumberField = i
				};
			}
			Database.Insert<TestModel>(newItems);

			// Delete all those items
			int deletedCount = Database.Delete<TestModel>("`TextField` = 'DeleteTest'");

			// Check if the query deleted the correct amount of elements
			Assert.AreEqual(newItems.Length, deletedCount, $"The query did not delete the correct amount of elements.");
		}

		[TestMethod]
		public void Update()
		{
			Database.CreateTable<TestModel>();

			// Populate the database with test data
			var newItems = new TestModel[10];
			for (int i = 0; i < newItems.Length; i++)
			{
				newItems[i] = new TestModel()
				{
					NumberField = i
				};
			}
			Database.Insert<TestModel>(newItems);

			// Update the text of the new items
			foreach (var item in newItems)
			{
				item.TextField = "UpdateTest";
			}
			
			// Update the database entries with the modified versions here
			var updatedCount = Database.Update<TestModel>(newItems);
			Trace.WriteLine(updatedCount);

			// Check if the correct amount of rows were affected
			Assert.AreEqual(newItems.Length, updatedCount, $"The query did not update the correct amount of elements.");
		}
	}
}
