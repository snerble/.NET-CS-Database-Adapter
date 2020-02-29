using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;

namespace Database.SQLite
{
	[TestClass]
	public class SQLiteTests
	{
		/// <summary>
		/// The path to the database used by the tests in this class.
		/// </summary>
		public const string DatabaseName = "Database.db";

		/// <summary>
		/// Gets <see cref="SQLiteAdapter"/> used by all tests in this class.
		/// </summary>
		public static SQLiteAdapter Database { get; private set; }

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

		[TestMethod]
		public void CreateTable()
		{
			var command = Database.Connection.CreateCommand();
			command.CommandText = @"CREATE TABLE test (
				id INTEGER PRIMARY KEY,
				text_field STRING
			);";
			command.ExecuteNonQuery();
		}
	}
}
