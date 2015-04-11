using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using GpxFS;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace UnGiroInGiro
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Declare a Geolocator Object (it will be inizialized later)	
        Geolocator geolocator = null;
        IList<BasicGeoposition> positions;
        //This bool will be useful to taking into account if Tracking is ON or OFF
        bool tracking = false;
        MapPolyline track = new MapPolyline();
        string osmTilesFldName = "OsmTiles";

        double distance = 0;
        BasicGeoposition prevPosition;
        double trackedMeters = 5;
        MapIcon mapIcon;

        CoreApplicationView view;
        CoreApplicationView viewSave;
        string gpx;
        MapIcon myPosition = new MapIcon();
        Windows.UI.Xaml.Shapes.Ellipse fence;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            view = CoreApplication.GetCurrentView();
            viewSave = CoreApplication.GetCurrentView();
        }

        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        private void CommandInvokedHandler(IUICommand command)
        {
            if (command.Label == "Ok")
            {
                localSettings.Values["LocationConsent"] = true;
            }
            else
            {
                localSettings.Values["LocationConsent"] = false;
            }
        }

        private async void Consent()
        {
            if (localSettings.Values.ContainsKey("LocationConsent"))
            {
                //User already gave us his agreement for using his position
                if ((bool)localSettings.Values["LocationConsent"] == true)

                    return;
                //If he didn't we ask for it
                else
                {
                    MessageDialog messageDialog = new MessageDialog("Can I use your position?");
                    // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
                    messageDialog.Commands.Add(new UICommand(
                        "Ok",
                        new UICommandInvokedHandler(this.CommandInvokedHandler)));
                    messageDialog.Commands.Add(new UICommand(
                        "Cancel",
                        new UICommandInvokedHandler(this.CommandInvokedHandler)));

                    // Set the command that will be invoked by default
                    messageDialog.DefaultCommandIndex = 0;

                    // Set the command to be invoked when escape is pressed
                    messageDialog.CancelCommandIndex = 1;

                    // Show the message dialog
                    await messageDialog.ShowAsync();
                }
            }

                //Ask for user agreement in using his position
            else
            {
                MessageDialog messageDialog = new MessageDialog("Can I use your position?");
                // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
                messageDialog.Commands.Add(new UICommand(
                    "Ok",
                    new UICommandInvokedHandler(this.CommandInvokedHandler)));
                messageDialog.Commands.Add(new UICommand(
                    "Cancel",
                    new UICommandInvokedHandler(this.CommandInvokedHandler)));

                // Set the command that will be invoked by default
                messageDialog.DefaultCommandIndex = 0;

                // Set the command to be invoked when escape is pressed
                messageDialog.CancelCommandIndex = 1;

                // Show the message dialog
                await messageDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
            myMap.MapServiceToken = "AgIsJWaa5xxbEFWs1-OSOZAIiflVPKECwtkTo7frmLe3B7HTXM44n5fqRyXLmvre";

            Consent();
        }

        private async void GetLocation()
        {
            if (geolocator == null)
                geolocator = new Geolocator();

            geolocator.DesiredAccuracyInMeters = 5;

            var position = await geolocator.GetGeopositionAsync();

            await myMap.TrySetViewAsync(position.Coordinate.Point, 15D);

            var lat = position.Coordinate.Point.Position.Latitude;
            var lon = position.Coordinate.Point.Position.Longitude;
            BasicGeoposition currPosition = new BasicGeoposition() { Latitude = lat, Longitude = lon };

            if (mapIcon == null)
            {
                mapIcon = new MapIcon();
                mapIcon.Image =
                    RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/triangle.png"));
            }

            if (!mapIcon.Visible)
                mapIcon.Visible = true;

            if (!myMap.MapElements.Contains(mapIcon))
                myMap.MapElements.Add(mapIcon);

            mapIcon.Location = new Geopoint(currPosition);
            mapIcon.NormalizedAnchorPoint = new Point(0.5, 0.5);
        }

        private void AddCurrentPosition_Click(object sender, RoutedEventArgs e)
        {
            GetLocation();
        }

        //This is the handler used when the user click on the specific button for track his position
        private void TrackLocation_Click(object sender, RoutedEventArgs e)
        {
            //First of all we have to check for user agreement in use his position. 
            if ((bool)localSettings.Values["LocationConsent"] != true)
            {
                // The user has opted out of Location.
                StatusTextBlock.Text = "Status = I can't use your position.";
                return;
            }
            //Previously we weren't tracking the position, when the user click on the button it means that from now we have to track him
            if (tracking == false)
            {
                //Now let's begin to track position
                positions = new List<BasicGeoposition>();

                //Inizialize Geolocator object
                geolocator = new Geolocator();
                //Set his accuracy. High has better result, but is more expensive for the battery. Otherwise, you can choose Default
                geolocator.DesiredAccuracy = PositionAccuracy.High;
                //This is the distance of required movement in meters, for the location provider to raise a PositionChanged event. Let's see the code:
                geolocator.MovementThreshold = trackedMeters;
                //Event handlers for the StatusChanged and PositionChanged events are registered.
                geolocator.StatusChanged += geolocator_StatusChanged;
                geolocator.PositionChanged += geolocator_PositionChanged;
                //From now position is tracked 
                tracking = true;
                TrackLocationButton.Label = "Stop Track";
                TrackLocationButton.Icon = new SymbolIcon(Symbol.Stop);

                gpx = "<?xml version=\"1.0\"?><gpx creator=\"UnGiroInGiro\" version=\"1.1\" xmlns=\"http://www.topografix.com/GPX/1/1\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd\"><trk>";
                gpx += "<name>" + DateTime.Now.ToUniversalTime() + "</name><trkseg>";
            }
            //Until now we were tracking user position. With the click the user told use to stop it.
            else
            {
                //Remove the 2 event handlers
                geolocator.PositionChanged -= geolocator_PositionChanged;
                geolocator.StatusChanged -= geolocator_StatusChanged;
                //Set the object to null
                geolocator = null;
                //From now position is untracked
                tracking = false;
                TrackLocationButton.Label = "Start Track";
                TrackLocationButton.Icon = new SymbolIcon(Symbol.Play);
            }
        }

        //This is the handler for StatusChanged event. It will tell to the user the status of the tracking
        async void geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            string status = "";

            switch (args.Status)
            {
                case PositionStatus.Disabled:
                    // the application does not have the right capability or the location master switch is off
                    status = "location is disabled in phone settings";
                    break;
                case PositionStatus.Initializing:
                    // the geolocator started the tracking operation
                    status = "initializing";
                    break;
                case PositionStatus.NoData:
                    // the location service was not able to acquire the location
                    status = "no data";
                    break;
                case PositionStatus.Ready:
                    // the location service is generating geopositions as specified by the tracking parameters
                    status = "ready";
                    break;
                case PositionStatus.NotAvailable:
                    status = "not available";
                    // not used in WindowsPhone, Windows desktop uses this value to signal that there is no hardware capable to acquire location information
                    break;
                case PositionStatus.NotInitialized:
                    // the initial state of the geolocator, once the tracking operation is stopped by the user the geolocator moves back to this state

                    break;
            }
            /*This part write the status to the UI element StatusTextBlock*/
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                StatusTextBlock.Text = "Status= " + status;
            });
        }

        //This is the handler for PositionChanged event. It will tell to the user his position
        async void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            //BeginInvoke is used to write the position to the specific UI element
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var lat = args.Position.Coordinate.Point.Position.Latitude;
                var lon = args.Position.Coordinate.Point.Position.Longitude;
                var alt = args.Position.Coordinate.Point.Position.Altitude;

                gpx +=
                    "<trkpt lat=\"" + lat.ToString("0.00000000000000") + "\" lon=\"" + lon.ToString("0.00000000000000") + "\">"
                    + "<ele>" + alt.ToString("0.00000000000000") + "</ele>"
                    + "<time>" + DateTime.Now.ToString(@"yyyy-MM-dd\THH:mm:ss\Z") + "</time>"
                    + "</trkpt>";

                BasicGeoposition currPosition = new BasicGeoposition() { Latitude = lat, Longitude = lon };
                positions.Add(currPosition);

                distance += trackedMeters;

                Geopath path = new Geopath(positions);
                track.StrokeColor = Colors.Red;
                track.StrokeThickness = 6;
                track.Path = path;

                //LatitudeTextBlock.Text = "Latitude: " + lat.ToString("0.00000000000000");
                //LongitudeTextBlock.Text = "Longidude: " + lon.ToString("0.00000000000000");
                double KMs = distance / 1000;
                //distanceTextBlock.Text = "Distance: " + KMs.ToString("0.000");

                myMap.MapElements.Add(track);

                if (mapIcon == null)
                {
                    mapIcon = new MapIcon();
                    mapIcon.Image =
                        RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/triangle.png"));
                }

                if (!mapIcon.Visible)
                    mapIcon.Visible = true;

                if (!myMap.MapElements.Contains(mapIcon))
                    myMap.MapElements.Add(mapIcon);

                mapIcon.Location = new Geopoint(currPosition);
                mapIcon.NormalizedAnchorPoint = new Point(0.5, 0.5);

                track.ZIndex = 10000;
                mapIcon.ZIndex = 10;

                if(fence == null)
                    fence = new Windows.UI.Xaml.Shapes.Ellipse();

                fence.Width = 11;
                fence.Height = 11;
                fence.Stroke = new SolidColorBrush(Colors.Gold);
                fence.StrokeThickness = 6;
                MapControl.SetLocation(fence, new Geopoint(currPosition));
                MapControl.SetNormalizedAnchorPoint(fence, new Point(0.5, 0.5));

                if (!myMap.Children.Contains(fence))
                    myMap.Children.Add(fence);
            });
        }

        private void SaveTrackButton_Click(object sender, RoutedEventArgs e)
        {
            //Frame.Navigate(typeof(SaveTrack),positions);

            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            // Dropdown of file types the user can save the file a
            savePicker.FileTypeChoices.Add("GPX File", new List<string>() { ".gpx" });
            // Default extension if the user does not select a choice explicitly from the dropdown
            savePicker.DefaultFileExtension = ".gpx";
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = DateTime.Now.ToString("yyyyMMdd");
            savePicker.PickSaveFileAndContinue();

            viewSave.Activated += ViewSaveActivated;
        }

        private string PositionsToGpx()
        {
            string gpx = "<?xml version=\"1.0\"?><gpx creator=\"UnGiroInGiro\" version=\"1.1\" xmlns=\"http://www.topografix.com/GPX/1/1\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd\"><trk><name>Nome di Prova</name><trkseg>";

            foreach (BasicGeoposition pos in positions)
                gpx += "<trkpt lat=\"" + pos.Latitude.ToString("0.00000000000000") + "\" lon=\"" + pos.Longitude.ToString("0.00000000000000") + "\"><ele>" + pos.Altitude.ToString("0.00000000000000") + "</ele></trkpt>";

            gpx += "</trkseg></trk></gpx>";
            return gpx;
        }

        private async void ViewSaveActivated(CoreApplicationView sender, IActivatedEventArgs args1)
        {
            FileSavePickerContinuationEventArgs args = args1 as FileSavePickerContinuationEventArgs;

            //string gpx = PositionsToGpx();

            gpx += "</trkseg></trk></gpx>";

            if(args != null)
                await Windows.Storage.FileIO.WriteTextAsync(args.File, gpx);
        }

        private string GetNext(string next)
        {
            switch (next)
            {
                case "a":
                    return "b";
                case "b":
                    return "c";
                case "c":
                    return "a";
                default:
                    return "";
            }
        }

        private void btnOsmOnline_Click(object sender, RoutedEventArgs e)
        {
            var next = "a";

            var httpsource = new HttpMapTileDataSource();

            httpsource.UriRequested += (source, args) =>
            {
                var deferral = args.Request.GetDeferral();
                next = GetNext(next);

                var lev = args.ZoomLevel.ToString();
                var x = args.X.ToString();
                var y = args.Y.ToString();

                string src = "http://" + next + ".tile.openstreetmap.org/" +
                    lev + "/" + x + "/" + y + ".png";
                args.Request.Uri = new Uri(src);
                
                deferral.Complete();
            };

            var ts = new MapTileSource(httpsource);
            myMap.TileSources.Add(ts);
        }

        private void btnOsmOffline_Click(object sender, RoutedEventArgs e)
        {
            MapZoomLevelRange range;
            range.Min = 1;
            range.Max = 19;

            // Create a local data source.
            LocalMapTileDataSource dataSource = new LocalMapTileDataSource(
                "ms-appdata:///local/" + osmTilesFldName + "/{zoomlevel}_{x}_{y}.png");

            // Create a tile source and add it to the Map control.
            MapTileSource tileSource = new MapTileSource(dataSource);
            tileSource.ZoomLevelRange = range;
            myMap.TileSources.Add(tileSource);
        }

        public async void DownloadFile(Uri source, string lev, string x, string y)
        {
            var localAppFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            var osmTilesDir = await localAppFolder.CreateFolderAsync(osmTilesFldName, CreationCollisionOption.OpenIfExists);
            var levDir = await osmTilesDir.CreateFolderAsync(lev, CreationCollisionOption.OpenIfExists);
            var xDir = await levDir.CreateFolderAsync(x, CreationCollisionOption.OpenIfExists);
            StorageFile destinationFile = await xDir.CreateFileAsync(y.ToString() + ".png",
                CreationCollisionOption.ReplaceExisting);

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(source, destinationFile);
        }

        private void btnClearMap_Click(object sender, RoutedEventArgs e)
        {
            myMap.MapElements.Clear();
            myMap.TileSources.Clear();
        }

        private void LoadTrack_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.HomeGroup;
            filePicker.ViewMode = PickerViewMode.List;

            // Filter to include a sample subset of file types
            filePicker.FileTypeFilter.Clear();
            filePicker.FileTypeFilter.Add("*");

            filePicker.PickSingleFileAndContinue();
            view.Activated += ViewActivated;
        }

        private async void ViewActivated(CoreApplicationView sender, IActivatedEventArgs args1)
        {
            FileOpenPickerContinuationEventArgs args = args1 as FileOpenPickerContinuationEventArgs;

            if (args != null)
            {
                if (args.Files.Count == 0) return;

                view.Activated -= ViewActivated;
                StorageFile selectedFile = args.Files[0];
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile copiedFile = await selectedFile.CopyAsync(localFolder, selectedFile.Name, NameCollisionOption.ReplaceExisting);
                
                string content = await Windows.Storage.FileIO.ReadTextAsync(copiedFile);
                GpxFS.GpxLoader gpxLdr = new GpxFS.GpxLoader(content);
                List<GpxTrack> tracks = gpxLdr.GetTracks();
                List<GpxTrack> routes = gpxLdr.GetRoutes();

                var itinerario = new MapPolyline();
                var geopositions = new List<BasicGeoposition>();

                double startLat = 0;
                double startLon = 0;

                if (tracks.Count > 0)
                {
                    foreach (GpxPoint gp in tracks[0].Segs)
                        geopositions.Add(new BasicGeoposition() { Latitude = gp.Latitude, Longitude = gp.Longitude });

                    startLat = tracks[0].Segs[0].Latitude;
                    startLon = tracks[0].Segs[0].Longitude;
                }
                else
                {
                    foreach (GpxPoint gp in routes[0].Segs)
                        geopositions.Add(new BasicGeoposition() { Latitude = gp.Latitude, Longitude = gp.Longitude });

                    startLat = routes[0].Segs[0].Latitude;
                    startLon = routes[0].Segs[0].Longitude;
                }

                itinerario.Path = new Geopath(geopositions);

                itinerario.StrokeThickness = 3.5;

                myMap.MapElements.Add(itinerario);

                Geopoint p = new Geopoint((new BasicGeoposition() { Latitude = startLat, Longitude = startLon }));

                await myMap.TrySetViewAsync(p, 15D);
            }
        }
    }
}
