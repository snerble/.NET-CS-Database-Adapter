using Database.SQLite.Modeling;
using System;

namespace Database.SQLite.Models
{
	/// <summary>
	/// Simple object model containing basic types. This class cannot be inherited.
	/// </summary>
	public sealed class BasicModel : TestModel
	{
		[Primary]
		public int? Id { get; set; }
		public bool Boolean { get; set; }
		public int Integer { get; set; }
		public double Decimal { get; set; }
		public string Text { get; set; }
		public byte[] Blob { get; set; }
		public BasicModelEnum Enum { get; set; }
		public DateTime DateTime { get; set; }
	}

	/// <summary>
	/// Basic enumeration used by the <see cref="BasicModel"/> for testing.
	/// </summary>
	public enum BasicModelEnum
	{
		Value1,
		Value2,
		Value3,
		Value4,
		Value5,
		Value6,
		Value7,
		Value8
	}
}
