using System;
using System.Collections.Generic;

namespace Database
{
	/// <summary>
	/// Represents a wrapper for communicating with a data source.
	/// </summary>
	public interface IDbAdapter : IDisposable
	{
		/// <summary>
		/// Returns all entries of the given type.
		/// </summary>
		/// <typeparam name="T">The type of the object to select from the database.</typeparam>
		public IEnumerable<T> Select<T>() where T : new();
		/// <summary>
		/// Returns all entries of the given type that match the <paramref name="condition"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object to select from the database.</typeparam>
		/// <param name="condition">The condition to test all selected elements against.</param>
		public IEnumerable<T> Select<T>(string condition) where T : new();
		/// <summary>
		/// Returns all entries of the given type that match the <paramref name="condition"/>.
		/// <para/>
		/// Properties in the <paramref name="param"/> object will be mapped to parameter tags
		/// in <paramref name="condition"/> with the same name.
		/// </summary>
		/// <typeparam name="T">The type of the object to select from the database.</typeparam>
		/// <param name="condition">The condition to test all selected elements against.</param>
		/// <param name="param">An object whose properties to map onto the <paramref name="condition"/>.</param>
		/// <remarks>
		/// The parameter tags in <paramref name="condition"/> must begin with @ in order to work.
		/// </remarks>
		public IEnumerable<T> Select<T>(string condition, object param) where T : new();

		/// <summary>
		/// Inserts an object into the database.
		/// </summary>
		/// <typeparam name="T">The type of the object to insert into the database.</typeparam>
		/// <param name="item">The object to insert into the database.</param>
		/// <returns>If the table has an auto increment column, returns the id of the inserted item. Otherwise -1.</returns>
		public long Insert<T>(T item);
		/// <summary>
		/// Inserts a collection of objects into this database.
		/// </summary>
		/// <typeparam name="T">The type of the objects to insert into the database.</typeparam>
		/// <param name="items">The objects to insert into the database.</param>
		/// <returns>If the table has an auto increment column, returns the id of the first inserted item. Otherwise -1.</returns>
		public long Insert<T>(IList<T> items);

		/// <summary>
		/// Updates the specified object in the database.
		/// </summary>
		/// <typeparam name="T">The type of the object to update in the database.</typeparam>
		/// <param name="item">The object to update in the database.</param>
		/// <returns>The number of affected rows.</returns>
		public int Update<T>(T item);
		/// <summary>
		/// Updates a collection of objects in the database.
		/// </summary>
		/// <typeparam name="T">The type of the objects to update in the database.</typeparam>
		/// <param name="items">The collection of items to update in the database.</param>
		/// <returns>The number of affected rows.</returns>
		public int Update<T>(IList<T> items);

		/// <summary>
		/// Deletes all objects their database table that match the specified condition.
		/// </summary>
		/// <typeparam name="T">The type of the objects to delete from the database.</typeparam>
		/// <param name="condition">A condition that will match all elements that will be deleted.</param>
		/// <returns>The number of affected rows.</returns>
		public int Delete<T>(string condition);
		/// <summary>
		/// Deletes an specified object from the database.
		/// </summary>
		/// <typeparam name="T">The type of the object to delete from the database.</typeparam>
		/// <param name="item">The object to delete from the database.</param>
		/// <returns>The number of affected rows.</returns>
		public int Delete<T>(T item);
		/// <summary>
		/// Deletes a collection of objects from the database.
		/// </summary>
		/// <typeparam name="T">The type of the objects to delete from the database.</typeparam>
		/// <param name="items">The collection of items to delete from the database.</param>
		/// <returns>The number of affected rows.</returns>
		public int Delete<T>(IList<T> items);
	}
}