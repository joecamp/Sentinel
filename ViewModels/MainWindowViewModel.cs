using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    }
}