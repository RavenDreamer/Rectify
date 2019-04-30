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
			var result = Rectify.MakeRectangles(TestData.OneDRectangleTest(), DataLayout.CodeInitializedArray);

			var pathfinder = new RectifyPathfinder(result);

			var resultPath = pathfinder.CalculatePath(new Position(0, 7), new Position(6, 7), (int)EdgeType.None);
			Assert.AreEqual(5, resultPath.Count, "Didn't find straight-line path as expected");

			resultPath = pathfinder.CalculatePath(new Position(0, 4), new Position(6, 7), (int)EdgeType.None);
			Assert.AreEqual(4, resultPath.Count, "Didn't find 8-length path as expected");
		}

		[TestMethod]
		[TestCategory("Pathfinder")]
		public void MultiEdgePathfinderTest()
		{
			var result = Rectify.MakeRectangles(TestData.OneDRectangleTest(), DataLayout.CodeInitializedArray);

			var pathfinder = new RectifyPathfinder(result);

			var resultPath = pathfinder.CalculatePath(new Position(0, 6), new Position(7, 6), (int)EdgeType.None + (int)EdgeType.Wall);
			Assert.AreEqual(5, resultPath.Count, "Didn't find straight-line path as expected");
		}

		[TestMethod]
		[TestCategory("Pathfinder")]
		public void NoPathPathfinderTest()
		{
			var result = Rectify.MakeRectangles(TestData.OneDRectangleTest(), DataLayout.CodeInitializedArray);

			var pathfinder = new RectifyPathfinder(result);

			var resultPath = pathfinder.CalculatePath(new Position(0, 1), new Position(3, 6), (int)EdgeType.None);
			Assert.AreEqual(0, resultPath.Count, "Didn't fail to find a path as expected");
		}

		[TestMethod]
		[TestCategory("Pathfinder")]
		public void CachePathTest()
		{

			List<RectDetectPair> edges = new List<RectDetectPair>
			{
				new RectDetectPair(0, 3, EdgeType.Aperture)
			};



			var result = Rectify.MakeRectangles(TestData.UnityModifiedDesertTitans(), DataLayout.CodeInitializedArray, edgeOverrides: edges);

			var pathfinder = new RectifyPathfinder(result);

			var resultPath = pathfinder.CalculatePath(new Position(109, 147), new Position(150, 75), (int)EdgeType.None | (int)EdgeType.Aperture);
			//resultPath = pathfinder.CalculatePath(new Position(119, 99), new Position(150, 63));

			//throw new NotImplementedException("Need to figure out how to cache the RSR results");

			Assert.AreEqual(44, resultPath.Count, "fail to find a path as expected");
		}

		[TestMethod]
		[TestCategory("Pathfinder")]
		public void LPathTest()
		{

			List<RectDetectPair> edges = new List<RectDetectPair>
			{
				new RectDetectPair(0, 3, EdgeType.Aperture)
			};

			var result = Rectify.MakeRectangles(TestData.LPathTest(), DataLayout.CodeInitializedArray, edgeOverrides: edges);

			var pathfinder = new RectifyPathfinder(result);

			var resultPath = pathfinder.CalculatePath(new Position(1, 6), new Position(7, 0), (int)EdgeType.None | (int)EdgeType.Aperture);
			Assert.AreNotEqual(13, resultPath.Count, "failed to find a path as expected");
		}

		[TestMethod]
		[TestCategory("Pathfinder")]
		public void TestPathToRoom()
		{

			List<RectDetectPair> edges = new List<RectDetectPair>
			{
				new RectDetectPair(0, 3, EdgeType.Aperture)
			};


			var result = Rectify.MakeRectangles(TestData.SealedRoomTest(), DataLayout.CodeInitializedArray, edgeOverrides: edges);

			var pathfinder = new RectifyPathfinder(result);

			var resultPath = pathfinder.CalculatePath(new Position(0, 0), new Position(3, 2), (int)EdgeType.None | (int)EdgeType.Aperture);
			Assert.AreNotEqual(0, resultPath.Count, "failed to find a path as expected");
		}
	}
}
