using System;

namespace Database.MySQL.Modeling
{
	/// <summary>
	/// Marks a column of a database table model as PRIMARY.
	/// </summary>
	/// <remarks>
	/// Primary keys must be unique and are used for foreign key constraints.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class PrimaryAttribute : Attribute { }

	/// <summary>
	/// Marks a column of a database table model as Auto Increment and PRIMARY.
	/// </summary>
	/// <remarks>
	/// Auto increment columns must be of a numerical type. Every entry will have a
	/// primary value incremented once from the previous.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class AutoIncrementAttribute : PrimaryAttribute { }

	/// <summary>
	/// Marks a column of a database table model as UNIQUE.
	/// </summary>
	/// <remarks>
	/// Unique keys demand that no other entry may share it's value, or else an error is raised.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class UniqueAttribute : Attribute { }

	/// <summary>
	/// Marks a column of a database table model as INDEX.
	/// </summary>
	/// <remarks>
	/// Indexes improve the speed of queries by indexing the rows based on duplicate values.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class IndexAttribute : Attribute { }

	/// <summary>
	/// Marks a column of a database table model as FULLTEXT.
	/// </summary>
	/// <remarks>
	/// Fulltext indexes improve the speed of queries using fulltext searches using the MATCH() or AGAINST()
	/// functions.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class FulltextAttribute : Attribute { }


}
