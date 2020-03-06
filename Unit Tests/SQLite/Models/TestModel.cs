using Database.SQLite.Modeling;
using System;
using System.Collections.Generic;

namespace Database.SQLite.Models
{
	/// <summary>
	/// Simple database model for the unit tests.
	/// </summary>
	public class TestModel
	{
		[AutoIncrement]
		public int? Id { get; set; }
		public string TextField { get; set; }
		public int NumberField { get; set; } = 0;
		public int NumberField1 { get; set; } = 0;
		public int NumberField2 { get; set; } = 0;
		public int NumberField3 { get; set; } = 0;
		public int NumberField4 { get; set; } = 0;
		public DateTime DateTimeField { get; set; }
		public byte[] BinaryField { get; set; }
		public virtual List<SecondTestModel> ForeignKey { get; set; }
	}
}
