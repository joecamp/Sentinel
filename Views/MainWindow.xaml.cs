using System.Windows;

using CommunityToolkit.Mvvm.Messaging;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;

using Sentinel.Core.Models;
using Sentinel.Core.ViewModels;

namespace Sentinel
{
    public partial class MainWindow : Window
    {
        private const string MarkerLayerName   = "CenterMarker";
        private const string AircraftLayerName = "AircraftMarkers";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();

            MapControl.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());

            WeakReferenceMessenger.Default.Register<CenterMapMessage>(this, (_, msg) =>
            {
                var boundingBox = ComputeBoundingBox(msg.Latitude, msg.Longitude, msg.RadiusKm);
                var (cx, cy)    = SphericalMercator.FromLonLat(msg.Longitude, msg.Latitude);

                Dispatcher.InvokeAsync(() =>
                {
                    MapControl.Map.Navigator.ZoomToBox(boundingBox);
                    UpdateCenterMarker(cx, cy);
                });
            });

            WeakReferenceMessenger.Default.Register<CenterOnCoordsMessage>(this, (_, msg) =>
            {
                var (x, y) = SphericalMercator.FromLonLat(msg.Longitude, msg.Latitude);

                Dispatcher.InvokeAsync(() =>
                {
                    MapControl.Map.Navigator.CenterOn(x, y);
                });
            });

            WeakReferenceMessenger.Default.Register<FetchAircraftMessage>(this, (_, msg) =>
            {
                Dispatcher.InvokeAsync(() => UpdateAircraftMarkers(msg.Coords));
            });
        }

        private static MRect ComputeBoundingBox(double lat, double lon, double radiusKm)
        {
            double latDelta = radiusKm / 111.0;
            double lonDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));

            var (swx, swy) = SphericalMercator.FromLonLat(lon - lonDelta, lat - latDelta);
            var (nex, ney) = SphericalMercator.FromLonLat(lon + lonDelta, lat + latDelta);

            return new MRect(swx, swy, nex, ney);
        }

        private void UpdateAircraftMarkers(IReadOnlyList<GeoCoords> coords)
        {
            var existing = MapControl.Map.Layers.FirstOrDefault(l => l.Name == AircraftLayerName);
            if (existing != null) MapControl.Map.Layers.Remove(existing);

            var features = coords.Select(c =>
            {
                var (x, y) = SphericalMercator.FromLonLat(c.Longitude, c.Latitude);
                return (IFeature)new PointFeature(new MPoint(x, y));
            }).ToList();

            MapControl.Map.Layers.Add(new MemoryLayer(AircraftLayerName)
            {
                Features = features,
                Style = new SymbolStyle
                {
                    SymbolScale = 0.3,
                    SymbolType = SymbolType.Ellipse,
                    Fill = new Brush(new Mapsui.Styles.Color(234, 88, 12)),
                    Outline = null
                }
            });
        }

        private void UpdateCenterMarker(double cx, double cy)
        {
            var existing = MapControl.Map.Layers.FirstOrDefault(l => l.Name == MarkerLayerName);
            if (existing != null) MapControl.Map.Layers.Remove(existing);

            var centerMarkerFeature = new PointFeature(new MPoint(cx, cy));
            centerMarkerFeature.Styles.Add(new SymbolStyle
            {
                SymbolScale = 0.7,
                Fill = new Brush(new Mapsui.Styles.Color(37, 99, 235)),
                Outline = new Pen(Mapsui.Styles.Color.White, 2)
            });

            MapControl.Map.Layers.Add(new MemoryLayer(MarkerLayerName) { Features = [centerMarkerFeature] });
        }
    }
}