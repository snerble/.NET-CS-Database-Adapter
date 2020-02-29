using System;
using System.Data;
using System.Collections.Generic;
using System.Text;

namespace Database
{
	/// <summary>
	/// Represents a wrapper for communicating with an <see cref="IDbConnection"/>.
	/// </summary>
	public interface IDbAdapter
	{
		/// <summary>
		/// Gets the <see cref="IDbConnection"/> instance used by this adapter.
		/// </summary>
		public abstract IDbConnection Connection { get; }
	}
}
