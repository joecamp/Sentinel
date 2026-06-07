using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Sentinel.Core.Models;
using Sentinel.Core.Services;

namespace Sentinel.Core.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly OpenSkyService _service = new();

    public ObservableCollection<AircraftState> AircraftStates { get; } = new();

    [ObservableProperty] private AircraftState? _selectedAircraftState;
    [ObservableProperty] private double _latitude = 37.0;
    [ObservableProperty] private double _longitude = -95.0;
    [ObservableProperty] private double _radius = 500.0;

    public MainWindowViewModel()
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    private void LogSelectedCallsign()
    {
        Debug.WriteLine($"DoubleClick: {SelectedAircraftState?.Callsign ?? "null"}");

        if (SelectedAircraftState?.Position is not { } position)
            return;

        WeakReferenceMessenger.Default.Send(new CenterOnCoordsMessage(position.Latitude, position.Longitude));
    }

    [RelayCommand]
    private async Task UpdateAircraftAsync()
    {
        AircraftStates.Clear();
        await FetchAndPopulate();
    }

    private async Task LoadAsync() => await FetchAndPopulate();

    private async Task FetchAndPopulate()
    {
        double latDelta = Radius / 111.0;
        double lonDelta = Radius / (111.0 * Math.Cos(Latitude * Math.PI / 180.0));

        var states = await _service.GetStatesAsync(
            lamin: Latitude - latDelta,
            lomin: Longitude - lonDelta,
            lamax: Latitude + latDelta,
            lomax: Longitude + lonDelta);

        foreach (var state in states)
            AircraftStates.Add(state);

        var coords = AircraftStates.Select(s => s.Position).OfType<GeoCoords>().ToList();
        WeakReferenceMessenger.Default.Send(new FetchAircraftMessage(coords));

        WeakReferenceMessenger.Default.Send(new CenterMapMessage(Latitude, Longitude, Radius));
    }
}