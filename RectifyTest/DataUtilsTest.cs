using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RectifyUtils;

namespace RectifyTest
{
	[TestClass]
	public class DataUtilsTest
	{

		[TestMethod]
		[TestCategory("DataUtils")]
		public void TwoDArrayInitializedDataTest()
		{
			var initialData = new string[,]
			{
				{ "02","12","22","32", },
				{ "01","11","21","31", },
				{ "00","10","20","30", },
			};

			var rotatedData = RectifyDataUtils.RotateAndSwapData(initialData);

			Assert.AreNotEqual("00", initialData[0, 0], "data was not rotated as expected");

			Assert.AreEqual("00", rotatedData[0, 0], "Data was not rotated successfully as expected");
			Assert.AreEqual("32", rotatedData[3, 2], "Data was not rotated successfully as expected");
			Assert.AreEqual("30", rotatedData[3, 0], "Data was not rotated successfully as expected");
			Assert.AreEqual("02", rotatedData[0, 2], "Data was not rotated successfully as expected");
		}
	}
}
