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
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using GpxFS;
using GpxFS.GpxXml;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace UnGiroInGiro
{
    public class ElevationData
    {
        public string Meter { get; set; }
        public int Elevation { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ElevationChart : Page
    {
        GpxFS.GpxLoader gpxLdr;

        public ElevationChart()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.Loaded += Page_Loaded;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
            gpxLdr = (GpxFS.GpxLoader)e.Parameter;
        }

        void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadChartContents();
        }

        private void LoadChartContents()
        {
            Random rand = new Random();
            List<ElevationData> elevationsList = new List<ElevationData>();
            //elevationsList.Add(new ElevationData() { Meter = "MSFT", Elevation = rand.Next(0, 200) });
            //elevationsList.Add(new ElevationData() { Meter = "AAPL", Elevation = rand.Next(0, 200) });
            //elevationsList.Add(new ElevationData() { Meter = "GOOG", Elevation = rand.Next(0, 200) });
            //elevationsList.Add(new ElevationData() { Meter = "BBRY", Elevation = rand.Next(0, 200) });
            GpxTrack track = gpxLdr.GetTracks()[0];
            //GpxPoint startPoint = track.Segs[0];
            //double lat1 = startPoint.Latitude;
            //double lon1 = startPoint.Longitude;
            //double elev1 = startPoint.Elevation;
            int prevDistance = -1;

            GpxPoint prevPoint = null;
            double distanceSoFar = 0;
            int step = 500;

            foreach (GpxPoint gpt in track.Segs)
            {
                if(prevPoint != null)
                {
                    double lat1 = prevPoint.Latitude;
                    double lon1 = prevPoint.Longitude;
                    double elev1 = prevPoint.Elevation;

                    double lat = gpt.Latitude;
                    double lon = gpt.Longitude;
                    double elev = gpt.Elevation;
                    distanceSoFar += Geo.distance(lat1, lon1, elev1, lat, lon, elev, false);
                    int distance = System.Convert.ToInt32(distanceSoFar / step);
                    if (distance > prevDistance)
                    {
                        elevationsList.Add(new ElevationData() { Meter = (distance * step).ToString(), Elevation = System.Convert.ToInt32(elev) });
                        prevDistance = distance;
                    }
                }

                prevPoint = gpt;
            }


            (LineChart.Series[0] as LineSeries).ItemsSource = elevationsList;
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadChartContents();
        }
    }
}
