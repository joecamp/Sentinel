using System.Diagnostics;
using System.Text.Json;

using Sentinel.Core.Models;

namespace Sentinel.Core.Services;

/// <summary>
/// Converts raw OpenSky JSON state vectors into typed AircraftState domain objects.
/// Each state vector is a JSON array with positionally-defined fields.
/// This class handles missing/null values defensively — OpenSky data is noisy.
/// </summary>
public static class OpenSkyResponseParser
{
    private const double MetersToFeet = 3.28084;
    private const double MpsToKnots = 1.94384;
    private const double MpsToFpm = 196.850;

    public static IReadOnlyList<AircraftState> Parse(OpenSkyResponse response)
    {
        if (response.States is not { } statesElement ||
            statesElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<AircraftState>();
        }

        var results = new List<AircraftState>();

        foreach (var stateVector in statesElement.EnumerateArray())
        {
            if (stateVector.ValueKind != JsonValueKind.Array)
                continue;

            var elements = stateVector.EnumerateArray().ToArray();
            if (elements.Length < 18)
                continue; // malformed — skip

            try
            {
                results.Add(ParseStateVector(elements));
            }
            catch (Exception ex)
            {
                // Log and continue — one bad record shouldn't kill the batch
                // In Step 4 we'll inject ILogger here
                Debug.WriteLine($"Failed to parse state vector: {ex.Message}");
            }
        }

        return results;
    }

    private static AircraftState ParseStateVector(JsonElement[] e)
    {
        double? longitude = GetDouble(e[5]);
        double? latitude  = GetDouble(e[6]);
        GeoCoords? coords = latitude.HasValue && longitude.HasValue
            ? new GeoCoords(latitude.Value, longitude.Value)
            : null;

        return new AircraftState
        {
            Icao24 = GetString(e[0]) ?? string.Empty,
            Callsign = (GetString(e[1]) ?? string.Empty).Trim(),
            OriginCountry = GetString(e[2]) ?? string.Empty,
            LastPositionTime = GetUnixTime(e[3]),
            LastContact = GetUnixTime(e[4]) ?? DateTimeOffset.UtcNow,
            Position = coords,
            BaroAltitudeFeet = GetDouble(e[7]) is double baro ? baro * MetersToFeet : null,
            OnGround = GetBool(e[8]),
            VelocityKnots = GetDouble(e[9]) is double vel ? vel * MpsToKnots : null,
            TrueTrackDegrees = GetDouble(e[10]),
            VerticalRateFpm = GetDouble(e[11]) is double vr ? vr * MpsToFpm : null,
            GeoAltitudeFeet = GetDouble(e[13]) is double geo ? geo * MetersToFeet : null,
            Squawk = GetString(e[14]),
            PositionSource = (PositionSource)GetInt(e[16]),
            Category = GetInt(e[17])
        };
    }

    // --- Safe extraction helpers ---
    // JsonElement can be Null, Number, String, etc. — never assume.

    private static string? GetString(JsonElement e) =>
        e.ValueKind == JsonValueKind.String ? e.GetString() : null;

    private static double? GetDouble(JsonElement e) =>
        e.ValueKind == JsonValueKind.Number ? e.GetDouble() : null;

    private static bool GetBool(JsonElement e) =>
        e.ValueKind == JsonValueKind.True;

    private static int GetInt(JsonElement e) =>
        e.ValueKind == JsonValueKind.Number ? e.GetInt32() : 0;

    private static DateTimeOffset? GetUnixTime(JsonElement e) =>
        e.ValueKind == JsonValueKind.Number
            ? DateTimeOffset.FromUnixTimeSeconds(e.GetInt64())
            : null;
}