using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			public List<RectifyRectangle> nearestStartNeighbors = new List<RectifyRectangle>();
			public List<RectifyRectangle> nearestEndNeighbors = new List<RectifyRectangle>();

			/// <summary>
			/// Not used for equality
			/// </summary>
			public List<Position> pathNodes = new List<Position>();

			public readonly HashSet<EdgeType> pathEdges;

			public PathQuery(RectifyRectangle start, RectifyRectangle end, IEnumerable<EdgeType> edges, List<RectifyRectangle> nearestStart, List<RectifyRectangle> nearestEnd, List<Position> path)
			{
				startRect = start;
				endRect = end;
				pathEdges = new HashSet<EdgeType>(edges);
				this.nearestStartNeighbors = nearestStart;
				this.nearestEndNeighbors = nearestEnd;
				this.pathNodes = path;
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
				return (startRect == p.startRect) && (endRect == p.endRect) && (HashSetContainsSameEdges(p.pathEdges)) && ListsOrderedTheSame(this.nearestStartNeighbors, p.nearestStartNeighbors) && ListsOrderedTheSame(this.nearestEndNeighbors, p.nearestEndNeighbors);
			}

			/// <summary>
			/// Returns true if the lists contain the same elements in the same order.
			/// </summary>
			/// <param name="nearestNeighbors1"></param>
			/// <param name="nearestNeighbors2"></param>
			/// <returns></returns>
			private static bool ListsOrderedTheSame(List<RectifyRectangle> myNeighbors, List<RectifyRectangle> othersNeighbors)
			{
				//We only care about neighbors up to the point where we exit.
				//This means our count should always be less-than or equal to the comparing list
				if (myNeighbors.Count > othersNeighbors.Count) return false;
				for (int i = 0; i < myNeighbors.Count; i++)
				{
					if (myNeighbors[i] != othersNeighbors[i])
					{
						return false;
					}
				}

				return true;
			}

			public bool Equals(PathQuery p)
			{
				// If parameter is null return false:
				if (p is null)
				{
					return false;
				}

				// Return true if the fields match:
				return (startRect == p.startRect) && (endRect == p.endRect) && (HashSetContainsSameEdges(p.pathEdges)) && ListsOrderedTheSame(this.nearestStartNeighbors, p.nearestStartNeighbors) && ListsOrderedTheSame(this.nearestEndNeighbors, p.nearestEndNeighbors);
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

			//not sure we use this outside of limited debugging
			public override string ToString()
			{
				return "Edges: " + GetEdgesString();
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

		/// <summary>
		/// Changes the pathgroup at the given Position, splitting one rectangle into 3-5
		/// Clears the cache of any paths using the old rectangle, and sets a dirty flag on the pathfinder.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="v"></param>
		public void ReplaceCellAt(Position position, int pathGroup)
		{
			//find the rectangle that contains this position.
			var containingRect = FindRectangleAroundPoint(position);
			var ogPathGroup = containingRect.PathGroup;

			//no change
			if (ogPathGroup == pathGroup) return;

			IsDirty = true;

			//if it's a 1x1 rectangle, we don't need to do anything other than update the pathGroup / BaseCost
			//if (containingRect.Width == 1 && containingRect.Height == 1)
			//{
			//	containingRect.BaseCost = BaseCost;
			//	containingRect.PathGroup = pathGroup;
			//	//TODO: update cache etc.
			//	return;
			//}

			var offsetVector = position - containingRect.Offset;

			//in all other cases, 3 steps:
			//1. Create a new Rectify Rectangle to be the center cell
			RectifyRectangle centerCell = new RectifyRectangle(position, new Position(position.xPos + 1, position.yPos + 1),
																new RectNeighbor[1], new RectNeighbor[1], new RectNeighbor[1], new RectNeighbor[1], pathGroup);
			//2. create top & bottom rectangles (these are the narrow ones, going straight up/down but picked arbitrarily)
			RectifyRectangle topRect = null, botRect = null;
			if (containingRect.Height - offsetVector.yPos - 1 > 0)
			{
				int topHeight = containingRect.Top - offsetVector.yPos - 1;
				topRect = new RectifyRectangle(new Position(position.xPos, position.yPos + 1), new Position(position.xPos + 1, position.yPos + topHeight + 1),
												new RectNeighbor[topHeight], new RectNeighbor[topHeight], new RectNeighbor[1], new RectNeighbor[1], ogPathGroup);
			}
			if (offsetVector.yPos != 0)
			{
				int botHeight = offsetVector.yPos - containingRect.Bottom;
				botRect = new RectifyRectangle(new Position(position.xPos, containingRect.Bottom), new Position(position.xPos + 1, position.yPos),
												new RectNeighbor[botHeight], new RectNeighbor[botHeight], new RectNeighbor[1], new RectNeighbor[1], ogPathGroup);
			}
			//3. create left & right rectangles
			RectifyRectangle leftRect = null, rightRect = null;
			if (offsetVector.xPos != 0)
			{
				int leftWidth = offsetVector.xPos - containingRect.Left;
				leftRect = new RectifyRectangle(new Position(containingRect.Left, containingRect.Bottom), new Position(containingRect.Left + offsetVector.xPos, containingRect.Top),
												new RectNeighbor[containingRect.Height], new RectNeighbor[containingRect.Height], new RectNeighbor[leftWidth], new RectNeighbor[leftWidth], ogPathGroup);
			}
			if (containingRect.Width - offsetVector.xPos - 1 > 0)
			{
				int rightWidth = containingRect.Right - offsetVector.xPos - 1;
				rightRect = new RectifyRectangle(new Position(position.xPos + 1, containingRect.Bottom), new Position(containingRect.Right, containingRect.Top),
												new RectNeighbor[containingRect.Height], new RectNeighbor[containingRect.Height], new RectNeighbor[rightWidth], new RectNeighbor[rightWidth], ogPathGroup);
			}


			List<RectifyRectangle> newRects = new List<RectifyRectangle>() { centerCell };
			if (topRect != null) newRects.Add(topRect);
			if (botRect != null) newRects.Add(botRect);
			if (leftRect != null) newRects.Add(leftRect);
			if (rightRect != null) newRects.Add(rightRect);

			//now link the rectangles with any neighbors of the parent;
			List<RectifyRectangle> parentNeighbors = containingRect.AllNeighbors;
			parentNeighbors.AddRange(newRects);

			//Link Rectangles here.
			foreach (RectifyRectangle linkRect in newRects)
			{
				//left edge
				var leftNeighbors = parentNeighbors.FindAll(r => r.Right == linkRect.Left && (linkRect.Bottom < r.Top && linkRect.Top > r.Bottom));
				linkRect.SetNeighbors(leftNeighbors, Direction.West);

				//right edge
				var rightNeighbors = parentNeighbors.FindAll(r => r.Left == linkRect.Right && (linkRect.Bottom < r.Top && linkRect.Top > r.Bottom));
				linkRect.SetNeighbors(rightNeighbors, Direction.East);

				//top edge
				var topNeighbors = parentNeighbors.FindAll(r => r.Bottom == linkRect.Top && (linkRect.Left < r.Right && linkRect.Right > r.Left));
				linkRect.SetNeighbors(topNeighbors, Direction.North);

				//bottom edge
				var bottomNeighbors = parentNeighbors.FindAll(r => r.Top == linkRect.Bottom && (linkRect.Left < r.Right && linkRect.Right > r.Left));
				linkRect.SetNeighbors(bottomNeighbors, Direction.South);
			}

			//finally, set the edges on the center cell (and surrounding cells) to be wall-type. (If they weren't, we wouldn't have called this method!)
			centerCell.LeftEdge[0].EdgeType = EdgeType.Wall;
			if (leftRect != null)
			{
				//rectangle was split, so this is definitely a wall
				leftRect.RightEdge[offsetVector.yPos].EdgeType = EdgeType.Wall;
			}
			else
			{
				//find by point search
				var tempLeft = FindRectangleAroundPoint(new Position(position.xPos - 1, position.yPos), true);
				if (tempLeft != null)
				{
					var leftOffsetVector = position - tempLeft.Offset;
					tempLeft.RightEdge[leftOffsetVector.yPos].EdgeType = tempLeft.PathGroup == centerCell.PathGroup ? EdgeType.None : EdgeType.Wall;
					tempLeft.RightEdge[leftOffsetVector.yPos].Neighbor = centerCell;
					centerCell.LeftEdge[0].EdgeType = tempLeft.PathGroup == centerCell.PathGroup ? EdgeType.None : EdgeType.Wall;
					//centerCell.LeftEdge[0].Neighbor = tempLeft;
				}

			}

			centerCell.RightEdge[0].EdgeType = EdgeType.Wall;
			if (rightRect != null)
			{
				rightRect.LeftEdge[offsetVector.yPos].EdgeType = EdgeType.Wall;

			}
			else
			{
				//find by point search
				var tempRight = FindRectangleAroundPoint(new Position(position.xPos + 1, position.yPos), true);
				if (tempRight != null)
				{
					var rightOffsetVector = position - tempRight.Offset;
					tempRight.LeftEdge[rightOffsetVector.yPos].EdgeType = tempRight.PathGroup == centerCell.PathGroup ? EdgeType.None : EdgeType.Wall;
					tempRight.LeftEdge[rightOffsetVector.yPos].Neighbor = centerCell;
					centerCell.RightEdge[0].EdgeType = tempRight.PathGroup == centerCell.PathGroup ? EdgeType.None : EdgeType.Wall;
				}
			}

			centerCell.TopEdge[0].EdgeType = EdgeType.Wall;
			if (topRect != null)
			{

				topRect.BottomEdge[0].EdgeType = EdgeType.Wall;
			}
			else
			{
				//find by point search
				var tempTop = FindRectangleAroundPoint(new Position(position.xPos, position.yPos + 1), true);
				if (tempTop != null)
				{
					var topOffsetVector = position - tempTop.Offset;
					tempTop.BottomEdge[topOffsetVector.xPos].EdgeType = tempTop.PathGroup == centerCell.PathGroup ? EdgeType.None : EdgeType.Wall;
					tempTop.BottomEdge[topOffsetVector.xPos].Neighbor = centerCell;
					centerCell.TopEdge[0].EdgeType = tempTop.PathGroup == centerCell.PathGroup ? EdgeType.None : EdgeType.Wall;
				}
			}
			centerCell.BottomEdge[0].EdgeType = EdgeType.Wall;
			if (botRect != null)
			{

				botRect.TopEdge[0].EdgeType = EdgeType.Wall;
			}
			else
			{
				//find by point search
				var tempBot = FindRectangleAroundPoint(new Position(position.xPos, position.yPos - 1), true);
				if (tempBot != null)
				{
					var botOffsetVector = position - tempBot.Offset;
					tempBot.TopEdge[botOffsetVector.xPos].EdgeType = tempBot.PathGroup == centerCell.PathGroup ? EdgeType.None : EdgeType.Wall;
					tempBot.TopEdge[botOffsetVector.xPos].Neighbor = centerCell;
					centerCell.BottomEdge[0].EdgeType = tempBot.PathGroup == centerCell.PathGroup ? EdgeType.None : EdgeType.Wall;
				}
			}

			this.RectNodes.Remove(containingRect);
			this.RectNodes.AddRange(newRects);

		}

		/// <summary>
		/// Used for lattice Pathfinders. 2x+1,2y+1 is the base cell, then adjust accordingly.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="edgeDirection"></param>
		/// <param name="pathGroup"></param>
		public void ReplaceCellAt(Position position, Direction edgeDirection, int pathGroup)
		{
			int newX = position.xPos * 2 + 1;
			int newY = position.yPos * 2 + 1;
			switch (edgeDirection)
			{
				//simple case -- just do the regular version of this with the
				//translated coords.
				case Direction.Center:
					ReplaceCellAt(new Position(newX, newY), pathGroup);
					return;
				case Direction.East:
					newX++;
					break;
				case Direction.South:
					newY--;
					break;
				case Direction.West:
					newX--;
					break;
				case Direction.North:
					newY++;
					break;

				default:
					throw new Exception("Direction unaccounted for");
			}
			//The below may or may not be needed

			////more complicated case: run this on 1, 2 or 3 nodes.
			////first look up which node is *always* relevant. (the cardinal ones)
			////then include any associated corners with the same parentRect;
			////send that LIST to be converted into a new rectangle.
			//Position rectLow, rectHigh;
			//RectifyRectangle baseRect = FindRectangleAroundPoint(new Position(newX, newY));
			//// Y+-1
			//if (edgeDirection == Direction.West || edgeDirection == Direction.East)
			//{
			//	rectHigh = FindRectangleAroundPoint(new Position(newX, newY + 1)) == baseRect ? new Position(newX, newY + 1) : new Position(newX, newY);
			//	rectLow = FindRectangleAroundPoint(new Position(newX, newY - 1)) == baseRect ? new Position(newX, newY - 1) : new Position(newX, newY);
			//}
			//// X+-1
			//else
			//{
			//	rectHigh = FindRectangleAroundPoint(new Position(newX + 1, newY)) == baseRect ? new Position(newX + 1, newY) : new Position(newX, newY);
			//	rectLow = FindRectangleAroundPoint(new Position(newX - 1, newY)) == baseRect ? new Position(newX - 1, newY) : new Position(newX, newY);
			//}

			ReplaceCellAt(new Position(newX, newY), pathGroup);
		}

		private void ReplaceCellAt(Position rectLow, Position rectHigh, int pathGroup)
		{
			throw new NotImplementedException();
		}

		public class PathfinderMetrics
		{
			public int FrontierSize { get; set; }
			public int VisitedNodes { get; set; }
			public long RuntimeInMillis { get; set; }

			public PathfinderMetrics(int frontier, int visited, long runtime)
			{
				FrontierSize = frontier;
				VisitedNodes = visited;
				RuntimeInMillis = runtime;
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
		public object NodeCount
		{
			get
			{
				return RectNodes.Count;
			}
		}

		private readonly List<PathQuery> pathCache = new List<PathQuery>();
		private readonly int pathCacheSize = 0;
		public bool IsDirty { get; private set; }
		public bool IsLattice { get; private set; }

		public RectifyPathfinder(List<RectifyRectangle> rectNodes, bool isLattice, int pathCacheSize = 20)
		{
			this.RectNodes = rectNodes;
			this.pathCacheSize = pathCacheSize;
			this.IsLattice = isLattice;
		}


		/// <summary>
		/// Returns the bottomLeft & topRight positions of the rectify rect that encapsulates this point.
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public Tuple<Position, Position> GetRectBordersFromPoint(Position p)
		{
			if (IsLattice)
			{
				//multiply starting position
				Position truePosition = new Position(p.xPos * 2 + 1, p.yPos * 2 + 1);
				int lowX = p.xPos;
				int highX = p.xPos;
				int lowY = p.yPos;
				int highY = p.yPos;

				RectifyRectangle startRect = null;

				foreach (RectifyRectangle rr in RectNodes)
				{
					if (rr.ContainsPoint(truePosition, .5f))
					{
						startRect = rr;
						break;
					}
				}

				//look in the 4 cardinal directions until we find something that's not startRect;
				RectifyRectangle northLook = FindRectangleAroundPoint(new Position(2 * p.xPos + 1, 2 * (highY + 1) + 1), true);
				while (northLook != null)
				{
					if (northLook == startRect)
					{
						highY++;
						northLook = FindRectangleAroundPoint(new Position(2 * p.xPos + 1, 2 * (highY + 1) + 1), true);
					}
					else
					{
						break;
					}
				}
				RectifyRectangle southLook = FindRectangleAroundPoint(new Position(2 * p.xPos + 1, 2 * (lowY - 1) + 1), true);
				while (southLook != null)
				{
					if (southLook == startRect)
					{
						lowY--;
						southLook = FindRectangleAroundPoint(new Position(2 * p.xPos + 1, 2 * (lowY - 1) + 1), true);
					}
					else
					{
						break;
					}
				}

				RectifyRectangle westLook = FindRectangleAroundPoint(new Position(2 * (lowX - 1) + 1, 2 * p.yPos + 1), true);
				while (westLook != null)
				{
					if (westLook == startRect)
					{
						lowX--;
						westLook = FindRectangleAroundPoint(new Position(2 * (lowX - 1) + 1, 2 * p.yPos + 1), true);
					}
					else
					{
						break;
					}
				}
				RectifyRectangle eastLook = FindRectangleAroundPoint(new Position(2 * (highX + 1) + 1, 2 * p.yPos + 1), true);
				while (eastLook != null)
				{
					if (eastLook == startRect)
					{
						highX++;
						eastLook = FindRectangleAroundPoint(new Position(2 * (highX + 1) + 1, 2 * p.yPos + 1), true);
					}
					else
					{
						break;
					}
				}

				return new Tuple<Position, Position>(new Position(lowX, lowY), new Position(highX + 1, highY + 1));

			}
			else
			{
				foreach (RectifyRectangle rr in RectNodes)
				{
					if (rr.ContainsPoint(p, .5f))
					{
						return new Tuple<Position, Position>(new Position(rr.Left, rr.Bottom), new Position(rr.Right, rr.Top));
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Calculates a path from the given start position to the given end position for this Pathfinder's list of
		/// rectangles. Only rectangles with a shared edge within flagsMask will be considered. This overload Includes additional
		/// metrics about the nature of the pathfinding.
		/// </summary>
		/// <param name="startPosition"></param>
		/// <param name="endPosition"></param>
		/// <param name="metrics"></param>
		/// <param name="flagsMask"></param>
		/// <returns></returns>
		public List<Position> CalculatePath(Position startPosition, Position endPosition, out PathfinderMetrics metrics, int flagsMask = (int)EdgeType.None)
		{
			var watch = Stopwatch.StartNew();
			PathfinderMetrics results = new PathfinderMetrics(0, 0, 0);
			// something to time
			var pathResult = CalculatePath(startPosition, endPosition, flagsMask, results);
			// done timing
			watch.Stop();
			results.RuntimeInMillis = watch.ElapsedMilliseconds;

			metrics = results;

			return pathResult;
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
			//hide the metric overload so there's less confusion about which method to call
			return CalculatePath(startPosition, endPosition, flagsMask, null);
		}


		private List<Position> CalculatePath(Position initialPosition, Position finalPosition, int flagsMask = (int)EdgeType.None, PathfinderMetrics metrics = null)
		{
			Position startPosition, endPosition;
			if (IsLattice)
			{
				//multiply by 2x+1
				startPosition = new Position(2 * initialPosition.xPos + 1, 2 * initialPosition.yPos + 1);
				endPosition = new Position(2 * finalPosition.xPos + 1, 2 * finalPosition.yPos + 1);
			}
			else
			{
				startPosition = initialPosition;
				endPosition = finalPosition;
			}

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
			//if (startRect == endRect)
			//{
			//	//can move directly to destination b/c it's within the same rectangle
			//	return new List<Position>() { startPosition, endPosition };
			//}
			//construct path query to see if it's in the cache
			PathQuery cacheQuery = new PathQuery(startRect, endRect, edgeTypesFromMask, GetNearestNeighbors(startRect, startPosition, edgeTypesFromMask), GetNearestNeighbors(endRect, endPosition, edgeTypesFromMask), null);
			var cacheResult = pathCache.Find(c => c.Equals(cacheQuery));
			if (cacheResult != null)
			{
				//return cached path w/ current start / end position

				//move cached path to the top of the list;
				pathCache.Remove(cacheResult);
				pathCache.Insert(0, cacheResult);

				List<Position> path = new List<Position>(cacheResult.pathNodes)
				{
					endPosition
				};
				path.Insert(0, startPosition);

				//hit the cache, so no processing at all.
				if (metrics != null)
				{
					metrics.FrontierSize = 0;
					metrics.VisitedNodes = 0;
				}

				return IsLattice ? TranslatePath(path) : path;
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
				List<Position> path = GetPathBetweenRectangles(startPosition, endPosition, startRect, endRect, edgeTypesFromMask, out int visitedNodeCount, out int frontierNodeCount);

				if (path.Count == 0)
				{
					//no path found
					return path;
				}

				//copy & remove the first/last nodes
				List<Position> cachePath = new List<Position>(path);
				cachePath.RemoveAt(0);
				cachePath.RemoveAt(cachePath.Count - 1);

				//set the cachePath on the cacheQuery we constructed earlier (it's still valid)
				cacheQuery.pathNodes = cachePath;

				//remove the extra neighbor nodes by getting the first rectangle from the path that isn't the start rectangle.
				//first, get the rect immediately after the startRect
				TrimNeighbors(startRect, cacheQuery.nearestStartNeighbors, cachePath, false);
				TrimNeighbors(endRect, cacheQuery.nearestStartNeighbors, cachePath, false);

				//and the same from the end node
				//intentionally broke

				//cache
				pathCache.Insert(0, cacheQuery);

				//add metrics if requested
				if (metrics != null)
				{
					metrics.FrontierSize = frontierNodeCount;
					metrics.VisitedNodes = visitedNodeCount;
				}

				return IsLattice ? TranslatePath(path) : path;
			}

		}

		/// <summary>
		/// reduce each value by 1, then halve.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private List<Position> TranslatePath(List<Position> path)
		{
			List<Position> translatedPath = new List<Position>();
			foreach (Position p in path)
			{
				//these points don't reflect traversable nodes
				if (p.xPos % 2 == 0 || p.yPos % 2 == 0) continue;

				var newPos = new Position((p.xPos - 1) / 2, (p.yPos - 1) / 2);
				translatedPath.Add(newPos);
			}

			return translatedPath;
		}

		/// <summary>
		/// If the nearest neighbors for the start / endpoints are in the same order, any path between start / end rect must be the same.
		/// Anything further away than the actual path taken can be discounted as non-optimal. (I think)
		/// </summary>
		/// <param name="startRect"></param>
		/// <param name="neighbors"></param>
		/// <param name="initialPath"></param>
		/// <param name="applyReverse"></param>
		private static void TrimNeighbors(RectifyRectangle startRect, List<RectifyRectangle> neighbors, List<Position> initialPath, bool applyReverse)
		{
			var path = new List<Position>(initialPath);
			if (applyReverse) path.Reverse();

			for (int i = 0; i < path.Count; i++)
			{
				if (startRect.ContainsPoint(path[i])) continue;

				//we have the first point not in the startRect
				//minus 1 because if the last rect is the nearest neighbor we need the whole list anyway
				for (int j = 0; j < neighbors.Count - 1; j++)
				{
					//skip until we find the first path rect
					if (neighbors[j].ContainsPoint(path[i]) == false) continue;
					{
						//drop all neighbors in the j+1th elements.
						neighbors.RemoveRange(j + 1, neighbors.Count - j - 1);
					}
				}
				break;
			}
		}

		/// <summary>
		/// Gets a list of the neighbors for the given rect, and the minimum distance to leave 
		/// via them.
		/// </summary>
		/// <param name="startRect"></param>
		/// <param name="startPosition"></param>
		/// <returns></returns>
		private List<RectifyRectangle> GetNearestNeighbors(RectifyRectangle startRect, Position startPosition, HashSet<EdgeType> allowedEdges)
		{
			Dictionary<RectifyRectangle, int> nearDistanceCache = new Dictionary<RectifyRectangle, int>();


			//top & bottom edge

			for (int i = 0; i < startRect.TopEdge.Length; i++)
			{
				var topPair = startRect.TopEdge[i];

				if (allowedEdges.Contains(topPair.EdgeType) == false || topPair.Neighbor == null)
				{
					//not a valid neighbor
				}
				else
				{
					Position topOffset = startRect.Offset + new Position(i, startRect.Height);

					if (nearDistanceCache.TryGetValue(topPair.Neighbor, out int oldValue))
					{
						int newValue = (topOffset - startPosition).Magnitude;
						if (newValue < oldValue)
						{
							nearDistanceCache[topPair.Neighbor] = newValue;
						}
					}
					else
					{
						//not in neighbor cache, add it.
						nearDistanceCache[topPair.Neighbor] = (topOffset - startPosition).Magnitude;
					}
				}
				//bottom
				var botPair = startRect.BottomEdge[i];
				if (allowedEdges.Contains(botPair.EdgeType) == false || botPair.Neighbor == null)
				{
					//not a valid neighbor
				}
				else
				{
					Position botOffset = startRect.Offset + new Position(i, 0);

					if (nearDistanceCache.TryGetValue(botPair.Neighbor, out int oldValue))
					{
						int newValue = (botOffset - startPosition).Magnitude;
						if (newValue < oldValue)
						{
							nearDistanceCache[botPair.Neighbor] = newValue;
						}
					}
					else
					{
						//not in neighbor cache, add it.
						nearDistanceCache[botPair.Neighbor] = (botOffset - startPosition).Magnitude;
					}
				}
			}

			//left & right edge

			for (int j = 0; j < startRect.LeftEdge.Length; j++)
			{
				var leftPair = startRect.LeftEdge[j];

				if (allowedEdges.Contains(leftPair.EdgeType) == false || leftPair.Neighbor == null)
				{
					//not a valid neighbor
				}
				else
				{
					Position leftOffset = startRect.Offset + new Position(0, j);

					if (nearDistanceCache.TryGetValue(leftPair.Neighbor, out int oldValue))
					{
						int newValue = (leftOffset - startPosition).Magnitude;
						if (newValue < oldValue)
						{
							nearDistanceCache[leftPair.Neighbor] = newValue;
						}
					}
					else
					{
						//not in neighbor cache, add it.
						nearDistanceCache[leftPair.Neighbor] = (leftOffset - startPosition).Magnitude;
					}
				}
				//bottom
				var rightPair = startRect.RightEdge[j];
				if (allowedEdges.Contains(rightPair.EdgeType) == false || rightPair.Neighbor == null)
				{
					//not a valid neighbor
				}
				else
				{
					Position rightOffset = startRect.Offset + new Position(startRect.Width, j);

					if (nearDistanceCache.TryGetValue(rightPair.Neighbor, out int oldValue))
					{
						int newValue = (rightOffset - startPosition).Magnitude;
						if (newValue < oldValue)
						{
							nearDistanceCache[rightPair.Neighbor] = newValue;
						}
					}
					else
					{
						//not in neighbor cache, add it.
						nearDistanceCache[rightPair.Neighbor] = (rightOffset - startPosition).Magnitude;
					}
				}
			}

			//now convert to list of RectifyRectangles, after orderby-ing the distance.
			var rectList = nearDistanceCache.ToList().OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key);

			return rectList.ToList();
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
			//if it's the same rect, always reachable. (Though optimal path may not be, we're not calculating that yet)
			if (startRect == endRect) return true;

			HashSet<RectifyRectangle> foundNeighbors = new HashSet<RectifyRectangle>() { startRect };
			List<RectifyRectangle> neighborsToAdd = new List<RectifyRectangle>(GetNeighborsSimple(startRect, edgeTypesFromMask));

			while (neighborsToAdd.Count > 0)
			{
				var workingNeighbor = neighborsToAdd[0];
				neighborsToAdd.RemoveAt(0);
				foundNeighbors.Add(workingNeighbor);

				if (workingNeighbor == endRect) return true;

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
		private List<Position> GetPathBetweenRectangles(Position startPos, Position endPos, RectifyRectangle startRect, RectifyRectangle endRect, HashSet<EdgeType> edgeTypesFromMask, out int visitedNodeCount, out int frontierNodeCount)
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
				visitedNodeCount = visitedNodes.Count;
				frontierNodeCount = frontierNodes.Count;
				return new List<Position>();
			}
			else
			{
				List<RectifyNode> reversePath = new List<RectifyNode>();
				RectifyNode iterNode = goalNode;
				while (iterNode != startNode)
				{
					reversePath.Add(iterNode);
					iterNode = iterNode.PrevNode;
				}

				//and finally add the start node.
				reversePath.Add(startNode);

				reversePath.Reverse();

				//TODO: Make this optional
				//condense the paths if they're in the same node.
				var groupedPaths = reversePath.GroupBy(n => n.NodeRect);
				List<Position> finalPath = new List<Position>();
				foreach (var rectPos in groupedPaths)
				{
					finalPath.Add(rectPos.First().Position);
					if (rectPos.Count() > 1)
					{
						finalPath.Add(rectPos.Last().Position);
					}
				}
				visitedNodeCount = visitedNodes.Count;
				frontierNodeCount = frontierNodes.Count;

				return finalPath;
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

			//Add Diagonals in here. Even if you don't allow diagnoal movement, we can take advantage of
			//it here to find the ideal node paths b/c we're in a rectangle.

			//topLeft
			{
				RectifyNode topLeft = null;
				//can't be on the left or top edges already.
				if (parent.Left < nodePos.xPos && parent.Top - 1 > nodePos.yPos)
				{
					//add -1,+1 until nodePos.xpos = left or parent.Top - 1
					var leftSteps = nodePos.xPos - parent.Left;
					var upSteps = parent.Top - 1 - nodePos.yPos;
					var minSteps = Math.Min(leftSteps, upSteps);

					topLeft = new RectifyNode(parent, new Position(nodePos.xPos - minSteps, nodePos.yPos + minSteps));
					//                     base cost * distance travelled                         + cost to get here
					topLeft.PathCost = parent.BaseCost * ((nodePos - topLeft.Position).Magnitude);
					topLeft.Manhatten = (goalPos - topLeft.Position).Magnitude;
					outNodes.Add(topLeft);
				}
			}

			//topRight
			{
				RectifyNode topRight = null;
				//can't be on the right or top edges already.
				if (parent.Right - 1 > nodePos.xPos && parent.Top - 1 > nodePos.yPos)
				{
					//add +1,+1 until nodePos.xpos = right - 1 or parent.Top - 1
					var rightSteps = parent.Right - 1 - nodePos.xPos;
					var upSteps = parent.Top - 1 - nodePos.yPos;
					var minSteps = Math.Min(rightSteps, upSteps);

					topRight = new RectifyNode(parent, new Position(nodePos.xPos + minSteps, nodePos.yPos + minSteps));
					//                     base cost * distance travelled                         + cost to get here
					topRight.PathCost = parent.BaseCost * ((nodePos - topRight.Position).Magnitude);
					topRight.Manhatten = (goalPos - topRight.Position).Magnitude;
					outNodes.Add(topRight);
				}
			}

			//bottomRight
			{
				RectifyNode bottomRight = null;
				//can't be on the right or bottom edges already.
				if (parent.Right - 1 > nodePos.xPos && parent.Bottom < nodePos.yPos)
				{
					//add +1,-1 until nodePos.xpos = right -1 or parent.Bottom
					var rightSteps = parent.Right - 1 - nodePos.xPos;
					var downSteps = nodePos.yPos - parent.Bottom;
					var minSteps = Math.Min(downSteps, rightSteps);

					bottomRight = new RectifyNode(parent, new Position(nodePos.xPos + minSteps, nodePos.yPos - minSteps));
					//                     base cost * distance travelled                         + cost to get here
					bottomRight.PathCost = parent.BaseCost * ((nodePos - bottomRight.Position).Magnitude);
					bottomRight.Manhatten = (goalPos - bottomRight.Position).Magnitude;
					outNodes.Add(bottomRight);
				}
			}

			//bottomLeft
			{
				RectifyNode bottomLeft = null;
				//can't be on the right or bottom edges already.
				if (parent.Left < nodePos.xPos && parent.Bottom < nodePos.yPos)
				{
					//add -1,-1 until nodePos.xpos = parent.left or parent.Bottom
					var leftSteps = nodePos.xPos - parent.Left;
					var downSteps = nodePos.yPos - parent.Bottom;
					var minSteps = Math.Min(downSteps, leftSteps);

					bottomLeft = new RectifyNode(parent, new Position(nodePos.xPos - minSteps, nodePos.yPos - minSteps));
					//                     base cost * distance travelled                         + cost to get here
					bottomLeft.PathCost = parent.BaseCost * ((nodePos - bottomLeft.Position).Magnitude);
					bottomLeft.Manhatten = (goalPos - bottomLeft.Position).Magnitude;
					outNodes.Add(bottomLeft);
				}
			}

			//left
			{
				RectifyNode leftNode = null;
				if (parent.Left < nodePos.xPos && (parent.Top - 1 == nodePos.yPos || parent.Bottom == nodePos.yPos))
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
				if (parent.Right - 1 > nodePos.xPos && (parent.Top - 1 == nodePos.yPos || parent.Bottom == nodePos.yPos))
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
				if (parent.Top - 1 > nodePos.yPos && (parent.Left == nodePos.xPos || parent.Right - 1 == nodePos.xPos))
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
				if (parent.Bottom < nodePos.yPos && (parent.Left == nodePos.xPos || parent.Right - 1 == nodePos.xPos))
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

		private RectifyRectangle FindRectangleAroundPoint(Position position, bool allowNull = false)
		{
			foreach (RectifyRectangle rr in RectNodes)
			{
				if (rr.ContainsPoint(position, .5f)) return rr;
			}

			if (allowNull) return null;

			throw new PathOutOfBoundsException("Position: " + position.ToString() + "was not within this pathfinder's rect nodes");
		}
	}
}
