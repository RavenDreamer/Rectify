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

		//[TestMethod]
		//public void FunctionalAndImperativeAreEquivalentTest()
		//{
		//	var impResult = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles(), DataLayout.CodeInitializedArray);

		//	var funcResult = Rectify.GetRectNodesFunctional(TestData.BinaryConcaveShapeNoHoles());

		//	int maxWidth = impResult.GetLength(1);
		//	int maxHeight = impResult.GetLength(0);

		//	for (int h = 0; h < maxHeight; h++)
		//	{
		//		for (int w = 0; w < maxWidth; w++)
		//		{
		//			Assert.AreEqual(impResult[h, w], funcResult[h, w]);
		//		}
		//	}
		//}

		[TestMethod]
		public void BinaryConcaveShapeNoHolesMakeRectNodesTest()
		{
			var result = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles(), DataLayout.CodeInitializedArray);

			Assert.AreEqual(12, result.GetLength(0), "width not dimension 0");
			Assert.AreEqual(17, result.GetLength(1), "height not dimension 1");

			//check walls on map edges for 0,0
			Assert.AreEqual(EdgeType.Wall, result[0, 0].Edges.West, "West border not a wall");
			Assert.AreEqual(EdgeType.Wall, result[0, 0].Edges.South, "South border not a wall");
			Assert.AreEqual(EdgeType.None, result[0, 0].Edges.East, "East border not a none");
			Assert.AreEqual(EdgeType.None, result[0, 0].Edges.North, "North border not a none");

			//check walls on map edges for 4,0
			Assert.AreEqual(EdgeType.None, result[4, 0].Edges.West, "West border not a none");
			Assert.AreEqual(EdgeType.Wall, result[4, 0].Edges.South, "South border not a wall");
			Assert.AreEqual(EdgeType.Wall, result[4, 0].Edges.East, "East border not a wall");
			Assert.AreEqual(EdgeType.None, result[4, 0].Edges.North, "North border not a none");

			//check walls on map edges for 5,0
			Assert.AreEqual(EdgeType.Wall, result[5, 0].Edges.West, "West border not a wall");
			Assert.AreEqual(EdgeType.Wall, result[5, 0].Edges.North, "North border not a wall");
			Assert.AreEqual(EdgeType.None, result[5, 0].Edges.East, "East border not a none");
			Assert.AreEqual(EdgeType.Wall, result[5, 0].Edges.South, "South border not a wall");

			//check walls on map edges for 0,16
			Assert.AreEqual(EdgeType.Wall, result[0, 16].Edges.West, "West border not a wall");
			Assert.AreEqual(EdgeType.None, result[0, 16].Edges.South, "South border not a none");
			Assert.AreEqual(EdgeType.None, result[0, 16].Edges.East, "East border not a none");
			Assert.AreEqual(EdgeType.Wall, result[0, 16].Edges.North, "North border not a wall");

			//check walls on map edges for 6,10
			Assert.AreEqual(EdgeType.None, result[6, 10].Edges.West, "West border not a none");
			Assert.AreEqual(EdgeType.None, result[6, 10].Edges.North, "North border not a none");
			Assert.AreEqual(EdgeType.None, result[6, 10].Edges.East, "East border not a none");
			Assert.AreEqual(EdgeType.None, result[6, 10].Edges.South, "South border not a none");

			//check walls on map edges for 1,1
			Assert.AreEqual(EdgeType.None, result[1, 1].Edges.West, "West border not a none");
			Assert.AreEqual(EdgeType.None, result[1, 1].Edges.North, "North border not a none");
			Assert.AreEqual(EdgeType.None, result[1, 1].Edges.East, "East border not a none");
			Assert.AreEqual(EdgeType.None, result[1, 1].Edges.South, "South border not a none");
		}

		[TestMethod]
		public void BinaryConcaveShapeNoHolesTraverseShapesTest()
		{
			var result = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);

			Assert.AreEqual(4, output.Count, "Did not traverse 4 shapes as expected");
			Assert.AreEqual(52, output[0].Perimeter.Count, "Top Left Shape not edged as expected");
			Assert.AreEqual(62, output[1].Perimeter.Count, "Yellow Shape not edged as expected");
			Assert.AreEqual(34, output[3].Perimeter.Count, "Top Right Shape not edged as expected");
			Assert.AreEqual(22, output[2].Perimeter.Count, "Bottom Right Shape not edged as expected");

			//52 edges (west)
			//62 edges (yellow)
			//34 edges (top right)
			//22 edges (bottom right)
		}

		[TestMethod]
		public void BinaryConcaveShapeNoHolesMakePolygonsTest()
		{
			var result = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			Assert.AreEqual(4, polygons.Count, "Did not maintain 4 shapes as expected");
			Assert.AreEqual(12, polygons[0].Vertices.Count, "Top Left Shape not verticed as expected");
			Assert.AreEqual(22, polygons[1].Vertices.Count, "Yellow Shape not verticed as expected");
			Assert.AreEqual(8, polygons[3].Vertices.Count, "Top Right Shape not verticed as expected");
			Assert.AreEqual(6, polygons[2].Vertices.Count, "Bottom Right Shape not verticed as expected");

			Assert.AreEqual(4, polygons[0].Vertices.FindAll(v => v.IsConcave == true).Count, "Had wrong number of concave vertices");
			Assert.AreEqual(9, polygons[1].Vertices.FindAll(v => v.IsConcave == true).Count, "Had wrong number of concave vertices");
			Assert.AreEqual(2, polygons[3].Vertices.FindAll(v => v.IsConcave == true).Count, "Had wrong number of concave vertices");
			Assert.AreEqual(1, polygons[2].Vertices.FindAll(v => v.IsConcave == true).Count, "Had wrong number of concave vertices");
		}

		[TestMethod]
		public void BinaryConcaveShapeNoHolesPickChordsTest()
		{
			var result = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[1]);

			Assert.AreEqual(4, subpolygons.Count, "Didn't get 4 subpolygons as expected");
			Assert.AreEqual(8, subpolygons[0].Vertices.Count, "Had wrong number of vertices for bottom stair-step shape");
			Assert.AreEqual(4, subpolygons[2].Vertices.Count, "Had wrong number of vertices for bottom middle thin shape");
			Assert.AreEqual(6, subpolygons[1].Vertices.Count, "Had wrong number of vertices for top bent shape");
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
		public void SquareWithSquareHolePickChordsTest()
		{
			var result = Rectify.GetRectNodes(TestData.SquareWithSquareHole(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[0]);

			Assert.AreEqual(1, subpolygons.Count, "Didn't get 1 subpolygon as expected");
			Assert.AreEqual(4, subpolygons[0].Vertices.Count, "Had wrong number of vertices for main square");

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

			var subsubPolygons = new List<RectShape>();
			foreach (var sp in subpolygons)
			{
				subsubPolygons.AddRange(Rectify.SecondLevelDecomposition(sp));
			}

			//perimeters are traversable.
			foreach (var sp in subsubPolygons)
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

				Assert.IsTrue(sp.Vertices.Count == 4, "Not a rectangle, had more than 4 verts");
			}

			Assert.AreEqual(4, subsubPolygons.Count, "Did not decomp into the minimum 4 polygons");
		}

		[TestMethod]
		public void SquareWithTwoSquareHolesPickChordsTest()
		{
			var result = Rectify.GetRectNodes(TestData.SquareWithTwoSquareHole(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[0]);

			Assert.AreEqual(1, subpolygons.Count, "Didn't get 1 subpolygon as expected");
			Assert.AreEqual(4, subpolygons[0].Vertices.Count, "Had wrong number of vertices for main square");

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

			var subsubPolygons = new List<RectShape>();
			foreach (var sp in subpolygons)
			{
				subsubPolygons.AddRange(Rectify.SecondLevelDecomposition(sp));
			}

			//perimeters are traversable.
			foreach (var sp in subsubPolygons)
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

				Assert.IsTrue(sp.Vertices.Count == 4, "Not a rectangle, had more than 4 verts");
			}

			Assert.AreEqual(7, subsubPolygons.Count, "Did not decomp into the minimum 7 polygons");
		}

		[TestMethod]
		public void SquareWithCogridHolesPickChordsTest()
		{
			var result = Rectify.GetRectNodes(TestData.SquareWithCogridHoles(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[0]);

			Assert.AreEqual(1, subpolygons.Count, "Didn't get 1 subpolygon as expected");
			Assert.AreEqual(4, subpolygons[0].Vertices.Count, "Had wrong number of vertices for main square");

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

			var subsubPolygons = new List<RectShape>();
			foreach (var sp in subpolygons)
			{
				subsubPolygons.AddRange(Rectify.SecondLevelDecomposition(sp));
			}

			//perimeters are traversable.
			foreach (var sp in subsubPolygons)
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

				Assert.IsTrue(sp.Vertices.Count == 4, "Not a rectangle, had more than 4 verts");
			}

			Assert.AreEqual(6, subsubPolygons.Count, "Did not decomp into the minimum 6 polygons");
		}

		[TestMethod]
		public void OneDRectangleVertexTest()
		{
			var result = Rectify.GetRectNodes(TestData.OneDRectangleTest(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[0]);

			Assert.AreEqual(1, subpolygons.Count, "Didn't get 1 subpolygon as expected");
			Assert.AreEqual(4, subpolygons[0].Vertices.Count, "Had wrong number of vertices for main square");

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

			var subsubPolygons = new List<RectShape>();
			foreach (var sp in subpolygons)
			{
				subsubPolygons.AddRange(Rectify.SecondLevelDecomposition(sp));
			}

			//perimeters are traversable.
			foreach (var sp in subsubPolygons)
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

				Assert.IsTrue(sp.Vertices.Count == 4, "Not a rectangle, had more than 4 verts");
			}

			Assert.AreEqual(4, subsubPolygons.Count, "Did not decomp into the minimum 4 polygons");
		}

		[TestMethod]
		public void HoleSelfCutTest()
		{
			var result = Rectify.GetRectNodes(TestData.SquareWithSelfHoleCut(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[0]);

			Assert.AreEqual(2, subpolygons.Count, "Didn't get 2 subpolygon as expected");
			Assert.AreEqual(4, subpolygons[0].Vertices.Count, "Had wrong number of vertices for main square");
			List<RectShape> subsubPolygons = ValidateRects(subpolygons);

			Assert.AreEqual(5, subsubPolygons.Count, "Did not decomp into the minimum 5 polygons");
		}

		[TestMethod]
		public void HoleSelfCutWithCogridCompanionTest()
		{
			var result = Rectify.GetRectNodes(TestData.SelfHoleCutWithCogridSide(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[1]);

			Assert.AreEqual(2, subpolygons.Count, "Didn't get 2 subpolygon as expected");
			List<RectShape> subsubPolygons = ValidateRects(subpolygons);

			Assert.AreEqual(6, subsubPolygons.Count, "Did not decomp into the minimum 6 polygons");
		}

		[TestMethod]
		public void DisjointHatTest()
		{
			var result = Rectify.GetRectNodes(TestData.DisjointHat(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[0]);

			Assert.AreEqual(3, subpolygons.Count, "Didn't get 3 subpolygon as expected");
			Assert.AreEqual(4, subpolygons[0].Vertices.Count, "Had wrong number of vertices for main square");
			List<RectShape> subsubPolygons = ValidateRects(subpolygons);

			Assert.AreEqual(8, subsubPolygons.Count, "Did not decomp into the minimum 8 polygons");
		}

		[TestMethod]
		public void MetoriteSiteTest()
		{
			var result = Rectify.GetRectNodes(TestData.MeteorStrike(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[0]);

			Assert.AreEqual(1, subpolygons.Count, "Didn't get 1 subpolygon as expected");
			List<RectShape> subsubPolygons = ValidateRects(subpolygons);

			Assert.AreEqual(8, subsubPolygons.Count, "Did not decomp into the minimum 8 polygons");
		}

		private static List<RectShape> ValidateRects(List<RectShape> subpolygons)
		{
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

			var subsubPolygons = new List<RectShape>();
			foreach (var sp in subpolygons)
			{
				subsubPolygons.AddRange(Rectify.SecondLevelDecomposition(sp));
			}

			//perimeters are traversable.
			foreach (var sp in subsubPolygons)
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

				Assert.IsTrue(sp.Vertices.Count == 4, "Not a rectangle, had more than 4 verts");
			}

			return subsubPolygons;
		}

		[TestMethod]
		public void MakeRectanglesTest()
		{
			var result = Rectify.MakeRectangles(TestData.BinaryConcaveShapeNoHoles(), DataLayout.CodeInitializedArray);
			Assert.AreEqual(17, result.Count, "did not make 17 total rectangles as expected");
		}

		[TestMethod]
		public void EastEdgeTest()
		{
			var result = Rectify.MakeRectangles(TestData.EastEdge(), DataLayout.CodeInitializedArray);
			Assert.AreEqual(26, result.Count, "did not make 26 total rectangles as expected");
		}

		[TestMethod]
		public void MeteorWallTest()
		{
			var result = Rectify.MakeRectangles(TestData.MeteorWall(), DataLayout.CodeInitializedArray);
			Assert.AreEqual(21, result.Count, "did not make 21 total rectangles as expected");
		}

		[TestMethod]
		public void BaseEdgeTest()
		{
			var result = Rectify.MakeRectangles(TestData.BaseEdge(), DataLayout.CodeInitializedArray);
			Assert.AreEqual(36, result.Count, "did not make 36 total rectangles as expected");
		}

		[TestMethod]
		public void VertexConcavityTest()
		{
			var result = Rectify.MakeRectangles(TestData.VertexConcavity(), DataLayout.CodeInitializedArray);
			Assert.AreEqual(10, result.Count, "did not make 10 total rectangles as expected");
		}

		[TestMethod]
		public void NeighborsInitialTest()
		{
			var result = Rectify.MakeRectangles(TestData.OneDRectangleTest(), DataLayout.CodeInitializedArray);
			Assert.AreEqual(3, result[0].NeighborCount, "0,0::3,2 has 3 neighbors (2 east, 1 south)");
			Assert.AreEqual(3, result[1].NeighborCount, "0,2::8,8 has 3 neighbors (3 north)");
			Assert.AreEqual(3, result[2].NeighborCount, "4,0::8,2 has 3 neighbors (2 west, 1 south)");
			Assert.AreEqual(3, result[3].NeighborCount, "3,0::4,1 has 3 neighbors (1 east, 1 west, 1 south)");
			Assert.AreEqual(4, result[4].NeighborCount, "3,1::4,2 has 4 neighbors (1 east, 1 west, 1 south, 1 north)");
		}

		[TestMethod]
		public void RoomHolesTest()
		{

			var result = Rectify.GetRectNodes(TestData.RoomHoles(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);

			Assert.AreEqual(2, output[0].Holes.Count, "Did not get 2 holes as expected");
		}

		[TestMethod]
		public void ExciseHoleTest()
		{


			var result = Rectify.GetRectNodes(TestData.ExcisedHoleIsland(), DataLayout.CodeInitializedArray);
			var output = Rectify.TraverseShapeOutlines(result);
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = new List<RectShape>();
			foreach (var p in polygons)
			{
				subpolygons.AddRange(Rectify.FirstLevelDecomposition(p));
			}

			foreach (RectShape rs in subpolygons)
			{
				Assert.IsTrue(rs.Holes.Count < 2, "Did not get less than 2 holes as expected");
			}

		}

		[TestMethod]
		public void MakeRimworldSaveTest()
		{
			//break this down into pieces.
			List<RectifyRectangle> result = null;
			//result = Rectify.MakeRectangles(TestData.DesertTitans(), new Position(0, 0), new Position(50, 50)); //completes w/o error
			//result = Rectify.MakeRectangles(TestData.DesertTitans(), new Position(50, 0), new Position(100, 50)); //completes w/o error
			//result = Rectify.MakeRectangles(TestData.DesertTitans(), new Position(100, 0), new Position(275, 50)); //completes w/o error
			//result = Rectify.MakeRectangles(TestData.DesertTitans(), new Position(100, 50), new Position(150, 100)); //completes w/o error
			//result = Rectify.MakeRectangles(TestData.DesertTitans(), new Position(200, 50), new Position(275, 150)); //completes w/o error
			//result = Rectify.MakeRectangles(TestData.DesertTitans(), new Position(0, 150), new Position(50, 274)); //completes w/o error //for whatever reason, only 274 height. Missed a row originally, I guess?
			//result = Rectify.MakeRectangles(TestData.DesertTitans(), new Position(150, 150), new Position(250, 200)); //completes w/o error
			//result = Rectify.MakeRectangles(TestData.DesertTitans(), new Position(0, 25), new Position(100, 200)); //completes w/o error
			//result = Rectify.MakeRectangles(TestData.DesertTitans(), new Position(100, 0), new Position(275, 200)); //completes w/o error

			//result = Rectify.MakeRectangles(TestData.DesertTitans(), new Position(15, 0), new Position(275, 274));

			result = Rectify.MakeRectangles(TestData.DesertTitans(), DataLayout.CodeInitializedArray);
			Assert.AreEqual(724, result.Count, "Expect 724 rectangles, probably");

			//let's make sure that no rectangle overlaps. That would be super sad :(
			for (int i = 0; i < result.Count; i++)
			{
				var rr = result[i];
				for (int j = i + 1; j < result.Count; j++)
				{
					var rr_other = result[j];

					Assert.IsFalse(
						rr.Left < rr_other.Right && rr.Right > rr_other.Left &&
						rr.Top > rr_other.Bottom && rr.Bottom < rr_other.Top, "Intersecting rectangles :(");
				}
			}
		}

		[TestMethod]
		[TestCategory("From GridLattice")]
		public void RectNodesFromEmptyGridLatticeTest()
		{
			List<RectifyRectangle> result = Rectify.MakeRectangles(GridLatticeTestData.EmptyGridLattice());
			Assert.AreEqual(1, result.Count, "Didn't get the single Rectangle as expected");
		}

		[TestMethod]
		[TestCategory("From GridLattice")]
		public void RectNodesFromGridLatticeTest()
		{
			//List<RectifyRectangle> result = Rectify.MakeRectangles(GridLatticeTestData.EdgeTorusGridLattice());
			//Assert.AreEqual(5, result.Count, "Didn't get the 5 Rectangles expected");

			//List<RectifyRectangle> result2 = Rectify.MakeRectangles(GridLatticeTestData.CenterCellTorusGridLattice());
			//Assert.AreEqual(5, result2.Count, "Didn't get the 5 Rectangles expected");

			List<RectifyRectangle> result3 = Rectify.MakeRectangles(GridLatticeTestData.SingleHorizEdgeGridLattice());
			Assert.AreEqual(5, result3.Count, "Didn't get the 5 Rectangles expected");

			List<RectifyRectangle> result4 = Rectify.MakeRectangles(GridLatticeTestData.SingleVertEdgeGridLattice());
			Assert.AreEqual(5, result4.Count, "Didn't get the 5 Rectangles expected");
		}



		[TestMethod]
		public void BinaryConcaveShapeNoHolesSecondLevelDecompTest()
		{
			var result = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles(), DataLayout.CodeInitializedArray);
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
			Assert.AreEqual(6, subsubPolygons[1].Perimeter.Count, "Had wrong perimeter middle bottom shape");
			Assert.AreEqual(18, subsubPolygons[2].Perimeter.Count, "Had wrong perimeter west-most bottom shape");

			//bottom middle thin shape
			Assert.AreEqual(22, subsubPolygons[6].Perimeter.Count, "Had wrong perimeter for bottom middle thin shape");

			//top bent shape
			//I wonder if these are swapping b/c Find is non deterministic?
			Assert.AreEqual(12, subsubPolygons[3].Perimeter.Count, "Had wrong perimeter for top-most top shape");
			Assert.AreEqual(6, subsubPolygons[4].Perimeter.Count, "Had wrong perimeter for west-most top shape");

			//squarish shape
			Assert.AreEqual(24, subsubPolygons[5].Perimeter.Count, "Had wrong perimeter for squarsh shape");
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
