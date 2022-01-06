using Database.SQLite.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Database.SQLite
{
	public partial class QueryTests
	{
		/// <summary>
		/// Sample dataset for the <see cref="BasicModel"/> type.
		/// </summary>
		private static IEnumerable<BasicModel> BasicModelSample
		{
			get
			{
				int sampleSize = 10;
				for (int i = 0; i < sampleSize; ++i)
				{
					// Generate (repeatable) random bytes
					byte[] bytes = new byte[32];
					new Random(i).NextBytes(bytes);

					yield return new BasicModel()
					{
						// Halfway the id skips to 1.5 times the sample size
						Id = i == sampleSize / 2 ? (int)(sampleSize * 1.5) : (int?)null,
						Boolean = i >= sampleSize / 2 ? true : false,
						Integer = i,
						Decimal = i * Math.PI,
						Text = "sample_" + i,
						DateTime = new DateTime(1970 + i, 1, 1),
						Enum = (BasicModelEnum)(i % ((int)BasicModelEnum.Value8 + 1)),
						Blob = bytes
					};
				}
			}
		}

		[TestMethod]
		[TestCategory("Create Table")]
		public void CreateBasicModelTable() => Database.CreateTable<BasicModel>();

		[TestMethod]
		[TestCategory("Delete")]
		[DataRow(0, DisplayName = "Delete collection")]
		[DataRow(1, DisplayName = "Delete with condition")]
		public void DeleteBasicModel(int deleteMode)
		{
			Database.CreateTable<BasicModel>();

			// Insert the test data
			var sample = BasicModelSample.Select(x => { x.Text = "DeleteTest"; return x; }).ToArray();
			Database.Insert<BasicModel>(sample);

			// Delete the newly inserted data
			var deletedCount = deleteMode switch
			{
				0 => Database.Delete<BasicModel>(sample),
				1 => Database.Delete<BasicModel>("`Text` = 'DeleteTest'"),
				_ => throw new ArgumentOutOfRangeException(nameof(deleteMode))
			};

			// Check if the query deleted the correct amount of elements
			Assert.AreEqual(sample.Length, deletedCount, "The query did not delete the correct amount of elements.");
		}

		[TestMethod]
		[TestCategory("Insert")]
		public void InsertBasicModel()
		{
			Database.CreateTable<BasicModel>();

			// Insert the test data
			var sample = BasicModelSample.ToArray();
			var lastInsertId = Database.Insert<BasicModel>(sample);

			// Verify the returned rowid
			Assert.AreEqual((int)(sample.Length * 1.5 + (sample.Length - 1) / 2), lastInsertId, "The returned LAST_INSERT_ROWID did not have the expected value.");

			// Verify that the id's have changed
			Assert.IsFalse(sample.Any(x => x.Id == null), "The primary keys have not been replaced.");

			// Verify the sequence of the id's
			var expectedIds = new int[sample.Length].Select((x, i) => i >= sample.Length / 2 ? i + sample.Length : i + 1).ToArray();
			Assert.IsTrue(expectedIds.SequenceEqual(sample.Select(x => x.Id.Value)), "The primary keys did not follow the expected sequence.");
		}

		[TestMethod]
		[TestCategory("Select")]
		[DataRow(0, DisplayName = "Select all")]
		[DataRow(1, DisplayName = "Select with condition")]
		public void SelectBasicModel(int selectMode)
		{
			Database.CreateTable<BasicModel>();

			// Insert the test data
			var sample = BasicModelSample.Select(x => { x.Text = "SelectTest"; return x; }).ToArray();
			Database.Insert<BasicModel>(sample);

			// Compare the sample with selected data
			var items = selectMode switch
			{
				0 => Database.Select<BasicModel>(),
				1 => Database.Select<BasicModel>("`Text` = 'SelectTest'"),
				_ => throw new ArgumentOutOfRangeException(nameof(selectMode))
			};
			Assert.IsTrue(sample.SequenceEqual(items), "The sample did not match the selected data.");
		}

		[TestMethod]
		[TestCategory("Update")]
		public void UpdateBasicModel()
		{
			Database.CreateTable<BasicModel>();

			// Insert the test data
			var sample = BasicModelSample.ToArray();
			Database.Insert<BasicModel>(sample);

			// Swap the values in the sample
			for (int i = 0, j = sample.Length - 1; i < sample.Length / 2; ++i, --j)
			{
				// Copy their primary keys
				int id_i = sample[i].Id.Value, id_j = sample[j].Id.Value;

				// Swap primary keys
				sample[i].Id = id_j;
				sample[j].Id = id_i;
				// Swap array posiion
				var temp = sample[i];
				sample[i] = sample[j];
				sample[j] = temp;
			}

			var updatedCount = Database.Update<BasicModel>(sample);

			// Verify the amount of updated items
			Assert.AreEqual(sample.Length % 2 == 0 ? sample.Length : sample.Length - 1, updatedCount, "The amount of updated elements did not match the expected amount.");

			// Compare the sample with selected data
			var items = Database.Select<BasicModel>();
			Assert.IsTrue(sample.SequenceEqual(items), "The sample did not match the selected data.");
		}
	}
}
