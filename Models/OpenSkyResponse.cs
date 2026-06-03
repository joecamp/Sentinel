using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sentinel.Core.Models;

public class OpenSkyResponse
{
    [JsonPropertyName("time")]
    public long Time { get; init; }

    // Deserialize states as raw JsonElement so we can index into
    // the heterogeneous inner arrays manually in the parser.
    [JsonPropertyName("states")]
    public JsonElement? States { get; init; }
}
