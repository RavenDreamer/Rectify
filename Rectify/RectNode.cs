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
		North = 3, //TODO: make these 1,2,4,8, once it stops breaking the tests and add "Unknown - 0"
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
		public Position Vert { get; private set; }
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
			this.Vert = point;
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
			return IsConcave == v.IsConcave && Vert.Equals(v.Vert);
		}

		public bool Equals(Vertex v)
		{
			// If parameter is null return false:
			if (v == null)
			{
				return false;
			}

			// Return true if the fields match:
			return IsConcave == v.IsConcave && Vert.Equals(v.Vert);
		}

		//copied from MSDN's "TwoDPoint" implementation
		public override int GetHashCode()
		{
			return Vert.GetHashCode() + (IsConcave ? 1 : 0);
		}


		public override string ToString()
		{
			return "Concave: " + IsConcave + " Vertex: " + Vert.ToString();
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
				if (direct.xPos == 0 && direct.yPos == 1)
				{
					return global::RectifyUtils.Direction.South;
				}
				if (direct.xPos == 0 && direct.yPos == -1)
				{
					return global::RectifyUtils.Direction.North;
				}
				if (direct.xPos == 1 && direct.yPos == 0)
				{
					return global::RectifyUtils.Direction.East;
				}
				if (direct.xPos == -1 && direct.yPos == 0)
				{
					return global::RectifyUtils.Direction.West;
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
		internal List<RectFlowNode> FindLinkedNode(RectFlowNode sinkNode)
		{
			List<RectFlowNode> outList = new List<RectFlowNode>() { this };

			if (this == sinkNode)
			{
				return outList;
			}
			else
			{
				foreach (RectFlowNode rfn in DestinationNodes)
				{
					List<RectFlowNode> retList = rfn.FindLinkedNode(sinkNode);
					if (retList == null)
					{
						continue;
					}
					else
					{
						outList.AddRange(retList);
						return outList;
					}
				}
				//none of the children could find it
				return null;
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
