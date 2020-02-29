using System;

namespace Database.Modeling
{
	/// <summary>
	/// Prevents a property in a <see cref="ItemAdapter"/> subclass from being considered a column.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class IgnorePropertyAttribute : Attribute { }
}
