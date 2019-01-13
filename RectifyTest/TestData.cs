﻿using System;
using System.Collections.Generic;
using System.Text;
using RectifyUtils;

namespace RectifyTest
{
	public class TestData
	{
		public static int[,] BinaryConcaveShapeNoHoles()
		{
			return new int[,]
			{
				{ 0,0,0,0,0,1,1,0,0,0,0,0 },
				{ 0,0,0,1,1,1,1,0,0,0,0,0 },
				{ 0,0,0,1,1,1,1,0,0,0,0,0 },
				{ 0,0,0,0,0,1,1,1,1,1,1,0 },
				{ 0,0,0,0,0,1,1,1,1,1,1,0 },
				{ 0,0,0,0,0,1,1,1,1,1,1,0 },
				{ 0,0,0,0,0,1,1,1,1,1,1,0 },
				{ 0,0,0,0,0,1,1,1,1,1,1,0 },
				{ 0,0,0,0,0,1,1,1,1,1,1,0 },
				{ 0,1,1,1,1,1,1,1,1,1,0,0 },
				{ 0,1,1,1,1,1,1,1,1,1,0,0 },
				{ 0,0,0,1,1,1,1,1,1,1,1,1 },
				{ 0,0,0,1,1,1,1,1,1,1,1,1 },
				{ 0,0,0,1,1,1,1,0,0,0,0,0 },
				{ 0,0,0,1,1,1,1,0,0,0,0,0 },
				{ 0,0,0,1,1,1,1,0,0,0,0,0 },
				{ 0,0,0,1,1,0,0,0,0,0,0,0 }

			};
		}

