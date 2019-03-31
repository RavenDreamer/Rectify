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

		protected class RectifyNode
		{

			public RectifyRectangle NodeRect { get; set; }

			public RectifyNode(RectifyRectangle nodeRect)
			{
				this.NodeRect = nodeRect;
			}

			public int PathCost { get; set; }
			public int ManhattenFromGoal { get; set; }

			public RectifyNode PrevNode { get; set; }

			/// <summary>
			/// the tile on the PrevNode we entered into
			/// </summary>
			public NodeEdge EntryPoint { get; set; }

			/// <summary>
			/// the tile on the PrevNode we entered from
			/// </summary>
			public Position EntryPointOffset
			{
				get
				{
					Position offset = null;
					switch (EntryPoint.Direction)
					{
						case Direction.North:
							offset = new Position(0, -1);
							break;
						case Direction.East:
							offset = new Position(-1, 0);
							break;
						case Direction.West:
							offset = new Position(1, 0);
							break;
						case Direction.South:
							offset = new Position(0, 1);
							break;
						case Direction.Unknown:
							offset = new Position(0, 0);
							break;
					}

					return offset + EntryPoint.Position;
				}
			}

			//when determining equality (for lists / hashsets), all we care about
			//is whether or not the two Rectangles the nodes were made from
			//are the same reference
			public override bool Equals(object obj)
			{
				if (obj is RectifyNode == false) return false;
				return this.NodeRect.Equals((obj as RectifyNode).NodeRect) && this.EntryPoint.Position.Equals((obj as RectifyNode).EntryPoint.Position);
			}

			public override int GetHashCode()
			{
				return this.NodeRect.GetHashCode() ^ this.EntryPoint.Position.GetHashCode();
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
			if (pathCache.Contains(cacheQuery))
			{
				//return cached path w/ current start / end position
				PathQuery cacheResult = pathCache.Find(pq => pq.startRect == cacheQuery.startRect && pq.endRect == cacheQuery.endRect && pq.pathEdges == cacheQuery.pathEdges);
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
				//calculate the whole path
				List<Position> path = GetPathBetweenRectangles(startPosition, endPosition, startRect, endRect, edgeTypesFromMask);
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
		/// Uses a depth-first search to find the optimum path between the two rectangles
		/// </summary>
		/// <param name="startRect"></param>
		/// <param name="endRect"></param>
		/// <param name="edgeTypesFromMask"></param>
		/// <returns></returns>
		private List<Position> GetPathBetweenRectangles(Position startPos, Position endPos, RectifyRectangle startRect, RectifyRectangle endRect, HashSet<EdgeType> edgeTypesFromMask)
		{
			List<RectifyNode> frontier = new List<RectifyNode>() { new RectifyNode(startRect) { EntryPoint = new NodeEdge(startPos, Direction.Unknown) } };
			HashSet<RectifyNode> visitedNodes = new HashSet<RectifyNode>();

			while (frontier.Count > 0)
			{
				RectifyNode currentNode = frontier.First();
				frontier.Remove(currentNode);
				visitedNodes.Add(currentNode);

				//step 1 - get all neighbors who match at least one of the edgeTypes allowed
				List<RectifyNode> neighbors = GetValidNeighbors(currentNode.NodeRect, currentNode.EntryPoint, edgeTypesFromMask);
				//step 2 - calculate costs to reach each neighbor.
				foreach (var node in neighbors)
				{
					//// this would be used only when finding early-out path
					//node.ManhattenFromGoal = node.NodeRect.MinDistanceFrom(endPos);

					int pathCostDelta = (currentNode.EntryPoint.Position - node.EntryPointOffset).Magnitude + 1; //+1 for crossing between rectangles.
																												 //look up if we already have this neighbor in our visitedNodes
					if (visitedNodes.Contains(node))
					{
						//update the pathCost / previous IFF this pathCostDelta + currentNode's pathCost is lower than previous
						var ogNode = visitedNodes.Where(n => n.Equals(node)).First();
						if (ogNode.PathCost > currentNode.PathCost + pathCostDelta)
						{
							//this new route is faster
							ogNode.PathCost = currentNode.PathCost + pathCostDelta;
							ogNode.PrevNode = currentNode;
							ogNode.EntryPoint = node.EntryPoint;
						}
					}
					else
					{
						//add this to the frontier, and set the pathCost
						node.PathCost = currentNode.PathCost + pathCostDelta;
						node.PrevNode = currentNode;
						frontier.Add(node);
					}
				}
			}

			//at this point, we've exhausted the frontier. Find the node with the endRect and reverse construct the path
			IEnumerable<RectifyNode> finalNodes = visitedNodes.Where(n => n.NodeRect == endRect);
			//add the startPos -> endPos to the last node for final distance
			foreach (var rn in finalNodes)
			{
				rn.PathCost += (endPos - rn.EntryPointOffset).Magnitude;
			}
			RectifyNode shortestNode = finalNodes.OrderBy(o => o.PathCost).First();
			RectifyNode startNode = visitedNodes.Where(n => n.NodeRect == startRect).First();
			//path is endPos + finalNode's entry pos, -> prev Node.entry pos ->... ... firstNode.entryPos (which is startPos)

			List<Position> reversePath = new List<Position>();
			RectifyNode iterNode = shortestNode;
			while (iterNode.PrevNode != startNode)
			{
				reversePath.Add(iterNode.EntryPoint.Position);
				reversePath.Add(iterNode.EntryPointOffset);
				iterNode = iterNode.PrevNode;
			}
			reversePath.Add(iterNode.EntryPoint.Position);
			reversePath.Add(iterNode.EntryPointOffset);

			reversePath.Reverse();

			reversePath.Insert(0, startPos);
			reversePath.Add(endPos);

			var finalPath = reversePath.Distinct();

			return finalPath.ToList();

		}

		/// <summary>
		/// Gets all neighbors of the given RectifyRectangle which share an edge with one of the valid 
		/// edge types from the mask
		/// </summary>
		/// <param name="nodeRect"></param>
		/// <param name="edgeTypesFromMask"></param>
		/// <returns></returns>
		private List<RectifyNode> GetValidNeighbors(RectifyRectangle nodeRect, NodeEdge startPos, HashSet<EdgeType> edgeTypesFromMask)
		{
			Dictionary<RectifyRectangle, List<NodeEdge>> neighborDict = new Dictionary<RectifyRectangle, List<NodeEdge>>();

			//left && right
			for (int i = 0; i < nodeRect.Height; i++)
			{
				RectNeighbor left = nodeRect.LeftEdge[i];

				if (left.Neighbor != null && edgeTypesFromMask.Contains(left.EdgeType))
				{
					if (neighborDict.ContainsKey(left.Neighbor))
					{
						//add this position to the neighbor's position list
						neighborDict[left.Neighbor].Add(new NodeEdge(new Position(nodeRect.Left, nodeRect.Bottom + i), Direction.West));
					}
					else
					{
						//add this neighbor to the list 
						List<NodeEdge> firstPosition = new List<NodeEdge>() { new NodeEdge(new Position(nodeRect.Left, nodeRect.Bottom + i), Direction.West) };
						neighborDict[left.Neighbor] = firstPosition;
					}
				}

				RectNeighbor right = nodeRect.RightEdge[i];

				if (right.Neighbor != null && edgeTypesFromMask.Contains(right.EdgeType))
				{
					if (neighborDict.ContainsKey(right.Neighbor))
					{
						//add this position to the neighbor's position list
						neighborDict[right.Neighbor].Add(new NodeEdge(new Position(nodeRect.Right, nodeRect.Bottom + i), Direction.East));
					}
					else
					{
						//add this neighbor to the list 
						List<NodeEdge> firstPosition = new List<NodeEdge>() { new NodeEdge(new Position(nodeRect.Right, nodeRect.Bottom + i), Direction.East) };
						neighborDict[right.Neighbor] = firstPosition;
					}
				}
			}
			//top & bot
			for (int i = 0; i < nodeRect.Width; i++)
			{
				RectNeighbor top = nodeRect.TopEdge[i];

				if (top.Neighbor != null && edgeTypesFromMask.Contains(top.EdgeType))
				{
					if (neighborDict.ContainsKey(top.Neighbor))
					{
						//add this position to the neighbor's position list
						neighborDict[top.Neighbor].Add(new NodeEdge(new Position(nodeRect.Left + i, nodeRect.Top), Direction.North));
					}
					else
					{
						//add this neighbor to the list 
						List<NodeEdge> firstPosition = new List<NodeEdge>() { new NodeEdge(new Position(nodeRect.Left + i, nodeRect.Top), Direction.North) };
						neighborDict[top.Neighbor] = firstPosition;
					}
				}

				RectNeighbor bottom = nodeRect.BottomEdge[i];

				if (bottom.Neighbor != null && edgeTypesFromMask.Contains(bottom.EdgeType))
				{
					if (neighborDict.ContainsKey(bottom.Neighbor))
					{
						//add this position to the neighbor's position list
						neighborDict[bottom.Neighbor].Add(new NodeEdge(new Position(nodeRect.Left + i, nodeRect.Bottom), Direction.South));
					}
					else
					{
						//add this neighbor to the list 
						List<NodeEdge> firstPosition = new List<NodeEdge>() { new NodeEdge(new Position(nodeRect.Left + i, nodeRect.Bottom), Direction.South) };
						neighborDict[bottom.Neighbor] = firstPosition;
					}
				}
			}

			List<RectifyNode> outList = new List<RectifyNode>();

			//have all of the neighbors, now turn them into nodes
			foreach (var neighborPair in neighborDict)
			{
				//find position w/ shortest magnitude from startPosition

				var shortestEntrance = neighborPair.Value.OrderBy(p => (p.Position - startPos.Position).Magnitude).First();

				outList.Add(new RectifyNode(neighborPair.Key)
				{
					EntryPoint = shortestEntrance
				});
			}

			return outList;
		}

		private RectifyRectangle FindRectangleAroundPoint(Position position)
		{
			foreach (RectifyRectangle rr in RectNodes)
			{
				if (rr.ContainsPoint(position)) return rr;
			}

			throw new PathOutOfBoundsException("Position: " + position.ToString() + "was not within this pathfinder's rect nodes");
		}
	}
}
