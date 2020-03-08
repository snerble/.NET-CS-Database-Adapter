using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Database.SQLite
{
	/// <summary>
	/// Static class containing a mapping between .NET types and SQLite types.
	/// </summary>
	static class TypeMapping
	{
		///// <summary>
		///// A <see cref="Dictionary{TKey, TValue}"/> that maps a <see cref="Type"/> to a <see cref="DbType"/>.
		///// </summary>
		//private static readonly Dictionary<Type, DbType> Mappings = new Dictionary<Type, DbType>
		//{
		//	[typeof(byte)] = DbType.Byte,
		//	[typeof(sbyte)] = DbType.SByte,
		//	[typeof(short)] = DbType.Int16,
		//	[typeof(ushort)] = DbType.UInt16,
		//	[typeof(int)] = DbType.Int32,
		//	[typeof(uint)] = DbType.UInt32,
		//	[typeof(long)] = DbType.Int64,
		//	[typeof(ulong)] = DbType.UInt64,
		//	[typeof(float)] = DbType.Single,
		//	[typeof(double)] = DbType.Double,
		//	[typeof(decimal)] = DbType.Decimal,
		//	[typeof(bool)] = DbType.Boolean,
		//	[typeof(string)] = DbType.String,
		//	[typeof(char)] = DbType.StringFixedLength,
		//	[typeof(Guid)] = DbType.Guid,
		//	[typeof(DateTime)] = DbType.DateTime,
		//	[typeof(DateTimeOffset)] = DbType.DateTimeOffset,
		//	[typeof(byte[])] = DbType.Binary
		//};
		
		/// <summary>
		/// A <see cref="Dictionary{TKey, TValue}"/> that maps a <see cref="Type"/> to an SQLite type.
		/// </summary>
		private static readonly Dictionary<Type, string> Mappings = new Dictionary<Type, string>
		{
			[typeof(byte)] = "INTEGER",
			[typeof(sbyte)] = "INTEGER",
			[typeof(short)] = "INTEGER",
			[typeof(ushort)] = "INTEGER",
			[typeof(int)] = "INTEGER",
			[typeof(uint)] = "INTEGER",
			[typeof(long)] = "INTEGER",
			[typeof(ulong)] = "INTEGER",
			[typeof(float)] = "REAL",
			[typeof(double)] = "REAL",
			[typeof(decimal)] = "NUMERIC",
			[typeof(bool)] = "boolean",
			[typeof(string)] = "TEXT",
			[typeof(char)] = "TEXT",
			[typeof(Guid)] = "guid",
			[typeof(DateTime)] = "datetime",
			[typeof(DateTimeOffset)] = "datetimeoffset",
			[typeof(TimeSpan)] = "time",
			[typeof(byte[])] = "BLOB"
		};

		/// <summary>
		/// Returns an equivalent SQLite type for the given type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type to get the equivalent SQLite type from.</typeparam>
		public static string GetType<T>() => GetType(typeof(T));
		/// <summary>
		/// Returns an equivalent SQLite type for the given <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The type to get the equivalent SQLite type from.</param>
		/// <exception cref="KeyNotFoundException">Thrown when the given type does not have a mapping.</exception>
		public static string GetType(Type type)
		{
			// Get the actual type
			type = Nullable.GetUnderlyingType(type) ?? type;
			// Default enum types to a string
			if (type.IsEnum) return Mappings[typeof(string)];
			return Mappings[type];
		}

		/// <summary>
		/// Returns an equivalent <see cref="Type"/> for the given SQLite <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The SQLite type to get the equivalent <see cref="Type"/> from.</param>
		/// <exception cref="InvalidOperationException">Thrown when the given type does not have a mapping.</exception>
		public static Type GetType(string type) => Mappings.First(x => x.Value == type).Key;
	}
}
