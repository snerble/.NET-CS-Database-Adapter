using MySql.Data.MySqlClient;
using Database.Modeling;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Database
{
	/// <summary>
	/// Package-only class containing various utilities used in the Modeling namespace
	/// </summary>
	protected static class Utils
	{
		/// <summary>
		/// Simple dictionary that maps a C# type to a MySqlDbType. Used for automatic typing for queries.
		/// </summary>
		public static Dictionary<Type, MySqlDbType> TypeMap { get; } = new Dictionary<Type, MySqlDbType>
		{
			{typeof(bool), MySqlDbType.Bit },
			{typeof(sbyte), MySqlDbType.Byte },
			{typeof(short), MySqlDbType.Int16 },
			{typeof(int), MySqlDbType.Int32 },
			{typeof(long), MySqlDbType.Int64 },
			{typeof(byte), MySqlDbType.UByte },
			{typeof(ushort), MySqlDbType.UInt16 },
			{typeof(uint), MySqlDbType.UInt32 },
			{typeof(ulong), MySqlDbType.UInt64 },
			{typeof(string), MySqlDbType.Text },
			{typeof(byte[]), MySqlDbType.Blob }
		};

		/// <summary>
		/// Returns the <see cref="ColumnAttribute"/> of the specified property, or generates a new
		/// <see cref="ColumnAttribute"/> based on the property's name and type.
		/// </summary>
		/// <param name="autoGetType">If true, will automatically try to set the property's column type if it is null.</param>
		public static ColumnAttribute GetColumnData(PropertyInfo property, bool autoGetType = false)
		{
			// Get or create a columnnAttribute instance for this property
			var attribute = property.GetCustomAttribute<ColumnAttribute>() ?? new ColumnAttribute(property.Name);
			if (autoGetType && attribute.Type == null) // If the type is not specified, try to get it using the property type
			{
				// If the type is an enum, assign the enum type
				if (property.PropertyType.IsEnum) attribute.Type = MySqlDbType.Enum;
				// If the typemap contains an entry for the property's type, assign that type
				else if (TypeMap.TryGetValue(Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType, out MySqlDbType type))
					attribute.Type = type;
				// Otherwise, leave it null
			}
			return attribute;
		}

		/// <summary>
		/// Returns the table name of the specified data model, or the data model type name if no name is specified.
		/// </summary>
		public static string GetTableName<T>() where T : ItemAdapter => GetTableName(typeof(T));
		/// <summary>
		/// Returns the table name of the specified data model, or the data model type name if no name is specified.
		/// </summary>
		public static string GetTableName(Type t)
			=> t.GetCustomAttribute<TableAttribute>()?.Name ?? t.Name;

		/// <summary>
		/// Returns all columns/properties of the specified type <typeparamref name="T"/> with the specified
		/// attribute <typeparamref name="A"/>.
		/// </summary>
		/// <typeparam name="T">A type extending <see cref="ItemAdapter"/>.</typeparam>
		/// <typeparam name="A">A type extending <see cref="Attribute"/>.</typeparam>
		public static IEnumerable<PropertyInfo> GetColumns<T, A>() where T : ItemAdapter where A : Attribute
			=> GetColumns<A>(GetAllColumns<T>());
		/// <summary>
		/// Returns all columns/properties from the specified collection with the specified attriute <typeparamref name="A"/>.
		/// </summary>
		/// <typeparam name="A">A type extending <see cref="Attribute"/>.</typeparam>
		/// <param name="columns">The collection of columns to iterate through.</param>
		public static IEnumerable<PropertyInfo> GetColumns<A>(IEnumerable<PropertyInfo> columns)
			where A : Attribute
		{
			// Yield all column properties that have the specified attributes
			foreach (var column in columns)
				if (column.GetCustomAttribute<A>() != null)
					yield return column;
		}

		/// <summary>
		/// Returns an <see cref="IEnumerable{T}"/> of <see cref="PropertyInfo"/>s of the specified data model.
		/// </summary>
		public static IEnumerable<PropertyInfo> GetAllColumns<T>() where T : ItemAdapter
			=> GetAllColumns(typeof(T));
		/// <summary>
		/// Returns an <see cref="IEnumerable{T}"/> containing <see cref="PropertyInfo"/> instances
		/// of the specified type extending <see cref="ItemAdapter"/>.
		/// </summary>
		/// <param name="t">The type of a class extending <see cref="ItemAdapter"/>.</param>
		public static IEnumerable<PropertyInfo> GetAllColumns(Type t)
		{
			// Yield all properties not marked with IgnorePropertyAttribute
			foreach (var property in t.GetProperties())
				if (property.GetCustomAttribute<IgnorePropertyAttribute>() == null)
					yield return property;
		}
	}
}
