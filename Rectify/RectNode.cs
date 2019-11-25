using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace RectifyUtils
{
	[Flags]
	public enum EdgeType
	{
		None = 0,
		Wall = 1,
		Aperture = 2,
		BurnedHoleEdge = 4, //the outside edge of a hole that's already been catalogued.
		Unknown = 8,
	}

	/// <summary>
	/// Data Layout for a [,] in memory
	/// </summary>
	public enum DataLayout
	{
		CodeInitializedArray, //you initialized a 2d array in code, but want the visual bottom left value to be @ 0,0
		Quadrant1, //origin is @ 0,0 in the bottom left corner. No processing needed.
	}

	public enum FlowType
	{
		source, //original source for flow nodes
		vertical, //one half of the bipartite flow graph
		horizontal, //the other half of the bipartite flow graph
		sink, //the ultimate sink for flow nodes
	}

	public enum Direction
	{
		East = 0,
		South = 1,
		West = 2,
		North = 3, //Potential TODO: make these 1,2,4,8, once it stops breaking the tests and add "Unknown - 0" ?
		Unknown = -1,
		Center = -2,
	}

	/// <summary>
	/// When iterating over int-based data, use this to override the default "EdgeType.Wall" designation
	/// </summary>
	public class RectDetectPair
	{
		public EdgeType EdgeType { get; private set; }
		HashSet<int> detector = new HashSet<int>();

		public RectDetectPair(int first, int second, EdgeType edgeOverride)
		{
			detector.Add(first);
			detector.Add(second);
			this.EdgeType = edgeOverride;
		}

		public bool MatchesEdge(int first, int second)
		{
			return detector.Contains(first) && detector.Contains(second);
		}
	}

	public class DirectionVector
	{
		public Direction Direction { get; set; }
		public Position Vector { get; set; }
	}

	public class RectNode
	{
		public class EdgeBox
		{
			public EdgeType North { get; set; }
			public EdgeType South { get; set; }
			public EdgeType West { get; set; }
			public EdgeType East { get; set; }
		}


		/// <summary>
		/// Identifies this Node as a member of a particular region (shape).
		/// </summary>
		public int ParentRegion { get; set; } = -1;

		public EdgeBox Edges { get; private set; } = new EdgeBox();

		//used for finding holes properly
		public int NorthEdgeID { get; set; } = -1;
		public int SouthEdgeID { get; set; } = -1;

		public override bool Equals(object obj)
		{
			// If parameter is null return false.
			if (obj == null)
			{
				return false;
			}

			// If parameter cannot be cast to RectNode return false.
			if (!(obj is RectNode rn))
			{
				return false;
			}

			// Return true if the fields match:
			return this.Edges.North == rn.Edges.North &&
				this.Edges.East == rn.Edges.East &&
				this.Edges.West == rn.Edges.West &&
				this.Edges.South == rn.Edges.South;
		}

		public override int GetHashCode()
		{
			int hashOut = 0;
			if (Edges.East != EdgeType.None) hashOut++;
			if (Edges.South != EdgeType.None) hashOut += 2;
			if (Edges.West != EdgeType.None) hashOut += 4;
			if (Edges.North != EdgeType.None) hashOut += 8;

			return hashOut;
		}

		internal void SetEdge(DirectionVector dv, EdgeType edge)
		{
			switch (dv.Direction)
			{
				case Direction.East:
					this.Edges.East = edge;
					break;
				case Direction.South:
					this.Edges.South = edge;
					break;
				case Direction.West:
					this.Edges.West = edge;
					break;
				case Direction.North:
					this.Edges.North = edge;
					break;
			}
		}
	}

	public class Vertex
	{
		public void SetConvex()
		{
			this.IsConcave = false;
		}
		public void SetConcave()
		{
			this.IsConcave = true;
		}

		public Position VertPosition { get; private set; }
		public bool IsConcave { get; private set; }
		public bool IsConvex
		{
			get
			{
				return !IsConcave;
			}
		}

		public Vertex(Position point, bool concave)
		{
			this.VertPosition = point;
			this.IsConcave = concave;
		}

		//copying this method from the MSDN implementation of "TwoDPoint"
		public override bool Equals(object obj)
		{
			// If parameter is null return false.
			if (obj == null)
			{
				return false;
			}

			// If parameter cannot be cast to RectEdge return false.
			if (!(obj is Vertex v))
			{
				return false;
			}

			// Return true if the fields match:
			return IsConcave == v.IsConcave && VertPosition.Equals(v.VertPosition);
		}

		public bool Equals(Vertex v)
		{
			// If parameter is null return false:
			if (v == null)
			{
				return false;
			}

			// Return true if the fields match:
			return IsConcave == v.IsConcave && VertPosition.Equals(v.VertPosition);
		}

		//copied from MSDN's "TwoDPoint" implementation
		public override int GetHashCode()
		{
			return VertPosition.GetHashCode() + (IsConcave ? 1 : 0);
		}


		public override string ToString()
		{
			return "Concave: " + IsConcave + " Vertex: " + VertPosition.ToString();
		}
	}

	/// <summary>
	/// A RectShape is a list of RectEdges
	/// </summary>
	public class RectShape
	{

		public List<RectEdge> Perimeter { get; set; } = new List<RectEdge>();

		private List<Vertex> _vertices = new List<Vertex>();
		public List<Vertex> Vertices
		{
			get
			{
				return _vertices;
			}
			set
			{
#if debug
				_vertices = value;
				//if this is a hole, there will be more concave verts than convex ones. (4 more)
				int concaveVertCount = _vertices.FindAll(v => v.IsConcave).Count;
				int convexVertCount = _vertices.Count - concaveVertCount;
				if (_vertices.Count != 0 && Math.Abs(concaveVertCount - convexVertCount) != 4)
				{
					Console.WriteLine("blarugh");
				}
#else
				_vertices = value;
#endif
			}
		}

		private List<RectShape> _holes = new List<RectShape>(); //easier when empty;
		public List<RectShape> Holes
		{
			get
			{
				return _holes; //this should be an immutable list somehow, we want this list to be modified only by the setter or the method below
			}
			set
			{
				_holes = value;
				UpdateHoleVerts();
			}
		}
		public List<Vertex> HoleVertices { get; private set; } = new List<Vertex>(); //this should be immutable as above. it could be modified currently, but never SHOULD be outside this class.

		internal void AddHole(RectShape foundHole)
		{
			_holes.Add(foundHole);
			HoleVertices.AddRange(foundHole.Vertices);
		}

		internal void RemoveHole(RectShape foundHole)
		{
			_holes.Remove(foundHole);
			foreach (Vertex v in foundHole.Vertices)
			{
				HoleVertices.Remove(v);
			}
		}

		internal void UpdateHoleVerts()
		{
			List<Vertex> holeVerts = new List<Vertex>();
			foreach (RectShape rs in _holes)
			{
				holeVerts.AddRange(rs.Vertices);
			}
			HoleVertices = holeVerts;
		}

		internal string ToPointListString()
		{
			string output = "";
			foreach (RectEdge re in this.Perimeter)
			{
				output += re.FirstPosition.xPos + "\t-" + re.FirstPosition.yPos + System.Environment.NewLine;
			}

			return output;
		}

		/// <summary>
		/// Reverses the perimeter's direction as a hole becomes part of a shape.
		/// </summary>
		internal void ReversePerimeter()
		{
			List<RectEdge> perimTraversal = new List<RectEdge> { };
			var firstPerim = this.Perimeter[0];
			var workingPerim = this.Perimeter[0];
			while (workingPerim.Next != firstPerim)
			{
				perimTraversal.Add(workingPerim);
				workingPerim = workingPerim.Next;
			}
			perimTraversal.Add(workingPerim); //don't forget the last edge that completes the loop.
											  //need to update .Next AND swap the first/last positions.
			List<RectEdge> reversedPerimeter = new List<RectEdge>();
			for (int i = perimTraversal.Count - 1; i >= 0; i--)
			{
				var reverseEdge = perimTraversal[i].SwappedPositions;
				reversedPerimeter.Add(reverseEdge);
			}
			//now walk the reversed perimeter forwards, setting next
			for (int i = 0; i < reversedPerimeter.Count - 1; i++)
			{
				reversedPerimeter[i].Next = reversedPerimeter[i + 1];
			}
			//and the last loops around to the first
			reversedPerimeter.Last().Next = reversedPerimeter[0];

			this.Perimeter = reversedPerimeter;

			//reversing the perimeter also swaps the concavity of vertices.
			foreach (Vertex v in this._vertices)
			{
				if (v.IsConcave)
				{
					v.SetConvex();
				}
				else
				{
					v.SetConcave();
				}
			}
		}
	}


	public class RectNeighbor
	{
		public RectifyRectangle Neighbor { get; set; }
		public EdgeType EdgeType { get; set; }

		public RectNeighbor(RectifyRectangle neighbor, EdgeType edgeType)
		{
			this.Neighbor = neighbor;
			this.EdgeType = edgeType;
		}
	}

	/// <summary>
	/// Represents a rectangle (4 vertices), a perimeter of edges, a list of which rectangles
	/// this rectangle neighbors, and which edges allow connections to those neighbors
	/// </summary>
	public class RectifyRectangle
	{
		//public class EdgeRange
		//{
		//	public EdgeType EdgeType { get; set; }
		//	public int LowRangeCoordinate { get; set; }
		//	public int HighRangeCoordinate { get; set; }
		//}



		internal RectNeighbor[] LeftEdge;
		internal RectNeighbor[] RightEdge;
		internal RectNeighbor[] TopEdge;
		internal RectNeighbor[] BottomEdge;

		//private HashSet<RectNeighbor> neighbors = new HashSet<RectNeighbor>();

		public int NeighborCount
		{
			get
			{
				HashSet<RectifyRectangle> uniqueNeighbors = new HashSet<RectifyRectangle>();
				foreach (RectNeighbor rn in LeftEdge)
				{
					if (rn.Neighbor == null) continue;
					uniqueNeighbors.Add(rn.Neighbor);
				}
				foreach (RectNeighbor rn in RightEdge)
				{
					if (rn.Neighbor == null) continue;
					uniqueNeighbors.Add(rn.Neighbor);
				}
				foreach (RectNeighbor rn in TopEdge)
				{
					if (rn.Neighbor == null) continue;
					uniqueNeighbors.Add(rn.Neighbor);
				}
				foreach (RectNeighbor rn in BottomEdge)
				{
					if (rn.Neighbor == null) continue;
					uniqueNeighbors.Add(rn.Neighbor);
				}

				return uniqueNeighbors.Count;
			}
		}

		private readonly Position topRight;
		private readonly Position bottomLeft;

		private RectifyRectangle(Position bottomLeft, Position topRight,
			RectNeighbor[] LeftEdge, RectNeighbor[] RightEdge,
			RectNeighbor[] TopEdge, RectNeighbor[] BottomEdge)
		{
			this.bottomLeft = bottomLeft;
			this.topRight = topRight;
			this.LeftEdge = LeftEdge;
			this.RightEdge = RightEdge;
			this.TopEdge = TopEdge;
			this.BottomEdge = BottomEdge;
		}

		public RectifyRectangle(RectShape shape, bool validateRectangle = true)
		{
			if (validateRectangle)
			{
				if (shape.Vertices.Count != 4)
				{
					throw new Exception("Rectangle w/o exactly 4 vertices");
				}
				RectEdge startEdge = shape.Perimeter.First();
				RectEdge endEdge = startEdge;
				for (int i = 0; i < shape.Perimeter.Count; i++)
				{
					endEdge = endEdge.Next;
				}
				if (startEdge != endEdge)
				{
					throw new Exception("Rectangle w/o contiguous perimeter");
				}
			}
			//get the defining positions from the verts
			//temporary assignment
			topRight = shape.Vertices.First().VertPosition;
			bottomLeft = topRight;
			for (int i = 1; i < 4; i++)
			{
				if (shape.Vertices[i].VertPosition.xPos < bottomLeft.xPos ||
					shape.Vertices[i].VertPosition.yPos < bottomLeft.yPos)
				{
					bottomLeft = shape.Vertices[i].VertPosition;
				}

				if (shape.Vertices[i].VertPosition.xPos > topRight.xPos ||
					shape.Vertices[i].VertPosition.yPos > topRight.yPos)
				{
					topRight = shape.Vertices[i].VertPosition;
				}
			}

			//instantiate the edgearrays

			//get all the edges w/ firstPosition x == topLeft.x && secondPosition x == topLeft.x
			LeftEdge = new RectNeighbor[this.Height];
			var workingEdges = shape.Perimeter.FindAll(e => e.HeadingDirection == Direction.North).OrderBy(e => e.SecondPosition.yPos).ToArray();
			for (int i = 0; i < workingEdges.Count(); i++)
			{
				LeftEdge[i] = new RectNeighbor(null, workingEdges[i].EdgeType);
			}

			RightEdge = new RectNeighbor[this.Height];
			workingEdges = shape.Perimeter.FindAll(e => e.HeadingDirection == Direction.South).OrderBy(e => e.FirstPosition.yPos).ToArray();
			for (int i = 0; i < workingEdges.Count(); i++)
			{
				RightEdge[i] = new RectNeighbor(null, workingEdges[i].EdgeType);
			}

			//get all the edges w/ firstPosition y == topLeft.y && secondPosition y == topLeft.y
			TopEdge = new RectNeighbor[this.Width];
			workingEdges = shape.Perimeter.FindAll(e => e.HeadingDirection == Direction.East).OrderBy(e => e.SecondPosition.xPos).ToArray();
			for (int i = 0; i < workingEdges.Count(); i++)
			{
				TopEdge[i] = new RectNeighbor(null, workingEdges[i].EdgeType);
			}

			BottomEdge = new RectNeighbor[this.Width];
			workingEdges = shape.Perimeter.FindAll(e => e.HeadingDirection == Direction.West).OrderBy(e => e.FirstPosition.xPos).ToArray();
			for (int i = 0; i < workingEdges.Count(); i++)
			{
				BottomEdge[i] = new RectNeighbor(null, workingEdges[i].EdgeType);
			}
		}

		/// <summary>
		/// Adds references to the neighbors at the corresponding edgePair arrays.
		/// </summary>
		/// <param name="neighbors"></param>
		/// <param name="direction"></param>
		internal void SetNeighbors(List<RectifyRectangle> newNeighbors, Direction direction)
		{
			//foreach (RectNeighbor rn in (newNeighbors.Select(rr => new RectNeighbor() { Neighbor = rr, NeighborDirection = direction })))
			//{
			//	neighbors.Add(rn);
			//}

			foreach (RectifyRectangle n in newNeighbors)
			{
				//EdgeType[] neighborEdge;

				RectNeighbor[] myEdge = getEdgeAndOffsets(direction, n, out int startOffset, out int endIndex);

				for (int i = 0; i < endIndex; i++)
				{
					myEdge[startOffset + i].Neighbor = n;
				}
			}
		}

		private RectNeighbor[] getEdgeAndOffsets(Direction direction, RectifyRectangle neighbor, out int startOffset, out int endIndex)
		{
			RectNeighbor[] myEdge;
			//calculate overlapping areas. e.g. for West:
			//if neighbor.bottom < this.bottom, start at neighbor.right[this.bottom]
			//if neighbor.bottom > this.bottom, start at neighbor.right[0]

			//if neighbor.top > this.top, end at neighbor.right[this.top]
			//if neighbor.top < this.top, end at neighbor.right[N]
			int neighborStartOffset = 0;
			startOffset = 0;
			endIndex = 0;
			switch (direction)
			{
				case Direction.East:
					//neighborEdge = neighbor.LeftEdge;
					myEdge = RightEdge;
					//start offsets
					if (neighbor.Bottom < this.Bottom)
					{
						neighborStartOffset = this.Bottom - neighbor.Bottom;
						startOffset = 0;
					}
					else if (neighbor.Bottom > this.Bottom)
					{
						neighborStartOffset = 0;
						startOffset = neighbor.Bottom - this.Bottom;
					}
					//end index
					if (neighbor.Top >= this.Top)
					{
						endIndex = this.Top - (this.Bottom + startOffset);
					}
					else if (neighbor.Top < this.Top)
					{
						endIndex = neighbor.Top - (neighbor.Bottom + neighborStartOffset);
					}
					break;
				case Direction.West:
					//neighborEdge = neighbor.RightEdge;
					myEdge = LeftEdge;
					//start offsets
					if (neighbor.Bottom < this.Bottom)
					{
						neighborStartOffset = this.Bottom - neighbor.Bottom;
						startOffset = 0;
					}
					else if (neighbor.Bottom > this.Bottom)
					{
						neighborStartOffset = 0;
						startOffset = neighbor.Bottom - this.Bottom;
					}
					//end index
					if (neighbor.Top >= this.Top)
					{
						endIndex = this.Top - (this.Bottom + startOffset);
					}
					else if (neighbor.Top < this.Top)
					{
						endIndex = neighbor.Top - (neighbor.Bottom + neighborStartOffset);
					}
					break;
				case Direction.South:
					//neighborEdge = neighbor.TopEdge;
					myEdge = BottomEdge;
					//start offsets
					if (neighbor.Left < this.Left)
					{
						neighborStartOffset = this.Left - neighbor.Left;
						startOffset = 0;
					}
					else if (neighbor.Left > this.Left)
					{
						neighborStartOffset = 0;
						startOffset = neighbor.Left - this.Left;
					}
					//end index
					if (neighbor.Right >= this.Right)
					{
						endIndex = this.Right - (this.Left + startOffset);
					}
					else if (neighbor.Right < this.Right)
					{
						endIndex = neighbor.Right - (neighbor.Left + neighborStartOffset);
					}
					break;
				case Direction.North:
					//neighborEdge = neighbor.BottomEdge;
					myEdge = TopEdge;
					//start offsets
					if (neighbor.Left < this.Left)
					{
						neighborStartOffset = this.Left - neighbor.Left;
						startOffset = 0;
					}
					else if (neighbor.Left > this.Left)
					{
						neighborStartOffset = 0;
						startOffset = neighbor.Left - this.Left;
					}
					//end index
					if (neighbor.Right >= this.Right)
					{
						endIndex = this.Right - (this.Left + startOffset);
					}
					else if (neighbor.Right < this.Right)
					{
						endIndex = neighbor.Right - (neighbor.Left + neighborStartOffset);
					}
					break;
				default:
					throw new Exception("Unknown direction for neighbors");
			}

			return myEdge;
		}

		///// <summary>
		///// Collapses a Rectangle by a factor of 3. Used for grid lattice decomp.
		///// </summary>
		///// <param name="validate"></param>
		///// <returns></returns>
		//internal RectifyRectangle Shrink(bool validate = false)
		//{
		//	Position smallBotLeft = new Position(this.bottomLeft.xPos / 3, this.bottomLeft.yPos / 3);
		//	//default to bottom left if height would be truncated
		//	Position smallTopRight = new Position(this.Width >= 3 ? this.topRight.xPos / 3 : this.bottomLeft.xPos / 3,
		//										 this.Height >= 3 ? this.topRight.yPos / 3 : this.bottomLeft.yPos / 3);

		//	RectNeighbor[] smallTop = new RectNeighbor[this.Width / 3];
		//	RectNeighbor[] smallBot = new RectNeighbor[this.Width / 3];

		//	RectNeighbor[] smallLeft = new RectNeighbor[this.Height / 3];
		//	RectNeighbor[] smallRight = new RectNeighbor[this.Height / 3];

		//	//transfer RectNeighbor wall types
		//	for (int i = 0; i < this.Width / 3; i++)
		//	{
		//		if (validate)
		//		{
		//			if (this.TopEdge[3 * i].EdgeType != this.TopEdge[(3 * i) + 1].EdgeType && this.TopEdge[3 * i].EdgeType != this.TopEdge[(3 * i) + 2].EdgeType)
		//			{
		//				throw new Exception("Data loss during shrink");
		//			}
		//			if (this.BottomEdge[3 * i].EdgeType != this.BottomEdge[(3 * i) + 1].EdgeType && this.BottomEdge[3 * i].EdgeType != this.BottomEdge[(3 * i) + 2].EdgeType)
		//			{
		//				throw new Exception("Data loss during shrink");
		//			}
		//		}

		//		smallTop[i] = this.TopEdge[3 * i];
		//		smallBot[i] = this.BottomEdge[3 * i];
		//	}
		//	for (int i = 0; i < this.Height / 3; i++)
		//	{
		//		if (validate)
		//		{
		//			if (this.LeftEdge[3 * i].EdgeType != this.LeftEdge[(3 * i) + 1].EdgeType && this.LeftEdge[3 * i].EdgeType != this.LeftEdge[(3 * i) + 2].EdgeType)
		//			{
		//				throw new Exception("Data loss during shrink");
		//			}
		//			if (this.RightEdge[3 * i].EdgeType != this.RightEdge[(3 * i) + 1].EdgeType && this.RightEdge[3 * i].EdgeType != this.RightEdge[(3 * i) + 2].EdgeType)
		//			{
		//				throw new Exception("Data loss during shrink");
		//			}
		//		}

		//		smallLeft[i] = this.LeftEdge[3 * i];
		//		smallRight[i] = this.RightEdge[3 * i];
		//	}

		//	return new RectifyRectangle(smallBotLeft, smallTopRight, smallLeft, smallRight, smallTop, smallBot);

		//}


		/// <summary>
		/// Returns the minimum distance of this rectangle's corners to a specified point
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public int MinDistanceFrom(Position p)
		{
			//if the point is inside the rectangle, return 0.
			if (topRight.xPos <= p.xPos && p.xPos <= bottomLeft.xPos &&
				topRight.yPos <= p.yPos && p.yPos <= bottomLeft.yPos)
			{
				return 0;
			}


			int x = Clamp(p.xPos, Left, Right);
			int y = Clamp(p.yPos, Top, Bottom);

			int dl, dr, dt, db;
			dl = Math.Abs(x - Left);
			dr = Math.Abs(x - Right);
			dt = Math.Abs(y - Top);
			db = Math.Abs(y - Bottom);

			int min = new List<int>() { dl, dr, dt, db }.Min();

			if (min == dt) return (new Position(x, Top) - p).Magnitude;
			if (min == db) return (new Position(x, Bottom) - p).Magnitude;
			if (min == dl) return (new Position(Left, y) - p).Magnitude;

			//else
			return (new Position(Right, y) - p).Magnitude;

		}

		private int Clamp(int x, int lower, int upper)
		{
			return Math.Max(lower, Math.Min(upper, x));
		}

		private int DistanceBetween(Position p, Position other)
		{
			return (int)Math.Sqrt(((p.xPos - other.xPos) * (p.xPos - other.xPos)) + ((p.yPos - other.yPos) * (p.yPos - other.yPos)));
		}

		//internal bool ContainsPoint(Position position)
		//{
		//	if (position.xPos < this.Right && position.xPos >= this.Left && position.yPos < this.Top && position.yPos >= this.Bottom) return true;

		//	return false;
		//}

		internal bool ContainsPoint(Position position, float positiveOffset = .5f)
		{
			if ((position.xPos + positiveOffset) < this.Right && (position.xPos + positiveOffset) >= this.Left && (position.yPos + positiveOffset) < this.Top && (position.yPos + positiveOffset) >= this.Bottom) return true;

			return false;
		}

		public Position Offset
		{
			get
			{
				return new Position(bottomLeft);
			}
		}

		public int Width
		{
			get
			{
				return topRight.xPos - bottomLeft.xPos;
			}
		}

		public int Height
		{
			get
			{
				return topRight.yPos - bottomLeft.yPos;
			}
		}

		public int Left
		{
			get
			{
				return bottomLeft.xPos;
			}
		}

		public int Top
		{
			get
			{
				return topRight.yPos;
			}
		}

		public int Right
		{
			get
			{
				return topRight.xPos;
			}
		}


		public int Bottom
		{
			get
			{
				return bottomLeft.yPos;
			}
		}

		public int BaseCost { get; internal set; } = 1;
	}


	/// <summary>
	/// An immutable type that holds 2 integer values and a link to the next RectEdge in the RectShape
	/// </summary>
	public class RectEdge
	{
		public Position FirstPosition;
		public Position SecondPosition;
		public EdgeType EdgeType;

		private RectEdge _next = null;
		public RectEdge Next
		{
			get
			{
				return _next;
			}
			set
			{
				if (value == this)
				{
					_next = null; //no self-referential edges
				}
				else
				{
					_next = value;
				}
			}
		}

		/// <summary>
		/// Gets a direction vector for which direction this edge is going. Because we always move clockwise,
		/// direction also tells us what side of the cell we're bordering. (e.g. <1,0> == East direction & North side of the cell)
		/// </summary>
		public Position HeadingVector
		{
			get
			{
				return new Position(SecondPosition.xPos - FirstPosition.xPos, SecondPosition.yPos - FirstPosition.yPos);
			}
		}

		public Direction HeadingDirection
		{
			get
			{
				var direct = this.HeadingVector;
				if (direct.xPos == 0 && direct.yPos >= 1)
				{
					return Direction.North;
				}
				if (direct.xPos == 0 && direct.yPos <= -1)
				{
					return Direction.South;
				}
				if (direct.xPos >= 1 && direct.yPos == 0)
				{
					return Direction.East;
				}
				if (direct.xPos <= -1 && direct.yPos == 0)
				{
					return Direction.West;
				}

				throw new Exception("Unable to determine heading");
			}
		}

		/// <summary>
		/// Returns a new edge with the positions swapped
		/// </summary>
		public RectEdge SwappedPositions
		{

			get
			{
				return new RectEdge(this.SecondPosition, this.FirstPosition, this.EdgeType);
			}


		}

		/// <summary>
		/// given our lines are gridded, this will always be an integer
		/// </summary>
		public int Magnitude
		{
			get
			{

				if (SecondPosition.xPos == FirstPosition.xPos)
				{
					//vertical line
					return Math.Abs(SecondPosition.yPos - FirstPosition.yPos);
				}
				else
				{
					//horizontal line
					return Math.Abs(SecondPosition.xPos - FirstPosition.xPos);
				}

				//return Math.Sqrt(((SecondPosition.xPos - FirstPosition.xPos) * (SecondPosition.xPos - FirstPosition.xPos)) +
				//					((SecondPosition.yPos - FirstPosition.yPos) * (SecondPosition.yPos - FirstPosition.yPos)));
			}
		}

		public RectEdge(Position first, Position second, EdgeType edge)
		{
			FirstPosition = first;
			SecondPosition = second;
			EdgeType = edge;
		}


		//copying this method from the MSDN implementation of "TwoDPoint"
		public override bool Equals(object obj)
		{
			// If parameter is null return false.
			if (obj == null)
			{
				return false;
			}

			// If parameter cannot be cast to RectEdge return false.
			if (!(obj is RectEdge r))
			{
				return false;
			}

			// Return true if the fields match:
			return (FirstPosition.Equals(r.FirstPosition) && (SecondPosition.Equals(r.SecondPosition)) && EdgeType == r.EdgeType);
		}

		public bool Equals(RectEdge r)
		{
			// If parameter is null return false:
			if (r == null)
			{
				return false;
			}

			// Return true if the fields match:
			return (FirstPosition.Equals(r.FirstPosition) && (SecondPosition.Equals(r.SecondPosition)) && EdgeType == r.EdgeType);
		}

		//copied from MSDN's "TwoDPoint" implementation
		public override int GetHashCode()
		{
			return (FirstPosition.GetHashCode() ^ SecondPosition.GetHashCode()) + (int)EdgeType;
		}


		public override string ToString()
		{
			return "First: " + FirstPosition.ToString() + " Second: " + SecondPosition.ToString() + " Edge: " + EdgeType.ToString();
		}
	}

	/// <summary>
	/// An immutable type that holds 2 edge values and whether or not they're part of a maximum matching
	/// </summary>
	public class RectEdgeEdge
	{
		public RectEdge FirstEdge { get; private set; }
		public RectEdge SecondEdge { get; private set; }
		public bool IsInMatching { get; set; } = false;

		public RectEdgeEdge(RectEdge first, RectEdge second, bool isInMatching)
		{
			FirstEdge = first;
			SecondEdge = second;
			IsInMatching = isInMatching;
		}

		//copying this method from the MSDN implementation of "TwoDPoint"
		public override bool Equals(object obj)
		{
			// If parameter is null return false.
			if (obj == null)
			{
				return false;
			}

			// If parameter cannot be cast to RectEdge return false.
			if (!(obj is RectEdgeEdge ee))
			{
				return false;
			}

			// Return true if the edges match (order doesn't matter)
			return (FirstEdge.Equals(ee.FirstEdge) && (SecondEdge.Equals(ee.SecondEdge))) || (FirstEdge.Equals(ee.SecondEdge) && (SecondEdge.Equals(ee.FirstEdge)));
		}

		public bool Equals(RectEdgeEdge ee)
		{
			// If parameter is null return false:
			if (ee == null)
			{
				return false;
			}

			// Return true if the edges match (order doesn't matter)
			return (FirstEdge.Equals(ee.FirstEdge) && (SecondEdge.Equals(ee.SecondEdge))) || (FirstEdge.Equals(ee.SecondEdge) && (SecondEdge.Equals(ee.FirstEdge)));
		}

		//copied from MSDN's "TwoDPoint" implementation
		public override int GetHashCode()
		{
			return (FirstEdge.GetHashCode() ^ SecondEdge.GetHashCode());
		}


		public override string ToString()
		{
			return "First: " + FirstEdge.ToString() + " Second: " + SecondEdge.ToString();
		}
	}

	/// <summary>
	/// Helper tuple-class for breadth first search
	/// </summary>
	public class BFSFlowNode
	{
		//the node we're investigating
		public RectFlowNode Node { get; set; }

		//the path to get to the node. Node is not part of this list.
		public List<RectFlowNode> Path { get; set; }

		public BFSFlowNode(RectFlowNode node, List<RectFlowNode> path)
		{
			this.Node = node; this.Path = path;
		}
	}

	/// <summary>
	/// Node class used with calculating Maximal Flow Algorithm
	/// </summary>
	public class RectFlowNode
	{
		public FlowType FlowType
		{
			get; private set;
		}

		public RectEdge Edge { get; private set; }

		public HashSet<RectFlowNode> DestinationNodes { get; private set; }

		/// <summary>
		/// Adds a directional link from this node to the linked node
		/// </summary>
		/// <param name="linkedNode"></param>
		public void AddLink(RectFlowNode linkedNode)
		{
			DestinationNodes.Add(linkedNode);
		}

		/// <summary>
		/// Reverses a directional link from this node to the other
		/// </summary>
		/// <param name="other"></param>
		public void ReverseLink(RectFlowNode other)
		{
			if (DestinationNodes.Contains(other))
			{
				DestinationNodes.Remove(other);
				other.AddLink(this);
			}
			else
			{
				//nothing to do
			}
		}

		public RectFlowNode(RectEdge edge, FlowType ftype)
		{
			this.Edge = edge;
			this.FlowType = ftype;
			DestinationNodes = new HashSet<RectFlowNode>();
		}

		public override string ToString()
		{
			return Edge.ToString();
		}

		/// <summary>
		/// Navigates through the linked nodes looking for the sinkNode
		/// </summary>
		/// <param name="sinkNode"></param>
		/// <returns></returns>
		internal bool FindLinkedNode(RectFlowNode sinkNode, out List<RectFlowNode> destNodes)
		{
			if (this == sinkNode)
			{
				destNodes = new List<RectFlowNode>() { this };
				return true;
			}
			else
			{
				destNodes = new List<RectFlowNode>(this.DestinationNodes);
				return false;
			}
		}
	}

	[Serializable]
	/// <summary>
	/// An immutable type that holds 2 integer values
	/// </summary>
	public class Position
	{
		public readonly int xPos;
		public readonly int yPos;

		public int Magnitude
		{
			get
			{
				return Math.Abs(xPos) + Math.Abs(yPos);
			}
		}

		public Position(int x, int y)
		{
			xPos = x;
			yPos = y;
		}

		public Position(Position p)
		{
			xPos = p.xPos;
			yPos = p.yPos;
		}

		//copying this method from the MSDN implementation of "TwoDPoint"
		public override bool Equals(object obj)
		{
			// If parameter is null return false.
			if (obj == null)
			{
				return false;
			}

			// If parameter cannot be cast to Position return false.
			Position p = obj as Position;
			if ((System.Object)p == null)
			{
				return false;
			}

			// Return true if the fields match:
			return (xPos == p.xPos) && (yPos == p.yPos);
		}

		public bool Equals(Position p)
		{
			// If parameter is null return false:
			if ((object)p == null)
			{
				return false;
			}

			// Return true if the fields match:
			return (xPos == p.xPos) && (yPos == p.yPos);
		}

		//copied from MSDN's "TwoDPoint" implementation
		public override int GetHashCode()
		{
			return xPos ^ yPos;
		}

		//adds two positions together into a new point
		public static Position operator +(Position a, Position b)
		{
			return new Position(a.xPos + b.xPos, a.yPos + b.yPos);
		}

		//subtracts two positions together into a new point
		public static Position operator -(Position a, Position b)
		{
			return new Position(a.xPos - b.xPos, a.yPos - b.yPos);
		}

		public override string ToString()
		{
			return "X: " + xPos + " Y: " + yPos;
		}
	}
}
