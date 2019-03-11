using System;
using System.Collections.Generic;
using System.Text;

namespace RectifyUtils
{
	public class PathOutOfBoundsException : Exception
	{
		public PathOutOfBoundsException(string message) : base(message)
		{
		}
	}
}
