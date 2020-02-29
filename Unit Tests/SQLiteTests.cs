using Database.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTesting
{
	[TestClass]
	public class SQLiteTests
	{
		/// <summary>
		/// Gets <see cref="SQLiteAdapter"/> used by all tests in this class.
		/// </summary>
		public static SQLiteAdapter Database { get; private set; }

		#region Class Setup
		/// <summary>
		/// Initializes the resources shared by all tests in this class.
		/// </summary>
		[ClassInitialize]
		public void ClassInit(TestContext _)
		{
			Database = new SQLiteAdapter("Unit Test Database.db");
		}

		/// <summary>
		/// Disposes the resources used by all tests in this class.
		/// </summary>
		[ClassCleanup]
		public void ClassCleanup()
		{
			Database.Dispose();
		} 
		#endregion
	}
}
