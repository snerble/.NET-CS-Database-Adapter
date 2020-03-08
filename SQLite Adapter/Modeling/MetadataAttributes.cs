using System;

/* 
 * This file contains all attributes allow the implicit metatada to be
 * overwritten, E.G: Table name, column name and column type.
 */

namespace Database.SQLite.Modeling
{
	/// <summary>
	/// Specifies the name of a type's table. This class cannot be inherited.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
	public sealed class TableAttribute : Attribute
	{
		/// <summary>
		/// Gets the name of the table.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="TableAttribute"/> with the specified name.
		/// </summary>
		/// <param name="name">The name of the table.</param>
		public TableAttribute(string name)
		{
			Name = name;
		}
	}
}
