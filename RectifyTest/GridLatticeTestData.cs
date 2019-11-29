using RectifyUtils;
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
		/// Returns an empty 3x3 gridlattice. If edge data is 0, it's "empty", otherwise
		/// it'll be treated as a wall.
		/// </summary>
		/// <returns></returns>
		public static GridLattice<IRectGrid> EmptyGridLattice()
		{
			GridLattice<IRectGrid> data = new GridLattice<IRectGrid>(3);

			InitGridLattice(data);

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


		static void InitGridLattice(GridLattice<IRectGrid> data)
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
