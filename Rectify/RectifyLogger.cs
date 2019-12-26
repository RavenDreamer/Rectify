using System;
using System.Collections.Generic;

namespace RectifyUtils
{
	internal class RectifyLogger
	{

		static List<string> StringLog { get; set; } = new List<string>();

		internal static string OutputShape(RectShape rectShape)
		{
			string pointPerim = "RECTSHAPE:";
			foreach (RectEdge perimEdge in rectShape.Perimeter)
			{
				pointPerim += perimEdge.FirstPosition.xPos + "," + perimEdge.FirstPosition.yPos + "|";
			}

			return pointPerim;
		}

		internal static void RecordShape(RectShape rectShape)
		{
#if debug
			string pointPerim = "RECTSHAPE:";
			foreach (RectEdge perimEdge in rectShape.Perimeter)
			{
				pointPerim += perimEdge.FirstPosition.xPos + "," + perimEdge.FirstPosition.yPos + "|";
			}

			StringLog.Add(pointPerim);
#endif
		}

		internal static void RecordChordCut(RectEdge rectEdge)
		{
#if debug
			string chordNote = "CHORDEDGE:";
			chordNote += rectEdge.FirstPosition.xPos + "," + rectEdge.FirstPosition.yPos + "|";
			chordNote += rectEdge.SecondPosition.xPos + "," + rectEdge.SecondPosition.yPos + "|";

			StringLog.Add(chordNote);
#endif
		}

		static public string PrintLog()
		{
			return string.Join(System.Environment.NewLine, StringLog);
		}

		internal static void RecordHole(RectShape hole)
		{
#if debug
			string pointPerim = "RECTHOLE:";
			foreach (RectEdge perimEdge in hole.Perimeter)
			{
				pointPerim += perimEdge.FirstPosition.xPos + "," + perimEdge.FirstPosition.yPos + "|";
			}

			StringLog.Add(pointPerim);
		}
#endif
	}
}