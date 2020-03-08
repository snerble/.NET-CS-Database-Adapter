using System;
using MySql.Data.MySqlClient;

namespace Database.MySQL.Modeling
{
	/// <summary>
	/// Specifies the metadata of a database column,
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class ColumnAttribute : Attribute
	{
		/// <summary>
		/// Gets name of the column.
		/// </summary>
		public string Name { get; }
		/// <summary>
		/// Gets the data type of the column.
		/// </summary>
		public MySqlDbType? Type { get; set; } = null;
		/// <summary>
		/// Gets the maximum length of the data in the column.
		/// </summary>
		public int Length { get; set; } = -1;

		/// <summary>
		/// Initializes a new <see cref="ColumnAttribute"/> instance with the specified name.
		/// </summary>
		/// <param name="name">The name of the column.</param>
		public ColumnAttribute(string name)
		{
			Name = name;
		}
	}
}
