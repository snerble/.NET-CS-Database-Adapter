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
	/// If used on INTEGER types, this attribute becomes an alias 
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
	/// <para/>
	/// Auto increment should generally be avoided if there is no requirement to prevent the
	/// reuse of ID's. This attribute essentially prevents the insertion of additional elements
	/// if the highest possible ROWID (<see cref="long.MaxValue"/>) was reached.
	/// <para/>
	/// Official documentation: https://www.sqlite.org/autoinc.html
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class AutoIncrementAttribute : PrimaryAttribute
	{
		// TODO: Add an `AutoAssignRowId` property that toggles the assigning of the rowid
		// in insert queries. Consider if it should be in the TableAttribute or in the SQLiteDbAdapter
		public override string Name { get; } = "PRIMARY KEY AUTOINCREMENT";
	}

	/// <summary>
	/// Specifies that the value of the column may not be null.
	/// Marks a column of a database table model as NOT NULL.
	/// </summary>
	/// <remarks>
	/// All columns are nullable by default, hence the lack of a NullAttribute.
	/// <para/>
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
