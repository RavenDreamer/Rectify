using System;
using System.Collections.Generic;
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

		protected class RectifyNode
		{

			public RectifyRectangle NodeRect { get; set; }

			public RectifyNode(RectifyRectangle nodeRect)
			{
				this.NodeRect = nodeRect;
			}

			public int PathCost { get; set; }

			public RectifyNode PrevNode { get; set; }
			//the position on the PrevNode we entered from
			public Position EntryPoint { get; set; }

			//when determining equality (for lists / hashsets), all we care about
			//is whether or not the two Rectangles the nodes were made from
			//are the same reference
			public bool Equals(RectifyNode obj)
			{
				return NodeRect.Equals(obj.NodeRect);
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
				return GetPathBetweenRectangles(startPosition, endPosition, startRect, endRect, edgeTypesFromMask);
			}

		}

		/// <summary>
		/// Uses a depth-first search to find the optimum path between the two rectangles, given the 
		/// </summary>
		/// <param name="startRect"></param>
		/// <param name="endRect"></param>
		/// <param name="edgeTypesFromMask"></param>
		/// <returns></returns>
		private List<Position> GetPathBetweenRectangles(Position startPos, Position endPos, RectifyRectangle startRect, RectifyRectangle endRect, HashSet<EdgeType> edgeTypesFromMask)
		{
			List<RectifyNode> unvisitedNodes = new List<RectifyNode>() { new RectifyNode(startRect) { EntryPoint = startPos } };
			HashSet<RectifyNode> visitedNodes = new HashSet<RectifyNode>();

			while (unvisitedNodes.Count > 0)
			{
				RectifyNode currentNode = unvisitedNodes[0];
				//step 1 - get all neighbors who match at least one of the edgeTypes allowed
				List<RectifyNode> neighbors = GetValidNeighbors(currentNode.NodeRect, startPos, edgeTypesFromMask);
			}

			throw new NotImplementedException();

		}

		/// <summary>
		/// Gets all neighbors of the given RectifyRectangle which share an edge with one of the valid 
		/// edge types from the mask
		/// </summary>
		/// <param name="nodeRect"></param>
		/// <param name="edgeTypesFromMask"></param>
		/// <returns></returns>
		private List<RectifyNode> GetValidNeighbors(RectifyRectangle nodeRect, Position startPos, HashSet<EdgeType> edgeTypesFromMask)
		{

			having trouble concentrating.
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
