using GpxFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
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
                double xPoint = (i * 200) / ((double)(points.Length - 1));
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
            List<GpxTrack> tracks = gpxLdr.GetTracks();
            GpxPoint startPoint = tracks[0].Segs[0];
            double startLat = startPoint.Latitude;
            double startLon = startPoint.Longitude;

            foreach (GpxPoint p in tracks[0].Segs)
            {
                double lat = p.Latitude;
                double lon = p.Longitude;
                int distance = System.Convert.ToInt32(Geo.distance(lat, lon, 0, startLat, startLon, 0, false));
                if(!elevations.ContainsKey(distance))
                    elevations.Add(distance, System.Convert.ToInt32(p.Elevation));
            }
        }

        private SimpleOrientationSensor _simpleorientation; 

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateGridLines(linegraphCanvas.Width, linegraphCanvas.Height, 5, 5);

            _simpleorientation = SimpleOrientationSensor.GetDefault();
            // Assign an event handler for the sensor orientation-changed event 
            if (_simpleorientation != null)
            {
                _simpleorientation.OrientationChanged += new TypedEventHandler<SimpleOrientationSensor, SimpleOrientationSensorOrientationChangedEventArgs>(OrientationChanged);
            } 
        }

        private async void OrientationChanged(object sender, SimpleOrientationSensorOrientationChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SimpleOrientation orientation = e.Orientation;
                switch (orientation)
                {
                    case SimpleOrientation.NotRotated:
                        //Portrait Up 
                        txtOrientation.Text = "Not Rotated";
                        break;
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        //LandscapeLeft 
                        txtOrientation.Text = "Rotated 90 Degrees Counterclockwise";
                        
                        break;
                    case SimpleOrientation.Rotated180DegreesCounterclockwise:
                        //PortraitDown 
                        txtOrientation.Text = "Rotated 180 Degrees Counterclockwise";
                        
                        break;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        //LandscapeRight 
                        txtOrientation.Text = "Rotated 270 Degrees Counterclockwise";
                        linegraphCanvas.Width = 1000;
                        break;
                    case SimpleOrientation.Faceup:
                        txtOrientation.Text = "Faceup";
                        break;
                    case SimpleOrientation.Facedown:
                        txtOrientation.Text = "Facedown";
                        break;
                    default:
                        txtOrientation.Text = "Unknown orientation";
                        break;
                }
            });
        } 

        Dictionary<int, int> elevations = new Dictionary<int, int>();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Point[] graphPoints = new Point[380];
            DrawLineChart(graphPoints, 409, 1300);
        }


    }
}
