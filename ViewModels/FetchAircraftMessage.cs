using Sentinel.Core.Models;
namespace Sentinel.Core.ViewModels;

public record FetchAircraftMessage(IReadOnlyList<GeoCoords> Coords);