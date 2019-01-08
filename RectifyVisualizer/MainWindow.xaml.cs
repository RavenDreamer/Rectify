using RectifyTest;
using RectifyUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RectifyVisualizer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		List<RectShape> initialData = new List<RectShape>();

		public MainWindow()
		{
			InitializeComponent();
			DisplayFinalData();
			DisplayInitialData();

		}

		private void DisplayFinalData()
		{
			var result = Rectify.GetRectNodes(TestData.BinaryConcaveShapeNoHoles());
			var output = Rectify.TraverseShapeOutlines(result);
			initialData = output;
			var polygons = Rectify.FindVertsFromEdges(output);

			var subpolygons = Rectify.FirstLevelDecomposition(polygons[1]);
			var outergons = new List<RectShape>();
			outergons.AddRange(Rectify.FirstLevelDecomposition(polygons[0]));
			outergons.AddRange(Rectify.FirstLevelDecomposition(polygons[2]));
			outergons.AddRange(Rectify.FirstLevelDecomposition(polygons[3]));

			var subsubPolygons = new List<RectShape>();
			foreach (var sp in subpolygons)
			{
				subsubPolygons.AddRange(Rectify.SecondLevelDecomposition(sp));
			}

			var suboutergons = new List<RectShape>();
			foreach (var sp in outergons)
			{
				suboutergons.AddRange(Rectify.SecondLevelDecomposition(sp));
			}

			foreach (RectShape rs in subsubPolygons)
			{

				Polygon p = new Polygon
				{
					Points = new PointCollection(rs.Vertices.Select(v => new Point(v.Vert.xPos * 12, v.Vert.yPos * 12)))
				};

				p.Fill = Brushes.OrangeRed;

				p.Stroke = Brushes.Black;
				p.StrokeThickness = 1;
				p.Width = 250;
				p.Height = 250;
				FinalDataCanvas.Children.Add(p);
			}

			foreach (RectShape rs in suboutergons)
			{

				Polygon p = new Polygon
				{
					Points = new PointCollection(rs.Vertices.Select(v => new Point(v.Vert.xPos * 12, v.Vert.yPos * 12)))
				};

				p.Stroke = Brushes.Black;
				p.StrokeThickness = 1;
				p.Width = 250;
				p.Height = 250;
				FinalDataCanvas.Children.Add(p);
			}
		}

		private void DisplayInitialData()
		{
			int i = 0;
			foreach (RectShape rs in initialData)
			{

				Polygon p = new Polygon
				{
					Points = new PointCollection(rs.Vertices.Select(v => new Point(v.Vert.xPos * 12, v.Vert.yPos * 12)))
				};
				if (i == 1)
				{
					p.Fill = Brushes.Blue;
				}
				p.Stroke = Brushes.Black;
				p.StrokeThickness = 2;
				p.Width = 250;
				p.Height = 250;
				InitialDataCanvas.Children.Add(p);
				i++;
			}
		}
	}
}
