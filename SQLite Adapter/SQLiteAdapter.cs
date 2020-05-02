using Database.SQLite.Modeling;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
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
			var columns = Utils.GetProperties(type).ToList();

			// Build the entire query
			var sb = new StringBuilder("CREATE TABLE ");
			sb.Append(Utils.GetTableName(type));
			sb.Append(" (");

			// Build the columns
			bool first = true;
			foreach (PropertyInfo column in new List<PropertyInfo>(columns)) // Clone the list to prevent exceptions
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
					// Primary keys are appended seperately
					if (columnModifier.GetType() == typeof(PrimaryAttribute))
						continue;

					if (columnModifier is ForeignKeyAttribute foreignKey)
					{
						string referenceTableName = Utils.GetTableName(foreignKey.ReferenceType);
						PropertyInfo[] referencedPrimaries = Utils.GetProperties<PrimaryAttribute>(Utils.GetProperties(foreignKey.ReferenceType)).ToArray();

						// Check if table exists
						if (!Select<SQLiteMaster>("`name` = @name", new { name = referenceTableName[1..^1] }).Any())
							throw new SQLiteException($"No such table {referenceTableName}");
						// Throw exception if the referenced class has no primary keys
						if (!referencedPrimaries.Any())
							throw new ArgumentException($"The referenced table {referenceTableName} has no primary keys.");
						// Throw NotImplementedException if the referenced table has composite primary keys
						if (referencedPrimaries.Length > 1)
							throw new NotImplementedException();

						sb.Append(" REFERENCES");
						sb.Append(referenceTableName);
						sb.Append('(');
						sb.Append(referencedPrimaries.First().Name);
						sb.Append(')');
					}
					else
					{
						sb.Append(' ');
						sb.Append(columnModifier.Name);
					}
					// Remove the property to avoid later reuse
					columns.Remove(column);
				}
				first = false;
			}

			PropertyInfo[] primaries = Utils.GetProperties<PrimaryAttribute>(columns).ToArray();
			if (primaries.Any())
			{
				sb.Append(',');
				sb.Append(new PrimaryAttribute().Name); // Create new instance to avoid more hardcoded strings
				sb.Append('(');
				sb.AppendJoin(',', primaries.Select(x => x.Name));
				sb.Append(')');
			}

			sb.Append(')');

			// Append WITHOUT ROWID if the attribute is specified
			if (type.GetCustomAttribute<WithoutRowIdAttribute>() != null)
				sb.Append("WITHOUT ROWID");

			sb.Append(';');

			// Create the command and execute it
			using var command = new SQLiteCommand(Connection) { CommandText = sb.ToString() };
			command.ExecuteNonQuery();
		}
		/// <inheritdoc cref="CreateTableIfNotExists(Type)"/>
		public bool CreateTableIfNotExists<T>() => CreateTableIfNotExists(typeof(T));
		/// <summary>
		/// Creates a table for the specified type if it doesnt exist.
		/// </summary>
		public bool CreateTableIfNotExists(Type type)
		{
			if (Select<SQLiteMaster>("`name` = @name", new { name = Utils.GetTableName(type)[1..^1] }).Any())
				return false;

			CreateTable(type);
			return true;
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
		/// <inheritdoc cref="DropTableIfExists(Type)"/>
		public bool DropTableIfExists<T>() => DropTableIfExists(typeof(T));
		/// <summary>
		/// Drops the table of the specified <paramref name="type"/> if it exists.
		/// Otherwise does nothing.
		/// </summary>
		/// <returns>True if the table was dropped. Otherwise false.</returns>
		public bool DropTableIfExists(Type type)
		{
			if (!Select<SQLiteMaster>("`name` = @name", new { name = Utils.GetTableName(type)[1..^1] }).Any())
				return false;

			DropTable(type);
			return true;
		}

		public int Delete<T>(string condition) => Delete<T>(condition, null);
		public virtual int Delete<T>(string condition, [AllowNull] object param)
		{
			// Notify the deleting event
			OnDeleting<T>(condition);

			using var command = new SQLiteCommand(Connection) { CommandText = $"DELETE FROM {Utils.GetTableName<T>()} WHERE {condition}" };

			// Turn the properties of param into SQLiteParameters
			if (param != null)
				foreach (PropertyInfo prop in Utils.GetProperties(param.GetType()))
					command.Parameters.Add(new SQLiteParameter($"@{prop.Name}", prop.GetValue(param)));

			return command.ExecuteNonQuery();
		}
		public int Delete<T>(T item) => Delete<T>(new[] { item });
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

			// Use the Insert_WithAutoIncrement when an auto increment column is present or if AutoAssignRowId
			// is false (this is because Insert_WithAutoIncrement is more efficient)
			if (!(Utils.GetProperties<T, AutoIncrementAttribute>().FirstOrDefault() is null) || !AutoAssignRowId)
				return Insert_WithAutoIncrement(items);
			else
				return Insert_NoAutoIncrement(items);
		}

		/// <summary>
		/// Insert query variant that performs one query for every item in the collection.
		/// </summary>
		private long Insert_NoAutoIncrement<T>(IList<T> items)
		{
			var tableName = Utils.GetTableName<T>();
			PropertyInfo[] properties = Utils.GetProperties<T>().ToArray();
			// Get the rowid property (if it is null, this will skip the rowid assignment step)
			PropertyInfo rowid = AutoAssignRowId ? Utils.GetRowIdProperty<T>(properties) : null;

			long scalar = -1;
			var sb = new StringBuilder();
			using var command = new SQLiteCommand(Connection);

			// Since this area uses a transaction, it should be locked in it's entirety to prevent issues
			SQLiteTransaction transaction = null;
			if (Connection.AutoCommit)
			{
				transaction = Connection.BeginTransaction();
				// Lock using Monitor.Enter since the lock may not always be acquired
				System.Threading.Monitor.Enter(Connection);
			}

			try
			{
				foreach (T item in items)
				{
					// Reset the command and StringBuilder for reuse
					if (sb.Length != 0)
					{
						sb.Clear();
						command.Reset();
					}

					#region Query Building
					sb.Append("INSERT INTO");
					sb.Append(tableName);
					sb.Append("VALUES(");

					// Append the column parameter names and simultaneously create the SQLiteParameter objects
					sb.AppendJoin(',', properties.Select((x, i) =>
					{
						var value = x.GetValue(item);
						// Skip creating the SQLiteParameter if the value is null
						if (value is null)
							return "NULL";

						// Cast enum values to int or string
						Type type = Nullable.GetUnderlyingType(x.PropertyType) ?? x.PropertyType;
						if (type.IsEnum)
						{
							if (StoreEnumsAsText)
								value = value.ToString();
							else
								value = (int)value;
						}

						var paramName = $"@{i}";
						command.Parameters.Add(new SQLiteParameter(paramName, value));
						return paramName;
					}));
					sb.Append(");");

					command.CommandText = sb.ToString();
					#endregion

					command.ExecuteNonQuery();
					scalar = Connection.LastInsertRowId;

					// Assign the rowid value if the rowid is not null
					rowid?.SetValue(item, Convert.ChangeType(scalar, Nullable.GetUnderlyingType(rowid.PropertyType) ?? rowid.PropertyType));
				}
			}
			finally
			{
				if (!(transaction is null))
				{
					transaction.Commit();
					System.Threading.Monitor.Exit(Connection);
				}
			}

			return scalar;
		}
		private long Insert_WithAutoIncrement<T>(IList<T> items)
		{
			var tableName = Utils.GetTableName<T>();
			PropertyInfo[] properties = Utils.GetProperties<T>().ToArray();
			// Get the rowid property (if it is null, this will skip the rowid assignment step)
			PropertyInfo rowid = AutoAssignRowId ? Utils.GetRowIdProperty<T>(properties) : null;

			using var command = new SQLiteCommand(Connection);

			#region Query Building
			bool autoCommit = Connection.AutoCommit;

			var sb = new StringBuilder();
			if (!(rowid is null))
			{
				sb.Append("SELECT seq FROM sqlite_sequence WHERE name = @tableName;");
				command.Parameters.Add(new SQLiteParameter("@tableName", tableName[1..^1]));
			}

			sb.Append("INSERT INTO");
			sb.Append(tableName);
			sb.Append("VALUES");

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
					if (value is null)
						return "NULL";

					// Cast enum values to int or string
					Type type = Nullable.GetUnderlyingType(x.PropertyType) ?? x.PropertyType;
					if (type.IsEnum)
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
			#endregion

			// Execute the command and get the scalar with the last AutoIncrement ID
			long scalar;
			lock (Connection)
			{
				if (Connection.AutoCommit)
				{
					sb.Insert(0, "BEGIN EXCLUSIVE;");
					sb.Append("COMMIT;");
				}

				command.CommandText = sb.ToString();
				object res = command.ExecuteScalar() ?? 0L;

				scalar = res == DBNull.Value ? 0 : (long)res;
			}

			// Assign the scalar for every inserted element if there is a ROWID property
			if (rowid != null)
			{
				foreach (T item in items)
				{
					object oldValue = rowid.GetValue(item);
					// Skip the rowid assigning if it is not null and recalculate the scalar if necessary
					if (oldValue != null)
					{
						long oldRowId = Convert.ToInt64(oldValue);
						if (oldRowId >= scalar)
							scalar = oldRowId;
						continue;
					}
					// Change the type of the scalar to the type of the rowid property and set it
					rowid.SetValue(item, Convert.ChangeType(++scalar, Nullable.GetUnderlyingType(rowid.PropertyType) ?? rowid.PropertyType));
				}
			}

			return scalar;
		}

		public IEnumerable<T> Select<T>() where T : new() => Select<T>("1");
		public IEnumerable<T> Select<T>(string condition) where T : new()
			=> Select<T>(condition, null);
		public virtual IEnumerable<T> Select<T>(string condition, [AllowNull] object param) where T : new()
		{
			if (string.IsNullOrEmpty(condition))
				throw new ArgumentException("Value may not be empty or null.", nameof(condition));

			// Notify the selecting event
			OnSelecting<T>(condition);

			// Create the command
			using var command = new SQLiteCommand(Connection) { CommandText = $"SELECT * FROM {Utils.GetTableName<T>()} WHERE {condition}" };

			// Turn the properties of param into SQLiteParameters
			if (param != null)
				foreach (PropertyInfo prop in Utils.GetProperties(param.GetType()))
					command.Parameters.Add(new SQLiteParameter($"@{prop.Name}", prop.GetValue(param)));

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

		public virtual void Dispose()
		{
			Connection.Dispose();
		}
	}
}
