using Mapsui;
using Mapsui.ArcGIS;
using Mapsui.ArcGIS.ImageServiceProvider;
using Mapsui.Cache;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.UI.Maui;
using Polygon = NetTopologySuite.Geometries.Polygon;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        const int ZOOM_DELAY = 5000;
        
        const string ARC_IMAGE_SERVICE_URL = "https://landsat2.arcgis.com/arcgis/rest/services/LandsatGLS/MS/ImageServer";
        private ArcGISImageCapabilities? _capabilities;
        public List<Polygon> PolygonList { get; set; }
        public MapControl MyMapControl { get; set; }

        public MainPage()
        {
            RunOnMainThreadAsync(async () =>
            {
                InitializeComponent();

                var mapControl = new Mapsui.UI.Maui.MapControl()
                {
                    Map = new Mapsui.Map()
                    {
                        CRS = "EPSG:3857",
                    }
                };

                MRect bbox = new(
                    -20037508.34278
                    , -20037508.34278
                    , 20037508.34278
                    , 20037508.34278
                );

                mapControl.Map.Navigator.OverridePanBounds = bbox;

                var layer = await CreateLayerAsync(default);
                mapControl.Map?.Layers.Add(layer);

                mapControl.Loaded += MapControl_Loaded;
                MyMapControl = mapControl;

                Content = mapControl;

            });
        }

        private void MapControl_Loaded(object sender, EventArgs e)
        {
            double x = -9188151.36056;
            double y = 3235144.74039;

            var point = new MPoint(x, y);
            Task.Delay(ZOOM_DELAY).ContinueWith(task =>
            {
                MyMapControl.Map.Navigator.CenterOnAndZoomTo(point, resolution: 1000, 500, Mapsui.Animations.Easing.CubicOut);
            });
        }


        public async Task<ILayer> CreateLayerAsync(IUrlPersistentCache defaultCache)
        {
            var provider = await CreateArcGisHttpProviderAsync(defaultCache);
            provider.CRS = "3857";

            return new ImageLayer("NOAA NCEI")
            {
                DataSource = provider,
                Style = new RasterStyle()
            };
        }

        private async Task<ArcGISImageServiceProvider> CreateArcGisHttpProviderAsync(IUrlPersistentCache defaultCache)
        {
            var capabilitiesHelper = new CapabilitiesHelper(defaultCache);
            capabilitiesHelper.CapabilitiesReceived += CapabilitiesReceived;
            capabilitiesHelper.GetCapabilities(ARC_IMAGE_SERVICE_URL, CapabilitiesType.ImageServiceCapabilities);

            while (_capabilities == null)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }

            return new ArcGISImageServiceProvider(_capabilities, persistentCache: defaultCache);

        }

        private void CapabilitiesReceived(object? sender, System.EventArgs e)
        {
            _capabilities = sender as ArcGISImageCapabilities;
        }

        public static Task RunOnMainThreadAsync(Func<Task> op)
        {
            var tcs = new TaskCompletionSource();

            Application.Current?.Dispatcher.Dispatch(async () =>
            {
                try
                {
                    await op();
                    tcs.SetResult();
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });

            return tcs.Task;
        }
    }
}