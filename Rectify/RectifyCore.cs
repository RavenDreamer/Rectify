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
		public static RectNode[,] GetRectNodes(int[,] data)
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

					//west; if we're in-bounds and the westPeek is the same value, no wall
					if (w > 0 && data[h, w - 1] == dataCell)
					{
						rNode.Edges.West = EdgeType.None;
					}
					else
					{
						rNode.Edges.West = EdgeType.Wall;
					}

					//East; if we're in-bounds and the eastPeek is the same value, no wall
					if (w < maxWidth - 1 && data[h, w + 1] == dataCell)
					{
						rNode.Edges.East = EdgeType.None;
					}
					else
					{
						rNode.Edges.East = EdgeType.Wall;
					}

					//South; if we're in-bounds and the southPeek is the same value, no wall
					//this *is* South despite the weird offsets, because our data is in quadrant II
					if (h < maxHeight - 1 && data[h + 1, w] == dataCell)
					{
						rNode.Edges.South = EdgeType.None;
					}
					else
					{
						rNode.Edges.South = EdgeType.Wall;
					}

					//North; if we're in-bounds and the northPeek is the same value, no wall
					//this *is* North despite the weird offsets, because our data is in quadrant II
					if (h > 0 && data[h - 1, w] == dataCell)
					{
						rNode.Edges.North = EdgeType.None;
					}
					else
					{
						rNode.Edges.North = EdgeType.Wall;
					}

					//this will translate the width x height into a top-left Quadrant II type grid
					output[w, h] = rNode;
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
					if (peekNode.Edges.North != EdgeType.None)
					{

						//found a new shape. Create a new vertex at w,h, then traverse the edges
						Position firstVertex = new Position(w, h);

						//the first edge is always known because we just checked for it explicitly.
						RectEdge firstEdge = new RectEdge(firstVertex, firstVertex + East, peekNode.Edges.North);

						RectShape foundShape = TranscribeShape(firstVertex, firstEdge, peekNode, parentRegion, data, maxWidth, maxHeight);


						retShapes.Add(parentRegion, foundShape);
						parentRegion++;

						//now that we've added the shape, check to see if there's a hole as well.
						//there's a hole if the y-1 node has a south border. (Since we're going top to bottom, that would be impossible UNLESS we're in a hole
						if (h != 0) //but we can skip the top-most row, ofcourse.
						{
							RectNode peekHole = data[w, h - 1];
							if (peekNode.Edges.South != EdgeType.None)
							{
								//we found a new hole. Treat this as a shape (because it is).
								Position firstHoleVertex = new Position(w, h);

								//the first edge is always known because we just checked for it explicitly.
								RectEdge firstHoleEdge = new RectEdge(firstHoleVertex, firstHoleVertex + West, peekNode.Edges.South);


								RectShape foundHole = TranscribeShape(firstHoleVertex, firstHoleEdge, peekHole, -1, data, maxWidth, maxHeight);

								//starting with peekHole, look north for a rectNode with parentRegion != -1. That's the shape this hole is within.
								int i_scanUpwards = h - 1;
								while (i_scanUpwards >= 0)
								{
									RectNode holeParent = data[w, i_scanUpwards];
									if (holeParent.ParentRegion == -1)
									{
										i_scanUpwards--;
										continue;
									}

									//found a parent ID. Look up which shape it is, and add foundHole as a hole on that shape.
									retShapes[holeParent.ParentRegion].AddHole(foundHole);
									break;
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
		private static RectShape TranscribeShape(Position firstVertex, RectEdge firstEdge, RectNode peekNode, int parentRegion, RectNode[,] data, int maxWidth, int maxHeight)
		{


			RectEdge workingEdge = firstEdge;

			HashSet<RectEdge> perimeterList = new HashSet<RectEdge>
						{
							workingEdge
						};

			ClearEdge(peekNode, Direction.North, parentRegion); //clear out the edge as we traverse it

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

			foreach (Vertex v in concaves)
			{
				//vertical cogrid chords
				List<Vertex> vertCogrids = concaves.FindAll(q => q.Vert.xPos == v.Vert.xPos && q.Vert.yPos != v.Vert.yPos);
				//it's only a valid chord if the shape doesn't have an edge at every step between the two verts.
				Vertex topV;
				Vertex bottomV;
				foreach (Vertex cg in vertCogrids)
				{
					if (cg.Vert.yPos > v.Vert.yPos)
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
					RectEdge prospectiveEdge = new RectEdge(topV.Vert, bottomV.Vert, EdgeType.None);
					if (vertEdges.Contains(prospectiveEdge)) continue;

					//look to see if there is an open edge space in any point between the two verts
					RectEdge interveningEdge = rectShape.Perimeter.Find(r => (r.FirstPosition.xPos == v.Vert.xPos &&
																					   r.FirstPosition.yPos < bottomV.Vert.yPos &&
																					   r.FirstPosition.yPos > topV.Vert.yPos) ||
																					   (r.SecondPosition.xPos == v.Vert.xPos &&
																					   r.SecondPosition.yPos < bottomV.Vert.yPos &&
																					   r.SecondPosition.yPos > topV.Vert.yPos));
					if (interveningEdge != null) continue; //can't construct chord, there's something in the way

					//this is a valid vertical chord
					vertEdges.Add(prospectiveEdge);

				}

				//horizontal cogrid chords
				List<Vertex> horizCogrids = concaves.FindAll(q => q.Vert.xPos != v.Vert.xPos && q.Vert.yPos == v.Vert.yPos);
				//it's only a valid chord if the shape doesn't have an edge at every step between the two verts.
				Vertex leftV;
				Vertex rightV;
				foreach (Vertex cg in horizCogrids)
				{
					if (cg.Vert.xPos > v.Vert.xPos)
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
					RectEdge prospectiveEdge = new RectEdge(leftV.Vert, rightV.Vert, EdgeType.None);
					if (horizEdges.Contains(prospectiveEdge)) continue;

					//look to see if there is an open edge space in any point between the two verts
					RectEdge interveningEdge = rectShape.Perimeter.Find(r => (r.FirstPosition.yPos == v.Vert.yPos &&
																					   r.FirstPosition.xPos < rightV.Vert.yPos &&
																					   r.FirstPosition.xPos > leftV.Vert.yPos) ||
																					   (r.SecondPosition.yPos == v.Vert.yPos &&
																					   r.SecondPosition.yPos < rightV.Vert.yPos &&
																					   r.SecondPosition.yPos > leftV.Vert.yPos));
					if (interveningEdge != null) continue; //can't construct chord, there's something in the way

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

			//nodes created, now set connections from U(horizontal) -> V(vertical).
			//for each edge in horizEdges or vertEdges, if they intersect or share a vertex, a connection exists
			//check for intersection
			foreach (RectFlowNode vNode in vertNodes)
			{
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
					}
					else if (LineSegmentsIntersect(vNode.Edge, hNode.Edge))
					{
						//intersect, so add a link from vert to horiz.
						hNode.AddLink(vNode);
					}
				}
			}

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

			while (shapesToDecompose.Count > 0)
			{
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
								prevEdge = hole.Perimeter.Find(e => e.SecondPosition.Equals(workingVert.Vert));
								workingEdge = prevEdge.Next;
							}
						}

						var closestEdge = GetClosestEdge(workingShape, workingVert, workingEdge, prevEdge.HeadingDirection);

						//check if the closest edge is part of the working shapes perimeter
						if (workingShape.Perimeter.Contains(closestEdge))
						{
							//hole-shape cut

							shapesToDecompose.AddRange(CutShapeWithChords(workingShape, new RectEdge(new Position(workingVert.Vert), closestEdge.FirstPosition, EdgeType.None)));
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

							RectShape combinedHole = CombineHolesUsingEdge(workingHole, otherHole, new RectEdge(workingEdge.FirstPosition, closestEdge.FirstPosition, EdgeType.None));
							workingShape.RemoveHole(workingHole);
							workingShape.RemoveHole(otherHole);
							workingShape.AddHole(combinedHole);
							continue; //rerun this shape, now that it has one fewer holes
						}
					}
				}
				//working on a concave vert in the shape (not a hole)
				else
				{
					prevEdge = workingShape.Perimeter.Find(e => e.SecondPosition.Equals(workingVert.Vert));
					workingEdge = prevEdge.Next;

					var closestEdge = GetClosestEdge(workingShape, workingVert, workingEdge, prevEdge.HeadingDirection);

					//shape-self cut or hole-shape cut
					shapesToDecompose.AddRange(CutShapeWithChords(workingShape, new RectEdge(new Position(workingVert.Vert), closestEdge.FirstPosition, EdgeType.None)));
					shapesToDecompose.Remove(workingShape);
					continue;
				}
			}



			return rectangles;
		}

		private static RectEdge GetClosestEdge(RectShape workingShape, Vertex workingVert, RectEdge workingEdge, Direction prevHeading)
		{
			//hole to cut. cut direction can be any direction except heading && opposite of prevNode's heading
			List<RectEdge> horizCutTargets = workingShape.Perimeter.FindAll(e => e.FirstPosition.yPos == workingVert.Vert.yPos && e.FirstPosition.xPos != workingVert.Vert.xPos);
			List<RectEdge> vertCutTargets = workingShape.Perimeter.FindAll(e => e.FirstPosition.xPos == workingVert.Vert.xPos && e.FirstPosition.yPos != workingVert.Vert.yPos);

			foreach (RectShape hole in workingShape.Holes)
			{
				horizCutTargets.AddRange(hole.Perimeter.FindAll(e => e.FirstPosition.yPos == workingVert.Vert.yPos && e.FirstPosition.xPos != workingVert.Vert.xPos));
				vertCutTargets.AddRange(hole.Perimeter.FindAll(e => e.FirstPosition.xPos == workingVert.Vert.xPos && e.FirstPosition.yPos != workingVert.Vert.yPos));
			}

			//cut opposite of the existing heading.
			switch (workingEdge.HeadingDirection)
			{
				case Direction.East:
					//keep only those west of the workingVertex
					horizCutTargets = horizCutTargets.FindAll(e => e.FirstPosition.xPos < workingVert.Vert.xPos);
					break;
				case Direction.South:
					//keep only those north of the working vertex
					vertCutTargets = vertCutTargets.FindAll(e => e.FirstPosition.yPos < workingVert.Vert.yPos);
					break;
				case Direction.West:
					//keep only those east of the workingVertex
					horizCutTargets = horizCutTargets.FindAll(e => e.FirstPosition.xPos > workingVert.Vert.xPos);
					break;
				case Direction.North:
					//keep only those south of the workingVertex
					vertCutTargets = vertCutTargets.FindAll(e => e.FirstPosition.yPos > workingVert.Vert.yPos);
					break;
			}

			//we're here because we changed directions, so the previous heading is one of the directions that we know
			//is "inwards" or "outwards" of the shape (instead of along the perimeter)
			switch (prevHeading)
			{
				case Direction.West:
					//keep only those west of the workingVertex
					horizCutTargets = horizCutTargets.FindAll(e => e.FirstPosition.xPos < workingVert.Vert.xPos);
					break;
				case Direction.North:
					//keep only those north of the working vertex
					vertCutTargets = vertCutTargets.FindAll(e => e.FirstPosition.yPos < workingVert.Vert.yPos);
					break;
				case Direction.East:
					//keep only those east of the workingVertex
					horizCutTargets = horizCutTargets.FindAll(e => e.FirstPosition.xPos > workingVert.Vert.xPos);
					break;
				case Direction.South:
					//keep only those south of the workingVertex
					vertCutTargets = vertCutTargets.FindAll(e => e.FirstPosition.yPos > workingVert.Vert.yPos);
					break;
			}


			//find the nearest of these
			List<RectEdge> closeEdges = new List<RectEdge>();
			closeEdges.AddRange(horizCutTargets);
			closeEdges.AddRange(vertCutTargets);

			return closeEdges.OrderBy(e => (e.FirstPosition - workingVert.Vert).Magnitude).First();
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
		private static RectShape CombineHolesUsingEdge(RectShape firstHole, RectShape secondHole, RectEdge spanningEdge)
		{
			//check to see if the chord contains both positions as part of the shape's perimeter.
			//we want to use the SecondPosition, since that's where the next edge starts from.
			var firstIncision = firstHole.Perimeter.Find(edge => edge.SecondPosition.Equals(spanningEdge.FirstPosition));
			var secondIncision = secondHole.Perimeter.Find(edge => edge.SecondPosition.Equals(spanningEdge.SecondPosition));

			//the list of finalVerts is easy -- it's all the verts of firstHole + all the verts of secondHole, + the intersection point
			//if it's not a vert of secondHole. And if it's not a ver of secondhole, it's always convex due to the nature of our intersection (I *think*)
			List<Vertex> finalVerts = new List<Vertex>();
			finalVerts.AddRange(firstHole.Vertices);
			finalVerts.AddRange(secondHole.Vertices);
			if (secondHole.Vertices.Find(v => v.Vert.Equals(spanningEdge.SecondPosition)) == null)
			{
				//add the new vert we're about to create.
				finalVerts.Add(new Vertex(new Position(spanningEdge.SecondPosition), false));
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
				firstChord.Last().Next = secondAdjacent;
				secondIncision.Next = secondChord.First();
				secondChord.Last().Next = firstAdjacent;

				//.Nexts all set, now trace the results into holePerimeter
				holePerimeter.Add(firstIncision);
				RectEdge loopEdge = firstIncision.Next;
				while (loopEdge != firstIncision)
				{
					holePerimeter.Add(loopEdge);
					loopEdge = loopEdge.Next;
				}

				return new RectShape() { Perimeter = holePerimeter, Vertices = finalVerts };
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
				RectShape ogShape = null;
				foreach (RectShape rshape in retShapes)
				{
					//check to see if the chord contains both positions as part of the shape's perimeter.
					//we want to use the SecondPosition, since that's where the next edge starts from.
					var firstIncision = rshape.Perimeter.Find(edge => edge.SecondPosition.Equals(redge.FirstPosition));
					var secondIncision = rshape.Perimeter.Find(edge => edge.SecondPosition.Equals(redge.SecondPosition));
					if (firstIncision != null && secondIncision != null)
					{
						//bool is "IsConcave"
						Dictionary<Position, bool> rectVertices = rshape.Vertices.Select(v => new KeyValuePair<Position, bool>(v.Vert, v.IsConcave)).ToDictionary(k => k.Key, v => v.Value);
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

							RectShape eastCut = CutPerimeterIntoShapes(firstIncision, secondIncision, rectVertices, chordEdgeUp, North, Direction.East, rshape.Holes);
							RectShape westCut = CutPerimeterIntoShapes(secondIncision, firstIncision, rectVertices, chordEdgeDown, North, Direction.West, rshape.Holes);
							shapesToAdd.Add(eastCut);
							shapesToAdd.Add(westCut);

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

							RectShape southCut = CutPerimeterIntoShapes(firstIncision, secondIncision, rectVertices, chordEdgeWest, West, Direction.South, rshape.Holes);
							RectShape northCut = CutPerimeterIntoShapes(secondIncision, firstIncision, rectVertices, chordEdgeEast, West, Direction.North, rshape.Holes);
							shapesToAdd.Add(southCut);
							shapesToAdd.Add(northCut);
						}
						else
						{
							throw new Exception("Non-grid-aligned chord cut. Abort!");
						}

						ogShape = rshape;
					}
				}

				if (shapesToAdd.Count > 0)
				{
					retShapes.AddRange(shapesToAdd);
					retShapes.Remove(ogShape);
				}
				else
				{
					throw new Exception("Chord cut across multiple shapes");
				}
			}

			return retShapes;

		}

		/// <summary>
		/// Cuts the shape "rectShape" into two smaller shapes along the given chord.
		/// </summary>
		/// <param name="rectShape"></param>
		/// <param name="chords"></param>
		/// <returns></returns>
		private static List<RectShape> CutShapeWithChords(RectShape rectShape, RectEdge chord)
		{
			List<RectShape> shapesToAdd = new List<RectShape>();

			//check to see if the chord contains both positions as part of the shape's perimeter.
			//we want to use the SecondPosition, since that's where the next edge starts from.
			var firstIncision = rectShape.Perimeter.Find(edge => edge.SecondPosition.Equals(chord.FirstPosition));
			var secondIncision = rectShape.Perimeter.Find(edge => edge.SecondPosition.Equals(chord.SecondPosition));
			if (firstIncision != null && secondIncision != null)
			{
				//bool is "IsConcave"
				Dictionary<Position, bool> rectVertices = rectShape.Vertices.Select(v => new KeyValuePair<Position, bool>(v.Vert, v.IsConcave)).ToDictionary(k => k.Key, v => v.Value);
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

					RectShape eastCut = CutPerimeterIntoShapes(firstIncision, secondIncision, rectVertices, chordEdgeUp, North, Direction.East, rectShape.Holes);
					RectShape westCut = CutPerimeterIntoShapes(secondIncision, firstIncision, rectVertices, chordEdgeDown, North, Direction.West, rectShape.Holes);
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

					RectShape southCut = CutPerimeterIntoShapes(firstIncision, secondIncision, rectVertices, chordEdgeWest, West, Direction.South, rectShape.Holes);
					RectShape northCut = CutPerimeterIntoShapes(secondIncision, firstIncision, rectVertices, chordEdgeEast, West, Direction.North, rectShape.Holes);
					shapesToAdd.Add(southCut);
					shapesToAdd.Add(northCut);
				}
				else
				{
					throw new Exception("Non-grid-aligned chord cut. Abort!");
				}
			}

			return shapesToAdd;

		}

		///need to reset hole list after this?
		private static RectShape CutPerimeterIntoShapes(RectEdge firstIncision, RectEdge secondIncision, Dictionary<Position, bool> rectVertices, List<RectEdge> chordEdgeUp, Position directionPosition, Direction cutSide, List<RectShape> holes)
		{
			//now we have to merge the chordEdge lists with some subsection of the original perimeter to get the child shapes.
			List<RectEdge> edges = new List<RectEdge>();
			List<Vertex> vertices = new List<Vertex>();
			Vertex temp;
			Position oppositeDirection = new Position(directionPosition.xPos * -1, directionPosition.yPos * -1);

			//if we have holes, chordEdgeUp (our cut) may be jagged.
			//for each hole in the OG shape, determine if it's left, right, or spans the cut line.
			List<RectShape> carriedHoles = new List<RectShape>();
			//List<RectShape> spanHoles = new List<RectShape>();
			//List<RectShape> foundSpan = new List<RectShape>();
			switch (cutSide)
			{
				case Direction.West:
					//holes must all be < firstIncision.secondPosition to be passed on to child shapes.
					carriedHoles = (holes.FindAll(s => s.Vertices.Find(v => v.Vert.xPos >= firstIncision.SecondPosition.xPos) == null));

					foreach (RectShape rs in carriedHoles)
					{
						holes.Remove(rs);
					}

					//foundSpan = holes.FindAll(s => (s.Vertices.Find(v => v.Vert.xPos < firstIncision.SecondPosition.xPos) != null) && (s.Vertices.Find(v => v.Vert.xPos >= firstIncision.SecondPosition.xPos) != null));
					//spanHoles.AddRange(foundSpan);
					//everything else can be ignored
					break;

				case Direction.East:
					//holes must all be > firstIncision.secondPosition to be passed on to child shapes.
					carriedHoles = (holes.FindAll(s => s.Vertices.Find(v => v.Vert.xPos >= firstIncision.SecondPosition.xPos) == null));

					foreach (RectShape rs in carriedHoles)
					{
						holes.Remove(rs);
					}
					//foundSpan = holes.FindAll(s => (s.Vertices.Find(v => v.Vert.xPos > firstIncision.SecondPosition.xPos) != null) && (s.Vertices.Find(v => v.Vert.xPos <= firstIncision.SecondPosition.xPos) != null));
					//don't add these to spanholes for East. We already added them on West.
					//spanHoles.AddRange(foundSpan);
					//everything else can be ignored
					break;
				case Direction.North:
					//holes must all be < firstIncision.secondPosition to be passed on to child shapes.
					carriedHoles = (holes.FindAll(s => s.Vertices.Find(v => v.Vert.yPos >= firstIncision.SecondPosition.yPos) == null));

					foreach (RectShape rs in carriedHoles)
					{
						holes.Remove(rs);
					}

					//foundSpan = holes.FindAll(s => (s.Vertices.Find(v => v.Vert.yPos < firstIncision.SecondPosition.yPos) != null) && (s.Vertices.Find(v => v.Vert.yPos >= firstIncision.SecondPosition.yPos) != null));
					//spanHoles.AddRange(foundSpan);
					//everything else can be ignored
					break;
				case Direction.South:
					//holes must all be > firstIncision.secondPosition to be passed on to child shapes.
					carriedHoles = (holes.FindAll(s => s.Vertices.Find(v => v.Vert.yPos <= firstIncision.SecondPosition.yPos) == null));

					foreach (RectShape rs in carriedHoles)
					{
						holes.Remove(rs);
					}

					//foundSpan = holes.FindAll(s => (s.Vertices.Find(v => v.Vert.yPos > firstIncision.SecondPosition.yPos) != null) && (s.Vertices.Find(v => v.Vert.yPos <= firstIncision.SecondPosition.yPos) != null));
					//don't add these to spanholes for South. We already added them on North.
					//spanHoles.AddRange(foundSpan);
					//everything else can be ignored
					break;
			}

			//for each of the span holes, we need the min and max yPos that the hole intersects our incision at. 
			//(this can be more than 2, but we need only the min & max because everything else is part of the perim of the hole itself)
			//chordEdgeUp = GetJaggedPerimeter(chordEdgeUp, foundSpan, cutSide);






			//we're cutting "down" from first incision to second incision, so following the Next's from 
			//the first incision to the second incision gives us the east edges, and following the second
			//incision around to the first incision gives us the west edges.

			RectEdge workingEdge = firstIncision;
			if (rectVertices.ContainsKey(workingEdge.Next.SecondPosition))
			{
				//this vertex is part of the new shape
				vertices.Add(new Vertex(workingEdge.Next.SecondPosition, rectVertices[workingEdge.Next.SecondPosition]));
			}

			while (workingEdge.Next != secondIncision) //get the east side
			{
				edges.Add(workingEdge.Next);
				workingEdge = workingEdge.Next;

				if (rectVertices.ContainsKey(workingEdge.Next.SecondPosition))
				{
					//this vertex is part of the new shape
					vertices.Add(new Vertex(workingEdge.Next.SecondPosition, rectVertices[workingEdge.Next.SecondPosition]));
				}
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
			//the cycle is now complete for the east shape. Add last two vertices (the incisions)
			//or update them to be convex if they were previously concave (happens in Stage 1 of the decomp)
			temp = vertices.Find(v => v.Vert.Equals(firstIncision.SecondPosition));
			if (temp == null)
			{
				//vertex doesn't exist, so add a new one.
				//check if it's _actually_ a vertex
				if (edges.Any(we => we.FirstPosition.Equals(firstIncision.SecondPosition + directionPosition) && edges.Any(ee => ee.FirstPosition.Equals(firstIncision.SecondPosition + oppositeDirection))))
				{
					//not a vertex
				}
				else
				{
					vertices.Add(new Vertex(firstIncision.SecondPosition, false));
				}
			}
			else
			{
				//vertex exists, so check if it's _still_ a vertex
				if (edges.Any(we => we.FirstPosition.Equals(temp.Vert + directionPosition) && edges.Any(ee => ee.FirstPosition.Equals(temp.Vert + oppositeDirection))))
				{
					//no longer a vertex, so remove it
					vertices.Remove(temp);
				}
				else
				{
					//concave => convex
					temp.SetConvex();
				}
			}
			temp = vertices.Find(v => v.Vert.Equals(secondIncision.SecondPosition));
			if (temp == null)
			{
				//vertex doesn't exist, so add a new one.
				//check if it's _actually_ a vertex
				if (edges.Any(we => we.FirstPosition.Equals(secondIncision.SecondPosition + directionPosition) && edges.Any(ee => ee.FirstPosition.Equals(secondIncision.SecondPosition + oppositeDirection))))
				{
					//not a vertex
				}
				else
				{
					vertices.Add(new Vertex(secondIncision.SecondPosition, false));
				}
			}
			else
			{
				//vertex exists, so check if it's _still_ a vertex
				if (edges.Any(we => we.FirstPosition.Equals(temp.Vert + directionPosition) && edges.Any(ee => ee.FirstPosition.Equals(temp.Vert + oppositeDirection))))
				{
					//no longer a vertex, so remove it
					vertices.Remove(temp);
				}
				else
				{
					//concave => convex
					temp.SetConvex();
				}
			}

			return new RectShape() { Perimeter = edges, Vertices = vertices, Holes = carriedHoles };
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
			List<RectFlowNode> path;
			while (sourceNode.DestinationNodes.Count > 0)
			{
				path = new List<RectFlowNode>();

				//Depth-First search to the goal
				List<RectFlowNode> nodePath = sourceNode.FindLinkedNode(sinkNode);

				if (nodePath == null) break; //no more paths

				//reverse each link in the path in nodePath
				for (int i = 1; i < nodePath.Count; i++)
				{
					nodePath[i - 1].ReverseLink(nodePath[i]);
				}

				//try to find another path. repeat
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
				FilloutVerts(shape, false);
				foreach (RectShape hole in shape.Holes)
				{
					FilloutVerts(hole, true);
				}
			}

			return input;
		}

		private static void FilloutVerts(RectShape shape, bool firstVertConcave)
		{
			//first vertex is always convex (90* inner angle) because of how we parse the data
			RectEdge firstEdge = shape.Perimeter[0];
			Direction firstHeading = firstEdge.HeadingDirection; //always east

			HashSet<Vertex> shapeVerts = new HashSet<Vertex>
				{
					new Vertex(firstEdge.FirstPosition, false)
				};

			RectEdge currentEdge = null;
			RectEdge prevEdge = firstEdge;
			Direction currentHeading;
			Direction prevHeading = firstHeading;

			for (int i = 1; i < shape.Perimeter.Count; i++)
			{
				currentEdge = shape.Perimeter[i];
				currentHeading = currentEdge.HeadingDirection;

				int concavity = ((int)prevHeading - (int)currentHeading + 3) % 4;

				//prevHeading - currentHeading  + 3 % 4  //the +1 is actually meaningless here. Oh well.

				//add +3 to everything % 4 and 0 == concave, 3 == nothing, 2 == convex 1 == 180* concave 
				//South - East = ( 1 - 0) == 4 % 4 = 0 (concave)
				//South - South = ( 1 - 1) == 3
				//South - West = (1 - 2) == 2 (convex)

				//West - South = (2 - 1) == 4 % 4 = 0 (concave)
				//West - West = (2 - 2) == 3
				//West - North = (2 - 3) == 2 (convex)

				//North - West = (3 - 2) == 4 % 4 = 0 (concave)
				//North - North = (3 - 3) == 3 
				//North - East = (3 - 0) == 6 % 4 = 2(convex)

				//East - North (0 - 3) == 0 (concave)
				//East - East (0 - 0) == 3 
				//East - South (0 - 1) == 2 (convex)

				switch (concavity)
				{
					case 0:// % in C# is the REMAINDER operator, not the Modulo operator, so negative values are possible if we didn't +3
						   //concave vertex
					case 1:
						shapeVerts.Add(new Vertex(currentEdge.FirstPosition, true));
						break;

					case 2:
						//convex vertex
						shapeVerts.Add(new Vertex(currentEdge.FirstPosition, false));
						break;
					case 3:
						//not a vert
						break;
				}

				prevEdge = currentEdge;
				prevHeading = currentHeading;
			}

			shape.Vertices = new List<Vertex>(shapeVerts);
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
			switch (direction)
			{
				case Direction.East:
					edgeNode.Edges.East = EdgeType.None; //clear out the edge as we traverse it
					break;
				case Direction.South:
					edgeNode.Edges.South = EdgeType.None; //clear out the edge as we traverse it
					break;
				case Direction.West:
					edgeNode.Edges.West = EdgeType.None; //clear out the edge as we traverse it
					break;
				case Direction.North:
					edgeNode.Edges.North = EdgeType.None; //clear out the edge as we traverse it
					break;
			}
			if (parent != -1)
			{
				edgeNode.ParentRegion = parent;
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
