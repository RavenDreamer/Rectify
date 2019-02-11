using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace RectifyUtils
{
	public class Rectify
	{


		/// <summary>
		/// Iterate over the entries in the array. For each entry, compare with neighbors
		/// to create an array of RectNodes for futher processing. Data in [height, width] 
		/// format to support passing data via instantiated arrays (see TestData)
		/// </summary>
		/// <param name="data"></param>
		public static RectNode[,] GetRectNodes(int[,] data, Position targetminXY = null, Position targetmaxXY = null)
		{
			int maxWidth = data.GetLength(1);
			int maxHeight = data.GetLength(0);

			int lowX;
			int lowY;
			int highX;
			int highY;
			if (targetminXY == null)
			{
				lowX = 0;
				lowY = 0;
			}
			else
			{
				lowX = targetminXY.xPos;
				lowY = targetminXY.yPos;
			}

			if (targetmaxXY == null)
			{
				highX = maxWidth;
				highY = maxHeight;
			}
			else
			{
				highX = targetmaxXY.xPos;
				highY = targetmaxXY.yPos;
			}

			RectNode[,] output = new RectNode[highX - lowX, highY - lowY];

			for (int h = lowY; h < highY; h++)
			{
				for (int w = lowX; w < highX; w++)
				{
					//for each cell, look at its neighbors
					//this isn't maximally efficient, but suffices for testing for now
					int dataCell = data[h, w];
					RectNode rNode = new RectNode();

					//west; if we're in-bounds and the westPeek is the same value, no wall
					if (w > lowX && data[h, w - 1] == dataCell)
					{
						rNode.Edges.West = EdgeType.None;
					}
					else
					{
						rNode.Edges.West = EdgeType.Wall;
					}

					//East; if we're in-bounds and the eastPeek is the same value, no wall
					if (w < highX - 1 && data[h, w + 1] == dataCell)
					{
						rNode.Edges.East = EdgeType.None;
					}
					else
					{
						rNode.Edges.East = EdgeType.Wall;
					}

					//South; if we're in-bounds and the southPeek is the same value, no wall
					//this *is* South despite the weird offsets, because our data is in quadrant II
					if (h < highY - 1 && data[h + 1, w] == dataCell)
					{
						rNode.Edges.South = EdgeType.None;
					}
					else
					{
						rNode.Edges.South = EdgeType.Wall;
					}

					//North; if we're in-bounds and the northPeek is the same value, no wall
					//this *is* North despite the weird offsets, because our data is in quadrant II
					if (h > lowY && data[h - 1, w] == dataCell)
					{
						rNode.Edges.North = EdgeType.None;
					}
					else
					{
						rNode.Edges.North = EdgeType.Wall;
					}

					//this will translate the width x height into a top-left Quadrant II type grid
					output[w - lowX, h - lowY] = rNode;
				}
			}

			return output;
		}

		static readonly Position North = new Position(0, -1);
		static readonly Position East = new Position(1, 0);
		static readonly Position West = new Position(-1, 0);
		static readonly Position South = new Position(0, 1);

		static readonly List<DirectionVector> eachDirection = new List<DirectionVector>() {
			new DirectionVector() {
				Direction = Direction.East,
				Vector = East
			},
			new DirectionVector() {
				Direction = Direction.South,
				Vector = South
			},
			new DirectionVector() {
				Direction = Direction.West,
				Vector = West
			},
			new DirectionVector() {
				Direction = Direction.North,
				Vector = North
			}
		};

		public static RectNode[,] GetRectNodesFunctional(int[,] data)
		{
			int maxWidth = data.GetLength(1);
			int maxHeight = data.GetLength(0);

			RectNode[,] output = new RectNode[maxWidth, maxHeight];

			for (int h = 0; h < maxHeight; h++)
			{
				for (int w = 0; w < maxWidth; w++)
				{
					//for each cell, look at its neighbors
					//this isn't maximally efficient, but suffices for testing for now
					int dataCell = data[h, w];
					RectNode rNode = new RectNode();
					Position gridPosition = new Position(w, h);

					foreach (var direction in eachDirection)
					{
						Position queryPosition = direction.Vector + gridPosition;

						//if we're in bounds                                            and the neighbor has a different weight
						if (PositionWithinBounds(queryPosition, maxWidth, maxHeight) && data[queryPosition.yPos, queryPosition.xPos] == dataCell)
						{
							rNode.SetEdge(direction, EdgeType.None);
						}
						else
						{
							rNode.SetEdge(direction, EdgeType.Wall);
						}

						//this will translate the width x height into a top-left Quadrant II type grid
						output[w, h] = rNode;
					}
				}
			}

			return output;
		}

		private static bool PositionWithinBounds(Position p, int maxWidth, int maxHeight)
		{
			//if x or y is less than zero, return false
			//if x or y is greater than or equal to the maximum, return false
			if (p.xPos >= 0 && p.xPos < maxWidth)
			{
				if (p.yPos >= 0 && p.yPos < maxHeight)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// From a grid of RectNodes, finds the list of edges that compose the constituent
		/// shapes. (Holes are determined in the next step).
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static List<RectShape> TraverseShapeOutlines(RectNode[,] data)
		{

			Dictionary<int, RectShape> retShapes = new Dictionary<int, RectShape>();

			int maxWidth = data.GetLength(0);
			int maxHeight = data.GetLength(1);
			int parentRegion = 0;

			for (int h = 0; h < maxHeight; h++)
			{
				for (int w = 0; w < maxWidth; w++)
				{
					//for each cell, check if there is a North edge.
					//because we intend to destroy the edges as we traverse the data,
					//the North edge should always be a true outer perimeter of the next shape.
					RectNode peekNode = data[w, h];
					if (peekNode.Edges.North != EdgeType.None && peekNode.Edges.North != EdgeType.BurnedHoleEdge)
					{

						//found a new shape. Create a new vertex at w,h, then traverse the edges
						Position firstVertex = new Position(w, h);

						//the first edge is always known because we just checked for it explicitly.
						RectEdge firstEdge = new RectEdge(firstVertex, firstVertex + East, peekNode.Edges.North);

						RectShape foundShape = TranscribeShape(firstVertex, firstEdge, peekNode, parentRegion, data, maxWidth, maxHeight);


						retShapes.Add(parentRegion, foundShape);
						parentRegion++;
					}
					//now that we've added the shape, check to see if there's a hole as well.
					//there's a hole if the y-1 node has a south border. (Since we're going top to bottom, that would be impossible UNLESS we're in a hole
					if (h != 0) //but we can skip the first row, ofcourse.
					{
						RectNode peekHole = data[w, h - 1];
						if (peekHole.Edges.South != EdgeType.None && peekHole.Edges.South != EdgeType.BurnedHoleEdge)
						{
							//we found a new hole. Treat this as a shape (because it is).
							Position firstHoleVertex = new Position(w, h);

							//the first edge is always known because we just checked for it explicitly.
							RectEdge firstHoleEdge = new RectEdge(firstHoleVertex + East, firstHoleVertex, peekHole.Edges.South);


							RectShape foundHole = TranscribeShape(firstHoleVertex + East, firstHoleEdge, peekHole, -1, data, maxWidth, maxHeight, true);

							//starting with peekHole, look north for a rectNode with parentRegion != -1. if you don't find it, look for a north edge with a 
							//burnedHoleEdge. If we find it, travel north until we find a burnedHoleEdge on the south border, then repeat.

							//because of how we trace holes, they should never be adjacent, so this should always find the containing parent shape
							//and never a fellow hole within the parent shape.

							int i_scanUpwards = h - 1;
							bool foundHoleEdge = false;
							while (i_scanUpwards >= 0)
							{
								RectNode holeParent = data[w, i_scanUpwards];

								if (foundHoleEdge == false)
								{
									if (holeParent.ParentRegion == -1)
									{
										//check for a hole border.
										if (holeParent.Edges.North == EdgeType.BurnedHoleEdge)
										{
											foundHoleEdge = true;
										}
										i_scanUpwards--;
										continue;
									}


									//found a parent ID. Look up which shape it is, and add foundHole as a hole on that shape.
									retShapes[holeParent.ParentRegion].AddHole(foundHole);
									break;
								}
								else
								{
									//look south for the other side of the hole
									if (holeParent.Edges.South == EdgeType.BurnedHoleEdge)
									{
										foundHoleEdge = false;
										continue;
									}
									i_scanUpwards--;
								}
							}
						}
					}
				}
			}

			return retShapes.Values.ToList();
		}

		/// <summary>
		/// From a known vertex starting position, discover the rest of the polygon, consuming the edges as we trace along the edges
		/// </summary>
		/// <param name="firstVertex"></param>
		/// <param name="firstEdge"></param>
		/// <param name="peekNode"></param>
		/// <param name="parentRegion"></param>
		/// <param name="data"></param>
		/// <param name="maxWidth"></param>
		/// <param name="maxHeight"></param>
		/// <returns></returns>
		private static RectShape TranscribeShape(Position firstVertex, RectEdge firstEdge, RectNode peekNode, int parentRegion, RectNode[,] data, int maxWidth, int maxHeight, bool isHole = false)
		{


			RectEdge workingEdge = firstEdge;

			HashSet<RectEdge> perimeterList = new HashSet<RectEdge>
						{
							workingEdge
						};

			if (isHole)
			{
				//starting from the other side if we're in a hole.
				ClearEdge(peekNode, Direction.South, parentRegion);
			}
			else
			{
				ClearEdge(peekNode, Direction.North, parentRegion); //clear out the edge as we traverse it
			}

			//keep looking for edges until we reach our starting point
			while (workingEdge.SecondPosition.Equals(firstVertex) == false)
			{

				int antiInfiniteLoop = perimeterList.Count;

				List<Direction> nextCellVectors = GetVectors(workingEdge, maxHeight, maxWidth);

				foreach (Direction d in nextCellVectors)
				{
					//check if there is an appropriate edge in the next cell for the given direction
					RectEdge edge = TraverseEdge(workingEdge, d, data, parentRegion, out peekNode);

					if (edge == null) continue; //no edge was traversed. Try the next direction

					perimeterList.Add(edge);
					workingEdge = edge;
					break;
				}

				//potentially add some anti-infinite loop help here
				if (antiInfiniteLoop == perimeterList.Count)
				{
					throw new Exception("Infinite loop detected");
				}
			}

			//at this point perimeterList has been filled out. Working edge is the last connector
			workingEdge.Next = firstEdge; //cycle is complete

			//and add shape to be returned
			return new RectShape
			{
				Perimeter = perimeterList.ToList()

			};
		}

		public static List<RectifyRectangle> MakeRectangles(int[,] v, Position minXY = null, Position maxXY = null)
		{
			var result = GetRectNodes(v, minXY, maxXY);
			var output = TraverseShapeOutlines(result);
			var polygons = FindVertsFromEdges(output);

			var subpolygons = new List<RectShape>();
			foreach (var p in polygons)
			{
				//var minX = p.Vertices.Min(vae => vae.VertPosition.xPos);
				//var maxX = p.Vertices.Max(vae => vae.VertPosition.xPos);
				//for (int i = minX; i <= maxX; i++)
				//{
				//	var edgeCounter = p.Perimeter.FindAll(e => e.SecondPosition.xPos == i && (e.HeadingDirection == Direction.East || e.HeadingDirection == Direction.West));
				//	if (edgeCounter.Count % 2 == 1)
				//	{
				//		Console.WriteLine("weerid");
				//	}
				//}


				subpolygons.AddRange(FirstLevelDecomposition(p));
			}

			var subsubPolygons = new List<RectShape>();
			foreach (var sp in subpolygons)
			{
				subsubPolygons.AddRange(SecondLevelDecomposition(sp));
			}

			var rectangles = new List<RectifyRectangle>();
			foreach (RectShape shape in subsubPolygons)
			{
				rectangles.Add(new RectifyRectangle(shape));
			}

			return rectangles;
		}

		///// <summary>
		///// Finds the initial list of cogrid chords to cut for decomposition
		///// </summary>
		///// <param name="rectShape"></param>
		//public static List<RectEdge> FindCuttingChords(RectShape rectShape)
		//{
		//	//Maintain two separate lists: horizontal cogrid chords, and vertical ones.
		//	//if either list is empty, do all of the other list. If both lists have values, have
		//	//to bust out the graph theory to figure out which chords to use.

		//	List<Vertex> concaves = rectShape.Vertices.FindAll(v => v.IsConcave == true);
		//	HashSet<RectEdge> vertEdges = new HashSet<RectEdge>();
		//	HashSet<RectEdge> horizEdges = new HashSet<RectEdge>();

		//	foreach (Vertex v in concaves)
		//	{
		//		//vertical cogrid chords
		//		List<Vertex> vertCogrids = concaves.FindAll(q => q.Vert.xPos == v.Vert.xPos && q.Vert.yPos != v.Vert.yPos);
		//		//it's only a valid chord if the shape doesn't have an edge at every step between the two verts.
		//		Vertex topV;
		//		Vertex bottomV;
		//		foreach (Vertex cg in vertCogrids)
		//		{
		//			if (cg.Vert.yPos > v.Vert.yPos)
		//			{
		//				bottomV = cg;
		//				topV = v;
		//			}
		//			else
		//			{
		//				bottomV = v;
		//				topV = cg;
		//			}
		//			//early out if we've already looked at these verts
		//			RectEdge prospectiveEdge = new RectEdge(topV.Vert, bottomV.Vert, EdgeType.None);
		//			if (vertEdges.Contains(prospectiveEdge)) continue;

		//			//look to see if there is an open edge space in any point between the two verts
		//			RectEdge interveningEdge = rectShape.Perimeter.Find(r => (r.FirstPosition.xPos == v.Vert.xPos &&
		//																			   r.FirstPosition.yPos < bottomV.Vert.yPos &&
		//																			   r.FirstPosition.yPos > topV.Vert.yPos) ||
		//																			   (r.SecondPosition.xPos == v.Vert.xPos &&
		//																			   r.SecondPosition.yPos < bottomV.Vert.yPos &&
		//																			   r.SecondPosition.yPos > topV.Vert.yPos));
		//			if (interveningEdge != null) continue; //can't construct chord, there's something in the way

		//			//this is a valid vertical chord
		//			vertEdges.Add(prospectiveEdge);

		//		}

		//		//horizontal cogrid chords
		//		List<Vertex> horizCogrids = concaves.FindAll(q => q.Vert.xPos != v.Vert.xPos && q.Vert.yPos == v.Vert.yPos);
		//		//it's only a valid chord if the shape doesn't have an edge at every step between the two verts.
		//		Vertex leftV;
		//		Vertex rightV;
		//		foreach (Vertex cg in horizCogrids)
		//		{
		//			if (cg.Vert.xPos > v.Vert.xPos)
		//			{
		//				rightV = cg;
		//				leftV = v;
		//			}
		//			else
		//			{
		//				rightV = v;
		//				leftV = cg;
		//			}
		//			//early out if we've already looked at these verts
		//			RectEdge prospectiveEdge = new RectEdge(leftV.Vert, rightV.Vert, EdgeType.None);
		//			if (horizEdges.Contains(prospectiveEdge)) continue;

		//			//look to see if there is an open edge space in any point between the two verts
		//			RectEdge interveningEdge = rectShape.Perimeter.Find(r => (r.FirstPosition.yPos == v.Vert.yPos &&
		//																			   r.FirstPosition.xPos < rightV.Vert.yPos &&
		//																			   r.FirstPosition.xPos > leftV.Vert.yPos) ||
		//																			   (r.SecondPosition.yPos == v.Vert.yPos &&
		//																			   r.SecondPosition.yPos < rightV.Vert.yPos &&
		//																			   r.SecondPosition.yPos > leftV.Vert.yPos));
		//			if (interveningEdge != null) continue; //can't construct chord, there's something in the way

		//			//this is a valid vertical chord
		//			horizEdges.Add(prospectiveEdge);
		//		}
		//	}
		//	//some special cases
		//	if (horizEdges.Count == 0 && vertEdges.Count == 0)
		//	{
		//		//no cogrid chords. Existing shape is fine to decompose as-is.
		//		return new List<RectEdge>() { };
		//	}
		//	if (horizEdges.Count == 0)
		//	{
		//		//only vertical cogrid chords; do all of them
		//		return vertEdges.ToList();
		//	}
		//	if (vertEdges.Count == 0)
		//	{
		//		//only horizontal cogrid chords; do all of them
		//		return horizEdges.ToList();
		//	}

		//	//need to further make a differentiation.
		//	return FindBipartiteChords(vertEdges.ToList(), horizEdges.ToList());
		//}

		///// <summary>
		///// Use Graph theory to determine which chords will result in the minimum number of rectangles
		///// </summary>
		///// <param name="vertEdges"></param>
		///// <param name="horizEdges"></param>
		///// <returns></returns>
		//private static List<RectEdge> FindBipartiteChords(List<RectEdge> vertEdges, List<RectEdge> horizEdges)
		//{
		//	//now we need to construct the bipartite flow graph

		//	RectFlowNode sourceNode = new RectFlowNode(null, FlowType.source);
		//	RectFlowNode sinkNode = new RectFlowNode(null, FlowType.sink);
		//	List<RectFlowNode> vertNodes = new List<RectFlowNode>();
		//	List<RectFlowNode> horizNodes = new List<RectFlowNode>();
		//	foreach (RectEdge hedge in horizEdges)
		//	{
		//		//source has connections to each horiz node (U nodes)
		//		RectFlowNode node = new RectFlowNode(hedge, FlowType.horizontal); //horizontal edges	
		//		sourceNode.AddLink(node);

		//		horizNodes.Add(node);
		//	}

		//	foreach (RectEdge vedge in vertEdges)
		//	{
		//		//sink has connections from each vert node (V nodes)
		//		RectFlowNode node = new RectFlowNode(vedge, FlowType.vertical); //vertical edges	
		//		node.AddLink(sinkNode);

		//		vertNodes.Add(node);
		//	}

		//	//nodes created, now set connections from U(horizontal) -> V(vertical).
		//	//for each edge in horizEdges or vertEdges, if they intersect or share a vertex, a connection exists
		//	//check for intersection
		//	foreach (RectFlowNode vNode in vertNodes)
		//	{
		//		foreach (RectFlowNode hNode in horizNodes)
		//		{
		//			//check for shared vertices
		//			if (vNode.Edge.FirstPosition.Equals(hNode.Edge.FirstPosition) ||
		//			   vNode.Edge.FirstPosition.Equals(hNode.Edge.SecondPosition) ||
		//			   vNode.Edge.SecondPosition.Equals(hNode.Edge.FirstPosition) ||
		//			   vNode.Edge.SecondPosition.Equals(hNode.Edge.SecondPosition))
		//			{
		//				//share a vertex, so add a link from vert to horiz.
		//				hNode.AddLink(vNode);
		//			}
		//			else if (LineSegmentsIntersect(vNode.Edge, hNode.Edge))
		//			{
		//				//intersect, so add a link from vert to horiz.
		//				hNode.AddLink(vNode);
		//			}
		//		}
		//	}

		//	//Perform maximum Flow algorithm. This will modify the data in-place.
		//	CalculateMaximumFlow(sourceNode, sinkNode);

		//	//at this point, we are interested in all horiz <-- vert link pairs. (horiz = U, vert = V)
		//	HashSet<RectEdgeEdge> maxMatching = new HashSet<RectEdgeEdge>();
		//	foreach (RectFlowNode vfn in vertNodes)
		//	{
		//		foreach (RectFlowNode dest in vfn.DestinationNodes)
		//		{
		//			//this may be redundant
		//			if (dest.FlowType == FlowType.horizontal)
		//			{
		//				maxMatching.Add(new RectEdgeEdge(dest.Edge, vfn.Edge, true));
		//			}
		//		}
		//	}

		//	//Kőnig’s theorem
		//	//To construct such a cover, let U be the set of unmatched vertices in L[horizontal] possibly empty), 
		//	//and let Z be the set of vertices that are either in U[horizontal] or are connected to U by alternating paths
		//	//(paths that alternate between edges that are in the matching and edges that are not in the matching).

		//	var uList = maxMatching.Select(ree => ree.FirstEdge);//maxmatching is <horizontal, vertical>, so only need to look for the firstEdge
		//	List<RectFlowNode> zNodes = new List<RectFlowNode>(horizNodes.FindAll(hn => uList.Contains(hn.Edge) == false));

		//	if (zNodes.Count == 0)
		//	{
		//		//edges to cut with are just U == the horizontal cuts from the matching.

		//		return maxMatching.ToList().Select(mm => mm.FirstEdge).ToList();
		//	}

		//	BuildBidirectionalGraph(horizNodes, vertNodes);

		//	var zEdges = FindAlternatingPathVerts(maxMatching, zNodes);
		//	//Set (min. vertex cover) == all U[horizontal] not in Z + all V[vertical] in Z
		//	//if Z == empty, this means Set == U!
		//	List<RectEdge> cuttingChords = new List<RectEdge>();

		//	cuttingChords.AddRange(horizEdges.Where(u => zEdges.Contains(u) == false));
		//	cuttingChords.AddRange(vertEdges.Where(v => zEdges.Contains(v) == true));

		//	return cuttingChords;
		//}

		/// <summary>
		/// For the given RectShape, find pairs of cogrid concave vertices and use this to split the object
		/// into subpolygons. If no cogrid concave vertices are found, return the input.
		/// </summary>
		/// <param name="rectShape"></param>
		/// <returns></returns>
		public static List<RectShape> FirstLevelDecomposition(RectShape rectShape)
		{
			//Maintain two separate lists: horizontal cogrid chords, and vertical ones.
			//if either list is empty, do all of the other list. If both lists have values, have
			//to bust out the graph theory to figure out which chords to use.

			List<Vertex> concaves = rectShape.Vertices.FindAll(v => v.IsConcave == true);
			HashSet<RectEdge> vertEdges = new HashSet<RectEdge>();
			HashSet<RectEdge> horizEdges = new HashSet<RectEdge>();

			//account for holes
			concaves.AddRange(rectShape.HoleVertices.FindAll(v => v.IsConcave == true));
			List<RectEdge> allPerimeters = new List<RectEdge>(rectShape.Perimeter);
			foreach (RectShape hole in rectShape.Holes)
			{
				allPerimeters.AddRange(hole.Perimeter);
			}

			foreach (Vertex v in concaves)
			{
				//vertical cogrid chords
				List<Vertex> vertCogrids = concaves.FindAll(q => q.VertPosition.xPos == v.VertPosition.xPos && q.VertPosition.yPos != v.VertPosition.yPos);
				//it's only a valid chord if the shape doesn't have an edge at every step between the two verts.
				Vertex topV;
				Vertex bottomV;
				foreach (Vertex cg in vertCogrids)
				{
					if (cg.VertPosition.yPos > v.VertPosition.yPos)
					{
						bottomV = cg;
						topV = v;
					}
					else
					{
						bottomV = v;
						topV = cg;
					}
					//early out if we've already looked at these verts
					RectEdge prospectiveEdge = new RectEdge(topV.VertPosition, bottomV.VertPosition, EdgeType.None);
					if (vertEdges.Contains(prospectiveEdge)) continue;

					//look to see if there is an open edge space in any point between the two verts
					//find an edge in the perimeters where EITHER first or second position have:
					// the x position equals the vertex x position and has a y Position that's below the bottom vert & above the top vert 
					RectEdge interveningEdge = allPerimeters.Find(r => (r.FirstPosition.xPos == v.VertPosition.xPos &&
																					   r.FirstPosition.yPos < bottomV.VertPosition.yPos &&
																					   r.FirstPosition.yPos > topV.VertPosition.yPos) ||
																					   (r.SecondPosition.xPos == v.VertPosition.xPos &&
																					   r.SecondPosition.yPos < bottomV.VertPosition.yPos &&
																					   r.SecondPosition.yPos > topV.VertPosition.yPos));

					if (interveningEdge != null) continue; //can't construct chord, there's something in the way

					//look to see if the two verts are adjacent on an edge (this may come up on 1x1 holes)
					RectEdge dupeEdge = allPerimeters.Find(r => r.FirstPosition.Equals(topV.VertPosition) && r.SecondPosition.Equals(bottomV.VertPosition) ||
																r.SecondPosition.Equals(topV.VertPosition) && r.FirstPosition.Equals(bottomV.VertPosition));

					if (dupeEdge != null) continue; //can't construct chord, there's something in the way


					//this is a valid vertical chord
					vertEdges.Add(prospectiveEdge);

				}

				//horizontal cogrid chords
				List<Vertex> horizCogrids = concaves.FindAll(q => q.VertPosition.xPos != v.VertPosition.xPos && q.VertPosition.yPos == v.VertPosition.yPos);
				//it's only a valid chord if the shape doesn't have an edge at every step between the two verts.
				Vertex leftV;
				Vertex rightV;
				foreach (Vertex cg in horizCogrids)
				{
					if (cg.VertPosition.xPos > v.VertPosition.xPos)
					{
						rightV = cg;
						leftV = v;
					}
					else
					{
						rightV = v;
						leftV = cg;
					}
					//early out if we've already looked at these verts
					RectEdge prospectiveEdge = new RectEdge(leftV.VertPosition, rightV.VertPosition, EdgeType.None);
					if (horizEdges.Contains(prospectiveEdge)) continue;

					//look to see if there is an open edge space in any point between the two verts
					RectEdge interveningEdge = allPerimeters.Find(r => (r.FirstPosition.yPos == v.VertPosition.yPos &&
																					   r.FirstPosition.xPos < rightV.VertPosition.xPos &&
																					   r.FirstPosition.xPos > leftV.VertPosition.xPos) ||
																					   (r.SecondPosition.yPos == v.VertPosition.yPos &&
																					   r.SecondPosition.xPos < rightV.VertPosition.xPos &&
																					   r.SecondPosition.xPos > leftV.VertPosition.xPos));
					if (interveningEdge != null) continue; //can't construct chord, there's something in the way

					//look to see if the two verts are adjacent on an edge (this may come up on 1x1 holes)
					RectEdge dupeEdge = allPerimeters.Find(r => r.FirstPosition.Equals(leftV.VertPosition) && r.SecondPosition.Equals(rightV.VertPosition) ||
																r.SecondPosition.Equals(leftV.VertPosition) && r.FirstPosition.Equals(rightV.VertPosition));

					if (dupeEdge != null) continue; //can't construct chord, there's something in the way

					//this is a valid vertical chord
					horizEdges.Add(prospectiveEdge);
				}
			}
			//some special cases
			if (horizEdges.Count == 0 && vertEdges.Count == 0)
			{
				//no cogrid chords. Existing shape is fine to decompose as-is.
				return new List<RectShape>() { rectShape };
			}
			if (horizEdges.Count == 0)
			{
				//only vertical cogrid chords; do all of them
				return CutShapeWithChords(rectShape, vertEdges.ToList());
			}
			if (vertEdges.Count == 0)
			{
				//only horizontal cogrid chords; do all of them
				return CutShapeWithChords(rectShape, horizEdges.ToList());
			}

			//now we need to construct the bipartite flow graph

			RectFlowNode sourceNode = new RectFlowNode(null, FlowType.source);
			RectFlowNode sinkNode = new RectFlowNode(null, FlowType.sink);
			List<RectFlowNode> vertNodes = new List<RectFlowNode>();
			List<RectFlowNode> horizNodes = new List<RectFlowNode>();
			foreach (RectEdge hedge in horizEdges)
			{
				//source has connections to each horiz node (U nodes)
				RectFlowNode node = new RectFlowNode(hedge, FlowType.horizontal); //horizontal edges	
				sourceNode.AddLink(node);

				horizNodes.Add(node);
			}

			foreach (RectEdge vedge in vertEdges)
			{
				//sink has connections from each vert node (V nodes)
				RectFlowNode node = new RectFlowNode(vedge, FlowType.vertical); //vertical edges	
				node.AddLink(sinkNode);

				vertNodes.Add(node);
			}

			//we always do links with no intersections. store them now for later.
			var isolatedVertEdges = new List<RectEdge>();

			//nodes created, now set connections from U(horizontal) -> V(vertical).
			//for each edge in horizEdges or vertEdges, if they intersect or share a vertex, a connection exists
			//check for intersection
			foreach (RectFlowNode vNode in vertNodes)
			{
				bool isReachable = false;

				foreach (RectFlowNode hNode in horizNodes)
				{
					//check for shared vertices
					if (vNode.Edge.FirstPosition.Equals(hNode.Edge.FirstPosition) ||
					   vNode.Edge.FirstPosition.Equals(hNode.Edge.SecondPosition) ||
					   vNode.Edge.SecondPosition.Equals(hNode.Edge.FirstPosition) ||
					   vNode.Edge.SecondPosition.Equals(hNode.Edge.SecondPosition))
					{
						//share a vertex, so add a link from vert to horiz.
						hNode.AddLink(vNode);
						isReachable = true;
					}
					else if (LineSegmentsIntersect(vNode.Edge, hNode.Edge))
					{
						//intersect, so add a link from vert to horiz.
						hNode.AddLink(vNode);
						isReachable = true;
					}
				}

				if (isReachable == false)
				{
					//node is isolated
					isolatedVertEdges.Add(vNode.Edge);
				}
			}

			//we always do links with no intersections. store them now for later.
			var isolatedHorizEdges = horizNodes.FindAll(rfn => rfn.DestinationNodes.Count == 0).Select(e => e.Edge);

			//Perform maximum Flow algorithm. This will modify the data in-place.
			CalculateMaximumFlow(sourceNode, sinkNode);

			//at this point, we are interested in all horiz <-- vert link pairs. (horiz = U, vert = V)
			HashSet<RectEdgeEdge> maxMatching = new HashSet<RectEdgeEdge>();
			foreach (RectFlowNode vfn in vertNodes)
			{
				foreach (RectFlowNode dest in vfn.DestinationNodes)
				{
					//this may be redundant
					if (dest.FlowType == FlowType.horizontal)
					{
						maxMatching.Add(new RectEdgeEdge(dest.Edge, vfn.Edge, true));
					}
				}
			}

			//Kőnig’s theorem
			//To construct such a cover, let U be the set of unmatched vertices in L[horizontal] possibly empty), 
			//and let Z be the set of vertices that are either in U[horizontal] or are connected to U by alternating paths
			//(paths that alternate between edges that are in the matching and edges that are not in the matching).

			var uList = maxMatching.Select(ree => ree.FirstEdge);//maxmatching is <horizontal, vertical>, so only need to look for the firstEdge
			List<RectFlowNode> zNodes = new List<RectFlowNode>(horizNodes.FindAll(hn => uList.Contains(hn.Edge) == false));

			if (zNodes.Count == 0)
			{
				//edges to cut with are just U == the horizontal cuts from the matching.

				return CutShapeWithChords(rectShape, maxMatching.ToList().Select(mm => mm.FirstEdge).ToList());
			}

			BuildBidirectionalGraph(horizNodes, vertNodes);

			var zEdges = FindAlternatingPathVerts(maxMatching, zNodes);
			//Set (min. vertex cover) == all U[horizontal] not in Z + all V[vertical] in Z
			//if Z == empty, this means Set == U!
			List<RectEdge> cuttingChords = new List<RectEdge>();

			cuttingChords.AddRange(horizEdges.Where(u => zEdges.Contains(u) == false));
			cuttingChords.AddRange(vertEdges.Where(v => zEdges.Contains(v) == true));

			//and add in isolated edges
			cuttingChords.AddRange(isolatedHorizEdges);
			cuttingChords.AddRange(isolatedVertEdges);

			return CutShapeWithChords(rectShape, cuttingChords);
		}

		/// <summary>
		/// For the given shape, perform slices from concave vertices until there are no more concave vertices
		/// </summary>
		/// <param name="shape"></param>
		/// <param name="useVertical"></param>
		/// <returns></returns>
		public static List<RectShape> SecondLevelDecomposition(RectShape shape, bool useVertical = true)
		{
			//construct the chord list 
			//cut chords from the shape until there are no more concave verts, then cut any remaining hole verts (there may not be any)
			List<RectShape> shapesToDecompose = new List<RectShape>() { shape };
			List<RectShape> rectangles = new List<RectShape>();

			int i = -1;
			while (shapesToDecompose.Count > 0)
			{
				i++;
				//find the next concave vert.
				RectShape workingShape = shapesToDecompose[0];
				Vertex workingVert = workingShape.Vertices.Find(v => v.IsConcave);
				RectEdge workingEdge = null;
				RectEdge prevEdge = null;
				//if no concave verts in shape, check for in holes
				if (workingVert == null)
				{
					workingVert = workingShape.HoleVertices.Find(v => v.IsConcave);
					//if no concave verts in holes (aka, no holes), shape is a bonafide rectangle.
					if (workingVert == null)
					{
						shapesToDecompose.Remove(workingShape);
						rectangles.Add(workingShape);
						continue;
					}
					else
					{
						RectShape workingHole = null;
						//find which hole has the vertex we found, then find the corresponding edge
						foreach (RectShape hole in workingShape.Holes)
						{
							if (hole.Vertices.Find(v => v == workingVert) != null)
							{
								workingHole = hole;
								prevEdge = hole.Perimeter.Find(e => e.Next.FirstPosition.Equals(workingVert.VertPosition));
								workingEdge = prevEdge.Next;
								break;
							}
						}

						var closestEdge = GetClosestEdge(workingShape, workingVert, workingEdge, prevEdge.HeadingDirection);

						//check if the closest edge is part of the working shapes perimeter
						if (workingShape.Perimeter.Contains(closestEdge))
						{
							//hole-shape cut

							shapesToDecompose.Add(CombineShapesUsingEdge(workingHole, workingShape, new RectEdge(new Position(workingVert.VertPosition), closestEdge.FirstPosition, EdgeType.None)));
							shapesToDecompose.Remove(workingShape);
							continue;
						}
						else
						{
							//hole-hole cut. combine the two holes
							//first, find the second hole.
							RectShape otherHole = null;
							foreach (RectShape hole in workingShape.Holes)
							{
								if (hole.Perimeter.Contains(closestEdge))
								{
									otherHole = hole;
									break;
								}
							}
							if (otherHole == null)
							{
								throw new Exception("Couldn't find 2nd hole");
							}

							if (otherHole == workingHole)
							{
								//special case for sectioning off a section of the hole and excising a rectangle(ish?) shape.
								List<RectShape> holeAndShape = CutShapeWithChords(otherHole, new RectEdge(workingEdge.FirstPosition, closestEdge.FirstPosition, EdgeType.None));
								//one of these is a hole, and the other one is a potentially concave shape.
								//the one that is NOT a hole is the one who has the vertices (from the cut) and
								//those vertices are NOT concave?

								workingShape.RemoveHole(workingHole);

								var potentialHole = holeAndShape[0];
								var otherPotentialHole = holeAndShape[1];

								//if this is a hole, there will be more concave verts than convex ones. (4 more)
								int concaveVertCount = potentialHole.Vertices.FindAll(v => v.IsConcave).Count;
								int convexVertCount = potentialHole.Vertices.Count - concaveVertCount;
								if (concaveVertCount < convexVertCount)
								{
									//potentialHole is not a hole, but otherPotentialHole is
									workingShape.AddHole(otherPotentialHole);

									//not actually a hole
									shapesToDecompose.Add(potentialHole);

									//keep workingShape in the shapesToDecompose list as well
								}
								else
								{
									workingShape.AddHole(potentialHole);

									//not actually a hole
									shapesToDecompose.Add(otherPotentialHole);
								}
								continue; //rerun this shape, now that it has one fewer holes
							}
							else
							{
								RectShape combinedHole = CombineShapesUsingEdge(workingHole, otherHole, new RectEdge(workingEdge.FirstPosition, closestEdge.FirstPosition, EdgeType.None));
								workingShape.RemoveHole(workingHole);
								workingShape.RemoveHole(otherHole);
								workingShape.AddHole(combinedHole);
								continue; //rerun this shape, now that it has one fewer holes
							}
						}
					}
				}
				//working on a concave vert in the shape (not a hole)
				else
				{
					prevEdge = workingShape.Perimeter.Find(e => e.Next.FirstPosition.Equals(workingVert.VertPosition));
					workingEdge = prevEdge.Next;

					var closestEdge = GetClosestEdge(workingShape, workingVert, workingEdge, prevEdge.HeadingDirection);

					//determine if closestEdge is in a hole or on the perimeter.
					if (workingShape.Perimeter.Contains(closestEdge))
					{
						//regular shape self-cut
						shapesToDecompose.AddRange(CutShapeWithChords(workingShape, new RectEdge(new Position(workingVert.VertPosition), closestEdge.FirstPosition, EdgeType.None)));
						shapesToDecompose.Remove(workingShape);
						continue;
					}
					else
					{
						RectShape workingHole = null;
						//find which hole has the perimeter we found, then find the corresponding edge
						foreach (RectShape hole in workingShape.Holes)
						{
							if (hole.Perimeter.Contains(closestEdge))
							{
								workingHole = hole;
								break;
							}
						}
						//shape-hole cut
						shapesToDecompose.Add(CombineShapesUsingEdge(workingShape, workingHole, new RectEdge(new Position(workingVert.VertPosition), closestEdge.FirstPosition, EdgeType.None)));
						shapesToDecompose.Remove(workingShape);
						continue;
					}


				}
			}



			return rectangles;
		}

		private static RectEdge GetClosestEdge(RectShape workingShape, Vertex workingVert, RectEdge workingEdge, Direction prevHeading)
		{
			//hole to cut. cut direction can be any direction except heading && opposite of prevNode's heading
			List<RectEdge> horizCutTargets = workingShape.Perimeter.FindAll(e => e.FirstPosition.yPos == workingVert.VertPosition.yPos && e.FirstPosition.xPos != workingVert.VertPosition.xPos);
			List<RectEdge> vertCutTargets = workingShape.Perimeter.FindAll(e => e.FirstPosition.xPos == workingVert.VertPosition.xPos && e.FirstPosition.yPos != workingVert.VertPosition.yPos);

			foreach (RectShape hole in workingShape.Holes)
			{
				horizCutTargets.AddRange(hole.Perimeter.FindAll(e => e.FirstPosition.yPos == workingVert.VertPosition.yPos && e.FirstPosition.xPos != workingVert.VertPosition.xPos));
				vertCutTargets.AddRange(hole.Perimeter.FindAll(e => e.FirstPosition.xPos == workingVert.VertPosition.xPos && e.FirstPosition.yPos != workingVert.VertPosition.yPos));
			}

			//cut opposite of the existing heading.
			switch (workingEdge.HeadingDirection)
			{
				case Direction.East:
					//keep only those west of the workingVertex
					horizCutTargets = horizCutTargets.FindAll(e => e.FirstPosition.xPos < workingVert.VertPosition.xPos);
					break;
				case Direction.South:
					//keep only those north of the working vertex
					vertCutTargets = vertCutTargets.FindAll(e => e.FirstPosition.yPos < workingVert.VertPosition.yPos);
					break;
				case Direction.West:
					//keep only those east of the workingVertex
					horizCutTargets = horizCutTargets.FindAll(e => e.FirstPosition.xPos > workingVert.VertPosition.xPos);
					break;
				case Direction.North:
					//keep only those south of the workingVertex
					vertCutTargets = vertCutTargets.FindAll(e => e.FirstPosition.yPos > workingVert.VertPosition.yPos);
					break;
			}

			//we're here because we changed directions, so the previous heading is one of the directions that we know
			//is "inwards" or "outwards" of the shape (instead of along the perimeter)
			switch (prevHeading)
			{
				case Direction.West:
					//keep only those west of the workingVertex
					horizCutTargets = horizCutTargets.FindAll(e => e.FirstPosition.xPos < workingVert.VertPosition.xPos);
					break;
				case Direction.North:
					//keep only those north of the working vertex
					vertCutTargets = vertCutTargets.FindAll(e => e.FirstPosition.yPos < workingVert.VertPosition.yPos);
					break;
				case Direction.East:
					//keep only those east of the workingVertex
					horizCutTargets = horizCutTargets.FindAll(e => e.FirstPosition.xPos > workingVert.VertPosition.xPos);
					break;
				case Direction.South:
					//keep only those south of the workingVertex
					vertCutTargets = vertCutTargets.FindAll(e => e.FirstPosition.yPos > workingVert.VertPosition.yPos);
					break;
			}


			//find the nearest of these
			List<RectEdge> closeEdges = new List<RectEdge>();
			closeEdges.AddRange(horizCutTargets);
			closeEdges.AddRange(vertCutTargets);

			return closeEdges.OrderBy(e => (e.FirstPosition - workingVert.VertPosition).Magnitude).First();
		}

		/// <summary>
		/// For each node in zNodes, find all unique vertices reachable via an "alternating" path. A path is alternating if
		/// it alternates between being an edge in the maxMatching matching, and not being an edge in the matching.
		/// </summary>
		/// <param name="maxMatching"></param>
		/// <param name="zNodes"></param>
		/// <returns></returns>
		public static HashSet<RectEdge> FindAlternatingPathVerts(HashSet<RectEdgeEdge> maxMatching, List<RectFlowNode> zNodes)
		{

			HashSet<RectEdge> altPathNodes = new HashSet<RectEdge>();


			HashSet<RectEdgeEdge> traversedLinksFromMatching = new HashSet<RectEdgeEdge>();
			HashSet<RectEdgeEdge> traversedLinksFromNonMatching = new HashSet<RectEdgeEdge>();


			//bool is "NextIsInMatching"
			List<Tuple<RectFlowNode, bool>> unvisited = new List<Tuple<RectFlowNode, bool>>(); //this is probably the IDE screaming to make a class for this

			//first, we need to look for any nodes reachable from zNodes that *aren't* in maxMatching.
			foreach (RectFlowNode rfn in zNodes)
			{
				unvisited.Add(new Tuple<RectFlowNode, bool>(rfn, false));
				altPathNodes.Add(rfn.Edge);
			}

			//need to do a breadth first search here, 
			while (unvisited.Count > 0)
			{
				//get next unvisted node, remove it from consideration.
				var pair = unvisited[0];
				unvisited.Remove(pair);

				//form a RectEdgeEdge from this node and all its destinations
				//add all valid alternating path vertices we haven't yet added
				foreach (RectFlowNode rfn in pair.Item1.DestinationNodes)
				{
					//construct a RectEdgeEdge and determine if it's part of the matching or not
					RectEdgeEdge next = new RectEdgeEdge(pair.Item1.Edge, rfn.Edge, false);

					//early out - check if visitedNodes contains rfn.Edge
					if (pair.Item2) //NextIsInMatching
					{
						//if next is in matching, update traversedLinksFromNonMatching
						if (traversedLinksFromNonMatching.Contains(next))
						{
							continue;
						}
						traversedLinksFromNonMatching.Add(next);
					}
					else //NextIsInMatching == false
					{
						//if next is NOT in matching, update traversedLinksFromMatching
						if (traversedLinksFromMatching.Contains(next))
						{
							continue;
						}
						traversedLinksFromMatching.Add(next);
					}

					if (maxMatching.Contains(next) == pair.Item2) //bool is "NextIsInMatching"
					{
						//we have found an alternating path!
						altPathNodes.Add(rfn.Edge);

						unvisited.Add(new Tuple<RectFlowNode, bool>(rfn, !pair.Item2));

					}
				}
			}

			return altPathNodes;
		}

		/// <summary>
		/// Our graph is directional due to running the MaxFlow algorithm. We need to make every directional edge
		/// bidirectional so we can find alternating paths
		/// </summary>
		/// <param name="horizNodes"></param>
		/// <param name="vertNodes"></param>
		private static void BuildBidirectionalGraph(List<RectFlowNode> horizNodes, List<RectFlowNode> vertNodes)
		{
			foreach (RectFlowNode h in horizNodes)
			{
				var iter = h.DestinationNodes.ToList();
				for (int i = 0; i < iter.Count; i++)
				{
					var dest = iter[i];
					//remove links to source / sink flow types
					if (dest.FlowType == FlowType.sink || dest.FlowType == FlowType.source)
					{
						h.DestinationNodes.Remove(dest);
					}
					//if this node does not link back to h, add the link
					if (dest.DestinationNodes.Contains(h) == false)
					{
						dest.AddLink(h);
					}
				}
			}

			foreach (RectFlowNode v in vertNodes)
			{
				var iter = v.DestinationNodes.ToList();
				for (int i = 0; i < iter.Count; i++)
				{
					var dest = iter[i];
					//remove links to source / sink flow types
					if (dest.FlowType == FlowType.sink || dest.FlowType == FlowType.source)
					{
						v.DestinationNodes.Remove(dest);
					}
					//if this node does not link back to h, add the link
					if (dest.DestinationNodes.Contains(v) == false)
					{
						dest.AddLink(v);
					}
				}
			}
		}

		/// <summary>
		/// Joins two holes into one by adding a straight line between a vertex of firstHole and a point
		/// on the perimeter or secondHole. (May or may not be a vertex). SpanningEdge is the segmentBetween them
		/// </summary>
		/// <param name="firstHole"></param>
		/// <param name="secondHole"></param>
		/// <param name="spanningEdge"></param>
		/// <returns></returns>
		private static RectShape CombineShapesUsingEdge(RectShape firstHole, RectShape secondHole, RectEdge spanningEdge)
		{
			//check to see if the chord contains both positions as part of the shape's perimeter.
			//we want to use the SecondPosition, since that's where the next edge starts from.
			RectEdge firstIncision = null;
			RectEdge secondIncision = null;

			var firstIncisionList = firstHole.Perimeter.FindAll(edge => edge.SecondPosition.Equals(spanningEdge.FirstPosition));
			var secondIncisionList = secondHole.Perimeter.FindAll(edge => edge.SecondPosition.Equals(spanningEdge.SecondPosition));

			//if the list has only 1 entry, use it. Otherwise, prefer the edge that forms a CONVEX angle.
			if (firstIncisionList.Count == 1)
			{
				firstIncision = firstIncisionList[0];
			}
			else
			{
				firstIncision = firstIncisionList.Find(edge => IsConvexOrColinear(spanningEdge.HeadingDirection, edge.HeadingDirection));
			}
			if (secondIncisionList.Count == 1)
			{
				secondIncision = secondIncisionList[0];
			}
			else
			{
				secondIncision = secondIncisionList.Find(edge => IsConvexOrColinear(spanningEdge.HeadingDirection, edge.HeadingDirection));
			}


			if (firstIncision == null && secondIncision == null)
			{
				//try swapping
				firstIncision = secondHole.Perimeter.Find(edge => edge.SecondPosition.Equals(spanningEdge.FirstPosition));
				secondIncision = firstHole.Perimeter.Find(edge => edge.SecondPosition.Equals(spanningEdge.SecondPosition));
			}
			if (firstIncision == null || secondIncision == null)
			{
				throw new Exception("Nope, we have bigger problems");
			}



			if (firstIncision != null && secondIncision != null)
			{
				//from the firstIncision, move along spanningEdge adding new Edges as we go. Once we reach the point shared by secondIcision, 
				//reverse the process and move backwards to complete the 1 dimensional edge between the two holes.

				List<RectEdge> firstChord = null;
				List<RectEdge> secondChord = null;
				//our cuts will always be left-to-right or down-to-top (mathmatically, in the quadrant we're working out of, this is cutting "down")
				//if firstIncision doesn't have the lower of the x/y values, swap first and second incision.
				if (spanningEdge.FirstPosition.xPos == spanningEdge.SecondPosition.xPos)
				{
					bool firstHoleIsAbove = true;
					//vertical cut
					if (firstIncision.SecondPosition.yPos > secondIncision.SecondPosition.yPos)
					{
						RectEdge tempEdge = secondIncision;
						secondIncision = firstIncision;
						firstIncision = tempEdge;

						firstHoleIsAbove = false;
					}

					List<RectEdge> chordEdgeDown = new List<RectEdge>();
					List<RectEdge> chordEdgeUp = new List<RectEdge>();
					for (int j = firstIncision.SecondPosition.yPos; j < secondIncision.SecondPosition.yPos; j++)
					{
						chordEdgeDown.Add(new RectEdge(new Position(spanningEdge.FirstPosition.xPos, j), new Position(spanningEdge.FirstPosition.xPos, j + 1), EdgeType.None));
						chordEdgeUp.Add(new RectEdge(new Position(spanningEdge.FirstPosition.xPos, j + 1), new Position(spanningEdge.FirstPosition.xPos, j), EdgeType.None));
					}
					chordEdgeUp.Reverse(); //trace the perimeters in opposite directions

					//swap back
					if (firstHoleIsAbove == false)
					{
						RectEdge tempEdge = secondIncision;
						secondIncision = firstIncision;
						firstIncision = tempEdge;

						firstChord = chordEdgeUp;
						secondChord = chordEdgeDown;
					}
					else
					{
						firstChord = chordEdgeDown;
						secondChord = chordEdgeUp;
					}
				}
				else if (spanningEdge.FirstPosition.yPos == spanningEdge.SecondPosition.yPos)
				{
					bool firstHoleOnLeft = true;
					//horizontal cut
					if (firstIncision.SecondPosition.xPos > secondIncision.SecondPosition.xPos)
					{
						RectEdge tempEdge = secondIncision;
						secondIncision = firstIncision;
						firstIncision = tempEdge;

						firstHoleOnLeft = false;
					}

					List<RectEdge> chordEdgeEast = new List<RectEdge>();
					List<RectEdge> chordEdgeWest = new List<RectEdge>();
					for (int i = firstIncision.SecondPosition.xPos; i < secondIncision.SecondPosition.xPos; i++)
					{
						chordEdgeEast.Add(new RectEdge(new Position(i, spanningEdge.FirstPosition.yPos), new Position(i + 1, spanningEdge.FirstPosition.yPos), EdgeType.None));
						chordEdgeWest.Add(new RectEdge(new Position(i + 1, spanningEdge.FirstPosition.yPos), new Position(i, spanningEdge.FirstPosition.yPos), EdgeType.None));
					}
					chordEdgeWest.Reverse(); //trace the perimeters in opposite directions

					//swap back
					if (firstHoleOnLeft == false)
					{
						RectEdge tempEdge = secondIncision;
						secondIncision = firstIncision;
						firstIncision = tempEdge;

						firstChord = chordEdgeWest;
						secondChord = chordEdgeEast;
					}
					else
					{
						firstChord = chordEdgeEast;
						secondChord = chordEdgeWest;
					}
				}
				else
				{
					throw new Exception("Non-grid-aligned chord cut. Abort!");
				}

				//get the perimeter of the firstHole, and start stitching.
				List<RectEdge> holePerimeter = new List<RectEdge>();
				//new perimeter looks like firstIncision.Next -> chordEdgeDown/Up (depending on "firstHoleIsAbove" -> secondIncision.Next -> 
				//rest of secondHole.Perimeter.Next -> chordEdgeUp/Down (the other one)  + rest of firstHole's perimeter

				RectEdge firstAdjacent = firstIncision.Next;
				RectEdge secondAdjacent = secondIncision.Next;

				firstIncision.Next = firstChord.First();
				for (int i = 0; i < firstChord.Count - 1; i++)
				{
					firstChord[i].Next = firstChord[i + 1];
				}
				firstChord.Last().Next = secondAdjacent;
				secondIncision.Next = secondChord.First();
				for (int i = 0; i < secondChord.Count - 1; i++)
				{
					secondChord[i].Next = secondChord[i + 1];
				}
				secondChord.Last().Next = firstAdjacent;

				//.Nexts all set, now trace the results into holePerimeter
				holePerimeter.Add(firstIncision);
				RectEdge loopEdge = firstIncision.Next;
				while (loopEdge != firstIncision)
				{
					holePerimeter.Add(loopEdge);
					loopEdge = loopEdge.Next;
				}

				//if this is a hole-shape cut, we need to add the OTHER holes from shape back into the combined shape.
				List<RectShape> newHoles;
				if (secondHole.Holes.Count > 0)
				{
					secondHole.RemoveHole(firstHole);
					newHoles = secondHole.Holes;
				}
				else
				{
					firstHole.RemoveHole(secondHole);
					newHoles = firstHole.Holes;
				}

#if debug
				var outShape = FilloutVerts(new RectShape() { Perimeter = holePerimeter, Holes = newHoles });
				if (Math.Abs(outShape.Vertices.FindAll(v => v.IsConcave).Count - outShape.Vertices.FindAll(v => v.IsConvex).Count) != 4)
				{
					Console.WriteLine("Huh");
				}
				return outShape;
#endif

				return FilloutVerts(new RectShape() { Perimeter = holePerimeter, Holes = newHoles });
			}
			else
			{
				throw new Exception("Could not find one of the incision points");
			}
		}



		/// <summary>
		/// Cuts the shape "rectShape" into a number of smaller shapes along the edge list. Because this list
		/// is a minimum vertex cover, none of these cuts will intersect with each other.
		/// After the first cut, you need to look through ALL created child shapes to find where the next cut goes
		/// 
		/// </summary>
		/// <param name="rectShape"></param>
		/// <param name="chords"></param>
		/// <returns></returns>
		private static List<RectShape> CutShapeWithChords(RectShape rectShape, List<RectEdge> chords)
		{

			List<RectShape> retShapes = new List<RectShape>() { rectShape };

			foreach (RectEdge redge in chords)
			{
				List<RectShape> shapesToAdd = new List<RectShape>();
				RectShape shapeToRemove = null;
				foreach (RectShape rshape in retShapes)
				{
					//check to see if the chord contains both positions as part of the shape's perimeter.
					//we want to use the SecondPosition, since that's where the next edge starts from.
					var firstIncision = rshape.Perimeter.Find(edge => edge.SecondPosition.Equals(redge.FirstPosition));
					var secondIncision = rshape.Perimeter.Find(edge => edge.SecondPosition.Equals(redge.SecondPosition));
					if (firstIncision != null && secondIncision != null)
					{
						//shape-shape cut
						//bool is "IsConcave"
						HashSet<Position> rectVertices = new HashSet<Position>(rshape.Vertices.Select(v => new Position(v.VertPosition)));
						//walk the shape's perimeter, once we find the first point, keep track until we find the second point,
						//then create straight edges between them (plan is to have no holes at this point to counfound this)
						//finally, set the .Next to point to the new edges, and create new RectShapes from the new cycles.

						//our cuts will always be left-to-right or down-to-top (mathmatically, in the quadrant we're working out of, this is cutting "down")
						//if firstIncision doesn't have the lower of the x/y values, swap first and second incision.
						if (redge.FirstPosition.xPos == redge.SecondPosition.xPos)
						{
							//vertical cut
							if (firstIncision.SecondPosition.yPos > secondIncision.SecondPosition.yPos)
							{
								RectEdge tempEdge = secondIncision;
								secondIncision = firstIncision;
								firstIncision = tempEdge;
							}

							List<RectEdge> chordEdgeDown = new List<RectEdge>();
							List<RectEdge> chordEdgeUp = new List<RectEdge>();
							for (int j = firstIncision.SecondPosition.yPos; j < secondIncision.SecondPosition.yPos; j++)
							{
								chordEdgeDown.Add(new RectEdge(new Position(redge.FirstPosition.xPos, j), new Position(redge.FirstPosition.xPos, j + 1), EdgeType.None));
								chordEdgeUp.Add(new RectEdge(new Position(redge.FirstPosition.xPos, j + 1), new Position(redge.FirstPosition.xPos, j), EdgeType.None));
							}
							chordEdgeUp.Reverse(); //trace the perimeters in opposite directions

							RectShape eastCut = CutPerimeterIntoShapes(firstIncision, secondIncision, rectVertices, chordEdgeUp, GetVertCutDirectionFromHeading(firstIncision), rshape);
							RectShape westCut = CutPerimeterIntoShapes(secondIncision, firstIncision, rectVertices, chordEdgeDown, GetVertCutDirectionFromHeading(secondIncision), rshape);

#if debug
							if (rshape.Holes.Count > 0)
							{
								Console.WriteLine("Hrmmm.");
							}
#endif

							shapesToAdd.Add(eastCut);
							shapesToAdd.Add(westCut);
							shapeToRemove = rshape;
							break;
						}
						else if (redge.FirstPosition.yPos == redge.SecondPosition.yPos)
						{
							//horizontal cut
							if (firstIncision.SecondPosition.xPos > secondIncision.SecondPosition.xPos)
							{
								RectEdge tempEdge = secondIncision;
								secondIncision = firstIncision;
								firstIncision = tempEdge;
							}

							List<RectEdge> chordEdgeEast = new List<RectEdge>();
							List<RectEdge> chordEdgeWest = new List<RectEdge>();
							for (int i = firstIncision.SecondPosition.xPos; i < secondIncision.SecondPosition.xPos; i++)
							{
								chordEdgeEast.Add(new RectEdge(new Position(i, redge.FirstPosition.yPos), new Position(i + 1, redge.FirstPosition.yPos), EdgeType.None));
								chordEdgeWest.Add(new RectEdge(new Position(i + 1, redge.FirstPosition.yPos), new Position(i, redge.FirstPosition.yPos), EdgeType.None));
							}
							chordEdgeWest.Reverse(); //trace the perimeters in opposite directions


							//cut direction is actually based on the first-passed incision's heading. North or West == North. South or East == South

							RectShape southCut = CutPerimeterIntoShapes(firstIncision, secondIncision, rectVertices, chordEdgeWest, Direction.South, rshape);
							RectShape northCut = CutPerimeterIntoShapes(secondIncision, firstIncision, rectVertices, chordEdgeEast, Direction.North, rshape);


#if debug
							if (rshape.Holes.Count > 0)
							{
								Console.WriteLine("Hrmmm.");
							}
#endif

							shapesToAdd.Add(southCut);
							shapesToAdd.Add(northCut);
							shapeToRemove = rshape;
							break;
						}
						else
						{
							throw new Exception("Non-grid-aligned chord cut. Abort!");
						}
					}
					else if (firstIncision == null && secondIncision == null)
					{
						//check if we're joining two of this shape's holes

						//because of how we trace the holes, it shouldn't be possible for two of them to share a vertex?
						//which means there are only a maximum of two holes that will match these finds.

						RectShape firstHole = null;
						RectShape secondHole = null;

						foreach (RectShape hole in rshape.Holes)
						{
							if (hole.Vertices.Find(v => v.VertPosition.Equals(redge.FirstPosition)) != null)
							{
								firstHole = hole;
							}
							if (hole.Vertices.Find(v => v.VertPosition.Equals(redge.SecondPosition)) != null)
							{
								secondHole = hole;
							}

							if (firstHole != null && secondHole != null)
							{
								break;
							}
						}

						//if we found no holes, look at next shape
						if (firstHole == null && secondHole == null)
						{
							continue;
						}

						//if we found only one hole, we're very confused.
						if ((firstHole == null && secondHole != null) ||
						   (firstHole != null && secondHole == null))
						{
							//throw new Exception("Tried to join a single hole");
							//TODO: Just skip this chord I guess?

							//I think this is the inverse half of the below code w/ the same crude hack
							Console.WriteLine("Chord cut appears to have been mooted. Skipping.");

							//crude hack
							shapesToAdd.Add(rshape);
							shapeToRemove = rshape;
							//end crude hack
							break;

						}

						//special case if these are the same hole.
						if (firstHole == secondHole)
						{
							List<RectShape> holeAndShape = CutShapeWithChords(firstHole, redge);
							//one of these is a hole, and the other one is a potentially concave shape.
							//the one that is NOT a hole is the one who has the vertices (from the cut) and
							//those vertices are NOT concave?

							rshape.RemoveHole(firstHole);

							var potentialHole = holeAndShape[0];
							var otherPotentialHole = holeAndShape[1];

							//if this is a hole, there will be more concave verts than convex ones. (4 more)
							int concaveVertCount = potentialHole.Vertices.FindAll(v => v.IsConcave).Count;
							int convexVertCount = potentialHole.Vertices.Count - concaveVertCount;
							if (concaveVertCount < convexVertCount)
							{
								//potentialHole is not a hole, but otherPotentialHole is
								rshape.AddHole(otherPotentialHole);

								//not actually a hole
								shapesToAdd.Add(potentialHole);
							}
							else
							{
								rshape.AddHole(potentialHole);

								//not actually a hole

								shapesToAdd.Add(otherPotentialHole);
							}
							break;
						}
						else
						{
							RectShape combinedHole = CombineShapesUsingEdge(firstHole, secondHole, redge);
							rshape.RemoveHole(firstHole);
							rshape.RemoveHole(secondHole);
							rshape.AddHole(combinedHole);

							shapesToAdd.Add(rshape);
							shapeToRemove = rshape;
							break;
						}
					}
					else
					{
						//we are joining the shape and a hole. Find that hole.
						RectShape joinedHole = null;
						if (firstIncision == null)
						{
							//firstIncision is a hole
							foreach (RectShape hole in rshape.Holes)
							{
								if (hole.Vertices.Find(v => v.VertPosition.Equals(redge.FirstPosition)) != null)
								{
									joinedHole = hole;
									break;
								}
							}
						}
						else //secondIncision == null
						{
							//firstIncision is a hole
							foreach (RectShape hole in rshape.Holes)
							{
								if (hole.Vertices.Find(v => v.VertPosition.Equals(redge.SecondPosition)) != null)
								{
									joinedHole = hole;
									break;
								}
							}
						}
						if (joinedHole == null)
						{
							//throw new Exception("Tried to join to absent hole");
							//TODO: Just skip this chord I guess?

							//I think the "appropriate" way to handle it would be to build out the
							//edge of the incision we did find until we run into the perimeter of another shape.
							//to create a 1-d edge but not actually doing any cutting.
							//TODO: DO THAT!
							Console.WriteLine("Chord cut appears to have been mooted. Skipping.");

							//crude hack
							shapesToAdd.Add(rshape);
							shapeToRemove = rshape;
							//end crude hack

							break;
						}

						RectShape mergedShape = CombineShapesUsingEdge(rshape, joinedHole, redge);
						shapesToAdd.Add(mergedShape);
						shapeToRemove = rshape;
						break;
					}
				}


				if (shapesToAdd.Count > 0)
				{
					retShapes.Remove(shapeToRemove);
					retShapes.AddRange(shapesToAdd);
				}
				else
				{
					throw new Exception("Could not find appropriate shape for chord to cut");
				}
			}

			return retShapes;

		}


		private static Direction GetHorizCutDirectionFromHeading(RectEdge firstIncision)
		{
			//cut direction is actually based on the first-passed incision's heading. North or West == North. South or East == South

			switch (firstIncision.HeadingDirection)
			{
				case Direction.North:
				case Direction.West:
					return Direction.North;
				case Direction.South:
				case Direction.East:
					return Direction.South;
			}

			throw new ArgumentException("Unexpected Direction");
		}

		private static Direction GetVertCutDirectionFromHeading(RectEdge firstIncision)
		{
			//cut direction is actually based on the first-passed incision's heading. North or West == North. South or East == South

			switch (firstIncision.HeadingDirection)
			{
				case Direction.North:
				case Direction.East:
					return Direction.East;
				case Direction.South:
				case Direction.West:
					return Direction.West;
			}

			throw new ArgumentException("Unexpected Direction");
		}


		/// <summary>
		/// Cuts the shape "rectShape" into two smaller shapes along the given chord.
		/// </summary>
		/// <param name="rectShape"></param>
		/// <param name="chords"></param>
		/// <returns></returns>
		private static List<RectShape> CutShapeWithChords(RectShape rectShape, RectEdge chord, RectShape workingHole = null)
		{
			List<RectShape> shapesToAdd = new List<RectShape>();

			//check to see if the chord contains both positions as part of the shape's perimeter.
			//we want to use the SecondPosition, since that's where the next edge starts from.

			//because 2-d edges exist (two edges on the same 2 points, with the firstPosition && secondPosition swapped)
			//we need to additionally find the edge whose heading makes a convex angle with ours, or a straight line.

			RectEdge firstIncision = null;
			RectEdge secondIncision = null;

			var firstIncisionList = rectShape.Perimeter.FindAll(edge => edge.SecondPosition.Equals(chord.FirstPosition));
			var secondIncisionList = rectShape.Perimeter.FindAll(edge => edge.SecondPosition.Equals(chord.SecondPosition));

			if (firstIncisionList.Count == 1)
			{
				firstIncision = firstIncisionList[0];
			}
			else
			{
				firstIncision = firstIncisionList.Find(edge => IsConvexOrColinear(chord.HeadingDirection, edge.HeadingDirection));
			}

			if (secondIncisionList.Count == 1)
			{
				secondIncision = secondIncisionList[0];
			}
			else
			{
				secondIncision = secondIncisionList.Find(edge => IsConvexOrColinear(chord.HeadingDirection, edge.HeadingDirection));
			}


			if (firstIncision != null && secondIncision != null)
			{
				//bool is "IsConcave"
				var rectVertices = new HashSet<Position>(rectShape.Vertices.Select(v => new Position(v.VertPosition))); //can have dupes here. Remove them.
																														//walk the shape's perimeter, once we find the first point, keep track until we find the second point,
																														//then create straight edges between them (plan is to have no holes at this point to counfound this)
																														//finally, set the .Next to point to the new edges, and create new RectShapes from the new cycles.

				//our cuts will always be left-to-right or down-to-top (mathmatically, in the quadrant we're working out of, this is cutting "down")
				//if firstIncision doesn't have the lower of the x/y values, swap first and second incision.
				if (chord.FirstPosition.xPos == chord.SecondPosition.xPos)
				{
					//vertical cut
					if (firstIncision.SecondPosition.yPos > secondIncision.SecondPosition.yPos)
					{
						RectEdge tempEdge = secondIncision;
						secondIncision = firstIncision;
						firstIncision = tempEdge;
					}

					List<RectEdge> chordEdgeDown = new List<RectEdge>();
					List<RectEdge> chordEdgeUp = new List<RectEdge>();
					for (int j = firstIncision.SecondPosition.yPos; j < secondIncision.SecondPosition.yPos; j++)
					{
						chordEdgeDown.Add(new RectEdge(new Position(chord.FirstPosition.xPos, j), new Position(chord.FirstPosition.xPos, j + 1), EdgeType.None));
						chordEdgeUp.Add(new RectEdge(new Position(chord.FirstPosition.xPos, j + 1), new Position(chord.FirstPosition.xPos, j), EdgeType.None));
					}
					chordEdgeUp.Reverse(); //trace the perimeters in opposite directions

					RectShape eastCut = CutPerimeterIntoShapes(firstIncision, secondIncision, rectVertices, chordEdgeUp, GetVertCutDirectionFromHeading(firstIncision), rectShape);
					RectShape westCut = CutPerimeterIntoShapes(secondIncision, firstIncision, rectVertices, chordEdgeDown, GetVertCutDirectionFromHeading(secondIncision), rectShape);

#if debug
					if (rectShape.Holes.Count > 0)
					{
						Console.WriteLine("Hrmmm.");
					}
#endif

					shapesToAdd.Add(eastCut);
					shapesToAdd.Add(westCut);

				}
				else if (chord.FirstPosition.yPos == chord.SecondPosition.yPos)
				{
					//horizontal cut
					if (firstIncision.SecondPosition.xPos > secondIncision.SecondPosition.xPos)
					{
						RectEdge tempEdge = secondIncision;
						secondIncision = firstIncision;
						firstIncision = tempEdge;
					}

					List<RectEdge> chordEdgeEast = new List<RectEdge>();
					List<RectEdge> chordEdgeWest = new List<RectEdge>();
					for (int i = firstIncision.SecondPosition.xPos; i < secondIncision.SecondPosition.xPos; i++)
					{
						chordEdgeEast.Add(new RectEdge(new Position(i, chord.FirstPosition.yPos), new Position(i + 1, chord.FirstPosition.yPos), EdgeType.None));
						chordEdgeWest.Add(new RectEdge(new Position(i + 1, chord.FirstPosition.yPos), new Position(i, chord.FirstPosition.yPos), EdgeType.None));
					}
					chordEdgeWest.Reverse(); //trace the perimeters in opposite directions

					//if we swapped, I think this *can* get the vertices in an unexpected order. I don't think this prevents future iters from working though. (And vertices have no ordering information anyway)
					RectShape northCut = CutPerimeterIntoShapes(firstIncision, secondIncision, rectVertices, chordEdgeWest, GetHorizCutDirectionFromHeading(firstIncision), rectShape);
					RectShape southCut = CutPerimeterIntoShapes(secondIncision, firstIncision, rectVertices, chordEdgeEast, GetHorizCutDirectionFromHeading(secondIncision), rectShape);

#if debug
					if (rectShape.Holes.Count > 0)
					{
						Console.WriteLine("Hrmmm.");
					}
#endif
					shapesToAdd.Add(southCut);
					shapesToAdd.Add(northCut);
				}
				else
				{
					throw new Exception("Non-grid-aligned chord cut. Abort!");
				}
			}
			else
			{
				throw new Exception("Null incision");
			}

			return shapesToAdd;

		}

		///need to reset hole list after this?
		private static RectShape CutPerimeterIntoShapes(RectEdge firstIncision, RectEdge secondIncision, HashSet<Position> vertPositions, List<RectEdge> chordEdgeUp, Direction cutSide, RectShape parentShape)
		{
			//now we have to merge the chordEdge lists with some subsection of the original perimeter to get the child shapes.
			List<RectEdge> edges = new List<RectEdge>();
			List<Vertex> vertices = new List<Vertex>();

			//we're cutting "down" from first incision to second incision, so following the Next's from 
			//the first incision to the second incision gives us the east edges, and following the second
			//incision around to the first incision gives us the west edges.

			RectEdge workingEdge = firstIncision;

			while (workingEdge.Next != secondIncision) //get the east side
			{
				edges.Add(workingEdge.Next);

				workingEdge = workingEdge.Next;
			}
			//now duplicate the second incision

			var eastSecondIncision = new RectEdge(new Position(secondIncision.FirstPosition), new Position(secondIncision.SecondPosition), EdgeType.None);
			workingEdge.Next = eastSecondIncision;
			edges.Add(eastSecondIncision);

			//last element in chordEdgeUp needs to point back to eastEdges.First()
			foreach (RectEdge re in chordEdgeUp)
			{
				edges.Last().Next = re;
				edges.Add(re);
			}
			chordEdgeUp.Last().Next = edges.First();

			//account for holes
			//from a vertex of a hole, find all perimeters of the new cut shape in the direction of the cut
			//if the total number is odd, we're in a hole. If it's even, we're not.
			//remember: even 1d segments have perimeters going both ways, and every point has exactly %2 
			//edges touching it, one with firstPosition, one with secondPosition
			List<RectShape> carriedHoles = new List<RectShape>();

			//switch (cutSide)
			//{
			//	case Direction.West:
			//	case Direction.East:
			foreach (RectShape rs in parentShape.Holes)
			{
				Position startPos = rs.Vertices.First().VertPosition;
				//look for edges opposite the cut
				//don't double-count certain edges, so only look for vertical / convex angles. If the vertex lies along the same line as a horiz. edge, we only care about the 
				//angle OFF of that edge.
				var containerEdgesWest = edges.FindAll(e => e.SecondPosition.xPos > startPos.xPos && e.SecondPosition.yPos == startPos.yPos && (IsConvexOrVertical(e.HeadingDirection, e.Next.HeadingDirection)));
				var containerEdgesEast = edges.FindAll(e => e.SecondPosition.xPos <= startPos.xPos && e.SecondPosition.yPos == startPos.yPos && (IsConvexOrVertical(e.HeadingDirection, e.Next.HeadingDirection)));
				if (containerEdgesWest.Count % 2 == 1 && containerEdgesEast.Count % 2 == 1)
				{
					//still a hole
					carriedHoles.Add(rs);
					//holes.Remove(rs);
				}
#if debug
				else if (containerEdgesEast.Count % 2 != containerEdgesWest.Count % 2)
				{
					Console.WriteLine("odd...");
				}
#endif
				else
				{
					//not a hole. skip.
				}
			}

			//now remove the carried holes, since another shape *can't* have them.
			foreach (RectShape rs in carriedHoles)
			{
				parentShape.RemoveHole(rs);
			}
#if debug
			var retShape = FilloutVerts(new RectShape() { Perimeter = edges, Vertices = vertices, Holes = carriedHoles });
			if (Math.Abs(retShape.Vertices.FindAll(v => v.IsConcave).Count - retShape.Vertices.FindAll(v => v.IsConvex).Count) != 4)
			{
				Console.WriteLine("Huh");
			}
			return retShape;
#endif

			return FilloutVerts(new RectShape() { Perimeter = edges, Vertices = vertices, Holes = carriedHoles });
		}
		/// <summary>
		/// Tests whether the vertex formed between the two headings is convex or the same direction.
		/// </summary>
		/// <param name="firstVector"></param>
		/// <param name="secondVector"></param>
		/// <returns></returns>
		private static bool IsConvexOrColinear(Direction firstVector, Direction secondVector)
		{
			if (firstVector == secondVector)
			{
				return true;
			}
			switch (firstVector)
			{
				case Direction.East:
					if (secondVector == Direction.South) return true;
					break;
				case Direction.South:
					if (secondVector == Direction.West) return true;
					break;
				case Direction.West:
					if (secondVector == Direction.North) return true;
					break;
				case Direction.North:
					if (secondVector == Direction.East) return true;
					break;
				default:
					break;
			}

			return false;
		}

		/// <summary>
		/// Tests whether the vertex formed between the two headings is convex or the same vertical direction.
		/// </summary>
		/// <param name="firstVector"></param>
		/// <param name="secondVector"></param>
		/// <returns></returns>
		private static bool IsConvexOrVertical(Direction firstVector, Direction secondVector)
		{
			if (firstVector == secondVector)
			{
				switch (firstVector)
				{
					case Direction.North:
					case Direction.South:
						return true;
					default:
						return false;
				}
			}
			switch (firstVector)
			{
				case Direction.East:
					if (secondVector == Direction.South) return true;
					break;
				case Direction.South:
					if (secondVector == Direction.West) return true;
					break;
				case Direction.West:
					if (secondVector == Direction.North) return true;
					break;
				case Direction.North:
					if (secondVector == Direction.East) return true;
					break;
				default:
					break;
			}

			return false;
		}


		/// <summary>
		/// Tests whether the vertex formed between the two headings is concave or not.
		/// </summary>
		/// <param name="firstVector"></param>
		/// <param name="secondVector"></param>
		/// <returns></returns>
		private static bool VertexIsConcave(Direction firstVector, Direction secondVector)
		{
			if (firstVector == secondVector)
			{
				throw new Exception("Not a vertex");
			}
			switch (firstVector)
			{
				case Direction.East:
					if (secondVector == Direction.South) return false;
					break;
				case Direction.South:
					if (secondVector == Direction.West) return false;
					break;
				case Direction.West:
					if (secondVector == Direction.North) return false;
					break;
				case Direction.North:
					if (secondVector == Direction.East) return false;
					break;
				default:
					break;
			}

			return true;
		}

		/// <summary>
		/// From the given perimeter (chordEdgeUp), generate a new perimeter, based on the intersection of each of the 
		/// shapes spanning that perimeter.
		/// </summary>
		/// <param name="chordEdgeUp"></param>
		/// <param name="foundSpan"></param>
		/// <returns></returns>
		private static List<RectEdge> GetJaggedPerimeter(List<RectEdge> chordEdgeUp, List<RectShape> foundSpan, Direction cutSide)
		{
			if (foundSpan.Count == 0) return chordEdgeUp;

			List<List<RectEdge>> perimeterSegments = new List<List<RectEdge>>();

			int positionVar;

			//because of the nature of the holes we're working with, we need the min. and max intersections
			bool isAscending = cutSide == Direction.West || cutSide == Direction.South;

			switch (cutSide)
			{
				case Direction.West:
					//vertical cut,
					positionVar = chordEdgeUp[0].FirstPosition.xPos;
					foreach (RectShape rs in foundSpan)
					{
						//we want "min y" edge with firstPosition == chordEdgeUp.xPosition
						//and "max y" edge with secondPosition == chordEdgeUp.xPosition
						List<RectEdge> ps = new List<RectEdge>();
						//get the highest of the intersections with our initial perimeter
						var workingSet = rs.Perimeter.FindAll(e => e.FirstPosition.xPos == positionVar).OrderBy(e => e.FirstPosition.yPos);
						RectEdge start = workingSet.First();
						var workingLast = rs.Perimeter.FindAll(e => e.SecondPosition.xPos == positionVar).OrderBy(e => e.SecondPosition.yPos);
						RectEdge end = workingSet.Last();

						RectEdge temp = start;
						while (temp != end)
						{
							ps.Add(temp);
							temp = temp.Next;
						}
						ps.Add(end); //temp is last here

						perimeterSegments.Add(ps);
					}

					perimeterSegments.OrderBy(lre => lre.First().FirstPosition.yPos);
					break;
				case Direction.East:
					//vertical cut [could cut out most of this if we could use the switch to simply set how to calculate "last" and "first"
					positionVar = chordEdgeUp[0].FirstPosition.xPos;
					foreach (RectShape rs in foundSpan)
					{
						//we want "max y" edge with firstPosition == chordEdgeUp.xPosition
						//and "min y" edge with secondPosition == chordEdgeUp.xPosition
						List<RectEdge> ps = new List<RectEdge>();
						//get the highest of the intersections with our initial perimeter
						var workingSet = rs.Perimeter.FindAll(e => e.FirstPosition.xPos == positionVar).OrderBy(e => e.FirstPosition.yPos);
						RectEdge end = workingSet.First();
						var workingLast = rs.Perimeter.FindAll(e => e.SecondPosition.xPos == positionVar).OrderBy(e => e.SecondPosition.yPos);
						RectEdge start = workingSet.Last();

						RectEdge temp = start;
						while (temp != end)
						{
							ps.Add(temp);
							temp = temp.Next;
						}
						ps.Add(end); //temp is last here

						perimeterSegments.Add(ps);
					}

					perimeterSegments.OrderBy(lre => lre.First().FirstPosition.yPos);
					break;
				case Direction.South:
					//horizontal cut
					positionVar = chordEdgeUp[0].FirstPosition.yPos;
					foreach (RectShape rs in foundSpan)
					{
						//we want "min y" edge with firstPosition == chordEdgeUp.xPosition
						//and "max y" edge with secondPosition == chordEdgeUp.xPosition
						List<RectEdge> ps = new List<RectEdge>();
						//get the highest of the intersections with our initial perimeter
						var workingSet = rs.Perimeter.FindAll(e => e.FirstPosition.yPos == positionVar).OrderBy(e => e.FirstPosition.xPos);
						RectEdge start = workingSet.First();
						var workingLast = rs.Perimeter.FindAll(e => e.SecondPosition.yPos == positionVar).OrderBy(e => e.SecondPosition.xPos);
						RectEdge end = workingSet.Last();

						RectEdge temp = start;
						while (temp != end)
						{
							ps.Add(temp);
							temp = temp.Next;
						}
						ps.Add(end); //temp is last here

						perimeterSegments.Add(ps);
					}

					perimeterSegments.OrderBy(lre => lre.First().FirstPosition.yPos);
					break;

				case Direction.North:
					//horizontal cut
					positionVar = chordEdgeUp[0].FirstPosition.yPos;
					foreach (RectShape rs in foundSpan)
					{
						//we want "min y" edge with firstPosition == chordEdgeUp.xPosition
						//and "max y" edge with secondPosition == chordEdgeUp.xPosition
						List<RectEdge> ps = new List<RectEdge>();
						//get the highest of the intersections with our initial perimeter
						var workingSet = rs.Perimeter.FindAll(e => e.FirstPosition.yPos == positionVar).OrderBy(e => e.FirstPosition.xPos);
						RectEdge end = workingSet.First();
						var workingLast = rs.Perimeter.FindAll(e => e.SecondPosition.yPos == positionVar).OrderBy(e => e.SecondPosition.xPos);
						RectEdge start = workingSet.Last();

						RectEdge temp = start;
						while (temp != end)
						{
							ps.Add(temp);
							temp = temp.Next;
						}
						ps.Add(end); //temp is last here

						perimeterSegments.Add(ps);
					}

					perimeterSegments.OrderBy(lre => lre.First().FirstPosition.yPos);
					break;
			}

			//now that we have the perimeter segments, we start going down chordEdgeUp until we have a 2nd position that matches the next perimeterSegment
			List<RectEdge> retList = new List<RectEdge>();
			RectEdge workingEdge = chordEdgeUp.First();
			RectEdge lastEdge = chordEdgeUp.Last();
			for (int i = 0; i < perimeterSegments.Count; i++)
			{
				List<RectEdge> ps = perimeterSegments[i];
				RectEdge firstPerimeterEdge = ps.First();

				//add edges until we have a 2nd position that matches the next perimeterSegment
				//when workingEdge.FirstPosition == the perimeter firstPosition, we've found the first
				//segment that's being replaced.
				while (workingEdge.FirstPosition.Equals(firstPerimeterEdge.FirstPosition) == false)
				{
					retList.Add(new RectEdge(workingEdge.FirstPosition, workingEdge.SecondPosition, EdgeType.None));
					workingEdge = workingEdge.Next;
				}
				//instead of adding more from the original perimeter, now we add all of the perimeter segment.
				foreach (RectEdge re in ps)
				{
					retList.Add(new RectEdge(re.FirstPosition, re.SecondPosition, EdgeType.None));
				}

				//now we look for the next edge in chordEdgeUp w/ firstPosition == retList.Last().secondPosition
				RectEdge lastLocal = retList.Last();
				workingEdge = chordEdgeUp.Find(e => e.FirstPosition.Equals(lastLocal.SecondPosition));
			}

			//and finally, we point every edge in our perimeter (except the last one) to point to the next edge
			for (int i = 0; i < retList.Count - 1; i++)
			{
				retList[i].Next = retList[i + 1];
			}

			return retList;

		}

		/// <summary>
		/// Find a path from source to sink.
		/// Once a path is found, reverse it.
		/// Repeat until no more paths can be found
		/// We are interested in the "reversed" paths from a horizontal node to a vertical node 
		/// </summary>
		/// <param name="sourceNode"></param>
		/// <param name="sinkNode"></param>
		private static void CalculateMaximumFlow(RectFlowNode sourceNode, RectFlowNode sinkNode)
		{
			while (sourceNode.DestinationNodes.Count > 0)
			{
				RectFlowNode firstDestination = sourceNode.DestinationNodes.First();

				List<BFSFlowNode> unvisited = new List<BFSFlowNode>()
				{
					//initial node
					new BFSFlowNode(firstDestination, new List<RectFlowNode>(){sourceNode})
				};
				HashSet<RectFlowNode> visited = new HashSet<RectFlowNode>();

				List<RectFlowNode> nodePath = null;

				//Breadth-First search to the goal
				while (unvisited.Count > 0)
				{
					//first, check if this node is the sink node.
					RectFlowNode workingNode = unvisited[0].Node;
					List<RectFlowNode> workingPath = unvisited[0].Path;
					unvisited.RemoveAt(0);

					if (workingNode == sinkNode)
					{
						nodePath = workingPath;
						nodePath.Add(workingNode);
						break;
					}

					//second, check if this node is in the visited list
					if (visited.Contains(workingNode))
					{
						//we can continue
						continue;
					}

					//didn't find the path, so add the children to the unvisited lists, and add 
					//the path we took to get here.
					visited.Add(workingNode);
					foreach (var childNode in workingNode.DestinationNodes)
					{
						//but only if they're not already in the visited list;
						if (visited.Contains(childNode) == false)
						{
							var tempest = new List<RectFlowNode>();
							tempest.AddRange(workingPath);
							tempest.Add(workingNode);

							var temp = new BFSFlowNode(childNode, tempest);
							unvisited.Add(temp);
						}
					}
				}

				if (nodePath == null)
				{
					//there was no path from the source to the destination. This is possible, it means the
					//edge is isolated and there are no intersections.

					//therefore, we can safely remove it from here.
					sourceNode.DestinationNodes.Remove(firstDestination);
				}

				else
				{
					//reverse each link in the path in nodePath
					for (int i = 1; i < nodePath.Count; i++)
					{
						nodePath[i - 1].ReverseLink(nodePath[i]);
					}
				}


			}

			//done finding paths.
		}

		/// <summary>
		/// Determine if the three points are listed in counterclockwise order.
		/// If the slope of the line AB is less than the slope of the line AC then the three points
		/// are listed in a counterclockwise order.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		private static bool PointsCounterClockwise(Position a, Position b, Position c)
		{
			return (c.yPos - a.yPos) * (b.xPos - a.xPos) > (b.yPos - a.yPos) * (c.xPos - a.xPos);
		}

		/// <summary>
		/// For two line segments AB, and CD, they intersect if and only if points A and B are separated by segment CD
		/// and points C and D are separated by segment AB. If points A and B are separated by 
		/// segment CD then ACD and BCD should have opposite orientation meaning either ACD or 
		/// BCD is counterclockwise but not both. 
		/// </summary>
		/// <param name="firstEdge"></param>
		/// <param name="secondEdge"></param>
		/// <returns></returns>
		private static bool LineSegmentsIntersect(RectEdge firstEdge, RectEdge secondEdge)
		{
			return (PointsCounterClockwise(firstEdge.FirstPosition, secondEdge.FirstPosition, secondEdge.SecondPosition) !=
					PointsCounterClockwise(firstEdge.SecondPosition, secondEdge.FirstPosition, secondEdge.SecondPosition)) &&

				   (PointsCounterClockwise(firstEdge.FirstPosition, firstEdge.SecondPosition, secondEdge.FirstPosition) !=
					PointsCounterClockwise(firstEdge.FirstPosition, firstEdge.SecondPosition, secondEdge.SecondPosition));
		}

		/// <summary>
		/// At this point, we have reduced any complex shapes (holes) into multiple simple, possibly convex, shapes instead.
		/// This is good enough to run the Rectilinear decomposition to reduce them to... rectangles. Before we can do that, though,
		/// wee need to get the set of verts and determine their concavity.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static List<RectShape> FindVertsFromEdges(List<RectShape> input)
		{
			foreach (RectShape shape in input)
			{
				FilloutVerts(shape);
				foreach (RectShape hole in shape.Holes)
				{
					FilloutVerts(hole);
				}

				//fill out the holeVerts
				shape.UpdateHoleVerts();
			}

			return input;
		}

		private static RectShape FilloutVerts(RectShape shape)
		{
			//first vertex is always convex (90* inner angle) because of how we parse the data
			RectEdge firstEdge = shape.Perimeter[0];
			Direction firstHeading = firstEdge.HeadingDirection; //always east (if shape) or south (if hole)

			List<Vertex> shapeVerts = new List<Vertex> { };

			RectEdge workingEdge = firstEdge;

			while (workingEdge.Next != firstEdge) //get the east side
			{
				if (workingEdge.HeadingDirection == workingEdge.Next.HeadingDirection)
				{
					//this means workingEdge.Next is between two points going the same way. Ergo, not a true vertex for this shape post-cut.
				}
				else
				{
					//it is possible for the concave-ness of the vertice to have changed after the cut, so re-calc the angle here
					bool isConcave = VertexIsConcave(workingEdge.HeadingDirection, workingEdge.Next.HeadingDirection);
					shapeVerts.Add(new Vertex(workingEdge.SecondPosition, isConcave));
				}

				workingEdge = workingEdge.Next;
			}

			////after doing all of this, have to do it for the wraparound piece of the perimeter
			if (workingEdge.HeadingDirection == workingEdge.Next.HeadingDirection)
			{
				//this means workingEdge.Next is between two points going the same way. Ergo, not a true vertex for this shape post-cut.
			}
			else
			{
				//it is possible for the concave-ness of the vertice to have changed after the cut, so re-calc the angle here
				bool isConcave = VertexIsConcave(workingEdge.HeadingDirection, workingEdge.Next.HeadingDirection);
				shapeVerts.Add(new Vertex(workingEdge.SecondPosition, isConcave));
			}

			shape.Vertices = new List<Vertex>(shapeVerts);

			return shape;
		}

		/// <summary>
		/// Looks ahead for the correct cell in data to follow the perimeter of the shape
		/// </summary>
		/// <param name="workingEdge">the previous edge</param>
		/// <param name="d">The direction to look for a new edge</param>
		/// <param name="data"></param>
		/// <param name="peekNode"></param>
		/// <returns></returns>
		private static RectEdge TraverseEdge(RectEdge workingEdge, Direction d, RectNode[,] data, int parentRegion, out RectNode peekNode)
		{
			Position headingOffset = null;
			Position directionOffset = null;
			Direction edgeDirection;
			//first thing to do is figure out what cell we need to look at next.

			//based on the heading, we can get a translation vector to simulate secondPosition to the top-left vertex
			//(to consistently match our coordinate system), then a 2nd offset to account for the direction.
			//the direction offset varies based on whether or not the angle formed would be concave or convex (or straight)

			switch (workingEdge.HeadingDirection)
			{
				case Direction.East:
					headingOffset = new Position(-1, 0);
					switch (d)
					{
						case Direction.North:
							directionOffset = new Position(1, -1);
							edgeDirection = Direction.West;
							break;
						case Direction.East:
							directionOffset = new Position(1, 0);
							edgeDirection = Direction.North;
							break;
						case Direction.South:
							directionOffset = new Position(0, 0);
							edgeDirection = Direction.East;
							break;
						default:
							throw new Exception("Bad direction with current heading");
					}
					break;
				case Direction.South:
					headingOffset = new Position(-1, -1);
					switch (d)
					{
						case Direction.East:
							directionOffset = new Position(1, 1);
							edgeDirection = Direction.North;
							break;
						case Direction.South:
							directionOffset = new Position(0, 1);
							edgeDirection = Direction.East;
							break;
						case Direction.West:
							directionOffset = new Position(0, 0);
							edgeDirection = Direction.South;
							break;
						default:
							throw new Exception("Bad direction with current heading");
					}
					break;
				case Direction.West:
					headingOffset = new Position(0, -1);
					switch (d)
					{
						case Direction.South:
							directionOffset = new Position(-1, 1);
							edgeDirection = Direction.East;
							break;
						case Direction.West:
							directionOffset = new Position(-1, 0);
							edgeDirection = Direction.South;
							break;
						case Direction.North:
							directionOffset = new Position(0, 0);
							edgeDirection = Direction.West;
							break;
						default:
							throw new Exception("Bad direction with current heading");
					}
					break;
				case Direction.North:
					headingOffset = new Position(0, 0);
					switch (d)
					{
						case Direction.West:
							directionOffset = new Position(-1, -1);
							edgeDirection = Direction.South;
							break;
						case Direction.North:
							directionOffset = new Position(0, -1);
							edgeDirection = Direction.West;
							break;
						case Direction.East:
							directionOffset = new Position(0, 0);
							edgeDirection = Direction.North;
							break;
						default:
							throw new Exception("Bad direction with current heading");
					}
					break;
				default:
					throw new Exception("Bad heading");
			}
			Position indexVector = workingEdge.SecondPosition + headingOffset + directionOffset;
			peekNode = data[indexVector.xPos, indexVector.yPos]; //given how we screened directions before getting here, this should always be a valid index

			EdgeType retEdge;
			switch (edgeDirection)
			{
				case Direction.East:
					retEdge = peekNode.Edges.East;
					break;
				case Direction.South:
					retEdge = peekNode.Edges.South;
					break;
				case Direction.West:
					retEdge = peekNode.Edges.West;
					break;
				case Direction.North:
					retEdge = peekNode.Edges.North;
					break;
				default:
					throw new Exception("Bad edge direction");
			}

			if (retEdge == EdgeType.None)
			{
				return null; //no edge in the given direction
			}

			//if we got here, it's time to construct the new edge.
			//also need to set workingEdge.Next to the new edge before we return it.
			Position vertexOffset = null;
			switch (d)
			{
				case Direction.East:
					vertexOffset = East;
					break;
				case Direction.South:
					vertexOffset = South;
					break;
				case Direction.West:
					vertexOffset = West;
					break;
				case Direction.North:
					vertexOffset = North;
					break;
			}

			RectEdge traverseEdge = new RectEdge(workingEdge.SecondPosition, workingEdge.SecondPosition + vertexOffset, retEdge);

			workingEdge.Next = traverseEdge;
			ClearEdge(peekNode, edgeDirection, parentRegion); //since we traversed it, clear it.

			return traverseEdge;
		}

		/// <summary>
		/// removes the edge from the given RectNode, based on the given heading
		/// </summary>
		/// <param name="edgeNode"></param>
		/// <param name="direction"></param>
		private static void ClearEdge(RectNode edgeNode, Direction direction, int parent)
		{
			EdgeType replacementEdge;
			if (parent == -1)
			{
				//we're demarcating a hole
				replacementEdge = EdgeType.BurnedHoleEdge;
			}
			else
			{
				edgeNode.ParentRegion = parent;
				replacementEdge = EdgeType.None;
			}

			switch (direction)
			{
				case Direction.East:
					edgeNode.Edges.East = replacementEdge; //clear out the edge as we traverse it
					break;
				case Direction.South:
					edgeNode.Edges.South = replacementEdge; //clear out the edge as we traverse it
					break;
				case Direction.West:
					edgeNode.Edges.West = replacementEdge; //clear out the edge as we traverse it
					break;
				case Direction.North:
					edgeNode.Edges.North = replacementEdge; //clear out the edge as we traverse it
					break;
			}
		}



		/// <summary>
		/// Gets the next vectors to look for edges, respecting the bounds of the array. Will return 1-3 directional unit-vectors
		/// </summary>
		/// <param name="workingEdge"></param>
		/// <param name="maxHeight"></param>
		/// <param name="maxWidth"></param>
		/// <returns></returns>
		private static List<Direction> GetVectors(RectEdge workingEdge, int maxHeight, int maxWidth)
		{
			List<Direction> outVectors = new List<Direction>();

			Direction heading = workingEdge.HeadingDirection;
			switch (heading)
			{
				case Direction.East:
					//East looks South (convex), East, North (concave)

					//because we're moving interior clockwise, we can always add south
					outVectors.Add(Direction.South);

					if (workingEdge.SecondPosition.xPos < maxWidth)
					{
						outVectors.Add(Direction.East);
					}

					if (workingEdge.SecondPosition.yPos > 0)
					{
						outVectors.Add(Direction.North);
					}

					break;
				case Direction.South:
					//South looks West (convex), South, East (concave)

					//because we're moving interior clockwise, we can always add west
					outVectors.Add(Direction.West);

					if (workingEdge.SecondPosition.yPos < maxHeight)
					{
						outVectors.Add(Direction.South);
					}

					if (workingEdge.SecondPosition.xPos < maxWidth)
					{
						outVectors.Add(Direction.East);
					}

					break;
				case Direction.West:
					//West looks North (convex), West, South (concave)

					//because we're moving interior clockwise, we can always add North
					outVectors.Add(Direction.North);

					if (workingEdge.SecondPosition.xPos > 0)
					{
						outVectors.Add(Direction.West);
					}

					if (workingEdge.SecondPosition.yPos < maxHeight)
					{
						outVectors.Add(Direction.South);
					}

					break;
				case Direction.North:
					//North looks East (convex), North, West (concave)

					//because we're moving interior clockwise, we can always add East
					outVectors.Add(Direction.East);

					if (workingEdge.SecondPosition.yPos > 0)
					{
						outVectors.Add(Direction.North);
					}

					if (workingEdge.SecondPosition.xPos > 0)
					{
						outVectors.Add(Direction.West);
					}

					break;
				default:
					//can't get here if edges are constructed correctly.
					throw new Exception("Unknown Direction");
			}
			return outVectors;
		}
	}
}
