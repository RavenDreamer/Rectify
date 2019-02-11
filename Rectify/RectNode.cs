using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace RectifyUtils
{
	public enum EdgeType
	{
		None,
		Wall,
		Aperture,
		BurnedHoleEdge, //the outside edge of a hole that's already been catalogued.
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
		public List<Vertex> Vertices { get; set; } = new List<Vertex>();

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
				output += re.FirstPosition.xPos + "," + re.FirstPosition.yPos + System.Environment.NewLine;
			}

			return output;
		}
	}

	/// <summary>
	/// Represents a rectangle (4 vertices), a perimeter of edges, a list of which rectangles
	/// this rectangle neighbors, and which edges allow connections to those neighbors
	/// </summary>
	public class RectifyRectangle
	{

		public class EdgePair
		{
			public EdgeType Edge { get; set; }
			public RectifyRectangle Neighbor { get; set; }
			public EdgePair(EdgeType edge, RectifyRectangle neighbor)
			{
				Edge = edge;
				Neighbor = neighbor;
			}
		}

		private readonly EdgePair[] LeftEdge;
		private readonly EdgePair[] RightEdge;
		private readonly EdgePair[] TopEdge;
		private readonly EdgePair[] BottomEdge;

		private readonly Position topLeft;
		private readonly Position bottomRight;

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
			topLeft = shape.Vertices.First().VertPosition;
			bottomRight = topLeft;
			for (int i = 1; i < 4; i++)
			{
				if (shape.Vertices[i].VertPosition.xPos < topLeft.xPos ||
					shape.Vertices[i].VertPosition.yPos < topLeft.yPos)
				{
					topLeft = shape.Vertices[i].VertPosition;
				}

				if (shape.Vertices[i].VertPosition.xPos > bottomRight.xPos ||
					shape.Vertices[i].VertPosition.yPos > bottomRight.yPos)
				{
					bottomRight = shape.Vertices[i].VertPosition;
				}
			}

			//instantiate the edgearrays

			//get all the edges w/ firstPosition x == topLeft.x && secondPosition x == topLeft.x
			LeftEdge = new EdgePair[this.Height];
			var workingEdges = shape.Perimeter.FindAll(e => e.FirstPosition.xPos == topLeft.xPos && e.SecondPosition.xPos == topLeft.xPos).OrderBy(e => e.SecondPosition.yPos).ToArray();
			for (int i = 0; i < workingEdges.Count(); i++)
			{
				LeftEdge[i] = new EdgePair(workingEdges[i].EdgeType, null);
			}

			RightEdge = new EdgePair[this.Height];
			workingEdges = shape.Perimeter.FindAll(e => e.FirstPosition.xPos == bottomRight.xPos && e.SecondPosition.xPos == bottomRight.xPos).OrderBy(e => e.FirstPosition.yPos).ToArray();
			for (int i = 0; i < workingEdges.Count(); i++)
			{
				RightEdge[i] = new EdgePair(workingEdges[i].EdgeType, null);
			}

			//get all the edges w/ firstPosition y == topLeft.y && secondPosition y == topLeft.y
			TopEdge = new EdgePair[this.Width];
			workingEdges = shape.Perimeter.FindAll(e => e.FirstPosition.yPos == topLeft.yPos && e.SecondPosition.yPos == topLeft.yPos).OrderBy(e => e.FirstPosition.xPos).ToArray();
			for (int i = 0; i < workingEdges.Count(); i++)
			{
				TopEdge[i] = new EdgePair(workingEdges[i].EdgeType, null);
			}

			BottomEdge = new EdgePair[this.Width];
			workingEdges = shape.Perimeter.FindAll(e => e.FirstPosition.yPos == bottomRight.yPos && e.SecondPosition.yPos == bottomRight.yPos).OrderBy(e => e.SecondPosition.xPos).ToArray();
			for (int i = 0; i < workingEdges.Count(); i++)
			{
				BottomEdge[i] = new EdgePair(workingEdges[i].EdgeType, null);
			}
		}

		/// <summary>
		/// Returns the minimum distance of this rectangle's corners to a specified point
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public int MinDistanceFrom(Position p)
		{
			//if the point is inside the rectangle, return 0.
			if (topLeft.xPos <= p.xPos && p.xPos <= bottomRight.xPos &&
				topLeft.yPos <= p.yPos && p.yPos <= bottomRight.yPos)
			{
				return 0;
			}

			List<int> distList = new List<int>(4);
			distList.Add(DistanceBetween(p, topLeft));
			distList.Add(DistanceBetween(p, new Position(topLeft.xPos, bottomRight.yPos)));
			distList.Add(DistanceBetween(p, new Position(bottomRight.xPos, topLeft.yPos)));
			distList.Add(DistanceBetween(p, bottomRight));
			distList.Sort();
			return distList.First();

		}

		private int DistanceBetween(Position p, Position other)
		{
			return (int)Math.Sqrt(((p.xPos - other.xPos) * (p.xPos - other.xPos)) + ((p.yPos - other.yPos) * (p.yPos - other.yPos)));
		}

		public Position Offset
		{
			get
			{
				return new Position(topLeft);
			}
		}

		public int Width
		{
			get
			{
				return bottomRight.xPos - topLeft.xPos;
			}
		}

		public int Height
		{
			get
			{
				return bottomRight.yPos - topLeft.yPos;
			}
		}
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
					return Direction.South;
				}
				if (direct.xPos == 0 && direct.yPos <= -1)
				{
					return Direction.North;
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
