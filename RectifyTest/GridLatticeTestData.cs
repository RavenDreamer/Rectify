﻿using RectifyUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RectifyTest
{
	public class GridLatticeTestData
	{
		/// <summary>
		/// Returns an even NxN gridlattice, defaulting to 3
		/// </summary>
		/// <returns></returns>
		public static GridLattice<IRectGrid> EmptyGridLattice(int i = 3)
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(i);

			InitGridLattice(data);

			return data;
		}


		/// <summary>
		/// Returns an empty ixj gridlattice.
		/// </summary>
		/// <returns></returns>
		public static GridLattice<IRectGrid> EmptyGridLattice(int i, int j)
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(i, j);

			InitGridLattice(data);

			return data;
		}


		/// <summary>
		/// 10x10 Gridlattice w/ 6 randomish wall intersections on it
		/// </summary>
		/// <returns></returns>
		public static GridLattice<IRectGrid> IntersectionLattice()
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(10);

			InitGridLattice(data);

			data[1, 2, Direction.East] = new RectGridCell(1, 1);
			data[1, 2, Direction.South] = new RectGridCell(1, 1);

			data[1, 5, Direction.East] = new RectGridCell(1, 1);
			data[1, 5, Direction.North] = new RectGridCell(1, 1);

			data[2, 7, Direction.West] = new RectGridCell(1, 1);
			data[2, 7, Direction.North] = new RectGridCell(1, 1);

			data[4, 2, Direction.East] = new RectGridCell(1, 1);
			data[4, 2, Direction.North] = new RectGridCell(1, 1);
			data[5, 2, Direction.West] = new RectGridCell(1, 1);

			data[8, 1, Direction.North] = new RectGridCell(1, 1);
			data[8, 1, Direction.West] = new RectGridCell(1, 1);
			data[8, 2, Direction.West] = new RectGridCell(1, 1);

			return data;
		}

		public static GridLattice<IRectGrid> CornersLattice()
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(10);

			InitGridLattice(data);

			data[1, 1, Direction.West] = new RectGridCell(1, 1);
			data[1, 1, Direction.South] = new RectGridCell(1, 1);

			data[2, 6, Direction.West] = new RectGridCell(1, 1);
			data[2, 6, Direction.North] = new RectGridCell(1, 1);

			data[4, 6, Direction.East] = new RectGridCell(1, 1);
			data[4, 6, Direction.South] = new RectGridCell(1, 1);

			data[6, 4, Direction.North] = new RectGridCell(1, 1);
			data[6, 4, Direction.East] = new RectGridCell(1, 1);

			return data;
		}

		/// <summary>
		/// 5x5 Gridlattice w/ most of a wall blocking it.
		/// </summary>
		/// <returns></returns>
		public static GridLattice<IRectGrid> KeyholeApertureLattice()
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(5);

			InitGridLattice(data);

			data[0, 2, Direction.North] = new RectGridCell(1, 1);
			data[1, 2, Direction.North] = new RectGridCell(1, 1);
			//data[2, 2, Direction.North] = new RectGridCell(1, 1);
			data[3, 2, Direction.North] = new RectGridCell(1, 1);
			data[4, 2, Direction.North] = new RectGridCell(1, 1);

			return data;
		}

		/// <summary>
		/// 5x5 Gridlattice w/ most of a wall blocking it.
		/// </summary>
		/// <returns></returns>
		public static GridLattice<IRectGrid> VertKeyholeApertureLattice()
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(5);

			InitGridLattice(data);

			data[2, 0, Direction.West] = new RectGridCell(1, 1);
			data[2, 1, Direction.West] = new RectGridCell(1, 1);
			//data[2, 2, Direction.West] = new RectGridCell(1, 1);
			data[2, 3, Direction.West] = new RectGridCell(1, 1);
			data[2, 4, Direction.West] = new RectGridCell(1, 1);

			return data;
		}

		/// <summary>
		/// 5x5 Gridlattice w/ a wall bisecting it.
		/// </summary>
		/// <returns></returns>
		public static GridLattice<IRectGrid> HorizBisectedLattice()
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(5);

			InitGridLattice(data);

			data[0, 2, Direction.North] = new RectGridCell(1, 1);
			data[1, 2, Direction.North] = new RectGridCell(1, 1);
			data[2, 2, Direction.North] = new RectGridCell(1, 1);
			data[3, 2, Direction.North] = new RectGridCell(1, 1);
			data[4, 2, Direction.North] = new RectGridCell(1, 1);

			return data;
		}

		/// <summary>
		/// Returns a circular 5x5 gridlattice. If edge data is 0, it's "empty", otherwise
		/// it'll be treated as a wall.
		/// </summary>
		/// <returns></returns>
		public static GridLattice<IRectGrid> EdgeTorusGridLattice()
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(5);

			InitGridLattice(data);

			//should also be able to do this by setting the center to a different pathgroup
			data[2, 2, Direction.North] = new RectGridCell(1, 1);
			data[2, 2, Direction.South] = new RectGridCell(1, 1);
			data[2, 2, Direction.East] = new RectGridCell(1, 1);
			data[2, 2, Direction.West] = new RectGridCell(1, 1);

			return data;
		}

		public static GridLattice<IRectGrid> SingleVertEdgeGridLattice()
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(5);

			InitGridLattice(data);
			data[2, 2, Direction.West] = new RectGridCell(1, 1);

			return data;
		}

		public static GridLattice<IRectGrid> SingleHorizEdgeGridLattice()
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(5);

			InitGridLattice(data);
			data[2, 2, Direction.South] = new RectGridCell(1, 1);

			return data;
		}

		public static GridLattice<IRectGrid> CenterCellTorusGridLattice()
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(5);

			InitGridLattice(data);

			//should also be able to do this by setting the center to a different pathgroup
			data[2, 2, Direction.Center] = new RectGridCell(1, 1);

			return data;
		}


		public static void InitGridLattice(GridLattice<IRectGrid> data)
		{
			for (int x = 0; x < data.Width; x++)
			{
				for (int y = 0; y < data.Height; y++)
				{
					if (x == 0)
					{
						data[x, y, Direction.West] = new RectGridCell(0, 1);
					}
					if (y == 0)
					{
						data[x, y, Direction.South] = new RectGridCell(0, 1);
					}

					data[x, y, Direction.North] = new RectGridCell(0, 1);
					data[x, y, Direction.East] = new RectGridCell(0, 1);
					data[x, y, Direction.Center] = new RectGridCell(0, 1);
				}
			}

		}
	}
}
