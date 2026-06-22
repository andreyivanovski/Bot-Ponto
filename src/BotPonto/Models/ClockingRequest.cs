using System.Text.Json.Serialization;

namespace BotPonto.Models;

public class ClockingRequest
{
    [JsonPropertyName("latitude")]
    public string Latitude { get; set; } = string.Empty;

    [JsonPropertyName("longitude")]
    public string Longitude { get; set; } = string.Empty;

    [JsonPropertyName("timezone")]
    public int Timezone { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("hour")]
    public int Hour { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;
}
