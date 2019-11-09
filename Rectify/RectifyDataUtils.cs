using System;
using System.Collections.Generic;
using System.Text;

namespace RectifyUtils
{
	public class RectifyDataUtils
	{

		/// <summary>
		/// For a given 2d integer array, reorder the data so that it is in 
		/// "Quadrant 1" with 0,0 being the bottom-left most data point.
		/// </summary>
		/// <param name="original"></param>
		/// <returns></returns>
		public static T[,] RotateAndSwapData<T>(T[,] original)
		{
			int maxY = original.GetUpperBound(1);
			int maxX = original.GetUpperBound(0);

			var inverseArray = new T[maxY + 1, maxX + 1];

			//Swap X&Y, then invert the X values
			for (int x = 0; x <= maxY; x++)
			{
				//Loop through the height of the map
				for (int y = 0; y <= maxX; y++)
				{
					inverseArray[x, maxX - y] = original[y, x];
				}
			}

			return inverseArray;
		}

		public static RectNode[,] GridLatticeToRectNode2D(GridLattice<int> input)
		{
			throw new NotImplementedException();
		}
	}
}
