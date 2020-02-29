using System;

namespace Database.Modeling
{
	/// <summary>
	/// Specifies the metadata of a database table. This class cannot be inherited.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class TableAttribute : Attribute
	{
		/// <summary>
		/// The name of of the table.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="TableAttribute"/> with the specified name.
		/// </summary>
		/// <param name="name">The name to assign to the table.</param>
		public TableAttribute(string name)
		{
			Name = name;
		}
	}
}
