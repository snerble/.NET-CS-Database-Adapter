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
			throw new NotImplementedException();
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
