using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RectifyUtils
{
	public class RectifyPathfinder
	{
		protected class PathQuery
		{
			public readonly RectifyRectangle startRect;
			public readonly RectifyRectangle endRect;
			public List<Position> pathNodes;

			public readonly HashSet<EdgeType> pathEdges;

			public PathQuery(RectifyRectangle start, RectifyRectangle end, IEnumerable<EdgeType> edges, List<Position> path)
			{
				startRect = start;
				endRect = end;
				pathEdges = new HashSet<EdgeType>(edges);
				pathNodes = path;
			}

			//modifying this method based on the MSDN implementation of "TwoDPoint"
			public override bool Equals(object obj)
			{
				// If parameter is null return false.
				if (obj == null)
				{
					return false;
				}

				// If parameter cannot be cast to Position return false.
				if (!(obj is PathQuery p))
				{
					return false;
				}

				// Return true if the fields match:
				return (startRect == p.startRect) && (endRect == p.endRect) && (HashSetContainsSameEdges(p.pathEdges));
			}

			public bool Equals(PathQuery p)
			{
				// If parameter is null return false:
				if (p is null)
				{
					return false;
				}

				// Return true if the fields match:
				return (startRect == p.startRect) && (endRect == p.endRect) && (HashSetContainsSameEdges(p.pathEdges));
			}

			//verifies that the other hashSet contains all of our elements and vice versa
			private bool HashSetContainsSameEdges(HashSet<EdgeType> pathEdges)
			{
				if (this.pathEdges.Count != pathEdges.Count)
				{
					return false;
				}
				foreach (EdgeType et in pathEdges)
				{
					if (pathEdges.Contains(et) == false)
					{
						return false;
					}
				}
				return true;
			}

			//copied from MSDN's "TwoDPoint" implementation
			public override int GetHashCode()
			{
				return startRect.GetHashCode() ^ endRect.GetHashCode() + GetPathMask();
			}

			private int GetPathMask()
			{
				int i = 0;
				foreach (EdgeType et in pathEdges)
				{
					i = i | (int)et;
				}
				return i;
			}

			public override string ToString()
			{
				return "Edges: " + GetEdgesString() + " Path: " + GetPathString();
			}

			private string GetEdgesString()
			{
				string s = "";
				foreach (EdgeType et in pathEdges)
				{
					s += et.ToString() + ", ";
				}
				return s.Substring(0, s.Length - 2);
			}

			private string GetPathString()
			{
				string s = "";
				foreach (Position p in pathNodes)
				{
					s += p.ToString();
				}
				return s;
			}
		}

		protected class NodeEdge
		{
			public Position Position { get; set; }
			public Direction Direction { get; set; }

			public NodeEdge(Position p, Direction d)
			{
				this.Position = p;
				this.Direction = d;
			}
		}

		protected class NodeNeighbor
		{
			public RectifyNode Node { get; set; }
			public Direction Direction { get; set; }
			public EdgeType EdgeType { get; set; }

			public NodeNeighbor(RectNeighbor n, Direction d)
			{
				this.EdgeType = n.EdgeType;
				this.Direction = d;
			}
		}



		protected class RectifyNode
		{

			public RectifyRectangle NodeRect { get; private set; }
			public int BaseCost
			{
				get
				{
					return NodeRect.BaseCost;
				}
			}

			public Position Position { get; private set; }

			public RectifyNode(RectifyRectangle nodeRect, Position p)
			{
				this.NodeRect = nodeRect;
				this.Position = p;
			}

			//standard node fields
			public int PathCost { get; set; }

			public RectifyNode PrevNode { get; set; }
			public int Manhatten { get; internal set; }

			//end standard node fields

			//Rectangular Symmetry Reduction helpers
			//public NodeNeighbor Left { get; set; }
			//public NodeNeighbor Right { get; set; }
			//public NodeNeighbor Top { get; set; }
			//public NodeNeighbor Bottom { get; set; }

			public override bool Equals(object obj)
			{
				if (obj is RectifyNode == false) return false;
				return this.NodeRect.Equals((obj as RectifyNode).NodeRect) && this.Position.Equals((obj as RectifyNode).Position);
			}

			public override int GetHashCode()
			{
				return this.NodeRect.GetHashCode() ^ this.Position.GetHashCode();
			}
		}



		private List<RectifyRectangle> RectNodes { get; set; }
		private readonly List<PathQuery> pathCache = new List<PathQuery>();
		private readonly int pathCacheSize = 0;

		public RectifyPathfinder(List<RectifyRectangle> rectNodes, int pathCacheSize = 20)
		{
			this.RectNodes = rectNodes;
			this.pathCacheSize = pathCacheSize;
		}


		/// <summary>
		/// Returns the topleft & bottomright positions of the rectify rect that encapsulates this point.
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public Tuple<Position, Position> GetRectBordersFromPoint(Position p)
		{
			foreach (RectifyRectangle rr in RectNodes)
			{
				if (rr.ContainsPoint(p, .5f))
				{
					return new Tuple<Position, Position>(new Position(rr.Left, rr.Top), new Position(rr.Right, rr.Bottom));
				}
			}

			return null;
		}

		/// <summary>
		/// Calculates a path from the given start position to the given end position for this Pathfinder's list of
		/// rectangles. Only rectangles with a shared edge within flagsMask will be considered.
		/// </summary>
		/// <param name="startPosition"></param>
		/// <param name="endPosition"></param>
		/// <param name="flagsMask"></param>
		/// <returns></returns>
		public List<Position> CalculatePath(Position startPosition, Position endPosition, int flagsMask = (int)EdgeType.None)
		{
			//get valid edgetypes from the mask
			HashSet<EdgeType> edgeTypesFromMask = new HashSet<EdgeType>();
			foreach (EdgeType et in Enum.GetValues(typeof(EdgeType)))
			{
				if (((int)et & flagsMask) == (int)et)
				{
					edgeTypesFromMask.Add(et);
				}
			}
			//find path rectangles
			RectifyRectangle startRect = FindRectangleAroundPoint(startPosition);
			RectifyRectangle endRect = FindRectangleAroundPoint(endPosition);
			//early out
			if (startRect == endRect)
			{
				//can move directly to destination b/c it's within the same rectangle
				return new List<Position>() { startPosition, endPosition };
			}
			//construct path query to see if it's in the cache
			PathQuery cacheQuery = new PathQuery(startRect, endRect, edgeTypesFromMask, null);
			if (pathCache.Contains(cacheQuery) && false)
			{
				//return cached path w/ current start / end position
				PathQuery cacheResult = pathCache.Find(pq => pq.startRect.Equals(cacheQuery.startRect) && pq.endRect.Equals(cacheQuery.endRect) && pq.pathEdges.Intersect(cacheQuery.pathEdges).Count() == pq.pathEdges.Count);
				//move cached path to the top of the list;
				pathCache.Remove(cacheResult);
				pathCache.Insert(0, cacheResult);

				List<Position> path = new List<Position>(cacheResult.pathNodes)
				{
					endPosition
				};
				path.Insert(0, startPosition);

				return path;
			}
			else
			{
				//determine reachability 
				if (GetRecursiveNeighbors(startRect, endRect, edgeTypesFromMask) == false)
				{
					//destination not reachable.
					return new List<Position>();
				}

				//calculate the whole path
				List<Position> path = GetPathBetweenRectangles(startPosition, endPosition, startRect, endRect, edgeTypesFromMask);

				if (path.Count == 0)
				{
					//no path found
					return path;
				}

				//copy & remove the first/last nodes
				List<Position> cachePath = new List<Position>(path);
				cachePath.RemoveAt(0);
				cachePath.RemoveAt(cachePath.Count - 1);

				//cache
				pathCache.Insert(0, new PathQuery(startRect, endRect, edgeTypesFromMask, cachePath));

				return path;
			}

		}

		/// <summary>
		/// For the set of all rectangles, is endRect reachable from startRect?
		/// </summary>
		/// <param name="startRect"></param>
		/// <param name="endRect"></param>
		/// <param name="edgeTypesFromMask"></param>
		/// <returns></returns>
		private bool GetRecursiveNeighbors(RectifyRectangle startRect, RectifyRectangle endRect, HashSet<EdgeType> edgeTypesFromMask)
		{
			HashSet<RectifyRectangle> foundNeighbors = new HashSet<RectifyRectangle>() { startRect };
			List<RectifyRectangle> neighborsToAdd = new List<RectifyRectangle>(GetNeighborsSimple(startRect, edgeTypesFromMask));

			while (neighborsToAdd.Count > 0)
			{
				var workingNeighbor = neighborsToAdd[0];
				neighborsToAdd.RemoveAt(0);
				foundNeighbors.Add(workingNeighbor);

				var neighborNeighbors = GetNeighborsSimple(workingNeighbor, edgeTypesFromMask);

				foreach (RectifyRectangle rr in neighborNeighbors)
				{

					if (rr == endRect) return true;

					if (foundNeighbors.Contains(rr))
					{
						//do nothing, already looked at
					}
					else
					{
						neighborsToAdd.Add(rr);
					}
				}
			}

			return false;


		}

		///// <summary>
		///// Generate RectifyNodes for the "macro edge" for use in Rectangular Symmetry Reduction.
		///// A 1x1 Rect returns a single node. a 2x2 rect returns 4 nodes, a 3x3 rect returns the outer 8, etc.
		///// 1xN and 2xN return every node. Most effective for larger rectangles
		///// </summary>
		///// <returns></returns>
		//internal List<RectifyNode> GetNodesForRectangle(RectifyRectangle rect)
		//{
		//	if (rect.Width == 1 && rect.Height == 1)
		//	{
		//		return new List<RectifyNode>(){
		//			new RectifyNode(rect, rect.Offset)
		//			{
		//				Left = new NodeNeighbor(rect.LeftEdge[0], Direction.West),
		//				Right = new NodeNeighbor(rect.RightEdge[0], Direction.East),
		//				Top = new NodeNeighbor(rect.TopEdge[0], Direction.North),
		//				Bottom = new NodeNeighbor(rect.BottomEdge[0], Direction.South)
		//			}
		//		};
		//	}

		//	var topNodes = new RectifyNode[rect.Width];
		//	var botNodes = new RectifyNode[rect.Width];

		//	for(int x = 0; x < rect.Width; x++)
		//	{
		//		topNodes[x] = new RectifyNode(rect, new Position(rect.Offset.xPos + x, rect.Top));
		//		botNodes[x] = new RectifyNode(rect, new Position(rect.Offset.xPos + x, rect.Bottom));
		//	}

		//	var leftNodes = new RectifyNode[rect.Height];
		//	var rightNodes = new RectifyNode[rect.Height];

		//	//don't create extra corner nodes
		//	for(int y = 1; y < rect.Height-1; y++)
		//	{
		//		leftNodes[y] = new RectifyNode(rect, new Position(rect.Left, rect.Offset.yPos + y));
		//		rightNodes[y] = new RectifyNode(rect, new Position(rect.Right, rect.Offset.yPos + y));
		//	}
		//	//set corners
		//	leftNodes[0] = botNodes[0];
		//	leftNodes[rect.Height - 1] = topNodes[0];
		//	rightNodes[0] = botNodes[rect.Width - 1];
		//	rightNodes[rect.Height - 1] = topNodes[rect.Width - 1];

		//	//set neighborNodes
		//}

		private HashSet<RectifyRectangle> GetNeighborsSimple(RectifyRectangle rect, HashSet<EdgeType> edgeTypesFromMask)
		{
			HashSet<RectifyRectangle> uniqueNeighbors = new HashSet<RectifyRectangle>();

			//left && right
			foreach (var n in rect.LeftEdge)
			{
				if (edgeTypesFromMask.Contains(n.EdgeType))
				{
					uniqueNeighbors.Add(n.Neighbor);
				}
			}

			foreach (var n in rect.RightEdge)
			{
				if (edgeTypesFromMask.Contains(n.EdgeType))
				{
					uniqueNeighbors.Add(n.Neighbor);
				}
			}

			foreach (var n in rect.TopEdge)
			{
				if (edgeTypesFromMask.Contains(n.EdgeType))
				{
					uniqueNeighbors.Add(n.Neighbor);
				}
			}

			foreach (var n in rect.BottomEdge)
			{
				if (edgeTypesFromMask.Contains(n.EdgeType))
				{
					uniqueNeighbors.Add(n.Neighbor);
				}
			}

			uniqueNeighbors.Remove(null);

			return uniqueNeighbors;
		}

		/// <summary>
		/// Uses a depth-first search to find the optimum path between the two rectangles
		/// </summary>
		/// <param name="startRect"></param>
		/// <param name="endRect"></param>
		/// <param name="edgeTypesFromMask"></param>
		/// <returns></returns>
		private List<Position> GetPathBetweenRectangles(Position startPos, Position endPos, RectifyRectangle startRect, RectifyRectangle endRect, HashSet<EdgeType> edgeTypesFromMask)
		{
			SimplePriorityQueue<RectifyNode> frontierQueue = new SimplePriorityQueue<RectifyNode>();
			var startNode = new RectifyNode(startRect, startPos) { PathCost = 0 };
			frontierQueue.Enqueue(startNode, 0);

			Dictionary<Position, RectifyNode> visitedNodes = new Dictionary<Position, RectifyNode>();
			Dictionary<Position, RectifyNode> frontierNodes = new Dictionary<Position, RectifyNode>();

			bool foundGoal = false;
			RectifyNode goalNode = null;


			while (frontierQueue.Count > 0)
			{
				RectifyNode currentNode = frontierQueue.Dequeue();
				visitedNodes.Add(currentNode.Position, currentNode);

				//step 0 - check if this is the goal node
				if (currentNode.Position.Equals(endPos))
				{
					foundGoal = true;
					goalNode = currentNode;
					break;
				}

				//step 1 - get all neighbors who match at least one of the edgeTypes allowed
				List<RectifyNode> neighbors = GetValidNeighbors(currentNode, edgeTypesFromMask, endPos, currentNode.NodeRect == endRect);

				//step 2 - determine whether or not to insert neighbors into frontier
				foreach (var node in neighbors)
				{
					//if it's been visited, ignore it
					if (visitedNodes.ContainsKey(node.Position)) continue;

					//if it's in the frontier, either update it, or ignore it
					if (frontierNodes.ContainsKey(node.Position))
					{
						//update the pathCost / previous IFF this pathCostDelta + currentNode's pathCost is lower than previous
						var ogNode = frontierNodes[node.Position];
						if (ogNode.PathCost > node.PathCost + currentNode.PathCost)
						{
							//this new route is faster
							ogNode.PathCost = node.PathCost + currentNode.PathCost;
							ogNode.PrevNode = currentNode;

							//remove from queue, re-add.
							frontierQueue.Remove(ogNode);
							frontierQueue.Enqueue(ogNode, ogNode.PathCost + ogNode.Manhatten);
						}
						else
						{
							//slower, ignore it
						}
					}
					else
					{
						//add this to the frontier, and set the pathCost
						node.PrevNode = currentNode;
						node.PathCost = node.PathCost + currentNode.PathCost;
						frontierNodes.Add(node.Position, node);
						frontierQueue.Enqueue(node, node.PathCost + node.Manhatten);
					}
				}

				//step 3 - repeat until we find a goal node
			}

			if (foundGoal == false)
			{
				//should never get here b/c we test pathability earlier.
				//maybe remove that now that we're not using janky pathfinding
				//shenannigans?
				return new List<Position>();
			}
			else
			{
				List<Position> reversePath = new List<Position>();
				RectifyNode iterNode = goalNode;
				while (iterNode != startNode)
				{
					reversePath.Add(iterNode.Position);
					iterNode = iterNode.PrevNode;
				}

				//and finally add the start node.
				reversePath.Add(startNode.Position);

				reversePath.Reverse();

				return reversePath;
			}

		}

		/// <summary>
		/// Gets all neighbors of the given RectifyRectangle which share an edge with one of the valid 
		/// edge types from the mask. If the neighbor would be in the same rect, "jump" to the far side of the rect and return that jump instead.
		/// </summary>
		/// <param name="nodeRect"></param>
		/// <param name="edgeTypesFromMask"></param>
		/// <returns></returns>
		private List<RectifyNode> GetValidNeighbors(RectifyNode currentNode, HashSet<EdgeType> edgeTypesFromMask, Position goalPos, bool isGoalRect)
		{
			List<RectifyNode> outNodes = new List<RectifyNode>();

			Position nodePos = currentNode.Position;
			RectifyRectangle parent = currentNode.NodeRect;

			//if we're in the goal Rect, add the goal as a neighbor
			if (isGoalRect)
			{
				RectifyNode goalNode = new RectifyNode(parent, new Position(goalPos));
				//                     base cost * distance travelled                         + cost to get here
				goalNode.PathCost = parent.BaseCost * (nodePos.xPos - goalNode.Position.xPos);
				outNodes.Add(goalNode);
			}

			//TODO: Add Diagonals in here. Even if you don't allow diagnoal movement, we can take advantage of
			//it here to find the ideal node paths b/c we're in a rectangle.

			//left
			{
				RectifyNode leftNode = null;
				if (parent.Left < nodePos.xPos && (parent.Top == nodePos.yPos || parent.Bottom == nodePos.yPos))
				{
					//return the adjacent node within this macro edge
					//JPS implementation goes here, I think
					leftNode = new RectifyNode(parent, new Position(nodePos.xPos - 1, nodePos.yPos));
				}
				else if (parent.Left < nodePos.xPos)
				{
					//"jump" and return corresponding left-most node within this rect.
					leftNode = new RectifyNode(parent, new Position(parent.Left, nodePos.yPos));
				}
				else if (parent.Left == nodePos.xPos)
				{
					//look in the leftEdge box to see if there's a neighbor.
					var seeker = parent.LeftEdge[nodePos.yPos - parent.Offset.yPos];
					if (seeker.Neighbor != null && edgeTypesFromMask.Contains(seeker.EdgeType))
					{
						//make new valid node
						leftNode = new RectifyNode(seeker.Neighbor, new Position(nodePos.xPos - 1, nodePos.yPos));
					}
				}
				if (leftNode != null)
				{
					//                     base cost * distance travelled                         + cost to get here
					leftNode.PathCost = parent.BaseCost * (nodePos.xPos - leftNode.Position.xPos);
					leftNode.Manhatten = (goalPos - leftNode.Position).Magnitude;
					outNodes.Add(leftNode);
				}
			}
			//right
			{
				RectifyNode rightNode = null;
				if (parent.Right - 1 > nodePos.xPos && (parent.Top == nodePos.yPos || parent.Bottom == nodePos.yPos))
				{
					//return the adjacent node within this macro edge
					//JPS implementation goes here, I think
					rightNode = new RectifyNode(parent, new Position(nodePos.xPos + 1, nodePos.yPos));
				}
				else if (parent.Right - 1 > nodePos.xPos)
				{
					//"jump" and return corresponding left-most node within this rect.
					rightNode = new RectifyNode(parent, new Position(parent.Right - 1, nodePos.yPos));
				}
				else if (parent.Right - 1 == nodePos.xPos)
				{
					//look in the rightEdge box to see if there's a neighbor.
					var seeker = parent.RightEdge[nodePos.yPos - parent.Offset.yPos];
					if (seeker.Neighbor != null && edgeTypesFromMask.Contains(seeker.EdgeType))
					{
						//make new valid node
						rightNode = new RectifyNode(seeker.Neighbor, new Position(nodePos.xPos + 1, nodePos.yPos));
					}
				}
				if (rightNode != null)
				{
					//                       base cost * distance travelled                         + cost to get here
					rightNode.PathCost = parent.BaseCost * (rightNode.Position.xPos - nodePos.xPos);
					rightNode.Manhatten = (goalPos - rightNode.Position).Magnitude;
					outNodes.Add(rightNode);
				}
			}

			//top
			{
				RectifyNode topNode = null;
				if (parent.Top - 1 > nodePos.yPos && (parent.Left == nodePos.xPos || parent.Right == nodePos.xPos))
				{
					//return the adjacent node within this macro edge
					//JPS implementation goes here, I think
					topNode = new RectifyNode(parent, new Position(nodePos.xPos, nodePos.yPos + 1));
				}
				else if (parent.Top - 1 > nodePos.yPos)
				{
					//"jump" and return corresponding left-most node within this rect.
					topNode = new RectifyNode(parent, new Position(nodePos.xPos, parent.Top - 1));
				}
				else if (parent.Top - 1 == nodePos.yPos)
				{
					//look in the topEdge box to see if there's a neighbor.
					var seeker = parent.TopEdge[nodePos.xPos - parent.Offset.xPos];
					if (seeker.Neighbor != null && edgeTypesFromMask.Contains(seeker.EdgeType))
					{
						//make new valid node
						topNode = new RectifyNode(seeker.Neighbor, new Position(nodePos.xPos, nodePos.yPos + 1));
					}
				}
				if (topNode != null)
				{
					//                   base cost * distance travelled                         + cost to get here
					topNode.PathCost = parent.BaseCost * (topNode.Position.yPos - nodePos.yPos);
					topNode.Manhatten = (goalPos - topNode.Position).Magnitude;
					outNodes.Add(topNode);
				}
			}

			//bottom
			{
				RectifyNode bottomNode = null;
				if (parent.Bottom < nodePos.yPos && (parent.Left == nodePos.xPos || parent.Right == nodePos.xPos))
				{
					//return the adjacent node within this macro edge
					//JPS implementation goes here, I think
					bottomNode = new RectifyNode(parent, new Position(nodePos.xPos, nodePos.yPos - 1));
				}
				else if (parent.Bottom < nodePos.yPos)
				{
					//"jump" and return corresponding left-most node within this rect.
					bottomNode = new RectifyNode(parent, new Position(nodePos.xPos, parent.Bottom));
				}
				else if (parent.Bottom == nodePos.yPos)
				{
					//look in the bottomEdge box to see if there's a neighbor.
					var seeker = parent.BottomEdge[nodePos.xPos - parent.Offset.xPos];
					if (seeker.Neighbor != null && edgeTypesFromMask.Contains(seeker.EdgeType))
					{
						//make new valid node
						bottomNode = new RectifyNode(seeker.Neighbor, new Position(nodePos.xPos, nodePos.yPos - 1));
					}
				}
				if (bottomNode != null)
				{
					//                   base cost * distance travelled                               + cost to get here
					bottomNode.PathCost = parent.BaseCost * (nodePos.yPos - bottomNode.Position.yPos);
					bottomNode.Manhatten = (goalPos - bottomNode.Position).Magnitude;
					outNodes.Add(bottomNode);
				}
			}

			return outNodes;
		}

		///// <summary>
		///// Gets all neighbors of the given RectifyRectangle which share an edge with one of the valid 
		///// edge types from the mask
		///// </summary>
		///// <param name="nodeRect"></param>
		///// <param name="edgeTypesFromMask"></param>
		///// <returns></returns>
		//private List<RectifyNode> GetValidNeighbors(RectifyRectangle nodeRect, NodeEdge startPos, HashSet<EdgeType> edgeTypesFromMask)
		//{
		//	Dictionary<RectifyRectangle, List<NodeEdge>> neighborDict = new Dictionary<RectifyRectangle, List<NodeEdge>>();

		//	//left && right
		//	for (int i = 0; i < nodeRect.Height; i++)
		//	{
		//		RectNeighbor left = nodeRect.LeftEdge[i];

		//		if (left.Neighbor != null && edgeTypesFromMask.Contains(left.EdgeType))
		//		{
		//			if (neighborDict.ContainsKey(left.Neighbor))
		//			{
		//				//add this position to the neighbor's position list
		//				neighborDict[left.Neighbor].Add(new NodeEdge(new Position(nodeRect.Left, nodeRect.Bottom + i), Direction.West));
		//			}
		//			else
		//			{
		//				//add this neighbor to the list 
		//				List<NodeEdge> firstPosition = new List<NodeEdge>() { new NodeEdge(new Position(nodeRect.Left, nodeRect.Bottom + i), Direction.West) };
		//				neighborDict[left.Neighbor] = firstPosition;
		//			}
		//		}

		//		RectNeighbor right = nodeRect.RightEdge[i];

		//		if (right.Neighbor != null && edgeTypesFromMask.Contains(right.EdgeType))
		//		{
		//			if (neighborDict.ContainsKey(right.Neighbor))
		//			{
		//				//add this position to the neighbor's position list
		//				neighborDict[right.Neighbor].Add(new NodeEdge(new Position(nodeRect.Right, nodeRect.Bottom + i), Direction.East));
		//			}
		//			else
		//			{
		//				//add this neighbor to the list 
		//				List<NodeEdge> firstPosition = new List<NodeEdge>() { new NodeEdge(new Position(nodeRect.Right, nodeRect.Bottom + i), Direction.East) };
		//				neighborDict[right.Neighbor] = firstPosition;
		//			}
		//		}
		//	}
		//	//top & bot
		//	for (int i = 0; i < nodeRect.Width; i++)
		//	{
		//		RectNeighbor top = nodeRect.TopEdge[i];

		//		if (top.Neighbor != null && edgeTypesFromMask.Contains(top.EdgeType))
		//		{
		//			if (neighborDict.ContainsKey(top.Neighbor))
		//			{
		//				//add this position to the neighbor's position list
		//				neighborDict[top.Neighbor].Add(new NodeEdge(new Position(nodeRect.Left + i, nodeRect.Top), Direction.North));
		//			}
		//			else
		//			{
		//				//add this neighbor to the list 
		//				List<NodeEdge> firstPosition = new List<NodeEdge>() { new NodeEdge(new Position(nodeRect.Left + i, nodeRect.Top), Direction.North) };
		//				neighborDict[top.Neighbor] = firstPosition;
		//			}
		//		}

		//		RectNeighbor bottom = nodeRect.BottomEdge[i];

		//		if (bottom.Neighbor != null && edgeTypesFromMask.Contains(bottom.EdgeType))
		//		{
		//			if (neighborDict.ContainsKey(bottom.Neighbor))
		//			{
		//				//add this position to the neighbor's position list
		//				neighborDict[bottom.Neighbor].Add(new NodeEdge(new Position(nodeRect.Left + i, nodeRect.Bottom), Direction.South));
		//			}
		//			else
		//			{
		//				//add this neighbor to the list 
		//				List<NodeEdge> firstPosition = new List<NodeEdge>() { new NodeEdge(new Position(nodeRect.Left + i, nodeRect.Bottom), Direction.South) };
		//				neighborDict[bottom.Neighbor] = firstPosition;
		//			}
		//		}
		//	}

		//	List<RectifyNode> outList = new List<RectifyNode>();

		//	//have all of the neighbors, now turn them into nodes
		//	foreach (var neighborPair in neighborDict)
		//	{
		//		//find position w/ shortest magnitude from startPosition

		//		var shortestEntrance = neighborPair.Value.OrderBy(p => (p.Position - startPos.Position).Magnitude).First();

		//		outList.Add(new RectifyNode(neighborPair.Key)
		//		{
		//			EntryPoint = shortestEntrance
		//		});
		//	}

		//	return outList;
		//}

		private RectifyRectangle FindRectangleAroundPoint(Position position)
		{
			foreach (RectifyRectangle rr in RectNodes)
			{
				if (rr.ContainsPoint(position, .5f)) return rr;
			}

			throw new PathOutOfBoundsException("Position: " + position.ToString() + "was not within this pathfinder's rect nodes");
		}
	}
}
