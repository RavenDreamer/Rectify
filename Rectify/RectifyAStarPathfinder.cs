using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RectifyUtils
{
	public class RectifyAStarPathfinder
	{

		protected class CellNode
		{

			public int BaseCost { get; set; }

			public Position Position { get; private set; }

			public CellNode(Position p, int pathCost)
			{
				this.Position = p;
				this.PathCost = pathCost;
			}

			//standard node fields
			public int PathCost { get; set; }
			public CellNode PrevNode { get; set; }
			//end standard node fields

			public override bool Equals(object obj)
			{
				if (obj is CellNode == false) return false;
				return this.Position.Equals((obj as CellNode).Position);
			}

			public override int GetHashCode()
			{
				return this.Position.GetHashCode();
			}
		}

		readonly GridLattice<IRectGrid> latticePathData = null;

		//TODO: make use of for non-lattice case, I guess?
		//IRectGrid[,] gridPathData = null;

		public RectifyAStarPathfinder(GridLattice<IRectGrid> pathData)
		{
			latticePathData = pathData;
		}

		/// <summary>
		/// Calculates a path from the given start position to the given end position for this Pathfinder's list of
		/// rectangles. This overload Includes additional
		/// metrics about the nature of the pathfinding.
		/// </summary>
		/// <param name="startPosition"></param>
		/// <param name="endPosition"></param>
		/// <param name="metrics"></param>
		/// <returns></returns>
		public List<Position> CalculatePath(Position startPosition, Position endPosition, out PathfinderMetrics metrics)
		{
			var watch = Stopwatch.StartNew();
			PathfinderMetrics results = new PathfinderMetrics(0, 0, 0);
			// something to time
			var pathResult = CalculatePath(startPosition, endPosition, results);
			// done timing
			watch.Stop();
			results.RuntimeInMillis = watch.ElapsedMilliseconds;

			metrics = results;

			return pathResult;
		}

		/// <summary>
		/// Calculates a path from the given start position to the given end position for this Pathfinder's list of
		/// rectangles.
		/// </summary>
		/// <param name="startPosition"></param>
		/// <param name="endPosition"></param>
		/// <param name="flagsMask"></param>
		/// <returns></returns>
		public List<Position> CalculatePath(Position startPosition, Position endPosition)
		{
			//hide the metric overload so there's less confusion about which method to call
			return CalculatePath(startPosition, endPosition, null);
		}

		private List<Position> CalculatePath(Position initialPosition, Position finalPosition, PathfinderMetrics metrics = null)
		{
			Position startPosition, endPosition;

			//no path needed.
			if (initialPosition.Equals(finalPosition)) return new List<Position>();

			//Allows for preprocessing (if needed again in the future)
			startPosition = initialPosition;
			endPosition = finalPosition;

			//look for caches where startposition ==||== endposition match also
			//TODO
			//end cache TODO

			List<Position> path = GetPathBetweenPositions(startPosition, endPosition, out int visitedNodeCount, out int frontierNodeCount);

			if (path.Count == 0)
			{
				//no path found
				return path;
			}


			//add metrics if requested
			if (metrics != null)
			{
				metrics.FrontierSize = frontierNodeCount;
				metrics.VisitedNodes = visitedNodeCount;
			}

			return path;
		}

		private List<Position> GetPathBetweenPositions(Position startPosition, Position endPosition, out int visitedNodeCount, out int frontierNodeCount)
		{
			SimplePriorityQueue<CellNode> frontierQueue = new SimplePriorityQueue<CellNode>();
			CellNode startNode = new CellNode(startPosition, 0);
			frontierQueue.Enqueue(startNode, 0);

			Dictionary<Position, CellNode> visitedNodes = new Dictionary<Position, CellNode>();
			Dictionary<Position, CellNode> frontierNodes = new Dictionary<Position, CellNode>();

			bool foundGoal = false;
			CellNode goalNode = null;

			while (frontierQueue.Count > 0)
			{
				CellNode currentNode = frontierQueue.Dequeue();
				visitedNodes.Add(currentNode.Position, currentNode);

				//step 0 - check if this is the goal node
				if (currentNode.Position.Equals(endPosition))
				{
					foundGoal = true;
					goalNode = currentNode;
					break;
				}

				//step 1 - get all neighbors who match at least one of the edgeTypes allowed
				List<CellNode> neighbors = GetValidNeighbors(currentNode);

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
							frontierQueue.Enqueue(ogNode, ogNode.PathCost);
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
						frontierQueue.Enqueue(node, node.PathCost);
					}
				}

				//step 3 - repeat until we find a goal node
			}

			if (foundGoal == false)
			{
				visitedNodeCount = visitedNodes.Count;
				frontierNodeCount = frontierNodes.Count;
				return new List<Position>();
			}
			else
			{
				//path is reversed, so un-reverse it.
				List<Position> finalPath = new List<Position>();
				CellNode iterNode = goalNode;
				while (iterNode != startNode)
				{
					finalPath.Add(iterNode.Position);
					iterNode = iterNode.PrevNode;
				}

				//and finally add the start node.
				finalPath.Add(startNode.Position);

				finalPath.Reverse();


				visitedNodeCount = visitedNodes.Count;
				frontierNodeCount = frontierNodes.Count;

				return finalPath;
			}
		}

		/// <summary>
		/// Generates all nodes that are adjacent to the given node. Nodes re only considered adjacent if they share a pathgroup.
		/// </summary>
		/// <param name="currentNode"></param>
		/// <returns></returns>
		private List<CellNode> GetValidNeighbors(CellNode currentNode)
		{
			IRectGrid nodeCell = latticePathData[currentNode.Position.xPos, currentNode.Position.yPos];
			int nodePathGroup = nodeCell.PathGroup();

			List<CellNode> neighbors = new List<CellNode>();

			//left
			if (latticePathData[currentNode.Position.xPos, currentNode.Position.yPos, Direction.West].PathGroup() == nodePathGroup)
			{
				if (currentNode.Position.xPos != 0)
				{
					var potentialNeighbor = latticePathData[currentNode.Position.xPos - 1, currentNode.Position.yPos];
					if (potentialNeighbor.PathGroup() == nodePathGroup)
					{
						neighbors.Add(new CellNode(new Position(currentNode.Position.xPos - 1, currentNode.Position.yPos), 1 * potentialNeighbor.PathModifier()));
					}
				}
			}
			//right
			if (latticePathData[currentNode.Position.xPos, currentNode.Position.yPos, Direction.East].PathGroup() == nodePathGroup)
			{
				if (currentNode.Position.xPos < latticePathData.Width - 1)
				{
					var potentialNeighbor = latticePathData[currentNode.Position.xPos + 1, currentNode.Position.yPos];
					if (potentialNeighbor.PathGroup() == nodePathGroup)
					{
						neighbors.Add(new CellNode(new Position(currentNode.Position.xPos + 1, currentNode.Position.yPos), 1 * potentialNeighbor.PathModifier()));
					}
				}
			}
			//down
			if (latticePathData[currentNode.Position.xPos, currentNode.Position.yPos, Direction.South].PathGroup() == nodePathGroup)
			{
				if (currentNode.Position.yPos != 0)
				{
					var potentialNeighbor = latticePathData[currentNode.Position.xPos, currentNode.Position.yPos - 1];
					if (potentialNeighbor.PathGroup() == nodePathGroup)
					{
						neighbors.Add(new CellNode(new Position(currentNode.Position.xPos, currentNode.Position.yPos - 1), 1 * potentialNeighbor.PathModifier()));
					}
				}
			}
			//up
			if (latticePathData[currentNode.Position.xPos, currentNode.Position.yPos, Direction.North].PathGroup() == nodePathGroup)
			{
				if (currentNode.Position.yPos < latticePathData.Height - 1)
				{
					var potentialNeighbor = latticePathData[currentNode.Position.xPos, currentNode.Position.yPos + 1];
					if (potentialNeighbor.PathGroup() == nodePathGroup)
					{
						neighbors.Add(new CellNode(new Position(currentNode.Position.xPos, currentNode.Position.yPos + 1), 1 * potentialNeighbor.PathModifier()));
					}
				}
			}

			return neighbors;
		}
	}
}
