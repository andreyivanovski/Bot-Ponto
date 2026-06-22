using System.Text.Json.Serialization;

namespace BotPonto.Models;

public class TodayClockingsResponse
{
    [JsonPropertyName("hasNext")]
    public bool HasNext { get; set; }

    [JsonPropertyName("items")]
    public List<ClockingItem> Items { get; set; } = [];
}

public class ClockingItem
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("hour")]
    public int Hour { get; set; }

    [JsonPropertyName("latitude")]
    public string? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public string? Longitude { get; set; }

    [JsonPropertyName("direction")]
    public string? Direction { get; set; }
}
