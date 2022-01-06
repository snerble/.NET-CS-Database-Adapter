using Database.SQLite.Modeling;
using System;
using System.Collections.Generic;
using System.Text;

namespace Database.SQLite
{
	/// <summary>
	/// Object representation of the sqlite_master table.
	/// </summary>
	[Table("sqlite_master")]
	internal sealed class SQLiteMaster
	{
		public DbObjectType Type { get; set; }
		public string Name { get; set; }
		public string Tbl_Name { get; set; }
		public long RootPage { get; set; }
		public string SQL { get; set; }
	}

	/// <summary>
	/// Represents the 4 types of an sqlite database object found in sqlite_master.
	/// </summary>
	internal enum DbObjectType
	{
		table,
		index,
		trigger,
		view
	}
}
