using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Database.SQLite.Modeling;

namespace Database.SQLite
{
	/// <summary>
	/// Package-only static class filled with utilities used in this namespace.
	/// </summary>
	static class Utils
	{
		/// <summary>
		/// Returns the <see cref="TableAttribute.Name"/> value from the given type, or the
		/// type's name if no <see cref="TableAttribute"/> was found.
		/// </summary>
		/// <typeparam name="T">The type whose table name to return.</typeparam>
		public static string GetTableName<T>() => GetTableName(typeof(T));
		/// <summary>
		/// Returns the <see cref="TableAttribute.Name"/> value from the given type, or the
		/// type's name if no <see cref="TableAttribute"/> was found.
		/// </summary>
		/// <param name="type">The type whose table name to return.</typeparam>
		public static string GetTableName(Type type) => $"`{type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name}`";

		/// <summary>
		/// Returns all properties from the specified type.
		/// </summary>
		/// <typeparam name="T">The type whose properties to return.</param>
		/// <remarks>
		/// Virtual properties are ignored.
		/// </remarks>
		public static IEnumerable<PropertyInfo> GetProperties<T>() => GetProperties(typeof(T));
		/// <summary>
		/// Returns all properties from the specified type.
		/// </summary>
		/// <param name="type">The type whose properties to return.</param>
		/// <remarks>
		/// Virtual properties are ignored.
		/// </remarks>
		public static IEnumerable<PropertyInfo> GetProperties(Type type)
			=> type.GetProperties()
			.Where(x => !(x.GetGetMethod()?.IsVirtual ?? false) && !(x.GetSetMethod()?.IsVirtual ?? false));

		/// <summary>
		/// Returns all properties from the given type <typeparamref name="T"/> with the
		/// specified attribute <typeparamref name="A"/>
		/// </summary>
		/// <typeparam name="T">The type whose properties to return.</typeparam>
		/// <typeparam name="A">The attribute to filter the properties with.</typeparam>
		public static IEnumerable<PropertyInfo> GetProperties<T, A>() where A : Attribute
			=> GetProperties<A>(typeof(T).GetProperties());
		/// <summary>
		/// Filters the given collection of <see cref="PropertyInfo"/> objects by only returning those
		/// which have the specified attribute <typeparamref name="A"/>.
		/// </summary>
		/// <typeparam name="A">The attribute to filter the properties with.</typeparam>
		public static IEnumerable<PropertyInfo> GetProperties<A>(IEnumerable<PropertyInfo> props) where A : Attribute
			=> props.Where(x => x.GetCustomAttribute<A>() != null);

		/// <summary>
		/// Returns the <see cref="PropertyInfo"/> that aliases the ROWID column. Otherwise
		/// returns null if no such property exists, or if the type has specified the
		/// <see cref="WithoutRowIdAttribute"/>.
		/// </summary>
		/// <typeparam name="T">The type whose ROWID alias property to return.</typeparam>
		public static PropertyInfo GetRowIdProperty<T>()
			=> GetRowIdProperty<T>(GetProperties<T, PrimaryAttribute>());
		/// <summary>
		/// Returns the <see cref="PropertyInfo"/> that aliases the ROWID column. Otherwise
		/// returns null if no such property exists, or if the type has specified the
		/// <see cref="WithoutRowIdAttribute"/>.
		/// </summary>
		/// <typeparam name="T">The type whose ROWID alias property to return.</typeparam>
		/// <param name="properties">An existing list of properties that should belong to the given type.</param>
		/// <remarks>
		/// This overload only exists to avoid getting yet another list of properties.
		/// </remarks>
		public static PropertyInfo GetRowIdProperty<T>(IEnumerable<PropertyInfo> properties)
		{
			// If the type simply does not have a rowid, return null
			if (typeof(T).GetCustomAttribute<WithoutRowIdAttribute>() != null)
				return null;
			
			// Return the first property that either has the AutoIncrementAttribute
			// or if it has a PrimaryAttribute and it's type is INTEGER.
			// TODO: Don't forget to change this if the attributes get changed
			return properties.Where(x => x.GetCustomAttribute<PrimaryAttribute>() != null).FirstOrDefault(
				x => x.GetCustomAttribute<AutoIncrementAttribute>() != null
					 || TypeMapping.GetType(x.PropertyType) == "INTEGER"
			);
		}

		/// <summary>
		/// Maps the <paramref name="reader"/>'s current result set to the type <typeparamref name="T"/>
		/// and yields the new instances of <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type to map the <paramref name="reader"/> results to.</typeparam>
		/// <param name="reader">A <see cref="IDataReader"/> object to read from.</param>
		/// <param name="ignoreColumnCase">Toggles whether the mapping between columns and properties is
		/// case-insensitive.</param>
		public static IEnumerable<T> ParseReader<T>(IDataReader reader, bool ignoreColumnCase = true) where T : new()
		{
			if (reader is null)
				throw new ArgumentNullException(nameof(reader));

			var properties = GetProperties<T>();
			while (reader.Read())
			{
				T outObj = new T();
				for (int i = 0; i < reader.FieldCount; i++)
				{
					var name = reader.GetName(i);
					// Get a matching property by comparing the name of the current field with the property names
					var column = properties.First(
						x => ignoreColumnCase
							? x.Name.ToLower() == reader.GetName(i).ToLower()
							: x.Name == reader.GetName(i)
					);
					var type = Nullable.GetUnderlyingType(column.PropertyType) ?? column.PropertyType;
					var value = reader.GetValue(i);

					// If-else block for handling special values
					if (value == DBNull.Value)
					{
						// Convert DBNull to null
						value = null;
					}
					else if (type.IsEnum)
					{
						// Parse the value in case the property type is an enum
						value = Enum.Parse(type, value.ToString());
					}
					else if (reader.GetFieldType(i).IsAssignableFrom(typeof(long)))
					{
						// If the type is a long, convert it to the integer type of the property
						value = Convert.ChangeType(value, Nullable.GetUnderlyingType(type) ?? type);
					}

					// Set the value in outObj with the property
					column.SetValue(outObj, value);
				}
				yield return outObj;
			}
			// Advance the reader to the next result set
			reader.NextResult();
		}
	}
}
