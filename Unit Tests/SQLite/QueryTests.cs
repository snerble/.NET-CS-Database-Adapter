using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Database.SQLite.Models;
using System;
using System.Collections.Generic;

namespace Database.SQLite
{
	/// <summary>
	/// Test class containing test methods that focus on the integrity of all queries.
	/// </summary>
	[TestClass]
	public partial class QueryTests
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
	}
}
