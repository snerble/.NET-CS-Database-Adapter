using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace Database.SQLite
{
	public class SQLiteAdapter : IDbAdapter
	{
		/// <summary>
		/// The <see cref="SQLiteConnection"/> used by this <see cref="SQLiteAdapter"/>.
		/// </summary>
		public readonly SQLiteConnection Connection;

		/// <summary>
		/// Initializes a new instance of <see cref="SQLiteAdapter"/>.
		/// </summary>
		/// <param name="datasource">The path to the database file to use.</param>
		public SQLiteAdapter(string datasource)
		{
			if (datasource is null) throw new ArgumentNullException(nameof(datasource));

			Connection = new SQLiteConnection($"Data Source={datasource};foreign keys=true");
			Connection.Open();
		}

		public int Delete<T>(string condition)
		{
			throw new NotImplementedException();
		}
		public int Delete<T>(T item)
		{
			throw new NotImplementedException();
		}
		public int Delete<T>(ICollection<T> items)
		{
			throw new NotImplementedException();
		}

		public int Insert<T>(T item)
		{
			// Get all properties that will represent columns
			var columns = Utils.GetAllColumns<T>().ToArray();

			using var command = Connection.CreateCommand();

			// Build segments of the final query using string joining
			string fieldsText = string.Join(", ", columns.Select(x => x.Name));
			string parametersText = string.Join(", ", columns.Select(x => "@" + x.Name));

			// TODO: Use StringBuilders. (Or not, I don't know what is more efficient)
			// Construct the final query using the type's table name, column list and parameters. 
			command.CommandText = $"INSERT INTO `{Utils.GetTableName<T>()}` ({fieldsText}) VALUES({parametersText}); ";

			// Construct a parameter object for each value
			foreach (var property in columns)
				command.Parameters.AddWithValue('@' + property.Name, property.GetValue(item) ?? DBNull.Value);

			// TODO: Implement the Insert overload that takes a collection instead.
			command.ExecuteNonQuery();

			// Retrieve the last inserted id with a new query
			return (int)(long) new SQLiteCommand(Connection) { CommandText = "SELECT LAST_INSERT_ROWID()" }.ExecuteScalar();
		}
		public int Insert<T>(ICollection<T> items)
		{
			// TODO: Create one big query instead of many small ones
			if (items.Count == 0) return -1;

			// Get and return the first auto increment id and update the remaining items.
			int scalar = Update(items.First());
			foreach (var item in items.Skip(1))
				Update(item);
			return scalar;
		}

		public IEnumerable<T> Select<T>() => Select<T>("1");
		public IEnumerable<T> Select<T>(string condition)
		{
			throw new NotImplementedException();
		}

		public int Update<T>(T item)
		{
			// TODO: Create condition string
			return Update(item, null);
		}
		public int Update<T>(T item, string condition)
		{
			throw new NotImplementedException();
		}
		public int Update<T>(ICollection<T> items)
		{
			// TODO: Create one big query instead of many small ones
			int affected_rows = 0;
			// Update each object individually and count all affected rows.
			foreach (var item in items)
				affected_rows += Update(item);
			return affected_rows;
		}

		public void Dispose()
		{
			Connection.Dispose();
		}
	}
}
