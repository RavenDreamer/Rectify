using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RectifyUtils;

namespace RectifyTest
{
	[TestClass]
	public class CreateRectNodesTest
	{

		[TestMethod]
		public void FunctionalAndImperativeAreEquivalentTest()
		{
			var impResult = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles());

			var funcResult = Rectify.GetRectNodesFunctional(TestData.BinaryConcaveShapeNoHoles());

			int maxWidth = impResult.GetLength(1);
			int maxHeight = impResult.GetLength(0);

			for (int h = 0; h < maxHeight; h++)
			{
				for (int w = 0; w < maxWidth; w++)
				{
					Assert.AreEqual(impResult[h, w], funcResult[h, w]);
				}
			}
		}

		[TestMethod]
		public void BinaryConcaveShapeNoHolesMakeRectNodesTest()
		{
			var result = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles());

			Assert.AreEqual(12, result.GetLength(0), "width not dimension 0");
			Assert.AreEqual(17, result.GetLength(1), "height not dimension 1");

			//check walls on map edges for 0,0
			Assert.AreEqual(EdgeType.Wall, result[0, 0].Edges.West, "West border not a wall");
			Assert.AreEqual(EdgeType.Wall, result[0, 0].Edges.North, "North border not a wall");
			Assert.AreEqual(EdgeType.None, result[0, 0].Edges.East, "East border not a none");
			Assert.AreEqual(EdgeType.None, result[0, 0].Edges.South, "South border not a none");

			//check walls on map edges for 4,0
			Assert.AreEqual(EdgeType.None, result[4, 0].Edges.West, "West border not a none");
			Assert.AreEqual(EdgeType.Wall, result[4, 0].Edges.North, "North border not a wall");
			Assert.AreEqual(EdgeType.Wall, result[4, 0].Edges.East, "East border not a wall");
			Assert.AreEqual(EdgeType.Wall, result[4, 0].Edges.South, "South border not a wall");

			//check walls on map edges for 5,0
			Assert.AreEqual(EdgeType.Wall, result[5, 0].Edges.West, "West border not a wall");
			Assert.AreEqual(EdgeType.Wall, result[5, 0].Edges.North, "North border not a wall");
			Assert.AreEqual(EdgeType.None, result[5, 0].Edges.East, "East border not a none");
			Assert.AreEqual(EdgeType.None, result[5, 0].Edges.South, "South border not a none");

			//check walls on map edges for 0,16
			Assert.AreEqual(EdgeType.Wall, result[0, 16].Edges.West, "West border not a wall");
			Assert.AreEqual(EdgeType.None, result[0, 16].Edges.North, "North border not a none");
			Assert.AreEqual(EdgeType.None, result[0, 16].Edges.East, "East border not a none");
			Assert.AreEqual(EdgeType.Wall, result[0, 16].Edges.South, "South border not a wall");

			//check walls on map edges for 5,10
			Assert.AreEqual(EdgeType.None, result[5, 10].Edges.West, "West border not a none");
			Assert.AreEqual(EdgeType.None, result[5, 10].Edges.North, "North border not a none");
			Assert.AreEqual(EdgeType.None, result[5, 10].Edges.East, "East border not a none");
			Assert.AreEqual(EdgeType.None, result[5, 10].Edges.South, "South border not a none");

