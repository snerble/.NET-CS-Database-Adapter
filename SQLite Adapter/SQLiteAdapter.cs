using Database.SQLite.Modeling;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Database.SQLite
{
	public partial class SQLiteAdapter : IDbAdapter
	{
		/// <summary>
		/// Gets the <see cref="SQLiteConnection"/> used by this <see cref="SQLiteAdapter"/>.
		/// </summary>
		public SQLiteConnection Connection { get; }

		/// <summary>
		/// Gets or sets whether inserted objects automatically get assigned their new
		/// row ID. True by default.
		/// </summary>
		/// <remarks>
		/// Only applies to row ID tables that have an INTEGER PRIMARY KEY column.
		/// </remarks>
		/// <seealso cref="https://www.sqlite.org/lang_createtable.html">ROWID tables</seealso>
		/// <seealso cref="https://www.sqlite.org/lang_createtable.html#rowid">ROWID and INTEGER PRIMARY KEY</seealso>
		public bool AutoAssignRowId { get; set; } = true;
		/// <summary>
		/// Gets or sets whether <see cref="Enum"/> types will be stored as text or as int.
		/// </summary>
		/// <remarks>
		/// As long as the numeric values or names of an enum don't change, this <see cref="SQLiteAdapter"/>
		/// will still be able to parse both text and int enum values regardless of this setting.
		/// <para/>
		/// Enum values that are combinations of other enum values only get converted to text
		/// if they have the FlagAttribute. Otherwise they will still be stored as an int.
		/// </remarks>
		public bool StoreEnumsAsText { get; set; } = true;

		/// <summary>
		/// Initializes a new instance of <see cref="SQLiteAdapter"/>.
		/// </summary>
		/// <param name="datasource">The path to the database file to use.</param>
		public SQLiteAdapter(string datasource)
		{
			if (datasource is null)
				throw new ArgumentNullException(nameof(datasource));

			Connection = new SQLiteConnection($"Data Source={datasource};foreign keys=true");
			Connection.Open();
		}

		/// <inheritdoc cref="CreateTable(Type)"/>
		public void CreateTable<T>() => CreateTable(typeof(T));
		/// <summary>
		/// Creates a new table that represents the given type.
		/// </summary>
		public void CreateTable(Type type)
		{
			PropertyInfo[] columns = Utils.GetProperties(type).ToArray();

			// Build the entire query
			var sb = new StringBuilder("CREATE TABLE ");
			sb.Append(Utils.GetTableName(type));
			sb.Append(" (");

			// Build the columns
			bool first = true;
			foreach (PropertyInfo column in columns)
			{
				// Omit the comma for the first entry
				if (!first)
					sb.Append(',');

				// TODO: Implement overwritable column data
				//var columnData = Utils.GetColumnData(column);
				sb.Append(column.Name);
				sb.Append(' ');
				sb.Append(TypeMapping.GetType(column.PropertyType));

				// Concatenate the column modifiers
				foreach (SQLiteTableConstraintAttribute columnModifier in column.GetCustomAttributes<SQLiteTableConstraintAttribute>(false))
				{
					sb.Append(' ');
					sb.Append(columnModifier.Name);
				}
				first = false;
			}
			sb.Append("\n)");

			// Append WITHOUT ROWID if the attribute is specified
			if (type.GetCustomAttribute<WithoutRowIdAttribute>() != null)
				sb.Append("WITHOUT ROWID");

			sb.Append(';');

			// Create the command and execute it
			using var command = new SQLiteCommand(Connection) { CommandText = sb.ToString() };
			command.ExecuteNonQuery();
		}
		/// <inheritdoc cref="TryCreateTable(Type)"/>
		public bool TryCreateTable<T>() => TryCreateTable(typeof(T));
		/// <summary>
		/// Attempts to create a table for the specified type if it does not exist.
		/// </summary>
		public bool TryCreateTable(Type type)
		{
			try
			{
				CreateTable(type);
				return true;
			}
			catch (SQLiteException)
			{
				return false;
			}
		}

		/// <inheritdoc cref="DropTable(Type)"/>
		public void DropTable<T>() => DropTable(typeof(T));
		/// <summary>
		/// Drops the table for the specified type.
		/// </summary>
		public void DropTable(Type type)
		{
			using var command = new SQLiteCommand(Connection) { CommandText = $"DROP TABLE `{Utils.GetTableName(type)}`" };
			command.ExecuteNonQuery();
		}
		/// <inheritdoc cref="TryDropTable(Type)"/>
		public bool TryDropTable<T>() => TryDropTable(typeof(T));
		/// <summary>
		/// Attempts to drop the table for the specified type.
		/// </summary>
		/// <returns>True if the table was successfully dropped. Otherwise false.</returns>
		public bool TryDropTable(Type type)
		{
			try
			{
				DropTable(type);
				return true;
			}
			catch (SQLiteException)
			{
				return false;
			}
		}

		public virtual int Delete<T>(string condition)
		{
			// Notify the deleting event
			OnDeleting<T>(condition);

			using var command = new SQLiteCommand(Connection) { CommandText = $"DELETE FROM {Utils.GetTableName<T>()} WHERE {condition}" };

			return command.ExecuteNonQuery();
		}
		public int Delete<T>(T item) => Delete<T>(new T[] { item });
		public virtual int Delete<T>(IList<T> items)
		{
			if (!items.Any())
				return 0;

			// Notify the deleting event
			OnDeleting(items);

			using var command = new SQLiteCommand(Connection);
			#region Query Building
			var sb = new StringBuilder("DELETE FROM");
			sb.Append(Utils.GetTableName<T>());
			sb.Append("WHERE(");

			// Try to get the primary key properties. Otherwise just get all properties
			PropertyInfo[] properties = Utils.GetProperties<T, PrimaryAttribute>().ToArray();
			if (!properties.Any())
				properties = Utils.GetProperties<T>().ToArray();

			// Build conditions for every property and item
			for (int i = 0; i < properties.Length; ++i)
			{
				var property = properties[i];

				if (i != 0)
					sb.Append(") AND (");

				// Combine all conditions and simultaneously create SQLiteParameter objects for every
				// distinct value of this property
				sb.AppendJoin(" OR ", items.Select(x => property.GetValue(x)).Distinct().Select((value, j) =>
				{
					// Skip creating an SQLiteParameter if the value is null
					if (value == null)
						return $"{property.Name} IS NULL";

					var paramName = $"@{i}_{j}";
					command.Parameters.Add(new SQLiteParameter(paramName, value));

					// Return a comparison between the column and the new parameter
					return $"{property.Name} = {paramName}";
				}));
			}
			sb.Append(");");
			command.CommandText = sb.ToString();
			#endregion

			// Execute the command and return the amount of affected rows
			return command.ExecuteNonQuery();
		}

		public long Insert<T>(T item) => Insert<T>(new T[] { item });
		public virtual long Insert<T>(IList<T> items)
		{
			if (items.Count == 0)
				return -1;

			// Notify the inserting event
			OnInserting(items);

			var tableName = Utils.GetTableName<T>();
			PropertyInfo[] properties = Utils.GetProperties<T>().ToArray();
			// Get the rowid property (if it is null, the query will be shortened)
			PropertyInfo rowid = Utils.GetRowIdProperty<T>(properties);
			using var command = new SQLiteCommand(Connection);

			#region Query Building
			var sb = new StringBuilder();

			// Begin with another command to get the highest ROWID if AutoAssignRowId is false
			// or if there is no rowid property
			if (AutoAssignRowId && rowid != null)
			{
				sb.Append("SELECT MAX(ROWID) FROM");
				sb.Append(tableName);
				sb.Append("LIMIT 1;");
			}

			// Begin actual insert query
			sb.Append("INSERT INTO");
			sb.Append(tableName);
			sb.Append('(');
			// Join all column names with commas in between
			sb.Append(string.Join(',', properties.Select(x => x.Name)));
			sb.Append(")VALUES");

			// Build the parameter section for every given element.
			int i = 0;
			foreach (T item in items)
			{
				if (i != 0)
					sb.Append(',');
				sb.Append('(');

				// Append the column parameter names and simultaneously create the SQLiteParameter objects
				sb.AppendJoin(',', properties.Select((x, j) =>
				{
					var value = x.GetValue(item);
					// Skip creating the SQLiteParameter if the value is null
					if (value == null)
						return "NULL";

					// Cast enum values to int or string
					if (x.PropertyType.IsEnum)
					{
						if (StoreEnumsAsText)
							value = value.ToString();
						else
							value = (int)value;
					}

					var paramName = $"@{j}_{i}";
					command.Parameters.Add(new SQLiteParameter(paramName, value));
					return paramName;
				}));

				sb.Append(')');
				++i;
			}
			sb.Append(';');

			// Add another query to get the scalar if false or if there is no rowid property
			if (!AutoAssignRowId || rowid == null)
				sb.Append("SELECT LAST_INSERT_ROWID();");
			command.CommandText = sb.ToString();
			#endregion

			// Execute the command and return the scalar with the max ROWID
			object _ = command.ExecuteScalar();
			var scalar = (long)(_ == DBNull.Value ? 0L : _); // DBNull gets replaced with 0

			// Skip assigning rowids if false
			if (!AutoAssignRowId)
				return scalar;

			// Assign the scalar for every inserted element if there is a ROWID property
			if (rowid != null)
			{
				++scalar;
				foreach (T item in items)
				{
					object oldValue = rowid.GetValue(item);
					// Skip the rowid assigning if it is not null and recalculate the scalar if necessary
					if (oldValue != null)
					{
						long oldRowId = Convert.ToInt64(oldValue);
						if (oldRowId >= scalar)
							scalar = oldRowId + 1;
						continue;
					}
					// Change the type of the scalar to the type of the rowid property and set it
					rowid.SetValue(item, Convert.ChangeType(scalar++, Nullable.GetUnderlyingType(rowid.PropertyType) ?? rowid.PropertyType));
				}
			}

			return scalar;
		}

		public IEnumerable<T> Select<T>() where T : new() => Select<T>("1");
		public virtual IEnumerable<T> Select<T>(string condition) where T : new()
		{
			if (string.IsNullOrEmpty(condition))
				throw new ArgumentException("Value may not be empty or null.", nameof(condition));

			// Notify the selecting event
			OnSelecting<T>(condition);

			// Create the command
			using var command = new SQLiteCommand(Connection) { CommandText = $"SELECT * FROM {Utils.GetTableName<T>()} WHERE {condition}" };

			// Execute the command
			using SQLiteDataReader reader = command.ExecuteReader();

			// Map the reader's resultset to type T and yield it's results.
			// Using a foreach here to keep the reader alive untill the generator is done.
			foreach (T item in Utils.ParseReader<T>(reader))
				yield return item;
		}

		public int Update<T>(T item) => Update<T>(new T[] { item });
		public virtual int Update<T>(IList<T> items)
		{
			// Notify the updating event
			OnUpdating(items);

			// Reset command abort flag
			aborting = false;

			if (!items.Any())
				return 0;

			PropertyInfo[] properties = Utils.GetProperties<T>().ToArray();

			// Try to get the primary key attributes. If empty, throw an exception
			PropertyInfo[] primaries = Utils.GetProperties<PrimaryAttribute>(properties).ToArray();
			if (!primaries.Any())
				throw new ArgumentException("The given generic type does not contain a primary key property.", "T");

			// Remove the primary properties from the properties list
			properties = properties.Except(primaries).ToArray();

			var tableName = Utils.GetTableName<T>();
			using var command = new SQLiteCommand(Connection);

			#region Query Building
			var sb = new StringBuilder();
			int i = 0;
			foreach (T item in items)
			{
				sb.Append("UPDATE");
				sb.Append(tableName);
				sb.Append("SET ");

				// Append the setters for every non-primary-key property and simultaneously create
				// the SQLiteParameter objects
				sb.AppendJoin(',', properties.Select((x, j) =>
				{
					var value = x.GetValue(item);
					// Skip creating the SQLiteParameter if the value is null
					if (value is null)
						return $"{x.Name}=NULL";

					var paramName = $"@{i}_{j}";
					command.Parameters.Add(new SQLiteParameter(paramName, value));

					// Return the setter for this column
					return $"{x.Name}={paramName}";
				}));
				sb.Append(" WHERE ");

				// Append the conditions that match the primary key values of the item
				sb.AppendJoin(" AND ", primaries.Select((x, j) =>
				{
					var value = x.GetValue(item);
					// Skip creating an SQLiteParameter if the value is null
					if (value == null)
						return $"{x.Name} IS NULL";

					var paramName = $"@{i}_c{j}";
					command.Parameters.Add(new SQLiteParameter(paramName, value));

					// Return a comparison between the column and the new parameter
					return $"{x.Name}={paramName}";
				}));

				sb.Append(';');
				++i;
			}
			command.CommandText = sb.ToString();
			#endregion

			// Execute the command and return the amount of affected rows
			return command.ExecuteNonQuery();
		}

		public void Dispose()
		{
			Connection.Dispose();
		}
	}
}
