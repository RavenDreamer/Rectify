using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RectifyUtils;
using static RectifyUtils.RectifyPathfinder;

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

			var pathfinder = new RectifyPathfinder(result, false);

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

			var pathfinder = new RectifyPathfinder(result, false);

			var resultPath = pathfinder.CalculatePath(new Position(0, 6), new Position(7, 6), (int)EdgeType.None + (int)EdgeType.Wall);
			Assert.AreEqual(5, resultPath.Count, "Didn't find straight-line path as expected");
		}

		[TestMethod]
		[TestCategory("Pathfinder")]
		public void NoPathPathfinderTest()
		{
			var result = Rectify.MakeRectangles(TestData.OneDRectangleTest(), DataLayout.CodeInitializedArray);

			var pathfinder = new RectifyPathfinder(result, false);

			var resultPath = pathfinder.CalculatePath(new Position(0, 1), new Position(3, 6), (int)EdgeType.None);
			Assert.AreEqual(0, resultPath.Count, "Didn't fail to find a path as expected");
		}

		[TestMethod]
		[TestCategory("Pathfinder")]
		public void CachePathTest()
		{
			//The caching was removed, so this test will fail. The prior method of caching worked _pretty_ well, but needs some refinements.
			//could run into an issue where navigating to two adjacent cells cut the corner due to the caching optimistically assuming the diagonal could
			//be taken when it could not. (See TestSingleCellObstruction)

			//I suspect there's a better way to handle caching. Perhaps some way based on the distance from the rectangle traversal edges?


			List<RectDetectPair> edges = new List<RectDetectPair>
			{
				new RectDetectPair(0, 3, EdgeType.Aperture)
			};

			var result = Rectify.MakeRectangles(TestData.UnityModifiedDesertTitans(), DataLayout.CodeInitializedArray, edgeOverrides: edges);

			var pathfinder = new RectifyPathfinder(result, false);

			var resultPath = pathfinder.CalculatePath(new Position(109, 147), new Position(150, 75), out PathfinderMetrics metrics, (int)EdgeType.None | (int)EdgeType.Aperture);
			Assert.AreEqual(44, resultPath.Count, "fail to find a path as expected");

			//same overall path, this should hit the cache.
			resultPath = pathfinder.CalculatePath(new Position(109, 145), new Position(150, 75), out PathfinderMetrics metricsAfterCache, (int)EdgeType.None | (int)EdgeType.Aperture);

			Assert.AreNotEqual(0, metrics.VisitedNodes, "pathfound w/o traversing nodes somehow");
			Assert.AreEqual(0, metricsAfterCache.VisitedNodes, "pathfound w/o using the cache as intended");

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

			var pathfinder = new RectifyPathfinder(result, false);

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

			var pathfinder = new RectifyPathfinder(result, false);

			var resultPath = pathfinder.CalculatePath(new Position(0, 0), new Position(3, 2), (int)EdgeType.None | (int)EdgeType.Aperture);
			Assert.AreNotEqual(0, resultPath.Count, "failed to find a path as expected");
		}

		[TestMethod]
		[TestCategory("Pathfinder")]
		public void TestAddToRectangles()
		{
			var result = Rectify.MakeRectangles(TestData.UniformRectangle(), DataLayout.CodeInitializedArray);
			Assert.AreEqual(1, result.Count, "Did not get single rectangle as expected");

			var pathfinder = new RectifyPathfinder(result, false);

			pathfinder.ReplaceCellAt(new Position(2, 2), 4);

			Assert.AreEqual(5, pathfinder.NodeCount, "Did not get 5 total rectangles as expected");

			var resultPath = pathfinder.CalculatePath(new Position(0, 0), new Position(2, 2));

			Assert.AreEqual(0, resultPath.Count, "found a path when none expected");
		}

		[TestMethod]
		[TestCategory("Pathfinder")]
		public void TestAddToTorus()
		{
			var result = Rectify.MakeRectangles(TestData.BigTorusTest(), DataLayout.CodeInitializedArray);
			Assert.AreEqual(9, result.Count, "Did not get 9 rectangles as expected");

			var pathfinder = new RectifyPathfinder(result, false);

			pathfinder.ReplaceCellAt(new Position(3, 3), 4);

			Assert.AreEqual(11, pathfinder.NodeCount, "Did not get the 11 total rectangles as expected");

			//These succeed because a wall already exists between the two regions.
			var resultPath = pathfinder.CalculatePath(new Position(3, 3), new Position(2, 2));
			Assert.AreEqual(0, resultPath.Count, "found a path when none expected");

			resultPath = pathfinder.CalculatePath(new Position(2, 2), new Position(3, 3));
			Assert.AreEqual(0, resultPath.Count, "found a path when none expected");
		}

		[TestMethod]
		[TestCategory("Pathfinder")]
		public void TestBlockPaths()
		{
			var result = Rectify.MakeRectangles(TestData.KeyholeTest(), DataLayout.CodeInitializedArray);
			Assert.AreEqual(5, result.Count, "Did not get 5 initial rectangles as expected");

			var pathfinder = new RectifyPathfinder(result, false);

			pathfinder.ReplaceCellAt(new Position(2, 2), 2);

			Assert.AreEqual(5, pathfinder.NodeCount, "Did not get the 5 total rectangles expected");

			var resultPath = pathfinder.CalculatePath(new Position(2, 3), new Position(2, 1));
			Assert.AreEqual(0, resultPath.Count, "found a path when none expected");

			resultPath = pathfinder.CalculatePath(new Position(2, 2), new Position(2, 3));
			Assert.AreEqual(0, resultPath.Count, "found a path when none expected");

			resultPath = pathfinder.CalculatePath(new Position(1, 2), new Position(3, 2));
			Assert.AreEqual(3, resultPath.Count, "Did not find a path where expected");

			resultPath = pathfinder.CalculatePath(new Position(2, 2), new Position(1, 2));
			Assert.AreEqual(2, resultPath.Count, "Did not find a path where expected");

		}

		[TestMethod]
		[TestCategory("LatticePathfinder")]
		public void TestBlockPathsVertical()
		{
			var result = Rectify.MakeRectangles(GridLatticeTestData.VertKeyholeApertureLattice());
			Assert.AreEqual(2, result.Count, "Did not get 2 initial rectangles as expected");

			var pathfinder = new RectifyPathfinder(result, true);

			var resultPath = pathfinder.CalculatePath(new Position(3, 2), new Position(1, 2));
			Assert.AreEqual(3, resultPath.Count, "Did not find a path where expected");

			pathfinder.ReplaceCellEdgeAt(new Position(2, 2), Direction.West, EdgeType.Wall);

			resultPath = pathfinder.CalculatePath(new Position(3, 2), new Position(1, 2));
			Assert.AreEqual(0, resultPath.Count, "Found a path where none expected");

		}

		[TestMethod]
		[TestCategory("LatticePathfinder")]
		public void TestBlockPathsLattice()
		{
			var result = Rectify.MakeRectangles(GridLatticeTestData.KeyholeApertureLattice());
			Assert.AreEqual(2, result.Count, "Did not get 2 initial rectangles as expected");

			var pathfinder = new RectifyPathfinder(result, true);

			var resultPath = pathfinder.CalculatePath(new Position(2, 3), new Position(2, 1));
			Assert.AreEqual(3, resultPath.Count, "Did not find a path where expected");

			pathfinder.ReplaceCellEdgeAt(new Position(2, 2), Direction.North, EdgeType.Wall);

			Assert.AreEqual(5, pathfinder.NodeCount, "Did not get the 5 total rectangles expected");


			resultPath = pathfinder.CalculatePath(new Position(2, 2), new Position(2, 3));
			Assert.AreEqual(0, resultPath.Count, "did not find a path when expected");

			resultPath = pathfinder.CalculatePath(new Position(2, 2), new Position(2, 1));
			Assert.AreEqual(2, resultPath.Count, "found a path when none expected");


			resultPath = pathfinder.CalculatePath(new Position(1, 2), new Position(2, 2));
			Assert.AreEqual(2, resultPath.Count, "Did not find a path where expected");

		}

		[TestMethod]
		[TestCategory("LatticePathfinder")]
		public void SequentialEdgeAdditionTest()
		{
			var result = Rectify.MakeRectangles(GridLatticeTestData.EmptyGridLattice());
			var pathfinder = new RectifyPathfinder(result, true);

			//add edges to the same cell one after another

			pathfinder.ReplaceCellEdgeAt(new Position(1, 1), Direction.West, EdgeType.Wall);
			var resultPath = pathfinder.CalculatePath(new Position(0, 1), new Position(1, 1));
			Assert.AreNotEqual(2, resultPath.Count, "Did not path around wall edge as expected");

			pathfinder.ReplaceCellEdgeAt(new Position(1, 1), Direction.South, EdgeType.Wall);
			resultPath = pathfinder.CalculatePath(new Position(0, 1), new Position(1, 1));
			Assert.AreNotEqual(2, resultPath.Count, "Did not path around wall edge as expected");

			pathfinder.ReplaceCellEdgeAt(new Position(1, 1), Direction.East, EdgeType.Wall);
			resultPath = pathfinder.CalculatePath(new Position(0, 0), new Position(2, 0));
			Assert.AreEqual(3, resultPath.Count, "Did not path around wall edge as expected");

		}

		[TestMethod]
		[TestCategory("LatticePathfinder")]
		public void TestSingleCellObstruction()
		{
			var result = Rectify.MakeRectangles(GridLatticeTestData.EmptyGridLattice(10));
			var pathfinder = new RectifyPathfinder(result, true);

			pathfinder.ReplaceCellAt(new Position(2, 4), 7);
			var resultPath = pathfinder.CalculatePath(new Position(3, 4), new Position(1, 3));
			Assert.AreEqual(4, resultPath.Count, "path was not length 4 as expected");
			resultPath = pathfinder.CalculatePath(new Position(3, 4), new Position(1, 4));
			Assert.AreEqual(5, resultPath.Count, "path was not length 5 as expected");
		}

		[TestMethod]
		[TestCategory("From GridLattice")]
		public void StardewFarmPathingTest()
		{
			//var result = Rectify.MakeRectangles(TestData.StardewTestData(), new Position(0, 0), new Position(55, 39));
			//var result = Rectify.MakeRectangles(TestData.StardewTestData(), new Position(0, 0), new Position(50, 25));
			var result = Rectify.MakeRectangles(TestData.StardewTestData());
			Assert.AreEqual(195, result.Count);
		}

		[TestMethod]
		[TestCategory("LatticePathfinder")]
		public void BiggerSequentialEdgeAdditionTest()
		{
			var result = Rectify.MakeRectangles(GridLatticeTestData.EmptyGridLattice(10));
			var pathfinder = new RectifyPathfinder(result, true);

			//add edges to the same cell one after another

			pathfinder.ReplaceCellEdgeAt(new Position(6, 4), Direction.West, EdgeType.Wall);

			pathfinder.ReplaceCellEdgeAt(new Position(6, 4), Direction.South, EdgeType.Wall);

			pathfinder.ReplaceCellEdgeAt(new Position(7, 4), Direction.West, EdgeType.Wall);

			var resultPath = pathfinder.CalculatePath(new Position(3, 3), new Position(8, 2));
			Assert.AreNotEqual(0, resultPath.Count, "Did not find expected path");

		}

		[TestMethod]
		[TestCategory("LatticePathfinder")]
		public void TestLatticeCornerPathing()
		{
			var result = Rectify.MakeRectangles(GridLatticeTestData.CornersLattice());
			Assert.AreEqual(14, result.Count, "Did not get 23 initial rectangles as expected");

			var pathfinder = new RectifyPathfinder(result, true);

			var resultPath = pathfinder.CalculatePath(new Position(0, 0), new Position(1, 0));
			Assert.AreEqual(2, resultPath.Count, "Did not find a path of 2 where expected");
			Assert.AreEqual(new Position(0, 0), resultPath[0]);
			Assert.AreEqual(new Position(1, 0), resultPath[1]);
		}

		[TestMethod]
		[TestCategory("LatticePathfinder")]
		public void TestNoPathNecessary()
		{
			var result = Rectify.MakeRectangles(GridLatticeTestData.HorizBisectedLattice());
			var pathfinder = new RectifyPathfinder(result, true);

			var resultPath = pathfinder.CalculatePath(new Position(2, 0), new Position(2, 4));
			Assert.AreEqual(0, resultPath.Count, "Did not generate a zero path as expected");

			pathfinder.ReplaceCellEdgeAt(new Position(2, 2), Direction.North, EdgeType.None);

			resultPath = pathfinder.CalculatePath(new Position(2, 0), new Position(2, 4));
			Assert.AreNotEqual(0, resultPath.Count, "Did not generate a non-zero path as expected");

		}

		[TestMethod]
		[TestCategory("LatticePathfinder")]
		public void TestPathAfterNewEdges()
		{
			var result = Rectify.MakeRectangles(GridLatticeTestData.EmptyGridLattice(10, 10));
			var pathfinder = new RectifyPathfinder(result, true);

			for (int i = 0; i < 10; i++)
			{
				pathfinder.ReplaceCellEdgeAt(new Position(7, i), Direction.West, EdgeType.Wall);
			}

			var resultPath = pathfinder.CalculatePath(new Position(0, 6), new Position(9, 5));
			Assert.AreEqual(0, resultPath.Count, "Did not generate a zero path as expected");

			pathfinder.ReplaceCellEdgeAt(new Position(5, 2), Direction.North, EdgeType.Wall);

			resultPath = pathfinder.CalculatePath(new Position(0, 6), new Position(9, 5));
			Assert.AreEqual(0, resultPath.Count, "Did not generate a zero path as expected");
		}

		[TestMethod]
		[TestCategory("LatticePathfinder")]
		public void TestPathTranslate()
		{
			var result = Rectify.MakeRectangles(GridLatticeTestData.CornersLattice());

			var pathfinder = new RectifyPathfinder(result, true);

			var resultPath = pathfinder.CalculatePath(new Position(3, 6), new Position(6, 4));

			var resultPathAlt = pathfinder.CalculatePath(new Position(6, 4), new Position(3, 6));

			//Due to changes in underlying code, this test is moot.
		}

		[TestMethod]
		[TestCategory("Pathfinder")]
		public void TestCacheInvalidated()
		{
			var result = Rectify.MakeRectangles(TestData.BigKeyholeTest(), DataLayout.CodeInitializedArray);
			var pathfinder = new RectifyPathfinder(result, false);

			var resultPath = pathfinder.CalculatePath(new Position(0, 0), new Position(0, 5));
			Assert.AreEqual(8, resultPath.Count, "Did not find a path where expected");

			pathfinder.ReplaceCellAt(new Position(2, 4), 2);

			//either the caching algorithm isn't really working in the first place, or the checks we're already doing
			//wind up failling with the new rectangles.

			//This test is moot.
			resultPath = pathfinder.CalculatePath(new Position(0, 0), new Position(0, 5));
			Assert.AreEqual(0, resultPath.Count, "Found previous path, cache not invalidated");
		}

		[TestMethod]
		[TestCategory("AStarPathfinder")]
		public void TestAStarBlockPathsLattice()
		{
			var result = GridLatticeTestData.KeyholeApertureLattice();

			var pathfinder = new RectifyAStarPathfinder(result);

			var resultPath = pathfinder.CalculatePath(new Position(2, 3), new Position(2, 0));
			Assert.AreEqual(4, resultPath.Count, "Did not find a path where expected");

			result[2, 2, Direction.North] = new RectGridCell(8, 1);
			pathfinder = new RectifyAStarPathfinder(result);

			resultPath = pathfinder.CalculatePath(new Position(2, 2), new Position(2, 3));
			Assert.AreEqual(0, resultPath.Count, "did not find a path when expected");

			resultPath = pathfinder.CalculatePath(new Position(2, 2), new Position(2, 1));
			Assert.AreEqual(2, resultPath.Count, "found a path when none expected");

			resultPath = pathfinder.CalculatePath(new Position(1, 2), new Position(2, 2));
			Assert.AreEqual(2, resultPath.Count, "Did not find a path where expected");

		}

	}
}
