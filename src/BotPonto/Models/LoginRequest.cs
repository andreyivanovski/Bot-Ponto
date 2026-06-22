using System.Text.Json.Serialization;

namespace BotPonto.Models;

public class LoginRequest
{
    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("redirectUrl")]
    public string RedirectUrl { get; set; } = string.Empty;

    [JsonPropertyName("restUrl")]
    public string RestUrl { get; set; } = string.Empty;
}