		internal static Tuple<HashSet<RectEdgeEdge>, List<RectFlowNode>> GetAlternatingNodesMatching()
		{
			HashSet<RectEdgeEdge> edges = new HashSet<RectEdgeEdge>();
			List<RectFlowNode> nodes = new List<RectFlowNode>();

			RectEdge rone = new RectEdge(new Position(1, 1), new Position(1, 1), EdgeType.None);
			RectEdge rtwo = new RectEdge(new Position(2, 2), new Position(2, 2), EdgeType.None);
			RectEdge rthree = new RectEdge(new Position(3, 3), new Position(3, 3), EdgeType.None);
			RectEdge rfour = new RectEdge(new Position(4, 4), new Position(4, 4), EdgeType.None);
			RectEdge rfive = new RectEdge(new Position(5, 5), new Position(5, 5), EdgeType.None);
			RectEdge rsix = new RectEdge(new Position(6, 6), new Position(6, 6), EdgeType.None);
			RectEdge rseven = new RectEdge(new Position(7, 7), new Position(7, 7), EdgeType.None);
			RectEdge reight = new RectEdge(new Position(8, 8), new Position(8, 8), EdgeType.None);

			RectEdge cone = new RectEdge(new Position(-1, -1), new Position(-1, -1), EdgeType.None);
			RectEdge ctwo = new RectEdge(new Position(-2, -2), new Position(-2, -2), EdgeType.None);
			RectEdge cthree = new RectEdge(new Position(-3, -3), new Position(-3, -3), EdgeType.None);
			RectEdge cfour = new RectEdge(new Position(-4, -4), new Position(-4, -4), EdgeType.None);
			RectEdge cfive = new RectEdge(new Position(-5, -5), new Position(-5, -5), EdgeType.None);
			RectEdge csix = new RectEdge(new Position(-6, -6), new Position(-6, -6), EdgeType.None);
			RectEdge cseven = new RectEdge(new Position(-7, -7), new Position(-7, -7), EdgeType.None);
			RectEdge ceight = new RectEdge(new Position(-8, -8), new Position(-8, -8), EdgeType.None);


			RectEdgeEdge m1 = new RectEdgeEdge(rtwo, cthree, true);
			RectEdgeEdge m5 = new RectEdgeEdge(rsix, cone, true);
			RectEdgeEdge m2 = new RectEdgeEdge(rfour, ceight, true);
			RectEdgeEdge m6 = new RectEdgeEdge(rfive, ctwo, true);
			RectEdgeEdge m3 = new RectEdgeEdge(rone, cfour, true);
			RectEdgeEdge m4 = new RectEdgeEdge(rthree, cfive, true);


			RectFlowNode nr2 = new RectFlowNode(rtwo, FlowType.horizontal);
			RectFlowNode nr6 = new RectFlowNode(rsix, FlowType.horizontal);
			RectFlowNode nr4 = new RectFlowNode(rfour, FlowType.horizontal);
			RectFlowNode nr5 = new RectFlowNode(rfive, FlowType.horizontal);
			RectFlowNode nr7 = new RectFlowNode(rseven, FlowType.horizontal);
			RectFlowNode nr1 = new RectFlowNode(rone, FlowType.horizontal);
			RectFlowNode nr8 = new RectFlowNode(reight, FlowType.horizontal);
			RectFlowNode nr3 = new RectFlowNode(rthree, FlowType.horizontal);

			RectFlowNode nc6 = new RectFlowNode(csix, FlowType.vertical);
			RectFlowNode nc7 = new RectFlowNode(cseven, FlowType.vertical);
			RectFlowNode nc1 = new RectFlowNode(cone, FlowType.vertical);
			RectFlowNode nc3 = new RectFlowNode(cthree, FlowType.vertical);
			RectFlowNode nc2 = new RectFlowNode(ctwo, FlowType.vertical);
			RectFlowNode nc8 = new RectFlowNode(ceight, FlowType.vertical);
			RectFlowNode nc5 = new RectFlowNode(cfive, FlowType.vertical);
			RectFlowNode nc4 = new RectFlowNode(cfour, FlowType.vertical);

			nr2.AddLink(nc1);
			nr2.AddLink(nc3);
			nr2.AddLink(nc6);
			nr2.AddLink(nc7);

			nr6.AddLink(nc1);
			nr6.AddLink(nc2);
			nr6.AddLink(nc3);
			nr6.AddLink(nc6);
			nr6.AddLink(nc7);

			nr4.AddLink(nc2);
			nr4.AddLink(nc8);

			nr5.AddLink(nc2);
			nr5.AddLink(nc8);

			nr7.AddLink(nc2);
			nr7.AddLink(nc8);

			nr1.AddLink(nc2);
			nr1.AddLink(nc4);
			nr1.AddLink(nc5);
			nr1.AddLink(nc8);

			nr8.AddLink(nc2);
			nr8.AddLink(nc4);

			nr3.AddLink(nc4);
			nr3.AddLink(nc5);



			nc6.AddLink(nr2);
			nc6.AddLink(nr6);

			nc7.AddLink(nr2);
			nc7.AddLink(nr6);

			nc1.AddLink(nr2);
			nc1.AddLink(nr6);

			nc3.AddLink(nr2);
			nc3.AddLink(nr6);

			nc2.AddLink(nr2);
			nc2.AddLink(nr6);
			nc2.AddLink(nr4);
			nc2.AddLink(nr5);
			nc2.AddLink(nr7);
			nc2.AddLink(nr1);
			nc2.AddLink(nr8);

			nc8.AddLink(nr4);
			nc8.AddLink(nr5);
			nc8.AddLink(nr7);
			nc8.AddLink(nr1);

			nc5.AddLink(nr6);
			nc5.AddLink(nr1);
			nc5.AddLink(nr3);

			nc4.AddLink(nr1);
			nc4.AddLink(nr3);

			nodes.Add(nc1);
			nodes.Add(nc2);
			nodes.Add(nc3);
			nodes.Add(nc4);
			nodes.Add(nc5);
			nodes.Add(nc6);
			nodes.Add(nc7);
			nodes.Add(nc8);

			nodes.Add(nr1);
			nodes.Add(nr2);
			nodes.Add(nr3);
			nodes.Add(nr4);
			nodes.Add(nr5);
			nodes.Add(nr6);
			nodes.Add(nr7);
			nodes.Add(nr8);

			edges.Add(m1);
			edges.Add(m2);
			edges.Add(m3);
			edges.Add(m4);
			edges.Add(m5);
			edges.Add(m6);

			return new Tuple<HashSet<RectEdgeEdge>, List<RectFlowNode>>(edges, nodes);

		}

		internal static List<RectFlowNode> GetAlternatingZNodes()
		{
			throw new NotImplementedException();
		}
	}
}