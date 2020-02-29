using MySql.Data.MySqlClient;
using Database.Modeling;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Database.MySQL
{
	/// <summary>
	/// Abstract class that must be implemented to set up a MySQL database.
	/// </summary>
	public abstract class DatabaseAdapter : IDisposable
	{
		/// <summary>
		/// The <see cref="MySqlConnection"/> instance used by this adapter.
		/// </summary>
		public abstract MySqlConnection Connection { get; }

		#region Command Getters
		/// <summary>
		/// Builds and returns a new <see cref="MySqlCommand"/> with a SELECT query and returns a result set
		/// containing the data of the specified data model.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		public MySqlCommand GetSelect<T>(string condition = null) where T : ItemAdapter
			=> new MySqlCommand($"SELECT * FROM `{Utils.GetTableName<T>()}` WHERE {condition ?? "1"}", Connection);

		/// <summary>
		/// Builds and returns a new <see cref="MySqlCommand"/> with an INSERT query and uploads the <paramref name="item"/>
		/// to the remote database when executed.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="item">The item to put in the query.</param>
		public MySqlCommand GetInsert<T>(T item) where T : ItemAdapter
			=> GetInsert(new T[] { item });
		/// <summary>
		/// Builds and returns a new <see cref="MySqlCommand"/> with an INSERT query and uploads the <paramref name="items"/>
		/// collection to the remote database when executed.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="items">The collection of items to put in the query.</param>
		public MySqlCommand GetInsert<T>(ICollection<T> items) where T : ItemAdapter
		{
			// Get all columns/properties of the model
			var columns = Utils.GetAllColumns<T>().ToDictionary(x => Utils.GetColumnData(x));

			// Func that creates a parameter string for one item
			int i = 0;
			string getValueString(T item)
			{
				var s = '(' + string.Join(", ", columns.Keys.Select(x => $"@{x.Name}_{i}")) + ')';
				i++;
				return s;
			}

			// Generate query for the collection of items
			var query = new StringBuilder($"INSERT INTO `{Utils.GetTableName<T>()}` (");
			query.Append(string.Join(", ", columns.Keys.Select(x => $"`{x.Name}`"))); // Build component with column names
			query.Append(") VALUES ");
			query.Append(string.Join(",", items.Select(x => getValueString(x))));     // Build components with all item parameters

			// Create command
			var command = new MySqlCommand(query.ToString(), Connection);
			// Create parameter objects for every value and item
			i = 0;
			foreach (var item in items)
			{
				foreach (var keyValue in columns)
					command.Parameters.Add(new MySqlParameter(keyValue.Key.Name + '_' + i, keyValue.Value.GetValue(item) ?? DBNull.Value));
				i++;
			}
			// Return the generated command
			return command;
		}

		/// <summary>
		/// Builds and returns a new <see cref="MySqlCommand"/> with an UPDATE query and updates all rows matching <paramref name="condition"/>
		/// with the values in <paramref name="item"/>.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="item">The item whose values to copy to all matched rows.</param>
		/// <param name="condition">A MySql condition that returns true when it matches a table row.</param>
		/// <seealso cref="ItemAdapter.Cache"/>.
		public MySqlCommand GetUpdate<T>(T item, string condition) where T : ItemAdapter
		{
			var tableName = Utils.GetTableName<T>();
			// Get all columns/properties of the model
			var columns = Utils.GetAllColumns<T>().ToDictionary(x => Utils.GetColumnData(x));

			// Build the query
			var query = new StringBuilder();
			query.Append($"UPDATE `{tableName}` SET ");
			query.Append(string.Join(",", columns.Keys.Select(x => $"{x.Name} = @{x.Name}")));
			query.Append(" WHERE ");
			query.Append(condition);
			query.Append(';');
			var command = new MySqlCommand(query.ToString(), Connection);

			// Create the parameter objects for the first half of the query
			foreach (var (info, column) in columns)
				command.Parameters.Add(new MySqlParameter(info.Name, column.GetValue(item) ?? DBNull.Value));

			return command;
		}
		/// <summary>
		/// Builds and returns a new <see cref="MySqlCommand"/> with an UPDATE query and updates the <paramref name="item"/>
		/// based on it's internal cache.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="item">The item to update.</param>
		/// <seealso cref="ItemAdapter.Cache"/>.
		public MySqlCommand GetUpdate<T>(T item) where T : ItemAdapter
			=> GetUpdate(new T[] { item });
		/// <summary>
		/// Builds and returns a new <see cref="MySqlCommand"/> with an UPDATE query and updates the <paramref name="items"/>
		/// based on their internal cache.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <seealso cref="ItemAdapter.Cache"/>.
		public MySqlCommand GetUpdate<T>(ICollection<T> items) where T : ItemAdapter
		{
			var tableName = Utils.GetTableName<T>();
			// Get all columns/properties of the model
			var columns = Utils.GetAllColumns<T>().ToDictionary(x => Utils.GetColumnData(x));
			items = items.Where(x => x.IsChanged).ToList();

			var query = new StringBuilder();
			int i = 0;
			foreach (var item in items)
			{
				query.Append($"UPDATE `{tableName}` SET ");
				query.Append(string.Join(",", columns.Keys.Select(x => $"`{x.Name}` = @{x.Name}_{i}")));
				query.Append(" WHERE ");
				query.Append(string.Join(" AND ", columns.Select(x => $"`{x.Key.Name}` {(x.Value.GetValue(item.clone) == null ? "IS" : "=")} @{x.Key.Name}_c{i}")));
				query.Append(';');
				i++;
			}
			var command = new MySqlCommand(query.ToString(), Connection);

			i = 0;
			foreach (var item in items)
			{
				foreach (var (info, column) in columns)
				{
					command.Parameters.Add(new MySqlParameter(info.Name + '_' + i, column.GetValue(item) ?? DBNull.Value));
					command.Parameters.Add(new MySqlParameter(info.Name + "_c" + i, column.GetValue(item.clone) ?? DBNull.Value));
				}
				i++;
			}
			return command;
		}

		/// <summary>
		/// Builds and returns a new <see cref="MySqlCommand"/> with a DELETE query that deletes all
		/// rows that match the specified condition from the remote database when executed.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="condition">A MySql condition that returns true for every row to delete.</param>
		public MySqlCommand GetDelete<T>(string condition) where T : ItemAdapter
		{
			// Get the columns of the data model
			var columns = Utils.GetAllColumns<T>().ToDictionary(x => Utils.GetColumnData(x));

			// Generate query
			var query = $"DELETE FROM `{Utils.GetTableName<T>()}` WHERE " + condition;
			// Create command
			var command = new MySqlCommand(query.ToString(), Connection);
	
			// Return the new command
			return command;
		}
		/// <summary>
		/// Builds and returns a new <see cref="MySqlCommand"/> with a DELETE query that deletes the <paramref name="item"/>
		/// from the remote database when executed.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="item">The item to put in the query.</param>
		public MySqlCommand GetDelete<T>(T item) where T : ItemAdapter
			=> GetDelete(new T[] { item });
		/// <summary>
		/// Builds and returns a new <see cref="MySqlCommand"/> with a DELETE query that deletes the <paramref name="items"/>
		/// collection from the remote database when executed.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="items">The collection of items to put in the query.</param>
		public MySqlCommand GetDelete<T>(ICollection<T> items) where T : ItemAdapter
		{
			// Get the columns of the data model
			var columns = Utils.GetAllColumns<T>().ToDictionary(x => Utils.GetColumnData(x));

			int i = 0;
			string getCondition(T item)
			{
				var s = string.Join(" AND ", columns.Where(x => x.Value.GetValue(item) != null).Select(x => $"`{x.Key.Name}` = @{x.Key.Name}_{i}"));
				i++;
				return s;
			}

			// Generate query
			var query = new StringBuilder($"DELETE FROM `{Utils.GetTableName<T>()}` WHERE ");

			// Create condition testing every non-null value of the item to their column
			query.Append(string.Join(" OR ", items.Select(item => '(' + getCondition(item) + ')')));

			// Create command
			var command = new MySqlCommand(query.ToString(), Connection);
			// Generate the parameters
			i = 0;
			foreach (var item in items)
			{
				foreach (var keyValue in columns)
					command.Parameters.Add(new MySqlParameter(keyValue.Key.Name + '_' + i, keyValue.Value.GetValue(item)));
				i++;
			}
			// Return the new command
			return command;
		}
		#endregion

		#region Query Methods
		/// <summary>
		/// Returns all entries of the given data model in an <see cref="IEnumerable{T}"/>.
		/// </summary>
		/// <typeparam name="T">A subclass of <see cref="ItemAdapter"/>.</typeparam>
		public IEnumerable<T> Select<T>(string condition = null) where T : ItemAdapter
		{
			// Fix condition if it is empty
			if (!condition.Any())
				condition = "1";

			using var command = GetSelect<T>(condition);
			using var reader = command.ExecuteReader();

			// Get the properties of the model and sort them based on name
			var columns = Utils.GetAllColumns<T>().ToDictionary(x => Utils.GetColumnData(x).Name.ToLower());
			do
			{
				// Collect field names in a list
				var fields = new List<string>();
				for (int i = 0; i < reader.FieldCount; i++)
					fields.Add(reader.GetName(i).ToLower());
				// Loop through items
				while (reader.Read())
				{
					T outObj = Activator.CreateInstance(typeof(T)) as T;
					//T outObj = new T();
					for (int i = 0; i < reader.FieldCount; i++)
					{
						var property = columns[fields[i]];
						var type = property.PropertyType;
						var value = reader.GetValue(i);

						// Convert enum since database always returns string
						if (type.IsEnum) value = Enum.Parse(type, value.ToString());

						property.SetValue(outObj, value == DBNull.Value ? null : value);
					}
					outObj.Cache();
					yield return outObj;
				}
			}
			while (reader.NextResult());
		}

		/// <summary>
		/// Inserts an object into this database.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="item">The object to insert into the database.</param>
		/// <param name="discardIndex">If true, will skip getting and assigning the last insert id to the item.</param>
		/// <returns>If the table has an auto increment column, the id of the inserted item. Otherwise -1.</returns>
		public long Insert<T>(T item, bool discardIndex = false) where T : ItemAdapter
			=> Insert(new T[] { item }, discardIndex);
		/// <summary>
		/// Inserts the collection of objects into this database.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="items">The objects to insert into the database.</param>
		/// <param name="discardIndex">If true, will skip getting and assigning the last insert id to the items.</param>
		/// <returns>If the table has an auto increment column, the id of the inserted item. Otherwise -1.</returns>
		public long Insert<T>(ICollection<T> items, bool discardIndex = false) where T : ItemAdapter
		{
			// Get the command
			using var command = GetInsert(items);

			// Run command and get the scalar
			command.ExecuteNonQuery();
			var scalar = command.LastInsertedId;

			// Try-Finally block so we can cache stuff in the finally block
			try
			{
				// skip other things if it is -1 or if we want to discard the new index
				if (scalar == -1 || discardIndex) return -1;

				// Get the auto increment property/column
				var columns = Utils.GetAllColumns<T>().ToDictionary(x => Utils.GetColumnData(x));
				var autoIncrement = Utils.GetColumns<AutoIncrementAttribute>(columns.Values).FirstOrDefault();
				if (autoIncrement != null)
				{
					var autoIncType = Nullable.GetUnderlyingType(autoIncrement.PropertyType) ?? autoIncrement.PropertyType;
					long i = 0;
					// Assign the insert index to every item (if their auto increment value is null)
					foreach (var item in items)
					{
						// Set the autoIncrement column to the existing value, or scalar + i if it is null
						object oldValue = autoIncrement.GetValue(item);
						if (oldValue != null) // if the auto increment was already assigned
						{
							var oldValueLong = Convert.ToInt64(oldValue);
							// if the value is larger than 0, skip assigning the auto increment value
							if (oldValueLong > 0)
							{
								if (oldValueLong > scalar + i)
									i = scalar - oldValueLong;
								continue;
							}
						}
						// Convert the scalar to the type of the auto increment property and set it
						autoIncrement.SetValue(item, Convert.ChangeType(scalar + i, autoIncType));
						i++;
					}
				}
				return scalar;
			}
			finally
			{
				// Cache all inserted items
				foreach (var item in items)
					item.Cache();
			}
		}

		/// <summary>
		/// Updates the rows in the remote database the data of the specified item if they match the
		/// specified condition/
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="item">The item to update.</param>
		/// <param name="condition">A MySql condition that returns true when it matches a table row.</param>
		/// <seealso cref="ItemAdapter.Cache"/>.
		/// <returns>The number of affected rows.</returns>
		public long Update<T>(T item, string condition) where T : ItemAdapter
		{
			using var command = GetUpdate(item, condition);
			return command.ExecuteNonQuery();
		}
		/// <summary>
		/// Updates the specified item in the remote database using the item's internal cache.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="item">The item to update.</param>
		/// <seealso cref="ItemAdapter.Cache"/>.
		/// <returns>The number of affected rows.</returns>
		public long Update<T>(T item) where T : ItemAdapter
		{
			using var command = GetUpdate(item);
			// If the command is uninitialized, return 0
			if (command.CommandText.Length == 0) return 0;
			return command.ExecuteNonQuery();
		}
		/// <summary>
		/// Updates the rows in the remote database with the given collection of items. Items are only
		/// updated if they have been altered and are no longer equal to their internal cache.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="items">The collection of items to update.</param>
		/// <seealso cref="ItemAdapter.Cache"/>.
		/// <returns>The number of affected rows.</returns>
		public long Update<T>(ICollection<T> items) where T : ItemAdapter
		{
			using var command = GetUpdate(items);
			// If the command is uninitialized, return 0
			if (command.CommandText.Length == 0) return 0;
			// Run the query
			var updated = command.ExecuteNonQuery();
			// Reset the itemAdapter's internal cache
			foreach (var item in items)
				item.Cache();
			// Return the amount of updated rows
			return updated;
		}

		/// <summary>
		/// Deletes all objects from the model's table that match the specified condition.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="condition">A MySql condition that returns true for every row to delete.</param>
		/// <returns>The number of affected rows.</returns>
		public long Delete<T>(string condition) where T : ItemAdapter
		{
			using var command = GetDelete<T>(condition);
			return command.ExecuteNonQuery();
		}
		/// <summary>
		/// Deletes the specified object from the remote database.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="item">The item to delete from the database.</param>
		/// <returns>The number of affected rows.</returns>
		public long Delete<T>(T item) where T : ItemAdapter
		{
			using var command = GetDelete(item);
			return command.ExecuteNonQuery();
		}
		/// <summary>
		/// Deletes the specified collection of objects from the remote database.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <param name="items">The collection of items to delete from the database.</param>
		/// <returns>The number of affected rows.</returns>
		public long Delete<T>(ICollection<T> items) where T : ItemAdapter
		{
			using var command = GetDelete(items);
			return command.ExecuteNonQuery();
		} 
		#endregion

		/// <summary>
		/// Disposes the resources used by this <see cref="DatabaseAdapter"/>.
		/// </summary>
		public void Dispose()
		{
			Connection.Dispose();
		}
	}
}