			//check walls on map edges for 1,1
			Assert.AreEqual(EdgeType.None, result[1, 1].Edges.West, "West border not a none");
			Assert.AreEqual(EdgeType.None, result[1, 1].Edges.North, "North border not a none");
			Assert.AreEqual(EdgeType.None, result[1, 1].Edges.East, "East border not a none");
			Assert.AreEqual(EdgeType.None, result[1, 1].Edges.South, "South border not a none");
		}

		[TestMethod]
		public void BinaryConcaveShapeNoHolesTraverseShapesTest()
		{
			var result = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles());
			var output = Rectify.TraverseShapeOutlines(result);

			Assert.AreEqual(4, output.Count, "Did not traverse 4 shapes as expected");
			Assert.AreEqual(52, output[0].Perimeter.Count, "Top Left Shape not edged as expected");
			Assert.AreEqual(62, output[1].Perimeter.Count, "Yellow Shape not edged as expected");
			Assert.AreEqual(34, output[2].Perimeter.Count, "Top Right Shape not edged as expected");
			Assert.AreEqual(22, output[3].Perimeter.Count, "Bottom Right Shape not edged as expected");

			//52 edges (west)
			//62 edges (yellow)
			//34 edges (top right)
			//22 edges (bottom right)
		}

		[TestMethod]
		public void BinaryConcaveShapeNoHolesMakePolygonsTest()
		{
			var result = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles());
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			Assert.AreEqual(4, polygons.Count, "Did not maintain 4 shapes as expected");
			Assert.AreEqual(12, polygons[0].Vertices.Count, "Top Left Shape not verticed as expected");
			Assert.AreEqual(22, polygons[1].Vertices.Count, "Yellow Shape not verticed as expected");
			Assert.AreEqual(8, polygons[2].Vertices.Count, "Top Right Shape not verticed as expected");
			Assert.AreEqual(6, polygons[3].Vertices.Count, "Bottom Right Shape not verticed as expected");

			Assert.AreEqual(4, polygons[0].Vertices.FindAll(v => v.IsConcave == true).Count, "Had wrong number of concave vertices");
			Assert.AreEqual(9, polygons[1].Vertices.FindAll(v => v.IsConcave == true).Count, "Had wrong number of concave vertices");
			Assert.AreEqual(2, polygons[2].Vertices.FindAll(v => v.IsConcave == true).Count, "Had wrong number of concave vertices");
			Assert.AreEqual(1, polygons[3].Vertices.FindAll(v => v.IsConcave == true).Count, "Had wrong number of concave vertices");
		}

		[TestMethod]
		public void BinaryConcaveShapeNoHolesPickChordsTest()
		{
			var result = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles());
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[1]);

			Assert.AreEqual(4, subpolygons.Count, "Didn't get 4 subpolygons as expected");
			Assert.AreEqual(8, subpolygons[0].Vertices.Count, "Had wrong number of vertices for bottom stair-step shape");
			Assert.AreEqual(4, subpolygons[1].Vertices.Count, "Had wrong number of vertices for bottom middle thin shape");
			Assert.AreEqual(6, subpolygons[2].Vertices.Count, "Had wrong number of vertices for top bent shape");
			Assert.AreEqual(4, subpolygons[3].Vertices.Count, "Had wrong number of vertices for squarish shape");

			//perimeters are traversable.
			foreach (var sp in subpolygons)
			{
				bool cycled = false;
				RectEdge start = sp.Perimeter[0];
				RectEdge next = start.Next;
				for (int i = 0; i < 999; i++)
				{
					if (next == start)
					{
						cycled = true;
						break;
					}
					next = next.Next;
				}
				if (cycled == false)
				{
					Assert.Fail("Perimeter did not cycle");
				}

				foreach (RectEdge re in sp.Perimeter)
				{
					Position p = re.SecondPosition - re.Next.FirstPosition;
					if (p.Magnitude != 0)
					{
						Assert.Fail("Two edges were not end-to-end");
					}
					Position q = re.FirstPosition - re.Next.FirstPosition;
					if (q.Magnitude != 1)
					{
						Assert.Fail("Two edges were further than 1 apart");
					}
				}

			}
		}

		[TestMethod]
		public void BinaryConcaveShapeNoHolesSecondLevelDecompTest()
		{
			var result = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles());
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[1]);

			var subsubPolygons = new List<RectShape>();
			foreach (var sp in subpolygons)
			{
				subsubPolygons.AddRange(Rectify.SecondLevelDecomposition(sp));
			}

			Assert.AreEqual(7, subsubPolygons.Count, "Did not decomp into the minimum 7 polygons");

			foreach (RectShape rs in subsubPolygons)
			{
				Assert.AreEqual(4, rs.Vertices.Count, "2nd level decomp with > 4 verts");
			}

			//Assert.AreEqual(8, subpolygons[0].Vertices.Count, "Had wrong number of vertices for bottom stair-step shape");
			//Assert.AreEqual(4, subpolygons[1].Vertices.Count, "Had wrong number of vertices for bottom middle thin shape");
			//Assert.AreEqual(6, subpolygons[2].Vertices.Count, "Had wrong number of vertices for top bent shape");
			//Assert.AreEqual(4, subpolygons[3].Vertices.Count, "Had wrong number of vertices for squarish shape");

			//bottom stair step shape decomps
			Assert.AreEqual(14, subsubPolygons[0].Perimeter.Count, "Had wrong perimeter east-most bottom shape");
			Assert.AreEqual(18, subsubPolygons[1].Perimeter.Count, "Had wrong perimeter middle bottom shape");
			Assert.AreEqual(6, subsubPolygons[2].Perimeter.Count, "Had wrong perimeter west-most bottom shape");

			//bottom middle thin shape
			Assert.AreEqual(22, subsubPolygons[3].Perimeter.Count, "Had wrong perimeter for bottom middle thin shape");

			//top bent shape
			Assert.AreEqual(6, subsubPolygons[4].Perimeter.Count, "Had wrong perimeter for top-most top shape");
			Assert.AreEqual(12, subsubPolygons[5].Perimeter.Count, "Had wrong perimeter for west-most top shape");

			//squarish shape
			Assert.AreEqual(24, subsubPolygons[6].Perimeter.Count, "Had wrong perimeter for squarsh shape");
		}

		[TestMethod]
		public void KoenigsAlgorithmAlternatingPathTest()
		{



			var data = TestData.GetAlternatingNodesMatching();

			var uList = data.Item1.Select(ree => ree.FirstEdge);//maxmatching is <horizontal, vertical>, so only need to look for the firstEdge
			List<RectFlowNode> zNodes = new List<RectFlowNode>(data.Item2.FindAll(hn => hn.FlowType == FlowType.horizontal && uList.Contains(hn.Edge) == false));

			var result = Rectify.FindAlternatingPathVerts(data.Item1, zNodes);

			Assert.AreEqual(10, result.Count, "alternating path didn't return 10 items as expected");
		}
	}
}
