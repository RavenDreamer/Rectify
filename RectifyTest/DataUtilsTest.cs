using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RectifyUtils;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace RectifyTest
{
	[TestClass]
	public class DataUtilsTest
	{

		[TestMethod]
		[TestCategory("DataUtils")]
		public void TwoDArrayInitializedDataTest()
		{
			var initialData = new string[,]
			{
				{ "02","12","22","32", },
				{ "01","11","21","31", },
				{ "00","10","20","30", },
			};

			var rotatedData = RectifyDataUtils.RotateAndSwapData(initialData);

			Assert.AreNotEqual("00", initialData[0, 0], "data was not rotated as expected");

			Assert.AreEqual("00", rotatedData[0, 0], "Data was not rotated successfully as expected");
			Assert.AreEqual("32", rotatedData[3, 2], "Data was not rotated successfully as expected");
			Assert.AreEqual("30", rotatedData[3, 0], "Data was not rotated successfully as expected");
			Assert.AreEqual("02", rotatedData[0, 2], "Data was not rotated successfully as expected");
		}


		[TestMethod]
		[TestCategory("DataUtils")]
		public void LatticeGetBoundsTest()
		{
			var result = Rectify.MakeRectangles(GridLatticeTestData.EmptyGridLattice());
			Assert.AreEqual(1, result.Count, "Did not get 1 initial rectangles as expected");

			var pathfinder = new RectifyPathfinder(result, true);

			var bounds = pathfinder.GetRectBordersFromPoint(new Position(0, 0));
			Assert.AreEqual(3, bounds.Item2.xPos, "did not get 3 width as expected");


			var altLattice = new GridLattice<IRectGrid>(10);
			GridLatticeTestData.InitGridLattice(altLattice);
			altLattice[0, 0, Direction.East] = new RectGridCell(1, 1);
			result = Rectify.MakeRectangles(altLattice);
			Assert.AreEqual(4, result.Count, "Did not get 4 rectangles as expected");
			var altfinder = new RectifyPathfinder(result, true);

			//because one of the 4 rectangles is the wall, we should expect to get exactly 3 distinct rectangles
			//here
			HashSet<Position> uniques = new HashSet<Position>();
			for (int x = 0; x < 10; x++)
			{
				for (int y = 0; y < 10; y++)
				{
					uniques.Add(altfinder.GetRectBordersFromPoint(new Position(x, y)).Item1);
				}
			}
			Assert.AreEqual(3, uniques.Count, "Had more uniques than expected");
		}

		[TestMethod]
		[TestCategory("DataUtils")]
		public void GridLatticeTest()
		{
			//run everything three times, with grids where W > H, H > W, and H == W

			for (int z = 3; z <= 5; z++)
			{
				var width = z; //123(45)
				var height = 4; //ABCD
								//left edge == LL
								//right edge == RR
								//top edge == NN
								//bottom edge == SS

				GridLattice<string> testLattice = new GridLattice<string>(width, height);

				for (int i = 0; i < height; i++)
				{
					for (int j = 0; j < width; j++)
					{
						testLattice[j, i] = j + GetHeightCode(i); //Each Cell should contain a string formatted e.g. 2B or 1A
					}
				}

				//Set the West edge
				for (int j = 0; j < height; j++)
				{
					for (int i = 0; i < width; i++)
					{
						if (i == 0)
						{
							testLattice[i, j, Direction.West] = "LL" + testLattice[i, j]; //Each Edge should contain a string based on the two cells it borders e.g. LL1A or 1A1B
						}
						else
						{
							testLattice[i, j, Direction.West] = testLattice[i - 1, j] + testLattice[i, j];
						}
					}
				}
				//Set the East edge
				for (int j = 0; j < height; j++)
				{
					for (int i = 0; i < width; i++)
					{
						if (i == width - 1)
						{
							testLattice[i, j, Direction.East] = testLattice[i, j] + "RR"; //Each Edge should contain a string based on the two cells it borders e.g. LL1A or 1A1B
						}
						else
						{
							testLattice[i, j, Direction.East] = testLattice[i, j] + testLattice[i + 1, j];
						}
					}
				}
				//Set the North edge
				for (int j = 0; j < height; j++)
				{
					for (int i = 0; i < width; i++)
					{
						if (j == height - 1)
						{
							testLattice[i, j, Direction.North] = testLattice[i, j] + "NN"; //Each Edge should contain a string based on the two cells it borders e.g. LL1A or 1A1B
						}
						else
						{
							testLattice[i, j, Direction.North] = testLattice[i, j] + testLattice[i, j + 1];
						}
					}
				}
				//Set the South edge
				for (int j = 0; j < height; j++)
				{
					for (int i = 0; i < width; i++)
					{
						if (j == 0)
						{
							testLattice[i, j, Direction.South] = "SS" + testLattice[i, j]; //Each Edge should contain a string based on the two cells it borders e.g. LL1A or 1A1B
						}
						else
						{
							testLattice[i, j, Direction.South] = testLattice[i, j - 1] + testLattice[i, j];
						}
					}
				}

				//All cells & edges set to unique values.
				//should have 3(width*height) + width + height unique values
				HashSet<string> uniqueVals = new HashSet<string>();

				for (int j = 0; j < height; j++)
				{
					for (int i = 0; i < width; i++)
					{
						uniqueVals.Add(testLattice[i, j]);
						uniqueVals.Add(testLattice[i, j, Direction.North]);
						uniqueVals.Add(testLattice[i, j, Direction.East]);
						uniqueVals.Add(testLattice[i, j, Direction.West]);
						uniqueVals.Add(testLattice[i, j, Direction.South]);
					}
				}
				Assert.AreEqual((3 * width * height + width + height), uniqueVals.Count, "Error setting or getting lattice elements for z = " + z);

				//for square 1,1, change its neighbors and verify that it's neighbors edges are then what we expect.
				testLattice[1, 1, Direction.South] = "south";
				Assert.AreEqual("south", testLattice[1, 0, Direction.North]);

				testLattice[1, 1, Direction.North] = "north";
				Assert.AreEqual("north", testLattice[1, 2, Direction.South]);

				testLattice[1, 1, Direction.West] = "west";
				Assert.AreEqual("west", testLattice[0, 1, Direction.East]);

				testLattice[1, 1, Direction.East] = "east";
				Assert.AreEqual("east", testLattice[2, 1, Direction.West]);
			}
		}

		private string GetHeightCode(int i)
		{
			switch (i)
			{
				case 0:
					return "A";
				case 1:
					return "B";
				case 2:
					return "C";
				case 3:
					return "D";
				case 4:
					return "E";
				default:
					return "ZZZ";

			}

		}
	}
}
