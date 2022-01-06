using System;
using System.Linq;

namespace Database.SQLite.Models
{
	/// <summary>
	/// Abstract superclass containing extra implementation for unit testing.
	/// </summary>
	public abstract class TestModel
	{
		public override bool Equals(object obj)
		{
			// If either object is null or their types don't match, use the standard equals function
			if (this == null || obj == null || obj.GetType() != GetType()) return base.Equals(obj);

			// Checks if a equals b, or if a and b are both null
			static bool isEqual(object a, object b)
			{
				// Compare sequence if both are an array
				if (a is Array && b is Array)
				{
					var A_array = a as Array;
					var B_array = b as Array;
					// Return false if they aren't the same length
					if (A_array.Length != B_array.Length) return false;
					// Return false when the elements are not equal
					for (int i = 0; i < A_array.Length; i++)
						if (!A_array.GetValue(i).Equals(B_array.GetValue(i)))
							return false;
					// All elements are equal
					return true;
				}
				return a?.Equals(b) ?? a == b;
			}

			// Get all non-virtual properties
			var properties = GetType()
				.GetProperties()
				.Where(x => !(x.GetGetMethod()?.IsVirtual ?? false) && !(x.GetSetMethod()?.IsVirtual ?? false));
			// Compare the values of the properties for both objects
			return properties.All(x => isEqual(x.GetValue(this), x.GetValue(obj)));
		}
		public override int GetHashCode() => base.GetHashCode();
	}
}
