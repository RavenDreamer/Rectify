using System;
using System.Collections.Generic;
using System.Text;

namespace RectifyUtils
{
	/// <summary>
	/// A gridlattice filled with these can be converted into pathing data for a RectNode[,]
	/// </summary>
	public interface IRectGrid
	{
		int PathGroup();
		int PathModifier();
	}

	public class RectGridCell : IRectGrid
	{

		int _PathGroup { get; set; }
		int _PathModifier { get; set; }

		public RectGridCell(int pathGroup, int pathModifier)
		{
			this._PathGroup = pathGroup;
			this._PathModifier = pathModifier;
		}


		public int PathGroup()
		{
			return _PathGroup;
		}

		public int PathModifier()
		{
			return _PathModifier;
		}
	}


	/// <summary>
	/// A dense 1-d array that maps to an M by N grid w/ N.E.W.S. edge data for each cell.
	/// </summary>
	public class GridLattice<T>
	{

		/// <summary>
		/// Returns a flat array containing all the elements contained in this GridLattice
		/// </summary>
		/// <returns></returns>
		public T[] ToArray()
		{
			T[] outArray = new T[LatticeElements.Length];
			Array.Copy(LatticeElements, outArray, LatticeElements.Length);
			return outArray;
		}

		public int Width { get; private set; }
		private int RowSize
		{
			get
			{
				return (2 * Width) + 1;
			}
		}

		public int Height { get; private set; }

		public int Count { get { return LatticeElements.Length; } }

		private T[] LatticeElements { get; set; }

		/// <summary>
		/// Fills an instantiated GridLattice with the given elements. (e.g. use when unserializing) 
		/// Throws an error if the arrayLength does not match. 
		/// </summary>
		/// <param name="elements"></param>
		public GridLattice<T> ReplaceElements(T[] contents)
		{
			if (contents.Length != LatticeElements.Length)
			{
				throw new IndexOutOfRangeException("Expected array of " + LatticeElements.Length + " but received array of " + contents.Length);
			}
			else
			{
				Array.Copy(contents, LatticeElements, LatticeElements.Length);
			}

			return this;
		}

		/// <summary>
		/// Instantiates a square GridLattice with the given height & width
		/// </summary>
		/// <param name="mag"></param>
		public GridLattice(int mag)
		{
			Width = mag;
			Height = mag;
			//Total elements in lattice are sum of:
			// width * height (the cells)
			// (height + 1) * width (the top/bottom edges)
			// (width + 1) * height (the west/east edges)
			// summed: 3*width*height + width + height
			LatticeElements = new T[3 * mag * mag + mag + mag];
		}

		/// <summary>
		/// Instantiates an empty GridLattice with the given width and height
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public GridLattice(int width, int height)
		{
			Width = width;
			Height = height;
			//Total elements in lattice are sum of:
			// width * height (the cells)
			// (height + 1) * width (the top/bottom edges)
			// (width + 1) * height (the west/east edges)
			// summed: 3*width*height + width + height
			LatticeElements = new T[(3 * width * height) + width + height];
		}

		/// <summary>
		/// this formula is basically magic -- mathamagic!
		/// 2 * xIndex (x offset, accounting for the left edge of each cell. This is "effective" width)
		/// (RowSize+Width)* yIndex (amount of lattice elements between Rows. This is "effective" Height)
		/// Width + 2 (constant offset, so that 0,0 picks the first cell element, rather than it's bottom-edge)
		/// </summary>
		/// <param name="xIndex"></param>
		/// <param name="yIndex"></param>
		/// <returns></returns>
		private int GetCellOffset(int xIndex, int yIndex)
		{
			return (2 * xIndex) + (RowSize + Width) * yIndex + Width + 2 - 1; //-1 to account for zero indexing
		}

		/// <summary>
		/// Returns the LatticeElement along the "dir" edge of the cell at xIndex, yIndex
		/// </summary>
		/// <param name="xIndex"></param>
		/// <param name="yIndex"></param>
		/// <param name="dir"></param>
		/// <returns></returns>
		public T this[int xIndex, int yIndex, Direction dir = Direction.Center]
		{
			get
			{
				//start with cell offset, and calc relative to that.
				//
				int CellOffset = GetCellOffset(xIndex, yIndex);
				switch (dir)
				{
					//these are the easy ones
					case Direction.Center:
						return LatticeElements[CellOffset];
					case Direction.East:
						return LatticeElements[CellOffset + 1];
					case Direction.West:
						return LatticeElements[CellOffset - 1];
					// these also seem pretty magical, but there are very 
					//mathy reasons for them to point to the intended element
					case Direction.North:
						//x Index - 1 + rowSize == an offset to point to the cell directly
						//above this one.
						return LatticeElements[CellOffset + RowSize - xIndex - 1];
					case Direction.South:
						// - x Index - width - 1 == an offset to point to the cell directly
						//below this one
						return LatticeElements[CellOffset - Width - xIndex - 1];
					default:
						throw new IndexOutOfRangeException("Invalid direction used with GridLattice");
				}
			}
			set
			{
				//start with cell offset, and calc relative to that.
				//
				int CellOffset = GetCellOffset(xIndex, yIndex);
				switch (dir)
				{
					//these are the easy ones
					case Direction.Center:
						LatticeElements[CellOffset] = value;
						break;
					case Direction.East:
						LatticeElements[CellOffset + 1] = value;
						break;
					case Direction.West:
						LatticeElements[CellOffset - 1] = value;
						break;
					// these also seem pretty magical, but there are very 
					//mathy reasons for them to point to the intended element
					case Direction.North:
						//x Index - 1 + rowSize == an offset to point to the cell directly
						//above this one.
						LatticeElements[CellOffset + RowSize - xIndex - 1] = value;
						break;
					case Direction.South:
						// - x Index - width - 1 == an offset to point to the cell directly
						//below this one
						LatticeElements[CellOffset - Width - xIndex - 1] = value;
						break;
					default:
						throw new IndexOutOfRangeException("Invalid direction used with GridLattice");
				}
			}
		}

	}
}
