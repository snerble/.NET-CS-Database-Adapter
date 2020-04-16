using Database.SQLite.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Database.SQLite
{
	public partial class QueryTests
	{
		[TestMethod]
		public void TestSelectParam()
		{
			Database.CreateTable<BasicModel>();
			Database.Insert<BasicModel>(BasicModelSample.ToArray());

			var stuff = Database.Select<BasicModel>("Text = @text", new { text = "sample_0" }).ToList();
		}
	}
}
