﻿using Database.SQLite.Modeling;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

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
			var sb = new StringBuilder("CREATE TABLE `");
			sb.Append(type.Name);
			sb.Append("` (\n");

			// Build the columns
			bool first = true;
			foreach (var column in columns)
			{
				// Omit the comma for the first entry
				if (!first)
				{
					sb.Append(",\n");
					first = false;
				}

				sb.Append('\t');
				// TODO: Implement overwritable column data
				//var columnData = Utils.GetColumnData(column);
				sb.Append(column.Name);
				sb.Append(' ');
				sb.Append(TypeMapping.GetType(column.PropertyType));

				// Concatenate the column modifiers
				foreach (var columnModifier in column.GetCustomAttributes(typeof(SQLiteColumnKeywordAttribute), false) as SQLiteColumnKeywordAttribute[])
				{
					sb.Append(' ');
					sb.Append(columnModifier.Name);
				}
				first = false;
			}
			sb.Append("\n);");

			// Create the command and execute it
			using var command = new SQLiteCommand(Connection) { CommandText = sb.ToString() };
			command.ExecuteNonQuery();
		}

		public int Delete<T>(string condition)
		{
			using var command = new SQLiteCommand(Connection) { CommandText = $"DELETE FROM `{Utils.GetTableName<T>()}` WHERE {condition}" };
			return command.ExecuteNonQuery();
		}
		public int Delete<T>(T item)
		{
			var primary = Utils.GetProperties<T, PrimaryAttribute>().FirstOrDefault();
			
			// Create query that checks only for the primary key if one exists
			if (primary != null)
			{
				using var command = Connection.CreateCommand();

				var sb = new StringBuilder("DELETE FROM `");
				sb.Append(Utils.GetTableName<T>());
				sb.Append("` WHERE `");
				// TODO: Implement overwritable column data
				sb.Append(primary.Name);
				sb.Append("` = @p");

				// Add parameter for the primary column value
				command.Parameters.Add(new SQLiteParameter("@p", primary.GetValue(item)));

				// Execute the command and return the amount of affected rows
				return command.ExecuteNonQuery();
			}
			// Otherwise make a query that compares the values of every column
			else
			{
				throw new NotImplementedException("Deletion of an object without a primary key is currently not supported.");
			}
		}
		public int Delete<T>(ICollection<T> items)
		{
			// TODO: Create one big query instead of many small ones
			int affected_rows = 0;
			// Delete each object individually and count all affected rows.
			foreach (var item in items)
				affected_rows += Delete(item);
			return affected_rows;
		}

		public int Insert<T>(T item)
		{
			// Get all properties that will represent columns
			var columns = Utils.GetProperties<T>().ToArray();

			using var command = Connection.CreateCommand();

			// Build segments of the final query using string joining
			string fieldsText = string.Join(", ", columns.Select(x => x.Name));
			string parametersText = string.Join(", ", columns.Select(x => "@" + x.Name));

			// TODO: Use StringBuilders. (Or not, I don't know what is more efficient)
			// Construct the final query using the type's table name, column list and parameters. 
			command.CommandText = $"INSERT INTO `{Utils.GetTableName<T>()}` ({fieldsText}) VALUES({parametersText}); ";

			// Construct a parameter object for each value
			// TODO: Add special case for enums
			foreach (var property in columns)
				command.Parameters.AddWithValue('@' + property.Name, property.GetValue(item) ?? DBNull.Value);

			// TODO: Implement the Insert overload that takes a collection instead.
			command.ExecuteNonQuery();

			// Retrieve the last inserted id with a new query
			using var scalarCommand = new SQLiteCommand(Connection) { CommandText = "SELECT LAST_INSERT_ROWID()" };
			return (int)(long) scalarCommand.ExecuteScalar();
		}
		public int Insert<T>(ICollection<T> items)
		{
			// TODO: Create one big query instead of many small ones
			if (items.Count == 0) return -1;

			// Get and return the first auto increment id and update the remaining items.
			int scalar = Insert(items.First());
			foreach (var item in items.Skip(1))
				Insert(item);
			return scalar;
		}

		public IEnumerable<T> Select<T>() where T : new() => Select<T>("1");
		public IEnumerable<T> Select<T>(string condition) where T : new()
		{
			if (string.IsNullOrEmpty(condition))
				throw new ArgumentException("Value may not be empty or null.", nameof(condition));

			// Create and execute the command
			using var command = new SQLiteCommand(Connection) { CommandText = $"SELECT * FROM `{Utils.GetTableName<T>()}` WHERE {condition}" };
			using var reader = command.ExecuteReader();

			var columns = Utils.GetProperties<T>();
			while (reader.Read())
			{
				T outObj = new T();
				for (int i = 0; i < reader.FieldCount; i++)
				{
					// TODO: Make the case-insensitivity optional
					var column = columns.First(x => x.Name.ToLower() == reader.GetName(i).ToLower());
					var type = column.PropertyType;
					var value = reader.GetValue(i);

					// Parse the value in case the model's type is an enum
					if (type.IsEnum) value = Enum.Parse(type, value.ToString());

					// Convert the value to the property type if it is a long (required since all int values are returned as a long)
					else if (reader.GetFieldType(i).IsAssignableFrom(typeof(long)))
						value = Convert.ChangeType(value, Nullable.GetUnderlyingType(type) ?? type);

					// Set the value in outObj with the property
					column.SetValue(outObj, value == DBNull.Value ? null : value);
				}
				// TODO: Add weak reference or some other system for accurately tracking the outObj
				yield return outObj;
			}
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
