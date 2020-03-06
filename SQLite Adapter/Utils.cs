using System;
using System.Collections.Generic;
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
		public static string GetTableName(Type type) => type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;

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

	}
}
