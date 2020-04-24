using System;

namespace Database.SQLite.Modeling
{
	/// <summary>
	/// Abstract superclass for SQLite table constraints.
	/// </summary>
	public abstract class SQLiteTableConstraintAttribute : Attribute
	{
		/// <summary>
		/// Gets the name of the SQLite keyword that is represented by this attribute.
		/// </summary>
		public abstract string Name { get; }
	}

	/// <summary>
	/// Marks a column of a database table model as a PRIMARY KEY.
	/// </summary>
	/// <remarks>
	/// If used on INTEGER types in a table with a ROWID, the property becomes an alias
	/// to the ROWID column.
	/// </remarks>
	/// <seealso cref="https://www.sqlite.org/lang_createtable.html#primkeyconst">
	/// Official documentation</seealso>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class PrimaryAttribute : SQLiteTableConstraintAttribute
	{
		public override string Name { get; } = "PRIMARY KEY";
	}

	/// <summary>
	/// Changes the automatic ROWID assignment algorithm to prevent the reuse of
	/// previous ROWIDs.
	/// </summary>
	/// <remarks>
	/// Only works on INTEGER types.
	/// <para/>
	/// Auto increment should generally be avoided if there is no requirement to prevent the
	/// reuse of ID's. This attribute essentially prevents the insertion of additional elements
	/// if the highest possible ROWID (<see cref="long.MaxValue"/>) was reached.
	/// </remarks>
	/// <seealso cref="https://www.sqlite.org/autoinc.html">Official documentation
	/// </seealso>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class AutoIncrementAttribute : PrimaryAttribute
	{
		public override string Name { get; } = "PRIMARY KEY AUTOINCREMENT";
	}

	/// <summary>
	/// Specifies that the value of a column may not be null.
	/// </summary>
	/// <remarks>
	/// All columns are NULL by default, except for PRIMARY KEYs in WHITHOUT ROWID
	/// tables.
	/// </remarks>
	/// <seealso cref="https://www.sqlite.org/lang_createtable.html#notnullconst">
	/// Official documentation</seealso>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class NotNullAttribute : SQLiteTableConstraintAttribute
	{
		public override string Name { get; } = "NOT NULL";
	}

	/// <summary>
	/// Specifies that all values in a column must be distinct.
	/// </summary>
	/// <seealso cref="https://www.sqlite.org/lang_createtable.html#uniqueconst">
	/// Official documentation</seealso>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class UniqueAttribute : SQLiteTableConstraintAttribute
	{
		public override string Name { get; } = "UNIQUE";
	}

	/// <summary>
	/// Specifies that the table does not have a ROWID column. This class cannot be
	/// inherited.
	/// </summary>
	/// <seealso cref="https://www.sqlite.org/withoutrowid.html">Official
	/// documentation</seealso>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class WithoutRowIdAttribute : Attribute { } // TODO: Improve???? See TODO in PrimaryAttribute
}
