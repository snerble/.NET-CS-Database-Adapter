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
	public class SQLiteAdapter : IDbAdapter
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

		private bool aborting = false;

		/// <summary>
		/// Initializes a new instance of <see cref="SQLiteAdapter"/>.
		/// </summary>
		/// <param name="datasource">The path to the database file to use.</param>
		public SQLiteAdapter(string datasource)
		{
			if (datasource is null) throw new ArgumentNullException(nameof(datasource));

			Connection = new SQLiteConnection($"Data Source={datasource};foreign keys=true");

			//Connection.AddTypeMapping("INTEGER", DbType.Byte, true);
			//Connection.AddTypeMapping("INTEGER", DbType.SByte, true);
			//Connection.AddTypeMapping("INTEGER", DbType.Int16, true);
			//Connection.AddTypeMapping("INTEGER", DbType.UInt16, true);
			//Connection.AddTypeMapping("INTEGER", DbType.Int32, true);
			//Connection.AddTypeMapping("INTEGER", DbType.UInt32, true);
			//Connection.AddTypeMapping("INTEGER", DbType.Int64, true);
			//Connection.AddTypeMapping("INTEGER", DbType.UInt64, true);
			//Connection.AddTypeMapping("REAL", DbType.Single, true);
			//Connection.AddTypeMapping("REAL", DbType.Double, true);
			//Connection.AddTypeMapping("NUMERIC", DbType.Decimal, true);
			//Connection.AddTypeMapping("boolean", DbType.Boolean, true);
			//Connection.AddTypeMapping("TEXT", DbType.String, true);
			//Connection.AddTypeMapping("TEXT", DbType.StringFixedLength, true);
			//Connection.AddTypeMapping("BLOB", DbType.Binary, true);

			Connection.Open();
		}

		/// <summary>
		/// Creates a new table that represents the given type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type to make a database table for.</typeparam>
		public void CreateTable<T>() => CreateTable(typeof(T));
		/// <summary>
		/// Creates a new table that represents the given <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The type to make a database table for.</param>
		public void CreateTable(Type type)
		{
			var columns = Utils.GetProperties(type);

			// Build the entire query
			var sb = new StringBuilder("CREATE TABLE ");
			sb.Append(Utils.GetTableName(type));
			sb.Append(" (");

			// Build the columns
			bool first = true;
			foreach (var column in columns)
			{
				// Omit the comma for the first entry
				if (!first) sb.Append(',');

				// TODO: Implement overwritable column data
				//var columnData = Utils.GetColumnData(column);
				sb.Append(column.Name);
				sb.Append(' ');
				sb.Append(TypeMapping.GetType(column.PropertyType));

				// Concatenate the column modifiers
				foreach (var columnModifier in column.GetCustomAttributes(typeof(SQLiteTableConstraintAttribute), false) as SQLiteTableConstraintAttribute[])
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

		public virtual int Delete<T>(string condition)
		{
			// Notify the deleting event
			InvokeEvent(Deleting, new DeleteEventArgs(typeof(T), condition));

			using var command = new SQLiteCommand(Connection) { CommandText = $"DELETE FROM {Utils.GetTableName<T>()} WHERE {condition}" };

			return command.ExecuteNonQuery();
		}
		public int Delete<T>(T item) => Delete<T>(new T[] { item });
		public virtual int Delete<T>(IList<T> items)
		{
			if (!items.Any()) return 0;

			// Notify the deleting event
			InvokeEvent(Deleting, new DeleteEventArgs(typeof(T), (IList<object>)items));

			using var command = Connection.CreateCommand();
			#region Query Building
			var sb = new StringBuilder("DELETE FROM");
			sb.Append(Utils.GetTableName<T>());
			sb.Append("WHERE(");

			// Try to get the primary key properties. Otherwise just get all properties
			var properties = Utils.GetProperties<T, PrimaryAttribute>().ToArray();
			if (!properties.Any()) properties = Utils.GetProperties<T>().ToArray();

			// Build conditions for every property and item
			for (int i = 0; i < properties.Length; ++i)
			{
				var property = properties[i];

				if (i != 0) sb.Append(") AND (");

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
			if (items.Count == 0) return -1;

			// Notify the inserting event
			InvokeEvent(Inserting, new InsertEventArgs(typeof(T), (IList<object>)items));

			var tableName = Utils.GetTableName<T>();
			var properties = Utils.GetProperties<T>();
			// Get the rowid property (if it is null, the query will be shortened)
			var rowid = Utils.GetRowIdProperty<T>(properties);
			using var command = Connection.CreateCommand();

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
			foreach (var item in items)
			{
				if (i != 0) sb.Append(',');
				sb.Append('(');

				// Append the column parameter names and simultaneously create the SQLiteParameter objects
				sb.AppendJoin(',', properties.Select((x, j) =>
				{
					var value = x.PropertyType.IsEnum ? x.GetValue(item).ToString() : x.GetValue(item);
					// Skip creating the SQLiteParameter if the value is null
					if (value == null) return "NULL";
					var paramName = $"@{j}_{i}";
					command.Parameters.Add(new SQLiteParameter(paramName, value));
					return paramName;
				}));

				sb.Append(')');
				++i;
			}
			sb.Append(';');

			// Add another query to get the scalar if false or if there is no rowid property
			if (!AutoAssignRowId || rowid == null) sb.Append("SELECT LAST_INSERT_ROWID();");
			command.CommandText = sb.ToString();
			#endregion

			// Execute the command and return the scalar with the max ROWID
			object _ = command.ExecuteScalar();
			var scalar = (long)(_ == DBNull.Value ? 0L : _); // DBNull gets replaced with 0

			// Skip assigning rowids if false
			if (!AutoAssignRowId) return scalar;

			// Assign the scalar for every inserted element if there is a ROWID property
			if (rowid != null)
			{
				++scalar;
				foreach (var item in items)
				{
					object oldValue = rowid.GetValue(item);
					// Skip the rowid assigning if it is not null and recalculate the scalar if necessary
					if (oldValue != null)
					{
						long oldRowId = Convert.ToInt64(oldValue);
						if (oldRowId >= scalar) scalar = oldRowId + 1;
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
			InvokeEvent(Selecting, new SelectEventArgs(typeof(T), condition));

			// Create the command
			using var command = new SQLiteCommand(Connection) { CommandText = $"SELECT * FROM {Utils.GetTableName<T>()} WHERE {condition}" };

			// Execute the command
			using var reader = command.ExecuteReader();

			// Map the reader's resultset to type T and yield it's results.
			// Using a foreach here to keep the reader alive untill the generator is done.
			foreach (var item in Utils.ParseReader<T>(reader))
				yield return item;
		}

		public int Update<T>(T item) => Update<T>(new T[] { item });
		public virtual int Update<T>(IList<T> items)
		{
			// Notify the updating event
			InvokeEvent(Updating, new UpdateEventArgs(typeof(T), (IList<object>)items));

			// Reset command abort flag
			aborting = false;

			if (!items.Any()) return 0;

			var properties = Utils.GetProperties<T>().ToArray();

			// Try to get the primary key attributes. If empty, throw an exception
			var primaries = Utils.GetProperties<PrimaryAttribute>(properties).ToArray();
			if (!primaries.Any()) throw new ArgumentException("The given generic type does not contain a primary key property.", "T");

			// Remove the primary properties from the properties list
			properties = properties.Except(primaries).ToArray();

			var tableName = Utils.GetTableName<T>();
			using var command = Connection.CreateCommand();

			#region Query Building
			var sb = new StringBuilder();
			int i = 0;
			foreach (var item in items)
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
					if (value is null) return $"{x.Name}=NULL";

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
					if (value == null) return $"{x.Name} IS NULL";

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

		/// <summary>
		/// Prevents the current <see cref="SQLiteCommand"/> from being executed.
		/// </summary>
		/// <remarks>
		/// Intended to be used only during the <see cref="Deleting"/>, <see cref="Inserting"/>
		/// <see cref="Selecting"/> or <see cref="Updating"/> events.
		/// </remarks>
		public void Abort() => aborting = true;

		/// <summary>
		/// Invokes a given <see cref="QueryEventHandler{TEventArgs}"/> and handles the <see cref="aborting"/> flag.
		/// </summary>
		/// <typeparam name="TEventArgs">The event arguments type for <paramref name="eventHandler"/>.</typeparam>
		/// <param name="eventHandler">The <see cref="QueryEventHandler{TEventArgs}"/> to invoke.</param>
		private void InvokeEvent<TEventArgs>(QueryEventHandler<TEventArgs> eventHandler, TEventArgs args)
		{
			if (eventHandler is null) return;

			// Reset the aborting flag
			aborting = false;

			eventHandler.Invoke(this, args);

			// Throw exception if the event delegate set the abort flag
			if (aborting)
			{
				aborting = false;
				throw new CommandAbortedException();
			}
		}

		/// <summary>
		/// Represents the method that will handle <see cref="SQLiteAdapter"/> query events.
		/// </summary>
		/// <typeparam name="TEventArgs">A type extending <see cref="CommandEventArgs"/>.</typeparam>
		/// <param name="sender">The <see cref="SQLiteAdapter"/> instance invoking this handler.</param>
		/// <param name="args">An event arguments object containing data about query.</param>
		public delegate void QueryEventHandler<TEventArgs>(SQLiteAdapter sender, TEventArgs args);

		/// <summary>
		/// This event is called when this <see cref="SQLiteDataAdapter"/> is about to execute a
		/// DELETE query.
		/// </summary>
		public event QueryEventHandler<DeleteEventArgs> Deleting;
		/// <summary>
		/// This event is called when this <see cref="SQLiteDataAdapter"/> is about to execute a
		/// INSERT query.
		/// </summary>
		public event QueryEventHandler<InsertEventArgs> Inserting;
		/// <summary>
		/// This event is called when this <see cref="SQLiteDataAdapter"/> is about to execute a
		/// SELECT query.
		/// </summary>
		public event QueryEventHandler<SelectEventArgs> Selecting;
		/// <summary>
		/// This event is called when this <see cref="SQLiteDataAdapter"/> is about to execute a
		/// UPDATE query.
		/// </summary>
		public event QueryEventHandler<UpdateEventArgs> Updating;

		public void Dispose()
		{
			Connection.Dispose();
		}
	}

	/// <summary>
	/// Event arguments class containing relevant information about a <see cref="SQLiteAdapter"/> DELETE event.
	/// This class cannot be inherited.
	/// </summary>
	public sealed class DeleteEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the condition string by which the query will delete elements from the database,
		/// or null if no condition was specified.
		/// </summary>
		public string Condition { get; }
		/// <summary>
		/// Gets the collection of objects that will be deleted from the database,
		/// or null if no object collection
		/// was specified.
		/// </summary>
		public IList<object> Collection { get; }
		/// <summary>
		/// Gets the type of the object model used in the query.
		/// </summary>
		public Type ModelType { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="DeleteEventArgs"/> with the specified model
		/// type and condition string.
		/// </summary>
		/// <param name="modelType">The type of the object model used in the query.</param>
		/// <param name="condition">The condition by which the query will remove elements.</param>
		internal DeleteEventArgs(Type modelType, string condition)
		{
			ModelType = modelType;
			Condition = condition;
		}
		/// <summary>
		/// Initializes a new instance of <see cref="DeleteEventArgs"/> with the specified
		/// model type and collection of objects.
		/// </summary>
		/// <param name="modelType">The type of the object model used in the query.</param>
		/// <param name="collection">The objects that will be deleted by the query.</param>
		internal DeleteEventArgs(Type modelType, IList<object> collection)
		{
			ModelType = modelType;
			Collection = collection;
		}
	}

	/// <summary>
	/// Event arguments class containing relevant information about a <see cref="SQLiteAdapter"/> INSERT event.
	/// This class cannot be inherited.
	/// </summary>
	public sealed class InsertEventArgs
	{
		/// <summary>
		/// Gets the collection of objects that will be inserted into the database.
		/// </summary>
		public IList<object> Collection { get; }
		/// <summary>
		/// Gets the type of the object model used in the query.
		/// </summary>
		public Type ModelType { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="InsertEventArgs"/> with the specified model
		/// type and collection of objects.
		/// </summary>
		/// <param name="modelType">The type of the object model used in the query.</param>
		/// <param name="collection">The objects that will be inserted by the query.</param>
		internal InsertEventArgs(Type modelType, IList<object> collection)
		{
			ModelType = modelType;
			Collection = collection;
		}
	}

	/// <summary>
	/// Event arguments class containing relevant information about a <see cref="SQLiteAdapter"/> SELECT event.
	/// This class cannot be inherited.
	/// </summary>
	public sealed class SelectEventArgs
	{
		/// <summary>
		/// Gets the condition string by which the query will select elements.
		/// </summary>
		public string Condition { get; }
		/// <summary>
		/// Gets the type of the object model used in the query.
		/// </summary>
		public Type ModelType { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="SelectEventArgs"/> with the specified model
		/// type and condition string.
		/// </summary>
		/// <param name="modelType">The type of the object model used in the query.</param>
		/// <param name="condition">The condition by which the query will select elements.</param>
		internal SelectEventArgs(Type modelType, string condition)
		{
			ModelType = modelType;
			Condition = condition;
		}
	}

	/// <summary>
	/// Event arguments class containing relevant information about a <see cref="SQLiteAdapter"/> UPDATE event.
	/// This class cannot be inherited.
	/// </summary>
	public sealed class UpdateEventArgs
	{
		/// <summary>
		/// Gets the collection of objects that will be updated in the database.
		/// </summary>
		public IList<object> Collection { get; }
		/// <summary>
		/// Gets the type of the object model used in the query.
		/// </summary>
		public Type ModelType { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="InsertEventArgs"/> with the specified model
		/// type and collection of objects.
		/// </summary>
		/// <param name="modelType">The type of the object model used in the query.</param>
		/// <param name="collection">The objects that will be updated by the query.</param>
		internal UpdateEventArgs(Type modelType, IList<object> collection)
		{
			ModelType = modelType;
			Collection = collection;
		}
	}
}
