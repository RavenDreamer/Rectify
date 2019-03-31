using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RectifyUtils;

namespace RectifyTest
{
	[TestClass]
	public class RectifyPathfinderTest
	{
		[TestMethod]
		[TestCategory("Pathfinder")]
		public void BasicPathfinderTest()
		{
			var result = Rectify.MakeRectangles(TestData.OneDRectangleTest());

			var pathfinder = new RectifyPathfinder(result);

			var resultPath = pathfinder.CalculatePath(new Position(0, 0), new Position(7, 0), (int)EdgeType.None);
			Assert.AreEqual(5, resultPath.Count, "Didn't find straight-line path as expected");

			resultPath = pathfinder.CalculatePath(new Position(0, 3), new Position(7, 0), (int)EdgeType.None);
			Assert.AreEqual(4, resultPath.Count, "Didn't find 4-length path as expected");
		}
	}
}
