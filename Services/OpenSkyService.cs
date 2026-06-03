using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;

using Sentinel.Core.Models;

namespace Sentinel.Core.Services;

/// <summary>
/// Fetches aircraft state data from the OpenSky Network REST API.
/// 
/// Threading notes:
///   - All public methods are async and return to the caller's SynchronizationContext.
///   - HttpClient is intentionally shared (not created per-call) to avoid socket exhaustion.
///   - Anonymous access: max 1 request per 10 seconds, 400 credits/day.
/// 
/// IDisposable: HttpClient owns unmanaged socket resources. We dispose it when
/// the service is disposed. In a DI container this service should be registered
/// as a singleton so the client is reused across the application lifetime.
/// </summary>
public sealed class OpenSkyService : IOpenSkyService, IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    // Bounding box for continental US — adjust or make configurable later
    private const double DefaultLatMin = 24.0;
    private const double DefaultLatMax = 50.0;
    private const double DefaultLongMin = -125.0;
    private const double DefaultLongMax = -65.0;

    public OpenSkyService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://opensky-network.org"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        // OpenSky requires a User-Agent
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SentinelDemo/1.0");
    }

    /// <summary>
    /// Fetches all aircraft states within a geographic bounding box.
    /// Returns an empty list if the API returns no data or on HTTP errors.
    /// Throws OperationCanceledException if the token is cancelled.
    /// </summary>
    public async Task<IReadOnlyList<AircraftState>> GetStatesAsync(
        double lamin = DefaultLatMin,
        double lomin = DefaultLongMin,
        double lamax = DefaultLatMax,
        double lomax = DefaultLongMax,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var url = $"/api/states/all?lamin={lamin}&lomin={lomin}&lamax={lamax}&lomax={lomax}&extended=1";

        // GetFromJsonAsync handles deserialization and will throw on non-2xx
        // We catch HttpRequestException to degrade gracefully (API down, rate limited)
        try
        {
            var response = await _httpClient
                .GetFromJsonAsync<OpenSkyResponse>(url, cancellationToken)
                .ConfigureAwait(false); // Don't force return to UI thread in library code

            if (response is null)
                return Array.Empty<AircraftState>();

            return OpenSkyResponseParser.Parse(response);
        }
        catch (HttpRequestException ex)
        {
            // Caller decides how to handle — log and return empty for now
            Debug.WriteLine($"OpenSky API error: {ex.StatusCode} {ex.Message}");
            return Array.Empty<AircraftState>();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _disposed = true;
    }
}

/// <summary>Interface extracted for testability — lets us mock in unit tests.</summary>
public interface IOpenSkyService
{
    Task<IReadOnlyList<AircraftState>> GetStatesAsync(
        double lamin = 24.0, double lomin = -125.0,
        double lamax = 50.0, double lomax = -65.0,
        CancellationToken cancellationToken = default);
}