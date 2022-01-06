using System;
using System.Collections.Generic;
using System.Text;

namespace Database.SQLite
{
	/// <summary>
	/// Thrown when a query has been aborted. This class cannot be inherited.
	/// </summary>
	public sealed class CommandAbortedException : Exception { }
}
