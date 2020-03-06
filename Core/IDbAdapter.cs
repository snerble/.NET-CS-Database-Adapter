using System;
using System.Collections.Generic;
using System.Data;

namespace Database
{
	/// <summary>
	/// Represents a wrapper for communicating with an <see cref="IDbConnection"/>.
	/// </summary>
	public interface IDbAdapter : IDisposable
	{
		/* All these generic types must be creatable, so check it with this.
		 *	if(classType.GetConstructor(Type.EmptyTypes) != null && !classType.IsAbstract)
		 *	{
		 *		//this type is constructable with default constructor
		 *	}
		 */

		/// <summary>
		/// Returns all entries of the given object in an <see cref="IEnumerable{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object to select from the database.</typeparam>
		public IEnumerable<T> Select<T>() where T : new();
		/// <summary>
		/// Returns all entries of the given object that match the given condition
		/// in an <see cref="IEnumerable{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object to select from the database.</typeparam>
		/// <param name="condition">The condition to test all selected elements against.</param>
		public IEnumerable<T> Select<T>(string condition) where T : new();
		// TODO: Add additional Select methods to allow the use of types without a parameterless constructor

		/// <summary>
		/// Inserts an object into the database.
		/// </summary>
		/// <typeparam name="T">The type of the object to insert into the database.</typeparam>
		/// <param name="item">The object to insert into the database.</param>
		/// <returns>If the table has an auto increment column, returns the id of the inserted item. Otherwise -1.</returns>
		public int Insert<T>(T item);
		/// <summary>
		/// Inserts the collection of objects into this database.
		/// </summary>
		/// <typeparam name="T">The type of the objects to insert into the database.</typeparam>
		/// <param name="items">The objects to insert into the database.</param>
		/// <returns>If the table has an auto increment column, returns the id of the first inserted item. Otherwise -1.</returns>
		public int Insert<T>(ICollection<T> items);

		/// <summary>
		/// Updates the specified object in the database.
		/// </summary>
		/// <typeparam name="T">The type of the object to update in the database.</typeparam>
		/// <param name="item">The object to update in the database.</param>
		/// <returns>The number of affected rows.</returns>
		public int Update<T>(T item);
		/// <summary>
		/// Updates all elements matching the given conditon with the specified object.
		/// </summary>
		/// <typeparam name="T">The type of the objects to update in the database.</typeparam>
		/// <param name="item">The object to update all matched elements with in the database.</param>
		/// <param name="condition">A condition that matches all elements that will be updated.</param>
		/// <returns>The number of affected rows.</returns>
		public int Update<T>(T item, string condition);
		/// <summary>
		/// Updates the collection of objects in the database.
		/// </summary>
		/// <typeparam name="T">The type of the objects to update in the database.</typeparam>
		/// <param name="items">The collection of items to update in the database.</param>
		/// <returns>The number of affected rows.</returns>
		public int Update<T>(ICollection<T> items);

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
		public int Delete<T>(ICollection<T> items);
	}
}