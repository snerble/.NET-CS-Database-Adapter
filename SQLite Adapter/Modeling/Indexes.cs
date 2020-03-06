using System;

namespace Database.SQLite.Modeling
{
	/// <summary>
	/// Abstract superclass for SQLite table column modifiers.
	/// </summary>
	public abstract class SQLiteColumnKeywordAttribute : Attribute
	{
		/// <summary>
		/// Gets the name of the SQLite keyword that is represented by this attribute.
		/// </summary>
		public abstract string Name { get; }
	}

	/// <summary>
	/// Marks a column of a database table model as PRIMARY.
	/// </summary>
	/// <remarks>
	/// Official documentation: https://www.sqlite.org/lang_createtable.html#primkeyconst
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class PrimaryAttribute : SQLiteColumnKeywordAttribute
	{
		public override string Name { get; } = "PRIMARY KEY";
	}

	/// <summary>
	/// Marks a column of a database table model as AUTOINCREMENT and PRIMARY.
	/// </summary>
	/// <remarks>
	/// Only works on INTEGER types.
	/// Official documentation: https://www.sqlite.org/autoinc.html
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class AutoIncrementAttribute : PrimaryAttribute
	{
		public override string Name { get; } = "PRIMARY KEY AUTOINCREMENT";
	}

	/// <summary>
	/// Marks a column of a database table model as NOT NULL.
	/// </summary>
	/// <remarks>
	/// All columns are nullable by default, hence the lack of a NullAttribute.
	/// Official documentation: https://www.sqlite.org/lang_createtable.html#notnullconst
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class NotNullAttribute : SQLiteColumnKeywordAttribute
	{
		public override string Name { get; } = "NOT NULL";
	}

	/// <summary>
	/// Marks a column of a database table model as UNIQUE.
	/// </summary>
	/// <remarks>.
	/// Official documentation: https://www.sqlite.org/lang_createtable.html#uniqueconst
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class UniqueAttribute : SQLiteColumnKeywordAttribute
	{
		public override string Name { get; } = "UNIQUE";
	}
}
