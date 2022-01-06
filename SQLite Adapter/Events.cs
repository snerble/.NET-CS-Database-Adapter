using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Database.SQLite
{
	public partial class SQLiteAdapter : IDbAdapter
	{
		/// <summary>
		/// Boolean for interrupting a query.
		/// TODO Replace with something more tread-safe.
		/// </summary>
		private bool aborting = false;

		/// <summary>
		/// Prevents the current <see cref="SQLiteCommand"/> from being executed.
		/// </summary>
		/// <remarks>
		/// Intended to be used only during the <see cref="Deleting"/>, <see cref="Inserting"/>
		/// <see cref="Selecting"/> or <see cref="Updating"/> events.
		/// </remarks>
		public void Abort() => aborting = true;

		#region Event Triggers
		/// <summary>
		/// Invokes a given <see cref="QueryEventHandler{TEventArgs}"/> and handles the <see cref="aborting"/> flag.
		/// </summary>
		/// <typeparam name="TEventArgs">The event arguments type for <paramref name="eventHandler"/>.</typeparam>
		/// <param name="eventHandler">The <see cref="QueryEventHandler{TEventArgs}"/> to invoke.</param>
		private void InvokeQueryEvent<TEventArgs>(QueryEventHandler<TEventArgs> eventHandler, TEventArgs args)
		{
			if (eventHandler is null)
				return;

			// Reset the aborting flag
			aborting = false;

			eventHandler.Invoke(this, args);

			// Throw exception if the event delegate set the abort flag
			if (aborting)
			{
				aborting = false;
				throw new CommandAbortedException();
			}
		}

		/// <inheritdoc cref="OnDeleting(Type, string)"/>
		protected void OnDeleting<T>(string condition) => OnDeleting(typeof(T), condition);
		/// <summary>
		/// Invokes the <see cref="Deleting"/> event with the specified model type and condition string.
		/// </summary>
		protected void OnDeleting(Type modelType, string condition)
			=> InvokeQueryEvent(Deleting, new DeleteEventArgs(modelType, condition));
		/// <inheritdoc cref="OnDeleting(Type, IList{object})"/>
		protected void OnDeleting<T>(IList<T> collection) => OnDeleting(typeof(T), (IList<object>)collection);
		/// <summary>
		/// Invokes the <see cref="Deleting"/> event with the specified model type and collection of
		/// objects to delete.
		/// </summary>
		protected virtual void OnDeleting(Type modelType, IList<object> collection)
			=> InvokeQueryEvent(Deleting, new DeleteEventArgs(modelType, collection));

		/// <inheritdoc cref="OnInserting(Type, IList{object})"/>
		protected void OnInserting<T>(IList<T> collection) => OnInserting(typeof(T), (IList<object>)collection);
		/// <summary>
		/// Invokes the <see cref="Inserting"/> event with the specified model type and collection
		/// of objects to insert.
		/// </summary>
		protected virtual void OnInserting(Type modelType, IList<object> collection)
			=> InvokeQueryEvent(Inserting, new InsertEventArgs(modelType, collection));

		/// <inheritdoc cref="OnSelecting(Type, string)"/>
		protected void OnSelecting<T>(string condition) => OnSelecting(typeof(T), condition);
		/// <summary>
		/// Invokes the <see cref="Selecting"/> event with the specified model type and condition string.
		/// </summary>
		protected virtual void OnSelecting(Type modelType, string condition)
			=> InvokeQueryEvent(Selecting, new SelectEventArgs(modelType, condition));

		/// <inheritdoc cref="OnUpdating(Type, IList{object})"/>
		protected void OnUpdating<T>(IList<T> collection) => OnUpdating(typeof(T), (IList<object>)collection);
		/// <summary>
		/// Invokes the <see cref="Updating"/> event with the specified model type and collection
		/// of objects to update.
		/// </summary>
		protected virtual void OnUpdating(Type modelType, IList<object> collection)
			=> InvokeQueryEvent(Updating, new UpdateEventArgs(modelType, collection));
		#endregion

		#region Events
		/// <summary>
		/// This event is called when this <see cref="SQLiteDataAdapter"/> is about to execute a
		/// DELETE query.
		/// </summary>
		public event QueryEventHandler<DeleteEventArgs> Deleting;
		/// <summary>
		/// This event is called when this <see cref="SQLiteDataAdapter"/> is about to execute a
		/// INSERT query.
		/// </summary>
		public event QueryEventHandler<InsertEventArgs> Inserting;
		/// <summary>
		/// This event is called when this <see cref="SQLiteDataAdapter"/> is about to execute a
		/// SELECT query.
		/// </summary>
		public event QueryEventHandler<SelectEventArgs> Selecting;
		/// <summary>
		/// This event is called when this <see cref="SQLiteDataAdapter"/> is about to execute a
		/// UPDATE query.
		/// </summary>
		public event QueryEventHandler<UpdateEventArgs> Updating;
		#endregion
	}

	#region Delegates
	/// <summary>
	/// Represents the method that will handle <see cref="SQLiteAdapter"/> query events.
	/// </summary>
	/// <typeparam name="TEventArgs">A type extending <see cref="CommandEventArgs"/>.</typeparam>
	/// <param name="sender">The <see cref="SQLiteAdapter"/> instance invoking this handler.</param>
	/// <param name="args">An event arguments object containing data about query.</param>
	public delegate void QueryEventHandler<TEventArgs>(SQLiteAdapter sender, TEventArgs args);
	#endregion

	#region Event Args
	/// <summary>
	/// Event arguments class containing relevant information about a <see cref="SQLiteAdapter"/> DELETE event.
	/// This class cannot be inherited.
	/// </summary>
	public sealed class DeleteEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the condition string by which the query will delete elements from the database,
		/// or null if no condition was specified.
		/// </summary>
		public string Condition { get; }
		/// <summary>
		/// Gets the collection of objects that will be deleted from the database,
		/// or null if no object collection
		/// was specified.
		/// </summary>
		public IList<object> Collection { get; }
		/// <summary>
		/// Gets the type of the object model used in the query.
		/// </summary>
		public Type ModelType { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="DeleteEventArgs"/> with the specified model
		/// type and condition string.
		/// </summary>
		/// <param name="modelType">The type of the object model used in the query.</param>
		/// <param name="condition">The condition by which the query will remove elements.</param>
		internal DeleteEventArgs(Type modelType, string condition)
		{
			ModelType = modelType;
			Condition = condition;
		}
		/// <summary>
		/// Initializes a new instance of <see cref="DeleteEventArgs"/> with the specified
		/// model type and collection of objects.
		/// </summary>
		/// <param name="modelType">The type of the object model used in the query.</param>
		/// <param name="collection">The objects that will be deleted by the query.</param>
		internal DeleteEventArgs(Type modelType, IList<object> collection)
		{
			ModelType = modelType;
			Collection = collection;
		}
	}

	/// <summary>
	/// Event arguments class containing relevant information about a <see cref="SQLiteAdapter"/> INSERT event.
	/// This class cannot be inherited.
	/// </summary>
	public sealed class InsertEventArgs
	{
		/// <summary>
		/// Gets the collection of objects that will be inserted into the database.
		/// </summary>
		public IList<object> Collection { get; }
		/// <summary>
		/// Gets the type of the object model used in the query.
		/// </summary>
		public Type ModelType { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="InsertEventArgs"/> with the specified model
		/// type and collection of objects.
		/// </summary>
		/// <param name="modelType">The type of the object model used in the query.</param>
		/// <param name="collection">The objects that will be inserted by the query.</param>
		internal InsertEventArgs(Type modelType, IList<object> collection)
		{
			ModelType = modelType;
			Collection = collection;
		}
	}

	/// <summary>
	/// Event arguments class containing relevant information about a <see cref="SQLiteAdapter"/> SELECT event.
	/// This class cannot be inherited.
	/// </summary>
	public sealed class SelectEventArgs
	{
		/// <summary>
		/// Gets the condition string by which the query will select elements.
		/// </summary>
		public string Condition { get; }
		/// <summary>
		/// Gets the type of the object model used in the query.
		/// </summary>
		public Type ModelType { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="SelectEventArgs"/> with the specified model
		/// type and condition string.
		/// </summary>
		/// <param name="modelType">The type of the object model used in the query.</param>
		/// <param name="condition">The condition by which the query will select elements.</param>
		internal SelectEventArgs(Type modelType, string condition)
		{
			ModelType = modelType;
			Condition = condition;
		}
	}

	/// <summary>
	/// Event arguments class containing relevant information about a <see cref="SQLiteAdapter"/> UPDATE event.
	/// This class cannot be inherited.
	/// </summary>
	public sealed class UpdateEventArgs
	{
		/// <summary>
		/// Gets the collection of objects that will be updated in the database.
		/// </summary>
		public IList<object> Collection { get; }
		/// <summary>
		/// Gets the type of the object model used in the query.
		/// </summary>
		public Type ModelType { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="InsertEventArgs"/> with the specified model
		/// type and collection of objects.
		/// </summary>
		/// <param name="modelType">The type of the object model used in the query.</param>
		/// <param name="collection">The objects that will be updated by the query.</param>
		internal UpdateEventArgs(Type modelType, IList<object> collection)
		{
			ModelType = modelType;
			Collection = collection;
		}
	}
	#endregion
}
