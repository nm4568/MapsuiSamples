using Mapsui;
using Mapsui.ArcGIS;
using Mapsui.ArcGIS.ImageServiceProvider;
using Mapsui.Cache;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts.Extensions;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using NetTopologySuite.Geometries;
using System.Diagnostics;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Polygon = NetTopologySuite.Geometries.Polygon;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
       
        Polygon _draggedPolygon;
        List<Coordinate> _draggedOrigCoords;
        MPoint _touchStartedScreenPoint { get; set; }
        ILayer _touchStartedLayer { get; set; }
        Polygon _polygon;
        MapControl _mapControl;
        double x = -9188151.36056;
        double y = 3235144.74039;

        public MainPage()
        {
            InitializeComponent();

            _mapControl = new Mapsui.UI.Maui.MapControl()
            {
                Map = new Mapsui.Map()
                {
                    CRS = "EPSG:3857",
                }
            };

            _mapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
            _mapControl.Loaded += MapControl_Loaded;
            _mapControl.TouchStarted += MapControl_TouchStarted;
            _mapControl.TouchMove += MapControl_TouchMove;
            _mapControl.TouchEnded += MapControl_TouchEnded;

            var point = new MPoint(x, y);
            _mapControl.Map.Home = (n) => n.CenterOnAndZoomTo(point, resolution: 1000, 500, Mapsui.Animations.Easing.CubicOut);

            Content = _mapControl;
        }

        private void MapControl_TouchEnded(object sender, Mapsui.UI.TouchedEventArgs e)
        {
            _mapControl.Map.Navigator.PanLock = false;
            _touchStartedScreenPoint = null;
            ClearPolygonFields();
        }

        private void MapControl_TouchMove(object sender, Mapsui.UI.TouchedEventArgs e)
        {
            var point = e.ScreenPoints.First();
            if (_touchStartedLayer?.Name == "Polygons")
            {
                MovePolygon(point, _polygon);
            }
        }

        private void MapControl_TouchStarted(object sender, Mapsui.UI.TouchedEventArgs e)
        {
            var mapInfo = _mapControl.GetMapInfo(e.ScreenPoints.FirstOrDefault());
            _touchStartedLayer = mapInfo.Layer;
            _touchStartedScreenPoint = e.ScreenPoints.First();
        }

        private void MapControl_Loaded(object sender, EventArgs e)
        {
            var baseCoordinate = new Coordinate(x, y);
            _polygon = CreatePolygon(baseCoordinate);
            var polygonLayer = CreatePolygonLayer(_polygon);
            _mapControl.Map.Layers.Add(polygonLayer);
            _mapControl.Refresh();
        }

        private void ClearPolygonFields()
        {
            Debug.WriteLine("Ended");

            _draggedPolygon = null;
            _draggedOrigCoords = null;
        }

        private void MovePolygon(MPoint screenPoint, Polygon polygon)
        {
            Debug.WriteLine($"Polygon Coord 0 X: {polygon.Coordinates.First().X}");
            Debug.WriteLine($"Polygon Coord 0 Y: {polygon.Coordinates.First().Y}");


            if (_draggedPolygon == null)
            {
                _mapControl.Map.Navigator.PanLock = true;
                // set the dragged polygon
                _draggedPolygon = polygon;
                _draggedOrigCoords = polygon.Coordinates.Select(r => new Coordinate(r.X, r.Y)).ToList();
            }
            else if (_touchStartedScreenPoint != null)
            {
                // move polygon with screenpoint 
                for (var i = 0; i < polygon.Coordinates.Length; i++)
                {
                    var coord = polygon.Coordinates[i];
                    var origCoord = _draggedOrigCoords[i];
                    var firstTouchedWorldPoint = _mapControl.Map.Navigator.Viewport.ScreenToWorld(_touchStartedScreenPoint);
                    var currentWorldPoint = _mapControl.Map.Navigator.Viewport.ScreenToWorld(screenPoint);

                    coord.X = Math.Round(currentWorldPoint.X - firstTouchedWorldPoint.X + origCoord.X, 5, MidpointRounding.AwayFromZero);
                    coord.Y = Math.Round(currentWorldPoint.Y - firstTouchedWorldPoint.Y + origCoord.Y, 5, MidpointRounding.AwayFromZero);

                }

                _mapControl.Refresh();
            }
        }

        private ILayer CreatePolygonLayer(Polygon polygon)
        {
            var polygonList = new List<Polygon> { { polygon } };
            var layer = new Layer("Polygons")
            {
                DataSource = new MemoryProvider(polygonList.ToFeatures()),
                Style = new VectorStyle
                {
                    Fill = new Brush(Color.Orange),
                    Outline = new Pen
                    {
                        Color = Color.Orange,
                        Width = 1,
                        PenStyle = PenStyle.DashDotDot,
                        PenStrokeCap = PenStrokeCap.Round
                    }
                },
                Opacity = .3,
                IsMapInfoLayer = true,
            };
            layer.DataSource.CRS = "EPSG:3857";
            return layer;
        }

        private Polygon CreatePolygon(Coordinate baseCoordinate)
        {
            var width = 250000;
            var height = 250000;

            var point = new MPoint(baseCoordinate.X, baseCoordinate.Y);
            var topLeft = new Coordinate(point.X - (width / 2), point.Y - (height / 2));
            var topRight = new Coordinate(point.X + (width / 2), point.Y - (height / 2));
            var bottomRight = new Coordinate(point.X + (width / 2), point.Y + (height / 2));
            var bottomLeft = new Coordinate(point.X - (width / 2), point.Y + (height / 2));

            var ring = new LinearRing(new[] { topLeft, topRight, bottomRight, bottomLeft, topLeft });
            var polygon = new Polygon(ring);

            return polygon;
        }
    }
}