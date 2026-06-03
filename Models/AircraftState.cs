namespace Sentinel.Core.Models;

/// <summary>
/// Represents a single aircraft's state vector from the OpenSky Network API.
/// Altitudes are stored in feet, velocity in knots — converted from metric on ingestion.
/// </summary>
public class AircraftState
{
    public string Icao24 { get; init; } = string.Empty;
    public string Callsign { get; init; } = string.Empty;
    public string OriginCountry { get; init; } = string.Empty;
    public DateTimeOffset? LastPositionTime { get; init; }
    public DateTimeOffset LastContact { get; init; }

    public double? Longitude { get; init; }
    public double? Latitude { get; init; }

    /// <summary>Barometric altitude in feet. Null if not available.</summary>
    public double? BaroAltitudeFeet { get; init; }

    /// <summary>Geometric (GPS) altitude in feet. Null if not available.</summary>
    public double? GeoAltitudeFeet { get; init; }

    public bool OnGround { get; init; }

    /// <summary>Ground speed in knots.</summary>
    public double? VelocityKnots { get; init; }

    /// <summary>True track in degrees (0 = North, clockwise).</summary>
    public double? TrueTrackDegrees { get; init; }

    /// <summary>Vertical rate in feet per minute.</summary>
    public double? VerticalRateFpm { get; init; }

    public string? Squawk { get; init; }
    public PositionSource PositionSource { get; init; }

    public int Category { get; init; }

    public string CategoryString => CategoryIntToString();

    // Derived display helpers — good for data binding later
    public string LatLongDisplay => Latitude.HasValue && Longitude.HasValue
        ? $"({Latitude.Value:N2},{Longitude.Value:N2})"
        : "N/A";

    public string AltitudeDisplay => BaroAltitudeFeet.HasValue
        ? $"{BaroAltitudeFeet.Value:N0} ft"
        : "N/A";

    public string SpeedDisplay => VelocityKnots.HasValue
        ? $"{VelocityKnots.Value:N0} kts"
        : "N/A";

    public string CategoryIntToString()
    {
        if(AircraftCategoryToStringDict.ContainsKey(Category))
        {
            return AircraftCategoryToStringDict[Category];
        }

        return "Category out of bounds";
    }

    public static Dictionary<int, string> AircraftCategoryToStringDict = new()
    {
        [0]  = "No information at all",
        [1]  = "No ADS-B Emitter Category Information",
        [2]  = "Light (< 15500 lbs)",
        [3]  = "Small (15500 to 75000 lbs)",
        [4]  = "Large (75000 to 300000 lbs)",
        [5]  = "High Vortex Large (aircraft such as B-757)",
        [6]  = "Heavy (> 300000 lbs)",
        [7]  = "High Performance (> 5g acceleration and 400 kts)",
        [8]  = "Rotorcraft",
        [9]  = "Glider / sailplane",
        [10] = "Lighter-than-air",
        [11] = "Parachutist / Skydiver",
        [12] = "Ultralight / hang-glider / paraglider",
        [13] = "Reserved",
        [14] = "Unmanned Aerial Vehicle",
        [15] = "Space / Trans-atmospheric vehicle",
        [16] = "Surface Vehicle – Emergency Vehicle",
        [17] = "Surface Vehicle – Service Vehicle",
        [18] = "Point Obstacle (includes tethered balloons)",
        [19] = "Cluster Obstacle",
        [20] = "Line Obstacle"
    };
}

public enum PositionSource
{
    AdsB = 0,
    Asterix = 1,
    Mlat = 2,
    Flarm = 3
}