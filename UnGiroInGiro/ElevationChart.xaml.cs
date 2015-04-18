using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace UnGiroInGiro
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ElevationChart : Page
    {
        GpxFS.GpxLoader gpxLdr;

        public ElevationChart()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Method to generate the Grid Lines
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        public void GenerateGridLines(double width, double height, int rows, int columns)
        {
            GeometryGroup glines = new GeometryGroup();
            Windows.UI.Xaml.Shapes.Path p = new Windows.UI.Xaml.Shapes.Path();
            for (int currentRow = 1; currentRow < rows; ++currentRow)
            {
                double pos = (height * currentRow) / ((double)rows);
                LineGeometry line = new LineGeometry();
                line.StartPoint = new Point(0, pos);
                line.EndPoint = new Point(width, pos);
                glines.Children.Add(line);
            }
            for (int currentColumn = 1; currentColumn < columns; ++currentColumn)
            {
                double pos = (width * currentColumn) / ((double)columns);
                LineGeometry line = new LineGeometry();
                line.StartPoint = new Point(pos, 0);
                line.EndPoint = new Point(pos, height);
                glines.Children.Add(line);
            }
            p.Stroke = new SolidColorBrush(Colors.Blue);
            p.Data = glines;
            linegraphCanvas.Children.Add(p);
        }

        

        /// <summary>
        /// Method to Draw Line Graph
        /// </summary>
        /// <param name="points"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        void DrawLineChart(Point[] points, int min, int max)
        {
            linegraphCanvas.Children.Clear(); //Clear All Children
            //Draw the Grid Line
            GenerateGridLines(linegraphCanvas.Width, linegraphCanvas.Height, 5, 5);
            int Range = max - min;
            //The Scale for Chart
            double Scale = (Range == 0) ? 1.0 : 100.0 / ((double)Range);
            PointCollection pointData = new PointCollection();
            //Get the X,Y Point collection based upon value of the Sale. Here the EndSale value is selected
            int prevElev = 0;
            for (int i = 0; i < points.Length; i++)
            {
                int sale = elevations.ContainsKey(i) ? elevations[i] : prevElev;
                prevElev = sale;
                int diff_Max_Min = max - sale;
                double yPoint = ((double)diff_Max_Min) * Scale;
                double xPoint = (i * 100) / ((double)(points.Length - 1));
                points[i] = new Point(xPoint, yPoint);
                pointData.Add(points[i]);
            }
            Polyline pline = new Polyline();
            pline.StrokeThickness = 1;
            pline.Stroke = new SolidColorBrush(Colors.Red);
            pline.Points = pointData;
            linegraphCanvas.Children.Add(pline);
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            gpxLdr = (GpxFS.GpxLoader)e.Parameter;
            
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateGridLines(linegraphCanvas.Width, linegraphCanvas.Height, 5, 5);
            elevations.Add(0, 10);
            elevations.Add(20, 500);
            elevations.Add(30, 600);
            elevations.Add(40, 610);
            elevations.Add(50, 630);
            elevations.Add(100, 700);
            elevations.Add(200, 1000);
            elevations.Add(300, 1300);
            elevations.Add(380, 2000);
        }

        Dictionary<int, int> elevations = new Dictionary<int, int>();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Point[] graphPoints = new Point[380];
            DrawLineChart(graphPoints, 0, 2000);
        }
    }
}
